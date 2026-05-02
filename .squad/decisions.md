# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-05-02T21:58:21Z: GAP-046 design locked to dedicated CI FunctionKind entries

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)

**Merged sources:** frank-gap046-design.

- GAP-046 is now durably locked to the catalog-complete path: add FunctionKind.TildeStartsWith / FunctionKind.TildeEndsWith plus FunctionMeta.CIVariantOf so CI functions exist as real function metadata rather than only as HasCIVariant side effects.
- Parser behavior stays intentionally unchanged; the ~ null-denotation path still derives CI-capable names from base-function HasCIVariant, while hover/completion/MCP follow-through must project from the new CI entries themselves.
- The open downstream checker concern remains explicit: calling ~startsWith / ~endsWith with a non-~string first argument still needs a future diagnostic decision when the real CIFunctionCallExpression handler lands.

---

### 2026-05-02T21:58:20Z: Canonical checker review resolutions D-15 through D-25 recorded

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)

**Merged sources:** frank-george-canonical-response.

- Frank's canonical response now has its own durable ledger record: widening is single-hop only, binary fallback order is deterministic (left, right, both), numeric literals stay bottom-up with context retry, event handlers get event-arg scope, and identifier resolution priority is bindings > args > fields.
- The response also locks the remaining checker-shape decisions George forced open: FieldScopeMode gates forward references, function overload resolution follows one deterministic pipeline, Slice 6 stays unsplit, TypedTransitionRow.ResolvedArgs stays rejected as anti-mirroring, and TypedEditDeclaration is placeholder-only for future stateless-edit work.
- Net result: all 11 checker slices are implementation-ready with no unresolved design blockers.

---

### 2026-05-02T21:58:19Z: Research validation integration pattern and Slice 4 range check locked

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)

**Merged sources:** frank-research-crossref.

- Research-validation artifacts are now durably patterned: the full validation file lives in research/language/, the design doc cites it in a `## Research Validation` section, the working draft stays in docs/working/ but is marked superseded, and research/language/README.md indexes the validation set.
- The same decision also adds out-of-range numeric literal checking to checker Slice 4: once expectedType resolves a literal, the checker validates the value against the representable range exposed by type metadata.
- This establishes a reusable design-validation workflow instead of leaving research cross-reference work as one-off process drift.

---

### 2026-05-02T21:58:18Z: GAP-035, GAP-040, and GAP-042 implementation batch recorded

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)

**Merged sources:** george-gap035-040-042.

- George's implementation batch now durably records the shipped parser/catalog work: TypeMeta.ChoiceLiteralTokens drives ParseChoiceValue, BagAccessors.countof uses ElementParameterAccessor, and the dead variant-action dispatch arms for CollectionValueBy, RemoveAtIndex, and CollectionIntoBy were deleted rather than scaffolded.
- The execution record preserves the test impact that the earlier design-only summaries did not: GAP-035 added choice-literal metadata/parser tests, GAP-040 added accessor-shape regressions, and GAP-042 remained a zero-behavior-change cleanup that stayed green across all 2713 existing tests.
- Durable parser rule: variant-action syntax shapes do not justify unreachable helper methods or dispatch arms; if Actions.ByTokenKind cannot route there, the parser must throw rather than pretend to construct a live statement node.

---

### 2026-05-02T21:58:17Z: GAP-046 implementation completed and consumer follow-through pinned

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)

**Merged sources:** george-gap046-complete.

- George shipped the GAP-046 implementation exactly on Frank's approved design path: FunctionKind.TildeStartsWith / TildeEndsWith, FunctionMeta.CIVariantOf, CI hover cross-reference text, the §3.7 catalog footnote, and the matching function-test count updates all landed green.
- The implementation record locks the downstream consumer contracts: LS CI completions derive from Functions.All.Where(f => f.CIVariantOf != null), future MCP language output projects a ciVariantOf field from the same metadata, and the eventual checker handler resolves CI calls through Functions.ByName["~" + name].
- Parser Tilde handling, TypeChecker logic, and diagnostics all remain intentionally unchanged in this slice; the implementation is catalog/tooling surface completion, not a new parser or checker behavior change.

---

### 2026-05-02T21:58:16Z: Iteration 10 doc/catalog and catalog/impl audits synchronized

**By:** Scribe

**Status:** Recovered from merged inbox, deduplicated, inbox cleared (2 files; cross-audit merge normalized)

**Merged sources:** frank-iteration10-audit, george-iteration10-audit.

- Frank's doc/catalog pass and George's catalog/impl pass now sit in one durable audit trail: GAP-043/044/045 were fixed inline in docs, GAP-047 remains the open spec-underdocuments-domain-overloads issue, and the earlier GAP-046 catalog gap is now closed by the shipped implementation.
- George's follow-on audit closes two more implementation drifts: ParseCollectionIntoStatement must throw on unreachable DequeueBy, and Modifiers now exposes an O(1) ByFieldToken index so parser modifier dispatch matches the rest of the catalog-driven parser surface.
- The combined carry-forward rule is explicit: parser-facing catalogs need direct metadata indexes, and variant-action dead arms can hide in nested sub-switches just as easily as at top-level dispatch.

---

### 2026-05-02T20:05:35Z: GAP-040 bag `countof` parameter locked to DU accessor metadata

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-gap040-decision`.

- GAP-040 is locked to the metadata-driven path: `bag.countof(...)` must stop pretending its parameter is `integer` and instead use a dedicated `ElementParameterAccessor` DU subtype whose parameter resolves to the bag element type.
- The flat `ParameterType` axis is now explicitly treated as a three-shape problem (`no parameter`, `fixed parameter type`, `element-type parameter`); the element-type case does not get a boolean flag or `TypeKind.Element` sentinel because both would create illegal or non-language-level states.
- Downstream consumers should pattern-match on the accessor subtype, keep MCP/tooling serialization as a thin projection of that metadata, and update bag-accessor assertions so `countof` renders as an element-typed accessor rather than `integer`.

---
### 2026-05-02T20:05:34Z: Frank type-checker review response accepted and locked

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-typechecker-review-response`.

