## Core Context

- Owns test discipline, validation strategy, and compile/runtime verification for the DSL and surrounding tools.
- Samples and hero candidates should be validated against the real compiler/runtime before the team treats them as canonical.
- Coverage work should record meaningful gaps, not just raw counts; behavior claims need executable proof.

## Recent Updates

### 2026-04-08 - Slice 7: stateless precept tests (issue #22)
- Wrote 127 new tests across 3 files covering all stateless precept behaviors: parser (IsStateless, root edit all/fields, C12/C13/C49/C55 diagnostics), runtime (CreateInstance null state, Fire→Undefined, Inspect→null currentState, Update editability), MCP tools (CompileTool IsStateless/InitialState/StateCount, InspectTool/FireTool/UpdateTool with null currentState), and LS completions (root edit 'all' and field names).
- Fixed 8 nullable warnings in pre-existing test files (InitialState!, TransitionRows!, col!).
- Fixed stale CompileToolTests.DeadEndState_HintDiagnostic — C50 severity is Warning not Hint per squad decision.
- Final baseline: 754 passing, 0 failing (612 core / 55 mcp / 87 ls). Build: 0 warnings, 0 errors.
- Key learning: root edit blocks have State == null; "all" is stored as FieldNames sentinel ["all"] expanded at engine construction. GetEditableFieldNames(null) is the internal API. Stateless CreateInstance(state, ...) throws ArgumentException with "stateless" in the message.

### 2026-04-10 - Hero candidate DSL validation
- Compile-validated the hero candidate set with precept_compile and separated valid examples from advisory failures.
- Key learning: a hero sample is not real until the engine accepts it without caveat.

### 2026-04-10 - Test refresh coverage analysis
- Reviewed current test/project coverage and documented where follow-up strengthening would matter most.
- Key learning: coverage summaries are useful only when they identify the specific behavioral lanes still at risk.
