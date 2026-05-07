# Soup Nazi coverage gaps addressed

## Summary
- Closed the type-reference gap with new `TypeReferenceTests.cs` assertions over `CollectionTypeReference`, `ChoiceTypeReference`, and `CITypeReference` payloads.
- Closed the action-chain gap with new `ActionChainTests.cs` assertions over `ParsedAction` DU shapes for add, remove, enqueue, dequeue, push, pop, and clear.
- Closed the interpolation gap with new `InterpolationTests.cs` assertions over `InterpolatedStringExpression`, `TextSegment`, and `HoleSegment`, plus the plain-string boundary.
- Expanded existing parser suites for wildcard routing (`from any`, `modify all`, `omit all`), richer event arg lists, and negative expression recovery/diagnostic-code assertions.

## Files
- `test/Precept.Tests/Parser/TypeReferenceTests.cs`
- `test/Precept.Tests/Parser/ActionChainTests.cs`
- `test/Precept.Tests/Parser/InterpolationTests.cs`
- `test/Precept.Tests/Parser/ParserDirectConstructTests.cs`
- `test/Precept.Tests/Parser/ParserExpressionTests.cs`
- `test/Precept.Tests/Parser/ParserScopedConstructTests.cs`

## Notes
- Event arg coverage was aligned to the current `ArgumentListSlot` payload (`Name`, `Type`) rather than inventing nullable/modifier fields the parser does not currently carry.
- Negative-expression assertions pin the parser's real recovery behavior today: empty guards use `MissingExpression`; incomplete binary operands recover with the placeholder literal and `ExpectedToken`.
- Validation closed green with `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` at 2974 passing tests.
