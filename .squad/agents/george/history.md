## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.
- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.

## Live Guidance

- Mutual-exclusion metadata must be symmetric; PRECEPT0011c makes one-way declarations non-buildable.
- Interpolation remains feasible, but Slice 2 is the dominant cost center and binder plus multiple LS walkers must ship with it to avoid checker/tooling drift.
- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.

## Historical Summary

- Earlier Track 2, parser-gap, proof-engine, and diagnostic-split detail was archived into `history-archive.md` during the 15 KB summarization pass.
- `.squad/decisions.md` remains the canonical chronology; this file now keeps only current implementation guidance and the newest merged outcomes.

## Recent Updates

### 2026-05-11T20:03:33Z — ConflictingModifiers implementation recorded
- George's canonical closeout is `DiagnosticCode.ConflictingModifiers = 120`, dedicated validator routing, and symmetric `MutuallyExclusiveWith` declarations on `Optional` and `Notempty`.
- The PRECEPT0011c symmetry requirement is now a durable implementation note for any future mutual-exclusion work.

### 2026-05-11T20:03:33Z — Interpolation LOE warning retained
- The interpolation plan is still feasible, but Slice 2 owns most of the complexity and the binder / language-server walker follow-through must be treated as in-scope work, not cleanup.
