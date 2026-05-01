## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, and parser whitespace-insensitivity implementation while keeping catalog truth primary.

## Learnings

- Spec grammar optionality markers (`?`) must match parser enforcement exactly. If the parser requires a token (e.g., `because` in ensure declarations), the spec grammar must not mark it optional — the spec is the contract that documentation consumers and future implementers read first.


- Eliminate hardcoded parallel copies of catalog knowledge; derive parser/checker behavior from metadata whenever the behavior is part of the language contract.
- Exhaustiveness guarantees need explicit compiler enforcement (`#pragma warning disable CS8524` + no wildcard arms) plus pinned regression tests.
- Use `None = 0` only for real structural sentinels; otherwise make named enum members 1-based so zero-initialization fails loudly.
- Shared catalog signature changes require a full constructor-call audit before compiling.
- Qualifier parsing needs two gates: confirm the type even supports qualifiers, then use catalog-derived lookahead for ambiguous prepositions.
- Whitespace-insensitivity belongs at the trivia-stripping boundary, not in ad hoc parser escape hatches.
- Multi-qualifier scalar types should be modeled as immutable collections, not stacked nullable singletons.
- Parser-facing type properties that reflect durable language truth belong in catalog traits (`TypeTrait.ChoiceElement`), not hand-maintained token lists.
- A slice is only complete when docs, diagnostics, tests, and samples all still agree on the contract.
- Record signature changes require a construction-site audit across the ENTIRE file including dead-code exhaustive switch arms — not just the live parse methods. BuildNode's exhaustive switch creates silent obligations whenever a record gains a constructor parameter.
- New expression AST nodes placed in `src/Precept/Pipeline/SyntaxNodes/Expressions/` should use namespace `Precept.Pipeline.SyntaxNodes` (not the `.Expressions` sub-namespace) — the physical folder organization is finer-grained than the namespace structure.
- When mirroring interpolated string parsing for a new token family, the only changes needed are the three token kind references (Start/Middle/End) and the return type — everything else (loop structure, `InterpolationPart` DU reuse) is identical.
- Multi-token postfix operators (`is set`, `is not set`) do NOT belong in `OperatorKind`/`Operators.All` because `OperatorMeta` has a single `Token` field and `Arity` has no `Postfix` value. Handle them directly as special cases in the Pratt led loop, placed BEFORE the `OperatorPrecedence.TryGetValue` check (same pattern as member-access `.`).
- The `TokenKind.Is` token is not in `ExpressionBoundaryTokens` (not a construct leading token), but it IS silently swallowed by the `OperatorPrecedence.TryGetValue` fallthrough-break if no handler is placed before it. Any new led-handler for a token that isn't in `OperatorPrecedence` must be inserted between the `Dot` handler and the `OperatorPrecedence.TryGetValue` guard.
- `dotnet build ... -q` can mask the distinction between a "Question build" diagnostic (informational) and actual compilation errors. Omit `-q` when a build is failing unexpectedly — filter the output with `Where-Object` instead so real errors are visible.
- `HandlesCatalogExhaustivelyAttribute` uses `AttributeTargets.Class`. If the annotation target is a `ref struct` (like `ParseSession`), you MUST expand the attribute target to `AttributeTargets.Class | AttributeTargets.Struct` first, or the C# compiler rejects the annotation.
- PRECEPT0019 as `DiagnosticSeverity.Error` with `TreatWarningsAsErrors=true` in Precept.csproj would block the build for every known gap. Use `DiagnosticSeverity.Warning` + `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` in Precept.csproj so gaps are visible but non-blocking. This is consistent with other cross-reference analyzers (PRECEPT0008, 0011, 0013, 0015, 0016) which also use Warning severity.
- The actual annotation target for pipeline-coverage enforcement is the inner `ref struct ParseSession`, not the outer `static class Parser`. PRECEPT0019 (`RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType)`) works on any `INamedTypeSymbol` including ref structs.
- TokenKind uses `NumberLiteral` (not `IntegerLiteral`/`DecimalLiteral`) and `True`/`False` (not `BooleanTrue`/`BooleanFalse`). No `Null` token exists. Always verify token names against `TokenKind.cs` before writing catalog metadata.

### 2026-05-01 — Extended plan technical design authored

