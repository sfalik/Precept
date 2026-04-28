## Core Context

- Owns the core DSL/runtime architecture: parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the 2026 combined compiler/runtime design consolidation, proof/fault boundary hardening, catalog-consumer drift analysis, and canonical design-doc promotion work.

## Learnings

- D3's closed-world access default (read unless explicitly opened to write) is a structural safety property, not just a convenience choice. Inverting it to write-by-default changes the omission failure mode from safe (locked) to unsafe (exposed).
- Computed fields align naturally with D3's read default. A universal write default creates a hidden exception for computed fields that weakens inspectability.
- Runtime boundaries should be described by dependency direction, not by claiming that no analysis knowledge crosses. Lowering can and should carry runtime-native residue derived from compile-time analysis.
- Catalog completeness is no longer the main bottleneck; consumer drift is. The highest-value work is removing hardcoded language knowledge from checker, LS, and tooling consumers.
- Parser and lexer algorithms should remain hand-written, but vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever possible.
- Proof and safety work fit Precept as bounded abstract interpretation over the existing narrowing pipeline, not as a general SMT-backed system.
- The clean consumer split is stable: language-intelligence surfaces read `CompilationResult`; execution and preview surfaces read lowered `Precept`.
- Constraint evaluation and proof/fault are sibling contracts, not one pseudo-validation system: runtime constraint plans govern expected outcomes, while faults remain impossible-path backstops.
- The action family and naming rule are stable architectural memory: three semantic shapes only, with semantic field names instead of syntax-shaped names.
- MCP/CLI surface changes are operating-model decisions. Repo-local development needs one authoritative source-first definition with client-specific projections.
- Runtime resolved-value enums (`FieldAccessMode { Read, Write }`) represent the output of compilation, not the declaration surface. Compile-time modifiers (`writable`) feed into the builder; the builder produces the resolved descriptors the runtime reads. These are different abstraction layers and must not be conflated during doc audits.
- When a new field modifier is added, the files that need updates are: token catalog (kind enum + count), modifier catalog (member list + count), field-modifier flag list in parser, dispatch/grammar notes in parser, type-checker processing model, diagnostic enum + switch, spec grammar nodes, spec modifier table, spec validation table, spec diagnostic catalog, vision keyword list, vision declaration form table, vision access-modes section, and evaluator enforcement prose. Files that do NOT need updates: result-types, runtime fault codes, working docs (historical), MCP/LS stubs.

## Recent Updates

### 2025-07-14 — Per-field `readonly` proposal analysis
- Rejected the proposal to invert D3 into a write-by-default model with field-level `readonly`.
- Locked the rationale: conservative defaults, auditability, computed-field consistency, and domain-language positioning all favor the existing `write`-opens-exceptions model.
- If verbosity relief is ever needed, the acceptable lane is narrower sugar such as `write all except ...`, not a default inversion.

### 2026-07-17 — Combined design comprehensive revision
- Applied team review feedback to `combined-design-v2`, adding parser specificity, flat evaluation-plan commitments, versioning/restore clarifications, innovations callouts, and new grammar/MCP sections.
- Promotion outcome: the revised working doc became `docs/compiler-and-runtime-design.md`, replacing the short-form version and absorbing its surviving rationale.

### 2026-04-28 — Combined design boundary and philosophy revisions
- Corrected the overclaimed "nothing crosses the boundary" language and recentered the document on Precept's philosophy and guarantee rather than on defending a split.
- Logged that descriptors and lowered runtime-native shapes legitimately carry selected analysis knowledge across the lowering boundary.

### 2026-04-26 — Catalog audit and doc promotion lane
- Confirmed catalog surfaced-type coverage was largely complete and that the bigger risk is consumer drift.
- Promoted the combined design doc to its canonical location and kept code, not design prose, as the source of truth for concrete signatures.

### 2026-04-24 — Precept.Next pre-TypeChecker gate
- Found TypeChecker start blocked by contract scaffolding gaps: hollow TypedModel surface, nullable SyntaxTree root mismatch, missing diagnostics, and no SourceSpan→SourceRange bridge.

