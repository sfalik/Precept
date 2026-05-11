## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.
- Action syntax work must stay metadata-first: slot roles are typed catalog values, separator tokens derive from slot metadata, and optional slots replace ad-hoc support flags.

## Learnings

### 2026-05-11T09:32:39.453-04:00 — Pipeline audit fixes landed cleanly on feature/pipeline-audit-fixes

- Added repo-root `Directory.Build.props` so the workspace defaults to a single Release-with-PDB build surface.
- Eliminated every remaining `Debug.Assert` site in `src/Precept/` and converted D26/D5/mode-stack invariants to unconditional `InvalidOperationException` guards; the pipeline no longer has build-configuration-dependent safety nets.
- Closed the D26 diagnostic gaps by emitting `TypeMismatch` before returning `TypedErrorExpression`/error-typed `TypedAction` from operator lookup failures plus the `Resolve()` / `ResolveAction()` defensive fallbacks.
- `Fault.cs` already had the requested `ExpressionContext` and `InputValues` fields, so I verified the shape and added regression coverage instead of re-implementing it.
- The fix-plan's GraphAnalyzer loop assumed `TypedEvent.Modifiers`, but the current semantic model exposes only `TypedEvent.IsInitial`; I derived the active event modifier set from that surface before dispatching through `EventModifierMeta.RequiredAnalysis`, which preserves the intended invariant without widening the typed-event model mid-fix.
- Validation closed green with `dotnet build src/Precept/Precept.csproj -c Release` after each change group and a final `dotnet test test/Precept.Tests/ -c Release` run at 4,598 / 4,598 passing.

### 2026-05-11T00:08:48-04:00 — Text qualifier axis: design drafted, implementation blocked

- **System state found:** `QualifierAxis` has 9 members (None, Currency, Unit, Dimension, FromCurrency, ToCurrency, Timezone, TemporalDimension, TemporalUnit). All are single-value axes mapped to external catalogs. `text`/`String` type has `QualifierShape: null` — no qualifier support at all.
- **Design gate triggered:** Frank's V1/V2 scope ruling (`.squad/decisions/inbox/frank-typed-literal-completion-review.md`) explicitly deferred `text` qualifier-aware mode to V2, calling out "no current qualifier shape for `text`". No approved design exists for the DSL type-system change.
- **Key design tension:** `field Status as text in ['pending', 'active', 'closed']` requires the FIRST multi-value qualifier in the system. All existing qualifiers take a single `TypedConstant`: `in 'USD'`, `in 'days'`. A closed string set is structurally different and may require a `ParsedQualifier` discriminated union if bracket-list syntax is chosen. This is precedent-setting.
- **Design doc written:** `docs/Working/george-text-qualifier-design.md` covers syntax options (bracket-list vs repeated-single-value), `DeclaredQualifierMeta.TextValues` shape, parser change surface, type checker behavior, runtime enforcement question, Kramer's integration point, and Newman's MCP concern.
- **Inbox note:** `.squad/decisions/inbox/george-text-qualifier-design-needed.md` — implementation is blocked pending Frank + Shane sign-off on the design doc.
- **No implementation code written.** Design gate respected.

### 2026-05-10T15:38:30-04:00 — SupportsPostActionEnsure removed (BUG)
- Code commit: `c1572613`; test commit: `5be86341`. Final suite: 4,388 total (3,891 Precept.Tests, 280 Analyzers.Tests, 157 LS.Tests, 60 Mcp.Tests). Zero failures.
- `SupportsPostActionEnsure` was an out-of-band parser injection flag that grafted EventEnsure slot semantics (`ensure expr because reason`) onto EventHandler after the main slot-walk. This violated the `on`-family disambiguation contract: `ensure` and `->` are mutually exclusive routing tokens — the parser must never mix their semantics on a single construct.
- Removal pattern: delete the flag from `ConstructMeta`, remove it from the `EventHandler` catalog entry, delete the conditional post-slot-walk block in `ParseScopedConstruct`. Three tests asserting the deleted behavior were removed. No replacement — the behavior was wrong, not merely misphrased.
- This confirms the same principle as `SupportsPreVerbWhenGuard`: ad-hoc support flags on `ConstructMeta` are always wrong. If a construct needs extended parsing, that extension must be encoded as catalog-driven optional slots in the slot walk.

