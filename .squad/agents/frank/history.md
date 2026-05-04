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
- When migrating a gap register, check target docs first — almost all gaps had already been captured informally in their respective docs. The migration job is source attribution + resolving the "Resolved in Source" closed ones, not wholesale insertion.


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

### 2026-05-04T00:02:05Z — Chunk 4: migrate-catalog-gaps

**Register processed:** `docs/working/Archived/catalog-gap-register.md` (43 entries)

**Outcome:**
- All 23 "Pending Decision" gaps confirmed in their target docs; `*Source: catalog-gap-register.md #N*` attributions added to all Open Question blocks.
- Gap #39 (Diagnostic Related Locations) upgraded from a plain bullet to a proper `⚠ Open Question` block in `docs/compiler/diagnostic-system.md`.
- Gaps #17 (`ActionMeta.SyntaxShape`), #18 (`FunctionMeta.HasCIVariant`), #24 (`ModifierMeta.ModifierCategory`) marked `✅ Resolved in Source` in their respective docs: `mcp.md`, `type-checker.md`, `catalog-system.md`.
- 5 "Already Captured" gaps confirmed in `catalog-system.md` § Open Questions — no changes needed.
- 8 "Moved/Captured in cross-cutting" entries noted — tracked via CC# register.
- 4 "Out of Scope" entries filed in `frank-chunk4-unplaced-gaps.md`.
- Register archived as `catalog-gap-register-migrated.md` with migration notice appended.
- Summary inbox: `frank-chunk4-gaps.md`.

**Blocking gaps still open:** #7 (SlotValue subtype shape conflicts) and #16 (SemanticIndex reference-tracking) require owner ruling before dependent implementation can proceed.



### 2026-05-04T04:30:00Z — Full Decisions Audit (CC#25 Q1–Q10 + CC#2 + PreceptValue slot storage)

**Scope:** Cross-checked every locked decision from the CC#25 and CC#2 series against all canonical runtime and compiler docs. Fixed five PARTIAL/MISSING doc gaps. Audit report at `.squad/decisions/inbox/frank-audit-report.md`.

**Key pattern confirmed:** `result-types.md` and `evaluator.md` were lagging behind `runtime-api.md` by 1–2 passes. Both still showed the old `object?`/`IReadOnlyDictionary` API. The `cross-cutting-decisions.md` CC#2 entry was still 🔴 Pending despite CC#2 having been locked weeks earlier. The `compiler-and-runtime-design.md` §5 still described expression slots as carrying `SourceSpan` only — directly contradicting CC#2 Option C.

**Files changed:**
- `docs/runtime/result-types.md` — G1: Version API Surface block replaced with two-lane API (JSON + typed lanes), cross-references runtime-api.md as authoritative. G2: I/O table updated from `IReadOnlyDictionary<string, object?>` to JSON lane / typed lane with cross-reference. G3: `FieldAccessInfo.CurrentValue` object? → PreceptValue; `FieldAccessMode { Read, Write }` → `{ Readonly, Editable }`. G4: `FieldSnapshot.Value` object? → PreceptValue?
- `docs/compiler-and-runtime-design.md` — §5 parser section: "expression-carrying slots currently carry only `SourceSpan`" text replaced with CC#2 locked decision summary. Stale Open Question about three representation options replaced with ✅ Resolved block.
- `docs/working/cross-cutting-decisions.md` — CC#2 entry: 🔴 Pending Shane decision → ✅ Resolved Option C Hybrid (2026-05-03T23:39:16Z) with decision summary and blocked-items-now-unblocked callout.
- `docs/runtime/evaluator.md` — Version struct: object?[] → PreceptValue[]; stale Slots open question removed; Evaluator signatures updated; FiredArgs added to Transitioned/Applied/Rejected; Working Copy Management section updated to rent/donate/return zero-copy flow; new §7.0 "Evaluation Stack Allocation" section added (CC#25 Q6: stackalloc PreceptValue[32], MaxStackDepth enforcement, sync-only constraint).
- `docs/runtime/precept-builder.md` — ExecutionPlan record: added `int MaxStackDepth` field with CC#25 Q6 annotation.

**Decisions confirmed COVERED (no change needed):** CC#25 Q1, Q2, Q4, Q7, Q10. Slot storage with PreceptValue in precept-builder.md and runtime-api.md.

**Decisions fixed:** CC#25 Q3 (CC#2 expression slot text), Q5 (evaluator.md zero-copy), Q6 (evaluator.md §7.0 stackalloc + precept-builder.md MaxStackDepth), Q8 (result-types.md G1–G4 + evaluator.md FiredArgs), CC#2 (cross-cutting-decisions.md status + compiler-and-runtime-design.md §5).

**Open flags raised (require Shane direction):**
- TypeRuntime<T> shape reconciliation between runtime-api.md (flat sealed record) and catalog-system.md (abstract base + generic subclass hierarchy). Catalog-system.md is authoritative; runtime-api.md needs a cleanup pass when the TypeRuntime.cs stub is built.
- Non-expression SlotValue shape conflicts (ModifierListSlot, AccessModeSlot, BecauseClauseSlot) remain unresolved — separate from CC#2.

**Durable architectural takeaways:**
- Always audit result-types.md and evaluator.md together after a runtime API pass — they lag the public-facing runtime-api.md because they document the internal representation separately.
- FiredArgs belongs on Transitioned, Applied, and Rejected — the three variants that correspond to a row having matched. InvalidArgs, Unmatched, EventConstraintsFailed, and UndefinedEvent are failure/error paths with no submission context.
- The CC#2 resolution date in the squad ledger should be cross-posted to cross-cutting-decisions.md immediately — that doc is checked first by new contributors and was still showing 🔴 Pending.


### 2026-05-04T04:19:13Z — chunk-4 migration + full decisions audit closed
- Frank-66 is complete: all 43 catalog-gap-register entries were migrated or triaged, the working register was archived, and 23 pending gaps were attributed across 9 canonical docs while cross-cutting and out-of-scope items stayed explicitly routed.
- Frank-67 is complete: the full CC#25 / CC#2 audit fixed 5 lagging docs (`evaluator.md`, `result-types.md`, `precept-builder.md`, `compiler-and-runtime-design.md`, `cross-cutting-decisions.md`) and preserved the remaining open flags around `TypeRuntime<T>` reconciliation and non-expression `SlotValue` shapes.
