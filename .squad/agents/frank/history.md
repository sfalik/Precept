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


### 2026-05-04T03:39:16Z — CC#2 acceptance merged
- Frank-62's CC#2 acceptance is now durable in the squad ledger.
- Canonical record: Option C (Hybrid) accepted; the five locked SlotValue shape decisions now live in decisions.md.
- Redundant stamped options brief was cleared during inbox dedupe.

### 2026-05-03T23:45:15Z — runtime-api.md updated for CC#25 Q2/Q5/Q7 and CC#2

- Applied all locked CC#25 ingress/egress decisions to `docs/runtime/runtime-api.md` in full.
- Replaced `Metadata-First Principle` section with `Two-Lane Ingress Principle` — the dictionary design is gone; JSON lane (`JsonElement?`) and typed lane (`Action<IArgBuilder>?` / `Action<IFieldBuilder>?`) are the complete ingress surface.
- Updated `Precept` class block: two overloads each for `Create`/`InspectCreate` (JSON + typed); `Restore` is JSON-only.
- Updated `Version` class block: `PreceptValue` indexer + `Get<T>` typed access; `ArgDescriptor` replaces `ArgInfo`; `RequiredArgs` returns `IReadOnlyList<ArgDescriptor>`; all commit and inspect methods split into JSON lane and typed lane overloads; no dictionary overloads.
- Replaced all code examples (`Create`, `Restore`, `Fire`, `Update`, `InspectFire`, `InspectUpdate`) — every dictionary literal gone, both lanes shown where applicable; Restore shows JSON only.
- Updated `FieldAccessInfo.CurrentValue` from `object?` to `PreceptValue`.
- Added new type sections: `IArgBuilder`, `IFieldBuilder` (Ingress Types), `PreceptValue`, `TypeRuntime<T>`, `FiredArgs` (Value Types).
- Updated Design Rationale and Decisions section to document two-lane design rationale and updated Decisions tracking to include CC#25 closures.
- Updated R3 open question to reflect CC#25 Q5 zero-copy `PreceptValue[]` slot donation decision.
- Added `IReadOnlyDictionary` exclusion to Deliberate Exclusions.
- Status table updated: doc maturity reflects locked CC#25 decisions.

- Frank's Q7 revision pass is fully accepted and merged into the squad ledger.
- All seven locked decisions are now durable context: From/To naming, no string JSON overloads, typed Restore removed, arg slot arrays with presence mask, zero-boxing `TypeRuntime<T>`, `FiredArgs` typed egress, and fluent typed builders for Fire/Inspect/Create.
- `IReadOnlyDictionary<string, object?>` convenience/extension methods are obsolete and removed from scope.

### 2026-05-03T23:59:12Z — Fix 1: result-types.md FiredArgs on EventOutcome variants; Fix 2: C# stub signature update

**Files changed:**
- `docs/runtime/result-types.md` — Added `FiredArgs Args` to `Transitioned`, `Applied`, and `Rejected` record declarations and updated table notes for those three variants. Added `FiredArgs.cs` to the source file list. `UndefinedEvent`, `Unmatched`, `InvalidArgs`, `EventConstraintsFailed` unchanged — they are failure/error paths where no submission context exists.
- `src/Precept/Runtime/EventOutcome.cs` — Updated `Transitioned`, `Applied`, `Rejected` record signatures to include `FiredArgs Args`. XML summaries updated to reference the property.
- `src/Precept/Runtime/Precept.cs` — Added `using System.Text.Json;`. Replaced single-overload `Create(IReadOnlyDictionary?)` / `InspectCreate(IReadOnlyDictionary?)` with JSON lane (`JsonElement?`) + typed lane (`Action<IArgBuilder>?`) overload pairs. `Restore` signature updated from `IReadOnlyDictionary<string, object?>` to `JsonElement` (JSON-only hydration path per Q7 locked decision).
- `src/Precept/Runtime/Version.cs` — Added `using System.Text.Json;`. Indexer return type changed from `object?` to `PreceptValue`. Added `Get<T>(string fieldName)` method. Replaced all four single-overload commit/inspect methods with JSON lane + typed lane pairs: `Fire`×2, `Update`×2, `InspectFire`×2, `InspectUpdate`×2. Removed stale comment about string-keyed args.

