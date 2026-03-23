# Constraint Violation — Implementation Plan

Date: 2026-03-22
Spec: `docs/ConstraintViolationDesign.md` (violation model), session naming decisions (all 9 topics)
Prerequisite: Language redesign and MCP server complete. 486 tests passing.

This plan implements two tightly coupled changes in a single pass:

1. **Naming renames** — 15 type renames, 2 enum rewrites (new members + value renames), 3 method renames, 1 property rename, 1 file rename. All agreed in the naming consistency review session.
2. **Structured constraint violations** — Replace flat `IReadOnlyList<string> Reasons` with `IReadOnlyList<ConstraintViolation> Violations` carrying typed targets and sources. Add `IsSuccess` to result types.

These are combined because the violation model introduces `ConstraintViolation` which replaces `PreceptViolation` (a rename target), and the outcome enum rewrites add the new `ConstraintFailure` member that the violation model requires. Doing them separately would mean touching every result type and test twice.

---

## Guiding Principles

1. **Phase order follows dependency.** Model types first (no consumers), then runtime (the producer), then consumers (LS, MCP, tests).
2. **Build at every checkpoint.** Each phase ends with `dotnet build` passing. Tests may temporarily fail during the rename phases — that's expected and resolved by the end of Phase 5.
3. **Use IDE rename refactoring where possible.** Symbol renames (`F2` / rename symbol) are safer than find-replace for type and member names. Fall back to text replacement only for string literals (MCP tool output, test assertions on string values).
4. **No behavioral changes in rename phases.** Phases 0–2 are pure renames — no new logic, no changed semantics. Phase 3 adds the new violation model. Phase 4 wires it through. Phase 5 updates tests.
5. **Hard cut.** No backward-compat aliases. The old names cease to exist.

---

## Reference: Complete Rename Table

### Type Renames

| Old Name | New Name | File |
|----------|----------|------|
| `PreceptAssertPreposition` | `AssertAnchor` | PreceptModel.cs |
| `PreceptStateAssert` | `StateAssertion` | PreceptModel.cs |
| `PreceptEventAssert` | `EventAssertion` | PreceptModel.cs |
| `PreceptRejection` | `Rejection` | PreceptModel.cs |
| `PreceptStateTransition` | `StateTransition` | PreceptModel.cs |
| `PreceptNoTransition` | `NoTransition` | PreceptModel.cs |
| `PreceptOutcomeKind` | `TransitionOutcome` | PreceptRuntime.cs |
| `PreceptUpdateOutcome` | `UpdateOutcome` | PreceptRuntime.cs |
| `PreceptFireResult` | `FireResult` | PreceptRuntime.cs |
| `PreceptUpdateResult` | `UpdateResult` | PreceptRuntime.cs |
| `PreceptEventInspectionResult` | `EventInspectionResult` | PreceptRuntime.cs |
| `PreceptInspectionResult` | `InspectionResult` | PreceptRuntime.cs |
| `PreceptViolation` | DELETED (replaced by `ConstraintViolation`) | PreceptRuntime.cs |
| `PreceptCompileValidationResult` | `CompileResult` | PreceptTypeChecker.cs |
| `ConstraintCatalog` | `DiagnosticCatalog` | ConstraintCatalog.cs → DiagnosticCatalog.cs |

### Enum Value Renames

**`TransitionOutcome`** (was `PreceptOutcomeKind`):

| Old | New |
|-----|-----|
| `Accepted` | `Transition` |
| `AcceptedInPlace` | `NoTransition` |
| `Rejected` | `Rejected` |
| *(new)* | `ConstraintFailure` |
| `NotApplicable` | `Unmatched` |
| `NotDefined` | `Undefined` |

**`UpdateOutcome`** (was `PreceptUpdateOutcome`):

| Old | New |
|-----|-----|
| `Updated` | `Update` |
| `Blocked` | `ConstraintFailure` |
| `NotAllowed` | `UneditableField` |
| `Invalid` | `InvalidInput` |

### Method Renames

| Old | New | File |
|-----|-----|------|
| `CollectValidationViolations` | `CollectConstraintViolations` | PreceptRuntime.cs |
| `EvaluateEventAsserts` | `EvaluateEventAssertions` | PreceptRuntime.cs |
| `EvaluateStateAsserts` | `EvaluateStateAssertions` | PreceptRuntime.cs |

### Property Renames