- Frank formally accepted 5 of George's 6 findings, with Finding 5 reclassified as a non-finding because GAP-032 (`pow(integer, integer)` proof requirements) was already fixed.
- The response locks all 5 implementation pre-requisites as mandatory before or during the slice plan: no new operation indexes, a pre-Slice 0 shape commit, array-primary field storage plus a derived frozen name map, `ActionSecondaryRole` stamping for `TypedInputAction`, and per-slice `[HandlesCatalogMember]` stub migration.
- The revised checker plan also records durable shape choices for the remaining open design points: `ContentValidation` becomes a DU, resolution must always return partial typed results via `TypedErrorExpression`, qualifier propagation lives on typed binary expressions, and `MethodCallExpression` / interpolated forms are assigned explicit slice ownership.

---

### 2026-05-02T19:49:00Z: GAP-035 choice literal dispatch locked to ChoiceLiteralTokens metadata

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 late-arriving file)

**Merged sources:** `frank-gap035-decision`.

- GAP-035 is now locked to the catalog-complete path: add nullable `TypeMeta.ChoiceLiteralTokens` metadata rather than a `NumericLiteral` trait or a documented parser exception.
- `ParseChoiceValue` must derive both the signed numeric branch and literal-token validity from `Types.ByToken[elemToken.Kind].ChoiceLiteralTokens`, eliminating both remaining `elemToken.Kind` identity switches from parser choice-literal dispatch.
- `TypeTrait.ChoiceElement` remains the declaration-validation gate, while `ChoiceLiteralTokens` becomes the parse-time dispatch contract; couple them with an invariant test and mark GAP-035 fixed only after the parser rewrite is verified.

---
### 2026-05-02T19:48:45Z: TypeChecker pre-slice requirements and BinaryIndex semantics locked

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `george-typechecker-preslice-design`.

- The checker now has a pre-Slice 0 contract: land the full shape-only `SemanticIndex` and typed-record hierarchy before numbered slices, and keep field storage array-primary with a derived frozen name index so declaration order survives.
- Runtime lookup direction is locked to the existing `Operations.FindCandidates` / `FindUnary` APIs: `BinaryIndex` already returns multi-candidate arrays, so money/quantity overloads must disambiguate qualifier-matched entries and emit `QualifierMismatch` on failure.
- `TypedInputAction.SecondaryExpression` must carry an explicit secondary-role discriminator for evaluator dispatch, and GAP-032 / `pow(integer, integer)` is recorded as already closed rather than a live blocker.

---
### 2026-05-02T19:42:08Z: Collection-types plan review rounds R1/R2 synchronized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (12 files; multi-pass review normalized)

**Merged sources:** `frank-review-r1`, `george-review-r1`, `soup-review-r1`, `frank-review-r2`, `george-review-r2`, `soup-review-r2`, `frank-review-2`, `george-review-2`, `soup-review-2`, `frank-plan-v3-complete`, `frank-plan-v4-complete`, `frank-plan-v5-complete`.

- R1/R2 review passes converged on the same mandatory corrections: add `TokenKind.To` end-to-end, align codes 95–98 names and stage ownership with the spec, remove the spurious `ExpressionFormKind.LookupAccess` plan work, and route every proposed test into real existing files rather than phantom catch-all test files.
- The revision chain now preserves exact downstream obligations: update `DiagnosticsTests.cs` stage-group member data, fix hardcoded token counts and member-name regressions for `countof` / `peekby`, keep prefix `~startsWith` / `~endsWith` syntax correct, and spell out the `Countof` / `Peekby` member-name exceptions wherever token metadata is asserted.
- Durable planning rule: every slice must name real file targets, update existing hardcoded counts and member-data helpers, and keep plan/spec/catalog terminology synchronized before implementation begins.

---

### 2026-05-02T19:42:07Z: Collection-types final blocker stack and revision path recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (6 files; final-review stack normalized)

**Merged sources:** `frank-final-review`, `george-final-review`, `soup-final-review`, `frank-final-plan-revision-v2`, `frank-plan-revision-complete`, `elaine-final-review`.

**Deduplicated/skipped:** `frank-plan-review`.

- The final review stack agrees the plan is close but still blocked by surgical hazards rather than design rework: PRECEPT0019 parser annotations for new expression forms, wrong pseudocode symbols (`TokenKind.Assign`, `Statement(SourceSpan)`, `BinaryExpression`), missing updates to real test inventories, and incorrect parser-routing assumptions around `for` and `remove ... at`.
- Frank's revision passes also cleaned the plan spine itself: stale Phase 3/runtime scope was removed, dependency ordering was narrowed to the real slice ranges, and the catalog-first lookup-access direction stayed explicit with `for` pinned to a dedicated binding tier instead of vague pseudo-constants.
- Elaine's final UX pass downgraded remaining hover/MCP copy issues to follow-on tooling polish, so the plan now reads as mechanically repairable rather than conceptually blocked.

---

### 2026-05-02T19:42:06Z: Collection-types catalog and parser design decisions locked

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (11 files; architecture questions normalized)

**Merged sources:** `george-plan-review`, `frank-b1-analysis`, `copilot-b1-decision`, `frank-b2-analysis`, `frank-b2-catalog-exhaustive`, `frank-b2-reanalysis`, `frank-rubber-duck-exhaustively`, `frank-c3-verdict`, `frank-g5-rubber-duck`, `frank-countof-peekby-naming`, `frank-slice9-correction`.

- B1 is now locked to the catalog-complete path: keep secondary action kinds in `Actions.All`, add `ActionMeta.PrimaryActionKind`, and derive `ByTokenKind` from primary actions only so startup stays crash-free without hiding real language surface from catalog consumers.
- B2 is resolved as explicit switch-arm maintenance, not by stretching `[HandlesCatalogExhaustively]` onto `ActionSyntaxShape` or `ConstructSlotKind`; the annotation bridge remains the right tool for distributed handler coverage, not local parser shape switches.
- C3/G5/slice-9 clarifications are durable: `remove F at N` is handled before value parsing rather than via unreachable shape routing, `AppendBy` disambiguation stays syntactic instead of adding redundant catalog metadata, lexer keyword recognition remains fully catalog-driven, and `countof` / `peekby` stay as member-name-legal compound accessors.

---

### 2026-05-02T19:42:05Z: Collection-types documentation and wording corrections merged

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)

**Merged sources:** `docs-audit`, `elaine-review-2`, `elaine-spec-fixes-complete`, `elaine-catalog-fixes-complete`.

