# George findings — bare `<-` diagnostic and `ExpressionFormKind` exhaustiveness

**Date:** 2026-05-07
**Requested by:** Shane

## What changed

1. `ParseComputeExpression` no longer accepts `field X as T <-` without an expression.
   - Added a catalog-derived `ExpressionStartTokens` set from `ExpressionForms.All` (`!IsLeftDenotation` + `LeadTokens`).
   - After consuming `BackArrow`, the parser now validates that the next token can start an expression.
   - If not, it emits `DiagnosticCode.ExpectedToken` (`"expression"`) and recovers to the next construct boundary without manufacturing a fake compute expression node.

2. `ExpressionFormKind` coverage is now analyzer-enforced on the Pratt parser.
   - Added `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` to `ParserState`.
   - Added `[HandlesCatalogMember(...)]` coverage on the expression handlers that construct each parsed form:
     - `ParseLiteral`, `ParseInterpolatedString`, `ParseInterpolatedTypedConstant`
     - `ParseUnaryOperation`
     - `ParseIdentifierOrFunctionCall`
     - `ParseGrouped`
     - `ParseListLiteral`
     - `ParseConditional`
     - `ParseQuantifier`
     - `ParseCIFunctionCall`
     - `ParseMemberAccessOrMethodCall`
     - `ParsePostfixIs`
     - `ParseBinaryInfix`

## Sanity check

- Temporarily removed the `Quantifier` annotation.
- `dotnet build src\Precept\Precept.csproj --no-restore --nologo` failed with:
  - `PRECEPT0019: ParserState is missing [HandlesCatalogMember] coverage for 1 member(s): Quantifier`
- Restored the annotation immediately after confirming the analyzer fired.

## Validation

- `dotnet build src\Precept\Precept.csproj --no-restore --nologo` ✅
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` ✅
  - Result: **2810 passed, 0 failed**

## Notes

- Reused existing parse diagnostic `DiagnosticCode.ExpectedToken`; no new diagnostic code was needed.
- The compute-expression fix is behavior-only recovery tightening; no DSL surface change was introduced.
