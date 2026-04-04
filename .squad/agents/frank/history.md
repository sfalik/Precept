# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible. Declarative `.precept` files compile to executable runtime contracts.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions
- **Components:** `src/Precept/` (core DSL pipeline), `tools/Precept.LanguageServer/`, `tools/Precept.Mcp/`, `tools/Precept.VsCode/`, `tools/Precept.Plugin/`
- **Key docs:** `docs/PreceptLanguageDesign.md` (DSL spec), `docs/RuntimeApiDesign.md` (public API), `docs/RulesDesign.md` (constraints), `docs/McpServerDesign.md` (MCP tools)
- **Distribution:** NuGet, VS Code Marketplace, Claude Marketplace
- **Created:** 2026-04-04

## Learnings

### 2026-04-06 — Architectural Knowledge Refresh

#### I. Component Map (Ownership)

**Core Runtime** (`src/Precept/Dsl/`)
- **PreceptParser.cs** — Tokenizes and parses `.precept` DSL text into `PreceptDefinition` (immutable model). Uses Superpower parser combinators. Entry points: `Parse(text)` (throws), `ParseWithDiagnostics(text)` (returns diagnostics list). Produces structured parse tree with zero semantic validation — that's the compiler's job.
- **PreceptModel.cs** — Complete AST data types: `PreceptDefinition`, `PreceptField`, `PreceptCollectionField`, `PreceptState`, `PreceptEvent`, `PreceptEventArg`, `PreceptInvariant`, `StateAssertion`, `EventAssertion`, `PreceptTransitionRow`, `PreceptEditBlock`, `PreceptExpression` (and AST node types). Records, all immutable. This is the bridge between parser output and compiler input.
- **PreceptRuntime.cs** — The three-leg public API: `PreceptCompiler` (static methods `Compile()`, `Validate()`), `PreceptEngine` (immutable compiled engine, methods: `CreateInstance()`, `Inspect()`, `Fire()`, `Update()`, `CheckCompatibility()`, `CoerceEventArguments()`), `PreceptInstance` (value record: WorkflowName, CurrentState, InstanceData, UpdatedAt, LastEvent).
- **PreceptTypeChecker.cs** — Compile-time type validation. Validates field/arg/expression types, null-flow narrowing, operator compatibility, `when` guard and set expression RHS types. Produces `TypeContext` consumed by the compiler. Uses interval/set analysis for per-field domain analysis.
- **PreceptExpressionEvaluator.cs** — Expression AST evaluator. Interprets `PreceptExpression` trees at runtime (during `Fire` and `Inspect`). Handles arithmetic, logical, comparison, and membership operators with deterministic semantics.
- **PreceptTokenizer.cs** — Lexical analyzer. Builds keyword dictionary from `PreceptToken` enum's `[TokenSymbol]` attributes at startup. Recognizes keywords, operators, punctuation, literals, identifiers, comments. No ambiguity — tokenizer outputs `PreceptToken` enum values.
- **PreceptToken.cs** — Enum of all language tokens with `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes. Reflection-driven: keyword dict, semantic token mapping, completions grouping, and MCP vocabulary are all derived from these attributes.
- **PreceptAnalysis.cs** — Graph-level compile-time analysis. Computes reachability (BFS from initial state), detects orphaned events (never referenced in rows), dead-end states (no outgoing transitions or all reject), reject-only event/state pairs, and empty precepts. Registered constraints: C48–C53 (warnings/hints for structural concerns).
- **ConstructCatalog.cs** — Parser combinators self-register with their syntax form, context, description, and parseable example. Used by language server hovers, completions, and MCP `precept_language` tool. One catalog per component — synchronized by the parser registering each combinator.
- **DiagnosticCatalog.cs** — Compile-time and parsing constraints register themselves (ID, phase, rule description, message template, severity). Used by parser/compiler error messages and language server diagnostics. No hand-maintained lists — all enforcement code has `// SYNC:CONSTRAINT:Cnn` comments.
- **ConstraintViolation.cs** — Runtime constraint violation model. Carries `Message` (reason string), `ConstraintSource` (which rule failed, with expression text and source line), `ConstraintTarget[]` (which fields/args/events/states it references). Replaced the old flat `IReadOnlyList<string> Reasons`.

