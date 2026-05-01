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

## Recent Updates

### 2026-05-01 — GAP-3: `is set` / `is not set` postfix operators implemented (Slice 3)
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
