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

- When `FieldTargetSlot` changes from single-name to multi-name, every test that asserts `.FieldName.Should().Be("all")` on a broadcast path will break — the broadcast path must have a stable identity contract (`IsBroadcast` property or equivalent) before any consuming test can be written or migrated.
- `DiagnosticsTests.cs TypeCodes` and `ProofCodes` lists are exhaustiveness traps: new diagnostic codes added to the enum are picked up by `AllDiagnosticCodes` automatically but silently omitted from the stage-group theory tests unless hand-added. Always add new codes to these lists in the same commit that adds them to `DiagnosticCode.cs`.
- When a design has open questions about diagnostic multiplicity (wildcard rows, self-loops, OR disjuncts), do not write the affected tests until the behavior is decided — a test written on a false assumption will green immediately and lock the wrong behavior.

- Skip a clean-path test only when the existing test has the exact method name required; a different name means add it, even if the body is near-identical, because the charter regression name is the anchor.

- Conflicting-modifier tests for event args need a minimal but valid lifecycle (initial state, at least one non-initial state, and a matching transition) or the precept will fail for structural reasons, masking the modifier diagnostic.
- Exhaustiveness-style `Diagnostics.Create(...)` fixture tests must supply enough placeholder args for the highest index used by any current message template; proof diagnostics now reach `{5}`, so a stale four-arg fixture throws before the metadata assertion runs.
- Comma-list `StateTarget` coverage is not review-ready unless the parser suite itself pins two-name lists, three-plus-name lists, whitespace variants, and trailing-comma failure; type-checker-only anchors are not an acceptable substitute for parser behavior.
- Multi-state expansion tests must assert cloned semantics, not just counts: guard/action/outcome payloads and per-name `UndeclaredState` fan-out need explicit anchors or real bugs can hide behind "have count" assertions.
- Commit-level test-count claims must be checked against `dotnet test` output; this spike advertises `4966` core tests, but the current `Precept.Tests` run reports `4962`.



## Historical Summary



- 2026-05-01 through 2026-05-09 established the durable posture: convert review findings into shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.

- The canonical decision ledger in `.squad/decisions.md` carries the full batch chronology; this history keeps the lasting testing rules and the most recent high-value closeouts.



## Recent Updates

### 2026-05-12T23:20:42Z — George's blocker closeout and redesign re-review approved

- Re-review of commits `53d68d51` and `cf3c6a81` found `0 blockers / 2 good findings`: B1-B5 are closed, the explicit-wildcard `ResolvedStateTarget` redesign is structurally correct, and the spike is ready to merge.

### 2026-05-12T23:02:04Z — Comma-list spike review kept parser/test closure blocked

- Recorded the remaining review gate on commit `a63d88b4`: parser AST coverage for 2-name/3+-name/whitespace/trailing-comma cases, semantic-clone assertions on expanded rows, and explicit multi-unknown-state diagnostic fan-out.
- Locked the count-integrity rule for this spike: published core-suite totals must match real `dotnet test` output, and the current tester anchor is `4962`, not the previously cited `4966`.

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

### 2026-05-12T17:56:47-04:00 — HoverHandlerTests surface audit: all 5 failures already resolved

- Arrived to find 44/44 HoverHandlerTests passing (was 36/5 at Kramer B5 observation). No changes required; documented the repair history.
- All 5 failures were **implementation bugs**, not stale expectations: the tests correctly described the locked V6 design; the implementation was behind.
  - `af6e563c` — `"\n"` → `"\n\n"` join separator in 6 card builders (field/state/event/argument/rule/ensure). Single-newline collapses in VS Code hover; the multi-section `Contain` assertions failed on run-on card output.
  - `0ef4b8d0` — wired qualifier `SourceFieldName` / resolved-source into the stored-field card.
  - `5ab6030e` — added qualifier axis/checks/exchange-rate card content; extended stored-field test.
  - `516aa6ba` — added proof-aware hover routing (gap card on transitions, proof-diagnostic span winner, proof-bearing subexpression card).
  - `7829e9c6` — proof-chain chained-expression hover details.
