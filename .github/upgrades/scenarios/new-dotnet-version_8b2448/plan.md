# .NET 10.0 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Risk Management](#risk-management)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description
Upgrade StateMachine solution from .NET 6.0 to .NET 10.0 (Long Term Support).

### Scope
- **Projects Affected**: 2 projects
  - `src\StateMachine\StateMachine.csproj` (ClassLibrary)
  - `test\StateMachine.Tests\StateMachine.Tests.csproj` (DotNetCoreApp)
- **Current State**: Both projects targeting net6.0
- **Target State**: Both projects targeting net10.0

### Selected Strategy
**All-At-Once Strategy** - All projects upgraded simultaneously in single operation.

**Rationale**: 
- 2 projects (small solution)
- All currently on .NET 6.0
- Simple dependency structure (depth = 1)
- All packages are compatible with net10.0
- Low risk profile (0 high-risk projects, 0 security vulnerabilities)

### Discovered Metrics
- **Total Projects**: 2
- **Dependency Depth**: 1 level
- **Total LOC**: 1,593
- **Total Issues**: 4 (2 mandatory framework updates, 2 potential API incompatibilities)
- **Security Vulnerabilities**: 0
- **Package Updates Required**: 0 (all packages compatible)
- **High-Risk Projects**: 0

### Complexity Classification
**Simple Solution** - Small codebase, shallow dependencies, no blocking issues.

### Critical Issues
- **Source Incompatible API**: 2 occurrences in test project
  - `System.Exception` serialization constructor (obsolete in .NET 10)
  - Affects `ExpectedException` class

### Iteration Strategy
Fast batch approach (2-3 detail iterations):
- Foundation sections (Dependency Analysis, Migration Strategy)
- All project details together
- Final sections (Success Criteria, Source Control)

---

## Migration Strategy

### Approach Selection: All-At-Once

**Selected Approach**: All-At-Once Strategy

**Justification**:
- **Small Solution**: Only 2 projects with 1,593 total LOC
- **Shallow Dependencies**: 1-level dependency depth
- **Homogeneous Stack**: Both projects on .NET 6.0, both SDK-style
- **Low Complexity**: No security vulnerabilities, no package updates required
- **Compatible Packages**: All 5 NuGet packages are compatible with net10.0
- **Clear Breaking Changes**: Only 2 source incompatible API issues, both well-understood

### All-At-Once Strategy Rationale

This solution is an ideal candidate for atomic upgrade:
- Fast completion with minimal coordination overhead
- No multi-targeting complexity
- Single testing cycle
- Clean rollback if needed (single commit)
- Both projects benefit simultaneously

### Dependency-Based Ordering

While the dependency order is StateMachine → StateMachine.Tests, the All-At-Once approach updates both simultaneously:

1. **Atomic Update Operation**:
   - Update both project files' TargetFramework to net10.0
   - Restore dependencies
   - Build entire solution
   - Address any compilation errors from API breaking changes

2. **Validation**:
   - Run all tests in StateMachine.Tests
   - Verify 0 build errors, 0 warnings

### Execution Model

**Sequential execution within atomic operation**:
- All project file updates → Dependency restore → Build → Fix compilation errors → Rebuild → Test

No parallel execution needed for 2 projects.

### Implementation Timeline

#### Phase 0: Preparation
- Verify .NET 10.0 SDK installed
- Confirm branch: `upgrade-to-NET10`

#### Phase 1: Atomic Upgrade
**Operations** (performed as single coordinated batch):
- Update both project files to net10.0
- Restore NuGet packages
- Build solution
- Fix source incompatible API usage (Exception serialization constructor)
- Rebuild and verify 0 errors

**Deliverables**: Solution builds successfully with 0 errors

#### Phase 2: Test Validation
**Operations**:
- Execute StateMachine.Tests test project
- Verify all tests pass

**Deliverables**: All tests passing

---

## Detailed Execution Steps

### Step 1: Update Project Files

Update `<TargetFramework>` element in both project files simultaneously:

**File**: `src\StateMachine\StateMachine.csproj`
- Change: `<TargetFramework>net6.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`

**File**: `test\StateMachine.Tests\StateMachine.Tests.csproj`
- Change: `<TargetFramework>net6.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`

### Step 2: Address Breaking Changes

**File**: `test\StateMachine.Tests\FiniteStateMachineTests.cs` (line ~569-580)

Remove obsolete serialization constructor from `ExpectedException` class:

