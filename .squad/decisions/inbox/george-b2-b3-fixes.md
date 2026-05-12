# George B2/B3 fixes

## B1 — hover routing
- **File:** `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` (`CreateHover` around lines 50-83, `TryCreateFieldDeclarationHover` around lines 170-191)
- **Wrong:** generic `type` / `action` help could return before construct cards, and field type tokens had no declaration-span route, so `money` / `set` inside field declarations bypassed the field card.
- **Changed:** moved rich-routing ahead of generic type/action help, and added a field declaration-span route for type tokens so declaration hovers win before generic help while preserving typed-constant and state-symbol precedence.

## B2 — field mutability omit blindness
- **File:** `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` (`GetFieldWriteMapByState` around lines 1659-1692, `IsFieldOmittedInState` around lines 1764-1767)
- **Wrong:** the global-writable fallback treated every state as writable and the final write map ignored omit-driven absence, so omitted states could render ✏️ instead of 🔒.
- **Changed:** computed omitted states first, filtered them out of both global-writable and explicit unconditional-write buckets, and treated `omit all` as omitting every field for field-level mutability summaries.

## B3 — state writable summary omit blindness
- **File:** `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` (`GetWritableFieldsForState` around lines 1695-1729)
- **Wrong:** the fallback state summary returned all globally writable fields without subtracting state-local omits, so omitted fields still appeared under `Writable here:`.
- **Changed:** added a per-state omitted-field set, returned no writable fields for `omit all`, and filtered omitted fields out of both explicit write-access results and the global-writable fallback.

## Test coverage
- **File:** `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs` (routing checks around lines 193-294, omit-mutability checks around lines 336-379 and 437-454)
- Added regression coverage for field declaration routing (`set`, `money`), transition-action routing (`add`), globally writable fields omitted to 🔒, and state summaries excluding omitted writable fields.
