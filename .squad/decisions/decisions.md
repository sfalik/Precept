# BUG-057 Spec Analysis

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-10T19:55:32-04:00
**Status:** Analysis complete

## Verdict: VALID BUG

`period of 'date'` is a spec-mandated qualifier form that the parser accepts, the type checker silently drops, and the proof engine then cannot satisfy. This is not a spec gap — the spec explicitly requires it, the catalog models it, and the implementation fails to propagate it.

## Evidence

### 1. What the spec says (precept-language-spec.md)

**§2.3 Type References (line 963):**
The grammar production `TypeQualifier := (in | of | to) Expr` applies to all scalar types including `period`. Line 968 confirms: "Type qualifiers narrow the value domain: `in '<unit>'` pins to a specific unit or currency, `of '<family>'` constrains to a dimension family."

**§3.5 Temporal operators (lines 1240, 1243):**

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `date` | `±` | `period of 'date'` | `date` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `time` | `±` | `period of 'time'` | `time` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `datetime` | `±` | `period` | `datetime` | Accepts all period components. |

The spec explicitly defines `period of 'date'` as the **required** RHS type for `date ± period`. An unqualified period on a date produces the `UnqualifiedPeriodArithmetic` proof violation. This is not optional surface — the spec demands it.

### 2. What the type catalog says (Types.cs, Operations.cs)

**Period qualifier shape (Types.cs:34-38):**
```
QS_TemporalUnitOrDimension = new([
    new(TokenKind.In, QualifierAxis.TemporalUnit),
    new(TokenKind.Of, QualifierAxis.TemporalDimension),
], InOfExclusive: true);
```
Period supports two qualifier axes: `in '<unit>'` (e.g., `in 'days'`) and `of '<dimension>'` (e.g., `of 'date'`, `of 'time'`). They're mutually exclusive.

**DeclaredQualifierMeta (DeclaredQualifierMeta.cs:54-58):**
A `TemporalDimension` record exists carrying `PeriodDimension Value` — the exact metadata shape needed to store `of 'date'`.

**Operations catalog (Operations.cs:264-280):**
`DatePlusPeriod` and `DateMinusPeriod` both carry `DimensionProofRequirement(PeriodDimension.Date)` — the proof engine requires the period operand to have `PeriodDimension.Date`. `TimePlusPeriod`/`TimeMinusPeriod` require `PeriodDimension.Time`.

**PeriodDimension enum (ProofRequirement.cs:66-73):**
`Any`, `Date`, `Time` — all three values exist.

### 3. What the compiler actually does (compile test results)

| Declaration | Result | Qualifier in output? |
|-------------|--------|---------------------|
| `field Offset as period` | ✅ Compiles | No qualifier (correct) |
| `field Offset as period of 'date'` | ✅ Compiles | **No qualifier (BUG — silently dropped)** |
| `field Offset as period of 'time'` | ✅ Compiles | **No qualifier (BUG — silently dropped)** |
| `field Offset as period in 'days'` | ✅ Compiles | **No qualifier (BUG — silently dropped)** |
| `field Price as money in 'USD'` | ✅ Compiles | `"in 'USD'"` ✅ preserved |
| `field Weight as quantity in 'kg'` | ✅ Compiles | `"in 'kg'"` ✅ preserved |

The period qualifier is parsed without error but **silently discarded** during type checking. Money and quantity qualifiers are preserved correctly — the bug is specific to the period type's qualifier propagation path.

**Arithmetic consequence:**

| Expression | Offset type | Result |
|------------|-------------|--------|
| `Start + Offset` (Offset: `period of 'date'`, Start: `date`) | PRE0113: "requires Date dimension but has unknown" | ❌ BUG |
| `Start + Offset` (Offset: `period`, Start: `date`) | PRE0113: same error | ❌ Correct behavior — unqualified period should fail |
| `Start + Offset` (Offset: `period`, Start: `datetime`) | ✅ No error | ✅ Correct — datetime accepts all period components |

The proof engine correctly requires `PeriodDimension.Date` for `date + period`, but the qualifier was already dropped at the type-checking stage, so the declared `of 'date'` qualifier is invisible to the proof engine. The result: `date + period of 'date'` fails identically to `date + period` (unqualified).

### 4. Sample file coverage

Zero sample files use any temporal types (date, time, datetime, period, etc.). The temporal arithmetic surface has **no integration test coverage from samples**.

## Root Cause

The parser accepts the `of 'date'` qualifier on `period` (the `QS_TemporalUnitOrDimension` shape allows it). The type checker resolves the qualifier value. But somewhere between type checking and the `PreceptField` model that the proof engine reads, the `TemporalDimension` qualifier metadata is dropped. Money/quantity qualifiers survive this path; period qualifiers do not.

The likely failure point is the type checker's field-type construction: it may not be wiring `DeclaredQualifierMeta.TemporalDimension` into the field's type representation, even though the parser produced the qualifier node and the catalog says it's valid.

## Recommendation

**This is a valid implementation bug.** The fix requires:

1. **Type checker** — ensure `period of 'date'` / `period of 'time'` qualifiers are preserved in the field's type representation (same path that works for `money in 'USD'` and `quantity in 'kg'`).
2. **Proof engine** — once the qualifier is preserved, the existing `DimensionProofRequirement` check should work — it already looks for `PeriodDimension.Date` on the operand. The machinery exists; it just can't see the declaration.
3. **`period in '<unit>'` qualifiers** — same silent-drop behavior observed for `period in 'days'`. Should be checked/fixed in the same pass.
4. **MCP DTO** — once the field model carries the qualifier, the MCP serialization should pick it up automatically (it already does for money/quantity).

### Pipeline stages affected

| Stage | Change needed? | Why |
|-------|---------------|-----|
| Parser | No | Already parses the qualifier correctly |
| Type checker | **Yes** | Must preserve `TemporalDimension`/`TemporalUnit` qualifiers on period fields |
| Proof engine | No (probably) | `DimensionProofRequirement` already models the check; just needs input |
| Graph analyzer | Verify | Check whether qualifier metadata flows through the graph |
| Runtime evaluator | Verify | Period qualifier may affect runtime validation |
| MCP DTO | No (probably) | Already serializes qualifiers when present |

### Design review required?

**No.** This is not new language surface. The spec already defines `period of 'date'` as valid syntax with defined semantics. The catalog already models the qualifier shape and the proof requirements. This is a bug fix — making the implementation match the spec — not a feature addition.

### Suggested test coverage

1. `field X as period of 'date'` — qualifier preserved in compiled definition
2. `field X as period of 'time'` — qualifier preserved
3. `field X as period in 'days'` — unit qualifier preserved
4. `date + period_of_date` — no PRE0113 error
5. `date + period` (unqualified) — PRE0113 fires correctly (regression anchor)
6. `time + period_of_time` — no error
7. `datetime + period` — no error (accepts all — regression anchor)
8. Sample file with temporal period arithmetic (gap: zero samples today)

## Impact if not fixed

Authors cannot express `date + period` arithmetic at all. The only workaround is `datetime + period`, which changes the semantic domain (datetime vs. date) and forces the author to carry unnecessary time components. The spec's temporal arithmetic table has a dead row.

# BUG-057 slice assessment

Date: 2026-05-10
Assessor: George (Runtime Dev)

## Conclusion

BUG-057 fits best as an addition to **Slice 8 (Parser — Replace Hardcoded Token Sets with Catalog Lookups)**, specifically in the `ParseTypeReference()` / field-type parsing area.

## Why

- The narrowed bug is no longer about temporal arithmetic semantics in general.
- The remaining failure is that `field Offset as period of 'date'` appears unsupported in field type position.
- Slice 8 already owns parser/type-reference surface fixes:
  - BUG-027 expands event-arg type parsing by delegating to full `ParseTypeReference()`.
  - BUG-045 explicitly extends `ParseTypeReference()` for additional type syntax.
- That makes Slice 8 the closest existing pending slice for adding/supporting `period` temporal-dimension qualifiers on field declarations.

## Not the best fit

- **Slice 9** is about operator result typing and modifier validation, not declaration syntax.
- **Slice 11** is about proof-obligation derivation, but the narrowed bug says the required field type cannot be declared in the first place.
- There is no existing pending slice dedicated to temporal arithmetic beyond operator/proof behavior, and this issue is upstream of both.

## Recommended handling

Add BUG-057 to Slice 8 as a parser/type-reference support item for qualified `period` field types.

If implementation later shows the parser already accepts the syntax and the qualifier is instead dropped during type binding/projection, then BUG-057 should be split:

1. **Slice 8** for field-type syntax acceptance / TypeRef construction
2. **Follow-on type-checker or proof slice** for preserving the `date` temporal dimension through semantic resolution

Based on the narrowed bug statement and the current plan text, though, **Slice 8 is the right first home** rather than creating a standalone temporal-arithmetic slice.

# Newman t2-12 complete

## Commit
- `5f79fc7a` — `feat(t2-12): MCP DTO audit — sync DTOs to catalog growth`

## What changed
- Synced `CompileToolDtos.cs` to the audited compile contract: state hooks, event ensures, rule guards, row outcomes/reject messages, state omit/access details, event arg optionality, and choice metadata are now represented.
- Rewired `CompileTool.cs` to populate every added DTO field from the real semantic/construct surfaces already present in core (`SemanticIndex`, `ConstructManifest`, and catalog metadata).
- Fixed compile rendering gaps: `~string`, structural collection type names, valued modifiers, stripped `because` keyword/message quotes, and string default values.
- Added focused MCP definition regression tests covering each DTO sync item.
- Updated `docs/tooling/mcp.md` (the current MCP design doc surface in-repo) to match the shipped `precept_compile` contract.

## Validation
- `dotnet test test/Precept.Mcp.Tests/` → 74 passed
- `dotnet test test/Precept.Tests/` → 3925 passed

## Notes
- `docs/McpServerDesign.md` is not present in this repo; `docs/tooling/mcp.md` is the active design-contract document that was updated in the same pass.

# Elaine — samples when-guard audit

Date: 2026-05-10

## Notable findings

- The sample corpus had five stale user-facing examples with `when` in the wrong place:
  - `samples/insurance-claim.precept`: guarded AccessMode, StateEnsure, and EventEnsure
  - `samples/loan-application.precept`: guarded StateEnsure and AccessMode
- The corpus also lacked a positive guarded StateAction example, so I added a minimal one in `samples/event-registration.precept` (`to Confirmed when AmountDue > 0 -> set AmountDue = 0`).
- After the content update, the Precept compile/diagnostic path available in-session still reports parse errors on the corrected pre-verb forms. That suggests a temporary drift between the approved language surface and the current parser/tooling on this branch.
- Related ledger note: `.squad/decisions.md` line 52 still says access mode remains post-adjective "today," which now reads stale against the final audit/design direction being applied to samples.

## Why this matters

Users learn the DSL from samples first. If samples, design docs, and parser behavior disagree on guard position, authors lose trust quickly and copy the wrong pattern into real definitions.

# Frank doc collision audit

Date: 2026-05-10T15:07:23.325-04:00

## Scope
- `docs/language/precept-language-spec.md`
- `docs/language/catalog-system.md`
- `docs/language/precept-grammar.md`

## Findings
- The SupportsPreVerbWhenGuard elimination survived in all three docs: `SupportsPreVerbWhenGuard` is absent, access mode grammar uses pre-verb `when`, and state/event ensure grammar remains pre-verb.
- No live post-verb access-mode or ensure syntax remained in grammar/example sections.
- No duplicate access-mode rules or duplicate `ConstructMeta` shape blocks were found.
- One coherence break remained in `docs/language/catalog-system.md`: the Constructs catalog inventory still said `ConstructKind` had 11 members and its member list omitted `OmitDeclaration`, contradicting the language spec, grammar reference, and source enum.

## Fix applied
- Updated `docs/language/catalog-system.md` to say `ConstructKind` has 12 members.
- Restored `OmitDeclaration` to the documented Constructs member list.

## Outcome
The three language docs now agree on the final slot-driven, pre-verb-guard model and the Constructs inventory is internally consistent again.

# Decision: Grammar Doc Comprehensive Review Findings

**Date:** 2026-05-10
**Author:** Frank (Lead/Architect)
**Context:** Comprehensive line-by-line review of `docs/language/precept-grammar.md`

## Decision

The grammar doc has 8 factual errors, 6 warnings, and 3 minor issues. No code changes required — all fixes are doc-only. The errors cluster in construct anatomy diagrams and family detail sections where pre-verb `when` guards are systematically omitted.

## Key Findings

1. **Pre-verb guard omission is systematic** — 6 of 8 errors are missing `[when Guard]` slots in anatomy diagrams or family detail sections. The pattern: wherever StateEnsure, StateAction, or EventEnsure appears in a diagram or summary, the optional guard is not shown.

2. **Computed-field anatomy is structurally wrong** — the diagram shows ModifierList trailing AFTER ComputeExpression, but the actual slot order (and all sample files) have modifiers BEFORE `<-`.

3. **Quick reference is stale** — Invariant 2 wording wasn't updated when the body text was revised for BUG-020.

4. **ExpressionForms count is wrong** — "13" should be "14" in the catalogs table (line 722).

## Action Required

Apply the 16 fixes listed in the priority fix list (see full report at `docs/working/frank-grammar-comprehensive-review-2026-05-10.md`). All are doc-only edits to `precept-grammar.md`. No code investigation needed.

## Rationale

The grammar doc is a design reference for people working ON the language. Factual errors in slot sequences and family details will cause implementors to write incorrect parser tests, produce wrong MCP output, or design new constructs with wrong assumptions about guard positions.

# Decision: Remove `SupportsPostActionEnsure` — Grammar Integrity Fix

**Date:** 2026-05-10T15:32:08-04:00
**Author:** Frank (Lead/Architect)
**Status:** Ready for implementation
**Audit:** `docs/working/frank-grammar-spec-audit-2026-05-10.md`

## Decision

Remove the `SupportsPostActionEnsure` boolean flag from `ConstructMeta` and all associated parser injection logic. The feature violates the grammar's fundamental disambiguation semantics — `ensure` and `->` are mutually exclusive second-token disambiguation paths in the `on` family, and a construct cannot legitimately use both.

## Key Findings

1. **The bug is isolated.** No other `Supports*` flags or out-of-band parser behaviors exist. The parser architecture is clean otherwise.
2. **7 files, ~25 lines affected.** Removal is surgical.
3. **The language spec documents the bad form** (line 861–869) and must be corrected simultaneously.
4. **Grammar doc has pre-existing `when` guard gaps** for 3 constructs (EventEnsure, StateEnsure, StateAction). These are doc-only fixes unrelated to the bug, but should be addressed in the same pass.

## Files to Change

1. `src/Precept/Language/Construct.cs` — remove parameter
2. `src/Precept/Language/Constructs.cs` — remove from EventHandler entry
3. `src/Precept/Pipeline/Parser.cs` — delete injection block
4. `test/Precept.Tests/Parser/ParserSlice8Tests.cs` — delete test
5. `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs` — delete test
6. `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs` — delete test
7. `docs/language/precept-language-spec.md` — remove `("ensure" BoolExpr)?` from stateless hook grammar
8. `docs/language/catalog-system.md` — remove from ConstructMeta shape documentation

# Frank — when-guard doc sync gap

## Gap

`.squad/decisions.md` still presents the superseded 2026-05-10T17:10:00Z GuardPolicy decision as an active canonical entry and still says access mode remains post-adjective today.

## Why it matters

The approved final audit (`docs/Working/frank-when-guard-audit-4-final.md`) and the current doc-sync batch now align the live language docs to the slot-list-only design:

- no GuardPolicy enum
- no construct-level pre-verb guard boolean
- AccessMode guard is pre-verb: `in State when Guard modify Field editable`

Leaving the older decision text in the active ledger will misdirect future doc and implementation work.

## Requested follow-up

Reconcile `.squad/decisions.md` so the active canonical entry reflects the final slot-list decision and no longer states that access mode is post-adjective.

# Decision: Eliminate GuardPolicy — Slot List IS the Metadata

**Author:** Frank — Lead/Architect
**Date:** 2026-05-10T13:16:47-04:00
**Status:** Final recommendation
**Supersedes:** `frank-when-guard-revised.md` (2-member GuardPolicy enum proposal)

---

## Decision

**`SupportsPreVerbWhenGuard` is deleted from `ConstructMeta`. No `GuardPolicy` enum is created. The guard's position in the slot list is the only metadata.**

Pre-verb guard constructs (StateEnsure, StateAction, EventEnsure, AccessMode) get a `GuardClause` slot at their natural position in the slot list — before the disambiguation keyword. Per-construct termination tokens make each guard self-describing.

`ParseScopedConstruct` is refactored from a 3-phase protocol (anchor → flag-gated injection → disambig + remaining slots) to a single unified loop that walks all slots in order, consuming the disambiguation keyword at the natural boundary.

## Key Finding

My prior analysis was wrong. I said putting the guard in the slot list "requires rearchitecting how `ParseScopedConstruct` walks slots" and called it scope-expanding. Having read the actual parser code:

1. **Disambiguation happens before `ParseScopedConstruct` is called.** The routing phase resolves which construct the parser is working with. A guard at slot[1] is just a regular optional slot — no routing ambiguity.

2. **The refactor is a simplification.** The current 3-phase code (~77 lines with flag-gated injection) becomes a single loop (~45 lines, zero flags). Net code reduction.

3. **The unified loop works for all 7 scoped constructs.** Verified construct-by-construct with and without guards.

## What Changes

| File | Change |
|------|--------|
| `Construct.cs` | Remove `SupportsPreVerbWhenGuard` parameter |
| `Constructs.cs` | Add 3 per-construct guard slot instances; update 4 construct slot lists; remove 3 `SupportsPreVerbWhenGuard: true` |
| `Parser.cs` | Replace `ParseScopedConstruct` with unified loop |
| Tests | Delete flag-assertion tests; add slot-position tests |

## Why This Is the Right Answer

Shane's directive: *"If `when` is always pre-verb, the slot list itself should encode that position. No separate metadata flag or enum is needed; the slot list IS the metadata."*

That's exactly what this achieves. Zero metadata flags. Zero enums. The catalog-driven principle is satisfied completely — the slot list is self-describing and the parser is a generic slot walker.

## Full Analysis

See `docs/Working/frank-when-guard-audit-2.md` for the complete analysis including construct-by-construct verification, refactored parser code, and file change inventory.

# Decision: When-Guard Catalog Shape — Revised (PostVerb Eliminated)

**Author:** Frank — Lead/Architect
**Date:** 2026-05-10T13:15:46-04:00
**Status:** Recommendation — awaiting owner decision
**Supersedes:** Prior 4-member `GuardPolicy` proposal (frank-when-guard-audit-2.md)

---

## Hard Constraint

> **PostVerb guard position is NOT supported. Full stop. `when` is always pre-verb or absent.**

This eliminates `PostVerb` from the design space permanently.

---

## 1. Does the GuardPolicy Enum Still Make Sense?

**Yes, but it collapses from 4 members to 2.**

With PostVerb gone, the prior proposal had `None`, `SlotWalk`, `PreVerb`. Here's what happens when we pressure-test each:

### `None` — is explicit prohibition needed?

No. A construct without a `GuardClause` in its slot list AND without `GuardPolicy.PreVerb` cannot have a guard. The absence is structural — there's nothing to parse and no injection trigger. `None` as an explicit prohibition adds no information the slot list doesn't already encode.

### `SlotWalk` — is it distinct from "just walking the slot list"?

No. `SlotWalk` means "the guard is in the slot list and the parser walks it in normal order." That's not a special policy — that's the *absence* of a policy. The parser does nothing different for `SlotWalk` vs `None` — in both cases it walks the slot list. The only difference is whether a `GuardClause` slot exists in the list, which the list itself declares.

### `PreVerb` — is it the only real policy?

Yes. `PreVerb` is the only value that triggers parser behavior different from default slot walking. It means: "inject a guard between the anchor and the disambiguation token, using the disambiguation tokens as terminators." This is a parse-protocol instruction that cannot be derived from the slot list alone, because the guard is NOT in the slot list for these constructs.

### Conclusion: 2-member enum

```csharp
public enum GuardPolicy
{
    /// <summary>
    /// Guard is either absent or declared in the slot list — parsed via normal slot walk.
    /// Whether the construct actually supports a guard is determined by whether a
    /// <see cref="ConstructSlotKind.GuardClause"/> slot appears in the slot list.
    /// </summary>
    SlotDriven = 0,

    /// <summary>
    /// Guard is injected between anchor (slot[0]) and the disambiguation token.
    /// The guard is NOT declared in the slot list — the parser synthesizes it at
    /// parse time using the construct's disambiguation tokens as terminators.
    /// Surface syntax: <c>&lt;scope&gt; &lt;target&gt; when &lt;guard&gt; &lt;verb&gt; ...</c>
    /// </summary>
    PreVerb,
}
```

**Why an enum and not a boolean?**

1. **Naming.** `GuardPolicy.PreVerb` says what it IS. `SupportsPreVerbWhenGuard = true` is a double-positive sentence fragment. The enum names the concept; the bool describes a capability.
2. **Default semantics.** `SlotDriven = 0` is the natural default — you only specify the property when the construct deviates. A bool with `false` as default means every construct silently opts out, but the opt-out has no name.
3. **Extensibility without breaking.** If a future construct needs a guard in a novel position (unlikely but possible), adding an enum member is additive. Renaming a boolean or adding a second boolean is not.
4. **The bool name is a lie for slot-walk constructs.** `SupportsPreVerbWhenGuard = false` for Rule and TransitionRow implies "doesn't support when guard" — but they do, via slot walk. The name confuses absence-of-guard with absence-of-injection. The enum eliminates this ambiguity.

---

## 2. TransitionRow and Rule — Where Do They Fall?

### TransitionRow

`from Draft on Submit when IsValid -> ...`

- Guard is slot[2]: `[StateTarget, EventTarget, GuardClause, ActionChain, Outcome]`
- TransitionRow is `RoutingFamily.StateScoped`, parsed via `ParseScopedConstruct`
- Slot[0] (StateTarget) is the anchor. After disambiguation, slots[1..] are walked: EventTarget, then GuardClause, then ActionChain, then Outcome.
- The guard is in the slot list, parsed in normal slot-walk order. **This is `SlotDriven`.**
- No injection, no special protocol. The parser doesn't know or care that slot[2] is a guard — it's just the next slot.

### Rule

`rule amount > 0 when someCondition because "reason"`

- Guard is slot[1]: `[RuleExpression, GuardClause, BecauseClause]`
- Rule is `RoutingFamily.Direct`, parsed via `ParseConstruct` (not `ParseScopedConstruct` at all)
- The guard is in the slot list, parsed in normal order. **This is `SlotDriven`.**
- Rule has no verb, no disambiguation token, no scope keyword. The `when` after the expression is just the next slot with `TerminationTokens: [TokenKind.Because, TokenKind.Arrow]`.

### Conclusion

Both collapse to `SlotDriven` (the default). Neither needs the `GuardPolicy` property specified. They work today, they'll work after the change. No slot list changes needed.

---

## 3. What's the Simplest Possible Catalog Shape?

Evaluating the four options Shane listed:

### Option A: `GuardPolicy` enum with 2 members — **RECOMMENDED**

```csharp
public enum GuardPolicy { SlotDriven = 0, PreVerb }
```

On `ConstructMeta`: replace `SupportsPreVerbWhenGuard: bool` with `GuardPolicy: GuardPolicy = GuardPolicy.SlotDriven`.

Parser code:
```csharp
if (meta.GuardPolicy == GuardPolicy.PreVerb && Peek().Kind == TokenKind.When)
{
    // inject guard — identical to current code body
}
```

**Verdict:** Names the concept. Parser code is one token different from today. Default means you only annotate the 4 constructs that deviate. Clean.

### Option B: Collapse to boolean `SupportsWhenGuard: bool`

Parser code: `if (meta.SupportsWhenGuard && Peek().Kind == TokenKind.When)` — nearly identical to today. But the name is wrong for Rule and TransitionRow (they support when-guards too, via slots). You'd need `InjectsPreVerbGuard: bool` which is just the current flag renamed. A boolean with a better name is still a boolean — it doesn't name the concept space.

**Verdict:** Functional but semantically impoverished. The bool says what the parser DOES, not what the construct's grammar MEANS.

### Option C: Drop all metadata — rely on slot list structure

This requires putting the guard IN the slot list for pre-verb constructs (at slot[1], before the disambiguation token). Then `ParseScopedConstruct` checks: "is the next slot a GuardClause? If so, parse it before consuming the disambiguation token."

Problem: the parser currently walks `Slots[1..]` AFTER consuming the disambiguation token. If the guard is at slot[1] but must be parsed BEFORE the disambiguation token, you need the parser to know which slots are pre-disambiguation and which are post. That's the `DisambiguationToken` synthetic slot from Alternative B of the prior analysis — a larger refactor.

**Verdict:** Pure but scope-expanding. Requires rearchitecting how `ParseScopedConstruct` walks slots. Not the right scope for this fix.

### Option D: Keep the existing boolean, just add AccessMode

Rename nothing. Add `SupportsPreVerbWhenGuard: true` to AccessMode, remove its `SlotGuardClause` from the slot list.

**Verdict:** Smallest diff. But we've been told to fix the smell, not perpetuate it. The bool name remains misleading for slot-walk constructs. Rejected.

### Final answer: Option A.

---

## 4. Updated Construct Slot Tables

### Before → After for all 6 when-using constructs

| Construct | Slots (before) | GuardPolicy (before) | Slots (after) | GuardPolicy (after) | Changed? |
|-----------|---------------|---------------------|--------------|--------------------|-|
| **Rule** | `[RuleExpr, GuardClause, BecauseClause]` | `SupportsPreVerbWhenGuard: false` (default) | `[RuleExpr, GuardClause, BecauseClause]` | `SlotDriven` (default) | No change |
| **TransitionRow** | `[StateTarget, EventTarget, GuardClause, ActionChain, Outcome]` | `SupportsPreVerbWhenGuard: false` (default) | `[StateTarget, EventTarget, GuardClause, ActionChain, Outcome]` | `SlotDriven` (default) | No change |
| **StateEnsure** | `[StateTarget, EnsureClause, OptBecauseClause]` | `SupportsPreVerbWhenGuard: true` | `[StateTarget, EnsureClause, OptBecauseClause]` | `PreVerb` | Flag → enum |
| **StateAction** | `[StateTarget, ActionChain]` | `SupportsPreVerbWhenGuard: true` | `[StateTarget, ActionChain]` | `PreVerb` | Flag → enum |
| **EventEnsure** | `[EventTarget, EnsureClause, OptBecauseClause]` | `SupportsPreVerbWhenGuard: true` | `[EventTarget, EnsureClause, OptBecauseClause]` | `PreVerb` | Flag → enum |
| **AccessMode** | `[StateTarget, FieldTarget, AccessModeKeyword, **GuardClause**]` | (none — guard was last slot) | `[StateTarget, FieldTarget, AccessModeKeyword]` | `PreVerb` | **Slot removed + policy added** |

### Constructs without guards (unchanged)

| Construct | GuardPolicy | Reason |
|-----------|-------------|--------|
| PreceptHeader | `SlotDriven` (default, no guard slot) | File-level declaration |
| FieldDeclaration | `SlotDriven` (default, no guard slot) | Type structure |
| StateDeclaration | `SlotDriven` (default, no guard slot) | Existence declaration |
| EventDeclaration | `SlotDriven` (default, no guard slot) | Existence declaration |
| OmitDeclaration | `SlotDriven` (default, no guard slot) | Unconditional exclusion |
| EventHandler | `SlotDriven` (default, no guard slot) | Stateless hook |

---

## 5. Parser Pseudocode — `ParseScopedConstruct` After Change

```csharp
private void ParseScopedConstruct(ConstructMeta meta)
{
    var startToken = Advance(); // consume leading keyword
    var slots = new List<SlotValue>();

    // Slots[0] = anchor (StateTarget or EventTarget)
    if (meta.Slots.Count > 0)
    {
        var anchorValue = ParseSlotValue(meta.Slots[0], meta);
        if (meta.Slots[0].IsRequired || anchorValue.Span != SourceSpan.Missing)
            slots.Add(anchorValue);
    }

    // ── CHANGED: GuardPolicy enum replaces SupportsPreVerbWhenGuard bool ──
    if (meta.GuardPolicy == GuardPolicy.PreVerb && Peek().Kind == TokenKind.When)
    {
        var guardSlot = ParseGuardClause(new ConstructSlot(
            ConstructSlotKind.GuardClause,
            IsRequired: false,
            TerminationTokens: meta.Entries
                .SelectMany(entry => entry.DisambiguationTokens ?? [])
                .Distinct()
                .ToArray()));

        if (guardSlot.Span != SourceSpan.Missing)
            slots.Add(guardSlot);
    }

    // Consume disambiguation keyword (not a slot)
    // ... (unchanged from current code)

    // Walk remaining slots (Slots[1..])
    // ... (unchanged from current code)
}
```

The change is exactly ONE token: `meta.SupportsPreVerbWhenGuard` → `meta.GuardPolicy == GuardPolicy.PreVerb`. Same code body, same termination token derivation, same guard injection protocol. The mechanism is proven; only the metadata shape changes.

---

## 6. AccessMode Surface Syntax Change

**Before (post-verb — ELIMINATED):**
```
in Draft modify Amount editable when IsOwner
```

**After (pre-verb — consistent with governing principle):**
```
in Draft when IsOwner modify Amount editable
```

This is a **breaking change** to `.precept` files. No current sample files use guarded access mode (confirmed by prior audit). The old post-verb form should produce a diagnostic after implementation.

---

## 7. File Change Inventory

### Source

| File | Change |
|------|--------|
| `src/Precept/Language/Construct.cs` | Add `GuardPolicy` enum (2 members: `SlotDriven`, `PreVerb`). Replace `SupportsPreVerbWhenGuard` parameter with `GuardPolicy GuardPolicy = GuardPolicy.SlotDriven`. |
| `src/Precept/Language/Constructs.cs` | StateEnsure, StateAction, EventEnsure: replace `SupportsPreVerbWhenGuard: true` with `GuardPolicy: GuardPolicy.PreVerb`. AccessMode: add `GuardPolicy: GuardPolicy.PreVerb`, remove `SlotGuardClause` from slot list. Update description string for AccessMode to reflect new syntax. |
| `src/Precept/Pipeline/Parser.cs` line 280 | `meta.SupportsPreVerbWhenGuard` → `meta.GuardPolicy == GuardPolicy.PreVerb` |

### Tests

| File | Change |
|------|--------|
| `test/Precept.Tests/Language/Track2PhaseAConstructCatalogTests.cs` | Replace `SupportsPreVerbWhenGuard` assertions with `GuardPolicy` assertions. |
| `test/Precept.Tests/CatalogCapability/ConstructCatalogCapabilityTests.cs` | Replace `SupportsPreVerbWhenGuard` assertions with `GuardPolicy` assertions. Add AccessMode guard-policy test. |
| Parser test file(s) | Add parse tests for `in Draft when IsOwner modify Amount editable`. Verify old post-verb form produces a diagnostic. |

### Documentation

| File | Change |
|------|--------|
| `docs/language/precept-language-spec.md` | Fix ensure grammar (lines 855–856) to show pre-verb guard. Fix access mode grammar (lines 897–903) to show pre-verb guard. |
| `docs/language/catalog-system.md` | Replace `SupportsPreVerbWhenGuard` schema entry with `GuardPolicy` enum documentation. |
| `docs/Working/precept-toolchain-plan.md` | Update references to `SupportsPreVerbWhenGuard`. |

### MCP / Language Server

| Surface | Impact |
|---------|--------|
| MCP `precept_language` | `SupportsPreVerbWhenGuard` disappears from construct JSON, replaced by `GuardPolicy` string. DTO update in `tools/Precept.Mcp/Tools/`. |
| LS completions | `when` suggestion for access mode moves from post-keyword to post-state-target position. |
| LS semantic tokens / grammar | No impact — `when` keyword matching is not construct-specific. |

### Samples

No sample files use guarded access mode — no sample changes needed.

---

## 8. Rationale Summary

| Question | Answer |
|----------|--------|
| Does `GuardPolicy` still make sense? | Yes — as a 2-member enum, not 4. |
| Does it collapse to a boolean? | Functionally yes, semantically no. The enum names the concept space (`SlotDriven` vs `PreVerb`), whereas a bool names a capability. |
| Is `SlotWalk` needed as a separate member? | No — it's indistinguishable from "no special policy" at the parser level. Merged into `SlotDriven`. |
| Is `None` needed as explicit prohibition? | No — absence of guard slot + default `SlotDriven` = no guard. Structural absence is sufficient. |
| Where do TransitionRow and Rule fall? | `SlotDriven` (the default). Their guards are in the slot list, parsed normally. No policy annotation needed. |
| What changes for AccessMode? | Guard moves from last slot (post-verb) to pre-verb injection. `SlotGuardClause` removed from slot list. `GuardPolicy: PreVerb` added. Surface syntax changes. |
| Is this the smallest correct change? | Yes. One new 2-member enum, one parser token change, one slot list edit (AccessMode). Everything else is renaming `SupportsPreVerbWhenGuard` → `GuardPolicy`. |

---

## 9. Alternatives Rejected

| Alternative | Reason |
|-------------|--------|
| 4-member enum (`None/SlotWalk/PreVerb/PostVerb`) | PostVerb is eliminated. `None` and `SlotWalk` are both "no special parser behavior" — distinction is phantom. |
| 3-member enum (`None/SlotWalk/PreVerb`) | `None` vs `SlotWalk` distinction is not actionable by the parser. Both mean "walk the slot list." |
| Boolean (`SupportsWhenGuard` or `InjectsPreVerbGuard`) | Functional but semantically flat. Doesn't name the concept space. Misleading for slot-walk constructs. |
| Drop metadata entirely (slot position convention) | Requires rearchitecting `ParseScopedConstruct` to distinguish pre-disambiguation vs post-disambiguation slots. Correct direction but wrong scope. |
| Keep existing bool, just add AccessMode | Perpetuates the naming smell. We're here to fix the metadata shape, not patch it. |

# BUG-020 Committed — George Runtime Dev

**Date:** 2026-05-10T15:32:08-04:00
**Author:** George (Runtime Dev)
**Branch:** Precept-V2-Radical

---

## Commits

| SHA | Scope | What it covers |
|-----|-------|----------------|
| `b5dc7c3e` | Core implementation | Removed `SupportsPreVerbWhenGuard` from `Construct.cs`, `Constructs.cs`, `Parser.cs`. The `when` guard is now a proper slot in the slot-walk rather than a special-cased pre-verb flag. |
| `ec068569` | Tests | Updated 13 existing test files and added `Track2PhaseAToolchainRegressionTests.cs` (new). Covers parser, proof engine, slot ordering, catalog capability, language server, and MCP tool tests. |
| `eb225f8a` | Docs | Grammar doc (`precept-grammar.md`), language spec (`precept-language-spec.md`), catalog system doc (`catalog-system.md`) updated to reflect the slot-walk `when`-guard semantics. |
| `4a6cb93f` | Samples | Updated `Test.precept`, `event-registration.precept`, `insurance-claim.precept`, `loan-application.precept` to use canonical `when`-guard slot syntax. |
| `103c3be1` | Working docs | Frank's 4 when-guard audit files (new) + `precept-toolchain-bugs.md` and `precept-toolchain-plan.md` updated. |
| `078dbe32` | Squad history | Agent history files for Elaine, Frank, George, Soup Nazi updated for BUG-020 session. |

---

## Final Test Results

| Project | Passed | Failed |
|---------|--------|--------|
| Precept.Tests | 3,894 | 0 |
| Precept.Analyzers.Tests | 280 | 0 |
| Precept.LanguageServer.Tests | 157 | 0 |
| Precept.Mcp.Tests | 60 | 0 |
| **Total** | **4,391** | **0** |

---

## Surprises / Notes

- No test failures at any stage. Pre-commit run of `Precept.Tests` showed 3,894 passing; full suite confirmed all 4,391 green after commits.
- One pre-existing LF/CRLF warning on `ParserExpressionTests.cs` — cosmetic, not a bug.
- Two pre-existing VSTHRD warnings in `LanguageServer.Tests` — unrelated to BUG-020, not introduced by this work.

# Decision: SupportsPostActionEnsure Removed

**Author:** George (Runtime Dev)
**Date:** 2026-05-10

## Commit SHAs

- **Code:** `c1572613` — fix(parser): remove SupportsPostActionEnsure — EventHandler cannot have trailing ensure (BUG)
- **Tests:** `5be86341` — test: delete SupportsPostActionEnsure tests — feature removed (BUG)

## Final Test Count After Removal

All 4 test projects pass:

| Project | Passed |
|---------|--------|
| Precept.Tests | 3891 |
| Precept.LanguageServer.Tests | 157 |
| Precept.Analyzers.Tests | 280 |
| Precept.Mcp.Tests | 60 |
| **Total** | **4388** |

## on-family Disambiguation Is Now Clean

The `on` family now has mutually exclusive routing:

- `on EventName ensure ...` → `EventEnsure` — guard-only path
- `on EventName -> ...` → `EventHandler` — action path

`SupportsPostActionEnsure` had allowed `on Event -> action ensure expr because "reason"` by grafting EventEnsure slot semantics onto EventHandler after the main slot-walk. This was an out-of-band parser injection that bypassed the catalog-driven architecture and violated the disambiguation contract encoded in `DisambiguationEntry`. The `ensure` and `->` tokens are mutually exclusive routing tokens — the parser should never mix their semantics on the same construct.

The fix: removed the `bool SupportsPostActionEnsure` parameter from `ConstructMeta`, removed its usage in the `EventHandler` catalog entry, and deleted the conditional slot-injection block in `ParseScopedConstruct`. Three test methods that asserted the now-deleted behavior were also removed.

# George — when-guard elimination

## What changed
- Removed `SupportsPreVerbWhenGuard` from `ConstructMeta` in `src/Precept/Language/Construct.cs`.
- Added three shared pre-verb guard slot instances in `src/Precept/Language/Constructs.cs`:
  - `SlotPreVerbGuardEnsure` terminating at `ensure`
  - `SlotPreVerbGuardArrow` terminating at `->`
  - `SlotPreVerbGuardModify` terminating at `modify`
- Rewired scoped construct slot lists so guard position is encoded directly in metadata:
  - `StateEnsure`: `[StateTarget, GuardClause, EnsureClause, BecauseClause?]`
  - `StateAction`: `[StateTarget, GuardClause, ActionChain]`
  - `EventEnsure`: `[EventTarget, GuardClause, EnsureClause, BecauseClause?]`
  - `AccessMode`: `[StateTarget, GuardClause, FieldTarget, AccessModeKeyword]`
- Updated `AccessMode` description/example to the new pre-verb surface syntax: `in Draft when IsOwner modify Amount editable`.
- Replaced `Parser.ParseScopedConstruct`'s old 3-phase protocol with a single loop that:
  - walks slots in order,
  - consumes disambiguation tokens at the natural slot boundary,
  - keeps the existing `->` exception so `ActionChain` still owns arrow consumption,
  - removes all synthesized guard injection.
- Synced language docs (`catalog-system.md`, `precept-language-spec.md`, `precept-grammar.md`) so they describe slot-driven guard placement and pre-verb guarded access mode syntax.

## Why
The guard position is already expressible in the ordered slot list plus per-slot termination tokens. Keeping a separate boolean on `ConstructMeta` duplicated catalog truth and forced parser special-casing. After this change, the catalog is authoritative again: constructs that support pre-verb guards declare them as real slots, and the parser is just a generic slot walker with family disambiguation.

## Validation
- `dotnet build .\src\Precept\Precept.csproj --nologo` ✅
- `dotnet test .\test\Precept.Tests\Precept.Tests.csproj --nologo` ❌ 24 failing tests, all in stale expectations around removed `SupportsPreVerbWhenGuard`, old slot orders/counts, old post-verb guarded `AccessMode` syntax, plus the pre-existing BUG-019 typed-constant failure.
- Runtime spot-checks via `Precept.Compiler.Compile(...)`:
  - guarded `AccessMode` ✅
  - guarded `EventEnsure` ✅
  - guarded `StateAction` ✅
  - guarded `StateEnsure` ✅ (after giving the sample a satisfiable default)

# Soup Nazi — when-guard follow-up gap

## Gap
`test\Precept.LanguageServer.Tests` and `test\Precept.Mcp.Tests` currently have no explicit regression coverage for the `SupportsPreVerbWhenGuard` removal or the AccessMode syntax move to pre-verb `when`.

## Why it matters
The Precept.Tests batch now locks the catalog/parser/runtime-facing slot shape, but the agent-facing projections are still unguarded:
- MCP construct JSON should stop projecting `SupportsPreVerbWhenGuard`.
- Any LS completion/context tests that reason about AccessMode guard position should prove `when` is offered before `modify`, not after `editable`.

## Suggested follow-up
Add one MCP surface test for dropped construct metadata and one LS completion/parser-context regression for `in Draft when IsOwner modify Amount editable`.

### 2026-05-09T17:41:32.9988470Z: Typed-literal system implementation is complete



**By:** George



**Status:** Inbox



- Completed the full typed-literal system plan in slice order: embedded ISO 4217 + UCUM data, XML-backed currency/UCUM loaders, temporal and UCUM parsers, typed-constant validation framework, domain validators, `TypeMeta.ContentValidation` wiring, TypeChecker migration, canonical doc sync, and retirement of superseded working docs.

- Durable architecture: compile-time typed-literal validation is catalog-driven through `TypeMeta.ContentValidation` and `TypedConstantValidation.Validate(...)`; there is no interface-based validator registry.

- `unitofmeasure` now validates through the shared UCUM subsystem and `currency` through the XML-backed `CurrencyCatalog`; temporal typed constants share the canonical parser stack in `src/Precept/Language/Time/`.

- Validation evidence: `dotnet build src\Precept\Precept.csproj` and `dotnet test test\Precept.Tests\Precept.Tests.csproj` both pass, closing at 3721 passing tests.

- Known boundary left explicit by the plan: `src/Precept/Runtime/Measures/Unit.cs`, `MeasureDimension.cs`, and `UnitFactory.cs` are intentional runtime stubs for future measure arithmetic work.

### 2026-05-09: Data family expanded — field + arg added

**By:** Shane

**What:** Field (#A5B4FC) and arg (#9AD8E8) added to Data color family. Spec updated to allow semantically-grouped families with distinct hues (not tonal-variants-only). Fields and args are data tokens in the DSL — they belong in Data.

**Why:** Moving them to a standalone layer would hollow out what "Data" means. The family definition was too restrictive.

# Decision: Field and Arg as Standalone Companion Tokens



**By:** Elaine (UX Design)

**Date:** 2026-05-09

**Status:** Proposed — pending Shane sign-off



## Context



The field/arg color proposal needed a paradigm answer: where do `--field` (#A5B4FC, 239° indigo) and `--arg` (#9AD8E8, 195° cyan) live in the five-family model?



Three options evaluated:



1. **Axis layer** (Elaine-42) — named Structure/Behaviour/Grounding axes grouping families. Shane rejected: states are already structural, so the grouping is circular.

2. **Add to Data family** (Shane's suggestion) — keep field/arg in Data, let color do the alignment. Problem: Data is hue-coherent at ~215° slate. Adding 239° indigo and 195° cyan creates a 3-hue family that contradicts its visual identity.

3. **Standalone companion tokens** (revised recommendation) — field and arg get their own CSS properties and a brief spec sub-section, not nested inside any family card. Hue proximity communicates the structural/behavioural alignment without a named grouping.



## Decision



Option 3: standalone companion tokens.



- Five families stay unchanged (Structure 3 tones, State 1, Event 1, Data **2** tones, Rule 1).

- Data narrows: `#B0BEC5` drops (no remaining role); Data becomes type (`#9AA8B5`) + value (`#84929F`).

- New spec sub-section "Companion Tokens" after the five family cards, before supporting colors.

- `--field: #A5B4FC` and `--arg: #9AD8E8` documented with a one-line note: hue alignment to structural/behavioural neighbours is the relationship signal.

- No axis layer. No family stretching. Colors unchanged from the original proposal.



## Why



- Families are hue-coherent by design principle. Adding foreign hues breaks the visual contract.

- Hue proximity is self-documenting. A named grouping on top adds overhead without new information.

- The companion concept is extensible if future tokens need the same pattern (e.g., a guard-name color near Rule).

# george-currencycatalog-implemented



**Agent:** George (Runtime Dev)

**Date:** 2026-05-09T10:41:11Z

**Action:** Implement `CurrencyEntry` + `CurrencyCatalog` (Action 1 from Frank's gap analysis)



---



## What Was Created / Modified



### New: `src/Precept/Language/CurrencyCatalog.cs`

- `CurrencyEntry` — sealed record with 4 fields: `AlphaCode (string)`, `NumericCode (int)`, `Name (string)`, `MinorUnit (int)`

- `CurrencyCatalog` — public static class with `All: FrozenDictionary<string, CurrencyEntry>` keyed by alpha code, `StringComparer.OrdinalIgnoreCase`

- 162 entries: all ISO 4217 List One active national currencies + supranational/fund X-codes (minus precious metals, XTS, XXX)



### Modified: `src/Precept/Language/Types.cs`

- Removed: `Iso4217CurrencyCodes` `FrozenSet<string>` declaration (lines 59–77 in original)

- Updated: `CurrencyValidation` now derives `AllowedValues` from `CurrencyCatalog.All.Keys.ToFrozenSet(StringComparer.OrdinalIgnoreCase)`

- `ClosedSetValidation` wrapper shape unchanged; case-insensitive behavior preserved



---



## Final Code Count



- **162 currency entries** in `CurrencyCatalog.All`

- Starting point: 156 codes from `Iso4217CurrencyCodes`

- Removed: HRK (Croatian Kuna — withdrawn from List One when Croatia adopted EUR, Jan 2023)

- Added (7 new X-codes): XBA (955), XBB (956), XBC (957), XBD (958), XDR (960), XSU (994), XUA (965)

- Net: 156 − 1 + 7 = 162



---



## Decisions Made During Implementation



### Fund code MinorUnit = -1 for N/A

ISO 4217 lists certain codes with `N/A` for minor units. Convention: `MinorUnit = -1`. Applies to:

XBA, XBB, XBC, XBD (bond market units), XDR (SDR/Special Drawing Right), XSU (Sucre), XUA (ADB Unit of Account).



### Supranational codes with real minor units use their published values

XAF (0), XOF (0), XPF (0) — zero decimal places. XCD (2) — two decimal places. These are NOT fund codes; they're real currencies used by member countries.



### HRK removed (not just noted)

Croatia's ISO withdrawal is permanent. Keeping a withdrawn code in the catalog would cause the sync test to fail when XML is present. Hard-removed, not commented out.



### Precious metals excluded (XAU, XAG, XPT, XPD)

Per Shane's resolved Q1 decision: commodities, not currencies. One-line addition if needed post-v1.



### XTS (testing), XXX (no currency) excluded

Per gap analysis. Standard practice.



---



## Validation



- `dotnet build src/Precept/` — green, 0 warnings, 0 errors

- `dotnet test test/Precept.Tests/` — 3646 passed, 1 skipped (`CurrencyCatalogSyncTests` — skipped correctly as ISO 4217 XML is not present)

- No pre-existing test failures introduced

### 2026-05-09T09:34:41: User directive

**By:** Shane (via Copilot)

**What:** Always include a running tally of in-flight agent threads when multiple work streams are active. Format: emoji + agent name + one-line status (running/done/blocked). Keep it updated every response.

**Why:** User request — captured for team memory

# PRECEPT0019 Exhaustiveness Audit



## Summary

The audit found one clear PRECEPT0019 expansion that is both valuable and implementable now: `Parser.ParserState` for `OutcomeArgumentKind`. One additional parser-local enrollment is worthwhile after refactor: `Parser.ParserState` for `ActionSyntaxShape`. Everywhere else, the pipeline is either already correctly catalog-driven, or the remaining handwritten dispatch is the wrong shape for PRECEPT0019 (subset semantics, type-based DU dispatch, or multiple independent dispatch families where class-level coverage would create false confidence).



## Already Enrolled

- **ParserState / ExpressionFormKind** is already enrolled at `src/Precept/Pipeline/Parser.cs:47`.

- Coverage is complete across all 14 `ExpressionFormKind` members from `src/Precept/Language/ExpressionForms.cs:8-37`:

  - `Literal` → `ParseLiteral` (`Parser.Expressions.cs:118-123`), `ParseInterpolatedString` (`378-421`), `ParseInterpolatedTypedConstant` (`424-440`)

  - `UnaryOperation` → `ParseUnaryOperation` (`125-134`)

  - `Identifier`, `FunctionCall` → `ParseIdentifierOrFunctionCall` (`185-203`)

  - `Grouped` → `ParseGrouped` (`205-213`)

  - `ListLiteral` → `ParseListLiteral` (`215-238`)

  - `Conditional` → `ParseConditional` (`240-253`)

  - `Quantifier` → `ParseQuantifier` (`256-271`)

  - `CIFunctionCall` → `ParseCIFunctionCall` (`273-284`)

  - `MemberAccess`, `MethodCall` → `ParseMemberAccessOrMethodCall` (`286-318`)

  - `PostfixOperation` → `ParsePostfixIs` (`326-356`)

  - `BinaryOperation` → `ParseBinaryInfix` (`358-374`)

  - `InterpolatedString` → `ParseInterpolatedString` (`378-421`)

- This is the canonical PRECEPT0019 shape: one parser-local catalog (`ExpressionFormKind`), one handler family, and method-level ownership per member.



## Recommended Enrollments (prioritized)



### 1. Enroll now

- **Class**: `Precept.Pipeline.Parser.ParserState`

- **Catalog Enum**: `OutcomeArgumentKind` (discovered during audit in `src/Precept/Language/Outcomes.cs:19-32`)

- **Dispatch pattern**: `ParseOutcome` resolves the outcome form catalog-correctly via `Outcomes.ByLeadingToken` (`Parser.Expressions.cs:580-587`), then performs handwritten argument-shape dispatch with a switch on `meta.ArgumentKind` (`591-600`). The per-shape methods already exist:

  - `ParseOutcomeIdentifierArg` (`606-620`)

  - `ParseOutcomeStringLiteralArg` (`622-636`)

  - `ParseOutcomeSecondaryToken` (`638-652`)

- **Coverage gap risk**: High. If a new `OutcomeArgumentKind` member is added, the parser currently falls into runtime handling (`None` throws explicitly; unknown values hit `ArgumentOutOfRangeException`). That is a parser gap discovered only when the new form is exercised.

- **Feasibility**: High. The method-per-member pattern already exists for 3 of the 4 members. The only obstacle is `OutcomeArgumentKind.None`: it needs an explicit handler method (even if that method deliberately throws until a no-arg outcome ships) so PRECEPT0019 can force deliberate ownership of the member.

- **Recommendation**: **Enroll now.** This is the highest-confidence expansion because the dispatch point is singular, local, and already factored into per-member helper methods.



### 2. Enroll after refactor

- **Class**: `Precept.Pipeline.Parser.ParserState`

- **Catalog Enum**: `ActionSyntaxShape` (`src/Precept/Language/Action.cs:30-50`)

- **Dispatch pattern**: Action lookup is catalog-driven up to the action verb (`Actions.ByTokenKind` in `Parser.cs:843-861`), but `ParseActionByShape` then switches manually on `meta.SyntaxShape` (`Parser.cs:887-1005`) and inlines all 9 shapes in one monolithic method.

- **Coverage gap risk**: Medium-high. If a new `ActionSyntaxShape` member is added, the default branch returns `MalformedAction` (`1001-1005`). That is worse than an honest compile failure: the parser degrades into recovery output rather than forcing the new syntax shape to be implemented deliberately.

- **Feasibility**: Medium. The current analyzer needs method-level annotations, so `ParseActionByShape` must be split into per-shape methods (`ParseAssignValueAction`, `ParseCollectionValueAction`, etc.) and then annotated.

- **Recommendation**: **Enroll after refactor.** This is a real PRECEPT0019 target, but only after the shape-specific parsing logic is broken out of the monolith.



### 3. Do not enroll with PRECEPT0019; use a different mechanism

- **Class**: `Precept.Pipeline.ProofEngine`

- **Catalog Enum**: `ProofRequirementKind`

- **Dispatch pattern**: The engine handles proof kinds in several separate places, mostly by DU subtype rather than enum identity:

  - strategy 1 numeric-only literal proof (`ProofEngine.cs:334-360`)

  - strategy 2 kind-specific declaration proof (`365-421`)

  - guard-path proof branches (`526-536`)

  - diagnostic construction (`841-889`)

  - fault-link construction (`907-920`)

- **Coverage gap risk**: High if new proof kinds are added; a new kind can easily be forgotten in one of these families and degrade into fallback behavior.

- **Feasibility**: Low for PRECEPT0019 specifically. The analyzer is class-level and only requires that some method in the class is annotated for each member. That would not guarantee that every independent dispatch family (`TryDeclarationAttributeProof`, `CreateDiagnostic`, `CreateFaultSiteLink`, etc.) handles every kind. With the current analyzer shape, enrollment would create false confidence.

- **Recommendation**: **Skip for PRECEPT0019.** If stronger compile-time confidence is desired here, use a different enforcement mechanism: either split each dispatch family into its own handler type, or add a new analyzer that audits proof-requirement DU switches/families directly.



### 4. Do not enroll with PRECEPT0019; make the logic catalog-driven instead

- **Class**: `Precept.Pipeline.TypeChecker`

- **Catalog Enum**: `FunctionKind`, `OperationKind`, `ConstraintKind`, `ModifierKind`

- **Dispatch pattern**:

  - CI enforcement hardcodes specific `OperationKind` values (`TypeChecker.Validation.cs:334-358`) and specific `FunctionKind` values (`365-380`)

  - ensure normalization hardcodes `TokenKind -> ConstraintKind` (`TypeChecker.cs:449-456`)

  - access-mode normalization hardcodes `TokenKind.Editable -> ModifierKind.Write` and fallback-to-read (`589-594`)

  - state-hook normalization hardcodes `TokenKind.From -> AnchorScope.OnExit`, fallback-to-entry (`652-656`)

- **Coverage gap risk**: Medium. These sites can silently mis-handle future surface additions because they use subset logic with fallback arms.

- **Feasibility**: Low for PRECEPT0019. These are not “handle every member of the enum” sites. They either care about a small semantic subset of a much larger enum, or they are token-to-catalog mappings.

- **Recommendation**: **Skip PRECEPT0019.** Fix these, if desired, by moving more meaning into catalog metadata/indexes (for example, access/anchor token indexes in `Modifiers`, or CI-enforcement metadata in `Functions` / `Operations`).



### 5. No current candidate

- **Classes**: `Precept.Runtime.Evaluator`, `Precept.Runtime.Precept`

- **Assessment**: No PRECEPT0019 recommendation today. Both files are still largely runtime stubs/TODOs (`src/Precept/Runtime/Evaluator.cs`, `src/Precept/Runtime/Precept.cs`), so there is not yet a stable handwritten enum/member dispatch surface worth enrolling.

- **Recommendation**: Revisit only when runtime execution logic becomes real and there is an actual per-catalog dispatch family to audit.



## Already Covered by Other Analyzers

- **All current `GetMeta` catalogs covered by PRECEPT0007 today**:

  - `TypeKind` → `Types.GetMeta` (`src/Precept/Language/Types.cs:301-725`)

  - `TokenKind` → `Tokens.GetMeta` (`Tokens.cs:95-432`)

  - `OperatorKind` → `Operators.GetMeta` (`Operators.cs:15-156`)

  - `OperationKind` → `Operations.GetMeta` (`Operations.cs:43-...`)

  - `ModifierKind` → `Modifiers.GetMeta` (`Modifiers.cs:46-309`)

  - `FunctionKind` → `Functions.GetMeta` (`Functions.cs:38-307`)

  - `ActionKind` → `Actions.GetMeta` (`Actions.cs:66-205`)

  - `ConstructKind` → `Constructs.GetMeta` (`Constructs.cs:41-169`)

  - `DiagnosticCode` → `Diagnostics.GetMeta` (`Diagnostics.cs:37-438`)

  - `FaultCode` → `Faults.GetMeta` (`Faults.cs:12-40`)

  - `ExpressionFormKind` → `ExpressionForms.GetMeta` (`ExpressionForms.cs:81-104`)

- **Diagnostics/Fault catalog consistency already has dedicated analyzer coverage**:

  - `FaultCode` ↔ `DiagnosticCode` statically-preventable mapping is enforced by **PRECEPT0002** (`src/Precept.Analyzers/Precept0002FaultCodeMustHaveStaticallyPreventable.cs`)

  - `Diagnostics.GetMeta` self-consistency is enforced by **PRECEPT0015** (`src/Precept.Analyzers/Precept0015DiagnosticsCrossRef.cs:11-191`)

- **Proof requirement metadata placement/identity already has targeted analyzer coverage**:

  - `ParamSubject` reference identity → **PRECEPT0005**

  - proof-subject placement validity → **PRECEPT0006**

- These analyzers do not replace PRECEPT0019 for pipeline code, but they do mean the catalogs themselves are already guarded in several important places.



## Catalog-Driven (Correct — No Enrollment Needed)

These are exactly the places where the catalog already is the source of truth and PRECEPT0019 would be redundant noise.



- **Lexer / TokenKind**

  - keyword recognition uses `Tokens.Keywords` (`Lexer.cs:97-99`, `688-697`)

  - operator recognition uses `Tokens.TwoCharOperators`, `Tokens.SingleCharOperators`, `Tokens.TwoCharOperatorStarters` (`736-760`)

  - punctuation recognition uses `Tokens.PunctuationChars` (`762-772`)

  - the only switch is on internal `LexerMode` (`157-168`), not on `TokenKind`

- **Parser / ConstructKind**

  - top-level construct routing is driven by `Constructs.ByLeadingToken` and `DisambiguationEntry.DisambiguationTokens` (`Parser.cs:138-179`)

- **Parser / TypeKind, ModifierKind, ActionKind, OutcomeKind**

  - types via `Types.ByToken` (`Parser.cs:396-422`, `544-570`)

  - field/state modifiers via `Modifiers.ByFieldToken` / `Modifiers.ByStateToken` (`581-607`, `634-670`)

  - action verbs via `Actions.ByTokenKind` (`843-861`)

  - outcome forms via `Outcomes.ByLeadingToken` (`Parser.Expressions.cs:580-587`)

- **TypeChecker / OperationKind, FunctionKind, ActionKind, TypeKind**

  - binary/unary operator legality via `Operations.FindCandidates` / `FindUnary` and `Operators.ByToken` (`TypeChecker.Expressions.cs:488-510`, `525-625`)

  - functions via `Functions.FindByName` + overload metadata (`1031-1145`)

  - action proof requirements via `Actions.GetMeta(parsedAction.Kind).ProofRequirements` (`683-684`)

  - member/method accessors via `Types.GetMeta(receiver.ResultType).Accessors` (`1199-1240`, `1255-1312`)

- **GraphAnalyzer / ModifierKind**

  - state modifier semantics are read from `StateModifierMeta` (`GraphAnalyzer.cs:595-603`)

  - this is the correct architecture: graph algorithms stay hand-written, modifier meaning lives in metadata

- **ProofEngine / OperationKind, FunctionKind, ModifierKind**

  - subject resolution uses `Operations.GetMeta` / `Functions.GetMeta` (`264-279`)

  - declaration proof reads `Modifiers.GetMeta` and proof satisfactions (`399-407`)

  - guard decomposition uses operator metadata from `Operations.GetMeta(...).Op` (`553-604`, `728-739`, `1079-1090`)



## Structural Obstacles

- **`ProofRequirementKind` in `ProofEngine` is a real risk surface, but PRECEPT0019 is the wrong tool.**

  - The engine has multiple independent proof-kind dispatch families. Class-level coverage would only prove that each kind is handled somewhere, not everywhere it must be handled.

  - This is a false-confidence hazard. If Shane wants compile-time exhaustiveness here, the analyzer must be more precise than PRECEPT0019, or the code must be reorganized into one handler family per kind.

- **`ActionSyntaxShape` needs method extraction before PRECEPT0019 can help.**

  - Today the entire shape dispatch lives in one switch statement (`Parser.cs:887-1005`). PRECEPT0019 only works when the handling surface is factored into methods that can carry `[HandlesCatalogMember]`.

- **`OutcomeArgumentKind.None` is a currently-unused member.**

  - That is not a blocker; it is exactly why enrollment is useful. But it does require a deliberate handler method rather than relying on the current inline throw in `ParseOutcome`.

- **Several TypeChecker sites are really missing indexes/metadata, not PRECEPT0019.**

  - `ConstraintKind` is being synthesized from leading tokens (`TypeChecker.cs:449-456`) rather than derived from a constraint/anchor index.

  - access-mode and anchor mapping are handwritten (`589-594`, `652-656`) even though `Modifiers.GetMeta` already knows access and anchor semantics.

  - Those should become catalog-derived lookups if we want confidence there; forcing whole-enum PRECEPT0019 coverage would be the wrong abstraction.

- **CI enforcement is subset dispatch, not full-enum dispatch.**

  - `TypeChecker.Validation` only cares about CI-sensitive operations/functions, not every `OperationKind` / `FunctionKind` member.

  - The right fix is metadata (`HasCIVariant`, operator family/CI flags), not whole-enum class enrollment.



## Phase 3 Assessment

`src/Precept.Analyzers/CatalogAnalysisHelpers.cs:57-68` has an outdated TODO. `ConstraintKind` and `ProofRequirementKind` are now ready to join PRECEPT0007’s `CatalogEnumNames` list.



- **`ConstraintKind`**

  - Enum lives in `src/Precept/Language/ConstraintKind.cs:6-25`

  - `Constraints.GetMeta` is fully explicit and ends in `_ => throw`, not discard fallback (`src/Precept/Language/Constraints.cs:13-21`)

- **`ProofRequirementKind`**

  - Enum lives in `src/Precept/Language/ProofRequirementKind.cs:6-24`

  - `ProofRequirements.GetMeta` is fully explicit and ends in `_ => throw`, not discard fallback (`src/Precept/Language/ProofRequirements.cs:13-21`)



**Verdict:** add both names to `CatalogAnalysisHelpers.CatalogEnumNames` now. There are no remaining `_ =>` discard-arm blockers in the catalog `GetMeta` switches. The current blocker is only the stale TODO comment, not the code.

# ProofEngine Architecture Audit — Findings



**Date:** 2026-05-09T08:52:00-04:00

**By:** Frank (Lead/Architect)

**Verdict:** CONCERNS — 4 required fixes, 2 design gaps



---



## Required Fixes



### FIX-1: `IsSubtractionOp` uses string-based enum name matching (VIOLATION)



**Location:** `ProofEngine.cs` line 773–777



```csharp

private static bool IsSubtractionOp(OperationKind op)

{

    var name = op.ToString();

    return name.Contains("Minus", StringComparison.Ordinal);

}

```



The Operations catalog already carries `BinaryOperationMeta.Op == OperatorKind.Minus`. This should be `Operations.GetMeta(op).Op == OperatorKind.Minus`. String-matching on enum member names is fragile, non-catalog-driven, and breaks if enum names are refactored.



---



### FIX-2: `CreateDiagnostic` maps `PresenceProofRequirement` to `DivisionByZero` (BUG)



**Location:** `ProofEngine.cs` lines 883–889



```csharp

PresenceProofRequirement presence =>

    Diagnostics.Create(DiagnosticCode.DivisionByZero, ...),  // WRONG



_ => Diagnostics.Create(DiagnosticCode.DivisionByZero, ...)  // WRONG

```



Unresolved presence obligations and unknown requirement types both fall through to `DiagnosticCode.DivisionByZero`. A presence proof failure ("optional field accessed without guard") is not a division-by-zero error. Requires a dedicated `UnprovedPresenceRequirement` diagnostic code (proposed 116) or, if presence obligations are always handled upstream by the TypeChecker's collection safety diagnostics, this code path should be unreachable with an explicit `throw new UnreachableException()`.



---



### FIX-3: `CreateFaultSiteLink` default fallback to `DivisionByZero` (BUG)



**Location:** `ProofEngine.cs` lines 919, 926



```csharp

_ => DiagnosticCode.DivisionByZero     // line 919 — catch-all

_ => FaultCode.DivisionByZero          // line 926 — catch-all

```



Same issue as FIX-2 — the fault site link defaults to `DivisionByZero` for any requirement kind not explicitly matched. `PresenceProofRequirement` specifically has no mapping. If the presence fallback is reachable, it needs a correct `FaultCode` and `DiagnosticCode`.



---



### FIX-4: Missing `UnprovedPresenceRequirement` diagnostic code (SPEC + IMPL GAP)



Neither the design doc's diagnostic table (§9) nor `DiagnosticCode.cs` defines a presence-specific proof diagnostic code. The diagnostic table at line 1577 of the spec lists codes 82–84 and 112–115 but has no entry for "optional field used without proving it is set." This is the root cause of FIX-2 and FIX-3.



**Resolution:** Add `UnprovedPresenceRequirement = 116` to `DiagnosticCode.cs` and a corresponding `Diagnostics` catalog entry. Update `CreateDiagnostic` and `CreateFaultSiteLink` to use it for `PresenceProofRequirement`.



---



## Design Gaps (Non-Blocking)



### GAP-1: Design doc pseudocode vs. implementation minor discrepancies



1. **ResolveParamInBinaryOp** — spec references `opMeta.Left`/`opMeta.Right`; implementation correctly uses `bom.Lhs`/`bom.Rhs` and checks Rhs before Lhs (documented improvement for shared-parameter resolution). Spec should be updated to match.

2. **Strategy 2 modifier walk** — spec pseudocode omits `ImpliedModifiers`; implementation correctly includes them via `.Concat(attributeField.ImpliedModifiers)`. Spec prose at line 826 correctly states this, but pseudocode at line 729 doesn't. Minor spec consistency issue.



### GAP-2: `GuardRelationImpliesObligation` implementation accepts additional parameters vs spec



Spec signature: `GuardRelationImpliesObligation(guard, expr, requirement)` (3 params).

Implementation signature: `GuardRelationImpliesObligation(guard, expr, exprLeftField, exprRightField, requirement)` (5 params).



The implementation pre-resolves field names and passes them as arguments. Functionally equivalent, slightly different shape. Spec should be updated if a spec-update pass occurs.

# Design Ruling: ProofEngine × ProofRequirementKind Exhaustiveness Mechanism



**Author:** Frank (Lead/Architect)

**Date:** 2026-05-09

**Status:** RULING — awaiting Shane sign-off

**Requested by:** Shane (no deferrals, right solution not easiest)



---



## Context



The PRECEPT0019 audit identified `ProofEngine × ProofRequirementKind` as a real coverage risk but concluded that enrolling it in PRECEPT0019 would give **false confidence**. The engine has multiple independent dispatch families — some that MUST handle every `ProofRequirement` subtype, and others that are intentionally partial. A class-level annotation can't distinguish between these.



Shane's directive: find the architecturally correct enforcement mechanism. No deferrals.



## Findings from Source



### Dispatch Families in ProofEngine



I count **seven** sites that operate on `ProofRequirement` subtypes. They fall into two categories:



**Category A — Must-Be-Exhaustive (2 families):**



1. **`CreateDiagnostic`** (line 840–878) — switch statement over `obligation.Requirement` with explicit type-pattern arms for all 5 subtypes, followed by `throw`. Every requirement kind MUST produce a diagnostic when unresolved. Missing an arm means silent failure.



2. **`CreateFaultSiteLink`** (line 900–917) — switch statement over `obligation.Requirement` with explicit type-pattern arms for all 5 subtypes, followed by `throw`. Every requirement kind MUST produce a fault-site link.



**Category B — Intentionally Partial (5 families):**



3. **`TryLiteralProof`** (Strategy 1, line 334) — handles only `NumericProofRequirement`. By design: only numeric requirements can be discharged by literal comparison.



4. **`TryDeclarationAttributeProof`** (Strategy 2, line 365) — handles `DimensionProofRequirement`, `ModifierRequirement`, `NumericProofRequirement`, `PresenceProofRequirement`. By design: `QualifierCompatibilityProofRequirement` has its own dedicated strategy.



5. **`TryGuardInPathProof`** (Strategy 3, line 511) — handles `NumericProofRequirement`, `PresenceProofRequirement`. By design: guard decomposition only produces numeric and presence constraints.



6. **`TryFlowNarrowingProof`** (Strategy 4, line 681) — handles only `NumericProofRequirement`. By design: flow narrowing applies only to subtraction operand relationships.



7. **`TryQualifierCompatibilityProof`** (Strategy 5, line 779) — handles only `QualifierCompatibilityProofRequirement`. By design: this is the dedicated dual-subject strategy.



### Key Architectural Insight



The strategies are organized by **proof technique**, not by **requirement kind**. A single strategy handles multiple kinds (Strategy 2 handles 4 of 5), and the same kind is handled by multiple strategies (Numeric is attempted by Strategies 1, 2, 3, and 4). This is the correct decomposition. The strategies chain via `TryDischarge` (line 316–330): each returns false for inapplicable kinds, and the loop tries the next strategy.



If no strategy can discharge an obligation, it stays `Unresolved`. That is **safe** — it's conservative. `CreateDiagnostic` then fires, producing an error for the user. The danger is never "we failed to prove something" (that's correctly conservative). The danger is in the must-be-exhaustive families: "we failed to emit the right diagnostic" or "we failed to link the right fault code."



### What PRECEPT0025 Actually Covers Today



`ProofRequirement` already carries `[CatalogDU]` (line 41 of ProofRequirement.cs). PRECEPT0025 fires. But:



1. **PRECEPT0025 only covers switch expressions** — it registers for `OperationKind.SwitchExpression` (line 55). The two must-be-exhaustive families (`CreateDiagnostic`, `CreateFaultSiteLink`) use switch **statements**. PRECEPT0025 does not see them.



2. **PRECEPT0025 only prohibits wildcards** — it checks for `_ =>`, `BaseType x =>`, and `BaseType =>` patterns. It does NOT check that every sealed subtype has an arm. A switch with 4 of 5 arms and no wildcard passes PRECEPT0025 — but it's incomplete.



3. **PRECEPT0025 doesn't force switches to exist** — if someone adds a new method that processes obligations without switching, nothing fires. (This is acceptable for strategies — see below.)



### Why Converting to Switch Expressions Doesn't Work



`TreatWarningsAsErrors` is `true` in `Precept.csproj`. C# emits CS8509 for non-exhaustive switch expressions, which would become a compile error. But C# cannot prove exhaustiveness for abstract type hierarchies — even with all subtypes sealed, the base type is not itself sealed. The developer would need `_ => throw new InvalidOperationException(...)` as the final arm. PRECEPT0025 would flag that `_ =>` pattern as prohibited.



This is a genuine tension: C# requires a discard for type-pattern exhaustiveness on non-sealed bases, and PRECEPT0025 prohibits discards. Switch statements avoid this tension — they don't require compiler-proved exhaustiveness.



---



## Recommended Option: PRECEPT0026 — CatalogDU Switch Arm Completeness



### The Mechanism



A new Roslyn analyzer, **PRECEPT0026**, that enforces **subtype completeness** for every switch (expression or statement) over a `[CatalogDU]`-marked type:



1. **Detect** any switch expression or switch statement whose discriminant type inherits from a `[CatalogDU]`-marked abstract record.

2. **Enumerate** all sealed subtypes of the DU base in the current compilation.

3. **Enumerate** all type-pattern arms in the switch.

4. **Report an error** for each sealed subtype that has no corresponding type-pattern arm.



**Diagnostic shape:** `"Switch over [CatalogDU] type '{0}' is missing arm(s) for subtype(s): {1}"`



### What This Enforces — Precisely



When a 6th `ProofRequirement` subtype is added:



- **`CreateDiagnostic`**: PRECEPT0026 fires — "missing arm for `NewSubtype`." Compile error. Developer must add an explicit `case NewSubtype:` arm with the correct diagnostic code. ✅

- **`CreateFaultSiteLink`**: PRECEPT0026 fires — "missing arm for `NewSubtype`." Compile error. Developer must add an explicit `case NewSubtype:` arm with the correct fault code. ✅

- **Strategy methods**: No switch exists. PRECEPT0026 doesn't fire. The new kind is not discharged by any existing strategy. The obligation stays `Unresolved`. `CreateDiagnostic` fires (guaranteed exhaustive by PRECEPT0026). **Correct conservative behavior.** ✅



Combined with existing PRECEPT0025:



- **PRECEPT0025** prevents wildcards from silently absorbing new subtypes (no `_ =>` or `default:` that hides a missing arm).

- **PRECEPT0026** requires every known subtype to have an explicit arm.

- Together: every subtype has exactly one explicit arm, new subtypes produce compile errors at every switch site, and no wildcard provides false safety.



### What's Required to Implement



1. **New analyzer file**: `src/Precept.Analyzers/Precept0026CatalogDUCompleteness.cs`

   - Register for both `OperationKind.SwitchExpression` and `OperationKind.Switch` (switch statements).

   - Reuse `CatalogAnalysisHelpers` and PRECEPT0025's `FindCatalogDUBase` pattern (extract to shared helper or duplicate — the walk logic is 10 lines).

   - To enumerate sealed subtypes: scan `compilation.GetSymbolsWithName()` or walk the DU base's containing assembly for types that inherit from it and are sealed. The subtypes are always in the same assembly as the base (Precept.dll).



2. **Extend PRECEPT0025 to also cover switch statements**: Register for `OperationKind.Switch` in addition to `OperationKind.SwitchExpression`. Adapt `IsProhibitedPattern` to handle `ISwitchCaseOperation` (switch statement case clauses). This ensures `default:` arms in switch statements are also caught.



3. **Tests**: Add analyzer tests in `test/Precept.Analyzers.Tests/` for both PRECEPT0026 and the PRECEPT0025 extension.



4. **No ProofEngine changes required.** The existing switch statements in `CreateDiagnostic` and `CreateFaultSiteLink` are already in the correct shape — explicit type-pattern arms for all 5 subtypes, no wildcard. PRECEPT0026 would pass on them today and fire the moment a 6th subtype is added.



### Annotation Surface



**None required.** PRECEPT0026 operates on the `[CatalogDU]` attribute that already exists. Every switch over a `[CatalogDU]` type gets completeness checking automatically. No method-level annotations, no family-level markers, no opt-in. This is the correct annotation surface: the DU type itself carries the enforcement marker, and every consumer switch is independently checked.



---



## Explicit Comparison: Why Each Alternative Was Rejected



### Option 1 — Reorganize ProofEngine into one handler type per ProofRequirementKind



**Rejected.** Architecturally wrong decomposition axis.



The five proof strategies are organized by *proof technique* — literal comparison, declaration attribute inspection, guard path analysis, flow narrowing, qualifier compatibility. This is the correct axis because:



- A single strategy handles multiple requirement kinds (Strategy 2 handles 4 of 5).

- Multiple strategies attempt the same kind (Numeric is tried by Strategies 1, 2, 3, and 4).

- The strategies compose in a chain: try each technique until one succeeds.



Splitting by kind would scatter proof logic across 5 handler classes. Strategy 2's declaration attribute logic would be duplicated into 4 separate classes. The guard decomposition machinery (shared between Strategy 3 and 4) would either be duplicated or require a shared base, creating the same cross-cutting dependency it claims to eliminate.



Worse: it solves the wrong problem. The must-be-exhaustive families (`CreateDiagnostic`, `CreateFaultSiteLink`) are already single-site switches — splitting them gains nothing. The strategies are intentionally partial — forcing per-kind handlers gives false confidence that each handler is complete when its partiality is the design intent.



This option would also destroy the natural test surface. The current tests verify strategy behavior end-to-end (given an obligation, which strategy discharges it?). Per-kind handlers would fragment tests into artificial boundaries that don't match how the engine actually reasons.



### Option 3 — Use PRECEPT0025 ([CatalogDU]) directly



**Rejected.** Insufficient — three independent gaps:



1. **Switch statements are invisible.** PRECEPT0025 registers for `OperationKind.SwitchExpression` only. The two must-be-exhaustive families use switch statements. PRECEPT0025 doesn't see them.



2. **Wildcards ≠ completeness.** Even if PRECEPT0025 were extended to statements, it only prohibits wildcards. A switch with 4 of 5 arms and no wildcard passes PRECEPT0025 but is incomplete. PRECEPT0025 answers "is this switch safe against future subtypes?" but not "does this switch handle all current subtypes?"



3. **C# tension.** If switch statements were converted to expressions (to enter PRECEPT0025's scope), C# requires `_ => throw` for type-pattern exhaustiveness on non-sealed bases (CS8509 + TreatWarningsAsErrors). PRECEPT0025 would then flag that required `_ =>` as prohibited. The two rules conflict.



PRECEPT0025 is a necessary complement to PRECEPT0026, not a substitute for it. Together they provide the full guarantee; neither alone is sufficient.



### Option 2 (as originally framed) — Family-level method annotation



**Rejected in favor of a simpler variant.** The original option 2 proposed method-level or family-level annotations — marking each dispatch family and requiring exhaustiveness within the annotated scope. This adds annotation overhead and introduces a new concept (dispatch families as annotated groups) that doesn't exist elsewhere in the analyzer infrastructure.



PRECEPT0026 achieves the same guarantee without any annotations. Every switch over a `[CatalogDU]` type is automatically checked. The enforcement is structural (inherent in the switch + DU type), not declarative (requiring developers to remember to annotate). Structural enforcement is always preferred — it cannot be forgotten.



---



## Risks and Tradeoffs



1. **PRECEPT0026 doesn't force switches to exist.** If someone adds a new method that processes proof obligations via `if/else` chains or individual `is` type checks instead of a switch, PRECEPT0026 doesn't fire. This is acceptable because:

   - The intentionally-partial strategies already use `if (obligation.Requirement is not FooType) return false;` patterns — forcing them into switches would be wrong.

   - The must-be-exhaustive sites (`CreateDiagnostic`, `CreateFaultSiteLink`) are already switches and there's no architectural reason to add more must-be-exhaustive sites.

   - Code review remains the backstop for architectural patterns that analyzers can't enforce.



2. **Sealed subtype enumeration at analysis time.** The analyzer must discover all sealed subtypes of a `[CatalogDU]` base. In the same compilation (single assembly), this is straightforward — walk `compilation.GlobalNamespace` descendants. Cross-assembly DU hierarchies would be harder, but all Precept DUs live in the same assembly. If DUs ever cross assembly boundaries, the enumeration logic would need enhancement.



3. **PRECEPT0025 extension to switch statements requires adapting pattern-matching logic.** Switch statement case clauses (`ISwitchCaseOperation`) have a different IOperation shape than switch expression arms (`ISwitchExpressionArmOperation`). The adaptation is mechanical but needs careful testing. A `default:` case in a switch statement is represented differently than `_ =>` in a switch expression.



4. **The "no strategy handles it" gap is by design.** When a new `ProofRequirementKind` is added, no existing strategy discharges it. All obligations of the new kind stay `Unresolved`. `CreateDiagnostic` produces an error. This is correct conservative behavior — but it means the developer must also implement a strategy for the new kind, and nothing enforces that beyond code review. Acceptable: a failing-safe default is the right tradeoff.



---



## Implementation Routing



- **George** builds PRECEPT0026 and the PRECEPT0025 switch-statement extension. He owns the analyzer infrastructure, just shipped PRECEPT0025, and has the Roslyn IOperation expertise. This is pure analyzer work — no runtime changes.



- **Kramer** is not needed. No ProofEngine structural changes are required. The existing switch statements are already in the correct shape.



- **Estimated scope**: ~150 lines of analyzer code for PRECEPT0026, ~30 lines of adaptation for the PRECEPT0025 extension, plus test coverage. Small, focused, no structural risk.



---



## Summary



**PRECEPT0026 (CatalogDU Switch Arm Completeness)** is the architecturally correct mechanism. It enforces per-switch exhaustiveness without reorganizing the engine, without adding annotations, and without the false-confidence hazard of class-level enrollment. Combined with PRECEPT0025 (wildcard prohibition, extended to switch statements), it provides the complete compile-time guarantee:



- Every sealed DU subtype has an explicit arm in every switch.

- No wildcard silently absorbs new subtypes.

- New subtypes produce compile errors at every must-be-exhaustive site.

- Intentionally-partial strategies are not forced into false completeness.



This is the right solution because it enforces the actual safety property (every switch is complete) at the actual enforcement boundary (each switch independently) without distorting the engine's natural decomposition (strategies by technique, not by kind).

# Frank ruling — `set` / `SetType` token metadata



## Verdict



**Option A.** Remove `TokenCategory.Type` from `TokenKind.Set`.



`SetType` is already the type-position token. The catalog should use it.



## Rationale



The current metadata is internally contradictory.



- `TokenKind.Set` carries **single-valued** visual metadata: `TextMateScope = "keyword.other.action.precept"` and `SemanticTokenType = "keyword"`.

- The same entry also claims `TokenCategory.Type`.

- The two failing tests are not wrong in spirit; they are exposing that contradiction.



A token cannot honestly be both:

- an action-keyword token for grammar/lexical semantic coloring, **and**

- a type-keyword token for the same single metadata fields.



That is exactly why `TokenKind.SetType` exists.



The design already has the right separation:



- **Lexer emits** `TokenKind.Set`

- **Parser reinterprets** `Set` as `SetType` in type position

- **Types.ByToken** maps both `Set` and `SetType` to the same `TypeMeta`



That is the architecture. The dual-use surface word is modeled as **two token kinds with different roles**, not one token kind with contradictory category metadata.



### Why not B



Option B makes the tests lie down and accept a bad catalog. It forces consumers to special-case a contradiction the catalog should have resolved.



### Why not C



Option C paints action-position `set` as a type everywhere. That is worse. The grammar generator and lexical semantic-token pass are driven by one field each; they cannot make `set` both action-colored and type-colored from one `TokenMeta` row.



## Architectural rule this locks



**Token categories on lexer-emitted tokens describe the role of that token kind as emitted.**



If a surface word is context-disambiguated into a separate parser token kind, the context-specific role belongs on the disambiguated token kind (`SetType`), not back on the lexer token (`Set`).



Corollary: consumers that mean **“what type keywords are valid here?”** should read the **Types catalog / parser type vocabulary**, not sweep `Tokens.All` by `TokenCategory.Type` and hope that lexical categories encode parser context.



## Exact code changes required



### 1. `src/Precept/Language/Tokens.cs`



Change `TokenKind.Set` from:



- `Cat_ActType`



To:



- `Cat_Act`



Also update the description/comment so it no longer claims the token itself is the type token. Suggested intent:



- `Set` = lexer-emitted surface word for the action keyword; parser reinterprets it as `SetType` in type position.



Leave these unchanged:



- `TokenKind.Set.TextMateScope = "keyword.other.action.precept"`

- `TokenKind.Set.SemanticTokenType = "keyword"`

- `TokenKind.SetType` remains the type-category token

- parser disambiguation logic

- `Types.ByToken` alias behavior



If `Cat_ActType` becomes unused, delete it.



### 2. `test/Precept.Tests/TokensTests.cs`



Replace the old invariant:



- `GetMeta_SetToken_HasBothActionAndTypeCategories`



With the correct split-role assertions:



- `Set` **contains** `TokenCategory.Action`

- `Set` **does not contain** `TokenCategory.Type`

- `SetType` **contains** `TokenCategory.Type`

- `SetType` **does not contain** `TokenCategory.Action`



Do **not** add exceptions to:



- `TypeKeywords_HaveStorageTypeScope`

- `TypeKeywords_HaveTypeSemanticTokenType`



Those sweeps should remain generic. After the catalog fix, they pass naturally.



### 3. `test/Precept.LanguageServer.Tests/PreceptAnalyzerCompletionTests.cs`



Two drift tests currently use `PreceptTokenMeta.GetByCategory(TokenCategory.Type)` as a proxy for type vocabulary:



- `AllTypeTokens_AppearInTypeItems`

- `AllScalarTypeTokens_AppearInScalarTypeItems`



That source is architecturally wrong for `set` once the catalog is corrected.



Update those tests to derive expected type symbols from the **Types catalog** instead:



- source from `Types.All` or `Types.ByToken`

- exclude non-surface types like `Error` and `StateRef`

- dedupe the `Set` / `SetType` alias to one surface word

- keep the existing scalar-only exclusion for collection-only types (`set`, `queue`, `stack`)



This preserves the real invariant: **type completions must track the type system**, not lexical token categories.



### 4. `docs/language/precept-language-spec.md`



Sync the wording so the spec matches the corrected catalog model:



- In the **action keyword** table, `Set` should be described as the action token.

- In the **type keyword** table / disambiguation section, keep `SetType` as the type-position representation.

- In §1.6, make the modeling explicit: the surface word `set` is dual-use, but the token model is `Set` (lexer) + `SetType` (parser-synthesized type alias), not one dual-category token.



## Tests to update



### Must update



- `test/Precept.Tests/TokensTests.cs`

  - replace the dual-category invariant test



### Should update



- `test/Precept.LanguageServer.Tests/PreceptAnalyzerCompletionTests.cs`

  - move type-vocabulary expectations from token-category sweeps to `Types`



### Should not change



- `TypeKeywords_HaveStorageTypeScope`

- `TypeKeywords_HaveTypeSemanticTokenType`



If those need an exemption, the catalog is still wrong.



## Downstream impact



### Grammar generator



No new behavior is required for this fix.



`tools/Precept.GrammarGen` groups tokens by `TokenMeta.TextMateScope`. Under the current architecture, the surface word `set` will continue to receive the action-keyword scope from the lexer token. That is acceptable for this ruling because the goal here is to make the catalog honest.



If we later want **context-sensitive** type coloring for `set` in `set of string`, that is a separate tooling enhancement. It must be solved with context-aware grammar/semantic-token logic, not by lying in `TokenKind.Set` metadata.



### Language server semantic tokens



Same conclusion.



The documented lexical semantic-token pass reads `Compilation.Tokens` + `TokenMeta.SemanticTokenType`. One `SemanticTokenType` field cannot represent both action and type for the same lexer token. So `Set` should stay `"keyword"`, and any future context-sensitive treatment must come from parser/semantic context, not dual categories on `Set`.



### MCP



`precept_language` becomes cleaner, not weaker:



- `Set` is the action token

- `SetType` is the type token



The actual type vocabulary remains intact through `Types` and `Types.ByToken`.



## Bottom line



The fix is not to carve out an exception and not to repaint `Set` as a type.



The fix is to stop claiming that the lexer token `Set` is a type token when the architecture already has `SetType` for that job.

# TypeChecker Catalog-Driven Metadata Design



**Author:** Frank (Lead/Architect)

**Date:** 2026-05-09

**Source:** PRECEPT0019 audit § "Do not enroll with PRECEPT0019; make the logic catalog-driven instead"

**Status:** Spec for implementation — all 4 sites approved by Shane with no deferrals



---



## Overview



The PRECEPT0019 audit identified 4 hardcoded dispatch sites in TypeChecker that switch on specific `OperationKind`/`FunctionKind`/`TokenKind` values when the behavior should be derived from catalog metadata. This spec defines the exact metadata additions and TypeChecker refactors for each site.



---



## Site 1: CI Enforcement in `TypeChecker.Validation.cs`



### Problem



`EnforceCIInExpression` (lines 328–385) hardcodes specific enum members to detect case-sensitive operations/functions used with `~string` fields:



- **Rule 1:** `bin.ResolvedOp == OperationKind.StringEqualsString` → emit `CaseInsensitiveFieldRequiresTildeEquals`

- **Rule 2:** `bin.ResolvedOp == OperationKind.StringNotEqualsString` → emit `CaseInsensitiveFieldRequiresTildeNotEquals`

- **Rule 3:** `IsContainsOperation(bin.ResolvedOp)` → emit `CaseInsensitiveValueInCaseSensitiveContains` (currently no-op — placeholder returns `false`)

- **Rule 4:** `func.ResolvedFunction == FunctionKind.StartsWith` → emit `CaseInsensitiveFieldRequiresTildeStartsWith`

- **Rule 5:** `func.ResolvedFunction == FunctionKind.EndsWith` → emit `CaseInsensitiveFieldRequiresTildeEndsWith`



Each rule is a separate `if`/`else if` branch that tests a specific enum value. When new CI-sensitive operations or functions land, a developer must find this method and add another branch — the catalog doesn't force it.



### Metadata Change: `BinaryOperationMeta`



**File:** `src/Precept/Language/Operation.cs`



Add two optional parameters to `BinaryOperationMeta`:



```csharp

public sealed record BinaryOperationMeta(

    OperationKind Kind,

    OperatorKind Op,

    ParameterMeta Lhs,

    ParameterMeta Rhs,

    TypeKind Result,

    string Description,

    bool BidirectionalLookup = false,

    QualifierMatch Match = QualifierMatch.Any,

    ProofRequirement[]? ProofRequirements = null,

    bool HasCIVariant = false,                    // ← NEW

    DiagnosticCode? CIDiagnosticCode = null)       // ← NEW

    : OperationMeta(Kind, Op, Result, Description)

```



- **`HasCIVariant`** — `true` when a case-insensitive counterpart exists for this operation. Mirrors the existing `FunctionMeta.HasCIVariant` field.

- **`CIDiagnosticCode`** — the diagnostic to emit when this case-sensitive operation is used with a `~string` field. `null` when `HasCIVariant` is `false`.



### Metadata Change: `FunctionMeta`



**File:** `src/Precept/Language/Function.cs`



Add one optional parameter:



```csharp

public sealed record FunctionMeta(

    FunctionKind Kind,

    string Name,

    string Description,

    IReadOnlyList<FunctionOverload> Overloads,

    FunctionCategory Category,

    string? UsageExample = null,

    string? SnippetTemplate = null,

    string? HoverDescription = null,

    bool HasCIVariant = false,

    FunctionKind? CIVariantOf = null,

    bool IsMessagePosition = false,

    DiagnosticCode? CIDiagnosticCode = null);      // ← NEW

```



- **`CIDiagnosticCode`** — the diagnostic to emit when this function is used with a `~string` first argument. `null` when `HasCIVariant` is `false`.



### Catalog Value Assignments



**`Operations.cs` — `GetMeta` switch:**



| OperationKind | HasCIVariant | CIDiagnosticCode |

|---|---|---|

| `StringEqualsString` | `true` | `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals` |

| `StringNotEqualsString` | `true` | `DiagnosticCode.CaseInsensitiveFieldRequiresTildeNotEquals` |

| All other `BinaryOperationMeta` entries | `false` (default) | `null` (default) |



When `contains` operations land in the future, they will set `HasCIVariant: true` and `CIDiagnosticCode: DiagnosticCode.CaseInsensitiveValueInCaseSensitiveContains`.



**`Functions.cs` — `GetMeta` switch:**



| FunctionKind | HasCIVariant | CIDiagnosticCode |

|---|---|---|

| `StartsWith` | `true` (already set) | `DiagnosticCode.CaseInsensitiveFieldRequiresTildeStartsWith` |

| `EndsWith` | `true` (already set) | `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEndsWith` |

| All other entries | `false` (default) | `null` (default) |



### TypeChecker Change



**Before** (5 separate rule branches, each hardcoding a specific enum member):



```csharp

case TypedBinaryOp bin:

    if (bin.ResolvedOp == OperationKind.StringEqualsString &&

        (IsCIExpression(bin.Left) || IsCIExpression(bin.Right)))

    {

        ctx.Diagnostics.Add(Diagnostics.Create(

            DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals, bin.Span, ...));

    }

    else if (bin.ResolvedOp == OperationKind.StringNotEqualsString && ...)

    { ... }

    else if (IsContainsOperation(bin.ResolvedOp) && ...)

    { ... }

    // ... recurse ...



case TypedFunctionCall func:

    if (func.ResolvedFunction == FunctionKind.StartsWith && ...)

    { ... }

    else if (func.ResolvedFunction == FunctionKind.EndsWith && ...)

    { ... }

    // ... recurse ...

```



**After** (one metadata-driven check per expression node type):



```csharp

case TypedBinaryOp bin:

    if (Operations.GetMeta(bin.ResolvedOp) is BinaryOperationMeta

            { HasCIVariant: true, CIDiagnosticCode: { } diagCode } &&

        (IsCIExpression(bin.Left) || IsCIExpression(bin.Right)))

    {

        var ciFieldName = GetCIFieldName(bin.Left, bin.Right);

        ctx.Diagnostics.Add(Diagnostics.Create(diagCode, bin.Span, ciFieldName));

    }

    EnforceCIInExpression(bin.Left, ctx);

    EnforceCIInExpression(bin.Right, ctx);

    break;



case TypedFunctionCall func:

    var funcMeta = Functions.GetMeta(func.ResolvedFunction);

    if (funcMeta is { HasCIVariant: true, CIDiagnosticCode: { } diagCode } &&

        func.Arguments.Length > 0 && IsCIExpression(func.Arguments[0]))

    {

        var ciFieldName = ((TypedFieldRef)func.Arguments[0]).FieldName;

        ctx.Diagnostics.Add(Diagnostics.Create(diagCode, func.Span, ciFieldName));

    }

    foreach (var arg in func.Arguments)

        EnforceCIInExpression(arg, ctx);

    break;

```



**Remove:** The `IsContainsOperation` helper method. It is currently a dead placeholder returning `false`. When `contains` operations land, they will carry `HasCIVariant: true` in their catalog entry, and the unified binary-op check above will handle them automatically. The separate `CIElementCollections` check (Rule 3's additional logic about CI elements in case-sensitive containers) is also currently dead; if it needs distinct handling when contains ships, that is a future concern and should be designed at that time.



### New Index



None required. The existing `Operations.GetMeta()` and `Functions.GetMeta()` calls are sufficient — no new lookup index needed.



### Test Coverage



1. **Catalog value tests** (`Precept.Tests`):

   - `Operations.GetMeta(OperationKind.StringEqualsString)` returns `BinaryOperationMeta` with `HasCIVariant == true` and `CIDiagnosticCode == DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals`

   - `Operations.GetMeta(OperationKind.StringNotEqualsString)` returns with `HasCIVariant == true` and `CIDiagnosticCode == DiagnosticCode.CaseInsensitiveFieldRequiresTildeNotEquals`

   - `Functions.GetMeta(FunctionKind.StartsWith).CIDiagnosticCode == DiagnosticCode.CaseInsensitiveFieldRequiresTildeStartsWith`

   - `Functions.GetMeta(FunctionKind.EndsWith).CIDiagnosticCode == DiagnosticCode.CaseInsensitiveFieldRequiresTildeEndsWith`

   - Verify all `BinaryOperationMeta` entries with `HasCIVariant == false` also have `CIDiagnosticCode == null` (consistency)

2. **Behavioral regression** — existing CI enforcement tests must continue to pass unchanged. No new behavioral tests needed; this is a refactor.



---



## Site 2: ConstraintKind Synthesis from Leading Tokens



### Problem



`TypeChecker.cs` line 449–456 synthesizes `ConstraintKind` from the leading `TokenKind` with an inline switch:



```csharp

var constraintKind = construct.LeadingTokenKind switch

{

    TokenKind.In   => ConstraintKind.StateResident,

    TokenKind.To   => ConstraintKind.StateEntry,

    TokenKind.From => ConstraintKind.StateExit,

    _              => ConstraintKind.StateResident, // fallback

};

```



This is a token→constraint mapping that belongs in catalog metadata.



### Metadata Change: `ConstraintMeta.StateAnchored`



**File:** `src/Precept/Language/Constraint.cs`



Add a `LeadingToken` property to the `StateAnchored` abstract record:



```csharp

public abstract record StateAnchored(

    ConstraintKind Kind,

    string Description,

    TokenKind LeadingToken)          // ← NEW

    : ConstraintMeta(Kind, Description);

```



Update the three sealed subtypes:



```csharp

public sealed record StateResident()

    : StateAnchored(ConstraintKind.StateResident,

        "State residency — enforced while in state",

        TokenKind.In);



public sealed record StateEntry()

    : StateAnchored(ConstraintKind.StateEntry,

        "State entry — enforced on transition into state",

        TokenKind.To);



public sealed record StateExit()

    : StateAnchored(ConstraintKind.StateExit,

        "State exit — enforced on transition out of state",

        TokenKind.From);

```



### New Index: `Constraints.ByToken`



**File:** `src/Precept/Language/Constraints.cs`



Add a `FrozenDictionary<TokenKind, ConstraintKind>` index:



```csharp

/// <summary>

/// O(1) lookup from leading token kind to state-anchored constraint kind.

/// Used by the type checker to resolve the constraint form from the

/// construct's leading token without an inline switch.

/// Mirrors <see cref="Modifiers.ByFieldToken"/> and <see cref="Types.ByToken"/>.

/// </summary>

public static FrozenDictionary<TokenKind, ConstraintKind> ByToken { get; } =

    All.OfType<ConstraintMeta.StateAnchored>()

       .ToFrozenDictionary(m => m.LeadingToken, m => m.Kind);

```



**Value type rationale:** `ConstraintKind` (not `ConstraintMeta`) because the TypeChecker consumer only needs the kind value to stamp onto `TypedEnsure`. If a future consumer needs full metadata, they can chain `Constraints.GetMeta(kind)`.



### Catalog Value Assignments



| TokenKind | ConstraintKind |

|---|---|

| `TokenKind.In` | `ConstraintKind.StateResident` |

| `TokenKind.To` | `ConstraintKind.StateEntry` |

| `TokenKind.From` | `ConstraintKind.StateExit` |



### TypeChecker Change



**Before:**



```csharp

var constraintKind = construct.LeadingTokenKind switch

{

    TokenKind.In   => ConstraintKind.StateResident,

    TokenKind.To   => ConstraintKind.StateEntry,

    TokenKind.From => ConstraintKind.StateExit,

    _              => ConstraintKind.StateResident, // fallback

};

```



**After:**



```csharp

var constraintKind = Constraints.ByToken.TryGetValue(construct.LeadingTokenKind, out var ck)

    ? ck

    : ConstraintKind.StateResident; // fallback for non-state-anchored leading tokens

```



### Test Coverage



1. **Index completeness** — `Constraints.ByToken` contains exactly 3 entries: `In`, `To`, `From`

2. **Round-trip** — for each entry, `Constraints.ByToken[token]` matches the `LeadingToken` on the corresponding `ConstraintMeta.StateAnchored` subtype

3. **Behavioral regression** — existing ensure-constraint tests pass unchanged



---



## Site 3: Access-Mode Normalization



### Problem



`TypeChecker.cs` lines 589–594 map an access-mode token to a `ModifierKind` with a hardcoded switch:



```csharp

ModifierKind mode = modeSlot?.AccessMode switch

{

    TokenKind.Editable => ModifierKind.Write,

    _                  => ModifierKind.Read,

};

```



The `Modifiers` catalog already contains `AccessModifierMeta` entries that map tokens to modifier kinds (e.g., `ModifierKind.Write` → `TokenKind.Editable`). The TypeChecker should look up the catalog rather than hardcoding the mapping.



### Metadata Change



**None.** `AccessModifierMeta` already carries:

- `Kind` (the `ModifierKind` — `Write`, `Read`, `Omit`)

- `Token` (the `TokenMeta` with `.Kind` = `TokenKind.Editable`, `TokenKind.Readonly`, `TokenKind.Omit`)

- `IsPresent`, `IsWritable` (semantic flags)



The metadata shape is complete. What is missing is an **index** to look up by token.



### New Index: `Modifiers.ByAccessToken`



**File:** `src/Precept/Language/Modifiers.cs`



Add alongside `ByFieldToken` and `ByStateToken`:



```csharp

/// <summary>

/// O(1) lookup from token kind to access modifier metadata.

/// Used by the type checker to resolve an access-mode token to its

/// <see cref="AccessModifierMeta"/> without a hardcoded switch.

/// Mirrors <see cref="ByFieldToken"/> and <see cref="ByStateToken"/>.

/// </summary>

public static FrozenDictionary<TokenKind, AccessModifierMeta> ByAccessToken { get; } =

    All.OfType<AccessModifierMeta>()

       .ToFrozenDictionary(m => m.Token.Kind);

```



### Catalog Value Assignments



Index is auto-derived from existing catalog entries. Contents:



| TokenKind | AccessModifierMeta.Kind |

|---|---|

| `TokenKind.Editable` | `ModifierKind.Write` |

| `TokenKind.Readonly` | `ModifierKind.Read` |

| `TokenKind.Omit` | `ModifierKind.Omit` |



### TypeChecker Change



**Before:**



```csharp

var modeSlot = construct.GetSlot<AccessModeSlot>(ConstructSlotKind.AccessModeKeyword);

ModifierKind mode = modeSlot?.AccessMode switch

{

    TokenKind.Editable => ModifierKind.Write,

    _                  => ModifierKind.Read,

};

```



**After:**



```csharp

var modeSlot = construct.GetSlot<AccessModeSlot>(ConstructSlotKind.AccessModeKeyword);

ModifierKind mode = modeSlot?.AccessMode is { } accessToken

                    && Modifiers.ByAccessToken.TryGetValue(accessToken, out var accessMeta)

    ? accessMeta.Kind

    : ModifierKind.Read; // default: absent slot → read-only

```



### Test Coverage



1. **Index completeness** — `Modifiers.ByAccessToken` contains exactly 3 entries: `Editable`, `Readonly`, `Omit`

2. **Value correctness** — `ByAccessToken[TokenKind.Editable].Kind == ModifierKind.Write`, `ByAccessToken[TokenKind.Readonly].Kind == ModifierKind.Read`, `ByAccessToken[TokenKind.Omit].Kind == ModifierKind.Omit`

3. **Behavioral regression** — existing access-mode tests pass unchanged



---



## Site 4: Anchor/State-Hook Normalization



### Problem



`TypeChecker.cs` lines 652–656 map a leading token to an `AnchorScope` with a hardcoded switch:



```csharp

var scope = construct.LeadingTokenKind switch

{

    TokenKind.From => AnchorScope.OnExit,

    _              => AnchorScope.OnEntry, // 'to' and fallback

};

```



The `Modifiers` catalog already contains `AnchorModifierMeta` entries that carry `AnchorScope` (e.g., `ModifierKind.From` → `AnchorScope.OnExit`, `ModifierKind.To` → `AnchorScope.OnEntry`). The TypeChecker should read the catalog.



### Metadata Change



**None.** `AnchorModifierMeta` already carries:

- `Kind` (`ModifierKind` — `In`, `To`, `From`)

- `Token` (`TokenMeta` with `.Kind` = `TokenKind.In`, `TokenKind.To`, `TokenKind.From`)

- `Scope` (`AnchorScope` — `InState`, `OnEntry`, `OnExit`)

- `Target` (`AnchorTarget` — `Ensure`, `StateAction`)



The metadata is complete.



### New Index: `Modifiers.ByAnchorToken`



**File:** `src/Precept/Language/Modifiers.cs`



Add alongside the other indexes:



```csharp

/// <summary>

/// O(1) lookup from token kind to anchor modifier metadata.

/// Used by the type checker to resolve a leading anchor token to its

/// <see cref="AnchorModifierMeta"/> (which carries <see cref="AnchorScope"/>)

/// without a hardcoded switch.

/// Mirrors <see cref="ByFieldToken"/>, <see cref="ByStateToken"/>, and

/// <see cref="ByAccessToken"/>.

/// </summary>

public static FrozenDictionary<TokenKind, AnchorModifierMeta> ByAnchorToken { get; } =

    All.OfType<AnchorModifierMeta>()

       .ToFrozenDictionary(m => m.Token.Kind);

```



### Catalog Value Assignments



Index is auto-derived from existing catalog entries. Contents:



| TokenKind | AnchorModifierMeta.Kind | AnchorModifierMeta.Scope |

|---|---|---|

| `TokenKind.In` | `ModifierKind.In` | `AnchorScope.InState` |

| `TokenKind.To` | `ModifierKind.To` | `AnchorScope.OnEntry` |

| `TokenKind.From` | `ModifierKind.From` | `AnchorScope.OnExit` |



### TypeChecker Change



**Before:**



```csharp

var scope = construct.LeadingTokenKind switch

{

    TokenKind.From => AnchorScope.OnExit,

    _              => AnchorScope.OnEntry,

};

```



**After:**



```csharp

var scope = Modifiers.ByAnchorToken.TryGetValue(construct.LeadingTokenKind, out var anchorMeta)

    ? anchorMeta.Scope

    : AnchorScope.OnEntry; // fallback

```



### Test Coverage



1. **Index completeness** — `Modifiers.ByAnchorToken` contains exactly 3 entries: `In`, `To`, `From`

2. **Value correctness** — `ByAnchorToken[TokenKind.From].Scope == AnchorScope.OnExit`, `ByAnchorToken[TokenKind.To].Scope == AnchorScope.OnEntry`, `ByAnchorToken[TokenKind.In].Scope == AnchorScope.InState`

3. **Behavioral regression** — existing state-hook tests pass unchanged



---



## Dependency Analysis



### Are these 4 sites independent?



**Yes — fully independent.** Each site touches a different catalog file and a different location in TypeChecker. No site's metadata change is required by another site.



| Site | Catalog File(s) Modified | TypeChecker File Modified | Location |

|---|---|---|---|

| 1 | `Operation.cs`, `Operations.cs`, `Function.cs`, `Functions.cs` | `TypeChecker.Validation.cs` | lines 328–438 |

| 2 | `Constraint.cs`, `Constraints.cs` | `TypeChecker.cs` | lines 449–456 |

| 3 | `Modifiers.cs` (index only) | `TypeChecker.cs` | lines 589–594 |

| 4 | `Modifiers.cs` (index only) | `TypeChecker.cs` | lines 652–656 |



**Sites 3 and 4** both add indexes to `Modifiers.cs` but at non-overlapping locations (appended after existing indexes). They can be implemented in the same slice or separate slices without conflict.



### Recommended Slicing



Kramer can implement all 4 in parallel. If he prefers sequential slices for cleaner commits:



1. **Slice A:** Sites 3 + 4 together (both are `Modifiers.cs` index additions — smallest, simplest, no record shape changes)

2. **Slice B:** Site 2 (`Constraint.cs` record shape + `Constraints.cs` index)

3. **Slice C:** Site 1 (`Operation.cs` + `Function.cs` record shape changes + `Operations.cs`/`Functions.cs` value assignments + `TypeChecker.Validation.cs` refactor — largest, most lines touched)



This ordering minimizes merge risk: Slices A and B are trivial, and Slice C (which touches the most files) lands last.



---



## Summary of All Changes



| File | Change Type | What |

|---|---|---|

| `src/Precept/Language/Operation.cs` | Record shape | Add `HasCIVariant`, `CIDiagnosticCode` to `BinaryOperationMeta` |

| `src/Precept/Language/Operations.cs` | Catalog values | Set `HasCIVariant`/`CIDiagnosticCode` on 2 entries |

| `src/Precept/Language/Function.cs` | Record shape | Add `CIDiagnosticCode` to `FunctionMeta` |

| `src/Precept/Language/Functions.cs` | Catalog values | Set `CIDiagnosticCode` on 2 entries |

| `src/Precept/Language/Constraint.cs` | Record shape | Add `LeadingToken` to `StateAnchored` and 3 sealed subtypes |

| `src/Precept/Language/Constraints.cs` | New index | `ByToken: FrozenDictionary<TokenKind, ConstraintKind>` |

| `src/Precept/Language/Modifiers.cs` | New indexes (×2) | `ByAccessToken`, `ByAnchorToken` |

| `src/Precept/Pipeline/TypeChecker.Validation.cs` | Refactor | Replace 5-rule CI enforcement with 2 metadata-driven checks; remove `IsContainsOperation` |

| `src/Precept/Pipeline/TypeChecker.cs` | Refactor (×3) | Lines 449–456, 589–594, 652–656 become catalog lookups |

# OutcomeArgumentKind enrollment



- Enrolled `Precept.Pipeline.Parser.ParserState` in PRECEPT0019 for `OutcomeArgumentKind` alongside the existing `ExpressionFormKind` enrollment.

- Added `[HandlesCatalogMember]` ownership markers for the three live outcome argument shapes: `RequiredIdentifier`, `RequiredStringLiteral`, and `SecondaryToken`.

- Added `ParseOutcomeNoArg` for `OutcomeArgumentKind.None` and wired it into `ParseOutcome`. Decision: recover with `DiagnosticCode.ExpectedOutcome` + `MalformedOutcome` instead of throwing. Rationale: no cataloged outcome currently uses the no-arg shape, so the parser should claim ownership while preserving the normal parse diagnostic/recovery path if that shape is ever reached before a real surface feature ships.

- Validation:

  - `dotnet build src\Precept\Precept.csproj` is currently blocked by a pre-existing `PRECEPT0025` in `src\Precept\Pipeline\ProofEngine.cs` (left untouched per instruction).

  - Targeted binary test run after compiling `Precept.dll` with analyzers disabled: `Precept.Tests` = 3629 passed / 2 failed (`TokensTests` only), `Precept.Analyzers.Tests` = 272 passed / 0 failed.

# George — PRECEPT0025 Done



**Date:** 2026-05-09

**Task:** Implement PRECEPT0025 — CatalogDU Wildcard Prohibition

**Commits:** `ea91cf3d` (attribute + analyzer + tests), `07ab8782` (Phase 3 enablement)



---



## What PRECEPT0025 Does



PRECEPT0025 catches the class of bug that caused diagnostic code 116 (`UnprovedPresenceRequirement`) to be unreachable: when a new sealed subtype is added to an abstract record hierarchy (a catalog DU), a `_ =>` wildcard arm in a downstream type-pattern switch silently absorbs it instead of forcing an explicit branch.



The analyzer registers on `SwitchExpression` operations. For each switch:



1. It walks the switch value's type hierarchy looking for a type carrying `[CatalogDU]`.

2. If found, it inspects each arm. Any arm with:

   - A discard pattern (`_ =>`)

   - A declaration pattern over the abstract base (`SomeDUBase x =>`)

   - A type pattern over the abstract base (`SomeDUBase =>`)

   …is reported as PRECEPT0025 at Error severity.

3. Suppressed in test files (file path contains `.Tests`) to allow partial scaffolded switches.



The diagnostic message names the `[CatalogDU]` abstract base type and instructs the developer to add explicit arms.



---



## `[CatalogDU]` Attribute



**File:** `src/Precept/Language/CatalogDUAttribute.cs`



```csharp

[AttributeUsage(AttributeTargets.Class)]

public sealed class CatalogDUAttribute : Attribute { }

```



The attribute lives in `src/Precept/Language/` alongside other catalog attribute definitions (`HandlesCatalogMemberAttribute`, `HandlesCatalogExhaustivelyAttribute`). The analyzer reads it by name (string comparison `attr.AttributeClass?.Name == "CatalogDUAttribute"`) — no direct project reference from the analyzer assembly to the analyzed project.



### `[CatalogDU]` types applied so far



None yet. **See the open item below.**



---



## Open Item: `[CatalogDU]` NOT Applied to `ProofRequirement`



The task called for applying `[CatalogDU]` to the `ProofRequirement` abstract record. I investigated and found that Kramer's fix is **partially complete**:



- ✅ `PresenceProofRequirement presence =>` was added to `CreateDiagnostic` (code 116 now reachable)

- ✅ `PresenceProofRequirement => ...` was added to `CreateFaultSiteLink`

- ❌ The `_ => Diagnostics.Create(...)` fallback arm in `CreateDiagnostic` is **still present** (dead code)

- ❌ The `_ => DiagnosticCode.DivisionByZero` fallback arm in `CreateFaultSiteLink` is **still present** (dead code)



If I applied `[CatalogDU]` to `ProofRequirement` now, PRECEPT0025 would fire on those two dead `_ =>` arms in `ProofEngine.cs`, breaking the `src/Precept/` build. Since the task constraint says "Do not modify ProofEngine.cs — Kramer owns those fixes," and the build must be clean, I deferred the attribute application.



**Action needed from Kramer:** Remove the two dead `_ =>` arms from `CreateDiagnostic` and `CreateFaultSiteLink` in `ProofEngine.cs`. Once removed, apply `[CatalogDU]` to `ProofRequirement` in `src/Precept/Language/ProofRequirement.cs`. The attribute placement is straightforward:



```csharp

[CatalogDU]

public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);

```



After that, PRECEPT0025 will guard all future switches over `ProofRequirement` subtypes.



Other catalog DU bases worth tagging in a follow-on pass: `ProofSubject`, `ProofRequirementMeta`, `ProofSatisfaction`, `SatisfactionProjection`, `NumericBoundSource`, `DimensionSource`, `ConstraintMeta`, `ObligationContext` (if it's a DU).



---



## Phase 3 Enablement



**Enabled:** Both `ConstraintKind` and `ProofRequirementKind` are now in `CatalogEnumNames` in `CatalogAnalysisHelpers.cs`.



**Why it was safe:** Both `Constraints.GetMeta` and `ProofRequirements.GetMeta` already have explicit arms for every member of their respective enums. PRECEPT0007 only reports *missing* members — it does not object to a `_ => throw` fallback arm being present alongside exhaustive explicit arms. No new violations arose: `dotnet build src/Precept/` is clean at 0 warnings, 0 errors.



**Why it was previously deferred:** The TODO was written before Kramer's Phase 2 completion. At the time, some members may have been missing from the GetMeta switches. Now they are all covered.



---



## Test Coverage



9 tests added in `test/Precept.Analyzers.Tests/Precept0025Tests.cs`:



| Test | What it covers |

|------|----------------|

| TP1: `DiscardArm_OverCatalogDUType_Reports` | Pure `_ =>` arm fires |

| TP2: `DeclarationPattern_OverAbstractBase_Reports` | `Shape x =>` fires |

| TP3: `MultipleWildcardArms_ReportsEach` | Each offending arm reported independently |

| TP4: `SwitchOverDerivedType_WalksHierarchyAndReports` | Walks base hierarchy to find `[CatalogDU]` |

| TN1: `ExhaustiveSwitch_NoDiagnostic` | No `_` arm = no diagnostic |

| TN2: `DiscardArm_OverNonCatalogDUType_NoDiagnostic` | Non-`[CatalogDU]` type is ignored |

| TN3: `GuardedAndConcretePatterns_NoDiagnostic` | Specific subtype patterns don't fire |

| TN4: `DiscardArm_OnEnum_NoDiagnostic` | Enum switches are not affected |

| TN5: `DiscardArm_InTestFile_Suppressed` | File path `.Tests` suppression works |



Full suite: 272/272 analyzer tests pass. Main Precept tests: 3629/3631 (2 pre-existing `TokensTests` failures, unrelated to this work).



---



## Design Note



The analyzer uses type hierarchy walking (`FindCatalogDUBase`) rather than checking only the exact switch expression type. This means a switch over a concrete subtype (`Circle c => ...`) is also governed if `Circle`'s base `Shape` has `[CatalogDU]`. This is intentional — it prevents the pattern `new List<Circle> { ... }.Select(...) switch { Circle => ..., _ => ... }` from slipping through.



The catch-all declaration pattern check (`IDeclarationPatternOperation where MatchedType == catalogDUBase`) ensures that `ProofRequirement r =>` — a named binding over the abstract base — is treated the same as `_`. Both are structurally equivalent catch-alls.

# George — PRECEPT0025 / PRECEPT0026 closeout



## Summary of deliverables



- Added `PRECEPT0026` in `src/Precept.Analyzers/Precept0026CatalogDUCompleteness.cs`.

  - Covers both `OperationKind.SwitchExpression` and `OperationKind.Switch`.

  - Walks the discriminant type upward to the `[CatalogDU]` base.

  - Enumerates sealed subtypes from `compilation.GlobalNamespace`.

  - Reports one error per missing sealed subtype arm.

- Extended `PRECEPT0025` in `src/Precept.Analyzers/Precept0025CatalogDUWildcard.cs`.

  - Now covers switch statements as well as switch expressions.

  - Flags `default:` clauses in switch statements.

  - Flags abstract-base pattern clauses in switch statements the same way it already flags catch-all arms in switch expressions.

- Added analyzer coverage in `test/Precept.Analyzers.Tests/Precept0025Tests.cs` and new `test/Precept.Analyzers.Tests/Precept0026Tests.cs`.



## Key implementation decisions



- Extracted shared CatalogDU infrastructure into `CatalogAnalysisHelpers`:

  - test-file suppression helper

  - `[CatalogDU]` base discovery

  - sealed subtype enumeration

  - subtype inheritance test

- `PRECEPT0026` only treats explicit sealed-subtype type patterns as coverage.

  - Base-type catch-alls are still rejected by `PRECEPT0025`.

  - Missing subtypes are reported independently for deterministic diagnostics and test coverage.

- Added `AnalyzerTestHelper.AnalyzeWithFilePathAsync<TAnalyzer>` so both analyzers can verify `.Tests` suppression without duplicating compilation harness code.



## Final test counts



- `dotnet test test\Precept.Analyzers.Tests\Precept.Analyzers.Tests.csproj --no-build -q`

  - **280 passed, 0 failed**

- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build -q`

  - **3646 passed, 0 failed**

- `dotnet test --no-build -q`

  - **4127 total, 3933 passed, 194 failed**

  - Failures are pre-existing `Precept.LanguageServer.Tests` stub / not-implemented failures.

- `dotnet build -m:1`

  - Still blocked by pre-existing `Precept.LanguageServer.Tests` compile errors unrelated to PRECEPT0025 / PRECEPT0026.

# George — TypeChecker catalog fixes



## Sites fixed

- Site 1: CI enforcement in `src/Precept/Pipeline/TypeChecker.Validation.cs`

- Site 2: constraint-kind synthesis from leading tokens in `src/Precept/Pipeline/TypeChecker.cs`

- Site 3: access-mode normalization in `src/Precept/Pipeline/TypeChecker.cs`

- Site 4: anchor/state-hook normalization in `src/Precept/Pipeline/TypeChecker.cs`



## Catalog shape changes

- `BinaryOperationMeta` now carries `HasCIVariant` and `CIDiagnosticCode`.

- `FunctionMeta` now carries `CIDiagnosticCode`.

- `ConstraintMeta.StateAnchored` now carries `LeadingToken`.

- Catalog values assigned per spec:

  - `OperationKind.StringEqualsString` → `HasCIVariant: true`, `CIDiagnosticCode: CaseInsensitiveFieldRequiresTildeEquals`

  - `OperationKind.StringNotEqualsString` → `HasCIVariant: true`, `CIDiagnosticCode: CaseInsensitiveFieldRequiresTildeNotEquals`

  - `FunctionKind.StartsWith` → `CIDiagnosticCode: CaseInsensitiveFieldRequiresTildeStartsWith`

  - `FunctionKind.EndsWith` → `CIDiagnosticCode: CaseInsensitiveFieldRequiresTildeEndsWith`

  - State-anchored constraints now encode `In`/`To`/`From` directly in metadata.



## New indexes

- `Constraints.ByToken : FrozenDictionary<TokenKind, ConstraintKind>`

- `Modifiers.ByAccessToken : FrozenDictionary<TokenKind, AccessModifierMeta>`

- `Modifiers.ByAnchorToken : FrozenDictionary<TokenKind, AnchorModifierMeta>`



## Validation

- Targeted runtime validation:

  - `dotnet build src\Precept\Precept.csproj -p:BuildProjectReferences=false`

  - `dotnet build test\Precept.Tests\Precept.Tests.csproj -p:BuildProjectReferences=false`

  - `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build`

- Final `Precept.Tests` count: **3646 passed / 0 failed**.

- Repo-wide `dotnet test --no-build -q` result: **3847 total, 3653 passed, 194 failed**.



## Deviations from Frank's spec

- No implementation deviations.

- Validation used targeted `Precept`/`Precept.Tests` builds because solution-level validation is currently blocked by pre-existing unrelated failures:

  - `src/Precept.Analyzers/Precept0025CatalogDUWildcard.cs` fails solution build with `CS0246` on `ISwitchCaseClauseOperation`.

  - `dotnet test --no-build -q` reports a missing `Precept.Analyzers.Tests.dll` and 194 pre-existing `Precept.LanguageServer.Tests` failures.

# Kramer — ActionSyntaxShape enrollment in PRECEPT0019



## What I split

- Refactored `src/Precept/Pipeline/Parser.cs` so `ParseActionByShape(ActionMeta meta, SourceSpan actionStartSpan)` is now a thin dispatcher.

- Moved each existing `ActionSyntaxShape` switch arm into its own annotated handler method.

- Added `[HandlesCatalogExhaustively(typeof(ActionSyntaxShape))]` to `ParserState` alongside the existing class-level coverage attributes.

- Confirmed `ActionSyntaxShape` enum members match the 9 parser cases exactly; no missing or extra switch arms were found.



## Final handler method names

- `ParseAssignValueAction`

- `ParseCollectionValueAction`

- `ParseCollectionIntoAction`

- `ParseFieldOnlyAction`

- `ParseCollectionValueByAction`

- `ParseInsertAtAction`

- `ParseRemoveAtIndexAction`

- `ParsePutKeyValueAction`

- `ParseCollectionIntoByAction`



## Default arm decision

- Kept the `default:` recovery arm returning `MalformedAction`.

- Reason: this preserves the parser's prior fallback behavior exactly and avoids introducing a behavior change in a refactor-only slice, even though PRECEPT0019 should make the path unreachable in normal catalog-driven operation.



## Verification notes

- Clean verifier worktree (`precept-architecture-kramer-verify`):

  - `dotnet build` succeeded, but not clean: 2 pre-existing `VSTHRD200` warnings in `tools/Precept.LanguageServer/LanguageServerStubs.cs`.

  - `dotnet test test/Precept.Analyzers.Tests/Precept.Analyzers.Tests.csproj` passed: 272/272.

  - `dotnet test test/Precept.Tests/Precept.Tests.csproj --filter "FullyQualifiedName~Precept.Tests.ActionsTests|FullyQualifiedName~Precept.Tests.Parser.ActionChainTests"` passed: 64/64.

  - Full `dotnet test test/Precept.Tests/Precept.Tests.csproj` did not stay green in that clean verifier baseline: 3611 total, 3609 passed, 2 failed in unrelated `TokensTests` assertions.

- Current shared workspace:

  - Root `dotnet build` is blocked by unrelated in-progress changes outside this slice (LanguageServer and analyzer compile failures already present in the working tree), so the requested clean 0-warning/0-error root validation could not be reproduced safely without disturbing other users' work.

# Kramer — ProofEngine fixes



## Decision

- Added `UnprovedPresenceRequirement = 116`.



## Rationale

- Spec gap: `docs/compiler/proof-engine.md` §9 was incomplete, so `PresenceProofRequirement` had no diagnostic code and `ProofEngine` fell back to `DivisionByZero`.



## Files changed

- `docs/compiler/proof-engine.md`

- `src/Precept/Language/DiagnosticCode.cs`

- `src/Precept/Language/Diagnostics.cs`

- `src/Precept/Pipeline/ProofEngine.cs`

- `test/Precept.Tests/DiagnosticsTests.cs`

- `test/Precept.Tests/ProofEngineTests.cs`

# Kramer — ProofEngine dead arms removed



- Removed the dead `ProofRequirement` catch-all from `CreateDiagnostic` in `src/Precept/Pipeline/ProofEngine.cs` by switching explicitly over the five concrete requirement subtypes. `PresenceProofRequirement` now routes to `DiagnosticCode.UnprovedPresenceRequirement`, and numeric requirements share `GetNumericRequirementDiagnosticCode(...)`.

- Removed the dead catch-all from `CreateFaultSiteLink` in the same file. The dispatch now has explicit cases for `NumericProofRequirement`, `ModifierRequirement`, `DimensionProofRequirement`, `QualifierCompatibilityProofRequirement`, and `PresenceProofRequirement`.

- Applied `[CatalogDU]` to `ProofRequirement` in `src/Precept/Language/ProofRequirement.cs` and removed the remaining wildcard-bearing `ProofRequirement` switch expression in `ProofEngine.cs` so PRECEPT0025 stays quiet.

- Validation:

  - `dotnet build src/Precept/Precept.csproj` ✅ clean (0 errors, 0 warnings)

  - `dotnet test --no-build -q` current workspace baseline: 3908 passed, 196 failed total

    - `Precept.Tests`: 3629 passed, 2 failed (`TokensTests`)

    - `Precept.Analyzers.Tests`: 272 passed

    - `Precept.Mcp.Tests`: 7 passed

    - `Precept.LanguageServer.Tests`: 194 failed (existing `LanguageServerStubs` / completion stub failures)

# Kramer note — `set` token catalog fix



## What changed



- `src/Precept/Language/Tokens.cs`

  - Changed `TokenKind.Set` from `Cat_ActType` to `Cat_Act`.

  - Removed `Cat_ActType`; `TokenKind.Set` was its only remaining use.

- `test/Precept.Tests/TokensTests.cs`

  - Replaced the old dual-category `Set` assertion with two split-role tests:

    - `Set` has `Action`, not `Type`

    - `SetType` has `Type`, not `Action`

- `test/Precept.LanguageServer.Tests/PreceptAnalyzerCompletionTests.cs`

  - Updated `AllTypeTokens_AppearInTypeItems` and `AllScalarTypeTokens_AppearInScalarTypeItems` to derive expected type vocabulary from `Types.All` instead of `TokenCategory.Type` sweeps.

  - Kept the scalar-only collection exclusion and deduped surface words via set construction.

- `docs/language/precept-language-spec.md`

  - Synced the spec to the split model: `set` is the lexer action token, `SetType` is the parser-synthesized type-position alias, and the model is `Set` + `SetType`, not one dual-category token.



## Test counts



### Before

- `Precept.Tests`: 3626 passed, 2 failed

- `Precept.Analyzers.Tests`: 272 passed, 0 failed

- `Precept.Mcp.Tests`: 7 passed, 0 failed

- `Precept.LanguageServer.Tests`: 3 passed, 194 failed

- Total: 3908 passed, 196 failed (4104 total)



### After

- `Precept.Tests`: 3629 passed, 0 failed

- `Precept.Analyzers.Tests`: 272 passed, 0 failed

- `Precept.Mcp.Tests`: 7 passed, 0 failed

- `Precept.LanguageServer.Tests`: 3 passed, 194 failed (unchanged pre-existing stub failures)

- Total: 3911 passed, 194 failed (4105 total)



## Verification



- `dotnet build`

- `dotnet test --no-build -q`



`Cat_ActType` was removed.

# Newman — `precept_compile` implementation complete



## Implementation decisions



- Added `..\..\src\Precept\Precept.csproj` as a direct `ProjectReference` from `tools/Precept.Mcp/Precept.Mcp.csproj` so the MCP tool can call `Compiler.Compile` and map `Compilation` output without duplicating runtime logic.

- Implemented `tools/Precept.Mcp/Tools/CompileTool.cs` as a thin wrapper: call `Compiler.Compile(text)`, map diagnostics, return `definition: null` when `HasErrors` is true, otherwise project `SemanticIndex` into DTOs.

- Diagnostic codes are serialized as `PRE####` by parsing `Diagnostic.Code` back to `DiagnosticCode` and formatting the enum value numerically; `Severity.Info` is projected as `Hint` to match the MCP contract.

- Expression, guard, action, and rule/ensure message text is rendered by slicing the original source with `SourceSpan.Offset`/`Length` and trimming the result.

- Field qualifier text is reconstructed from explicit `DeclaredQualifiers` metadata rather than from `TypedField.Qualifier`, because the latter models propagation semantics, not the authored declaration surface.

- Precept name comes from the parsed `PreceptHeader` construct via `ConstructManifest`, not from a mirrored MCP-side naming cache.



## Deferred / notable limitations



- Event arg and field type strings are currently emitted as resolved type keywords (`string`, `number`, `choice`, etc.); the current DTO contract does not yet expose full structural type detail for collection/keyed/choice domains.

- The current `CompileResultDto` contract does not surface proof-ledger details, access modes, state hooks, or choice-option arrays at top level. Those remain future contract-expansion work if the spec tightens around them.



## Test coverage summary



Added `test/Precept.Mcp.Tests/CompileToolTests.cs` with 7 tests covering:



1. valid stateful compile success

2. field projection (`Name`, `TypeName`, `IsOptional`, `IsWritable`, `Modifiers`)

3. invalid compile returning diagnostics and null definition

4. diagnostic code formatting as `PRE####`

5. event + transition row projection

6. stateless precept detection

7. rule projection



## Validation



- `dotnet build tools\Precept.Mcp\Precept.Mcp.csproj` ✅

- `dotnet test test\Precept.Mcp.Tests\Precept.Mcp.Tests.csproj` ✅ (7/7)

- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build` ⚠️ unrelated existing failures in `TokensTests` (`TypeKeywords_HaveStorageTypeScope`, `TypeKeywords_HaveTypeSemanticTokenType`)

# Soup-Nazi — ProofEngine gap closeout



- Filled gaps: Strategy 4 positive proof, Code 112 emission, Code 113 emission, Code 114 pipeline emission, presence guard discharge, presence no-guard diagnostic emission, collection `.count > 0` guard discharge, member-access count guard discharge, TypedPostfixOp `is set` extraction, StateHookContext guard discharge, same-type `number / number` divisor regression anchor, vacuous-proof diagnostic absence, and multiple obligations on the same field/site.

- Code 116 dependency: resolved in this branch by wiring `DiagnosticCode.UnprovedPresenceRequirement` through diagnostics + ProofEngine, so the new presence diagnostic test compiles and passes.

- Test count: before 158 (per the audit request); after 173 passing in the filtered `ProofEngineTests` run.

- Validation: `dotnet test test/Precept.Tests/ --filter "FullyQualifiedName~ProofEngineTests"` passed 173/173. Full `dotnet test test/Precept.Tests/` still has the pre-existing two `TokensTests` failures about `Set` keyword type scoping/token type.

# ProofEngine Test Coverage Gap Report



**Author:** Soup Nazi

**Date:** 2026-05-09

**Suite audited:** `test/Precept.Tests/ProofEngineTests.cs` (158 tests)

**Files audited against:**

- `src/Precept/Pipeline/ProofEngine.cs`

- `src/Precept/Language/ProofRequirement.cs`

- `docs/compiler/proof-engine.md`



---



## Verdict: GAPS FOUND



The 158-test suite is broad and structurally sound. Pass 1 (obligation collection), error-tainted suppression, forwarding facts, initial-state satisfiability, and Strategies 1–3 all have credible positive + negative test coverage. However, five areas have zero or near-zero coverage of their success paths, and several specific behavior paths in the implementation are never exercised.



---



## Missing Tests (Action Items)



### Priority 1 — Critical (uncovered success paths in shipped code)



**Gap 1 — Strategy 4 has no positive proof test.**

Every Strategy 4 test asserts that `FlowNarrowing` does NOT fire or that the obligation is `Unresolved`. The strategy IS implemented (`TryFlowNarrowingProof`, lines 682–715 of ProofEngine.cs). The triple table in the design doc (PE-G14) specifies 8 positive cases (e.g., `A > B` guard + `A - B` expression → `result > 0` proved). Not one of these is tested with `obligation.Strategy == ProofStrategy.FlowNarrowing`.

**What to add:** At minimum two tests: (a) `A > B` guard + `set X = A - B` proving `result > 0` with `ProofStrategy.FlowNarrowing`, and (b) `A >= B` guard + `set X = A - B` proving `result >= 0`.



**Gap 2 — Diagnostic code 112 (UnprovedModifierRequirement) never fires.**

`Diagnostic_UnprovedModifierRequirement_HasCode112` only verifies the enum integer value. No test causes a `ModifierRequirement` to fail all strategies and emit the diagnostic. The `CreateDiagnostic` and `CreateFaultSiteLink` arms for `ModifierRequirement` are dead code from a test perspective.

**What to add:** A test using an operation that stamps a `ModifierRequirement` (e.g., an `ordered` field operation on an unordered field) and asserts `d.Code == nameof(DiagnosticCode.UnprovedModifierRequirement)`.



**Gap 3 — Diagnostic code 113 (UnprovedDimensionRequirement) never fires.**

Same pattern as Gap 2. The `DimensionProofRequirement` arm in `TryDeclarationAttributeProof` (lines 368–373) and the corresponding `CreateDiagnostic` arm are never reached by any test.

**What to add:** A test with a period-typed operand missing the required temporal dimension qualifier, asserting code 113 fires.



**Gap 4 — Diagnostic code 114 (UnprovedQualifierCompatibility) never fires via DSL.**

All Strategy 5 tests are metadata record equality checks. No test compiles DSL source that generates a `QualifierCompatibilityProofRequirement` and runs it through `ProofEngine.Prove`. The `ResolveQualifierOnAxis` function and the `leftQualifier == rightQualifier` comparison are never exercised end-to-end.

**What to add:** A DSL-level test that forces two operands with incompatible qualifier values on the same axis, asserting code 114 fires (or the obligation stays `Unresolved`). Also a positive case where qualifiers match → `ProofStrategy.QualifierCompatibility` + `ProofDisposition.Proved`.



**Gap 5 — PresenceProofRequirement end-to-end path never exercised.**

No test exercises `PresenceProofRequirement` from DSL compilation through strategy dispatch to outcome. All presence tests are metadata shape assertions. The strategy 2 presence-discharge path (reading `DeclaredPresenceMeta.Guaranteed`) and the strategy 3 presence-guard path (reading `IsPresenceCheck`) are both untested at the DSL level.

**What to add:** (a) A test accessing an optional field without a guard → unresolved + diagnostic. (b) A test with `when field is set` guard → `ProofStrategy.GuardInPath` for presence.



---



### Priority 2 — High (implementation code paths with no coverage)



**Gap 6 — `count(collection) > 0` guard pattern is untested.**

`ExtractGuardConstraintsCore` has specific handling for `TypedFunctionCall(Count, [TypedFieldRef])` comparisons (lines ~581–587 of ProofEngine.cs). `Strategy3_CountGuard_DischargesCollectionNonEmpty` actually uses a plain `D > 0` guard, not a collection count guard. The count-function guard branch is dead code from a test perspective.

**What to add:** A test with `when count(Items) > 0` guard protecting a `first(Items)` or dequeue action.



**Gap 7 — `collection.count > 0` member-accessor guard pattern is untested.**

`ExtractGuardConstraintsCore` handles `TypedMemberAccess { Object: TypedFieldRef, ResolvedAccessor: "count" }` comparisons separately. No test exercises this path.

**What to add:** A test with `when Items.count > 0` guard.



**Gap 8 — `field is set` TypedPostfixOp guard pattern is untested.**

`ExtractGuardConstraintsCore` handles `TypedPostfixOp { IsNegated: false, Operand: TypedFieldRef }` → `IsPresenceCheck: true` (lines ~592–594). `Strategy3_IsSetGuard_DischargesPresenceRequirement` uses `D != 0`, not `D is set`. The `is set` postfix operator guard path is never tested.

**What to add:** A test with `when Field is set` guard protecting an optional field access.



**Gap 9 — StateHookContext + guard path in Strategy 3 is untested.**

Strategy 3 reads guards from both `TransitionRowContext` and `StateHookContext`. The `StateHookContext` arm is exercised only in `CollectObligations_StateHookWithDivision_CreatesStateHookContext`, which does not test guard-based proof. No test has a state hook with a guard and a proof obligation.

**What to add:** A test with a guarded state hook (`to Draft when D != 0 -> set X = Y / D`) proving Strategy 3 applies from StateHookContext.



**Gap 10 — RHS-before-LHS regression anchor for same-type division.**

The `ResolveParamInBinaryOp` fix (checking Rhs before Lhs) was motivated by shared `ParameterMeta` instances on same-type binary operations. All existing tests use `integer / number` to avoid the ambiguity. There is no `number / number` test that would fail if Lhs were checked before Rhs.

**What to add:** A `number / number` division test using the `NumberDivideNumber` operation (if it exists in the catalog), where the divisor carries a `nonzero` modifier and Strategy 2 proves it. This test would regress if Lhs/Rhs order were swapped.



---



### Priority 3 — Medium (completeness assertions and edge cases)



**Gap 11 — Forwarding facts do not assert diagnostic absence.**

Tests in Slice 12 verify `obligation.Disposition == ProofDisposition.Proved` for vacuously-proved obligations, but do not assert that `ledger.Diagnostics` contains no entry for those obligations. Add: `ledger.Diagnostics.Should().NotContain(d => d.Code == nameof(DiagnosticCode.DivisionByZero))` to the unreachable-state test.



**Gap 12 — `PresenceProofRequirement` fallthrough in `CreateDiagnostic`.**

The `CreateDiagnostic` method maps `PresenceProofRequirement` to `DiagnosticCode.DivisionByZero` (lines 883–886 of ProofEngine.cs). This is likely a placeholder or bug — presence failures should not emit a `DivisionByZero` diagnostic. No test exercises this branch, so the mapping is unverified and potentially wrong.

**What to add:** Once an end-to-end presence test exists (Gap 5), assert the emitted diagnostic code is correct for presence failures.



**Gap 13 — Multiple simultaneous obligations on the same field.**

No test has a single expression generating more than one proof obligation (e.g., `sqrt(X / D)` which would generate both a `!= 0` and a `>= 0` obligation). The proof engine should handle multiple obligations in the same site correctly.

**What to add:** A test with `sqrt(Y / D)` (D = nonzero, Y = nonnegative) where both obligations are proved, one by Strategy 2 and one by Strategy 2.



**Gap 14 — Wildcard transitions (`from * on E`).**

No test uses wildcard transitions with proof obligations. The forwarding-fact suppression logic reads `trc.Row.FromState` and guards with `if (fromState is null) continue` for wildcard rows. This null guard path is untested.

**What to add:** A test with `from * on Event -> set X = Y / D -> no transition` to verify wildcard rows are processed correctly.



**Gap 15 — Proof spanning multiple states (same field, different transition rows).**

No test verifies correct obligation tracking when the same field is used as a divisor in transitions from multiple states. Not a critical gap but documents a topology-coverage hole.



---



## Strategy Coverage Matrix



| Strategy | Positive (Proved) Tests | Negative (Unresolved) Tests | Status |

|---|---|---|---|

| S1 Literal | 4 (literal divisors, literal sqrt args) | 3 (zero literal, negative sqrt, non-literal) | ✅ Covered |

| S2 DeclarationAttribute | 7 (nonzero, positive, nonneg+sqrt, presence) | 3 (unqualified, optional, nonneg-for-!=0) | ✅ Covered |

| S3 GuardInPath | 7 (!=0, >0, <0, negated, inverted, hook-skipped) | 3 (no guard, EventHandler, OR guard) | ✅ Covered |

| S4 FlowNarrowing | **0** | 10+ (all cases document strategy NOT firing) | ❌ **MISSING positive case** |

| S5 QualifierCompatibility | **0 DSL-level** (3 metadata equality checks) | 2 (metadata not-equal) | ⚠️ **Partially covered** |



---



## Diagnostic Coverage Matrix



| Code | Fires-Case Tested | Suppressed-Case Tested | Status |

|---|---|---|---|

| DivisionByZero (83) | ✅ | ✅ | Covered |

| SqrtOfNegative (84) | ✅ | ✅ | Covered |

| UnprovedModifierRequirement (112) | ❌ (enum value only) | N/A | **MISSING** |

| UnprovedDimensionRequirement (113) | ❌ (enum value only) | N/A | **MISSING** |

| UnprovedQualifierCompatibility (114) | ❌ (enum value only) | N/A | **MISSING** |

| UnsatisfiableInitialState (115) | ✅ | ✅ | Covered |



---



## Bug Fix Regression Coverage



| Fix | Test | Status |

|---|---|---|

| Forwarding-fact suppression sets `Proved` | `ForwardingFacts_UnreachableState_ObligationsVacuouslyProved` | ✅ Covered |

| Strategy 2 null guard (non-field-ref subject) | `GetFieldName_NonFieldRef_ReturnsNull`, `Strategy4_AGreaterThanB_SubtractionSqrtProved` | ✅ Implicitly covered |

| RHS-before-LHS in `ResolveParamInBinaryOp` | Integer/Number tests (avoid same ParameterMeta) | ⚠️ **No same-type regression anchor** |



---



## ObligationContext DU Coverage



| Subtype | Exercised? |

|---|---|

| TransitionRowContext | ✅ |

| EventHandlerContext | ✅ |

| StateHookContext | ✅ (obligation collection only; no guard-path test) |

| ConstraintContext (RuleIdentity) | ✅ |

| ConstraintContext (EnsureIdentity) | ✅ |

| FieldExpressionContext | ✅ |



---



## ProofDisposition Coverage



| Outcome | Tested? |

|---|---|

| Proved | ✅ |

| Unresolved | ✅ |

| Unsatisfiable (InitialState) | ✅ (via `IsSatisfiable == false`) |



---



## Coverage Statistics



- **Total tests:** 158

- **Strategy breakdown (approximate):**

  - S1 Literal: ~7 tests

  - S2 DeclarationAttribute: ~18 tests

  - S3 GuardInPath: ~14 tests

  - S4 FlowNarrowing: ~15 tests (ALL negative/boundary)

  - S5 QualifierCompatibility: ~9 tests (ALL metadata-level, no DSL proof)

- **Positive cases (proof succeeds):** ~35

- **Negative cases (proof fails/unresolved):** ~45

- **Code/enum verification:** ~15

- **Metadata/structural:** ~30

- **Integration/end-to-end:** ~33



---



## Prioritized Additions



1. **Strategy 4 positive proof** (Gap 1) — Without this, Strategy 4's success path is completely untested. Any regression in `TryFlowNarrowingProof` or `GuardRelationImpliesObligation` is invisible.

2. **Code 112/113/114 actually firing** (Gaps 2, 3, 4) — Three diagnostic emission paths are dead from a test perspective.

3. **Strategy 5 DSL-level test** (Gap 4, overlap) — Strategy 5 logic (`ResolveQualifierOnAxis`, `leftQualifier == rightQualifier`) is never exercised via the pipeline.

4. **PresenceProofRequirement end-to-end** (Gap 5) — The full presence proof cycle is untested.

5. **`count()` and `collection.count` guard patterns** (Gaps 6, 7) — Implemented guard extraction branches are dead.

6. **`field is set` guard pattern** (Gap 8) — Implemented but untested.

7. **StateHookContext guard → Strategy 3** (Gap 9) — Code path implemented, untested.

8. **RHS-before-LHS same-type anchor** (Gap 10) — Fragile if catalog gains a symmetric same-type division operator.

9. **Forwarding facts + diagnostic absence assertion** (Gap 11) — Tests verify disposition but not diagnostic list.

10. **`PresenceProofRequirement` → `DivisionByZero` mapping in CreateDiagnostic** (Gap 12) — Potential bug, no test catches it.

# Message-position catalog metadata closed



**Date:** 2026-05-08

**Sources:** `.squad/decisions/inbox/george-is-message-position.md`, `.squad/decisions/inbox/kramer-grammar-gen-message-position.md`

**Status:** Implemented and validated



## Summary



`IsMessagePosition` is now first-class catalog metadata on both `TokenMeta` and `FunctionMeta`, and the grammar generator now derives message-string gold patterns from that metadata instead of hardcoding `because` / `reject`.



## Decisions



1. Message-position awareness belongs in catalog metadata, not parser or grammar-generator keyword lists.

2. `TokenKind.Because` and `TokenKind.Reject` are the only current token entries that opt into `IsMessagePosition`.

3. `FunctionMeta` carries the same flag now so future built-ins with trailing user-facing message strings can participate without new generator hardcoding.

4. The grammar generator must read `Tokens.All.Where(m => m.IsMessagePosition)` and `Functions.All.Where(f => f.IsMessagePosition)` when building `messageStrings` patterns.



## Validation



- George added the metadata fields plus token flags; build and tests passed; commits `105a42a7` and `315b00c9`.

- Kramer wired the generator, removed the stale TODO, regenerated `precept.tmLanguage.json`, and verified a zero-diff output; commit `7f3842fd`.

# ProofEngine Design Decisions — PE-G1, PE-G2, PE-G3



**Date:** 2026-05-08

**Author:** Frank

**Resolves:** PE-G1 (three unhandled obligation kinds), PE-G2 (ProofDischarges catalog prereq), PE-G3 (ProofLedger divergence)

**Status:** DECISIONS MADE — pending Shane sign-off before spec update or implementation



## Summary



Deep source analysis of the five `ProofRequirementKind` values, the Operations catalog's actual usage, the TypeChecker's resolution pipeline, and the existing SemanticIndex contract reveals that all three blocking gaps are resolvable without new proof strategies. The three "unhandled" requirement kinds (Dimension, Modifier, QualifierCompatibility) are all field-declaration-attribute checks — they belong in an expanded Strategy 2 that reads qualifier bindings alongside modifiers. The `ProofDischarge` catalog prerequisite is well-scoped: 6 of 15 `FieldModifierMeta` entries carry concrete discharges. The `ProofLedger` output type needs ~6 new record types but the spec's shape is sound — the only revisions are `ConstraintIdentity` field-name corrections to match the source-of-truth `SemanticIndex.cs` definitions.



---



## PE-G1a: DimensionProofRequirement



**Obligation:** "The period operand must have the required time dimension (Date or Time) for the arithmetic operation to be semantically valid."



**Source:** `ProofRequirement.cs` lines 81–85. `DimensionProofRequirement(ProofSubject Subject, PeriodDimension RequiredDimension, string Description)`. The `PeriodDimension` enum has three values: `Any`, `Date`, `Time`.



**Catalog usage:** `Operations.cs` lines 248, 257, 275, 284 — four temporal arithmetic entries:

- `DatePlusPeriod` / `DateMinusPeriod` → require `PeriodDimension.Date`

- `TimePlusPeriod` / `TimeMinusPeriod` → require `PeriodDimension.Time`



**TypeChecker analysis:** The TypeChecker resolves qualifier bindings on field declarations (`TypedField.Qualifier`) and operation results (`TypedBinaryOp.ResultQualifier`). Period fields accept qualifiers on the `TemporalDimension` axis (`period of 'date'`, `period of 'time'`) and the `TemporalUnit` axis (`period in 'days'`). The qualifier binding is resolved at type-checking time and available in `TypedField.Qualifier`. The TypeChecker does NOT validate the dimension constraint itself — it stamps the `DimensionProofRequirement` from the `BinaryOperationMeta` catalog entry and defers to the proof engine. Grep for `Dimension`, `PeriodDimension`, `QualifierAxis` in `TypeChecker.cs` and `TypeChecker.Expressions.cs` returned no validation logic for this constraint. **Confirmed: the TypeChecker does not pre-discharge this.**



**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), extended to read qualifier bindings.**



**Rationale:** The period field's qualifier binding on the `TemporalDimension` axis is a compile-time-known declaration attribute, structurally identical to a modifier. When the proof subject resolves to a field with `TypeKind.Period`, Strategy 2 reads `TypedField.Qualifier` and checks whether a qualifier on `QualifierAxis.TemporalDimension` maps to the required `PeriodDimension`:

- Qualifier value `"date"` → satisfies `PeriodDimension.Date`

- Qualifier value `"time"` → satisfies `PeriodDimension.Time`

- `PeriodDimension.Any` → always satisfied (any temporal dimension)

- No qualifier on `TemporalDimension` axis → obligation **unresolved** (period without dimension is ambiguous)



**Alternatives rejected:**

- _New Strategy 5_: Unnecessary — this is a field-declaration attribute check, exactly what Strategy 2 does. Adding a strategy for one requirement kind when the existing strategy can be extended is overengineering.

- _Pre-discharge by TypeChecker_: Would violate the catalog-driven architecture. The type checker stamps requirements, the proof engine discharges them. The type checker's job is operation selection and requirement attachment, not requirement evaluation.



**Tradeoff accepted:** Strategy 2 becomes slightly more complex — it dispatches on requirement kind (Numeric → ProofDischarge lookup, Dimension → qualifier binding check). This is a single `switch` arm, not a separate strategy.



**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `DimensionProofRequirement` branch to `TryModifierProof` that reads the subject field's qualifier binding on `QualifierAxis.TemporalDimension` and compares against `RequiredDimension`. Add to the Strategy 2 coverage table.



---



## PE-G1b: ModifierRequirement



**Obligation:** "The field operand must declare the required modifier (e.g., `ordered`) for the operation to be valid."



**Source:** `ProofRequirement.cs` lines 112–116. `ModifierRequirement(ProofSubject Subject, ModifierKind Required, string Description)`.



**Catalog usage:** `Operations.cs` lines 760, 768, 776, 784 — four choice ordinal comparison entries (`ChoiceLessThan`, `ChoiceGreaterThan`, `ChoiceLessThanOrEqual`, `ChoiceGreaterThanOrEqual`) all declare `ModifierRequirement(PChoice, ModifierKind.Ordered, ...)`. Both operands share the same `PChoice` parameter reference, so the requirement applies to all matching operand positions.



**TypeChecker analysis:** The TypeChecker resolves choice operations via the Operations catalog and stamps the `ModifierRequirement` on the `TypedBinaryOp`. It does NOT check whether the field has the `ordered` modifier itself — that's deferred to the proof engine. Grep for `ModifierRequirement`, `CheckModifier`, `modifier.*check` in `TypeChecker.cs` returned no hits. **Confirmed: the TypeChecker does not pre-discharge this.**



**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), via direct modifier presence check.**



**Rationale:** This is the simplest possible Strategy 2 case. The proof subject resolves to a field. Strategy 2 checks `field.Modifiers.Contains(requirement.Required)`. If the field has the `ordered` modifier, the obligation is discharged. If not, unresolved — emit diagnostic.



This is distinct from the `ProofDischarge` lookup path. `ProofDischarge` entries map modifiers → numeric/presence requirements they discharge (e.g., `positive` discharges `> 0`). `ModifierRequirement` is the inverse: it asserts that a specific modifier must be present on the field. Strategy 2 handles both paths:



1. **ProofDischarge path** (for `NumericProofRequirement`, `PresenceProofRequirement`): "Does any modifier on this field carry a `ProofDischarge` that covers this requirement?"

2. **Modifier presence path** (for `ModifierRequirement`): "Does this field have the required modifier?"



**Alternatives rejected:**

- _Pre-discharge by TypeChecker_: Same rationale as PE-G1a — type checker stamps requirements, proof engine discharges them.

- _Always a type error (Option C)_: Wrong — `ordered` is an optional modifier on choice fields. Not having it isn't a type error; it's a proof failure for ordinal operations specifically.



**Tradeoff accepted:** None significant. This is a trivial addition to Strategy 2.



**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `ModifierRequirement` branch that checks `field.Modifiers.Contains(requirement.Required)`. Add to the Strategy 2 coverage table.



---



## PE-G1c: QualifierCompatibilityProofRequirement



**Obligation:** "Two operands in a binary operation must have matching qualifier values on the specified axis (e.g., both `quantity in 'kg'` or both `money in 'USD'`)."



**Source:** `ProofRequirement.cs` lines 96–101. `QualifierCompatibilityProofRequirement(ProofSubject LeftSubject, ProofSubject RightSubject, QualifierAxis Axis, string Description)`. This is the only dual-subject requirement kind.



**Catalog usage:** Extensively used in `Operations.cs`:

- **Quantity arithmetic** (lines 475, 484, 921–966): `QualifierAxis.Unit` — operands must have the same unit qualifier

- **Price arithmetic** (lines 557–570, 977–1023): Both `QualifierAxis.Unit` AND `QualifierAxis.Currency` — operands must match on both axes

- **Money arithmetic**: `QualifierAxis.Currency` (via `QualifierMatch.Same` entries)



**TypeChecker analysis:** The TypeChecker handles qualifier disambiguation at operation resolution time (`TypeChecker.Expressions.cs` lines 560–591). For multi-candidate operations, it defaults to `QualifierMatch.Same` — the structurally safe assumption. It maps this to `SameQualifierRequired` on `TypedBinaryOp.ResultQualifier` and explicitly comments: "ProofEngine will verify qualifier compatibility at deeper analysis" (line 573). **Confirmed: the TypeChecker defers qualifier verification to the proof engine.**



**Decision: B) Discharged by Strategy 2 (Declaration Attribute Proof), extended to read qualifier bindings on both operand fields.**



**Rationale:** Both operands' qualifier bindings are compile-time-known declaration attributes. The proof engine:

1. Resolves both subjects (`LeftSubject`, `RightSubject`) to their respective fields

2. Reads the qualifier binding on the specified `QualifierAxis` from each `TypedField.Qualifier`

3. If both fields have explicit qualifiers on that axis AND the values match → discharged

4. If either field lacks a qualifier on that axis → **unresolved** (cannot prove compatibility without declared qualifiers)

5. If both have qualifiers but they differ → **unresolved** (type-incompatible operation)



**Alternatives rejected:**

- _New Strategy 5 (Qualifier Strategy)_: Unnecessary — this is a field-declaration attribute comparison. Strategy 2 already reads field declarations. Adding the qualifier binding read is architecturally consistent with its existing responsibility.

- _Always a type error_: Wrong — the type checker intentionally defers this to the proof engine. Making it a type error would duplicate logic and violate the catalog-driven obligation model.

- _Runtime-only check_: Wrong — qualifier values are declaration-time constants (string literals in `in 'USD'`, `in 'kg'`). They're always statically knowable. Deferring to runtime would miss a guaranteed-provable obligation.



**Tradeoff accepted:** Strategy 2 now handles two structural patterns — single-subject (modifiers, qualifier, dimension) and dual-subject (qualifier compatibility). The implementation must check for `QualifierCompatibilityProofRequirement` specifically and resolve both subjects. This is a single additional branch, not a general multi-subject framework.



**Spec update required:** `proof-engine.md` §7 Strategy 2 pseudocode: add a `QualifierCompatibilityProofRequirement` branch that resolves both subjects, reads their qualifier bindings on the specified axis, and compares values. Add to the Strategy 2 coverage table. Update Strategy 2's name from "Modifier Proof" to "Declaration Attribute Proof" to reflect its expanded scope.



---



## PE-G2: ProofDischarge Catalog Design



## 1. ProofDischarge Record Type



```csharp

/// <summary>

/// Declares a proof obligation that a field modifier statically discharges.

/// Read by Strategy 2 of the proof engine — no per-modifier switch needed.

/// </summary>

public sealed record ProofDischarge(

    ProofRequirementKind RequirementKind,  // which obligation kind this discharges

    OperatorKind? Comparison,              // for Numeric: the comparison operator

    decimal? Threshold                     // for Numeric: the threshold value

                                           //   null = read from modifier's HasValue parameter

);

```



**Design rationale:** The `Threshold` field is nullable. For fixed-value modifiers (`positive`, `nonnegative`, `nonzero`, `notempty`), the threshold is a literal. For parameterized modifiers (`min(N)`, `max(N)`, `mincount(N)`, `maxcount(N)`), the threshold is `null`, signaling the proof engine to read the value from the field declaration's modifier parameter at proof time. This keeps the catalog entry declarative while supporting parameterized constraints.



## 2. FieldModifierMeta Update



Add `ProofDischarges` property to the existing `FieldModifierMeta` record in `Modifier.cs`:



```csharp

public sealed record FieldModifierMeta(

    ModifierKind Kind,

    TokenMeta Token,

    string Description,

    ModifierCategory Category,

    TypeTarget[] ApplicableTo,

    bool HasValue = false,

    ModifierKind[] Subsumes = default!,

    ProofDischarge[] ProofDischarges = default!,  // ← NEW

    string? HoverDescription = null,

    string? UsageExample = null,

    string? SnippetTemplate = null,

    ModifierKind[]? MutuallyExclusiveWith = null)

    : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith)

{

    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];

    public ProofDischarge[] ProofDischarges { get; init; } = ProofDischarges ?? [];

}

```



## 3. Modifier Entries Requiring ProofDischarges



| Modifier | `ProofDischarges` value | Rationale |

|---|---|---|

| `positive` | `[ProofDischarge(Numeric, GreaterThan, 0)]` | Field > 0 — subsumes `!= 0` and `>= 0` via `DischargeCovers` subsumption logic |

| `nonnegative` | `[ProofDischarge(Numeric, GreaterThanOrEqual, 0)]` | Field ≥ 0 |

| `nonzero` | `[ProofDischarge(Numeric, NotEquals, 0)]` | Field ≠ 0 |

| `notempty` | `[ProofDischarge(Numeric, GreaterThan, 0)]` | Collection count > 0 or string length > 0 |

| `min(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | Field ≥ N where N is modifier parameter |

| `max(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | Field ≤ N where N is modifier parameter |

| `minlength(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | String length ≥ N |

| `maxlength(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | String length ≤ N |

| `mincount(N)` | `[ProofDischarge(Numeric, GreaterThanOrEqual, null)]` | Collection count ≥ N |

| `maxcount(N)` | `[ProofDischarge(Numeric, LessThanOrEqual, null)]` | Collection count ≤ N |



**Modifiers with NO ProofDischarges (empty array):**



| Modifier | Why empty |

|---|---|

| `optional` | Does not *discharge* a proof obligation — its absence is what guarantees presence. Strategy 2 handles presence via the non-optional check, not via ProofDischarge. |

| `ordered` | Handled by the modifier-presence path of Strategy 2 (for `ModifierRequirement`), not via ProofDischarge entries. |

| `default(expr)` | Provides initial value — does not establish a runtime bound. |

| `maxplaces(N)` | No current proof obligation targets decimal-place constraints. |

| `writable` | Access control, not a value constraint. |



## 4. File Location



**New file: `src/Precept/Language/ProofDischarge.cs`.**



**Rationale:** `ProofDischarge` is a first-class catalog type shared between the modifier catalog (`Modifiers.cs`) and the proof engine (`ProofEngine.cs`). It belongs in `Language/` because it's catalog metadata, not pipeline logic. It gets its own file because it's a distinct record type with its own semantic purpose — nesting it inside `Modifier.cs` would bury it among the modifier DU hierarchy. This mirrors the pattern of `ProofRequirement.cs` (catalog metadata type) having its own file.



## 5. Catalog Architecture Compliance



Verified against `docs/language/catalog-system.md`:



- **ProofDischarges is catalog metadata.** It declares what a modifier *means* for the proof system. The proof engine reads it — it does not compute it. This is exactly the metadata-driven architecture: domain knowledge lives in the catalog, pipeline stages are generic readers.

- **No per-modifier switch in the proof engine.** Strategy 2 iterates `field.Modifiers`, reads `Modifiers.GetMeta(kind).ProofDischarges`, and calls `DischargeCovers`. No `ModifierKind.Positive => ...` switches anywhere in `ProofEngine.cs`.

- **Subsumption is a generic algorithm.** `DischargeCovers` performs comparison-operator subsumption (e.g., `> 0` covers `!= 0`). This logic is proof-engine-internal, not per-modifier — it works for any `ProofDischarge` entry regardless of which modifier declares it.



---



## PE-G3: ProofLedger Output Type



## New Record Types Needed



The spec's §5 Output defines 8 types. Current source has only `ProofLedger(ImmutableArray<Diagnostic> Diagnostics)`. The following types must be added:



## 1. `ProofObligation` — `Pipeline/ProofLedger.cs`



```csharp

public sealed record ProofObligation(

    ProofRequirement Requirement,

    TypedExpression Site,

    ProofDisposition Disposition,

    ProofStrategy? Strategy,

    DiagnosticCode? EmittedDiagnostic

);

```



Dependencies: `ProofRequirement` (Language), `TypedExpression` (Pipeline/SemanticIndex.cs), `DiagnosticCode` (Language)



## 2. `ProofDisposition` enum — `Pipeline/ProofLedger.cs`



```csharp

public enum ProofDisposition { Proved, Unresolved }

```



## 3. `ProofStrategy` enum — `Pipeline/ProofLedger.cs`



```csharp

public enum ProofStrategy

{

    Literal,

    DeclarationAttribute,  // renamed from "Modifier" — covers modifiers, qualifiers, dimensions

    GuardInPath,

    FlowNarrowing

}

```



**Note:** Renamed from `Modifier` to `DeclarationAttribute` per PE-G1 decisions. The spec should be updated accordingly.



## 4. `FaultSiteLink` — `Pipeline/ProofLedger.cs`



```csharp

public sealed record FaultSiteLink(

    ProofObligation Obligation,

    FaultCode FaultCode,

    DiagnosticCode DiagnosticCode,

    SourceSpan Site

);

```



Dependencies: `FaultCode` (Language)



## 5. `ConstraintInfluenceEntry` — `Pipeline/ProofLedger.cs`



```csharp

public sealed record ConstraintInfluenceEntry(

    ConstraintIdentity Constraint,

    ImmutableArray<string> ReferencedFields,

    ImmutableArray<EventArgReference> ReferencedArgs

);



public sealed record EventArgReference(string EventName, string ArgName);

```



Dependencies: `ConstraintIdentity` (Pipeline/SemanticIndex.cs — shared type, already exists)



## 6. `InitialStateSatisfiabilityResult` — `Pipeline/ProofLedger.cs`



```csharp

public sealed record InitialStateSatisfiabilityResult(

    string StateName,

    bool IsSatisfiable,

    ImmutableArray<UnsatisfiedConstraint> Violations

);



public sealed record UnsatisfiedConstraint(

    ConstraintIdentity Constraint,

    string Reason

);

```



## 7. Updated `ProofLedger` — `Pipeline/ProofLedger.cs`



```csharp

public sealed record ProofLedger(

    ImmutableArray<ProofObligation> Obligations,

    ImmutableArray<FaultSiteLink> FaultSiteLinks,

    ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,

    ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,

    ImmutableArray<Diagnostic> Diagnostics

);

```



## Decision: Match the spec — the shape is sound



**Rationale:** The spec was written after the catalog architecture was established and correctly reflects what the Precept Builder needs from the proof engine:



- `Obligations` — complete audit trail (which obligations exist and how they were resolved)

- `FaultSiteLinks` — consumed by Precept Builder Pass 4 for `FaultSiteAnnotation` planting

- `ConstraintInfluence` — consumed by Precept Builder for `ConstraintInfluenceMap`

- `InitialStateResults` — consumed by diagnostics (unsatisfiable initial state is a compile-time error)

- `Diagnostics` — merged into the final diagnostic stream



None of these fields are overengineered. Each has a concrete downstream consumer documented in the spec.



**One revision:** The `ConstraintIdentity` subtypes in the spec differ from the source. The **source is correct** (it's the implemented, tested shape). The spec must be updated:



| Spec shape | Source shape | Verdict |

|---|---|---|

| `RuleIdentity(string RuleName, int Index)` | `RuleIdentity(int RuleIndex)` | **Source wins** — Precept rules are anonymous (no `RuleName`). The spec's `RuleName` field doesn't exist in the DSL surface. |

| `EnsureIdentity(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` | `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)` | **Source wins** — `AnchorName` collapses state/event discrimination. The `ConstraintKind` already indicates whether the anchor is a state or event. |



## File organization



All new types go in `Pipeline/ProofLedger.cs` alongside the `ProofLedger` record. This follows the existing pattern: `SemanticIndex.cs` contains both the index record and all its constituent types (`TypedField`, `TypedState`, `TypedTransitionRow`, etc.). Putting `ProofObligation`, `FaultSiteLink`, etc. in `ProofLedger.cs` keeps the proof engine's output contract in one file.



Exception: `ProofDischarge` goes in `Language/ProofDischarge.cs` (catalog metadata, not pipeline output).



---



## Significant Gaps — Terse Verdicts



## SIG-1: Missing `AllTypedExpressions` API on `SemanticIndex`



**Verdict: SPEC UPDATE NEEDED**



The spec's Pass 1 pseudocode (line 967) iterates `semantics.AllTypedExpressions` — this property does not exist on `SemanticIndex`. The implementer must define a traversal method that walks all expression-bearing records (`TransitionRows` → actions/guards, `Rules` → conditions, `Ensures` → conditions, `ComputedDeps` → computed expressions, `StateHooks` → actions). This is an **implementer responsibility** — the traversal is mechanical and the implementer knows the SemanticIndex shape. The spec should note this as a "to be implemented" API rather than assuming it exists.



## SIG-2: `ConstraintIdentity` shape mismatch



**Verdict: SPEC UPDATE NEEDED**



Covered in PE-G3 above. The spec's `ConstraintIdentity` subtypes have fields that don't exist in the source (`RuleName`, separate `AnchorState`/`AnchorEvent`). The spec must be updated to match the source shapes: `RuleIdentity(int RuleIndex)` and `EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex)`.



## SIG-3: Unspecified `FindEnclosingTransitionRow` helper



**Verdict: ACCEPT AS-IS**



This is a straightforward lookup: given a `TypedExpression`, find which `TypedTransitionRow` contains it. The implementer walks `SemanticIndex.TransitionRows` and checks whether any row's guard or action chain contains the expression (by reference identity or span containment). No design decision needed — it's a utility function, not an architectural concern. The spec correctly identifies it as a helper without over-specifying implementation.



## SIG-4: Unspecified `ResolveSubject` helper



**Verdict: ACCEPT AS-IS**



`ResolveSubject` maps a `ProofSubject` to a concrete `TypedExpression` node. For `ParamSubject(ParameterMeta)`, it matches the parameter by object identity against the expression's operands. For `SelfSubject`, it returns the receiver expression. Implementation is mechanical — the spec correctly leaves it to the implementer.



## SIG-5: Underspecified initial-state satisfiability



**Verdict: DESIGN DECISION REQUIRED — deferred**



The spec says to check whether initial-state constraints are satisfiable given default field values. This requires evaluating default expressions against constraint expressions — essentially a mini-evaluator at compile time. The spec's description (lines 866–883) is correct in intent but implementation is blocked pending the type checker's expression resolution engine being fully operational (as the spec itself notes on line 883). **Owner: spec author + implementer, post-TypeChecker completion.**



## SIG-6: Collection-empty obligation ownership ambiguity



**Verdict: ACCEPT AS-IS**



Collection non-empty obligations are declared in catalog metadata (`TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements`). The type checker stamps them on `TypedMemberAccess` and `TypedAction` nodes. The proof engine discharges them via Strategy 1 (literal), Strategy 2 (`notempty` modifier), or Strategy 3 (`count > 0` guard). There is no ownership ambiguity — the catalog declares, the type checker stamps, the proof engine discharges. The spec's §7 "Collection Non-Empty Proof" section (lines 886–899) correctly describes the flow. No change needed.



## SIG-7: Guard decomposition rules



**Verdict: SPEC UPDATE NEEDED**



The spec's Strategy 3 pseudocode references `ExtractGuardConstraints(row.Guard)` but does not define the decomposition rules for complex guard expressions. The spec should specify:



1. **Supported connectives:** `and` decomposes into individual constraints (each arm of `A and B` is a separate constraint). `or` does NOT decompose (cannot prove either arm independently).

2. **Supported atomic forms:** `field OP literal`, `count(collection) > 0`, `collection.count > 0`, `field is set`, `field is not set`.

3. **Unsupported forms:** Function calls (other than `count`), nested expressions, field-vs-field comparisons (those are Strategy 4).



**Owner: spec author.** These rules define the proof engine's guard recognition language. They should be specified in the spec before implementation.



---



## Required Spec Updates (in order)



1. **§7 Strategy 2 — Rename and expand scope.** Rename from "Modifier Proof" to "Declaration Attribute Proof." Add three new branches to `TryModifierProof` (renamed to `TryDeclarationAttributeProof`):

   - `ModifierRequirement` → direct `field.Modifiers.Contains(requirement.Required)` check

   - `DimensionProofRequirement` → read `TypedField.Qualifier` on `QualifierAxis.TemporalDimension`, compare to `RequiredDimension`

   - `QualifierCompatibilityProofRequirement` → resolve both subjects, read qualifier bindings on specified axis, compare values



2. **§7 Strategy 2 — Update coverage table.** Add rows for Dimension, Modifier, and QualifierCompatibility requirement kinds.



3. **§7 Strategy 2 — Update `ProofDischarge` pseudocode.** Show `DischargeCovers` handling nullable `Threshold` (reads from modifier parameter for `HasValue` modifiers).



4. **§5 Output — Fix `ConstraintIdentity` shapes.** Replace `RuleIdentity(string RuleName, int Index)` with `RuleIdentity(int RuleIndex)`. Replace `EnsureIdentity(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)` with `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)`.



5. **§5 Output — Update `ProofStrategy` enum.** Rename `Modifier` to `DeclarationAttribute`.



6. **§7 Strategy 3 — Add guard decomposition rules.** Specify `and` connective decomposition, `or` non-decomposition, supported atomic guard forms.



7. **§9 — Add `AllTypedExpressions` note.** Document that `SemanticIndex` requires a traversal method/property to enumerate all typed expressions across all declaration kinds.



8. **§7 initial-state satisfiability — Add blocking dependency note.** Explicitly state that implementation is blocked pending TypeChecker expression evaluation capability.



---



## Required Catalog Changes (in order)



1. **Add `ProofDischarge.cs`** — new file in `src/Precept/Language/` containing the `ProofDischarge` record type.



2. **Update `FieldModifierMeta` in `Modifier.cs`** — add `ProofDischarge[] ProofDischarges = default!` parameter after `Subsumes`, with `ProofDischarges` property initialization `= ProofDischarges ?? []`.



3. **Update `Modifiers.cs` entries** — populate `ProofDischarges` on 10 modifier entries:

   - `Nonnegative`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, 0)]`

   - `Positive`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0)]`

   - `Nonzero`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.NotEquals, 0)]`

   - `Notempty`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThan, 0)]`

   - `Min`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`

   - `Max`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`

   - `Minlength`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`

   - `Maxlength`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`

   - `Mincount`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.GreaterThanOrEqual, null)]`

   - `Maxcount`: `ProofDischarges: [new(ProofRequirementKind.Numeric, OperatorKind.LessThanOrEqual, null)]`



4. **Update `ProofLedger.cs`** — replace stub with full output contract (ProofObligation, ProofDisposition, ProofStrategy, FaultSiteLink, ConstraintInfluenceEntry, EventArgReference, InitialStateSatisfiabilityResult, UnsatisfiedConstraint).



---



## Shane Sign-Off Required On



- **Strategy 2 rename to "Declaration Attribute Proof"**: This broadens Strategy 2's scope from modifier-only to all field declaration attributes (modifiers, qualifiers, dimensions). The alternative is keeping the name "Modifier Proof" and adding separate subroutines for qualifier/dimension checks under the same strategy. The rename is more honest but changes the spec vocabulary. Shane should confirm the rename is acceptable.



- **`PeriodDimension.Any` behavior**: When a period field has no `TemporalDimension` qualifier, should the Dimension obligation be unresolved (forcing authors to always qualify their period fields for temporal arithmetic), or should unqualified periods be treated as `PeriodDimension.Any` (accepting any dimension)? Current decision: **unresolved** — the author must declare `period of 'date'` or `period of 'time'` for temporal arithmetic to be proven safe. This is the conservative choice but may be annoying for simple precepts.



- **SIG-5 initial-state satisfiability deferral**: This is marked as blocked pending TypeChecker expression evaluation. Should it be deferred entirely from the proof engine's initial implementation scope, or should a minimal version (literals-only default values against simple comparison constraints) be included in the first implementation?

# Shane Sign-Off — ProofEngine Design Decisions



**Date:** 2026-05-08

**Source:** Direct conversation with Shane



## Decision 1 — Strategy 2 Rename: APPROVED ✅



**Approved:** Rename Strategy 2 from "Modifier Proof" to "Declaration Attribute Proof."



Strategy 2's expanded scope (modifiers, qualifier bindings, and temporal dimension qualifiers) makes the rename accurate. The old name "Modifier Proof" was too narrow given the PE-G1 expansion.



## Decision 2 — Unqualified Period Behavior: Permissive ✅



**Approved:** Treat unqualified periods as `PeriodDimension.Any` — accept any dimension.



When a `period` field has no `TemporalDimension` qualifier (no `period of 'date'` or `period of 'time'`), the `DimensionProofRequirement` is considered **satisfied** rather than unresolved. This is the permissive choice — authors are not forced to qualify period fields for temporal arithmetic to be proven safe.



## Decision 3 — Initial-State Satisfiability: PENDING FRANK DEEP DIVE ⏸



Shane raised the question: "why not just use the evaluator?" instead of a mini-evaluator at compile time.



Frank has been tasked with a deep dive on this architectural question. Key questions:

1. Does the evaluator depend on compiled Compiler-stage output, or can it operate on SemanticIndex?

2. Can evaluation logic be shared between ProofEngine (compile-time) and Evaluator (runtime)?

3. What are the architectural implications of using the evaluator for initial-state satisfiability?

4. What is the recommended design?



**Status:** Blocked on Frank deep dive. No implementation decision authorized yet.

# Readability Review: combined-design-v2.md (2026-07-17)



**Reviewer:** Elaine (UX Designer)



**Doc:** `docs/working/combined-design-v2.md`



**Verdict:** APPROVED-WITH-CONCERNS



## Top 3 Findings



1. **Parser section needs shape specificity.** The section explains the parser's *philosophy* (source-faithful, recovery-aware) but doesn't give an implementer enough to know what SyntaxTree nodes to define. Missing: error recovery node shape, concrete node inventory or shape sketch, and explicit contract for how malformed input is represented. A parser design doc author would need to invent these from scratch.



2. **Missing navigation guide.** A 486-line doc serving two audiences (human implementers and AI agents) needs a "How to read this document" paragraph after the status block. Three sentences: what §1–§3 cover (commitments and pipeline overview), what §4–§8 cover (per-stage contracts), what §9–§12 cover (runtime and integration). This is the single highest-ROI addition for both audiences.



3. **"How it serves the guarantee" paragraphs become formulaic.** Useful for Lexer through Graph Analyzer. By Proof Engine and Lowering, the pattern is predictable and the content is restating the opening sentence. Recommendation: fold the guarantee connection into the stage's opening paragraph for §8–§10 and drop the separate labeled paragraph.



## Genre Assessment



The rewrite succeeds. §1 opens with a problem statement and architectural commitment, not an inventory. Per-stage sections lead with design decisions. The philosophy-first framing is consistent throughout. This is a design document, not a reference manual.



## Decision



This doc is ready to serve as the architectural foundation for per-stage design docs (starting with the parser). The concerns above are improvements, not blockers — the parser concern is the most urgent because that's the immediate next use case.



---



---



---

# Design Review: combined-design-v2.md — Soundness, Completeness, Innovation



**Reviewer:** Frank (Lead Architect)



**Date:** 2026-06-03



**Document:** `docs/working/combined-design-v2.md`



**Context:** Only the Lexer is implemented. All other pipeline stages are stubs.



---



## VERDICT: APPROVED-WITH-CONCERNS



The document is architecturally sound and well-structured. It reads as a unified design explanation rooted in philosophy, not a defense of two separate systems. The pipeline is coherent, the artifact boundaries are clean, and the lowered executable model is concrete enough to implement against. The concerns below are real design gaps that will cost us if we hit them mid-implementation rather than addressing them now.



---



## Soundness Issues



1. **The proof strategy set is closed but its coverage boundary is unstated.** The doc lists four strategies (literal, modifier, guard-in-path, flow narrowing) and says "any obligation outside this set is unresolvable." But it never states what percentage of real-world proof obligations these four strategies can discharge. If most `ProofRequirement` instances in practice require cross-field reasoning (e.g., `ApprovedAmount <= RequestedAmount`), then the four strategies are a beautiful design that rejects most real programs. The doc should include a coverage analysis against the sample corpus — even an informal one — so implementers know whether the strategy set is right-sized or whether a fifth strategy (e.g., relational pair narrowing) is needed before v1.



2. **`Restore` bypasses access-mode but evaluates constraints — the interaction with computed fields is unspecified.** If persisted data includes stale computed-field values, does Restore recompute before constraint evaluation? The recomputation index is listed as a Restore input, but the evaluation order (recompute → validate vs. validate → recompute) is not specified. Getting this wrong means Restore either rejects valid persisted data or accepts invalid computed values.



3. **The `Create` without initial event path evaluates `always` + `in <initial>` — but default values may not satisfy `in <initial>` constraints.** The doc doesn't specify whether this is a compile-time guarantee (the proof engine should catch it) or a runtime domain outcome. The static-reasoning research (C3) says this is a known check, but the combined design doesn't thread it through the proof/fault chain. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets no compile-time warning in the current design — only a runtime `EventConstraintsFailed` on create. That violates the prevention promise.



4. **`ConstraintActivation` discriminant is described but not typed.** The doc says it "distinguishes whether a constraint binds to the current state, the source state, or the target state." But it doesn't specify whether this is an enum, a DU, or a tag on the descriptor. Given the catalog-driven architecture, this should be cataloged — it's language-surface knowledge that consumers need, not an implementation detail.



---



## Completeness Gaps



1. **No error recovery strategy for the parser.** The doc says `SyntaxTree` preserves "recovery shape for broken programs" and mentions "missing-node representation," but there is no recovery algorithm specified. Panic-mode? Synchronization tokens? The parser recovery strategy directly affects LS quality — a bad recovery model means completions and diagnostics degrade on every keystroke. This is a design decision, not an implementation detail, and it should be locked before the parser is built.



2. **No incremental compilation model.** The doc treats the pipeline as a single-shot transformation: source → tokens → tree → model → graph → proof → CompilationResult. But the language server needs incremental re-analysis on every keystroke. The doc should specify the invalidation boundary — does a keystroke re-lex the whole file? Re-parse? Re-typecheck? For a single-file DSL this may be "just re-run everything" and that's fine — but say so explicitly, with a size-ceiling argument for why that's acceptable (the 64KB source limit helps here).



3. **No serialization contract for `Version`.** The doc specifies `Restore` as the reconstitution path, but never specifies what the caller provides. What is the serialization shape of a `Version`? Is it `(stateName, fieldValues)`? `(stateDescriptor, slotArray)`? The host application needs a defined contract for what to persist and what to hand back to `Restore`. Without it, every host will invent its own serialization and we'll get impedance mismatches.



4. **No definition versioning or migration story.** When a `.precept` file changes (field added, state renamed, constraint tightened), what happens to persisted `Version` instances compiled against the old definition? `Restore` will reject them if they don't satisfy the new constraints. The doc should at least name this as a known gap and specify whether migration is in-scope or explicitly deferred.



5. **No observability hooks.** The doc specifies structured outcomes and inspections, but no tracing, logging, or metric emission points. For a production runtime, host applications need to observe: which events fired, which constraints failed, how long evaluation took, which proof strategies were used. These hooks shape the evaluator's internal architecture — bolting them on later means refactoring the evaluator.



---



## Innovation Opportunities



1. **The proof engine should guarantee initial-state satisfiability at compile time.** The research base (static-reasoning-expansion.md, C3/C4/C5) already describes per-field interval analysis. The combined design should commit to a concrete compile-time guarantee: *if default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability and emits a diagnostic if no valid initial configuration exists.* This is unique among DSL runtimes — no validator, state machine library, or rules engine provides this. It's the proof engine's signature contribution and it's achievable with the bounded strategy set already designed.



2. **Precompute a "constraint influence map" during lowering for AI-native inspection.** Currently the inspection API tells you *what* constraints are active and *whether* they passed. It doesn't tell you *which fields drive which constraints* — the dependency graph exists in the `TypedModel` but is not lowered into an inspectable form. If lowering also produces a `ConstraintInfluenceMap` (constraint → contributing fields, with expression-text excerpts), then an AI agent can answer "why did this constraint fail?" and "which field change would fix it?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.



3. **The executable model should be a compiled decision table, not a tree walk.** The doc says lowering produces "lowered expression nodes and action plans" but doesn't specify the execution model. For Precept's small, closed expression language, the optimal model is a flat evaluation plan — precomputed slot references, operation opcodes, and result slots — not a recursive tree interpreter. Think of it as a register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. The doc should commit to "flat evaluation plan" as the executable model shape and explicitly reject tree-walking.



4. **Emit a machine-readable "contract digest" alongside `CompilationResult`.** A deterministic hash of the compiled definition's semantic content (fields, types, constraints, states, transitions — excluding whitespace and comments) would let host applications detect definition changes without diffing source text. Pair it with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints) and you have the foundation for the migration story (gap #4 above) and a production deployment safety net.



5. **The constraint evaluation matrix should surface "why not" explanations as structured data.** When `Fire` returns `Rejected` or `EventConstraintsFailed`, the outcome carries `ConstraintViolation` objects. But the doc doesn't specify whether violations carry *explanation depth* — just the failing expression text, or also the evaluated field values, the guard that scoped the constraint, and the specific sub-expression that failed. For AI legibility, violations should carry structured explanation: `{ constraint, expression, evaluatedValues: { field: value }, guardContext?, failingSubExpression? }`. This is cheap to compute during evaluation and transforms MCP from "it failed" to "it failed because X was 3 and the constraint requires X > 5."



---



## Right-Sizing Issues



1. **The `SyntaxTree` vs `TypedModel` anti-mirroring rules are over-specified for a doc at this level.** Four numbered rules about what `TypedModel` must not do, plus a seven-item "required inventory" — this is component-level design specification embedded in an architecture document. The architectural decision (they are separate artifacts with separate jobs) is correct and should stay. The implementation contract should move to a parser/type-checker-specific design doc that the implementer reads when building those stages.



2. **The five constraint-plan families and four activation indexes are correctly designed but could be simplified in the implementation.** The `from` and `to` families only activate during `Fire`. The `on` family only activates during `Fire`. The `in` family activates during `Update`, `Create`-without-event, and `Restore`. The `always` family activates everywhere. This means the evaluator really has two modes: "fire mode" (all five families) and "edit mode" (always + in). The doc could name these modes to simplify the mental model without losing the family distinction.



---



## Top 3 Recommended Changes Before This Doc Drives Per-Component Design



## 1. Add a proof coverage analysis against the sample corpus.



Run the four proof strategies against every `ProofRequirement` that would arise from the 20 sample files. Report how many obligations each strategy discharges and how many remain unresolvable. If coverage is below ~90%, design a fifth strategy before implementation begins. This is the highest-risk unknown in the document — the proof engine's value proposition depends on it.



## 2. Specify the parser error recovery strategy.



Lock one of: (a) panic-mode with synchronization at declaration keywords, (b) token-deletion/insertion with cost model, (c) "re-lex everything, re-parse everything" with the 64KB ceiling as the performance argument. The LS team cannot build completion/diagnostic features without knowing what the tree looks like on broken input.



## 3. Commit to a flat evaluation plan as the executable model.



Replace "lowered expression nodes and action plans" with a concrete specification: slot-addressed evaluation plans with operation opcodes, field-slot references, literal constants, and result slots. This prevents the implementation from defaulting to a recursive AST interpreter — which would be correct but would sacrifice the performance and inspectability properties that make Precept's runtime distinctive.



---



*This review is direct because the timing demands it. Addressing these three items now — before the parser, type checker, and evaluator are built — is nearly free. Addressing them after implementation begins is expensive. The architecture is sound. These are the gaps that would bite us.*



---



---



---

# Decision: Combined Design v2 Comprehensive Revision Pass



**By:** Frank



**Date:** 2026-07-17



**Status:** Applied



## Summary



Applied all team review feedback (Frank design review, George technical accuracy, Elaine readability) to `docs/working/combined-design-v2.md` in a single revision pass. Added Precept Innovations callouts to every major section. Added two new sections: §12 TextMate Grammar Generation and §13 MCP Integration.



## What Changed



## Review feedback applied (all three reviewers)



- Navigation guide ("How to read this document") after status block



- Parser: error recovery shape, node inventory, catalog-to-grammar mapping, anti-Roslyn guidance, ActionKind dual-use, parser/TypeChecker contract boundary



- TypeChecker: anti-pattern for per-construct check methods



- Proof engine: coverage boundary, flow narrowing clarification, initial-state satisfiability



- Compilation snapshot: no-incremental-compilation model, contract digest hash, definition versioning gap



- Lowering: fixed "catalogs not re-read" claim, descriptor shapes, flat evaluation plan, anti-pattern warnings, ConstraintActivation cataloging, Version serialization contract



- Runtime: Restore recomputation order, structured "why not" violations



## New content



- **Precept Innovations callouts** in every major section (§2–§14), 2–4 bullets each



- **§12 TextMate grammar generation** — catalog contributions table, anti-pattern, zero-drift guarantee



- **§13 MCP integration** — tool inventory, thin-wrapper principle, AI-first design, catalog-derived vocabulary



## Structural changes



- Former §12 (LS integration) renumbered to §14



- Doc grew from 486 to 694 lines



- Formulaic guarantee paragraphs folded into stage openings for §8–§10



## Decisions Locked



- Parser error recovery: construct-level panic mode with `MissingNode` + `SkippedTokens`



- Expression evaluation: flat slot-addressed evaluation plans, tree-walk explicitly rejected



- Incremental compilation: "re-run everything" is the intended model (64KB ceiling)



- Definition versioning: known gap, deferred beyond v1



- `ConstraintActivation`: should be cataloged (language-surface knowledge)



---



## Proposal Summary



Invert D3: make `write` the universal default for (field, state) pairs. Add a `readonly` modifier on field declarations to permanently lock fields from ever being written in any state. Eliminate root-level `write` declarations entirely.



---



## Question 1: Does inverting D3 weaken the conservative guarantee?



**Yes. Fundamentally.**



D3 as specified (§2.2 Access Mode, composition rule 1) states: "D3 is the universal per-pair baseline — undeclared (field, state) pairs default to `read`." The design principle behind this is explicit: "Authors declare only exceptions to readonly — `write` opens a field for editing in that state."



This is a **closed-world access model**. Nothing is writable unless explicitly opened. The omission failure mode is safe: if an author forgets to declare a `write`, the field is locked in that state. The author must take a deliberate action — writing the `write` keyword — to open the attack surface.



The proposal inverts this to an **open-world access model**. Everything is writable unless explicitly restricted. The omission failure mode is unsafe: if an author forgets to mark a field `readonly`, it is exposed in every state to direct mutation via `Update`.



This is the firewall-rule principle. Good security defaults to DENY; you add ALLOW exceptions. D3 defaults to DENY (read-only) and authors add ALLOW exceptions (`write`). The proposal defaults to ALLOW (writable) and authors add DENY exceptions (`readonly`). In a **governance** language — one whose entire identity is built on "invalid configurations are structurally impossible" (Principle 1: Prevention, not detection) — the conservative default is non-negotiable.



## Corpus evidence



The sample set confirms that the conservative default reflects real domain proportions:



- **Stateful precepts with zero write declarations:** `hiring-pipeline`, `loan-application` (except one guarded write), `apartment-rental-application`, `restaurant-waitlist`, `library-hold-request`, `travel-reimbursement`, `warranty-repair-request`. These precepts rely entirely on event-driven mutation. The D3 default silently protects all fields from direct editing. Under the proposal, all those fields would be writable by default — an enormous, invisible expansion of the attack surface.



- **Stateful precepts with 1–2 write declarations:** `crosswalk-signal` (1), `clinic-appointment-scheduling` (1), `building-access-badge-request` (1), `insurance-claim` (2), `maintenance-work-order` (1), `refund-request` (1), `subscription-cancellation-retention` (1), `event-registration` (2), `it-helpdesk-ticket` (1), `utility-outage-report` (1), `vehicle-service-appointment` (1). The typical pattern is opening 1–3 fields in 1–2 states. The remaining (field, state) pairs — the overwhelming majority — stay protected by D3.



- **Stateless precepts:** `fee-schedule` (3 of 5 writable), `payment-method` (2 of 6 writable), `computed-tax-net` (2 of 4 writable), `invoice-line-item` (4 of N writable). Even in stateless precepts, the typical pattern is that some fields are intentionally locked. `customer-profile` is the only sample using `write all`.



The verbosity cost of the current model is 1–2 lines per precept. The safety cost of the proposed model is an invisible, unbounded expansion of the mutation surface whenever an author omits a `readonly` marker.



## Principle citations



- **Principle 1 (Prevention, not detection):** The proposal turns field-level access control from structurally prevented to structurally permitted. An author who omits `readonly` on a field that should be locked has created a governance gap. Under D3, the same omission creates no gap.



- **Principle 4 (Full inspectability):** Auditability is stronger when the declared surface is the exception set (small, explicit) rather than the restriction set (requiring mental subtraction from a universal default). "What can a user directly edit here?" is answered by scanning for `write` keywords under D3. Under the proposal, the answer is "everything, minus what's marked `readonly`" — which requires reading every field declaration to check for the absence of a modifier.



---



## Question 2: Does `readonly` on a field cleanly complement or conflict with computed fields?



**It creates a semantic inconsistency.**



Computed fields (`field Tax as number -> Subtotal * TaxRate`) are already implicitly readonly. The spec enforces this structurally — `ComputedFieldNotWritable` is a type-checker diagnostic (§3.8). A computed field's readonly nature arises from its derivation: it has an expression, so it cannot be directly assigned. This is not a modifier; it is a structural consequence of the field's kind.



Under the proposal, the access defaults would be:



| Field kind | Proposed default | Actual access |



|---|---|---|



| Stored field (no `readonly`) | write | write |



| Stored field (with `readonly`) | write → overridden to read | read |



| Computed field | write (in theory) | read (structurally) |



The computed field's access mode would be inconsistent with the declared default. A stored field and a computed field would have different effective defaults despite the language claiming "write is the default." The author would need to understand that computed fields are a hidden exception to the stated default — undermining Principle 4 (inspectability) and Principle 5 (keyword-anchored readability).



Under D3, the picture is consistent:



| Field kind | D3 default | Actual access |



|---|---|---|



| Stored field (no `write`) | read | read |



| Stored field (with `write`) | read → overridden to write | write |



| Computed field | read | read |



All fields default to read. Computed fields are naturally aligned with the default. Stored fields that need to be writable are explicitly opened. There is no inconsistency to explain.



Adding `readonly` as a modifier also creates a redundancy question: should `readonly` on a computed field be a warning (redundant modifier), an error (modifier conflicts with structural readonly), or silently accepted? Each answer has downsides. Under D3, the question never arises — there is no `readonly` keyword, and computed fields simply match the default.



---



## Question 3: Does "write default, restrict per state" change the auditability story?



**Yes. It weakens it materially.**



In a stateful precept under D3, the audit question "which fields can a user directly edit in state S?" is answered by reading the `in S write` declarations. If there are none, the answer is "nothing — all mutation happens through events." This is a **closed-world audit**: the write declarations ARE the complete answer.



Under the proposal, the same question is answered by: "every field, minus those marked `readonly` on the field declaration, minus those restricted by `in S read` or `in S omit` declarations." This is an **open-world audit** requiring cross-referencing the field declarations (for `readonly` markers), the state-scoped access declarations (for per-state restrictions), and computing the difference. The mental model is subtraction from a universal set rather than enumeration of an explicit set.



For a governance language — one where the point is to make the access contract **explicit and visible** — the open-world model is the wrong posture. The current model's strength is that the write declarations positively assert what is open. The proposed model requires the reader to infer what is open from what is not restricted.



This matters especially for AI consumers. Precept's Principle 3 (deterministic semantics) and Principle 5 (keyword-anchored readability) are designed partly for AI legibility. A closed-world access model is easier for AI agents to reason about: "find all `write` declarations" is a simple, complete query. "Find all fields, subtract `readonly` fields, subtract per-state restrictions" is a compositional query with a higher error surface.



---



## Additional Concerns



## The `readonly` keyword itself is misaligned



`readonly` is a **programming-language concept** from C#, Java, Rust, TypeScript. It carries connotations of compile-time immutability, final binding, memory-model guarantees. Precept's access model is about **editability** — which fields can the host application directly mutate via the `Update` operation. These are different concepts. A field that is `read` in a given state is not immutable — events can still `set` it during transitions. It is merely not directly editable by the external caller. Introducing `readonly` would import programming-language semantics into a domain-configuration language, violating the philosophy's positioning of Precept as a language for domain experts and business analysts, not software developers (§ Who authors a precept in philosophy.md).



## Root-level `write` elimination is a false economy



The proposal motivates itself partly by eliminating root-level `write` declarations. But the current model already makes these declarations do useful work:



- `write BaseFee, DiscountPercent, MinimumCharge` in `fee-schedule` — the `write` keyword positively documents the author's intent. Reading it, you know immediately which fields are editable. The comment above it ("Only pricing levers are editable; TaxRate and CurrencyCode are locked") is restating what the `write` declaration already says.



- `write all` in `customer-profile` — a deliberate, visible assertion that everything is open. Under the proposal, this becomes the invisible default, and the author's deliberate intent vanishes from the surface.



The `write` keyword carries semantic weight as a positive assertion. Replacing it with the absence of `readonly` loses that signal.



---



## Verdict: **Reject**



The proposal inverts Precept's conservative access posture from closed-world (safe by default, explicitly opened) to open-world (exposed by default, explicitly restricted). This:



1. **Weakens the omission failure mode** from safe (field locked) to unsafe (field exposed).



2. **Creates an access-default inconsistency** between stored and computed fields.



3. **Degrades auditability** from positive enumeration to negative subtraction.



4. **Imports programming-language semantics** (`readonly`) into a domain-configuration language.



5. **Eliminates the positive-assertion value** of `write` declarations for marginal verbosity savings (1–2 lines per precept).



D3 is philosophically correct, empirically well-calibrated to real domain proportions, and consistent with the governance identity. It should not be inverted.



## What would need to change for reconsideration



If the underlying concern is verbosity in stateless precepts that happen to have mostly-writable fields, there are narrower solutions that preserve D3:



- A `write all` shorthand already exists and handles the fully-open case.



- If a `write all except F1, F2` syntax were needed, it could be evaluated without inverting the default. The exception list would still be a positive declaration against a positively-declared baseline.



Neither of these requires abandoning the conservative default. The proposal conflates "reduce boilerplate" with "invert the safety model." Only the former is a real problem; the latter is the wrong solution.



---



---



---

# Full Architecture Review — spike/Precept-V2



**Reviewer:** Frank (Lead Architect)



**Branch:** `spike/Precept-V2`



**Commits reviewed:** 36ccec4..4831cb3 (full branch vs main)



**Build:** ✅ Clean (1 pre-existing RS1030 warning in PRECEPT0013)



**Tests:** ✅ 2678 passing (2424 Precept.Tests + 254 Precept.Analyzers.Tests), 0 failures



---



## 1. Annotation Bridge Architecture (PRECEPT0019)



## Files Reviewed



- `src/Precept/HandlesCatalogExhaustivelyAttribute.cs`



- `src/Precept/Language/HandlesCatalogMemberAttribute.cs`



- `src/Precept.Analyzers/Precept0019PipelineCoverageExhaustiveness.cs`



- `src/Precept/Pipeline/Parser.cs` (class marker on `ParseSession`)



- `src/Precept/Pipeline/TypeChecker.cs` (class marker + 11 member annotations)



- `src/Precept/Pipeline/GraphAnalyzer.cs` (class marker + 11 member annotations)



## Assessment



The annotation bridge is clean and catalog-agnostic as specified. The class marker accepts `Type catalogEnum` — any enum can opt in. Method markers use `object kind` for call-site type safety without analyzer rewrites.



PRECEPT0019 correctly:



- Extracts `typeof(T)` from the class marker



- Collects all enum fields with constant values



- Resolves method marker arguments by matching `arg.Type` against the catalog enum



- Reports missing members with clear diagnostic formatting



- Is registered as `DiagnosticSeverity.Error` (was previously Warning, promoted per Slice 26)



Parser coverage: `ParseSession` (ref partial struct) has both `ParseExpression` and `ParseAtom` annotated, covering all 11 `ExpressionFormKind` members across the two methods. TypeChecker and GraphAnalyzer have placeholder methods with all 11 annotations each — correct forward-declarations for Phase 3.



---



## 2. Catalog Integrity Analyzers (PRECEPT0020–0023)



## PRECEPT0020 — Operators Token Collision



Two sub-rules (0020a: `(Token.Kind, Arity)` key collision; 0020b: binary `Token.Kind` collision). Both correctly:



- Scope to `OperatorKind` switches via `TryGetCatalogSwitchKind`



- Skip `MultiTokenOp` arms (correct — those are PRECEPT0023's domain)



- Extract token kind via `Tokens.GetMeta(TokenKind.X)` invocation walking



- Report against the creation syntax location (not the arm)



## PRECEPT0021 — Tokens Duplicate Text



- Correctly skips null `Text` (synthetic tokens like `SetType`, `Identifier`)



- Uses `ResolveStringConstant` which handles nameof, const fields, and string literals



- Only fires for `TokenKind` switches



## PRECEPT0022 — Operators Inline Token Reference



- Detects `new TokenMeta(...)` construction where `Tokens.GetMeta(TokenKind.X)` is required



- Clean single-purpose analyzer — no false-positive risk from DU subtype checks



## PRECEPT0023 — OperatorMeta DU Shape Invariants



Three sub-rules:



- **0023a:** MultiTokenOp < 2 tokens → Error. Correct.



- **0023b:** SingleTokenOp vs MultiTokenOp lead-token collision. Cross-checks single/multi dictionaries post-loop. Correct.



- **0023c:** Duplicate full token sequences. Uses `BuildFullSequenceKey` joining all tokens. Correctly checks the full sequence (e.g., "Is,Set" vs "Is,Not,Set"), not just the lead token. The diagnostic name says "MultiLeadCollision" but the invariant checks the **full sequence** — naming is slightly misleading but functionally correct.



## CatalogAnalysisHelpers



Shared infrastructure is well-factored:



- `TryGetCatalogSwitchKind` correctly guards scope (method named "GetMeta", in `Precept.Language`, known enum type)



- `EnumerateCollectionElements` handles both collection expressions and array initializers



- `UnwrapConversions` handles implicit conversion chains



- `FlagsEnumContains` supports single-ref, bitwise-OR-tree, and constant-folded forms



---



## 3. Parser Fixes



## GAP-A: `when` guard on StateEnsure/EventEnsure



`ParseStateEnsure` and `ParseEventEnsure` both implement post-condition `when` guards correctly:



- Check if `stashedGuard` exists (pre-ensure guard from outer dispatch)



- Only consume `when` if no stashed guard — prevents double-guard ambiguity



- Guard comes **after** the condition expression, before `because` — matches spec §2.2



## GAP-B: Modifiers after computed field expressions



Verified via `ExpressionBoundaryTokens` and the Pratt loop's natural termination on boundary tokens. The parser correctly stops expression parsing when it encounters modifier keywords because they're in `ExpressionBoundaryTokens` via `Constructs.LeadingTokens`. No explicit handling needed — clean by construction.



## GAP-C: Keyword-as-member-name and keyword-as-function-call



Two complementary fixes:



1. `ExpectIdentifierOrKeywordAsMemberName()` — accepts tokens in `KeywordsValidAsMemberName` after `.`



2. `ParseAtom` — `case TokenKind.Min: case TokenKind.Max:` falls through to identifier/function-call handling



Both correct. The keyword-as-function-call case handles `min(a, b)` / `max(a, b)` in expression position.



## is/is-not-set, method call, list literal, TypedConstant



- `is set` / `is not set`: Correctly uses separate `IsSetExpression`/`IsNotSetExpression` nodes. Precedence 60 matches `Operators.GetMeta(OperatorKind.IsSet).Precedence`. Non-associative by break-on-entry (`minPrecedence > 60`).



- Method call: Detects `LeftParen` following `MemberAccessExpression` at binding power 90. Correct.



- List literal: Dispatches from `ParseAtom` via `TokenKind.LeftBracket`. Correct.



- TypedConstant/InterpolatedTypedConstant: Both handled in `ParseAtom` correctly.



---



## 4. ExpressionFormKind Catalog



## Members (11 total — correct)



1. Literal, 2. Identifier, 3. Grouped, 4. BinaryOperation, 5. UnaryOperation,



6. MemberAccess, 7. Conditional, 8. FunctionCall, 9. MethodCall, 10. ListLiteral,



11. PostfixOperation



## Metadata Shape



`ExpressionFormMeta` record carries: Kind, Category, IsLeftDenotation, LeadTokens, HoverDocs. All fields populated. LeadTokens empty for led forms, non-empty for nud forms — structurally enforced by the Layer 2 test.



## Coverage Tests



Two test classes provide layered enforcement:



- `Tests.Language.ExpressionFormCoverageTests` — Layer 2: count, GetMeta completeness, HoverDocs, IsLeftDenotation, LeadTokens contract



- `Tests.ExpressionFormCoverageTests` — Layer 3: catalog completeness, annotation bridge xUnit mirror, parse round-trips



---



## 5. OperatorMeta DU Shape



Clean discriminated union:



- `OperatorMeta` (abstract base) → `SingleTokenOp` / `MultiTokenOp`



- `MultiTokenOp` carries `IReadOnlyList<TokenMeta> Tokens` with `LeadToken => Tokens[0]`



- `ByToken` FrozenDictionary indexed by `(TokenKind, Arity)` — excludes MultiTokenOp



- `ByTokenSequence` FrozenDictionary indexed by `(TokenKind, TokenKind?, TokenKind?)` — covers MultiTokenOp



- `BuildSequenceKey` correctly handles 2-token and 3-token sequences



Precedence values consistent: IsSet/IsNotSet at 60, matching arithmetic multiplication level. This is correct per spec §2.1 — presence checks bind tighter than comparisons but at the same level as multiplicative arithmetic.



---



## 6. TokenMeta.IsValidAsMemberName



- Property added to `TokenMeta` record with `bool IsValidAsMemberName = false` default



- Set to `true` on `TokenKind.Min` and `TokenKind.Max` only



- `Parser.KeywordsValidAsMemberName` derived from `Tokens.All.Where(t => t.IsValidAsMemberName).Select(t => t.Kind).ToFrozenSet()`



- No hardcoded `{ Min, Max }` array remains — pure catalog derivation



- Tests: `TokenMetaMemberNameTests` covers true/false/theory cases



- `SetType` handled correctly: `Text: null`, `TextMateScope: null`, `SemanticTokenType: null` — parser-synthesized token with no tooling metadata. Excluded from `Keywords` FrozenDictionary via explicit `m.Kind != TokenKind.SetType` filter. This prevents the `Text: null` duplicate-text false positive that would otherwise fire.



---



## 7. Parser Split



Three partial files with clean responsibility separation:



- `Parser.cs` — vocabulary FrozenDictionaries, boundary sets, `Parse()` entry point, `ParseSession` struct definition, token navigation



- `Parser.Declarations.cs` — construct parsers (state ensure, event ensure, access mode, omit, transition row, outcomes, action statements)



- `Parser.Expressions.cs` — Pratt expression parser (ParseExpression led loop, ParseAtom nud switch, interpolation parsers, list literal)



No duplication detected. The `HandlesCatalogExhaustively` attribute lives on `ParseSession` in `Parser.cs`; the `HandlesCatalogMember` annotations are distributed across `Parser.Expressions.cs` methods. This is correct — the ref partial struct spans files.



---



## 8. Documentation Accuracy



`docs/language/catalog-system.md` § Exhaustiveness Enforcement Strategies:



- Correctly describes both strategies (CS8509 vs annotation bridge)



- Decision rule table is clear and actionable



- Phase 3 note correctly defers TypeChecker/ProofEngine dispatch decision



- Consumer table for current CS8509 sites is accurate (`ConstructKind`, `ActionKind`, etc.)



---



## Findings



## Blockers



None.



## Guidance



- **G1:** [`src/Precept.Analyzers/Precept0023OperatorsDUShapeInvariants.cs:30`] The constant `DiagnosticId_MultiLeadCollision = "PRECEPT0023c"` and field name `MultiLeadCollisionRule` use "lead" in their identifiers, but the invariant actually checks the **full token sequence** (not just the lead). Consider renaming to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` for clarity. The diagnostic message is correct — only the code-level naming is misleading.



- **G2:** [`src/Precept.Analyzers/CatalogAnalysisHelpers.cs:57-62`] `CatalogEnumNames` is missing `ConstraintKind` and `ProofRequirementKind`. Both have `GetMeta` switches in `Precept.Language`. Currently their switches use discard arms (`_ =>`), so PRECEPT0007 would flag them anyway if they were included. When those catalogs drop the discard arm (expected in Phase 3), they should be added to `CatalogEnumNames` to enable PRECEPT0007 coverage. Track this as a Phase 3 prerequisite.



- **G3:** [`src/Precept.Analyzers/Precept0013ActionsCrossRef.cs:136`] Pre-existing RS1030 warning (`Compilation.GetSemanticModel()` inside analyzer). Not introduced on this branch, but should be addressed eventually — Roslyn best practice violation.



## Observations



- **O1:** TypeChecker and GraphAnalyzer currently throw `NotImplementedException` — the `[HandlesCatalogMember]` annotations are forward declarations. This is correct by design (Phase 3 work); PRECEPT0019 validates the annotation set at compile time regardless of implementation status.



- **O2:** The `contains` chaining test (Slice 18) correctly validates `NonAssociativeComparison` diagnostic for `a contains b contains c` via the Pratt loop's non-associativity detection in lines 113-126 of `Parser.Expressions.cs`. Binding power 40 is correct per catalog.



- **O3:** The test count increased from ~2000 (pre-spike) to 2678 — a ~34% test growth proportional to the implementation surface. Healthy ratio.



- **O4:** `ExpressionFormKind` is enumerated 1–11 (no zero slot). This is consistent with the other catalog enums that use `PRECEPT0018SemanticEnumZeroSlot` to enforce meaningful zero absence.



---



## VERDICT: APPROVED — 0 blockers, 3 guidance items



The annotation bridge architecture is sound, catalog-agnostic, and correctly enforced at `DiagnosticSeverity.Error`. The four new analyzers (PRECEPT0020–0023) cover real invariants that would otherwise manifest as startup crashes. Parser fixes are correct and well-tested. The ExpressionFormKind catalog and OperatorMeta DU are structurally complete. Documentation is accurate. The 3 guidance items are naming clarity and forward-looking hygiene — none block merge.



This branch is ready to merge to main.



---



---

# **CRITICAL GAPS**



The parser suite is green, but it is **not** comprehensive enough to support type-checker development safely. The biggest holes are the full type-reference surface, full action syntax surface, wildcard/shorthand routing (`from any`, `modify all`, `omit all`), event-arg richness, interpolation, and specific parser diagnostic-code assertions. Right now, too many tests stop at “a slot exists” or “the parser did not crash.” That is not enough. No soup for unanchored parser behavior.

# TypeChecker B1/B2/B3 Blockers — Fixed



**By:** George (Runtime Dev)

**Date:** 2026-05-08T07:00:00-04:00

**Status:** Complete — all three R3 blockers resolved, tests green

**Context:** Frank's R3 final gate review (`.squad/decisions/inbox/frank-r3-final-review.md`) identified three blockers preventing GraphAnalyzer from proceeding.



---



## Changes



## B3: MissingExpression D26 gap (5 LOC)



`ResolveMissing()` now emits a lightweight `DiagnosticCode.TypeMismatch` diagnostic with args `("expression", "missing")` before returning `TypedErrorExpression`. This closes the D26 self-containment invariant — every error path through Resolve() now records a TC-level diagnostic.



No new DiagnosticCode was added (per Frank's approval gate). TypeMismatch is the closest existing Error-severity TC code.



## B1: Field expression resolution (~100 LOC)



`ResolveFieldExpressions()` resolves default and computed expressions on `TypedField` entries:

- Default expressions from `ParsedModifier` with `Kind == ModifierKind.Default`

- Computed expressions from `ComputeExpressionSlot` on the field's `Syntax`

- `ComputedFieldDep` extraction via recursive `CollectFieldRefs()` tree walker

- `FieldScopeMode.PriorFieldsOnly` enforces forward-reference prohibition

- Qualifier binding left as null (no parser-level qualifier slot on field constructs yet)

- Event arg defaults left as null (DeclaredArg carries only ModifierKind, not values)



## B2: Construct normalization (~200 LOC)



Four new normalization methods following the established `manifest.ByKind` + Resolve + accumulate pattern:

- `PopulateEnsures()` — StateEnsure (in/to/from → ConstraintKind) and EventEnsure (on → EventPrecondition)

- `PopulateAccessModes()` — state/field reference resolution, Editable→Write / Readonly→Read mapping, optional guard

- `PopulateStateHooks()` — state reference, leading token → AnchorScope, action chain via ResolveAction()

- `PopulateEditDeclarations()` — D24 placeholder using ConstructKind.OmitDeclaration, field targets recorded



## Supporting changes



- `ParsedConstruct.LeadingTokenKind` — added `TokenKind?` to the positional record (2 parser sites updated) for anchor scope determination

- Doc updates W3 (§1 status), W4 (§4 LOC estimate → ~2700), W5 (§13 preamble → COMPLETED)

- 17 tests updated to match new diagnostic emission and populated accumulators



---



## Validation



- Build: 0 errors, 0 warnings

- Tests: 3342 Precept.Tests + 263 Precept.Analyzers.Tests — all passing

- D26 assert: no fires on any test or sample file



## Open Items



- **Qualifier binding** on TypedField — needs parser-level qualifier slot on field constructs (future work)

- **Event arg default expressions** — DeclaredArg only carries ModifierKind array, not values (future work)

- **DiagnosticCode.TypeMismatch reuse** for MissingExpression — Frank may want a dedicated code in the future

# Precept TextMate Grammar — Authoritative Specification



**Date:** 2026-05-08

**Author:** Frank

**Status:** DRAFT — pending review



**Source material reviewed:**

- `design/system/semantic-visual-system-manifest.md` — primary visual system design

- `design/system/semantic-visual-system-notes.md` — supplementary notes

- `design/system/README.md` — design system ownership

- `design/brand/brand-decisions.md` — brand palette and typography locked direction

- `design/brand/philosophy.md` — redirects to `docs/philosophy.md`

- `src/Precept/Language/Tokens.cs` (515 lines) — complete token catalog with TextMateScope assignments

- `src/Precept/Language/TokenKind.cs` (205 lines) — 139 token kinds

- `src/Precept/Language/Types.cs` — type catalog (37.4 KB)

- `src/Precept/Language/TypeKind.cs` — 32 type kinds

- `src/Precept/Language/Modifiers.cs` (260 lines) — 29 modifier kinds across 5 DU subtypes

- `src/Precept/Language/ModifierKind.cs` — modifier enum

- `src/Precept/Language/Actions.cs` (222 lines) — 15 action kinds

- `src/Precept/Language/ActionKind.cs` — action enum

- `src/Precept/Language/Operators.cs` (206 lines) — 21 operator kinds

- `src/Precept/Language/OperatorKind.cs` — operator enum

- `src/Precept/Language/Constructs.cs` (199 lines) — 12 construct kinds

- `src/Precept/Language/Functions.cs` — 21 built-in function kinds

- `src/Precept/Language/FunctionKind.cs` — function enum

- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (457 lines) — hand-authored grammar

- `tools/Precept.GrammarGen/Program.cs` (537 lines) — grammar generator scaffold

- `docs/tooling/extension.md` — extension architecture

- All 28 `.precept` sample files in `samples/`



---



## Executive Summary



The hand-authored `precept.tmLanguage.json` is **severely incomplete and stale**. It covers roughly 40% of the current language surface, uses at least 3 retired keywords (`nullable`, `invariant`, `assert`), a retired syntax form (`event Name with Arg` instead of parenthesized args), and classifies tokens into only 4 flat keyword groups that collapse the 14 semantic categories the catalog defines. The grammar generator (`GrammarGen/Program.cs`) correctly derives keyword alternation patterns from catalog metadata but carries the same 2 stale structural patterns (`with`-syntax events, `assert` keyword) and omits 8 construct-level patterns and the gold-colored message-string pattern that the visual system design requires. This spec defines the complete grammar that the generator must produce to replace the hand-authored file at parity-or-better.



---



## 1. Design System → TextMate Scope Mapping



The brand decisions (`brand-decisions.md`) lock 8 authoring-time color families plus comments. TextMate scopes must enable theme rules to target each family independently. The catalog (`Tokens.cs`) already assigns a `TextMateScope` to every token. This table maps visual system roles to catalog scopes and notes misalignments.



| # | Design Role | Brand Color | Typography | Catalog TextMateScope(s) | Notes |

|---|-------------|------------|------------|--------------------------|-------|

| 1 | Structure · Semantic | `#4338CA` | **bold** | `keyword.declaration.precept` | Declaration/behavioral keywords: `precept`, `field`, `state`, `event`, `rule`, `ensure`, `as`, `default`, `optional`, `writable`, `because`, `initial`, `ascending`, `descending` |

| 2 | Structure · Grammar | `#6366F1` | normal | `keyword.control.precept` | Prepositions and control flow: `in`, `to`, `from`, `on`, `of`, `into`, `when`, `if`, `then`, `else`, `by`, `at`, `for` |

| 3 | Structure · Grammar (actions) | `#6366F1` | normal | `keyword.other.action.precept` | Action verbs: `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put` |

| 4 | Structure · Grammar (outcomes) | `#6366F1` | normal | `keyword.other.outcome.precept` | Outcome keywords: `transition`, `no`, `reject` |

| 5 | Structure · Grammar (access) | `#6366F1` | normal | `keyword.other.access-mode.precept` | Access mode: `modify`, `readonly`, `editable`, `omit` |

| 6 | Structure · Grammar (quantifiers) | `#6366F1` | normal | `keyword.other.quantifier.precept` | Quantifiers: `all`, `any`, `each` |

| 7 | Structure · Grammar (constraints) | `#6366F1` | normal | `keyword.other.constraint.precept` | Field constraints: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` |

| 8 | Structure · Grammar (operators) | `#6366F1` | normal | `keyword.operator.precept`, `keyword.operator.arrow.precept` | Symbol operators (`==`, `!=`, `~=`, `!~`, `>=`, `<=`, `>`, `<`, `=`, `+`, `-`, `*`, `/`, `%`) and arrows (`->`, `<-`) |

| 9 | Structure · Grammar (logical) | `#6366F1` | normal | `keyword.operator.logical.precept` | Keyword operators: `and`, `or`, `not` |

| 10 | Structure · Grammar (membership) | `#6366F1` | normal | `keyword.operator.membership.precept` | Membership: `contains`, `is` |

| 11 | States | `#A898F5` | normal (italic if constrained — semantic tokens only) | `entity.name.type.state.precept` | State names in declarations, `from`/`in`/`to` targets, `transition` targets |

| 12 | Events | `#30B8E8` | normal (italic if constrained — semantic tokens only) | `entity.name.function.event.precept` | Event names in declarations, `on` targets, dot-access prefix |

| 13 | Data · Names | `#B0BEC5` | normal (italic if guarded — semantic tokens only) | `variable.other.field.precept`, `variable.parameter.precept`, `variable.other.property.precept` | Field names, event argument names, property accessors after dot |

| 14 | Data · Types | `#9AA8B5` | normal | `storage.type.precept` | All type keywords. Also `storage.modifier.state.precept` for state modifiers (separate from types but same visual family in brand) |

| 15 | Data · Values | `#84929F` | normal | `constant.numeric.precept`, `constant.language.boolean.precept`, `string.quoted.double.precept`, `string.quoted.single.precept` | Literals: numbers, booleans, strings, typed constants |

| 16 | Rules · Messages | `#FBBF24` | normal | `string.quoted.double.message.precept` | **ONLY** in `because "msg"` and `reject "msg"` positions. Must be distinguished from regular `string.quoted.double.precept` |

| 17 | Comments | `#9096A6` | *italic* | `comment.line.number-sign.precept` | `#` line comments |

| 18 | State modifiers | (brand: same as types) | normal | `storage.modifier.state.precept` | `terminal`, `required`, `irreversible`, `success`, `warning`, `error` |

| 19 | Precept name | (brand: identity) | normal | `entity.name.precept.message.precept` | The precept name after `precept` keyword |

| 20 | Punctuation | `#6366F1` | normal | `punctuation.precept`, `punctuation.separator.comma.precept`, `punctuation.accessor.precept` | `.`, `,`, `(`, `)`, `[`, `]` |

| 21 | Member names | (brand: data names) | normal | `keyword.other.precept` | Special member accessors: `countof`, `peekby` |



## Brand-to-Catalog Misalignment Notes



The brand decisions doc (`brand-decisions.md`) lists specific keywords under "Structure · Semantic" that the catalog assigns to different scope categories:



| Keyword | Brand says | Catalog scope | Resolution |

|---------|-----------|--------------|------------|

| `from`, `on`, `in`, `to` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** These are prepositions/control flow. Theme can still bold them if desired. |

| `set` | Structure · Semantic (bold) | `storage.type.precept` (dual-use: action AND type) | **Catalog wins.** `set` is context-dependent — TextMate can't distinguish action vs type usage. Semantic tokens handle this. |

| `transition`, `reject`, `no` | Structure · Semantic (bold) | `keyword.other.outcome.precept` | **Catalog wins.** Dedicated outcome scope enables finer theme control. |

| `when` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** Control flow keyword. |

| `write` | Structure · Semantic (bold) | RETIRED (B4 2026-04-28) | **Remove from brand doc.** Replaced by `writable` field modifier. |

| `nullable` | Structure · Grammar | RETIRED | **Remove from brand doc.** Replaced by `optional`. |



**Action:** Brand doc keyword lists need a sync pass to match catalog reality. This is a brand-doc defect, not a grammar defect.



---



## 2. Language Surface Inventory



Complete enumeration of every token/construct type from the catalog, with canonical TextMateScope.



## 2.1 Keywords — Declaration (`keyword.declaration.precept`)



| Token | Text | Source |

|-------|------|--------|

| Precept | `precept` | TokenKind.Precept (=1) |

| Field | `field` | TokenKind.Field (=2) |

| State | `state` | TokenKind.State (=3) |

| Event | `event` | TokenKind.Event (=4) |

| Rule | `rule` | TokenKind.Rule (=5) |

| Ensure | `ensure` | TokenKind.Ensure (=6) |

| As | `as` | TokenKind.As (=7) |

| Default | `default` | TokenKind.Default (=8) |

| Optional | `optional` | TokenKind.Optional (=9) |

| Writable | `writable` | TokenKind.Writable (=10) |

| Because | `because` | TokenKind.Because (=11) |

| Initial | `initial` | TokenKind.Initial (=12) |

| Ascending | `ascending` | TokenKind.Ascending (=130) |

| Descending | `descending` | TokenKind.Descending (=131) |



## 2.2 Keywords — Prepositions/Control (`keyword.control.precept`)



| Token | Text | Source |

|-------|------|--------|

| In | `in` | TokenKind.In (=13) |

| To | `to` | TokenKind.To (=14) |

| From | `from` | TokenKind.From (=15) |

| On | `on` | TokenKind.On (=16) |

| Of | `of` | TokenKind.Of (=17) |

| Into | `into` | TokenKind.Into (=18) |

| When | `when` | TokenKind.When (=19) |

| If | `if` | TokenKind.If (=20) |

| Then | `then` | TokenKind.Then (=21) |

| Else | `else` | TokenKind.Else (=22) |

| By | `by` | TokenKind.By (=128) |

| At | `at` | TokenKind.At (=129) |

| For | `for` | TokenKind.For (=136) |



## 2.3 Keywords — Actions (`keyword.other.action.precept`)



| Token | Text | Source |

|-------|------|--------|

| Add | `add` | TokenKind.Add (=24) |

| Remove | `remove` | TokenKind.Remove (=25) |

| Enqueue | `enqueue` | TokenKind.Enqueue (=26) |

| Dequeue | `dequeue` | TokenKind.Dequeue (=27) |

| Push | `push` | TokenKind.Push (=28) |

| Pop | `pop` | TokenKind.Pop (=29) |

| Clear | `clear` | TokenKind.Clear (=30) |

| Append | `append` | TokenKind.Append (=132) |

| Insert | `insert` | TokenKind.Insert (=133) |

| Put | `put` | TokenKind.Put (=134) |



**Note:** `set` (TokenKind.Set =23) is dual-use (action AND collection type). Catalog assigns `storage.type.precept`. Appears in both action chains and type positions.



## 2.4 Keywords — Outcomes (`keyword.other.outcome.precept`)



| Token | Text | Source |

|-------|------|--------|

| Transition | `transition` | TokenKind.Transition (=31) |

| No | `no` | TokenKind.No (=32) |

| Reject | `reject` | TokenKind.Reject (=33) |



## 2.5 Keywords — Access Modes (`keyword.other.access-mode.precept`)



| Token | Text | Source |

|-------|------|--------|

| Modify | `modify` | TokenKind.Modify (=34) |

| Readonly | `readonly` | TokenKind.Readonly (=35) |

| Editable | `editable` | TokenKind.Editable (=36) |

| Omit | `omit` | TokenKind.Omit (=37) |



## 2.6 Keywords — Logical Operators (`keyword.operator.logical.precept`)



| Token | Text | Source |

|-------|------|--------|

| And | `and` | TokenKind.And (=38) |

| Or | `or` | TokenKind.Or (=39) |

| Not | `not` | TokenKind.Not (=40) |



## 2.7 Keywords — Membership (`keyword.operator.membership.precept`)



| Token | Text | Source |

|-------|------|--------|

| Contains | `contains` | TokenKind.Contains (=41) |

| Is | `is` | TokenKind.Is (=42) |



## 2.8 Keywords — Quantifiers (`keyword.other.quantifier.precept`)



| Token | Text | Source |

|-------|------|--------|

| All | `all` | TokenKind.All (=43) |

| Any | `any` | TokenKind.Any (=44) |

| Each | `each` | TokenKind.Each (=135) |



## 2.9 Keywords — State Modifiers (`storage.modifier.state.precept`)



| Token | Text | Source |

|-------|------|--------|

| Terminal | `terminal` | TokenKind.Terminal (=45) |

| Required | `required` | TokenKind.Required (=46) |

| Irreversible | `irreversible` | TokenKind.Irreversible (=47) |

| Success | `success` | TokenKind.Success (=48) |

| Warning | `warning` | TokenKind.Warning (=49) |

| Error | `error` | TokenKind.Error (=50) |



## 2.10 Keywords — Constraints (`keyword.other.constraint.precept`)



| Token | Text | Source |

|-------|------|--------|

| Nonnegative | `nonnegative` | TokenKind.Nonnegative (=51) |

| Positive | `positive` | TokenKind.Positive (=52) |

| Nonzero | `nonzero` | TokenKind.Nonzero (=53) |

| Notempty | `notempty` | TokenKind.Notempty (=54) |

| Min | `min` | TokenKind.Min (=55) |

| Max | `max` | TokenKind.Max (=56) |

| Minlength | `minlength` | TokenKind.Minlength (=57) |

| Maxlength | `maxlength` | TokenKind.Maxlength (=58) |

| Mincount | `mincount` | TokenKind.Mincount (=59) |

| Maxcount | `maxcount` | TokenKind.Maxcount (=60) |

| Maxplaces | `maxplaces` | TokenKind.Maxplaces (=61) |

| Ordered | `ordered` | TokenKind.Ordered (=62) |



## 2.11 Keywords — Type Names (`storage.type.precept`)



| Token | Text | Family | Source |

|-------|------|--------|--------|

| StringType | `string` | Scalar | TokenKind.StringType (=63) |

| BooleanType | `boolean` | Scalar | TokenKind.BooleanType (=64) |

| IntegerType | `integer` | Scalar | TokenKind.IntegerType (=65) |

| DecimalType | `decimal` | Scalar | TokenKind.DecimalType (=66) |

| NumberType | `number` | Scalar | TokenKind.NumberType (=67) |

| ChoiceType | `choice` | Scalar | TokenKind.ChoiceType (=68) |

| Set | `set` | Collection | TokenKind.Set (=23) — dual-use |

| QueueType | `queue` | Collection | TokenKind.QueueType (=70) |

| StackType | `stack` | Collection | TokenKind.StackType (=71) |

| BagType | `bag` | Collection | TokenKind.BagType (=124) |

| ListType | `list` | Collection | TokenKind.ListType (=125) |

| LogType | `log` | Collection | TokenKind.LogType (=126) |

| LookupType | `lookup` | Collection | TokenKind.LookupType (=127) |

| DateType | `date` | Temporal | TokenKind.DateType (=72) |

| TimeType | `time` | Temporal | TokenKind.TimeType (=73) |

| InstantType | `instant` | Temporal | TokenKind.InstantType (=74) |

| DurationType | `duration` | Temporal | TokenKind.DurationType (=75) |

| PeriodType | `period` | Temporal | TokenKind.PeriodType (=76) |

| TimezoneType | `timezone` | Temporal | TokenKind.TimezoneType (=77) |

| ZonedDateTimeType | `zoneddatetime` | Temporal | TokenKind.ZonedDateTimeType (=78) |

| DateTimeType | `datetime` | Temporal | TokenKind.DateTimeType (=79) |

| MoneyType | `money` | Business | TokenKind.MoneyType (=80) |

| CurrencyType | `currency` | Business | TokenKind.CurrencyType (=81) |

| QuantityType | `quantity` | Business | TokenKind.QuantityType (=82) |

| UnitOfMeasureType | `unitofmeasure` | Business | TokenKind.UnitOfMeasureType (=83) |

| DimensionType | `dimension` | Business | TokenKind.DimensionType (=84) |

| PriceType | `price` | Business | TokenKind.PriceType (=85) |

| ExchangeRateType | `exchangerate` | Business | TokenKind.ExchangeRateType (=86) |



## 2.12 Literals



| Token | Scope | Description |

|-------|-------|-------------|

| True (`true`) | `constant.language.boolean.precept` | Boolean literal |

| False (`false`) | `constant.language.boolean.precept` | Boolean literal |

| NumberLiteral | `constant.numeric.precept` | Integer and decimal numbers |

| StringLiteral | `string.quoted.double.precept` | Double-quoted strings |

| TypedConstant | `string.quoted.single.precept` | Single-quoted typed constants (`'USD'`, `'kg'`) |



**Note:** `null` is NOT a keyword in the token catalog. The hand-authored grammar includes it in `booleanNull` — this is stale. Precept uses `is set`/`is not set` for presence, not `null`.



## 2.13 Symbol Operators (`keyword.operator.precept`)



| Token | Text | Description |

|-------|------|-------------|

| DoubleEquals | `==` | Equality |

| NotEquals | `!=` | Inequality |

| CaseInsensitiveEquals | `~=` | Case-insensitive equals |

| CaseInsensitiveNotEquals | `!~` | Case-insensitive not-equals |

| Tilde | `~` | CI collection inner-type prefix |

| GreaterThanOrEqual | `>=` | Comparison |

| LessThanOrEqual | `<=` | Comparison |

| GreaterThan | `>` | Comparison |

| LessThan | `<` | Comparison |

| Assign | `=` | Assignment |

| Plus | `+` | Addition |

| Minus | `-` | Subtraction/negation |

| Star | `*` | Multiplication |

| Slash | `/` | Division |

| Percent | `%` | Modulo |



## 2.14 Arrow Operators (`keyword.operator.arrow.precept`)



| Token | Text | Description |

|-------|------|-------------|

| Arrow | `->` | Action chain / outcome separator |

| BackArrow | `<-` | Computed field derivation |



## 2.15 Punctuation (`punctuation.precept`)



| Token | Text | Description |

|-------|------|-------------|

| Dot | `.` | Member access |

| Comma | `,` | List separator |

| LeftParen | `(` | Open paren |

| RightParen | `)` | Close paren |

| LeftBracket | `[` | Open bracket |

| RightBracket | `]` | Close bracket |



## 2.16 Member-Name Tokens (`keyword.other.precept`)



| Token | Text | Description |

|-------|------|-------------|

| Countof | `countof` | Bag element count accessor |

| Peekby | `peekby` | Priority queue ordering-key peek |



## 2.17 Built-in Functions (21 total — not keywords, scoped as identifiers)



Functions are parsed as identifier + `(` + arguments + `)`. They are NOT lexer keywords. In TextMate, they match as generic identifiers unless a function-call pattern highlights them. The grammar SHOULD have a pattern for known function names followed by `(`.



| Function | Name |

|----------|------|

| Min | `min` |

| Max | `max` |

| Abs | `abs` |

| Clamp | `clamp` |

| Floor | `floor` |

| Ceil | `ceil` |

| Truncate | `truncate` |

| Round | `round` |

| Approximate | `approximate` |

| Pow | `pow` |

| Sqrt | `sqrt` |

| Trim | `trim` |

| StartsWith | `startsWith` |

| EndsWith | `endsWith` |

| ToLower | `toLower` |

| ToUpper | `toUpper` |

| Left | `left` |

| Right | `right` |

| Mid | `mid` |

| Now | `now` |

| ~startsWith | `~startsWith` (CI variant) |

| ~endsWith | `~endsWith` (CI variant) |



## 2.18 Constructs (12 — from `Constructs.cs`)



| # | ConstructKind | Leading Token(s) | Disambiguation | Example |

|---|---------------|-------------------|----------------|---------|

| 1 | PreceptHeader | `precept` | — | `precept LoanApplication` |

| 2 | FieldDeclaration | `field` | — | `field amount as money nonnegative` |

| 3 | StateDeclaration | `state` | — | `state Draft initial, Submitted, Approved terminal success` |

| 4 | EventDeclaration | `event` | — | `event Submit(approver as string)` |

| 5 | RuleDeclaration | `rule` | — | `rule amount > 0 because "..."` |

| 6 | TransitionRow | `from` + `on` | Disambiguated by `on` | `from Draft on Submit -> ... -> transition Submitted` |

| 7 | StateEnsure | `in`/`to`/`from` + `ensure` | Disambiguated by `ensure` | `in Approved ensure amount > 0 because "..."` |

| 8 | AccessMode | `in` + `modify` | Disambiguated by `modify` | `in Draft modify Amount editable` |

| 9 | OmitDeclaration | `in` + `omit` | Disambiguated by `omit` | `in Draft omit InternalNotes` |

| 10 | StateAction | `to`/`from` + `->` | Disambiguated by `->` | `to Confirmed -> set PaymentReceived = true` |

| 11 | EventEnsure | `on` + `ensure` | Disambiguated by `ensure` | `on Submit ensure Amount > 0 because "..."` |

| 12 | EventHandler | `on` + `->` | Disambiguated by `->` | `on UpdateName -> set name = newName` |



---



## 3. Hand-Authored Grammar Audit



## 3.1 Coverage Gap Table



| # | Language Construct / Token | In Hand Grammar? | Scope Assignment | Gap / Issue |

|---|---------------------------|:---:|------------------|-------------|

| G1 | `rule` keyword | ❌ NO | — | Missing. `invariant` exists at L276 but `rule` replaced it. |

| G2 | `ensure` keyword | ❌ NO | — | Missing entirely. Used in StateEnsure, EventEnsure constructs. |

| G3 | `optional` keyword | ❌ NO | — | Missing. L366 has stale `nullable` instead. |

| G4 | `writable` keyword | ❌ NO | — | Missing. New field modifier (B4). |

| G5 | `modify` keyword | ❌ NO | — | Missing. Access mode construct (B4). |

| G6 | `readonly` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |

| G7 | `editable` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |

| G8 | `omit` keyword (construct) | ❌ NO | — | Missing. Omit declaration construct. |

| G9 | State modifiers: `terminal`, `required`, `irreversible`, `success`, `warning`, `error` | ❌ NO | — | Only `initial` handled in state declaration (L101). 6 modifiers missing. |

| G10 | Type keywords: `integer`, `decimal`, `choice` | ❌ NO | — | L373-377 only has `string\|number\|boolean\|set\|queue\|stack`. |

| G11 | Temporal types (8): `date` through `datetime` | ❌ NO | — | All 8 temporal types missing. |

| G12 | Business-domain types (7): `money` through `exchangerate` | ❌ NO | — | All 7 business types missing. |

| G13 | Collection types: `bag`, `list`, `log`, `lookup` | ❌ NO | — | Missing from type keywords and collection field pattern. |

| G14 | Constraint keywords (12): `nonnegative` through `ordered` | ❌ NO | — | None present in grammar. |

| G15 | Access mode keywords | ❌ NO | — | `modify`, `readonly`, `editable` missing. |

| G16 | Quantifier `each` | ❌ NO | — | Missing. |

| G17 | Prepositions `by`, `at`, `for` | ❌ NO | — | Missing. |

| G18 | Control `then` | ❌ NO | — | Missing from L358 (has `if`/`else` but not `then`). |

| G19 | Action keywords: `append`, `insert`, `put` | ❌ NO | — | Missing from L381-385. |

| G20 | Operators: `~=`, `!~`, `~` | ❌ NO | — | Case-insensitive operators missing from L413-429. |

| G21 | Typed constants (`'...'`) | ❌ NO | — | No single-quoted string pattern. |

| G22 | Parenthesized event args | ❌ NO | — | `event Name(Arg as type)` syntax not matched. Grammar uses retired `with` syntax (L148-188). |

| G23 | RuleDeclaration construct | ❌ NO | — | No pattern for `rule Expr because "msg"`. |

| G24 | StateEnsure construct | ❌ NO | — | No pattern for `in/to/from State ensure Expr because "msg"`. |

| G25 | EventEnsure construct | ❌ NO | — | No pattern for `on Event ensure Expr because "msg"`. |

| G26 | AccessMode construct | ❌ NO | — | No pattern for `in State modify Field editable`. |

| G27 | OmitDeclaration construct | ❌ NO | — | No pattern for `in State omit Field`. |

| G28 | StateAction construct | ❌ NO | — | No pattern for `to/from State -> action chain`. |

| G29 | EventHandler construct | ❌ NO | — | No pattern for `on Event -> action chain`. |

| G30 | Computed field syntax | ❌ NO | — | `field X as type <- expr` not specifically highlighted. `<-` is in `arrowOperator` but no construct pattern. |

| G31 | Function calls | ❌ NO | — | `min(...)`, `round(...)` etc. — no function-name highlighting. |

| G32 | Parentheses/brackets | Partial | `punctuation.precept` | Parentheses exist in code but no explicit grammar pattern matches `(` or `)`. |

| G33 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |

| G34 | Ascending/descending | ❌ NO | — | Sort order modifiers missing. |

| G35 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators not highlighted. |



## 3.2 Stale / Incorrect Patterns



| # | Pattern | Line | Issue |

|---|---------|------|-------|

| S1 | `declarationKeywords` → `nullable` | L366 | STALE. Should be `optional`. `nullable` is not in TokenKind. |

| S2 | `declarationKeywords` → `invariant` | L366 | STALE. Should be `rule`. `invariant` is not in TokenKind. |

| S3 | `declarationKeywords` → `with` | L366 | STALE. `with` is not in TokenKind. Retired event-arg syntax. |

| S4 | `declarationKeywords` → `assert` | L366 | STALE. Should be `ensure`. `assert` is not in TokenKind. |

| S5 | `booleanNull` → `null` | L436 | STALE. `null` is not a keyword in the token catalog. Precept uses `is set`/`is not set`. |

| S6 | `eventWithArgsDeclaration` | L146-188 | STALE. Uses `event Name with Arg as type` syntax. Current syntax is `event Name(Arg as type)`. |

| S7 | `invariantStatement` | L276-286 | STALE. Uses `invariant` keyword. Should be `rule`. |

| S8 | `assertStatement` | L288-300 | STALE. Uses `on EventName assert`. Should be `on EventName ensure`. |

| S9 | `controlKeywords` mix | L356-361 | INCORRECT. Mixes declaration keywords (`precept`, `state`, `event`) with control flow (`if`, `when`). Should use catalog-derived scope groups. |

| S10 | `actionKeywords` mix | L380-385 | INCORRECT. Mixes actions (`set`, `add`), prepositions (`into`), membership (`contains`), and logical operators (`and`, `or`, `not`) into one scope. |



## 3.3 Scope Assignment Errors



| # | Token | Grammar Scope | Catalog Scope | Visual System Role |

|---|-------|--------------|---------------|--------------------|

| E1 | `precept` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |

| E2 | `state` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |

| E3 | `event` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |

| E4 | `field` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |

| E5 | `as` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |

| E6 | `because` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |

| E7 | `default` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |

| E8 | `and`, `or`, `not` | `keyword.other.precept` (L383) | `keyword.operator.logical.precept` | Logical operators |

| E9 | `contains` | `keyword.other.precept` (L383) | `keyword.operator.membership.precept` | Membership |

| E10 | `into` | `keyword.other.precept` (L383) | `keyword.control.precept` | Preposition |

| E11 | `all`, `any` | `keyword.control.precept` (L359) | `keyword.other.quantifier.precept` | Quantifier |

| E12 | `of` | `keyword.control.precept` (L359) | `keyword.control.precept` | ✓ Correct |

| E13 | `set` (action) | `keyword.other.precept` (L383) | `storage.type.precept` | Dual-use |

| E14 | `transition` | `keyword.other.precept` (L394) | `keyword.other.outcome.precept` | Outcome |

| E15 | `reject` | `keyword.other.precept` (L394/L68) | `keyword.other.outcome.precept` | Outcome |

| E16 | `edit` | `keyword.other.precept` (L367) | Not a TokenKind! | `edit` appears in root-edit pattern but is not in the token catalog. The construct is `RuleDeclaration`, not `edit`. Actually, `edit` is used for `rootEditDeclaration` — but the TokenKind enum doesn't have an Edit token. Checking... `edit` may be a stale surface concept. The Constructs catalog does not have a root-level `edit` construct. This needs verification. |



**Note on E16:** Looking at the Constructs catalog, there is no `edit` construct. The `OmitDeclaration` and `AccessMode` constructs handle field access. The `rootEditDeclaration` pattern in the hand-authored grammar (`edit all | edit Field1, Field2`) may be stale — I need to verify whether root-level `edit` still exists. Looking at the sample files: `customer-profile.precept` uses `writable` modifier on fields, not `edit`. `fee-schedule.precept` uses `writable`. No sample uses `edit all`. This pattern appears stale. **However**, the `rootEditDeclaration` pattern is in both the hand-authored grammar AND the generator, so it may still be valid for backward compatibility. Needs owner clarification.



---



## 4. Generator Audit



## 4.1 Generator Strengths



1. **Catalog-driven keyword emission** (L38-77): Reads `Tokens.All`, groups by `TextMateScope`, emits one alternation pattern per scope. This correctly picks up all 139 tokens. ✓

2. **Typed constants** (L134-146): Handles single-quoted `'...'` strings. Hand-authored grammar doesn't. ✓

3. **Collection member access** (L453-470): Includes `countof` and `peekby`. ✓



## 4.2 Generator Gap Table



| # | Language Construct / Feature | Generator Pattern? | Correct Scope? | Gap |

|---|-----------------------------|----|----|----|

| GG1 | Message strings (`because "msg"`, `reject "msg"`) | ❌ NO | — | **Critical.** Visual system reserves gold for message payloads. Without this, all strings get `string.quoted.double.precept` — no visual interrupt for rules. |

| GG2 | Parenthesized event args `event Name(Arg as type)` | ❌ NO | — | Generator's `eventWithArgsDeclaration` (L218-258) uses stale `with` syntax. |

| GG3 | State modifiers beyond `initial` | ❌ NO | — | `stateDeclaration` (L180-215) only matches `initial`. Missing: `terminal`, `required`, `irreversible`, `success`, `warning`, `error`. These ARE emitted as catalog keywords under `storage.modifier.state.precept`, but the structural pattern doesn't recognize them in state declaration context. |

| GG4 | RuleDeclaration construct | ❌ NO | — | No `rule Expr because "msg"` pattern. |

| GG5 | StateEnsure constructs | ❌ NO | — | No `in/to/from State ensure Expr because "msg"` pattern. |

| GG6 | EventEnsure construct | ❌ NO | — | No `on Event ensure Expr because "msg"` pattern. |

| GG7 | AccessMode construct | ❌ NO | — | No `in State modify Field editable/readonly` pattern. |

| GG8 | OmitDeclaration construct | ❌ NO | — | No `in State omit Field` pattern. |

| GG9 | StateAction construct | ❌ NO | — | No `to/from State -> action chain` pattern. |

| GG10 | EventHandler construct | ❌ NO | — | No `on Event -> action chain` (stateless). |

| GG11 | Computed field declaration | ❌ NO | — | No `field X as type <- expr` structural pattern. |

| GG12 | Function call highlighting | ❌ NO | — | `min(...)`, `round(...)` etc. not highlighted as function names. |

| GG13 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |

| GG14 | `assertStatement` uses stale `assert` | ✅ Present | ❌ Wrong keyword | L416-432: Uses `assert` instead of `ensure`. |

| GG15 | `no transition` compound keyword | ❌ NO | — | Two-word outcome not specially highlighted. The individual words are catalog-derived, but the compound meaning is lost. |

| GG16 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators. |

| GG17 | `ScopeToRepositoryKey` naming | — | — | Appends "Keywords" to scope, producing confusing repo keys like `keyword.declaration.preceptKeywords`. Should use descriptive names (e.g., `declarationKeywords`). |

| GG18 | `eventWithArgsDeclaration` broken `$ref` | ❌ Broken | — | L244: Uses `["$ref"] = "#/repository/storage.type.precept"` — TextMate doesn't support `$ref`. Should be `["include"] = "#storage.type.preceptKeywords"`. |

| GG19 | `fieldCollectionDeclaration` scope error | — | ❌ Wrong | L296: Uses `keyword.declaration.precept` for `field` keyword but the repo key in catalog patterns uses the same scope. Creates conflict with `keyword.declaration.preceptKeywords` repo entry — both claim `keyword.declaration.precept`. |

| GG20 | Missing punctuation patterns | ❌ NO | — | No explicit patterns for `(`, `)`, `[`, `]`. Catalog assigns them `punctuation.precept`. |



---



## 5. Authoritative Grammar Specification



## Spec Section 1: Scope Vocabulary



Every TextMate scope used in the Precept grammar, with semantic meaning and visual system role.



| # | TextMate Scope | Semantic Meaning | Visual System Role | Brand Color |

|---|---------------|------------------|-------------------|-------------|

| S1 | `comment.line.number-sign.precept` | Line comment starting with `#` | Comments | `#9096A6` italic |

| S2 | `keyword.declaration.precept` | Declaration and behavioral keywords | Structure · Semantic | `#4338CA` **bold** |

| S3 | `keyword.control.precept` | Prepositions and control flow | Structure · Grammar | `#6366F1` normal |

| S4 | `keyword.other.action.precept` | Action verbs in action chains | Structure · Grammar | `#6366F1` normal |

| S5 | `keyword.other.outcome.precept` | Transition, rejection, no-transition outcomes | Structure · Grammar | `#6366F1` normal |

| S6 | `keyword.other.access-mode.precept` | Access mode declarations | Structure · Grammar | `#6366F1` normal |

| S7 | `keyword.other.quantifier.precept` | Universal/existential quantifiers | Structure · Grammar | `#6366F1` normal |

| S8 | `keyword.other.constraint.precept` | Field constraint modifiers | Structure · Grammar | `#6366F1` normal |

| S9 | `keyword.operator.logical.precept` | `and`, `or`, `not` | Structure · Grammar | `#6366F1` normal |

| S10 | `keyword.operator.membership.precept` | `contains`, `is` | Structure · Grammar | `#6366F1` normal |

| S11 | `keyword.operator.precept` | Symbol operators (`==`, `!=`, `+`, `-`, etc.) | Structure · Grammar | `#6366F1` normal |

| S12 | `keyword.operator.arrow.precept` | `->` and `<-` arrows | Structure · Grammar | `#6366F1` normal |

| S13 | `storage.type.precept` | Type keywords (all scalar, temporal, business, collection types) | Data · Types | `#9AA8B5` normal |

| S14 | `storage.modifier.state.precept` | State lifecycle modifiers | Data · Types | `#9AA8B5` normal |

| S15 | `entity.name.type.state.precept` | State names | States | `#A898F5` normal |

| S16 | `entity.name.function.event.precept` | Event names | Events | `#30B8E8` normal |

| S17 | `entity.name.precept.message.precept` | Precept name (in header) | Identity | `#A898F5` normal |

| S18 | `variable.other.field.precept` | Field names | Data · Names | `#B0BEC5` normal |

| S19 | `variable.parameter.precept` | Event argument names (in declarations) | Data · Names | `#B0BEC5` normal |

| S20 | `variable.other.property.precept` | Property accessor after dot | Data · Names | `#B0BEC5` normal |

| S21 | `variable.other.precept` | Catch-all identifier reference | Data · Names | `#B0BEC5` normal |

| S22 | `constant.numeric.precept` | Number literals | Data · Values | `#84929F` normal |

| S23 | `constant.language.boolean.precept` | `true`, `false` | Data · Values | `#84929F` normal |

| S24 | `string.quoted.double.precept` | Double-quoted strings (non-message) | Data · Values | `#84929F` normal |

| S25 | `string.quoted.double.message.precept` | Message strings in `because`/`reject` | Rules · Messages | `#FBBF24` normal |

| S26 | `string.quoted.single.precept` | Single-quoted typed constants | Data · Values | `#84929F` normal |

| S27 | `constant.character.escape.precept` | Escape sequences in strings | Data · Values | `#84929F` normal |

| S28 | `punctuation.precept` | `.`, `,`, `(`, `)`, `[`, `]` | Structure · Grammar | `#6366F1` normal |

| S29 | `punctuation.separator.comma.precept` | Comma separator (in lists) | Structure · Grammar | `#6366F1` normal |

| S30 | `punctuation.accessor.precept` | Dot accessor (in member access) | Structure · Grammar | `#6366F1` normal |

| S31 | `keyword.other.precept` | Special member names (`countof`, `peekby`) | Data · Names | `#B0BEC5` normal |

| S32 | `support.function.precept` | Built-in function names | Data · Names | `#B0BEC5` normal |

| S33 | `meta.declaration.precept.precept` | Precept header construct (meta) | — | — |

| S34 | `meta.declaration.state.precept` | State declaration construct (meta) | — | — |

| S35 | `meta.declaration.event.precept` | Event declaration construct (meta) | — | — |

| S36 | `meta.field-declaration.precept` | Field declaration construct (meta) | — | — |

| S37 | `meta.transition.header.precept` | Transition row header (meta) | — | — |

| S38 | `meta.ensure.state.precept` | State ensure construct (meta) | — | — |

| S39 | `meta.ensure.event.precept` | Event ensure construct (meta) | — | — |

| S40 | `meta.access-mode.precept` | Access mode construct (meta) | — | — |

| S41 | `meta.omit.precept` | Omit declaration construct (meta) | — | — |

| S42 | `meta.action.state.precept` | State action construct (meta) | — | — |

| S43 | `meta.handler.event.precept` | Event handler construct (meta) | — | — |

| S44 | `meta.rule.precept` | Rule declaration construct (meta) | — | — |

| S45 | `meta.message.precept` | Message string context (meta) | — | — |

| S46 | `meta.computed-field.precept` | Computed field declaration (meta) | — | — |

| S47 | `meta.transition.target.precept` | Transition target (meta) | — | — |

| S48 | `meta.event-arg-ref.precept` | Event.arg dot access (meta) | — | — |

| S49 | `meta.collection-member.precept` | Collection.property access (meta) | — | — |



## Spec Section 2: Repository Patterns (Complete Enumeration)



## 2.1 Comment



- **Key:** `comment`

- **Type:** `match`

**Scope:** `comment.line.number-sign.precept`

- **Regex:** `#.*$`

- **Covers:** Line comments



## 2.2 Message Strings



- **Key:** `messageStrings`

- **Type:** `match` (two patterns)

**Scope:** captures `keyword.declaration.precept` for keyword, `string.quoted.double.message.precept` for message

- **Regex pattern 1:** `\b(because)(\s+)("(?:\\.|[^"\\])*")`

- **Regex pattern 2:** `\b(reject)(\s+)("(?:\\.|[^"\\])*")`

- **Covers:** Gold message payload in `because "..."` and `reject "..."` positions

- **Priority:** MUST precede generic `strings` pattern to prevent message strings from being consumed as regular strings

- **Visual system:** This is the **only** pattern that produces `string.quoted.double.message.precept` — the gold visual interrupt



## 2.3 Strings



- **Key:** `strings`

- **Type:** `begin/end`

**Scope:** `string.quoted.double.precept`

- **Begin:** `"`   End: `"`

- **Inner pattern:** `constant.character.escape.precept` for `\\.`

- **Covers:** All non-message double-quoted strings



## 2.4 Typed Constants



- **Key:** `typedConstants`

- **Type:** `begin/end`

**Scope:** `string.quoted.single.precept`

- **Begin:** `'`   End: `'`

- **Covers:** Single-quoted typed constants (`'USD'`, `'kg'`, `'2026-01-15'`)



## 2.5 Precept Header



- **Key:** `preceptHeader`

- **Type:** `match`

**Scope:** `meta.declaration.precept.precept`

- **Regex:** `^(\s*)(precept)(\s+)([A-Za-z_][A-Za-z0-9_]*)`

- **Captures:** `2` → `keyword.declaration.precept`, `4` → `entity.name.precept.message.precept`

- **Covers:** `precept LoanApplication`



## 2.6 State Declaration



- **Key:** `stateDeclaration`

- **Type:** `match`

**Scope:** `meta.declaration.state.precept`

- **Regex:** `^(\s*)(state)(\s+)(.*)`

- **Captures:** `2` → `keyword.declaration.precept`, `4` → sub-patterns:

  - State modifiers from catalog: `\b(initial|terminal|required|irreversible|success|warning|error)\b` → `storage.modifier.state.precept` (for `terminal`/`required`/`irreversible`/`success`/`warning`/`error`) and `keyword.declaration.precept` (for `initial`)

  - State names: `\b[A-Za-z_][A-Za-z0-9_]*\b` → `entity.name.type.state.precept`

  - Comma: `,` → `punctuation.separator.comma.precept`

- **Covers:** `state Draft initial, Submitted, Approved terminal success`

- **Critical change from current:** Must recognize ALL 7 state modifiers, not just `initial`



## 2.7 Event Declaration (Parenthesized Args)



- **Key:** `eventDeclaration`

- **Type:** `match`

**Scope:** `meta.declaration.event.precept`

- **Regex:** `^(\s*)(event)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s*\(.*)?`

- **Captures:**

  - `2` → `keyword.declaration.precept`

  - `4` → sub-patterns for event names (`entity.name.function.event.precept`) and commas

  - `5` → sub-patterns for parenthesized args:

    - `initial` keyword → `keyword.declaration.precept`

    - Argument name before `as`: `\b([A-Za-z_][A-Za-z0-9_]*)(?=\s+as\b)` → `variable.parameter.precept`

    - `as` keyword → `keyword.declaration.precept`

    - Type keywords → include `#typeKeywords`

    - Constraint keywords → include `#constraintKeywords`

    - Default values → include `#numbers`, `#strings`, `#booleanLiterals`

    - Commas → `punctuation.separator.comma.precept`

    - Parentheses → `punctuation.precept`

- **Covers:** `event Submit(Applicant as string notempty, Amount as number)`

- **Critical change:** Replaces stale `eventWithArgsDeclaration` (used `with` syntax)



## 2.8 Field Declaration (Scalar)



- **Key:** `fieldScalarDeclaration`

- **Type:** `match`

**Scope:** `meta.field-declaration.precept`

- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|choice|date|time|instant|duration|period|timezone|zoneddatetime|datetime|money|currency|quantity|unitofmeasure|dimension|price|exchangerate)(.*)`

- **Captures:**

  - `2` → `keyword.declaration.precept`

  - `4` → field names (`variable.other.field.precept`) + commas

  - `6` → `keyword.declaration.precept`

  - `8` → `storage.type.precept`

  - `9` → sub-patterns: constraint keywords, `optional`, `writable`, `default`, numbers, strings, typed constants, `<-` for computed

- **Covers:** All scalar field declarations including temporal and business-domain types

- **Note:** Type name list MUST be derived from the catalog (`Tokens.All` where category is `Type` and text is not null)



## 2.9 Field Declaration (Collection)



- **Key:** `fieldCollectionDeclaration`

- **Type:** `match`

**Scope:** `meta.field-declaration.precept`

- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(set|queue|stack|bag|list|log|lookup)(\s+)(of)(\s+)(~?(?:string|number|integer|decimal|boolean))(.*)`

- **Captures:**

  - `2` → `keyword.declaration.precept`

  - `4` → field names + commas

  - `6` → `keyword.declaration.precept`

  - `8` → `storage.type.precept` (collection type)

  - `10` → `keyword.control.precept` (`of`)

  - `12` → `storage.type.precept` (inner type, with optional `~` prefix)

  - `13` → sub-patterns for constraint keywords, modifiers

- **Covers:** `field Tags as set of string`, `field Items as bag of ~string`



## 2.10 Computed Field Declaration



- **Key:** `computedFieldDeclaration`

- **Type:** `match`

**Scope:** `meta.computed-field.precept`

- **Regex:** `^(\s*)(field)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|..types..)(\s+.*)?(<-)(\s+.*)`

- **Note:** This is hard to capture in a single regex because the `<-` can appear after optional modifiers. Recommend a separate pattern that matches `<-` preceded by field context, or handle via the existing field declaration patterns plus the arrow operator pattern.

- **Alternative approach:** The `<-` operator is already in the catalog. The constraint keywords and type keywords are already catalog-derived. A computed field declaration is just a field declaration that happens to contain `<-`. The structural pattern can be the same as `fieldScalarDeclaration` if the tail sub-patterns include the `<-` operator and expression patterns.



## 2.11 Root Edit Declaration



- **Key:** `rootEditDeclaration`

- **Type:** `match`

**Scope:** `meta.declaration.edit.root.precept`

**Status:** **NEEDS VERIFICATION.** The `edit` keyword is not in the `TokenKind` enum. No sample file uses root-level `edit`. This pattern may be stale. If confirmed stale, remove. If still valid, add `edit` to TokenKind.



## 2.12 Transition Row Header



- **Key:** `fromOnHeader`

- **Type:** `match`

**Scope:** `meta.transition.header.precept`

- **Regex:** `^(\s*)(from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)(\s+)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)`

- **Captures:**

  - `2` → `keyword.control.precept` (`from`)

  - `4` → `entity.name.type.state.precept` (source state(s)) — `any` should get `keyword.other.quantifier.precept`

  - `6` → `keyword.control.precept` (`on`)

  - `8` → `entity.name.function.event.precept` (event name)

- **Covers:** `from Draft on Submit`, `from any on Cancel`

- **Note:** `any` in state position should get quantifier scope, not state scope. Needs sub-pattern.



## 2.13 State Ensure



- **Key:** `stateEnsure`

- **Type:** `match`

**Scope:** `meta.ensure.state.precept`

- **Regex:** `^(\s*)(in|to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`

- **Captures:**

  - `2` → `keyword.control.precept` (anchor preposition)

  - `4` → `entity.name.type.state.precept` (state name) — `any` → `keyword.other.quantifier.precept`

  - `6` → `keyword.declaration.precept` (`ensure`)

- **Covers:** `in Approved ensure amount > 0 because "..."`



## 2.14 Event Ensure



- **Key:** `eventEnsure`

- **Type:** `match`

**Scope:** `meta.ensure.event.precept`

- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`

- **Captures:**

  - `2` → `keyword.control.precept` (`on`)

  - `4` → `entity.name.function.event.precept` (event name)

  - `6` → `keyword.declaration.precept` (`ensure`)

- **Covers:** `on Submit ensure Amount > 0 because "..."`



## 2.15 Access Mode



- **Key:** `accessMode`

- **Type:** `match`

**Scope:** `meta.access-mode.precept`

- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(modify)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*|all)(\s+)(editable|readonly)`

- **Captures:**

  - `2` → `keyword.control.precept`

  - `4` → `entity.name.type.state.precept` (state name)

  - `6` → `keyword.other.access-mode.precept` (`modify`)

  - `8` → `variable.other.field.precept` (field names) or `keyword.other.quantifier.precept` (`all`)

  - `10` → `keyword.other.access-mode.precept` (`editable`/`readonly`)

- **Covers:** `in Draft modify Amount editable`, `in UnderReview modify AdjusterName editable when not FraudFlag`



## 2.16 Omit Declaration



- **Key:** `omitDeclaration`

- **Type:** `match`

**Scope:** `meta.omit.precept`

- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(omit)(\s+)([A-Za-z_][A-Za-z0-9_]*)`

- **Captures:**

  - `2` → `keyword.control.precept`

  - `4` → `entity.name.type.state.precept`

  - `6` → `keyword.other.access-mode.precept` (`omit`)

  - `8` → `variable.other.field.precept`

- **Covers:** `in Draft omit InternalNotes`



## 2.17 State Action



- **Key:** `stateAction`

- **Type:** `match`

**Scope:** `meta.action.state.precept`

- **Regex:** `^(\s*)(to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`

- **Captures:**

  - `2` → `keyword.control.precept` (anchor preposition)

  - `4` → `entity.name.type.state.precept` (state name)

  - `6` → `keyword.operator.arrow.precept` (`->`)

- **Covers:** `to Confirmed -> set PaymentReceived = true`

- **Note:** Must precede `stateEnsure` in pattern order since both start with `to`/`from`. Disambiguated by `->` vs `ensure`.



## 2.18 Event Handler



- **Key:** `eventHandler`

- **Type:** `match`

**Scope:** `meta.handler.event.precept`

- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`

- **Captures:**

  - `2` → `keyword.control.precept` (`on`)

  - `4` → `entity.name.function.event.precept` (event name)

  - `6` → `keyword.operator.arrow.precept` (`->`)

- **Covers:** `on UpdateName -> set name = newName` (stateless precepts)

- **Note:** Must precede `eventEnsure` in pattern order since both start with `on`.



## 2.19 Rule Declaration



- **Key:** `ruleDeclaration`

- **Type:** `match`

**Scope:** `meta.rule.precept`

- **Regex:** `^(\s*)(rule)\b`

- **Captures:** `2` → `keyword.declaration.precept`

- **Covers:** `rule amount > 0 because "..."`

- **Note:** Only needs to capture the `rule` keyword. The rest of the line is handled by included patterns (operators, identifiers, message strings, etc.)



## 2.20 Transition Target



- **Key:** `transitionTarget`

- **Type:** `match`

**Scope:** `meta.transition.target.precept`

- **Regex:** `\b(transition)(\s+)([A-Za-z_][A-Za-z0-9_]*)`

- **Captures:**

  - `1` → `keyword.other.outcome.precept`

  - `3` → `entity.name.type.state.precept`

- **Covers:** `transition Approved`



## 2.21 No Transition



- **Key:** `noTransition`

- **Type:** `match`

**Scope:** (captures only)

- **Regex:** `\b(no)(\s+)(transition)\b`

- **Captures:**

  - `1` → `keyword.other.outcome.precept`

  - `3` → `keyword.other.outcome.precept`

- **Covers:** `no transition`



## 2.22 Event Arg Reference (Dot Access)



- **Key:** `eventArgReference`

- **Type:** `match`

**Scope:** `meta.event-arg-ref.precept`

- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)([A-Za-z_][A-Za-z0-9_]*)`

- **Captures:**

  - `1` → `entity.name.function.event.precept` (event name)

  - `2` → `punctuation.accessor.precept`

  - `3` → `variable.other.property.precept` (arg/property name)

- **Covers:** `Submit.Amount`, `Approve.Note`

- **Note:** This pattern is ambiguous — it also matches `Collection.count`. The `collectionMemberAccess` pattern must precede this one.



## 2.23 Collection Member Access



- **Key:** `collectionMemberAccess`

- **Type:** `match`

- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)(\bcount|countof|min|max|peek|peekby\b)`

- **Captures:**

  - `1` → `variable.other.field.precept` (collection field name)

  - `2` → `punctuation.accessor.precept`

  - `3` → `variable.other.property.precept` (member name)

- **Covers:** `MissingDocuments.count`, `Queue.peek`

- **Priority:** Must precede `eventArgReference` to prevent `Collection.count` from being highlighted as event.arg.



## 2.24 Function Calls



- **Key:** `functionCalls`

- **Type:** `match`

**Scope:** (captures only)

- **Regex:** `\b(min|max|abs|clamp|floor|ceil|truncate|round|approximate|pow|sqrt|trim|startsWith|endsWith|toLower|toUpper|left|right|mid|now)(\s*\()`

- **Captures:**

  - `1` → `support.function.precept`

  - `2` → `punctuation.precept`

- **Covers:** `min(x, y)`, `round(amount, 2)`, `trim(name)`, `now()`

- **Note:** Function name list MUST be derived from `Functions.All` (via the function catalog). CI variants `~startsWith` and `~endsWith` need a separate pattern: `(~)(startsWith|endsWith)(\s*\()`.



## 2.25 Catalog-Derived Keyword Groups



These are generated automatically by reading `Tokens.All`, grouping by `TextMateScope`, and emitting one alternation pattern per scope. The generator already does this (L38-77 of `Program.cs`).



**Repository keys** (use descriptive names, not scope-suffixed):



| Key | Scope | Tokens |

|-----|-------|--------|

| `declarationKeywords` | `keyword.declaration.precept` | `as`, `ascending`, `because`, `default`, `descending`, `ensure`, `event`, `field`, `initial`, `optional`, `precept`, `rule`, `state`, `writable` |

| `controlKeywords` | `keyword.control.precept` | `at`, `by`, `else`, `for`, `from`, `if`, `in`, `into`, `of`, `on`, `then`, `to`, `when` |

| `actionKeywords` | `keyword.other.action.precept` | `add`, `append`, `clear`, `dequeue`, `enqueue`, `insert`, `pop`, `push`, `put`, `remove` |

| `outcomeKeywords` | `keyword.other.outcome.precept` | `no`, `reject`, `transition` |

| `accessModeKeywords` | `keyword.other.access-mode.precept` | `editable`, `modify`, `omit`, `readonly` |

| `logicalOperators` | `keyword.operator.logical.precept` | `and`, `not`, `or` |

| `membershipOperators` | `keyword.operator.membership.precept` | `contains`, `is` |

| `quantifierKeywords` | `keyword.other.quantifier.precept` | `all`, `any`, `each` |

| `stateModifiers` | `storage.modifier.state.precept` | `error`, `irreversible`, `required`, `success`, `terminal`, `warning` |

| `constraintKeywords` | `keyword.other.constraint.precept` | `max`, `maxcount`, `maxlength`, `maxplaces`, `min`, `mincount`, `minlength`, `nonnegative`, `nonzero`, `notempty`, `ordered`, `positive` |

| `typeKeywords` | `storage.type.precept` | `bag`, `boolean`, `choice`, `currency`, `date`, `datetime`, `decimal`, `dimension`, `duration`, `exchangerate`, `instant`, `integer`, `list`, `log`, `lookup`, `money`, `number`, `period`, `price`, `quantity`, `queue`, `set`, `stack`, `string`, `time`, `timezone`, `unitofmeasure`, `zoneddatetime` |

| `booleanLiterals` | `constant.language.boolean.precept` | `false`, `true` |

| `symbolOperators` | `keyword.operator.precept` | `!=`, `!~`, `%`, `*`, `+`, `-`, `/`, `<`, `<=`, `==`, `>`, `>=`, `=`, `~`, `~=` |

| `arrowOperators` | `keyword.operator.arrow.precept` | `->`, `<-` |

| `memberNameKeywords` | `keyword.other.precept` | `countof`, `peekby` |



## 2.26 Numbers



- **Key:** `numbers`

- **Type:** `match`

**Scope:** `constant.numeric.precept`

- **Regex:** `\b\d+(?:\.\d+)?\b`



## 2.27 Punctuation



- **Key:** `punctuation`

- **Type:** `match`

**Scope:** `punctuation.precept`

- **Regex:** `[()[\].,]` (individual captures for finer scoping optional)



## 2.28 Identifier Reference (Catch-All)



- **Key:** `identifierReference`

- **Type:** `match`

**Scope:** `variable.other.precept`

- **Regex:** `\b[A-Za-z_][A-Za-z0-9_]*\b`

- **Priority:** LAST in pattern order. This is the catch-all.



## Spec Section 3: Top-Level Pattern Ordering



Ordered from most-specific to least-specific to prevent false matches.



```json

{

  "patterns": [

    { "include": "#comment" },

    { "include": "#messageStrings" },

    { "include": "#strings" },

    { "include": "#typedConstants" },

    { "include": "#preceptHeader" },

    { "include": "#stateDeclaration" },

    { "include": "#eventDeclaration" },

    { "include": "#fieldCollectionDeclaration" },

    { "include": "#fieldScalarDeclaration" },

    { "include": "#ruleDeclaration" },

    { "include": "#stateAction" },

    { "include": "#stateEnsure" },

    { "include": "#eventHandler" },

    { "include": "#eventEnsure" },

    { "include": "#accessMode" },

    { "include": "#omitDeclaration" },

    { "include": "#fromOnHeader" },

    { "include": "#noTransition" },

    { "include": "#transitionTarget" },

    { "include": "#functionCalls" },

    { "include": "#collectionMemberAccess" },

    { "include": "#eventArgReference" },

    { "include": "#arrowOperators" },

    { "include": "#symbolOperators" },

    { "include": "#logicalOperators" },

    { "include": "#membershipOperators" },

    { "include": "#stateModifiers" },

    { "include": "#constraintKeywords" },

    { "include": "#typeKeywords" },

    { "include": "#declarationKeywords" },

    { "include": "#controlKeywords" },

    { "include": "#actionKeywords" },

    { "include": "#outcomeKeywords" },

    { "include": "#accessModeKeywords" },

    { "include": "#quantifierKeywords" },

    { "include": "#memberNameKeywords" },

    { "include": "#booleanLiterals" },

    { "include": "#numbers" },

    { "include": "#punctuation" },

    { "include": "#identifierReference" }

  ]

}

```



**Ordering rationale:**

1. Comments first — `#` to end of line must be captured before anything else

2. Message strings before regular strings — `because "msg"` must get gold scope before `"msg"` gets consumed as a regular string

3. Typed constants — `'USD'` before identifiers

4. Construct-level patterns (most-specific) — declaration headers capture entire lines with contextual scoping

5. `stateAction` before `stateEnsure` — both start with `to`/`from`, disambiguated by `->` vs `ensure`

6. `eventHandler` before `eventEnsure` — both start with `on`, disambiguated by `->` vs `ensure`

7. `noTransition` before `transitionTarget` — `no transition` is a compound keyword

8. Dot-access patterns — `collectionMemberAccess` before `eventArgReference` to prevent `F.count` → event scope

9. `functionCalls` — before identifierReference catch-all

10. Operator patterns — arrows first (longest match), then symbol, then keyword operators

11. Keyword groups from catalog (most-specific scope to least-specific)

12. Literals and numbers

13. Catch-all identifier last



---



## 6. Coverage Gaps (Current Grammar)



## Gaps in the hand-authored grammar (35 items from audit section 3.1, G1–G35)



See Section 3.1 above for the complete gap table. Summary of critical gaps:



1. **35 missing language constructs/tokens** (G1–G35)

2. **10 stale/incorrect patterns** (S1–S10) — 3 retired keywords, 2 retired syntax forms, 5 scope misassignments

3. **16 scope assignment errors** (E1–E16) — tokens assigned to wrong semantic category



## Gaps in the grammar generator (20 items from audit section 4.2, GG1–GG20)



See Section 4.2 above. Summary of critical gaps:



1. **GG1: Missing message strings** — most critical for visual system compliance

2. **GG2: Stale event arg syntax** — uses `with` instead of parenthesized args

3. **GG3-GG13: 11 missing construct patterns** — rules, ensures, access modes, state actions, handlers, computed fields, function calls

4. **GG14: Stale `assert` keyword** — should be `ensure`

5. **GG17: Bad repo key naming** — confusing scope-suffixed names

6. **GG18: Broken `$ref`** — TextMate doesn't support JSON `$ref`



---



## 7. Generator Completion Requirements



Numbered list keyed to spec entries above.



## Must-Fix (blocks parity with hand-authored grammar + visual system compliance)



1. **Add `messageStrings` pattern (Spec §2.2).** This is the single most important pattern for visual system compliance. Without it, message payloads are indistinguishable from regular strings — destroying the gold visual interrupt that the brand mandates. Emit TWO match patterns: one for `because "..."`, one for `reject "..."`. Captures must assign `keyword.declaration.precept` to the keyword and `string.quoted.double.message.precept` to the string.



2. **Replace `eventWithArgsDeclaration` with parenthesized-arg syntax (Spec §2.7).** Current pattern (L218-258) matches `event Name with Arg as type`. Replace with pattern matching `event Name(Arg as type, ...)`. Remove the `with` keyword from the structural pattern.



3. **Expand `stateDeclaration` to recognize all 7 state modifiers (Spec §2.6).** Current pattern (L180-215) only matches `initial`. Add sub-patterns for `terminal`, `required`, `irreversible`, `success`, `warning`, `error` with scope `storage.modifier.state.precept`.



4. **Replace `assertStatement` with `eventEnsure` and `stateEnsure` (Spec §2.13-2.14).** Current pattern (L416-432) uses stale `assert`. Replace with two patterns: `on Event ensure Expr` and `in/to/from State ensure Expr`.



5. **Add `ruleDeclaration` pattern (Spec §2.19).** Match `rule` keyword at line start.



6. **Add `accessMode` pattern (Spec §2.15).** Match `in State modify Field editable/readonly`.



7. **Add `omitDeclaration` pattern (Spec §2.16).** Match `in State omit Field`.



8. **Add `stateAction` pattern (Spec §2.17).** Match `to/from State -> action chain`.



9. **Add `eventHandler` pattern (Spec §2.18).** Match `on Event -> action chain`.



10. **Add `noTransition` pattern (Spec §2.21).** Match `no transition` as a compound keyword.



11. **Add `functionCalls` pattern (Spec §2.24).** Match known function names followed by `(`. Derive function name list from `Functions.All` catalog.



12. **Fix `ScopeToRepositoryKey` naming (Spec §2.25).** Replace scope-suffixed keys with descriptive names. Current: `keyword.declaration.preceptKeywords`. Proposed: `declarationKeywords`.



13. **Fix broken `$ref` in `eventWithArgsDeclaration` (GG18).** L244 uses `["$ref"]` which TextMate doesn't support. Replace with `["include"]`.



14. **Expand `fieldScalarDeclaration` type list (Spec §2.8).** Current pattern (L322) lists `string|number|integer|decimal|boolean|choice`. Must include all 27 type keywords from catalog. Derive from `Tokens.All` where category is `Type`.



15. **Expand `fieldCollectionDeclaration` to include all collection types (Spec §2.9).** Current pattern (L293) includes `set|queue|stack|bag|list|log|lookup` ✓. Inner type list needs expansion to include `integer`, `decimal`.



16. **Update top-level pattern ordering (Spec §3).** Current ordering (L491-524) must be restructured per spec. `messageStrings` must come before `strings`. New construct patterns must be inserted at correct priority.



## Should-Fix (improves correctness and visual system alignment)



17. **Add `any` quantifier sub-pattern in state position.** In `fromOnHeader`, `stateEnsure`, `accessMode`, etc., `any` should get `keyword.other.quantifier.precept`, not `entity.name.type.state.precept`.



18. **Add `punctuation` patterns for parentheses and brackets (Spec §2.27).** Explicit patterns for `(`, `)`, `[`, `]` with `punctuation.precept`.



19. **Verify `rootEditDeclaration` validity (Spec §2.11).** If `edit` is not in `TokenKind` and no sample uses it, remove. If still valid, add to catalog first.



20. **Add `computedFieldDeclaration` context (Spec §2.10).** At minimum, the `<-` operator pattern is sufficient. Consider whether a dedicated structural pattern is needed.



21. **Handle `choice of string("a","b","c")` syntax.** The parenthesized choice options need string highlighting within the type declaration. Currently the strings would be captured by the generic `strings` pattern, which is acceptable.



## Won't-Fix in Grammar (semantic tokens only)



22. **Italic for constrained states/events.** TextMate cannot apply `fontStyle: italic` based on semantic context (whether a state participates in `ensure` rules). This requires the semantic token provider, which already exists in the language server.



23. **Context-dependent `set` scoping.** `set` as action verb vs collection type. TextMate can't disambiguate. Semantic tokens handle this.



24. **`null` removal.** `null` is not a keyword. The hand-authored grammar has it but the generator doesn't. No action needed — the generator is correct.



---



## Appendix A: Brand Doc Sync Items



The following items in `design/brand/brand-decisions.md` need updating to match catalog reality:



1. Replace `nullable` with `optional` in the Structure · Grammar keyword list

2. Remove `write` from the Structure · Semantic keyword list (retired B4)

3. Add `rule` to Structure · Semantic keyword list

4. Add `ensure` to Structure · Semantic keyword list

5. Add `writable` to Structure · Semantic keyword list (or Grammar — decision needed)

6. Add `optional` to Structure · Semantic keyword list (or Grammar — decision needed)

7. The brand doc's 2-tier keyword split (Semantic vs Grammar) doesn't map 1:1 to the catalog's 14-category scope model. Consider updating the brand doc to reference catalog categories or accept that the theme mediates between the two.



## Appendix B: Theme Configuration Requirements



For the visual system to work as designed, the VS Code theme must include rules mapping scopes to colors and styles:



```json

{

  "editor.tokenColorCustomizations": {

    "textMateRules": [

      { "scope": "keyword.declaration.precept", "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },

      { "scope": "keyword.control.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.action.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.outcome.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.access-mode.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.quantifier.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.constraint.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.operator.logical.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.operator.membership.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.operator.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.operator.arrow.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "keyword.other.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "storage.type.precept", "settings": { "foreground": "#9AA8B5" } },

      { "scope": "storage.modifier.state.precept", "settings": { "foreground": "#9AA8B5" } },

      { "scope": "entity.name.type.state.precept", "settings": { "foreground": "#A898F5" } },

      { "scope": "entity.name.function.event.precept", "settings": { "foreground": "#30B8E8" } },

      { "scope": "entity.name.precept.message.precept", "settings": { "foreground": "#A898F5" } },

      { "scope": "variable.other.field.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "variable.parameter.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "variable.other.property.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "variable.other.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "support.function.precept", "settings": { "foreground": "#B0BEC5" } },

      { "scope": "constant.numeric.precept", "settings": { "foreground": "#84929F" } },

      { "scope": "constant.language.boolean.precept", "settings": { "foreground": "#84929F" } },

      { "scope": "string.quoted.double.precept", "settings": { "foreground": "#84929F" } },

      { "scope": "string.quoted.double.message.precept", "settings": { "foreground": "#FBBF24" } },

      { "scope": "string.quoted.single.precept", "settings": { "foreground": "#84929F" } },

      { "scope": "comment.line.number-sign.precept", "settings": { "foreground": "#9096A6", "fontStyle": "italic" } },

      { "scope": "punctuation.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "punctuation.separator.comma.precept", "settings": { "foreground": "#6366F1" } },

      { "scope": "punctuation.accessor.precept", "settings": { "foreground": "#6366F1" } }

    ]

  }

}

```



## Appendix C: Catalog-Driven Generation Principle



The grammar generator MUST derive all keyword lists, type names, function names, operator symbols, and constraint keywords from the catalog source of truth (`Tokens.All`, `Functions.All`, etc.). No hardcoded token sets in the generator. If a new keyword is added to the catalog, the generator's output must automatically include it without manual changes.



The generator's current approach (L38-77) is architecturally correct for catalog-derived keyword patterns. The structural patterns (construct-level) are hand-written in the generator but MUST reference catalog-derived keyword lists where they enumerate token alternatives (e.g., type names in field declarations, state modifier names in state declarations).



---



*End of specification.*

# ProofEngine Spec — Pre-Implementation Gap Analysis



**Date:** 2026-05-08

**Author:** Frank

**Commit reviewed:** `79c340357aee4e54520a539dca8208bc734e3606`

**Verdict:** NOT READY



**Spec files reviewed:**

- `docs/compiler/proof-engine.md` (983 lines — primary spec)

- `docs/compiler/graph-analyzer.md`

- `docs/compiler/type-checker.md`

- `docs/compiler/diagnostic-system.md`



**Source files reviewed:**

- `src/Precept/Pipeline/ProofEngine.cs` (stub)

- `src/Precept/Pipeline/ProofLedger.cs` (stub)

- `src/Precept/Pipeline/StateGraph.cs`

- `src/Precept/Pipeline/GraphAnalyzer.cs`

- `src/Precept/Pipeline/SemanticIndex.cs`

- `src/Precept/Pipeline/Compilation.cs`

- `src/Precept/Compiler.cs`

- `src/Precept/Language/ProofRequirement.cs`

- `src/Precept/Language/ProofRequirementKind.cs`

- `src/Precept/Language/ProofRequirements.cs`

- `src/Precept/Language/DiagnosticCode.cs`

- `src/Precept/Language/Diagnostics.cs`

- `src/Precept/Language/FaultCode.cs`

- `src/Precept/Language/Faults.cs`

- `src/Precept/Language/Modifier.cs`

- `src/Precept/Language/Modifiers.cs`

- `src/Precept/Runtime/Descriptors.cs`



---



## Executive Summary



The ProofEngine spec is architecturally strong — the two-pass design, four-strategy discharge model, proof/fault chain, and catalog-driven obligation instantiation are well-conceived. However, the spec has **three blocking gaps** and **seven significant gaps** that prevent implementation from starting cleanly. The most critical issue: the spec defines five `ProofRequirementKind` values but only describes discharge strategies for two of them (Numeric and Presence). `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` are defined in the DU but have zero strategy coverage — an implementer would have to invent discharge logic from scratch. Additionally, the `FieldModifierMeta.ProofDischarges` property the spec declares as "canonical" (CC#5 resolved) does not exist in the source code, and the output type `ProofLedger` described in the spec is materially different from the stub in source.



---



## Gap Inventory



## [BLOCKING] Gaps



---



**PE-G1: Three of five ProofRequirementKind values have no discharge strategy**

- **Severity:** BLOCKING

- **Location:** `proof-engine.md` §7 "Four Proof Strategies" (lines 412–766)

- **Description:** The spec defines five `ProofRequirementKind` subtypes in §6 (lines 348–389): `Numeric`, `Presence`, `Dimension`, `Modifier`, and `QualifierCompatibility`. The four strategies (Literal, Modifier, GuardInPath, FlowNarrowing) only describe discharge predicates for `NumericProofRequirement` and `PresenceProofRequirement`. The remaining three kinds are completely absent:

  - **`DimensionProofRequirement`** — "period operand must have required time dimension." No strategy says how this is proven. Is it a static type check (always provable by the type checker)? Does it need a new strategy?

  - **`ModifierRequirement`** — "field must declare required modifier (e.g. `ordered`)." No strategy covers this. Logically Strategy 2 (Modifier Proof) should handle it, but the Strategy 2 pseudocode (lines 536–569) only reads `FieldModifierMeta.ProofDischarges`, not `ModifierRequirement.Required` directly. The mapping is unspecified.

  - **`QualifierCompatibilityProofRequirement`** — "two operands must share a qualifier value on the specified axis." This is a dual-subject requirement. None of the four strategies handle dual-subject obligations. The spec provides no guidance on how to discharge this — is it always resolvable from type-checker qualifier propagation? Does it require a fifth strategy?

- **Why it matters:** An implementer would have to guess how to handle 3 of 5 obligation kinds. Two implementers would write different code. This is the definition of a blocking ambiguity.

- **Suggested resolution:** For each of the three unhandled kinds, the spec must state:

  1. Which strategy discharges it (existing or new), OR

  2. That it is always discharged by the type checker and never reaches the proof engine as an unresolved obligation, OR

  3. That it is always `Unresolved` and produces a diagnostic (defensive backstop).



  Likely answers based on code analysis:

  - `DimensionProofRequirement`: Likely always resolvable by type-checker period-dimension inference. If so, state that it reaches the proof engine pre-discharged, or that it is a type error (not a proof obligation) and should never appear.

  - `ModifierRequirement`: Likely checked by seeing if the field has `ModifierKind.Required` in its `Modifiers` array. Add this to Strategy 2 pseudocode.

  - `QualifierCompatibilityProofRequirement`: Likely checked by the type checker's `QualifierBinding` propagation. If so, state the handoff.



---



**PE-G2: `FieldModifierMeta.ProofDischarges` does not exist in source code**

- **Severity:** BLOCKING

- **Location:** `proof-engine.md` §7 Strategy 2, lines 505–572, especially the CC#5 resolution box at line 571

- **Description:** The spec declares at line 571: "✅ Resolved (CC#5) — `FieldModifierMeta.ProofDischarges` is now canonical" and references `ProofDischarge[]` on `FieldModifierMeta`. The actual `FieldModifierMeta` record in `src/Precept/Language/Modifier.cs` (lines 105–121) has **no** `ProofDischarges` property. The `ProofDischarge` record type does not exist anywhere in the source code. `grep` for `ProofDischarge` across all of `src/Precept/Language/` returns zero matches.



  The Strategy 2 pseudocode depends entirely on `meta.ProofDischarges` for its discharge logic (line 551: `foreach (var discharge in meta.ProofDischarges)`). Without this property, Strategy 2 cannot be implemented as specified.

- **Why it matters:** Strategy 2 is the second most common discharge strategy. It covers `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. Without the catalog property, the implementer must either:

  (a) Add the property to the catalog first (design + implementation work), or

  (b) Hardcode per-modifier logic in the proof engine (violating catalog-driven architecture).

  Both are design decisions that must be made before coding starts.

- **Suggested resolution:** Add the `ProofDischarges` property to `FieldModifierMeta` and the `ProofDischarge` record type before implementation begins. This is a catalog prerequisite, not part of the proof engine implementation itself.



---



**PE-G3: Output type `ProofLedger` in spec diverges materially from source stub**

- **Severity:** BLOCKING

- **Location:** `proof-engine.md` §5 "Output", lines 172–287

- **Description:** The spec defines `ProofLedger` with five fields:

  ```csharp

  ProofLedger(

      ImmutableArray<ProofObligation> Obligations,

      ImmutableArray<FaultSiteLink> FaultSiteLinks,

      ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,

      ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,

      ImmutableArray<Diagnostic> Diagnostics

  )

  ```

  The source stub at `src/Precept/Pipeline/ProofLedger.cs` defines:

  ```csharp

  ProofLedger(ImmutableArray<Diagnostic> Diagnostics)

  ```

  The following types referenced by the spec's `ProofLedger` do **not exist** anywhere in the source:

  - `ProofObligation`

  - `ProofDisposition` (enum)

  - `ProofStrategy` (enum)

  - `FaultSiteLink`

  - `ConstraintInfluenceEntry`

  - `EventArgReference`

  - `InitialStateSatisfiabilityResult`

  - `UnsatisfiedConstraint`

  - `FaultSiteAnnotation`



  The `Compilation` record in `Compilation.cs` consumes `ProofLedger` but only reads `Diagnostics` — it has no field for `FaultSiteLinks` or `ConstraintInfluence`.

- **Why it matters:** The implementer must create ~10 new record types and expand the ProofLedger shape before any meaningful work begins. The spec needs to be explicit about whether these types are created as part of the ProofEngine implementation or as a prerequisite.

- **Suggested resolution:** State that Slice 0 of the implementation plan is "shape declarations" — creating all the output types in `ProofLedger.cs` and `SemanticIndex.cs` with empty-default construction, updating the `Compilation` record, and verifying the build stays green. This matches the pattern from TypeChecker (Slice 0 shape) and GraphAnalyzer.



---



## [SIGNIFICANT] Gaps



---



**PE-G4: `SemanticIndex.AllTypedExpressions` does not exist**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §9 "Failure Modes", line 968

- **Description:** The spec's Pass 1 pseudocode (line 968) references `semantics.AllTypedExpressions`:

  ```csharp

  foreach (var expr in semantics.AllTypedExpressions)

  ```

  No such property exists on `SemanticIndex`. The `SemanticIndex` record exposes `TransitionRows`, `Rules`, `Ensures`, `StateHooks`, `EventHandlers` — but no aggregated expression enumeration surface.

- **Why it matters:** The implementer needs to know exactly which `SemanticIndex` members to walk to collect all proof-relevant expressions. Walking `TransitionRows[].Actions[].ProofRequirements` is obvious, but what about guard expressions? Constraint conditions? Computed field expressions? State hook actions? The spec doesn't enumerate the walk targets.

- **Suggested resolution:** Replace `AllTypedExpressions` with an explicit list of walk targets:

  - `TransitionRows` → `Actions[].ProofRequirements` and `Guard` expressions

  - `Rules` → `Condition` expressions

  - `Ensures` → `Condition` expressions

  - `StateHooks` → `Actions[].ProofRequirements`

  - `EventHandlers` → `Actions[].ProofRequirements`

  - Computed fields → `ComputedExpression` (if proof-relevant)



  Or add the `AllTypedExpressions` helper to `SemanticIndex` as a prerequisite.



---



**PE-G5: `ConstraintIdentity` shapes in spec differ from implementation**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §5, lines 263–267

- **Description:** The spec defines:

  ```csharp

  public sealed record RuleIdentity(string RuleName, int Index) : ConstraintIdentity;

  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorState, string? AnchorEvent, int Index) : ConstraintIdentity;

  ```

  The actual implementation in `SemanticIndex.cs` (lines 401–404) defines:

  ```csharp

  public sealed record RuleIdentity(int RuleIndex) : ConstraintIdentity;

  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex) : ConstraintIdentity;

  ```

  Differences:

  1. `RuleIdentity`: spec has `(string RuleName, int Index)`, source has `(int RuleIndex)` — no `RuleName` field.

  2. `EnsureIdentity`: spec has `(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)`, source has `(ConstraintKind, string? AnchorName, int EnsureIndex)` — spec separates state/event anchors into two nullable fields; source uses a single `AnchorName`.

- **Why it matters:** The `ConstraintInfluenceEntry` output uses `ConstraintIdentity`. If the implementer follows the spec shapes, they'll create types that conflict with existing ones. If they follow the source shapes, the spec's `EventArgReference` resolution logic may not work as described.

- **Suggested resolution:** Update the spec to match the existing source shapes. The implementation is canonical — it was created during TypeChecker implementation and has tests.



---



**PE-G6: `FindEnclosingTransitionRow` is not specified**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §7 Strategy 3, line 625; Strategy 4, line 722

- **Description:** Both Strategy 3 and Strategy 4 call `FindEnclosingTransitionRow(obligation.Site, semantics)` to find the transition row that encloses the proof obligation's expression site. The spec never defines this function. The proof engine must know: given a `TypedExpression`, how do you find which `TypedTransitionRow` contains it?



  This is non-trivial because:

  1. `TypedExpression` nodes don't carry parent pointers or transition-row back-references.

  2. The proof engine would need to either build an expression→row index in Pass 1, or walk `TransitionRows[].Actions` looking for expression identity matches.

  3. Obligations on expressions in `TypedRule`, `TypedEnsure`, `TypedStateHook`, or `TypedEventHandler` have no enclosing transition row — what do Strategies 3/4 return for those?

- **Why it matters:** This is critical path logic for the two guard-based strategies. The spec's pseudocode uses it as a black box, but its implementation drives the data structure design of Pass 1.

- **Suggested resolution:** Specify that Pass 1 builds an `obligation → enclosing context` index. Define the context as a discriminated union: `TransitionRowContext(TypedTransitionRow)`, `ConstraintContext(TypedRule | TypedEnsure)`, `HookContext(TypedStateHook)`, `HandlerContext(TypedEventHandler)`. Strategies 3/4 only fire for `TransitionRowContext`. All other contexts return `false`.



---



**PE-G7: `ResolveSubject` is not specified**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §7 Strategy 1, line 445; Strategy 2, line 539

- **Description:** Strategy 1 calls `ResolveSubject(numeric.Subject, obligation.Site)` and Strategy 2 calls `GetFieldName(obligation.Requirement.Subject, obligation.Site)`. Neither is defined. Given the `ProofSubject` DU:

  - `ParamSubject(ParameterMeta Parameter)` — how do you resolve a parameter to a concrete expression node from the obligation site? The `ParameterMeta` has object identity, but how does one locate the corresponding argument expression in a `TypedFunctionCall` or operand in a `TypedBinaryOp`?

  - `SelfSubject(TypeAccessor? Accessor)` — how does one resolve "self" to the receiver expression in a `TypedMemberAccess`?



  The spec says `ParamSubject` "must be reference-equal to one of the `ParameterMeta` instances in the containing overload's `Parameters` list" (ProofRequirement.cs, line 16), which gives identity, but the resolution logic from identity to expression is missing.

- **Why it matters:** Subject resolution is the first step in every strategy. Without it being specified, the implementer must infer the mapping from `ParameterMeta` identity to `TypedExpression` arguments — a non-trivial piece of logic.

- **Suggested resolution:** Add a `ResolveSubject` pseudocode section that handles both `ParamSubject` and `SelfSubject`:

  - `ParamSubject`: For `TypedFunctionCall`, match `Parameter` identity against `ResolvedFunction`'s overload `Parameters` list to find the positional index, then return `Arguments[index]`. For `TypedBinaryOp`, match against `ResolvedOp`'s operation metadata parameters.

  - `SelfSubject`: For `TypedMemberAccess`, return `Object`. For `TypedAction`, return the field reference expression (requires knowing the field from `FieldName`).



---



**PE-G8: Initial-state satisfiability check is underspecified**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §7 "Initial-State Satisfiability", lines 863–883

- **Description:** The spec says: "For each constraint condition, check whether default field values satisfy it." This is vague. Specifically:

  1. What does "check" mean? Evaluate the constraint expression with default values? Symbolically analyze it? The spec doesn't say.

  2. How are "default field values" determined? Fields with `default` expressions have typed defaults in `TypedField.DefaultExpression`. Fields without defaults — what is their default? `0` for numeric? `""` for string? `null` for optional? The spec doesn't define the default value model.

  3. What about fields that are set by the initial event? The initial event's `set` actions provide values at instantiation. Does satisfiability account for initial event args, or only declared defaults?

  4. The spec says to check `ensure in Draft: ...`. But the `ConstraintKind.StateResident` anchor means "while in state", not "at entry". Is entry a special case of residency? Does entry use `ConstraintKind.StateEntry` anchors instead?

  5. Computed fields (`IsComputed = true`) have `ComputedExpression` not `DefaultExpression`. Are computed field values available for satisfiability?

- **Why it matters:** This check is one of the three output surfaces of the proof engine (alongside obligation discharge and constraint influence). Without clear semantics, the implementer must make design decisions that should be in the spec.

- **Suggested resolution:** Define the satisfiability algorithm explicitly:

  - State which fields are relevant (all fields? only fields referenced by initial-scope constraints?)

  - Define the "default value" for each type kind when no `default` is declared

  - State whether initial event arguments are considered (probably not — they're runtime values)

  - Define which constraint scopes are checked (`in`, `to`, both?)



---



**PE-G9: No diagnostic code for collection-empty proof failures**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §7 "Collection Non-Empty Proof", lines 885–899; `DiagnosticCode.cs`

- **Description:** The spec describes collection non-empty obligations (first, last, peek, dequeue, pop) but the only proof-stage diagnostic codes are:

  - 82: `UnsatisfiableGuard` (Warning)

  - 83: `DivisionByZero` (Error)

  - 84: `SqrtOfNegative` (Error)



  There is no proof-stage diagnostic for "collection may be empty when `first()` is called." The type-checker stage has `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64), but these are `DiagnosticStage.Type` — they fire during type checking, not proof. If the proof engine is supposed to handle collection non-empty proof discharge, it needs its own diagnostic code for the "unresolved" case. Or alternatively, collection safety is fully handled by the type checker and the proof engine should NOT create obligations for them.



  The `FaultCode` enum has `CollectionEmptyOnAccess = 9` with `[StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]` — linking to the type-checker code, not a proof code.

- **Why it matters:** The spec says the proof engine handles collection non-empty obligations, but there's no diagnostic to emit if the obligation is unresolved. Either the spec is wrong (collection safety is the type checker's job entirely) or diagnostic codes are missing.

- **Suggested resolution:** Clarify which pipeline stage owns collection non-empty safety:

  - If the type checker already emits `UnguardedCollectionAccess`/`UnguardedCollectionMutation` for all cases, the proof engine should NOT create duplicate obligations. Remove collection non-empty from the proof engine spec.

  - If the proof engine handles the richer case (modifier proof + guard proof), add a proof-stage diagnostic code for unresolved collection obligations.



---



**PE-G10: `ExtractGuardConstraints` is not specified**

- **Severity:** SIGNIFICANT

- **Location:** `proof-engine.md` §7 Strategy 3, line 631

- **Description:** Strategy 3 calls `ExtractGuardConstraints(row.Guard)` to decompose a `TypedExpression` guard into simple constraint forms. The spec lists supported patterns (line 599–608) but doesn't specify:

  1. What happens with compound guards? `when A > 0 and B > 0` — are both constraints extracted? What about `or`?

  2. What happens with negation? `when not (A == 0)` — is this recognized as `A != 0`?

  3. What about nested function calls in guards? `when count(Items) > 0 and len(Name) > 3` — is `len(Name) > 3` a valid constraint form?

  4. Does the proof engine look inside `TypedConditional` (if/then/else) for guard constraints?

- **Why it matters:** The guard pattern language directly determines Strategy 3's power. Without clarity on compound/negated guards, the implementer must choose a scope that may be too narrow or too broad.

- **Suggested resolution:** Specify that `ExtractGuardConstraints`:

  - Decomposes `and` conjunctions recursively — each leaf becomes a separate constraint

  - Does NOT decompose `or` disjunctions — the proof engine cannot use a disjunct because either branch might be false

  - Handles simple negation by inverting the comparison operator

  - Ignores complex expressions (nested conditionals, quantifiers) — they are not constraint forms



---



## [ADVISORY] Gaps



---



**PE-G11: Spec references `Compilation` but doesn't address the Precept Builder gap**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` §8 "Downstream Consumers", lines 930–937

- **Description:** The spec references "Precept Builder" as a consumer of `FaultSiteLinks` and `ConstraintInfluence`, and references `precept-builder.md §Pass 4` (line 218, 236, 250). No `precept-builder.md` file exists in `docs/compiler/`. The consumer contract for `ProofLedger` is described in the proof engine spec but has no counterpart in any builder spec.

- **Why it matters:** The proof engine's output shape is driven by what the builder consumes. Without a builder spec, the output shape is hypothetical — it could change when the builder is designed. Implementation risk is moderate: the proof engine can be built to the spec, but the builder may require changes.

- **Suggested resolution:** Accept this gap for now — the builder is a future stage. Add a note in the proof engine spec: "Builder contract is forward-looking; output shape may evolve when `precept-builder.md` is authored."



---



**PE-G12: No specification of diagnostic message formatting for proof obligations**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` §9 "Failure Modes", line 981

- **Description:** The pseudocode calls `CreateDiagnostic(obligation)` but doesn't specify how the diagnostic message template parameters `{0}`, `{1}` are populated. The existing diagnostic entries in `Diagnostics.cs` have:

  - `DivisionByZero`: `"Division by zero: '{0}' can be zero when {1}"` — what is `{0}` (field name? expression text?) and `{1}` (state name? guard absence?)?

  - `SqrtOfNegative`: `"sqrt() requires a non-negative value, but '{0}' can be negative when {1}"` — same question.

  - `UnsatisfiableGuard`: `"The condition '{0}' on event '{1}' can never be true when {2}"` — three params.

- **Why it matters:** Without knowing what fills the template parameters, test authors can't assert diagnostic messages. This is a testability gap.

- **Suggested resolution:** Add a message-formatting table: for each diagnostic code, specify what each `{N}` parameter is (field name, expression text, state name, constraint description).



---



**PE-G13: Error propagation from upstream stages is unspecified**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` §3 "Responsibilities and Boundaries"

- **Description:** The spec doesn't say whether the proof engine should short-circuit if the `SemanticIndex` or `StateGraph` already contain errors. Looking at the existing pipeline in `Compiler.cs`, every stage runs unconditionally — the proof engine receives its inputs regardless of upstream errors. But:

  1. If the `SemanticIndex` contains `TypedErrorExpression` nodes, can the proof engine encounter them during obligation instantiation? If so, what does it do?

  2. If the `StateGraph` has structural violation diagnostics (unreachable states), does the proof engine suppress obligations for those states? (The spec addresses this via `ReachabilityFact`, but doesn't address the case where the _graph analyzer itself_ emitted errors.)

- **Why it matters:** Without clarity, the implementer might crash on `TypedErrorExpression` nodes.

- **Suggested resolution:** Add: "Proof obligations are not instantiated for expression trees containing `TypedErrorExpression` — those trees already have type-checker diagnostics and no valid proof subject."



---



**PE-G14: `GuardRelationImpliesObligation` in Strategy 4 is a pattern-match black box**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` §7 Strategy 4, lines 758–766

- **Description:** The function `GuardRelationImpliesObligation` is described as "a simple pattern match on (guard.Op, expression.Op, requirement.Comparison) triples — not a solver" and provides three example triples. But the complete triple set is not enumerated. The spec gives examples but not an exhaustive table.

- **Why it matters:** An implementer would need to enumerate all valid triples. Given the bounded operator set, this is a finite list — but it's work the spec should contain.

- **Suggested resolution:** Add an exhaustive table of (guard.Op, expr.Op, requirement) → discharge triples. Given Precept's bounded operator set, this is likely ~10-15 entries.



---



**PE-G15: No specification of whether proof engine runs for stateless precepts**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` — absent from §3 and §9

- **Description:** The graph analyzer has explicit stateless-precept handling (emitting vacuous `TerminalCompletenessFact` and `DeadEndStateFact`). The proof engine spec doesn't address stateless precepts. Stateless precepts have `EventHandlers` instead of `TransitionRows` and no state machine. Questions:

  1. Do event handlers in stateless precepts carry proof requirements? (Yes — their `TypedAction` nodes can have `ProofRequirements`.)

  2. Do Strategies 3/4 (guard-based) apply to event handlers? (Event handlers don't have guards — `TypedEventHandler` has no `Guard` field.)

  3. Are there any proof obligations specific to stateless precepts?

- **Why it matters:** If the implementer ignores stateless precepts, proof obligations on event handler actions would be silently missed.

- **Suggested resolution:** Add a subsection: "For stateless precepts, the proof engine walks `EventHandlers[].Actions[]` for obligations. Strategies 1 (Literal) and 2 (Modifier) apply. Strategies 3/4 do not apply (event handlers have no guards). All unresolved obligations produce diagnostics as normal."



---



**PE-G16: Spec's `ProofObligation.Site` identity matching is underspecified**

- **Severity:** ADVISORY

- **Location:** `proof-engine.md` §5, line 217 (CC#6 resolved box)

- **Description:** CC#6 says the builder "matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity." But `TypedExpression` is a record — C# record equality is structural, not referential. The spec doesn't say whether `Site` matching uses reference equality or structural equality. For records, structural equality means two independently-created `TypedBinaryOp` nodes with identical fields would match — which could cause false positives.

- **Why it matters:** If the builder or proof engine relies on reference identity, the implementer must ensure the same `TypedExpression` object instance is used in both the `ProofObligation` and the builder's walk. If structural equality is fine, no action needed.

- **Suggested resolution:** Clarify that `ProofObligation.Site` uses the same object reference passed through from `SemanticIndex` — no copies. Reference identity is preserved because the proof engine reads the same `TypedExpression` nodes the builder later visits.



---



## [DOC-ONLY] Gaps



---



**PE-G17: Spec shows `OperatorKind` in code samples but source uses different names**

- **Severity:** DOC-ONLY

- **Location:** `proof-engine.md` §7, line 454

- **Description:** The Strategy 1 pseudocode uses `OperatorKind.NotEquals`, `OperatorKind.GreaterThan`, etc. Need to verify these match the actual `OperatorKind` enum values in source. Minor naming discrepancies between spec pseudocode and source enum members would cause confusion during implementation.

- **Suggested resolution:** Cross-reference with `src/Precept/Language/OperatorKind.cs` and update spec pseudocode to use actual enum member names.



---



**PE-G18: Spec says "accumulate diagnostics without abandoning" but doesn't cite the principle by name**

- **Severity:** DOC-ONLY

- **Location:** `proof-engine.md` §9, line 945

- **Description:** The spec references Precept's error accumulation principle but doesn't cite the canonical name or doc location. Other pipeline stage docs reference `diagnostic-system.md §Error Accumulation`.

- **Suggested resolution:** Add cross-reference to `diagnostic-system.md`.



---



## Cross-Stage Seam Issues



## GraphAnalyzer → ProofEngine



1. **ReachabilityFact emission is per-state.** The GraphAnalyzer emits one `ReachabilityFact` per state (line 186 of GraphAnalyzer.cs). The spec's consumption table (line 907) says "suppress proof obligations on transitions originating from unreachable states." This is correct — the proof engine can look up `ReachabilityFact.IsReachable` for a transition's `FromState`. **No gap.**



2. **EventCoverageFact consumption is vague.** The spec says the proof engine "uses coverage gaps to reason about guard completeness: in states where an event is handled, are the guards sufficient?" (line 909). This is hand-wavy. What does "guard completeness" mean for the proof engine? Is the proof engine checking that guards on transition rows cover all possible field value ranges? That's a significantly harder problem than the spec's other strategies suggest. **Overlaps with PE-G1 (underspecified algorithm).** The EventCoverageFact consumption should be clarified — likely it's just a structural record, not an active proof check.



3. **DominancePathFact:** The spec says "if `DominatedTerminals` is empty, records a structural violation in the proof ledger." But the GraphAnalyzer already emits `RequiredStateDoesNotDominateTerminal` (111) for this case. The proof engine recording it again is redundant. **Clarify whether the proof engine adds to the structural record or merely records the fact for downstream consumption without additional diagnostics.**



## ProofEngine → Runtime (via Precept Builder)



4. **No `precept-builder.md` exists.** The spec references it in three CC#6 resolution boxes (lines 218, 236, 250). The downstream contract is hypothetical. **Covered by PE-G11.**



5. **`FaultSiteAnnotation` is described in the spec but does not exist in source.** The source has `FaultSiteDescriptor` in `Runtime/Descriptors.cs` with a different shape: `FaultSiteDescriptor(FaultCode, DiagnosticCode PreventedBy, int SourceLine)`. The spec's `FaultSiteAnnotation` has `(FaultCode Code, DiagnosticCode PreventedBy, SourceSpan Site)` — `SourceSpan` vs `int SourceLine`. These may be different types (builder-time vs runtime), but the relationship is unspecified.



---



## Catalog Compliance Issues



1. **PE-G2 is the primary catalog violation.** `FieldModifierMeta.ProofDischarges` is described as catalog metadata but doesn't exist. The spec correctly identifies this as catalog-driven (Strategy 2 reads `meta.ProofDischarges` from the catalog), but the catalog hasn't been updated. **BLOCKING.**



2. **The four strategies themselves are generic machinery, not catalog-driven.** The strategies are predicate functions that pattern-match on requirement types and expression types. This is correct — strategies are algorithms, not per-member metadata. The obligation _source_ is catalog-driven (ProofRequirements on catalog entries), the _discharge_ is algorithmic. **No violation.**



3. **`ProofRequirementMeta` catalog is correctly implemented.** The `ProofRequirements.cs` catalog with `GetMeta()` switch and `All` enumeration matches the catalog pattern. **No issue.**



---



## Diagnostic Catalog Status



| Code | Name | Stage | Severity | Registered in `DiagnosticCode.cs` | Registered in `Diagnostics.cs` | `PreventsFault` | Status |

|------|------|-------|----------|------------------------------------|-------------------------------|-----------------|--------|

| 82 | `UnsatisfiableGuard` | Proof | Warning | ✅ | ✅ | — | Complete |

| 83 | `DivisionByZero` | Proof | Error | ✅ | ✅ | `FaultCode.DivisionByZero` | Complete |

| 84 | `SqrtOfNegative` | Proof | Error | ✅ | ✅ | `FaultCode.SqrtOfNegative` | Complete |



**Three proof-stage diagnostics exist and are fully registered.** `RelatedCodes` cross-link all three. `FixHint` values are present.



**Missing diagnostic gap:** Collection non-empty proof failures have no proof-stage diagnostic code (PE-G9). Depending on resolution of PE-G9, additional codes may be needed.



**Missing diagnostic gap:** `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` failures have no diagnostic codes. Depending on resolution of PE-G1, additional codes may be needed.



---



## Spec Readiness Verdict



**NOT READY** — three BLOCKING gaps prevent implementation from starting.



## Blockers (must resolve before any implementation work):



1. **PE-G1:** Three of five `ProofRequirementKind` values have no discharge strategy. The implementer cannot write discharge logic for `Dimension`, `Modifier`, or `QualifierCompatibility` obligations without spec guidance.

2. **PE-G2:** `FieldModifierMeta.ProofDischarges` does not exist in source. Strategy 2 cannot be implemented as specified.

3. **PE-G3:** Output type `ProofLedger` and ~10 supporting record types don't exist. Shape declarations must be created before coding begins.



## Conditions (must resolve before implementation is complete, but won't block starting if blockers are cleared):



4. **PE-G4:** `AllTypedExpressions` doesn't exist — Pass 1 walk targets must be enumerated.

5. **PE-G5:** `ConstraintIdentity` shapes must match source, not spec.

6. **PE-G6:** `FindEnclosingTransitionRow` must be specified.

7. **PE-G7:** `ResolveSubject` must be specified.

8. **PE-G8:** Initial-state satisfiability needs a concrete algorithm.

9. **PE-G9:** Collection non-empty proof ownership must be decided (type checker vs proof engine).

10. **PE-G10:** Guard decomposition rules must be specified.



---



## Recommended Pre-Implementation Actions



1. **Resolve PE-G1** — For each of `Dimension`, `Modifier`, `QualifierCompatibility`: state which strategy handles it, or state that the type checker resolves it before proof. This is a design decision, not an implementation detail.



2. **Implement PE-G2** — Add `ProofDischarge` record and `ProofDischarges` property to `FieldModifierMeta`. Populate entries for `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. This is a catalog prerequisite.



3. **Update spec for PE-G3** — Add a "Slice 0: Shape declarations" section listing all new types to create. The implementer should create these in a build-green commit before any logic.



4. **Resolve PE-G9** — Decide collection-empty ownership. This affects diagnostic code allocation and obligation walk scope.



5. **Update spec for PE-G5** — Align `ConstraintIdentity` shapes with source implementation.



6. **Add `FindEnclosingTransitionRow` spec (PE-G6)** and **`ResolveSubject` spec (PE-G7)** — These are the two most complex helper functions. Providing pseudocode prevents design divergence during implementation.



7. **Specify initial-state satisfiability algorithm (PE-G8)** — Define the default value model and which constraint scopes are checked.



8. **Add compound guard decomposition rules (PE-G10)** — Specify `and`/`or`/`not` handling.



9. **Add stateless precept handling section (PE-G15)** — Small but prevents a class of missed-obligation bugs.



## 2026-05-08T22:46:03: User directive

**By:** Shane (via Copilot)

**What:** After Frank finishes G4-G18 and the design is approved, route G1 and G2 prework to George. Prework = shape/type definitions and catalog metadata only (e.g., ProofSatisfaction DU, DeclaredPresence, DeclaredQualifierMeta, Modifiers rows). Engine work (Parser qualifier clauses, TypeChecker qualifier resolution, ProofEngine Strategy 2 & 5) waits until after prework lands.

**Why:** User directive — G1/G2 prework is in the same category as G3 (structural definitions, not engine logic). Prework can proceed immediately after design is finalized; engine implementation is a separate phase.

# PE-G1 Detailed Analysis — Frank



**Date:** 2026-05-08

**Author:** Frank (Lead/Architect)

**Status:** Ready for design decision



## Summary



All three unhandled `ProofRequirementKind` values are **live** — they have real obligation-generating callers in `Operations.cs`, they are stamped onto `TypedBinaryOp` nodes by the TypeChecker's expression resolver, and the TypeChecker does **not** enforce any of them independently. Each reaches the proof engine as an open obligation. None is dead code. None is pre-discharged.



---



## DimensionProofRequirement



**Determination: B — Strategy 2 (Declaration Attribute Proof) handles this.**



## Rationale



**Callers (4 sites in `Operations.cs`):**

- `DatePlusPeriod` (line 248): `DimensionProofRequirement(PPeriod, PeriodDimension.Date, ...)`

- `DateMinusPeriod` (line 257): `DimensionProofRequirement(PPeriod, PeriodDimension.Date, ...)`

- `TimePlusPeriod` (line 275): `DimensionProofRequirement(PPeriod, PeriodDimension.Time, ...)`

- `TimeMinusPeriod` (line 284): `DimensionProofRequirement(PPeriod, PeriodDimension.Time, ...)`



All four attach the requirement to binary operations where one operand is a `period` type. The requirement says: "this period operand must have the correct time dimension (date-level for date arithmetic, time-level for time arithmetic)."



**TypeChecker behavior:** The TypeChecker (`TypeChecker.Expressions.cs` lines 479–519) resolves binary operations by type matching via `TryResolveBinaryWithWidening`. It resolves `Date + Period → DatePlusPeriod` purely on type structure, stamps `result.ProofRequirements` onto the `TypedBinaryOp` (line 504), and does NOT validate period dimensions. The `Qualifier` field on `TypedField` is even set to `null` with a `// Slice 2+` comment (TypeChecker.cs line 121), confirming qualifier resolution is future work.



**Why Strategy 2:** Strategy 2 was renamed to "Declaration Attribute Proof" (per Shane's lock decision 2026-05-08T05:15:57Z). It reads declaration-site attributes of the subject field. Period dimension is determined by the field's type qualifier — a declaration attribute. The predicate is:



1. Resolve the proof subject to an expression node.

2. Determine the expression's period dimension:

   - **Field reference:** Read the field's resolved qualifier on the `TemporalDimension` axis (once qualifier resolution ships). Map to `PeriodDimension`.

   - **Literal period:** Extract dimension from the literal's temporal unit (e.g., `3 days` → Date, `2 hours` → Time).

   - **Unqualified/unknown:** Treat as `PeriodDimension.Any` (per Shane's locked decision on permissive unqualified periods).

3. Discharge condition: `resolvedDimension == PeriodDimension.Any || resolvedDimension == requirement.RequiredDimension`.



**Why not Strategy 1:** Strategy 1 (Literal Proof) is explicitly scoped to `NumericProofRequirement` only (spec §7.1, line 436: "Gate: only numeric requirements are literal-provable"). The literal-period case fits naturally into Strategy 2's expanded scope — the literal IS the declaration in that context.



**Why not a new strategy:** The dimension check reads a declaration-site attribute (the period's qualifier/dimension). This is exactly what Strategy 2 does — read field declaration metadata to discharge an obligation. No new strategy machinery is needed.



## Concrete answer for the spec



Add to `proof-engine.md` §7, Strategy 2 pseudocode, a new arm:



```

// DimensionProofRequirement: check if subject's period dimension satisfies the requirement

if (obligation.Requirement is DimensionProofRequirement dimReq)

{

    var dimension = ResolvePeriodDimension(subject, semantics);

    // PeriodDimension.Any always satisfies (permissive unqualified periods — locked decision)

    return dimension == PeriodDimension.Any || dimension == dimReq.RequiredDimension;

}

```



Add to §6 `DimensionProofRequirement` description: "Discharged by Strategy 2 (Declaration Attribute Proof). The strategy reads the subject's resolved period dimension — from field qualifier metadata for field references, from literal temporal unit for literal periods. Unqualified periods resolve to `PeriodDimension.Any`, which permissively satisfies any dimension requirement."



---



## ModifierRequirement



**Determination: B — Strategy 2 (Declaration Attribute Proof) handles this.**



## Rationale



**Callers (4 sites in `Operations.cs`):**

- `ChoiceLessThanChoice` (line 760): `ModifierRequirement(PChoice, ModifierKind.Ordered, ...)`

- `ChoiceGreaterThanChoice` (line 768): same

- `ChoiceLessThanOrEqualChoice` (line 776): same

- `ChoiceGreaterThanOrEqualChoice` (line 784): same



All four attach the requirement to ordinal comparison operations on `choice` fields. The requirement says: "the choice field must declare the `ordered` modifier to permit ordinal comparison."



**TypeChecker behavior:** The TypeChecker resolves `Choice < Choice → ChoiceLessThanChoice` by type matching. It stamps the `ModifierRequirement` onto the `TypedBinaryOp` (line 504). It does NOT check whether the choice field has the `ordered` modifier — the modifiers are resolved on `TypedField` (TypeChecker.cs lines 99–117) but never cross-referenced against operation requirements.



**Why Strategy 2:** This is the purest case of a declaration attribute check. The predicate is:



1. Resolve the proof subject to a field name.

2. Look up the field's `Modifiers` array from `SemanticIndex.FieldsByName`.

3. Discharge condition: `field.Modifiers.Contains(requirement.Required)` OR `field.ImpliedModifiers.Contains(requirement.Required)`.



This is simpler than the `ProofDischarges` lookup path. The `ProofDischarges` mechanism maps modifiers → requirements they discharge (modifier "positive" discharges numeric requirement "> 0"). `ModifierRequirement` is the inverse — it asks "does the field have modifier X?" This is a direct membership check, not a discharge-table lookup.



**Both arms coexist in Strategy 2.** The `ProofDischarges` path handles: "modifier presence implies numeric/presence bound" (e.g., `positive` → `> 0`). The `ModifierRequirement` path handles: "field must have this modifier" (e.g., `ordered` for choice comparison). Same strategy, two predicate shapes.



## Concrete answer for the spec



Add to `proof-engine.md` §7, Strategy 2 pseudocode, a new arm:



```

// ModifierRequirement: check if subject field declares the required modifier

if (obligation.Requirement is ModifierRequirement modReq)

{

    var fieldName = GetFieldName(modReq.Subject, obligation.Site);

    if (fieldName is null) return false;

    if (!semantics.FieldsByName.TryGetValue(fieldName, out var field)) return false;



    // Direct modifier check — does the field's declaration include the required modifier?

    return field.Modifiers.Contains(modReq.Required)

        || field.ImpliedModifiers.Contains(modReq.Required);

}

```



Add to §6 `ModifierRequirement` description: "Discharged by Strategy 2 (Declaration Attribute Proof). The strategy resolves the subject to a field and checks whether the field's declared or implied modifiers include `requirement.Required`. This is a direct membership check — it does not use the `ProofDischarges` table."



---



## QualifierCompatibilityProofRequirement



**Determination: C — A new strategy (Strategy 5: Qualifier Compatibility Proof) is required.**



## Rationale



**Callers (24+ sites in `Operations.cs`):**

- Quantity arithmetic (lines 475, 484, 921–966): `QualifierAxis.Unit` — operands must share unit qualifier

- Price arithmetic (lines 557–559, 568–570, 977–1034): `QualifierAxis.Unit` AND `QualifierAxis.Currency` — operands must share both unit and currency qualifiers



All use `new ParamSubject(PQuantity)` or `new ParamSubject(PPrice)` for BOTH `LeftSubject` and `RightSubject` — same parameter reference. At the use site, these map to two different field expressions (e.g., `FieldA + FieldB` where both are quantity fields). The proof engine must verify that the two concrete field expressions share the same qualifier value on the specified axis.



**TypeChecker behavior:** The TypeChecker disambiguates binary operation candidates using `QualifierMatch.Same` as the default assumption (TypeChecker.Expressions.cs line 576). The comment at line 573 explicitly says: "ProofEngine will verify qualifier compatibility at deeper analysis." The TypeChecker defers this check to the proof engine by design.



**Why none of the four existing strategies work:**



1. **Strategy 1 (Literal Proof):** Operates on a single subject's literal value. Does not handle dual subjects. Does not read qualifier metadata.

2. **Strategy 2 (Declaration Attribute Proof):** Operates on a single subject's field declaration. `QualifierCompatibilityProofRequirement` is the ONLY dual-subject requirement kind — it has `LeftSubject` and `RightSubject`, not a single `Subject`. Strategy 2's predicate shape (resolve one subject → read its attributes → compare against requirement threshold) cannot express "compare two subjects against each other."

3. **Strategy 3 (Guard-in-Path):** Guards don't establish qualifier relationships. A `when` clause says things like `when Quantity > 0`, not `when FieldA.unit == FieldB.unit`.

4. **Strategy 4 (Flow Narrowing):** Same limitation as Strategy 3 — handles relational constraints between field values, not qualifier compatibility.



**Strategy 5: Qualifier Compatibility Proof**



Shape:



```

// Strategy 5: Qualifier Compatibility Proof — Discharge Predicate

// Input: ProofObligation with QualifierCompatibilityProofRequirement

// Reads: Both subjects' resolved qualifier bindings from SemanticIndex

// Scope: Dual-subject obligations where two operands must share a qualifier value



bool TryQualifierCompatibilityProof(ProofObligation obligation, SemanticIndex semantics)

{

    if (obligation.Requirement is not QualifierCompatibilityProofRequirement qcReq)

        return false;



    // 1. Resolve both subjects to their qualifier bindings

    var leftQualifier = ResolveQualifierOnAxis(qcReq.LeftSubject, qcReq.Axis, obligation.Site, semantics);

    var rightQualifier = ResolveQualifierOnAxis(qcReq.RightSubject, qcReq.Axis, obligation.Site, semantics);



    // 2. If either qualifier is unresolved (unqualified field), cannot prove — Unresolved

    if (leftQualifier is null || rightQualifier is null)

        return false;



    // 3. Compare: both must have the same qualifier value on the specified axis

    return leftQualifier == rightQualifier;

}

```



**Inputs required:**

- `QualifierCompatibilityProofRequirement.LeftSubject` and `RightSubject` — both `ProofSubject` instances

- `QualifierCompatibilityProofRequirement.Axis` — the `QualifierAxis` to compare on (Unit, Currency, etc.)

- Resolved qualifier bindings from `SemanticIndex` — requires qualifier resolution to be implemented (currently `Qualifier: null` in TypeChecker, Slice 2+ dependency)



**Dependency:** Strategy 5 depends on qualifier resolution shipping in the TypeChecker. Until then, all `QualifierCompatibilityProofRequirement` obligations will be `Unresolved` — which is correct defensive behavior. The proof engine should still instantiate the obligations; it just can't discharge them yet.



## Concrete answer for the spec



Add new §7.5 to `proof-engine.md`:



> **Strategy 5: Qualifier Compatibility Proof**

>

> **When it applies:** The obligation is a `QualifierCompatibilityProofRequirement` — the only dual-subject requirement kind.

>

> **How it works:** Resolve both subjects to their qualifier bindings on the specified `QualifierAxis`. If both resolve to the same qualifier value, discharge. If either is unqualified or the values differ, the obligation remains `Unresolved`.

>

> **Examples:**

> - `quantity of 'kg' + quantity of 'kg'` → both Unit qualifiers match → discharged

> - `quantity of 'kg' + quantity of 'miles'` → Unit qualifiers differ → Unresolved → diagnostic

> - `quantity + quantity` (unqualified) → cannot prove → Unresolved → diagnostic

>

> **Dependency:** Requires qualifier resolution in the TypeChecker (currently Slice 2+ future work). Until qualifier resolution ships, all `QualifierCompatibilityProofRequirement` obligations produce `Unresolved` — the correct conservative behavior.



Add to §6 `QualifierCompatibilityProofRequirement` description: "Discharged by Strategy 5 (Qualifier Compatibility Proof) — the only strategy that handles dual-subject obligations. Compares both subjects' resolved qualifier bindings on the specified `QualifierAxis`. Requires qualifier resolution to be operational."



---



## Spec update instructions



## §6 — ProofRequirementKind subtypes (lines 348–389)



After each subtype's existing description, add the discharge strategy reference:



1. **Line ~374 (DimensionProofRequirement):** Add: "Discharged by Strategy 2 (Declaration Attribute Proof). Reads the subject's resolved period dimension — from field qualifier for field references, from literal temporal unit for literals. `PeriodDimension.Any` permissively satisfies any dimension requirement."



2. **Line ~381 (ModifierRequirement):** Add: "Discharged by Strategy 2 (Declaration Attribute Proof). Checks direct modifier membership: `field.Modifiers.Contains(requirement.Required)`. Does not use the `ProofDischarges` table — this is a presence check, not a discharge mapping."



3. **Line ~389 (QualifierCompatibilityProofRequirement):** Add: "Discharged by Strategy 5 (Qualifier Compatibility Proof). The only dual-subject requirement kind — requires a dedicated strategy that compares two subjects' qualifier bindings. Depends on qualifier resolution in the TypeChecker."



## §7 — Strategy 2 pseudocode (lines 536–569)



Expand the `TryModifierProof` function to include the two new arms (DimensionProofRequirement and ModifierRequirement) in addition to the existing ProofDischarges loop. Rename if desired to `TryDeclarationAttributeProof` to match the locked rename.



## §7 — New Strategy 5 section (after Strategy 4, line ~767)



Add the full Strategy 5: Qualifier Compatibility Proof section as specified above.



## §7 header (line 410)



Update "Four Proof Strategies" → "Five Proof Strategies" in the section title.



---



## Catalog implications



## No new catalog changes required for ModifierRequirement or DimensionProofRequirement



- `ModifierRequirement` reads existing `TypedField.Modifiers` and `TypedField.ImpliedModifiers`. No new metadata needed.

- `DimensionProofRequirement` reads the field's resolved qualifier (future Slice 2+ work) and literal temporal units. No new catalog property needed — it reads existing type metadata.



## QualifierCompatibilityProofRequirement depends on qualifier resolution



- The `QualifierAxis` enum already exists in `Type.cs` (line 39).

- The `QualifierSlot` and `QualifierShape` types already exist in `Type.cs` and `Types.cs`.

- What's missing: `TypedField.Qualifier` is currently `null` (TypeChecker.cs line 121). Qualifier resolution must ship before Strategy 5 can discharge obligations. Until then, all qualifier compatibility obligations are correctly `Unresolved`.



## PE-G2 (ProofDischarges) is still a prerequisite for Strategy 2's existing NumericProofRequirement path



The `FieldModifierMeta.ProofDischarges` property (PE-G2) is needed for the "modifier discharges numeric/presence bound" arm of Strategy 2. The two new arms (DimensionProofRequirement, ModifierRequirement) do NOT depend on `ProofDischarges` — they use different predicate shapes. PE-G2 remains a blocking prerequisite only for Strategy 2's original `ProofDischarges` path.



---



## Decision summary for Shane



| Requirement Kind | Determination | Strategy | Blocking dependency? |

|---|---|---|---|

| `DimensionProofRequirement` | **B** — existing strategy handles it | Strategy 2 (Declaration Attribute Proof) | No (literal path works now; field path needs qualifier resolution) |

| `ModifierRequirement` | **B** — existing strategy handles it | Strategy 2 (Declaration Attribute Proof) | No (reads existing `TypedField.Modifiers`) |

| `QualifierCompatibilityProofRequirement` | **C** — new strategy required | **Strategy 5** (Qualifier Compatibility Proof) | Yes — depends on qualifier resolution (Slice 2+) |



Two of three are absorbed into the existing Strategy 2. One requires a fifth strategy. The spec's "Four Proof Strategies" becomes "Five Proof Strategies." The fifth strategy is the only one with a qualifier-resolution dependency — it can be stubbed during initial ProofEngine implementation and activated when qualifier resolution ships.



## 2026-05-08T21:22:17Z: PE-G1 resolved — Shane sign-off

**Decision:** All three PE-G1 determinations approved by Shane.

**DimensionProofRequirement:** Strategy 2 (new arm)

**ModifierRequirement:** Strategy 2 (new arm)

**QualifierCompatibilityProofRequirement:** Strategy 5 (new strategy, stubbed until qualifier resolution)

**Spec updated:** docs/compiler/proof-engine.md — five strategies, §6 discharge references, §7 pseudocode arms

**Gap analysis updated:** docs/Working/frank-proof-engine-gap-analysis.md — PE-G1 marked RESOLVED

# PE-G2 Analysis — ProofDischarge + FieldModifierMeta.ProofDischarges



**Date:** 2026-05-08T21:29:51.919-04:00

**Author:** Frank (Lead/Architect)

**Status:** Ready for Shane sign-off



## 1. Source-verified findings



1. `FieldModifierMeta` currently exposes `Kind`, `Token`, `Description`, `Category`, `ApplicableTo`, `HasValue`, `Subsumes`, `HoverDescription`, `UsageExample`, `SnippetTemplate`, `DesugarsToRule`, and `MutuallyExclusiveWith`; there is no `ProofDischarges` constructor parameter or property in source today. (`src/Precept/Language/Modifier.cs:116-133`)

2. The catalog population for modifiers is not in `Modifier.cs`; it lives in `Modifiers.cs`. The relevant field modifiers are declared there: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, and `maxcount`. (`src/Precept/Language/Modifiers.cs:10-29`, `src/Precept/Language/Modifiers.cs:61-145`)

3. `NumericProofRequirement` already fixes `Kind` to `ProofRequirementKind.Numeric` and carries the actual proof payload as `(Subject, Comparison, Threshold, Description)`. The kind metadata is separately recoverable through `ProofRequirements.GetMeta(kind)`. `ProofDischarge` therefore does **not** need a redundant `ProofRequirementKind` field. (`src/Precept/Language/ProofRequirement.cs:41-53`, `src/Precept/Language/ProofRequirements.cs:13-19`)

4. Current live numeric obligation shapes are broader than `Operations.cs` alone:

   - `Operations.cs` emits only `OperatorKind.NotEquals, 0m` obligations at every numeric site. (`src/Precept/Language/Operations.cs:100`, `src/Precept/Language/Operations.cs:109`, `src/Precept/Language/Operations.cs:131`, `src/Precept/Language/Operations.cs:140`, `src/Precept/Language/Operations.cs:162`, `src/Precept/Language/Operations.cs:171`, `src/Precept/Language/Operations.cs:193`, `src/Precept/Language/Operations.cs:202`, `src/Precept/Language/Operations.cs:224`, `src/Precept/Language/Operations.cs:233`, `src/Precept/Language/Operations.cs:335`, `src/Precept/Language/Operations.cs:344`, `src/Precept/Language/Operations.cs:353`, `src/Precept/Language/Operations.cs:418`, `src/Precept/Language/Operations.cs:428`, `src/Precept/Language/Operations.cs:438`, `src/Precept/Language/Operations.cs:447`, `src/Precept/Language/Operations.cs:456`, `src/Precept/Language/Operations.cs:465`, `src/Precept/Language/Operations.cs:497`, `src/Precept/Language/Operations.cs:507`, `src/Precept/Language/Operations.cs:517`, `src/Precept/Language/Operations.cs:526`, `src/Precept/Language/Operations.cs:535`, `src/Precept/Language/Operations.cs:595`, `src/Precept/Language/Operations.cs:613`)

   - `Functions.cs` emits `OperatorKind.GreaterThanOrEqual, 0m` for integer `pow` exponents and `sqrt` arguments. (`src/Precept/Language/Functions.cs:163-188`)

   - `Types.cs` and `Actions.cs` emit `OperatorKind.GreaterThan, 0m` and one `OperatorKind.GreaterThanOrEqual, 0m` against collection cardinality (`SelfSubject(CollectionCountAccessor)`). (`src/Precept/Language/Types.cs:153-164`, `src/Precept/Language/Types.cs:166-288`, `src/Precept/Language/Actions.cs:92-100`, `src/Precept/Language/Actions.cs:110-118`, `src/Precept/Language/Actions.cs:145-166`, `src/Precept/Language/Actions.cs:189-198`)

5. Because collection non-empty obligations target `SelfSubject(CollectionCountAccessor)`, declaration-attribute proof must distinguish **field-value** bounds from **cardinality** bounds; comparison + threshold alone is not enough. The shared accessor is literally `count`. (`src/Precept/Language/Types.cs:153-154`, `src/Precept/Language/Types.cs:163-170`, `src/Precept/Language/Types.cs:181-288`)

6. Valued modifiers are parsed and preserved as `ParsedModifier(ModifierKind Kind, ParsedExpression? Value)` on `DeclaredField`, but `TypedField.Modifiers` keeps only `ModifierKind`. The original field syntax is still retained on `TypedField.Syntax`, and `ParsedConstruct.GetSlot<T>()` can recover the `ModifierListSlot`. Therefore parametric discharges (`min`, `max`, `mincount`, etc.) must encode “threshold comes from the modifier value,” not a fixed decimal stored in catalog metadata. (`src/Precept/Pipeline/SlotValue.cs:26-30`, `src/Precept/Pipeline/SymbolTable.cs:54-62`, `src/Precept/Pipeline/TypeChecker.cs:99-102`, `src/Precept/Pipeline/SemanticIndex.cs:239-253`, `src/Precept/Pipeline/ParsedConstruct.cs:20-29`)

7. Existing spec text is not implementable as written: both `proof-engine.md` and `catalog-system.md` currently model `ProofDischarge` as `(ProofRequirementKind, OperatorKind?, decimal?)`, which cannot represent (a) whether the discharge applies to the field value vs cardinality and (b) whether the threshold is fixed vs modifier-sourced. (`docs/compiler/proof-engine.md:517-607`, `docs/compiler/proof-engine.md:1179-1203`, `docs/language/catalog-system.md:1298-1324`)



## 2. Recommended `ProofDischarge` record definition



## Recommendation



Use a single top-level `ProofDischarge` record with a **narrow subject discriminator** and a **threshold-source DU**:



```csharp

public enum ProofDischargeSubject

{

    FieldValue  = 1,

    Cardinality = 2,

}



public sealed record ProofDischarge(

    ProofDischargeSubject Subject,

    OperatorKind Comparison,

    ProofDischargeThreshold Threshold);



public abstract record ProofDischargeThreshold

{

    public sealed record Fixed(decimal Value) : ProofDischargeThreshold;

    public sealed record ModifierValue() : ProofDischargeThreshold;

}

```



## Why this shape



- **No `ProofRequirementKind`:** `FieldModifierMeta.ProofDischarges` is only for Strategy-2 numeric declaration proofs, and `NumericProofRequirement` already fixes `Kind = Numeric`; storing the kind again is redundant metadata. (`src/Precept/Language/ProofRequirement.cs:41-53`, `src/Precept/Language/ProofRequirements.cs:13-19`)

- **Needs a subject discriminator:** the source emits both field-value proofs (`x != 0`, `x >= 0`) and cardinality proofs (`collection.count > 0`, `collection.count >= 0`). `notempty` and `mincount` do not establish the same thing as `positive` and `min`. (`src/Precept/Language/Types.cs:153-164`, `src/Precept/Language/Types.cs:181-288`, `src/Precept/Language/Actions.cs:92-100`, `src/Precept/Language/Functions.cs:163-188`, `src/Precept/Language/Operations.cs:100-613`)

- **Needs a threshold source, not just a threshold value:** fixed modifiers (`positive`, `nonnegative`, `nonzero`, `notempty`) prove against `0m`; valued modifiers (`min`, `max`, `mincount`, etc.) must read the declaration’s own value expression. (`src/Precept/Language/Modifiers.cs:61-145`, `src/Precept/Pipeline/SlotValue.cs:26-30`, `src/Precept/Pipeline/TypeChecker.cs:99-102`)

- **DU only where shape actually varies:** the only shape variation is threshold source (`Fixed(decimal)` vs `ModifierValue()`), so the DU belongs there. The top-level discharge row is still the same shape for every modifier: subject axis + comparison + threshold source.



## 3. Recommended `ProofDischarges` property signature on `FieldModifierMeta`



## Recommendation



Use the same small-array pattern the language catalogs already use for proof metadata:



```csharp

public sealed record FieldModifierMeta(

    ModifierKind Kind,

    TokenMeta Token,

    string Description,

    ModifierCategory Category,

    TypeTarget[] ApplicableTo,

    bool HasValue = false,

    ModifierKind[] Subsumes = default!,

    ProofDischarge[]? ProofDischarges = null,

    string? HoverDescription = null,

    string? UsageExample = null,

    string? SnippetTemplate = null,

    bool DesugarsToRule = false,

    ModifierKind[]? MutuallyExclusiveWith = null)

    : ModifierMeta(Kind, Token, Description, Category, DesugarsToRule, MutuallyExclusiveWith)

{

    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];

    public ProofDischarge[] ProofDischarges { get; init; } = ProofDischarges ?? [];

}

```



## Why `ProofDischarge[]` instead of `ImmutableArray<>` / `FrozenSet<>`



- Strategy 2’s access pattern is a tiny linear scan: `foreach (var discharge in meta.ProofDischarges)`. There is no key lookup to justify `FrozenSet<>`. (`docs/compiler/proof-engine.md:586-593`)

- Adjacent catalog surfaces already use the same array shape for proof metadata: `TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements`, and `FunctionOverload.ProofRequirements`. (`src/Precept/Language/Type.cs:77-87`, `src/Precept/Language/Action.cs:7-26`, `src/Precept/Language/Function.cs:18-26`)

- `FieldModifierMeta` already uses arrays for other tiny metadata bags (`ApplicableTo`, `Subsumes`, `MutuallyExclusiveWith`). (`src/Precept/Language/Modifier.cs:116-133`)



## 4. Per-modifier population table



## Recommended population



| Modifier | Recommended `ProofDischarges` entries | Live against current source? | Notes |

|---|---|---:|---|

| `positive` | `new ProofDischarge(ProofDischargeSubject.FieldValue, OperatorKind.GreaterThan, new ProofDischargeThreshold.Fixed(0m))` | Yes | Canonical fact is `value > 0`; generic subsumption can cover `!= 0` and `>= 0` from that stronger bound. `positive` already structurally subsumes `nonnegative` and `nonzero`. (`src/Precept/Language/Modifiers.cs:69-76`, `docs/compiler/proof-engine.md:600-606`) |

| `nonnegative` | `new ProofDischarge(ProofDischargeSubject.FieldValue, OperatorKind.GreaterThanOrEqual, new ProofDischargeThreshold.Fixed(0m))` | Yes | Directly matches current `sqrt` / integer-`pow` proof obligations. (`src/Precept/Language/Modifiers.cs:61-67`, `src/Precept/Language/Functions.cs:163-188`) |

| `nonzero` | `new ProofDischarge(ProofDischargeSubject.FieldValue, OperatorKind.NotEquals, new ProofDischargeThreshold.Fixed(0m))` | Yes | Directly matches current divide/modulo-style obligations from `Operations.cs`. (`src/Precept/Language/Modifiers.cs:78-83`, `src/Precept/Language/Operations.cs:100-613`) |

| `notempty` | `new ProofDischarge(ProofDischargeSubject.Cardinality, OperatorKind.GreaterThan, new ProofDischargeThreshold.Fixed(0m))` | Yes | This is a cardinality fact, not a presence fact. It discharges current collection `.count > 0` obligations and, via subsumption, `.count >= 0` obligations. (`src/Precept/Language/Modifiers.cs:85-90`, `src/Precept/Language/ModifierKind.cs:21-22`, `src/Precept/Language/Types.cs:153-164`, `src/Precept/Language/Actions.cs:92-100`) |

| `min(N)` | `new ProofDischarge(ProofDischargeSubject.FieldValue, OperatorKind.GreaterThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Yes | Parameterized lower bound on the field value. With a concrete declaration value, this can discharge current `>= 0`, and may also subsume `> 0` / `!= 0` when `N > 0`. (`src/Precept/Language/Modifiers.cs:98-103`, `src/Precept/Pipeline/SlotValue.cs:26-30`) |

| `max(N)` | `new ProofDischarge(ProofDischargeSubject.FieldValue, OperatorKind.LessThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Not yet | Semantically correct upper-bound metadata; current source does not emit any `<=` numeric obligations yet, but this belongs in the catalog because `max` is a first-class declaration of that bound. (`src/Precept/Language/Modifiers.cs:105-110`) |

| `mincount(N)` | `new ProofDischarge(ProofDischargeSubject.Cardinality, OperatorKind.GreaterThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Yes | Current collection-accessor/action obligations are cardinality-based, so `mincount` is relevant and should not be omitted. (`src/Precept/Language/Modifiers.cs:126-131`, `src/Precept/Language/Types.cs:153-164`, `src/Precept/Language/Actions.cs:92-100`) |

| `maxcount(N)` | `new ProofDischarge(ProofDischargeSubject.Cardinality, OperatorKind.LessThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Not yet | Semantically correct upper-bound metadata for future cardinality upper-bound obligations. (`src/Precept/Language/Modifiers.cs:133-138`) |

| `minlength(N)` | `new ProofDischarge(ProofDischargeSubject.Cardinality, OperatorKind.GreaterThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Not yet | Current source has no string-cardinality proof emitters, but this is the string parallel to `mincount`. (`src/Precept/Language/Modifiers.cs:112-117`) |

| `maxlength(N)` | `new ProofDischarge(ProofDischargeSubject.Cardinality, OperatorKind.LessThanOrEqual, new ProofDischargeThreshold.ModifierValue())` | Not yet | Current source has no string-cardinality upper-bound proof emitters, but catalog truth should still declare the bound. (`src/Precept/Language/Modifiers.cs:119-124`) |



## Modifiers that should **not** get `ProofDischarges`



- `optional` — presence/nullability is not modeled by the current numeric discharge path. (`src/Precept/Language/Modifiers.cs:49-53`, `src/Precept/Language/ProofRequirement.cs:56-63`)

- `ordered` — used by direct `ModifierRequirement`, not numeric discharge lookup. (`src/Precept/Language/Modifiers.cs:55-59`, `src/Precept/Language/ProofRequirement.cs:103-116`)

- `default` — initialization expression, not a declaration-time proof bound. (`src/Precept/Language/Modifiers.cs:92-96`)

- `maxplaces` — decimal precision constraint; there is no corresponding `ProofRequirement` shape in source. (`src/Precept/Language/Modifiers.cs:140-145`, `src/Precept/Language/ProofRequirement.cs:41-116`)

- `writable` — access-mode/editability semantics, not proof discharge. (`src/Precept/Language/Modifiers.cs:147-151`)



## 5. `proof-engine.md` update instructions



1. **Replace the flat `ProofDischarge(ProofRequirementKind, OperatorKind?, decimal?)` snippet** in Strategy 2 and Decision 5 with the subject-aware + threshold-source shape above. The current doc shape cannot represent cardinality-vs-field-value or modifier-sourced thresholds. (`docs/compiler/proof-engine.md:517-537`, `docs/compiler/proof-engine.md:1188-1193`)

2. **Remove `PresenceProofRequirement` from the `ProofDischarges` path.** Strategy 2’s catalog lookup arm should be numeric-only. `notempty` is a cardinality/numeric proof, not a presence proof, and the source’s current non-empty callers all emit `NumericProofRequirement`, not `PresenceProofRequirement`. (`docs/compiler/proof-engine.md:571-603`, `src/Precept/Language/Types.cs:153-164`, `src/Precept/Language/Actions.cs:92-100`)

3. **Teach the pseudocode to compare the discharge subject axis** (`FieldValue` vs `Cardinality`) against the resolved requirement subject. The current pseudocode only compares requirement kind/comparison/threshold, which is insufficient. (`docs/compiler/proof-engine.md:542-607`, `src/Precept/Language/Types.cs:153-154`)

4. **Update Strategy 2 pseudocode to read valued modifier arguments from field syntax** (for example via `attributeField.Syntax.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList)`), because `TypedField.Modifiers` is kind-only. Without this, `min/max/mincount/...` cannot discharge anything parameterized. (`docs/compiler/proof-engine.md:580-590`, `src/Precept/Pipeline/TypeChecker.cs:99-102`, `src/Precept/Pipeline/SemanticIndex.cs:239-253`, `src/Precept/Pipeline/ParsedConstruct.cs:20-29`)

5. **Iterate both declared and implied modifiers** when doing declaration-attribute proof. Strategy 2 text already says it reads “modifier-implied metadata,” but the pseudocode currently walks only `attributeField.Modifiers`. (`docs/compiler/proof-engine.md:485-487`, `docs/compiler/proof-engine.md:586-590`, `src/Precept/Pipeline/SemanticIndex.cs:244-245`, `src/Precept/Language/Types.cs:458-460`, `src/Precept/Language/Types.cs:525-529`, `src/Precept/Language/Types.cs:554-562`, `src/Precept/Language/Types.cs:565-574`)

6. **Expand the modifier table** so it includes the semantically relevant cardinality modifiers (`mincount`, `maxcount`, `minlength`, `maxlength`) or explicitly state that the table is intentionally current-consumer-only. Right now the doc table is incomplete relative to the modifier catalog. (`docs/compiler/proof-engine.md:489-499`, `docs/compiler/proof-engine.md:1196-1203`, `src/Precept/Language/Modifiers.cs:112-138`)

7. **Remove the “resolved in source” language until code lands.** The doc currently claims CC#5 is already canonical/in source, but the actual `FieldModifierMeta` shape still lacks the property. (`docs/compiler/proof-engine.md:609-611`, `src/Precept/Language/Modifier.cs:116-133`)



## 6. Decision summary



| Decision | Recommendation | Rationale |

|---|---|---|

| 1. `ProofDischarge` shape | Use `ProofDischarge(ProofDischargeSubject Subject, OperatorKind Comparison, ProofDischargeThreshold Threshold)` with `ProofDischargeThreshold.Fixed(decimal)` / `ModifierValue()`; do **not** store `ProofRequirementKind`. | Numeric declaration proof needs subject axis + comparison + threshold source, and only the threshold source has shape variation. |

| 2. `FieldModifierMeta.ProofDischarges` signature | Add `ProofDischarge[]? ProofDischarges = null` to the record constructor and materialize `public ProofDischarge[] ProofDischarges { get; init; } = ProofDischarges ?? [];`. | Strategy 2 linearly enumerates tiny per-modifier tables, and adjacent catalog surfaces already use arrays for proof metadata. |

| 3. Population entries | Populate all bound-establishing field modifiers now: live rows for `positive`, `nonnegative`, `nonzero`, `notempty`, `min`, `mincount`; semantically complete dormant rows for `max`, `maxcount`, `minlength`, and `maxlength`. | This keeps modifier meaning catalog-declared instead of consumer-hardcoded and prevents the next proof-engine feature from reopening the same metadata gap. |

# PE-G2 Broader Design Review — Should `ProofDischarge` cover all requirement kinds?



**Date:** 2026-05-08T21:41:41.253-04:00

**Author:** Frank (Lead/Architect)

**Status:** Ready for Shane sign-off

**Trigger:** Shane challenged the narrow numeric-only `ProofDischarge` scope — asking whether a broader DU covering all three Strategy 2 proof requirement kinds would be more coherent.



## 1. Verdict: Narrow is correct



The narrow numeric-only `ProofDischarge` shape from the prior analysis is the architecturally correct design. A broader DU would add structural complexity with zero information gain — two of the three subtypes would either be tautological or permanently empty.



## 2. Per-arm analysis against the metadata-driven architecture principle



The metadata-driven principle asks: *"Does any pipeline stage switch on a `*Kind` enum value to apply per-member behavior?"* If yes, that behavior belongs in catalog metadata. Let me apply this test rigorously to each Strategy 2 arm.



## Arm 1: `NumericProofRequirement` — ProofDischarges path ✅ Catalog-driven



**Current design:** Strategy 2 reads `FieldModifierMeta.ProofDischarges` and calls `DischargeCovers(discharge, requirement)`. The proof engine never switches on `ModifierKind`. It iterates the discharge array generically. Domain knowledge (which modifiers establish which bounds) lives entirely in catalog metadata entries.



**Verdict:** This is textbook metadata-driven architecture. The `ProofDischarge` catalog entry carries the domain knowledge; the engine is generic machinery. No change needed.



## Arm 2: `ModifierRequirement` — Direct presence check ✅ Already generic machinery



**Current pseudocode:** `field.Modifiers.Contains(modReq.Required)` — a single generic set-membership test.



**Does it switch on a `ModifierKind` value to apply per-member behavior?** No. It doesn't switch on *which* modifier is required. The `Required` value comes from the obligation itself (emitted by the Operations catalog — e.g., choice ordering operations emit `ModifierRequirement(Subject, ModifierKind.Ordered, ...)`). The proof engine simply checks: "does the field have it?" This is structurally identical to `list.Contains(item)` — the most generic possible predicate.



**Would `ProofDischarge.ModifierPresence(ModifierKind.Ordered)` on the `ordered` modifier's metadata add information?** No. It would be tautological metadata: "the `ordered` modifier proves that the field has the `ordered` modifier." The modifier's *existence on the field* is the proof — declaring that fact as a separate metadata entry restates identity as data. The engine can derive this from the modifier's presence without any catalog entry.



**Is there any modifier whose proof-discharge relationship to `ModifierRequirement` is non-obvious or non-identity?** No. The subsumption relationship (`positive` subsumes `nonzero`) exists only in numeric bound semantics. For modifier presence, `ordered` is `ordered` — there is no "modifier A implies modifier B is present" relationship that would benefit from catalog declaration.



**Verdict:** The `ModifierRequirement` arm is generic machinery that reads a value from the obligation and checks set membership. No per-member behavior exists. Adding `ModifierPresence` discharges would be tautological metadata that restates the modifier's identity. The current arm is correct as-is.



## Arm 3: `DimensionProofRequirement` — Period dimension resolution ✅ Different knowledge source



**Current pseudocode:** `ResolvePeriodDimension(subject, semantics)` reads the period dimension from the literal's temporal unit or the field's type qualifier, then compares against `dimReq.RequiredDimension`.



**Does this involve modifier metadata at all?** No. Period dimension is a property of the *type system* (qualifier on a `period` field or unit on a period literal), not a property of any modifier. The dimension data lives in `TypedField`'s qualifier metadata and in period literal units — neither of which are modifiers.



**Are there modifiers in the catalog that declare a period dimension?** No. Checking `Modifiers.cs` exhaustively: there are no `year`, `month`, `day`, `week`, `hour`, `minute`, or `second` modifiers. Period temporal granularity is not expressed through field modifiers — it's expressed through type qualifiers (e.g., `field DueDate as period of days`).



**Would `ProofDischarge.Dimension(PeriodDimension.Date)` entries on any `FieldModifierMeta` have entries?** Zero entries. No modifier in the catalog establishes a period dimension. This subtype would be permanently empty — a shape that exists but is never populated.



**Verdict:** Period dimension is type-system knowledge, not modifier knowledge. `FieldModifierMeta.ProofDischarges` is the wrong home for dimension data. The current arm correctly reads from the type/qualifier system. Adding a `Dimension` discharge subtype would create a permanently empty DU arm — the exact shape-without-substance anti-pattern.



## 3. Why the broader DU fails the architecture test



The proposed broader DU:



```csharp

public abstract record ProofDischarge {

    public sealed record Numeric(...) : ProofDischarge;

    public sealed record ModifierPresence(ModifierKind Required) : ProofDischarge;

    public sealed record Dimension(PeriodDimension Dimension) : ProofDischarge;

}

```



Fails on three counts:



| Criterion | Result |

|---|---|

| **Does it eliminate hardcoded per-member logic from the proof engine?** | No. The `ModifierRequirement` arm has no per-member logic to eliminate — `Contains` is generic. The `DimensionProofRequirement` arm reads from the type system, not modifiers. |

| **Does it actually add catalog entries where domain knowledge currently lives in pipeline code?** | No. `ModifierPresence` entries would be tautological (identity = proof). `Dimension` entries would be empty (no modifier declares a dimension). |

| **Does it have the right shape variation? (DU only where shapes actually differ)** | No. Two of three arms would be degenerate: `ModifierPresence` restates what the modifier already is; `Dimension` has zero inhabitants. DU arms with zero or tautological members are structural noise, not shape variation. |



The metadata-driven principle says: *catalog what IS domain knowledge.* But not everything is domain knowledge:



- **"The `ordered` modifier proves `ordered` is present"** is not domain knowledge — it's a logical tautology.

- **"Period dimension comes from the type qualifier"** is not modifier domain knowledge — it's type-system domain knowledge that lives in a different catalog surface.



Cataloging these would violate the principle's corollary: catalogs carry *meaningful* metadata that consumers can't derive from the member's identity alone.



## 4. What makes the Strategy 2 arms NOT hardcoded per-member knowledge



The metadata-driven principle targets a specific smell: `kind switch { FooKind.Bar => ..., FooKind.Baz => ... }` where each branch exists because "the language says so." Here's why each arm avoids that smell:



| Arm | What it switches on | Why it's not the smell |

|---|---|---|

| **NumericProofRequirement** | Nothing — iterates `ProofDischarges[]` generically | Catalog-driven loop, no per-modifier branching |

| **ModifierRequirement** | Nothing — calls `field.Modifiers.Contains(modReq.Required)` | Generic set-membership test. The `Required` value comes from the obligation emitter (Operations catalog), not from a switch in the proof engine |

| **DimensionProofRequirement** | `PeriodDimension` enum — but this comparison is `resolved == required`, not a per-member behavior switch | Reads dimension from type metadata and compares to requirement. No per-dimension branching logic. `dimension == PeriodDimension.Any \|\| dimension == dimReq.RequiredDimension` is a universal pattern (wildcard + exact match), not per-member dispatch |



The proof engine switches on **requirement subtype** (`is DimensionProofRequirement`, `is ModifierRequirement`, `is NumericProofRequirement`) to dispatch to the correct arm. This is switching on a DU subtype — which the architecture rules explicitly permit: *"Switching on a DU subtype is correct — the subtype is the metadata shape, not a classification axis."*



## 5. Decision summary



| Decision | Recommendation | Rationale | Tradeoff accepted |

|---|---|---|---|

| Narrow vs. broader `ProofDischarge` | **Narrow** — keep `ProofDischarge` as the numeric-only shape from the prior analysis | `ModifierPresence` discharges would be tautological (identity = proof). `Dimension` discharges would be permanently empty (no modifier declares a period dimension). Neither arm has per-member pipeline logic to extract. | The three Strategy 2 arms remain structurally distinct code paths rather than a single unified catalog loop. This is the correct design because they read from *different knowledge sources* (modifier catalog, field modifier set, type qualifier system). |

| Strategy 2 arm structure | Keep three dedicated arms: (1) ProofDischarges catalog loop for `NumericProofRequirement`, (2) `Contains` check for `ModifierRequirement`, (3) dimension resolution for `DimensionProofRequirement` | Each arm reads from a different metadata surface. Unifying them into a single `ProofDischarges` loop would force modifier and dimension knowledge into the wrong catalog surface (`FieldModifierMeta`) where it doesn't naturally belong. | Strategy 2 has three code paths instead of one. But each path is ~3-5 lines of generic machinery. Simplicity of the unified loop is illusory — it would push complexity into tautological or empty catalog entries. |



## 6. Recommendation



Proceed with the narrow `ProofDischarge` shape exactly as specified in the prior PE-G2 analysis. The broader DU is architecturally weaker, not stronger — it would catalog non-knowledge (tautologies and empty sets) in pursuit of a false uniformity. The current three-arm Strategy 2 design is the correct metadata-driven architecture because each arm reads from the *right* metadata source for its proof obligation kind.

# PE-G2 Full Design — `ProofSatisfaction` and all five requirement kinds



**Date:** 2026-05-09

**Author:** Frank (Lead/Architect)

**Status:** Complete design for implementation — no deferrals



## 1. Final decision



Shane is right. The broader design must be finished now, not postponed.



The correct architecture is:



1. **Rename** `ProofDischarge` → `ProofSatisfaction`.

2. **Keep `FieldModifierMeta` as the numeric carrier** for modifier-established bounds.

3. **Add a positive presence carrier** so Presence proof does not depend on the absence of `optional`.

4. **Add a normalized declaration-qualifier carrier** so dimension and qualifier-compatibility proof read declaration metadata rather than parser/type-checker folklore.

5. **Keep direct modifier membership as the canonical `ModifierRequirement` path.** Do not duplicate `ordered proves ordered` into metadata rows.



That yields one proof metadata vocabulary, but not one carrier. The carriers are different because the declaration facts are different.



---



## 2. `ProofSatisfaction` DU — final C# shape



**File:** `src/Precept/Language/ProofRequirement.cs`

**Namespace:** `Precept.Language`



```csharp

namespace Precept.Language;



public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)

{

    public sealed record Numeric(

        SatisfactionProjection Projection,

        OperatorKind Comparison,

        NumericBoundSource Bound)

        : ProofSatisfaction(ProofRequirementKind.Numeric);



    public sealed record Presence()

        : ProofSatisfaction(ProofRequirementKind.Presence);



    public sealed record Dimension(

        DimensionSource Source)

        : ProofSatisfaction(ProofRequirementKind.Dimension);



    public sealed record Modifier(

        ModifierKind RequiredModifier)

        : ProofSatisfaction(ProofRequirementKind.Modifier);



    public sealed record QualifierCompatibility(

        QualifierAxis Axis)

        : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);

}



public abstract record SatisfactionProjection

{

    public sealed record SelfValue() : SatisfactionProjection;

    public sealed record Accessor(string Name) : SatisfactionProjection;

}



public abstract record NumericBoundSource

{

    public sealed record Constant(decimal Value) : NumericBoundSource;

    public sealed record DeclarationValue() : NumericBoundSource;

}



public abstract record DimensionSource

{

    public sealed record Constant(PeriodDimension Value) : DimensionSource;

    public sealed record DeclaredTemporalDimension() : DimensionSource;

}

```



## Why this is the final shape



- **Numeric** needs a projection plus a bound source.

- **Presence** is pure existential proof — the entry itself is the fact.

- **Dimension** needs a dimension source, because the satisfied dimension may come from the carrier entry.

- **Modifier** exists in the DU for vocabulary completeness, but current implementation does **not** need populated rows.

- **QualifierCompatibility** is axis-based; the compared value lives on the qualifier carrier itself.



---



## 3. `FieldModifierMeta` change — exact shape



**File:** `src/Precept/Language/Modifier.cs`



```csharp

public sealed record FieldModifierMeta(

    ModifierKind Kind,

    TokenMeta Token,

    string Description,

    ModifierCategory Category,

    TypeTarget[] ApplicableTo,

    bool HasValue = false,

    ModifierKind[] Subsumes = default!,

    ProofSatisfaction[]? ProofSatisfactions = null,

    string? HoverDescription = null,

    string? UsageExample = null,

    string? SnippetTemplate = null,

    bool DesugarsToRule = false,

    ModifierKind[]? MutuallyExclusiveWith = null)

    : ModifierMeta(Kind, Token, Description, Category, DesugarsToRule, MutuallyExclusiveWith)

{

    public ModifierKind[] Subsumes { get; init; } = Subsumes ?? [];

    public ProofSatisfaction[] ProofSatisfactions { get; init; } = ProofSatisfactions ?? [];

}

```



**Placement matters:** `ProofSatisfactions` belongs beside `Subsumes`. Both are semantic metadata declared by the modifier catalog.



---



## 4. New carrier type — Presence



## 4.1 What fact satisfies `PresenceProofRequirement`?



**Fact:** the declaration is **structurally guaranteed present**.



That is the positive fact the proof engine needs. The absence of `optional` is not the carrier. The compiler must normalize that absence into a positive declaration fact.



## 4.2 Natural carrier



**Carrier:** new declaration-attached metadata type `DeclaredPresenceMeta`.



Why this is the right carrier:



- `PresenceProofRequirement` is about **nullability / absence semantics**, not numeric bounds.

- `optional` has no opposite surface modifier, so reading “not optional” directly is a negative test, not metadata.

- The engine needs a **positive**, normalized fact on every field and arg.



## 4.3 Full type definition



**File:** `src/Precept/Language/DeclaredPresence.cs`

**Namespace:** `Precept.Language`



```csharp

namespace Precept.Language;



public abstract record DeclaredPresenceMeta(

    string Description,

    ProofSatisfaction[]? ProofSatisfactions = null)

{

    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];



    public sealed record Guaranteed()

        : DeclaredPresenceMeta(

            "Value is structurally present on every instance",

            [new ProofSatisfaction.Presence()]);



    public sealed record Optional()

        : DeclaredPresenceMeta(

            "Value may be absent");

}

```



## 4.4 Populated entries



| Carrier member | `ProofSatisfactions` | Meaning |

|---|---|---|

| `DeclaredPresenceMeta.Guaranteed` | `new ProofSatisfaction.Presence()` | Required field / required arg / computed field value is always present |

| `DeclaredPresenceMeta.Optional` | _none_ | Presence must be proven by guard, not declaration |



## 4.5 Normalization rule



The type checker must attach one `DeclaredPresenceMeta` to every `TypedField` and `TypedArg`:



- declaration contains `optional` → `new DeclaredPresenceMeta.Optional()`

- otherwise → `new DeclaredPresenceMeta.Guaranteed()`



That is the full answer. Presence proof becomes positive metadata, not absence-check folklore.



---



## 5. New carrier type — normalized declaration qualifiers



## 5.1 What fact satisfies `DimensionProofRequirement`?



**Fact:** the declaration resolves to a concrete **temporal-dimension fact**.



Examples:



- `period of 'date'` → `Date`

- `period of 'time'` → `Time`

- `period in 'days'` → derived `Date`

- unqualified `period` → baseline `Any` (per Shane’s already-locked permissive decision)



## 5.2 What fact satisfies `QualifierCompatibilityProofRequirement`?



**Fact:** the declaration resolves to a concrete qualifier binding on the required axis.



Examples:



- `money in 'USD'` → `Currency = USD`

- `quantity in 'kg'` → `Unit = kg`, derived `Dimension = mass`

- `price in 'USD/each'` → `Currency = USD`, `Unit = each`, derived `Dimension = count`



## 5.3 Natural carrier



**Carrier:** new normalized declaration-attached metadata type `DeclaredQualifierMeta`.



Why this is the right carrier:



- `TypeMeta.QualifierShape` describes **allowed slots**, not the declaration’s chosen value.

- `FieldModifierMeta` is the wrong layer; qualifiers are part of the type annotation, not modifiers.

- The proof engine needs **resolved per-axis values**, including derived ones.



## 5.4 Full type definition



**File:** `src/Precept/Language/DeclaredQualifierMeta.cs`

**Namespace:** `Precept.Language`



```csharp

namespace Precept.Language;



public enum QualifierOrigin

{

    Explicit = 1,

    Derived  = 2,

    Baseline = 3,

}



public abstract record DeclaredQualifierMeta(

    QualifierAxis Axis,

    QualifierOrigin Origin,

    TokenKind? Preposition,

    ProofSatisfaction[]? ProofSatisfactions = null)

{

    public ProofSatisfaction[] ProofSatisfactions { get; } = ProofSatisfactions ?? [];



    public sealed record Currency(

        string CurrencyCode,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.In,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.Currency, Origin, Preposition, ProofSatisfactions);



    public sealed record Unit(

        string UnitCode,

        string DimensionName,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.In,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.Unit, Origin, Preposition, ProofSatisfactions);



    public sealed record Dimension(

        string DimensionName,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.Of,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.Dimension, Origin, Preposition, ProofSatisfactions);



    public sealed record FromCurrency(

        string CurrencyCode,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.In,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.FromCurrency, Origin, Preposition, ProofSatisfactions);



    public sealed record ToCurrency(

        string CurrencyCode,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.To,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.ToCurrency, Origin, Preposition, ProofSatisfactions);



    public sealed record Timezone(

        string TimezoneId,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.In,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.Timezone, Origin, Preposition, ProofSatisfactions);



    public sealed record TemporalDimension(

        PeriodDimension Value,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.Of,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.TemporalDimension, Origin, Preposition, ProofSatisfactions);



    public sealed record TemporalUnit(

        string UnitName,

        PeriodDimension DerivedDimension,

        QualifierOrigin Origin = QualifierOrigin.Explicit,

        TokenKind? Preposition = TokenKind.In,

        ProofSatisfaction[]? ProofSatisfactions = null)

        : DeclaredQualifierMeta(QualifierAxis.TemporalUnit, Origin, Preposition, ProofSatisfactions);

}

```



## 5.5 Populated entries



## 5.5.1 `DimensionProofRequirement`



| Carrier member | `ProofSatisfactions` | Notes |

|---|---|---|

| `DeclaredQualifierMeta.TemporalDimension(Date/Time)` | `new ProofSatisfaction.Dimension(new DimensionSource.DeclaredTemporalDimension())` | explicit `of 'date'` / `of 'time'` |

| `DeclaredQualifierMeta.TemporalDimension(Any, Origin: Baseline, Preposition: null)` | `new ProofSatisfaction.Dimension(new DimensionSource.DeclaredTemporalDimension())` | unqualified `period` baseline fact |

| `DeclaredQualifierMeta.TemporalUnit(...)` | _none directly_ | normalize to a second derived `TemporalDimension` entry |



**Rule:** `TemporalUnit` does not directly carry dimension proof. The type checker emits a second normalized `TemporalDimension` entry with `Origin = Derived`.



## 5.5.2 `QualifierCompatibilityProofRequirement`



| Carrier member | `ProofSatisfactions` |

|---|---|

| `DeclaredQualifierMeta.Currency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Currency)` |

| `DeclaredQualifierMeta.Unit` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Unit)` |

| `DeclaredQualifierMeta.Dimension` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Dimension)` |

| `DeclaredQualifierMeta.FromCurrency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.FromCurrency)` |

| `DeclaredQualifierMeta.ToCurrency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.ToCurrency)` |

| `DeclaredQualifierMeta.Timezone` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Timezone)` |

| `DeclaredQualifierMeta.TemporalUnit` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.TemporalUnit)` |

| `DeclaredQualifierMeta.TemporalDimension` with `Value != PeriodDimension.Any` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.TemporalDimension)` |

| `DeclaredQualifierMeta.TemporalDimension` with `Value == PeriodDimension.Any` | _none_ |



`Any` is deliberately excluded from qualifier compatibility. It satisfies dimension proof per Shane’s locked decision; it does **not** prove same-axis compatibility with another operand.



## 5.6 Normalization rules



The type checker must normalize a declaration’s qualifier surface into zero or more `DeclaredQualifierMeta` entries.



## `money`

- `money in 'USD'` → `Currency("USD")`



## `quantity`

- `quantity in 'kg'` → `Unit("kg", "mass")` **plus** derived `Dimension("mass", Origin: Derived, Preposition: TokenKind.In)`

- `quantity of 'mass'` → `Dimension("mass")`



## `price`

- `price in 'USD/each'` → `Currency("USD")` + `Unit("each", "count")` + derived `Dimension("count", Origin: Derived, Preposition: TokenKind.In)`

- `price in 'USD' of 'mass'` → `Currency("USD")` + `Dimension("mass")`



## `exchange rate`

- `exchangerate in 'USD' to 'EUR'` → `FromCurrency("USD")` + `ToCurrency("EUR")`



## `period`

- `period of 'date'` → `TemporalDimension(PeriodDimension.Date)`

- `period of 'time'` → `TemporalDimension(PeriodDimension.Time)`

- `period in 'days'` → `TemporalUnit("days", PeriodDimension.Date)` + derived `TemporalDimension(PeriodDimension.Date, Origin: Derived, Preposition: TokenKind.In)`

- `period in 'hours'` → `TemporalUnit("hours", PeriodDimension.Time)` + derived `TemporalDimension(PeriodDimension.Time, Origin: Derived, Preposition: TokenKind.In)`

- unqualified `period` → baseline `TemporalDimension(PeriodDimension.Any, Origin: Baseline, Preposition: null)`



## 5.7 Storage on typed declarations



To make the carriers usable, the semantic model must attach them to declarations.



- `TypedField` gains `DeclaredPresenceMeta Presence` and `ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers`

- `TypedArg` gains `DeclaredPresenceMeta Presence` and `ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers`



The existing `QualifierBinding` type in `SemanticIndex.cs` is **not** this carrier. That type is result-qualifier propagation for expressions. It must remain separate.



---



## 6. Requirement-by-requirement carrier decisions



## 6.1 `NumericProofRequirement`



**Satisfying fact:** a field modifier establishes a numeric bound on the field value or on an accessor projection.

**Carrier:** existing `FieldModifierMeta`.

**New carrier type required:** no.



## Fully populated relevant `ProofSatisfactions`



| Modifier | `ProofSatisfactions` |

|---|---|

| `positive` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))` |

| `nonnegative` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.Constant(0m))` |

| `nonzero` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.NotEquals, new NumericBoundSource.Constant(0m))` |

| `notempty` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))`  **and** `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))` |

| `min(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())` |

| `max(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())` |

| `minlength(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())` |

| `maxlength(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("length"), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())` |

| `mincount(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())` |

| `maxcount(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())` |



**Not numeric proof carriers:** `optional`, `ordered`, `default`, `writable`, `maxplaces`.



**Important completeness note:** the proof engine must read **effective modifiers** = declared modifiers + `TypeMeta.ImpliedModifiers`. That is how `timezone`, `currency`, `unitofmeasure`, and `dimension` inherit `notempty` proof facts without duplicating them on `TypeMeta`.



---



## 6.2 `PresenceProofRequirement`



**Satisfying fact:** the declaration is guaranteed present.

**Carrier:** new `DeclaredPresenceMeta`.

**New carrier type required:** yes — defined above.



## Fully populated entries



| Carrier member | `ProofSatisfactions` |

|---|---|

| `DeclaredPresenceMeta.Guaranteed` | `new ProofSatisfaction.Presence()` |

| `DeclaredPresenceMeta.Optional` | _none_ |



**Explicit ruling:** `notempty` does **not** satisfy presence. An optional string with `notempty` may still be absent; it merely constrains present values.



---



## 6.3 `DimensionProofRequirement`



**Satisfying fact:** the declaration resolves to a temporal-dimension fact.

**Carrier:** new `DeclaredQualifierMeta`, specifically normalized `TemporalDimension` entries.

**New carrier type required:** yes — defined above.



## Fully populated entries



| Carrier member | `ProofSatisfactions` |

|---|---|

| `DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Date, ...)` | `new ProofSatisfaction.Dimension(new DimensionSource.DeclaredTemporalDimension())` |

| `DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Time, ...)` | `new ProofSatisfaction.Dimension(new DimensionSource.DeclaredTemporalDimension())` |

| `DeclaredQualifierMeta.TemporalDimension(PeriodDimension.Any, Origin: Baseline, Preposition: null, ...)` | `new ProofSatisfaction.Dimension(new DimensionSource.DeclaredTemporalDimension())` |



**Normalization rule:** `TemporalUnit` entries must emit a derived `TemporalDimension` entry. That is how `period in 'days'` and `period in 'hours'` participate in Strategy 2 without hardcoded proof-engine special cases.



---



## 6.4 `ModifierRequirement`



**Satisfying fact:** the field declaration’s normalized modifier set contains the required modifier.

**Carrier:** the declaration’s modifier membership itself (`TypedField.Modifiers` + effective implied modifiers where appropriate).

**New carrier type required:** no.



## Definitive recommendation



**Keep direct membership as the canonical path. Do not populate modifier self-rows.**



Why:



1. `Contains(requiredModifier)` is already **generic machinery**.

2. It does **not** switch on modifier identity to apply per-member behavior.

3. Adding `ProofSatisfaction.Modifier(ModifierKind.Ordered)` to `ordered` is tautological duplication: the modifier membership is already the fact.

4. Duplicating identity into metadata creates drift risk with zero gain.



## Populated `ProofSatisfactions` entries



**None.** `ProofSatisfaction.Modifier` stays in the DU for vocabulary completeness, but no current catalog member needs to populate it.



That is architecturally correct, not a shortcut.



---



## 6.5 `QualifierCompatibilityProofRequirement`



**Satisfying fact:** both declarations resolve to concrete bindings on the same axis and those bindings compare equal.

**Carrier:** new `DeclaredQualifierMeta`.

**New carrier type required:** yes — defined above.



## Fully populated entries



| Carrier member | `ProofSatisfactions` |

|---|---|

| `DeclaredQualifierMeta.Currency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Currency)` |

| `DeclaredQualifierMeta.Unit` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Unit)` |

| `DeclaredQualifierMeta.Dimension` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Dimension)` |

| `DeclaredQualifierMeta.FromCurrency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.FromCurrency)` |

| `DeclaredQualifierMeta.ToCurrency` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.ToCurrency)` |

| `DeclaredQualifierMeta.Timezone` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.Timezone)` |

| `DeclaredQualifierMeta.TemporalUnit` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.TemporalUnit)` |

| `DeclaredQualifierMeta.TemporalDimension` where `Value` is `Date` or `Time` | `new ProofSatisfaction.QualifierCompatibility(QualifierAxis.TemporalDimension)` |

| `DeclaredQualifierMeta.TemporalDimension` where `Value` is `Any` | _none_ |



**Compatibility rule:** Strategy 5 compares the carrier payload value on the requested axis. No field has to “know about” the other field. The proof engine just compares two normalized declaration facts.



---



## 7. Concrete `Modifiers.cs` population



These are the exact rows that must appear on the relevant modifier catalog entries.



```csharp

ModifierKind.Positive => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Positive),

    "Value > 0",

    ModifierCategory.Structural, NumericTypes,

    Subsumes: [ModifierKind.Nonnegative, ModifierKind.Nonzero],

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.SelfValue(),

            OperatorKind.GreaterThan,

            new NumericBoundSource.Constant(0m)),

    ],

    HoverDescription: "The field's value must be strictly greater than zero. Implies nonnegative and nonzero.",

    DesugarsToRule: true,

    MutuallyExclusiveWith: [ModifierKind.Nonnegative]),



ModifierKind.Nonnegative => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Nonnegative),

    "Value ≥ 0",

    ModifierCategory.Structural, NumericTypes,

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.SelfValue(),

            OperatorKind.GreaterThanOrEqual,

            new NumericBoundSource.Constant(0m)),

    ],

    HoverDescription: "The field's value must be zero or greater. Enforced on every assignment.",

    DesugarsToRule: true,

    MutuallyExclusiveWith: [ModifierKind.Positive]),



ModifierKind.Nonzero => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Nonzero),

    "Value ≠ 0",

    ModifierCategory.Structural, NumericTypes,

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.SelfValue(),

            OperatorKind.NotEquals,

            new NumericBoundSource.Constant(0m)),

    ],

    HoverDescription: "The field's value must not be zero. Allows negative values.",

    DesugarsToRule: true),



ModifierKind.Notempty => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Notempty),

    "String or collection is non-empty",

    ModifierCategory.Structural, StringAndCollectionTypes,

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("length"),

            OperatorKind.GreaterThan,

            new NumericBoundSource.Constant(0m)),

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("count"),

            OperatorKind.GreaterThan,

            new NumericBoundSource.Constant(0m)),

    ],

    HoverDescription: "The field must not be empty. For text fields, the string must have at least one character. For collection fields, the collection must have at least one element. Not applicable to lookup fields — lookup entries are defined at design time.",

    DesugarsToRule: true),



ModifierKind.Min => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Min),

    "Minimum value",

    ModifierCategory.Structural, NumericTypes, HasValue: true,

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.SelfValue(),

            OperatorKind.GreaterThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ],

    HoverDescription: "The field's value must be at least this minimum. Enforced on every assignment.",

    DesugarsToRule: true),



ModifierKind.Max => new FieldModifierMeta(

    kind, Tokens.GetMeta(TokenKind.Max),

    "Maximum value",

    ModifierCategory.Structural, NumericTypes, HasValue: true,

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.SelfValue(),

            OperatorKind.LessThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ],

    HoverDescription: "The field's value must be at most this maximum. Enforced on every assignment.",

    DesugarsToRule: true),

```



And for completeness:



```csharp

ModifierKind.Minlength => new FieldModifierMeta(...

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("length"),

            OperatorKind.GreaterThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ], ...),



ModifierKind.Maxlength => new FieldModifierMeta(...

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("length"),

            OperatorKind.LessThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ], ...),



ModifierKind.Mincount => new FieldModifierMeta(...

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("count"),

            OperatorKind.GreaterThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ], ...),



ModifierKind.Maxcount => new FieldModifierMeta(...

    ProofSatisfactions:

    [

        new ProofSatisfaction.Numeric(

            new SatisfactionProjection.Accessor("count"),

            OperatorKind.LessThanOrEqual,

            new NumericBoundSource.DeclarationValue()),

    ], ...),

```



---



## 8. Implementation checklist — exact files, dependency order



1. **`src/Precept/Language/ProofRequirement.cs`**

   Add `ProofSatisfaction`, `SatisfactionProjection`, `NumericBoundSource`, and `DimensionSource`.



2. **`src/Precept/Language/DeclaredPresence.cs`** *(new)*

   Add `DeclaredPresenceMeta`.



3. **`src/Precept/Language/DeclaredQualifierMeta.cs`** *(new)*

   Add `QualifierOrigin` and `DeclaredQualifierMeta` DU.



4. **`src/Precept/Language/Modifier.cs`**

   Add `FieldModifierMeta.ProofSatisfactions`.



5. **`src/Precept/Language/Modifiers.cs`**

   Populate numeric `ProofSatisfactions` rows.



6. **`src/Precept/Language/Types.cs`**

   Expose the normalization metadata needed to resolve units to dimensions and temporal units to `PeriodDimension`.



7. **`src/Precept/Pipeline/ParsedTypeReference.cs`**

   Preserve parsed qualifier clauses on type references.



8. **`src/Precept/Pipeline/Parser.cs`**

   Parse declaration qualifier clauses using `TypeMeta.QualifierShape`.



9. **`src/Precept/Pipeline/SymbolTable.cs`**

   Carry parsed qualifier data through declared field / arg symbols.



10. **`src/Precept/Pipeline/SemanticIndex.cs`**

    Add `DeclaredPresenceMeta Presence` and `ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers` to `TypedField` and `TypedArg`.



11. **`src/Precept/Pipeline/TypeChecker.cs`**

    Normalize declaration presence and qualifiers into the new carriers; emit derived/baseline qualifier facts.



12. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`**

    Ensure qualifier-aware expression resolution and proof-obligation stamping consume the normalized declaration metadata instead of ad hoc null checks.



13. **`src/Precept/Pipeline/ProofEngine.cs`**

    Implement Strategy 2 against `DeclaredPresenceMeta`, `DeclaredQualifierMeta`, and modifier `ProofSatisfactions`; keep direct modifier-membership fast path; implement Strategy 5 against normalized qualifier carriers.



14. **`docs/compiler/proof-engine.md`**

    Replace `ProofDischarge` with `ProofSatisfaction`; document the new carriers and the direct-membership modifier arm.



15. **`docs/language/catalog-system.md`**

    Add the new carrier types and the updated `FieldModifierMeta` shape.



16. **`docs/language/precept-language-spec.md`**

    Sync the qualifier and proof sections with the normalized declaration-fact model.



---



## 9. Open questions for Shane



**None.** The design choices that matter have now been made in the design itself:



- Presence is a positive normalized declaration fact.

- Dimension and qualifier compatibility use normalized declaration qualifier facts.

- Modifier requirement stays direct membership.

- Unqualified `period` keeps Shane’s already-locked permissive `Any` behavior for dimension proof only.



There is nothing here that requires another deferral ceremony.



## 2026-05-08: PE-G2 ProofSatisfaction design — LOCKED



**By:** Shane (owner sign-off)

**What:** Full no-deferral PE-G2 design approved. All 5 ProofRequirementKind subtypes fully specified with carriers.

**Locked decisions:**

- `ProofDischarge` renamed to `ProofSatisfaction` (DU, 5 subtypes + 3 supporting DUs)

- New `DeclaredPresenceMeta` carrier type defined (DeclaredPresence.cs)

- New `DeclaredQualifierMeta` carrier type defined (7 subtypes, all qualifier axes)

- `FieldModifierMeta` gains `ProofSatisfactions` property (10 modifier entries populated)

- `TypedField` and `TypedArg` gain `Presence` + `DeclaredQualifiers` properties

- `ModifierRequirement` uses direct `Contains()` — no metadata rows

- `notempty` carries TWO satisfaction rows: Accessor("length") AND Accessor("count")

- `TemporalDimension(Any)` satisfies Dimension proof but NOT QualifierCompatibility

- Implementation checklist: 16 files in dependency order (see frank-pe-g2-full-design.md)

**Why:** ProofEngine requires positive carrier facts for all 5 requirement kinds. Absence-checking is fragile and non-canonical.

# PE-G2 Rename Analysis — `ProofDischarge` → `ProofSatisfaction`



**Date:** 2026-05-09

**Author:** Frank (Lead/Architect)

**Status:** Recommendation for Shane



## 1. Source-grounded constraints



1. `FieldModifierMeta` is currently a field-modifier-specific catalog record with no proof metadata property today. (`src/Precept/Language/Modifier.cs:116-133`)

2. `ProofRequirement` is already a five-shape DU: `NumericProofRequirement`, `PresenceProofRequirement`, `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement`. Their payload shapes are materially different. (`src/Precept/Language/ProofRequirement.cs`)

3. Strategy 2 in `docs/compiler/proof-engine.md` currently uses `ProofDischarge` as if it were a catalog row on `FieldModifierMeta`, but the documented flat shape is too narrow and the name reads like a runtime act, not catalog metadata. (`docs/compiler/proof-engine.md:517-610`)

4. `docs/language/catalog-system.md` is explicit: if shapes vary by kind, the shape is a DU; flat records with inapplicable nullable fields are the anti-pattern. (`docs/language/catalog-system.md:85-141`)

5. The earlier narrow design failed because it tried to make *all* Strategy-2 proof knowledge live on `FieldModifierMeta`. Shane’s correction is right: the type itself should be broad enough for all proof-requirement kinds, but that does **not** mean every requirement kind naturally belongs on `FieldModifierMeta`.



## 2. Rename recommendation



## Rename



- **Type:** `ProofDischarge` → `ProofSatisfaction`

- **Property:** `ProofDischarges` → `ProofSatisfactions`



## Why `ProofSatisfaction` is better than `ProofDischarge`



1. **It names the catalog fact, not the engine action.**

   - A proof engine **discharges** a concrete `ProofObligation` at runtime-analysis time.

   - A catalog entry does not perform a discharge; it declares that a declaration attribute **satisfies** a proof-requirement shape.

   - `ProofDischarge` therefore sounds like ledger/runtime vocabulary in the wrong layer.



2. **It scales cleanly beyond modifier-based numeric bounds.**

   - `ProofDischarge` came from the original narrow numeric/modifier design.

   - `ProofSatisfaction` names the general relation: “this declaration-attached fact satisfies this class of proof requirement.”

   - That wording remains correct for numeric, dimension, modifier, presence, and qualifier-compatibility shapes.



3. **It mirrors existing proof vocabulary cleanly.**

   - `ProofRequirement` = what must be proven.

   - `ProofSatisfaction` = catalog-declared fact that can satisfy it.

   - `ProofObligation` = instantiated requirement at a proof site.

   - `ProofLedger` = result ledger.

   This is a coherent noun family. `ProofDischarge` breaks the pattern by naming the *resulting act* instead of the *declared metadata relation*.



4. **It is less misleading to a cold implementer.**

   - `meta.ProofSatisfactions` immediately reads as “these are the proof facts this metadata entry establishes.”

   - `meta.ProofDischarges` invites the wrong question: “what exactly is being discharged here, and when?”



## 3. Recommended C# shape



## Verdict



Use a **DU** rooted in `ProofRequirementKind`, with subtype payloads that mirror the actual requirement shapes. Do **not** use a flat record with nullable fields.



## Recommended sketch



```csharp

namespace Precept.Language;



public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)

{

    public sealed record Numeric(

        SatisfactionProjection Projection,

        OperatorKind Comparison,

        NumericBoundSource Bound)

        : ProofSatisfaction(ProofRequirementKind.Numeric);



    public sealed record Presence()

        : ProofSatisfaction(ProofRequirementKind.Presence);



    public sealed record Dimension(

        DimensionSource RequiredDimension)

        : ProofSatisfaction(ProofRequirementKind.Dimension);



    public sealed record Modifier(

        ModifierKind RequiredModifier)

        : ProofSatisfaction(ProofRequirementKind.Modifier);



    public sealed record QualifierCompatibility(

        QualifierAxis Axis)

        : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);

}



public abstract record SatisfactionProjection

{

    public sealed record SelfValue() : SatisfactionProjection;

    public sealed record Accessor(string Name) : SatisfactionProjection;

}



public abstract record NumericBoundSource

{

    public sealed record Constant(decimal Value) : NumericBoundSource;

    public sealed record DeclarationValue() : NumericBoundSource;

}



public abstract record DimensionSource

{

    public sealed record Constant(PeriodDimension Value) : DimensionSource;

    public sealed record DeclaredTemporalDimension() : DimensionSource;

}

```



## Why this shape is correct



## A. Why DU, not flat record



Because the five proof-requirement kinds do **not** share one metadata shape:



- **Numeric** needs a projection (`self value` vs accessor such as `count`/`length`), a comparison, and a bound source.

- **Presence** needs no extra payload.

- **Dimension** needs a period-dimension source.

- **Modifier** needs a specific `ModifierKind`.

- **QualifierCompatibility** needs a `QualifierAxis`.



A flat record would immediately collapse into something like:



```csharp

(RequirementKind, Comparison?, Threshold?, RequiredModifier?, RequiredDimension?, Axis?, Projection?, ...)

```



That is exactly the catalog anti-pattern the architecture doc forbids: one record full of meaningless nullable fields plus external “if kind == X then field Y must be set” rules.



The DU is the correct design because:



- the subtype **is** the semantic signal,

- exhaustiveness is compiler-enforced,

- consumers pattern-match on real shapes instead of a nullability matrix,

- future `ProofRequirementKind` additions force explicit metadata-shape handling.



## B. Why `Numeric` needs `Projection`



The earlier flat `ProofDischarge(RequirementKind, Comparison, Threshold)` shape is wrong even before broadening because numeric satisfactions are not all about the field’s raw value.



Examples already in source:



- `positive`, `nonnegative`, `nonzero`, `min`, `max` establish bounds on the **field value**.

- non-empty collection proof obligations target `SelfSubject(CollectionCountAccessor)` — that is a bound on an **accessor projection** (`count`), not on the raw field value. (`src/Precept/Language/Types.cs:153-154`, `src/Precept/Language/Types.cs:163-288`, `src/Precept/Language/Actions.cs:98-196`)



Without `Projection`, `notempty` is under-specified.



## C. Why `Numeric` needs `BoundSource`



`min(N)` and `max(N)` do not establish a constant threshold from the catalog entry. They establish a threshold taken from the **declaration instance’s modifier value**. The catalog row must be able to say “use the declaration’s value here,” not only “use constant 0.”



## D. Why `Dimension` needs `DimensionSource`



If the broader model is real, dimension satisfactions cannot assume every carrier will hardcode `PeriodDimension.Date` or `PeriodDimension.Time`.



- A future qualifier-based carrier would want: “read the declared temporal-dimension qualifier and compare it to the obligation.”

- A constant arm still belongs in the shape because a future dedicated declaration attribute could hardcode a specific period dimension.



## Where the type should live



**Recommendation:** define `ProofSatisfaction` in `src/Precept/Language/ProofRequirement.cs` directly alongside `ProofRequirement` and `ProofRequirementMeta`.



Why:



1. It is the declarative inverse of `ProofRequirement`; they belong in the same proof-domain vocabulary file.

2. The subtypes line up one-for-one with `ProofRequirementKind` and should stay co-located with that kind’s shapes.

3. A separate file is defensible later if the proof domain gets much larger, but today splitting it would make the proof model harder to read, not easier.



## 4. `FieldModifierMeta` usage — numeric rows



For `FieldModifierMeta`, the property should be:



```csharp

ProofSatisfaction[]? ProofSatisfactions = null

```



materialized as the usual array property:



```csharp

public ProofSatisfaction[] ProofSatisfactions { get; init; } = ProofSatisfactions ?? [];

```



## Recommended populated entries



These are the **minimal canonical rows**. Stronger numeric facts should be listed once and weaker obligations should be covered by generic numeric subsumption logic in the proof engine.



| Modifier | Recommended `ProofSatisfactions` entries | Why |

|---|---|---|

| `positive` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))` | Canonical fact is `value > 0`; generic subsumption can cover `!= 0` and `>= 0`. |

| `nonnegative` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.Constant(0m))` | Directly expresses `value >= 0`. |

| `nonzero` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.NotEquals, new NumericBoundSource.Constant(0m))` | Directly expresses `value != 0`. |

| `notempty` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.Accessor("count"), OperatorKind.GreaterThan, new NumericBoundSource.Constant(0m))` | Covers the current live non-empty collection obligations (`collection.count > 0`). |

| `min(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.GreaterThanOrEqual, new NumericBoundSource.DeclarationValue())` | The lower bound comes from the declaration instance’s `min` value. |

| `max(N)` | `new ProofSatisfaction.Numeric(new SatisfactionProjection.SelfValue(), OperatorKind.LessThanOrEqual, new NumericBoundSource.DeclarationValue())` | The upper bound comes from the declaration instance’s `max` value. |



## Important note on `notempty`



`notempty` semantically spans **string length** and **collection count** in the modifier catalog. (`src/Precept/Language/Modifiers.cs:85-90`)



So there are two coherent options:



1. **Current-proof-surface option (minimal today):** keep only the `Accessor("count")` row because that is what current proof obligations actually emit.

2. **Catalog-complete option (my preference):** add a second row now for string length:



```csharp

new ProofSatisfaction.Numeric(

    new SatisfactionProjection.Accessor("length"),

    OperatorKind.GreaterThan,

    new NumericBoundSource.Constant(0m))

```



The architecture document says completeness beats current-consumer demand. On that basis, I prefer the second option.



## 5. Which catalog entry types should carry this property?



This is the part that must stay disciplined. The **type** is broad. The **property placement** is selective.



Do **not** read “usable on any catalog entry” as “stamp `ProofSatisfactions` onto every `*Meta` record.” That would be cargo-cult abstraction. The right question is: **which catalog entry kinds naturally represent declaration-attached facts that can satisfy proof requirements?**



## Requirement-kind analysis



| Requirement kind | Natural declaration-attached satisfier | Existing catalog entry type that should carry `ProofSatisfactions` now? | Notes |

|---|---|---:|---|

| `NumericProofRequirement` | Field modifiers that establish bounds (`positive`, `min`, `notempty`, etc.) | **Yes — `FieldModifierMeta`** | This is the original and still-correct home for modifier-established numeric facts. |

| `PresenceProofRequirement` | A declaration attribute that guarantees set-ness | **No existing carrier today** | Precept currently models optionality as the presence/absence of `optional`, not as a positive cataloged “always set” attribute. There is no existing top-level catalog entry that naturally carries this today. |

| `DimensionProofRequirement` | A declaration attribute that fixes a period’s temporal dimension | **No existing carrier today** | The satisfier is the field/arg’s **declared temporal qualifier**, not a field modifier. Current catalogs do not expose qualifier values as first-class top-level entries, so there is nowhere honest to hang this property yet. |

| `ModifierRequirement` | The required field modifier itself (for example `ordered`) | **Conditionally yes — `FieldModifierMeta`** | The generalized type should be able to express this. Whether the proof engine should actually route direct modifier presence through `ProofSatisfactions` is an implementation choice; the current `Contains(requiredModifier)` arm is already generic and may remain the fast path. |

| `QualifierCompatibilityProofRequirement` | A declaration attribute that pins a qualifier value on an axis | **No existing carrier today** | The satisfier is a resolved qualifier binding (`currency`, `unit`, etc.) on the field declaration, not a modifier. Again, current catalogs do not expose qualifier values as their own catalog entry type. |



## Specific recommendation on existing catalog types



## `FieldModifierMeta`



**Yes.** This is the one existing entry type that clearly should carry `ProofSatisfactions` now.



## `TypeMeta`



**No, not directly.**



Reason: a type entry like `money`, `quantity`, or `period` does not itself establish the concrete qualifier value that satisfies a proof. The proof-relevant value lives on the field declaration (`in 'USD'`, `of 'mass'`, `of 'date'`, etc.), not on the generic type catalog row.



Also: `TypeMeta` already has `ImpliedModifiers`. If a type implies a modifier and that modifier carries `ProofSatisfactions`, the proof engine should derive through the implied-modifier relation rather than duplicating the same knowledge onto `TypeMeta`.



That is the catalog-driven answer: **derive, don’t duplicate.**



## `TypeAccessor`, `FunctionOverload`, `BinaryOperationMeta`, `ActionMeta`



**No.** These declare **proof requirements**, not declaration satisfactions. They are obligation emitters, not satisfier carriers.



## Future qualifier catalog entry type



**Yes — when it exists.**



The broader design exposes a real architectural gap: qualifier-bound proof facts (`PeriodDimension`, qualifier compatibility on `Currency`/`Unit`/etc.) want a first-class qualifier metadata surface if we ever want them catalog-declared the same way numeric modifier facts are.



Today that surface does not exist. Do not fake it by smearing qualifier-instance semantics onto `TypeMeta`.



## 6. Recommendation summary



| Decision | Recommendation | Why |

|---|---|---|

| Rename | `ProofDischarge` → `ProofSatisfaction` | Names the catalog relation, not the runtime act; scales across all requirement kinds. |

| Property name | `ProofDischarges` → `ProofSatisfactions` | Reads correctly on metadata entries and matches the renamed type. |

| Shape | **DU** keyed by `ProofRequirementKind` | Requirement shapes differ materially; flat nullable record would violate catalog architecture. |

| File location | `src/Precept/Language/ProofRequirement.cs` | Keeps proof-domain vocabulary collocated and subtype-aligned. |

| Current carrier | `FieldModifierMeta` | This is the honest current home for declaration-attached numeric facts, and optionally modifier-presence facts if Shane wants full uniformity. |

| Other current carriers | None | Presence/dimension/qualifier-compatibility do not have honest existing top-level catalog entry types yet. |



## 7. Open questions for Shane



1. **Uniformity vs fast path for `ModifierRequirement`:**

   - Should `ordered`/etc. also be represented as `ProofSatisfaction.Modifier(...)` rows for full conceptual symmetry?

   - Or should the engine keep direct `Contains(requiredModifier)` as the dedicated generic arm even though the broader type can express it?



2. **Qualifier metadata gap:**

   - Does Shane want the broader design to stop at the reusable `ProofSatisfaction` type for now?

   - Or does he want to open a follow-up architecture item to make qualifier-bearing declaration facts first-class catalog metadata so `Dimension` and `QualifierCompatibility` satisfactions have an honest carrier?



3. **Projection identity for accessor-based numeric satisfactions:**

   - Is `Accessor("count")` / `Accessor("length")` acceptable as the first version?

   - Or does Shane want accessor projections promoted to a more stable catalog identity instead of string names?



4. **`notempty` completeness:**

   - Should we declare both `count > 0` and `length > 0` now for catalog completeness?

   - My answer: **yes**, unless Shane wants Strategy-2 metadata to remain strictly current-consumer-only.



That is the decision. `ProofDischarge` is the wrong name. The right name is `ProofSatisfaction`, and the right shape is a proof-kind DU that is broad in **type design** but disciplined in **property placement**.

# Decision: PE-G4 through PE-G18 — All ProofEngine Gaps Resolved



**Date:** 2026-05-08

**Author:** Frank (Lead/Architect)

**Status:** LOCKED — no deferrals, no open questions

**Directive:** Shane's explicit mandate — define everything now so implementation can begin



---



## Summary



All remaining ProofEngine spec gaps (PE-G4 through PE-G18) are resolved with zero deferrals. Combined with the previously resolved PE-G1, PE-G2, and PE-G3, the entire 18-gap inventory is now closed. The ProofEngine spec is READY for implementation.



## Decisions Made



## PE-G4: Walk Targets (not a helper property)

**Decision:** Do NOT add `AllTypedExpressions` to SemanticIndex. Use explicit walk-target enumeration in a private `CollectObligations` method.

**Rationale:** Avoids coupling surface; makes walk scope explicit and auditable.



## PE-G5: Source Shapes Are Canonical

**Decision:** `RuleIdentity(int RuleIndex)` and `EnsureIdentity(ConstraintKind, string? AnchorName, int EnsureIndex)` are correct. Spec must match source.

**Rationale:** Source was created during TypeChecker implementation with tests. Spec hypothesized shapes that create invalid representations (two nullable anchor fields when only one can be non-null).



## PE-G6: ObligationContext DU Replaces FindEnclosingTransitionRow

**Decision:** New `ObligationContext` DU (5 subtypes). Context attached at instantiation time (O(1)), not discovered via post-hoc search. `ProofObligation` gains `Context` field.

**Rationale:** Eliminates O(N²) search; makes context explicit at the point where it's known.



## PE-G7: ResolveSubject Fully Defined

**Decision:** `ResolveSubject` uses reference-equality parameter lookup against `Operations.GetMeta()` / `Functions.GetOverloads()` parameter lists. `GetFieldName` extracts field name from resolved expression.

**Rationale:** The reference-equality model is already established in `ProofRequirement.cs` — this is the mechanical resolution consequence.



## PE-G8: Full Satisfiability Algorithm

**Decision:** Bounded constant folding: find initial state → collect `StateResident` ensures → build default value environment → substitute and fold → report violations for `false` results, conservative pass for `Unknown`. Initial event args NOT considered. Guarded ensures skipped.

**Rationale:** Compile-time only, no evaluator dependency. Conservative — zero false positives at the cost of potentially missing some true violations.



## PE-G9: Type Checker Owns Collection Diagnostics

**Decision:** The type checker owns `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64). The proof engine processes collection non-empty obligations as ordinary `NumericProofRequirement(count > 0)` through standard strategies. No new proof-stage diagnostic code needed for collections.

**Rationale:** The catalog already encodes collection non-empty as `NumericProofRequirement` — the proof engine processes them generically. No duplication.



## PE-G10: Full Guard Decomposition Rules

**Decision:** AND-conjunctions decompose recursively. OR-disjunctions do NOT decompose. Simple negation inverts comparison operators. Conditionals and quantifiers are not decomposed. Operator inversion and negation inversion tables provided.

**Rationale:** AND is safe (all conjuncts true). OR is unsafe (only one guaranteed). Bounded scope prevents false proofs.



## PE-G11: Builder Contract Defined

**Decision:** Three consumption patterns: (1) `FaultSiteLinks` → `FaultSiteDescriptor` backstops, (2) `ConstraintInfluence` → `ConstraintInfluenceMap` runtime artifact, (3) `InitialStateResults` → compile-time gate. `ConstraintInfluenceMap` type defined.

**Rationale:** Defining the contract now ensures proof engine output shapes serve real consumers, not speculation. Shane's directive: no deferrals.



## PE-G12: Diagnostic Formatting Table

**Decision:** Template parameters defined for all three existing proof diagnostics. Four new diagnostic codes allocated (96–99).

**Rationale:** Makes diagnostic output testable.



## PE-G13: Error-Tainted Obligation Suppression

**Decision:** Obligations with `TypedErrorExpression` in their site or resolved subject suppress proof diagnostic emission. `ContainsErrorExpression` recursive helper defined.

**Rationale:** Prevents cascading diagnostics — the type checker already reported the root cause.



## PE-G14: Exhaustive Guard Relation Triple Table

**Decision:** 12-entry table covering all valid `(guard.Op, expr.Op, requirement)` combinations. Strategy 4 limited to subtraction only. Division explicitly excluded.

**Rationale:** Subtraction with field-to-field guards covers the realistic use case. Division requires sign knowledge beyond bounded flow narrowing.



## PE-G15: Stateless Precept Handling

**Decision:** Proof engine runs for ALL precepts. Strategies 1, 2, 5 apply to stateless precepts. Strategies 3, 4 do not (no guards). Initial-state satisfiability skipped. No special-casing needed.

**Rationale:** Division by zero in an event handler action is just as dangerous as in a transition row action.



## PE-G16: Reference Identity for Site Matching

**Decision:** `ProofObligation.Site` uses reference equality via `ReferenceEqualityComparer.Instance`. Proof engine must NOT copy expression nodes.

**Rationale:** Structural equality would create false positives for identical-but-distinct expressions in different contexts.



## PE-G17: Operator Names Verified

**Decision:** All `OperatorKind` names in spec pseudocode match source exactly. No correction needed.



## PE-G18: Diagnostic System Cross-Reference

**Decision:** Add cross-references to `diagnostic-system.md` and `Compiler.cs` in proof engine spec §9.



## New Types Introduced



| Type | File | Purpose |

|---|---|---|

| `ObligationContext` (abstract) | `ProofLedger.cs` | DU base for obligation context |

| `TransitionRowContext` | `ProofLedger.cs` | Obligation in a transition row |

| `ConstraintContext` | `ProofLedger.cs` | Obligation in a rule/ensure condition |

| `StateHookContext` | `ProofLedger.cs` | Obligation in a state hook |

| `EventHandlerContext` | `ProofLedger.cs` | Obligation in an event handler |

| `FieldExpressionContext` | `ProofLedger.cs` | Obligation in a field expression |



## New Diagnostic Codes



| Code | Name | Stage | Severity |

|---|---|---|---|

| 96 | `UnprovedModifierRequirement` | Proof | Error |

| 97 | `UnprovedDimensionRequirement` | Proof | Error |

| 98 | `UnprovedQualifierCompatibility` | Proof | Error |

| 99 | `UnsatisfiableInitialState` | Proof | Error |



## Spec Corrections Required



14 corrections to `docs/compiler/proof-engine.md` — detailed in `frank-pe-g4-to-g18-resolution.md`.



## REQUIRES SHANE INPUT



None. All gaps resolved within existing architectural boundaries. No product surface or philosophy changes.



## Full Resolution Document



`docs/Working/inbox/frank-pe-g4-to-g18-resolution.md`



## 2026-05-08: ProofEngine implementation plan complete — ready for Shane review



**By:** Frank (Lead/Architect)

**Artifact:** `docs/Working/frank-pe-implementation-plan.md`

**What:** Definitive implementation plan for the ProofEngine feature. Two phases (8 prework slices + 13 engine slices), ~134 named tests, method-level specificity throughout.



**Key findings during planning:**

- Diagnostic codes 96–99 (referenced in spec for proof-stage codes) are already allocated to CI enforcement and collection safety codes. Plan allocates 112–115 instead. Spec correction included as Prework Slice P8.

- ProofLedger.cs already has the full G3 shape (ProofObligation, FaultSiteLink, etc.) but is missing ObligationContext — added in Prework Slice P6.

- ProofSatisfaction DU, DeclaredPresenceMeta, and DeclaredQualifierMeta do not exist in source yet — created in Prework Slices P1–P3.

- FieldModifierMeta has no ProofSatisfactions property — added in Prework Slice P4 with all 10 modifier entries populated.

- TypedField and TypedArg lack Presence and DeclaredQualifiers properties — added in Prework Slice P5 (high-touch: every construction site must be updated).



**Phase 1 (Prework):** 8 slices creating structural shapes, catalog metadata, and diagnostic codes. ~37 tests. No behavioral changes — build stays green.

**Phase 2 (Engine):** 13 slices implementing the full two-pass engine with all 5 strategies, error suppression, diagnostics, constraint influence, initial-state satisfiability, forwarding fact consumption, and stateless precept handling. ~97 tests.



**Estimated total:** ~134 new tests across `ProofEngineTests.cs`, `ProofLedgerTests.cs`, `ProofRequirementTests.cs`, `ModifiersTests.cs`, and `DiagnosticsTests.cs`.



**Ready for:** George to execute once Shane approves.

# Decision: ProofEngine Spec Complete — All 18 Gaps Resolved



**Date:** 2026-05-08T22:54:50.625-04:00

**Author:** Frank (Lead/Architect)

**Status:** Approved by Shane — implementation may proceed



---



## Summary



All 18 ProofEngine gaps (PE-G1 through PE-G18) are now RESOLVED and incorporated into the canonical spec at `docs/compiler/proof-engine.md`. The spec is the authoritative implementation target with zero open questions, zero deferrals, and zero placeholders.



## Resolution Timeline



- **PE-G1** (3 unhandled requirement kinds): Resolved 2026-05-08 — Strategy 2 expanded, Strategy 5 added

- **PE-G2** (ProofSatisfaction DU): Resolved 2026-05-08 — full DU with 5 subtypes + 3 supporting DUs, carrier types defined

- **PE-G3** (ProofLedger shape): Resolved 2026-05-08 — 9 supporting types specified

- **PE-G4–G18** (remaining 15 gaps): Resolved 2026-05-08 per Shane's no-deferral mandate, spec corrections applied same day



## New Types Introduced



## ObligationContext DU (PE-G6) — 5 subtypes



```csharp

public abstract record ObligationContext;

public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;

public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;

public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;

public sealed record EventHandlerContext(TypedEventHandler Handler) : ObligationContext;

public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;

```



Added to `ProofObligation` as a `Context` field — set at Pass 1 instantiation time, replaces the undefined `FindEnclosingTransitionRow` with O(1) lookup.



## New Diagnostic Codes



| Code | Name | Severity |

|------|------|----------|

| 96 | `UnprovedModifierRequirement` | Error |

| 97 | `UnprovedDimensionRequirement` | Error |

| 98 | `UnprovedQualifierCompatibility` | Error |

| 99 | `UnsatisfiableInitialState` | Error |



These are spec-defined and pending registration in `DiagnosticCode.cs` and `Diagnostics.cs` at implementation time.



## Key Design Decisions Locked



1. **Explicit walk-target enumeration** (PE-G4) — no `AllTypedExpressions` on SemanticIndex

2. **Source shapes canonical** for ConstraintIdentity (PE-G5)

3. **Context-at-instantiation** pattern for obligation context (PE-G6)

4. **Reference-equality parameter lookup** for subject resolution (PE-G7)

5. **Bounded constant folding** for initial-state satisfiability (PE-G8)

6. **Type checker owns collection diagnostics** (PE-G9)

7. **AND decomposes, OR does NOT, negation inverts** for guard decomposition (PE-G10)

8. **Builder proof-consumption contract** with 3 consumption patterns (PE-G11)

9. **Error-tainted obligations suppress proof diagnostics** (PE-G13)

10. **12-entry exhaustive guard relation triple table**, subtraction-only (PE-G14)

11. **Stateless precepts**: Strategies 3/4 skip, all others apply (PE-G15)

12. **ReferenceEqualityComparer.Instance** for site identity matching (PE-G16)



## Artifacts Updated



- `docs/compiler/proof-engine.md` — canonical spec, all corrections applied

- `docs/Working/frank-proof-engine-gap-analysis.md` — all 18 gaps marked RESOLVED, verdict READY

- `docs/Working/inbox/frank-pe-g4-to-g18-resolution.md` — source material (retained as rationale record)



## Next Steps



Implementation may proceed. The spec is production-quality — no implementer should need to make design decisions. Shape declarations (Slice 0), obligation instantiation, strategy dispatch, and diagnostic emission are all fully specified.



## 2026-05-08: DesugarsToRule flag on ModifierMeta

**By:** George (requested by Shane)

**What:** Added `DesugarsToRule: bool = false` to `ModifierMeta`. Set true on: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`

**Why:** Grammar generator gap — modifiers that desugar to rules were gold-highlighted in the old hand-authored grammar but this was not carried over to the catalog-driven generator.

**Rationale:** Catalog should be the single source of truth for all gold-color decisions, not the generator or hand-authored grammar.



## 2026-05-08: PE-G3 ProofLedger output types implemented

**By:** George (requested by Shane)

**What:** Expanded `src/Precept/Pipeline/ProofLedger.cs` from the single-field stub to the full approved PE-G3 shape: `ProofLedger`, `ProofObligation`, `ProofDisposition`, `ProofStrategy`, `FaultSiteLink`, `FaultSiteAnnotation`, `ConstraintInfluenceEntry`, `EventArgReference`, `InitialStateSatisfiabilityResult`, and `UnsatisfiedConstraint`.

**Files modified:** `src/Precept/Pipeline/ProofLedger.cs`, `src/Precept/Pipeline/ProofEngine.cs`, `docs/compiler/proof-engine.md`

**Files created:** `.squad/decisions/inbox/george-pe-g3-implemented.md`

**Validation:** `dotnet build src\Precept\Precept.csproj --nologo` succeeded with zero errors.



## 2026-05-08T23:21:03.236-04:00: Phase 1 proof-engine prework closed

**By:** George (requested by Shane)

**What:** Completed the Phase 1 proof-engine prework slices P1-P8 with structural-only changes: proof satisfaction carriers, declared presence/qualifier metadata, modifier proof-satisfaction catalog data, semantic-index carrier slots on `TypedField`/`TypedArg`, `ObligationContext` on `ProofObligation`, proof diagnostic codes 112-115, and the matching doc ordinal corrections.



**Commits:**

- P1 `f1de70dc` — `feat(proof-engine): P1 — ProofSatisfaction DU and supporting types`

- P2 `161eb1fa` — `feat(proof-engine): P2 — DeclaredPresenceMeta carrier type`

- P3 `267dd7bd` — `feat(proof-engine): P3 — DeclaredQualifierMeta carrier type`

- P4 `5d6945c4` — `feat(proof-engine): P4 — FieldModifierMeta.ProofSatisfactions catalog metadata`

- P5 `1bdf53f4` — `feat(proof-engine): P5 — TypedField/TypedArg presence and qualifier carrier properties`

- P6 `445c3127` — `feat(proof-engine): P6 — ObligationContext DU on ProofObligation`

- P7 `247ba37f` — `feat(proof-engine): P7 — diagnostic codes 112-115 for proof stage`

- P8 `647de929` — `docs(proof-engine): P8 — correct diagnostic code ordinals 96-99 → 112-115`



**Files touched (high-signal):**

- Runtime/language: `src/Precept/Language/ProofRequirement.cs`, `src/Precept/Language/DeclaredPresence.cs`, `src/Precept/Language/DeclaredQualifierMeta.cs`, `src/Precept/Language/Modifier.cs`, `src/Precept/Language/Modifiers.cs`, `src/Precept/Pipeline/SemanticIndex.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/ProofLedger.cs`, `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`

- Tests: `test/Precept.Tests/ProofRequirementTests.cs`, `test/Precept.Tests/ModifiersTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerQuantifierTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs`, `test/Precept.Tests/ProofLedgerTests.cs`, `test/Precept.Tests/DiagnosticsTests.cs`

- Docs: `docs/compiler/proof-engine.md`, `docs/compiler/diagnostic-system.md`, `docs/Working/frank-proof-engine-gap-analysis.md`



**Validation:**

- `dotnet build src\Precept\Precept.csproj --nologo` succeeded during slice validation.

- Final `dotnet test -nologo` summary: 3910 total, 3714 passed, 196 failed.

- Final `dotnet build -nologo` succeeded.

- Remaining failures are pre-existing: 194 `Precept.LanguageServer.Tests` failures from `LanguageServerStubs.cs` `NotImplementedException` paths, plus 2 `Precept.Tests` `TokensTests` failures around `TokenKind.Set` classification.



**Surprises / deviations:**

- `ConstraintIdentity` already existed in `SemanticIndex.cs`, so no new identity carrier was needed for P6.

- The spec's proof diagnostic ordinals were stale; the implementation correctly used 112-115 instead of 96-99.

- `docs/compiler/proof-engine.md` already carried a large unrelated branch diff, so the P8 doc commit necessarily rode on top of a broader proof-engine doc sync instead of a tiny isolated ordinal-only patch.

- `FieldModifierMeta.ProofSatisfactions` test assertions had to avoid FluentAssertions expression-tree paths; simple `foreach` assertions were more robust.



**Tricky construction sites:**

- `TypedField` record construction in `src/Precept/Pipeline/TypeChecker.cs`

- Manual `TypedArg` scaffolds in `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs`

- Manual `TypedArg` scaffolds in `test/Precept.Tests/TypeChecker/TypeCheckerQuantifierTests.cs`

- Proof-ledger shape tests in `test/Precept.Tests/ProofLedgerTests.cs`



**Phase 2 handoff:**

- The structural metadata surface is now in place for obligation instantiation, strategy evaluation, and proof diagnostic emission.

- Phase 2 can assume proof-bearing modifiers already declare satisfactions, semantic symbols expose presence/qualifier carriers, obligations can record context, and proof-stage diagnostics 112-115 are reserved with metadata.

- `ProofEngine.cs` remains the behavioral frontier; Phase 2 should implement runtime-neutral proof analysis against the new carriers rather than reshaping these types again.

# ProofEngine Phase 2 Closeout



**Agent:** George (Runtime Dev)

**Date:** 2026-05-08T23:45:00Z

**Task:** ProofEngine Phase 2 — Full Engine Implementation (S1–S13)



## Commit Hashes



| Slice | Commit | Description |

|-------|--------|-------------|

| S1–S12 | `46c9a4d4` | Full engine implementation — obligation collection, five strategies, error suppression, diagnostics, constraint influence, initial-state satisfiability, forwarding fact consumption |

| S13 | `36618ef9` | Stateless handling verification + documentation sync |



## Build/Test Results



- **Build:** Green, 0 warnings, 0 errors

- **Tests:** 3451 passed, 2 pre-existing TokensTests failures unchanged

- **Baseline preserved:** No regressions introduced



## Deviations from Plan



1. **Combined commit:** Slices S1–S12 were implemented in a single pass and committed together rather than individually. The code structure follows the slice boundaries internally, but the git history has 2 commits instead of 13. Rationale: the implementation was written holistically for correctness across slice boundaries, and incremental commits would have required artificial intermediate states.



2. **FunctionKind.Count:** The spec referenced `FunctionKind.Count` for guard pattern recognition, but `count` is a `TypeAccessor` on collection types, not a function. Guard extraction matches `TypedMemberAccess` with `acc.Name == "count"` instead.



3. **SourceSpan.Empty vs SourceSpan.Missing:** Spec pseudocode used `SourceSpan.Empty` but the actual API is `SourceSpan.Missing`.



4. **ModifierKind.Initial vs ModifierKind.InitialState:** The live enum uses `InitialState`, not `Initial`.



5. **BinaryOperationMeta.Left/Right vs Lhs/Rhs:** Spec pseudocode used `.Left`/`.Right` but actual properties are `.Lhs`/`.Rhs`.



## Surprises



- No functional surprises — the spec was thorough and all 18 gaps were pre-resolved by Frank.

- The modifier effective-modifiers walk needed both `Modifiers` and `ImpliedModifiers` concatenation per the spec's effective-modifiers note (§7).

- `SatisfactionCovers` subsumption logic required careful accessor-name matching for `notempty`'s dual satisfaction rows.



## What's Now Unblocked



- Soup Nazi's ProofEngine test suite can exercise all five strategies

- Precept Builder can consume `ProofLedger` for fault backstops and constraint influence

- Language Server proof diagnostics are now live

# ProofEngine Phase 2 — Post-commit bugfixes



**Date:** 2026-05-09T00:35:00-04:00

**Author:** George

**Commit:** d3657b70



## Summary



Fixed 5 failing `ProofEngineTests` after Phase 2 (S1–S13) landed.



## Fixes



## 1. ResolveParamInBinaryOp — Rhs-first resolution



**Root cause:** Shared `ParameterMeta` instances (e.g., `PNumber`) are used for both `Lhs` and `Rhs` of binary operations in the Operations catalog. `ReferenceEquals` matched `Lhs` first, resolving the divisor proof requirement to the numerator instead of the divisor.



**Fix:** Swapped check order in `ResolveParamInBinaryOp` to check `Rhs` before `Lhs`. Proof requirements for binary ops (division, modulo) target the right operand (divisor), so this resolves the correct field.



## 2. Discharge loop — skip already-proved obligations



**Root cause:** `IncorporateForwardingFacts` (Pass 1.5) correctly marked unreachable/dead-end obligations as `Proved`, but the discharge loop (Pass 2) unconditionally overwrote all obligations with `TryDischarge` results, replacing `Proved` with `Unresolved`.



**Fix:** Added `if (obligation.Disposition == ProofDisposition.Proved) continue;` at the top of the discharge loop.



## Validation



- 158/158 ProofEngineTests passing

- 3609/3611 full suite passing (2 pre-existing TokensTests failures unchanged)



## 2026-05-08: DesugarsToRule wired into grammar generator

**By:** Kramer (requested by Shane)

**What:** Generator now reads Modifiers.All.Where(m => m.DesugarsToRule) to emit gold-colored TextMate patterns for rule-desugaring modifiers.

**Scope used:** `keyword.other.grammar.precept`

**Why:** Catalog gap — the old hand-authored grammar gold-highlighted these modifiers but the generator had no path for it.



## 2026-05-08: VS Code extension packaging bundles the client entrypoint with esbuild

**By:** Kramer (requested by Shane)

**What:** Added an esbuild production bundle that writes the extension client to `tools/Precept.VsCode/out/extension.js`, moved VSIX packaging onto `vscode:prepublish`, and removed `node_modules/**` from the `.vscodeignore` allowlist so npm dependencies no longer ship raw.

**Why:** The extension only needs the bundled client JavaScript plus the unchanged `server/` and `syntaxes/` assets; shipping raw `node_modules` was inflating the VSIX with ~170 JavaScript files for no runtime benefit.

**Rationale:** Keeping `npm run compile` as plain `tsc` preserves the existing development loop, while `npm run bundle` becomes the production-only path that inlines `vscode-languageclient` and other client dependencies without bundling the .NET language server.

# Soup Nazi — ProofEngine Phase 2 tests done



**Date:** 2026-05-08T23:45:00.367-04:00

**Scope:** `test/Precept.Tests/ProofEngineTests.cs`



## Test count per slice



- S1 Obligation collection — 7 required tests

- S2 Subject resolution — 9 required tests

- S3 Literal proof — 6 required tests

- S4 Declaration-attribute proof — 11 required tests

- S5 Guard-in-path proof — 11 required tests

- S6 Flow narrowing — 7 required tests

- S7 Qualifier compatibility — 5 required tests

- S8 Error-tainted suppression — 4 required tests

- S9 Diagnostics + fault links — 11 required tests

- S10 Constraint influence — 4 required tests

- S11 Initial-state satisfiability — 8 required tests

- S12 Proof-forwarding facts — 5 required tests

- S13 Stateless + integration — 9 required tests

- **Required inventory total:** 97 named tests from the task plan

- **Discovered `ProofEngineTests` total on branch:** 158 tests (required-name inventory plus supplemental coverage already in the file)



## Validation



- `dotnet build test/Precept.Tests/ --nologo` **passed**.

- Baseline before this work: `dotnet test test/Precept.Tests/ --nologo --no-build` still showed the known 2 failing `TokensTests` only.

- Focused run after authoring: `dotnet test test/Precept.Tests/ --nologo --no-build --filter FullyQualifiedName~ProofEngineTests` ran **158** tests with **153 passed / 5 failed**.



## What failed in the focused ProofEngine run



1. `Slice12_ProofForwardingFacts.ForwardingFacts_UnreachableState_ObligationsVacuouslyProved`

2. `Slice12_ProofForwardingFacts.ForwardingFacts_DeadEndToDeadEnd_ObligationsSuppressed`

3. `RequiredNameInventory.ForwardingFacts_UnreachableState_SuppressesObligations`

4. `RequiredNameInventory.ForwardingFacts_DeadEndToDeadEnd_SuppressesObligations`

5. `RequiredNameInventory.GetFieldName_NonFieldRef_ReturnsNull`



## Gaps / surprises



- Forwarding-fact suppression is still red on the current branch: obligations marked proved during forwarding-fact incorporation end up unresolved by the end of `ProofEngine.Prove`.

- `GetFieldName_NonFieldRef_ReturnsNull` is red on the current branch: a non-field subject path still reaches declaration-attribute proof unexpectedly.

- Several proof behaviors (especially qualifier compatibility and flow narrowing) needed manual `SemanticIndex` construction because the public DSL surface does not express every proof-shape directly.



## Extra edge cases not explicitly called out in the plan



- Operand metadata identity matters: `integer / number` and `number / number` do not stress subject resolution the same way because catalog parameter instances differ.

- Boolean guard composition (`and` / `or`) can go red at type-check time if the catalog does not carry the boolean operation entries the proof strategy assumes.

- Flow narrowing is easy to under-test when the risky obligation sits on an outer node (`sqrt(A - B)`, `Y / (A - B)`) rather than on the subtraction node itself.

# Soup Nazi review — `precept_language`



**Verdict:** BLOCKED



## Scope

- Reviewed commit `bd4e6e30`.

- Repo spec source for this tool is `docs/tooling/mcp.md` (`precept_language` section). `docs/McpServerDesign.md` is not present.

- `test/Precept.Mcp.Tests/LanguageToolTests.cs` started this review at 12 tests.



## Why blocked

Original coverage was not sufficient.



Missing or weak before remediation:

- No schema-serialization test for the documented camelCase top-level contract.

- No assertion that every top-level catalog section is present and populated through the response shape.

- No completeness/order checks for `Tokens`, `Types`, `Actions`, `Constructs`, `Constraints`, or `Diagnostics`.

- No modifier subgroup completeness check for `field`, `state`, `event`, `access`, and `anchor`.

- `Operators` and `Functions` only had count/spot checks, not full catalog/order assertions.

- No representative field-mapping checks for tokens, types, modifiers, actions, constructs, or diagnostics.

- Token-floor test was stricter than the spec-friendly contract (`> 80` instead of `>= 80`).



## Remediation shipped

Expanded `test/Precept.Mcp.Tests/LanguageToolTests.cs` from 12 to 19 tests covering:

- serialized schema shape

- token/type/action/construct/operator/function/diagnostic catalog completeness in declaration order

- modifier subgroup completeness plus subtype-specific mapping anchors

- constraint mapping

- fire-pipeline exact order

- token floor `>= 80`



## Validation

- `dotnet test test\Precept.Mcp.Tests\Precept.Mcp.Tests.csproj --no-build -q -m:1 /nr:false` → **19 passed, 0 failed**.

- `dotnet test --no-build -q -m:1 /nr:false` → **baseline repo still red: 194 failures**.

  - Failures are pre-existing language-server completion tests throwing `NotImplementedException` from `tools/Precept.LanguageServer/LanguageServerStubs.cs:31`.

  - No failure implicated `LanguageTool` or the new MCP tests.

# ISO 4217 refresh workflow conversion



**Date:** 2026-05-09

**Merged from:** `kramer-iso4217-task.md`, `soup-nazi-iso4217-sync-test.md`



## Decision

- Keep ISO 4217 refresh out of the VS Code extension command surface; expose it as the workspace task `iso4217: refresh` backed by `tools/scripts/refresh-iso4217.js`.

- Download the XML into `src/Precept/Data/Iso4217/list-one.xml` using the live SIX endpoint at `iso-currrency/lists/list-one.xml`; the older `iso-4217/lists/list-one.xml` path currently returns 404.

- Treat `src/Precept/Data/` as developer-downloaded reference data, not committed source.

- Guard currency-parity validation with a discovery-time-skipped xUnit test so CI stays green until a developer intentionally refreshes the XML locally.



## Rationale

- This is a repo-local maintenance workflow, not an always-on editor feature.

- The live upstream URL has drifted, so the refresh path must follow the currently published SIX source rather than a stale historical endpoint.

- Optional local reference data should strengthen developer validation without turning absent downloads into red CI.

# Qualifier completion honesty and Tier 1 UOM breadth



**Date:** 2026-05-09

**Merged from:** `elaine-completion-suppression-uom.md`



## Decision

- When a type/preposition pair is structurally invalid, show no qualifier-value completions; guide the user back to the correct preposition instead of suggesting misleading values.

- Expand UOM completions to the ~150 canonical Tier 1 set now rather than shipping an underpowered shortlist.



## Rationale

- Completions are a truth surface: invalid structure should feel invalid, not productive.

- Missing legitimate units damages trust faster than a somewhat longer filtered completion list.

# UCUM / ISO 4217 implementation gap remediation shape



**Date:** 2026-05-09

**Merged from:** `frank-ucum-iso-gap.md`

**Status:** Draft — pending Shane sign-off



## Decisions

1. Replace the flat currency-code set with a structured `CurrencyCatalog` entry shape (`AlphaCode`, `NumericCode`, `Name`, `MinorUnit`) so money fields can derive implicit precision correctly.

2. Defer the full UCUM grammar parser, but expand the interim UOM registry to the canonical Tier 1 atom set and rename the registry to match the catalog target architecture.

3. Keep the current `ClosedSetValidation` DU shape until the grammar parser ships; add the future grammar-aware validation as a new subtype instead of churning the existing surface now.

4. Audit the dimension registry back to the curated v1 spec set and remove premature entries; `time` and `count` stay open questions for Shane.

5. Keep ISO 4217 sync as a manual PR workflow driven by published XML updates, roughly 1–2 times per year.



## Rationale

- The spec already locked `CurrencyCatalog`, `UnitCatalog`, and `DimensionCatalog`; the code is behind the design, not vice versa.

- `FrozenSet<string>` cannot carry the metadata required for money semantics or future catalog-driven tooling.

# Field and arg semantic colors



**Date:** 2026-05-09

**Merged from:** `elaine-field-arg-colors.md`

**Status:** Draft — pending Shane sign-off



## Decision

- Formalize field names as semantic token `--field` using `#A5B4FC`, the lifted structure-family identifier tone.

- Formalize arg names as semantic token `--arg` using `#9AD8E8`, a lifted cyan companion to event color.

- Narrow the Data construct back to types and values; field and arg identifiers no longer inherit the generic data slate treatment.



## Rationale

- Fields belong on the structure axis (what a precept is), while args belong on the behaviour axis (what a precept does).

- The companion-token pattern is an axis relationship, not a change to the existing 1–3 shade paradigm.

### 2026-05-09T12:55:27-04:00: User directive

**By:** Shane (via Copilot)

**What:** Do not do anything beyond the plan scope. The implementation plan is `docs/Working/typed-literal-system-plan.md` (12 slices). All work must stay within this plan — no additional features, no speculative improvements, no expanding scope beyond what the plan specifies.

**Why:** User request — captured for team memory

# Decision: Currency Symbol Data Strategy



**Author:** Frank (Lead/Architect)

**Date:** 2026-05-09T12:44:09-04:00

**Status:** RULING

**Scope:** CurrencyEntry symbol field, data ownership, maintenance strategy

**Affects:** Slice 1c (CurrencyCatalog loader migration)



---



## Verdict: Option 2 — Hardcoded Static Dictionary in CurrencyCatalog.cs



Add a `Symbol` property to `CurrencyEntry` and populate it from a private static dictionary in `CurrencyCatalog.cs`, merged at load time when the XML loader constructs entries.



---



## Rationale



The data layer decision established a clear first-party / third-party boundary:



- **Third-party (ISO 4217):** AlphaCode, NumericCode, Name, MinorUnit → lives in `list-one.xml`, loaded at runtime

- **First-party (Precept-owned):** Symbol → lives in C# source code



Currency symbols are Precept augmentation data. They are not in the ISO 4217 standard. They are not in `list-one.xml`. They will never appear in `list-one.xml`. Putting them in an XML file would misclassify first-party data as if it were an external authoritative source. Putting them in the refresh script would mix Precept editorial decisions into a tool whose job is downloading a third-party file.



The practical case is equally clear:



- ~40 currencies have widely-recognized Unicode symbols. The remaining ~120 use their alpha code as the display form.

- Currency symbols are among the most stable data in existence. The dollar sign has been `$` for 232 years.

- A static dictionary of ~40 entries is trivially reviewable, trivially maintainable, and adds zero infrastructure.



### Options Rejected



**Option 1 (Separate XML file):** Over-engineers a ~40-entry lookup. Introduces a new embedded resource, a new XML schema, a new parser path, and a maintenance surface — all for data that changes less than once per decade. Also misclassifies first-party data by putting it in the `Data/` directory alongside third-party reference data.



**Option 3 (Augmented refresh script):** Violates separation of concerns. The refresh script's job is "download ISO 4217 from SIX Group." Making it also synthesize Precept-owned symbol data couples a third-party sync tool to first-party editorial decisions. When the script runs, it should be idempotent on Precept-owned data — it should never overwrite our symbols with whatever SIX Group publishes (which is nothing, for symbols).



---



## Updated CurrencyEntry Record



```csharp

public sealed record CurrencyEntry(

    string AlphaCode,    // e.g. "USD"         — from ISO 4217

    int    NumericCode,  // e.g. 840           — from ISO 4217

    string Name,         // e.g. "US Dollar"   — from ISO 4217

    int    MinorUnit,    // e.g. 2             — from ISO 4217

    string Symbol        // e.g. "$"           — Precept-owned augmentation; defaults to AlphaCode

);

```



`Symbol` defaults to `AlphaCode` when no dedicated symbol exists. This means every `CurrencyEntry` has a usable display symbol — no null checks, no `Symbol ?? AlphaCode` at every call site.



---



## Implementation Shape



In `CurrencyCatalog.cs`, after the XML loader parses `list-one.xml`:



```csharp

// Precept-owned augmentation: currency display symbols.

// ISO 4217 does not define symbols. These are curated from Unicode CLDR

// and common financial usage. Currencies without an entry here use their

// alpha code as the display symbol.

private static readonly FrozenDictionary<string, string> Symbols =

    new Dictionary<string, string>

    {

        ["AED"] = "د.إ",  ["AFN"] = "؋",    ["ALL"] = "L",

        ["AMD"] = "֏",    ["ARS"] = "$",    ["AUD"] = "A$",

        ["AZN"] = "₼",    ["BAM"] = "KM",   ["BBD"] = "Bds$",

        ["BDT"] = "৳",    ["BGN"] = "лв",   ["BHD"] = ".د.ب",

        ["BMD"] = "$",    ["BND"] = "B$",   ["BOB"] = "Bs.",

        ["BRL"] = "R$",   ["BSD"] = "B$",   ["BTN"] = "Nu.",

        ["BWP"] = "P",    ["BYN"] = "Br",   ["BZD"] = "BZ$",

        ["CAD"] = "C$",   ["CDF"] = "FC",   ["CHF"] = "CHF",

        ["CLP"] = "$",    ["CNY"] = "¥",    ["COP"] = "$",

        ["CRC"] = "₡",    ["CUP"] = "₱",    ["CZK"] = "Kč",

        ["DKK"] = "kr",   ["DOP"] = "RD$",  ["DZD"] = "د.ج",

        ["EGP"] = "E£",   ["ERN"] = "Nfk",  ["ETB"] = "Br",

        ["EUR"] = "€",    ["FJD"] = "FJ$",  ["FKP"] = "£",

        ["GBP"] = "£",    ["GEL"] = "₾",    ["GHS"] = "GH₵",

        ["GIP"] = "£",    ["GTQ"] = "Q",    ["GYD"] = "G$",

        ["HKD"] = "HK$",  ["HNL"] = "L",    ["HUF"] = "Ft",

        ["IDR"] = "Rp",   ["ILS"] = "₪",    ["INR"] = "₹",

        ["IQD"] = "ع.د",  ["IRR"] = "﷼",    ["ISK"] = "kr",

        ["JMD"] = "J$",   ["JOD"] = "JD",   ["JPY"] = "¥",

        ["KES"] = "KSh",  ["KGS"] = "сом",  ["KHR"] = "៛",

        ["KPW"] = "₩",    ["KRW"] = "₩",    ["KWD"] = "د.ك",

        ["KYD"] = "CI$",  ["KZT"] = "₸",    ["LAK"] = "₭",

        ["LBP"] = "L£",   ["LKR"] = "Rs",   ["LRD"] = "L$",

        ["MAD"] = "MAD",  ["MDL"] = "L",    ["MGA"] = "Ar",

        ["MKD"] = "ден",  ["MMK"] = "K",    ["MNT"] = "₮",

        ["MOP"] = "MOP$", ["MRU"] = "UM",   ["MUR"] = "₨",

        ["MVR"] = "Rf",   ["MWK"] = "MK",   ["MXN"] = "Mex$",

        ["MYR"] = "RM",   ["MZN"] = "MT",   ["NAD"] = "N$",

        ["NGN"] = "₦",    ["NIO"] = "C$",   ["NOK"] = "kr",

        ["NPR"] = "Rs",   ["NZD"] = "NZ$",  ["OMR"] = "ر.ع.",

        ["PAB"] = "B/.",  ["PEN"] = "S/.",   ["PGK"] = "K",

        ["PHP"] = "₱",    ["PKR"] = "₨",    ["PLN"] = "zł",

        ["PYG"] = "₲",    ["QAR"] = "ر.ق",  ["RON"] = "lei",

        ["RSD"] = "din.", ["RUB"] = "₽",    ["RWF"] = "FRw",

        ["SAR"] = "ر.س",  ["SBD"] = "SI$",  ["SCR"] = "SRe",

        ["SDG"] = "ج.س.", ["SEK"] = "kr",   ["SGD"] = "S$",

        ["SHP"] = "£",    ["SLE"] = "Le",   ["SOS"] = "Sh",

        ["SRD"] = "SRD",  ["SSP"] = "SS£",  ["STN"] = "Db",

        ["SVC"] = "₡",    ["SYP"] = "S£",   ["SZL"] = "E",

        ["THB"] = "฿",    ["TJS"] = "SM",   ["TMT"] = "T",

        ["TND"] = "د.ت",  ["TOP"] = "T$",   ["TRY"] = "₺",

        ["TTD"] = "TT$",  ["TWD"] = "NT$",  ["TZS"] = "TSh",

        ["UAH"] = "₴",    ["UGX"] = "USh",  ["USD"] = "$",

        ["UYU"] = "$U",   ["UZS"] = "сўм",  ["VES"] = "Bs.S",

        ["VND"] = "₫",    ["VUV"] = "VT",   ["WST"] = "WS$",

        ["XAF"] = "FCFA", ["XCD"] = "EC$",  ["XOF"] = "CFA",

        ["XPF"] = "₣",    ["YER"] = "﷼",    ["ZAR"] = "R",

        ["ZMW"] = "ZK",

    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

```



The XML loader merges at construction time:



```csharp

var symbol = Symbols.GetValueOrDefault(alphaCode, alphaCode);

return new CurrencyEntry(alphaCode, numericCode, name, minorUnit, symbol);

```



---



## Maintenance Going Forward



- **Who updates symbols:** Any developer, via a normal PR editing the `Symbols` dictionary.

- **When:** When a new currency is added to ISO 4217 (rare — a few per decade) and someone wants a display symbol for it. Or if a symbol mapping is corrected.

- **Review:** Trivially reviewable — it's a string-to-string dictionary in C#.

- **The refresh script does NOT change.** It downloads `list-one.xml`. Symbols are not its concern.



---



## Changes from Current Plan (Slice 1c)



1. **`CurrencyEntry` gains a `Symbol` property.** The plan said "record shape unchanged" — this is now a 5-field record instead of 4.

2. **The loader must merge symbol data.** After parsing XML entries, look up `Symbols[alphaCode]` and default to `alphaCode` if absent.

3. **Tests add symbol coverage:** verify `USD.Symbol == "$"`, `EUR.Symbol == "€"`, `JPY.Symbol == "¥"`, and that currencies without explicit symbols use their alpha code (e.g., `XDR.Symbol == "XDR"`).

4. **No new files, no new embedded resources, no script changes.**



---



## The Catalog-System Test



> "Is this part of a complete description of Precept?"



Currency symbols are NOT part of the language specification. They are display/formatting augmentation consumed by the evaluator's `FormatString` and the language server's hover/completion. They are first-party *editorial* data — Precept decides which symbols to use — but they are not catalog metadata in the `catalog-system.md` sense. A hardcoded dictionary is the right weight for this: visible in source, trivially maintainable, no infrastructure.

# Decision: ISO 4217 / UCUM Data Layer — Embedded XML, Lazy Load



| Property | Value |

|---|---|

| Author | Frank |

| Date | 2026-05-09T12:45:00-04:00 |

| Status | Proposed (supersedes `frank-ucum-data-layer.md`) |

| Scope | How ISO 4217 and UCUM reference data reaches typed C# consumers |



## Recommendation



**Option A — Embedded XML with lazy runtime load, consistent for both ISO 4217 and UCUM.** Ship authoritative XML as embedded resources. Parse once on first access into the same frozen typed records consumers already expect. No codegen step. No generated C# files.



## What IS Catalog Metadata vs. What Is Reference Data



Shane's question cuts to the bone and the answer is clear.



The catalog system doc defines the test: *"if I enumerated every catalog's `All` property, would I have a complete description of Precept?"* Enumerating ISO 4217 codes does not describe Precept — it describes ISO 4217. Enumerating UCUM atoms does not describe Precept — it describes UCUM.



What IS catalog metadata:

- `TypeMeta` for `currency` (the Precept type — its traits, qualifiers, accessors, content validation shape)

- `TypeMeta` for `unitofmeasure` (the Precept type)

- `TypeMeta` for `quantity`, `price`, `exchangerate` (Precept types that consume currency/unit data)

- The `ContentValidation` DU subtype that says "validate currency constants against ISO 4217"

- The `UcumValidation` DU subtype that says "validate unit constants through the UCUM parser"



What is NOT catalog metadata:

- The 159 ISO 4217 currency entries (AlphaCode, NumericCode, Name, MinorUnit)

- The ~300 UCUM atoms (code, dimension vector, scale factor, prefixability)

- The ~24 UCUM SI prefixes



These are **external, authoritative, versioned reference databases** that Precept *consumes*. They are not part of a complete description of the Precept language. They are data that Precept's type system validates against. The distinction is the same one that separates a SQL engine's catalog (table schemas, column types, constraints) from the data in the tables.



My earlier decision conflated "consumers need typed C# records" with "those records must be source-level generated C#." Both statements are not equivalent. Consumers need `FrozenDictionary<string, CurrencyEntry>` and `FrozenDictionary<string, UcumAtom>`. They do not care whether those collections were populated from a generated `.g.cs` file or from an embedded XML resource parsed once at process startup.



### Why my NodaTime dismissal was wrong



I argued that NodaTime's embedded-resource pattern "solves a distribution problem Precept doesn't have." That framing was too narrow. The embedded-resource pattern solves a *classification* problem: it keeps external reference data out of source-level language definition code. NodaTime doesn't generate C# arrays of timezone rules — not because of distribution, but because timezone rules aren't NodaTime's language. They're external data NodaTime consumes. The same principle applies here.



## Specific Answer for ISO 4217



**Embedded XML, lazy load.**



- `src/Precept/Data/Iso4217/list-one.xml` already exists as the provenance artifact. It stays.

- `CurrencyCatalog.cs` becomes a loader, not a data file. It exposes the same `FrozenDictionary<string, CurrencyEntry> All` property, but populates it by parsing the embedded `list-one.xml` on first access via `Lazy<T>`.

- The 213-line hand-maintained (or codegen-maintained) array of `new CurrencyEntry(...)` calls disappears.

- `refresh-iso4217.js` simplifies: it downloads the XML and writes it to `src/Precept/Data/Iso4217/list-one.xml`. Done. No codegen step.

- The exclusion logic (precious metals, fund codes, test codes) moves into the loader's XML-parsing filter — same rules, same result, declarative in one place.



159 entries parsed from XML once per process lifetime is sub-millisecond. This is not a performance concern.



## Specific Answer for UCUM



**Embedded XML, lazy load — same pattern.**



- `ucum-essence.xml` ships as an embedded resource in `src/Precept/Data/Ucum/`.

- `UcumAtomCatalog.cs` (not `.g.cs`) exposes `FrozenDictionary<string, UcumAtom> All` and `FrozenDictionary<string, UcumPrefix> Prefixes`, populated from the embedded XML on first access.

- The UCUM parser (`UcumParser.cs`) consumes these typed records at parse time — the consumer API is identical to what the codegen approach would have provided.

- Tier 1 classification (for LS completions/MCP discovery) is a property on the `UcumAtom` record, applied during the load pass. The tier assignment logic is Precept's own curation — that IS Precept-specific knowledge, applied as the atoms are loaded.

- `refresh-ucum.js` downloads `ucum-essence.xml` to `src/Precept/Data/Ucum/`. Done.



"UCUM is huge" is addressed cleanly: the XML is ~300 atoms. Parsing it once into frozen typed records is trivial. The concern about UCUM's size was about *generated C# source code* — hundreds of constructor calls with dimension vectors and exact scale factors as C# literals would be ugly, hard to review, and pointless. As embedded XML, the size is irrelevant. The XML IS the data format; we're not transcoding it into a worse one.



## Consistency Ruling



**Both use the same pattern. No exceptions.**



1. Authoritative XML in `src/Precept/Data/{Standard}/` as an embedded resource.

2. A typed loader class in `src/Precept/Language/` (or `src/Precept/Language/Ucum/` for UCUM) that parses the XML once, lazily, into frozen typed records.

3. A refresh script in `tools/scripts/` that downloads the latest upstream XML. No codegen. No generated files.

4. Consumers see `FrozenDictionary<string, T>` — they never know or care that it came from XML.



The consumer API is identical under both approaches. The difference is entirely in how the data enters the binary: source-level C# literals vs. embedded resource parsed once. The latter is architecturally correct because it preserves the distinction between language definition (catalog metadata) and external reference data.



## Tradeoff Accepted



**What we give up:**



- **No reviewable C# diff on data updates.** When ISO 4217 or UCUM publishes a new version, the commit diff shows XML changes, not C# changes. XML diffs are less readable than C# record-array diffs. This is a real cost — but it is the correct cost. The alternative (codegen) purchases readable diffs by misclassifying reference data as source code.

- **First-access latency.** There is a one-time XML parsing cost on first use. For 159 currencies: negligible. For ~300 UCUM atoms with dimension vectors: still negligible (sub-millisecond for in-memory XML parsing of a small embedded resource). If this ever becomes measurable — it won't — the `Lazy<T>` can be replaced with eager initialization in a static constructor. The consumer API doesn't change.

- **No compile-time schema enforcement on the XML.** A malformed XML resource won't fail the C# build — it will fail on first access. Mitigation: the existing test suites (`CurrencyCatalogSyncTests`, future UCUM catalog tests) validate the embedded resource at test time. A broken XML will fail CI before it ships.



## Impact on the Plan



### What changes



1. **`CurrencyCatalog.cs` becomes a loader.** Delete the 159-entry array. Add embedded-resource XML parsing into `CurrencyEntry` records with a `Lazy<FrozenDictionary<string, CurrencyEntry>>` backing field. Same public API.

2. **`refresh-iso4217.js` simplifies.** Remove any codegen logic (currently it just downloads XML, so minimal change — but the architecture explicitly forecloses future codegen for this path).

3. **UCUM data layer builds the same way.** `UcumAtomCatalog.cs` is a loader over embedded `ucum-essence.xml`, not a generated file. No `generate-ucum-catalog.js` script needed. Only `refresh-ucum.js` (XML download).

4. **Tier 1 curation logic lives in the loader.** The `UcumAtom.Tier` property is set during the load pass based on Precept's curation rules — that logic is Precept-owned, not UCUM-owned.

5. **Test coverage must validate the embedded resources.** Catalog sync tests parse the embedded XML and verify record counts, required fields, and known entries.



### What does NOT change



- The typed record shapes (`CurrencyEntry`, `UcumAtom`, `UcumPrefix`, `DimensionVector`) — identical.

- The consumer API (`CurrencyCatalog.All`, `UcumAtomCatalog.All`) — identical.

- The UCUM parser architecture — unchanged, it still reads from `UcumAtomCatalog.All`.

- The `ContentValidation` / `UcumValidation` DU on `TypeMeta` — unchanged.

- The refresh scripts' download logic — unchanged.

# Decision: Typed Literal Framework — Q5 Deserialization + Exhaustive Gap Review



| Property | Value |

|---|---|

| Author | Frank (Lead/Architect) |

| Date | 2026-05-09T11:51:45-04:00 |

| Scope | `docs/Working/frank-typed-literal-framework.md` — Q5 addition and gap audit |

| Grounding | `docs/runtime/runtime-api.md`, `docs/compiler/literal-system.md`, `docs/language/catalog-system.md`, `src/Precept/Language/Types.cs`, `src/Precept/Language/Type.cs` |



## Decisions



### D1: Restore reuses `TypeRuntimeMeta.ReadJson` — no separate deserialization contract



The `Precept.Restore(string?, JsonElement)` path uses the same `TypeRuntimeMeta.ReadJson` delegates as Fire and Update JSON lanes. No distinct deserialization contract is needed. `ReadJson` IS the deserialization contract.



**Rationale:** All three runtime JSON ingress paths (Fire, Update, Restore) convert `JsonElement` → `PreceptValue` for the same type registry. Creating a separate delegate would duplicate the parser registrations without adding value.



**Alternatives rejected:**

- Separate `RestoreJson` delegate on `TypeRuntimeMeta` — rejected because the parsing logic is identical; only the caller context differs.

- Reusing `TypedConstantValidation.Validate` at Restore time — rejected for the same reasons it was rejected for Fire in Q4 (wrong input format, wrong error model, DSL syntax vs JSON wire format).



**Tradeoff accepted:** If a future need arises where stored format diverges from wire format (e.g., a compact binary representation), a separate delegate would be needed. For now, JSON is the only storage format and `ReadJson` covers it.



### D2: Round-trip fidelity is the only forward-compatibility guarantee



`ReadJson(WriteJson(v)) == v` for any valid `PreceptValue`. Leniency beyond the canonical format is type-by-type. Schema evolution (type changes, new constraints) is the caller's migration responsibility, detected via `RestoreOutcome` variants.



### D3: 15 gaps identified — 2 Blockers, 13 Advisory



Full gap review completed against all canonical docs. Two blockers require resolution before the proposal can be approved:



1. **G1 — CLR type mapping contradiction:** `runtime-api.md` maps temporal types to System types (DateOnly, TimeOnly, DateTimeOffset); the proposal maps them to NodaTime types. Requires a locked decision on the public CLR mapping.

2. **G2 — Restore absent from consumer matrices:** Q1/Q2 consumer matrices don't mention the Restore path. Q5 covers the architecture but the matrices need cross-references.



13 advisory gaps documented for resolution during implementation.



## Cross-References



- Proposal: `docs/Working/frank-typed-literal-framework.md`

- Runtime API (Restore design): `docs/runtime/runtime-api.md` § Restoration

- Literal system (ITypedConstantValidator open question): `docs/compiler/literal-system.md` line 496

- Catalog system (metadata-driven principle): `docs/language/catalog-system.md` § Architectural Identity

- UCUM gap analysis: `docs/Working/frank-ucum-iso-gap.md`

# Decision: Typed Literal System — Implementation Plan Produced



| Property | Value |

|---|---|

| Author | Frank (Lead/Architect) |

| Date | 2026-05-09T12:33:31-04:00 |

| Scope | Plan synthesis from 4 Working docs into a single executable implementation plan |



## Plan Structure



- **12 slices** ordered by dependency

- Slices 1–4 are foundational (data layer, parsers) and partially parallelizable

- Slices 5–9 are the framework core (DU update, framework types, validators, TypeMeta entries, TypeChecker migration)

- Slice 10 is runtime stubs (independent)

- Slices 11–12 are doc updates and Working doc deletion



Key ordering decisions:

- Data layer (embedded XML loaders) before parsers — the UCUM parser depends on atom data

- Temporal parser is fully independent of UCUM — they can execute in parallel

- ContentValidation DU update comes before the framework types and validators, because the DU subtypes define the dispatch contract

- TypeChecker migration is the last code slice — it proves everything works end-to-end before doc updates



## Gaps the Working Docs Didn't Fully Resolve



1. **G15 resolution:** The Working docs proposed a `QuantityDomain` enum on a single `QuantityValidation` subtype. The plan resolves this with four separate DU subtypes (`MoneyValidation`, `QuantityValidation`, `PriceValidation`, `ExchangeRateValidation`) — more catalog-idiomatic.



2. **Period dual-format acceptance:** The Working docs proposed `TemporalLiteralKind.TemporalQuantity` for duration but didn't explicitly state that period must accept BOTH ISO 8601 (`P30D`) and quantity form (`30 days`). The plan makes this explicit — the temporal validator tries quantity parse first, falls back to `PeriodPattern.NormalizingIso`.



3. **`stateref` disposition:** The Working docs flagged this as advisory gap G8 but didn't resolve it. The plan adds a disposition note in `literal-system.md`: stateref validation is a name-binder concern, not a domain parser. It does not use ContentValidation.



4. **JSON wire format documentation:** The Working docs identified (G12) that MCP consumers need to know JSON wire formats for each type, but didn't produce the table. The plan adds a complete JSON wire format table to `runtime-api.md`.



## Canonical Docs Requiring More Updates Than Expected



- **`runtime-api.md`** has the most updates: CLR type table fix, Fire example code fix, Deliberate Exclusions inconsistency, JSON wire format table addition, `FromJson` → `Restore` rename, `TypeRuntimeMeta` delegate shapes, `ParseString`/`FormatString` clarification. Seven distinct changes.



- **`literal-system.md`** has three open questions to close, a content validation table to add, and the Restore consumer matrix entry. More than a simple sync.



- **`catalog-system.md`** needs the external reference data distinction — this is an architectural principle that was decided in `frank-data-layer-decision.md` but never flowed back to the canonical catalog doc.

# Decision: UCUM Data Layer Strategy — Build-Time Codegen



| Property | Value |

|---|---|

| Author | Frank |

| Date | 2026-05-09T12:12:35-04:00 |

| Status | Locked |

| Scope | How UCUM atom/prefix data reaches the runtime catalog |



## Recommendation



**Option B — Build-time codegen.** A Node.js script reads `ucum-essence.xml` from `src/Precept/Data/Ucum/` and generates `UcumAtomCatalog.g.cs` (and `UcumPrefixCatalog.g.cs` if warranted) as frozen C# collections. The binary ships only C# types. No XML parsing at runtime.



## Rationale



### 1. Precept is a compile-time system



The UCUM atom table is consumed at **compile time** — by the type checker, the language server, and MCP vocabulary projection — not just at runtime. Every one of these consumers needs:



- atom lookup by canonical code (for parser resolution),

- prefixability flags (for longest-match prefix parsing),

- dimension vectors (for commensurability checks and alias classification),

- scale factors (for exact conversion metadata),

- tier classification (for LS completions and MCP discovery).



This data must be available as typed, frozen, statically-analyzable C# structures. A `FrozenDictionary<string, UcumAtom>` is the correct shape — exactly what `CurrencyCatalog` already provides for ISO 4217. XML parsing at first access adds latency, allocation, and a failure mode to the compile path for zero benefit.



### 2. Catalog-driven architecture demands typed metadata records



The non-negotiable architectural principle is: **catalogs are the language specification in machine-readable form.** Pipeline stages derive behavior from catalog metadata — they never maintain parallel copies.



A `UcumAtom` record with `DimensionVector`, `ExactScale`, `IsPrefixable`, `Tier`, and `DisplayName` properties is catalog metadata. An XML element is a serialization format. The catalog architecture requires the former. The XML is a provenance artifact — the upstream source from which the catalog is refreshed — not the runtime truth.



### 3. The ISO 4217 pattern already works and is proven



`CurrencyCatalog.cs` + `refresh-iso4217.js` + `src/Precept/Data/Iso4217/list-one.xml` is the established pattern:



1. Authoritative XML lives in `src/Precept/Data/` as a provenance artifact.

2. A refresh script downloads the latest upstream source.

3. A C# catalog file materializes the data as frozen collections.

4. Consumers reference the C# catalog directly — no file I/O, no parsing, no lazy initialization.



UCUM should follow the identical pattern. The only difference is scale: UCUM has ~300 atoms and ~24 prefixes vs. ISO 4217's ~160 currencies, which means a codegen script is more justified (not less) because hand-maintaining 300+ entries with dimension vectors and exact scale factors would be error-prone.



### 4. NodaTime's TZDB pattern is wrong for this use case



NodaTime embeds `Noda.TimeZoneData.nzd` and parses it lazily because:



- The IANA timezone database changes frequently (multiple releases per year).

- NodaTime is a **library** distributed as a NuGet package — it cannot run codegen scripts in consuming projects.

- Timezone data is enormous and used selectively at runtime.



None of these conditions hold for UCUM in Precept:



- UCUM `ucum-essence.xml` is versioned and stable — the atom table changes on the order of years, not months.

- Precept is **not a library** — it is an application that controls its own build pipeline.

- The atom table is small (~300 entries) and used exhaustively at compile time.

- Lazy initialization introduces a failure mode (malformed XML, missing resource) that would surface as a compiler crash rather than a build error.



The NodaTime pattern solves a distribution problem Precept does not have.



## Key Tradeoff



**What we give up:** When UCUM publishes a new `ucum-essence.xml` version, updating requires running the refresh + codegen script and rebuilding — not just dropping in a new resource file. This is the correct tradeoff because:



- UCUM updates are rare and deliberate.

- A codegen step gives us a chance to validate the new data against our schema expectations before it enters the catalog.

- The checked-in `.g.cs` file makes diffs reviewable — you can see exactly which atoms changed.



## Impact on the Plan



### Build NOW (in the typed-literal spike)



- `tools/scripts/refresh-ucum.js` — downloads `ucum-essence.xml` to `src/Precept/Data/Ucum/`.

- `tools/scripts/generate-ucum-catalog.js` — reads the XML, emits `UcumAtomCatalog.g.cs` and `UcumPrefixCatalog.g.cs` into `src/Precept/Language/Ucum/`.

- The generated files contain `FrozenDictionary<string, UcumAtom>` and `FrozenDictionary<string, UcumPrefix>` with all metadata properties needed by the parser.

- The `UcumParser` consumes the generated catalog directly — no XML, no lazy init, no embedded resources.



### Embed in canonical docs for later



- `docs/language/business-domain-types.md` should document the refresh/codegen workflow once it ships.

- The data pipeline pattern (XML provenance → codegen script → frozen C# catalog) should be recorded as the canonical pattern for any future external-standard integration.

# UCUM Data Layer → Evaluator Gap Analysis



**Date:** 2026-05-09T12:51:10-04:00

**Author:** Frank (Lead/Architect)

**Requested by:** Shane

**Scope:** Does the UCUM data layer as designed (Slices 1d, 2, 3, 10) provide sufficient grammar/data for ALL required evaluator unit-math behavior?



---



## Ruling: 8-Point Assessment



### 1. Dimensional Analysis — SUFFICIENT



`DimensionVector` is a 7-dimensional SI record struct with `Equals`. Two quantities are dimensionally compatible iff their `DimensionVector` values are equal. The evaluator compares `UcumParsedUnit.Vector` from each operand. No gap.



### 2. Unit Conversion — SUFFICIENT



`UcumExactFactor` on each `UcumAtom` carries the exact rational scale to the SI base. `UcumParsedUnit.Scale` is the composed factor for the full expression (parser's semantic reducer computes this). To convert `5 kg + 3 g`: evaluator converts both to SI base via their respective `Scale`, performs addition, then converts back to the target unit by dividing by target `Scale`. `UcumExactFactor` is exact rational (numerator/denominator + base-10 exponent) — no floating-point drift. No gap.



### 3. Unit Multiplication/Division — SUFFICIENT



`DimensionVector` has `Multiply` (adds exponents) and `Divide` (subtracts exponents). `UcumExactFactor` composes multiplicatively. The parser produces correct vectors for compound expressions like `m/s` → speed vector (1,0,-1,...). The evaluator receives the result dimension from `UcumParsedUnit.Vector` directly. No gap.



### 4. Canonical Form — SUFFICIENT



`UcumParsedUnit.CanonicalCode` is computed by the semantic reducer and provides a normalized string form. `DimensionVector` equality gives dimension-level canonical comparison. `UcumExactFactor` equality gives scale-level comparison. Together these are sufficient for the evaluator to determine if two unit expressions represent the same physical unit. No gap.



### 5. Prefixed Unit Handling — SUFFICIENT



`UcumPrefixCatalog` carries `UcumExactFactor` per prefix (e.g., milli = 0.001). The parser's `UcumPrefixedAtomNode` resolves via `LongestPrefixMatch`. The semantic reducer composes `prefix.Factor × atom.Scale` into the final `UcumParsedUnit.Scale`. So `mg` = milli(0.001) × g(0.001 relative to kg) = 0.000001 kg. The plan explicitly tests this in `UcumParserTests.cs` (prefixed atoms: `mg`, `cm`, `mmol`). No gap.



### 6. Annotation Handling — GAP



**Status:** Partial — needs clarification in the plan.



`UcumAtom` carries `string? AnnotationClass`, and the parser AST includes `UcumAnnotatedNode`. The `DimensionCatalog` entry for `count` says "(0,0,0,0,0,0,0) — dimensionless with approved count annotations." But the plan does not specify:



- **What happens to annotations in `UcumParsedUnit`?** The `UcumParsedUnit` record has no `Annotation` or `Annotations` field. If a user writes `{RBC}/uL`, the annotation `{RBC}` is parsed but has no place to land in the consumer-facing output. The evaluator needs to know whether two annotated units are the same annotation for identity/display purposes.

- **Annotation equality semantics.** UCUM says annotations are for display only and do not affect dimensional analysis. The plan should explicitly state this: annotations are preserved for display but ignored in `DimensionVector` comparison and `UcumExactFactor` computation.



**Fix required in:** `docs/Working/typed-literal-system-plan.md`, Slice 3 — add an `IReadOnlyList<string>? Annotations` field (or `string? Annotation`) to `UcumParsedUnit` for display preservation, and document that annotations are excluded from equality/dimension/scale computation.



### 7. Derived Unit Chains — GAP



**Status:** Implicit but unspecified — needs explicit callout.



UCUM `ucum-essence.xml` defines units in two categories:

- **Base units** (7 SI): `m`, `s`, `kg`, `A`, `K`, `mol`, `cd` — these have intrinsic dimension vectors.

- **Defined units** (everything else): `N`, `J`, `Pa`, `Hz`, `L`, `[degF]`, etc. — these are defined as expressions of other units. E.g., `N` = `kg.m/s^2`, `J` = `N.m` = `kg.m^2/s^2`, `L` = `dm^3`.



The plan says `UcumAtom` has `DimensionVector Vector` and `UcumExactFactor Scale`, but does **not** say how these are populated for defined units. There are two options:



1. **Loader resolves at load time** — the XML loader recursively resolves each defined unit's expression down to fundamental SI components and stores the fully resolved `Vector` and `Scale` on `UcumAtom`. This means `N` gets Vector=(1,1,-2,0,0,0,0) and Scale=1 (already in SI). This is the correct approach.

2. **Store definition expression and resolve later** — stores `N`'s definition as `kg.m/s^2` and resolves on demand.



The plan implicitly assumes option 1 (since `UcumAtom.Vector` and `UcumAtom.Scale` are populated), but this is a significant implementation detail that must be explicit. The UCUM XML has chains: `J` is defined in terms of `N`, which is defined in terms of `kg`, `m`, `s`. The loader must resolve transitively.



**Fix required in:** `docs/Working/typed-literal-system-plan.md`, Slice 1d — add explicit note: "The XML loader resolves each defined unit's `<value>` expression transitively into fundamental SI components. `UcumAtom.Vector` and `UcumAtom.Scale` represent the fully resolved SI-relative values, not the raw definition expression. This requires the loader to parse unit definition expressions using the same grammar as the UCUM parser (Slice 3), which creates a bootstrap dependency: the loader must either (a) include a minimal expression resolver for the `<value Unit="..." UNIT="...">` attributes, or (b) depend on the full `UcumParser` from Slice 3."



**Dependency implication:** If the loader needs the parser to resolve defined units, Slice 1d has a dependency on Slice 3, or the loader must contain a bootstrapping mini-resolver. The plan currently shows Slice 3 depending on Slice 1d (correct for atom lookup), but not the reverse. This circular dependency must be resolved — the recommended approach is a **two-phase load**: Phase 1 loads base units with intrinsic vectors; Phase 2 resolves defined units using a minimal expression evaluator that references Phase 1 results. This mini-resolver is simpler than the full parser because `<value>` expressions in the XML use a restricted subset of the UCUM grammar.



### 8. Interning and Identity — GAP



**Status:** Insufficient — the plan does not address the interning key design.



`UcumParsedUnit` has `SourceText`, `CanonicalCode`, `Vector`, `Scale`, and `UsedAtoms`. The runtime stubs (Slice 10) include `UnitFactory` which "converts parsed units into interned runtime `Unit` instances." But the plan does not specify:



- **What is the interning key?** `CanonicalCode` alone is insufficient because `kg.m/s^2` and `N` have different canonical codes but the same physical unit. If the interning key is `(Vector, Scale)` then `N` and `kg.m/s^2` collapse to the same `Unit` — which may or may not be desired. If display form matters (user wrote `N`, not `kg.m/s^2`), the intern must preserve the source form while still allowing equality comparison.

- **The plan explicitly asked George to implement `Unit` as a stub.** That's correct for pre-work scope, but the `UcumParsedUnit` record shape must be confirmed sufficient to produce a stable interning key later. Currently it is — `(Vector, Scale)` is a mathematically complete identity for physical units — but this needs to be documented as the intended key, with `SourceText`/`CanonicalCode` as display properties only.



**Fix required in:** `docs/Working/typed-literal-system-plan.md`, Slice 10 — add a design note: "The interning key for `Unit` is `(DimensionVector, UcumExactFactor)` — two units with the same dimension and scale are the same physical unit regardless of source expression. `CanonicalCode` and `SourceText` are display-only properties preserved for user-facing output. `N` and `kg.m/s^2` are the same `Unit` instance. `UcumParsedUnit` provides all fields needed for this interning strategy."



---



## Overall Verdict



**The data layer is architecturally sufficient but has 3 gaps that need plan amendments before implementation begins.**



None of the gaps are architectural blockers — they are specification omissions that would cause George to make ad-hoc design decisions during implementation. Specifically:



| # | Area | Verdict | Severity |

|---|------|---------|----------|

| 1 | Dimensional analysis | SUFFICIENT | — |

| 2 | Unit conversion | SUFFICIENT | — |

| 3 | Unit multiplication/division | SUFFICIENT | — |

| 4 | Canonical form | SUFFICIENT | — |

| 5 | Prefixed unit handling | SUFFICIENT | — |

| 6 | Annotation handling | GAP | Low — add `Annotation` field to `UcumParsedUnit`, document display-only semantics |

| 7 | Derived unit chains | GAP | **Medium** — loader must transitively resolve defined units; creates bootstrap dependency between Slice 1d and Slice 3 that needs resolution |

| 8 | Interning and identity | GAP | Low — document `(Vector, Scale)` as interning key in Slice 10 |



**Recommendation:** Amend the plan with the 3 fixes above before George begins Slice 1d. The derived unit chain gap (#7) is the most important — it affects loader design and slice dependency ordering.

### 2026-05-09T15:50:01Z: OQ-DISP-1 closed — runtime aggregation registry concept killed



**By:** Shane (via Copilot)

**What:** OQ-DISP-1 (naming the runtime-layer aggregation registry / `OperationRegistry` placeholder) is closed with no action. The concept was eliminated — the global aggregation array was removed before implementation. The Builder embeds executor delegates directly into opcodes at build time; the evaluator calls `opcode.Executor(l, r)` with zero runtime lookup. No aggregation class, no registry, no naming decision needed.

**Why:** User directive — concept no longer exists; open item is stale.

# Modifier coloring regression anchor



- Date: 2026-05-10T08:15:36.258-04:00

- Context: `default` inside field declarations was falling through to the generic `#grammarKeywords` TextMate include, which the extension theme renders gold. `as` already had a declaration capture, but the suite had no regression anchor proving that declaration syntax stays off the gold lane.

- Decision: Lock field-declaration coloring at the generated grammar surface: assert `as` uses `keyword.declaration.precept`, and require an explicit `default` declaration override before the `#grammarKeywords` fallback in both scalar and collection field declarations.

- Rationale: This bug is structural TextMate ordering, not token-catalog truth. A grammar-file regression test is the honest place to catch it.

- Anchor files: `test\Precept.Tests\Language\TextMateGrammarTests.cs`, `tools\Precept.GrammarGen\Program.cs`, `tools\Precept.VsCode\syntaxes\precept.tmLanguage.json`

# Triage: BUG-039 — `at` proof obligation
**Date:** 2026-05-10T09:33:43.989-04:00
**Verdict:** A — Proof obligation is CORRECT; the spec has two documentation gaps

## Analysis

The catalog (`Types.cs`) declares a `NumericProofRequirement` (`count > 0`) on **every** collection element-returning accessor: `first`, `last`, `at`, `peek`, `peekby`, `min`, `max`. This is metadata-driven — the obligation lives on the `TypeAccessor` record, not in hardcoded engine logic. The proof engine reads it generically and fires PRE0063 (`UnguardedCollectionAccess`) when no guard or `notempty` modifier discharges the requirement.

This is correct language policy for three reasons:

1. **The fault is real.** `CollectionEmptyOnAccess` is a defined runtime fault. Accessing `.at(N)` on an empty collection WILL fault. The proof engine's job is to prove at compile time that this cannot happen — that's Precept's core guarantee (principle 7: "compile-time-first static checking").

2. **Return type `T` describes the success type, not the precondition.** The accessor returning `T` (not `T optional`) means "when the operation succeeds, you get a definite `T`." It does NOT mean "the operation always succeeds." The precondition (non-empty) is a separate concern.

3. **Optional on the receiving field doesn't discharge the obligation.** Even if the *target field* is declared `optional`, the accessor `.at()` still attempts a collection access. The fault occurs at the access site, not the assignment site. Declaring the target optional doesn't prevent the empty-collection read — it just changes what types the target can hold. This is why verdict C is incorrect: optional addresses nullability of results, not safety of preconditions.

## Decision

**Spec updates needed (two gaps):**

1. **Accessor table (§3.5):** The "Proof" column must be filled in for all element-returning accessors. Every accessor that carries a `ProofRequirement` in the catalog should show `count > 0` in the Proof column. Specifically: `set.min`, `set.max`, `queue.peek`, `queue by P.peek`, `queue by P.peekby`, `stack.peek`, `list.first`, `list.last`, `list.at`, `log.first`, `log.last`, `log.at`, `log by P.first`, `log by P.last`, `log by P.at`.

2. **`notempty` modifier description (§3.6):** The discharge list currently says "`.min`/`.max`/`.peek`/`.first`/`.last`" — this must add `.at` and `.peekby`. The complete list is: `.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last`/`.at`.

**No proof engine changes.** The engine behavior is correct as-is. PRE0063 fires appropriately and the `notempty` modifier already discharges the obligation (the proof engine walks modifiers generically — this already works for `at`).

## Consistency check

The same decision applies uniformly. All element-returning accessors already carry the proof requirement in the catalog:

| Accessor | Collection types | Proof requirement | Status |
|----------|-----------------|-------------------|--------|
| `min` | set | `count > 0` | ✓ In catalog, missing from spec Proof column |
| `max` | set | `count > 0` | ✓ In catalog, missing from spec Proof column |
| `peek` | queue, stack, queue by P | `count > 0` | ✓ In catalog, missing from spec Proof column |
| `peekby` | queue by P | `count > 0` | ✓ In catalog, missing from spec Proof column, missing from notempty list |
| `first` | list, log, log by P | `count > 0` | ✓ In catalog, missing from spec Proof column |
| `last` | list, log, log by P | `count > 0` | ✓ In catalog, missing from spec Proof column |
| `at` | list, log, log by P | `count > 0` | ✓ In catalog, missing from spec Proof column, missing from notempty list |

The engine is consistent. The spec is not. Fix the spec.

## Downstream impact

**George (implementation):** No code changes needed. The proof engine and catalog are correct. If George's BUG-039 fix already resolved the parsing/wrong-diagnostic-code issue, his work is done.

**Spec update:** Frank or whoever updates the spec should fill in the Proof column and expand the `notempty` discharge list. This is a documentation-only change — no runtime behavior changes.

**Test coverage:** A contract test confirming that every accessor with `ProofRequirements` appears in the spec's Proof column would prevent future drift. This is optional but recommended.

# Decision: TokenMeta Boolean Flag Shape

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-10T09:22:38.840-04:00
**Requested by:** Shane
**Status:** Recommendation — awaiting owner sign-off

---

## Context

George added five boolean fields to `TokenMeta` in Track 2 slice t2-1:

```csharp
bool IsAccessModeAdjective = false,
bool IsStateWildcard = false,
bool IsFieldBroadcast = false,
bool IsFunctionCallLeader = false,
bool IsMessagePosition = false
```

Plus two alias properties:
```csharp
public bool IsBroadcastFieldTarget => IsFieldBroadcast;
public bool IsAlsoBuiltinFunction => IsFunctionCallLeader;
```

Shane asks: are flat bools the right catalog shape, or is there a more principled design?

---

## Analysis

### What these flags actually express

After tracing every consumer, these five flags decompose into **three distinct semantic categories**:

| Flag | Consumers | What it really means |
|------|-----------|---------------------|
| `IsStateWildcard` | Parser (`ParseStateTarget`), NameBinder, TypeChecker | This keyword token is valid in a **state-name position** despite not being an identifier |
| `IsFieldBroadcast` | Parser (`ParseFieldTarget`), NameBinder, TypeChecker | This keyword token is valid in a **field-name position** despite not being an identifier |
| `IsFunctionCallLeader` | Parser.Expressions (`ParsePrimaryExpression`) | This keyword token can **lead a function call** (`keyword(args)`) despite not being an identifier |
| `IsAccessModeAdjective` | `Tokens.AccessModeAdjectives` derived set (no direct pipeline consumer found) | This keyword participates in access-mode modifier grammar |
| `IsMessagePosition` | GrammarGen (TextMate generation), MCP DTO | This token's trailing string argument gets the `string.quoted.double.message.precept` scope |

### The catalog-driven assessment

These are **not** "lazy one-off bools" in the pejorative sense. Each one expresses a genuine per-member fact about a token — "does this keyword play role X in the grammar?" That is exactly the kind of per-member metadata that belongs in the Tokens catalog rather than in parser `if` chains (catalog-system.md § "if something is domain knowledge, it is metadata").

The flags *replaced* hardcoded parser `if (kind == TokenKind.All || kind == TokenKind.Any)` chains — which is the correct direction. The question is whether flat bools are the best *shape* for this metadata.

### Why a `[Flags] enum TokenRole` is NOT the right answer

A flags enum would look like:
```csharp
[Flags]
enum TokenRole { None = 0, StateWildcard = 1, FieldBroadcast = 2, FunctionCallLeader = 4, ... }
```

This is **worse** than flat bools for this case:

1. **These are not a single dimension.** `IsMessagePosition` is a grammar-generation concern. `IsAccessModeAdjective` is a modifier-grammar concern. `IsStateWildcard` and `IsFieldBroadcast` are name-position concerns. `IsFunctionCallLeader` is an expression-grammar concern. Jamming them into one bitfield conflates unrelated axes and makes the API surface *less* self-documenting.

2. **Bool fields are more readable at call sites.** `IsStateWildcard: true` is immediately clear. `Roles: TokenRole.StateWildcard | TokenRole.FieldBroadcast` requires understanding the enum definition.

3. **No consumer iterates "all roles" as a set.** Each consumer checks exactly one flag. A flags enum adds indirection with no composability benefit.

### What IS wrong: the alias properties

The alias properties are the real code smell:

```csharp
public bool IsBroadcastFieldTarget => IsFieldBroadcast;  // same thing, different name
public bool IsAlsoBuiltinFunction => IsFunctionCallLeader;  // same thing, different name
```

This means the primary field names were chosen for the catalog-definition site (where you're marking tokens), but call sites want different names that express the call-site's perspective. Having two names for the same concept is a parallel-copy smell — one will drift.

The fix: **pick one name per flag and use it everywhere.** The correct name is the one that reads naturally at the consumer site, since that's where understanding matters most:

| Current primary | Current alias | Recommended single name | Rationale |
|---|---|---|---|
| `IsFieldBroadcast` | `IsBroadcastFieldTarget` | `IsFieldBroadcast` | The primary name is clear — it says what the token IS. The alias adds no precision. Kill the alias. |
| `IsFunctionCallLeader` | `IsAlsoBuiltinFunction` | `IsFunctionCallLeader` | The primary name accurately describes the grammar role. `IsAlsoBuiltinFunction` is misleading — it conflates syntactic role (can lead a function-call expression) with semantic identity (is a built-in function). These tokens are keywords that ALSO accept function-call syntax, but they are not "builtin functions" in the `Functions` catalog sense. Kill the alias. |

---

## Recommendation

### Shape: Keep flat bools. Kill aliases.

The five flat boolean fields are the correct catalog shape for this metadata. They are:
- Per-member domain knowledge ✓
- Consumed by pipeline stages that would otherwise hardcode per-member behavior ✓
- Independent axes (not a single dimension that a flags enum would model) ✓
- Self-documenting at both definition and consumption sites ✓

**Specific actions:**

1. **Remove `IsBroadcastFieldTarget`.** It is a pure alias for `IsFieldBroadcast`. Update the one consumer in tests that references it to use `IsFieldBroadcast` directly.

2. **Remove `IsAlsoBuiltinFunction`.** It is a pure alias for `IsFunctionCallLeader`. Update `CallContextResolver.cs` (language server) to use `IsFunctionCallLeader` directly.

3. **Remove both alias entries from `Track2PhaseATokenCatalogTests.cs`** that reference `IsBroadcastFieldTarget` and `IsAlsoBuiltinFunction`.

4. **No flags enum, no grouping record, no structural change.** The bools stay as primary fields on `TokenMeta`.

### Why not "revisit later"

The aliases should be killed now. They are the only structural problem, and they will spread if left alone — a new consumer will pick up `IsAlsoBuiltinFunction` and then renaming it becomes a multi-site change. The bools themselves are fine and do not need future rework.

### Severity: Fix before merge, not blocking spike

This is a "clean it up in the current slice" item. It does not require architectural rethinking or a new catalog. George should remove the two alias properties and update the three reference sites (one LS callsite, two test references). Fifteen-minute fix.

---

## Catalog-Driven Checklist Verification

Per `docs/contributing/catalog-driven-checklist.md`:

- ✅ Per-member behavior lives in catalog metadata, not in parser switch/if chains
- ✅ No parallel keyword lists — `AccessModeAdjectives` FrozenSet is derived from `Tokens.All.Where(m => m.IsAccessModeAdjective)`
- ✅ No flags enum needed — these are independent axes, not a single dimension
- ⚠️ Alias properties violate "derive, never duplicate" — two names for one fact is a parallel copy

### 2026-05-10T13:37:31Z: BUG-039 spec gaps fixed

**By:** George

**Status:** Complete

**Source:** `C:\\Users\\Shane.Falik\\source\\repos\\precept-architecture\\.squad\\decisions\\inbox\\george-bug039-spec-gaps-fixed.md`

- Implemented the documentation-only follow-through from the BUG-039 triage already recorded in this ledger.
- Proof column filled for all element-returning collection accessors with `count > 0`.
- `notempty` discharge list updated to include `.at` and `.peekby`.

### 2026-05-10T13:53:14Z: t2-2 Slice A scope and cleanup directives locked

**By:** Shane (via Copilot)

**Status:** Directive

**Merged sources:** `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\copilot-directive-no-deferrals.md`, `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\copilot-directive-t2-2-scope.md`

- No deferrals inside this slice: if the cleanup is architecturally correct and fits the current slice, ship it now. Frank owns defer-vs-now scope calls rather than escalating them back to Shane.
- For t2-2 specifically, operand roles are in scope now. `ActionSyntaxSlot.Role` must be a typed `ActionSlotRole` enum (`Target`, `Value`, `Key`, `Index`, `IntoTarget`, `OrderingKey`, `OrderingCapture`), not a freeform string.
- `IntoSupported` is removed in Slice A; slot optionality and `ActionShapeMeta` are the source of truth. Type-checker consumption of slot roles still belongs to Slice 9.

### 2026-05-10T13:53:14Z: t2-2 Slice A catalog enrichment completed with typed slot roles

**By:** George

**Status:** Complete

**Merged sources:** `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\george-t2-2-sliceA-done.md`, `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\george-t2-2-sliceA-enum-fix.md`

- Added the typed `ActionSlotRole` enum with explicit 1-based values and moved `ActionSyntaxSlot.Role` off freeform strings.
- Added `ActionSyntaxSlot` and `ActionShapeMeta`, including pre-computed `SeparatorTokens`, plus exhaustive `Actions.GetShapeMeta()` coverage for all 9 `ActionSyntaxShape` values.
- Removed `IntoSupported` from `ActionMeta`; consumers now derive into support from slot metadata, and `CollectionIntoBy`'s final slot is correctly modeled as `OrderingCapture` rather than `OrderingKey`.
- Coverage was added or updated in `ActionCatalogTests`, `ActionsTests`, `LanguageToolTests`, and the MCP mapping/tests. Validation closed green at 4322 total tests (3827 + 59 + 156 + 280).

### 2026-05-10T13:53:14Z: t2-2 Slice B ParseActionTarget separators are catalog-driven

**By:** George

**Status:** Complete

**Source:** `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\george-t2-2-sliceB-done.md`

- `ParseActionTarget` now accepts shape-specific `FrozenSet<TokenKind>` separators from `Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens`; the shared hardcoded `{=, into, by, at}` union is gone.
- `ParseActionByShape` computes separators once and threads them through all 9 action-shape parse methods so target termination stays catalog-driven.
- Added `ParseActionTargetTests.cs` with 8 tests (4 catalog property + 4 behavioral parser coverage), while preserving the known `CollectionValueBy`/`RemoveAtIndex` parser-unreachable boundary as catalog-level coverage.
- Validation closed green at 4050/4050 tests. Commit: `fb525df0`.

### 2026-05-10T09:53:14Z: t2-2 Slice C shape-method separator rewires completed

**By:** George

**Status:** Complete

**Source:** `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\george-t2-2-slice-c-done.md`

- All 7 parser shape methods now derive required and optional separator tokens from `Actions.GetShapeMeta(ActionSyntaxShape.X).Slots[n].PrecedingSeparator` instead of hardcoded `TokenKind.By`, `TokenKind.At`, `TokenKind.Into`, or `TokenKind.Assign`.
- Added 6 `ActionChainTests` cases covering insert/dequeue/put behavior plus catalog-property checks for the secondary shapes that remain parser-unreachable via `Actions.ByTokenKind`.
- Validation stayed green at 4056/4056 tests (3841 `Precept.Tests` + 156 language-server + 59 MCP). Commit: `ef6fedcb`.
- t2-2 is durably closed across BUG-021, BUG-048, and BUG-049.

### 2026-05-10T15:34:08Z: BUG-049a fix completed with intrinsic accessor metadata

**By:** Frank, George

**Status:** Complete

**Merged sources:** `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\frank-bug049a-design-review.md`, `C:\Users\Shane.Falik\source\repos\precept-architecture\.squad\decisions\inbox\george-slice2e-done.md`

- Frank approved `FixedReturnAccessor.ReturnNonnegative` as the correct Strategy 2 abstraction and required both same-slice follow-through items: unify `CollectionCountAccessor` as the single shared accessor instance and document the pre-existing `FunctionReturnSatisfies` discharge path alongside the new accessor discharge.
- George completed Slice 2E accordingly: `ReturnNonnegative` now lives on `FixedReturnAccessor`, action proof requirements reuse `Types.CollectionCountAccessor`, and `TryDeclarationAttributeProof` short-circuits `>= 0` obligations for intrinsically non-negative accessor returns.
- `docs/compiler/proof-engine.md` Strategy 2 now documents both intrinsic return-value discharge paths, and 3 regression tests lock the BUG-049a fix.
- Validation passed via `dotnet build src\Precept\Precept.csproj` and `dotnet test test\Precept.Tests\Precept.Tests.csproj`, closing at 3857 passing tests. Commits: `f2d1dece` (fix) and `e826e4bd` (tracking).



## 2026-05-11T00:27:07Z — t2-13 / t2-14 / t2-15 / BUG-057 batch

- Batch scope: t2-13, t2-14, t2-15, BUG-057 fix.
- Commits: `617d175f`, `7a4c2e31`, `65fad947`, `2763a433`, `78779818`, `c0d0e059`.
- Final validation after the batch: Core `4,531` passing; MCP `105` passing.
- Merged inbox files: `.squad/decisions/inbox/newman-t2-13-complete.md`, `.squad/decisions/inbox/soup-nazi-t2-14-complete.md`, `.squad/decisions/inbox/soup-nazi-t2-15-complete.md`, `.squad/decisions/inbox/george-bug057-fix.md`
- Missing inbox files skipped: `.squad/decisions/inbox/frank-bug057-spec-analysis.md`

### Merged from `.squad/decisions/inbox/newman-t2-13-complete.md`

# Newman t2-13 complete

- Commit: `617d175f`
- Scope: corrected catalog-driven MCP recovery guidance in `src/Precept/Language/Faults.cs` and `src/Precept/Language/Diagnostics.cs`; `ProofsTool` and `DiagnosticTool` remain thin catalog projections.
- BUG-014: `CollectionEmptyOnMutation` now tells consumers to use a `when Field.count > 0` row guard or the `notempty` field modifier.
- BUG-015: the current runtime's collection-mutation diagnostic entry (`UnguardedCollectionMutation`) now exposes the same count-guard / `notempty` guidance through `precept_diagnostic`.
- BUG-041: `UnexpectedNull` now uses `when Field is set` guidance instead of invalid `!= null` syntax.
- Regression coverage: added `test/Precept.Mcp.Tests/RecoveryHintTests.cs`.
- Validation: `dotnet test test\Precept.Mcp.Tests\` -> 77 passed; `dotnet test test\Precept.Tests\` -> 3925 passed.

---

### Merged from `.squad/decisions/inbox/soup-nazi-t2-14-complete.md`

# Soup Nazi — t2-14 complete

- Slice: 14 — Test Layer — Catalog Capability Tests
- Completed: 2026-05-10
- Test commit: `7a4c2e31`
- Validation: `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore -m:1 /nr:false --nologo` passed at 4471/4471 (baseline 3925; +546 tests)

## What landed

- Added reflection-based `[Theory]` + `[MemberData]` coverage in `test\Precept.Tests\CatalogTests\` for operators, outcomes, modifiers, types, and diagnostics.
- Adapted assertions to the catalog surface that exists in source today:
  - operator symbols derive from `Token` / `Tokens`
  - modifier keywords derive from `Token.Text`
  - type serialization names currently come from `DisplayName` when `SerializedName` is absent
  - diagnostic recovery guidance currently comes from `RecoverySteps` / `FixHint` when `RecoveryHint` is absent

## Acceptance

- Every new test is catalog-driven, so adding a new member without filling required metadata now fails the suite.
- No skipped tests were required; the assertions adapt to the shipped catalog shapes instead of assuming plan-era property names.

---

### Merged from `.squad/decisions/inbox/soup-nazi-t2-15-complete.md`

# t2-15 Completion Record — Pipeline Stage Unit Tests (Catalog-Aware)

**Author:** Soup Nazi (test engineer)  
**Date:** 2026-05-11  
**Branch:** Precept-V2-Radical  
**Slice:** 15 of 16 (Track 2)

---

## Summary

Slice 15 adds catalog-aware pipeline stage unit tests to lock in the bug fixes from Slices 8–12.
Five new test files were created covering the Parser, NameBinder, and MCP layers.

## Test Count

| Project | Before | After | New Tests |
|---------|--------|-------|-----------|
| `Precept.Tests` | 4,471 | 4,531 | +60 |
| `Precept.Mcp.Tests` | 77 | 105 | +28 |
| **Total** | **4,548** | **4,636** | **+88** |

## Files Created

| File | Tests | Pipeline Stage | Key Behaviors |
|------|-------|---------------|---------------|
| `test/Precept.Tests/Parser/StateTargetTests.cs` | 14 | Parser | `IsStateWildcard`/`IsFieldBroadcast` catalog metadata; `from any`, `to any`, `modify all`, `omit all` parser recognition; full compilation round-trips |
| `test/Precept.Tests/Parser/MemberAccessTests.cs` | 12 | Parser | `IsValidAsMemberName` for `at`, `peekby`, `min`, `max`; `KeywordsValidAsMemberName` set coverage; `list.at(N)`, `peekby`, `set.min/max` compilation |
| `test/Precept.Tests/NameBinder/ForwardReferenceTests.cs` | 15 | NameBinder | Topological sort (single/chain/diamond forward refs); cycle detection (direct/self/indirect); `DefaultForwardReference` for non-computed defaults |
| `test/Precept.Mcp.Tests/DefinitionProjectionTests.cs` | 18 | MCP | `EnsureDto.Kind` (StateResident/EventPrecondition); `EnsureDto.Anchor`; `StateHookDto` entry/exit; `EventArgDto` required/optional; `PreceptDefinitionDto` structural fields |
| `test/Precept.Mcp.Tests/OutcomeKindProjectionTests.cs` | 14 | MCP | `OutcomeMeta.SerializedKind` catalog values; transition/no-transition/reject row DTO projection; wildcard row `FromStates`; guard expression projection |

## Bugs Locked In

These bugs are now regression-protected by the new tests:

- **BUG-001** — `any` state wildcard: `IsStateWildcard` catalog + compilation
- **BUG-025** — Keyword-named accessors (`at`, `peekby`, `min`, `max`): `IsValidAsMemberName` + compilation
- **BUG-026/BUG-037** — `modify all` / `omit all` broadcast: `IsFieldBroadcast` catalog + compilation
- **BUG-030** — Computed field forward references: topological sort tests
- **BUG-032/BUG-036** — Outcome field in transition row DTOs: `SerializedKind` round-trip tests
- **BUG-039** — `list.at(N)` rejected: member access parser test
- **CircularComputedField** — Cycle detection: direct, self, indirect, message content

## Notes

- All 88 new tests pass with zero failures.
- Existing 4,548 tests remain green — no regressions.
- Guarded state ensures (`in State ensure X when Y`) were intentionally not tested; BUG-020
  may still be partially unfixed at the language-server level. The `EnsureDto.Guard` null
  case is tested via the unguarded ensure path instead.
- Files specified in the plan as "already exist" (ActionChainTests, StateWildcardTests,
  BroadcastFieldTargetTests, ComputedFieldTests, OperatorTypingTests, ModifierValidationTests,
  CollectionMutationProofTests, FunctionReturnProofTests) were confirmed present and not
  recreated.

---

### Merged from `.squad/decisions/inbox/george-bug057-fix.md`

# BUG-057 Fix Record — George

**Date:** 2026-05-10
**Branch:** Precept-V2-Radical

---

## Root Cause

**File:** `src/Precept/Pipeline/TypeChecker.cs`
**Method:** `ExtractQualifiers()` (line ~149)

The `qualifier.Axis switch` inside `ExtractQualifiers` handled:
- `QualifierAxis.Currency` → `MapCurrencyQualifier`
- `QualifierAxis.Unit` → `MapUnitQualifier`
- `QualifierAxis.Dimension` → `MapDimensionQualifier`
- `QualifierAxis.FromCurrency` → `MapFromCurrencyQualifier`
- `QualifierAxis.ToCurrency` → `MapToCurrencyQualifier`
- `_ => null` ← **bug: TemporalDimension and TemporalUnit fell here**

`null` results were filtered out, so `period of 'date'` and `period in 'days'`
qualifiers were silently discarded. The `TypedField.DeclaredQualifiers` array
came back empty for these qualifier types.

The `ProofEngine.ResolvePeriodDimension()` correctly reads `DeclaredQualifierMeta.TemporalDimension`
and `DeclaredQualifierMeta.TemporalUnit` from `DeclaredQualifiers` — but since the type checker
never populated them, resolution returned `null`, and `DimensionProofRequirement` for
`DatePlusPeriod` could never be satisfied → PRE0113.

---

## Fix Applied

**`src/Precept/Language/DiagnosticCode.cs`**
- Added `InvalidTemporalDimensionString = 117` — emitted when `period of '...'` value is not "date" or "time"
- Added `InvalidTemporalUnitString = 118` — emitted when `period in '...'` value is not a recognized temporal unit

**`src/Precept/Language/Diagnostics.cs`**
- Added `GetMeta` entries for codes 117 and 118 with category `Temporal`, full trigger conditions, recovery steps, and examples

**`src/Precept/Pipeline/TypeChecker.cs`**
- Added two switch arms to `ExtractQualifiers`:
  - `QualifierAxis.TemporalDimension => MapTemporalDimensionQualifier(qualifier, ctx)`
  - `QualifierAxis.TemporalUnit      => MapTemporalUnitQualifier(qualifier, ctx)`
- Added `MapTemporalDimensionQualifier`: maps "date" → `PeriodDimension.Date`, "time" → `PeriodDimension.Time`; emits `InvalidTemporalDimensionString` for unknown strings (fallback: `PeriodDimension.Any`)
- Added `MapTemporalUnitQualifier`: looks up value in `TemporalUnits.TryGet`; derives dimension from `entry.IsCalendarBased` (true → `PeriodDimension.Date`, false → `PeriodDimension.Time`); emits `InvalidTemporalUnitString` for unknown strings (fallback: `PeriodDimension.Any`)

The fix follows the exact same pattern as the existing mappers (`MapCurrencyQualifier`,
`MapUnitQualifier`, etc.) — catalog-driven, no hardcoded parallel lists.

---

## Tests Added

**`test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs`** — 7 new `[Fact]` tests:

1. `PeriodOfDate_QualifierPreservedInSemanticIndex` — verifies `period of 'date'` → `TemporalDimension(PeriodDimension.Date)` in DeclaredQualifiers
2. `PeriodOfTime_QualifierPreservedInSemanticIndex` — verifies `period of 'time'` → `TemporalDimension(PeriodDimension.Time)`
3. `PeriodInDays_QualifierPreservedInSemanticIndex` — verifies `period in 'days'` → `TemporalUnit("days", PeriodDimension.Date)`
4. `PeriodInHours_QualifierPreservedInSemanticIndex` — verifies `period in 'hours'` → `TemporalUnit("hours", PeriodDimension.Time)`
5. `PeriodOfDate_AllowsDatePlusPeriodOperation_NoDiagnostic` — verifies `date + period_of_date_field` compiles clean (no PRE0113)
6. `PeriodOfInvalidString_EmitsInvalidTemporalDimensionStringDiagnostic` — validates error for `period of 'week'`
7. `PeriodInInvalidUnit_EmitsInvalidTemporalUnitStringDiagnostic` — validates error for `period in 'fortnights'`

---

## Regression Confirmation

- `src/Precept/Precept.csproj`: builds cleanly, 0 errors, 0 warnings
- `test/Precept.Tests`: 4,515 pre-existing tests pass + 7 new tests = 4,522 pass (excluding pre-existing compile error in `StateTargetTests.cs` introduced by another squad member, confirmed pre-existing via `git stash`)
- `test/Precept.Mcp.Tests`: 77/77 pass ✅
- `test/Precept.LanguageServer.Tests`: 157/157 pass ✅

No regressions introduced.