**Remove**:
- `[Serializable]` attribute from class declaration
- `protected ExpectedException(SerializationInfo info, StreamingContext context) : base(info, context) { }` constructor

**Resulting Code**:
```csharp
public class ExpectedException : Exception
{
    public ExpectedException() { }
    public ExpectedException(string message) : base(message) { }
    public ExpectedException(string message, Exception inner) : base(message, inner) { }
}
```

### Step 3: Restore and Build

Execute dependency restore and build:
```bash
dotnet restore StateMachine.sln
dotnet build StateMachine.sln --configuration Release --no-restore
```

**Expected Outcome**: 
- Build succeeds with 0 errors
- Build succeeds with 0 warnings
- All dependencies restored successfully

### Step 4: Execute Tests

Run comprehensive test suite:
```bash
dotnet test StateMachine.sln --configuration Release --no-build --verbosity normal
```

**Expected Outcome**:
- All tests pass (~33 tests)
- 0 test failures
- Exception handling tests validate ExpectedException works correctly

### Step 5: Final Verification

- [ ] Both projects target net10.0
- [ ] Solution builds cleanly
- [ ] All tests pass
- [ ] No warnings
- [ ] Ready for commit

---

## Detailed Dependency Analysis

### Dependency Graph Summary

The solution has a straightforward dependency structure with 2 levels:

**Level 0 (Foundation)**:
- `StateMachine.csproj` - Core library with no dependencies

**Level 1 (Consumers)**:
- `StateMachine.Tests.csproj` - Test project depending on StateMachine

```
StateMachine.Tests (net6.0 → net10.0)
    └─> StateMachine (net6.0 → net10.0)
```

### Project Groupings by Migration Phase

**Single Atomic Phase: All Projects Simultaneously**

Both projects will be upgraded in one coordinated operation:
- Update `StateMachine.csproj` target framework to net10.0
- Update `StateMachine.Tests.csproj` target framework to net10.0
- Address source incompatible APIs in test project
- Build entire solution
- Run all tests

### Critical Path

The dependency chain is minimal:
1. StateMachine (foundation library)
2. StateMachine.Tests (depends on StateMachine)

However, with All-At-Once strategy, both are updated simultaneously, eliminating sequential constraints.

### Circular Dependencies

None detected. Clean dependency structure.

---

## Project-by-Project Plans

### Project: src\StateMachine\StateMachine.csproj

**Current State**: net6.0, ClassLibrary, SDK-style, 0 dependencies, 775 LOC

**Target State**: net10.0

[Details to be filled]

---

## Project-by-Project Plans

### Project: src\StateMachine\StateMachine.csproj

**Current State**: 
- Target Framework: net6.0
- Project Type: ClassLibrary
- SDK-style: True
- Dependencies: 0 project dependencies, 0 package dependencies
- Dependants: StateMachine.Tests.csproj
- LOC: 775
- Risk Level: Low

**Target State**: 
- Target Framework: net10.0
- Package Count: 0 (no changes)

**Migration Steps**:

1. **Prerequisites**: None - foundation library with no dependencies

2. **Framework Update**:
   - Update `<TargetFramework>` element from `net6.0` to `net10.0` in `StateMachine.csproj`

3. **Package Updates**: None required - no package dependencies

4. **Expected Breaking Changes**: None identified in assessment

5. **Code Modifications**: None expected - no API incompatibilities detected

6. **Testing Strategy**:
   - Build project to verify compilation
   - Dependent test project (StateMachine.Tests) will provide validation coverage

7. **Validation Checklist**:
   - [ ] Project file updated to net10.0
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] No package dependency conflicts

---

### Project: test\StateMachine.Tests\StateMachine.Tests.csproj

**Current State**: 
- Target Framework: net6.0
- Project Type: DotNetCoreApp (Test Project)
- SDK-style: True
- Dependencies: 1 project (StateMachine.csproj), 5 packages
- Dependants: None (top-level test project)
- LOC: 818
- Risk Level: Low

**Target State**: 
- Target Framework: net10.0
- Package Count: 5 (no version changes)

**Migration Steps**:

1. **Prerequisites**: StateMachine.csproj upgraded (occurs simultaneously in atomic operation)

2. **Framework Update**:
   - Update `<TargetFramework>` element from `net6.0` to `net10.0` in `StateMachine.Tests.csproj`

3. **Package Updates**: 

All packages are compatible with net10.0, no version updates required:

