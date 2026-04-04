# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0, TypeScript — full stack code review
- **My domain:** Code review across all components: `src/Precept/`, `tools/`, `test/`
- **Key checklists:** Grammar Sync Checklist and Intellisense Sync Checklist (in custom instructions) — must verify on DSL surface changes
- **MCP thin-wrapper rule:** Logic in tools that belongs in `src/Precept/` is a violation
- **Docs sync rule:** Code changes must keep `README.md` and `docs/` in sync
- **Created:** 2026-04-04

## Learnings

### Code Review Knowledge Refresh (2026-04-05)

**Codebase Structure:**
- `src/Precept/Dsl/` (11 core files): Parser → tokenizer → type checker → runtime engine. Clean separation of concerns.
- `tools/Precept.LanguageServer/` (9 handlers + analyzer): LSP server wired to parser/compiler for diagnostics, completions, hover, semantic tokens, preview.
- `tools/Precept.Mcp/Tools/` (7 files): Five MCP tools wrapping core compile/fire/inspect/update APIs as thin DTOs. No domain logic leakage into tools.
- All three components follow identical null-handling and error-reporting patterns.

**Code Style & Conventions:**
1. **Records over classes** for immutable data: PreceptDefinition, PreceptField, PreceptEvent, all DTOs are sealed records. Exceptions: PreceptEngine, PreceptInstance, internal working classes.
2. **Sealed types** aggressively used — no inheritance surprises. Static inner types for working data.
3. **Ordinal comparisons** throughout: `StringComparer.Ordinal` is the standard for all identifiers, state names, event names (culture-blind, case-sensitive).
4. **Pattern matching** heavily used: `switch` expressions for model navigation and outcome evaluation. Exhaustive matching is checked.
5. **Nullable reference types** enabled (`<Nullable>enable</Nullable>` in csproj). Consistent use of `?` for optional fields.
6. **No null-forgiveness** (`null!`) except one instance in PreceptAnalyzer.cs line 35 — `TryGetDocumentText(..., out text!)` — which is safe because TryGetValue populates the out var.
7. **Dictionary.TryGetValue pattern** the standard for all lookups — no KeyNotFoundException throws.
8. **Inline DTOs** preferred over separate files when single-use (e.g., BranchDto, StateDto inline in CompileTool.cs).

**Null Handling (Consistent Pattern):**
- Input validation uses `?? Array.Empty<T>()` for model collections that might be null (line 72: `_invariants = model.Invariants ?? Array.Empty<PreceptInvariant>()`).
- Optional output uses nullable types: `IReadOnlyDictionary<string, object?>?` for event arguments.
- Defensive checks: `if (value is null)` followed by early return or fail. No null-coalescing chains or silent null-skipping.
- Collection items checked on insertion: `if (item is not null) list.Add(item)` in hydration methods.
- Rare `?.` usage (safe navigation) only on optional properties where null is expected (e.g., `value?.ToString()`).

**Error Handling Pattern (Rock-Solid):**
1. **ConstraintViolationException** carries the LanguageConstraint for diagnostic code derivation.
2. **DiagnosticCatalog** is the central registry — every error has an ID, phase, rule, message template, severity.
3. **Constraint sync comments**: Lines in parser/compiler have `// SYNC:CONSTRAINT:C7` comments linking to the constraint ID.
4. **Three entry points for errors:**
   - `Parser.Parse()` → throws InvalidOperationException wrapped as ConstraintViolationException.
   - `Parser.ParseWithDiagnostics()` → returns (model, List<ParseDiagnostic>) for LS use.
   - `Compiler.Validate()` → returns TypeCheckResult with diagnostics list.
5. **MCP tools** catch diagnostics into DTOs (DiagnosticDto) with line, column, code, severity.

**Naming Consistency (Excellent):**
- State/event/field names: Always singular or plural as declared (no renaming).
- Method names: PascalCase, verb-first for mutations (Fire, Update, Inspect), noun-first for queries (CheckCompatibility, GetDiagnostics).
- Parameter names: Camel case, descriptive (eventName not evt, instanceData not data).
- Internal prefixes: `_` for private fields, `__collection__` for internal hydrated collection keys (escaping is explicit and consistent).

**Design Doc Alignment:**
- ✅ `RuntimeApiDesign.md` accurately reflects code: PreceptParser.Parse/ParseWithDiagnostics, PreceptCompiler.Compile, PreceptEngine.CreateInstance, .Fire, .Inspect, .Update all match spec.
- ✅ `McpServerDesign.md` tool specs match CompileTool, FireTool, InspectTool, UpdateTool, LanguageTool output structures.
- ✅ `PreceptLanguageDesign.md` DSL semantics align with parser combinator implementations.
- No drift detected; docs are canonical and code honors them.

**Quality Concerns (Top 3 Red Flags):**

1. **null-forgiveness in PreceptAnalyzer.cs:35** — `out text!` assumes TryGetValue always succeeds. Safe here (TryGetValue contract) but fragile pattern. Consider explicit `_ = _documents.TryGetValue(...) ? text : ""` or assert.

2. **Incomplete regex coverage in PreceptAnalyzer completions** — Many inline regexes (NewFieldDeclRegex, NewEventWithArgsRegex, SetAssignmentExpressionRegex) are hand-written and case-insensitive. If parser syntax drifts, these regexes won't catch errors. **Action:** Document that grammar changes require regex review here. No automated check exists.

3. **Hydrate/Dehydrate dual format** — Internal format uses `__collection__<fieldName>` prefix for CollectionValue objects; public API uses clean field names with List<object>. Conversion happens in HydrateInstanceData/DehydrateData/CloneCollections (three places). Mutation sites must use all three correctly or silent corruption occurs. **Action:** Add invariant checks or consolidate to single format.

**Documentation Sync Requirement Met:**
- Code matches all design docs. No aspirational claims found in README.
- API surface stable (three-step pipeline: Parse → Compile → Engine).

**Standards to Enforce on PRs:**
- Every error path must land a DiagnosticCatalog entry with SYNC:CONSTRAINT comment.
- New DSL syntax requires grammar + completions regex + semantic token updates (three-sync rule).
- Null-forgiveness banned except in immediate safe contexts (document rationale inline).
- All collections use TryGetValue; no KeyNotFoundException throws.
- Records immutable; only engine internals mutate (and only under clone-and-commit model).
