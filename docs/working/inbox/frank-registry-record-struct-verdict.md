### 2026-05-05T03:03Z: `record struct` opcodes do not change the embedded-delegate verdict

**By:** Frank

**Status:** Accepted — closes follow-up question on value-type opcodes.

**Merged source:** `frank-registry-record-struct-verdict.md`.

- If opcodes were `record struct`, Scenario A (global array + compact structs) would have 4× higher cache density per cache line. This is theoretically correct but practically irrelevant: Precept evaluates 5–50 opcodes per dispatch; the entire working set fits in L1 cache regardless.
- `record struct` transition is premature optimization. Do not pursue until profiling demands it.
- The prior verdict's conclusion (embedded delegates, eliminate global registry) holds for the deeper architectural reason: simplicity — one fewer indirection, one fewer global mutable structure, one fewer initialization ceremony, self-contained evaluator. These benefits are independent of value-type vs. reference-type.
