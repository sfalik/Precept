# Frank — Architectural Compliance Review

**Date:** 2026-04-28
**Subject:** Remediated Parser.cs (R1–R6 slices)
**Reviewer:** Frank (Lead/Architect & Language Designer)
**Build:** `dotnet build src/Precept/Precept.csproj` — 0 errors, 1 pre-existing analyzer warning (RS1030, unrelated)

---

**Verdict:** APPROVED

---

## Findings

### G1: Top-level dispatch is fully catalog-driven (R1)

`ParseAll()` (line 185) dispatches exclusively via `Constructs.ByLeadingToken.TryGetValue()`. No hardcoded token-to-method mapping anywhere in the loop. The single-candidate fast path (line 192–194) checks `candidates is [var only]` with no disambiguation tokens, then routes through `ParseDirectConstruct`. Multi-candidate paths route through `DisambiguateAndParse`. No vestiges of the old switch/if-chain dispatch remain.

### G2: Rule slot machinery is clean (R2)

`ParseRuleDeclaration()` (lines 722–731) follows the exact v8 pattern: `Constructs.GetMeta(ConstructKind.RuleDeclaration)` → `ParseConstructSlots(meta)` → `BuildNode()`. No hand-rolled field extraction. The slot sequence `[RuleExpression, GuardClause, BecauseClause]` drives parsing entirely from catalog metadata.

### G3: State catalog correct, slot machinery routed (R3)

`StateDeclaration` in Constructs.cs (line 60–67) has exactly one slot: `SlotStateEntryList`. No `StateModifierList` — the bad slot is gone. `ParseStateDeclaration()` (lines 700–708) routes through `ParseConstructSlots()` → `BuildNode()`. State modifiers are parsed per-entry inside `ParseStateEntries()` using the catalog-derived `StateModifierKeywords` frozen set, which is the correct level — modifiers are entry-level, not construct-level.

### G4: Event slot machinery + InitialMarker correct (R4)

`EventDeclaration` in Constructs.cs has slots `[IdentifierList, ArgumentList, InitialMarker]`. `ConstructSlotKind.InitialMarker` exists (ConstructSlot.cs line 25), with a corresponding `ParseInitialMarker()` (lines 937–942) that checks `TokenKind.Initial` and wraps it. `ParseEventDeclaration()` (lines 711–719) routes through slot machinery. No dead code from old hand-rolled event parsing remains.

### G5: Disambiguation uses catalog loop exclusively (R5)

`FindDisambiguatedConstruct()` (lines 273–283) performs the catalog lookup: `Constructs.ByLeadingToken.TryGetValue(leadingKind, ...)` then iterates candidates checking `entry.DisambiguationTokens?.Contains(disambToken)`. No hardcoded token → construct kind mapping. The routing switches in `DisambiguateAndParse()` (lines 237–242, 261–268) map from the catalog-returned `ConstructKind?` to parse methods — this is method routing, not vocabulary classification.

### G6: Header is clean (R6)

Lines 1–23 contain: `using` directives, namespace declaration, `<summary>`/`<remarks>` XML doc describing the parser's dispatch architecture and catalog derivation principle. No unauthorized comment block. The remarks reference `docs/language/catalog-system.md`, which is appropriate cross-referencing.

### G7: Slot machinery is catalog-driven and exhaustive

- `ParseConstructSlots()` (lines 807–816) iterates `meta.Slots` — purely catalog-driven.
- `InvokeSlotParser()` (lines 825–848) is an exhaustive switch over all 17 `ConstructSlotKind` members with CS8509 enforcement — adding a new member without an arm is a build error.
- `BuildNode()` (lines 1293–1359) is an exhaustive 12-arm switch over all `ConstructKind` values with a wildcard throw.

### G8: Sync recovery uses catalog-derived leading tokens

`SyncToNextDeclaration()` (line 675) uses `Constructs.LeadingTokens.Contains()` — catalog-derived, not hardcoded. Error recovery in `TryParseActionStatementWithRecovery()` (line 487) also uses `Constructs.LeadingTokens`.

### G9: Vocabulary frozen sets all derive from catalog metadata

`OperatorPrecedence` → `Operators.All`, `TypeKeywords` → `Types.ByToken`, `ModifierKeywords` → `Modifiers.All`, `StateModifierKeywords` → `Modifiers.All`, `ActionKeywords` → `Actions.All`. No hardcoded parallel keyword lists in parser code.

### G10: Two-tier architecture is sound

The parser correctly implements a two-tier dispatch model:
- **Non-disambiguated constructs** (field, state, event, rule) → full slot machinery via `ParseConstructSlots()` → `BuildNode()`
- **Disambiguated constructs** (transition row, state ensure, access mode, omit, state action, event ensure, event handler) → catalog-driven dispatch via `FindDisambiguatedConstruct()`, hand-written per-construct parsers

Both tiers get catalog-driven dispatch. Tier 1 gets full slot machinery. Tier 2 parsers are necessarily hand-written because the disambiguation loop has already consumed the anchor target before routing. This is the v8-designed architecture.

---

## Construct Addition Cost Assessment

Adding a new construct to the parser requires:

1. `ConstructKind` enum member — catalog
2. `GetMeta()` arm in Constructs.cs — catalog
3. `BuildNode()` arm — parser (one-line node construction)
4. If non-disambiguated: `ParseDirectConstruct()` arm + parse method (which is just `GetMeta` → `ParseConstructSlots` → `BuildNode` boilerplate)
5. If disambiguated: routing arm in `DisambiguateAndParse()` + parse method
6. New slot kinds (if any): `ConstructSlotKind` member + `InvokeSlotParser()` arm

The catalog determines **what** to parse and **how to dispatch**. The parser code handles **how to parse** each construct. This boundary is correct — parsing algorithms are by definition hand-written. The v8 design never promised zero-touch construct addition; it promised catalog-driven dispatch and vocabulary, which is what we have.

---

## Summary

The parser is now catalog-driven as designed. All six remediation slices (R1–R6) are correctly implemented. Top-level dispatch routes through `Constructs.ByLeadingToken`, disambiguation walks `DisambiguationTokens` from the catalog, non-disambiguated constructs parse through the generic slot machinery, sync recovery uses catalog-derived token sets, and vocabulary frozen sets derive from catalog metadata. The file compiles cleanly and the architecture matches the v8 design document.
