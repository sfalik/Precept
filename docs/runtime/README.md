# runtime/ — Runtime API and Component Design

> [!IMPORTANT]
> **Most runtime components are design-locked but not yet implemented.** `runtime-api.md` carries the public surface contract; `precept-builder.md`, `evaluator.md`, and `descriptor-types.md` are pre-implementation designs. Code in `src/Precept/Runtime/` exists (`Precept.cs`, `Version.cs`) but all operation bodies throw `NotImplementedException`. Treat this folder as design reference; the runtime ships with v1.

Design documents for the Precept runtime — the boundary between compiled precepts and host applications.

## Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [runtime-api.md](runtime-api.md) | Public runtime API surface — `Precept.From()`, entity representation, event firing, field updates, inspect/preview | Design |
| [result-types.md](result-types.md) | Result type taxonomy — three type families for representing operation outcomes (success, violation, fault) | Design |
| [fault-system.md](fault-system.md) | Fault codes and classification — each names a class of failure and the compiler diagnostic that prevents it, paired at build time | Draft |
| [precept-builder.md](precept-builder.md) | Precept Builder — compile-to-runtime transformation; descriptor tables, dispatch indexes, execution plans | Stub |
| [evaluator.md](evaluator.md) | Evaluator — plan executor for all four runtime operations (Create, Fire, Update, Restore) | Stub |
| [descriptor-types.md](descriptor-types.md) | Descriptor types — first-class runtime identity for all declared program elements | Stub |

## Reading Order

1. [runtime-api.md](runtime-api.md) — public surface and open decisions
2. [precept-builder.md](precept-builder.md) — compile-to-runtime transformation
3. [evaluator.md](evaluator.md) — plan execution and ingress validation
4. [descriptor-types.md](descriptor-types.md) — descriptor type shapes
5. [result-types.md](result-types.md) — how results flow back to callers
6. [fault-system.md](fault-system.md) — failure classification

## Open questions

Every runtime document now carries an `Open Questions / Implementation Notes` section with an index of what it leaves undecided. The discussion of each question stays in the section that raised it — the indexes and this table only make them findable. Ordered by the document that owns the question.

| Question — what somebody has to decide | Where it is discussed |
|---|---|
| How `Version` is represented internally — a slot array, or a hybrid with a name-based public API. Slot arrays are the direction; the formal close waits on the executable model contract. | [runtime-api.md](runtime-api.md) § R3 — Entity Representation Internals |
| What the executable model gives the evaluator — dispatch tables, slot layouts, constraint buckets, and the typed descriptors that replace raw strings across the API. | [runtime-api.md](runtime-api.md) § D8/R4 — Executable Model Contract |
| Whether a defect in Precept itself — a bug in the compiler, the builder or the evaluator — reaches the caller as a structured outcome or as a thrown exception. **Parked by the owner until the runtime design is done; his stated lean is exception, and a lean is not a ruling.** | [fault-system.md](fault-system.md) § Q3 (fullest statement); [evaluator.md](evaluator.md) § 9 Fault Production; [precept-builder.md](precept-builder.md) § 9 Builder Invariant Violations states one of the two accounts that disagree |
| What context a `Fault` should carry, if anything ever constructs one — where in the plan the breach was caught, what the engine's own state was, which operation was running. Waits on the question above. | [fault-system.md](fault-system.md) § Q2 |
| Which type carries what inspection reports where execution has no constraint verdict to preview — proof provenance rather than a verdict. `BuildConstraintResults` stays unspecified until it is decided. | [evaluator.md](evaluator.md) § 7.2 Inspection Operations |
| What a direct `Update` of an editable field checks before it commits: the field's own declared constraints only, or also every rule that mentions the field. **Outstanding owner decision**; his want document's reading (every rule that mentions the field) is the working assumption until he confirms it. | `precept-language-spec.md` § Open Questions; [runtime-api.md](runtime-api.md) § Update |
| Four opcode semantics: how `LOAD_ARG` treats an absent or null input, whether `BRANCH_FALSE` treats numeric zero as false, whether `RETURN` may fall through to further opcodes, and whether the evaluation stack is pooled rather than `stackalloc`-only. | [evaluator.md](evaluator.md) § 7.3 Opcode Execution Engine |
| The internal field layout of `PreceptValue` — tag type, where the union payload sits, which value types share which union region. The 32-byte size and tagged-struct shape are locked; the layout inside them is not. | [evaluator.md](evaluator.md) § 5 Inputs and Outputs |
| What a trace record looks like, how a trace context is attached to an evaluation call, and how the language server and MCP server read the trace. | [evaluator.md](evaluator.md) § 11 Decision 8 |
| What abandons a row part-way through, if anything does. The `ArrayPool` return path for a working collection array was written against a post-mutation constraint failure, which no longer exists. | [evaluator.md](evaluator.md) § D. Copy-on-Write Protocol for Multi-Mutation Events |
| Whether the working copy's slot array is stack-allocated for small precepts or always heap-allocated. | [evaluator.md](evaluator.md) § 13 Implementation Status |
| Which ownership model `Version.Slots` uses — the working array donated raw, or wrapped in an immutable type. It decides how far zero-copy promotion reaches. | [evaluator.md](evaluator.md) § 13 Implementation Status |
| Which storage shape `FieldDescriptor.AccessModes` takes — a dictionary keyed by state, or an indexed representation. | [precept-builder.md](precept-builder.md) § 7 Pass 1 — Descriptor Pass; [evaluator.md](evaluator.md) § 7.5 Access Mode Enforcement; the type is [descriptor-types.md](descriptor-types.md)'s |
| What happens when the certificate gate refuses — throw or structured result, whether it names the derivation step that failed to replay, whether verification is cached across loads. That the failure must explain itself is settled. | [precept-builder.md](precept-builder.md) § 9 The Certificate Gate |
| How the certificate reaches `Precept.From` at all — a sixth member of `Compilation`, a separate argument, or a file the method is pointed at. | [precept-builder.md](precept-builder.md) § 9 The Certificate Gate |
| Whether the `ConstraintPlanIndex` keeps its five state-and-event anchors, now that both surviving uses are anchored to a slot or to a catalog listing. It could shrink to what ingress needs, or be replaced by a premise list derived from the definition's certificate. | [precept-builder.md](precept-builder.md) § 12 Constraint Indexes, and What They Index Now |
| Whether `ConstructMeta` should carry `ModelContribution` metadata so the builder's assembly loop becomes fully generic. | [precept-builder.md](precept-builder.md) § 13 Pending Design Decisions |
| Whether the expression compiler stays a stack machine or moves to register allocation. A stack machine is the current design; the move is left open against profiling evidence. | [precept-builder.md](precept-builder.md) § 13 Pending Design Decisions |
| Whether the `ExecutionPlan` opcode array stays a discriminated union or becomes a flat struct array with an enum discriminator. | [precept-builder.md](precept-builder.md) § 13 Pending Design Decisions |
| What replaces `ConstraintDescriptor.ReferencedFields`, currently a provisional flat list of field names. The typed target hierarchy meant to take its place waits on the constraint evaluation attribution model, which is itself undecided. | [descriptor-types.md](descriptor-types.md) § Open Questions; the model it waits on is [evaluator.md](evaluator.md) § 7.2 |

