### 2026-05-05T03:07Z: `static readonly Func<>` delegates — not `unsafe delegate*` — for opcode executor storage

**By:** Frank

**Status:** Accepted.

**Merged source:** `frank-delegate-heap-verdict.md`.

- **Verdict:** use `static readonly Func<PreceptValue, PreceptValue, PreceptValue>` delegates. Do not use `unsafe delegate*`.
- All executor methods are static — no closures, no instance state. Both `delegate*` and `static readonly Func<>` are technically applicable.
- `unsafe delegate*` costs: propagates `unsafe` through `BinaryOp`, `ExecutionPlan`, and into user-facing APIs; cannot be stored in `object`, used as generic type arguments, or used with expression trees; saves ~150ns per event at business-operation cadence — unmeasurable.
- `static readonly Func<>` costs: ~48 bytes per delegate on x64; ~100 operations × 48 bytes = ~4.8 KB total, allocated once at type initialization, immortal for process lifetime. Zero per-eval allocation. Zero GC pressure.
- JIT devirtualizes and inlines static delegate calls in hot paths — the most optimizable delegate pattern.