- Confirmed Option B full DU is the correct shape: `abstract record OperatorMeta` base + `sealed record SingleTokenOp` (adds `TokenMeta Token`) + `sealed record MultiTokenOp` (adds `IReadOnlyList<TokenMeta> Tokens` + `TokenMeta LeadToken => Tokens[0]` convenience accessor). Frank's Option C flat-record extension is rejected because it creates an ambiguous `LeadToken` accessor that conflates "the one token" with "the first of several tokens" — the DU makes the distinction structurally impossible to violate.
- Enumerated all 5 call sites that will become compile errors under the DU: `Operators.cs` ByToken construction, `Parser.cs` OperatorPrecedence construction, and 3 test sites in `OperatorsTests.cs`. All mechanical `.OfType<SingleTokenOp>()` additions — no logic changes.
- `ByToken` multi-token design: recommend `ByTokenSequence(params TokenKind[] tokens)` method backed by `FrozenDictionary<(TokenKind, TokenKind?, TokenKind?), OperatorMeta>` for 3-token max sequences. The Pratt dispatch loop does NOT use this — it already handles multi-token ops by explicit token consumption. `ByTokenSequence` is a post-parse catalog lookup API for type checker, evaluator, and tooling consumers.
- GAP-A: `StateEnsureNode` and `EventEnsureNode` ALREADY have `Expression? Guard` — NO AST changes needed. Only `ParseStateEnsure` and `ParseEventEnsure` need updating (add `if (guard is null && Current().Kind == TokenKind.When) { Advance(); guard = ParseExpression(0); }` before `Expect(TokenKind.Because)`).
- GAP-B: Dedicated `ParseFieldDeclaration` override (bypassing slot system) is the right fix. Must explicitly call `Expect(TokenKind.As)` before `ParseTypeRef()` — the slot system did this via `ParseTypeExpression`, but the dedicated method must handle it directly.
- GAP-C: Only `TokenKind.Min` (55) and `TokenKind.Max` (56) are keyword-tokenized member names. `count`, `length`, `sum` are NOT keywords — they lex as `Identifier` and work already. The fix is surgical: `ExpectIdentifierOrKeywordAsMemberName()` helper + `KeywordsValidAsMemberName: FrozenSet<TokenKind>` static field, used only in the `Dot` handler of `ParseExpression`.
- PRECEPT0019 flip to Error is safe after Work Item A completes AND the `[HandlesForm(ExpressionFormKind.PostfixOperation)]` annotation is added to `ParseExpression`. Do NOT add `[HandlesCatalogExhaustively]` to unimplemented pipeline stubs (TypeChecker, GraphAnalyzer, Evaluator) yet — it would immediately fire PRECEPT0019 for all 11 forms with no implementation.
- Recommended slice order: D (GAP-A) → E (GAP-B) → F (GAP-C) → A+B (DU catalog) → C (severity flip). Do gap fixes first — they are independent of catalog changes and unblock all 7 broken sample files fastest.
- `OperatorFamily.Presence = 5` should be added alongside `Membership = 4` for `is set`/`is not set` — semantically distinct from collection membership (`contains`). Presence operators test value absence/presence on optional fields; membership operators test element inclusion in collections. The distinction enables clean tmLanguage scoping.

## Recent Updates

### 2026-05-01 — Slice 2: EventHandlerNode PostConditionGuard (GAP-2)
- Added `Expression? PostConditionGuard` to `EventHandlerNode` record in `src/Precept/Pipeline/SyntaxNodes/EventHandlerNode.cs`.
- Extended `ParseEventHandlerWithGuardCheck` in `Parser.cs`: after the action-loop exits, checks for `TokenKind.Ensure`, advances past it, calls `ParseExpression(0)` for the guard, passes it to the `EventHandlerNode` constructor. `Ensure` was already in `StructuralBoundaryTokens`, so expression parsing inside actions correctly stops at `ensure` — no boundary set change needed.
- Fixed `BuildNode` `EventHandler` arm to pass `null` as 4th argument (PostConditionGuard) — this was the Risk #1 compilation blocker.
- Risk #1 reality check: the plan doc referred to "StateEnsure and EventEnsure arms" but the actual compilation breakage was only in the **EventHandler arm** (which constructs `EventHandlerNode`). `EventEnsureNode` and `StateEnsureNode` were not changed in this slice. The `EventEnsure` arm's pre-existing `default!` for Message is a separate pre-existing issue not caused by this change.
- DSL operator convention confirmed: `and`/`or` (not `&&`/`||`). Verified against `TokenKind.cs` before writing test expressions.
- Updated spec §2.2 "Stateless event hook" grammar to document `("ensure" BoolExpr)?` post-condition clause.
- 4 new tests in `ParserTests.cs`: `Parse_EventWithEnsureGuard`, `Parse_EventWithEnsureGuard_ComplexExpr`, `Parse_EventWithoutEnsureGuard`, `Parse_EventWithEnsureGuard_AndActions`.
- Final count: 2189 Precept.Tests + 235 Analyzers.Tests = 2424 total. Zero regressions. Committed as `1b6c5ea`.

