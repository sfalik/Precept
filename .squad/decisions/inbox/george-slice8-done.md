# George Slice 8 Done — Parser catalog rewires

## What shipped

- Rewired parser member-name vocabulary to derive from token catalog metadata (`Tokens.All.Where(m => m.IsValidAsMemberName)`), with `TokenKind.At` included through catalog derivation.
- Added scoped-construct support for optional pre-verb guards (`when ...`) for `StateEnsure` and `StateAction`, including disambiguation that scans past guard expressions.
- Added optional post-action `ensure ... because ...` parsing for `EventHandler` when construct metadata enables it.
- Reworked event-argument parsing to use full `ParseTypeReference()` and declaration-site-aware value modifiers, including `default <expr>`.
- Made `ParseFieldTarget()` consume comma-separated identifiers and preserve broadcast-field metadata handling.
- Extended type parsing for `log ... by ...` optional `ascending|descending` ordering token capture.
- Enabled string-expression parsing in `because` and `reject` argument positions (plain + interpolated token sequences).
- Removed computed-field forward-reference rejection from binder/type checker declaration-order checks; computed references now resolve across all fields and cycles report `CircularComputedField`.
- Propagated expected type context for typed constants in action value/key/index expression resolution.
- Updated diagnostics text for `DefaultForwardReference` to "Computed expression ...".

## Validation

- `dotnet build src\\Precept\\Precept.csproj` ✅
- `dotnet test test\\Precept.Tests\\Precept.Tests.csproj` ✅ (3869/3869 passing)

## Follow-ups

- No missing prerequisite catalog fields were encountered in this implementation pass.