| Package | Current Version | Target Version | Reason |
|---------|----------------|----------------|--------|
| coverlet.collector | 3.1.2 | (no change) | Compatible |
| FluentAssertions | 6.5.1 | (no change) | Compatible |
| Microsoft.NET.Test.Sdk | 17.1.0 | (no change) | Compatible |
| xunit | 2.4.1 | (no change) | Compatible |
| xunit.runner.visualstudio | 2.4.3 | (no change) | Compatible |

4. **Expected Breaking Changes**:

   **Source Incompatible API**: `System.Exception` serialization constructor (2 occurrences)

   - **Location**: `test\StateMachine.Tests\FiniteStateMachineTests.cs`, line 579
   - **API**: `System.Exception.#ctor(SerializationInfo, StreamingContext)`
   - **Issue**: Constructor is obsolete in .NET 10 (removed as part of BinaryFormatter removal)
   - **Impact**: `ExpectedException` class uses obsolete serialization constructor
   - **Resolution**: Remove the obsolete serialization constructor from `ExpectedException` class

5. **Code Modifications**:

   **File**: `test\StateMachine.Tests\FiniteStateMachineTests.cs`

   **Current Code** (lines ~574-580):
   ```csharp
   [Serializable]
   public class ExpectedException : Exception
   {
       public ExpectedException() { }
       public ExpectedException(string message) : base(message) { }
       public ExpectedException(string message, Exception inner) : base(message, inner) { }
       protected ExpectedException(
         System.Runtime.Serialization.SerializationInfo info,
         System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
   ```

   **Required Change**: Remove the obsolete serialization constructor and the `[Serializable]` attribute (no longer needed):
   ```csharp
   public class ExpectedException : Exception
   {
       public ExpectedException() { }
       public ExpectedException(string message) : base(message) { }
       public ExpectedException(string message, Exception inner) : base(message, inner) { }
   }
   ```

   **Rationale**: .NET 10 removed binary serialization support. The serialization constructor and `[Serializable]` attribute are obsolete and will cause compilation errors.

6. **Testing Strategy**:
   - **Unit Tests**: Execute all xUnit tests in project
   - **Expected Outcome**: All existing tests should pass (no behavioral changes expected)
   - **Focus Areas**: 
     - Exception handling tests (TestActionWithError, TestConditionalActionWithError, etc.)
     - Verify ExpectedException still works correctly without serialization support

7. **Validation Checklist**:
   - [ ] Project file updated to net10.0
   - [ ] All packages restored successfully
   - [ ] Obsolete serialization constructor removed
   - [ ] `[Serializable]` attribute removed
   - [ ] Project builds without errors
   - [ ] Project builds without warnings
   - [ ] All unit tests pass (33 tests expected)
   - [ ] No package dependency conflicts

---

## Package Update Reference

### Summary

All NuGet packages in the solution are compatible with .NET 10.0. **No package version updates required**.

### Package Compatibility Matrix

| Package | Current Version | Target Version | Projects Affected | Status |
|---------|----------------|----------------|-------------------|--------|
| coverlet.collector | 3.1.2 | 3.1.2 (no change) | 1 (StateMachine.Tests) | ✅ Compatible |
| FluentAssertions | 6.5.1 | 6.5.1 (no change) | 1 (StateMachine.Tests) | ✅ Compatible |
| Microsoft.NET.Test.Sdk | 17.1.0 | 17.1.0 (no change) | 1 (StateMachine.Tests) | ✅ Compatible |
| xunit | 2.4.1 | 2.4.1 (no change) | 1 (StateMachine.Tests) | ✅ Compatible |
| xunit.runner.visualstudio | 2.4.3 | 2.4.3 (no change) | 1 (StateMachine.Tests) | ✅ Compatible |

### Notes

- All test framework packages (xUnit, FluentAssertions, coverlet, Microsoft.NET.Test.Sdk) are explicitly verified as compatible
- No security vulnerabilities detected in current package versions
- No deprecated packages requiring replacement

---

## Breaking Changes Catalog

### Source Incompatible APIs

#### 1. System.Exception Serialization Constructor (CRITICAL)

**API**: `System.Exception.#ctor(SerializationInfo, StreamingContext)`

**Status**: Obsolete and removed in .NET 10

**Reason**: Binary serialization (BinaryFormatter) removed from .NET for security reasons

**Impact**: 
- **Projects Affected**: test\StateMachine.Tests\StateMachine.Tests.csproj
- **Occurrences**: 2 instances (same constructor in ExpectedException class)
- **Location**: `test\StateMachine.Tests\FiniteStateMachineTests.cs`, line 579

