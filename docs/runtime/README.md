# runtime/ — Runtime API and Component Design

> [!IMPORTANT]
> **Most runtime components are design-locked but not yet implemented.** `runtime-api.md` carries the public surface contract; `precept-builder.md`, `evaluator.md`, and `descriptor-types.md` are pre-implementation designs. Code in `src/Precept/Runtime/` exists (`Precept.cs`, `Version.cs`) but all operation bodies throw `NotImplementedException`. Treat this folder as design reference; the runtime ships with v1.

Design documents for the Precept runtime — the boundary between compiled precepts and host applications.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [runtime-api.md](runtime-api.md) | Public runtime API surface — `Precept.From()`, entity representation, event firing, field updates, inspect/preview | Design |
| [result-types.md](result-types.md) | Result type taxonomy — three type families for representing operation outcomes (success, violation, fault) | Design |
| [fault-system.md](fault-system.md) | Runtime fault codes and classification — the runtime mirror of the compiler's diagnostic system | Draft |
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

- [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) — the architectural spine; pipeline → Compilation → Precept → Version chain. Read first.
- [`../compiler/README.md`](../compiler/README.md) — the pipeline whose `Compilation` output the runtime consumes.
- [`../tooling/README.md`](../tooling/README.md) — the LSP preview panel and planned MCP runtime tools (`precept_fire`, `precept_create`, `precept_update`, `precept_inspect`) depend on the runtime shipping.
- `research/architecture/runtime/` — 10-system evaluator architecture survey grounding these designs.

## Cross-cutting concerns

- **Fault system mirrors diagnostic system.** Runtime faults are the runtime-side mirror of compile-time diagnostics — same code namespace conventions, same severity model. See [`fault-system.md`](fault-system.md) and [`../compiler/diagnostic-system.md`](../compiler/diagnostic-system.md).
- **Inspectability.** Every runtime behavior must be inspectable at compile time — which actions would fire on a hypothetical event, what proof obligations the proof engine discharged, what diagnostics each transition surfaces. The runtime never produces opaque outcomes. See [philosophy.md § Inspectability](../philosophy.md).
- **Public-surface stability vs. implementation maturity.** `runtime-api.md` is "Design — public surface locked"; consumer code can be written against the designed surface, but operation bodies are stubs until v1. This is intentional — the designed surface is the contract; the implementation comes second.

## Known drift, recorded 2026-07-21

Found while establishing the operation execution order. Two of the four items below have since been reconciled; two remain open (both are code-vs-metadata gaps, not doc drift).

**Reconciled 2026-07-21:**

- **Pipeline vocabularies reconciled to § 3A.4.** `result-types.md` and `evaluator.md` previously used an orphaned "Stage 1 … Stage 10" numbering and a separate "Step 1–3 / 2a–2e" numbering, neither tied to canon. Both now name and cite the phases of `precept-language-spec.md` § 3A.4, *Operation execution order*, which is the single normative vocabulary.
- **`Fire` lifecycle enumerations brought into line.** `evaluator.md` (flow diagram, Working-Copy lifecycle list, and `Fire` pseudocode) and `runtime-api.md` (the Fire-pipeline sentence) now enumerate exit actions, the state change and `omit` reset, and entry actions at their § 3A.4 positions. Each preserves the two genuinely-open § 3A.4 caveats — state-action multiplicity/order, and whether entry actions fire at construction/self-transition — rather than asserting an answer.

**Still open (code-vs-metadata gaps, need implementation, not a doc edit):**

- **Constraint activation timing is promised from catalog metadata that carries none.** `src/Precept/Runtime/Evaluator.cs` states that constraint activation timing comes from the constraint catalog's metadata, but `src/Precept/Language/Constraints.cs` defines the activation kinds as empty records with no timing data on them. Either the metadata is owed or the promise is wrong.
- **Two write-semantics classifications have no members.** `ActionWriteSemantics` declares members that no action in the catalog uses, so any consumer switching over it carries live branches that nothing reaches. Either actions are missing a classification or the classification is over-specified.
