## Core Context

- Owns UX/design across README form, brand-spec presentation, semantic visual surfaces, and preview/mockup direction.
- Keeps visual decisions aligned with runtime semantics: Emerald/Amber/Rose are semantic runtime signals; Gold is a restrained brand accent.
- Historical summary (pre-2026-04-13): led README layout/hero passes, semantic-visual-system refinements, preview-concept explorations, and verdict-modifier UX analysis.

## Learnings

- README form work is mostly hierarchy, line economy, and plaintext resilience.
- Preview concepts must be validated against realistic sample complexity, not only small demo precepts.
- Verdict modifiers are strongest as subtle authored-intent cues layered beneath runtime outcomes.

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