### 2026-04-27 — `writable` field modifier doc audit
- Audited all 32 files in `docs/` for the `writable` field modifier language change.
- 7 files updated: `precept-language-spec.md`, `precept-language-vision.md`, `parser.md`, `type-checker.md`, `diagnostic-system.md`, `catalog-system.md`, `evaluator.md`.
- 25 files confirmed no change (including `docs/working/` historical records, `result-types.md`, and all stubs).
- Key pattern: files with concrete language surface vocabulary (token catalog, modifier tables, grammar nodes, diagnostic enum) required updates; files describing resolved runtime values or stub-level tooling surfaces did not.
- Confirmed architectural invariant: `result-types.md`'s `FieldAccessMode { Read, Write }` is the *resolved* runtime mode — correct as-is. The `writable` modifier is a compile-time declaration; resolution into runtime access mode happens in the Precept Builder.
- Locked two-layer composition model in spec, vision, and all compiler docs: Layer 1 = field `writable` baseline; Layer 2 = `in <State>` state-scoped override. State-level always wins.
- `WritableOnEventArg` added as a compile-time-only diagnostic (no runtime backstop path).
- Inbox decision written to `.squad/decisions/inbox/frank-doc-audit-writable.md`.

### 2026-04-27 — Parser dispatch table analysis
- Analyzed the top-level dispatch table from `precept-language-spec.md` §2.2 against the metadata-driven architecture.
- Confirmed `Parser.cs` is currently a stub (`throw new NotImplementedException()`), so the dispatch table exists only in the spec as design intent, not as implemented code.
- The ConstructCatalog already carries `LeadingToken` on every `ConstructMeta` — the token→construct mapping IS cataloged metadata.
- The key finding: four leading tokens (In, To, From, On) each map to MULTIPLE ConstructKinds (In→2, To→2, From→3, On→2). Disambiguation after the shared leading token requires lookahead past the state/event target to the following verb (ensure vs write/read/omit, ensure vs ->, on vs ensure vs ->). This is grammar structure, not domain knowledge.
- The catalog-system doc (§ Pipeline Stage Impact, Parser row) explicitly states: "Grammar productions stay hand-written. Vocabulary tables migrate to catalog-derived frozen dictionaries." The dispatch table is grammar production selection — carve-out territory.
- Verdict: no violation, no change needed. The `LeadingToken` field is the catalog's contribution to this concern. The disambiguation logic is hand-written parse mechanics by design.

### 2026-04-27 — Catalog-driven parser full scope document

- Scoped five layers of catalog-driven parser behavior (A: vocabulary tables, B: dispatch table, C: disambiguation, D: slot-driven productions, E: error recovery).
- Accepted layers A, B, D (with constraints), and E. Rejected layer C (disambiguation via catalog metadata) — the `when` guard re-dispatch breaks flat-metadata modeling, and the 4 disambiguation methods are ~60 lines of stable code not worth abstracting.
- Key architectural insight: the 1:N LeadingToken problem doesn't invalidate catalog dispatch — it means disambiguation stays hand-written while the dispatch table itself becomes catalog-derived. These are separable concerns.
- Layer D (slot-driven productions) is the biggest structural win: generic slot iteration replaces 11 per-construct parse methods. Per-construct AST node factories remain for downstream type safety — the generic iterator produces slots, the factory maps them to named record fields.
- No changes needed to `ConstructMeta` shape. Two new derived indexes on `Constructs` (`ByLeadingToken`, `LeadingTokens`). Vocabulary frozen dictionaries are parser-internal, derived from existing `All` properties.
- Estimated catalog-driven coverage: ~70% of parser decision-making. Remaining ~30% is grammar mechanics (disambiguation, expression parsing, slot-level parse methods, AST construction).

### 2026-04-27 — Parser dispatch rationale added to docs/compiler/parser.md

- Added `### Dispatch Table: Grammar Structure vs. Vocabulary` section to `docs/compiler/parser.md` in § Design Rationale and Decisions, immediately after `### The Parser Is Catalog-Driven Dispatch`.
- Section captures: (1) why the dispatch table is hand-written (grammar structure ≠ vocabulary); (2) the 1:N LeadingToken problem (In→2, To→2, From→3, On→2) as the concrete evidence catalog lookup can't select productions; (3) what the catalog does provide (`ConstructMeta.LeadingToken` for grammar gen, completions, MCP); (4) the vocabulary gap to watch — operator sets, type keywords, modifier sets, action keywords must derive from `Operators.All`, `Types.All`, `Modifiers.All`, `Actions.All`.
- Also tightened the Tokens Catalog section in § Dependencies to distinguish vocabulary (catalog-derived) from binding powers (parser-internal mechanics).