| Type | Old Property | New Property |
|------|-------------|-------------|
| `StateAssertion` (was `PreceptStateAssert`) | `Preposition` | `Anchor` |
| `PreceptStateAction` | `Preposition` | `Anchor` |

### Factory Method Renames

| Type | Old Factory | New Factory |
|------|------------|------------|
| `FireResult` | `Accepted()` | `Transitioned()` |
| `FireResult` | `AcceptedInPlace()` | `NoTransition()` |
| `FireResult` | `NotDefined()` | `Undefined()` |
| `FireResult` | `NotApplicable()` | `Unmatched()` |
| `EventInspectionResult` | `Accepted()` | `Transitioned()` |
| `EventInspectionResult` | `AcceptedInPlace()` | `NoTransition()` |
| `EventInspectionResult` | `NotDefined()` | `Undefined()` |
| `EventInspectionResult` | `NotApplicable()` | `Unmatched()` |
| `UpdateResult` | `Updated()` | `Succeeded()` |

### File Renames

| Old Path | New Path |
|----------|----------|
| `src/Precept/Dsl/ConstraintCatalog.cs` | `src/Precept/Dsl/DiagnosticCatalog.cs` |

---

## Phase 0: Model Type Renames (PreceptModel.cs)

**Goal:** Rename all model-layer types and the `Preposition` → `Anchor` property. No runtime or consumer changes yet.

### Steps

1. Rename `PreceptAssertPreposition` → `AssertAnchor` (enum, 3 members unchanged: `In`, `To`, `From`).
2. Rename `PreceptStateAssert` → `StateAssertion`. Rename property `Preposition` → `Anchor`.
3. Rename `PreceptEventAssert` → `EventAssertion`.
4. Rename `PreceptRejection` → `Rejection`.
5. Rename `PreceptStateTransition` → `StateTransition`.
6. Rename `PreceptNoTransition` → `NoTransition`.
7. In `PreceptStateAction`, rename property `Preposition` → `Anchor`.
8. Update all references in `PreceptModel.cs`, `PreceptParser.cs`, `PreceptTypeChecker.cs`, `PreceptRuntime.cs`, and consumer files.

### Affected files

| File | Changes |
|------|---------|
| `src/Precept/Dsl/PreceptModel.cs` | Type declarations + property renames |
| `src/Precept/Dsl/PreceptParser.cs` | Construction sites for model records |
| `src/Precept/Dsl/PreceptTypeChecker.cs` | `.Preposition` → `.Anchor`, enum references |
| `src/Precept/Dsl/PreceptRuntime.cs` | Pattern matches, key construction, enum comparisons |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | `.Preposition` → `.Anchor` filter |
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | `.Preposition` → `.Anchor` formatting |
| `tools/Precept.Mcp/Tools/SchemaTool.cs` | `PreceptRejection`/`PreceptStateTransition`/`PreceptNoTransition` pattern matches |
| `tools/Precept.Mcp/Tools/AuditTool.cs` | Same outcome type pattern matches |
| `test/Precept.Tests/NewSyntaxParserTests.cs` | `.Preposition` → `.Anchor` in assertions |

### Checkpoint

- `dotnet build` passes (entire solution)
- No references to `PreceptAssertPreposition`, `PreceptStateAssert`, `PreceptEventAssert`, `PreceptRejection`, `PreceptStateTransition`, `PreceptNoTransition`, or `.Preposition` on assert/action types remain

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 0 for the full rename list.

Perform the following renames in src/Precept/Dsl/PreceptModel.cs and all consumers:

1. PreceptAssertPreposition → AssertAnchor (enum)
2. PreceptStateAssert → StateAssertion (record)
3. PreceptEventAssert → EventAssertion (record)
4. PreceptRejection → Rejection (record)
5. PreceptStateTransition → StateTransition (record)
6. PreceptNoTransition → NoTransition (record)
7. Property .Preposition → .Anchor on StateAssertion and PreceptStateAction

Use IDE rename (vscode_renameSymbol) for each type and property to catch all references
automatically. After all renames, verify with grep that zero references to the old names
remain anywhere in the codebase (excluding docs/ and archive/).

Build the solution after all renames. Run all tests to verify no behavioral change.

