# Track 2: Catalog Compliance & Bug Fix — Master Implementation Plan

**Owner:** Frank (Lead)  
**Status:** In Progress  
**Branch:** Precept-V2-Radical  
**Last Updated:** 2026-05-10  
**Bug file:** `docs/Working/precept-toolchain-bugs.md` (BUG-001 through BUG-054)

---

## Overview

The Precept runtime has accumulated 54 confirmed bugs across its compilation pipeline and MCP
definition-serialization layer. The bugs are not randomly distributed: nearly every one of them
is a symptom of pipeline stages that hardcode language behavior instead of deriving it from
catalog metadata. The parser routes actions by hardcoded kind-switch rather than reading
`ActionMeta.SyntaxShape`. The type checker resolves operator result types by hardcoded
`OperatorKind` dispatch rather than reading from `OperatorMeta`. The MCP serialization layer
omits or flattens catalog-derived structure that the DTOs have no slot to hold. The name binder
treats `any` as a literal state lookup and `all` as a field name, ignoring the token
classification metadata that already distinguishes wildcards from identifiers.

Track 2 addresses this in two phases that must be executed in order. **Phase A (Slices 1–7)**
adds missing fields to the catalogs so that pipeline stages have somewhere to read the truth
from. **Phase B (Slices 8–15)** rewires pipeline stages and MCP DTOs to read those fields
instead of their current hardcoded equivalents. Doing Phase B without Phase A produces
patchwork fixes that cement the catalog-drift pattern. Doing Phase A first makes Phase B
mechanically derivable — in almost every case, "fix the pipeline stage" reduces to "replace
the switch or hardcoded check with a catalog field read."

A third layer — Slices 14–15 — closes the test gap. The 54 bugs were all detectable before
they shipped if the project had had catalog-capability fixture tests (one test per catalog
member, exercising that member through the full pipeline). Those tests would have caught ~40
of 54 bugs. Adding them now provides the regression net that prevents recurrence.

---

## Architectural Principle

Precept is a **metadata-driven architecture.** Domain knowledge lives in catalogs. Pipeline
stages are generic machinery that reads it. This is the foundational architectural identity of
the project and is non-negotiable (see `docs/language/catalog-system.md` § Architectural
Identity).

The corollary for Track 2 is sharp: **no pipeline stage may hardcode behavior that a catalog
already encodes.** The smell is any `kind switch { FooKind.Bar => ..., FooKind.Baz => ... }`
inside a pipeline stage where the reason each branch exists is "the language says so" — because
the catalog already says so and the pipeline stage should read it. Switching on a DU subtype is
correct (the subtype is the metadata shape); switching on a member's enum identity to apply
per-member behavior is the smell.

Every fix in Slices 8–11 must read from a catalog field rather than patch a switch arm. If the
catalog field does not exist, it must be added first (Slices 1–7).

---

## Implementation Strategy

**Fix order is mandatory:**

1. **Catalog field additions FIRST (Slices 1–7).** Before any pipeline fix, add the missing
   metadata fields to the catalog records (`TokenMeta`, `ActionMeta`, `ModifierMeta`,
   `OperatorMeta`, `ConstructMeta`, `OutcomeMeta`, `FunctionMeta`). These fields are what
   pipeline stages will read. Pipeline fixes that try to read non-existent fields cannot compile.

2. **Pipeline stage fixes SECOND (Slices 8–11).** Parser, type checker, name binder, and proof
   engine fixes read the newly added catalog fields. Each fix replaces a hardcoded switch or
   check with a catalog field read. Slices 8–11 may proceed in parallel with each other once
   all of their prerequisite catalog fields from Slices 1–7 are in place.

3. **MCP DTO audit THIRD (Slice 12).** MCP DTOs project from `SemanticIndex` types. Once the
   pipeline correctly produces typed structures, the DTO projection can be fixed to reflect
   the full catalog-derived shape. Some DTO fields (`StateHookDto`, `AccessModeDto`) already
   exist in `CompileToolDtos.cs` but are not wired into `PreceptDefinitionDto` or populated
   in `CompileTool.MapState`. Others (outcome kind, arg optionality, collection element types)
   require new fields on existing DTOs.

4. **MCP-docs fixes FOURTH (Slice 13).** Recovery hints in `ProofsTool.cs` and `DiagnosticTool.cs`
   are prose attached to `DiagnosticMeta` or `RuntimeFaultMeta`. Update after the compiler bugs
   they describe are fixed, so the corrected hints are accurate.

5. **Test layer additions FIFTH (Slices 14–15).** Catalog-capability tests and pipeline-stage
   unit tests provide the regression net. Write them against the fixed behavior to lock it in.

---

## Status Tracker

**Status evidence rule (2026-05-10):**
- `✅ Already satisfied` = the repo already contains the needed source-of-truth shape with no new slice work left to prove.
- `🟡 Worktree-landed` = the current working tree shows the slice's catalog/test surface, but there is no isolated commit on this branch yet.
- `🔄 Active` = the working tree shows live edits, but coordinated sync or finish-proof is still open.
- `⬜ Not started` = no trustworthy repo evidence yet.

**Current reconciliation:** treat the tiny parser/type-checker/proof reads bundled into the safe Phase A batch as part of Slices 1/3/7 prerequisite closure. Do **not** mark Slices 8, 9, or 11 started unless the dedicated Phase B consumer rewires begin.

Slices 1–2 bundle catalog + pipeline work and close bugs directly. Slices 3–7 are catalog prerequisites only — bugs close when the consuming pipeline slice (8–12) lands.

| Slice ID | Title | Status | Bugs Closed |
|----------|-------|--------|-------------|
| Slice 1 | TokenMeta — Catalog fields + parser rewire | ✅ Complete — `19569dda`, `a4cc3927` | BUG-001, BUG-006, BUG-025, BUG-026, BUG-037, BUG-039, BUG-051 |
| Slice 2 | ActionMeta — Catalog fields + parser rewire (t2-2) | ✅ Complete — Slices A–C (`edc95ad3`, `fb525df0`, `ef6fedcb`) + Slice D (`a65c9fed`). BUG-021, BUG-048, BUG-049b closed. | BUG-021, BUG-048, BUG-049b |
| Slice 2E | Proof Engine — `FixedReturnAccessor.ReturnNonnegative` early exit (BUG-049a) | ✅ Complete — `f2d1dece` | BUG-049a |
| Slice 3 | ModifierMeta — Catalog fields (prereq for Slice 8) | ✅ Complete — `b1c95512` | — |
| Slice 4 | OperatorMeta — Catalog fields (prereq for Slice 9) | ✅ Complete — `60de4cd0` (fields present; Slice 9 wires consumer) | — |
| Slice 5 | ConstructMeta — Catalog fields (prereq for Slice 8) | ✅ Complete — `5251b7e7` | — |
| Slice 6 | OutcomeMeta — Catalog fields (prereq for Slice 12) | ✅ Complete — `1536d0cb` (fields present; Slice 12 wires MCP consumer) | — |
| Slice 7 | FunctionMeta — Catalog fields (prereq for Slice 11) | ✅ Complete — `b1c95512` | — |
| Slice 8 | Parser — Replace Hardcoded Sets with Catalog Lookups | ✅ Complete — `e68008d0` | BUG-004, BUG-005, BUG-019, BUG-020, BUG-027, BUG-031, BUG-044, BUG-045, BUG-054 |
| Slice 9 | Type Checker — Catalog-Derived Operator Typing | ⬜ Not started | BUG-002, BUG-003, BUG-007, BUG-009, BUG-010, BUG-028, BUG-029, BUG-038, BUG-040, BUG-046, BUG-052, BUG-053 |
| Slice 10 | Name Binder — Catalog-Derived Name Resolution | ✅ Complete — `def91dbb` | BUG-001, BUG-026, BUG-030, BUG-037 |
| Slice 11 | Proof Engine — Catalog-Derived Proof Obligations | ✅ Complete — `004e68be` | BUG-008, BUG-013, BUG-050 |
| Slice 12 | MCP DTO Audit — Sync DTOs to Catalog Growth | ⬜ Not started | BUG-011, BUG-012, BUG-016, BUG-017, BUG-018, BUG-022, BUG-023, BUG-024, BUG-032, BUG-033, BUG-034, BUG-035, BUG-036, BUG-042, BUG-043, BUG-047 |
| Slice 13 | MCP-Docs — Fix Incorrect Recovery Hints | ⬜ Not started | BUG-014, BUG-015, BUG-041 |
| Slice 14 | Test Layer — Catalog Capability Tests | ⬜ Not started | (regression coverage for all 54) |
| Slice 15 | Test Layer — Pipeline Stage Unit Tests | ⬜ Not started | (regression coverage for all 54) |

**Why Slice 3 stays active:** the live working tree already shows the `ValueModifierMeta` rename/fix surfacing in core, tests, and MCP-facing code, but the coordinated rename is not yet durably recorded on this branch and downstream sync still has open edges. Keep only this slice active in Track 2 until that closes.

---

## Slice 1: Missing Catalog Fields — Tokens Catalog

**Goal:** Add missing flags to `TokenMeta` so pipeline stages can derive classification from
metadata rather than hardcoding token identity in switches, sets, and lookahead checks.

**Files:**
- `src/Precept/Language/Token.cs` — `TokenMeta` record definition
- `src/Precept/Language/Tokens.cs` — `Tokens.GetMeta` switch (set the new fields per token)

### Fields to Add to `TokenMeta`

**`IsStateWildcard: bool = false`**

Set `true` on `TokenKind.Any`. Consumed by:
- `src/Precept/Pipeline/NameBinder.cs` — when resolving a state target reference, check
  `Tokens.GetMeta(token.Kind).IsStateWildcard` first; if `true`, produce a wildcard state
  reference (no PRE0028) instead of a named-state lookup. Fixes BUG-001 (`any` wildcard).
- `src/Precept/Pipeline/Parser.cs` — when parsing a `StateTarget` slot, accept `Any` as a
  valid state identifier using `IsStateWildcard` instead of a hardcoded kind check.

**`IsFieldBroadcast: bool = false`**

Set `true` on `TokenKind.All`. Consumed by:
- `src/Precept/Pipeline/NameBinder.cs` — when resolving a `FieldTarget`, check
  `Tokens.GetMeta(token.Kind).IsFieldBroadcast`; if `true`, emit a broadcast field
  reference (all fields in scope) rather than a named field lookup. Fixes BUG-026, BUG-037.
- `src/Precept/Pipeline/Parser.cs` — when parsing a `FieldTarget` slot in `modify` and `omit`
  constructs, accept `All` as a valid target by checking `IsFieldBroadcast`.

