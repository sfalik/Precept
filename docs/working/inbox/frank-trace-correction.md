### 2026-05-04T01:08:14Z: Dual-interpreter model rejected; trace stays inside the single A+G interpreter

**By:** Shane (via Copilot)

**Status:** Recorded from inbox correction merge.

**Merged source:** `frank-trace-correction.md`.

- Rejected: a production A+G runtime paired with a separate LS/MCP tree-walk interpreter.
- Adopted instead: one stack-based opcode interpreter serves every consumer, with optional per-step trace emission for tooling and diagnostics.
- Trace record shape and LS/MCP consumption remain open implementation seams, but the architecture no longer permits a second semantic engine.
