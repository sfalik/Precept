## Core Context



- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.

- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.

- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.



## Learnings

### 2026-05-08T05:30:00Z — Slice 8: CI Enforcement shipped

- Commit: `00ef822`. Methods: `ValidateCIEnforcement`, `EnforceCIInExpression`, `EnforceCIInAction`, `IsCIExpression`, `IsContainsOperation`.
- DiagnosticCodes used: `CaseInsensitiveFieldRequiresTildeEquals` (66), `CaseInsensitiveFieldRequiresTildeNotEquals` (95), `CaseInsensitiveValueInCaseSensitiveContains` (96, dormant), `CaseInsensitiveFieldRequiresTildeStartsWith` (97), `CaseInsensitiveFieldRequiresTildeEndsWith` (98).
- CI tracking: `CheckContext.CIFields` and `CIElementCollections` HashSets populated during `PopulateFields` from `CITypeReference` checks. `TypedFieldRef.IsCaseInsensitive` now correctly set in `ResolveIdentifier`.
- Post-pass walks all resolved expression trees: field defaults/computeds, transition row guards/actions, event handler actions, rules, ensures.
- `contains` enforcement structurally correct but dormant — `IsContainsOperation` returns false until contains OperationKind entries land.
- 3242/3242 tests pass. No regressions.

### 2026-05-08T01:30:00Z — Slice 9: Quantifiers + List Literals shipped

- Commit: `54fa59b`. Methods: `ResolveQuantifier`, `ResolveListLiteral`. Stub arms promoted in `Resolve()` switch.
- DiagnosticCodes used: `InvalidQuantifierTarget` (102), `QuantifierPredicateNotBoolean` (106), `TypeMismatch` (18).
- TypedQuantifier: collection → GetElementType → push binding onto QuantifierBindings stack → resolve predicate (must be Boolean) → pop binding. Returns TypedQuantifier with ResultType=Boolean.
- TypedListLiteral: resolve each element → unify via bidirectional IsAssignable widening → return TypedListLiteral(List, unifiedElementType, elements). Empty lists → ElementType=Error.
- Existing stub test updated: `QuantifierExpression_Stub_ReturnsErrorExpression_NoDiagnostic` → `QuantifierExpression_NonCollectionTarget_EmitsInvalidQuantifierTarget`.
- 3242/3242 tests passing. No regressions.

### 2026-05-07T23:00:00Z — Slice 6: Structural Validation shipped

- Commit: `fe358ef`. Methods: `ResolvePostfixOp`, `ValidateStructural`, `DetectCycles`. Choice validation added inline to `PopulateFields`.
- DiagnosticCodes used: `IsSetOnNonOptional` (49), `EmptyChoice` (46), `DuplicateChoiceValue` (45), `CircularComputedField` (40), `DefaultForwardReference` (54). No new codes added.
- IsSet/IsNotSet: PostfixOperationExpression stub replaced with full resolution. Validates operand is optional field/arg; non-optional emits IsSetOnNonOptional.
- Choice validation: in PopulateFields, ChoiceTypeReference domain checked for empty (EmptyChoice) and duplicates (DuplicateChoiceValue).
- Computed field cycle detection: three-color DFS on ComputedDeps adjacency graph. O(n). Currently no-op (ComputedDeps empty until expression resolution wired).
- Forward-reference belt-and-suspenders: post-hoc check on ComputedDeps field ordering. Redundant with D8 in ResolveIdentifier.
- §13 Slice 6 scope followed — §14 explicitly excludes graph topology from TypeChecker.
- ValidateStructural wired into Check() after Pass 1. 3177 passing (baseline 3170). 19 pre-existing failures unchanged.

### 2026-05-07T21:00:00Z — Slice 4: TypedConstants + Context-Sensitive Resolution shipped

