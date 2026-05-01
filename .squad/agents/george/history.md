## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, parser whitespace-insensitivity, typed constants, event-handler ensure guards, presence-operator Pratt support, the expression-form catalog/annotation bridge, list literals, method calls, and the sample/coverage regression layer.
- Current ownership: Slice 27 is the structural `Parser.cs` split — preserve behavior while moving the parser into partial files with the outer `Parser` class holding shared static vocabulary.

## Learnings

- Spec grammar, parser enforcement, docs, tests, and samples must all agree before a slice is considered complete.
- Durable language truth belongs in catalogs/metadata; avoid hardcoded parallel tables in parser or tooling code.
- Shared record or catalog signature changes require a full construction-site and call-site audit, including dead-code exhaustive switch arms.
- Pratt handlers for tokens that are not in `OperatorPrecedence` must be inserted before the `TryGetValue` guard or they are silently swallowed.
- Use compiler/analyzer exhaustiveness intentionally: CS8509 for switch coverage, PRECEPT0007 for catalog metadata, PRECEPT0019 for handler coverage.
- `HandlesCatalogExhaustivelyAttribute` must allow `Struct` targets for `ref struct` handlers, and `ref struct` types still cannot own static fields.
- Verify real `TokenKind` names before writing metadata or tests; `dotnet build -q` can hide actionable diagnostics during failure analysis.
- Multi-token presence operators are a proposal-scale catalog completeness gap, while keyword member names like `.min` / `.max` need dedicated parser handling in the `Dot` path.

## Recent Updates

### 2026-05-01 — Slice 27 parser split decision received
- Frank locked Slice 27 to `partial class Parser` + `partial ref struct ParseSession` with three files: `Parser.cs`, `Parser.Declarations.cs`, and `Parser.Expressions.cs`.
- Keep `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` only on the primary `ParseSession` declaration; let `[HandlesForm(...)]` stay attached to the moved methods in `Parser.Expressions.cs`.
- Shared static vocabulary, including the future `KeywordsValidAsMemberName` set, must stay on the outer `Parser` class because `ref struct` types cannot declare static fields.

### 2026-05-01 — Parser-gap branch state summarized
- Branch work through Slice 13 is durably recorded: typed constants, event-handler ensure guards, presence-operator Pratt support, expression-form catalog/coverage, list literals, method calls, spec fixups, and the regression suites from Slices 8–13 are all in place.
- Remaining known-broken sample sentinels still point at three separate gaps: state/event ensure `when` guards, post-expression field modifiers, and keyword member names (`.min` / `.max`).