### 2026-05-10T15:32:08-04:00 — BUG-020 committed; full suite confirmed green
- All 6 BUG-020 commits landed cleanly on `Precept-V2-Radical`: core implementation (`b5dc7c3e`), tests (`ec068569`), grammar/spec/catalog docs (`eb225f8a`), samples (`4a6cb93f`), working docs (`103c3be1`), squad history (`078dbe32`).
- Final test count: 4,391 across Precept.Tests (3,894), Analyzers.Tests (280), LanguageServer.Tests (157), Mcp.Tests (60). Zero failures.
- The `SupportsPreVerbWhenGuard` removal confirms the pattern: optional pre-verb clauses should be catalog-driven optional slots in the slot walk, not ad-hoc flags on the construct record. The parser stays metadata-first.

- `CollectionValueBy` (AppendBy, EnqueueBy) and `RemoveAtIndex` (RemoveAt) are "secondary" action shapes: their `PrimaryActionKind` is non-null, so they are excluded from `Actions.ByTokenKind`. The parser NEVER directly dispatches to these shapes via `ByTokenKind`. Their parse methods exist in the switch but are unreachable from the normal action chain parse path. Tests for these shapes must be catalog property tests, not behavioral parser tests. Behavioral coverage requires the type-checker conversion path.
- When propagating shape-specific context through shape-method signatures, the cleanest approach is to compute the FrozenSet once at the dispatch site (ParseActionByShape) and pass it as a parameter to every shape method. This avoids re-computing per call site and keeps the shape methods unaware of the catalog lookup.
- `ParseExpression`'s `terminates()` lambda is checked in the WHILE loop (after ParseNud), not before ParseNud. ParseNud runs unconditionally on the current token. This means the termination set only matters for preventing led-loop continuation, not for gating the initial expression parse. For tokens with no binary-operator led binding power (=, into, by, at), the while loop breaks naturally via GetLedBindingPower = -1. The old hardcoded termination was redundant for the outer while loop but the design fix is still correct because it documents shape-specific intent and prevents future breakage if these tokens gain binary-operator roles.
- Slice 8 guard parsing needed disambiguation awareness: for `in/to/from` constructs with optional pre-verb `when`, the disambiguation keyword (`ensure`/`->`) may appear after an expression, so candidate selection must scan past the guard rather than relying on fixed `Peek(2)`.
- Typed constants were already valid expression atoms in the Pratt parser, but field modifier value parsing still hardcoded start tokens and skipped `TypedConstant`; using `ExpressionStartTokens` in modifier parsing fixed the leak and prevented stray top-level `ExpectedToken` failures.
- Forward references in computed expressions cannot be validated by declaration order alone. Allowing all-field resolution for computed formulas plus cycle diagnostics (`CircularComputedField`) is the stable contract; declaration-order enforcement remains appropriate for default expressions only.

