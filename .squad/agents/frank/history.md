## Core Context

- Owns the core DSL/runtime architecture: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary (pre-2026-04-13): led runtime feasibility and implementation analysis for guarded declarations, event hooks, computed fields, verdict-modifier semantics, and related issue/design-review work.

## Learnings

- The "nothing crosses the boundary" claim in combined-design-v2 §1–§2 is overclaimed. Descriptors already carry source lines, expression text, constraint metadata, and scope targets into the runtime — that is analysis knowledge in lowered form. The real boundary is lifecycle and dependency direction: runtime types do not reference compile-time artifact types, but runtime shapes carry selected semantic knowledge that lowering transforms. The doc should describe what actually crosses and why, not assert a purity it demonstrably does not have.
- "Pretending" and similar defensive language is AI slop. When a design boundary is real, state what the two sides do and why they exist. If the explanation needs charged language to feel convincing, the explanation is weak.
- Catalog completeness is no longer the main bottleneck; consumer drift is. The biggest remaining leverage is removing hardcoded consumer knowledge in the checker, language server, and tooling.
- Parser and lexer should stay hand-written at the algorithm level, but their vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever possible.
- Type-checker catalog integration has three highest-value moves: replace `OperatorTable` with `Operations`, move widening to `TypeMeta.WidensTo`, and close parser/checker enum-bridge gaps.
- Proof and safety work fits Precept best as bounded abstract interpretation over the existing narrowing pipeline, not as a general SMT-backed system.
- MCP/CLI surface changes are operating-model decisions. Repo-local development must have one authoritative source-first definition with client-specific projections, not three hand-authored contracts.
- Proof/fault architecture must preserve the existing lowering boundary: `CompilationResult.Proof` keeps the full analysis artifact, while `Precept.From(compilation)` lowers only runtime-relevant residue (`ExecutableConstraintPlan` + `ExecutableFaultPlan` + lowered descriptor-backed execution nodes).
- The clean split for consumers is now explicit: language intelligence surfaces (LS diagnostics/completions/hover/go-to-definition and MCP compile-style output) read `CompilationResult`; execution-preview surfaces read `Precept`.
- Constraint evaluation and proof/fault are sibling contracts, not one system: scope-indexed rule/ensure buckets drive ordinary runtime outcomes, while linked `FaultSiteDescriptor` + `FaultCode` + `DiagnosticCode` only covers defense-in-depth impossible paths.
- Shane's preference remains stable on this track: semantic naming over syntax-shaped naming, metadata as early as knowable, and the typed/lowered action family locked to three shapes (base, operand-bearing, binding).
- Key file paths for this synthesis: `docs\working\proposal-frank-proof-fault-contract.md`, `.squad\decisions\inbox\frank-proof-fault-contract.md`, `docs\compiler-and-runtime-design.md`, and `docs\runtime\runtime-api.md`.
- The combined-design baseline is now explicit: the five compiler stages terminate at `ProofModel`, `Precept.From(compilation)` alone owns lowering, and LS intelligence remains a `CompilationResult` consumer while preview/runtime consumes the lowered `Precept`.
- The honest runtime split is fixed: `ExecutableConstraintPlan` carries business enforcement buckets, `ExecutableFaultPlan` carries impossible-path backstops, and the two must never be collapsed into one pseudo-validation system.
- The action family and naming rule are stable enough to treat as architectural memory: three semantic shapes only, with semantic fields like `OperandExpression` and `Binding` replacing syntax-shaped contracts.
- The fault/diagnostic relationship is non-symmetric: every `FaultCode` needs a compiler-owned prevention counterpart, but normal runtime outcomes (`Rejected`, constraint failures, invalid input, access denial, unmatched routing) are not faults and should never be described as mirrored runtime diagnostics.

## Recent Updates

### 2026-04-26 — SyntaxTree vs TypedModel boundary review
- Reaffirmed that parallel `SyntaxTree` and `TypedModel` artifacts are an acceptable layering tradeoff, not an architectural flaw.
- Locked the role split more explicitly: `SyntaxTree` keeps source-structural/recovery/span fidelity, while `TypedModel` is the resolved semantic artifact.
- Named the real implementation hazard as accidental mirroring—letting the typed layer collapse into syntax-with-types instead of a semantically reorganized model.

