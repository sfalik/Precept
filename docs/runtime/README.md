# runtime/ — Runtime API and Component Design

Design documents for the Precept runtime — the boundary between compiled precepts and host applications.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [runtime-api.md](runtime-api.md) | Public runtime API surface — `Precept.From()`, entity representation, event firing, field updates, inspect/preview | Design |
| [result-types.md](result-types.md) | Result type taxonomy — three type families for representing operation outcomes (success, violation, fault) | Design |
| [fault-system.md](fault-system.md) | Runtime fault codes and classification — the runtime mirror of the compiler's diagnostic system | Stub |
| [precept-builder.md](precept-builder.md) | Precept Builder — compile-to-runtime transformation; descriptor tables, dispatch indexes, execution plans | Stub |
| [evaluator.md](evaluator.md) | Evaluator — plan executor for all four runtime operations (Create, Fire, Update, Restore) | Stub |
| [descriptor-types.md](descriptor-types.md) | Descriptor types — first-class runtime identity for all declared program elements | Stub |

## Reading Order

1. [runtime-api.md](runtime-api.md) — public surface and open decisions
2. [precept-builder.md](precept-builder.md) — compile-to-runtime transformation
3. [evaluator.md](evaluator.md) — plan execution and constraint evaluation
4. [descriptor-types.md](descriptor-types.md) — descriptor type shapes
5. [result-types.md](result-types.md) — how results flow back to callers
6. [fault-system.md](fault-system.md) — failure classification

## Relationship to Other Docs

- `docs/compiler-and-runtime-design.md` — pipeline, artifacts, runtime surfaces
- `docs/compiler/` — the compiler pipeline whose output the runtime consumes
- `research/architecture/runtime/` — 10-system evaluator architecture survey grounding these designs
