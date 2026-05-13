## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.
- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.

## Live Guidance

- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.
- State-target widening must keep compatibility getters for legacy single-name consumers and use `ToImmutable()` when deduplicating expanded rows.
- `PopulateEditDeclarations` still drops omit-state context today; any omit/access-mode unification must resolve `StateTarget` first instead of assuming state survives normalization.
- In a shared worktree, stage exact files only; never rely on broad adds when multiple agents may be editing nearby surfaces.

## Historical Summary

- 2026-05-12 closed a dense runtime streak: proof/qualifier repairs, temporal denominator support, comma-list `StateTarget` rollout, and hover V1 contract cleanup all landed across the checker and language server.
- Older batch-by-batch detail now lives in `history-archive.md` and `.squad/decisions.md`; this live file keeps the durable implementation posture plus the newest outcomes other agents are likely to need immediately.

## Recent Updates

### 2026-05-13T00:26:25Z — Circular Tokens/Types static-init crash is closed

- `Tokens.KeywordsValidAsMemberName` now defers its `Types.All` read through `Lazy<FrozenSet<TokenKind>>`, eliminating the `Tokens..cctor()` ↔ `Types..cctor()` re-entry crash.
- George confirmed the public API surface stayed unchanged and the repository test pass closed green at `4996/4996`.

### 2026-05-12T23:20:42Z — Soup Nazi re-review approved the comma-list closeout

- Commit `53d68d51` closed B1-B5 plus N1 with parser AST coverage, stronger expansion and guard-clone assertions, multi-unknown-state fan-out coverage, and the corrected `4969` core-test count in the spike doc.
- Commit `cf3c6a81` replaced the `ResolvedStateTarget` null-sentinel with explicit `IsWildcard`; Soup Nazi re-reviewed both commits and approved them with `0 blockers / 2 good findings`.

### 2026-05-12T23:02:04Z — B1 proof-hover blocker fixes are the current hover baseline

- Commit `c2a38a56` moved qualifier/proof hovers onto the compact-card contract, locked the shared badge vocabulary to `✅ Proven`, `⚡ Enforced`, and `⚠️ Gap`, and normalized shipped copy from `proved` to `proven`.
- Validation closed green at `41/41` `HoverHandlerTests` and `269/269` full language-server tests; the remaining repo-wide `dotnet test` failure is the unrelated multi-unknown-state baseline in `TypeCheckerTransitionTests`.

### 2026-05-12T22:18:18Z — Comma-list `StateTarget` shipped end to end

- Parser, type checker, normalization, diagnostics, docs, and samples now all reflect the shipped state-list subset: `Identifier ("," Identifier)* | any`.
- The remaining formal follow-up is structural polish, not viability: slot cardinality metadata, comment coverage for compatibility fallbacks, and test-count reconciliation.

### 2026-05-12T13:52:04Z — Same-qualifier proof fixes stabilized PRE0114 behavior

- Commit `d187230c` added `QualifierMatch.Same` to the six same-qualifier arithmetic operations and taught PRE0114 diagnostics to show recursive expression labels plus resolved qualifier values.
- Companion message work in `1d8962f7` locked the clearer proof wording without widening the proof metadata surface; George validated the combined path against the full suite at `5507/5507`.

### 2026-05-13T00:08:20Z — George's B2/B3 and B4 hover work is now the approved baseline

- Commit `47f3068c` fixed Frank's B2/B3 blockers by reordering construct routing ahead of generic hover help and making mutability summaries explicitly omit-aware.
- Commit `a6bf789f` introduced `EdgeProofStatus`, `EnrichGraphWithProofStatus`, and the 📍 state-hover proof narrative so graph-edge proof posture is visible on the rich state card.
- Frank's final approval sweep closed the hover program with `279/279` language-server tests and `4973` core tests green.
## Learnings

- **Tokens ↔ Types circular static init:** `Tokens.GetMeta(TokenKind.StringType)` (called during `Types..cctor()`) triggers `Tokens..cctor()`, which then reads `Types.All` (null — not yet assigned) via `KeywordsValidAsMemberName`. Fix: `Lazy<FrozenSet<TokenKind>>` on `KeywordsValidAsMemberName` defers the `Types.All` access until after both static constructors complete.



- Proof diagnostics are most robust when qualifier facts are resolved at emission time rather than baked into preformatted operand strings.
- Pure-copy state-list expansion should resolve shared payload once, then fan out per source-state name while preserving per-name spans and undeclared-state diagnostics.
- `TypedInterpolatedTypedConstant` qualifier work must treat compound-unit literals as numerator/denominator slot pairs; denominator-only fallbacks silently lose information.
- 2026-05-12 modifier applicability catalog gaps closed: `ZeroBoundNumericTypes` now includes `Price` and `ExchangeRate`; `RangedNumericTypes` adds `Price` only (ExchangeRate excluded — ordering undefined); new `BusinessMagnitudeTypes` array (`Decimal`, `Money`, `Quantity`, `Price`, `ExchangeRate`) replaces `DecimalOnly` for `Maxplaces`; `DecimalOnly` removed. TypeChecker.Validation applicability guard now skips when modifier is already in `impliedModifiers`, so identity types (`currency`, `unitofmeasure`, `dimension`) + `notempty` emit only `RedundantModifier`. All 4969 tests green, zero test flips.

### 2026-05-12T23:50:08Z — Modifier applicability gaps closed with final suite health

- Commit `a727dddb` widened modifier applicability for `price`, `exchangerate`, and business-magnitude `maxplaces`, and collapsed identity-type `notempty` handling to redundancy-only by skipping implied modifiers during applicability validation.
- Coordinator follow-up repaired the remaining price qualifier fixture and `ModifiersTests` catalog-drift expectations, so the final repository result closed at `4995/4995` tests passing.

### 2026-05-13T01:03:07Z — Catalog static-init constraint documented

- Updated `docs/language/catalog-system.md` to harden Frank's required static-initialization constraint paragraph after the catalog initialization guidance.
- The documentation now records that reverse `Tokens` → downstream catalog static references must use `Lazy<T>`; `Tokens.KeywordsValidAsMemberName` remains the current concrete example.

### 2026-05-13T03:56:26Z — Slice 9 OR/disjunction support landed cleanly

- Commits `c2d5b8fb` and `32da6a3e` closed the proof-engine disjunction slice: branch-aware OR splitting now drives Strategy 3 / Strategy 4, ensure guards survive normalization, and guarded ensures no longer leak as unconditional facts.
- Validation closed green at `5118/5118` tests passing.
- Frank's initial pre-commit concerns were all satisfied by the committed final state, so Slice 9 is the current baseline for disjunctive guard reasoning.