**New stub files created:**
- `src/Precept/Runtime/FiredArgs.cs` — `PreceptValue this[string]` indexer + `Get<T>(string)` (stubs, `NotImplementedException`)
- `src/Precept/Runtime/PreceptValue.cs` — abstract class with `FromJson`, `FromClr<T>`, `ToClr<T>`, `ToJson` stubs
- `src/Precept/Runtime/IArgBuilder.cs` — `IArgBuilder Set<T>(string, T)` interface stub
- `src/Precept/Runtime/IFieldBuilder.cs` — `IFieldBuilder Set<T>(string, T)` interface stub

**Build:** `dotnet build src/Precept/Precept.csproj` → 0 errors, 0 warnings.

### 2026-05-04T00:02:05Z — Chunk 3: cc25-doc-impact-pass

**Three targets assessed; two updated:**

- `docs/runtime/precept-builder.md` — Applied CC#25 Q1 (SlotLayout vocabulary): changed `object?[]` → `PreceptValue[]` for the evaluator register file description; added explicit construct-slots vs field-slots vocabulary callout; changed `LoadLit(object? Value)` → `LoadLit(PreceptValue Value)` per CC#25 compiler-output-impact (literals pre-wrapped at build time); updated evaluator stack machine example to `Stack<PreceptValue>` with typed variable declarations. Q10 (TypeMeta.Runtime) was NOT added here — correct scope is catalog-system.md.
- `docs/language/catalog-system.md` — Applied CC#25 Q10 (TypeMeta.Runtime): added `TypeRuntime? Runtime = null` to the `TypeMeta` record; added new `##### TypeRuntime — typed-lane registration` subsection documenting the abstract base + generic `TypeRuntime<T>` hierarchy, `ReadJson`/`WriteJson`/`ParseString`/`FormatString` catalog delegates, `FromClr`/`ToClr` typed-lane delegates, and the `PreceptRuntime.Register<T>` registration pattern; captured durable architecture rule (persistence/typed-lane behavior belongs on catalog metadata — no per-`TypeKind` switches).
- `README.md` — No changes. README uses a simplified illustrative API snippet (not the real runtime surface) and contains no dictionary ingress examples.

**Gaps recorded to inbox:** `frank-chunk3-gaps.md` — TypeRuntime stub file not yet created; `runtime-api.md` TypeRuntime<T> shape vs. catalog-system.md need reconciliation when stub lands; TypeMeta.Runtime scoping rationale.

- Frank-64's runtime/result-types pass is durably logged for the squad record.
- `docs/runtime/result-types.md`, `src/Precept/Runtime/Precept.cs`, and `src/Precept/Runtime/Version.cs` now reflect the two-lane ingress/value surface captured in the current stub set.
- New stubs `FiredArgs`, `PreceptValue`, `IArgBuilder`, and `IFieldBuilder` are part of the active runtime API baseline; build status remained clean.
- Follow-up gaps were preserved in decisions.md: stale `result-types.md` API surface text, stale inputs/outputs table wording, `FieldAccessInfo.CurrentValue`, `FieldSnapshot.Value`, and the missing `TypeRuntime<T>` stub still need owner-directed cleanup.

### 2026-05-04T04:13:09Z — Scribe closeout for frank-65

- Oversized decisions ledger checked at 796301 B; the 7-day archive gate ran and found no entries older than the cutoff, so no archive moves were required.
- Merged `frank-chunk3-gaps.md` into `decisions.md`, cleared the inbox, and recorded health at `decisions.md` 796301 B -> 801093 B.
- Durable orchestration/session logs written for the chunk-3 doc sync; history summarization remained unnecessary.
