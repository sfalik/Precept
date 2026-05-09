# George Q1 — UCUM Tier 1 Curated Codes

**Date:** 2026-05-09  
**Author:** George (Runtime Dev)  
**Branch:** Precept-V2-Radical  
**Commits:** `03613099` (research) + runtime/catalog commit in this change set

---

## Decision

Precept Q1 now uses the curated UCUM Tier 1 set from `research/language/ucum-tier1-curation.md`: 150 business-domain codes spanning length, mass, volume, area, temperature, energy/power, pressure, speed, force, count/dimensionless, and plane angle.

## Locked Rules

- Remove standalone time atoms from Tier 1: `s`, `min`, `h`, `d` stay out because temporal quantities belong to NodaTime.
- Remove `mol` from Tier 1; it is Tier 2 unless business scope shifts toward chemistry/pharma-first workflows.
- Keep business-critical customary and special UCUM codes exactly case-sensitive, including bracketed/apostrophe forms such as `[degF]`, `[in_i'Hg]`, `[arb'U]`, `"'"`, and `"''"`.
- Tier 1 browsing must include prefixed and compact-exponent UCUM forms even when they are not direct `UcumAtomCatalog.All` keys; synthesize browse entries from UCUM parsing/normalization rather than shrinking the curated list.
- `LookupAtom` remains the full authoritative atom-catalog lookup and is not reduced to Tier 1.

## Files

- `research/language/ucum-tier1-curation.md`
- `src/Precept/Language/Ucum/UcumCatalog.cs`
- `test/Precept.Tests/Language/Ucum/UcumCatalogTests.cs`

## Validation

- `dotnet build src\Precept\Precept.csproj`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj`

## Outcome

Build passed, full runtime tests passed, and Tier 1 source count is now 150 with `s`/`mol` excluded.
