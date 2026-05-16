## Core Context

- Owns MCP server and plugin distribution surfaces, including DTO shape, tool contracts, and plugin/package correctness.
- Enforces the thin-wrapper rule: MCP should expose core behavior, not duplicate domain logic.
- Historical summary (pre-2026-04-13): drove MCP vocabulary/spec sync for new types and constraints, declaration-guard DTO design, and Squad automation/docs cleanup around the retired `squad:copilot` lane.

## Learnings

- MCP contract changes should be additive when possible and preserve existing consumer shapes.
- Catalog-driven vocabulary is the preferred path for exposing DSL keywords/types; non-token constructs need explicit catalog registration.
- Label/automation removals should be staged through sync workflows and disabled notices, not silent deletion.
- Proof-engine MCP integration is only agent-complete when `precept_compile` exposes a documented proof schema with structured assessments; a raw `Dump()` reference is not a sufficient contract.
- `precept_language` already has the right catalog-driven insertion point for proof diagnostics, but `id`/`phase`/`rule` alone is too thin for contradiction-vs-unresolved proof semantics.
- Repo-local MCP config now has three explicit surfaces: repo-root `.mcp.json` for Copilot CLI, `.vscode/mcp.json` for VS Code workspace development, and `tools/Precept.Plugin/.mcp.json` for the shipped payload.
- **precept_patterns content location:** Pattern content lives in `src/Precept/Language/SyntaxReference.cs` as `static IReadOnlyList<CommonPattern> CommonPatterns` and `static IReadOnlyList<AntiPattern> AntiPatterns`. `CatalogFormatters.FormatPatterns()` (in `tools/Precept.Mcp/CatalogFormatters.cs`) reads those lists and renders markdown. `PatternsTool.cs` is a one-line wrapper. To add patterns: append `new("Name", "Description", """snippet""")` to `CommonPatterns`. To add anti-patterns: append `new("Name", "Description", """bad""", """good""", "whyItFails")`. After changes, update the count in `Quickstart.cs` (`precept_patterns` tool guide entry) and verify both `NewToolTests.cs` and `SyntaxReferenceTests.cs` assertions still hold.
- **Stale MustSetOmitToNonOmit test assertion:** `NewToolTests.Patterns_DefaultCall_ReturnsMarkdown` previously asserted `result.Should().Contain("MustSetOmitToNonOmit")`. That diagnostic code was renamed as part of the v3 field-state design; `SyntaxReferenceTests` explicitly asserts it is NOT in `WhyItFails`. The MCP test assertion was stale and was updated (2026-05-12) to check for `"omit ApprovedAmount"` (text from the good snippet) and the 6 new pattern headings instead.

- **Slice 11 pattern:** When the core DU has `IsConstruction` on the abstract base, the DTO maps from the base directly — no per-subtype dispatch. `TypedEventRowSuccess` and `TypedEventRowReject` both inherit the flag, keeping the MCP mapping to a single-line expression.

- PRECEPT0024 anti-mirroring enforcement: `RegisterOperationAction` on `OperationKind.PropertyReference` is the cleanest Roslyn hook for guarding member access. Namespace-qualified type resolution prevents false positives on unrelated types sharing a property name. Walking `ContainingSymbol` up through nested types handles lambdas, local functions, and inner classes correctly.

- Runtime doc accuracy gap: design docs (result-types.md, runtime-api.md, evaluator.md) describe the TARGET API, but code stubs deviated in several places. Key divergences to watch: EventOutcome/UpdateOutcome naming (nested vs flat, variant names), FieldAccessMode values (Read/Write vs Readonly/Editable), FromJson→Restore API shape change, ConstraintKind enum naming. All divergences documented in inbox.
- Restoration design changed significantly from the original `FromJson` design: the implementation chose `Restore(string? state, JsonElement fields) → RestoreOutcome` over `FromJson(JsonElement) → Version (throws)`. The structured `RestoreConstraintsFailed` variant captures schema-drift scenarios the original design silently skipped.
- The 5 non-ping MCP tools have zero implementation — no stubs, no DTOs, no handlers. "Stubbed" in the doc was inaccurate; corrected.
- `ConstraintKind` enum names in the runtime docs were stale aliases that never matched the actual Language catalog names (`Invariant/StateResident/StateEntry/StateExit/EventPrecondition`).
- Descriptor types (`FieldDescriptor`, `ArgDescriptor`, etc.) are fully implemented in `Descriptors.cs` — the doc was still saying "Planned". Also, `ClrType` and `SlotIndex` on `ArgDescriptor` described in the doc never made it into the implementation.
- `precept_language` is structurally correct but expensive as a first-call AI contract: the compact payload is ~196.9 KB (~50k tokens), and `domains` + `tokens` + `diagnostics` + `types` consume ~82% of it. If the goal is AI authoring, explicit companion tools (especially a tiny syntax-first reference) are cleaner than a single giant catalog dump.