- The documentation audit locks the reference pattern: spec summaries are acceptable only when they explicitly defer to canonical type docs; the highest drift-risk duplication surfaces remain diagnostics and repeated `contains` semantics, which must cross-reference the spec instead of silently forking it.
- Elaine's doc/hover blockers were closed in both spec and catalog text: collection type descriptions, `append` wording, and the missing codes 99–106 diagnostics are now recorded as corrected rather than still implicit TODOs.
- Durable doc-sync rule for this workstream: language-surface wording changes are only done when spec, hover/catalog text, and the plan's referenced diagnostic tables all agree on the same user-facing story.

---

### 2026-05-02T19:42:04Z: Diagnostics semantics and emission for codes 11 and 12 locked

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (5 files)

**Merged sources:** `elaine-diagnostics-review`, `elaine-diagnostic-distinction`, `frank-diagnostic-activation`, `frank-gap-019-024-analysis`, `george-diagnostic-emission`.

- `UnexpectedKeyword` and `InvalidCallTarget` are now durably treated as distinct parse failures: the former is a declaration keyword in value-expression position, while the latter is a non-callable expression followed by `(...)`.
- The recorded contract is parse-stage Error severity, catalog/spec wording sync, catalog-derived keyword detection from `Tokens.Keywords.Values`, and an explicit `InvalidCallTarget` emit in the infix `LeftParen` branch rather than a silent break.
- The paired GAP-024 analysis stays with this bundle because it locked the same architectural principle: bag/list/log TypeQualifier support belongs in the spec surface, not as a parser rollback, since qualifier semantics are orthogonal to collection kind.

---

### 2026-05-02T19:42:03Z: Language-consistency gap fixes batch recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (12 files; gap-fix stream normalized)

**Merged sources:** `frank-gap024-resolved`, `frank-gap-025-026-028-fixed`, `frank-gap029-fixed`, `frank-gap031-fixed`, `george-gap030-fixed`, `george-gap032-fixed`, `frank-gap033-fixed`, `ink-gap-code-fixes`, `frank-iter7-results`, `frank-iter8-catalog-results`, `george-iter8-results`, `frank-g1-rename`.

- The gap ledger now durably records closed fixes across spec, catalog, parser, and runtime surfaces: GAP-024 spec support for TypeQualifier on bag/list/log, GAP-025/026/028 catalog mismatches, GAP-029/030/031 parser hardcodes replaced with catalog-derived sets/lookups, GAP-032 proof requirements for `pow(integer, integer)`, and GAP-033 stale `Notempty` documentation.
- Iteration 7 and Iteration 8 converge on the same architectural rule: parser vocabulary and precedence helpers must derive from catalog metadata, while structural constructs such as `.` and `(` may remain intentional non-catalog hardcodes because they are grammar structure rather than surfaced operators.
- PRECEPT0023c's `MultiLead` → `MultiSequence` rename and Ink's wider gap-fix batch now sit in the same durable audit trail; remaining unresolved items are downstream TypeChecker/Evaluator work, not unknown language-surface decisions.

---

### 2026-05-02T19:42:02Z: Dapr hosting research and bounded-quantifier philosophy note merged

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)

**Merged sources:** `frank-dapr-research`, `george-dapr-research`, `frank-philosophy-q1-amendment`, `frank-subset-disjoint-squash`.

- Both Dapr analyses converge on the same only-credible distributed-hosting shape: actor-hosted Precept instances with a pod-level compiled-definition cache, typed rehydration before guard evaluation, and `Restore()` as the state-store boundary; workflows remain the wrong semantic fit for Precept entity execution.
- Frank's proposed §0.4.1 amendment stays an owner-review item only: bounded quantifiers are philosophically compatible because they unfold over statically finite collections, but philosophy text must not change without explicit sign-off.
- Frank's `subset` / `disjoint` verdict is now durable alongside that philosophy note: keep them only for `set of <choice>` where the compiler can prove the closed-domain guarantee, and squash them for open types where quantifiers already cover the runtime-only case.

---

### 2026-05-02T19:42:01Z: Collection-types implementation notes through slices 1 to 14 recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files; working-state snapshot preserved)

**Merged sources:** `impl-notes-slices-1-7b`, `impl-notes-slices-8-14`.

- The implementation snapshot now durably records tokens 124–138, the `countof` / `peekby` member-name exceptions, `for` reuse as the lookup-access token, `~string` scalar parsing, and the observation that several planned catalog/AST steps were effectively already landed by the time slices were written up.
- The notes also preserve important corrections to stale assumptions: `to` already existed as `TokenKind` 14, slice 8 catalog/AST work had effectively been completed earlier, and parser/action tests must use real transition-row context instead of pseudo snippets.
- Treat these notes as execution-trail context rather than final plan truth; the review-normalized entries above remain the authoritative gate for follow-on work.

---

### 2026-05-01T20:06:10Z: Catalog-member annotation rename locked; no exhaustiveness gaps found

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-handlesform-rename`.

- Frank-10's rename is now canonical squad state: use `[HandlesCatalogMember]` for per-member claims alongside `[HandlesCatalogExhaustively(typeof(T))]`; legacy `[HandlesForm]` wording is retained only as historical rename context.
- Historical ledger wording was updated inline so earlier Slice 4, Slice 27, and annotation-bridge records stay readable after the rename without implying the old attribute still exists.
- Frank-9's full sweep of catalog enum types found no currently-unannotated distributed-dispatch gaps: existing consumers already line up with the correct enforcement mode, with CS8509 retained for centralized switches and `[HandlesCatalogExhaustively]` reserved for real distributed handlers.

---

### 2026-05-01T19:50:46Z: Phase 2 acceptance gate fully closed

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `george-phase2-gate-closed`.

- George's final gate-closure batch is now durably recorded: PRECEPT0023c checks duplicate full token sequences rather than duplicate lead tokens, the diagnostic is promoted to Error, and the `IsSet` / `IsNotSet` false-positive case is locked by regression coverage.
- The remaining docs gate was verification-only: `docs/language/precept-language-spec.md` §2.1 already had precedence `60` for `is set` / `is not set`, so the acceptance checklist closed without another spec edit.
- Phase 2 is now fully closed as team state: all 14 acceptance-gate items are satisfied, `docs/working/parser-gap-fixes-plan.md` marks the gate resolved, and validation stands at 2678 passing tests with 0 failures and 0 warnings.

---

### 2026-05-01T19:50:46Z: Lexer exhaustiveness annotation scope resolved

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-lexer-exhaustiveness`.