**`IsFunctionCallLeader: bool = false`**

Set `true` on `TokenKind.Min` and `TokenKind.Max`. Consumed by:
- `src/Precept/Pipeline/Parser.Expressions.cs` — in the null-denotation dispatch (the Pratt
  parser's prefix position handler), when the current token has `IsFunctionCallLeader = true`,
  peek at the next token: if it is `TokenKind.LeftParen`, parse as a function call (delegate
  to `ParseFunctionCall`); otherwise, report an error ("constraint keyword not valid in
  expression position"). Do NOT check `IsFunctionCallLeader` in the field-modifier parser —
  that context already handles `Min`/`Max` as constraint keywords. Fixes BUG-006, BUG-051.

**Expand `IsValidAsMemberName` to cover type-keyword accessors**

`IsValidAsMemberName` already exists and is set on `TokenKind.Min` and `TokenKind.Max`.
The parser has a `KeywordsValidAsMemberName` derived set that must be consistent with this flag.
Add `IsValidAsMemberName = true` to the following tokens in `Tokens.GetMeta`:

| Token | Accessor use |
|-------|-------------|
| `TokenKind.CurrencyType` | `money.currency`, `price.currency` |
| `TokenKind.DateType` | `datetime.date`, `zoneddatetime.date` |
| `TokenKind.TimeType` | `datetime.time`, `zoneddatetime.time` |
| `TokenKind.InstantType` | `zoneddatetime.instant` |
| `TokenKind.TimezoneType` | `zoneddatetime.timezone` |
| `TokenKind.DateTimeType` | `zoneddatetime.datetime` |
| `TokenKind.DimensionType` | `quantity.dimension`, `period.dimension` |
| `TokenKind.From` | `exchangerate.from` |
| `TokenKind.To` | `exchangerate.to` |
| `TokenKind.At` (if it exists as a token) | `list.at(N)` |

The parser's member-access production in `Parser.Expressions.cs` must derive its valid-keyword
set from `Tokens.All.Where(m => m.IsValidAsMemberName)` rather than a hardcoded
`FrozenSet<TokenKind>`. Fixes BUG-025, BUG-039.

### Tests Required (under `test/Precept.Tests/`)

- `CatalogCapability/TokenCatalogTests.cs` — new class:
  - `Any_IsStateWildcard_True()` — assert `Tokens.GetMeta(TokenKind.Any).IsStateWildcard`
  - `All_IsFieldBroadcast_True()` — assert `Tokens.GetMeta(TokenKind.All).IsFieldBroadcast`
  - `Min_IsFunctionCallLeader_True()` — assert `Tokens.GetMeta(TokenKind.Min).IsFunctionCallLeader`
  - `Max_IsFunctionCallLeader_True()` — assert `Tokens.GetMeta(TokenKind.Max).IsFunctionCallLeader`
  - `TypeKeywords_IsValidAsMemberName_True()` — parameterized fact for all 9 tokens above
- Parser derivation coverage is executed in Slice 8, which owns the `Parser.Expressions.cs`
  rewire:
  - `KeywordsValidAsMemberName_DerivedFromCatalog()` — assert `Parser.KeywordsValidAsMemberName`
    contains exactly the tokens with `IsValidAsMemberName = true`

### Bugs Closed

BUG-001, BUG-006, BUG-025, BUG-026, BUG-037, BUG-039, BUG-051
(catalog prerequisite completes here; parser/name-binder fixes land in Slices 8 and 10).

### Closure Notes (2026-05-10)

**Field names as implemented:** Frank's design review (t2-1) locked the final field names.
`IsBroadcastFieldTarget` and `IsAlsoBuiltinFunction` were working names in this plan;
the implemented and committed names are `IsFieldBroadcast` and `IsFunctionCallLeader`.
Both forwarding aliases were removed from `Token.cs` as a parallel-copy smell.

**BUG-039 — `list.at(N)` proof obligation:** The original keyword-collision parsing bug is fixed.
`at` is now valid as a member-name token (`IsValidAsMemberName = true`). The PRE0063 diagnostic
that fires without `notempty` is **correct enforcement** — the proof engine correctly requires
`notempty` for all element-returning accessors. Two spec gaps were also fixed in `a4cc3927`:
the Proof column in the accessor table was blank for all element-returning accessor rows
(min, max, peek, peekby, first, last, at), and the `notempty` discharge note omitted `.at`
and `.peekby`. Both now document the `count > 0` requirement.

**BUG-006 / BUG-051 — `min(A,B)` / `max(A,B)` PRE0009:** Root cause was a stale extension
build. George's `IsFunctionCallLeader` fix in `Parser.Expressions.cs` is correct and passes
all unit tests. The live editor failure was because the DLL predated the fix commit by ~28
minutes. Resolution: Ctrl+Shift+B to rebuild. No code changes required.

---

## Slice 2: Actions Catalog Audit — Parser Prerequisite

**Goal:** Ensure `ActionMeta.SyntaxShape` is the single source of truth for how the action
parser parses each action form. The `ActionSyntaxShape` enum already defines the correct shapes
(`CollectionValueBy`, `InsertAt`, `RemoveAtIndex`) — the problem is the parser ignores them
and hardcodes grammar forms by `ActionKind`.

**Files:**
- `src/Precept/Language/Actions.cs` — no new fields needed; verify existing shapes are correct
- `src/Precept/Pipeline/Parser.cs` — the action-chain parser must switch on `ActionMeta.SyntaxShape`

**Phase A scope note:** this slice delivers the verified catalog baseline for action syntax.
Audit every `ActionMeta.SyntaxShape` assignment in `Actions.cs`, correct any drift there, and
leave all parser consumption work in Slice 8, which owns `Parser.cs`.

### Verification of Existing `ActionSyntaxShape` Assignments

Confirm that the following `ActionMeta` entries have the correct shapes (they should — the issue
is the parser not reading them):

| ActionKind | Expected SyntaxShape | Parses suffix |
|------------|---------------------|---------------|
| `AppendBy` | `CollectionValueBy` | `Field Expr by Expr` |
| `EnqueueBy` | `CollectionValueBy` | `Field Expr by Expr` |
| `DequeueBy` | `CollectionIntoBy` | `Field (into Field)? (by Expr)?` |
| `Insert` | `InsertAt` | `Field Expr at Expr` |
| `RemoveAt` | `RemoveAtIndex` | `Field at Expr` |

### Parser Change Required

In `Parser.cs`, the action-chain parser must dispatch by `ActionMeta.SyntaxShape` rather than
by `ActionKind`. The current code has a form like:

```csharp
// WRONG — hardcoded by kind:
case ActionKind.AppendBy:
    // parse "Field Expr" — MISSING the "by Expr" suffix
```

Replace with shape-driven dispatch:

```csharp
// CORRECT — shape-driven from catalog:
var shape = actionMeta.SyntaxShape;
switch (shape)
{
    case ActionSyntaxShape.CollectionValueBy:
        // parse "Field Expr by Expr" — by is required
        var field = ExpectIdentifier();
        var value = ParseExpression();
        Expect(TokenKind.By);
        var key   = ParseExpression();
        return new ParsedActionBy(actionMeta, field, value, key, span);

    case ActionSyntaxShape.InsertAt:
        // parse "Field Expr at Expr"
        var field = ExpectIdentifier();
        var value = ParseExpression();
        Expect(TokenKind.At);
        var index = ParseExpression();
        return new ParsedActionAt(actionMeta, field, value, index, span);

    case ActionSyntaxShape.RemoveAtIndex:
        // parse "Field at Expr"
        var field = ExpectIdentifier();
        Expect(TokenKind.At);
        var index = ParseExpression();
        return new ParsedActionAt(actionMeta, field, null, index, span);
    // ... other shapes
}
```

The `by` and `at` tokens must be parsed as structural keywords within the action grammar, NOT
consumed by the expression parser. The action parser reads them explicitly after `ParseExpression()`
terminates (since `by`/`at` are not expression-level infix operators in this context).

### Tests Required

- `CatalogCapability/ActionCatalogTests.cs`:
  - `AppendBy_SyntaxShape_IsCollectionValueBy()`
  - `EnqueueBy_SyntaxShape_IsCollectionValueBy()`
  - `DequeueBy_SyntaxShape_IsCollectionIntoBy()`
  - `Insert_SyntaxShape_IsInsertAt()`
  - `RemoveAt_SyntaxShape_IsRemoveAtIndex()`
- Parser integration coverage is executed in Slice 8, which owns the action parser rewrite:
  - `Parser_AppendBy_CompilesClean()` — uses repro from BUG-021a
  - `Parser_EnqueueBy_CompilesClean()` — uses repro from BUG-021b
  - `Parser_DequeueIntoBy_CompilesClean()` — uses repro from BUG-021c
  - `Parser_InsertAt_CompilesClean()` — uses repro from BUG-049
  - `Parser_RemoveAt_CompilesClean()` — uses repro from BUG-049

### Bugs Closed

BUG-021, BUG-048, BUG-049b closed by Slice D (`a65c9fed`). BUG-049a is a proof engine issue
tracked separately in Slice 2E below.
(catalog baseline completes here; parser fixes land in Slice 8).

---

## Slice 2E: Proof Engine — `FixedReturnAccessor.ReturnNonnegative` Early Exit (BUG-049a)

**Status:** ✅ Complete — `f2d1dece`

**Goal:** Fix spurious PRE0084 (`'<field>' can be negative`) firing on `insert F Val at Idx`
when the target field is a plain `list of string` with no modifiers.

**Design:** Frank-approved (2026-05-10). Two mandatory requirements: B1 (unify duplicate
`CollectionCount` instances) and B2 (update `proof-engine.md` Strategy 2 docs).

**Prerequisites:** None — all required types (`FixedReturnAccessor`, `Types.CollectionCountAccessor`,
`TryDeclarationAttributeProof`) already exist.

**Files:**
- `src/Precept/Language/Type.cs` — `FixedReturnAccessor` record definition
- `src/Precept/Language/Types.cs` — `CollectionCountAccessor` (make `internal`, set flag)
- `src/Precept/Language/Actions.cs` — delete `CollectionCount` local field (B1: de-dupe)
- `src/Precept/Pipeline/ProofEngine.cs` — `TryDeclarationAttributeProof` early exit
- `docs/compiler/proof-engine.md` — Strategy 2 subsection (B2)

### Root Cause

`ActionKind.Insert` has a `NumericProofRequirement(SelfSubject(CollectionCount), >=, 0m, ...)`
obligation that fires to prove the index is within bounds. `CreateActionProofSite` (George-8)
returns `TypedFieldRef("Steps")` for any `SelfSubject`. `TryDeclarationAttributeProof` resolves
to the `Steps` field — a plain `list of string` with no modifiers — and none of the 5 discharge
strategies can prove `count >= 0`. PRE0084 fires. The requirement is always trivially true
(collection count can never be negative); the engine has no mechanism to know this.

### Changes Required

**1. `FixedReturnAccessor.ReturnNonnegative` (B1: unify + add flag)**

In `Type.cs`, add `bool ReturnNonnegative = false` to the `FixedReturnAccessor` positional
record:

```csharp
public sealed record FixedReturnAccessor(
    string Name,
    TypeKind ReturnType,
    string Description,
    bool ReturnNonnegative = false   // ← new
) : TypeAccessor(Name, ReturnType, Description);
```

Precedent: `FunctionOverload.ReturnNonnegative` already exists in `Function.cs` for `abs()`.

**2. `Types.CollectionCountAccessor` — make `internal`, set `ReturnNonnegative: true` (B1)**

In `Types.cs`, change the existing private `CollectionCountAccessor` field:

```csharp
// BEFORE:
private static readonly FixedReturnAccessor CollectionCountAccessor =
    new("count", TypeKind.Integer, "Number of elements");

// AFTER:
internal static readonly FixedReturnAccessor CollectionCountAccessor =
    new("count", TypeKind.Integer, "Number of elements", ReturnNonnegative: true);
```

**3. Delete `Actions.CollectionCount`, use `Types.CollectionCountAccessor` (B1)**

In `Actions.cs`, delete the local copy:
```csharp
// DELETE:
private static readonly FixedReturnAccessor CollectionCount =
    new FixedReturnAccessor("count", TypeKind.Integer, "Number of elements");
```

In `ActionKind.Insert`'s `NumericProofRequirement`, replace `CollectionCount` with
`Types.CollectionCountAccessor`.

**4. Early exit in `TryDeclarationAttributeProof` (proof engine fix)**

In `ProofEngine.cs`, add an early exit at the top of `TryDeclarationAttributeProof`, before
the modifier loop:

```csharp
// Accessor-level nonnegative guarantee: if the requirement's accessor is statically
// known to return a nonnegative value (e.g. collection count), discharge >= 0 trivially.
if (reqSubject is SelfSubject { Accessor: FixedReturnAccessor { ReturnNonnegative: true } }
    && obligation.Requirement is NumericProofRequirement {
        Comparison: OperatorKind.GreaterThanOrEqual,
        Threshold: 0m })
    return true;
```

This is Strategy 2 (accessor-level nonnegative guarantee). Placement: before the modifier loop,
after the field lookup. The pattern mirrors `FunctionReturnSatisfies` logic but applies to
accessor metadata rather than function overload metadata.

**5. Update `docs/compiler/proof-engine.md` Strategy 2 (B2)**

In the Strategy 2 subsection of `proof-engine.md`, document both discharge paths as a coherent
subsection:

- **`FunctionReturnSatisfies`:** when the proof site is a `TypedFunctionCall` and the resolved
  overload has `ReturnNonnegative = true`, the `>= 0` obligation is discharged.
- **`FixedReturnAccessor.ReturnNonnegative`:** when the proof site's `SelfSubject` accessor has
  `ReturnNonnegative = true` (e.g. `CollectionCountAccessor`) and the obligation is `>= 0`,
  the obligation is discharged trivially. This handles `insert`/`insert-at` proof requirements
  on collection fields.

Also update the `FixedReturnAccessor` description in the doc to mention `ReturnNonnegative`.

### Tests Required

- `ProofEngineTests/ActionProofTests.cs` (new or existing):
  - `Insert_PlainListField_NoModifiers_CompilesClean()` — regression for BUG-049a; `list of string` field + `insert F Val at N` → zero diagnostics
  - `Insert_WithNotemptyField_CompilesClean()` — confirm `notempty` still works (no regression)
  - `Insert_NonnegativeCountDischarge_IsEarly()` — confirm PRE0084 does not fire even without any modifiers

### Bugs Closed

BUG-049a — fixed in `f2d1dece`

---

## Slice 3: Missing Catalog Fields — Modifiers Catalog

**Goal:** Add metadata to `ModifierMeta` that pipeline stages need to enforce modifier rules
without hardcoded per-modifier checks.

**Files:**
- `src/Precept/Language/Modifiers.cs` — `ModifierMeta` record subtypes; add new fields to
  `ValueModifierMeta`

### Fields to Add to `ValueModifierMeta`

**Declaration-site applicability metadata**

Most value modifiers are valid on more than one declaration surface. Rather than a narrow
event-arg boolean, model declaration-site applicability on `ValueModifierMeta` so parser and
checker can ask whether a modifier is legal on a given declaration site. Event arguments are one
consumer of that metadata; field declarations are another. The current grammar still spells the
slot `FieldModifier*`, but the canonical metadata surface for this Track 2 work is
`ValueModifierMeta`.

For BUG-004, the event-argument site must exclude `writable` and admit the rest of the value-
modifier family, including valued modifiers such as `default`, `min`, `max`, `minlength`, and
`maxcount`.

Consumed by:
- `src/Precept/Pipeline/Parser.cs` — `ParseArgDecl` reads the declaration-site applicability
  metadata from `Modifiers.ByValueToken[currentKind]`; if the event-argument site is excluded,
  emit PRE0009 "The '{modifier}' modifier is not valid on event arg declarations."
  Currently, `ParseArgDecl` either does not call the field-modifier parser at all, or doesn't
  handle the `default` modifier token sequence (`default Expr`). Both must be fixed. Fixes BUG-004.

**`BoundCounterpart: ModifierKind?` (new optional field)**

Identifies the paired bound modifier that forms an ordered pair: `Min`/`Max`, `Minlength`/`Maxlength`,
`Mincount`/`Maxcount`. The type checker uses this to enforce `InvalidModifierBounds`.

Set in `Modifiers.GetMeta`:
- `ModifierKind.Min` → `BoundCounterpart = ModifierKind.Max`
- `ModifierKind.Max` → `BoundCounterpart = ModifierKind.Min`
- `ModifierKind.Minlength` → `BoundCounterpart = ModifierKind.Maxlength`
- `ModifierKind.Maxlength` → `BoundCounterpart = ModifierKind.Minlength`
- `ModifierKind.Mincount` → `BoundCounterpart = ModifierKind.Maxcount`
- `ModifierKind.Maxcount` → `BoundCounterpart = ModifierKind.Mincount`
- All others → `BoundCounterpart = null`

Consumed by:
- `src/Precept/Pipeline/TypeChecker.Validation.cs` — after collecting all `ValueModifierMeta`
  entries on a field, for each modifier `m` where `m.BoundCounterpart != null`, find the
  counterpart in the collected set; if both are present and the lower bound value exceeds the
  upper bound value, emit `InvalidModifierBounds`. Do not hardcode the min/max/minlength pairs —
  iterate `field.Modifiers` and derive the check from `BoundCounterpart`. Fixes BUG-029, BUG-038.

### Tests Required

- `CatalogCapability/ModifierCatalogTests.cs`:
  - `Writable_ExcludesEventArgumentDeclarations()`
  - `Default_IncludesEventArgumentDeclarations()`
  - `Min_BoundCounterpart_IsMax()`
  - `Max_BoundCounterpart_IsMin()`
  - `Minlength_BoundCounterpart_IsMaxlength()`
  - `Mincount_BoundCounterpart_IsMaxcount()`
- Parser coverage is executed in Slice 8:
  - `Parser_EventArg_Default_CompilesClean()` — repro from BUG-004
- Type-checker coverage is executed in Slice 9:
  - `TypeChecker_InvalidModifierBounds_MinExceedsMax()` — repro from BUG-029
  - `TypeChecker_InvalidModifierBounds_MinlengthExceedsMaxlength()` — BUG-038 variant
  - `TypeChecker_InvalidModifierBounds_MincountExceedsMaxcount()` — BUG-038 variant

### Bugs Closed

BUG-004, BUG-029, BUG-038
(catalog prerequisite completes here; parser/type-checker fixes land in Slices 8 and 9).

---

## Slice 4: Missing Catalog Fields — Operators Catalog

**Goal:** Add result-type metadata to `OperatorMeta` so the type checker can derive result
types from the catalog instead of a hardcoded switch. This is the most impactful single
catalog gap: the type checker currently has no catalog field to read for operator result types.

**Files:**
- `src/Precept/Language/Operators.cs` — `OperatorMeta` base record and subtypes; add new fields
- `src/Precept/Language/Operators.cs` — define the new `ResultTypePolicy` enum adjacent to `OperatorMeta`

### Fields to Add to `OperatorMeta`

**`ResultType: TypeKind? = null`**

For operators whose result type is declared directly in the catalog.

Set in `Operators.GetMeta`:
- `OperatorKind.Or`, `OperatorKind.And` → `ResultType = TypeKind.Boolean`
- `OperatorKind.Not` → `ResultType = TypeKind.Boolean`
- `OperatorKind.Equals`, `NotEquals`, `CaseInsensitiveEquals`, `CaseInsensitiveNotEquals` → `ResultType = TypeKind.Boolean`
- `OperatorKind.LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual` → `ResultType = TypeKind.Boolean`
- `OperatorKind.Contains` → `ResultType = TypeKind.Boolean`
- `OperatorKind.IsSet`, `IsNotSet` → `ResultType = TypeKind.Boolean`
- Unary/binary arithmetic and `LookupAccess` → `ResultType = null` (derived)

**`ResultTypePolicy: ResultTypePolicy`**

Enum that tells the type checker how to derive the result type.

```csharp
public enum ResultTypePolicy
{
    Fixed,           // use ResultType directly
    LhsType,         // result is the resolved left/only operand type
    ElementType,     // result is the left operand's element/value type
    BothOperands,    // operands must agree on ResultType; result is that type
    OperationResult, // result comes from the resolved Operations catalog entry
}
```

Set in `Operators.GetMeta`:
- `OperatorKind.Or`, `OperatorKind.And` → `ResultTypePolicy = ResultTypePolicy.BothOperands`
- Logical unary `not`, comparison, membership, and presence operators → `ResultTypePolicy = ResultTypePolicy.Fixed`
- `OperatorKind.LookupAccess` → `ResultTypePolicy = ResultTypePolicy.ElementType`
- Binary arithmetic operators → `ResultTypePolicy = ResultTypePolicy.OperationResult`
- `OperatorKind.Negate` → `ResultTypePolicy = ResultTypePolicy.LhsType`

### Consumed by

- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — `ResolveOperatorResultType(op, lhsType, rhsType, resolvedOperation)`:
  ```csharp
  var meta = Operators.ByToken[(op.Kind, op.Arity)];
  return meta.ResultTypePolicy switch
  {
      ResultTypePolicy.Fixed => meta.ResultType!.Value,
      ResultTypePolicy.BothOperands => meta.ResultType!.Value,
      ResultTypePolicy.LhsType => lhsType,
      ResultTypePolicy.ElementType => ResolveElementType(lhsType),
      ResultTypePolicy.OperationResult => resolvedOperation.Result,
      _ => TypeKind.Unknown,
  };
  ```

  This replaces the current hardcoded switch that gets `and`/`or`/`not`/`contains`/`for` wrong.

### Tests Required

- `CatalogCapability/OperatorCatalogTests.cs`:
  - `BooleanOperators_DeclareBooleanResultTypes()`
  - `LookupAccess_ResultTypePolicy_IsElementType()`
  - `BinaryArithmeticOperators_UseOperationResultPolicy()`
  - `Negate_ResultTypePolicy_IsLhsType()`
  - `FixedAndBothOperandsPolicies_DeclareResultTypes()`
  - `DerivedPolicies_LeaveResultTypeNull()`
- Type-checker integration coverage is executed in Slice 9:
  - `TypeChecker_And_ResultType_IsBoolean()` — repro from BUG-003/053
  - `TypeChecker_Or_ResultType_IsBoolean()` — repro from BUG-003/053
  - `TypeChecker_Contains_ResultType_IsBoolean()` — repro from BUG-002/052
  - `TypeChecker_For_ResultType_IsValueType()` — repro from BUG-009

### Bugs Closed

BUG-002, BUG-003, BUG-009, BUG-052, BUG-053 (prerequisite only — Slice 9 completes)

---

## Slice 5: Missing Catalog Fields — Constructs Catalog

**Goal:** Encode optional pre-verb `when` guards directly in the ordered `ConstructMeta.Slots`
list for each guarded scoped construct. The parser should walk the slot list in order; no
separate guard-placement flag is needed.

**Files:**
- `src/Precept/Language/Construct.cs` — remove the obsolete guard-placement boolean from `ConstructMeta`
- `src/Precept/Language/Constructs.cs` — add per-construct guard slots and update slot lists

### Slot-list shape for guarded scoped constructs

Use optional `GuardClause` slots at the natural pre-verb position:
- `StateEnsure` — `[StateTarget, GuardClause(term: Ensure), EnsureClause, BecauseClause?]`
- `StateAction` — `[StateTarget, GuardClause(term: Arrow), ActionChain]`
- `EventEnsure` — `[EventTarget, GuardClause(term: Ensure), EnsureClause, BecauseClause?]`
- `AccessMode` — `[StateTarget, GuardClause(term: Modify), FieldTarget, AccessModeKeyword]`

Also: stateless `EventHandler` supports a post-action `ensure` clause per spec (BUG-054):
- `EventHandler` — `on EventTarget -> actions... ensure BoolExpr because StringExpr`

**`SupportsPostActionEnsure: bool = false`**

True for `EventHandler` (stateless handler) which supports a trailing `ensure BoolExpr because StringExpr`
after the action chain. This remains separate because the grammar position is post-action rather
than pre-verb. Fixes BUG-054.

### Consumed by

- `src/Precept/Pipeline/Parser.cs` — `ParseScopedConstruct` should walk the slot list in order and
  consume the disambiguation keyword at the natural boundary. Optional `when BoolExpr` parsing for
  state ensures, state actions, event ensures, and access modes falls out of the slot list; in
  `ParseEventHandler`, after parsing the action chain, check `constructMeta.SupportsPostActionEnsure`
  and optionally parse `ensure BoolExpr because StringExpr`.

### Tests Required

- `CatalogCapability/ConstructCatalogTests.cs`:
  - `StateEnsure_HasPreVerbGuardSlot()`
  - `StateAction_HasPreVerbGuardSlot()`
  - `EventEnsure_HasPreVerbGuardSlot()`
  - `AccessMode_HasPreVerbGuardSlot()`
  - `EventHandler_SupportsPostActionEnsure_True()`
- Parser integration coverage is executed in Slice 8:
  - `Parser_GuardedStateEnsure_CompilesClean()` — repro from BUG-020
  - `Parser_GuardedStateAction_CompilesClean()` — repro from BUG-044
  - `Parser_StatelessHookEnsure_CompilesClean()` — repro from BUG-054

### Bugs Closed

BUG-020, BUG-044, BUG-054 (prerequisite only — Slice 8 completes)

---

## Slice 6: Missing Catalog Fields — Outcomes Catalog

**Goal:** Add a `SerializedKind` field to `OutcomeMeta` so the MCP DTO layer can produce
a canonical string identifying each outcome kind without hardcoding.

**Files:**
- `src/Precept/Language/Outcomes.cs` — `OutcomeMeta` record; add `SerializedKind` field

### Field to Add to `OutcomeMeta`

**`SerializedKind: string`**

The string emitted in the MCP definition DTO `outcome` field of a transition row. Set in
`Outcomes.GetMeta`:

- `OutcomeKind.Transition` → `SerializedKind = "transition"`
- `OutcomeKind.NoTransition` → `SerializedKind = "no transition"`
- `OutcomeKind.Reject` → `SerializedKind = "reject"`

Consumed by:
- `tools/Precept.Mcp/Tools/CompileTool.cs` — `MapTransitionRow` must emit `Outcome = row.OutcomeKind has a value ? Outcomes.GetMeta(row.OutcomeKind).SerializedKind : null`
  (the `TransitionRowDto` needs a new `Outcome` field and `RejectMessage` field — see Slice 12).

### Tests Required

- `CatalogCapability/OutcomeCatalogTests.cs`:
  - `Transition_SerializedKind_IsTransition()`
  - `NoTransition_SerializedKind_IsNoTransition()`
  - `Reject_SerializedKind_IsReject()`
  - `AllOutcomes_SerializedKind_Distinct()` — assert no duplicates

### Bugs Closed

BUG-032, BUG-036 (prerequisite only — Slice 12 completes)

---

## Slice 7: Missing Catalog Fields — Functions Catalog

**Goal:** Add a `ReturnValueProofFacts` field to `FunctionMeta` so the proof engine can learn
that certain functions always return values with known numeric properties (specifically: `abs()`
always returns ≥ 0, enabling `sqrt(abs(x))` to compile clean).

**Files:**
- `src/Precept/Language/Functions.cs` — `FunctionMeta` record and `FunctionOverload` — add field;
  update `FunctionKind.Abs` entry

### Field to Add to `FunctionOverload`

**`ReturnNonnegative: bool = false`** (on `FunctionOverload`, not `FunctionMeta`, because it
can vary per overload)

When `true`, the proof engine records that the result of this overload is always ≥ 0. Set to
`true` on ALL overloads of `FunctionKind.Abs` (since `abs` always returns a non-negative value
regardless of operand type). This is the minimum change to fix BUG-013.

Consumed by:
- `src/Precept/Pipeline/ProofEngine.cs` — when building the proof ledger entry for a function
  call node, if the resolved overload has `ReturnNonnegative = true`, record a proof fact:
  `ValueIsNonnegative(resultVariable)`. This proof fact satisfies `SqrtOfNegative` obligations
  downstream. Fixes BUG-013.

### Tests Required

- `CatalogCapability/FunctionCatalogTests.cs`:
  - `Abs_AllOverloads_ReturnNonnegative_True()`
  - `Sqrt_Overload_ReturnNonnegative_False()` (negative test — sqrt can be negative input)
- Proof-engine integration coverage is executed in Slice 11:
  - `ProofEngine_Sqrt_Of_Abs_CompilesClean()` — repro from BUG-013

### Bugs Closed

BUG-013 (prerequisite only — Slice 11 completes)

---

## Slice 8: Parser — Replace Hardcoded Token Sets with Catalog Lookups

**Goal:** The parser must derive ALL token classification from catalog metadata. No hardcoded
token sets, no hardcoded grammar forms for actions, no hardcoded lookahead checks that ignore
existing catalog fields.

**Files:**
- `src/Precept/Pipeline/Parser.cs`
- `src/Precept/Pipeline/Parser.Expressions.cs`

**Prerequisites:** Slices 1, 2, 3, 5 must be complete.

### Changes Required

**1. Member Access: Read `IsValidAsMemberName` (BUG-025, BUG-039)**

In `Parser.Expressions.cs`, the member-access parser's inner identifier expect:
```csharp
// CURRENT (hardcoded set):
private static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName = [
    TokenKind.Min, TokenKind.Max,
];

// CORRECT (catalog-derived):
public static readonly FrozenSet<TokenKind> KeywordsValidAsMemberName =
    Tokens.All.Where(m => m.IsValidAsMemberName).Select(m => m.Kind).ToFrozenSet();
```

In the member-access dot-parsing, after consuming `.`, if the next token is a keyword, check
`KeywordsValidAsMemberName`; if present, accept it as a member name. Ensure `TokenKind.At` is
in this set once BUG-039's `IsValidAsMemberName = true` is added.

**2. State Target: Read `IsStateWildcard` (BUG-001)**

In `Parser.cs`, `ParseStateTarget()`:
```csharp
// After: var meta = Tokens.GetMeta(Current.Kind);
if (meta.IsStateWildcard)
    return new ParsedStateTarget.Wildcard(Consume().Span);
```
Do not hardcode `TokenKind.Any` here — read `IsStateWildcard` from the catalog.

**3. Field Target: Read `IsBroadcastFieldTarget` (BUG-026, BUG-037)**

In `Parser.cs`, `ParseFieldTarget()`:
```csharp
if (Tokens.GetMeta(Current.Kind).IsBroadcastFieldTarget)
    return new ParsedFieldTarget.Broadcast(Consume().Span);
```
Then parse the comma-separated identifier list for the non-broadcast case (BUG-005).

**4. Comma-Separated Field List (BUG-005)**

In `ParseFieldTarget()`, after consuming the first identifier, add:
```csharp
while (Current.Kind == TokenKind.Comma)
{
    Consume(); // comma
    names.Add(ExpectIdentifier());
}
```
The grammar is `identifier ("," identifier)*` — parse the `*` part.

**5. Built-in Function Dispatch: Read `IsAlsoBuiltinFunction` (BUG-006, BUG-051)**

In `Parser.Expressions.cs`, null-denotation switch (prefix position):
```csharp
// For tokens with IsAlsoBuiltinFunction = true:
var meta = Tokens.GetMeta(Current.Kind);
if (meta.IsAlsoBuiltinFunction && Peek(1).Kind == TokenKind.LeftParen)
{
    Consume(); // min or max
    return ParseFunctionCallArguments(meta.Text!); // delegate to existing function-call parser
}
// else fall through to error
```

**6. `default` on Event Args: Read declaration-site applicability metadata (BUG-004)**

In `Parser.cs`, `ParseArgDecl()`: invoke the field-modifier parser loop (same as `ParseFieldDeclaration`)
but gate each candidate modifier token through the declaration-site applicability metadata on
`Modifiers.ByValueToken[kind]`. The modifier loop must handle the `default Expr` two-token form
(consume `default`, then parse a value expression).

**7. `choice of T(...)` in event args (BUG-027)**

In `ParseArgDecl()`, when parsing the type reference, permit `ChoiceType` by delegating to the
same `ParseTypeReference()` used by field declarations. If the arg-type parser uses a restricted
subset that excludes collection/choice types, replace it with the full `ParseTypeReference()`.

**8. Guarded Ensures and State Actions (BUG-020, BUG-044)**

In `ParseScopedConstruct()` for `StateEnsure`, `StateAction`, `EventEnsure`, and `AccessMode`,
walk the declared slot list in order. The optional `GuardClause` slot appears before the
verb-bearing slot, so `when BoolExpr` parses before `ensure`, `->`, or `modify` without a
special-case flag. Store the guard in the declared `GuardClause` slot.

**9. Action-Chain Suffix Grammar: Read `ActionSyntaxShape` (BUG-021, BUG-048, BUG-049)**

Replace the hardcoded action-body parser with shape-driven dispatch (see Slice 2 design).
The `by` and `at` tokens must be consumed as structural keywords explicitly — NOT parsed as
infix operators inside `ParseExpression()`. The `ActionSyntaxShape.CollectionValueBy` case
consumes `Field`, `ParseExpression()`, `by`, `ParseExpression()`. The `InsertAt` case consumes
`Field`, `ParseExpression()`, `at`, `ParseExpression()`. The `RemoveAtIndex` case consumes
`Field`, `at`, `ParseExpression()`.

**10. Ascending/Descending Log Modifiers (BUG-045)**

In `ParseTypeReference()`, after parsing `log of T by P`, optionally consume
`TokenKind.Ascending` or `TokenKind.Descending` and record the ordering modifier. These tokens
are already reserved (per spec keyword table) — the parser simply doesn't handle them in type
position. No catalog field change needed; the token kinds exist.

**11. String Interpolation in `reject` / `because` (BUG-031)**

In `ParseOutcome()` for `reject` and in `ParseBecauseClause()`: instead of calling `Expect(TokenKind.StringLiteral)`,
call a `ParseStringExpression()` helper that handles both `StringLiteral` (plain string) and
`StringStart`/`Identifier`/`StringMiddle`/`StringEnd` sequences (interpolated string). This
helper should already exist or parallel what the expression parser does for string interpolation.

**12. Computed Field Forward References (BUG-030)**

In `src/Precept/Pipeline/NameBinder.cs` (not parser, but related): the name binder must
perform a topological sort of field declarations before binding computed expressions. Fields
with `<-` computed expressions must be sorted by dependency order, not declaration order.
Forward references are valid if they form no cycle. Cycles emit `CircularComputedField`;
forward references that resolve after sorting do not emit PRE0017.

Also fix the error message: PRE0054's message template for computed field forward references
must say "Computed expression" not "Default value".

**13. Typed Constants in Context Positions (BUG-019)**

In `Parser.Expressions.cs`, null-denotation for `TypedConstant` / `TypedConstantStart`: accept
these tokens as valid expression atoms (they are already lexed correctly). The parser should
produce a `ParsedTypedConstant` node. The type checker then resolves the constant's type from
the surrounding expression context (the `TypedConstantValidation.Validate(...)` call with the
expected type). Currently the parser either rejects `TypedConstant` in expression position or
the type checker doesn't propagate context to the validator.

**14. Stateless Hook Post-Action Ensure (BUG-054)**

In `ParseEventHandler()` (stateless `on Event -> actions` form): after parsing the action
chain, check `constructMeta.SupportsPostActionEnsure`; if `true`, optionally parse
`ensure BoolExpr because StringExpr`.

### Tests Required

One compiler-integration test per bullet above (compile the minimal repro from the bug report
and assert `hasErrors: false` and no diagnostics). Test class: `test/Precept.Tests/ParserTests/`.

### Bugs Closed

BUG-001 (shared with Slice 10), BUG-004, BUG-005, BUG-006, BUG-019, BUG-020, BUG-021,
BUG-025, BUG-026, BUG-027, BUG-030 (partial — Slice 10 owns NameBinder part), BUG-031,
BUG-037, BUG-039, BUG-044, BUG-045, BUG-048, BUG-049, BUG-051, BUG-054

---

## Slice 9: Type Checker — Catalog-Derived Operator Typing

**Goal:** The type checker must derive operator result types from `OperatorMeta.ResultType`
and `OperatorMeta.ResultTypePolicy` rather than a hardcoded switch. Fix all operator-related
type-check errors and modifier-validation bugs.

**Files:**
- `src/Precept/Pipeline/TypeChecker.Expressions.cs`
- `src/Precept/Pipeline/TypeChecker.Validation.cs`

**Prerequisites:** Slice 4 (OperatorMeta fields), Slice 3 (ModifierMeta.BoundCounterpart).

### Changes Required

**1. Operator Result Type Resolution (BUG-002, BUG-003, BUG-009, BUG-052, BUG-053)**

In `TypeChecker.Expressions.cs`, the `ResolveExpression` method's operator branch:
```csharp
case ParsedBinaryExpression binary:
    var lhs = ResolveExpression(binary.Left);
    var rhs = ResolveExpression(binary.Right);
    var opMeta = Operators.ByToken[(binary.Operator, Arity.Binary)];
    TypeKind resultType;
    resultType = opMeta.ResultTypePolicy switch {
        ResultTypePolicy.Fixed => opMeta.ResultType!.Value,
        ResultTypePolicy.BothOperands => opMeta.ResultType!.Value,
        ResultTypePolicy.LhsType => lhs.ResolvedType,
        ResultTypePolicy.ElementType => ResolveElementType(lhs),
        ResultTypePolicy.OperationResult => resolvedOperation.Result,
        _ => TypeKind.Unknown,
    };
    return new TypedBinaryExpression(binary.Span, lhs, binary.Operator, rhs, resultType);
```

This replaces the current switch-on-OperatorKind that returns wrong types for
`and`/`or`/`not`/`contains`/`for`.

**2. Pratt Parser Precedence Inversion (BUG-007)**

In `Parser.Expressions.cs`, the Pratt parser's `GetLeftBindingPower` method. The spec table
(line 693) assigns arithmetic operators (precedence 50, 60) higher precedence than comparison
operators (precedence 30). Verify the binding powers in `GetLeftBindingPower` match the catalog:
```csharp
return Operators.ByToken.TryGetValue((kind, Arity.Binary), out var meta)
    ? meta.Precedence
    : 0;
```
Replace any local binding-power table with the catalog-derived precedence lookup shown above.
`Operators.GetMeta(OperatorKind.Plus).Precedence` is 50; `Operators.GetMeta(OperatorKind.Equals).Precedence`
is 30 — 50 > 30, so arithmetic binds tighter than comparison. The implementation target for this
slice is a parser that derives left-binding power from `OperatorMeta.Precedence`, not from a
handwritten precedence table.

**3. Choice Literal in Comparison Context (BUG-010)**

In `TypeChecker.Expressions.cs`, when resolving a string or integer literal where the
surrounding context expects `TypeKind.Choice` (e.g., `Priority == "High"` where `Priority`
is `choice of string`), propagate the expected type to the literal node. A string literal in
choice-comparison position should be typed as `TypeKind.Choice` and validated as a declared
choice value (the same logic that already works in assignment position).

The specific fix: in `ResolveComparisonExpression`, if `lhs.ResolvedType == TypeKind.Choice`,
resolve the `rhs` literal with an expected type of `TypeKind.Choice` before emitting a type
mismatch error.

**4. RedundantModifier Garbled Message (BUG-028)**

In `TypeChecker.Validation.cs`, the `MutuallyExclusiveWith` check for modifiers like
`Nonnegative` + `Positive`: the code incorrectly routes through `InvalidModifierForType`
(PRE0033) when it should emit only `RedundantModifier` (PRE0037). Find the condition that
dispatches to PRE0033 and fix it to check whether the modifier is actually invalid for the
field type (it isn't — `nonnegative` IS valid on `number`) vs. whether it is superseded
by another modifier (`positive` subsumes `nonnegative`). The two code paths must not be
conflated. Read `ModifierMeta.Subsumes` to determine which diagnostic to emit.

**5. InvalidModifierBounds Enforcement (BUG-029, BUG-038)**

In `TypeChecker.Validation.cs`, after resolving all modifier values on a field, iterate modifiers
where `meta.BoundCounterpart != null`, find the pair, compare values, and emit `InvalidModifierBounds`
if the lower > upper. This is catalog-driven using `ValueModifierMeta.BoundCounterpart` from Slice 3.

**6. `peekby` Accessor on QueueBy Fields (BUG-040)**

In `TypeChecker.Expressions.cs`, the member-access resolver for `TypeKind.QueueBy`: add
`peekby` to the recognized accessor set for `QueueBy` fields. The `precept_types` tool already
documents it with `proofRequirements: ["self.count > 0"]`. Add it to the type checker's QueueBy
accessor dispatch.

**7. CI Enforcement in Quantifier Binding Variables (BUG-046)**

In `TypeChecker.Expressions.cs`, when resolving a quantifier expression (`each T in CISet (pred)`),
the binding variable `T` inherits its type from the collection's element type. If the element
type is `~string` (case-insensitive string), the binding variable must also carry the CI qualifier.
When `T` is compared with `==` instead of `~=`, emit `CaseInsensitiveFieldRequiresTildeEquals`.

### Tests Required

- `TypeCheckerTests/OperatorTypingTests.cs` — one test per operator type fix
- `TypeCheckerTests/ModifierValidationTests.cs` — tests for BUG-028, BUG-029, BUG-038, BUG-040, BUG-046

### Bugs Closed

BUG-002, BUG-003, BUG-007, BUG-009, BUG-010, BUG-028, BUG-029, BUG-038, BUG-040, BUG-046,
BUG-052, BUG-053

---

## Slice 10: Name Binder — Catalog-Derived Name Resolution

**Goal:** The name binder must read `TokenMeta.IsStateWildcard` and `TokenMeta.IsFieldBroadcast`
when resolving state and field target references, and must perform topological sorting of
computed fields before binding.

**File:** `src/Precept/Pipeline/NameBinder.cs`

**Prerequisites:** Slice 1 (TokenMeta fields), Slice 8 (parser produces correct parse nodes).

### Changes Required

**1. `any` State Wildcard (BUG-001)**

The parser (after Slice 8) emits `ParsedStateTarget.Wildcard` for `any`. The name binder must
accept `Wildcard` as a valid state target without a PRE0028 lookup. If the name binder currently
pattern-matches on `ParsedStateTarget` and falls through to a named-state lookup for wildcard
nodes, add the wildcard case. The double-firing (two PRE0028 per occurrence) suggests two
resolution passes — ensure both passes recognize `Wildcard`.

**2. `all` Broadcast Field Target (BUG-026, BUG-037)**

The parser (after Slice 8) emits `ParsedFieldTarget.Broadcast` for `all`. The name binder must
resolve a `Broadcast` field target to all fields in scope for the current state — not attempt
a named-field lookup for "all".

**3. Computed Field Forward References (BUG-030)**

In the field-binding pass: before resolving computed expressions (`<-`), build a dependency
graph of which computed fields reference which other fields. Perform a topological sort. Process
fields in topological order. Fields not in a cycle are processed correctly regardless of
declaration order (forward references resolved). Fields in a cycle emit `CircularComputedField`
rather than PRE0017.

Also fix PRE0054's message to say "Computed expression" not "Default value" (both the template
in `DiagnosticCatalog` and the emission site in `NameBinder.cs`).

### Tests Required

- `NameBinderTests/StateWildcardTests.cs` — repros from BUG-001
- `NameBinderTests/BroadcastFieldTargetTests.cs` — repros from BUG-026, BUG-037
- `NameBinderTests/ComputedFieldTests.cs` — repros from BUG-030a and BUG-030b

### Bugs Closed

BUG-001, BUG-026, BUG-030, BUG-037

### Closure Notes (2026-05-10)

- `NameBinder` now derives wildcard/broadcast handling from `TokenMeta.IsStateWildcard` / `IsFieldBroadcast` through the parser's string-backed `StateTargetSlot` and `FieldTargetSlot` surfaces, so neither `any` nor `all` falls through to named-symbol lookup.
- Computed fields are bound after a declaration-order-stable topological sort. Non-cyclic forward references now bind cleanly, and cyclic sets emit `CircularComputedField` without stray undeclared/forward-reference diagnostics.
- The current architecture also performs state-target normalization in `TypeChecker.cs`; Slice 10 updates that second pass so `to any`, `in any`, and other wildcard state anchors no longer re-emit PRE0028 after name binding succeeds.

---

## Slice 11: Proof Engine — Catalog-Derived Proof Obligations

**Goal:** Fix `pop`/`dequeue` proof obligations that emit `<unknown>` and fix `sqrt(abs(x))`
by reading `FunctionMeta.ReturnNonnegative` from the Functions catalog.

**Files:**
- `src/Precept/Pipeline/ProofEngine.cs`
- `src/Precept/Pipeline/ProofLedger.cs`

**Prerequisites:** Slice 7 (FunctionMeta.ReturnNonnegative).

### Changes Required

**1. `pop`/`dequeue` Proof Obligations Use `<unknown>` (BUG-008, BUG-050)**

The proof engine generates an `UnguardedCollectionMutation` obligation for `pop` and `dequeue`
actions but binds it to an unnamed synthetic variable rather than the actual collection field
name. The `peek` accessor does this correctly (uses the named field). Trace the obligation
generation for mutation actions:

- In `ProofEngine.cs`, find where `UnguardedCollectionMutation` (or the equivalent division-by-zero
  proof obligation) is created for `pop`/`dequeue`. Compare with the `peek` accessor path.
- The `peek` path correctly passes the field name from the parsed expression. The `pop`/`dequeue`
  path likely uses a different code path that produces a synthetic subject.
- Fix: pass the collection field name from the `ParsedAction`'s field identifier to the
  obligation subject, same as `peek`. The result must be `<FieldName>.count > 0` as the
  discharge condition, not `<unknown>.count > 0`.

Also confirm: PRE0083 fires for `pop`/`dequeue` but should fire the `EmptyCollectionAccess`
obligation, not `DivisionByZero`. If these share a code path (BUG-050's false PRE0083), ensure
the obligation type matches the action context.

**2. `sqrt(abs(x))` Returns `<unknown>` (BUG-013)**

After Slice 7 adds `ReturnNonnegative` to `FunctionOverload`, in `ProofEngine.cs` when
evaluating a function call node:
```csharp
var overload = Functions.GetMeta(funcKind).ResolveOverload(argTypes);
if (overload.ReturnNonnegative)
    ledger.RecordNonnegative(resultVariable);
```

This `RecordNonnegative` entry must be recognizable by the `SqrtOfNegative` obligation checker
as sufficient proof that the argument to `sqrt` is non-negative.

### Tests Required

- `ProofEngineTests/CollectionMutationProofTests.cs`:
  - `Pop_WithCountGuard_CompilesClean()` — repro from BUG-008
  - `Dequeue_WithCountGuard_CompilesClean()` — repro from BUG-008
  - `Pop_WithNotempty_CompilesClean()` — additional variant
- `ProofEngineTests/FunctionReturnProofTests.cs`:
  - `Sqrt_Of_Abs_CompilesClean()` — repro from BUG-013

### Bugs Closed

BUG-008, BUG-013, BUG-050

---

## Slice 12: MCP DTO Audit — Sync DTOs to Catalog Growth

**Goal:** Every catalog member and semantic index type that has a user-facing representation
must have a corresponding DTO field in `CompileToolDtos.cs` and must be populated in
`CompileTool.cs`.

**Files:**
- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`
- `tools/Precept.Mcp/Tools/CompileTool.cs`

**Note:** `AccessModeDto` and `StateHookDto` already exist in `CompileToolDtos.cs` but are not
wired into `PreceptDefinitionDto` or populated in `MapState`. Most fixes in this slice are
either adding new DTO fields or connecting existing DTOs that are orphaned.

### Changes Required

**1. Add State Hook Actions to `PreceptDefinitionDto` (BUG-011, BUG-047)**

`StateHookDto` already exists. Add it to the top-level definition:
```csharp
public sealed record PreceptDefinitionDto(
    string Name,
    bool IsStateless,
    PreceptFieldDto[] Fields,
    PreceptStateDto[] States,
    PreceptEventDto[] Events,
    PreceptRuleDto[] Rules,
    StateHookDto[] StateHooks    // NEW — entry/exit hook action chains
);
```
In `CompileTool.MapDefinition`: populate `StateHooks` from `semantics.StateHooks` (or equivalent
in `SemanticIndex`). Each entry: `new StateHookDto(stateName, kind: "entry"|"exit", actions)`.

**2. Stateless Event Handler Rows (BUG-012, BUG-047)**

`MapEvent` currently filters `semantics.TransitionRows` by event name. Stateless precept handlers
(`on Event -> actions`) are stored differently from transition rows (they are `EventHandler`
constructs, not `TransitionRow` constructs). Add a parallel lookup:
```csharp
// In MapEvent:
var handlerRows = semantics.EventHandlers  // or EventHookRows or equivalent
    .Where(h => h.EventName == @event.Name)
    .Select(h => MapHandlerRow(h, source));
```
These appear in `Rows` alongside or instead of transition rows for stateless precepts.

**3. Rule `when` Guard (BUG-016)**

Add `When` to `PreceptRuleDto`:
```csharp
public sealed record PreceptRuleDto(
    string Expression,
    string? Because,
    string? When    // NEW
);
```
In `CompileTool.MapRule`: `When = RenderExpression(rule.Guard, source)`.

**4. `~string` Qualifier in Field Type (BUG-017)**

In `CompileTool.RenderTypeName`: if the field's resolved type is `TypeKind.String` AND the
field has a CI qualifier (`DeclaredQualifierMeta.CaseInsensitive` or equivalent), render as
`"~string"` instead of `"string"`. Alternatively, add a `bool IsCaseInsensitive` property
to `PreceptFieldDto`.

**5. Collection Element Types (BUG-018)**

In `CompileTool.MapField`: for collection types (`Set`, `Queue`, `Stack`, `Log`, `List`, `Bag`,
`LogBy`, `QueueBy`), render the element type: `"set of string"` not `"set"`. For `Lookup`,
render `"lookup of K to V"`. The type rendering in `RenderTypeName(TypeKind)` currently ignores
the inner type — extend it or switch to `RenderTypeName(TypedField)` which has access to the
full `TypedType` structure including inner type information.

**6. Event Ensures in `PreceptEventDto` (BUG-022)**

Add `Constraints` to `PreceptEventDto`:
```csharp
public sealed record PreceptEventDto(
    string Name,
    EventArgDto[] Args,
    TransitionRowDto[] Rows,
    EnsureDto[]? Constraints    // NEW — event-scoped ensures
);
```
In `CompileTool.MapEvent`: populate from `semantics.EnsuresByEvent` (or the event-scoped slice
of `semantics.EnsuresByState`/`Ensures`).

**7. `because` Keyword Stripped from Value (BUG-023)**

In `CompileTool.MapEnsure` and `MapRule`, `RenderExpression(ensure.Message, source)` renders
the span including the `because` keyword. The fix: the `because` clause span must start AFTER
the `because` token. Either trim the keyword from the rendered span, or store the because-value
span (not the because-clause span) in `TypedEnsure` and `TypedRule`.

**8. `omit` Declarations in State DTOs (BUG-024)**

Add `OmittedFields` to `PreceptStateDto`:
```csharp
public sealed record PreceptStateDto(
    string Name,
    string[] Modifiers,
    EnsureDto[] Constraints,
    string[]? OmittedFields,    // NEW
    AccessModeDto[]? AccessModes // NEW — see item 10 below
);
```
In `CompileTool.MapState`: populate `OmittedFields` from `semantics.OmitsByState` (or equivalent).

**9. Outcome Kind in Transition Rows (BUG-032, BUG-036)**

Add `Outcome` and `RejectMessage` to `TransitionRowDto`:
```csharp
public sealed record TransitionRowDto(
    string[] FromStates,
    string? Guard,
    string[] Actions,
    string? ToState,
    string? Outcome,       // NEW — "transition" | "no transition" | "reject"
    string? RejectMessage  // NEW — populated only for "reject"
);
```
In `CompileTool.MapTransitionRow`: derive `Outcome = Outcomes.GetMeta(row.OutcomeKind).SerializedKind`
(using the `SerializedKind` field added in Slice 6). Populate `RejectMessage` by rendering
the reject expression span.

**10. Event Arg Optionality (BUG-033)**

Add `IsOptional` to `EventArgDto`:
```csharp
public sealed record EventArgDto(
    string Name,
    string Type,
    bool IsOptional    // NEW
);
```
In `CompileTool.MapArg`: `IsOptional = arg.IsOptional`.

**11. Per-State Access Mode Overrides (BUG-034)**

`AccessModeDto` already exists. Wire it in (see item 8 above — `AccessModes` added to
`PreceptStateDto`). In `CompileTool.MapState`: populate from `semantics.AccessModesByState`.

**12. Choice Element Type and Values (BUG-035)**

In `CompileTool.MapField`, for `TypeKind.Choice`:
- Add `ChoiceElementType: string?` and `ChoiceValues: string[]?` to `PreceptFieldDto`.
- Populate from `field.ResolvedType`'s choice metadata (element type and declared members).

**13. Modifier Bound Values (BUG-042)**

In `CompileTool.MapField`, for modifiers with values (`min`, `max`, `minlength`, `maxlength`,
`mincount`, `maxcount`, `maxplaces`): instead of rendering the modifier as a bare string name,
render as `"min 0"`, `"max 100"` etc. (include the value). Or add a structured `ModifierWithValue`
DTO. The simplest fix: render `ModifierKind` as `"{name} {value}"` using the modifier's declared
value from `TypedField.ModifierValues` (or equivalent field on `TypedField`).

**14. String Default Value Quotes (BUG-043)**

In `CompileTool.RenderExpression(field.DefaultExpression, source)`: the raw span includes
the surrounding double-quotes for string literals. Strip them. The fix is in `RenderSpan` or
in post-processing: if the rendered string starts and ends with `"`, trim those characters.
Same fix for `choice of string` default values (BUG-035 crossover).

### Tests Required

- `MCP-DefinitionTests/` (new test class) — compile each minimal repro, call `MapDefinition`,
  assert the expected DTO fields are present and correct. One test per bullet above.

### Bugs Closed

BUG-011, BUG-012, BUG-016, BUG-017, BUG-018, BUG-022, BUG-023, BUG-024, BUG-032, BUG-033,
BUG-034, BUG-035, BUG-036, BUG-042, BUG-043, BUG-047

---

## Slice 13: MCP-Docs — Fix Incorrect Recovery Hints

**Goal:** Update recovery hints in `ProofsTool.cs` and `DiagnosticTool.cs` (or their backing
catalog entries in `DiagnosticMeta` / `RuntimeFaultMeta`) that contain invalid Precept syntax
or incorrect advice. These fixes should happen AFTER the underlying compiler bugs are fixed
(Slices 8–11), so the hints can accurately describe working solutions.

**Files:**
- `tools/Precept.Mcp/Tools/ProofsTool.cs` — recovery hints for runtime faults
- `tools/Precept.Mcp/Tools/DiagnosticTool.cs` — per-code diagnostic descriptions
- `src/Precept/Language/Diagnostics.cs` / `src/Precept/Language/Faults.cs` — update the
  authoritative catalog entries when the tool text is projected from metadata

### Changes Required

**1. `CollectionEmptyOnMutation` Recovery Hint (BUG-014)**

After BUG-008 is fixed (Slice 11), the correct recovery for `pop`/`dequeue` is:
```
Guard the action with 'when Field.count > 0' in the transition row guard clause.
Alternatively, apply 'notempty' to the field declaration.
```
Update the hint to match what actually works.

**2. PRE0083 Dual-Role Description (BUG-015)**

PRE0083 fires for both division-by-zero AND collection mutation (empty collection). The
diagnostic description must document both scenarios:
- **Division-by-zero:** Add guard `when Divisor != 0` or apply `nonzero`/`positive`/`min 1`.
- **Collection mutation:** Guard with `when Field.count > 0` or apply `notempty`.
Update the `precept_diagnostic` entry for PRE0083 to cover both triggers.

**3. `UnexpectedNull` Invalid Syntax (BUG-041)**

Replace `when Field != null` (invalid in Precept v2) with:
```
Add the 'optional' modifier to the field declaration,
or guard access with 'when Field is set' before use.
```
`null` is not a Precept v2 literal. `!= null` is not valid syntax. Use `is set` / `is not set`.

### Tests Required

- `MCP-DocsTests/RecoveryHintTests.cs`:
  - `CollectionEmptyOnMutation_RecoveryHint_ContainsCountGuard()`
  - `PRE0083_Description_CoversBothDivisionAndCollection()`
  - `UnexpectedNull_RecoveryHint_UsesIsSet_NotNull()`

### Bugs Closed

BUG-014, BUG-015, BUG-041

---

## Slice 14: Test Layer — Catalog Capability Tests

**Goal:** Add a comprehensive suite of catalog-capability tests that compile one minimal precept
per relevant catalog member and assert clean output (or the expected diagnostic if the member
is inherently error-prone). These tests would have caught ~40 of 54 bugs.

**New test class:** `test/Precept.Tests/CatalogCapability/CatalogCapabilityTests.cs`

### Test Pattern

```csharp
[Theory]
[MemberData(nameof(AllActionKinds))]
public void AllActionKinds_MinimalPrecept_CompilesClean(ActionKind kind)
{
    var source = BuildMinimalPreceptFor(kind); // generates the minimal DSL for that action
    var result = Compiler.Compile(source);
    result.HasErrors.Should().BeFalse(
        because: $"ActionKind.{kind} should compile clean with a minimal valid precept");
    result.Diagnostics.Should().BeEmpty();
}
```

The generator `BuildMinimalPreceptFor(ActionKind kind)` uses `ActionMeta.SnippetTemplate` to
build a valid minimal precept for each action kind. Same pattern for `ConstructKind`,
`ModifierKind`, `OperatorKind`, `FunctionKind`.

### Test Files to Create

| Test class | Coverage |
|------------|----------|
| `ActionKindCoverageTests.cs` | All `ActionKind` members — minimal precept per action, assert clean compile |
| `ConstructKindCoverageTests.cs` | All `ConstructKind` members — one minimal precept per construct kind |
| `ModifierKindCoverageTests.cs` | All `ModifierKind` members — on applicable field/state/event types |
| `OperatorKindCoverageTests.cs` | All `OperatorKind` members — in expression positions (rule/ensure/when) |
| `FunctionKindCoverageTests.cs` | All `FunctionKind` members — one minimal expression per function |
| `OutcomeKindCoverageTests.cs` | All `OutcomeKind` members — one transition row per outcome form |
| `TokenKindKeywordCoverageTests.cs` | All keyword `TokenKind` members — appear in at least one valid precept |

These tests are catalog-reflection tests: they derive the test cases from `Enum.GetValues<T>()`.
If a new catalog member is added without a working implementation, these tests catch it
immediately at compile time.

---

## Slice 15: Test Layer — Pipeline Stage Unit Tests (Catalog-Aware)

**Goal:** Unit tests for individual pipeline stages using synthetic AST/semantic fixtures and
the real static catalogs (not mocked). Per the approved test strategy in `decisions.md` (2026-05-10T03:13:51Z):
the real static catalogs are the executable language contract; mocking them adds drift risk.

### Test Pattern

```csharp
// Synthetic AST fixture for a specific parser or type-checker behavior
var parsed = new ParsedBinaryExpression(
    Span: TestSpan(),
    Left: new ParsedFieldRef("Score", TestSpan()),
    Operator: OperatorKind.And,
    Right: new ParsedFieldRef("Active", TestSpan()));

// Real catalog metadata
var opMeta = Operators.GetMeta(OperatorKind.And);
opMeta.ResultType.Should().Be(TypeKind.Boolean);
opMeta.ResultTypePolicy.Should().Be(ResultTypePolicy.BothOperands);

// Real type checker with synthetic input
var ctx = CheckContext.ForTest(fieldTable: [("Score", TypeKind.Boolean), ("Active", TypeKind.Boolean)]);
var typed = TypeChecker.CheckExpression(parsed, ctx);
typed.ResolvedType.Should().Be(TypeKind.Boolean);
```

### Test Files to Create

| Test class | Pipeline stage | Key behaviors tested |
|------------|---------------|---------------------|
| `Parser.ActionChainTests.cs` | Parser | Shape-driven action parsing for all `ActionSyntaxShape` values |
| `Parser.StateTargetTests.cs` | Parser | Wildcard and broadcast recognition |
| `Parser.MemberAccessTests.cs` | Parser | Keyword member names from `IsValidAsMemberName` |
| `TypeChecker.OperatorTypingTests.cs` | Type Checker | Result type from `ResultType` / `ResultTypePolicy` |
| `TypeChecker.ModifierValidationTests.cs` | Type Checker | Bound pairs from `BoundCounterpart`, subsumption from `Subsumes` |
| `NameBinder.WildcardTests.cs` | Name Binder | `IsStateWildcard`, `IsBroadcastFieldTarget` |
| `NameBinder.ForwardReferenceTests.cs` | Name Binder | Topological sort, cycle detection |
| `ProofEngine.CollectionMutationTests.cs` | Proof Engine | Named field in pop/dequeue obligations |
| `ProofEngine.FunctionReturnTests.cs` | Proof Engine | `ReturnNonnegative` proof propagation |
| `MCP.DefinitionProjectionTests.cs` | MCP | DTO field coverage for all `SemanticIndex` types |
| `MCP.OutcomeKindProjectionTests.cs` | MCP | `SerializedKind` in transition row DTOs |

---

## Bug Map — Slice Coverage

| Bug | Title (abbreviated) | Category | Root Cause | Slice(s) |
|-----|---------------------|----------|------------|---------|
| BUG-001 | `any` state wildcard not recognized | Compiler | `IsStateWildcard` missing; name binder does literal lookup | Slice 1 + Slice 8 + Slice 10 |
| BUG-002 | `contains` operator rejected | Compiler | `OperatorMeta` lacked result-type metadata; type checker uses wrong result | Slice 4 + Slice 9 |
| BUG-003 | `and`/`or`/`not` rejected | Compiler | Same as BUG-002: logical operators lacked catalog result-type metadata | Slice 4 + Slice 9 |
| BUG-004 | `default` rejected on event args | Compiler | Declaration-site applicability metadata is modeled too narrowly and the arg parser doesn't invoke the value-modifier loop | Slice 3 + Slice 8 |
| BUG-005 | Comma-separated field list rejected | Compiler | Parser doesn't parse `("," Field)*` in `FieldTarget` | Slice 8 |
| BUG-006 | `min(a,b)` not recognized as function | Compiler | `IsAlsoBuiltinFunction` missing; no lookahead for `(` | Slice 1 + Slice 8 |
| BUG-007 | Arithmetic < comparison precedence | Compiler | Pratt binding powers inverted — not reading `OperatorMeta.Precedence` | Slice 9 |
| BUG-008 | pop/dequeue obligations use `<unknown>` | Compiler | Proof engine binds obligation to synthetic var, not field name | Slice 11 |
| BUG-009 | `for` resolves to key type | Compiler | `ResultTypePolicy.ElementType` missing; type checker returns LHS type | Slice 4 + Slice 9 |
| BUG-010 | `choice` literal not typed in comparison | Compiler | Context type not propagated to literal in comparison position | Slice 9 |
| BUG-011 | State hook actions not in MCP output | MCP-definition | `StateHookDto` not wired into `PreceptDefinitionDto` | Slice 12 |
| BUG-012 | Stateless handler actions not in MCP | MCP-definition | `MapEvent` reads only transition rows, not handler rows | Slice 12 |
| BUG-013 | `sqrt(abs(x))` uses `<unknown>` | Compiler | Proof engine doesn't know `abs()` returns ≥ 0 | Slice 7 + Slice 11 |
| BUG-014 | CollectionEmptyOnMutation hint incorrect | MCP-docs | Recovery hint suggests a guard that BUG-008 makes ineffective | Slice 13 |
| BUG-015 | PRE0083 description incomplete | MCP-docs | Only describes division-by-zero, not collection mutation | Slice 13 |
| BUG-016 | Rule `when` guard not in MCP output | MCP-definition | `PreceptRuleDto` has no `When` field | Slice 12 |
| BUG-017 | `~string` qualifier lost in MCP output | MCP-definition | Type rendering ignores CI qualifier | Slice 12 |
| BUG-018 | Collection element types lost in MCP | MCP-definition | `RenderTypeName(TypeKind)` ignores inner type | Slice 12 |
| BUG-019 | Typed constants not context-resolved | Compiler | Parser doesn't accept `TypedConstant` in expression atoms | Slice 8 |
| BUG-020 | Guarded ensures not parsed | Compiler | Pre-verb guard slot missing from construct metadata; parser doesn't accept `when` before verb | Slice 5 + Slice 8 |
| BUG-021 | `append by P` not parsed | Compiler | Parser ignores `CollectionValueBy` shape; `by` not consumed | Slice 2 + Slice 8 |
| BUG-022 | Event ensures not in MCP output | MCP-definition | `PreceptEventDto` has no `Constraints` field | Slice 12 |
| BUG-023 | `because` keyword in serialized value | MCP-definition | `RenderExpression` span includes keyword | Slice 12 |
| BUG-024 | `omit` declarations not in MCP output | MCP-definition | `PreceptStateDto` has no `OmittedFields` field | Slice 12 |
| BUG-025 | Keyword-named accessors rejected | Compiler | Type keywords not in `IsValidAsMemberName`; parser requires Identifier | Slice 1 + Slice 8 |
| BUG-026 | `modify all` treats `all` as field name | Compiler | `IsBroadcastFieldTarget` missing; name binder does literal lookup | Slice 1 + Slice 8 + Slice 10 |
| BUG-027 | `choice of T(...)` not valid in event args | Compiler | Arg-type parser uses restricted TypeRef that excludes ChoiceType | Slice 8 |
| BUG-028 | `RedundantModifier` garbled message | Compiler | Wrong diagnostic code (PRE0033 vs PRE0037); message templates concatenated | Slice 9 |
| BUG-029 | `InvalidModifierBounds` not enforced (num) | Compiler | `BoundCounterpart` missing; type checker has no catalog field to check | Slice 3 + Slice 9 |
| BUG-030 | Computed field forward references rejected | Compiler | Name binder processes fields in declaration order, no topological sort | Slice 8 (parser) + Slice 10 |
| BUG-031 | String interpolation not in reject/because | Compiler | Parser expects `StringLiteral`, ignores `StringStart`/`StringEnd` sequence | Slice 8 |
| BUG-032 | `reject` outcomes not in MCP rows | MCP-definition | `TransitionRowDto` has no `Outcome`/`RejectMessage` fields | Slice 6 + Slice 12 |
| BUG-033 | Event arg `optional` not in MCP | MCP-definition | `EventArgDto` has no `IsOptional` field | Slice 12 |
| BUG-034 | Per-state access modes not in MCP | MCP-definition | `AccessModeDto` exists but not wired into `PreceptStateDto` | Slice 12 |
| BUG-035 | Choice element type + values lost in MCP | MCP-definition | `PreceptFieldDto` has no `ChoiceElementType`/`ChoiceValues` fields | Slice 12 |
| BUG-036 | `no transition`/`reject` indistinguishable | MCP-definition | `TransitionRowDto` has no `Outcome` field (same root as BUG-032) | Slice 6 + Slice 12 |
| BUG-037 | `omit all` treats `all` as field name | Compiler | Same root as BUG-026; affects both `modify` and `omit` verbs | Slice 1 + Slice 8 + Slice 10 |
| BUG-038 | `InvalidModifierBounds` not enforced (str/col) | Compiler | Same root as BUG-029; `BoundCounterpart` missing for string/collection pairs | Slice 3 + Slice 9 |
| BUG-039 | `list.at(N)` rejected — `at` collision | Compiler | `at` not in `IsValidAsMemberName`; member-access parser requires Identifier | Slice 1 + Slice 8 |
| BUG-040 | `queue.peekby(P)` not implemented | Compiler | `peekby` accessor not in type checker's QueueBy dispatch | Slice 9 |
| BUG-041 | `UnexpectedNull` hint uses `!= null` | MCP-docs | Recovery hint uses invalid v2 syntax; should use `is set` | Slice 13 |
| BUG-042 | Modifier bound values not in MCP | MCP-definition | Modifiers rendered as bare names without values | Slice 12 |
| BUG-043 | String defaults include surrounding quotes | MCP-definition | `RenderExpression` returns raw token span including `"` delimiters | Slice 12 |
| BUG-044 | Guarded state actions not supported | Compiler | Pre-verb guard slot missing from `StateAction` metadata | Slice 5 + Slice 8 |
| BUG-045 | `ascending`/`descending` not recognized in log type | Compiler | Parser doesn't consume these tokens after `log of T by P` | Slice 8 |
| BUG-046 | CI enforcement not in quantifier binding | Compiler | Binding variable doesn't inherit CI qualifier from collection element type | Slice 9 |
| BUG-047 | Stateless hook actions not in MCP | MCP-definition | Same root as BUG-012; `rows` populated only from transition rows | Slice 12 |
| BUG-048 | `by` not recognized in append/enqueue | Compiler | Same root as BUG-021; `CollectionValueBy` shape not read by parser | Slice 2 + Slice 8 |
| BUG-049 | `insert`/`remove at` fail — `at` ambiguity | Compiler | `InsertAt`/`RemoveAtIndex` shapes not read by parser | Slice 2 + Slice 8 |
| BUG-050 | dequeue/pop trigger false PRE0083 | Compiler | Wrong proof obligation type — same `<unknown>` root as BUG-008 | Slice 11 |
| BUG-051 | `min(a,b)` fails — reserved keyword | Compiler | Same root as BUG-006; `IsAlsoBuiltinFunction` missing | Slice 1 + Slice 8 |
| BUG-052 | `contains` unusable in expression | Compiler | Same root as BUG-002; type checker result type wrong for `contains` | Slice 4 + Slice 9 |
| BUG-053 | `and`/`or` fail in all positions | Compiler | Same root as BUG-003; logical operators lacked catalog result-type metadata | Slice 4 + Slice 9 |
| BUG-054 | `ensure` not supported in stateless hooks | Compiler | `SupportsPostActionEnsure` missing on `EventHandler` construct | Slice 5 + Slice 8 |

---

## Out of Scope

- **Track 1 (Language Server) work** — LS Phase 2 gap-closure plan is in
  `docs/Working/language-server-implementation-plan.md`. Track 2 is independent.
- **New language features** — Track 2 fixes spec-documented behavior that is broken.
  It does not add new language constructs.
- **Performance optimization** — the frozen dictionary lookups added by catalog-field reads
  are O(1); no performance regression is expected or measured as part of this track.
- **`precept_inspect`, `precept_fire`, `precept_update` MCP tools** — these three tools are
  not yet implemented. Out of scope for Track 2.
- **Typed literal validation framework expansions** — temporal `ContentValidation` additions,
  UCUM parser work — these are separate tracks already in progress.
- **MCP AI authoring tool suite** (`precept_quickstart`, `precept_syntax`, etc.) — the focused
  named-tool suite is a separate track.

---

## Dependencies

Track 2 is **independent of Track 1** (language server work). Both tracks operate on the same
`Precept-V2-Radical` branch but do not share modified files — Track 1 adds new LS handlers,
Track 2 fixes core catalog and pipeline bugs.

Within Track 2, the ordering dependencies are strict:

```
Slices 1–7 (catalog additions)
    ↓
Slices 8–11 (pipeline fixes — can proceed in parallel after all of 1–7)
    ↓
Slice 12 (MCP DTO audit — requires clean pipeline output)
    ↓
Slice 13 (MCP-docs fixes — requires accurate compiler behavior to give correct hints)
    ↓
Slices 14–15 (test layer — written against fixed behavior to lock it in)
```

Within Phase A (Slices 1–7): keep the work metadata-only. The real schema-addition slices
(1, 3, 4, 5, 6, 7) are independent of each other and may proceed in parallel once each slice
applies its record/enum field additions before catalog-entry population. Slice 2 audits
`Actions.cs` and fixes catalog drift there; `Parser.cs` remains exclusively in Slice 8 by
design. Catalog tests execute in Phase A. Consumer integration tests execute in the
owning slices named in each section (Slices 8–13), because those slices contain the required
parser, checker, binder, proof, and MCP behavior changes.

Within Phase B (Slices 8–11): each pipeline stage fix depends on its prerequisite catalog
fields being present. Once all catalog slices are complete, pipeline stage fixes may proceed
in parallel — the parser (Slice 8), type checker (Slice 9), name binder (Slice 10), and
proof engine (Slice 11) do not share files.