Three entries that sit in an `Open Questions / Implementation Notes` section are **not** open, and are deliberately absent from the table above: `runtime-api.md` § Stateless Precepts — CreateInitialVersion is locked; `fault-system.md` § Q1 is resolved; and all three entries in `result-types.md` are requirements recorded against the executable model contract — arg-dependency sets, transitive unresolvability propagation, and Kleene three-value guard evaluation — rather than choices anybody still has to make.

## Relationship to Other Docs

- [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) — the architectural spine; pipeline → Compilation → Precept → Version chain. Read first.
- [`../compiler/README.md`](../compiler/README.md) — the pipeline whose `Compilation` output the runtime consumes.
- [`../tooling/README.md`](../tooling/README.md) — the LSP preview panel and planned MCP runtime tools (`precept_fire`, `precept_create`, `precept_update`, `precept_inspect`) depend on the runtime shipping.
- `research/architecture/runtime/` — 10-system evaluator architecture survey grounding these designs.

## Cross-cutting concerns

- **Fault system mirrors diagnostic system.** The fault classes are shaped as a mirror of the compile-time diagnostics — same code namespace conventions, same severity model — and each names the diagnostic that prevents it. The mirroring is a build-time pairing, not a pair of things happening at compile time and at run time. See [`fault-system.md`](fault-system.md) and [`../compiler/diagnostic-system.md`](../compiler/diagnostic-system.md).
- **Inspectability.** Every runtime behavior must be inspectable at compile time — which actions would fire on a hypothetical event, what proof obligations the proof engine discharged, what diagnostics each transition surfaces. The runtime never produces opaque outcomes. See [philosophy.md § Inspectability](../philosophy.md).
- **Public-surface stability vs. implementation maturity.** `runtime-api.md` is "Design — public surface locked"; consumer code can be written against the designed surface, but operation bodies are stubs until v1. This is intentional — the designed surface is the contract; the implementation comes second.

## Known drift, recorded 2026-07-21

Found while establishing the operation execution order. Two of the four items below have since been reconciled; two remain open (both are code-vs-metadata gaps, not doc drift).

**Reconciled 2026-07-21:**

- **Pipeline vocabularies reconciled to § 3A.4.** `result-types.md` and `evaluator.md` previously used an orphaned "Stage 1 … Stage 10" numbering and a separate "Step 1–3 / 2a–2e" numbering, neither tied to canon. Both now name and cite the phases of `precept-language-spec.md` § 3A.4, *Operation execution order*, which is the single normative vocabulary.
- **`Fire` lifecycle enumerations brought into line.** `evaluator.md` (flow diagram, Working-Copy lifecycle list, and `Fire` pseudocode) and `runtime-api.md` (the Fire-pipeline sentence) now enumerate exit actions, the state change and `omit` reset, and entry actions at their § 3A.4 positions. Each preserved the two § 3A.4 caveats then open — state-action multiplicity/order, and whether entry actions fire at construction/self-transition — rather than asserting an answer. Both were settled in § 3A.4 on 2026-11-25, and the caveats were replaced by the rules.

**Still open (code-vs-metadata gaps, need implementation, not a doc edit):**

- **Constraint activation timing is promised from catalog metadata that carries none.** `src/Precept/Runtime/Evaluator.cs` states that constraint activation timing comes from the constraint catalog's metadata, but `src/Precept/Language/Constraints.cs` defines the activation kinds as empty records with no timing data on them. Either the metadata is owed or the promise is wrong.
- **Two write-semantics classifications have no members.** `ActionWriteSemantics` declares members that no action in the catalog uses, so any consumer switching over it carries live branches that nothing reaches. Either actions are missing a classification or the classification is over-specified.
