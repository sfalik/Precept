# George N1 fix

- **Status:** Processed — merged into `.squad/decisions.md` on 2026-05-15T23:14:11Z.

## Context

Frank's review on commit `85974302` flagged a correctness gap in `ResolveSlotSourceQualifierAxis(...)`: the early-return path treated unresolved slot holes as `Absent` even when the hole expression's type can carry the requested qualifier axis.

## Decision

Use `IsAssignmentQualifierAxisApplicable(...)` before that early return. If the hole expression's type can carry the requested axis, return `QualifierResolutionKind.Unknown`; otherwise keep `QualifierResolutionKind.Absent`.

## Scope

- Surgical runtime-only change in `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs`
- No behavior changes outside `ResolveSlotSourceQualifierAxis(...)`
- No new diagnostics; `PRE0141` remains the uncertainty signal

## Validation

- `dotnet test test\Precept.Tests\ --no-restore --nologo --verbosity minimal --tl:off`
  - Remains on the known branch baseline: 5655 passed / 9 failed / 5664 total
- `dotnet test test\Precept.Tests\ --no-restore --nologo --verbosity minimal --tl:off --filter "FullyQualifiedName~TypeCheckerAssignmentQualifierTests"`
  - Passed 55 / 55