- Frank closed the annotation-scope question for the lexer: `Lexer` produces `TokenKind` values from catalog-driven lookup tables and never dispatches on `TokenKind`, so `[HandlesCatalogExhaustively]` would be the wrong contract.
- The correct safety net already exists in catalog metadata and lookup tables (`Tokens.Keywords`, operator tables, punctuation tables); production coverage stays catalog-driven rather than method-annotation-driven.
- The real follow-up remains the future evaluator implementation: when D8/R4 introduces expression-form dispatch in `Evaluator`, that same commit must add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` plus per-form handler annotations.

---

### 2026-05-01T18:17:13Z: Parser.cs partial split approved for Slice 27

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-parser-split`.

- `partial class Parser` + `partial ref struct ParseSession` is the approved zero-behavior-change split mechanism; `ParseSession` being a `ref struct` rules out helper-class alternatives because they would force `ref` threading through 60+ methods.
- The structural seam is locked as three files: `Parser.cs` for shell/vocabulary/dispatch, `Parser.Declarations.cs` for declaration grammar and slot/type machinery, and `Parser.Expressions.cs` for the Pratt loop, atom parsers, and expression helpers.
- Attribute placement is part of the contract: `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` stays only on the primary `ParseSession` declaration, `[HandlesCatalogMember(...)]` (renamed from `[HandlesForm(...)]`) moves with the methods in `Parser.Expressions.cs`, and static vocabulary remains on the outer `Parser` class.
- Durable implementation caveat for Slice 27 and Slice 16: `ref struct` types cannot own static fields, so `KeywordsValidAsMemberName` belongs on `Parser`, while `ExpectIdentifierOrKeywordAsMemberName()` stays on `ParseSession` beside the `Dot` handler.

---

### 2026-05-01T18:17:13Z: Parser-gap Slice 4 corrections and recording directive synchronized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files; blockers normalized into one record)

**Merged sources:** `copilot-directive-record-problems`, `frank-plan-review`, `george-plan-b1b4-fixes`.

- Shane's directive is now durable: when implementation uncovers problems, agents must write them into the working plan or decisions inbox instead of leaving them only in ephemeral output.
- Frank blocked Slice 4 on four exact plan defects: two existing attribute files incorrectly marked `Create`, the wrong analyzer filename/status, and a stale `HandlesCatalogMemberAttribute.Kind` snippet (then named `HandlesFormAttribute.Value`) that did not match the real API.
- George corrected `docs/working/parser-gap-fixes-plan.md` so `HandlesCatalogExhaustivelyAttribute.cs`, `HandlesCatalogMemberAttribute.cs` (renamed from `HandlesFormAttribute.cs`), and `Precept0019PipelineCoverageExhaustiveness.cs` are treated as existing files, and the code sample now uses `.Kind`.
- This record supersedes earlier stale ledger wording: the canonical annotation bridge remains generic `[HandlesCatalogExhaustively(typeof(T))]` + `[HandlesCatalogMember(kind)]` (renamed from `[HandlesForm(kind)]`), not a parameterless `HandlesExpressionForms` marker.

---

### 2026-05-01T18:17:13Z: Multi-token presence operators escalated to proposal-scope catalog work

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files; scope pending owner decision)

**Merged sources:** `frank-multi-token-operator-scope`, `george-multi-token-operator-scope`.

- Both analyses agree `is set` / `is not set` are real semantic operators with precedence, operand constraints, result typing, and documentation surface, so leaving them as uncataloged parser special-cases is a catalog-completeness bug rather than a parser-correctness bug.
- Shared implementation obligations are now explicit: add postfix-aware operator metadata, introduce `Arity.Postfix`, keep `.` and method-call `(` outside `Operators.All` as structural forms, and prevent the duplicate-key crash that would occur if both presence operators keyed `Operators.ByToken` on `(TokenKind.Is, Postfix)`.
- Frank recommends treating this as GitHub-issue/design-review work rather than a hotfix and prefers a full-fidelity catalog representation plus `ExpressionFormKind.PostfixOperation`; George supplied the bounded call-site inventory and the `ByToken` hazard that must be handled in the same commit.
- Carry-forward state: proposal scope and rationale are locked, but the final `OperatorMeta` shape still needs owner sign-off.

---

### 2026-05-01T18:17:13Z: Parser-gap implementation batch synchronized through Slice 13

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (10 files; implementation batch normalized)

**Merged sources:** `george-phase2a-complete`, `george-slice1-done`, `george-slice2-done`, `george-slice3-done`, `george-slice4-done`, `george-slice7-done`, `george-slices56-done`, `soup-nazi-slices-8-11-done`, `soup-nazi-slice12-done`, `soup-nazi-slice13-done`.

- George's branch reports now durably capture typed constants and interpolated typed constants, stateless event-handler post-condition `ensure` guards, Pratt support for `is set` / `is not set`, the `ExpressionFormKind` catalog + PRECEPT0019 annotation bridge, list literals, method calls, and the ensure-grammar spec correction.
- George also marked the broader parser-gap Phase 2A branch complete, so the slice reports above should be treated as the current implementation baseline rather than isolated point fixes.
- Soup Nazi's reports add regression coverage for comparison/contains operators, collection mutation actions, interpolated strings, sample-file integration, and reflection-backed expression-form coverage assertions.
- Slice 12 makes the remaining parser debt explicit instead of implicit: 21 sample files parse cleanly, while 7 stay in known-broken sentinels for three gaps still outside the completed slice batch — state/event ensure `when` guards, post-expression field modifiers, and reserved-keyword member names such as `.min` / `.max`.
- Validation stayed green across the slice reports and reached 2424 passing tests after the event-handler ensure-guard addition, while PRECEPT0019 finished green once list literals and method calls were annotated.

---

### 2026-05-01T06:21:31Z: Annotation-bridge enforcement pattern recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files; overlap normalized into one record)

**Merged sources:** `frank-annotation-bridge`, `george-annotation-bridge-plan`, `frank-class-marker`.

