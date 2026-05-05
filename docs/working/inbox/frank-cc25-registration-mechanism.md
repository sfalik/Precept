### 2026-05-05T01:27Z: LOCKED — CC#25 registration mechanism: `TypeRuntimeMeta` instance arrays; Runtime-layer `OperationRegistry` aggregation

**By:** Frank

**Status:** Accepted. Closes the open design question in catalog-system.md.

**Merged source:** `frank-cc25-registration-mechanism.md`.

- **`OperationMeta` is pure metadata, always.** No delegate fields, no executor references.
- **Executor modules** (`MoneyOperations`, `QuantityOperations`, etc.) are `internal static` classes in `src/Precept/Runtime/Operations/`. Methods have stable names and correspond 1:1 to `OperationKind` values.
- **`TypeRuntimeMeta.BinaryExecutors`/`UnaryExecutors`** are instance arrays, the **authority**. Each `TypeRuntime<T>` concrete implementation populates its arrays from the executor module's static methods at type initialization.
- **Runtime-layer aggregation registry** (name TBD — `OperationRegistry` is placeholder; must be in `Precept.Runtime` namespace, NOT `Precept.Language`) aggregates delegates from all registered `TypeRuntimeMeta` instances into flat `OperationKind`-indexed arrays at startup. This is the evaluator's dispatch table — a derived read-only view; `TypeRuntimeMeta` arrays are the source of truth.
- The `Operations.BinaryExecutors[(int)kind]` in evaluator.md §7 refers to this Runtime-layer class, NOT `Language.Operations`. Doc correction required: use fully-qualified name to eliminate ambiguity.
- **"The catalog entry IS the behavior"** principle applies to the `TypeMeta.Runtime` entry (the `TypeRuntime` instance), not to individual `OperationMeta` entries.
- Open items: OQ-DISP-1 (final class name — Shane decides), OQ-DISP-2 (evaluator.md doc correction), OQ-DISP-3 (catalog-system.md open question struck).
