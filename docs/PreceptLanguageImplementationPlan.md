# Precept Language Redesign — Implementation Plan

Date: 2026-03-05
Branch: `feature/language-redesign`
Spec: `docs/PreceptLanguageDesign.md`

This plan implements the full-stack language redesign: new Superpower-based parser, updated model, updated runtime, language server, grammar, tests, samples, and docs.

## Implemented Follow-Up: Shared Type Checking

### Motivation

While adding typed completions to the language server (e.g., offering only boolean fields on the RHS of `set BoolField =`), we discovered that `PreceptTypeContext` scope lookups were returning wrong results. Every transition row in the parsed model had `SourceLine = 0`, so when the analyzer asked "what type scope applies at this cursor position?", all rows collapsed together and the wrong `transition-actions` scope was selected.

**Root cause:** The Superpower parser was not propagating source line numbers from keyword tokens into `TransitionRowResult` or action result types. The model had no line attribution for transition rows.

**What that uncovered:** Fixing the parser line propagation forced a closer look at how the analyzer performed type inference. The analyzer maintained ~635 lines of its own parallel type logic — `TryInferKind()`, `ApplyNarrowing()`, `ValidateExpression()`, `ValidateCollectionMutations()`, `BuildSymbolKinds()`, `IsAssignable()` — that was diverging from compile-time behavior. Bugs fixed in one place wouldn't be fixed in the other, and new constraints had to be implemented twice.

**Decision:** Extract all expression/type checking into a shared `PreceptTypeChecker` in core so the compiler, language server, and MCP tools all get the same diagnostics from a single source of truth. See `/memories/repo/type-checker-design.md` for the full design decisions table.

### What changed

- **Parser source-line propagation:** `TransitionRowResult.SourceLine` and all action parsers now carry `SourceLine` from the keyword token's `Span.Position.Line`. This flows through `PreceptTransitionRow` so type scopes and diagnostics can be attributed to the correct row.
- **Shared type checker:** `PreceptTypeChecker.Check(PreceptDefinition)` returns `TypeCheckResult` containing diagnostics and a `PreceptTypeContext` with resolved types per expression position. Implements `StaticValueKind` flags-based type representation, null-flow narrowing (within-row `when` guards + cross-row negation), state assert narrowing (`in State assert X != null` propagates into `from State` rows), and per-state `from any` expansion.
- **Constraint codes C38–C43:** Six new compile-time diagnostics (`PRECEPT038`–`PRECEPT043`) covering unknown identifiers, expression type mismatches, unary/binary operator errors, null-flow violations, and collection `pop`/`dequeue into` target mismatches.
- **`PreceptCompiler.Validate(model)`:** Non-throwing validation path so consumers (MCP `precept_validate` tool, future CLI) can return all diagnostics structurally instead of throwing on the first error.
- **Analyzer delegation:** ~635 lines of duplicate type logic removed from the analyzer. It now calls the shared checker and consumes `PreceptTypeContext` for IDE features.
- **Typed completions:** `PreceptTypeContext` powers typed `set` RHS completions and typed collection mutation value completions in the analyzer.

### Scope note

This is intentionally not a general semantic-model layer. `PreceptTypeContext` and `PreceptTypeChecker` are `internal` — no public API surface, no caching layer, no incremental updates. The DSL is small enough that re-parsing and re-checking per completion request is sub-millisecond.

Two patterns emerged for typed completions:

- **Full re-check** (set RHS, collection mutation values): calls `PreceptTypeChecker.Check()` inline, gets `TypeContext` scopes and symbols, filters candidates. Correct even with narrowing.
- **Lightweight dictionaries** (`dequeue`/`pop into`): uses `FieldTypeKinds` and `CollectionInnerTypes` pre-computed during `PreceptDocumentIntellisense.Analyze()`. No re-check needed — just field-to-field type comparison.

If future typed completion features pile up and create pressure, the upgrade path is: introduce a cached semantic model (`PreceptSemanticModel`) that parses once per edit cycle and exposes resolved types, scopes, and diagnostics to all consumers. That's the Roslyn/rust-analyzer pattern — but premature until the performance ceiling is actually hit.

### Why compile-time checking is a strategic strength

Compile-time checking is not just defensive validation. It is one of the core reasons to author workflows in a DSL instead of embedding rules directly in application code or configuration.

The bar for Precept should be:

- if a workflow references a name that cannot exist, fail at compile time
- if an operator can never make sense for the operand types, fail at compile time
- if nullability has not been made explicit, fail at compile time
- if scope makes a reference invalid, fail at compile time

Runtime rejection should be reserved for cases that genuinely depend on runtime data and cannot be proven statically. That keeps preview, inspect, language-server diagnostics, and compile output aligned around a single contract: catch as much real semantic intent as possible before execution.

### Remaining work

#### Phase A: State action type checking — DONE

The type checker validates transition rows and rules but skips `PreceptStateAction` entirely. State actions hold `SetAssignments` and `CollectionMutations` — the same expression types already validated in transition rows.