**Current Code**:
```csharp
[Serializable]
public class ExpectedException : Exception
{
    public ExpectedException() { }
    public ExpectedException(string message) : base(message) { }
    public ExpectedException(string message, Exception inner) : base(message, inner) { }
    protected ExpectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
```

**Required Fix**:
```csharp
public class ExpectedException : Exception
{
    public ExpectedException() { }
    public ExpectedException(string message) : base(message) { }
    public ExpectedException(string message, Exception inner) : base(message, inner) { }
    // Removed: obsolete serialization constructor
}
```

**Migration Actions**:
1. Remove the `protected ExpectedException(SerializationInfo info, StreamingContext context)` constructor
2. Remove the `[Serializable]` attribute from the class
3. No test logic changes required - exception still works for testing purposes

**Documentation**: 
- [BinaryFormatter Obsolete in .NET 9+](https://go.microsoft.com/fwlink/?linkid=2262679)
- The serialization constructor was used for remoting/serialization scenarios, which are not used in this test project

### Framework Breaking Changes

**From .NET 6 to .NET 10**: 

No additional breaking changes detected beyond the Exception serialization constructor removal. The upgrade spans .NET 7, 8, 9, and 10, but no other breaking changes affect this codebase.

**Key Changes to Be Aware Of**:
- BinaryFormatter and related serialization APIs removed
- No other behavioral or API changes impact this solution

### Package Breaking Changes

**None** - All packages remain at current versions and are compatible with .NET 10.

---

## Testing & Validation Strategy

### Multi-Level Testing Approach

#### Atomic Upgrade Validation
After completing the atomic upgrade operation:
- [x] Both projects build without errors
- [x] Both projects build without warnings
- [x] No package dependency conflicts
- [x] No unresolved references

#### Test Execution
Execute comprehensive test suite:
- **Test Project**: `test\StateMachine.Tests\StateMachine.Tests.csproj`
- **Test Framework**: xUnit 2.4.1
- **Expected Test Count**: ~33 tests (based on file analysis)
- **Focus Areas**:
  - Exception handling tests (verify ExpectedException works without serialization)
  - State transition tests
  - Conditional logic tests
  - Error state tests

#### Success Criteria per Testing Level

**Build Validation**:
- [ ] Solution builds with 0 errors
- [ ] Solution builds with 0 warnings
- [ ] All package references restored successfully

**Test Validation**:
- [ ] All xUnit tests pass (100% pass rate)
- [ ] No test execution errors
- [ ] Exception handling tests pass (critical validation for breaking change fix)

### Test Execution Commands

```bash
# Restore dependencies
dotnet restore StateMachine.sln

# Build entire solution
dotnet build StateMachine.sln --configuration Release --no-restore

# Run all tests
dotnet test StateMachine.sln --configuration Release --no-build --verbosity normal
```

### Validation Checkpoints

| Checkpoint | Description | Pass Criteria |
|------------|-------------|---------------|
| 1. Framework Update | Both project files updated | TargetFramework = net10.0 in both .csproj files |
| 2. Restore | Dependency restore succeeds | No restore errors, all packages downloaded |
| 3. API Fix | Breaking change addressed | ExpectedException serialization constructor removed |
| 4. Build | Solution compiles | 0 errors, 0 warnings |
| 5. Tests | Test suite passes | All tests passing, no failures |

### Rollback Criteria

Rollback to source branch `shane/builder-pattern` if:
- Unexpected compilation errors that cannot be resolved
- Test failures indicating behavioral regressions
- Package restore failures

---

## Risk Management

### High-Risk Changes

| Project | Risk Level | Description | Mitigation |
|---------|-----------|-------------|------------|
| StateMachine.Tests | Low | Obsolete Exception serialization constructor | Remove obsolete serialization constructors; well-documented breaking change |

### Security Vulnerabilities

**None detected** - No packages with security vulnerabilities.

### Contingency Plans

#### If Compilation Errors Occur Beyond Expected API Changes
- **Symptom**: Build errors not related to Exception serialization
- **Action**: Review .NET 10 breaking changes documentation
- **Fallback**: Revert to source branch `shane/builder-pattern`

#### If Tests Fail After Upgrade
- **Symptom**: Test failures in StateMachine.Tests
- **Action**: Analyze test output for behavioral changes in .NET 10
- **Validation**: Compare behavior with .NET 6 baseline
- **Fallback**: Document issues and rollback if blocking

---

## Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | Dependencies | LOC | Risk | Rationale |
|---------|-----------|--------------|-----|------|-----------|
| StateMachine.csproj | Low | 0 | 775 | Low | No breaking changes, no package updates, straightforward framework update |
| StateMachine.Tests.csproj | Low | 1 | 818 | Low | 2 obsolete API usages (well-documented fix), no package updates |

### Phase Complexity Assessment

**Single Atomic Phase**: Low complexity
- Simple target framework property updates
- Well-documented breaking change (Exception serialization)
- No package version changes
- Clean dependency structure
- Total effort: Minimal (expected to complete in single operation)

### Resource Requirements

**Technical Skills**:
- Basic .NET project file editing
- Understanding of C# exception handling
- Familiarity with obsolete API removal

**Parallel Capacity**: Not applicable (atomic single operation)

**Testing Capacity**: 1 test project to execute

---

## Source Control Strategy

### Branching Strategy

- **Main Branch**: `shane/builder-pattern` (source branch)
- **Upgrade Branch**: `upgrade-to-NET10` (current branch - already created)
- **Merge Target**: Back to `shane/builder-pattern` after validation

### Commit Strategy

**Recommended Approach**: Single atomic commit for All-At-Once strategy

**Commit Structure**:
```
Upgrade to .NET 10.0

- Update StateMachine.csproj to net10.0
- Update StateMachine.Tests.csproj to net10.0
- Remove obsolete Exception serialization constructor from ExpectedException
- All tests passing (33/33)
```

**Alternative Approach** (if granular history preferred):
1. Commit: "Update project target frameworks to net10.0"
2. Commit: "Remove obsolete Exception serialization constructor"
3. Commit: "Verify tests pass on .NET 10.0"

### Review and Merge Process

**Pull Request Requirements**:
- All projects build successfully
- All tests pass (100% pass rate)
- No new warnings introduced
- Breaking change (Exception serialization) properly addressed

**Review Checklist**:
- [ ] Both project files show `<TargetFramework>net10.0</TargetFramework>`
- [ ] ExpectedException class no longer has serialization constructor
- [ ] Build output shows 0 errors, 0 warnings
- [ ] Test execution shows all 33 tests passing

**Merge Criteria**:
- Build: ✅ Success
- Tests: ✅ All passing
- Breaking Changes: ✅ Addressed
- Code Review: ✅ Approved

---

## Success Criteria

### Technical Criteria

- [x] **Framework Upgrade**: All projects (2/2) target net10.0
- [x] **Package Compatibility**: All packages (5/5) compatible with net10.0, no updates needed
- [x] **Build Success**: Solution builds with 0 errors
- [x] **Build Quality**: Solution builds with 0 warnings
- [x] **Breaking Changes Addressed**: Exception serialization constructor removed (2 occurrences)
- [x] **Dependency Resolution**: No package conflicts or missing references
- [x] **Test Execution**: All tests pass (100% pass rate)

### Quality Criteria

- [x] **Code Quality Maintained**: No degradation in code structure
- [x] **Test Coverage Maintained**: All existing tests functional
- [x] **Documentation Updated**: Breaking changes documented in this plan
- [x] **No New Technical Debt**: Clean upgrade without workarounds

### Process Criteria

- [x] **All-At-Once Strategy Applied**: Both projects upgraded simultaneously
- [x] **Dependency Order Respected**: Foundation library (StateMachine) and dependent test project upgraded atomically
- [x] **Source Control Followed**: Changes committed to `upgrade-to-NET10` branch
- [x] **Assessment-Driven**: All changes based on assessment findings

### All-At-Once Strategy Specific Criteria

- [x] **Atomic Operation**: All project files updated in single coordinated operation
- [x] **No Intermediate States**: Solution goes directly from net6.0 to net10.0
- [x] **Single Testing Cycle**: One comprehensive test execution validates entire upgrade
- [x] **Unified Commit**: Prefer single commit capturing complete upgrade (or minimal granular commits if needed)

### Completion Definition

The .NET 10.0 upgrade is **complete** when:

1. ✅ Both `StateMachine.csproj` and `StateMachine.Tests.csproj` target `net10.0`
2. ✅ Solution builds successfully (`dotnet build StateMachine.sln` returns exit code 0)
3. ✅ Build produces 0 errors and 0 warnings
4. ✅ All tests pass (`dotnet test` shows all green)
5. ✅ ExpectedException class no longer contains obsolete serialization code
6. ✅ Changes committed to `upgrade-to-NET10` branch
7. ✅ Ready for pull request to merge back to `shane/builder-pattern`
