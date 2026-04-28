## Core Context

- Owns the core DSL/runtime architecture across parser, type checker, diagnostics, graph analysis, and execution semantics.
- Protects cross-surface contract integrity across runtime, docs, MCP, and contributor workflow changes.
- Historical summary: led the combined compiler/runtime design consolidation, proof/fault boundary hardening, catalog-consumer drift analysis, and canonical design-doc promotion work.

## Learnings

- D3's closed-world access default is a structural safety property. `write` opens exceptions; inverting to write-by-default weakens auditability and computed-field clarity.
- Runtime boundaries should be described by dependency direction, not by pretending no analysis knowledge crosses lowering. Lowered runtime-native descriptors may carry selected compile-time residue.
- Catalog completeness is no longer the main bottleneck; consumer drift is. Highest-value follow-up is removing hardcoded language knowledge from checker, LS, MCP, and tooling consumers.
- Parser and lexer algorithms should stay hand-written, but vocabulary tables, precedence data, and classification sets should derive from catalog metadata wherever the catalog is the language truth.
- Authoring consumers read `CompilationResult`; execution and preview consumers read lowered `Precept`. Constraint plans and proof/fault behavior are sibling contracts, not one blended validation layer.
- Runtime resolved-value enums (for example `FieldAccessMode { Read, Write }`) are outputs of compilation, not declaration-surface vocabulary. Do not collapse modifier docs into runtime descriptor docs.
- For language-surface doc audits, the update set is predictable: token catalog, modifier catalog, parser/type-checker docs, diagnostic catalog, language spec/vision, and evaluator prose; historical working docs and runtime result-shape docs change only when their own contract changes.
- Clean-slate parser work changes the calculus: richer catalog-driven dispatch and routing are viable when the parser is still a stub, but semantic meaning and downstream type safety remain irreducibly hand-authored.
- Access mode design round verdicts (A1 guarded read approved with writable-only constraint, A2 guarded omit stays prohibited, A3 vocabulary unchanged, A4 redundancy rule locked as compile error) written to `docs/working/frank-access-mode-design-round.md` on 2026-04-28. Rule 4 split into 4a/4b/4c, vision doc updated, grammar updated with two guarded lines. Open question: rule 7 refinement semantics for unguarded write + guarded read on same pair (unchanged).
- Rule 7 closed: structurally precluded by 4b+4c mutual exclusion. `ConflictingAccessModes` not needed for this case.
- B2 vocabulary consistency round (2026-04-28): `unlocked` fails as a field modifier because it's a resultative participle (implies prior lock event), not a dispositional adjective (names a static property). The new constraint requires modifier and access mode keywords to share the same vocabulary family. Recommended `editable`/`editable`+`fixed`+`omit`: using the same word for modifier and upgrade keyword creates a tautological family connection; `fixed` is the universally business-natural antonym with no permanence, no I/O, no operational connotation. Six families evaluated; `frozen` was close second but narrower domain applicability.
- B3 grammar proposal evaluation (2026-04-28): Shane's omit/access-mode semantic framing (exclusion vs. constraint) is correct and sharper than prior rounds — accepted into design rationale. His `->` operator proposal rejected on semantic coherence grounds: `->` has established pipeline-flow semantics throughout Precept (action chains, state actions, computed fields); repurposing it for field targeting creates a second unrelated meaning. Grammar shape split (adjective-after-field) rejected as redundant — the vocabulary's part-of-speech difference (verb `omit` vs. adjectives `editable`/`fixed`) already encodes the semantic distinction. `readonly` rejected as I/O-rooted programmer vocabulary. Recommendation unchanged from B2: `editable`/`fixed`/`omit` in verb-before-field position.
- B4 `modify` verb evaluation (2026-04-28): Shane's `modify` proposal recommended-with-caveats. `modify` creates true verb parallelism with `omit` that B2's adjective-as-verb `editable` did not achieve. Verb/adjective separation is the core improvement: `modify` is the verb, `readonly`/`editable` are adjectives naming the access level. This fixes B2's double-duty problem. Adjective-after-field position is no longer redundant when `modify` is the verb — it's the natural complement position in a verb-object-complement construction. Recommended pair: `readonly`/`editable` (with `fixed`/`editable` as alternative pending Shane's call). Zero keyword collision in current catalog. Supersedes B2/B3 recommendations.
- B4 vocabulary LOCKED (2026-04-28): Shane accepted Frank's `readonly`/`editable` recommendation. Final grammar: `in State modify Field readonly|editable [when Guard]` / `in State omit Field`. `modify` = verb (disambiguation token, not stored as slot), `readonly`/`editable` = adjectives (stored in `AccessModeKeyword` slot). Guard position is post-field. `write`/`read` retired from access mode context. New tokens needed: `Modify`, `Readonly`, `Editable`. F11 (pre-verb guard) superseded by F12. Updated: language spec §2.2, vision doc guarded-access-modes section, v7 design doc (F12 decision, entries table, Slice 1.4/1.5/4.3/4.4/5.3), design round doc (§ B4 Final Decision). Decision written to inbox.

## Recent Updates

### 2026-04-27 — `writable` field modifier audit and review
- Audited all 32 docs files for the `writable` language change. Locked the two-layer access model: field-level `writable` baseline plus state-scoped `write|read|omit` overrides, with state-level rules winning per field/state pair.
- Confirmed compile-time-only `WritableOnEventArg`, preserved root `write all` for stateless precepts, and recorded which documentation surfaces must change when modifiers are added.
- Review verdict stayed blocked on one real catalog issue (`AccessMode.LeadingToken`) plus a few stale doc references.

### 2026-04-27 — Catalog-driven parser design loop
- Round 1 established the full-vision parser shape: `DisambiguationEntry`, generic disambiguation, generic slot iteration, and generator-ready architecture.
- Round 3 resolved George's six flagged items: accept `LeadingTokenSlot`, keep `BuildNode` as an exhaustive switch, apply peek-before-consume for `ActionChain`, allow both `when` guard positions, keep disambiguation tokens explicit, and sequence the catalog migration behind a `PrimaryLeadingToken` bridge.
- Extensibility outcome: catalog-driven parsing removes most parser-layer glue for new constructs, but generic AST and AST-as-catalog-tree were rejected; source generation stays deferred until construct count or consumers justify the infrastructure.

### 2026-04-27 — Catalog-driven parser design v7: final design & implementation plan
- Closed L1 (language change decision): Shane confirmed pre-verb guard position as canonical (`in State when Guard write Field`). Recorded as decision F11. Language simplification proposal rejected.
- Closed T1 (test nit): Restructured `BuildNodeHandlesEveryConstructKind` assertion to distinguish `ArgumentOutOfRangeException` (gap) from null-propagation exceptions (arm exists).
- Authored the five-PR implementation plan meeting CONTRIBUTING.md quality bar: PR 1 (catalog migration), PR 2 (parser infrastructure), PR 3 (non-disambiguated constructs), PR 4 (simple disambiguation), PR 5 (from-scoped + error sync).
- Design loop concluded after 7 rounds. 17 decisions (F1–F11, G1–G6) resolved with zero open items. v7 supersedes v1–v6.
- Deliverable: `docs/working/catalog-parser-design-v7.md` — method-level specificity, exact file paths, tests per slice, regression anchors, file inventory, tooling/MCP sync assessment.

### 2026-04-28 — Combined design boundary/philosophy revision
- Corrected the overclaim that "nothing crosses the boundary" and recentered the main design doc on Precept's philosophy and guarantee.
- Locked the real rule: type dependency direction stays one-way, while lowered runtime-native shapes may intentionally preserve selected analysis knowledge.

### 2026-04-28 — Spec grammar corrections (4 errors)
- Fixed guard/access-mode grammar inconsistency: grammar was showing `when` as valid with all three access verbs, contradicting rule 4 which correctly restricts guards to `write` only. Split grammar into guarded write line and unguarded read/omit line.
- Applied Shane-approved language change: moved `when` guard from pre-verb to post-field position (`in State write Field when Guard`). This eliminates the disambiguator pre-parsing complexity identified in George's v5-lang-simplify analysis. Updated spec, vision doc, and both affected sample files.
- Fixed ensure grammar: spec showed pre-ensure guard position; all sample files consistently use post-expression guard. Updated spec to match samples (`ensure Expr when Guard because Msg`).
- Added `FieldTarget` formal definition: grammar was showing a bare `FieldTarget` without defining it. Added `FieldTarget := identifier ("," identifier)* | all` to match documented prose and sample usage of comma-separated field lists.
### 2026-04-28 — Redundant access mode diagnostic and inbox merge
- `RedundantAccessMode` is now the canonical compile-time error for dead named-field access declarations; `RedundantGuardedRead` is retired, while `omit` and broadcast `all` remain exempt and rule 7 stays open.
- Access-mode backlog is durably merged: guarded `read` remains a writable-only downgrade, guarded `omit` remains prohibited, the vocabulary stays `read`/`write`/`omit`, and `write all` is removed from the language in favor of field-level `writable`.


### 2026-04-28 — B4 vocabulary locked
- Shane approved Frank's B4 direction: access-mode declarations now use `modify` as the verb with `readonly` / `editable` as the access adjectives.
- This closes the adjective-pair decision, supersedes B2/B3 recommendations, and preserves `omit` as the separate structural-exclusion verb rather than folding it into the same access-mode family.

### 2026-04-28T03:01 — Full documentation sweep: access-mode shorthand grammar

**Complete locked grammar (authoritative):**
```
in State modify Field readonly [when Guard]         ← singular access constraint
in State modify Field editable [when Guard]         ← singular access upgrade
in State modify F1, F2, ... readonly/editable [when Guard]  ← comma-separated shorthand
in State modify all readonly/editable [when Guard]  ← state-scoped all

in State omit Field                                 ← singular structural exclusion
in State omit F1, F2, ...                          ← comma-separated shorthand
in State omit all                                   ← state-scoped all (no fields visible)
```

**Key semantic notes:**
- `modify` = field IS present, access level is being declared (can be guarded)
- `omit` = field is structurally ABSENT from this state (cannot be guarded — ever)
- Both verbs share FieldTarget shape: singular | comma-separated list | `all`
- OmitDeclaration AST: no GuardClause slot. AccessModeDeclaration AST: has GuardClause slot (optional).

**Docs updated (8 files):**
- `docs/language/precept-language-spec.md` — keyword tables, reserved words, grammar (9 forms for both verbs), dispatch table, validation table, diagnostic catalog
- `docs/language/precept-language-vision.md` — source inputs, surface table, keyword families, Layer 2 verb table, composition rules, parser/type-checker responsibilities, language contract
- `docs/working/catalog-parser-design-v7.md` — ParseFieldTarget description, shorthand test cases
- `docs/working/frank-access-mode-design-round.md` — B4 Shorthand Addendum
- `docs/compiler/parser.md` — disambiguation tables, grammar, sync points, token tables, examples
- `docs/language/catalog-system.md` — AccessModifierMeta DU table and code example
- `docs/runtime/runtime-api.md` — FieldAccessMode enum, Restore prose
- `docs/runtime/evaluator.md` — Access-Mode Enforcement composition model reference

**Pattern found:** Old `write`/`read` vocabulary leaked into four categories: (1) keyword tables/lists, (2) grammar rules and dispatch tables, (3) diagnostic description examples, (4) runtime behavioral prose. The token catalog tables were the most consistent leakage surface — every doc that listed keywords had `write`/`read`. Secondary docs (lexer, tooling-surface, diagnostic-system) were clean because they reference catalog mechanics generically rather than listing specific keywords.
