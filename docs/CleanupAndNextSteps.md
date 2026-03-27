# Precept — Cleanup & Next Steps Plan

> Generated from a comprehensive review of the implementation against design documents.
> Each work item includes a session prompt — copy it into a new conversation to begin that item.

---

## Status Tracker

| # | Work Item | Status | Notes |
|---|-----------|--------|-------|
| 0 | Remove Legacy / Backward Compat Code | Done | All legacy types/properties removed from model, parser, runtime, language server. 4 backward-compat tests deleted, 2 ported. 344 tests passing (348 - 4). Zero references to PreceptTransition, PreceptClause, PreceptRule, TopLevelRules, or .Rules remain in source/test code. |
| 1 | Fix README.md | Done | Updated DSL example (precept/invariant/on assert/flat rows), C# API (PreceptParser.Parse, PreceptCompiler.Compile, PreceptEngine, instance.InstanceData), and Pillars section (rule→invariant). |
| 2 | Fix RuntimeApiDesign.md | Done | No Dsl* renames needed — document already used correct Precept* naming throughout. All API signatures verified against PreceptRuntime.cs: PreceptParser, PreceptCompiler, PreceptEngine (Name/States/InitialState/Events/Fields/CollectionFields), CreateInstance, Inspect (both overloads), aggregate Inspect, Fire, CheckCompatibility, CoerceEventArguments, and all result records (PreceptInstance, PreceptCompatibilityResult, PreceptEventInspectionResult, PreceptInspectionResult, PreceptFireResult, PreceptOutcomeKind) — all match implementation exactly. |
| 3 | Add Test Gaps | Done | Added 27 new tests: 2 StateAssert/EventAssert SourceLine tests (Category A, required parser fix to track SourceLine for state/event assert declarations); 5 CoerceEventArguments tests (Category B); 17 sample file Theory tests across all samples/*.precept files (Category C/E); 4 ParseWithDiagnostics tests (Category D). Also fixed a broken LS test (PreceptPreviewRulesTests scope "topLevel" → "invariant") that was broken by Item 0 but not caught at the time. 394 tests now passing. |
| 4 | Implement Editable Fields Runtime | Done | Runtime `Update()` API with `IUpdatePatchBuilder`, editability enforcement per state, type checking, patch conflict detection, invariant/state-assert validation post-edit, atomic rollback. `Inspect(instance)` returns `EditableFields`. `Inspect(instance, patch)` hypothetical-patch validation. 48 tests in `PreceptEditTests.cs`. 442 total tests passing. |
| 5 | Implement DiagnosticCatalog | Done | Tier 3 catalog in `src/Precept/Dsl/DiagnosticCatalog.cs`: `LanguageConstraint` record (Id, Phase, Rule, MessageTemplate, Severity) with `FormatMessage()` template substitution. 37 constraints registered (C1–C25 parse, C26–C32 compile, C33–C37 runtime) covering all 39 throw sites across `PreceptParser.cs` and `PreceptRuntime.cs`. Every throw updated with `// SYNC:CONSTRAINT:Cnn` comment and catalog-sourced message via `FormatMessage()`. `DiagnosticCatalog.Constraints` exposed as `IReadOnlyList<LanguageConstraint>` for consumers (LS diagnostics, MCP). `ParseWithDiagnostics` also updated. 447 tests passing, zero behavioral changes. |
| 6 | Implement MCP Server | Done | 6 MCP tools (`precept_validate`, `precept_schema`, `precept_audit`, `precept_run`, `precept_language`, `precept_inspect`) in `tools/Precept.Mcp/`. 39 integration tests in `test/Precept.Mcp.Tests/`. VS Code extension registers MCP server via `package.json`. README, McpServerDesign.md, and copilot-instructions.md updated. 486 total tests passing. |
| 7 | Naming Renames + Structured Constraint Violations (CV Phases 0-7) | Done | Phases 0-3: `PreceptOutcomeKind`→`TransitionOutcome`, `PreceptUpdateOutcome`→`UpdateOutcome`, all result types drop `Precept` prefix, `ConstraintCatalog`→`DiagnosticCatalog`, `PreceptCompileValidationResult`→`CompileResult` (later `ValidationResult`), 3 runtime method renames, `IsSuccess` added. Phases 4-7: `ConstraintViolation`/`ConstraintTarget`/`ConstraintSource`/`ExpressionSubjects` introduced, `Reasons`→`Violations`, `Rejected`/`ConstraintFailure` outcome split, `EventInspectionResult.Undefined` propagates violation reason, `BuildSnapshot` populates `ActiveRuleViolations`. All tests migrated; `PreceptConstraintViolationTests.cs` added. 650 tests passing. |

---

## Build Baseline (Pre-Cleanup)

- **348 tests passing** — all in `test/Precept.Tests/` and `test/Precept.LanguageServer.Tests/`
- **Build clean** — 0 errors, 0 warnings on core (`src/Precept/`, `test/`)
- Language server build may report a DLL-lock error if the language server process is running — stop it first

## Design Decision: Hard Cut (No Backward Compat)

The backward-compat layer was explicitly rejected. The decision is a **hard cut** to the new language with full removal of all legacy tech debt. The old types were never shipped externally — there are no consumers to migrate. Every reference to `PreceptTransition`, `PreceptClause`, `PreceptRule`, `TopLevelRules`, or `.Rules` should be deleted, not wrapped.

---

## Item 0 — Remove Legacy / Backward Compat Code

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

I need you to remove ALL backward-compatibility / legacy code from the Precept codebase.
Read docs/CleanupAndNextSteps.md Item 0 for the full inventory of what to remove.

The work is:

1. PreceptModel.cs — delete legacy types (PreceptTransition, PreceptClause, PreceptRule)
   and legacy properties (Transitions, TopLevelRules, Rules on State/Event/Field/CollectionField).
   Update the PreceptDefinition constructor to remove those parameters.

2. PreceptParser.cs — delete oldTransitions/oldTopLevelRules lists, the BuildOldTransition()
   method, all code that populates legacy model properties, and remove those arguments from
   the PreceptDefinition constructor call.

3. PreceptRuntime.cs — delete the old Transitions→TransitionRows conversion block in the
   constructor (~lines 50-88), the old Rules→Invariants/Asserts synthesis block (~lines 104-162),
   CombineGuards()/CombineGuardText() methods, ValidateLegacyRules() method, and the call
   to ValidateLegacyRules().

4. PreceptPreviewHandler.cs — replace old model rule iteration (TopLevelRules, field.Rules,
   state.Rules, event.Rules) with iteration over new model types (Invariants, StateAsserts,
   EventAsserts).

5. PreceptAnalyzer.cs — replace old model rule validation (TopLevelRules, state.Rules,
   event.Rules) with validation against new model types (Invariants, StateAsserts, EventAsserts).

6. PreceptRulesTests.cs — do NOT blindly delete. Follow the test handling instructions in
   this document: analyze every test against surviving test files, port unique scenarios to
   the appropriate file using new model assertions, confirm duplicates are truly covered,
   then delete. Goal: zero test scenarios lost, zero duplicated tests kept.

After each file change, build and run tests to catch cascading breaks. The goal is zero
references to PreceptTransition, PreceptClause, PreceptRule, TopLevelRules, or .Rules
anywhere in the codebase.

Note: The tests in PreceptRulesTests.cs use NEW DSL syntax in their input strings but assert
on OLD model properties. The DSL strings are fine — it's the assertions that need porting.

When finished, update the Status Tracker table in this document: set Item 0's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

The parser currently dual-populates both new model types (`PreceptTransitionRow`, `PreceptInvariant`, `PreceptStateAssert`, `PreceptEventAssert`) and old model types (`PreceptTransition`, `PreceptClause`, `PreceptRule`). The runtime constructor then converts old types back into new types as a fallback. This entire layer exists only for backward compatibility with code that was never shipped externally.

#### Legacy Types to Delete (PreceptModel.cs)

| Type / Property | Location | Replaced By |
|----------------|----------|-------------|
| `PreceptTransition` record | L125-133 | `PreceptTransitionRow` |
| `PreceptClause` record | L136-141 | Flat `PreceptTransitionRow` |
| `PreceptRule` record | L55-63 | `PreceptInvariant` / `PreceptStateAssert` / `PreceptEventAssert` |
| `PreceptDefinition.Transitions` | L10 | `PreceptDefinition.TransitionRows` |
| `PreceptDefinition.TopLevelRules` | L13 | `PreceptDefinition.Invariants` |
| `PreceptState.Rules` | L24 | `PreceptStateAssert` (separate list) |
| `PreceptEvent.Rules` | L29 | `PreceptEventAssert` (separate list) |
| `PreceptField.Rules` | L44 | `PreceptInvariant` (global list) |
| `PreceptCollectionField.Rules` | L50 | `PreceptInvariant` (global list) |

#### Parser Dual-Population Code to Delete (PreceptParser.cs)

| Code | Location | Purpose |
|------|----------|---------|
| `var oldTransitions = new List<PreceptTransition>();` | L711 | Collects old-format transitions |
| `var oldTopLevelRules = new List<PreceptRule>();` | L712 | Collects old-format rules |
| `oldTopLevelRules.Add(new PreceptRule(...))` | L733-735 | Maps invariant → old PreceptRule |
| `BuildOldTransition(...)` call | L808 | Converts each row into old transition |
| `oldTransitions,` in constructor call | L943 | Passes old list to model |
| `oldTopLevelRules.Count > 0 ? ...` in constructor | L945 | Passes old list to model |
| `BuildOldTransition()` method | L990-1015 | Entire helper method |

#### Runtime Backward Compat to Delete (PreceptRuntime.cs)

| Block | Location | Purpose |
|-------|----------|---------|
| Old Transitions → TransitionRows conversion | L50-88 | Reads `model.Transitions`, converts to `_transitionRowMap` |
| Synthesize invariants from TopLevelRules | L104-109 | `foreach (var rule in model.TopLevelRules)` |
| Synthesize invariants from field.Rules | L110-120 | `foreach (var field in model.Fields) if (field.Rules...)` |
| Synthesize invariants from col.Rules | L121-127 | `foreach (var col in model.CollectionFields) if (col.Rules...)` |
| Synthesize event asserts from event.Rules | L128-141 | `foreach (var evt in model.Events) if (evt.Rules...)` |
| Synthesize state asserts from state.Rules | L148-162 | Maps old state rules to `To` preposition |
| `CombineGuards()` method | L799-809 | Combines block-level + clause-level predicate ASTs |
| `CombineGuardText()` method | L811-818 | Combines block-level + clause-level predicate text |
| `ValidateLegacyRules()` method | L1492-1570 | Validates old-style rules at compile time |
| Call to `ValidateLegacyRules()` | L1424 | Invocation in compile pipeline |

#### Language Server References to Update

**PreceptPreviewHandler.cs:**

| Code | Location | Action |
|------|----------|--------|
| `model.TopLevelRules ?? Array.Empty<PreceptRule>()` | L257 | Replace with `model.Invariants` iteration |
| `ruleDefinitions.Add(... rule.ExpressionText, rule.Reason)` | L258 | Use `inv.ExpressionText`, `inv.Reason` |
| `field.Rules!` iteration | L263 | Remove (invariants are global, not per-field) |
| `state.Rules!` iteration | L267 | Replace with `model.StateAsserts` filtered by state |
| `evt.Rules!` iteration | L268 | Replace with `model.EventAsserts` filtered by event |

**PreceptAnalyzer.cs:**

| Code | Location | Action |
|------|----------|--------|
| `model.TopLevelRules` validation | L701-706 | Replace with `model.Invariants` |
| `state.Rules` validation | L709-723 | Replace with `model.StateAsserts` |
| `evt.Rules` validation | L726-740 | Replace with `model.EventAsserts` |

#### Test File Handling: PreceptRulesTests.cs

**Key subtlety:** `PreceptRulesTests.cs` (59 tests) uses **new DSL syntax** in its input strings (e.g. `invariant ... because`, `field ... as ... default`) but **asserts on old model properties** (`TopLevelRules`, `.Rules`, `PreceptRule`). The DSL strings are correct — the assertions are what's broken.

**Instructions for cleanup:**

1. Before deleting, analyze every test in `PreceptRulesTests.cs` against the surviving test files
2. For each test, determine if the **scenario** (not just the syntax) is already covered elsewhere
3. If a scenario is unique, port it to the appropriate surviving test file using new model assertions
4. If a scenario is duplicated, confirm the existing test covers the same edge case, then skip it
5. Delete `PreceptRulesTests.cs` only after all unique scenarios are ported

Goal: **zero test scenarios lost, zero duplicated test cases kept.**

#### Test File Survival Map

These test files survive Item 0 unchanged (all use new model types exclusively):

| File | Tests | Content |
|------|-------|---------|
| `NewSyntaxParserTests.cs` | 62 | Parser output verification — new model types |
| `NewSyntaxRuntimeTests.cs` | 39 | Runtime fire/inspect behavior |
| `PreceptWorkflowTests.cs` | 90+ | End-to-end workflow scenarios |
| `PreceptCollectionTests.cs` | ~30 | Collection type operations |
| `PreceptSetParsingTests.cs` | 4 | Set parsing edge cases |
| `PreceptExpressionParserTests.cs` | ~40 | Expression AST parsing |
| `PreceptExpressionParserEdgeCaseTests.cs` | ~20 | Expression edge cases |
| `PreceptExpressionRuntimeEvaluatorBehaviorTests.cs` | ~30 | Evaluator operator behavior |
| `PreceptAnalyzerNullNarrowingTests.cs` | ~20 | Language server null narrowing |
| `PreceptAnalyzerCollectionMutationTests.cs` | ~10 | Language server collection diagnostics |
| `PreceptPreviewRulesTests.cs` | ~10 | Preview panel rule display |

---

## Item 1 — Fix README.md

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

The README.md is completely out of date. It uses old DSL syntax (machine keyword, rule keyword,
old transition blocks) and old API names (instance.StateData). Read docs/CleanupAndNextSteps.md
Item 1 for the specific issues.

Rewrite the README to reflect the current implementation:
- Read at least one sample file from samples/ to confirm current syntax before writing
- Use `precept` keyword (not `machine`)
- Use `invariant ... because "..."` (not `rule ... "..."`)
- Use `in <State> assert ... because "..."` for state asserts
- Use `on <Event> assert ... because "..."` for event asserts
- Use flat `from X on Y ... transition Z` rows (not nested if/else blocks)
- Use correct API: PreceptCompiler.Compile(), PreceptEngine, instance.InstanceData
- Include a realistic example (the bank-loan or support-ticket sample works well)
- Keep it concise — overview, DSL example, C# API usage, that's it

When finished, update the Status Tracker table in this document: set Item 1's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

The current README.md has these specific problems:

| Issue | Current (Wrong) | Correct |
|-------|-----------------|---------|
| DSL keyword | `machine BankLoan` | `precept BankLoan` |
| Field rule syntax | `rule RemainingBalance >= 0 "..."` | `invariant RemainingBalance >= 0 because "..."` |
| Top-level rule | `rule ApprovedAmount <= RequestedAmount "..."` | `invariant ApprovedAmount <= RequestedAmount because "..."` |
| Event rule syntax | `rule Amount > 0 "..."` (inside event block) | `on MakePayment assert Amount > 0 because "..."` |
| Transition syntax | `if ... else reject` (nested block) | `from X on Y when ... transition Z` / `reject` (flat rows) |
| Instance data access | `instance.StateData` | `instance.InstanceData` |
| Missing concepts | No mention of `initial`, `state ... assert`, `edit` | These are core language features now |

---

## Item 2 — Fix RuntimeApiDesign.md

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

The RuntimeApiDesign.md uses stale "Dsl" prefix naming throughout. Read
docs/CleanupAndNextSteps.md Item 2 for the full mapping. Rename every occurrence
of the old Dsl* type names to match the actual codebase Precept* names. This is a
straightforward find-and-replace across the document — there are ~40 instances.
Also verify that the API signatures described in the document match the current
implementation in src/Precept/Dsl/PreceptRuntime.cs.

When finished, update the Status Tracker table in this document: set Item 2's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

The entire document uses `Dsl` prefix instead of `Precept` prefix. Every type name is wrong:

| Document Says | Actual Type Name |
|---------------|-----------------|
| `DslWorkflowModel` | `PreceptDefinition` |
| `DslEvent` | `PreceptEvent` |
| `DslField` | `PreceptField` |
| `DslCollectionField` | `PreceptCollectionField` |
| `DslWorkflowInstance` | `PreceptInstance` |
| `DslWorkflowEngine` | `PreceptEngine` |
| `DslInspectionResult` | `PreceptInspectionResult` |
| `DslEventInspectionResult` | `PreceptEventInspectionResult` |
| `DslFireResult` | `PreceptFireResult` |
| `DslCompatibilityResult` | `PreceptCompatibilityResult` |
| `DslOutcomeKind` | `PreceptOutcomeKind` |

Other design docs also use stale naming (EditableFieldsDesign.md, McpServerDesign.md, CliDesign.md) but those are lower priority — fix them when those features are implemented.

---

## Item 3 — Add Test Gaps

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

Read docs/CleanupAndNextSteps.md Item 3 for the full list of test gaps identified during
the codebase review. Add these tests to the appropriate existing test files
(NewSyntaxParserTests.cs, NewSyntaxRuntimeTests.cs, or PreceptWorkflowTests.cs).

The gaps fall into categories:
A. Edge cases from deleted PreceptRulesTests.cs that aren't covered elsewhere (21 scenarios)
B. Coerce/type-conversion edge cases
C. Compatibility/cross-version scenarios
D. ParseWithDiagnostics (non-throwing parse API)
E. Sample file validation (all samples/ files parse and compile clean)

Read at least one sample file from samples/ before writing any DSL test strings.
For each test, use NEW model types only (Invariants, StateAsserts, EventAsserts,
TransitionRows) — never assert on TopLevelRules, .Rules, PreceptRule, etc.

When finished, update the Status Tracker table in this document: set Item 3's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

#### A. Edge Cases to Port from PreceptRulesTests.cs

These 21 test scenarios from the deleted `PreceptRulesTests.cs` are NOT covered by existing new-syntax tests:

**Parsing edge cases:**

| Scenario | Original Test | What to Assert |
|----------|--------------|----------------|
| Event rule scoped to specific event | `Parse_EventRule_AttachedToCorrectEvent` | `model.EventAsserts` contains assert with correct `EventName` |
| Source line tracking on invariants | `Parse_TopLevelRule_HasSourceLine` | `model.Invariants[0].SourceLine > 0` |
| Source line tracking on state asserts | `Parse_StateRule_HasSourceLine` | `model.StateAsserts[0].SourceLine > 0` |
| Multiple invariants parsed in order | `Parse_TopLevelRule_MultipleRules` | `model.Invariants.Count == 2`, order preserved |
| No rules → null/empty collections | `Parse_NoRules_MachineHasNullTopLevelRules` | `model.Invariants` is empty, `model.StateAsserts` is empty |
| Nullable field + invariant interaction | `Parse_FieldRule_NullableFieldWithRule` | Nullable field has invariant that compiles |

**Compile-time validation edge cases:**

| Scenario | Original Test | What to Assert |
|----------|--------------|----------------|
| Invariant violated by defaults | `Compile_TopLevelRule_DefaultValueViolatesRule_Throws` | `Compile()` throws with invariant reason |
| Invariant satisfied by defaults | `Compile_TopLevelRule_DefaultValuesSatisfyRule_Succeeds` | `Compile()` succeeds |
| Collection invariant at creation | `Compile_CollectionRule_ViolatedAtCreation_Throws` | Collection invariant checked at compile |
| Initial state assert violated by defaults | `Compile_StateRule_InitialStateViolation_Throws` | `in Initial assert` checked at compile |
| Event assert with default args violating | `Compile_EventRule_DefaultArgViolation_Throws` | `on Event assert` checked at compile |
| Event assert skipped (no arg defaults) | `Compile_EventRule_NoDefaultArgs_Skipped` | No throw when args lack defaults |

**Runtime behavior edge cases:**

| Scenario | Original Test | What to Assert |
|----------|--------------|----------------|
| Invariant blocks fire mutation | `Fire_TopLevelRule_ViolatedAfterSets_IsBlocked` | Fire returns blocked/rejected |
| State assert blocks transition (to) | `Fire_StateRule_ViolatedOnEntry_IsBlocked` | Cannot enter state violating `to` assert |
| State assert blocks transition (from) | `Fire_StateRule_ViolatedOnExit_IsBlocked` | Cannot leave state violating `from` assert |
| Event assert blocks fire | `Fire_EventRule_ViolatedByArgs_IsBlocked` | Event assert rejects bad args |
| Inspect shows rule status per event | `Inspect_RulesShownPerEvent` | Inspection includes assert status |
| Collection rule violated after mutation | `Fire_CollectionRule_ViolatedAfterAdd_IsBlocked` | Collection mutation blocked by invariant |
| Stateless inspect with event asserts | `Inspect_Stateless_EventRulesShown` | Pre-fire inspect includes event asserts |
| In-state assert continuous enforcement | `Fire_InStateRule_ViolatedMidTransition_Blocks` | `in State assert` checked after mutations |
| Multiple invariants all checked | `Fire_MultipleRules_AllChecked` | All invariants evaluated, first violation reported |

#### B. Coerce / Type-Conversion Edge Cases

The expression evaluator has a `Coerce` method for type conversions. Test these edge cases:

| Scenario | Expected |
|----------|----------|
| String-to-number coercion in comparison | `"5" > 3` evaluates correctly or fails clearly |
| Null coercion in arithmetic | `null + 1` behavior is defined |
| Boolean-to-string in concatenation | Defined behavior |
| Number precision edge cases | Large numbers, decimal precision |

#### C. Compatibility / Cross-Version

| Scenario | Expected |
|----------|----------|
| All sample files parse without error | `PreceptCompiler.Compile(File.ReadAllText(path))` succeeds for every `samples/*.precept` |
| All sample files have no diagnostics | Language server reports 0 errors for each sample |

#### D. ParseWithDiagnostics

If a non-throwing parse API exists (or should exist), test:

| Scenario | Expected |
|----------|----------|
| Valid source returns empty diagnostics | No errors |
| Invalid source returns diagnostics (not exception) | Error list with line numbers |
| Partial parse recovers what it can | Model has partial data + diagnostics |

---

## Item 4 — Implement Editable Fields Runtime

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

Read docs/EditableFieldsDesign.md for the full design, then read
docs/CleanupAndNextSteps.md Item 4 for the implementation gap analysis.

The parser already produces PreceptEditBlock records. The runtime (PreceptRuntime.cs)
has NO edit support — no Update() API, no editability validation, no IUpdatePatchBuilder.

Implement the runtime support per the design:
1. PreceptEngine gets an Update(instance, patchBuilder) method
2. IUpdatePatchBuilder interface for building field mutations
3. Editability check: only fields listed in an `edit` block for the current state are mutable
4. After applying edits, evaluate all invariants and state asserts (same as post-fire validation)
5. Update the inspect result to include which fields are editable in the current state

Write tests in PreceptWorkflowTests.cs or a new PreceptEditTests.cs.
Read at least one sample from samples/ before writing any DSL strings.

When finished, update the Status Tracker table in this document: set Item 4's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

**What exists today:**
- Parser produces `PreceptEditBlock` records with `State`, `FieldNames`, and `SourceLine`
- Model stores `EditBlocks` on `PreceptDefinition`
- DSL syntax: `in <StateTarget> edit <FieldName>, <FieldName>, ...`
- `in any` expands to all states

**What's missing (from EditableFieldsDesign.md):**

| Component | Design Spec | Status |
|-----------|-------------|--------|
| `Update(instance, patchBuilder)` method on `PreceptEngine` | Core mutation API for direct field edits | Not implemented |
| `IUpdatePatchBuilder` interface | Builder pattern: `.Set("Field", value)` | Not implemented |
| Editability resolution | Union of matching `edit` blocks for current state | Not implemented |
| Post-edit validation | All invariants + `in <State> assert` checked after edits | Not implemented |
| Inspect editability | `InspectionResult` includes `EditableFields` for current state | Not implemented |
| Compile-time edit validation | Verify edited field names exist, types match | Not implemented |

**Design constraints to respect:**
- Editable fields are additive across declarations (union)
- `in any` includes terminal states
- Direct edits bypass event pipeline — invariants/asserts are the safety net
- All field types supported (scalar + collection)

---

## Item 5 — Implement DiagnosticCatalog

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

Read docs/CatalogInfrastructureDesign.md for the full Tier 3 design, then read
docs/CleanupAndNextSteps.md Item 5 for the implementation gap.

Implement the DiagnosticCatalog as the central registry for all enforcement points
(parse errors, compile-time checks, runtime violations). Each constraint gets an ID,
phase, rule description, message template, and severity.

Steps:
1. Create src/Precept/Dsl/DiagnosticCatalog.cs with the registry pattern from the design
2. Register all existing enforcement points (scan PreceptParser.cs, PreceptRuntime.cs for
   every throw/error — there are ~30-40 enforcement points)
3. Update error-throwing code to reference the registered constraint and use MessageTemplate
4. Expose DiagnosticCatalog.Constraints for consumers (language server diagnostics, MCP)

This is infrastructure — existing behavior must not change, only the error message source
becomes centralized.

When finished, update the Status Tracker table in this document: set Item 5's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

**Tier 1 (Token Catalog) — DONE:**
- `PreceptToken` enum with `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes
- `PreceptTokenMeta` reflection helpers

**Tier 2 (Construct Catalog) — DONE:**
- `ConstructCatalog.cs` with parser combinator registration

**Tier 3 (Constraint Catalog) — NOT IMPLEMENTED:**

Design specifies:

```csharp
public sealed record LanguageConstraint(
    string Id,           // e.g. "C7" → diagnostic code PRECEPT007
    string Phase,        // "parse" | "compile" | "runtime"
    string Rule,         // Human-readable description
    string MessageTemplate, // With {placeholders} for context
    ConstraintSeverity Severity);  // Error | Warning
```

Registration pattern:
```csharp
static readonly LanguageConstraint C7 = DiagnosticCatalog.Register(
    "C7", "parse", "Non-nullable fields without 'default' are a parse error.",
    "Field '{fieldName}' is non-nullable and has no default value.",
    ConstraintSeverity.Error);
```

Consumers:
- **Parser/Compiler/Runtime:** Use `MessageTemplate` for error messages
- **Language server:** Map constraint ID to LSP diagnostic code (`PRECEPT007`)
- **MCP server:** Serialize to JSON for `precept_language` endpoint
- **Copilot instructions:** `// SYNC:CONSTRAINT:Cnn` comments for drift defense

---

## Item 6 — Implement MCP Server

### Prompt

```
Before starting, suggest which AI model (e.g. Claude Sonnet, Claude Opus, GPT-4.1, etc.)
is best suited for this task and why, then wait for me to confirm or switch models.

Read docs/McpServerDesign.md and docs/McpServerImplementationPlan.md for the full design
and phased implementation plan. Then read docs/CleanupAndNextSteps.md Item 6 for context.

Implement the MCP server as a new project (tools/Precept.McpServer/) that exposes:
- precept_language: DSL vocabulary and constraints (from Token/Construct/DiagnosticCatalog)
- precept_compile: Compile a .precept source string, return model or diagnostics
- precept_inspect: Inspect an instance, return available events and field state
- precept_fire: Fire an event on an instance, return outcome
- precept_create_instance: Create a new instance from a compiled model

This depends on Item 5 (DiagnosticCatalog) for the precept_language endpoint.
Follow the implementation plan phases in McpServerImplementationPlan.md.

When finished, update the Status Tracker table in this document: set Item 6's Status
to "Done" and add any relevant notes. If anything is unclear or ambiguous during the work,
ask me for clarification before proceeding — do not guess.
```

### Analysis

**Design documents are comprehensive:**
- `McpServerDesign.md` specifies the tool surface, request/response schemas, and integration points
- `McpServerImplementationPlan.md` provides a phased rollout plan

**Dependencies:**
- Item 0 (legacy removal) should be done first so the MCP server only uses new types
- Item 5 (DiagnosticCatalog) provides the `precept_language` data source
- The MCP server wraps `PreceptCompiler`, `PreceptEngine`, and catalog APIs

**Design notes use stale `Dsl*` naming** — will be fixed by Item 2 or at implementation time.

---

## Appendix A — Current Implementation State

### What's Working

| Component | Status | Details |
|-----------|--------|---------|
| Tokenizer | Complete | Superpower-based, attribute-driven keyword dictionary, 58 token types |
| Parser | Complete | 50+ combinators, full expression parser, all DSL constructs |
| Model | Complete | Legacy types removed; new types only |
| Runtime (fire) | Complete | 6-stage pipeline: event asserts → row selection → exit actions → row mutations → entry actions → validation |
| Runtime (inspect) | Complete | Shows available events, transition outcomes, argument requirements |
| Runtime (compile) | Complete | 6 compile-time checks (C29–C32, C44–C45) |
| Token Catalog (Tier 1) | Complete | `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` on all tokens |
| Construct Catalog (Tier 2) | Complete | All 10 user-facing constructs registered; 4 active consumers (parser, MCP, LS hover, drift tests) |
| Language Server | Complete | Completions, semantic tokens, preview inspector, diagnostics |
| VS Code Extension | Complete | Syntax highlighting, language server client, preview panel |
| 348 tests | All passing | Core engine fully tested |

### What's Missing

| Component | Status | Blocking |
|-----------|--------|----------|
| 2 design-specified state assert checks | Deferred | Contradiction (#4) and deadlock (#5) require interval analysis — tracked in @ToDo.md |

### Compile-Time Checks: Design vs Implementation

The design (PreceptLanguageDesign.md) specifies 5 state assert checks. The implementation has 4 concrete-evaluation checks plus 1 legacy check:

**Currently Implemented:**

| # | Check | Method |
|---|-------|--------|
| 1 | Invariants vs default field values | `ValidateConstraintsAtCompileTime` |
| 2 | Initial state asserts (`in`/`to`) vs defaults | `ValidateConstraintsAtCompileTime` |
| 3 | Event asserts vs argument defaults | `ValidateConstraintsAtCompileTime` |
| 4 | Literal `set` assignments vs invariants | `ValidateLiteralSetAssignments` (C32) |
| 5 | Duplicate state assert | `ValidateDuplicateStateAsserts` (C44) |
| 6 | `in` + `to` subsumption | `ValidateSubsumedStateAsserts` (C45) |

**Designed but NOT Implemented:**

| # | Check | Description | Complexity |
|---|-------|-------------|------------|
| 4 | Same-Preposition Contradiction | Two asserts on same state with contradictory domains | High (needs interval analysis) |
| 5 | Cross-Preposition Deadlock | `in`/`to` vs `from` on same state creating unexitable state | High (needs interval analysis) |

Checks #4 and #5 require expression domain analysis and are tracked in @ToDo.md.

### Architecture Diagram

```
samples/*.precept
     │
     ▼
┌─────────────┐    ┌──────────────┐    ┌──────────────────┐
│ PreceptToken │───▶│  Tokenizer   │───▶│     Parser       │
│  (58 types)  │    │ (Superpower) │    │ (50+ combinators)│
│  + Tier 1    │    └──────────────┘    └────────┬─────────┘
│  Catalog     │                                 │
└─────────────┘                                  ▼
                                        ┌──────────────────┐
                                        │ PreceptDefinition │
                                        │  (new model)      │
                                        │  + TransitionRows │
                                        │  + Invariants     │
                                        │  + StateAsserts   │  ◄── LEGACY TYPES
                                        │  + EventAsserts   │      REMOVED IN
                                        │  + EditBlocks     │      ITEM 0
                                        └────────┬─────────┘
                                                 │
                                        ┌────────▼─────────┐
                                        │ PreceptCompiler   │
                                        │  .Compile()       │
                                        │  4 compile checks │
                                        └────────┬─────────┘
                                                 │
                                        ┌────────▼─────────┐
                                        │  PreceptEngine    │
                                        │  .Fire()          │
                                        │  .Inspect()       │
                                        │  .Update() ←ITEM4 │
                                        └────────┬─────────┘
                                                 │
                              ┌──────────────────┼──────────────────┐
                              ▼                  ▼                  ▼
                     ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
                     │ Language     │   │ MCP Server   │   │ CLI          │
                     │ Server      │   │ (ITEM 6)     │   │ (future)     │
                     │ (complete)  │   │              │   │              │
                     └──────────────┘   └──────────────┘   └──────────────┘
```

### Files Archived

The following documents were moved to `docs/archive/` during the review:

| File | Reason |
|------|--------|
| `WhenPreconditionDesign.md` | Superseded — `when` is implemented as guard syntax |
| `nameideas.md` | Brainstorming artifact — naming decisions are finalized |
| `InteractiveInspectorMockup.md` | Superseded — preview inspector is implemented |
| `DesignNotes-legacy.md` | Archived — old design notes from pre-redesign |
| `README-legacy.md` | Archived — old README from pre-redesign |
