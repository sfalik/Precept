## Core Context

- Owns test discipline across parser, type checker, runtime, MCP, language server, and analyzer validation.
- Treats behavioral claims as unproven until executable evidence exists and records gaps as actionable findings, not just counts.
- Historical summary (pre-2026-04-13): led broad verification for declaration guards, including parser/type-checker/runtime/LS/MCP coverage and test-matrix planning for guarded editability.

## Learnings

- Multi-source analyzer coverage needs only one structural test-helper upgrade: `AnalyzerTestHelper` must accept multiple source strings. The rest of the analyzer harness already scales.
- Cross-catalog analyzer stubs must stay minimal and must not pull in `FrozenDictionary`, `ImmutableArray`, or other real-catalog BCL-heavy surfaces.
- Dual-surface MCP validation is blind unless the config artifact and directly related documentation land together.
- Numeric-literal and arithmetic tests are heavily shaped by context flow: binary-expression operands resolve under null expected-type, so some mismatch paths are only observable at the unit-table layer.

## Recent Updates

### 2026-04-27 — MCP dual-surface validation rerun approved
- The pre-landing blocked run is now superseded. Root `.mcp.json` exists, parses cleanly, and correctly uses the CLI `mcpServers` schema with only the local `precept` server.
- `.vscode/mcp.json` remains the VS Code/workspace `servers` config with both `precept` and `github`, and `tools/Precept.Plugin/.mcp.json` remains the unchanged shipped payload.
- Directly related docs now describe the three-surface boundary precisely, and no stale live operating-model reference remains.
- Durable testing pattern: dual-surface changes should land config + doc updates together in the same PR.

### 2026-04-26 — PRECEPT0005/PRECEPT0006 shipped; PRECEPT0007 remains follow-up
- Implemented PRECEPT0005 and PRECEPT0006 with focused analyzer coverage and caught the real sqrt `ParameterMeta` reference-identity bug in production catalog code.
- Kept PRECEPT0007 as the next-step proposal: flag `Enum.GetValues<CatalogEnum>()` outside the owning `All` getter.

### 2026-04-26 — Analyzer infrastructure and full test-plan bar
- Defined the minimal harness change for cross-catalog analyzer tests and the stub rules needed to keep them reliable.
- Set the testing bar for the analyzer expansion at roughly 298 cases across helper coverage, analyzer-specific suites, and regression anchors.
- Accepted the only notable blind spot: spread elements inside shared static arrays, with declaration-site validation/regression coverage as the backstop.

### 2026-04-25 — Catalog-driven metadata test strategy review
- Flagged the operations matrix as the highest-value generated test surface and required snapshot/golden coverage per catalog.
- Framed catalog drift testing as non-negotiable once catalog-owned behavior starts replacing hand-written tables.

### 2026-04-24 — Precept.Next coverage audit and slice support
- Identified the compile-time blockers that prevented deeper TypeChecker test work: hollow model shapes and missing diagnostic codes.
- Added targeted Faults and OperatorTable/binary-expression coverage while documenting what remained untestable until scaffolding was fixed.