- Commit: `ac95de2`. Methods: `ResolveTypedConstant`, `ValidateContent`, `ValidateNodaTime`, `ValidateClosedSet`, `ValidateRegex`, `TryContextRetryBinaryOp`, `TryContextRetryOverload`.
- DiagnosticCodes used: `UnresolvedTypedConstant` (52), `InvalidTypedConstantContent` (53).
- ContentValidation DU (prerequisite C1) landed in same commit: `ContentValidation` abstract record → `RegexValidation`, `NodaTimeValidation`, `ClosedSetValidation` subtypes. Added `ContentValidation?` field to `TypeMeta`.
- Populated on: Date (NodaTime ISO), Time (NodaTime ExtendedIso), DateTime (NodaTime ExtendedIso), Period (NodaTime NormalizingIso), Currency (ISO 4217 ClosedSet), UnitOfMeasure (recognized units ClosedSet), Dimension (recognized families ClosedSet).
- Context threading: `expectedType` parameter added to `Resolve()` as `TypeKind? expectedType = null`. Callers provide target type for typed constant resolution and numeric literal context-sensitive widening.
- Context retry for binary ops: when bottom-up fails and one operand is a bare `LiteralExpression`, re-resolve with the other side's resolved type as `expectedType`. Handles `amount > 100` where `amount: money`.
- Context retry for function overloads: when no overload matches bottom-up, re-resolve literal args with each candidate parameter type. Handles `min(amount, 100)` where `amount: money`.
- NodaTime 3.x added as PackageReference to Precept.csproj.
- `ResolveNumericLiteral` now accepts `expectedType` — if integer widens to expectedType, literal resolves as that type directly.
- All 3126 tests pass (above 3075 baseline).

### 2026-05-07T20:15:00Z — Slice 3: Functions, Accessors, Interpolated Strings shipped

- Commit: `fa87df9`. Methods: `ResolveFunctionCall`, `ResolveCIFunctionCall`, `SelectOverload`, `ResolveMemberAccess`, `ResolveMethodCall`, `ResolveInterpolatedString`, `ResolveAccessorReturnType`, `GetElementType`, `IsAssignable`.
- DiagnosticCodes used: `UndeclaredFunction` (30), `InvalidMemberAccess` (20), `FunctionArityMismatch` (21), `TypeMismatch` (18).
- `Functions.FindByName(name)` returns `ReadOnlySpan<FunctionMeta>` — may contain multiple entries for same-name functions (e.g., "round" → Round + RoundPlaces with different arities).
- Overload scoring: arity filter first, then exact (score 0) vs widened (score = widen count). `IsAssignable` encapsulates identity + single-hop `WidensTo`. Lowest score wins; exact match short-circuits.
- CI function call: parser produces `CIFunctionCallExpression` with name sans tilde; type checker prepends `~` for `Functions.FindByName("~" + name)` lookup.
- Accessor return type resolution via DU: `FixedReturnAccessor.Returns`, `ElementParameterAccessor` → `TypeKind.Integer`, base `TypeAccessor` → owning field's `ElementType` via `GetElementType`.
- `GetElementType` only resolves `TypedFieldRef` receivers via `FieldLookup`; chained collection access returns null (acceptable for current surface).
- InterpolatedString: `TypedInterpolatedString` record hardcodes `TypeKind.String` as result; ErrorType propagation on any hole failure.
- All 3029 baseline tests pass. Untracked WIP test file (`TypeCheckerExpressionTests.cs`) exists and must be moved aside for builds.

### 2026-05-07T19:22:15Z — Slice 2: Scalar Expression Resolution shipped

- Commit: `1111da4`. Method: `Resolve(ParsedExpression, CheckContext)` with helpers: `ResolveLiteral`, `ResolveNumericLiteral`, `ResolveIdentifier`, `ResolveBinaryOp`, `ResolveUnaryOp`, `TryResolveBinaryWithWidening`, `DisambiguateCandidates`, `MapQualifierBinding`.
- DiagnosticCodes used: `UndeclaredField` (17), `DefaultForwardReference` (54), `TypeMismatch` (18).
- `Operations.FindCandidates(OperatorKind, TypeKind, TypeKind)` returns `ReadOnlySpan<BinaryOperationMeta>` — may contain >1 entry for qualifier-disambiguated operations (money/money, quantity/quantity division).
- `Operations.FindUnary(OperatorKind, TypeKind)` returns `UnaryOperationMeta?` — null if no match.
- TokenKind → OperatorKind mapping: `Operators.ByToken[(tokenKind, arity)]` → `OperatorMeta.Kind`.
- Widening algorithm: 4-level (exact → left widen → right widen → both). `Types.GetMeta(typeKind).WidensTo` gives targets. Single-hop only (D15). Only integer has widening targets: `[Decimal, Number]`.
- Qualifier disambiguation: multi-candidate → select `QualifierMatch.Same` by default. Sets `SameQualifierRequired` on TypedBinaryOp.ResultQualifier.
- Stub arms return `TypedErrorExpression` without diagnostic for unimplemented expression forms (Slices 3–9).
- All 2974 baseline + 47/55 Slice 1 tests pass. Same 8 pre-existing failures.