- Frank designed an annotation-bridge pattern for expression-form coverage: parser handlers advertise their responsibility with `HandlesCatalogMemberAttribute` (renamed from `HandlesFormAttribute`) instead of forcing an analyzer to reverse-engineer Pratt control flow.
- George's plan update locks that annotation bridge into Slice 4 rather than a follow-on: `HandlesCatalogMemberAttribute` (renamed from `HandlesFormAttribute`) lives beside `ExpressionForms`, PRECEPT0019 checks handler coverage across `Parser`, `TypeChecker`, `Evaluator`, and `GraphAnalyzer`, and Slice 13 stays the parser-routing assertion layer.
- Frank also locked the class-level opt-in marker for PRECEPT0019: use parameterless `[HandlesExpressionForms]` on pipeline classes from `src/Precept/HandlesExpressionFormsAttribute.cs`, while `[HandlesCatalogMember(ExpressionFormKind.X)]` (renamed from `[HandlesForm(ExpressionFormKind.X)]`) stays on methods to claim specific form coverage.
- Recommended enforcement is now three-layered: PRECEPT0007 keeps `ExpressionFormKind` exhaustiveness on catalog metadata, PRECEPT0019 checks that every form is claimed by handler annotations, and xUnit coverage tests verify end-to-end parser behavior.
- The durable design rule is to analyze stable metadata and attributes rather than parser implementation internals, so coverage enforcement survives refactors to switches, dictionaries, or helper methods.

---

### 2026-05-01T06:21:31Z: Parser-gap plan audit and coverage slice synchronized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files; overlap normalized into one record)

**Merged sources:** `frank-roslyn-analyzer-analysis`, `george-coverage-slice`, `george-plan-audit`.

- The parser-gap plan now carries Slice 13 (`ExpressionFormCoverageTests`) and expands Slice 4 so `LeadTokens` lives on `ExpressionFormMeta`; George sequenced the coverage slice after Slices 5 and 6 so it lands green from day one.
- Layer 1 compile-time coverage is locked to existing infrastructure: add `ExpressionFormKind` to `CatalogAnalysisHelpers.CatalogEnumNames` so PRECEPT0007 enforces explicit `GetMeta` arms. Standalone `GetLeadTokens()` + CS8509 and a new cross-method parser analyzer were both rejected.
- George's audit found the remaining plan hygiene fixes still worth carrying forward: add `src/Precept/Language/Operators.cs` to Slice 3's file inventory and remove or correct the dead `frank-expression-form-catalog-placement.md` reference. The previous missing-coverage-slice gap is now closed by Slice 13.

---

### 2026-04-29T05:34:09Z: Collection type expansion follow-up recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-collection-types-research`.

- Frank closed the remaining ordered-choice documentation gaps in `docs/language/collection-types.md`, so `choice(...) ordered` is treated consistently in the grammar, orderability framing, and comparison material.
- The doc now has `§ Proposed Additional Types`, evaluating six candidates with priority bands: `bag`, `log`, and `map` high; `sortedset` and `priorityqueue` medium; `deque` low.
- The new `§ Comparison With Other Collection Systems` cross-language table maps 14 capabilities across 9 ecosystems and reinforces restricted `map of choice(...) to V` as the strongest next collection-type research target.

---

### 2026-04-29T05:18:06Z: Collection types design doc authored and indexed

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)



**Merged sources:** `frank-collection-types-doc`.



- Frank authored `docs/language/collection-types.md` as the canonical collection-types reference, covering the shipped surface (`set`, `queue`, `stack`), actions, accessors, constraints, emptiness safety, inner-type behavior, `~string`, and diagnostic anchors.

- The new doc also preserves the current design frontier: proposed quantifier predicates plus collection-level modifiers such as `unique`, collection `notempty`, `subset`, and `disjoint`, with eight explicit owner-sign-off questions recorded before implementation.

- `docs/language/README.md` now indexes the new reference in the Documents table and reading order so collection guidance is discoverable from the language-doc hub.

---

### 2026-04-29T04:47:14Z: Vision→spec migration completed and vision archived

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files merged, 1 duplicate skipped)



**Merged sources:** `frank-slice1-preamble`, `frank-slice2-semantic-gaps`, `frank-slice3-4-archive`.

**Deduplicated/skipped:** `frank-vision-archive-audit` (already captured in the 2026-04-29T01:09:17Z vision/spec audit record).



- Frank's vision→spec migration is now durably closed as a complete sequence: §0 Preamble landed with the 11 Design Principles, Language Model, Governance Not Validation, Execution Model Properties, and pre-implementation graph/proof contracts; §3A Language Semantics landed with constraint semantics, outcome/verdict semantics, violation attribution, mutation atomicity, entity construction, and inspection as a first-class operation.

- The migration preserved substance rather than rewriting it: overlapping graph-analysis material was merged into one contract section, mutation atomicity/inspectability were expanded without duplicating their earlier anchors, and the spec now carries the identity-bearing language philosophy that previously lived only in the vision doc.

- Slice 3–4 then removed the two stale contradictions (`with` still listed as a structural preposition, and "root editability" wording left over from retired `write all` semantics), archived `docs/language/precept-language-vision.md` to `docs/archive/language-design/precept-language-vision.md`, updated the spec Status table, and swept 12 cross-references so the archived path never existed half-wired on the branch.

- Net result: the language spec is now the single canonical language document, the vision is preserved as archive material only, and the earlier archive-readiness audit remains the durable rationale for why this migration sequence was necessary.

---

### 2026-04-29T04:47:14Z: No-runtime-faults principles aligned; philosophy gap flagged

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)



**Merged sources:** `frank-principles-10-11-eval`, `frank-philosophy-eval`.



- Frank's evaluation of §0.1 Design Principles found Principles 10 and 11 incomplete rather than contradictory: Principle 10 previously treated runtime faults as acceptable "definite errors," and Principle 11 only promised compile-time elimination of type errors instead of all evaluator fault classes.

- The spec is now aligned with Shane's no-runtime-faults contract: Principle 10 requires the compiler to prove safety or emit an obligation diagnostic, Principle 11 extends the clean-compile guarantee across type, arithmetic, access, and range fault classes, and runtime traps are positioned only as defensive redundancy for compiler-proven-unreachable paths.

- The proof engine contract in §0.6 already supported the stronger guarantee through prove-safe / proved-dangerous / unresolved classification and obligation diagnostics; no proof-engine design change was needed because the principles were catching up to an already stronger compiler contract.

- Frank's philosophy-grounded follow-up endorsed those revisions but flagged a product-identity gap: `docs/philosophy.md` explicitly scopes "prevention, not detection" to invalid entity configurations and does not yet name evaluation-fault prevention with the same explicitness. Recommended wording was recorded for owner review only and was not applied.

- Net result: the spec now clearly states the no-runtime-faults promise, while the philosophy gap is durably recorded as a flag for Shane rather than an auto-applied philosophy change.

