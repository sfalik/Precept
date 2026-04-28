## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, access-mode redesign decisions, and parser catalog-shape direction.

## Learnings

- Conservative defaults are structural guarantees: write/edit surfaces open exceptions, they do not become the baseline by omission.
- Metadata belongs in catalogs when consumers need per-member knowledge; pipeline/tooling drift comes from hardcoded parallel copies.
- Parser algorithms stay hand-written, but vocabulary tables, precedence data, and disambiguation metadata should derive from catalog truth where possible.
- Authoring consumers read `CompilationResult`; execution/preview consumers read lowered `Precept`; runtime-native lowered data may intentionally preserve selected analysis residue.
- When a construct family splits, verification must cover catalog entries, AST nodes, BuildNode arms, routing tests, slot-order tests, and slice-level regression anchors.

## Recent Updates

### 2026-04-28 — Access-mode vocabulary and shorthand locked
- Final surface: `modify` verb, `readonly` / `editable` adjectives, `omit` as separate structural-exclusion verb, shared `FieldTarget` shorthand, and post-field guards only on `modify`.
- Live docs were swept so spec, vision, parser, catalog, runtime, and evaluator surfaces all reflect the same grammar.

### 2026-04-28 — Parser design v8 and review-cycle completion
- frank-4 authored `docs/working/catalog-parser-design-v8.md`, splitting `OmitDeclaration` from `AccessMode`, promoting `FieldTargetNode` to a DU, and updating the Phase 1 implementation slices.
- george-4 blocked v8 on 4 items: omit-guard diagnostic coverage, stashed-guard behavior on omit routing, sync clarification, and formalized 2.1a/2.1b split.
- frank-5 applied all 4 fixes; george-5 spot-checked and approved. Phase 1 is complete and ready to hand off to Phase 2.

### 2026-04-28 — Phase 4: parser-v2.md authored
- Created `docs/compiler/parser-v2.md` — the permanent canonical parser reference document, successor to `docs/compiler/parser.md`.
- Synthesized from parser.md (structure/format template), v8 design doc (catalog-driven architecture, disambiguation tables, FieldTargetNode DU, OmitDeclaration separation, validation tiers), and Constructs.cs (exact slot sequences).
- New sections vs. parser.md: AST Node Hierarchy (full 12-node hierarchy with DU rationale), Grammar Reference (all 9 forms), Slot Dispatch (InvokeSlotParser/BuildNode mechanisms), Validation Layer (4-tier pyramid), 5-Layer Architecture summary.
- Updated sections: Top-Level Dispatch (catalog-driven ByLeadingToken lookup), Preposition Disambiguation (3 candidates under In), Diagnostics (6 codes, +2 new), Sync-Point Recovery (LeadingTokens FrozenSet, not hardcoded list).
- Supporting artifacts: `docs/working/parser-v2-build-notes.md`, `.squad/decisions/inbox/frank-parser-v2-authored.md`.

### 2026-04-28 — Phase 3 cross-surface consistency audit
- Horizontal audit across all live surfaces: spec, v8 design doc, catalog source (Constructs.cs, TokenKind.cs, ConstructSlot.cs, Tokens.cs), parser.md, DiagnosticCode.cs, and 5 representative samples.
- Found 8 inconsistencies across 4 files: spec reserved keyword list included removed `write`/`read` tokens (3 fixes); parser.md had `modify`/`omit` in top-level sync set, wrong computed expression syntax `=` vs `->`, stale "11" ConstructKind count, nullable/singular AST node shapes (5 fixes); ConstructSlot.cs comment said `=` instead of `->` for computed expressions; Tokens.cs VA_AllQuantifier missing `TokenKind.Modify` for `modify all` completions.
- All fixes align secondary sources with their authoritative primaries. Build clean, all 2024 tests pass.
- Artifacts: `docs/working/audit-cross-notes.md`, `.squad/decisions/inbox/frank-audit-cross.md`.


- Audited all 11 session decisions against live documentation and source files.
- Found 9 gaps — all in `docs/compiler/parser.md` (6) and `docs/language/precept-language-spec.md` (3). Every gap was the same category: dispatch tables and AST docs treating `OmitDeclaration` as part of `AccessMode` rather than as a separate construct.
- Source catalog files (`Constructs.cs`, `TokenKind.cs`, `ConstructSlot.cs`, `DiagnosticCode.cs`) were already correct — no code changes needed.
- Artifacts: `docs/working/audit-decisions-notes.md`, `.squad/decisions/inbox/frank-audit-decisions.md`.

