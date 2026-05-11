# Kramer — semantic tokens delta fix 2

- **Timestamp:** 2026-05-11T02:20:00Z
- **Requester:** Shane

## Diagnosis
- The remaining live overlaps were **not** the qualified arg path anymore: `TypedArg.Span` already carries the arg-name span and qualified arg refs already use `expr.MemberSpan` / `ar.Site.Length == arg.Name.Length`.
- The real malformed tokens in the live samples came from two broader semantic reference sites:
  - `TransitionOutcome.Span` covered `-> transition StateName`, so the emitted state token started at the arrow and overlapped both the arrow token and `transition` keyword.
  - `FieldTargetSlot.Span` covered comma-separated field lists in access-mode / omit surfaces, so the first field reference token spanned the whole list and overlapped following punctuation / tokens.

## Decision
- Keep the defensive merge hardening in `ProjectMergedTokens`: filter invalid coordinates/lengths before sorting and deduplicate by `(Line, Character)` instead of `(Line, Character, Length)`.
- Fix the upstream semantic sites so the emitted tokens are correct before they reach OmniSharp:
  - add name-site spans on target slots,
  - add `TransitionOutcome.StateSpan`,
  - use those precise spans in NameBinder + TypeChecker reference/diagnostic emission.

## Validation
- `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --verbosity minimal` → **165 passed**.
- `dotnet build tools\Precept.LanguageServer\Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server --no-restore --verbosity minimal` → **succeeded**.
- Post-fix sample inspection: `loan-application.precept` overlaps = 0, `building-access-badge-request.precept` overlaps = 0.