### 2026-05-01 — Slices 5 & 6: ListLiteralExpression + MethodCallExpression (GAP-6 & GAP-7)
- Created `src/Precept/Pipeline/SyntaxNodes/Expressions/ListLiteralExpression.cs` — `ListLiteralExpression(Span, ImmutableArray<Expression> Elements)`.
- Added `case TokenKind.LeftBracket: return ParseListLiteral();` to `ParseAtom()` with `[HandlesForm(ExpressionFormKind.ListLiteral)]`. Handles empty list, single/multi elements, and trailing comma gracefully.
- Created `src/Precept/Pipeline/SyntaxNodes/Expressions/MethodCallExpression.cs` — `MethodCallExpression(Span, Expression Receiver, string MethodName, ImmutableArray<Expression> Arguments)`.
- Added `LeftParen` Pratt led handler in `ParseExpression()` after the `Is` handler and before the `OperatorPrecedence.TryGetValue` guard (binding power 90). When `left is MemberAccessExpression`, extracts receiver = `left.Object`, method = `left.Member.Text`. Added `[HandlesForm(ExpressionFormKind.MethodCall)]` to `ParseExpression()`.
- Dead-code `IdentifierExpression` arm preserved with `// unreachable: identifiers resolve as FunctionCall in ParseAtom` comment per plan Risk #2.
- PRECEPT0019 is now **fully green** — all 10 ExpressionFormKind members covered, zero warnings.
- 9 new tests: 5 ListLiteral + 4 MethodCall in `ExpressionParserTests.cs`.
- Final count: 2168 Precept.Tests + 235 Analyzers.Tests = 2403 total. Zero regressions. Committed as `a6d9e64` (Slice 5) and `f619878` (Slice 6).
- Key pattern reinforced: Pratt led handlers for tokens not in `OperatorPrecedence` must be inserted before the `TryGetValue` guard, not after it.

### 2026-05-01 — Slice 4: ExpressionFormKind catalog + annotation bridge activated
- Created `src/Precept/Language/ExpressionForms.cs` — ExpressionFormKind (10 members, 1-based), ExpressionCategory enum (4 members), ExpressionFormMeta record, ExpressionForms static class with exhaustive GetMeta switch and All flat list.
- Added `"ExpressionFormKind"` to `CatalogAnalysisHelpers.CatalogEnumNames` — activates PRECEPT0007 exhaustiveness checking for the new catalog's GetMeta switch.
- PRECEPT0019: Changed DiagnosticSeverity from Error to Warning; added `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` to Precept.csproj so known gaps (GAP-6 ListLiteral, GAP-7 MethodCall) surface as visible warnings without blocking the build.
- `HandlesCatalogExhaustivelyAttribute`: Extended AttributeTargets from `Class` to `Class | Struct` to support `ref struct ParseSession`.
- Parser.cs `ParseSession`: Added `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`; annotated `ParseExpression` (MemberAccess, BinaryOperation) and `ParseAtom` (Literal, Identifier, Grouped, UnaryOperation, Conditional, FunctionCall).
- PRECEPT0019 is GREEN for 8 implemented forms; RED (warning) for GAP-6 (ListLiteral) and GAP-7 (MethodCall). TypeChecker, GraphAnalyzer, and Runtime.Evaluator not yet annotated — all are unimplemented stubs, adding the class marker to them would fire PRECEPT0019 for all 10 forms with no signal value.
- Added 25 tests in `ExpressionFormCatalogTests.cs` and 4 in `Precept0019Tests.cs`.
- Final count: 2394 total (2159 Precept.Tests + 235 Analyzers.Tests). Zero regressions. Committed as `f42f074`.


- Created `IsSetExpression` and `IsNotSetExpression` AST nodes in `src/Precept/Pipeline/SyntaxNodes/Expressions/`, namespace `Precept.Pipeline.SyntaxNodes`.
- Added a Pratt led handler for `TokenKind.Is` in `ParseExpression`'s while loop, inserted before the `OperatorPrecedence.TryGetValue` guard (same position as the `Dot`/member-access handler). Binding power 60. Consumes `is [not] set` and returns the appropriate node.
- Neither `IsSet` nor `IsNotSet` were added to `OperatorKind`/`Operators.All` — the `OperatorMeta` record has a single `Token` field and `Arity` has no `Postfix` value, making catalog entries unclean for multi-token postfix operators.
- Added 4 tests in `ExpressionParserTests.cs`: `ParseExpression_IsSet`, `ParseExpression_IsNotSet`, `ParseExpression_IsSet_InCondition`, `ParseExpression_IsNotSet_InCondition`.
- Fresh build total: 2134 passing. Zero regressions. Committed as `4a041c2`.


