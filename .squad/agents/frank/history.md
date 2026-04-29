## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, access-mode redesign decisions, and parser catalog-shape direction.

## Learnings

- When plan examples use disjunction `or` patterns in switch arms, verify variable bindings — `or` patterns cannot bind names, making interpolated throw messages a compile-time trap.
- Plans that introduce error-recovery helpers (like `ErrorStatement()`) must specify whether they're new methods or existing ones. Phantom method calls are silent plan defects that become George's problem at implementation time.
-Conservative defaults are structural guarantees: write/edit surfaces open exceptions, they do not become the baseline by omission.
- Metadata belongs in catalogs when consumers need per-member knowledge; pipeline/tooling drift comes from hardcoded parallel copies.
- Parser algorithms stay hand-written, but vocabulary tables, precedence data, and disambiguation metadata should derive from catalog truth where possible.
- Authoring consumers read `CompilationResult`; execution/preview consumers read lowered `Precept`; runtime-native lowered data may intentionally preserve selected analysis residue.
- Design loop convergence pattern: validation infrastructure first (v5), then vocabulary lock (v7), then structural separation corrections (v8). Each layer depends on the previous — you can't split AST nodes correctly until the vocabulary is locked, and you can't lock vocabulary until the validation layer ensures nothing drifts silently.
- The v7→v8 OmitDeclaration split is the canonical example of why `catalog-system.md`'s flat-record prohibition matters: different slot counts + different guard eligibility + different semantic categories = separate constructs, not internal branching.
- A complete design evaluation requires reading the *implementation* alongside the *design* — the wrapper node pattern (TokenWrapper, etc.) and the InitialMarker remediation-phase addition were both correct implementation details not visible in the design documents.
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

### 2026-04-28 — Parser R1–R6 compliance review: APPROVED
- Reviewed all 6 remediation slices against v8 design. Verdict: APPROVED with zero blocking findings and 10 positive observations.
- Two-tier architecture confirmed correct: non-disambiguated constructs (field, state, event, rule) go through full slot machinery; disambiguated constructs use catalog-driven `FindDisambiguatedConstruct()` with hand-written per-construct parsers.
- All vocabulary frozen sets derive from catalog metadata. Sync recovery uses `Constructs.LeadingTokens`. No hardcoded parallel keyword lists.
- Key pattern: `InvokeSlotParser()` CS8509 enforcement means adding a new `ConstructSlotKind` without a parser arm is a compile error — structural safety net.
- Artifacts: `docs/working/parser-review.md`, `.squad/decisions/inbox/frank-parser-review.md`.


### 2026-04-28 — Vision vs. Spec audit completed
- Audited `docs/language/precept-language-vision.md` (74 KB, ~1167 lines) against `docs/language/precept-language-spec.md` (39 KB, ~1350 lines).
- **Two contradictions found:** (1) `with` listed as a structural preposition in vision but absent from spec — stale reference, never implemented. (2) "Root editability" in stateless precept list conflicts with the locked `write all` removal decision — should read "Field-level editability."
- **Critical unique content in vision not yet in spec:** the 11 Design Principles, Governance vs. Validation framing, 7 Execution Model Properties, 7 Proof Philosophy principles, outcome semantic distinctions, and constraint violation subject attribution. These are the "why" backbone — the spec has no preamble or §4/§5 to host them.
- **5 duplication zones:** access mode composition, function catalog, modifier applicability, numeric lane rules, entity construction — maintained in both docs, creating maintenance tax.
- **Recommendation: Do NOT archive yet.** Migrate Category A language-identity content into a new §0 "Language Design Goals" in the spec first. Remaining content migrates as spec §4 (graph analyzer) and §5 (proof engine) are written. Archive after all migrations complete.
- Artifact: `.squad/decisions/inbox/frank-vision-spec-audit.md`.

### 2026-04-29 — Parser remediation review batch recorded
- Architectural compliance review for Parser.cs remediation slices R1-R6 was approved with zero blocking findings and is now merged into the squad decision ledger.
- The parser-v2 reference and Phase 3 cross-surface audit are now recorded as durable team context alongside the approved remediation review.

