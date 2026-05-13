# Diagnostic Coverage Enforcement

> Working document — Frank, 2026-05-13

## Recommendation

**Option B: Convention test** — an xUnit source-scanning test that enumerates every `DiagnosticCode` member and verifies at least one emission-site reference exists in the pipeline or catalog-emission source files. This is the right first move because (a) the emission pattern is simple and uniform enough that source scanning is reliable, (b) it requires zero new infrastructure — the project already has `CatalogTestReflection` and `DiagnosticCatalogTests` as precedent, and (c) it delivers coverage immediately in the existing CI gate.

The Roslyn analyzer option (Option A) was seriously considered — `Precept.Analyzers` is mature with 26 existing analyzers — but the emission-site distinction problem makes it heavier than it looks. A `CompilationEndAction` analyzer would need to distinguish "referenced in the `Diagnostics.GetMeta` catalog" (not an emission) from "referenced in `Diagnostics.Create()` or `CIDiagnosticCode`" (an emission). That's tractable but adds complexity disproportionate to the enforcement value, since the test catches the same class of regressions at PR time rather than build time. The convention test is the pragmatic gate; a Roslyn analyzer can be added later if the pattern proves insufficient.

## Mechanism

A single test class `DiagnosticEmissionCoverageTests.cs` in `test/Precept.Tests/CatalogTests/`. It works as follows:

1. **Enumerate all codes.** `Enum.GetValues<DiagnosticCode>()` gives the complete set of defined diagnostic codes. This is the same reflection pattern already used in `DiagnosticsTests.AllDiagnosticCodes` and `CatalogTestReflection.AllDiagnostics()`.

2. **Define the emission-site source set.** These are the files where a `DiagnosticCode.X` reference constitutes an emission site:
   - `src/Precept/Pipeline/*.cs` — all pipeline stages (Lexer, Parser, NameBinder, TypeChecker, GraphAnalyzer, ProofEngine)
   - `src/Precept/Language/Operations.cs` — `CIDiagnosticCode` property on operation metadata
   - `src/Precept/Language/Functions.cs` — `CIDiagnosticCode` property on function metadata

3. **Exclude non-emission references.** These files reference `DiagnosticCode.X` but are NOT emission sites:
   - `src/Precept/Language/DiagnosticCode.cs` — the enum definition itself
   - `src/Precept/Language/Diagnostics.cs` — the `GetMeta` catalog (metadata, not emission)

4. **Scan.** For each `DiagnosticCode` member name, search the emission-site source files for the literal string `DiagnosticCode.{memberName}`. If zero matches, the code has no emission site.

5. **Report.** The test fails with a clear message listing every uncovered code, e.g.:
   ```
   The following 50 DiagnosticCode members have no emission site in any pipeline or catalog-emission file:
     - TemporalFieldMissingPeriod (PRE0069)
     - CrossCurrencyArithmetic (PRE0070)
     - ...
   ```

6. **Allow-list.** A `HashSet<DiagnosticCode>` of known-unemitted codes allows the test to pass today while tracking the debt. Each entry must have a comment explaining why it's allowed (e.g., "feature not yet implemented — tracked in #123"). The allow-list shrinks as gaps are closed.

## Scope of what it enforces

**"Covered" means:** The literal token `DiagnosticCode.{MemberName}` appears in at least one `.cs` file in the emission-site source set (pipeline files + catalog CIDiagnosticCode files), excluding the enum definition and metadata catalog.

**What passes:** A diagnostic code that appears in any of the three emission patterns:
- Direct emission: `Diagnostics.Create(DiagnosticCode.X, span, ...)` in any pipeline stage
- Catalog-mediated emission: `CIDiagnosticCode: DiagnosticCode.X` in Operations.cs or Functions.cs
- ProofEngine dispatch: `DiagnosticCode.X` in ProofEngine.Diagnostics.cs switch branches

