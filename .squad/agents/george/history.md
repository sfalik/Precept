## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.
- Action syntax work must stay metadata-first: slot roles are typed catalog values, separator tokens derive from slot metadata, and optional slots replace ad-hoc support flags.

## Learnings

- `CollectionValueBy` (AppendBy, EnqueueBy) and `RemoveAtIndex` (RemoveAt) are "secondary" action shapes: their `PrimaryActionKind` is non-null, so they are excluded from `Actions.ByTokenKind`. The parser NEVER directly dispatches to these shapes via `ByTokenKind`. Their parse methods exist in the switch but are unreachable from the normal action chain parse path. Tests for these shapes must be catalog property tests, not behavioral parser tests. Behavioral coverage requires the type-checker conversion path.
- When propagating shape-specific context through shape-method signatures, the cleanest approach is to compute the FrozenSet once at the dispatch site (ParseActionByShape) and pass it as a parameter to every shape method. This avoids re-computing per call site and keeps the shape methods unaware of the catalog lookup.
- `ParseExpression`'s `terminates()` lambda is checked in the WHILE loop (after ParseNud), not before ParseNud. ParseNud runs unconditionally on the current token. This means the termination set only matters for preventing led-loop continuation, not for gating the initial expression parse. For tokens with no binary-operator led binding power (=, into, by, at), the while loop breaks naturally via GetLedBindingPower = -1. The old hardcoded termination was redundant for the outer while loop but the design fix is still correct because it documents shape-specific intent and prevents future breakage if these tokens gain binary-operator roles.

- `ActionSlotRole` must be a typed enum, not a string on `ActionSyntaxSlot.Role`. Freeform strings on catalog records are always wrong — the catalog is the source of truth and its values must be first-class types.
- Project analyzer PRECEPT0018 requires explicit 1-based enum values. New semantic enums must declare `= 1` on the first member and renumber accordingly.
- `CollectionIntoBy`'s final slot is an output capture variable, so the durable role name is `OrderingCapture`, not `OrderingKey`.
- When removing a flag field (`IntoSupported`) that is equivalent to derived slot metadata, keep the downstream DTO stable by deriving the old surface from `GetShapeMeta()` instead of preserving duplicate state.
- `CollectionValue` and `CollectionValueBy` both have positional value slots with `PrecedingSeparator = null`; slot well-formedness should validate first-slot/null and `SeparatorTokens` consistency, not require separators on every later slot.

## Historical Summary

- Earlier 2026-05-09 and 2026-05-10 work completed the typed-literal system, enriched diagnostics/quickstart/syntax catalogs, added `TypedField.NameSpan` and `ArgReference`, landed outline/snippet/catalog metadata, shipped the Track 2 Phase A safe batch, renamed the value-modifier family, and closed the TokenMeta alias cleanup plus BUG-039 documentation follow-through.
- Durable chronology, rationale, and commit anchors live in `.squad/decisions.md`; this history keeps only the live implementation guidance George needs for the next slices.

## Recent Updates

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
