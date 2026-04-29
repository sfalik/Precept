## Soup Nazi — Test Coverage Audit

**Date:** 2026-04-28
**Auditor:** Soup Nazi (Tester)
**Scope:** 6 parser remediation slices R1–R6 against `src/Precept/Pipeline/Parser.cs`

**Verdict:** APPROVED

**Test Run:** 2034 tests, 2034 passing, 0 failing

---

### Coverage by Slice

| Slice | Coverage | Notes |
|-------|----------|-------|
| R6 | N/A | Cosmetic — removed unauthorized comment block from Parser.cs header; no behavioral change, no test needed |
| R1 | COVERED | Dispatch via `Constructs.ByLeadingToken` covered: `Parse_UnknownLeadingToken_ProducesDiagnosticAndSyncs` (error path), `Parser_Parse_EmptyInput_ReturnsTreeWithNullHeaderAndDiagnostic` (EndOfSource), `Parser_Parse_AllConstructKinds_RoundTrip` (all 8 leading token types in one shot), plus individual declaration-level tests for field/state/event/rule |
| R2 | COVERED | `Parse_RuleDeclaration_Simple`, `Parse_RuleDeclaration_WithGuard`, `ParseBecauseClause_MissingBecause_ProducesDiagnostic` (in SlotParserTests), `ParseRuleExpression_DirectExpression`, `ParseGuardClause_WhenPresent/WhenAbsent` |
| R3 | COVERED | `Parse_StateDeclaration_Simple`, `Parse_StateDeclaration_MultipleStates`, `ParseStateModifierList_Terminal`, `ParseStateModifierList_Initial`, `ParseStateModifierList_NoModifiers_ReturnsNull`. New test added: `StateDeclaration_HasExactlyOneSlot_StateEntryList` documents the R3 design (1 compound slot, not 2 separate slots) |
| R4 | COVERED | `Parse_EventDeclaration_Initial` (IsInitial=true), `Parse_EventDeclaration_Simple` (no marker), `ParseArgumentList_*` (slot machinery). New test added: `EventDeclaration_HasInitialMarkerSlot` pins the R4 catalog shape (3 slots: IdentifierList, ArgumentList(opt), InitialMarker(opt)) |
| R5 | COVERED | All 4 scoped prepositions exercised: `in` → `Parse_StateEnsure_In_Simple`, `Parse_AccessMode_*`, `Parse_OmitDeclaration_*`; `to` → `Parse_StateEnsure_To_Simple`, `Parse_StateAction_To_*`; `from on` → `Parse_TransitionRow_*`, `from ensure` → `Parse_StateEnsure_From_Simple`, `from ->` → `Parse_StateAction_From_Simple`; `on ensure` → `Parse_EventEnsure_*`, `on ->` → `Parse_EventHandler_*`. Disambiguation error path: `Disambiguator_NoMatchingToken_EmitsDiagnosticAndSyncs`, `Disambiguator_EmptyAfterAnchor_EmitsDiagnostic` |

---

### Blocking Findings (at audit start — resolved before verdict)

**B1:** `ParserInfrastructureTests.InvokeSlotParser_SwitchIsExhaustive` hardcoded slot-kind count as `16`. R4 added `ConstructSlotKind.InitialMarker`, making the count `17`. The parser switch already had the matching arm, but the test count was stale. **Fixed — updated expected count to 17 in `ParserInfrastructureTests.cs`.**

**B2:** `ConstructsTests.KeyConstructs_HaveMinimumSlotCount(StateDeclaration)` expected `StateDeclaration` to have ≥2 slots, but R3 correctly collapsed the state declaration shape to a single `StateEntryList` slot (compound, per-entry name+modifiers). The old assumption (separate IdentifierList + ModifierList) was the pre-R3 design. **Fixed — removed `StateDeclaration` from the theory. Added `StateDeclaration_HasExactlyOneSlot_StateEntryList` as a targeted replacement test documenting the R3 design rationale.**

---

### Tests Written

- `ParserInfrastructureTests.cs` — `InvokeSlotParser_SwitchIsExhaustive()` — updated expected count 16 → 17 to reflect R4 addition of `InitialMarker`
- `ConstructsTests.cs` — `StateDeclaration_HasExactlyOneSlot_StateEntryList()` — documents and pins the R3 slot design: exactly 1 `StateEntryList` slot, required
- `ConstructsTests.cs` — `EventDeclaration_HasInitialMarkerSlot()` — documents and pins the R4 slot design: 3 slots (IdentifierList, ArgumentList(opt), InitialMarker(opt))

---

### Summary

All 6 remediation slices have adequate behavioral coverage in the existing test suite. Two tests were broken by the remediation itself (stale hardcoded counts and a pre-R3 slot assumption) — both were fixed, and two new targeted tests were added to pin the R3 and R4 catalog shapes going forward. No `[Skip]` attributes were introduced anywhere. Final test run: **2034 passing, 0 failing**.