**Language Server** (`tools/Precept.LanguageServer/`)
- Exposes LSP (Language Server Protocol): diagnostics, hovers, completions, go-to-definition, semantic tokens, code actions.
- **PreceptAnalyzer.cs** — Consumer of `DiagnosticCatalog` and `ConstructCatalog`. Implements expression type checking in `when` guards and `set` assignments. Produces language server diagnostics with codes (e.g., PRECEPT046, PRECEPT047).
- **PreceptSemanticTokensHandler.cs** — Token coloring. Reflects `[TokenCategory]` attributes to map tokens to VS Code semantic token types. Highlights keywords, types, operators, identifiers differently.
- **PreceptPreviewHandler.cs** — Handles preview protocol messages from the webview (inspect, fire, update actions). Applies patches to working copies, calls `engine.Inspect()` / `engine.Fire()` / `engine.Update()`, translates bidirectional `ConstraintViolation` object graphs into index-based protocol format, returns snapshots with violations and editable fields metadata.

**MCP Server** (`tools/Precept.Mcp/`)
- Wraps core runtime in MCP protocol for Copilot and other LLMs. Five tools:
  - `precept_language` — Returns DSL vocabulary (keywords, operators, types, precepts, constructs, constraints) as structured JSON. Derived at startup from token attributes and catalogs — no drift.
  - `precept_compile` — Parses and compiles `.precept` text, returns typed definition + diagnostics. Single entry point for compile-time validation.
  - `precept_inspect` — Simulates firing an event (with optional args) on an instance. Non-mutating. Returns outcome, violations, editable fields, required args.
  - `precept_fire` — Executes an event on an instance. Mutating. Returns new instance + violations or rollback reason.
  - `precept_update` — Applies a direct field patch to an instance. Mutating. Returns new instance or violation list (with field attribution).

**VS Code Extension** (`tools/Precept.VsCode/`)
- TypeScript. Syntax highlighting, preview webview, language server client, extension commands.
- **syntaxes/precept.tmLanguage.json** — TextMate grammar (regex-based). Must be synced with parser keywords and operators. Pattern order matters: declarations before keywords, keywords before catch-alls. **Non-negotiable to keep in sync — copilot-instructions.md has explicit rules.**
- **inspector-preview.html** — Webview UI. Renders state machine snapshots (current state, fields, events, violations). Edit mode for direct field editing with per-keystroke validation via `Inspect(patch)`. Renders collection types with appropriate visualizations (set `{ a, b }`, queue `[ a, b →]`, stack `[ top | 2 | 3 ]`).

**Copilot Plugin** (`tools/Precept.Plugin/`)
- `.claude-plugin/plugin.json` — Claude plugin manifest. Lists MCP tools and agent.
- `precept-author.agent.md` — Persona + tool restrictions for Copilot's agent builder. Defines the "Precept Author" agent that helps users write `.precept` definitions.
- Authoring skill + debugging skill (markdown, agentskills.io spec-compliant).

#### II. API Surface (Public Contracts)

**Parsing:** `PreceptParser.Parse(text)` → `PreceptDefinition` or throws. `ParseWithDiagnostics(text)` → `(PreceptDefinition?, IReadOnlyList<ParseDiagnostic>)`.

**Compilation:** `PreceptCompiler.Compile(definition)` → `PreceptEngine`. Also: `Validate(definition)` → `ValidationResult` (structured diagnostics without building engine, useful for tooling).

**Engine (Immutable):**
- `CreateInstance(initialState?, data?)` — New instance with default or override data. Merges caller-supplied data with declared defaults. Collection fields must be supplied as `IEnumerable`, coerced to inner type.
- `Inspect(instance)` — Full snapshot: current state, instance data, per-event inspection results, editable fields metadata.
- `Inspect(instance, eventName, eventArgs?)` — Single event: outcome, target state, violations, required args for completeness.
- `Inspect(instance, Action<IUpdatePatchBuilder> patch)` — Hypothetical patch: applies patch to working copy, runs validation, returns snapshot with violations **without committing**.
- `Fire(instance, eventName, eventArgs?)` — Execute event. Returns new instance or violations (with rollback).
- `Update(instance, Action<IUpdatePatchBuilder> patch)` — Direct field edit. Type check, editability check, constraint evaluation, atomic commit.
- `CheckCompatibility(instance)` — Validate externally-loaded instance against schema and rules. Returns `(IsCompatible, Reason?)`.
- `CoerceEventArguments(eventName, args)` — JSON deserialization helper. Unwraps `JsonElement`, coerces to declared types.

