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

### 2026-04-28 — Phase 2 decisions audit
- Audited all 11 session decisions against live documentation and source files.
- Found 9 gaps — all in `docs/compiler/parser.md` (6) and `docs/language/precept-language-spec.md` (3). Every gap was the same category: dispatch tables and AST docs treating `OmitDeclaration` as part of `AccessMode` rather than as a separate construct.
- Source catalog files (`Constructs.cs`, `TokenKind.cs`, `ConstructSlot.cs`, `DiagnosticCode.cs`) were already correct — no code changes needed.
- Artifacts: `docs/working/audit-decisions-notes.md`, `.squad/decisions/inbox/frank-audit-decisions.md`.