**What fails:** A `DiagnosticCode` enum member with zero references in any emission-site source file that is not in the allow-list.

## What it does NOT catch

- **Dead code paths.** A diagnostic code referenced inside an unreachable `if` branch or a `case` arm that never matches still passes. The test proves *syntactic presence*, not *runtime reachability*.
- **Incorrect emission context.** A code referenced in a `FaultSiteLink` constructor or a comment would count as "present" even though neither is an actual emission. This is a minor risk — PRECEPT0003 already forces all real emissions through `Diagnostics.Create()`, so stray references outside that call are unlikely to be the *only* reference.
- **Test coverage.** This gate does not verify that any emitted diagnostic has a corresponding test proving it fires. That's Gate 2's job — see § Gate 2: Test Coverage Enforcement below.
- **Spec documentation.** Whether the diagnostic is documented in the language spec is outside scope.

## Implementation notes for George/Kramer

### File to create
`test/Precept.Tests/CatalogTests/DiagnosticEmissionCoverageTests.cs`

### Structure

```csharp
public sealed class DiagnosticEmissionCoverageTests
{
    // Known-unemitted codes — shrinks as gaps close.
    // Each entry must cite the tracking issue or reason.
    private static readonly HashSet<DiagnosticCode> AllowList = new()
    {
        // Temporal domain — not yet implemented
        DiagnosticCode.TemporalFieldMissingPeriod,      // PRE0069
        // ... all 50 from the gap analysis ...
    };

    [Fact]
    public void Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed()
    {
        // 1. Get repo root (walk up from test assembly location to find Precept.slnx)
        // 2. Build emission-site file list:
        //    - All .cs files under src/Precept/Pipeline/
        //    - src/Precept/Language/Operations.cs
        //    - src/Precept/Language/Functions.cs
        // 3. Read and concatenate their text content
        // 4. For each DiagnosticCode member:
        //    - Search for $"DiagnosticCode.{memberName}" in the concatenated text
        //    - If not found and not in AllowList → add to violations
        // 5. Assert violations is empty, with descriptive failure message
    }

    [Fact]
    public void AllowList_Contains_Only_Actually_Unemitted_Codes()
    {
        // Inverse check: if a code IS emitted, it should NOT be in the allow-list.
        // This prevents the allow-list from going stale when gaps are closed.
    }
}
```

### Key method to target
The scan targets `DiagnosticCode.{MemberName}` as a literal string in source files. This works because:
- PRECEPT0003 enforces that all emissions go through `Diagnostics.Create(DiagnosticCode code, ...)` — so `DiagnosticCode.X` always appears at the call site
- CIDiagnosticCode assignments write `CIDiagnosticCode: DiagnosticCode.X` — the same pattern
- ProofEngine dispatch uses `DiagnosticCode.X` in switch branches — the same pattern

### Enumerating DiagnosticCode members
`Enum.GetValues<DiagnosticCode>()` — same pattern as `DiagnosticsTests.AllDiagnosticCodes`.

### Expected failure output
```
DiagnosticEmissionCoverageTests.Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed
  Expected no uncovered diagnostic codes, but found 3:
    - DiagnosticCode.NewFeatureGap (not in allow-list)
    - DiagnosticCode.AnotherNewCode (not in allow-list)
    - DiagnosticCode.ThirdOne (not in allow-list)
  Add emission sites in the pipeline, or add to the allow-list with a tracking comment.
```

### Repo root discovery
Walk up from `Assembly.GetExecutingAssembly().Location` until finding `Precept.slnx`. Use `Path.Combine(repoRoot, "src", "Precept", "Pipeline")` etc. Pattern already used in other source-reading tests if any exist; otherwise straightforward.

## Catalog evolution note

**Option D — catalog-declared emission ownership — has genuine long-term merit** and should not be dismissed.

