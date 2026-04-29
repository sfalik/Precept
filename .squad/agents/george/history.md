## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: led feasibility passes for analyzer/runtime details, parser guardrails, catalog-consumer drift, and diagnostic exhaustiveness discipline.

## Learnings

- The highest implementation payoff comes from eliminating hardcoded parallel copies of catalog knowledge while keeping parser/checker mechanics explicit and hand-authored.
- Exhaustiveness invariants need compile-time or pinned-test enforcement: `BuildNode` switch arms, diagnostic metadata, and slot ordering all need dedicated guards.
- Permanently-locked language invariants require both structural tests and invalid-input diagnostic tests; one without the other leaves a silent gap.
- Disambiguation and recovery rules must name the real mechanism; misleading tests or prose around sync anchors create implementation drift.
- Contract alignment across docs, code, diagnostics, and samples is a prerequisite for trustworthy slice work.
- Catalog metadata additions require `None=0` sentinel values on new enums when tests need to assert "non-default" membership — the sentinel distinguishes intentionally-set values from zero-initialized defaults.
- `#pragma warning disable CS8524` is the right suppression for fully-named exhaustive switches (not CS8509): CS8509 fires for non-exhaustive coverage, CS8524 fires for unnamed enum integer values; suppressing CS8524 while letting CS8509 fire is correct when guarding against new named members.
- When adding required positional parameters to shared catalog record types, all call sites (including those using named arguments after the positional block) must be updated. Grep for `new ActionMeta(` and `new ConstructMeta(` before touching signatures.
- `IReadOnlyList<T>` does not expose `.ToFrozenDictionary()` directly — use `IEnumerable<T>` extension via the same reference (it works because `IReadOnlyList<T>` is `IEnumerable<T>`).

## Recent Updates

### 2026-04-28 — Catalog extensibility implementation (PR #138)
- Implemented all 7 slices of the catalog extensibility plan (v3, approved by Frank) on branch `feature/catalog-extensibility`.
- Slice 1: `ExpressionBoundaryTokens` now derives from `Constructs.LeadingTokens` + structural literals — no more hardcoded construct-leading tokens in parser.
- Slice 2: `BuildNode` wildcard removed; CS8509 now fires on missing arms; `#pragma disable CS8524` suppresses the unnamed-integer variant.
- Slice 3: `RoutingFamily` enum added to `ConstructMeta`; all 12 catalog entries populated; `None=0` sentinel enables default-initialization test.
- Slice 4: `ActionSyntaxShape` enum added to `ActionMeta`; `Actions.ByTokenKind` FrozenDictionary enables O(1) token→meta lookup; `None=0` sentinel.
- Slice 3b: Both `DisambiguateAndParse` switches made exhaustive: `null =>` catches ambiguous tokens; explicit wrong-family throw arms catch routing bugs.
- Slice 5: `ParseActionStatement` refactored to two-level CS8509: outer switch on `SyntaxShape`, inner switch on `ActionKind` per shape — 4 helper methods.
- Slice 6: `TokenMeta.IsAccessModeAdjective` flag added; `Tokens.AccessModeKeywords` derived set; `ParseAccessModeKeywordDirect` uses catalog instead of hardcoded `is TokenKind.Readonly or TokenKind.Editable`.
- All 2044 tests pass across 3 commits.

### 2026-04-28 — Access-mode migration and shorthand sync
- Confirmed the B4 migration: `Modify`, `Readonly`, `Editable`, and separate `OmitDeclaration` landed cleanly across catalog, samples, and tests.
- Synced the shorthand/AST direction: shared `FieldTarget` shapes stay available to both `modify` and `omit`, while `omit` remains guardless and structurally separate.

### 2026-04-28 — v8 review cycle closed
- george-4 reviewed `docs/working/catalog-parser-design-v8.md` and blocked on 4 concrete issues: missing omit guard diagnostic coverage, unspecified pre-stashed guard handling when routed to `OmitDeclaration`, unclear sync-anchor mechanism wording, and an underspecified 2.1 slice split.
- frank-5 applied all requested fixes. george-5 re-reviewed the targeted areas, verified each fix, and approved v8.
- Phase 1 is complete; proceed to Phase 2 with the corrected v8 document as the canonical parser-design anchor.