- **8 new MCP tools (2026-05-09):** Thin-wrapper pattern using `LanguageTool.Language()` as an internal projection entry-point works well: focused tools call it and extract the relevant catalog subsets, keeping each tool response small. `precept_language` deregistered (attribute removed, implementation kept). `precept_diagnostic` lookup supports both code-name ("UndeclaredField") and PRE-number ("PRE0017") formats via simple string prefix check. `precept_domains` adds `UcumPrefixCatalog` which was absent from the base `LanguageTool.Language()` domains payload. `precept_operations(category?)` filters by LhsType string (case-insensitive) — natural because unary ops put operand in LhsType and binary ops put lhs type there. All 59 MCP tests green; 3733 core tests unaffected.
- **Skill/agent MCP sync (2026-05-11):** Both Precept skills and the Precept Author agent were updated to match the actual 10-tool surface (ping + 9 focused tools). `precept-authoring` previously called `precept_language` (deregistered), referenced `precept_inspect`/`precept_fire` (never shipped) in a Verify Behavior step, and had stale DSL syntax (`nullable` → `optional`, `write` → `modify ... editable`, `set X = null` → `clear X`). `precept-debugging` similarly called `precept_language`, `precept_inspect`, `precept_fire`, and `precept_update`. All phantom tool references replaced. Debugging skill now makes explicit that all diagnosis is static — no runtime introspection exists via MCP.
- **DTO-free MCP rollout (2026-05-12):** The clean exit from the DTO forest is to let each focused tool read catalogs directly and render compact markdown, not to keep a hidden aggregate projection alive. `precept_types` and `precept_domains` need scope gates before shipping, `precept_operations` must stay filter-first, and `precept_compile` should stop at compact diagnostics plus a prose summary. Current repo reality check: `samples/inventory-item.precept` still does not compile cleanly, so any contract/tests that assume a green sample must be treated as a separate runtime/sample-state issue, not solved inside `tools/Precept.Mcp/`.

## Recent Updates

### 2026-05-16T09:08:43Z — Slice 11: isConstruction on event row DTO

- Added `CompileEventRowDto(EventName, IsConstruction)` to `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`.
- `CompileResultDto` gains `EventHandlers: CompileEventRowDto[]`.
- `CompileTool.cs` maps `compilation.Semantics.EventHandlers` via a one-line `MapEventRow` method.
- `IsConstruction` reads directly from `TypedEventRow.IsConstruction` on the DU base — no DU dispatch needed.
- `Compile_UsesExpectedJsonShape` test updated to include `"eventHandlers"` in expected JSON field order.
- Added `CompileTool_EventRow_IsConstruction_True` and `CompileTool_EventRow_IsConstruction_False` tests.
- `docs/tooling/mcp.md` updated: `precept_compile` contract now documents `eventHandlers` array and `CompileEventRowDto` shape.
- 46 MCP tests pass, 5781 core tests pass. Pre-existing LanguageServer build errors are unrelated.

### 2026-05-11T00:27:07Z — t2-13 MCP recovery guidance recorded
- Commit `617d175f` fixed catalog-driven recovery hints in `Faults.cs` / `Diagnostics.cs`, aligning BUG-014, BUG-015, and BUG-041 guidance with real `when ...count > 0`, `notempty`, and `is set` authoring patterns.
- Added `test/Precept.Mcp.Tests/RecoveryHintTests.cs`; the broader batch closed at 105 MCP tests and 4,531 core tests passing.

### 2026-05-10T23:55:32Z — t2-12 MCP DTO audit durably recorded
- Commits `5f79fc7a` and `6a211bc4` synced the `precept_compile` DTO surface, compile projection, focused MCP regression coverage, and `docs/tooling/mcp.md` contract text.
- Durable note: `docs/tooling/mcp.md` is the active in-repo MCP contract doc on this branch; `docs/McpServerDesign.md` is absent.

### 2026-05-09 to 2026-05-10 — Earlier MCP rollout condensed
- The `precept_language` / `precept_compile` rollout, focused-tool implementation pattern, stdout-to-stderr transport fix, and the tool-discovery diagnosis were all recorded in `.squad/decisions.md`; this live file now keeps only the durable posture and latest checkpoints.
- Key standing posture: focused tools project from `LanguageTool.Language()`, the repo-root `.mcp.json` stays the Copilot CLI surface, and older DTO/doc-drift details remain preserved in the ledger instead of repeated here.

### 2026-05-16T13:08:43Z — Constructor semantics batch closed around MCP surface

- George's Slice 8b and Kramer's Slices 9+10 established the semantic/editor baseline that the MCP `isConstruction` DTO now reflects.
- Frank's docs/sample closeout means the MCP compile contract, language tooling, docs, and sample surface now describe the same declaration-level `initial` model.
- Scribe recorded the full-stack closeout and preserved the durable MCP note in `.squad/decisions.md`.