`DiagnosticMeta` already declares `DiagnosticStage` (Lex/Parse/Type/Graph/Proof), which tells you *which pipeline stage* owns the diagnostic. Adding an `EmittedBy` or `EmissionSite` property — or simply a boolean `IsImplemented` flag — would let the catalog itself declare whether a diagnostic is live or aspirational. A catalog validation test could then check that every entry marked `IsImplemented = true` has an actual emission site, and that every `IsImplemented = false` entry is tracked in an issue.

This is the architecturally correct long-term answer because it follows the metadata-driven principle: the catalog declares truth, enforcement derives from it. But it requires touching all 132 catalog entries to add the new field, and doesn't actually prevent the gap — it just moves the enforcement locus. The convention test is the right first step. If the allow-list grows unwieldy or the team wants build-time enforcement, evolve to the catalog approach.

A Roslyn analyzer (Option A, as PRECEPT0027) is the natural middle evolution: it would fire at build time in the IDE, catching regressions before `dotnet test` runs. If the convention test proves its value and the team wants tighter feedback, promote the logic to a `CompilationEndAction` analyzer in `Precept.Analyzers`. The emission-site detection pattern (find `IFieldReferenceOperation` on `DiagnosticCode` members inside `Diagnostics.Create()` invocations or `CIDiagnosticCode` assignments) is well-understood from this analysis.

## Gate 2: Test Coverage Enforcement

Gate 1 answers "can this diagnostic fire?" Gate 2 answers "has anyone proved it fires correctly?" A diagnostic with an emission site but no test is emittable-but-unverified behavior — the most dangerous class of gap because it looks covered but isn't.

### Mechanism

A second test method in the same `DiagnosticEmissionCoverageTests` class. It enumerates every `DiagnosticCode` member, checks whether at least one test file references it, and fails if an emitted diagnostic has no test.

1. **Enumerate all codes.** Same `Enum.GetValues<DiagnosticCode>()` as Gate 1.

2. **Define the test-file scan set.** All `.cs` files under:
   - `test/Precept.Tests/`
   - `test/Precept.LanguageServer.Tests/`
   - `test/Precept.Mcp.Tests/`

3. **Scan.** For each `DiagnosticCode` member name, search the test-file scan set for the literal string `DiagnosticCode.{memberName}`. If zero matches, the code has no test.

4. **Filter.** Only flag codes that **pass Gate 1** (have an emission site) but fail Gate 2 (no test reference). Codes with no emission site are already on the Gate 1 allow-list — they trivially have no test obligation because there's nothing to test.

5. **Report.** The test fails listing every emitted-but-untested code:
   ```
   DiagnosticEmissionCoverageTests.Every_Emitted_DiagnosticCode_Has_A_Test
     Expected every emitted diagnostic code to be referenced in at least one test file, but found 2 uncovered:
       - DiagnosticCode.NewFeatureGap (emitted in TypeChecker.cs but no test references it)
       - DiagnosticCode.AnotherCode (emitted in Parser.cs but no test references it)
     Write a test that asserts this diagnostic fires, or add to the Gate 2 allow-list with a tracking comment.
   ```

### What counts as "covered by a test"

**Option A: literal `DiagnosticCode.{MemberName}` reference in a test file.** This is the signal.

A test "covers" a diagnostic if any `.cs` file in the test scan set contains the literal token `DiagnosticCode.{MemberName}`. This catches:

- Direct assertion: `diagnostics.Should().ContainSingle(d => d.Code == DiagnosticCode.TypeMismatch)`
- Collection filtering: `diagnostics.Where(d => d.Code == DiagnosticCode.X)`
- Helper method arguments: `AssertDiagnostic(result, DiagnosticCode.UndeclaredField)`
- Expected-failure lists: `new[] { DiagnosticCode.DuplicateFieldName, ... }`

**What it does NOT require:**

- The test does not need to prove the diagnostic fires on a specific input — only that a test author has written a test that references the code. This is a presence check, not a behavioral verification.
- The test does not need to be in any particular test class or follow any naming convention.