---

### 2026-04-29T03:09:18Z: PRECEPT0018 correctness gate closed and test backfill recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files)



**Merged sources:** `frank-precept0018-review`, `george-precept0018-implemented`, `george-precept0018-tests-added`.



- Frank's correctness-gate review confirmed the PRECEPT0018 analyzer, `AllowZeroDefaultAttribute`, all three intentional zero-value exemptions, and the 23 enum fixes were correct, then blocked merge only on three missing required regression tests: TP3 (zero-valued member not first), EC4 (`byte` underlying type), and EC5 (`long` underlying type).

- George's implementation record is now preserved as the baseline landing: commit `a7b0bb7` created the analyzer and attribute, applied `[AllowZeroDefault]` to `LexerMode.Normal`, `QualifierMatch.Any`, and `PeriodDimension.Any`, and made all 23 semantic enums 1-based with 225 analyzer tests and 2044 core tests green.

- George's follow-up commit `e7a643d` closed Frank's B1 finding and the two advisory anchors by adding TP7–TP9 and EC6–EC7 in `test/Precept.Analyzers.Tests/Precept0018Tests.cs`; analyzer tests rose to 230 while core tests stayed 2044.

- Net result: PRECEPT0018 is now durably recorded as implemented and correctness-cleared, with no post-review code changes beyond the missing regression tests.

---

### 2026-04-29T01:39:22Z: Catalog extensibility plan v3 cleared for George

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)



**Merged sources:** frank-catalog-ext-plan-v2-review, frank-catalog-extensibility-plan-review.



- Frank's first review blocked the catalog extensibility plan on three structural fixes: split PreceptHeader into RoutingFamily.Header, add Slice 3b to the execution order after Slice 3, and commit Slice 3b to explicit wrong-family ConstructKind listings so CS8509 stays active.

- Frank's second review confirmed those first-round blockers were resolved and approved the architecture, but found two new surgical blockers in the revised text: an unbound k variable in the Slice 3b throw examples and a phantom ErrorStatement(current) call in Slice 5.

- The coordinator patched both plan defects in plan.md: the Slice 3b guard text no longer references an unbound pattern variable, and Slice 5 now specifies the real synthetic-error-node handling instead of a nonexistent helper.

- src/Precept/Language/Token.cs was added to the file inventory because Slice 6 changes TokenMeta.IsAccessModeAdjective there, and the GetMeta wildcard note remains a documented non-blocking follow-up.

- Net result: the plan is now at v3, blockers are cleared, and George can implement from the updated plan.

---

### 2026-04-29T01:09:17Z: Catalog extensibility audit and parser design evaluation recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files)



**Merged sources:** `frank-catalog-extensibility`, `frank-parser-design-eval`, `frank-vision-spec-audit`.



- Frank's catalog extensibility audit confirmed the lexer is already 100% catalog-driven for keywords, operators, and punctuation; the remaining extensibility risk is entirely in parser/catalog enforcement boundaries.

- Eight parser hardening gaps are now the durable follow-up list: `BuildNode()` wildcard, `ParseDirectConstruct()` wildcard, hardcoded `DisambiguateAndParse()` routing, `ParseActionStatement()` switch exhaustiveness, hardcoded `ExpressionBoundaryTokens`, missing `ConstructKind`↔declaration subtype enforcement, missing `ActionKind`↔statement subtype enforcement, and hardcoded access-mode adjectives.

- The preferred remediation path is catalog shape change rather than Roslyn analyzers: remove wildcard fallthroughs for CS8509 coverage, derive boundary tokens from `Constructs.LeadingTokens`, add `RoutingFamily` to `ConstructMeta`, and add `ActionSyntaxShape` to `ActionMeta`.

- Frank's parser design evaluation across v5-v8 approved v8 as the closed canonical baseline: current code matches the parser spec, `OmitDeclaration` is correctly split from `AccessMode`, `FieldTargetNode` is a DU, and the 5-layer parser architecture is complete. Working docs are now audit trail, not pending design debt.

- Frank's vision-versus-spec audit found two live contradictions (`with` still listed as a structural preposition in the vision doc, and stale “root editability” wording after `write all` removal) and concluded the vision doc should not be archived until its language-identity material is migrated into the spec.

---

### 2026-04-29T00:43:25Z: Parser remediation review batch approved and synchronized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)



**Merged sources:** frank-audit-cross, frank-parser-review, frank-parser-v2-authored, soup-nazi-parser-coverage.



- Parser remediation slices R1-R6 are architecturally approved against the v8 catalog-driven parser design: top-level dispatch is catalog-owned, rule/state/event routing now flows through slot machinery where intended, preposition disambiguation is metadata-driven, and the cleanup removed the unauthorized header comment.

- The permanent parser reference is now authored in docs/compiler/parser-v2.md. It captures the catalog-driven dispatch model, 5-layer architecture, full 12-node declaration hierarchy, OmitDeclaration separation, FieldTargetNode DU, validation pyramid, and expanded parser diagnostics.

- Cross-surface consistency was re-aligned before the review closed: 8 inconsistencies were fixed across the spec, parser reference, slot comments, and token metadata so secondary sources match catalog-first primaries.

- Coverage for the 6 remediation slices is approved at 2034/2034 passing tests. The audit fixed the stale ConstructSlotKind count, replaced the obsolete StateDeclaration slot-count assertion with an exact slot-shape fact, and added EventDeclaration_HasInitialMarkerSlot as the new catalog regression anchor.

---

### 2026-04-28T06:41:30Z: Access-mode vocabulary locked and catalog fix landed

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (13 files)



**Merged sources:** `copilot-directive-2026-04-28T02-14-omit-vs-access-semantic`, `copilot-directive-2026-04-28T02-41-readonly-editable-vocab`, `copilot-directive-when-position`, `copilot-directive-writable-adjective`, `copilot-directive-writeable-spelling`, `frank-rule-7-closed`, `frank-vocab-B1`, `frank-vocab-B2`, `frank-vocab-B3`, `frank-vocab-B4`, `george-accessmode-guard-slot-fix`, `george-parser-complexity-reeval`, `george-parser-complexity-when`.



- Shane locked the access-mode surface as `in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?`, with `omit` preserved as the separate structural-exclusion verb. Earlier B1-B4 exploratory vocabulary rounds now collapse to this canonical surface; `->` and adjective-only forms are not the language.

