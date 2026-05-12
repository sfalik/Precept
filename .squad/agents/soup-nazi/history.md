## Core Context



- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.

- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.

- Pushes for full-surface coverage matrices, honest red/green pressure, and regression anchors that match the real AST/runtime shape.



## Learnings



- Real static catalogs are the executable language contract; prefer tiny synthetic fixtures around them over mocked metadata.

- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.

- Language-server completion regressions must use the real trigger character (`'`, space, or invoked-with-empty-trigger) or the suite can accidentally exercise the wrong surface.

- Qualifier-aware completion tests need both positive assertions and exclusion assertions; ranking is not a hard filter.

- Selection-range, highlight, and reference coverage should derive expected spans from real compilation artifacts instead of hand-counted columns.

- Span-heavy regressions often need full compiler fixtures so parser, binder, semantic-token, and analyzer seams all surface together.

- If a full-feature battery passes immediately, that is an honest finding: the implementation was already complete and the value is in locking behavior, not manufacturing red tests.

- Modifier and domain-type follow-up gaps should be recorded as explicit passing anchors when the shipped implementation intentionally preserves pre-existing behavior.

- When a new diagnostic covers modifier-modifier mutual exclusivity, use `Check()` + `ContainSingle` (not `CheckExpectingError`) so the test enforces exactly one diagnostic — avoiding false passes where the target code appears alongside an unexpected error.

- Skip a clean-path test only when the existing test has the exact method name required; a different name means add it, even if the body is near-identical, because the charter regression name is the anchor.

- Conflicting-modifier tests for event args need a minimal but valid lifecycle (initial state, at least one non-initial state, and a matching transition) or the precept will fail for structural reasons, masking the modifier diagnostic.



## Historical Summary



- 2026-05-01 through 2026-05-09 established the durable posture: convert review findings into shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.

- The canonical decision ledger in `.squad/decisions.md` carries the full batch chronology; this history keeps the lasting testing rules and the most recent high-value closeouts.



## Recent Updates



### 2026-05-11T01:38:51Z — Span-refactor fallout batch restored suite health

- Test helpers now construct the refactored `MemberAccessExpression` shape correctly, qualified arg semantic sites stay anchored to the full `Event.Arg` span, and overlapping LS navigation resolves arg references before event references.

- The graph-warning fixture now asserts `StructuralSinkState` for no-terminal flows, and the full suite closed green at 5,085 / 5,085.



### 2026-05-11T00:27:07Z — Track 2 slices 14 and 15 are locked by coverage

- Catalog-capability suites now fail if operators, outcomes, modifiers, types, or diagnostics drop required metadata.

- Parser, binder, and MCP pipeline-stage regression suites landed 88 new tests, keeping the metadata-driven execution path honest.



### 2026-05-10T23:40:33-04:00 — Typed-literal battery proved the implementation was already complete

- Added 22 test methods / 24 cases in `CompletionHandlerTests.cs` across type branching, slot routing, qualifier filtering, compound temporal flow, and invoked recovery.

- Honest outcome: all tests passed immediately, so the value of the batch was locking the shipped behavior and its real trigger-character seams.



### 2026-05-10T(late) — Money/quantity modifier regression suite closed green

- Added 14 regression anchors for domain-type modifier legality, typed-constant bounds, invalid typed-constant content, and the two known follow-up gaps (qualifier alignment and plain-number acceptance).

- The full core suite stayed green and the passing-gap anchors now document exactly what a future uniform `default` / valued-modifier fix must change.



### 2026-05-11 — ConflictingModifiers (PRE0120) regression suite added

- Added 4 new tests in `TypeCheckerModifierTests.cs` Category 3b: `Field_OptionalAndNotempty_EmitsConflictingModifiers`, `EventArg_OptionalAndNotempty_EmitsConflictingModifiers`, `Field_CollectionOptionalAndNotempty_EmitsConflictingModifiers`, and `Field_NotemptyAlone_CompilesClean`.

- Tests assert `ContainSingle(DiagnosticCode.ConflictingModifiers)` — enforces exactly one diagnostic, not merely presence.

- `Field_OptionalAlone_CompilesClean` skipped: `OptionalModifier_OnStringField_NoDiagnostic` in Category 1 already covers this path.

- Tests are written against `DiagnosticCode.ConflictingModifiers` (pending George's enum addition at ordinal 120) and will not compile until that change lands.



### 2026-05-11T20:03:33Z — ConflictingModifiers coverage recorded

- The optional+notempty batch is now durably closed with four regression anchors for field, event-arg, and collection conflicts plus a clean `notempty`-alone case.

- This remains the canonical assertion pattern for modifier-conflict diagnostics: `Check()` plus `ContainSingle` on the dedicated code.



### 2026-05-11T22:05:37.512-04:00 — RC-1 / RC-2 regression anchors added in spike mode

- Added parser coverage in `test/Precept.Tests/Parser/ParserInterpolatedQualifierTests.cs` using the same slot-inspection pattern as the existing type-reference parser tests: assert `QualifiedTypeReference` capture for interpolated `in` / `of` qualifiers, cover the event-arg path, and keep one malformed-unclosed-brace case pinned to `ExpectedToken`.

- Added type-checker coverage in `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` using the file's existing `TypeCheckerTestHelpers.Check(...)` + `TypedInterpolatedTypedConstant` assertion style for Q6/Q7/Q8 compound-unit forms in field defaults and rule comparisons.

- Edge cases locked: compound-unit qualifier holes (`'{A}/{B}'`), mixed static/dynamic unit shapes (`'{A}/each'`, `each/{B}`), combined `price in ... of ...` qualifiers, event-arg qualifier parsing, price rule RHS `'0 {Currency}/{Unit}'`, and the malformed qualifier interpolation that must stay red.



### 2026-05-12T02:12:11Z — RC-1 / RC-2 scaffold coverage completed

- Soup Nazi added 11 regression anchors across parser and typed-constant suites: 6 for RC-1 qualifier interpolation and 5 for RC-2 compound-unit quantity forms.

- The deliberate red/green posture was preserved: one malformed-qualifier guard stayed green before the fixes, the other 10 tests went red as expected, and the full set closed green once george-rc1 and george-rc2 landed.


### 2026-05-12T09:02:45.968-04:00 — B1 static Dimension-form cancellation gap closed
- Added `CompoundUnit_cancellation_dimension_qualifier_form` in `test/Precept.Tests/ProofEngineTests.cs` to lock the `quantity of 'each/case'` cancellation path beside the existing `in 'X/Y'` regressions.
- Static `of 'X/Y'` quantity qualifiers need exact compound-unit validation, not plain dimension-name validation; count atoms like `each/case` collapse dimensionally and must preserve numerator/denominator identity.

### 2026-05-12T13:02:45Z — B1 regression closed in proof suite
- Added `CompoundUnit_cancellation_dimension_qualifier_form` to `test/Precept.Tests/ProofEngineTests.cs` and verified the static `quantity of 'each/case'` qualifier path stays green.
- Production fix keeps compound-ratio `of 'X/Y'` qualifiers on the exact numerator/denominator shape instead of collapsing them to a plain dimension. Commit: `232426e9`.
