## Core Context

- Owns editorial/code review with emphasis on implementation honesty, clarity, and redundancy removal.
- Reviews should verify claims against code, samples, and tests rather than accept narrative at face value.
- Hero snippets and README examples must be compact, compile-credible, and free of decorative language that obscures the product claim.

## Recent Updates

### 2026-04-08 - Initial security survey: Phase 1 + Phase 2 complete

**Phase 1 — Internal codebase survey findings:**
- Product: AI-facing domain integrity engine for .NET. MCP server (stdio transport, 5 tools) + LSP language server. No HTTP endpoints, no file I/O, no process spawning, no reflection-based type loading.
- Primary trust boundaries: `text` parameter (unbounded DSL string), `data`/`fields`/`args` dictionaries (unvalidated key-value maps), MCP tool output (echoes raw user-supplied content), LSP document text.
- Top risks found: (1) No input size limits on `text` — DoS via unbounded parsing; (2) Error messages and violation DTOs echo raw identifier names and `because` clauses — indirect prompt injection via MCP output; (3) No logging of any kind in MCP server; (4) `data`/`fields` dicts accept unknown keys with no allowlisting.
- Positive: No file I/O, network calls, process spawning, or dangerous deserialization patterns. System.Text.Json with JsonElement is safe.

**Phase 2 — External resources fetched:**
- OWASP Input Validation Cheat Sheet (input bounds, allowlisting)
- OWASP LLM01:2025 Prompt Injection (indirect injection via output)
- OWASP LLM Top 10 Project page (context and scope)
- OWASP MCP Security Cheat Sheet (directly applicable — new resource, highest relevance)
- OWASP Deserialization Cheat Sheet (confirmed safe .NET pattern)
- OWASP Logging Cheat Sheet (no logging present — gap identified)
- Microsoft .NET Security and User Input (range checking guidance)

**Output written:** `docs/research/security/security-survey.md`

### 2026-04-05 - Consolidation safety gate recorded
- Rejected direct merge, force-repoint, blind squash, and docs-only cherry-pick for the unrelated-history return to trunk.
- Confirmed the health checks were green at review time (dotnet build; dotnet test --no-build, 703/703) and approved only a freeze-and-curate cutover from a frozen SHA.
