# George RC-3 Done

## What I found
- The PRE0069 inventory fallout was coming from assignment qualifier validation, not typed-constant form parsing.
- `QuantityTimesQuantity` already existed in the Operations catalog, but the checker still flattened binary expressions to leaf operands during assignment validation.
- That meant `qty[D] * qty[A/D]` compared both leaves directly to the target field and emitted false dimension mismatches instead of validating the product result.

## What I changed
- Added `ResultQualifierPolicy.CompoundUnitCancellation` to operation metadata and assigned it to `OperationKind.QuantityTimesQuantity`.
- Added `CompoundUnitCancellationRequired` as the typed-expression qualifier binding emitted for that policy.
- Updated assignment qualifier validation to derive the numerator unit/dimension for cancelling products (`A/B × B -> A` and `B × A/B -> A`) before recursing into child operands.
- Kept the fallback path intact so non-cancelling products still report mismatch diagnostics.
- Added 3 regression checks in `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs` (2 commutative cancellation rows + 1 non-cancelling guardrail).

## PRE0069 count
- Before RC-3 (Frank MCP baseline): 18 PRE0069 diagnostics in `samples/inventory-item.precept`.
- After RC-3 (`precept_compile` via MCP on current workspace): 0 PRE0069 diagnostics.

## inventory-item.precept result
- The sample dropped from 105 total diagnostics in Frank's post-RC baseline to 87 now.
- The expected PRE0069 drop landed.
- Remaining MCP diagnostics after RC-3 are: PRE0009 x4, PRE0018 x10, PRE0114 x73.

## Validation
- `dotnet build src/Precept/Precept.csproj --no-restore` ✅
- `dotnet test test/Precept.Tests/Precept.Tests.csproj --no-restore` is still blocked by the pre-existing `TypedTransitionRow` constructor fallout in `ProofEngineTests.cs` and `ProofLedgerTests.cs` (same unrelated failure seen at baseline).
- MCP checks run successfully with `precept_ping` + `precept_compile` using the repo-local MCP launcher.

## Edge cases
- RC-3 currently targets the intended single-denominator compound-unit form (`A/B × B -> A`, commutative both ways).
- Interpolated unit holes are handled symbolically by deriving `{Unit.dimension}` strings from `{Unit}` placeholders.
- Multi-slash compound units are intentionally left outside RC-3 scope; they still fall back to existing mismatch behavior.
