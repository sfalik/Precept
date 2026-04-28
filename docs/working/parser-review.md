# Parser Review — Phase 6

**Reviewer:** Frank (principal language designer) + Soup Nazi (test author)
**Date:** 2025-07-25
**Test count at review time:** 2226 (2019 Precept.Tests + 207 Analyzers) — all green

---

## Track A — Implementation Findings

### Blocker Issues (must fix before ship)

None found.

All 11 locked design decisions verified correct. All 9 valid grammar forms parse correctly. The implementation is structurally sound.

### Non-Blocker Issues (fix recommended)

**NB-1: Stale `Write` references in Parser.cs header comment (Severity: Non-Blocker)**
- **File:** `src/Precept/Pipeline/Parser.cs`, lines 29, 48, 72
- **What it says:** The introductory dispatch-table comment block (lines 26–73) references `Write` as a leading token and `ParseAccessMode()` as its handler. Line 29: "Five leading tokens (Field, State, Event, Rule, Write) map 1:1". Line 48: "Write → ParseAccessMode()". Line 72: "Write, In, To, From, On".
- **What it should say:** `Write`/`Read` tokens are retired from the access-mode context (locked decision #9). The dispatch table comment should not mention `Write` as a leading token. The actual dispatch loop (line 251) correctly does NOT dispatch on `Write` — only the comment is stale.
- **Impact:** Comment-only; no behavioral defect. But the comment is the first thing any reader sees and contradicts the actual implementation.

**NB-2: EventHandler does not forward stashed guard (Severity: Non-Blocker)**
- **File:** `src/Precept/Pipeline/Parser.cs`, line 305 vs 574
- **What happens:** `DisambiguateAndParse` for `on`-scoped constructs calls `TryParseStashedGuard()` at line 294, then for `Arrow` routes to `ParseEventHandler(start, eventTarget.Value)` at line 305. The stashed guard is silently discarded — `ParseEventHandler` doesn't receive it.
- **What should happen:** `EventHandlerNode` has no `Guard` property, so there's nowhere to inject it. But the user wrote `on Submit when X -> set foo = bar` — the `when X` is silently consumed and ignored with NO diagnostic. This differs from OmitDeclaration which correctly emits `OmitDoesNotSupportGuard` when a guard is structurally invalid.
- **Recommendation:** Either (a) emit a diagnostic when a stashed guard routes to EventHandler (like omit does), or (b) add a Guard slot to EventHandlerNode. The silent discard is the worst option.

**NB-3: `ExpectedOutcome` diagnostic missing from parser-v2.md diagnostic table (Severity: Non-Blocker)**
- **File:** `docs/compiler/parser-v2.md`, line 950
- **What it says:** The diagnostic code table lists 6 codes: `ExpectedToken`, `UnexpectedKeyword`, `NonAssociativeComparison`, `InvalidCallTarget`, `OmitDoesNotSupportGuard`, `PreEventGuardNotAllowed`.
- **What it should say:** `ExpectedOutcome` is also emitted by the parser (lines 509, 540 of Parser.cs) and exists in `DiagnosticCode.cs`. The doc table should list 7 codes.

**NB-4: `UnexpectedKeyword` and `InvalidCallTarget` listed in doc but never emitted by parser (Severity: Non-Blocker)**
- **File:** `docs/compiler/parser-v2.md`, lines 953–954; `src/Precept/Pipeline/Parser.cs` (full file)
- **What it says:** The doc lists `UnexpectedKeyword` and `InvalidCallTarget` as parse-stage diagnostic codes.
- **What actually happens:** grep of Parser.cs shows zero occurrences of `UnexpectedKeyword` or `InvalidCallTarget`. The parser uses `ExpectedToken` for unrecognized tokens at declaration position (line 270). These codes exist in `DiagnosticCode.cs` but the parser never emits them.
- **Recommendation:** Either remove them from the doc's parse-stage table (they may be type-checker codes), or note they are reserved/future.

**NB-5: `MutuallyExclusiveQualifiers` listed in `DiagnosticCode.cs` parse section but not in doc (Severity: Non-Blocker)**
- **File:** `src/Precept/Language/DiagnosticCode.cs`, line 20
- **What it says:** `MutuallyExclusiveQualifiers` is in the `// ── Parse ──` section of the enum.
- **What should happen:** Either the doc should mention it, or it should be moved to a different section if it's actually a type-checker code. It is not emitted by Parser.cs.

### Test Coverage Gaps (Soup Nazi)

**TG-1: No test for `in State when Guard modify Field editable` (pre-stashed guard on AccessMode with `editable`)**
- Tests cover `readonly` with pre-field guard (`Parse_AccessMode_WithPreFieldGuard` at line 346) and `editable` with post-field guard (`Parse_AccessMode_WithPostFieldGuard` at line 337). But no test covers `editable` with pre-stashed guard. Should be symmetric.

**TG-2: No test for `in State omit F1, F2, ... when Guard` (post-field guard on omit LIST form)**
- `Parse_OmitDeclaration_WithPostFieldGuard_EmitsDiagnostic` (line 387) tests singular field. The list and `all` forms with a trailing `when` guard are untested. Verify the diagnostic fires for all three FieldTarget shapes.

**TG-3: No test for `in State omit all when Guard` (post-field guard on omit ALL form)**
- Same as TG-2 for the `all` shorthand.

**TG-4: No test for `in State when Guard omit F1, F2` (pre-stashed guard on omit LIST form)**
- `Parse_OmitDeclaration_WithPreFieldGuard_EmitsDiagnostic` (line 396) tests singular field. List and `all` forms with pre-stashed guard are untested.

**TG-5: No test for `in State when Guard omit all` (pre-stashed guard on omit ALL form)**
- Same as TG-4 for the `all` shorthand.

**TG-6: No negative test for `modify` without `in` scope — e.g., bare `modify Amount readonly`**
- The dispatch loop doesn't handle `Modify` or `Omit` as leading tokens (correct), but there's no test proving the parser emits a diagnostic and recovers when `modify` appears bare at the top level.

**TG-7: No test for `on Event when Guard -> action` (stashed guard on EventHandler — NB-2)**
- Per NB-2, the stashed guard is silently discarded. No test documents this behavior (whether intentional or buggy).

**TG-8: No test for AccessMode or OmitDeclaration with empty input after verb — e.g., `in Draft modify` then EOF**
- Edge case: what happens when the token stream ends immediately after the disambiguation token?

**TG-9: No test for mixed AccessMode + OmitDeclaration in a single multi-line parse**
- The integration test at line 788 covers both in the same parse, but it doesn't assert that they are distinct node types with distinct slot shapes (e.g., AccessMode has Guard, Omit does not).

**TG-10: No test for `from State when Guard on Event` with guard on LIST or ALL FieldTarget forms**
- `Parse_TransitionRow_PreEventGuard_EmitsDiagnosticAndParses` tests the basic case. No variant tests with complex guard expressions.

**TG-11: No property-based assertion that `OmitDeclarationNode` has exactly 3 constructor params across ALL test files**
- `Parse_SampleFile_OmitDeclarationNodes_HaveNoGuard` (line 748) does a reflection check, which is good. But it only runs once, not parameterized across sample files.

### Implementation Verdict

**PASS — Implementation is correct.** All 11 locked design decisions are accurately implemented. The 9 valid grammar forms all parse correctly. Key findings:

1. `modify` is consumed by disambiguator, NOT stored — ✅ verified (line 395)
2. `readonly`/`editable` stored in `Mode` as `Token` — ✅ verified (AccessModeNode.cs)
3. `omit` = structural exclusion verb — ✅ verified (OmitDeclarationNode.cs, separate from AccessModeNode)
4. `omit` NEVER has guard — ✅ verified (both pre-field line 415–416, post-field lines 421–427)
5. `OmitDeclaration` is SEPARATE from `AccessMode` — ✅ verified (separate node types, separate parse methods, no shared parent)
6. `OmitDeclaration` slots `[StateTarget, FieldTarget]` ONLY — ✅ verified (Constructs.cs line 119, OmitDeclarationNode.cs)
7. `AccessMode` slots `[StateTarget, FieldTarget, AccessModeKeyword, GuardClause(opt)]` — ✅ verified (Constructs.cs line 110)
8. Guard position for AccessMode is POST-FIELD — ✅ verified (Parser.cs line 400–405)
9. `Write`/`Read` retired — ✅ verified (not in dispatch loop; only in stale comment)
10. `FieldTargetNode` is a DU — ✅ verified (FieldTargetNode.cs: abstract base + 3 sealed subtypes)
11. Both `modify` and `omit` support comma-separated list and `all` — ✅ verified (ParseFieldTargetDirect lines 597–631)

The one behavioral concern is NB-2 (EventHandler silently discarding stashed guard). This should be resolved before downstream consumers depend on the behavior.

---

## Track B — parser-v2.md Accuracy Findings

### Discrepancies

**D-1: Status section says "Stub — Parse() throws NotImplementedException" (Severity: Doc Update — STALE)**
- **File:** `docs/compiler/parser-v2.md`, line 10
- **What it says:** `Implementation state | Stub — Parse() throws NotImplementedException`
- **What's true:** Parser is fully implemented. 2226 tests pass. No remaining stubs.
- **Fix:** Update to `Implementation state | Complete — 5-PR implementation per v8 plan. All 12 constructs parse. 2226 tests green.`

**D-2: Architecture code sample shows `session.Build()` pattern — implementation uses `session.ParseAll()` (Severity: Doc Update)**
- **File:** `docs/compiler/parser-v2.md`, lines 87–104
- **What the doc shows:**
  ```csharp
  session.ParseAll();
  return session.Build();
  ```
  And `ParseSession` as `private struct`.
- **What's actually implemented:** `Parser.cs` line 162: `return session.ParseAll();` — `ParseAll()` returns the `SyntaxTree` directly. `ParseSession` is `internal ref struct` (line 170), not `private struct`.
- **Fix:** Update the code sample to match the actual pattern.

**D-3: Doc describes dispatch as "catalog-driven lookup" — implementation uses hand-written switch (Severity: Doc Update — MISLEADING)**
- **File:** `docs/compiler/parser-v2.md`, line 27
- **What it says:** "The parser is catalog-driven. Constructs.ByLeadingToken and Constructs.LeadingTokens are the primary dispatch indexes — not a hardcoded switch."
- **What's actually implemented:** The dispatch loop at Parser.cs line 251 is a hand-written `token.Kind switch { Field => ..., State => ..., In|To|From|On => DisambiguateAndParse }`. It does NOT use `Constructs.ByLeadingToken` for dispatch. `ByLeadingToken` exists and is populated correctly, but the actual dispatch is a switch expression.
- **Why it matters:** The doc claims "not a hardcoded switch" — but it IS a hardcoded switch. The vocab tables (operators, types, modifiers, actions) ARE catalog-derived (correctly). But the top-level dispatch itself is hand-written. The parser's own header comment (lines 15–26) correctly explains this — the doc should align.
- **Fix:** Revise the overview paragraph to say dispatch is hand-written (per the architecture boundary ruling) while vocabulary recognition is catalog-derived. The dispatch table in section "Top-Level Dispatch Loop" (lines 148–161) is accurate — it just needs the framing to match reality.

**D-4: Doc describes `SkippedTokens` and `IsMissing` nodes — parser implements neither (Severity: Doc Update)**
- **File:** `docs/compiler/parser-v2.md`, lines 52, 122, 866, 996, 1036
- **What the doc says:** Multiple references to `SkippedTokens` spans ("Recovery preserves all tokens between the error point and the sync point as a SkippedTokens span") and `IsMissing` nodes ("Expect() returns a synthetic IsMissing token", "IsMissing = true").
- **What's actually implemented:** `Expect()` at Parser.cs line 212 returns `new Token(kind, string.Empty, Current().Span)` — a synthetic token with empty text but no `IsMissing` property. `Token` is a `readonly record struct` with `Kind`, `Text`, `Span` — there is no `IsMissing` flag. Similarly, there is no `SkippedTokens` span or node type. Error recovery uses `SyncToNextDeclaration()` which silently advances past tokens.
- **Fix:** Remove or qualify references to `IsMissing` and `SkippedTokens`. Describe the actual error recovery: synthetic zero-length empty-text tokens and silent token advancement.

**D-5: Doc says `UnexpectedKeyword` is emitted at declaration position — parser emits `ExpectedToken` instead (Severity: Doc Update)**
- **File:** `docs/compiler/parser-v2.md`, line 853, 1025
- **What it says:** "emits an UnexpectedKeyword diagnostic and scans forward" (line 853) and the error-conditions table lists `UnexpectedKeyword` for "Unrecognized token at declaration position" (line 1025).
- **What's actually implemented:** Parser.cs line 270 emits `ExpectedToken` (not `UnexpectedKeyword`) for unrecognized tokens at declaration level.
- **Fix:** Replace `UnexpectedKeyword` with `ExpectedToken` in both locations.

**D-6: Doc says `InvalidCallTarget` is a parser diagnostic — not emitted by parser (Severity: Doc Update)**
- **File:** `docs/compiler/parser-v2.md`, line 1027
- **What it says:** Error condition "Non-callable expression followed by `(`" emits `InvalidCallTarget`.
- **What's actually implemented:** Parser.cs has no reference to `InvalidCallTarget`. The code exists in DiagnosticCode.cs but may be a type-checker diagnostic.
- **Fix:** Remove from the parse-stage error conditions table, or note it as reserved.

**D-7: Doc says `Precept` is in the dispatch table as direct-dispatch — implementation handles it outside the switch (Severity: Doc Update — MINOR)**
- **File:** `docs/compiler/parser-v2.md`, line 151
- **What it says:** `Precept → 1 (PreceptHeader) → direct` in the dispatch table.
- **What's actually implemented:** `precept` is handled BEFORE the dispatch loop (Parser.cs lines 238–242), not inside it. The dispatch loop's switch (line 251) has no `TokenKind.Precept` arm.
- **Fix:** Add a note that `Precept` is handled as a one-shot before the dispatch loop, not inside it. The table is conceptually correct but the implementation detail differs.

**D-8: Doc says `EventDeclaration` grammar uses `"with"` for args — implementation uses `"("` (Severity: Doc Update)**
- **File:** `docs/compiler/parser-v2.md`, line 343
- **What it says:** `event Identifier ("," Identifier)* ("with" ArgList)? ("initial")?`
- **What's actually implemented:** Parser.cs line 784: `if (Match(TokenKind.LeftParen))` — the argument list uses parentheses, not `with`.
- **Fix:** Update the grammar rule to `("(" ArgList ")")? ("initial")?`.

**D-9: Source file reference says `PreceptParserTests.cs` — actual file is `ParserTests.cs` (Severity: Doc Update — MINOR)**
- **File:** `docs/compiler/parser-v2.md`, line 1177
- **What it says:** `test/Precept.Tests/PreceptParserTests.cs`
- **What's true:** `test/Precept.Tests/ParserTests.cs`
- **Fix:** Update the filename.

### Doc Updates Needed

1. **D-1:** Update Status section — implementation is complete, not stub.
2. **D-2:** Update architecture code sample to match `internal ref struct` + `ParseAll()` returns `SyntaxTree`.
3. **D-3:** Revise "catalog-driven" framing in overview — dispatch is hand-written, vocabulary is catalog-derived.
4. **D-4:** Remove/qualify `IsMissing` and `SkippedTokens` references — they don't exist in the implementation.
5. **D-5:** Replace `UnexpectedKeyword` with `ExpectedToken` in sync-point recovery and error-conditions sections.
6. **D-6:** Remove `InvalidCallTarget` from parse-stage error conditions.
7. **D-7:** Note `Precept` is pre-loop, not in dispatch switch.
8. **D-8:** Fix EventDeclaration grammar rule — parentheses not `with`.
9. **D-9:** Fix test file name.
10. **NB-3:** Add `ExpectedOutcome` to the diagnostic code table.
11. **NB-5:** Reconcile `MutuallyExclusiveQualifiers` placement in DiagnosticCode.cs.

### Doc Verdict

**parser-v2.md is a thorough and well-structured reference** — it accurately documents the AST node hierarchy, slot sequences, grammar forms, disambiguation logic, guard handling, and the OmitDeclaration/AccessMode separation. The 9 valid grammar forms are correct. The Declaration Node Summary table is accurate.

**However, it has significant staleness in the Status section and several framing mismatches** (D-3 "catalog-driven dispatch", D-4 "IsMissing/SkippedTokens") that would mislead a reader expecting the doc to match code exactly. The D-3 and D-4 findings are the most important to fix — they describe architectural features that don't exist in the implementation.

**After the 11 doc updates above, parser-v2.md will be fit as the canonical reference.**

---

## Summary

| Category | Count |
|----------|-------|
| Blocker Issues | 0 |
| Non-Blocker Issues | 5 (NB-1 through NB-5) |
| Test Coverage Gaps | 11 (TG-1 through TG-11) |
| Doc Updates Needed | 11 (D-1 through D-9, plus NB-3, NB-5) |

**Overall verdict:** The parser implementation is correct and complete. No blockers. The most important action item is **NB-2** (EventHandler silently discarding stashed guard) — this is a behavioral gap that should be resolved before downstream consumers depend on it. The doc (parser-v2.md) needs a pass to align with the implementation, particularly the Status section (D-1), the "catalog-driven dispatch" framing (D-3), and the `IsMissing`/`SkippedTokens` references (D-4).

**Recommended Phase 7 (George) prioritization:**
1. Fix NB-2 (EventHandler guard discard — add diagnostic or guard slot)
2. Fix NB-1 (stale `Write` comments in Parser.cs)
3. Add test cases TG-1 through TG-11
4. Apply all 11 doc updates to parser-v2.md
5. Fix NB-3/NB-4/NB-5 (diagnostic code reconciliation)
