# George D2 done

- Updated the three `TypeCheckerAssignmentQualifierTests` exchangerate fixtures from `exchangerate from 'USD' to 'EUR'`-style syntax to the current `exchangerate in 'USD' to 'EUR'` form.
- `dotnet build src\Precept\Precept.csproj --no-restore` passed.
- `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo --filter "TypeCheckerAssignmentQualifier"` was blocked by pre-existing `TypedTransitionRow` constructor compile errors in `test\Precept.Tests\ProofEngineTests.cs` and `test\Precept.Tests\ProofLedgerTests.cs`, so the three qualifier tests could not be re-run to completion in this workspace.
