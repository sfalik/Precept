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

- PRECEPT0024 anti-mirroring enforcement: `RegisterOperationAction` on `OperationKind.PropertyReference` is the cleanest Roslyn hook for guarding member access. Namespace-qualified type resolution prevents false positives on unrelated types sharing a property name. Walking `ContainingSymbol` up through nested types handles lambdas, local functions, and inner classes correctly.

- Runtime doc accuracy gap: design docs (result-types.md, runtime-api.md, evaluator.md) describe the TARGET API, but code stubs deviated in several places. Key divergences to watch: EventOutcome/UpdateOutcome naming (nested vs flat, variant names), FieldAccessMode values (Read/Write vs Readonly/Editable), FromJson→Restore API shape change, ConstraintKind enum naming. All divergences documented in inbox.
- Restoration design changed significantly from the original `FromJson` design: the implementation chose `Restore(string? state, JsonElement fields) → RestoreOutcome` over `FromJson(JsonElement) → Version (throws)`. The structured `RestoreConstraintsFailed` variant captures schema-drift scenarios the original design silently skipped.
- The 5 non-ping MCP tools have zero implementation — no stubs, no DTOs, no handlers. "Stubbed" in the doc was inaccurate; corrected.
- `ConstraintKind` enum names in the runtime docs were stale aliases that never matched the actual Language catalog names (`Invariant/StateResident/StateEntry/StateExit/EventPrecondition`).
- Descriptor types (`FieldDescriptor`, `ArgDescriptor`, etc.) are fully implemented in `Descriptors.cs` — the doc was still saying "Planned". Also, `ClrType` and `SlotIndex` on `ArgDescriptor` described in the doc never made it into the implementation.

## Recent Updates

### 2026-05-09T14:14:17Z — `precept_language` tests expanded post-review
- Soup Nazi's review found 7 concrete `LanguageToolTests.cs` gaps after the initial `precept_language` shipment: missing schema coverage, missing top-level completeness/order assertions, missing modifier subgroup checks, weak operator/function validation, representative mapping gaps, and an overly strict token-floor assertion.
- The remediation expanded the suite from 12 total project tests to 19, locking catalog completeness/order coverage and the documented `>= 80` token-floor contract without changing the shipped `LanguageTool` surface.

### 2026-05-09T14:04:05Z — `precept_language` shipped
- Implemented `LanguageTool.cs`, added 12 `LanguageToolTests.cs` cases, and synced `docs/tooling/mcp.md` on commit `bd4e6e30`.
- The shipped MCP vocabulary baseline is now durably recorded as tokens/types/grouped modifiers/actions/constructs/constraints/operators/functions/diagnostics plus static `firePipeline`.




### 2026-05-08T03:08:18Z — Comprehensive runtime/MCP doc review recorded
- Newman-1 corrected runtime and MCP doc inaccuracies across descriptor/result/runtime API docs and `docs/tooling/mcp.md`.
- Remaining runtime/MCP contract divergences (outcome naming, restore shape, constraint violation payloads, MCP DTO gaps) are now preserved in `.squad/decisions.md` for owner follow-up.

### 2026-05-07 — PRECEPT0024 anti-mirroring enforcement landed
- Implemented Roslyn analyzer `Precept0024AntiMirroringEnforcement` guarding `.Syntax` access on 10 Typed* records outside TypeChecker.
- 8 tests (4 TP, 4 TN) cover GraphAnalyzer/ProofEngine/Builder access, lambda closures, nested classes, non-guarded types, and non-Syntax properties.
- Closes OQ1 from type-checker.md §13.


### 2026-04-27 — Dual-surface MCP config landed and validated
- Implemented the approved minimal change: repo-root `.mcp.json` now gives Copilot CLI a repo-local `mcpServers.precept` entry pointing at `node tools/scripts/start-precept-mcp.js`.
- Preserved the existing boundaries: `.vscode/mcp.json` stays the VS Code/workspace-local `servers` config, and `tools/Precept.Plugin/.mcp.json` stays the shipped `dotnet tool run precept-mcp` payload.
- Directly related docs were updated in the same batch, and the stale `docs/ArtifactOperatingModelDesign.md` reference was retired in favor of `tools/Precept.Plugin/README.md`.
- Validation confirmed the `github` server should not be mirrored into the root CLI config because Copilot CLI already provides it natively.

### 2026-04-25 — AI Surface Completeness Review
- Wrote `.squad/decisions/inbox/newman-catalog-metadata-ai-review.md` assessing all 10 catalogs as AI grounding surface.
- Current `precept_language` covers ~40% of what AI needs. Critical gaps: no type system metadata (widening, accessors), no operation legality table, no modifier applicability matrix, no syntax reference.
- Recommended: single-response catalog-keyed serialization, tagged unions for DUs, new `syntaxReference` prose section. No per-catalog endpoints.
- Prioritized 11 changes across 3 tiers. Types, Operations, and Modifiers catalogs are the bottleneck — MCP serialization is trivial once `All` exists.

### 2026-04-12 — Squad `squad:copilot` retirement cleanup
- Tracked the workflow/template/doc blast radius and kept `squad:chore` distinct from the retired coding-agent label.

### 2026-04-11 — Declaration guard DTO contract
- Locked the additive `precept_compile` expansion for invariants, state asserts, event asserts, and edit blocks with nullable `when`.

### 2026-05-08T02:26:13Z — PRECEPT0024 batch recorded
- Scribe recorded the shipped anti-mirroring analyzer batch and durable OQ1 closeout after the committed work validated green.
- Guarded surface remains the 10 `Typed*` records with `.Syntax` access allowed only inside `TypeChecker`; no MCP contract changes were required.

### 2026-05-09T01:31:51-04:00 — `precept_compile` implementation landed
- Implemented `precept_compile` in `tools/Precept.Mcp/Tools/CompileTool.cs` as a thin `Compiler.Compile` wrapper returning diagnostics plus a structured definition only when `Compilation.HasErrors` is false.
- Added the compile DTO hierarchy in `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`, including top-level result, diagnostics, states, events, rows, rules, and the expanded field shape (`Modifiers`, `IsWritable`).
- Added the missing `ProjectReference` from `tools/Precept.Mcp/Precept.Mcp.csproj` to `src/Precept/Precept.csproj`.
- Locked the expression/action rendering strategy to source-text slicing via `SourceSpan.Offset`/`Length`, with qualifier text reconstructed from explicit `DeclaredQualifiers` metadata.

### 2026-05-09T15:21:46Z — MCP tool discovery diagnosis remains in flight
- `newman-2` is still investigating the MCP discovery regression (3 tools found instead of 5) plus stdout pollution causing `Failed to parse message` warnings.
- Current durable breadcrumb: `docs/working/newman-mcp-tool-discovery-diagnosis.md`. Scribe logged the in-progress state in `.squad/orchestration-log/2026-05-09T15-21-46Z-newman-2.md`.
