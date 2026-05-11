# Soup Nazi — t2-14 complete

- Slice: 14 — Test Layer — Catalog Capability Tests
- Completed: 2026-05-10
- Test commit: `7a4c2e31`
- Validation: `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore -m:1 /nr:false --nologo` passed at 4471/4471 (baseline 3925; +546 tests)

## What landed

- Added reflection-based `[Theory]` + `[MemberData]` coverage in `test\Precept.Tests\CatalogTests\` for operators, outcomes, modifiers, types, and diagnostics.
- Adapted assertions to the catalog surface that exists in source today:
  - operator symbols derive from `Token` / `Tokens`
  - modifier keywords derive from `Token.Text`
  - type serialization names currently come from `DisplayName` when `SerializedName` is absent
  - diagnostic recovery guidance currently comes from `RecoverySteps` / `FixHint` when `RecoveryHint` is absent

## Acceptance

- Every new test is catalog-driven, so adding a new member without filling required metadata now fails the suite.
- No skipped tests were required; the assertions adapt to the shipped catalog shapes instead of assuming plan-era property names.
