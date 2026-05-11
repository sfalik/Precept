## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings.
- Pushes for full-surface coverage matrices, honest red/green pressure, and regression anchors that match the real AST/runtime shape.

## Learnings

- Real static catalogs are the executable language contract; prefer tiny synthetic fixtures around them over mocked metadata.
- Sample-file integration tests catch parser and language-surface gaps that isolated unit tests miss.
- Language-server completion regressions must use the real trigger character (`'`, space, or invoked-with-empty-trigger) or the suite can accidentally exercise the wrong surface.
- Qualifier-aware completion tests need both positive assertions and exclusion assertions; ranking is not a hard filter.
- Selection-range, highlight, and reference coverage should derive expected spans from real compilation artifacts instead of hand-counted columns.
- Span-heavy regressions often need full compiler fixtures so parser, binder, semantic-token, and analyzer seams all surface together.
- If a full-feature battery passes immediately, that is an honest finding: the implementation was already complete and the value is in locking behavior, not manufacturing red tests.
- Modifier and domain-type follow-up gaps should be recorded as explicit passing anchors when the shipped implementation intentionally preserves pre-existing behavior.

## Historical Summary

- 2026-05-01 through 2026-05-09 established the durable posture: convert review findings into shipped tests, keep sample-file gates live, and use real-catalog fixtures for parser/type-checker/analyzer coverage.
- The canonical decision ledger in `.squad/decisions.md` carries the full batch chronology; this history keeps the lasting testing rules and the most recent high-value closeouts.

## Recent Updates

### 2026-05-11T01:38:51Z — Span-refactor fallout batch restored suite health
- Test helpers now construct the refactored `MemberAccessExpression` shape correctly, qualified arg semantic sites stay anchored to the full `Event.Arg` span, and overlapping LS navigation resolves arg references before event references.
- The graph-warning fixture now asserts `StructuralSinkState` for no-terminal flows, and the full suite closed green at 5,085 / 5,085.

### 2026-05-11T00:27:07Z — Track 2 slices 14 and 15 are locked by coverage
- Catalog-capability suites now fail if operators, outcomes, modifiers, types, or diagnostics drop required metadata.
- Parser, binder, and MCP pipeline-stage regression suites landed 88 new tests, keeping the metadata-driven execution path honest.

### 2026-05-10T23:40:33-04:00 — Typed-literal battery proved the implementation was already complete
- Added 22 test methods / 24 cases in `CompletionHandlerTests.cs` across type branching, slot routing, qualifier filtering, compound temporal flow, and invoked recovery.
- Honest outcome: all tests passed immediately, so the value of the batch was locking the shipped behavior and its real trigger-character seams.

### 2026-05-10T(late) — Money/quantity modifier regression suite closed green
- Added 14 regression anchors for domain-type modifier legality, typed-constant bounds, invalid typed-constant content, and the two known follow-up gaps (qualifier alignment and plain-number acceptance).
- The full core suite stayed green and the passing-gap anchors now document exactly what a future uniform `default` / valued-modifier fix must change.