### 2026-05-07T19:30:00Z — Slice 1: Typed Symbol Population shipped

- Commit: `e882396`. Methods: `ResolveTypeKind`, `PopulateFields`, `PopulateStates`, `PopulateEvents`, `BuildPartialSemanticIndex`.
- DiagnosticCodes used: `TypeMismatch` (18), `NoInitialState` (32), `MultipleInitialStates` (31).
- Catalog field `TypeMeta.ImpliedModifiers` is the D3 source of truth for implied modifiers — it returns `ModifierKind[]` directly; no need to query the Modifiers catalog for this.
- `ParsedTypeReference` DU subtypes drive `ResolveTypeKind`: `SimpleTypeReference` → `.Type.Kind`, `CollectionTypeReference` → `.CollectionType.Kind` + recursive element resolution, `MissingTypeReference` → `TypeKind.Error`.
- Known upstream gap: `DeclaredArg` lacks modifiers/IsOptional; 8 new TypeCheckerSymbolTests fail for this + parser qualified-type errors + queue/log TypeKind ambiguity. All 2974 baseline tests pass.

- Closed-vocabulary slot data resolves at parse time: types, modifiers, access modes, and `because` text should not be deferred as raw spans.

- `peek(2)` is structural parser geometry for scoped-construct disambiguation because the anchor production is a single token.

- Optional construct slots still produce sentinel `SlotValue`s; downstream code should not assume omitted entries shrink the slot array.

- New parser metadata should expose the lookup axis the parser actually queries; branch-local linear scans are a smell.

- PRECEPT0019-style exhaustiveness is valuable when it is attached to the true owner of a catalog axis, not sprayed broadly.

- Additive extension points on shared value records should land as init-only properties when constructor stability matters; `Diagnostic.RelatedSpans` can grow the diagnostic payload without forcing churn across every `Diagnostics.Create(...)` site.

- Non-operator Pratt led precedence belongs in `ExpressionForms` metadata; `MemberAccess` should not survive as a parser-local binding-power constant.

- Expression-bearing construct slots can own their termination tokens, which lets slot-boundary behavior propagate from the Constructs catalog instead of bespoke parser lambdas.

- Multi-location diagnostics should carry per-location messages, but absence cases stay single-span: if no second concrete source location exists, keep `RelatedSpans` empty and let the primary diagnostic message explain the missing declaration.

## Recent Updates



### 2026-05-07T23:22:15Z — R0 blocker resolved for TypeChecker Slice 0

- George-12 renamed `TypedOutcomeKind` to `TransitionRowOutcome` in `SemanticIndex.cs` (`350f386`), closing Frank's only R0 blocker while preserving the 2974-test branch baseline.
- Slice 0's durable shape record now spans the initial shape commits `5260065` / `abf2532` plus the naming fix, so Slice 1 symbol-population work can proceed on the naming-correct baseline.

### 2026-05-07T08:42:03-04:00 — Bare `<-` parse defect fixed; `ExpressionFormKind` exhaustiveness enforced

- `ParseComputeExpression` now validates the token after `BackArrow` against a catalog-derived expression-start set; bare `<-` emits `ExpectedToken("expression")` and recovers structurally instead of synthesizing a fake compute expression.

- Added narrow `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` coverage on `ParserState` plus member annotations on the expression handlers; removing one annotation reproduced `PRECEPT0019`, proving the guard is live.

- Validation closed green: `dotnet build src\Precept\Precept.csproj --no-restore --nologo` and `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` passed for 2810/2810.



### 2026-05-07T08:18:45Z — BackArrow (`<-`) computed-field delimiter implemented

- Executed Frank's approved `<-` plan in commit `266ee5a`: added `TokenKind.BackArrow`, switched computed-field parsing to `<-`, and propagated the new delimiter through docs, samples, and tooling.

- Lexer support stayed catalog-derived; no dedicated lexer code path was required beyond the token addition.

- Batch validation at handoff: 2799 passing tests.



### 2026-05-07T08:05:00Z — ParsedOutcome DU implemented