- Durable semantic framing is now explicit: `omit` removes the field from the state's structural schema, while access modes keep the field present and only constrain mutability. Access-mode guards stay post-field, and the writable/writeable spelling debate is superseded by the locked `readonly`/`editable` pair.

- Implementation follow-through is locked: the access-mode body shape is verb + field target + access adjective + optional guard, catalog/token work needs `modify`, `readonly`, and `editable`, and the `AccessMode` disambiguation family is now `modify`/`omit` rather than the retired `read`/`write`/`omit` set.

- George's follow-through landed: `ConstructKind.AccessMode` now ends with `SlotGuardClause`, `DiagnosticCode.RedundantAccessMode` has catalog metadata, the stale `write all` description is removed, a regression test pins guard-slot presence/position, and the suite stayed green at 1809 passing tests.

---

### 2026-04-28T05:08:10Z: Access-mode and parser-design inbox batch canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (14 files)



**Merged sources:** `copilot-directive-2026-04-28T01-08`, `copilot-directive-extensibility-validation-20260427`, `frank-access-mode-design-round`, `frank-access-mode-docs-working`, `frank-r5-validation-layer-20260428`, `frank-r7-implementation-plan`, `frank-redundant-access-mode`, `frank-spec-grammar-fixes`, `george-access-mode-docs-working`, `george-access-mode-feasibility`, `george-lang-simplify`, `george-r4-parser-design`, `george-r6-review`.



- Access-mode design round locked the durable shape: guarded `read` is only valid as a writable-baseline downgrade, guarded `omit` stays prohibited because it would make structural field presence data-dependent, and the vocabulary remains `read` / `write` / `omit`. Frank's and George's working docs are the durable references for the reasoning behind those constraints.

- Redundancy handling is now uniform: dead named-field access declarations are compile errors under `RedundantAccessMode`, including `in S write F` on already-`writable` fields, unguarded `in S read F` on non-`writable` fields, and guarded `read` on non-`writable` fields. `RedundantGuardedRead` is retired; `omit` and broadcast `all` forms remain exempt; rule 7 is still open.

- Parser extensibility direction is validation-first, not generator-first: fail loudly when catalog metadata is incomplete, keep `_slotParsers` exhaustive, give rule bodies their own `RuleExpression` slot, keep `ensure` and `because` separate, and reject pre-event `when` on `from ... on` with a diagnostic instead of silently expanding the language surface.

- Design-loop status is now explicit: the v7 parser working doc remains the implementation-plan anchor, while language-simplification proposals were recorded as analysis input and only owner-approved surface changes should be treated as canonical.

---

### 2026-04-28T04:49:58Z: `write all` removed from language — stateless precepts use `writable` modifier

**By:** Shane (owner directive)

**Status:** Applied



**Merged sources:** `copilot-directive-write-all-removed`.



- `write all` is removed from the Precept language entirely. Stateless precepts now opt into mutability only through field-level `writable`; there is no root-level bulk access mode construct.

- This supersedes any earlier record that `write all` survived as stateless sugar.

- Stale references called out in the inbox covered the spec, vision doc, working docs, samples, and token/tooling vocabulary that still described root-level bulk access as live syntax.

- Canonical follow-through: language docs, samples, and downstream tooling must all treat field-level `writable` as the only stateless mutability opt-in.

---

### 2026-04-28T00:00:00Z: Combined Design v2 Structural Revision

**By:** Frank

**Status:** Applied



- Applied boundary reassessment: replaced "hard line / nothing crosses" claim with correct type dependency direction rule; clarified what crosses the lowering boundary.

- Readability/genre fixes: 13 stage-contract tables converted to labeled prose, two artifact tables merged, "How to read this document" added, §8 split, §9 moved to appendix, decision lead-ins added, problem statement added to §1, assertions moved to doc spine.

- No content dropped; all facts, contracts, and assertions preserved. Comparative tables retained where genuinely comparative.

- Motivation: Shifted from reference spec to design doc genre, making decisions and rationale explicit and readable.

---

### 2026-04-28T00:00:00Z: Combined Design v2 Gap Patch Complete

**By:** Frank

**Status:** Complete



- Added 10 missing design specifics to combined-design-v2.md: action-shape model, constraint activation indexes, constraint evaluation matrix, constraint exposure tiers, proof strategy enumeration, proof/fault chain formula, earliest-knowable kind assignment, named anti-patterns, compile-time vs lowered artifact table, implementation action items.

- Locked: three action shapes, precomputed constraint activation, closed proof strategies, explicit proof/fault chain ownership, five implementation action items.

- No philosophy gaps surfaced; all changes are implementation domain only.

---

### 2026-04-27T00:00:00Z: MCP dual-surface operating model canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)



**Merged sources:** `frank-mcp-operating-model`, `newman-mcp-dual-surface`, `soup-nazi-mcp-validation`, `copilot-directive-2026-04-26T11-13-50-367-04-00`.



- Repo-root `.mcp.json` is the Copilot CLI repo-local surface; `.vscode/mcp.json` remains the VS Code/workspace-local surface; `tools/Precept.Plugin/.mcp.json` remains the shipped/distribution payload.

- The authoritative repo-local development behavior stays source-first via `node tools/scripts/start-precept-mcp.js`. Client-specific files are projections/adapters, not separate contracts.

- The `github` MCP entry is intentionally **not** mirrored into repo-root `.mcp.json`; Copilot CLI provides GitHub MCP natively.

- Directly related docs were updated in the same change (`CONTRIBUTING.md`, `.github/copilot-instructions.md`, `tools/Precept.Plugin/README.md`, `.squad/skills/architecture/SKILL.md`), and the stale `docs/ArtifactOperatingModelDesign.md` reference is retired in favor of `tools/Precept.Plugin/README.md`.

- Validation rerun passed: all three MCP config surfaces parse cleanly, schemas stay separated (`mcpServers` for CLI/plugin, `servers` for VS Code), and no directly related stale live reference remains.

- Team pattern locked: dual-surface config work is only considered landed when the config artifact and at least one directly related doc land together.

---

### 2026-04-26T15:48:53Z: Analyzer expansion plan and catalog conventions canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (10 files)



