## Core Context

- Owns UX/design across README form, brand-spec presentation, semantic visual surfaces, and preview/mockup direction.
- Keeps visual decisions aligned with runtime semantics: Emerald/Amber/Rose are semantic runtime signals; Gold is a restrained brand accent.
- Historical summary (pre-2026-04-13): led README layout/hero passes, semantic-visual-system refinements, preview-concept explorations, and verdict-modifier UX analysis.

## Learnings

- README form work is mostly hierarchy, line economy, and plaintext resilience.
- Preview concepts must be validated against realistic sample complexity, not only small demo precepts.
- Verdict modifiers are strongest as subtle authored-intent cues layered beneath runtime outcomes.
- Diagnostic messages must pass the domain-author test, not just the developer test. "Typed constant," "interpolation expression," "member accessor," and "provably unsatisfiable" all fail. The audience thinks in fields, values, states, conditions — not tokens, types, and satisfiability.
- `InvalidCharacter` is the single most structurally broken diagnostic in the lexer: it covers three completely different problems (invalid source char, unrecognized escape, lone `}` in a text value) with one undifferentiated message. Each needs its own code and fix-oriented message.
- The lone `}` case is the highest-probability first-contact failure for domain authors using interpolation in text values. It has zero instructional value in the current message.

## Recent Updates

### 2026-04-11 — Verdict modifiers UX perspective
- Recommended badge-level authored verdict cues, not full-surface fills, to preserve clarity and avoid false confidence.
- Identified state verdicts as novel differentiator territory for Precept tooling and storytelling.

### 2026-04-18 — Proof engine design review (PR #108)
- Reviewed ProofEngineDesign.md and PR body Commit 14 (hover integration) for UX spec verification.
- Verdict: CHANGES_NEEDED — architecture is right, presentation layer needs finishing.
- 3 non-negotiable hover positions are correctly captured and architecturally enforced (no interval notation, no compiler internals, "why" attribution required).
- Key UX issues found:
  - `ToNaturalLanguage()` mapping table incomplete — only 4 shapes specified, need full coverage including `[N,N]`, `(-∞,N]`, `(N,M)`, etc.
  - C94–C98 diagnostic message templates use raw interval notation — violates position #1. Must use the same `ToNaturalLanguage()` formatter.
  - ConstraintViolationDesign.md stale for C92/C93, missing C94–C98 entirely.
  - `ProofAssessment.Evidence` as bare `string?` needs a formatting contract to prevent inconsistent hover attribution.
- Missing specs identified: expression hover triggers, multi-source attribution formatting (cap at 3), partial-proof display guidance, hover for rule/guard declarations themselves.
- What's right: shared assessment model, truth-based C92/C93 split, proven-violation-only C94 policy, silence on Unknown, no path for internal type names to reach hover.
- Full review in `.squad/decisions/inbox/elaine-proof-engine-review.md`.

### 2026-04-24 — docs.next/ full design review (document UX & navigability lens)
- Reviewed all 4 READMEs (top-level, compiler/, language/, runtime/) and 3 pipeline docs (lexer, parser, type-checker) for usability, structural consistency, and information architecture.
- Verdict: **APPROVED** — the navigation layer is well-built and the structural alignment across pipeline docs is strong.
- Key strengths: README tables are consistent, reading orders match real learning paths, cross-references are dense and accurate, all referenced files exist (no dead-end navigation), AI agent navigability is excellent (list_dir + README → right doc in one hop).
- Structural consistency across lexer/parser/type-checker is now very close — same section skeleton, same heading hierarchy, same pattern of Design Principles → Architecture → domain sections → Error Recovery → Consumer Contracts → Deliberate Exclusions → Cross-References → Source Files.
- Nits: type-checker Overview has a mild structural redundancy (public surface described twice), and the top-level README doesn't mention the runtime/ folder's reading-order dependency on compiler docs.
- The folder structure (compiler/, language/, runtime/) is intuitive and maps well to consumer mental models.
- No blockers found. All navigation paths terminate at real files. The type-checker doc is implementable as a standalone blueprint.
