# George currency catalog fix

**Date:** 2026-05-09T10:58:53.528-04:00
**Agent:** George

## Summary
- Synced `src/Precept/Language/CurrencyCatalog.cs` to the committed ISO 4217 XML snapshot.
- Added active fund and supranational codes: `BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, and `ZWG`.
- Removed catalog entries absent from the current XML snapshot: `ANG`, `BGN`, and `ZWL`.

## Test encoding decision
- `test/Precept.Tests/Language/CurrencyCatalogSyncTests.cs` now documents intentional XML-only exclusions with a case-insensitive `IntentionalExclusions` `FrozenSet<string>`.
- The exclusions are `XAU`, `XAG`, `XPT`, `XPD`, `XTS`, and `XXX`.
- Filtering applies only to `xmlCodesNotInCatalog`; `catalogCodesNotInXml` remains strict so withdrawn catalog entries still fail the sync check.

## Verification target
- `dotnet build src/Precept/Precept.csproj`
- `dotnet test test/Precept.Tests/ --filter CurrencyCatalogSyncTests`