**Merged sources:** `coordinator-analyzer-implementation-plan`, `coordinator-analyzer-queue-priority`, `coordinator-catalog-audit-findings`, `coordinator-catalog-conventions`, `coordinator-post-infra-analyzer-analysis`, `copilot-directive-2026-04-26T-catalog-lexer`, `frank-cross-catalog-invariants`, `george-cross-catalog-api-design`, `soup-nazi-analyzer-test-infra`, `soup-nazi-analyzer-test-plan`.



- The April 26 catalog audit now splits cleanly into **fixed now** vs **follow-up work**. Fixed in-session: `Period` gained `EqualityComparable`; qualifier modeling now reflects the full `in`/`of` system with exclusivity rules; `DisplayName` is required/populated for surfaced types; `UsageExample` is populated for surfaced types. Deferred follow-up: `TokenMeta.ValidAfter`, catalog-driven language-server completions, and the rest of the analyzer sweep.

- Canonical analyzer scope is 53 statically checkable invariants (37 cross-catalog, 16 intra-catalog) across 11 analyzers `PRECEPT0007`–`PRECEPT0017`; `PRECEPT0018` is dropped because Tokens is a leaf and exhaustiveness is already covered by `PRECEPT0007`.

- Shared analyzer infrastructure is now the center of gravity: `CatalogAnalysisHelpers.cs` plus a multi-source `AnalyzerTestHelper` overload. Test stubs stay minimal, avoid Frozen/Immutable BCL dependencies, and identify catalogs by class name rather than file path.

- Constructor parameters are the canonical way to express optional catalog metadata. `init`-only metadata properties on catalog records are now explicitly rejected because they create a second analyzer extraction path.

- Queue/order dedupe: the earlier simple-patterns-first plan is superseded by Shane's later directive to front-load the trait↔operation consistency path because it builds reusable switch-walker and enum-resolution infrastructure for the rest of the analyzer suite.

- Soup Nazi's test-plan bar stands: helper tests plus analyzer suites total about 298 cases, with the accepted blind spot limited to spread elements inside shared static arrays and guarded by declaration-site validation/regression anchors.

- Owner directive stands: lexer token classification must converge on fully catalog-driven behavior; implementation tactics may vary, but the architectural target is no-exceptions catalog authority.

---

### 2026-04-26T00:00:00Z: Catalog completeness, consumer drift, and analyzer sprint merge

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (7 files)



**Merged sources:** `frank-catalog-audit-findings`, `george-analyzer-expansion`, `george-lexer-scan-tables`, `soup-nazi-catalog-review-2`, `soup-nazi-catalog-baseline-tests`, `soup-nazi-analyzers-complete`, `soup-nazi-precept0007-proposal`, plus the coordinator's consolidated action list.



**Catalog audit outcome (Frank + George):**

- Surfaces are complete: the type catalog covers all surfaced language types. Frank's spec/design pass found 26 of 26 surfaced types represented; George's code-level inventory confirmed no missing surfaced type entries.

- `Period` is a real cross-catalog correctness bug, not a completeness gap. The spec and operations catalog permit equality, but `Types.cs` does not mark `Period` as `EqualityComparable`.

- `Quantity` qualifier modeling remains an open Shane decision. The current `QualifierShape` model expresses conjunctive qualifier slots well, but the language surface allows `quantity in 'kg'` OR `quantity of 'length'`, which is an alternative shape rather than a simple slot list.

- `TypeMeta.UsageExample` is still null across the catalog. This is not a correctness issue, but it keeps hover and MCP grounding thin.



**Doc-sync findings for `docs/catalog-system.md`:**

- The document must reflect `TypeTrait.EqualityComparable`.

- The documented `TypeMeta` full shape must include `DisplayName`, `HoverDescription`, and `UsageExample`.

- `QualifierAxis` documentation must include the temporal-dimension axis.

- `TypeMeta.Token` must stay nullable (`TokenMeta?`) for internal/sentinel types.

- The documented orderable set is stale: `zoneddatetime` should not be listed as orderable, and `price` should be.



**Consumer drift and enforcement status:**

- Consumer drift, not catalog structure, is now the dominant problem. George confirmed 14 hardcoded language-server completion lists still bypass the catalog contract.

- Lexer operator/punctuation scan tables now derive from `Tokens.All`, removing a parallel vocabulary table from `Lexer.cs` while preserving the hand-written scanner.

- Soup Nazi's catalog baseline pass added dedicated Tokens and Diagnostics catalog tests; all new tests were green.

- Soup Nazi's earlier finding that PRECEPT0005/PRECEPT0006 were missing is now superseded: both analyzers are implemented and green.

- PRECEPT0005 immediately caught a real production bug: the `sqrt` overload proof requirements referenced different `ParameterMeta` instances than the overload parameter list. The fix introduced dedicated named sqrt parameters shared by both `Parameters` and `ProofRequirements`.

- PRECEPT0007 remains a follow-up proposal: flag `Enum.GetValues<CatalogEnum>()` outside the owning catalog `All` getter. There are zero current source violations; test projects remain exempt.



**Backlog concentration after deduplication:**

- The consolidated review's correctness and metadata-gap items are recorded as completed in the source/design pass.

- Remaining work is concentrated in tooling-generation drift, broader analyzer expansion (PRECEPT0007-PRECEPT0014), snapshot/golden catalog tests, and generated matrix coverage.

---

### 2026-05-18T00:25:00Z: README DSL Hero Image Width Contract

**By:** Elaine (UX), Kramer (Tooling), with Frank's sizing analysis preserved

**Status:** Applied



The README DSL hero remains an image-based branded treatment, but it must now be sized against GitHub's actual repo-view image ceiling instead of the wider article frame.



**Decision:**

- Keep the README DSL hero as an image for now

- Regenerate/capture it at **1660px** source width from an **830px** viewport at **2×** device scale

- Treat **830px** as the effective GitHub repo README image display cap for this asset

- Tune the rendered code text for about **13px** apparent size at display

- Spend any extra composition room on whitespace rather than on additional contract width

- Preserve `design/brand/capture-hero-dsl.mjs` as the repeatable regeneration path



**Tradeoffs and retained learning:**

- Native README text/fenced code remains the only fully robust way to keep DSL text scaling in lockstep with surrounding prose across viewport and zoom changes.

- GitHub page-geometry research still matters: the repo shell tops out around **1280px** and the README/article frame around **1012px**, but the displayed README image for this treatment clamps earlier at about **830px**.

- Do not rely on custom CSS, sanitizer-sensitive HTML, or viewport-specific image swapping as a stable README contract.