**Why not PRE#### string matching?** Searching for `"PRE0055"` would catch tests that reference diagnostics by their display code. However, this pattern is fragile (display codes can renumber), less common in the test suite (the vast majority of tests use `DiagnosticCode.X` enum references), and would require maintaining a mapping from PRE numbers to member names. The enum reference is both more reliable and more prevalent. If a future test style uses PRE strings exclusively, the scan can be extended.

### Scan target

**All three test projects** — `test/Precept.Tests/`, `test/Precept.LanguageServer.Tests/`, and `test/Precept.Mcp.Tests/`.

Justification: A diagnostic might be tested only through the language server (e.g., code actions that respond to specific diagnostics) or through MCP tool output validation. Restricting to `Precept.Tests/` alone would miss legitimate coverage in downstream test projects. The scan is cheap (text search across test `.cs` files) and the broader scope eliminates false negatives without introducing false positives.

### Allow-list

The Gate 2 allow-list is **structurally empty today.** Here's why:

**Current state (verified by source scan, 2026-05-13):**

- **83 diagnostic codes** have emission sites in the pipeline or catalog-emission files.
- **Of those 83, every single one** also has at least one `DiagnosticCode.{MemberName}` reference in the test scan set.
- **0 emitted-but-untested codes exist today.** Gate 2 passes clean with an empty allow-list.

The 49 codes on the Gate 1 allow-list (no emission site) are excluded from Gate 2's enforcement scope — they have no emission to test. Notably, 42 of those 49 unemitted codes already have proactive test references in the test suite (tests written in anticipation of future implementation). The remaining 7 have neither emission nor test:

| Code | Notes |
|------|-------|
| `AmbiguousTypedConstant` | Unreachable due to single-candidate resolution |
| `EventHandlerDoesNotSupportGuard` | Spec gap — not yet implemented |
| `EventHandlerInStatefulPrecept` | Spec gap — not yet implemented |
| `OmitDoesNotSupportGuard` | Spec gap — not yet implemented |
| `OutOfRange` | Runtime-only — no compile-time emission planned |
| `PreEventGuardNotAllowed` | Spec gap — not yet implemented |
| `RedundantAccessMode` | Spec gap — not yet implemented |

These 7 are a subset of the Gate 1 allow-list and need no separate Gate 2 allow-listing — they're excluded by the "only flag codes that pass Gate 1" filter.

**When the allow-list becomes non-empty:** If a developer adds a new emission site without writing a test, Gate 2 fails. The developer must either write a test or add the code to the Gate 2 allow-list with a tracking comment. The allow-list is separate from Gate 1's allow-list because the two gates enforce different properties (emission existence vs. test existence).

### Implementation notes for George/Kramer

**File:** Same file as Gate 1 — `test/Precept.Tests/CatalogTests/DiagnosticEmissionCoverageTests.cs`

**Test method:**

```csharp
[Fact]
public void Every_Emitted_DiagnosticCode_Has_A_Test()
{
    // Gate 2 allow-list — emitted codes that intentionally lack tests.
    // Each entry must cite a tracking reason.
    // Currently empty — all emitted codes have test coverage.
    var gate2AllowList = new HashSet<DiagnosticCode>
    {
    };

    // 1. Get repo root (same helper as Gate 1)
    // 2. Determine which codes are emitted (same scan as Gate 1 — extract to shared helper)
    // 3. Build test-file scan set:
    //    - All .cs files under test/Precept.Tests/
    //    - All .cs files under test/Precept.LanguageServer.Tests/
    //    - All .cs files under test/Precept.Mcp.Tests/
    // 4. Read and concatenate their text content
    // 5. For each DiagnosticCode member:
    //    - Skip if NOT emitted (Gate 1 scope — no test obligation)
    //    - Search for $"DiagnosticCode.{memberName}" in the concatenated test text
    //    - If not found and not in gate2AllowList → add to violations
    // 6. Assert violations is empty, with descriptive failure message
}
```