- `ActionSlotRole` must be a typed enum, not a string on `ActionSyntaxSlot.Role`. Freeform strings on catalog records are always wrong — the catalog is the source of truth and its values must be first-class types.
- Project analyzer PRECEPT0018 requires explicit 1-based enum values. New semantic enums must declare `= 1` on the first member and renumber accordingly.
- `CollectionIntoBy`'s final slot is an output capture variable, so the durable role name is `OrderingCapture`, not `OrderingKey`.
- When removing a flag field (`IntoSupported`) that is equivalent to derived slot metadata, keep the downstream DTO stable by deriving the old surface from `GetShapeMeta()` instead of preserving duplicate state.
- `CollectionValue` and `CollectionValueBy` both have positional value slots with `PrecedingSeparator = null`; slot well-formedness should validate first-slot/null and `SeparatorTokens` consistency, not require separators on every later slot.
- Shape method bodies call `Actions.GetShapeMeta(ActionSyntaxShape.X)` directly and index into `Slots[n].PrecedingSeparator!.Value` for both required `Expect()` calls and optional `if (Peek().Kind == slot.PrecedingSeparator)` guards. This is the correct pattern — shape methods know their own shape identity, so calling `GetShapeMeta` inside is clean and allocation-cheap compared to the alternative of widening the parameter signature from `FrozenSet<TokenKind>` to full `ActionShapeMeta`.
- String literal `LiteralExpression.Text` is the raw lexed text WITHOUT surrounding quotes. `"Walk"` in source produces Text = `Walk`. Tests asserting on literal text must not include quote characters.
- Shared-environment MSBuild cache-file locks (`MSB3492: Could not read existing file`) are reliably cleared by deleting the offending `.cache` file manually (`Remove-Item`) before the build invocation. The lock is always stale (held by the VS Code language server OmniSharp/Roslyn process); deleting the file unblocks the build without killing any process.
- BUG-049a fix pattern: if a numeric accessor is structurally guaranteed non-negative, carry that fact on `FixedReturnAccessor.ReturnNonnegative` and let Strategy 2 discharge `>= 0` directly from accessor metadata. For collection counts, unify all action obligations on `Types.CollectionCountAccessor` (B1) instead of duplicating local accessor instances.
- `SupportsPreVerbWhenGuard` was pure duplicate metadata. Scoped constructs can encode pre-verb `when` support entirely by placing an optional `GuardClause` slot in the ordered slot list with construct-specific termination tokens (`Ensure`, `Arrow`, `Modify`).
- `ParseScopedConstruct` does not need phased anchor/guard/disambiguation handling. A single slot walk works if it checks for family disambiguation tokens before each post-anchor slot, preserves the `->` exception for `ActionChain`, and lets slot metadata drive everything else.

### 2026-05-10T16:59:02.8292215-04:00 — t2-4 OperatorMeta result typing landed
- `OperatorMeta` now carries `ResultType` and `ResultTypePolicy`; the old `StaticResultType` / `LookupValueType` / `ArithmeticPromotion` naming is gone. The durable policy set is `Fixed`, `LhsType`, `ElementType`, `BothOperands`, and `OperationResult`.
- Catalog assignments are now explicit: `or` / `and` declare `ResultType = boolean` with `BothOperands`; `not`, comparisons, `contains`, `is set`, and `is not set` declare `boolean` with `Fixed`; unary `-` uses `LhsType`; binary arithmetic uses `OperationResult`; `for` (`LookupAccess`) uses `ElementType` so t2-9 can read the lookup value type from the typed LHS metadata.
- `OperationResult` is the important shape decision for t2-9: arithmetic cannot be modeled as simple promotion because temporal and business-domain operator results come from `Operations.GetMeta(...).Result`, not from a primitive widen rule. Final validation for this slice: `dotnet test test/Precept.Tests/` green at 3,899 passing.

## Historical Summary

- Earlier 2026-05-09 and 2026-05-10 work completed the typed-literal system, enriched diagnostics/quickstart/syntax catalogs, added `TypedField.NameSpan` and `ArgReference`, landed outline/snippet/catalog metadata, shipped the Track 2 Phase A safe batch, renamed the value-modifier family, and closed the TokenMeta alias cleanup plus BUG-039 documentation follow-through.
- Durable chronology, rationale, and commit anchors live in `.squad/decisions.md`; this history keeps only the live implementation guidance George needs for the next slices.
- 2026-05-10T09:53:14Z — t2-2 Slice C: shape method body rewire: George-6 completed Slice C by replacing hardcoded separator tokens in all 7 affected shape methods: `ParseAssignValueAction`, `ParseCollectionIntoAction`, `ParseCollectionValueByAction`, `ParseInsertAtAction`, `ParseRemoveAtIndexAction`, `ParsePutKeyValueAction`, `ParseCollectionIntoByAction`.
- 2026-05-10T09:53:14Z — t2-2 Slice B: ParseActionTarget shape-specific separators: `ParseActionTarget` now accepts `FrozenSet<TokenKind> separators` from `Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens`; old hardcoded `{=, into, by, at}` union is gone.
- 2026-05-10T13:53:14Z — t2-2 Slice A complete with typed operand roles: Shane's scope ruling is now durable: no deferrals inside the slice, operand roles are in scope now, `ActionSyntaxSlot.Role` must be `ActionSlotRole`, and `IntoSupported` is removed rather than preserved beside slot metadata.
- 2026-05-10T13:53:14Z — Scribe handoff for t2-2 Slice B: George-5's Slice B result is now recorded in `.squad/decisions.md` and the orchestration/session logs, so future parser separator work should use the canonical ledger entry rather than the transient inbox note.
- 2026-05-11T01:38:51Z — Older recent-update entries were summarized into `history-archive.md`; keep this file focused on live guidance and the newest batch context.

