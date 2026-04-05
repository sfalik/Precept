## Core Context

- Owns editorial/code review with emphasis on implementation honesty, clarity, and redundancy removal.
- Reviews should verify claims against code, samples, and tests rather than accept narrative at face value.
- Hero snippets and README examples must be compact, compile-credible, and free of decorative language that obscures the product claim.

## Recent Updates

### 2026-04-05 - Consolidation safety gate recorded
- Rejected direct merge, force-repoint, blind squash, and docs-only cherry-pick for the unrelated-history return to trunk.
- Confirmed the health checks were green at review time (dotnet build; dotnet test --no-build, 703/703) and approved only a freeze-and-curate cutover from a frozen SHA.