### 2026-04-27 — MCP operating-model decision closed the dual-surface change
- Treated the Copilot CLI migration as an operating-model change, not a casual config edit.
- Locked the three-surface boundary: repo-root `.mcp.json` for Copilot CLI, `.vscode/mcp.json` for VS Code workspace development, and `tools/Precept.Plugin/.mcp.json` for the shipped payload.
- Reaffirmed that repo-local source-first behavior remains `node tools/scripts/start-precept-mcp.js`, and that the `github` server should not be mirrored into the root CLI file.

### 2026-04-27 — Combined compiler/runtime v2 draft contract
- Locked the stage-by-stage combined architecture around one explicit split: `CompilationResult` is the full analysis/tooling snapshot, while `Precept.From(compilation)` alone performs lowering into the executable model.
- Made the `SyntaxTree` vs `TypedModel` boundary concrete: syntax owns source fidelity and recovery; the typed model owns normalized semantic meaning and downstream analysis inputs.
- Made the language-server consumption model explicit and honest: tokens for lexical classification, syntax tree for source structure, typed model for semantic intelligence, runtime surface only for preview; current LS implementation remains a stub, so this is a contract, not a fiction.

### 2026-04-27 — Shared combined v2 synthesis
- Merged the two fresh combined-design source drafts into one shared `docs\working\combined-design-v2.md` and kept the result explicitly stage-ordered from lexer through runtime operations.
- Rebalanced the document so compiler and runtime carry equal architectural weight, with one repeating contract per stage: purpose, inputs, output, metadata entry, consumers, and current-vs-proposed reality.
- Kept the two most important honesty lines sharp: `SyntaxTree` vs `TypedModel` is a real boundary, and current language-server/runtime implementation is still mostly contract surface rather than finished execution.

### 2026-04-27 — Faults vs diagnostics boundary review
- Clarified that `Diagnostic` and `Fault` are a prevention/backstop pair, not symmetric user-facing error systems.
- Locked the cleaner reading of the no-runtime-errors promise: a successfully constructed `Precept` exposes domain outcomes and boundary-validation outcomes, while `Fault` remains reserved for impossible-path engine invariant breaches.
- Flagged wording cleanup for the v2 doc and adjacent fault/diagnostic docs so they do not imply that normal runtime operations can crash in-domain.

### 2026-04-26 — Cross-catalog invariants and analyzer direction
- Catalog audit confirmed surfaced type coverage is complete; the real correctness bug was `Period` missing `EqualityComparable`, and the real architecture debt is consumer drift.
- Enumerated 37 cross-catalog invariants, 16 intra-catalog structural invariants, and the helper surface needed to enforce them.
- The final queue now favors infrastructure-building analyzer work first, especially trait↔operation consistency, because it unlocks the rest of the sweep.

### 2026-04-25 — Catalog-driven pipeline and parser/lexer review
- Reassessed parser metadata-drivenness: grammar stays hand-written, but vocabulary tables and precedence maps should become catalog-derived.
- Confirmed the type checker is the highest-value catalog consumer after diagnostics, with `OperatorTable` and widening logic as the strongest duplication targets.
- Kept the architecture rule explicit: catalogs own language knowledge; stages own algorithms.

### 2026-04-24 — Precept.Next design and contract reviews
- Approved the early TypeChecker slice work while flagging the remaining design/doc mismatches that block a faithful implementation.
- Logged four blockers in the broader docs.next review: type naming drift, numeric-lane contradictions, incomplete function catalog documentation, and typed-constant validation-stage drift.

### 2026-04-18 to 2026-04-19 — Proof engine and type-checker design gate
- Reworked the proof-engine planning docs into the unified architecture and used that design baseline to ground issue #118/type-checker decomposition work.
- Kept Track B design-review discipline explicit: design documents first, implementation plans second.

### 2026-04-27 — Combined v2 contract hardening
- Hardened the anti-mirroring boundary from slogan to contract: `TypedModel` now explicitly requires symbols, bindings, normalized declarations, typed execution forms, dependency facts, and source-origin handles so LS semantic features do not cheat back to syntax.
- Locked an exact LS artifact-consumption matrix: token classification stays on `TokenStream`, source-structural UX stays on `SyntaxTree`, semantic intelligence stays on `TypedModel`, and preview stays on lowered `Precept`; `GraphResult` and `ProofModel` are explanation inputs, not default LS dependencies.
- Sharpened lowering into an executable-model contract with descriptor tables, slot layout, dispatch indexes, recomputation indexes, access-mode indexes, explicit `always` / `in` / `to` / `from` / `on` constraint-plan families, and proof-owned fault-site backstops.
- Made the diagnostics/outcomes/fault boundary explicit: authoring diagnostics block `Precept` construction, runtime outcomes carry expected domain and boundary behavior, and `Fault` remains reserved for impossible-path engine invariant breaches.

