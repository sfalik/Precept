## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.
- Action syntax work must stay metadata-first: slot roles are typed catalog values, separator tokens derive from slot metadata, and optional slots replace ad-hoc support flags.

## Learnings

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

## Recent Updates

### 2026-05-10T20:56:42Z — Track 2 slices 4/9/10/11 durably recorded
- George-6's commit `df874e15` established `OperatorMeta.ResultType` / `ResultTypePolicy` as the catalog authority for operator result typing, including the `OperationResult` handoff to `OperationMeta.Result` for arithmetic.
- George-7 finished t2-9 in `b7868d60` and `2f75c829`: TypeChecker now consumes operator typing metadata directly, tightened adjacent choice/quantifier/modifier typing, and closed BUG-002,003,007,009,010,028,029,038,040,046,052,053 at 3,925 / 3,925.
- George-8 finished t2-10 in `def91dbb` and `b08b1fc4`: wildcard/broadcast name resolution is catalog-derived, computed fields bind via stable topological ordering with cycle diagnostics, and BUG-001,026,030,037 closed at 3,911 / 3,911.
- George-9 finished t2-11 in `004e68be`, `e48c0071`, and `599206b6`: proof obligations now project by proof-site metadata, collection mutation diagnostics bind real field names, 5 new proof tests landed, and the transient shared-tree binder failures were superseded by George-7's clean full run.

### 2026-05-10T19:47:35Z — Grammar doc-fix commits and validation recorded
- George-5 durably closed the grammar/spec/catalog documentation batch in commits 9b8e8384 and b8e7df94, covering the precept-grammar.md correction pass plus the removal of illegal trailing-ensure EventHandler grammar and obsolete SupportsPostActionEnsure documentation.
- The squad ledger, orchestration log, and this history now agree on the batch boundary, so follow-up doc work should cite the committed artifacts rather than the deleted inbox notes.
- Final validation stayed green across all four test projects at 4,388 passing tests.

### 2026-05-10T15:34:08Z — Slice 2E and Slice C closeouts recorded
- Scribe merged both your t2-2 Slice C note and your Slice 2E BUG-049a completion into `.squad/decisions.md`, with BUG-049a paired to Frank's approved design review as one canonical closeout entry.
- Durable implementation rules now recorded: shape-method separators come from `Actions.GetShapeMeta(...).Slots[n].PrecedingSeparator`, and intrinsic non-negative accessor returns discharge through `FixedReturnAccessor.ReturnNonnegative` while action cardinality obligations reuse the single shared `Types.CollectionCountAccessor`.
- Validation anchors now captured in the ledger: Slice C stayed green at 4056/4056 on `ef6fedcb`; Slice 2E closed targeted build + tests at 3857 passing on `f2d1dece` and `e826e4bd`.

### 2026-05-10T09:53:14Z — t2-2 Slice C: shape method body rewire
- George-6 completed Slice C by replacing hardcoded separator tokens in all 7 affected shape methods: `ParseAssignValueAction`, `ParseCollectionIntoAction`, `ParseCollectionValueByAction`, `ParseInsertAtAction`, `ParseRemoveAtIndexAction`, `ParsePutKeyValueAction`, `ParseCollectionIntoByAction`.
- Each method calls `Actions.GetShapeMeta(ActionSyntaxShape.X).Slots[n]` and reads `PrecedingSeparator!.Value` for `Expect()` and `PrecedingSeparator` for optional `if (Peek().Kind == slot.PrecedingSeparator)` guards. No hardcoded `TokenKind.By`, `TokenKind.At`, `TokenKind.Into`, or `TokenKind.Assign` remain in any shape method body.
- Expression-terminator lambdas in intermediate expression slots (`CollectionValueBy`, `InsertAt`, `PutKeyValue`) are also slot-driven.
- 6 new tests in `ActionChainTests.cs`: 3 behavioral (Insert, Dequeue±into, Put) + 3 catalog-property (AppendBy slots, CollectionIntoBy slots, Dequeue without into).
- Validation: 4056/4056 tests (3841 Precept.Tests + 156 LS + 59 MCP). Commit: `ef6fedcb`.
- t2-2 (BUG-021 / BUG-048 / BUG-049) fully closed across all three slices.

### 2026-05-10T09:53:14Z — t2-2 Slice B: ParseActionTarget shape-specific separators
- `ParseActionTarget` now accepts `FrozenSet<TokenKind> separators` from `Actions.GetShapeMeta(meta.SyntaxShape).SeparatorTokens`; old hardcoded `{=, into, by, at}` union is gone.
- `ParseActionByShape` computes separators once from catalog; all 9 shape methods receive and forward it.
- 8 new tests in `ParseActionTargetTests.cs`: 4 catalog property tests + 4 behavioral parser tests.
- Validation: 4050/4050 tests (3835 Precept.Tests + 156 LS + 59 MCP). Commit: `fb525df0`.

### 2026-05-10T13:53:14Z — t2-2 Slice A complete with typed operand roles
- Shane's scope ruling is now durable: no deferrals inside the slice, operand roles are in scope now, `ActionSyntaxSlot.Role` must be `ActionSlotRole`, and `IntoSupported` is removed rather than preserved beside slot metadata.
- George-3 added `ActionSyntaxSlot`, `ActionShapeMeta`, pre-computed `SeparatorTokens`, and exhaustive `Actions.GetShapeMeta()` coverage for all 9 shapes, with slot metadata replacing the old into flag.
- George-4 finished the typed-role cleanup: `ActionSlotRole` is 1-based for PRECEPT0018, `CollectionIntoBy` now uses `OrderingCapture` for the dequeue-by output slot, and MCP/test consumers now read `ActionSlotRole.IntoTarget` directly.
- Validation stayed green at 4322 total tests across Precept, MCP, LanguageServer, and Analyzers.

### 2026-05-10T13:53:14Z — Scribe handoff for t2-2 Slice B
- George-5's Slice B result is now recorded in `.squad/decisions.md` and the orchestration/session logs, so future parser separator work should use the canonical ledger entry rather than the transient inbox note.
- Durable implementation guidance is unchanged: shape-specific separator sets come from `Actions.GetShapeMeta()`, and `CollectionValueBy` / `RemoveAtIndex` remain parser-unreachable via `Actions.ByTokenKind`, so behavioral coverage for those shapes still belongs outside direct parser dispatch tests.