### 2026-04-28 — Parser design evaluation (v5–v8) completed
- Evaluated all six working design documents (v5, v5-lang-simplify, v6, v7, v8, v8-session-notes) against the canonical parser spec (docs/compiler/parser.md) and current implementation (src/Precept/Pipeline/Parser.cs).
- **Verdict: ADOPT v8 as-is.** The design is sound, the implementation is complete, and it matches the canonical spec with zero contradictions.
- The evolution arc resolved every identified gap: v5 fixed validation infrastructure + RuleExpression, v7 locked vocabulary + implementation plan, v8 corrected OmitDeclaration structural separation + FieldTargetNode DU.
- Key confirmation: The catalog-driven boundary is drawn correctly — domain knowledge in catalogs, grammar mechanics hand-written. No metadata-driven architecture violations.
- Implementation (Parser.cs, 1386 lines) faithfully executes v8 across all 12 constructs, all 5 architectural layers, and 2034+ passing tests.
- Minor observation: `InitialMarker` slot kind was added during remediation, not in v8's original plan. Covered by the spec. Not a gap.
- Design loop is closed. Working documents can be archived as audit trail.

### 2026-04-28 — Catalog-driven extensibility audit completed
- Full baseline audit of the lexer/parser/AST metadata boundary. Lexer is 100% catalog-driven for keywords, operators, and punctuation — zero code changes needed for new vocabulary. Parser has a dual-layer architecture: vocabulary recognition (Layer A) is fully catalog-driven; grammar structure (Layer B) has 4 hand-written switches that need hardening.
- Identified 8 gaps where adding a new `ConstructKind`, `ActionKind`, or `ConstructSlotKind` doesn't produce compile errors: `BuildNode()` wildcard, `ParseDirectConstruct()` wildcard, `DisambiguateAndParse()` hardcoded routing, `ParseActionStatement()` 8-arm switch, `ExpressionBoundaryTokens` hardcoded set, no `ConstructKind`↔`Declaration` subtype enforcement, no `ActionKind`↔`Statement` subtype enforcement, hardcoded access mode adjectives.
- Key architectural recommendation: prefer catalog shape changes over Roslyn analyzers. Two immediate wins (remove `BuildNode()` wildcard for CS8509, derive `ExpressionBoundaryTokens` from `Constructs.LeadingTokens`). Two catalog shape investments (`RoutingFamily` on `ConstructMeta`, `ActionSyntaxShape` on `ActionMeta`). These four changes close all 8 gaps without custom Roslyn analyzers.
- `InvokeSlotParser()` is the gold standard — already CS8509-enforced, no wildcard. New `ConstructSlotKind` members produce build errors. This pattern should be replicated across all exhaustive switches.
- Artifacts: `.squad/decisions/inbox/frank-catalog-extensibility.md`.

### 2026-04-28 — Catalog extensibility implementation plan review: BLOCKED (3 fixable)
- Reviewed the coordinator's implementation plan for the 8 catalog extensibility gaps. Plan is structurally sound and architecturally aligned — all 8 gaps covered, catalog philosophy maintained, CS8509 mechanism confirmed effective with `TreatWarningsAsErrors=true`.
- Three blockers found: (1) PreceptHeader is classified as `RoutingFamily.Direct` but it's parsed in a pre-loop code path, not through `ParseDirectConstruct()` — needs a `Header` routing family member. (2) Slice 3b is missing from the execution order table. (3) Slice 3b presents two code options (catch-all `var k =>` vs explicit listing) without committing — only the explicit listing achieves CS8509.
- Confirmed `FrozenSet.Union()` + `.ToFrozenSet()` works via LINQ on `IEnumerable<T>`. ActionSyntaxShape as flat enum is correct (DU would be overengineering). BuildNode wildcard removal is safe — all 12 arms present.
- Noted that ActionSyntaxShape is added in Slice 4 but never consumed by Slice 5 (which switches on ActionKind, not shape). Plan should state whether this is intentional forward investment.
- Artifacts: `.squad/decisions/inbox/frank-catalog-extensibility-plan-review.md`.
