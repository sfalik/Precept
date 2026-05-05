### 2026-05-05T01:15Z: Executor delegates do not belong on `OperationMeta` records

**By:** Frank

**Status:** Accepted — settles one of two options for CC#25 registration mechanism.

**Merged source:** `frank-catalog-delegate-eval.md`.

- `OperationMeta` records serve the type checker, language server, MCP server, and doc generator — none of which execute operations. Adding `Func<…>?` executor fields pollutes language specification records with execution machinery.
- The catalog-driven architecture axiom: *pipeline stages read catalogs; catalogs do not become pipeline stages.* Putting executors on `OperationMeta` inverts this relationship.
- `Operations.cs` (1158 lines) has zero delegate fields anywhere — this is a deliberate, uniform pattern.
- Option A (catalog-delegate) in both forms — delegate field on `OperationMeta` pointing to named methods, or inline lambdas in the `GetMeta` switch — is dead on arrival.
- Named executor modules (`MoneyOperations`, `QuantityOperations`, etc.) in `src/Precept/Runtime/Operations/` are independently testable, named, and debuggable without catalog infrastructure.