### 2026-04-27 — Cross-review of George's catalog-parser estimate
- Read George's full implementation estimate against my 5-layer scope document.
- **Walked back Layer D** (slot-driven parsing). George's 154-hour / 4-5 week estimate with 5 concrete risk vectors (ActionChain loop-not-slot, Outcome 3-form sub-grammar, BuildDeclaration fragility, scale mismatch at 11 constructs, simultaneous regression surface) made the cost/benefit case definitive. The `Slots` metadata already serves tooling consumers — the parser doesn't need to be a consumer too at this scale.
- **Accepted Layer C narrower framing.** George proposed `DisambiguationToken: TokenKind[]?` as catalog metadata for LS/MCP consumers, NOT for parser restructuring. This is a different proposal than what I rejected — the field describes language structure that belongs in the catalog. Parser methods reference it as documentation but don't restructure around it.
- Key learning: abstraction value and grammar scale are not linearly related. A generic production framework for 200 constructs is a necessity; for 11 constructs it's over-engineering regardless of architectural elegance. The right boundary: catalog-drive vocabulary always, catalog-describe structure for consumers, hand-write grammar mechanics.

### 2026-04-27 — `<-` vs `->` calculated field arrow direction analysis

- Analyzed proposal to replace `->` with `<-` for computed field expressions to simplify the compiler.
- **Verdict: REJECT.** The simplification claim is false — there is no ambiguity in the current `->` usage between computed fields and action chains, because these are separate grammar productions entered through different leading tokens. Parser disambiguation cost is zero.
- Key finding: `<-` introduces a **real lexer conflict** with `< -` (less-than followed by negation). The maximal-munch scanner would greedily consume `<-` as a single token, breaking expressions like `Score < -5`. Resolution requires whitespace sensitivity or special-case scanning — both regressions from the current zero-special-case operator scanner.
- Semantic mismatch: `<-` evokes imperative assignment ("value flows into variable"), while Precept's computed fields are declarative derivations ("field is defined as expression"). `->` correctly conveys "produces" / "derives as."
- The spec (line 644) explicitly states `->` is "deliberately overloaded to create a visual pipeline" — the consistency of a single arrow glyph across computed fields and action chains is an intentional design property, not a coincidence.
- Alternatives assessed: `=>` (no advantage over `->`, false lambda association), `=` (collision with `Assign`, ambiguity with defaults), keyword-based (verbose, inferior to compact `->` glyph).
- Decision written to `.squad/decisions/inbox/frank-calculated-field-arrow-direction.md`.

### 2026-04-27 — AST node type reference on ConstructMeta analysis

- Analyzed Shane's question: can `ConstructMeta` carry a `Type` reference or factory delegate to the AST node type, simplifying the `BuildNode` factory problem from Layer D?
- **Ruling: No.** Three independent reasons:
  1. **Layer violation.** `ConstructMeta` is Language layer; AST nodes are Pipeline layer. Adding a `Type`/delegate reference inverts the dependency direction (Pipeline → Language becomes Language → Pipeline). The catalog would need `using Precept.Pipeline;`.
  2. **Doesn't eliminate per-construct code.** The 11 factory functions (mapping positional slots to named record fields with typed casts) exist regardless of whether they're lambdas on `ConstructMeta` entries or switch arms in a centralized `BuildNode`. The knowledge is identical; only the hosting location changes.
  3. **Fails the catalog decision framework.** AST node types are not language surface — they don't appear in `.precept` files, don't carry semantics consumers need, and wouldn't appear in a complete description of the language. They fail all three tests from `catalog-system.md` § Architectural Identity.
- The `BuildNode` exhaustive switch on `ConstructKind` is the correct pattern: Pipeline-internal, CS8509-enforced, centralized, ~33 lines at 11 constructs.
- `BuildNode` is 13% of Layer D's cost (20h of 154h). Layer D was walked back for the other 87% (slot iteration complexity, ActionChain loops, Outcome sub-grammar, regression risk). Simplifying `BuildNode` doesn't change the Layer D calculus.
- Key architectural insight: the catalog's contribution to this problem is complete — `Slots` defines the sequence, `ConstructSlot.Kind` routes to slot parsers, `IsRequired` drives optionality, `Kind` keys the `BuildNode` switch. The catalog defines the *what*; the parser defines the *how*. AST construction is *how*.
- Decision written to `.squad/decisions/inbox/frank-ast-catalog-reference.md`.
