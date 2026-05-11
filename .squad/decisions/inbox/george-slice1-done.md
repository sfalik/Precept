# George — Slice 1 (Parser) Complete

## Files Changed

- `src/Precept/Language/ExpressionForms.cs` — added `InterpolatedTypedConstant = 15` enum member + catalog metadata entry; moved `TypedConstantStart` from `Literal` LeadTokens to the new form
- `src/Precept/Pipeline/ParsedExpression.cs` — added `InterpolatedTypedConstantExpression` record (mirrors `InterpolatedStringExpression`, reuses `InterpolationSegment`/`TextSegment`/`HoleSegment`)
- `src/Precept/Pipeline/Parser.Expressions.cs` — rewrote `ParseInterpolatedTypedConstant()` to produce full segment AST instead of flat `LiteralExpression`
- `src/Precept/Pipeline/NameBinder.cs` — added `InterpolatedTypedConstantExpression` arms in both `CollectFieldDependencies` and `WalkExpression` to walk holes for name binding
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — added `InterpolatedTypedConstantExpression` case in `Resolve` switch routing to a new stub (`ResolveInterpolatedTypedConstantExpressionStub`) that emits `TypeMismatch` + returns `TypedErrorExpression`, preserving crash-prevention behavior until Slice 2 implements proper resolution
- `test/Precept.Tests/Parser/InterpolatedTypedConstantTests.cs` — **new file**, 10 parser round-trip tests
- `test/Precept.Tests/Parser/ParserExpressionTests.cs` — updated catalog coverage theory data + count assertion (14 → 15)
- `test/Precept.Tests/ExpressionFormCatalogTests.cs` — updated count assertion (14 → 15)
- `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` — updated count assertion (14 → 15)

## Tests Added (10)

1. `SingleHole_WholeValue_ProducesThreeSegments` — `'{x}'`
2. `SingleHole_TrailingUnit_ProducesThreeSegments` — `'{x} kg'`
3. `SingleHole_LeadingNumber_ProducesThreeSegments` — `'100 {x}'`
4. `TwoHoles_SpaceSeparated_ProducesFiveSegments` — `'{x} {y}'`
5. `TwoHoles_TrailingText_ProducesFiveSegments` — `'{x} {y}/each'`
6. `ThreeHoles_SlashSeparated_ProducesSevenSegments` — `'{x} {y}/{z}'`
7. `CompoundTemporal_TwoHolesWithPlusSeparator_ProducesFiveSegments` — `'{n} days + {m} hours'`
8. `HoleWithBinaryExpression_ParsesCorrectly` — `'{x + 1} kg'`
9. `HoleWithMemberAccess_ParsesCorrectly` — `'{a.b} USD'`
10. `InterpolatedTypedConstant_HasCorrectFormKind` — verifies `ExpressionFormKind.InterpolatedTypedConstant`

## Design Decisions

- **Reused `InterpolationSegment`/`TextSegment`/`HoleSegment` types** from string interpolation. No new segment types — the parser is context-free; slot classification is Slice 2's job.
- **`TypedConstantStart` moved from Literal LeadTokens to InterpolatedTypedConstant LeadTokens** in the ExpressionForms catalog, so the catalog correctly reflects that interpolated typed constants are their own expression form, not a sub-case of Literal.
- **Added TC stub** to route the new AST node through `ResolveInterpolatedTypedConstantExpressionStub` instead of falling through to `ResolveUnknownExpression`. This preserves the existing crash-prevention diagnostic message that existing TC tests assert on.

## What Slice 2 (TypeChecker) Needs to Know

- The new AST node is `InterpolatedTypedConstantExpression(ImmutableArray<InterpolationSegment> Segments, SourceSpan Span)`.
- Segments alternate: `TextSegment` (literal text) → `HoleSegment` (expression) → `TextSegment` → ... Always starts and ends with `TextSegment` (may be empty text).
- For N holes, there are 2N+1 segments total.
- The TC stub at `ResolveInterpolatedTypedConstantExpressionStub` is the entry point to replace with proper slot classification and per-hole type checking.
- The existing `ResolveInterpolatedTypedConstantStub` (for `LiteralExpression` with `TypedConstantStart`) is now dead code — no parser path produces that shape anymore. It can be removed when Slice 2 ships.