When finished, update the Status Tracker table at the bottom of this document.
```

---

## Phase 1: Result Type & Enum Renames (PreceptRuntime.cs)

**Goal:** Rename all runtime result types and outcome enums, including enum value renames and factory method renames.

### Steps

1. Rename `PreceptOutcomeKind` → `TransitionOutcome`. Rename members:
   - `Accepted` → `Transition`
   - `AcceptedInPlace` → `NoTransition`
   - `Rejected` → `Rejected` (unchanged)
   - `NotApplicable` → `Unmatched`
   - `NotDefined` → `Undefined`
   - **Add new member:** `ConstraintFailure`

2. Rename `PreceptUpdateOutcome` → `UpdateOutcome`. Rename members:
   - `Updated` → `Update`
   - `Blocked` → `ConstraintFailure`
   - `NotAllowed` → `UneditableField`
   - `Invalid` → `InvalidInput`

3. Rename `PreceptFireResult` → `FireResult`. Rename factories:
   - `Accepted()` → `Transitioned()`
   - `AcceptedInPlace()` → `NoTransition()`
   - `NotDefined()` → `Undefined()`
   - `NotApplicable()` → `Unmatched()`
   - `Rejected()` stays

4. Rename `PreceptEventInspectionResult` → `EventInspectionResult`. Same factory renames as above.

5. Rename `PreceptInspectionResult` → `InspectionResult`.

6. Rename `PreceptUpdateResult` → `UpdateResult`. Rename factory `Updated()` → `Succeeded()`.

7. Delete `PreceptViolation` record (will be replaced by `ConstraintViolation` in Phase 3).

8. Update all call sites, pattern matches, and string literal comparisons.

### Critical: String literal updates

These files map enum values to string output and must be updated manually (not caught by symbol rename):

| File | What to update |
|------|---------------|
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | `outcomeKinds` static array — string names |
| `tools/Precept.Mcp/Tools/RunTool.cs` | Abort condition pattern match |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | Success condition pattern match |
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | Outcome-to-UI-string switch |

### Affected files (besides PreceptRuntime.cs)

All test files that assert on outcome values (~234 references to `PreceptOutcomeKind` across source, test, and tool files):
- `test/Precept.Tests/PreceptWorkflowTests.cs`
- `test/Precept.Tests/NewSyntaxRuntimeTests.cs`
- `test/Precept.Tests/PreceptCollectionTests.cs`
- `test/Precept.Tests/PreceptSetParsingTests.cs`
- `test/Precept.Tests/PreceptEditTests.cs`
- `test/Precept.Tests/PreceptExpressionRuntimeEvaluatorBehaviorTests.cs`
- `test/Precept.Tests/PreceptRulesTests.cs`
- `test/Precept.LanguageServer.Tests/*.cs`
- `test/Precept.Mcp.Tests/*.cs` (if they exist)

### Checkpoint

- `dotnet build` passes
- All tests pass (enum value renames are caught because tests assert on specific members)
- No references to old enum names remain

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 1 for the full rename list.

This is the heaviest phase — ~234 references to PreceptOutcomeKind, plus all
result types. Strategy:

1. Use IDE rename (vscode_renameSymbol) for each type name first:
   PreceptOutcomeKind → TransitionOutcome
   PreceptUpdateOutcome → UpdateOutcome
   PreceptFireResult → FireResult
   PreceptEventInspectionResult → EventInspectionResult
   PreceptInspectionResult → InspectionResult
   PreceptUpdateResult → UpdateResult

2. Then rename enum members one by one (vscode_renameSymbol handles these):
   TransitionOutcome.Accepted → .Transition
   TransitionOutcome.AcceptedInPlace → .NoTransition
   TransitionOutcome.NotApplicable → .Unmatched
   TransitionOutcome.NotDefined → .Undefined
   UpdateOutcome.Updated → .Update
   UpdateOutcome.Blocked → .ConstraintFailure
   UpdateOutcome.NotAllowed → .UneditableField
   UpdateOutcome.Invalid → .InvalidInput

3. Add TransitionOutcome.ConstraintFailure as a new member.

4. Rename factory methods on FireResult and EventInspectionResult:
   Accepted() → Transitioned()
   AcceptedInPlace() → NoTransition()
   NotDefined() → Undefined()
   NotApplicable() → Unmatched()
   Updated() → Succeeded() (on UpdateResult)

5. Delete the PreceptViolation record entirely.

6. Manually update string literals in MCP tools and language server that
   map outcome enum values to output strings (grep for the old string values).

Build and run all tests after completion. Fix any test failures caused by
string-based assertions on the old enum value names.

When finished, update the Status Tracker table at the bottom of this document.
```

---

## Phase 2: Catalog & Compile Result Renames

**Goal:** Rename `ConstraintCatalog` → `DiagnosticCatalog` (class + file) and `PreceptCompileValidationResult` → `CompileResult`.

### Steps

1. Rename file: `src/Precept/Dsl/ConstraintCatalog.cs` → `src/Precept/Dsl/DiagnosticCatalog.cs`.
2. Rename class: `ConstraintCatalog` → `DiagnosticCatalog`.
3. Update all `ConstraintCatalog.` references across the codebase (~50+ sites in parser, runtime, MCP tools).
4. Rename `PreceptCompileValidationResult` → `CompileResult` (internal type, ~5 references).

### Affected files

| File | Changes |
|------|---------|
| `src/Precept/Dsl/DiagnosticCatalog.cs` (renamed) | Class declaration |
| `src/Precept/Dsl/PreceptParser.cs` | All `ConstraintCatalog.Cnn` references |
| `src/Precept/Dsl/PreceptRuntime.cs` | Runtime constraint references |
| `src/Precept/Dsl/PreceptTypeChecker.cs` | `PreceptCompileValidationResult` → `CompileResult` |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | `ConstraintCatalog.Constraints` |
| `tools/Precept.Mcp/Tools/ValidateTool.cs` | If it references the catalog |

### Important

`ConstraintViolationException` stays — this is a different concept (a compile-time/parse-time exception thrown by the `DiagnosticCatalog` infrastructure). It is not related to the runtime `ConstraintViolation` model. The name is correct as-is because it represents a violation of a language constraint (a registered diagnostic).

### Checkpoint

- `dotnet build` passes
- All tests pass
- Zero references to `ConstraintCatalog` (as a class name) or `PreceptCompileValidationResult` remain
- File `ConstraintCatalog.cs` no longer exists

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 2 for the rename list.

1. Rename the file src/Precept/Dsl/ConstraintCatalog.cs → DiagnosticCatalog.cs
   (use terminal: git mv src/Precept/Dsl/ConstraintCatalog.cs src/Precept/Dsl/DiagnosticCatalog.cs)

2. Use IDE rename to rename the class ConstraintCatalog → DiagnosticCatalog.
   This will update all ~50+ references across parser/runtime/MCP.

3. Use IDE rename to rename PreceptCompileValidationResult → CompileResult.

4. Do NOT rename ConstraintViolationException — it stays as-is (different concept).

Build and run all tests. When finished, update the Status Tracker table.
```

---

## Phase 3: Runtime Method Renames

**Goal:** Rename the three private runtime methods and add `IsSuccess` to result types.

### Steps

1. Rename `CollectValidationViolations` → `CollectConstraintViolations` in `PreceptRuntime.cs`.
2. Rename `EvaluateEventAsserts` → `EvaluateEventAssertions` in `PreceptRuntime.cs`.
3. Rename `EvaluateStateAsserts` → `EvaluateStateAssertions` in `PreceptRuntime.cs`.
4. Add `IsSuccess` computed property to `FireResult`:
   ```csharp
   public bool IsSuccess => Outcome is TransitionOutcome.Transition
                         or TransitionOutcome.NoTransition;
   ```
5. Add `IsSuccess` computed property to `EventInspectionResult`:
   ```csharp
   public bool IsSuccess => Outcome is TransitionOutcome.Transition
                         or TransitionOutcome.NoTransition;
   ```
6. Add `IsSuccess` computed property to `UpdateResult`:
   ```csharp
   public bool IsSuccess => Outcome is UpdateOutcome.Update;
   ```

### Checkpoint

- `dotnet build` passes
- All tests pass (no behavioral change — `IsSuccess` is additive)

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 3.

1. Rename 3 private methods in PreceptRuntime.cs:
   CollectValidationViolations → CollectConstraintViolations
   EvaluateEventAsserts → EvaluateEventAssertions
   EvaluateStateAsserts → EvaluateStateAssertions

2. Add bool IsSuccess computed property to FireResult, EventInspectionResult,
   and UpdateResult per the design doc shapes in docs/ConstraintViolationDesign.md.

Build and run all tests. When finished, update the Status Tracker table.
```

---

## Phase 4: Structured Constraint Violations

**Goal:** Introduce `ConstraintViolation`, `ConstraintTarget`, `ConstraintSource`, and `ExpressionSubjects`. Replace `IReadOnlyList<string> Reasons` with `IReadOnlyList<ConstraintViolation> Violations` on all result types. Split `Rejected` into `Rejected` (explicit author reject) vs `ConstraintFailure` (constraint failures).

This is the main behavioral change phase.

### Steps

#### A. New types

1. Add `ConstraintTarget` hierarchy (enum + abstract record + 5 sealed subtypes) to `PreceptModel.cs` or a new `ConstraintViolation.cs` file.
2. Add `ConstraintSource` hierarchy (enum + abstract record + 4 sealed subtypes).
3. Add `ConstraintViolation` sealed record.
4. Add `ExpressionSubjects` record.

#### B. Compile-time subject extraction

5. In `PreceptCompiler` (or a new helper), walk each constraint's expression AST to extract `ExpressionSubjects`:
   - `PreceptIdentifierExpression` without dot → field reference (or arg in event assert scope).
   - `PreceptIdentifierExpression` with dot → arg reference.
6. Store `ExpressionSubjects` on each `PreceptInvariant`, `StateAssertion`, `EventAssertion`, and `PreceptTransitionRow` (WhenGuard subjects, if needed later).

#### C. Runtime violation production

7. Update `EvaluateEventAssertions` to return `IReadOnlyList<ConstraintViolation>` with:
   - `Source`: `EventAssertionSource` (expression text, reason, event name, source line)
   - `Targets`: Expression-referenced args as `EventArgTarget` + `EventTarget` scope

8. Update `EvaluateStateAssertions` to return `IReadOnlyList<ConstraintViolation>` with:
   - `Source`: `StateAssertionSource` (expression text, reason, state, anchor, source line)
   - `Targets`: Expression-referenced fields as `FieldTarget` + `StateTarget` scope

9. Update `CollectConstraintViolations` (invariant checking) to return `IReadOnlyList<ConstraintViolation>` with:
   - `Source`: `InvariantSource` (expression text, reason, source line)
   - `Targets`: Expression-referenced fields as `FieldTarget` + `DefinitionTarget` scope

10. Update `reject` outcome handling to produce `ConstraintViolation` with:
    - `Source`: `TransitionRejectionSource` (reason, event name, source line)
    - `Targets`: `EventTarget` scope only

#### D. Result type property change

11. Change `FireResult.Reasons` → `FireResult.Violations` (type: `IReadOnlyList<ConstraintViolation>`).
12. Change `EventInspectionResult.Reasons` → `EventInspectionResult.Violations`.
13. Change `UpdateResult.Reasons` → `UpdateResult.Violations`.
14. Update all factory methods to accept and pass `Violations` instead of `Reasons`.

#### E. Outcome split

15. Wire `TransitionOutcome.ConstraintFailure` into the fire pipeline:
    - Event assert failures → `Rejected` (these are authored assertions, effectively rejects)
    - Invariant/state assert failures post-mutation → `ConstraintFailure`
    - Explicit `reject` row outcome → `Rejected`
16. Wire `UpdateOutcome.ConstraintFailure` into the update pipeline:
    - Invariant/state assert failures post-edit → `ConstraintFailure` (was `Blocked`)

### Design decision: Event assert outcome

Event asserts use `Rejected` (not `ConstraintFailure`) because they are author-intentional pre-checks on args — semantically equivalent to a reject. `ConstraintFailure` is reserved for post-mutation constraint violations where the outcome couldn't be predicted before execution.

### Affected files

| File | Changes |
|------|---------|
| `src/Precept/Dsl/PreceptModel.cs` (or new file) | New types: `ConstraintViolation`, `ConstraintTarget`, `ConstraintSource`, `ExpressionSubjects` |
| `src/Precept/Dsl/PreceptRuntime.cs` | Violation production, result type properties, factory methods, outcome split |
| `src/Precept/Dsl/PreceptCompiler.cs` | Expression subject extraction (if it exists) or inline in runtime |
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | `.Reasons` → `.Violations`, adapt to `ConstraintViolation` shape |
| `tools/Precept.Mcp/Tools/RunTool.cs` | `.Reasons` → `.Violations`, add `ConstraintFailure` to abort condition |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | `.Reasons` → `.Violations` |

### Checkpoint

- `dotnet build` passes
- Tests **will fail** — they assert on `Reasons` property and specific outcome values. That's expected; Phase 5 fixes them.

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationDesign.md for the full type model and scenarios.
Read docs/ConstraintViolationImplementationPlan.md Phase 4 for the step-by-step plan.

This phase introduces the structured constraint violation model:

1. Add new types: ConstraintTarget (hierarchy), ConstraintSource (hierarchy),
   ConstraintViolation, ExpressionSubjects — per the design doc.

2. Implement compile-time expression subject extraction: walk each constraint's
   expression AST to record which fields and args it references.

3. Update the three constraint evaluation methods to produce ConstraintViolation
   objects instead of plain strings. Each violation carries:
   - Message (the because reason)
   - Source (which constraint produced it, with expression text and source line)
   - Targets (expression-referenced subjects + scope target)

4. Change Reasons → Violations on all result types (FireResult,
   EventInspectionResult, UpdateResult).

5. Split the old Rejected outcome:
   - Author's explicit reject → Rejected
   - Invariant/state assert failures → ConstraintFailure
   - Event assert failures → Rejected (they are authored pre-checks)

6. Update factory methods accordingly.

Do NOT update tests in this phase — they will fail and that's expected.
Do NOT update language server or MCP tools beyond what's needed to compile.

Build the solution. Expect test failures. When finished, update the Status Tracker.
```

---

## Phase 5: Test Migration

**Goal:** Update all test files to use the new type names, enum values, property names, and structured violations.

### Strategy

1. **Mechanical renames** — Find-replace on test assertion patterns:
   - `.Outcome.Should().Be(PreceptOutcomeKind.Accepted)` → `.Outcome.Should().Be(TransitionOutcome.Transition)`
   - `.Reasons` → `.Violations`
   - `.Reasons.Should().ContainSingle(...)` → `.Violations.Should().ContainSingle().Which.Message.Should().Be(...)`

2. **Outcome value updates** — Tests that currently assert `Rejected` for constraint failures must be updated:
   - Post-mutation invariant/assert failures → `TransitionOutcome.ConstraintFailure`
   - Explicit reject outcomes → `TransitionOutcome.Rejected` (unchanged)
   - Post-edit constraint failures → `UpdateOutcome.ConstraintFailure`

3. **New test additions** — Add tests for:
   - `IsSuccess` property on each result type
   - `ConstraintViolation` target correctness (at least one test per scenario in the design doc)
   - `ConstraintFailure` vs `Rejected` distinction

### Affected test files

| File | Approximate changes |
|------|-------------------|
| `test/Precept.Tests/PreceptWorkflowTests.cs` | Outcome assertions, Reasons → Violations |
| `test/Precept.Tests/NewSyntaxRuntimeTests.cs` | Same |
| `test/Precept.Tests/PreceptCollectionTests.cs` | Same |
| `test/Precept.Tests/PreceptSetParsingTests.cs` | Model type assertions |
| `test/Precept.Tests/PreceptEditTests.cs` | UpdateOutcome values, Reasons → Violations |
| `test/Precept.Tests/PreceptRulesTests.cs` | Outcome + violation assertions |
| `test/Precept.Tests/PreceptExpressionRuntimeEvaluatorBehaviorTests.cs` | Outcome assertions |
| `test/Precept.Tests/NewSyntaxParserTests.cs` | Model type assertions (done in Phase 0) |
| `test/Precept.LanguageServer.Tests/*.cs` | Preview handler outcome strings |

### Checkpoint

- `dotnet build` passes
- **All tests pass** (486+ existing, plus new violation tests)
- Zero references to old type names, enum values, or `Reasons` property in test code

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 5 for the test migration plan.

Update all test files to use the new naming and violation model:

1. Replace all old type name references with new names.
2. Replace all old enum value references with new values.
3. Replace .Reasons assertions with .Violations assertions.
4. Update tests that asserted Rejected for constraint failures to assert
   ConstraintFailure instead (only post-mutation failures — explicit rejects stay).
5. Add new tests for IsSuccess on each result type.
6. Add at least one structured violation test per scenario in ConstraintViolationDesign.md
   (Scenarios 1-8).

Build and run all tests. Every test must pass. When finished, update the Status Tracker.
```

---

## Phase 6: Language Server, Visualizer & MCP Consumer Updates

**Goal:** Update all non-test consumers to fully use the new types, violation structure, and outcome values — including the preview/inspector visualizer.

### Steps

#### A. Preview handler (C#)

1. **PreceptPreviewHandler.cs** — Update outcome switch to use `TransitionOutcome.Transition`, `.NoTransition`, `.Unmatched`, `.Undefined`, `.ConstraintFailure`. Update `.Reasons` → `.Violations` (extract `.Message` for UI display). Handle the new `ConstraintFailure` outcome — map it to an appropriate UI string (e.g. `"constraintFailure"` or fold into `"blocked"` depending on UI needs).

#### B. Preview protocol DTOs

2. **PreceptPreviewProtocol.cs** — Update `PreceptPreviewEventStatus.Reasons` (currently `IReadOnlyList<string>`) to carry violation messages. Consider whether to keep as `IReadOnlyList<string>` (extracting `.Message` from `ConstraintViolation` in the handler) or expose structured data. The webview currently expects string arrays, so the simplest path is: keep the DTO field as `IReadOnlyList<string>` and map `.Violations.Select(v => v.Message)` in the handler.
   - Update `PreceptPreviewSnapshot.ActiveRuleViolations` (currently passed as `null`) — this is the natural place to surface structured violation data in the visualizer once the model is implemented.
   - Update `PreceptPreviewEditableField.Violation` — currently reads `PreceptViolation.Reason`. Replace with `ConstraintViolation.Message`.

#### C. Webview (JavaScript)

3. **inspector-preview.html** — Update the `toHostEventStatus()` function that reads `reasons`/`Reasons` from the protocol payload. If the DTO field name changes, update the JavaScript property access. Update `getEvaluationReason()` which renders violation messages — the `'blocked'` string check may need updating if the outcome string changes for `ConstraintFailure`.
   - Update any outcome string comparisons: the handler maps `PreceptOutcomeKind` values to webview strings (`"enabled"`, `"noTransition"`, `"blocked"`, `"notApplicable"`, `"undefined"`). If the outcome strings sent to the webview change, update the JavaScript event evaluation logic, button rendering, and CSS class assignments.
   - Add handling for the new `ConstraintFailure` outcome in the event dock rendering.

#### D. MCP tools

4. **LanguageTool.cs** — Update `outcomeKinds` static array to use new string names: `"Transition"`, `"NoTransition"`, `"Rejected"`, `"ConstraintFailure"`, `"Unmatched"`, `"Undefined"`. Update fire pipeline stage 6 description. Update `DiagnosticCatalog.Constraints` reference.

5. **RunTool.cs** — Update abort condition to include `ConstraintFailure`. Update `Reasons` → `Violations` in step output.

6. **InspectTool.cs** — Update success condition. Update `Reasons` → `Violations` in event output.

#### E. Tests

7. **MCP test files** — Update any MCP integration tests that assert on outcome strings or violation shapes.

8. **LS test files** — Update `PreceptPreviewRulesTests` and other LS tests that assert on preview handler output, outcome strings, or violation shapes.

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | Outcome switch, `.Violations`, `ConstraintFailure` handling |
| `tools/Precept.LanguageServer/PreceptPreviewProtocol.cs` | DTO field types for violations |
| `tools/Precept.VsCode/webview/inspector-preview.html` | JS outcome strings, reason display, `ConstraintFailure` handling |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | Outcome strings, DiagnosticCatalog |
| `tools/Precept.Mcp/Tools/RunTool.cs` | Abort condition, Violations |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | Success condition, Violations |
| `test/Precept.LanguageServer.Tests/*.cs` | Preview test assertions |
| `test/Precept.Mcp.Tests/*.cs` | MCP test assertions |

### Checkpoint

- `dotnet build` passes
- All tests pass (including MCP tests and LS tests)
- MCP tools output the correct new outcome strings
- Preview visualizer renders events correctly with new outcome values and violation messages

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 6.

Update the language server, visualizer, and MCP tool consumers:

1. PreceptPreviewHandler.cs — new outcome enum values in switch, .Violations,
   handle ConstraintFailure outcome mapping
2. PreceptPreviewProtocol.cs — update violation DTO fields (keep as string arrays
   for webview compat, map .Violations.Select(v => v.Message) in handler)
3. inspector-preview.html — update JS outcome string comparisons, reason display,
   add ConstraintFailure handling in event dock rendering
4. LanguageTool.cs — new outcome strings in static array, DiagnosticCatalog ref
5. RunTool.cs — add ConstraintFailure to abort condition, .Violations
6. InspectTool.cs — update success condition, .Violations
7. MCP and LS test files — update assertions

Build and run all tests. Manually verify the preview panel renders correctly
if possible. When finished, update the Status Tracker.
```

---

## Phase 7: Documentation & Cleanup

**Goal:** Final sync pass across all documentation and verify zero stale references.

### Steps

1. Update `docs/@ToDo.md` — replace the "Finish the validation-attribution design" and "Implement structured validation issues" items with a "Done" note or remove.
2. Update `docs/CleanupAndNextSteps.md` — add a new Item 7 entry for this work.
3. Grep the entire codebase (excluding `docs/archive/`) for any remaining old names. Fix stragglers.
4. Verify `README.md` code examples still compile conceptually with the new types.
5. Update session memory with completion status.

### Checkpoint

- Zero references to old names outside `docs/archive/`
- All docs consistent with implementation
- All tests pass

### Prompt

```
Before starting, suggest which AI model is best suited for this task and why,
then wait for me to confirm or switch models.

Read docs/ConstraintViolationImplementationPlan.md Phase 7.

1. Update docs/@ToDo.md — mark the validation design items as done.
2. Update docs/CleanupAndNextSteps.md — add Item 7 for this naming + violation work.
3. Grep the entire codebase for any remaining old names (exclude docs/archive/).
4. Verify README code examples match the new API surface.

Build and run all tests one final time. When finished, update the Status Tracker.
```

---

## Status Tracker

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Model type renames | Not started | 6 types + 2 property renames |
| 1 | Result type & enum renames | Not started | ~234 references for outcome enum across source/test/tools |
| 2 | Catalog & compile result renames | Not started | File rename + ~50 class references |
| 3 | Runtime method renames + IsSuccess | Not started | 3 methods + 3 computed properties |
| 4 | Structured constraint violations | Not started | New types + behavioral change |
| 5 | Test migration | Not started | All test files updated + new tests |
| 6 | LS, visualizer & MCP consumer updates | Not started | Handler + protocol + webview + MCP tools + tests |
| 7 | Documentation & cleanup | Not started | Final sweep |

---

## File Change Summary

| File | Action | Phase |
|------|--------|-------|
| `src/Precept/Dsl/PreceptModel.cs` | Major edit — 6 type renames, 2 property renames, new constraint types | 0, 4 |
| `src/Precept/Dsl/PreceptRuntime.cs` | Major edit — result types, enums, factories, violation production | 1, 3, 4 |
| `src/Precept/Dsl/PreceptParser.cs` | Edit — model type references, DiagnosticCatalog | 0, 2 |
| `src/Precept/Dsl/PreceptTypeChecker.cs` | Edit — CompileResult rename, Anchor property | 0, 2 |
| `src/Precept/Dsl/ConstraintCatalog.cs` | **Rename** → `DiagnosticCatalog.cs`, class rename | 2 |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | Edit — Anchor property reference | 0 |
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | Edit — outcome switch, Violations | 0, 1, 6 |
| `tools/Precept.LanguageServer/PreceptPreviewProtocol.cs` | Edit — violation DTO fields | 6 |
| `tools/Precept.VsCode/webview/inspector-preview.html` | Edit — JS outcome strings, reason display, ConstraintFailure | 6 |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | Edit — outcome strings, DiagnosticCatalog | 1, 2, 6 |
| `tools/Precept.Mcp/Tools/RunTool.cs` | Edit — outcome patterns, Violations | 1, 6 |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | Edit — outcome patterns, Violations | 1, 6 |
| `tools/Precept.Mcp/Tools/SchemaTool.cs` | Edit — model type references | 0 |
| `tools/Precept.Mcp/Tools/AuditTool.cs` | Edit — model type references | 0 |
| `test/Precept.Tests/*.cs` (7 files) | Major edit — all assertions | 0, 1, 5 |
| `test/Precept.LanguageServer.Tests/*.cs` | Edit — outcome assertions | 5 |

## Estimated Scope

| Phase | Changed LOC (est.) | Risk |
|-------|-------------------|------|
| 0. Model type renames | ~80 | Low (mechanical) |
| 1. Result type & enum renames | ~400 | Low-Medium (high volume, mechanical) |
| 2. Catalog & compile result renames | ~60 | Low (mechanical) |
| 3. Runtime method renames + IsSuccess | ~30 | Low |
| 4. Structured constraint violations | ~300 | Medium (new types + behavioral change) |
| 5. Test migration | ~500 | Medium (high volume + new tests) |
| 6. LS, visualizer & MCP consumer updates | ~120 | Low-Medium (JS + C# + protocol) |
| 7. Documentation & cleanup | ~30 | Low |
| **Total** | **~1,480** | |
