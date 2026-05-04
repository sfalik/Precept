## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, runtime, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow dispatches by metadata/shape instead of construct identity; CC#1 keeps a sealed typed-expression DU while the broader parser/runtime surface stays metadata-driven.
- CC#25 runtime baseline is locked around `PreceptValue` tagged-value execution plus catalog-owned delegate dispatch; LS/MCP interactive tooling keeps traced tree-walk evaluation.
- Runtime-type metadata is catalog-owned on `TypeMeta.Runtime` via abstract `TypeRuntime` + sealed subclasses; any indexed runtime table is derived from the catalog rather than maintained in parallel.
- Collections remain BCL-first (`System.Collections.Immutable` plus thin wrappers where needed); persistent semantics stay non-negotiable for working-copy discard.
- Public execution stays sync-only. Stack-depth enforcement is a Type Checker diagnostic / builder trust-boundary concern, not a runtime fallback path.
- Q7 accepted baseline: `Version.Get<T>` / `FiredArgs.Get<T>` are the primary typed access surfaces, raw indexers expose `PreceptValue`, `TypeRuntime` uses `FromJson` / `ToJson` / `FromClr` / `ToClr`, `IArgBuilder` uses slot arrays plus a presence mask, restore remains JSON-only hydration, and there is no dictionary or string convenience ingress lane.
- Q2 remains closed: opcodes stay in `Precept.From()` / the executable model, while `Compilation` / `CompilationResult` stay analysis-only for authoring surfaces.
- Q3/Q4/Q5/Q6 are all durably settled for current planning: execution plans are eager Pass 5 artifacts on the immutable `Precept` model; working copies do not cross row boundaries semantically; winning slot arrays donate directly into the next `Version`; and async wrappers are out of scope.

## Learnings

- When a late acceptance closes provisional design notes, record the supersession explicitly so older exploratory entries do not read as active architecture.
- Separate durable API surface decisions from rejected convenience lanes; the rejected lane still needs an explicit closeout note.
- Summarize back to architectural baselines once detailed delivery history crosses the active-context threshold.

## Recent Updates

### 2026-05-04T03:26:10Z — CC#25 Q7 acceptance revision complete
- Frank's Q7 revision pass is fully accepted and merged into the squad ledger.
- All seven locked decisions are now durable context: From/To naming, no string JSON overloads, typed Restore removed, arg slot arrays with presence mask, zero-boxing `TypeRuntime<T>`, `FiredArgs` typed egress, and fluent typed builders for Fire/Inspect/Create.
- `IReadOnlyDictionary<string, object?>` convenience/extension methods are obsolete and removed from scope.