- Full suite green: 44 hover, 272 LanguageServer, 4938 core.
- Badge vocabulary and "proven" (not "proved") wording consistent throughout all 44 tests. Design contract held.

### 2026-05-12T18:55:39-04:00 — v2 field-state guarantees test plan reviewed

- Assessed all four slices of the v2 plan. Found 31 missing test cases across parser, type checker, proof engine, and regressions. Revised total: ~74 (plan claimed ~43).
- Top 3 risks: (1) broadcast `.FieldName == "all"` contract breaks in 5 currently-green tests when `FieldTargetSlot` goes multi-field; (2) `DiagnosticsTests.cs` `TypeCodes`/`ProofCodes` lists are exhaustiveness traps that silently exclude D130–D134; (3) event handler validation has zero test coverage despite being an explicit design concern.
- Identified 5 open design questions that block test writing: broadcast identity contract, field baseline for D132, OR-disjunct access condition diagnostic, wildcard-row diagnostic multiplicity, and self-loop dual-diagnostic behavior.
- Filed findings to `.squad/decisions/inbox/soup-nazi-v2-test-review.md`.

### 2026-05-12T19:50:08-04:00 — Modifier catalog gap tests added for price/exchangerate/identity types

- Added `test/Precept.Tests/TypeChecker/PriceExchangeRateModifierTests.cs` — 17 tests covering: 7 price clean tests (nonnegative, positive, nonzero, min, max, minmax, maxplaces), 4 exchangerate clean tests (nonnegative, positive, nonzero, maxplaces), 2 exchangerate negative tests (min/max still `InvalidModifierForType` by design), 4 regression guards.
- Added `test/Precept.Tests/TypeChecker/IdentityTypeModifierTests.cs` — 3 tests covering the double-error fix: `currency`, `unitofmeasure`, and `dimension` + `notempty` must emit ONLY `RedundantModifier`, with explicit `NotContain(InvalidModifierForType)` assertion.
- Extended `test/Precept.Tests/TypeChecker/MoneyQuantityModifierRegressionTests.cs` — added `Maxplaces_OnMoneyField_NoDiagnostic` and `Maxplaces_OnQuantityField_NoDiagnostic`.
- Red/green posture at write time: 7 price tests RED (George's implementation pending), all exchangerate and identity-type tests GREEN (those gaps already fixed or never broken), 2 new MoneyQuantity tests GREEN.
- Confirmed exchangerate field qualifier syntax as `in 'USD' to 'EUR'` (not `in 'USD/EUR'`); price field qualifier as compound `in 'USD/each'`.
- Confirmed min/max on exchangerate CORRECTLY remain `InvalidModifierForType` — ordering is undefined for currency-pair rates per spec § "Business-domain comparison".
- The 5 pre-existing `ModifiersTests.NumericModifiers_ApplyToIntegerDecimalNumberMoneyQuantity` theory failures (Min, Max, Nonnegative, Positive, Nonzero) matched the filter string but are pre-existing, not caused by this batch.

### 2026-05-12T18:01:17.648-04:00 — Hover baseline verified clean after B1

- Verified `HoverHandlerTests` at 44/44 passing with `Precept.LanguageServer.Tests` 272/272 and core `Precept.Tests` 4938/4938. No test edits, disables, or skips were needed in this pass.
- Recorded the five earlier hover failures as already-fixed implementation bugs: markdown paragraph joining, qualifier source plumbing, qualifier axis/checks output, transition proof-gap routing, and proof-chain details.

### 2026-05-12T23:50:08Z — Modifier-gap regression suite closed green after coordinator follow-up

- Soup Nazi landed 22 regression tests across `PriceExchangeRateModifierTests.cs`, `IdentityTypeModifierTests.cs`, and `MoneyQuantityModifierRegressionTests.cs`, covering price/exchangerate legality, business-magnitude `maxplaces`, and identity-type redundancy behavior.
- Coordinator corrected the price qualifier fixture and updated `ModifiersTests` drift theories for the split `ZeroBound` vs `Ranged` catalog groups, bringing the final repo-wide validation result to `4995/4995`.
