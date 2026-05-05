### 2026-05-05T02:45Z: LOCKED — Global `Operations.BinaryExecutors[]` eliminated; delegates embedded in opcodes

**By:** Frank

**Status:** Accepted. Supersedes frank-operations-registry-analysis.md.

**Merged source:** `frank-operations-registry-verdict.md`.

- **The fatal flaw:** opcodes are `sealed record` (reference types). The memory-layout argument ("flat value-type array, cache-friendly") was factually wrong. The evaluator already chases a heap pointer to reach every opcode. Adding a `Func<>` field adds one pointer-width field to an object already dereferenced — marginal cost is zero.
- **Embedded delegates win:** deref opcode → fetch delegate → call (2 steps). Global array: deref opcode → extract Kind → index static array → fetch delegate → call (4 steps). Embedded path has one fewer indirection.
- **Verdict:** `BinaryOp` gains an `Executor: Func<PreceptValue, PreceptValue, PreceptValue>` field. `UnaryOp` gains `Executor: Func<PreceptValue, PreceptValue>`. Builder fetches from `TypeRuntimeMeta.BinaryExecutors[(int)kind]` at build time; evaluator calls `opcode.Executor(l, r)` directly.
- **`Language.Operations` catalog unchanged** — holds `OperationMeta` (language spec), never executors.
- **`TypeRuntimeMeta` remains the source of truth** for executor delegates.
- **Global aggregation array eliminated** — not yet implemented in source, so no removal needed.