**Result Types:**
- `FireResult(TransitionOutcome, PreviousState, EventName, NewState, Violations, UpdatedInstance)` — Outcome: `Transition`, `NoTransition`, `Rejected` (author's explicit reject), `ConstraintFailure` (rule violation), `Unmatched` (guards failed), `Undefined` (no rows).
- `UpdateResult(UpdateOutcome, Violations, UpdatedInstance)` — Outcome: `Update`, `ConstraintFailure`, `UneditableField`, `InvalidInput`.
- `EventInspectionResult(Outcome, CurrentState, EventName, TargetState, RequiredEventArgumentKeys, Violations)`.
- `InspectionResult(CurrentState, InstanceData, Events[], EditableFields?)`.
- `PreceptEditableFieldInfo(FieldName, FieldType, IsNullable, CurrentValue, Violation?)` — FieldType is composite string like `"set<string>"`.
- `ConstraintViolation(Message, ConstraintSource, ConstraintTarget[])` — Structured feedback with field/arg attribution.

**Instance (Immutable Record):**
```csharp
PreceptInstance(
    string WorkflowName,
    string CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData)
```
InstanceData uses clean keys: scalar fields → `object?`, collection fields → `List<object>` (no `__collection__` prefixes).

#### III. Compilation Pipeline

**Entry:** `.precept` text → `PreceptParser.Parse(text)` → `PreceptDefinition` (immutable tree, zero semantic validation).

**Compile Steps** (in `PreceptCompiler.Compile(definition)`):
1. **Type Check** — `PreceptTypeChecker` validates expressions in `when` guards, `set` assignments, invariants, asserts against declared field/arg types. Performs null-flow narrowing. Emits diagnostics (C38–C45).
2. **Reference Validation** — All state/event/field names referenced in transitions/asserts exist. Cross-field forward-reference checks on top-level rules. Emits diagnostics.
3. **Default Value Validation** — Evaluates invariants, field rules, state entry rules against declared defaults at compile time. Detects contradictions early.
4. **Compile-time Rule Evaluation** — Validates literal `set` assignments against field rules. Evaluates collection rules against known-empty initial state.
5. **Transition Structure Analysis** — Detects unreachable rows (row after unguarded row for same (state, event)), identical-guard duplicates (C47), missing outcomes (C10), incomplete `from any` expansions.
6. **Graph Analysis** — Computes reachability from initial state, detects orphaned events (C49), dead-end states (C50), reject-only pairs (C51), events never succeeding (C52), empty preceptsс (C53).
7. **Construct Example Validation** — Every example in `ConstructCatalog` parses successfully (drift-defense test).
8. **Build Engine** — If no error-severity diagnostics, construct `PreceptEngine` with precomputed editable field maps, expression subject tables (for structured violations), etc.

**Result:** `PreceptEngine` (immutable) or `ValidationResult` (diagnostics + no engine if errors exist).

**Key principle:** Compilation is a pure function. All validation reports as diagnostics, never throw-based (except on unrecoverable parser failures). The engine guarantees soundness — if it compiles, every `Fire`/`Inspect` call with valid inputs produces consistent results.

#### IV. Runtime Execution Model (Fire → Inspect → Update Pipeline)

**`Fire` (Mutating Event Execution):**
1. **Compatibility check** — Instance's WorkflowName, CurrentState, InstanceData match engine schema.
2. **Event arg validation** — Supplied args match declared types. Unknown keys rejected.
3. **Event assertions** — `on Event assert` (arg-only scope). Pre-transition. Violations = `Rejected`.
4. **`when` guard evaluation** — Ordered guarded rows, first match wins. Fields + event args visible (dotted form: `EventName.ArgName`). Narrowing carries to matched row's mutations.
5. **Exit actions** (`from SourceState ->`) — Automatic mutations when leaving source state.
6. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — Executed on working copy. Read-your-writes semantics.
7. **Entry actions** (`to TargetState ->`) — Automatic mutations when entering target state. Skipped for `no transition`.
8. **Validation** — Invariants, field rules, state asserts (`in`, `to`, `from`), top-level rules evaluated against post-mutation data. Collect all violations. If any fail: **full rollback**, outcome = `ConstraintFailure`.
9. **Commit** — Working copy becomes live instance data if all checks pass.

**`Inspect` (Non-mutating Simulation):**
- Same pipeline as `Fire` but operates on working copy only. No actual commit. Caller can safely preview "what would happen if...". Returns outcome + violations + required args (if event args incomplete).
- Used by language server and preview UI for per-keystroke validation.

**`Update` (Direct Field Edit):**
1. **Editability check** — Each patched field must be in `in <CurrentState> edit` list.
2. **Type check** — Supplied values match field types.
3. **Atomic mutation** — Apply patch ops to working copy in order. Conflict detection at build time (no duplicate Set, no Replace + granular op mix, etc.).
4. **Rule evaluation** — Invariants and `in <CurrentState>` asserts. Collect violations.
5. **Commit or rollback** — Same as `Fire`.

**Key semantics:**
- **Deterministic:** Same inputs always produce same outputs. No hidden state, no side effects.
- **Atomic:** All mutations succeed or all roll back. No partial application.
- **Comprehensive violation reporting:** All violated constraints reported at once, not first-fail short-circuit.
- **Structured violations:** Runtime always returns `ConstraintViolation` with targets (which fields/args/events were implicated). No reverse-mapping through mutations — violation targets are expression-referenced subjects.

#### V. Design Decisions (Locked)

**Language:**
- **No indentation-based structure** — Flat keyword-anchored statements. Parser sees keyword, knows statement type.
- **Minimal ceremony** — No colons, semicolons, braces. `because` is the sentinel between expressions and reasons.
- **Deterministic first-match routing** — Transition rows evaluated top-to-bottom; first matching row wins. No catch-all required. All rows for same (state, event) must be logically consistent (identical-guard duplicates are errors).
- **`from`/`to`/`in`/`on` prepositions** have stable meaning across all contexts. `from X on Y` is "leaving X, when Y fires," not "source state X, event Y."
- **Event args in transition rows require dotted form** (`EventName.ArgName`). Bare arg names only valid in event asserts. Prevents collision with field names.
- **`when` guards are routing, not constraints.** They don't produce violations if false — the row just doesn't match. Constraints are invariants, assertions, and explicit rejects.
- **Edit declarations (`in State edit Fields`)** are additive across declarations and independent from events. Direct field mutation path, not a lifecycle action.

**Runtime:**
- **Outcome enums are tense-neutral** (`Transition`, `NoTransition`, not `Transitioned`/`Accepted`) so they work in both Fire (past: "outcome was X") and Inspect (predictive: "would be X").
- **`Rejected` vs `ConstraintFailure`** — Author's explicit `reject` → `Rejected`. Rule violation post-mutation → `ConstraintFailure`.
- **Violations always include scope** — Every constraint includes its scope target (`Event(name)`, `State(name, Anchor)`, `Definition()`). Consumers can infer "this is about the workflow" vs "this is about a specific state."
- **No reverse-mapping through mutations** — If an invariant fails post-`set Balance = ...`, the violation targets `Field("Balance")`, not the expression that changed it. Consumers decide inline vs banner rendering based on their own inputs.
- **Three-step compilation** — Pure pipeline: parse → define → validate → engine. Separation of concerns.
- **Type checking is compile-time-first** — Invalid expressions rejected before runtime. Deterministic semantics and tooling reliability depend on this.

#### VI. Drift Defense (Critical for Maintenance)

Three layers prevent catalog/grammar/completions from going stale:

1. **SYNC Comments** — Every enforcement point marked `// SYNC:CONSTRAINT:Cnn`. Copilot sees these when editing code and knows to update catalogs. Pattern documented in `.github/copilot-instructions.md`.
2. **Tests** — Construct examples must parse (test fails if grammar changes without updating examples). Constraint triggers must be tested. Token attributes must be complete (test scans enum members for attributes). Reference sample coverage (every construct used in samples).
3. **Copilot Instructions** — Explicit rules in `.github/copilot-instructions.md` for every change type: adding keywords, adding constraints, updating grammar, syncing semantic tokens, etc.

**Non-negotiable sync points:**
- `precept.tmLanguage.json` (TextMate grammar) must track parser keywords/operators. Grammar sync checklist in instructions.
- `PreceptAnalyzer.cs` completions and `PreceptSemanticTokensHandler.cs` semantic tokens must track `DiagnosticCatalog` and new constructs. Intellisense sync checklist in instructions.
- All model type names in `PreceptModel.cs` must match public API names in `PreceptRuntime.cs` and documentation.
- `docs/PreceptLanguageDesign.md` constraint codes (C38–C53) must match `DiagnosticCatalog` IDs.

#### VII. Architectural Concerns Flagged

1. **Thin-wrapper violations in MCP tools** — Some tools have <20 lines of business logic but are independently maintained. Ensure logic stays in `src/Precept/` and tools only serialize/deserialize. Regular audit needed.

2. **Expression evaluator robustness** — `PreceptExpressionEvaluator` is not unit-tested independently; testing happens through runtime and inspect tests. Consider dedicated expression tests to isolate null-handling, arithmetic overflow, operator precedence issues.

3. **Naming density in ConstraintViolation model** — `ConstraintViolation`, `ConstraintTarget`, `ConstraintSource` are interdependent types. Documentation and samples are critical to prevent confusion. Ensure examples cover mixed-field violations, multi-target scenarios.

4. **Edit mode protocol complexity** — `PreceptPreviewProtocol` now carries typed field data, edit metadata, violations with index-based refs. Well-designed but fragile if protocol changes. Versioning strategy needed before wide distribution.

5. **Graph analysis completeness** — `PreceptAnalysis.cs` is sound but not exhaustive. It detects reachability and structural dead-ends but cannot detect impossible conditions from combined field constraints (e.g., a state whose entry rules contradict its data rules). This is acceptable ("let inspector catch runtime impossibilities") but must be documented.

6. **No cross-precept references** — Preceptsare currently isolated definitions. No support for composing workflows, importing shared rules, or inheriting state machines. Design needed if this becomes a requirement.

#### VIII. Drift Between Design & Implementation

**Minimal drift found:**
- **Design goal:** Language is "tooling-friendly" with keyword-anchored statements and no indentation. ✅ **Implemented:** Parser uses flat token stream, semantic tokens track keywords.
- **Design goal:** Deterministic deterministic semantics. ✅ **Implemented:** No hidden state, all results derived from input + definition.
- **Design goal:** Type-checking is compile-time-first. ✅ **Implemented:** `PreceptTypeChecker` runs in `Compile()`, blocks on errors.
- **Design goal:** Catalogs are the single source of truth for language knowledge. ✅ **Implemented:** Token attributes, `ConstructCatalog`, `DiagnosticCatalog` all defined once and reflected by consumers.
- **Design goal:** Violations are structured with attribution. ✅ **Implemented:** `ConstraintViolation` carries source + targets. Consumer decides rendering.

**Minor gaps (non-blocking):**
- **Edit declaration UI** has per-keystroke validation (✅) but collection granular ops (add/remove/enqueue/dequeue/push/pop) are designed but not fully tested. Likely works given generic `IUpdatePatchBuilder` pattern.
- **Graph analysis warnings** (C48–C53) are implemented but not yet hooked into `CompileFromText()` composed pipeline. `Validate()` returns them correctly; confirm language server is wired to use `Validate()` not just `Compile()`.
- **Semantic tokens** for rules and edit declarations likely working but not verified end-to-end in full extension build.

#### IX. Key Principles (Embedded in Architecture)

1. **The engine is the source of truth.** Documentation, DSL syntax, language server, and MCP tools all exist to serve the runtime engine, not the other way around.
2. **Precision over convenience.** Strict null handling, deterministic semantics, compile-time type checking — all prioritize auditability over developer convenience.
3. **One grammar, multiple consumers.** Parser outputs one canonical `PreceptDefinition`; compiler, language server, and MCP tools consume it independently.
4. **Violations have targets.** Runtime always reports what was violated, not just "something failed." This enables context-aware UI rendering.
5. **Catalogs are data, not behavior.** Token attributes, construct templates, constraint templates — all data-driven, testable, derived at startup. No hand-maintained duplicates.
6. **AI is a first-class consumer.** Deterministic semantics, structured APIs, complete MCP tooling, and queryable language reference all exist so an AI can author and validate preceptsahead without human iteration.

#### X. Testing & Quality (666 tests total)

- **Precept.Tests** (544 tests) — Parser, type checker, runtime (fire/inspect/update), rules, state machine behavior.
- **Precept.LanguageServer.Tests** (74 tests) — Analyzer, completions, semantic tokens, diagnostics.
- **Precept.Mcp.Tests** (48 tests) — Tool input/output, serialization, protocol.

All use xUnit + FluentAssertions. No project-specific test framework — standard .NET conventions. Drift-defense tests (construct examples, constraint triggers, token attributes) woven throughout.