- Executed Frank's approved plan in commit `94dec3b`: `OutcomeSlot` now carries `ParsedOutcome`, and malformed rows stay explicit via `MalformedOutcome` rather than falling back into unrelated expression nodes.

- This closes the synthetic-binary-expression outcome encoding defect while keeping the parser/type-checker boundary honest.



### Historical summary through 2026-05-07

- Earlier active-history detail was compacted to keep George under the 15 KB gate.

- The durable parser baseline remains: catalog-driven construct parsing, sentinel slot invariants, `EventHandler` without an Outcome slot, and parser-resolved closed vocabularies feeding `ParsedExpression` / `ParsedOutcome` rather than span-only placeholders.

- Use `.squad/decisions.md` for the full per-batch provenance trail and branch-level decision chronology.

### 2026-05-07T09:04:34Z — Parser implementation review recorded

- Reviewed `Lexer.cs`, `Parser.cs`, `Parser.Expressions.cs`, parser payload types, the language spec, and representative samples with a type-checker-readiness lens.

- Blocking findings: the parser drops interpolation hole expressions, type/action/modifier payloads are too lossy for checker work, and several invalid tails still collapse into placeholder `true` / silent sentinels instead of parse diagnostics.

- Catalog follow-up: `LeadingTokenSlot` is still unused, state/access modifier lookups are not fully catalog-derived, and dot/postfix led precedence remains partly hardcoded.





### 2026-05-07T18:28:05Z — Catalog metadata and diagnostic payload updates recorded

- Shipped parser metadata P4/P5: `ExpressionFormMeta.BindingPower` now owns member-access precedence and `ConstructSlot.TerminationTokens` now owns expression-slot boundary metadata.
- Shipped diagnostic payload P3: `Diagnostic.RelatedSpans` landed as an init-only additive extension backed by a `readonly record struct RelatedSpan`.
- Batch validation for the shipped George work closed green inside the 2949-test branch baseline; keep parser/checker follow-through catalog-derived and constructor-stable.



### 2026-05-07T18:51:59Z — Pre-Slice 0: TypeChecker shape committed

- All TypedXxx records, TypedExpression DU (14 subtypes), QualifierBinding DU, ActionSecondaryRole enum, TypedAction 3-shape DU, SemanticIndex full layout, CheckContext, and TypeCheckerTestHelpers committed.
- `TransitionOutcome` enum renamed to `TypedOutcomeKind` to avoid collision with `ParsedOutcome.TransitionOutcome` record.
- TypedDeclarations.cs stubs replaced with tombstone; all types migrated to SemanticIndex.cs.
- 2974/2974 tests passing after shape commit.
- TypedExpression subtypes created: TypedFieldRef, TypedArgRef, TypedLiteral, TypedBinaryOp, TypedUnaryOp, TypedFunctionCall, TypedMemberAccess, TypedConditional, TypedQuantifier, TypedInterpolatedString, TypedTypedConstant, TypedListLiteral, TypedPostfixOp, TypedErrorExpression.
- SemanticIndex primary arrays: Fields, States, Events, TransitionRows, Rules, Ensures, AccessModes, StateHooks, EventHandlers, EditDeclarations, ComputedDeps, ConstraintRefs, FieldReferences, StateReferences, EventReferences, Diagnostics.
- SemanticIndex secondary FrozenDictionary indexes: FieldsByName, StatesByName, EventsByName, EnsuresByState.
- CheckContext key fields: Fields/States/Events lists + lookups, CurrentEventArgs, CurrentFieldIndex, FieldScopeMode, QuantifierBindings stack, all declaration accumulators, Diagnostics list.
- Commits: 5260065 (shape), abf2532 (test helpers).

### 2026-05-07T18:51:59Z — H1 housekeeping: committed outstanding working-tree changes