### 2026-04-27 — Combined v2 approved and moved to stage-level follow-up
- George's final concurrence means the top-level architecture is now sharp enough to treat `docs\working\combined-design-v2.md` as the approved main working contract rather than another provisional synthesis pass.
- Remaining work is downstream and stage-specific: align `docs\runtime\fault-system.md`, carry the descriptor-backed public API direction into later runtime design, and avoid reopening the top-level split unless a new owner decision changes it.
- Preserve Shane's operating preference on this lane: future Frank architecture synthesis/review work for the combined compiler/runtime design should use Opus.

### 2026-04-28 — Combined v2 gap patch: 10 design specifics added in place

- Added a compile-time vs lowered artifact classification table to §2 — gives implementers a single-place reference for where every artifact lives and what the hard line between analysis and runtime means in practice.
- Locked the typed action family (three shapes only: `TypedAction`, `TypedInputAction`, `TypedBindingAction`) as a first-class subsection in §5.4, with field naming discipline (`InputExpression`, `Binding`, `ConstraintActivation`, `FaultSite`) made explicit as an enforceable rule.
- Added the earliest-knowable kind assignment two-column checklist to §5.4: parser-assigned kinds vs type-checker-assigned kinds, with the enforcement rule that no stage defers or reaches across the boundary.
- Added the proof strategy set (four bounded strategies) and the proof/fault chain formula to §5.6 — the chain from catalog metadata through `ProofRequirement → ProofObligation → DiagnosticCode → FaultCode → FaultSiteDescriptor` is now stated with per-link ownership.
- Added four precomputed constraint activation indexes (always, state, event, event-availability) to §5.8 — these are the implementation shape behind the `always`/`in`/`to`/`from`/`on` plan families.
- Added a full 7-operation constraint evaluation matrix to §5.9, including Restore, InspectCreate/Create without initial event, and the two rules: Restore bypasses access-mode but not constraints; `to` ensures are transitional only.
- Added the three constraint exposure tiers (`Precept.Constraints`, `Version.ApplicableConstraints`, `ConstraintResult/ConstraintViolation`) to §8 as the query/inspection contract.
- Added five concrete implementation action items to §9 (descriptor types, anchor shapes, FaultSiteDescriptor threading, DTO update, drift tests).
- Added two additional design assertions to §10: analysis/runtime collapse (assertion 10) and consumer-local vocabulary copies (assertion 11).
- Key learning: gap analysis at this level of specificity (named types, field names, index key shapes, per-operation constraint matrix) is exactly the content that makes a design doc actionable vs aspirational. These gaps were not cosmetic — they were the difference between a contract and a sketch.
- v1 (draft+final) reads like a decision manifest — it names every locked boundary crisply but leaves the stage-by-stage implementation story implicit. v2 (patched) reads like a reference spec — it carries per-stage contract tables, honest current-vs-proposed columns, concrete type signatures, a full artifact classification table, and dedicated runtime-operation sections that v1 never attempted. After the gap patch, v2 subsumes v1 entirely; no residual content in v1 is missing from v2. Future architecture work should treat `docs\working\combined-design-v2.md` as the single authoritative combined design and retire the draft/final pair to historical record only.
- v2 structurally revised per Elaine's genre/readability feedback and Frank's boundary reassessment. Key changes: (1) Added problem statement and moved §10 assertions to §1 as "Architectural commitments" so readers get the design's spine without reading 400 lines. (2) Corrected the "hard line / nothing crosses" boundary claim — the real rule is type dependency direction, not knowledge isolation; descriptors carry analysis knowledge in lowered form. (3) Removed "pretending" language and defensive rhetoric around the compile/runtime split. (4) Converted 13 stage-contract tables from key-value format to labeled prose, cutting visual density without losing information. (5) Merged two overlapping §2 artifact tables into one. (6) Split §8 into three subsections (result families, commit/inspection surfaces, constraint query contract). (7) Moved §9 implementation status to Appendix A. (8) Added decision lead-in paragraphs to every §5.x stage section — each now opens with the design choice, not just a description. (9) Converted §1 principles and §4 inputs from tables to prose. Genre shift: the doc now reads as a design document (decisions with rationale) rather than a reference manual (descriptions with tables).
