# George — Fund Codes Excluded From CurrencyCatalog

**By:** George  
**Date:** 2026-05-09T11:07:24.986-04:00  
**Status:** Inbox  
**Requested by:** Shane  

---

## Summary

`CurrencyCatalog` contains only transactional currencies used in business workflows. ISO 4217 fund codes and accounting units do not belong in the runtime catalog even when they appear in the XML reference file.

## Decision

Exclude `BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, and `ZWG` from `src/Precept/Language/CurrencyCatalog.cs`.

Keep those codes in `test/Precept.Tests/Language/CurrencyCatalogSyncTests.cs` as intentional XML-only exclusions alongside precious metals (`XAU`, `XAG`, `XPT`, `XPD`), the ISO testing code (`XTS`), and the no-currency placeholder (`XXX`).

## Rationale

These entries are fund codes or accounting units rather than transactional currencies carried through normal business workflows. The runtime catalog should model the transactional business-domain surface, while the sync test documents intentional differences from the full ISO 4217 XML payload.