- Working tree on Precept-V2-Radical had ~650 lines of uncommitted changes across 14 modified files and 7 new untracked files representing completed pipeline work.
- Contents committed (9 commits):
  - **Outcomes catalog** (`Outcomes.cs`, `OutcomesCatalogTests.cs`): OutcomeKind, OutcomeArgumentKind, OutcomeMeta as catalog #14.
  - **ParsedAction + ParsedTypeReference DU nodes** (`ParsedAction.cs`, `ParsedTypeReference.cs`, `ParsedExpression.cs`): full structural type-reference DU, action DU, MissingExpression sentinel, InterpolatedStringExpression with segment nodes.
  - **Parser enrichment** (`Parser.cs`, `Modifiers.cs`, `SlotValue.cs`, `ParsedConstruct.cs`, `ConstructManifest.cs`, + 4 test files): structured type parsing, CI-type prefix, action DU emission, interpolation parsing, Modifiers.ByStateToken, ConstructManifest.ByKind, ParsedConstruct slot helpers and IsComplete.
  - **Diagnostic payload** (`Diagnostic.cs`, `DiagnosticCode.cs`, `Diagnostics.cs`, `DiagnosticsTests.cs`, `diagnostic-system.md`): RelatedSpan struct, RelatedSpans init-only property, UndeclaredArg code 107.
  - **NameBinder stage** (`NameBinder.cs`, `SymbolTable.cs`, `Compiler.cs`, `Compilation.cs`, `TypeChecker.cs`, `NameBinderTests.cs`): two-pass binder wired into pipeline.
  - **Docs**: type-checker.md OQ1–3 locked; catalog-system.md ActionMeta.SyntaxShape and FunctionMeta CI fields.
  - **Squad housekeeping**: removed addressed inbox item; added pipeline-stage-design skill; updated Frank history.
- Commit SHAs: 1536d0c, 5f731f4, 3187bb5, e0dc066, 0592da3, 3c17e38, a8adcb5, a469217, 2337fd0
- Test count after all commits: **2974 passing, 0 failing**.
- Surprise: two additional modified files found beyond the 14/7 in the brief — `docs/language/catalog-system.md` and `.squad/agents/frank/history.md` were unstaged but in working tree; committed them as their own docs/chore commits.

### 2026-05-07T22:51:59Z — H1 housekeeping closeout recorded

- Scribe recorded George-9's nine committed housekeeping slices as the durable batch closeout and preserved the clean working-tree / 2974-test baseline.
- Frank-12's catalog doc sync was deduplicated into the same squad record because commit `a469217` was already part of the George-9 train.

### 2026-05-08T03:45:00Z — Slice 5+7 restoration + event arg ref fix
- **Problem**: Slice 6 commit (fe358ef) overwrote all of Slice 5's TypeChecker methods (PopulateTransitionRows, PopulateEventHandlers, NormalizeTransitionRow, NormalizeEventHandler, ResolveAction, ResolveActionTarget, ContainsErrorExpression, ContainsErrorExpressionInAction) and Slice 7's modifier validation methods (ValidateModifiers, ValidateFieldModifiers, IsTypeApplicable). BuildPartialSemanticIndex was also returning empty arrays instead of wiring CheckContext data.
- **Root cause**: Concurrent agent writes — Slice 6 was authored from a base that didn't include Slice 5/7 changes.
- **Prevention**: Always pull/merge before committing when parallel agents are running.
- **Secondary fix**: Qualified event arg references (EventName.ArgName in guards/rules) were emitting UndeclaredField instead of resolving as TypedArgRef. Added early check in ResolveMemberAccess per language spec §3.5.
- **Commit**: 4e1efd8
- **Test result**: 3196/3196 passing (26/26 TypeCheckerTransitionTests).

### 2026-05-08T04:30:00Z — Slice 10: Final assembly + D26 global assert
- **BuildSemanticIndex**: Replaced `BuildPartialSemanticIndex` with full `BuildSemanticIndex` — assembles all 16 ImmutableArray primaries and 4 FrozenDictionary secondaries (D4) from CheckContext.
- **D26 assert**: `Debug.Assert` at line ~2245 in `BuildSemanticIndex` — calls `ContainsAnyErrorExpression` which recursively walks all expression-bearing sites (Fields, Events args, TransitionRows, Rules, Ensures, AccessModes, StateHooks, EventHandlers) and their sub-expressions.
- **Last NotImplementedException removed**: `BuildSemanticIndex` was the final stub.
- **Full pipeline order**: PopulateFields → PopulateStates → PopulateEvents → PopulateTransitionRows → PopulateEventHandlers → PopulateRules → ValidateModifiers → ValidateStructural → ValidateCIEnforcement → BuildSemanticIndex.
- **Commit**: 844f00e
- **Test result**: 3294/3294 passing, 118 integration tests passing. TypeChecker implementation DONE.