- Created `TypedConstantExpression` (simple typed constant) and `InterpolatedTypedConstantExpression` (interpolated form) AST nodes in `src/Precept/Pipeline/SyntaxNodes/Expressions/`.
- Added `case TokenKind.TypedConstant` and `case TokenKind.TypedConstantStart` to `ParseAtom()` in `Parser.cs`.
- Added `ParseInterpolatedTypedConstant()` mirroring `ParseInterpolatedString()` exactly, reusing `InterpolationPart` DU.
- Added 4 tests in `ExpressionParserTests.cs`: simple (`'USD'`), date (`'2026-04-15'`), interpolated (`'Hello {name}'`), and field-default form.
- Final count: 2111 passing (baseline 2107 + 4 new). Zero regressions. Committed as `7248dbf`.

### 2026-05-01 — Annotation bridge plan follow-up noted
- Scribe recorded the generic annotation-bridge decision in the canonical ledger while George's plan update remains pending.
- Carry-forward expectation: plan/docs should use `HandlesCatalogExhaustively(typeof(T))` as the class marker and keep PRECEPT0019 generic rather than ExpressionFormKind-specific.
- Inbox processing for this batch is complete; only plan synchronization remains outstanding.

### 2026-05-01 — Parser-gap plan audit and coverage design synchronized
- Updated `docs/working/parser-gap-fixes-plan.md` to add Slice 13 (`ExpressionFormCoverageTests`), move `LeadTokens` onto `ExpressionFormMeta`, and register `ExpressionFormKind` for PRECEPT0007 coverage in `CatalogAnalysisHelpers`.
- Locked the annotation bridge into Slice 4: `HandlesFormAttribute` + PRECEPT0019 now ship with the catalog and annotate `Parser`, `TypeChecker`, `Evaluator`, and `GraphAnalyzer`, while Slice 13 remains the parser-routing assertion layer.
- Remaining plan hygiene items to carry forward: add `src/Precept/Language/Operators.cs` to Slice 3 inventory and fix the dead `frank-expression-form-catalog-placement.md` reference.


### 2026-05-01 — Frank's parser-gap plan reviewed (APPROVED WITH CONCERNS)
- Confirmed all method signatures, line numbers, token IDs, and Pratt loop topology are accurate.
- Found critical compilation blocker: `BuildNode`'s dead-code arms for `StateEnsure` (line 1553) and `EventEnsure` (line 1576) construct the records directly and will break compilation when GAP-2 adds `PostConditionGuard`. Must update both arms in the same commit as the record changes.
- Flagged dead code in GAP-7: `left is IdentifierExpression` in the Pratt `LeftParen` handler is unreachable because `ParseAtom()` eagerly consumes `identifier(args)` before the loop runs. Spec-literal but functionally dead.
- Flagged existing tests (`WSI_Integration_InsuranceClaim`, `WSI_Integration_LoanApplication`) that currently accept diagnostics due to GAP-2/GAP-3 — these must be retrofitted to assert zero diagnostics after fixes land.
- Minor: `contains` chaining test missing (should emit `NonAssociativeComparison` diagnostic); Slice 11 duplicates `SamplesDir` infrastructure rather than extending existing `ParserTests.cs` section.
- Rule reinforced: shared record signature changes require a full construction-site audit across the ENTIRE file (including dead-code exhaustive switch arms), not just the live parse methods.
- Rule reinforced: A slice is only complete when docs, diagnostics, tests, and samples all still agree on the contract.

### 2026-05-01 — GAP-1/2/3 parser analysis recorded
- Inbox analysis on `ParseAtom()`, inline `ensure ... when ...`, and multi-token presence operators was merged into `.squad/decisions/decisions.md`.
- Durable implementation sequence captured: GAP-2 immediately, GAP-1 simple typed constants immediately, GAP-3 after catalog/AST shape sign-off.

### 2026-05-01 — WSI parser slices 2–5 recorded
- Deleted the dead `SkipTrivia()` path and removed `NewLine` from `StructuralBoundaryTokens`, making parser structure explicitly trivia-free.
- Rewrote `ParseTypeRef` for multi-qualifier scalar types, with `AmbiguousQualifierPrepositions` derived from catalog metadata rather than hardcoded token tables.
- Added `TypeTrait.ChoiceElement` and derived `ChoiceElementTypeKeywords` from catalog truth.
- Validation recorded at 2310 passing tests.

### 2026-04-29 — PRECEPT0018 correctness gate closed
- Follow-up commit `e7a643d` added the 5 required regression anchors from Frank's blocked review without changing analyzer behavior.
- Durable closeout rule: when a reviewer names missing spec IDs, backfill by spec ID rather than by matching only the requested count.

### 2026-04-28 — Catalog extensibility and enum-safety hardening
- Landed catalog-driven parser routing/action-shape metadata, exhaustive CS8509 switch enforcement, and access-mode keyword derivation.
- Landed PRECEPT0018 analyzer enforcement so semantically meaningful zero-valued enum members are either explicit sentinels, `[Flags]`, or annotated exemptions.