- Add `ValidateStateActions()` to `PreceptTypeChecker` — reuse `ValidateExpression` and `ValidateCollectionMutations` with a data-only symbol scope (no event args, since state actions aren't event-scoped).
- Apply state assert narrowing for `to`/`from` prepositions.
- Tests: type mismatch in `to State -> set`, null-flow violation in state action, collection mutation type error.
- Verify MCP `precept_validate` surfaces these diagnostics (no MCP changes needed — `Validate()` already calls `Check()`).

#### Phase B: Typed `dequeue`/`pop ... into` completions — DONE

Completion scenario #4a (`dequeue QueueField into `) returns all scalar data fields unfiltered. It should filter to fields whose type matches the collection's inner type.

- Add `TryBuildTypedDequeuePopIntoCompletions()` in the analyzer.
- Use `PreceptTypeChecker.MapFieldContractKind()` to get the collection's inner type.
- Filter candidate fields by `IsAssignableKind`.
- Fallback: if type resolution fails, return untyped list (existing behavior).
- Tests: typed `into` completions for queue/stack with different inner types.

#### Phase C: Consolidate minor analyzer helpers — DONE

Both duplicate helpers have been replaced with public wrappers on `PreceptTypeChecker`:

- `MapCollectionInnerTypeKind` → `PreceptTypeChecker.MapScalarType()`
- `TryGetLiteralKind` → `PreceptTypeChecker.TryGetLiteralKind(string, out StaticValueKind)`

Zero type-related logic remains in the analyzer.

#### Not applicable: Guard expression typed completions

Guard completions do not need type filtering. All field types can participate in valid `when` expressions: numbers via comparison (`Count > 0`), strings via equality/null checks (`Name != null`), booleans directly (`IsActive`). Mid-expression type filtering (e.g., after `when X && `) would require partial expression parsing at the cursor — disproportionate complexity for marginal UX benefit. The type checker already catches invalid guards via C39.

## Planned Follow-Up: Compile-Time Checking Expansion

The redesign phases are complete, but the compile-time checker is intentionally still narrower than the language direction. The next follow-up work should deepen semantic checking, not broaden syntax.

### Phase D: Equality and null-compatible comparison policy

**Why this comes first:** equality is the largest remaining inconsistency in the operator surface. Arithmetic, logical operators, ordered comparisons, and nullability are compile-checked strictly; equality must now match that bar.

**Locked policy:** equality is allowed only for compatible operands.

- Same-family scalar comparisons are allowed: `string`, `number`, and `boolean` compare only against their own family.
- `null` participates only with a nullable operand of the same family, or with `null` itself.
- No implicit coercions are performed for equality.
- `NonNullableField == null` and `NonNullableField != null` are compile-time errors.
- Incompatible equality comparisons reuse the existing binary-operator diagnostic family (`C41 / PRECEPT041`).
- Update `PreceptTypeChecker` and runtime evaluator together so compile-time and runtime semantics stay aligned.
- Update `docs/PreceptLanguageDesign.md` with the locked rule table.

**Minimum tests:**

- same-type equality accepted for `string`, `number`, `boolean`
- cross-type equality rejected at compile time
- nullable-to-same-family equality accepted without requiring narrowing
- `== null` / `!= null` valid only for nullable operands
- non-nullable `== null` / `!= null` rejected at compile time

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

The change site is `PreceptTypeChecker.TryInferBinaryKind()` in `src/Precept/Dsl/PreceptTypeChecker.cs`. The `case "=="` / `case "!="` branch (~line 752) currently checks only that both sides are inferable (`leftKind != None && rightKind != None`) without verifying type compatibility. Replace this with same-family checking: strip the `Null` flag from both sides, verify the non-null families match (or one side is pure `Null` and the other is nullable). Incompatible operands produce C41. Also reject `NonNullableField == null` / `!= null` by checking whether one side is `Null` and the other lacks the `Null` flag.

The runtime evaluator in `src/Precept/Dsl/PreceptExpressionEvaluator.cs` (lines 177–188) has a `TryToNumber` fallback for `==`/`!=` that silently coerces types. Remove the numeric fallback so runtime behavior matches the stricter compile-time policy. After the type checker rejects cross-type equality at compile time, the runtime path should only see same-type operands (or null).

Add tests to `test/Precept.Tests/PreceptTypeCheckerTests.cs`. Verify all 18 samples still compile clean. Run the full test suite (602+ tests) before committing.

### Phase E: Scope and narrowing hardening

**Goal:** make symbol visibility and null narrowing equally strict across fields, event args, guards, asserts, and state actions.

**Locked policy:** transition rows require dotted event-arg form (`EventName.ArgName`). Bare arg names are valid only in event asserts.

- Remove bare arg names from transition row symbol tables in `BuildSymbolKinds()` — only dotted keys (`EventName.ArgName`) should be added for transition scope.
- Add explicit tests and diagnostics for nullable event-arg narrowing in `when` guards using dotted form.
- Reject cross-event arg references (`EventB.Arg`) when checking a row for `EventA`.
- Reject bare arg names in transition row expressions as unknown identifiers (C38).
- Keep event asserts arg-only and make field references compile-blocking.
- Verify parenthesized and compound guard forms preserve narrowing only where structurally sound.

**Minimum tests:**

- event arg narrowed by `when Event.Arg != null` (dotted form)
- bare arg name in transition row rejected as unknown identifier
- event arg not narrowed without explicit guard
- field reference inside event assert rejected
- cross-event arg reference rejected
- parenthesized narrowing forms accepted/rejected as designed

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

The change site is `PreceptTypeChecker.BuildSymbolKinds()` in `src/Precept/Dsl/PreceptTypeChecker.cs`. Currently it adds event args as both bare (`symbols[pair.Key]`) and dotted (`symbols[$"{eventName}.{pair.Key}"]`). Remove the bare key line for non-assert scopes so transition rows only see dotted keys. The method receives the scope context — add a parameter or flag to distinguish transition-row scope from event-assert scope.

Event assert scope is already enforced by the parser (`src/Precept/Dsl/PreceptParser.cs` lines 963–998, constraints C14/C15/C16). The checker’s event-assert validation builds symbols with bare keys only (no dotted form needed since scope is arg-only).

Note: `TryGetIdentifierKey()` (~line 983) builds the dictionary key from the parsed expression — `Name` for bare identifiers, `Name.Member` for dotted. After removing bare keys from the symbol table, bare arg names in transition rows will fail to resolve → C38 (unknown identifier). Narrowing via `TryApplyNullComparisonNarrowing()` also uses `TryGetIdentifierKey`, so narrowing will only work on the dotted form in transition scope — which is the correct behavior.

All 18 sample files already use dotted form exclusively — no sample changes needed. Existing tests that use bare arg names in transition rows will need updating to use dotted form (search for undotted event-arg references in `test/Precept.Tests/`). Add tests to `test/Precept.Tests/PreceptTypeCheckerTests.cs`. Run the full suite before committing.

### Phase F: Rule-position strictness and collection contracts

**Goal:** ensure every boolean-only and collection-only position has uniform compile-time semantics.

- Reject non-boolean expressions in invariants, state asserts, event asserts, and `when` guards with the same diagnostic category.
- Collection mutation nullability is locked: `T|null` values into non-nullable collections require prior narrowing (C42). Already implemented and tested.
- Decide whether collection membership/equality can participate in stronger compile-time reasoning without false positives.

**Minimum tests:**

- invariant with non-boolean expression rejected
- state assert with non-boolean expression rejected
- event assert with non-boolean expression rejected
- collection mutation from nullable source without narrowing rejected
- `contains` mismatch coverage for invalid lhs/rhs shapes

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

Register new constraint C44 in `src/Precept/Dsl/DiagnosticCatalog.cs` for non-boolean rule positions. The checker currently infers guard/assert expression types but doesn’t enforce that the result is boolean. Add a check after `TryInferExpressionKind()` returns for `when` guards, `invariant`, `in/to/from assert`, and `on assert` expressions: if the inferred kind doesn’t include `StaticValueKind.Boolean`, emit C44.

The validation positions are spread across `ValidateTransitionRows()` (for `when` guards), and the invariant/assert validation loops. Search for where `row.WhenGuard` is validated and where assert expressions are checked.

Collection mutation nullability is already implemented and tested (C42). The remaining collection work is `contains` hardening — verify existing C41 coverage for `contains` LHS/RHS mismatches is complete.

Add a C46 trigger fixture to `test/Precept.Tests/CatalogDriftTests.cs`. Add tests to `PreceptTypeCheckerTests.cs`. Run the full suite before committing.

### Phase G: Additional sound static reasoning

**Goal:** add only those higher-order diagnostics that remain clearly correct.

**Locked policy:** compile-time impossibility is limited to type+null-flow reasoning plus identical-guard duplicate detection. No cross-field or arithmetic reasoning.

- Non-nullable `== null` / `!= null` is already a C41 error under the locked equality policy.
- Post-narrowing null contradictions (e.g., `when Value == null -> no transition` followed by `when Value == null -> ...`) are caught by narrowing + C41.
- Identical-guard duplicate rows for the same `(state, event)` are a compile-time error. Detection is AST or normalized-text equality of the `when` expression.
- No cross-field arithmetic reasoning (e.g., `Amount > 100 && Amount < 50`) — the inspector handles these.
- Keep the no-false-positives rule: if a proof requires speculative cross-field reasoning, prefer runtime/inspect over compile-time error.

**Minimum tests:**

- identical-guard duplicate row detected as compile-time error
- non-identical guards on same `(state, event)` accepted
- post-narrowing null contradiction caught via C41
- no false-positive cases for non-local expressions

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

Register new constraint C47 in `src/Precept/Dsl/DiagnosticCatalog.cs` (renamed from `ConstraintCatalog.cs` in CV Phase 2) for identical-guard duplicate rows. The detection site is `PreceptTypeChecker.ValidateTransitionRows()` in `src/Precept/Dsl/PreceptTypeChecker.cs`, which already iterates rows grouped by `(state, event)` in source-line order. Before processing each row, compare its `WhenText` (or normalized AST) against prior rows in the same group. If an identical guard is found, emit C47.

Non-nullable `== null` is already covered by Phase D (C41). Post-narrowing null contradictions are automatically caught because cross-row narrowing strips the `Null` flag, and then Phase D’s equality check rejects `NonNullableField == null`.

Use normalized `WhenText` comparison (trimmed, whitespace-collapsed) rather than AST equality — simpler and sufficient since authors rarely write semantically-identical but syntactically-different guards. Add a C47 trigger fixture to `CatalogDriftTests.cs`. Run the full suite before committing.

### Phase H: Coverage and tooling sync

**Goal:** make the checker changes cheap to maintain by expanding tests and keeping tooling/docs aligned.

- Add regression tests for every newly locked compile-time rule.
- Add analyzer completion tests wherever tighter compile-time rules affect candidate filtering.
- Add catalog drift tests for any new constraint codes or changed messages.
- Keep README and design docs synchronized in the same change pass.

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

After Phases D–G are complete, do a sweep:

1. **Analyzer severity fix:** `MapValidationDiagnostic()` in `tools/Precept.LanguageServer/PreceptAnalyzer.cs` (~line 956) hardcodes `DiagnosticSeverity.Error`. Change it to read `diagnostic.Constraint.Severity` and map `ConstraintSeverity.Warning` to `DiagnosticSeverity.Warning`. This connects the `ConstraintSeverity` enum (already in `DiagnosticCatalog.cs`) to LSP reporting.

2. **Completion filtering:** Verify that `PreceptAnalyzer.GetCompletions()` still produces correctly filtered candidates after the equality and scope changes. The typed-set-RHS and collection-value completions use `PreceptTypeContext` — confirm they still work with the tighter symbol tables.

3. **Catalog drift:** Ensure `CatalogDriftTests.EveryConstraint_CanBeTriggered` covers C46 and C47 trigger fixtures.

4. **README sync:** Review `README.md` for any claims about equality semantics, event-arg access, or diagnostic behavior that need updating.

5. **Run the full suite** (all 3 test projects) and confirm green before committing.

### Phase I: Graph Analysis Warning Diagnostics

**Status:** Not yet implemented
**Prerequisite:** Phases D–H complete (type checker expansion, severity mapping fix)

**Goal:** Finish the compiler pipeline cleanup needed for graph diagnostics: remove throw-based compile validation, register warning-severity and hint-severity graph diagnostics in core, add declaration source anchoring, and expose a composed compile-from-text pipeline. This completes the diagnostic severity model described in `PreceptLanguageDesign.md` § Diagnostic severity policy and makes tooling consume one structured validation result instead of mixing diagnostics with exception parsing.

**New constraint IDs:**

| Code | Severity | Finding | Message Template |
|---|---|---|---|
| C48 | Warning | Unreachable state | "State '{State}' is unreachable from the initial state." |
| C49 | Warning | Orphaned event | "Event '{Event}' is declared but never referenced in any transition row." |
| C50 | Hint | Dead-end state | "State '{State}' has outgoing transitions but all reject or no-transition — no path forward." |
| C51 | Warning | Reject-only pair | "Every transition row for ({State}, {Event}) ends in reject — the event can never succeed from this state. Remove the rows and let Undefined handle it." |
| C52 | Warning | Event never succeeds | "Event '{Event}' can never succeed from any reachable state — every reachable state either has no rows or all rows reject for that event." |
| C53 | Hint | Empty precept | "Precept '{Name}' declares no events." |

**Implementation steps:**

1. **Refactor compile validation into `Validate()`** — move the remaining throw-based compile checks in `src/Precept/Dsl/PreceptRuntime.cs` into structured diagnostic collection:
    - C29 invariant violations against default data
    - C30 initial-state assert violations against default data
    - C31 event-assert violations against default argument defaults
    - C32 literal set assignments that violate invariants
    - C44 duplicate state asserts
    - C45 subsumed `to` assert when identical `in` assert exists
    `Compile()` remains as a thin wrapper that throws only if `Validate()` reports error-severity diagnostics.

2. **Fix diagnostic result semantics** — `ValidationResult.HasErrors` must key off `ConstraintSeverity.Error`, not `Diagnostics.Count > 0`. Warning/hint diagnostics must never block compilation.

3. **Add declaration source metadata** — extend `PreceptDefinition`, `PreceptState`, and `PreceptEvent` with source-line information in `src/Precept/Dsl/PreceptModel.cs`, and populate those fields from `PreceptParser`. Graph diagnostics should anchor to declarations directly instead of re-deriving positions from raw text.

4. **Register C48–C53 in `DiagnosticCatalog`** — six new `LanguageConstraint` entries with `ConstraintSeverity.Warning` (C48, C49, C51, C52) and `ConstraintSeverity.Hint` (C50, C53). Add the `Hint` member to the `ConstraintSeverity` enum.

5. **Implement `PreceptAnalysis.Analyze(PreceptDefinition)`** in `src/Precept/Dsl/PreceptAnalysis.cs` — new static class with a single public method that performs:
   - BFS from initial state → reachable states
   - Unreachable states = all states − reachable (C48)
   - Orphaned events = declared events not referenced in any transition row (C49)
   - Dead-end states = non-terminal states where all outgoing rows have `NoTransition` or `Rejection` outcomes (C50)
    - Reject-only pairs = (state, event) pairs where every row ends in `Rejection` (C51)
    - Event never succeeds = events with at least one transition row where every reachable state either has no rows for that event or only reject rows (C52)
    - Empty precept = `definition.Events.Count == 0` (C53)
    - Terminal states in structured output (not a diagnostic)
    - Returns `AnalysisResult` with structured findings plus graph diagnostics

6. **Wire graph analysis into `Validate()`** — run `PreceptAnalysis.Analyze(model)` after type checking and compile-time default-data checks. Graph analysis should run whenever parsing succeeded and a model exists, even if type errors are present, but it must only emit structural findings that do not depend on successful expression evaluation.

7. **Add `CompileFromText(string text)` to `PreceptCompiler`** — a composed pipeline that runs `ParseWithDiagnostics` → `Validate` → `PreceptEngine` construction when no blocking diagnostics exist. Return a structured result carrying:
    - diagnostics only on parse failure
    - parsed model + diagnostics on validation failure
    - parsed model + compiled engine + diagnostics on success
    Tooling should consume this structured result instead of parsing exception strings.

8. **Update tooling consumers in the same pass** — the language server and MCP wrappers must consume the new structured validation/analysis result instead of keeping separate graph logic or regex-parsing compile exceptions.

9. **Confirm language server severity mapping** — Phase H already fixed `MapValidationDiagnostic()` to read `Constraint.Severity`. Verify that Warning and Hint diagnostics appear correctly in the VS Code Problems panel with the right severity icons.

**Tests:**

- [ ] Fully connected graph → no C48–C53 diagnostics
- [ ] Definition with unreachable state → C48 warning with state name
- [ ] Definition with orphaned event → C49 warning with event name
- [ ] Definition with dead-end state → C50 hint with state name
- [ ] Definition with reject-only (state, event) pair → C51 warning
- [ ] Definition with event that never succeeds from any reachable state → C52 warning
- [ ] Empty precept (no events) → C53 hint
- [ ] Terminal states (no outgoing transitions) → **not** flagged as dead-ends (terminal is intentional)
- [ ] `from any` expansion → correctly marks all states as having outgoing transitions
- [ ] Reject-only pair where some rows have guards and some don't → still C51 if all outcomes are reject
- [ ] Event with reject-only pairs in some states but success in others → **not** C52
- [ ] Warnings do not block compilation — `ValidationResult.HasErrors` remains false
- [ ] Compile-time default-data violations (C29–C32) aggregate as diagnostics instead of failing fast via exceptions
- [ ] Duplicate and subsumed state-assert violations (C44/C45) aggregate as diagnostics instead of failing fast via exceptions
- [ ] `CompileFromText` returns engine + model + warnings on valid input
- [ ] `CompileFromText` returns diagnostics only on parse failure
- [ ] `CompileFromText` returns partial model + diagnostics on type errors
- [ ] Catalog drift test covers C48–C53 trigger fixtures
- [ ] Language server shows C48–C53 in Problems panel with correct severity
- [ ] MCP validate/compile surfaces structured diagnostics without regex recovery from thrown messages

**Implementation prompt:**

Before making changes in a new Copilot Chat session, read the relevant design sections and this phase in full, then pause and recommend the most appropriate model for the work before continuing. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models if desired, then continue with implementation.

1. **Add `ConstraintSeverity.Hint`** to the enum in `src/Precept/Dsl/DiagnosticCatalog.cs` and update the language server severity mapping.

2. **Remove throw-based compile validation** from `PreceptCompiler.Validate()` by converting C29–C32 and C44/C45 into structured diagnostics collected during validation instead of `InvalidOperationException` control flow.

3. **Register C48–C53** in `src/Precept/Dsl/DiagnosticCatalog.cs` after the existing C47 registration. Use `ConstraintSeverity.Warning` for C48, C49, C51, C52 and `ConstraintSeverity.Hint` for C50, C53.

4. **Add declaration source lines** to `PreceptDefinition`, `PreceptState`, and `PreceptEvent`, and populate them in `PreceptParser` so graph diagnostics can anchor to declarations directly.

5. **Create `src/Precept/Dsl/PreceptAnalysis.cs`** — a new static class with a single public method `Analyze(PreceptDefinition definition)` returning an `AnalysisResult` record with:
    - BFS reachability
    - unreachable states (C48)
    - orphaned events (C49)
    - dead-end states (C50)
    - reject-only pairs (C51)
    - events that never succeed (C52)
    - empty precept hint (C53)
    - terminal states in structured output only

6. **Wire graph analysis into `PreceptCompiler.Validate()`** and ensure `ValidationResult.HasErrors` stays false for warning/hint-only results.

7. **Add `CompileFromText(string text)`** to `PreceptCompiler` and make it the structured entry point for tooling. It should return diagnostics only on parse failure, parsed model + diagnostics on validation failure, and model + engine + diagnostics on success.

8. **Update the language server and MCP wrappers** to consume `Validate()` / `CompileFromText()` directly. Remove duplicate graph diagnostics from the analyzer and stop parsing compile exception strings in MCP wrappers.

9. **Add trigger fixtures and focused tests** for C48–C53, warning-only validation, aggregated compile-time diagnostics, and `CompileFromText` result shapes.

Run all three test projects (`Precept.Tests`, `Precept.LanguageServer.Tests`, `Precept.Mcp.Tests`) before committing. Verify all 19 sample files still compile clean.

**Checkpoint:**

- `dotnet build` passes
- All tests green (core + language server + MCP)
- `precept_validate` MCP tool (current implementation) surfaces warning/hint diagnostics without exception parsing
- VS Code Problems panel shows warning/hint icons for graph findings
- All 19 sample files produce zero C51/C52 warnings (no sample uses reject-only anti-pattern)

---

## Additional test coverage to add

These coverage items should be added to the implementation plan even if the implementation itself is split across several commits.

### Core checker coverage

- [ ] Equality rule matrix: same-type comparisons, cross-type rejection, nullable-compatible cases
- [ ] `== null` and `!= null` semantics for nullable and non-nullable operands
- [ ] Event-arg null narrowing in transition guards
- [ ] Cross-event arg reference rejection
- [ ] Event-assert scope isolation (data-field references rejected)
- [ ] Non-boolean invariant rejection
- [ ] Non-boolean state-assert rejection
- [ ] Non-boolean event-assert rejection
- [ ] Parenthesized narrowing cases
- [ ] Nested unary expression typing
- [ ] Multiple diagnostics in a single validation run (3+ independent errors)

### Collection and mutation coverage

- [ ] Nullable value used in `add` / `enqueue` / `push` without narrowing
- [ ] `pop` / `dequeue into` target incompatible with source inner type
- [ ] `contains` with invalid lhs shape
- [ ] `contains` with rhs type mismatch

### Language-server and integration coverage

- [ ] Analyzer completions remain type-filtered after equality-rule changes
- [ ] Narrowed event-arg scopes appear correctly in `PreceptTypeContext`
- [ ] `from any` expansion still attributes diagnostics to the specific state
- [ ] Workflow compile-blocking behavior mirrors `PreceptCompiler.Validate()` diagnostics

## Design discussions required to finalize the next wave

All design discussions for the current compile-time expansion wave (Phases D–H) have been resolved. No open questions remain.

### Diagnostic code policy (Locked)

**Rule:** overload an existing code when the new condition is the same conceptual error category. Introduce a new code only when a genuinely new category emerges.

Existing codes C38–C43 are stable and will not be split. The message text within each code provides the specificity (e.g., C41 covers all binary operator type errors but the message names the exact operator and operand types).

**Planned new codes for upcoming phases:**

| Code | Category | Phase | Trigger |
|---|---|---|---|
| C44 | Non-boolean rule position | F | `invariant`, `assert`, or `when` expression that doesn’t produce boolean |
| C45 | Identical-guard duplicate row | G | Two rows for the same `(state, event)` with the same guard expression |
| C48 | Unreachable state | I | State not reachable from initial via BFS |
| C49 | Orphaned event | I | Event declared but never referenced in `from … on` rows |
| C50 | Dead-end state | I | Non-terminal state where all outgoing transitions reject or no-transition |
| C51 | Reject-only pair | I | Every row for a (state, event) pair ends in `reject` |
| C52 | Event never succeeds | I | Event has rows but every reachable state either has no rows or all rows reject for that event |
| C53 | Empty precept | I | Precept declares no events |

**Reuse of existing codes:**

| Existing code | New condition | Rationale |
|---|---|---|
| C38 (unknown identifier) | Bare arg name in transition row | Not in symbol table — same category as any unknown identifier |
| C38 (unknown identifier) | Cross-event arg reference | `EventB.Arg` not in scope for `EventA` row — same resolution failure |
| C41 (binary operator) | Incompatible equality operands | Same conceptual error as other binary operator type mismatches |

Overhead per new code: ~6 lines total (4–5 in `DiagnosticCatalog.cs`, 1–2 in `CatalogDriftTests.cs`). Language server and MCP tools require zero per-code changes.

### Diagnostic severity policy (Locked)

All diagnostics follow the existing three-tier model already established in the language server:

- **Error**: provably wrong — type contradictions, null-flow violations, structural impossibilities (unreachable rows, identical-guard duplicates). Blocks compilation.
- **Warning**: probably wrong — structural quality observations from graph analysis (reject-only pairs, events that never succeed, orphaned events, unreachable states). Does not block compilation. **Key principle:** `reject` is for conditional denial within a state where some paths succeed. If an event can never succeed from a state, the rows should not exist — `Undefined` is the correct outcome. Writing reject-only rows for structurally unavailable events is an anti-pattern (see `PreceptLanguageDesign.md` § Reject vs Undefined).
- **Hint**: informational — dead-end states, empty precepts.

The `ConstraintSeverity` enum in `DiagnosticCatalog` already supports `Error`, `Warning`, and `Hint`. The `MapValidationDiagnostic` method in the analyzer should respect `Constraint.Severity` rather than hardcoding `Error`. New type-checker diagnostics that are provable contradictions use `Error`; any future structural-quality diagnostics promoted from the analyzer to the checker should use `Warning` or `Hint` as appropriate.

### Mandatory vs. Advisory

This plan contains two kinds of guidance — know which is which:

**Mandatory (do not deviate):**
- **Phase ordering and checkpoints** — dependencies are real (tokenizer → parser → runtime). Each phase must end with `dotnet build` passing.
- **Model record shapes** — dictated by the language design spec. Field names, record structures, and semantic meanings are locked.
- **Token enum members** — dictated by the grammar. Every keyword, operator, and punctuation symbol must be represented.
- **Fire pipeline stages and evaluation order** — locked semantics from the design doc.
- **Preposition scoping rules** — `in`/`to`/`from`/`on` meanings are locked.
- **Public API stability** — `PreceptEngine` method signatures, result types, and `DslWorkflowInstance` must not change.
- **Preserve list** — files marked "unchanged" must not be modified.

**Advisory (one reasonable approach — adapt freely):**
- **Catalog infrastructure patterns** — `ConstructCatalog.Register`, `DiagnosticCatalog`, `// SYNC:CONSTRAINT:Cnn` markers, `MessageTemplate` with placeholders. The *requirement* is that parser constructs and semantic constraints are introspectable at runtime (for language server features and future MCP serialization). The specific C# patterns shown are suggestions. If a simpler approach emerges (static arrays, source generators, well-organized constants), use it.
- **Superpower-specific techniques** — `Parse.Chain` for precedence, keyword recognition callbacks, tokenizer rule ordering. These describe how Superpower *probably* works based on documentation. If the actual API differs or offers better patterns, follow the library, not this plan.
- **Language server implementation details** — attribute-driven `SemanticTypeMap`, `BuildFromAttributes()` reflection, the `Previous token(s) → Suggest` lookup table. These are one approach. The *requirement* is that semantic tokens and completions derive from the token stream (not regex). How that mapping is structured is flexible.
- **Error recovery strategy** — "Skip to next statement-starting keyword" is a reasonable heuristic, but Superpower may have its own error recovery patterns that work better.
- **Code samples** — all ````csharp` blocks in this plan are illustrative. The structure and naming should be consistent with the codebase conventions discovered during implementation, not copied verbatim.
- **LOC estimates** — rough guidance for scoping, not targets.

When the plan says "do X," check: is it a semantic/behavioral requirement, or an implementation technique? If the latter, treat it as a starting point and improve on it when you find something better.

---

## Guiding Principle: Tokenize Once, Consume Everywhere

Superpower produces a `TokenList<PreceptToken>` with exact source spans (`Position` = line, column, absolute offset). This single token stream replaces all regex-based text analysis across the entire codebase:

| Consumer | Currently | After |
|---|---|---|
| Parser (`PreceptParser.cs`) | 20+ compiled Regex, 1308 lines imperative | Superpower combinators on `TokenList<PreceptToken>` |
| Expression parser (`PreceptExpressionParser.cs`) | Hand-written `Lexer` (224 lines) + recursive descent `Parser` | Superpower expression combinators on same token stream (no separate lexer) |
| Semantic tokens (`PreceptSemanticTokensHandler.cs`) | 12 Regex per line | Walk `TokenList<PreceptToken>` — token kind → semantic token type |
| Completions (`PreceptAnalyzer.cs`) | 8+ Regex for context detection | Token-at-cursor + previous-token lookup from `TokenList<PreceptToken>` |
| Diagnostics (`PreceptAnalyzer.cs`) | Exception message regex parsing | Superpower parse errors with position + expected tokens → LSP `Diagnostic` |
| Preview handler | Calls `PreceptParser.Parse(text)` | Same — just a different parser behind the same API |

### Token stream as shared infrastructure

The tokenizer is the **foundation layer** that every other component builds on. It must be robust to incomplete input (language server feeds it partial/in-progress files on every keystroke).

Key design decisions:
- Tokenization **always succeeds** — unknown characters become an `Error` or `Unknown` token, never an exception. This guarantees the language server always has a token list for coloring and completions, even when the file is mid-edit.
- The parser works on the token list and may fail — that's fine. Token-level features (semantic coloring, completions) work regardless.
- Statement-level error recovery: on parse failure, skip forward to the next statement-starting keyword token (`field`, `state`, `event`, `invariant`, `in`, `to`, `from`, `on`) and resume.

---

## Phase 0: Branch + Superpower Dependency

**Goal:** Clean branch, Superpower installed, builds green.

- [ ] Create branch `feature/language-redesign` from current main
- [ ] Add `Superpower` NuGet package to `src/Precept/Precept.csproj`
- [ ] Verify `dotnet build` passes (no code changes yet)

---

## Phase 1: Token Enum + Tokenizer

**Goal:** A `PreceptTokenizer` that converts raw source text into `TokenList<PreceptToken>`.

**New files:**
- `src/Precept/Dsl/PreceptToken.cs` — token enum
- `src/Precept/Dsl/PreceptTokenizer.cs` — Superpower `Tokenizer<PreceptToken>`

### Token enum

Each member carries three attributes for runtime reflection: `[TokenCategory]`, `[TokenDescription]`, and `[TokenSymbol]`. These attributes are **core infrastructure** — they power the tokenizer keyword dictionary, language server completions ('group by category'), semantic tokens, parser error messages ('expected a type keyword'), and are reflected by the MCP `precept_language` tool's vocabulary tier. See `docs/CatalogInfrastructureDesign.md` for the full architecture rationale.

```csharp
public enum PreceptToken
{
    // === Keywords: declarations ===
    [TokenCategory(TokenCategory.Declaration)] [TokenDescription("Top-level precept declaration")] [TokenSymbol("precept")]
    Precept,
    [TokenCategory(TokenCategory.Declaration)] [TokenDescription("Declares a data field")] [TokenSymbol("field")]
    Field,
    // ... remaining members follow the same pattern — every member has all three attributes
    As, Nullable, Default, Invariant, Because,
    State, Initial, Event, With, Assert, Edit,

    // === Keywords: prepositions + modifiers ===
    In, To, From, On, When, Any, Of,

    // === Keywords: actions ===
    Set, Add, Remove, Enqueue, Dequeue, Push, Pop, Clear, Into,

    // === Keywords: outcomes ===
    Transition, No, Reject,

    // === Type keywords ===
    StringType, NumberType, BooleanType,

    // === Literal keywords ===
    True, False, Null,

    // === Operators ===
    [TokenSymbol("==")]
    DoubleEquals,
    [TokenSymbol("!=")]
    NotEquals,
    GreaterThanOrEqual, // >=
    LessThanOrEqual,    // <=
    GreaterThan,     // >
    LessThan,        // <
    And,             // &&
    Or,              // ||
    Not,             // !
    Assign,          // =
    Contains,        // contains (keyword-operator)

    // === Punctuation ===
    [TokenSymbol("->")]
    Arrow,
    [TokenSymbol(",")]
    Comma,
    Dot,             // .
    LeftParen,       // (
    RightParen,      // )
    LeftBracket,     // [
    RightBracket,    // ]

    // === Identifiers + literals ===
    Identifier,
    StringLiteral,
    NumberLiteral,

    // === Structure ===
    Comment,         // # ...
    NewLine
}
```

### Tokenizer rules (priority-ordered)

1. Multi-char operators first: `==`, `!=`, `>=`, `<=`, `&&`, `||`, `->`
2. Single-char operators: `>`, `<`, `=`, `!`
3. Single-char punctuation: `,`, `.`, `(`, `)`, `[`, `]`
4. String literals: `"..."` (with `\"` escape)
5. Number literals: `\d+(\.\d+)?`
6. Comments: `#` to end-of-line
7. Keywords vs identifiers: tokenize as `Identifier`, then use Superpower's keyword recognition to promote matching identifiers. The keyword dictionary is **built by reflecting `[TokenSymbol]` attributes** on all keyword-category tokens — adding a keyword to the enum automatically adds it to the tokenizer (zero drift). This ensures `From` (uppercase) stays an `Identifier` while `from` (lowercase) becomes `From` token.
8. Newlines: `\r\n` or `\n` — significant only for comment termination and statement boundary heuristics, but tracked as tokens for position accuracy.

### Checkpoint

- `dotnet build` passes
- Unit test: tokenize a minimal `.precept` source and assert the token sequence
- Every `PreceptToken` member has `[TokenCategory]`, `[TokenDescription]`, and `[TokenSymbol]` attributes (keyword/operator/punctuation members require `[TokenSymbol]`; value/structure tokens may omit it)

---

## Phase 2: Model Records

**Goal:** New model records that represent the design doc's semantics. Old model records stay until Phase 4.

**Modified file:** `src/Precept/Dsl/PreceptModel.cs`

### New/changed records

| Record | Change | Rationale |
|---|---|---|
| `DslWorkflowModel` | Add `Invariants`, `StateAsserts`, `StateActions`; change `Transitions` to `IReadOnlyList<DslTransitionRow>` | New concepts + flat first-match rows |
| `DslInvariant` | **New** | `invariant <expr> because "reason"` — global data truth |
| `DslStateAssert` | **New** | `in/to/from <State> assert <expr> because "reason"` — with `Preposition` enum |
| `DslStateAction` | **New** | `to/from <State> -> <actions>` — entry/exit actions |
| `DslAssertPreposition` | **New enum** | `In`, `To`, `From` |
| `DslTransitionRow` | Replaces `DslTransition` | Flat: `FromState`, `EventName`, `WhenGuard?`, `Actions`, `Outcome`. No nested `DslClause` array. |
| `DslTransition` | **Removed** | Replaced by `DslTransitionRow` |
| `DslClause` | **Removed** | Clauses (if/else if/else) are replaced by ordered first-match rows |
| `DslRule` | **Removed** | Replaced by `DslInvariant`, `DslStateAssert`, `DslEventAssert` |
| `DslEventAssert` | **New** | `on <Event> assert <expr> because "reason"` — event arg validation |
| `DslEditBlock` | **New** | `in <State> edit <Field>, <Field>` — editable field declaration. Runtime `Update` API deferred; model/parser included now. |
| `DslEvent` | Drop `Rules`; args now use `with` keyword (parser concern only) | |
| `DslState` | Drop `Rules` | State asserts/actions are top-level, not attached to states |
| `DslField` | Drop `Rules` | Invariants are top-level |

### Preserve unchanged

- `DslExpression` hierarchy (all 5 records) — unchanged
- `DslSetAssignment` — unchanged
- `DslCollectionMutation` / `DslCollectionMutationVerb` — unchanged
- `DslClauseOutcome` hierarchy (`DslStateTransition`, `DslRejection`, `DslNoTransition`) — unchanged
- `DslScalarType`, `DslCollectionKind` — unchanged
- `DslEventArg` — unchanged

### Checkpoint

- `dotnet build` passes (old parser and runtime may use `#pragma` or compatibility shims temporarily)

---

## Phase 3: Superpower Parser

**Goal:** `PreceptParser.Parse(string)` returns a `DslWorkflowModel` using the new token+combinator pipeline.

**Modified file:** `src/Precept/Dsl/PreceptParser.cs` — **full rewrite**

### Architecture

```
string source
  → PreceptTokenizer.Tokenize(source)      // TokenList<PreceptToken>
  → PreceptParser.FileParser.Parse(tokens)  // DslWorkflowModel
```

### Parser combinators (one per grammar rule)

Organized by statement kind:

```csharp
// Entry point
static TokenListParser<PreceptToken, DslWorkflowModel> FileParser = ...;

// Declarations
static TokenListParser<PreceptToken, ...> PreceptHeader = ...;  // precept <Name>
static TokenListParser<PreceptToken, DslField> FieldDecl = ...;  // field <Name> as <Type> [nullable] [default <val>]
static TokenListParser<PreceptToken, DslInvariant> InvariantDecl = ...;  // invariant <expr> because "reason"
static TokenListParser<PreceptToken, DslState> StateDecl = ...;  // state <Name> [initial]
static TokenListParser<PreceptToken, DslEvent> EventDecl = ...;  // event <Name> [with <args>]

// Asserts
static TokenListParser<PreceptToken, DslStateAssert> StateAssert = ...;  // in/to/from <Target> assert <expr> because "reason"
static TokenListParser<PreceptToken, DslEventAssert> EventAssert = ...;  // on <Event> assert <expr> because "reason"

// Actions
static TokenListParser<PreceptToken, DslStateAction> StateAction = ...;  // to/from <Target> -> <chain>

// Editable fields
static TokenListParser<PreceptToken, DslEditBlock[]> EditDecl = ...;  // in <Target> edit <Field>, <Field>

// Transitions
static TokenListParser<PreceptToken, DslTransitionRow> TransitionRow = ...;  // from <Target> on <Event> [when <expr>] -> ... -> <outcome>

// Expressions (ported from DslExpressionParser)
static TokenListParser<PreceptToken, DslExpression> BoolExpr = ...;
// Uses Parse.Chain for operator precedence:
//   Level 1: ||
//   Level 2: &&
//   Level 3: ==, !=, >, >=, <, <=, contains
//   Level 4: ! (unary)
//   Level 5: atom (literal, identifier, identifier.member, parenthesized)

// Shared pieces
static TokenListParser<PreceptToken, string[]> StateTarget = ...;  // any | Name(, Name)*
static TokenListParser<PreceptToken, DslClauseOutcome> Outcome = ...;  // transition <State> | no transition | reject "reason"
static TokenListParser<PreceptToken, ...> ActionChain = ...;  // (-> Action)+
```

### Expression parser unification

The current `DslExpressionParser` (with its own `Lexer` class) is **eliminated**. Expression parsing uses the same Superpower token stream via combinators.

**Why eliminate it:**
- The hand-written `Lexer` class (~224 lines) is redundant — the Superpower tokenizer already produces typed tokens with spans. Keeping a separate lexer means maintaining two tokenization implementations that must agree on what constitutes a number, string, identifier, and operator.
- Substring extraction is fragile — the current parser extracts expression text as a raw string and hands it to `DslExpressionParser.Parse(string)`. This loses source position information, so expression-level diagnostics can't report accurate line/column. With unified token parsing, every sub-expression node carries its original source span.
- One fewer parser to maintain — bugs or features only need to be addressed in one place.

**Superpower-specific techniques for expression parsing:**

- **`Parse.Chain` for binary operators** — Superpower's `Parse.Chain(operatorParser, operandParser)` handles left-recursive binary operator chains with correct associativity. Define one chain per precedence level:
  ```
  Level 1 (lowest):  ||          via Parse.Chain(Or, level2)
  Level 2:           &&          via Parse.Chain(And, level3)
  Level 3:           ==, !=, >, >=, <, <=, contains   via Parse.Chain(comparison, level4)
  Level 4 (unary):   !           via prefix combinator on atom
  Level 5 (atom):    literal | identifier[.member] | (expr)
  ```
- **`contains` as a keyword-operator** — Unlike `&&` or `==`, `contains` is a keyword token, not a symbol token. It must appear in the Level 3 comparison operator parser alongside the symbol operators. In the token enum it's `PreceptToken.Contains`; in the operator parser it produces a `DslBinaryExpression` with `Operator = "contains"`, same as today.
- **Combinator testability** — Each expression level is a `static` field that can be tested in isolation:
  ```csharp
  var result = ExprAtom.TryParse(tokenize("Balance"));
  var result = BoolExpr.TryParse(tokenize("Balance > 0 && Email != null"));
  ```
  This makes precedence and associativity easy to verify without parsing a full `.precept` file.

### Keyword recognition strategy

All keywords are **strictly lowercase** (design doc: "Keywords are strictly lowercase. Identifiers are case-sensitive."). The tokenizer handles this by:

1. Tokenize all `[A-Za-z_][A-Za-z0-9_]*` sequences as `PreceptToken.Identifier`
2. Post-process: exact-match lowercase identifiers against the keyword table → promote to the specific keyword token (e.g., `"from"` → `PreceptToken.From`)
3. Non-matching identifiers stay as `PreceptToken.Identifier`

This ensures `From` (capital F) remains a valid identifier while `from` is a keyword. Superpower's `Tokenizer<T>` supports this via keyword recognition callbacks.

### `because` as expression terminator

When parsing `invariant <expr> because "reason"` or `assert <expr> because "reason"`, the expression combinator parses tokens until it encounters `Because`. This is a natural terminator — expressions can never contain the keyword `because`.

### Construct catalog (core infrastructure)

Each parser combinator is registered with a syntax template, description, and working example. This is **core infrastructure** — not MCP-specific. It powers parser error messages, language server hovers, and is serialized by the MCP `precept_language` tool.

**New file:** `src/Precept/Dsl/ConstructCatalog.cs`

```csharp
public sealed record ConstructInfo(string Name, string Form, string Context, string Description, string Example);

public static class ConstructCatalog
{
    private static readonly List<ConstructInfo> _constructs = [];
    public static IReadOnlyList<ConstructInfo> Constructs => _constructs;

    public static TokenListParser<PreceptToken, T> Register<T>(
        this TokenListParser<PreceptToken, T> parser, ConstructInfo info)
    {
        _constructs.Add(info);
        return parser;
    }
}
```

Usage in parser:

```csharp
static readonly TokenListParser<PreceptToken, DslField> FieldDecl =
    ( /* combinator chain */ )
    .Register(new ConstructInfo(
        "field-declaration",
        "field <Name> as <Type> [nullable] [default <Value>]",
        "top-level",
        "Declares a scalar data field tracked across events.",
        "field Priority as number default 3"));
```

Consumers:
- **Parser error messages:** `"Invalid field declaration. Expected: field <Name> as <Type> [nullable] [default <Value>]"`
- **Language server hovers:** Show the syntax template when hovering over a keyword
- **Language server completions:** Show descriptions alongside keyword suggestions
- **MCP `precept_language`:** Serializes `ConstructCatalog.Constructs` directly

### Error recovery strategy

- The `FileParser` combinator is: `PreceptHeader` then `Statement.Many()` where `Statement` tries each statement kind (field, invariant, state, etc.) via `TokenListParser.Or()`.
- On failure, skip to the next statement-starting keyword and resume. Collect errors along the way.
- Expose `ParseWithDiagnostics(string) → (DslWorkflowModel?, IReadOnlyList<ParseDiagnostic>)` for the language server.

### Checkpoint

- `dotnet build` passes
- Parser tests: parse a full `.precept` file → verify model
- Round-trip test: current samples parse without errors
- Every parser combinator registered in `ConstructCatalog` with a parseable example

---

## Phase 4: Runtime Adaptation

**Goal:** `PreceptEngine` works with the new model records.

**Modified files:**
- `src/Precept/Dsl/PreceptRuntime.cs` — moderate changes
- `PreceptCompiler` (inside `PreceptRuntime.cs`) — updated validation

### Key changes

| Area | Current | New |
|---|---|---|
| Transition lookup | `Dict<(State,Event), DslTransition>` (single entry, clauses inside) | `Dict<(State,Event), List<DslTransitionRow>>` (ordered list, first-match) |
| Transition resolution | Iterate `DslClause` in a `DslTransition` (if/else if/else guards) | Iterate `DslTransitionRow` list; `when` guard → skip if false, else match |
| Rule evaluation | `_topLevelRules` + `_allFieldRules` + `_stateRuleMap` + `_eventRules` (all `DslRule`) | `_invariants` + `_stateAsserts` (with preposition-aware evaluation) + `_eventAsserts` |
| State asserts | Single `DslRule` list per state | `DslStateAssert` with `In`/`To`/`From` preposition; evaluation depends on whether state is changing and direction |
| State actions | Not implemented | New: run exit actions → row mutations → entry actions |
| Compile-time checks | Field/collection/state rules against defaults | All 5 state assert checks (subsumption, duplication, initial-state, contradiction, deadlock) + transition checks (coverage, unreachable row, missing outcome) |

### Constraint catalog (core infrastructure)

Semantic constraints are declared as data alongside their enforcement code. This is **core infrastructure** — not MCP-specific. The constraint descriptions serve as error messages, power language server diagnostics, and are serialized by the MCP `precept_language` tool.

**New file:** `src/Precept/Dsl/DiagnosticCatalog.cs`

```csharp
public sealed record LanguageConstraint(
    string Id,
    string Phase,
    string Rule,
    string MessageTemplate,
    ConstraintSeverity Severity);

public enum ConstraintSeverity { Error, Warning }

public static class DiagnosticCatalog
{
    private static readonly List<LanguageConstraint> _constraints = [];
    public static IReadOnlyList<LanguageConstraint> Constraints => _constraints;

    public static LanguageConstraint Register(
        string id, string phase, string rule,
        string messageTemplate, ConstraintSeverity severity = ConstraintSeverity.Error)
    {
        var c = new LanguageConstraint(id, phase, rule, messageTemplate, severity);
        _constraints.Add(c);
        return c;
    }
}
```

Usage at each enforcement point in parser/compiler/engine:

```csharp
// SYNC:CONSTRAINT:C7
static readonly LanguageConstraint C7 = DiagnosticCatalog.Register(
    "C7", "parse",
    "Non-nullable fields without 'default' are a parse error.",
    "Field '{fieldName}' is non-nullable and has no default value.");

if (!field.HasDefault && !field.IsNullable)
    throw new ParseException(C7.FormatMessage(new { fieldName = field.Name }));
```

Consumers:
- **Parser/compiler/engine error messages:** `MessageTemplate` with contextual placeholders IS the error message — the `Rule` property provides the general description, while the template produces the specific diagnostic. No duplication.
- **Language server diagnostics:** Each diagnostic carries a stable code (`C7` → `PRECEPT007`), the constraint's `Severity` mapped to LSP `DiagnosticSeverity`, and the formatted message. The `Rule` is available as supplementary detail.
- **MCP `precept_language`:** Serializes `DiagnosticCatalog.Constraints` directly (ID, phase, rule)
- **`// SYNC:CONSTRAINT:Cnn` comments:** Copilot-visible markers at each enforcement point

Drift defense:
- `[Fact]` test: every registered constraint has a matching `// SYNC:CONSTRAINT:Cnn` comment in the codebase, and vice versa
- `[Theory]` test: each constraint has a test case with a violating input that triggers the expected error
- `copilot-instructions.md`: tells Copilot the sync rule explicitly

### Fire pipeline update

Current:
1. Event rules (arg-only)
2. Guard evaluation (resolve clause within single transition)
3. Set execution
4. Collection mutations
5. Field + top-level rules (post-mutation)
6. State rules (on transition)

New (per design doc):
1. **Event asserts** (`on <Event> assert`) — arg-only context, pre-transition. If false → `Rejected`.
2. **First-match row selection** — iterate rows for `(state, event)`, evaluate `when` guards. First match wins. No match → `Unmatched`.
3. **Exit actions** (`from <SourceState> ->`) — run automatic exit mutations.
4. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — the matched row's action chain.
5. **Entry actions** (`to <TargetState> ->`) — run automatic entry mutations.
6. **Validation** — invariants, state asserts (`in`/`to`/`from` with correct temporal scoping), collect-all. If any fail → full rollback, `ConstraintFailure`.

### State assert evaluation logic

```
Given: sourceState, targetState, proposedData

if sourceState == targetState (NoTransition):
    evaluate all `in <sourceState>` asserts
else (state transition):
    evaluate all `from <sourceState>` asserts
    evaluate all `to <targetState>` asserts
    evaluate all `in <targetState>` asserts
```

### Preserve unchanged

- `DslWorkflowInstance` record — unchanged
- `DslFireResult`, `DslInspectionResult`, `DslEventInspectionResult` — unchanged
- `DslOutcomeKind` enum — unchanged
- `CreateInstance()` — minor updates for new field defaults
- `Inspect()` — updated to use new model but same return types
- `CheckCompatibility()` — unchanged
- `CoerceEventArguments()` — unchanged
- All collection operations — unchanged
- `DslExpressionRuntimeEvaluator` — unchanged

### Checkpoint

- `dotnet build` passes
- Existing test logic adapted to new model and new syntax strings
- Fire/Inspect behavior matches design doc pipeline

---

## Phase 5: Tests

**Goal:** All tests pass with the new syntax and semantics.

**Modified files:** All 7 test files in `test/Precept.Tests/`

### Test migration strategy

1. **Expression parser tests** (`DslExpressionParserTests.cs`, `DslExpressionParserEdgeCaseTests.cs`) — likely minimal changes if the expression parser API stays `DslExpressionParser.Parse(string)`. If expressions are fully integrated into Superpower, these tests either test the expression combinator in isolation or remain as integration tests.

2. **Expression evaluator tests** (`DslExpressionRuntimeEvaluatorBehaviorTests.cs`) — keep pure runtime-valid expression tests here. Any expression that is intentionally compile-invalid belongs in type-checker / compiler tests instead of runtime-fire tests.

3. **Workflow tests** (`DslWorkflowTests.cs`) — rewrite all embedded DSL strings to new syntax. Test behavior stays the same.

4. **Set/collection tests** (`DslSetParsingTests.cs`, `DslCollectionTests.cs`) — rewrite DSL strings. Collection semantics unchanged.

5. **Rules tests** (`DslRulesTests.cs`) — rewrite to use `invariant ... because` and `assert ... because` syntax. Add new tests for `in`/`to`/`from` preposition semantics.

### New tests to add

- [ ] Tokenizer tests: specific token sequences for each statement form
- [ ] Parser combinator unit tests: each combinator in isolation
- [ ] `invariant` evaluation (always, post-commit)
- [ ] `in <State> assert` — checked on entry + NoTransition
- [ ] `to <State> assert` — checked only on cross-state entry
- [ ] `from <State> assert` — checked only on cross-state exit
- [ ] State entry/exit actions — execution order verification
- [ ] First-match row evaluation (guarded + unguarded fallback)
- [ ] `when` guard → `Unmatched` when no row matches
- [ ] Multi-state targets (`Open, InProgress`) and `any`
- [ ] `because` sentinel parsing (expressions with various operators before `because`)
- [ ] `event Name with args` syntax
- [ ] `field Name as type nullable default val` syntax
- [ ] Compile-time checks: subsumption, duplication, initial-state, contradiction, deadlock
- [ ] sectionless file parsing (statements in any order)
- [ ] Edit block parsing: `in <State> edit <Field>, <Field>` produces `DslEditBlock` records
- [ ] Multi-state edit: `in Open, InProgress edit` expands to one `DslEditBlock` per state
- [ ] `in any edit` expands to one `DslEditBlock` per declared state
- [ ] Additive semantics: overlapping edit declarations produce unioned field sets
- [ ] Compile-time checks: unknown field, unknown state, duplicate field (warning), empty field list (error)
- [ ] Disambiguation: `in Open assert` vs `in Open edit` parsed correctly
- [ ] Construct catalog: every registered example parses successfully
- [ ] Constraint catalog: every registered constraint has a violating input that triggers the expected error
- [ ] Constraint catalog: every `// SYNC:CONSTRAINT:Cnn` comment has a matching registry entry, and vice versa
- [ ] Token attributes: every `PreceptToken` member has `[TokenCategory]` and `[TokenDescription]`; keyword/operator/punctuation members have `[TokenSymbol]`
- [ ] Documentation constraints match: constraint list in `docs/DesignNotes.md § DSL Syntax Contract` matches `DiagnosticCatalog.Constraints` (same IDs, same rules)
- [ ] Reference sample coverage: at least one `.precept` sample file uses every construct registered in `ConstructCatalog`
- [ ] Equality rule matrix and null-comparison coverage once the comparison policy is locked
- [ ] Event-arg narrowing and event-assert scope-isolation coverage
- [ ] Non-boolean rule-position coverage for invariants, state asserts, event asserts, and guards
- [ ] Collection-mutation nullability coverage across `add`, `enqueue`, and `push`
- [ ] Multi-diagnostic validation coverage with 3+ independent compile-time errors

### Checkpoint

- `dotnet test` all green
- No skipped tests unless explicitly noted

---

## Phase 6: Samples

**Goal:** All 18 `.precept` sample files updated to new syntax.

**Modified files:** All files in `samples/`

### Migration pattern per file

```
Old:                              New:
machine Name                →     precept Name
from X edit (indented)      →     in X edit Field1, Field2  (flat comma-separated)
string? Email               →     field Email as string nullable
number Balance = 0          →     field Balance as number default 0
set<string> Tags            →     field Tags as set of string
state Open initial          →     state Open initial          (unchanged)
event Submit(string token)  →     event Submit with token as string
rule expr "reason"          →     invariant expr because "reason"  (or assert variant)
from X on Y                 →     from X on Y                (mostly unchanged)
  if guard reason "msg"     →     from X on Y when guard -> ... -> outcome
  set K = V                 →     (inline in -> pipeline)
  transition Z              →     (inline as -> transition Z)
```

### Checkpoint

- All 18 files parse without errors
- Preview handler can snapshot/fire/inspect at least one sample

---

## Phase 7: Language Server — Token-Based Rewrite

**Goal:** Language server uses the Superpower token stream instead of regex for everything.

This is where the "tokenize once, consume everywhere" principle pays off most.

### 7a. Shared tokenization layer

Add a shared tokenization method in the analyzer or a dedicated utility:

```csharp
// In PreceptAnalyzer or a new PreceptLspUtils
internal static TokenList<PreceptToken>? TryTokenize(string text)
{
    try { return PreceptTokenizer.Instance.Tokenize(text); }
    catch { return null; }
}
```

The language server calls `TryTokenize()` once per document change and caches the result. Parser, semantic tokens, completions, and diagnostics all consume from this cached token list.

### 7b. Semantic tokens (`PreceptSemanticTokensHandler.cs`) — rewrite

**Current:** 12+ regex patterns matched per line, manually pushing semantic tokens.

**New:** Walk `TokenList<PreceptToken>` with **attribute-driven token type mapping**. The `[TokenCategory]` attribute determines the semantic token type — no hardcoded `switch` arms for individual enum members:

```csharp
// Built once at startup via reflection
static readonly Dictionary<PreceptToken, string> SemanticTypeMap = BuildFromAttributes();

static Dictionary<PreceptToken, string> BuildFromAttributes()
{
    var map = new Dictionary<PreceptToken, string>();
    foreach (var member in Enum.GetValues<PreceptToken>())
    {
        var category = member.GetCustomAttribute<TokenCategoryAttribute>()?.Category;
        var semanticType = category switch
        {
            TokenCategory.Control or TokenCategory.Declaration
                or TokenCategory.Action or TokenCategory.Outcome => "keyword",
            TokenCategory.Type => "type",
            TokenCategory.Literal => "keyword",  // true/false/null
            TokenCategory.Operator => "operator",
            TokenCategory.Structure => member == PreceptToken.Comment ? "comment" : null,
            _ => null  // Value tokens classified contextually
        };
        if (semanticType != null) map[member] = semanticType;
    }
    return map;
}

foreach (var token in tokens)
{
    var semanticType = SemanticTypeMap.GetValueOrDefault(token.Kind)
        ?? (token.Kind == PreceptToken.Identifier ? ClassifyIdentifier(token, context) : null)
        ?? (token.Kind == PreceptToken.StringLiteral ? "string" : null)
        ?? (token.Kind == PreceptToken.NumberLiteral ? "number" : null);
    if (semanticType != null)
        builder.Push(token.Span.Position.Line, token.Span.Position.Column, token.Span.Length, semanticType);
}
```

Adding a new keyword to the `PreceptToken` enum with `[TokenCategory]` automatically gives it the correct semantic coloring — no separate change needed in the semantic tokens handler.

Identifier classification (state name, event name, field name, variable) uses the parsed model or declaration-tracking from earlier tokens.

### 7c. Completions (`PreceptAnalyzer.cs`) — rewrite

**Current:** 8+ regex patterns to detect cursor context, manual identifier collection.

**New:** Find the token at or before the cursor position in the token list. **Keyword sets and descriptions are derived from catalog infrastructure** — no hand-maintained lists:

- **Keyword groups** from `[TokenCategory]`: `TokenCategory.Type` → type keywords, `TokenCategory.Action` → action keywords, etc.
- **Completion detail text** from `[TokenDescription]`: each keyword suggestion shows its description (e.g., `field` → "Declares a data field")
- **Statement-level descriptions** from `ConstructCatalog`: statement-starting keywords show the construct's `Form` + `Description` (e.g., `field` → "field \<Name\> as \<Type\> [nullable] [default \<Value\>]")

| Previous token(s) | Suggest |
|---|---|
| `As` | Type keywords (from `TokenCategory.Type`): `string`, `number`, `boolean`, `set`, `queue`, `stack` |
| `Arrow` (`->`) | Action keywords (from `TokenCategory.Action` + `TokenCategory.Outcome`): `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `transition`, `no`, `reject` |
| `From` + `Identifier` | `on`, `assert`, `->` |
| `From` + `Identifier` + `On` | Event names |
| `On` + `Identifier` | `assert` |
| `In`/`To` + `Identifier` | `assert`, `->`, `edit` (for `In` only) |
| `In` + `Identifier` + `Edit` | Declared field names (comma-separated list) |
| `Of` | Scalar type keywords: `string`, `number`, `boolean` |
| `Identifier` + `Dot` | Member names: `count`, `min`, `max`, `peek`, event arg names |
| Start of line / no context | Statement-starting keywords (from `ConstructCatalog` top-level constructs), semantically ordered: `field`, `invariant`, `state`, `in`, `to`, `from`, `event`, `on` |

Statement-starting keyword ordering uses semantic context: what's already declared in the file determines priority (per design doc: fields first → states → events → transitions).

### 7d. Diagnostics (`PreceptAnalyzer.cs`)

**Current:** Try `PreceptParser.Parse()`, catch exception, regex-parse error message for line number.

**New:** Use `ParseWithDiagnostics()` from Phase 3. Each diagnostic has exact position from Superpower's token spans → map directly to LSP `Diagnostic` with precise range.

**Diagnostic codes from constraint IDs:** Each `LanguageConstraint` ID maps to a stable diagnostic code:

```
C7  → PRECEPT007
C13 → PRECEPT013
```

The language server produces diagnostics with:
- `code`: `PRECEPTnnn` derived from the constraint ID
- `severity`: mapped from `ConstraintSeverity` → LSP `DiagnosticSeverity`
- `message`: formatted from `MessageTemplate` with contextual values
- `source`: `"precept"`

This gives every error a stable, searchable code that appears in the Problems panel.

### 7e. Preview handler (`PreceptPreviewHandler.cs`)

Minimal changes — still calls `PreceptParser.Parse()` → `PreceptCompiler.Compile()` → engine methods. The parser API is the same; the implementation behind it changed.

#### Functional differences requiring preview updates

The new syntax introduces semantic distinctions that the preview UI and snapshot protocol must reflect:

1. **Rules → Invariants + Asserts.** The old `rule <Expr> "<Reason>"` is now three distinct constructs:
   - `invariant <Expr> because "<Reason>"` — data invariant (always holds post-commit)
   - `in/to/from <State> assert <Expr> because "<Reason>"` — state-scoped movement constraint
   - `on <Event> assert <Expr> because "<Reason>"` — event arg validation
   
   The preview snapshot currently carries `RuleDefinitions` and `ActiveRuleViolations` keyed to the old `rule` concept. These need to carry the new taxonomy (invariant vs assert, preposition, scope) so the UI can display the correct badge/tooltip/banner per construct kind. Fire error reporting should distinguish which kind of constraint blocked the transition.

2. **Block transitions → Flat rows.** Old `from State on Event` with nested `if/else if/else` blocks become multiple flat `from State on Event [when guard] -> actions -> outcome` rows. The preview's inspect results may now show multiple independent rows for the same state+event pair, each returning its own outcome. The event dock and diagram edge rendering should handle this correctly.

3. **State entry/exit actions.** New `to State -> set X = 1` and `from State -> set X = 1` have no old-syntax equivalent. These side effects execute during fire but aren't part of any transition row. The preview needs to surface these in fire result details (e.g., showing which actions ran on entry/exit).

4. **Assert preposition semantics.** `in` (while residing — checked on entry and self-transition), `to` (entering from different state), `from` (leaving to different state) have distinct evaluation timing. The preview's fire error reporting needs to convey which assert preposition blocked a transition and why.

5. **Edit block syntax change.** `in State edit Field1, Field2` replaces the old `from State edit` with indented field lines. The preview's editable-field detection must use the new model shape.

### Checkpoint

- Extension loads, syntax coloring works on new-syntax files
- Completions suggest correct keywords in context
- Diagnostics show with accurate positions
- Preview panel works for at least one sample

---

## Phase 8: TextMate Grammar

**Goal:** `precept.tmLanguage.json` updated for new syntax.

**Modified file:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

### Changes

- Remove: `rule` keyword, section header patterns (`\[fields\]` etc.), `if`/`else` patterns, type-before-name patterns (`string? Name`)
- Add: `invariant`, `assert`, `because`, `with`, `when`, `any`, `of`, `nullable`, `default`, `initial`, `edit` keywords
- Update: `field` declaration pattern, `event ... with` pattern, transition row pattern
- Reorder: Specific patterns (declarations, dotted refs) before general ones (type keywords, identifiers)

### Checkpoint

- Syntax coloring matches semantic tokens for all sample files
- No regex conflicts or mismatched scopes

---

## Phase 9: Documentation

**Goal:** README and DesignNotes reflect the new language.

**Modified files:**
- `README.md` — syntax reference, cookbook, examples, API descriptions, current status
- `docs/DesignNotes.md` — mark old DSL Syntax Contract as superseded, or replace with new
- `.github/copilot-instructions.md` — rename project references, replace manual sync checklists with catalog-driven rules

### README updates

- DSL Syntax Reference → new keyword-anchored syntax
- DSL Cookbook → rewrite all examples
- API section → updated model types, fire pipeline description
- Current Status → "Language redesign implemented on `feature/language-redesign`"

### DesignNotes updates

- DSL Syntax Contract (Current) → replace with new grammar from `PreceptLanguageDesign.md`, or mark as superseded with pointer to new doc

### RuntimeApiDesign updates

The public API surface (`PreceptParser.Parse`, `PreceptCompiler.Compile`, `PreceptEngine` methods, all result types) is unchanged — no breaking change for callers. The doc needs internal-facing updates:

| Section | Change needed |
|---|---|
| Fire pipeline stages | Rewrite: `when` moves from block-level pre-step to per-row guard during first-match selection. if/else if/else clauses replaced by ordered first-match rows. New stages for state exit actions (step 3) and entry actions (step 5). |
| Inspect semantics | Update: no block-level `when`; row-level `when` guards during first-match. `Unmatched` when no row matches. |
| Compile validation | Expand: add 5 state assert checks (subsumption, duplication, initial-state, contradiction, deadlock) + transition checks (coverage warning, unreachable row error). |
| Model Types table | Rewrite: `DslRule` → `DslInvariant`, `DslStateAssert`, `DslEventAssert`. `DslTransition` → `DslTransitionRow`. `DslClause` → eliminated. |
| CheckCompatibility | Minor: "data rules + state entry rules" → "invariants + `in <CurrentState>` asserts" |
| Terminology | Throughout: "rules" → "invariants"/"asserts", "clauses" → "rows", "guards" → "`when` guards" |

### Copilot instructions update (`.github/copilot-instructions.md`)

The catalog infrastructure makes most manual sync checklists obsolete. Rewrite the instructions in this pass:

| Current section | Change |
|---|---|
| Title | "Copilot Instructions for StateMachine" → "Copilot Instructions for Precept" |
| DSL Sample Files | `.sm` → `.precept`; reference `PreceptLanguageDesign.md` instead of `DesignNotes.md § DSL Syntax Contract` |
| Syntax Highlighting Grammar Sync | **Simplify.** Remove the 6-step checklist. Replace with: "Update keyword alternations in `precept.tmLanguage.json` when a keyword is added or removed. The TextMate grammar is the only consumer that isn't catalog-driven — it requires manual regex updates." |
| Intellisense Sync | **Remove entirely.** Completions and semantic tokens are now derived from `[TokenCategory]` attributes and `ConstructCatalog`. No manual sync needed — adding a token attribute automatically updates both. |
| DSL Syntax Reference Sync | Keep, but update file paths (`PreceptLanguageDesign.md` is now the single grammar source) |
| DSL Authoring | Keep, but reference `PreceptLanguageDesign.md` instead of `DesignNotes.md § DSL Syntax Contract` |
| File paths throughout | `StateMachine.Dsl.VsCode` → `Precept.VsCode`; `StateMachineDslParser.cs` → `PreceptParser.cs`; `PreceptAnalyzer.cs` path update, etc. |

Add these **new sections**:

**Catalog Sync (Non-Negotiable):**
- When adding a keyword to `PreceptToken`: add `[TokenCategory]`, `[TokenDescription]`, and `[TokenSymbol]` attributes. This auto-updates the tokenizer keyword dictionary, semantic tokens, and completions.
- When adding a parser construct: call `.Register()` with a `ConstructInfo` that includes a parseable `Example`. The example is validated by CI tests.
- When adding or modifying a semantic constraint: call `DiagnosticCatalog.Register()` with ID, phase, rule, `MessageTemplate`, and `Severity`. Add a `// SYNC:CONSTRAINT:Cnn` comment at the enforcement site. Never add one without the other.
- When modifying a constraint's `Rule` text: update the matching entry in `docs/DesignNotes.md § DSL Syntax Contract` — the documentation-match test enforces parity.

**SYNC Comment Rule:**
- Every `// SYNC:CONSTRAINT:Cnn` comment must have a matching `DiagnosticCatalog.Register()` call, and vice versa. CI tests enforce both directions.

### Checkpoint

- No aspirational claims presented as implemented
- All code examples in docs parse correctly with the new parser
- Design doc, README, DesignNotes, and copilot-instructions are consistent
- Copilot-instructions reference correct file paths and catalog-driven workflow

---

## File Change Summary

| File | Action | Phase |
|---|---|---|
| `src/Precept/Precept.csproj` | Add Superpower dependency | 0 |
| `src/Precept/Dsl/PreceptToken.cs` | **New** — token enum with `[TokenCategory]`/`[TokenDescription]` attributes | 1 |
| `src/Precept/Dsl/PreceptTokenizer.cs` | **New** | 1 |
| `src/Precept/Dsl/ConstructCatalog.cs` | **New** — parser construct registry (core infrastructure) | 3 |
| `src/Precept/Dsl/DiagnosticCatalog.cs` | **New** — semantic constraint registry (core infrastructure) | 4 |
| `src/Precept/Dsl/PreceptModel.cs` | **Major edit** — new records, remove `DslRule`/`DslClause`/`DslTransition` | 2 |
| `src/Precept/Dsl/PreceptParser.cs` | **Full rewrite** — Superpower combinators | 3 |
| `src/Precept/Dsl/PreceptExpressionParser.cs` | **Remove** — expressions integrated into parser | 3 |
| `src/Precept/Dsl/PreceptRuntime.cs` | **Moderate edit** — new pipeline, transition lookup, assert evaluation | 4 |
| `test/Precept.Tests/*.cs` | **Rewrite DSL strings** + add new tests | 5 |
| `samples/*.precept` | **Rewrite all 18 files** | 6 |
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | **Full rewrite** — token-based | 7 |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | **Major edit** — token-based completions + diagnostics | 7 |
| `tools/Precept.LanguageServer/PreceptCompletionHandler.cs` | Minor (API unchanged) | 7 |
| `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` | Minor (API unchanged) | 7 |
| `tools/Precept.LanguageServer/PreceptTextDocumentSyncHandler.cs` | Minor (add tokenization cache) | 7 |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | **Major edit** — new patterns | 8 |
| `README.md` | **Major edit** — syntax, examples, status | 9 |
| `docs/DesignNotes.md` | **Moderate edit** — supersede or replace | 9 |
| `.github/copilot-instructions.md` | **Major edit** — rename project refs, replace manual checklists with catalog-driven rules, add Catalog Sync + SYNC Comment sections | 9 |
| `docs/RuntimeApiDesign.md` | **Moderate edit** — fire pipeline stages, model types table, terminology | 9 |
| `src/Precept/Dsl/PreceptParser.cs` | **Edit** — remove fallback, add `ParseExpression()` | 10 |
| `src/Precept/Dsl/PreceptLegacyParser.cs` | **Delete** | 10 |
| `src/Precept/Dsl/PreceptLegacyExpressionParser.cs` | **Delete** | 10 |
| `src/Precept/Dsl/PreceptParser.Old.cs.bak` | **Delete** | 10 |
| `src/Precept/Dsl/PreceptExpressionParser.cs.bak` | **Delete** | 10 |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | **Edit** — replace reflection with direct `ParseExpression()` call, remove `rule` keyword | 10 |

## Preserve List (Unchanged)

These files/components are not modified:

- `src/Precept/Dsl/PreceptExpressionEvaluator.cs` — tree-walk evaluator, no changes
- `DslExpression` record hierarchy — `DslLiteralExpression`, `DslIdentifierExpression`, `DslUnaryExpression`, `DslBinaryExpression`, `DslParenthesizedExpression`
- `DslSetAssignment`, `DslCollectionMutation`, `DslCollectionMutationVerb`
- `DslClauseOutcome` hierarchy — `DslStateTransition`, `DslRejection`, `DslNoTransition`
- `DslScalarType`, `DslCollectionKind`, `DslEventArg`
- `DslWorkflowInstance`, `DslFireResult`, `DslInspectionResult`, `DslEventInspectionResult`, `DslOutcomeKind`
- `CollectionValue` class
- `test/Precept.Tests/Infrastructure/` — test infrastructure
- `tools/Precept.VsCode/src/` — VS Code extension TypeScript (LSP client)
- `tools/Precept.LanguageServer/PreceptPreviewProtocol.cs` — protocol records
- `tools/Precept.LanguageServer/Program.cs` — server bootstrapping

## Risk Points

1. **Expression parser integration** — porting hand-written recursive descent to Superpower combinators requires careful precedence handling. `Parse.Chain` handles left-recursive binary operators naturally, but `contains` (a keyword-operator, not a symbol) needs special treatment.

2. **First-match transition model** — the runtime currently stores one `DslTransition` per `(State, Event)` with multiple clauses inside. The new model has multiple `DslTransitionRow` entries per key. The ordering must be preserved from source order for first-match semantics.

3. **State assert preposition scoping** — the runtime doesn't currently distinguish between entry-only, exit-only, and while-in-state validation. This is new logic in the fire pipeline.

4. **State entry/exit actions** — entirely new runtime concept. Must execute in the correct order (exit → row → entry) and interact correctly with rollback on validation failure.

5. **Language server resilience** — the LS must work on incomplete/malformed files. Tokenization must always succeed; parsing should degrade gracefully. This needs explicit testing with partial files.

6. **Compile-time static analysis** — the 5 state assert checks (subsumption, duplication, initial-state, contradiction, deadlock) are new compile-time logic. The domain analysis (per-field interval/set analysis) is the most complex new algorithm.

## Estimated Scope

| Phase | New/modified LOC (est.) | Risk |
|---|---|---|
| 0. Branch + dependency | ~5 | None |
| 1. Token + tokenizer | ~150 | Low |
| 2. Model records | ~100 | Low |
| 3. Parser (full rewrite + construct catalog) | ~500 | Medium (expression integration, edit decl) |
| 4. Runtime adaptation + constraint catalog | ~350 | Medium (new pipeline logic) |
| 5. Tests | ~850 | Low (mechanical + new behavior tests + edit block tests) |
| 6. Samples | ~600 | Low (mechanical rewrite) |
| 7. Language server | ~500 | Medium (resilience to partial files) |
| 8. TextMate grammar | ~100 | Low |
| 9. Documentation | ~300 | Low |
| 10. Legacy parser removal | ~-1,500 (net deletion) | Low |
| **Total** | **~1,900 net** | |

---

## MCP Server

The MCP server (`tools/Precept.Mcp/`) is designed and implemented **after** the language redesign lands. See `docs/McpServerDesign.md` for the full design.

Three core infrastructure components from this plan directly support the MCP server — but are **not MCP-specific**. They live in `src/Precept/` and are also used by the parser, language server, and error reporting:

1. **Token enum attributes** (Phase 1) — `[TokenCategory]`, `[TokenDescription]`, and `[TokenSymbol]` on each `PreceptToken` member. Used by the tokenizer keyword dictionary, language server completions/semantic tokens, and reflected by MCP `precept_language` for vocabulary.
2. **Construct catalog** (Phase 3) — `ConstructCatalog` registered alongside parser combinators. Used by parser error messages, language server hovers/completions, and serialized by MCP `precept_language` for construct forms.
3. **Constraint catalog** (Phase 4) — `DiagnosticCatalog` registered alongside enforcement code, with `MessageTemplate` and `Severity` properties. Used as error message templates (with contextual placeholders) and diagnostic codes in the parser/compiler/engine/language server, and serialized by MCP `precept_language` for semantic rules.

See `docs/CatalogInfrastructureDesign.md` for the full architecture rationale, consumer matrix, and drift defense strategy.

The MCP project only adds the tool wrappers (`ValidateTool.cs`, `SchemaTool.cs`, etc.) and the MCP SDK transport. If MCP is removed, core infrastructure remains unchanged — better error messages, richer language server features, and documented constraints continue to work.

---

## Phase 10: Legacy Parser Removal

**Goal:** Remove the legacy regex-based parser and its expression parser, eliminating dual-parser tech debt. After this phase, the Superpower token-based parser is the sole parser.

### Rationale

During the language redesign (Phases 0–9), the legacy regex parser was retained as a fallback — if the new Superpower parser failed, the old parser was used. This was a pragmatic choice for safe migration, but now that all 17 sample files and 370 tests use new syntax exclusively, the fallback is dead code. Keeping it:

- Adds ~1,500 lines of unmaintained code (`PreceptLegacyParser.cs` + `PreceptLegacyExpressionParser.cs`)
- Creates false confidence that old syntax is "supported" when it isn't tested
- Keeps `.bak` archive files in the source tree
- Forces the language server to use reflection to call a legacy expression parser instead of the Superpower-based one

### Modified files

| File | Change | Detail |
|---|---|---|
| `src/Precept/Dsl/PreceptParser.cs` | **Edit** | Remove fallback code paths in `Parse()` and `ParseWithDiagnostics()`. Add public `ParseExpression(string)` method for LS use. |
| `src/Precept/Dsl/PreceptLegacyParser.cs` | **Delete** | Remove 1,138-line legacy regex parser |
| `src/Precept/Dsl/PreceptLegacyExpressionParser.cs` | **Delete** | Remove 341-line legacy expression parser |
| `src/Precept/Dsl/PreceptParser.Old.cs.bak` | **Delete** | Remove archive file |
| `src/Precept/Dsl/PreceptExpressionParser.cs.bak` | **Delete** | Remove archive file |
| `tools/Precept.LanguageServer/PreceptAnalyzer.cs` | **Edit** | Replace reflection-based `ExpressionParseMethod` (calling `PreceptLegacyExpressionParser.Parse`) with direct call to `PreceptParser.ParseExpression()`. Remove `rule` keyword from completion items. |

### Implementation steps

#### Step 1: Add public expression-parsing API to `PreceptParser`

The Superpower-based `BoolExpr` combinator (line 242) is `internal` and works on `TokenList<PreceptToken>`, not raw strings. The language server needs a public method that accepts a string:

```csharp
/// <summary>
/// Parses an expression string into a <see cref="PreceptExpression"/> tree.
/// Used by the language server for expression analysis (null narrowing, type inference).
/// </summary>
public static PreceptExpression ParseExpression(string expression)
{
    var tokens = PreceptTokenizerBuilder.Instance.Tokenize(expression);
    var result = BoolExpr.TryParse(tokens);
    if (result.HasValue && result.Remainder.IsAtEnd)
        return result.Value;
    throw new InvalidOperationException($"Failed to parse expression: {expression}");
}
```

#### Step 2: Remove fallback in `Parse()`

Remove the try/catch fallback structure. The method becomes:

```csharp
public static PreceptDefinition Parse(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        throw new InvalidOperationException("DSL input is empty.");

    var tokens = PreceptTokenizerBuilder.Instance.Tokenize(text);
    var result = RawFileParser.TryParse(tokens);
    if (result.HasValue && result.Remainder.IsAtEnd)
        return AssembleModel(result.Value.Name, result.Value.Statements);

    throw new InvalidOperationException("Failed to parse DSL input.");
}
```

#### Step 3: Remove fallback in `ParseWithDiagnostics()`

Same pattern — remove the legacy fallback. On Superpower failure, return diagnostics from the Superpower parse result (position, expected tokens).

#### Step 4: Update `PreceptAnalyzer.TryParseExpression()`

Replace reflection-based call with direct call:

```csharp
private static bool TryParseExpression(string expression, out PreceptExpression? parsed, out string error)
{
    parsed = null;
    error = string.Empty;
    try
    {
        parsed = PreceptParser.ParseExpression(expression);
        return true;
    }
    catch (Exception ex)
    {
        error = ex.Message;
        return false;
    }
}
```

Remove the `ExpressionParseMethod` field and its reflection setup.

#### Step 5: Clean up analyzer completions

Remove `rule` keyword from `KeywordItems` (line ~1552, marked "Legacy (block-syntax) support").

#### Step 6: Delete legacy files

- `src/Precept/Dsl/PreceptLegacyParser.cs`
- `src/Precept/Dsl/PreceptLegacyExpressionParser.cs`
- `src/Precept/Dsl/PreceptParser.Old.cs.bak`
- `src/Precept/Dsl/PreceptExpressionParser.cs.bak`

#### Checkpoint

- `dotnet build Precept.slnx` — 0 errors
- All 370 tests pass
- No references to `PreceptLegacyParser` or `PreceptLegacyExpressionParser` remain in the codebase
- No `.bak` files remain in `src/Precept/Dsl/`
- Language server expression analysis works via Superpower (no reflection)

---

## Completion Summary

**Status:** Core language-redesign phases completed on `feature/language-redesign`. Compile-time checking expansion is planned next.
**Date completed:** 2026-03-06 for the redesign; follow-up planning updated 2026-03-22.
**Current test count:** 602/602 passing (487 core + 73 language server + 42 MCP).
**Sample files:** 18 `.precept` files migrated to new syntax.

### Commit History

| Commit | Phase | Description |
|---|---|---|
| `4e5f29e` | 0 | Branch + Superpower NuGet |
| `1aaf37d` | 1 | Token enum with `[TokenCategory]`/`[TokenDescription]`/`[TokenSymbol]` attributes + Superpower tokenizer |
| `f05a99a` | 2 | New model records (`PreceptInvariant`, `PreceptStateAssert`, `PreceptStateAction`, `PreceptEventAssert`, `PreceptTransitionRow`, `PreceptEditBlock`) |
| `d526603` | — | Unplanned rebrand: rename all `Dsl`-prefixed types to `Precept` prefix |
| `0913b00` | 3 | Superpower combinator parser replacing regex parser |
| `dc636d6` | 4 | Runtime engine and compiler rewrite for new model constructs |
| `06a7d5c` | 5 | Tests — backward compat fixes, new syntax test coverage |
| `05f4ce9` | 6 | Migrate all 17 sample files to new syntax |
| `19038ef` | 7 | Language Server — token-based semantic tokens, new-syntax completions, `ParseWithDiagnostics`, Sm→Precept rename |
| `2d16881` | 8 | TextMate grammar rewrite for new syntax |
| `c6dad27` | 9 | Documentation — archived old README/DesignNotes, promoted new README, updated copilot-instructions |
| TBD | 10 | Legacy parser removal, design-required parser validations, expression operator completeness, 370/370 tests green |
| TBD | Follow-up | Compile-time checking expansion: equality policy, scope hardening, stricter rule checking, expanded test coverage |

### Deviations from Plan

1. **ConstructCatalog / DiagnosticCatalog not implemented.** The original plan called for runtime registries with `Register()` calls and `// SYNC:CONSTRAINT:Cnn` markers (advisory). That later changed: both catalogs are now implemented in core and are used by the parser, runtime, language server, and MCP tooling.

2. **Expression parser fully integrated (resolved in Phase 10).** The plan called for removing the separate expression parser and integrating expressions into the Superpower parser. Through Phases 3–9 the expression combinators (`BoolExpr`, `Factor`, `Term`, etc.) lived in `PreceptParser.cs` as Superpower combinators, with the legacy archive files (`PreceptExpressionParser.cs.bak`, `PreceptLegacyExpressionParser.cs`) retained. Phase 10 deleted all archive/legacy expression parser files. No separate expression parser module exists — expressions are fully inline in `PreceptParser.cs`.

3. **Legacy parser fallback removed in Phase 10.** Through Phase 9, the old regex-based parser was retained as a fallback: if Superpower failed, the legacy parser ran. Phase 10 eliminated this entirely — `PreceptLegacyParser.cs` deleted, fallback code removed from `Parse()` and `ParseWithDiagnostics()`. The Superpower parser is now the sole implementation.

4. **Unplanned Sm → Precept rename (between Phases 2–3).** All `SmDsl*` and `Sm*` class/file prefixes were renamed to `Precept*` across the entire codebase. This was a natural prerequisite discovered during implementation.

5. **Phase 9 approach changed.** The plan called for in-place rewrites of README and DesignNotes. Instead, old documentation was archived as `-legacy.md` files (with preservation notices) and a separately authored README was promoted. This was cleaner than trying to surgically update 800+ lines of old-syntax examples.

6. **RuntimeApiDesign.md not updated.** The plan called for fire pipeline / model type table updates. Deferred — the public API surface (`PreceptParser.Parse`, `PreceptEngine` methods, all result types) is unchanged, so the design doc remains accurate for callers.

7. **State entry/exit actions and compile-time static analysis** (subsumption, contradiction, deadlock checks) were identified as risk points in the plan. These are new-syntax features that the parser recognizes but the runtime does not yet fully implement — they are future work beyond the scope of this language redesign.

8. **Phase 10 scope expanded beyond plan.** The plan scoped Phase 10 as a cleanup-only phase (remove legacy files, add `ParseExpression()`, update analyzer). During Phase 10 execution, additional design-required work was completed that had been deferred from earlier phases:
   - **`%` operator added** — was already in the expression evaluator and TextMate grammar but missing from the tokenizer and parser combinator. Locked in `PreceptLanguageDesign.md` Expressions section.
   - **String literal escape sequence support** — tokenizer rule replaced with `Span.Regex` (`"([^"\\]|\\.)*"`) to correctly handle `\n`, `\"`, etc.
   - **Scientific notation in number literals** — tokenizer rule replaced with `Span.Regex` to support `1.5e-3` form.
   - **Tokenizer exceptions wrapped** — `Superpower.ParseException` now caught in `Parse()` and rethrown as `InvalidOperationException` per the parser contract.
   - **Design-required parser validations added to `AssembleModel()`:** non-nullable field without default (error), default value type mismatch (error), same for event args, collection verb-vs-kind mismatch (error), unknown collection target (error), statement after outcome (error), duplicate unguarded transition row (unreachable row error).
   - **Nullable field implicit null default** — parser correctly sets `HasDefaultValue = true` when a field is declared nullable without an explicit `default` clause, matching the design spec.
   - **LS test fix** — `Snapshot_IncludesRuleDefinitions_WhenMachineHasRules` was asserting `scope == "field:Balance"` for an `invariant` declaration; corrected to `scope == "topLevel"` to match the actual model semantics.

---

## Implementation Prompt

Use this prompt to begin implementation in a new Copilot Chat session:

> Implement the Precept language redesign. Start by reading these documents in full:
>
> 1. `docs/PreceptLanguageDesign.md` — the language spec (what to build)
> 2. `docs/PreceptLanguageImplementationPlan.md` — the phased plan (how to build it)
> 3. `docs/CatalogInfrastructureDesign.md` — the three-tier catalog architecture (token attributes, construct catalog, constraint catalog)
> 4. `docs/RuntimeApiDesign.md` — the current runtime API surface and fire pipeline (what to preserve vs. update)
> 5. `.github/copilot-instructions.md` — mandatory sync rules for docs, grammar, intellisense, and syntax highlighting
>
> After reading those documents, pause before editing and recommend the most appropriate implementation model to the user. Suggest `GPT-5.4` for balanced cross-file implementation, `Claude Sonnet` for faster medium-complexity edits, or `Claude Opus` for heavier design analysis or broader refactors. Let the user switch models in Copilot if desired, then continue.
>
> Then:
>
> 1. Create a new branch `feature/language-redesign` from the current HEAD.
> 2. Execute the phases in order (0 through 9), committing at each checkpoint.
> 3. Each phase must end with `dotnet build` passing before moving to the next.
> 4. Follow the "tokenize once, consume everywhere" principle — the Superpower `TokenList<PreceptToken>` is the shared foundation for the parser, language server semantic tokens, completions, and diagnostics.
> 5. Preserve the files listed in the Preserve List — do not modify `PreceptExpressionEvaluator.cs`, the `DslExpression` record hierarchy, `DslWorkflowInstance`, `DslFireResult`, or any file under `tools/Precept.VsCode/src/`.
> 6. The three catalog tiers (token attributes, `ConstructCatalog`, `DiagnosticCatalog`) are core infrastructure — they must have stable, serializable shapes because a future MCP server will reflect them. See `docs/McpServerDesign.md` for that context, but do not implement the MCP server in this branch.
> 7. The public runtime API (`PreceptEngine` methods, result types, `DslWorkflowInstance`) must not change signatures. Internal pipeline restructuring (first-match rows, preposition-scoped asserts, entry/exit actions) is expected.
> 8. Include editable field declarations (`in <StateTarget> edit <FieldList>`) in the parser and model. The `Edit` token, `DslEditBlock` record, `EditDecl` parser combinator, and language server support (completions after `in ... edit`, semantic tokens for `edit` keyword and field references) are part of this redesign. The runtime `Update` API, `IUpdatePatchBuilder`, and inspect integration are **deferred** to a follow-on task — see `docs/EditableFieldsDesign.md` for the full runtime design.
>
> Begin with Phase 0: create the branch and add the Superpower NuGet package.