**Shared helper:** Extract the "find emitted codes" scan into a private helper method so Gate 1 and Gate 2 reuse the same emission-site detection. This prevents the two gates from drifting on what counts as "emitted."

```csharp
private static HashSet<DiagnosticCode> GetEmittedCodes(string repoRoot)
{
    // Scan Pipeline/*.cs + Operations.cs + Functions.cs
    // Return set of DiagnosticCode members found
}
```

**Inverse staleness check (companion to Gate 1's inverse test):**

```csharp
[Fact]
public void Gate2_AllowList_Contains_Only_Actually_Untested_Codes()
{
    // If a code IS referenced in test files, it should NOT be in the Gate 2 allow-list.
    // Prevents staleness when tests are added for previously-untested codes.
}
```

**Expected test class structure:**

```csharp
public sealed class DiagnosticEmissionCoverageTests
{
    private static readonly HashSet<DiagnosticCode> Gate1AllowList = new() { /* 49 unemitted codes */ };
    private static readonly HashSet<DiagnosticCode> Gate2AllowList = new() { /* currently empty */ };

    [Fact] public void Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed() { /* Gate 1 */ }
    [Fact] public void AllowList_Contains_Only_Actually_Unemitted_Codes() { /* Gate 1 inverse */ }
    [Fact] public void Every_Emitted_DiagnosticCode_Has_A_Test() { /* Gate 2 */ }
    [Fact] public void Gate2_AllowList_Contains_Only_Actually_Untested_Codes() { /* Gate 2 inverse */ }

    private static HashSet<DiagnosticCode> GetEmittedCodes(string repoRoot) { /* shared */ }
    private static string GetRepoRoot() { /* shared */ }
}
```

### Relationship to Gate 1

**One test class, two gates, four test methods.** Justification:

- The two gates share infrastructure: repo-root discovery, emission-site scanning, and `DiagnosticCode` enumeration. A shared helper avoids duplication.
- The two allow-lists are conceptually distinct (Gate 1: "no emission site", Gate 2: "emission site exists but no test") but they live in the same enforcement context.
- The two inverse staleness checks are structurally identical — just operating on different allow-lists and scan sets.
- Separate test classes would force either duplicated helpers or a shared base class. Neither is justified for four tightly related tests.

**Execution order:** Gate 2 depends on Gate 1's emission-site scan to know which codes are in scope. If Gate 1 fails (a code has no emission site and isn't allow-listed), Gate 2's result for that code is undefined. In practice, both gates run independently — Gate 2 skips unemitted codes regardless of Gate 1's allow-list status.

## Open questions

1. **Allow-list granularity.** Should the initial Gate 1 allow-list contain all 49 unemitted codes from the gap analysis, or should we start with a smaller set and force immediate triage of the rest? If all 49, should each cite a specific tracking issue or is a cluster comment ("temporal domain — not yet implemented") sufficient?

2. **Pipeline-only vs. all-source scanning.** The recommended emission-site scan set covers `Pipeline/*.cs` + `Operations.cs` + `Functions.cs`. If future emission patterns emerge outside these files (e.g., a new `RuntimeValidator`), the scan set needs updating. Should the test scan all of `src/Precept/**/*.cs` minus the known non-emission files instead? Broader scan = fewer false negatives but more false positives from non-emission references.

3. **Doc-comment false positives.** The emission-site scan picks up `DiagnosticCode.X` references in XML doc comments (e.g., `/// <see cref="DiagnosticCode.X"/>`). These are not real emissions. Should the scan strip comments before matching, or is this edge case rare enough to ignore? Currently only one instance exists (`DiagnosticCode.X` as a generic placeholder in `GraphAnalyzer.cs` doc comments) and `X` is not a real enum member.
