# runtime/ — Runtime API and Fault System Design

Design documents for the Precept v2 runtime — the boundary between compiled precepts and host applications.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [runtime-api.md](runtime-api.md) | Public runtime API surface — `Precept.From()`, entity representation, event firing, field updates, inspect/preview. Decisions R1 ✅, R2 ✅, R5 ✅; R3 (entity representation) open. | Design |
| [result-types.md](result-types.md) | Result type taxonomy — three type families for representing operation outcomes (success, violation, fault). Decision R2. | Design |
| [fault-system.md](fault-system.md) | Runtime fault codes and classification — the runtime mirror of the compiler's diagnostic system. | Stub |

## Reading Order

1. [runtime-api.md](runtime-api.md) — public surface and open decisions
2. [result-types.md](result-types.md) — how results flow back to callers
3. [fault-system.md](fault-system.md) — failure classification

## Relationship to Other Docs

- `docs.next/architecture-planning.md` §4 — runtime architecture decisions (R1–R5, D8)
- `docs.next/compiler/` — the compiler pipeline whose output the runtime consumes
- `research/architecture/runtime/` — 10-system evaluator architecture survey grounding these designs
- `docs/RuntimeApiDesign.md` — v1 runtime API design. This folder designs the v2 replacement.
