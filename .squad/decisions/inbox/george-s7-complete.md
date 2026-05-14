# George — Slice 7 Complete: Parser Guard Gates (PRE0013–0015)

**Date:** 2025-07-14
**By:** George (Runtime Dev)

## What Was Wired

Three invalid-guard-position rejection paths in `ParseScopedConstruct`:

1. **PRE0013 `OmitDoesNotSupportGuard`** — Post-slot detection: after OmitDeclaration finishes parsing `in State omit Field`, if next token is `when`, emit diagnostic and skip to construct boundary.

2. **PRE0014 `EventHandlerDoesNotSupportGuard`** — Post-slot detection: after EventHandler finishes parsing `on Event` (ActionChain returns sentinel because no `->` follows), if next token is `when`, emit diagnostic and skip to construct boundary.

3. **PRE0015 `PreEventGuardNotAllowed`** — In-loop detection: within the slot iteration for TransitionRow, after StateTarget is parsed (i > 0) but before disambiguation `on` is consumed, if `when` appears, emit diagnostic, skip the guard expression tokens until the `on` token is found, then continue normal parsing.

## Implementation Approach

All three gates are in `ParseScopedConstruct` rather than in separate construct-specific methods (the parser is catalog-driven with no per-construct parse methods). The approach:
- TransitionRow: pre-disambiguator gate inside the slot loop (must fire before `on` consumption)
- OmitDeclaration + EventHandler: post-loop gate (fires after all slots are parsed)

A new `SkipGuardExpression(FrozenSet<TokenKind> stopTokens)` helper advances tokens until a stop token (disambiguation token), construct boundary, or EOF is reached.

## Recovery Behavior

- OmitDeclaration/EventHandler: `SkipToConstructBoundary()` — skips everything after `when` until the next construct leader, allowing the rest of the file to parse cleanly.
- TransitionRow: `SkipGuardExpression(disambTokens)` — skips only the guard expression, stopping at `on` so the transition row continues parsing its event target, guard-after-event, actions, and outcome.

## Allow-List

Removed all three codes from `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` Gate 1 allow-list.

## Anomalies

None. All 6 new tests pass. All existing parser tests (ParserScopedConstructTests, ParserCoverageGapTests, and full Parser test directory) pass with no regressions. The 23 pre-existing failures in the workspace are in TypeChecker/ProofEngine tests unrelated to this change.
