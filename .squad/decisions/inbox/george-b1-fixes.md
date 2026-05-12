# George B1 hover fixes

## Scope
- Fixed Frank's B1-B4 hover review blockers in `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`.
- Verified `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` did not need routing changes and had no shipped `proved` copy.
- Updated `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs` to the corrected compact-card contract.

## What changed
1. Replaced the verbose qualifier proof diagnostic hover with the compact 3-line card:
   - `⚠️ \`PRE....\` · ...`
   - `🔬 <use> · <expression>`
   - one evidence line summarizing left/right qualifier facts.
2. Replaced qualifier proof-expression hover output with compact 3-line cards:
   - proven path: `✅ Proven · ...`
   - gap path: `⚠️ Gap · ...`
3. Replaced qualifier declaration hover output with the compact `⚖️` card and removed the old axis/value/source/shape forensic dump.
4. Locked shared badge copy in `FormatStatus(...)` to:
   - `✅ Proven`
   - `⚡ Enforced`
   - `⚠️ Gap`
5. Changed the transition extra line label from `Proof gap:` to `Gap:`.
6. Normalized user-facing hover copy from `proved` to `proven` throughout the LS surface and matching tests.
7. Tightened proof-site snippet rendering so proof hover expressions no longer show padded parenthesis spacing like `( A - B )`.

## Validation
- Baseline before fix: `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --filter FullyQualifiedName~HoverHandlerTests --nologo` failed (`5` red tests at HEAD per Frank's verdict; local post-read baseline reproduced a failing suite before edits).
- After fix:
  - `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --filter FullyQualifiedName~HoverHandlerTests --nologo` → `41/41` passed
  - `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --nologo` → `269/269` passed
- Repo-wide check:
  - `dotnet test --nologo` still fails on unrelated existing baseline work in `test/Precept.Tests/TypeChecker/TypeCheckerTransitionTests.cs` (`TransitionRow_MultiStateFromList_MultipleUnknownStates_EmitsPerStateDiagnostic`: expected 2 `UndeclaredState`, saw 3). No runtime/type-checker changes were made in this fix pass.
