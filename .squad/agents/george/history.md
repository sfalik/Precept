## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, and parser whitespace-insensitivity implementation while keeping catalog truth primary.

## Learnings

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

## Recent Updates

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