## Recent Updates

### 2026-05-11T01:38:51Z — Terminal-state split closed with clean downstream validation
- George's C119/C108 graph-analyzer split is now the canonical implementation for terminal-state diagnostics; `DeadEndStateFact` keeps proof suppression semantics while vacuous no-terminal warnings stay gone.
- Soup Nazi's downstream test/handler fixes are part of the same durable batch boundary: `MemberAccessExpression` helpers now stamp both spans, arg-reference navigation again prefers `Event.Arg`, and the full suite closed green at 5,085 / 5,085.

### 2026-05-10T21:11:48-04:00 — Terminal-state diagnostic split implemented (StructuralSinkState + gated DeadEndState)
- Added `StructuralSinkState = 119` to `DiagnosticCode`; added full catalog entry in `Diagnostics.cs`.
- `GraphAnalyzer.Analyze()` now computes structural sinks (reachable, non-terminal, zero outgoing) first and fires C119 (Message A) unconditionally.
- `DeadEndState` (C108 / Message B) is now gated: only fires when `terminalStates.Length > 0`, and excludes structural sinks to prevent double-firing.
- `DeadEndStateFact` contains: BFS dead-ends when terminals exist; only structural sinks when no terminals exist. This preserves ProofEngine obligation suppression without false claims.
- Key invariant: structural sinks can never appear as `fromState` in proof obligations (no outgoing transitions), so removing them from the fact when terminals are absent is safe for the ProofEngine.
- Updated 2 existing `GraphAnalyzerTests` (`Analyze_DeadEndState_EmitsWarningAndDeadEndFact`, `Analyze_MultipleDeadEndStates_AllReported`) and added 2 new tests (`Analyze_NoTerminalStates_StructuralSinkFires_DeadEndDoesNot`, `Analyze_DeadEndState_WithOutgoingTransitions_FiresMessageB`).
- Pre-existing `TypeCheckerFunctionTests.cs` build errors (MemberAccessExpression missing Span parameter) are unrelated to this work — they exist in the working tree and were present before this task.

### 2026-05-11T00:27:07Z — BUG-057 temporal qualifier fix recorded
- Commit `2763a433` fixed `TypeChecker.ExtractQualifiers()` so `period of 'date'` / `period of 'time'` and `period in 'days'` qualifiers survive into semantic metadata instead of being dropped.
- Added temporal qualifier diagnostics (`InvalidTemporalDimensionString`, `InvalidTemporalUnitString`) plus 7 regression tests in `TypeCheckerSymbolTests`; the batch closes at 4,531 core tests and 105 MCP tests passing.

### 2026-05-10T23:55:32Z — BUG-057 routed to Slice 8; t2-16 plan updated
- George-7 narrowed BUG-057 to field-type/parser support for qualified `period` declarations, updated `precept-toolchain-bugs.md`, and wrote `george-bug057-slice-assessment.md` recommending Slice 8 as the first implementation home.
- George-6 appended the t2-16 DTO Source Generator slice spec to `precept-toolchain-plan.md`, so Track 2 now has an explicit generator-planning slice ready for implementation follow-through.
