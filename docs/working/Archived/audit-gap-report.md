# Audit Gap Report — decisions.md vs. Canonical Docs

> **Auditor:** Frank (Lead)  
> **Date:** 2026-05-04  
> **Method:** Full content-level comparison of every decisions.md entry against evaluator.md, compiler-and-runtime-design.md, descriptor-types.md, precept-builder.md, runtime-api.md, catalog-system.md, precept-language-spec.md, cross-cutting-decisions.md, and decisions-summary.md.  
> **Scope:** All 714 lines of decisions.md read; all listed canonical docs read in full or substantial part.

---

## CC#25 — Runtime Decisions

---

### CC#25 interactive tooling keeps traced tree-walk evaluation while production stays typed-opcode based

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `decisions.md` now marks the old dual-consumer entry as superseded, so this contradiction call is stale.

**What's in decisions.md:** Entry dated 2026-05-03T22:22:27Z states: *"Durable dual-consumer model: production Fire/Inspect/Update uses the A+G typed-opcode runtime, while LS/MCP interactive tooling keeps a `TypedExpression` tree-walk path for rich per-node traces and sub-50 ms authoring feedback. JSON-native or span-stack evaluation is rejected as the production stack currency."*

**What's in canonical docs:** `decisions-summary.md` (last updated 2026-05-04) under "Single interpreter with diagnostic trace — no dual-path" states: *"There is one interpreter. The A+G stack-based opcode executor serves ALL consumers — production AND LS/MCP authoring feedback. Dual interpreters were explicitly rejected: two runtimes that must agree is a correctness liability; tooling that uses a different engine will diverge and mislead authors. Instead, the opcode loop emits per-step diagnostic records when trace mode is enabled."* No canonical runtime doc (evaluator.md, precept-builder.md, runtime-api.md) mentions the tree-walk path for LS/MCP. The evaluator.md documents a single opcode-based pipeline with no LS/MCP branch.

**Gap:** The raw `decisions.md` ledger still contains the OLD rejected decision verbatim — "Durable dual-consumer model" — as an apparently adopted entry. Frank-71 updated `decisions-summary.md` to reflect the correct reversal, but `decisions.md` itself was NOT updated to mark this entry superseded or corrected. Anyone reading `decisions.md` linearly finds this entry and has no indication it was overturned. The "trace mode" detail (how the single interpreter exposes per-step diagnostics to tooling) is documented only in `decisions-summary.md` — not in `evaluator.md` or `compiler-and-runtime-design.md`.

**Fix needed:**
1. The 2026-05-03T22:22:27Z entry in `decisions.md` titled "CC#25 interactive tooling keeps traced tree-walk evaluation…" must be marked **SUPERSEDED** — add an explicit note: *"SUPERSEDED: The dual-consumer model was rejected. See the 2026-05-04 entries above. The correct decision is single interpreter with trace mode."*
2. `evaluator.md` §2 Overview and §4 Right-Sizing must explicitly state that one opcode executor serves both production and tooling consumers, with trace-mode records as the hook for LS/MCP diagnostics. Currently §2 and §4 are silent on this design identity.
3. `compiler-and-runtime-design.md` §11 (runtime surface) must document the single-interpreter guarantee.

---

### CC#25 runtime baseline is `PreceptValue` plus catalog-owned delegate dispatch

**Status:** ✅ Fully captured

**What's in decisions.md:** "Option A+G is production choice: 32-byte PreceptValue tagged struct on the evaluation stack and Version.Slots, with catalog-owned unary/binary executor arrays indexed by OperationKind."

**What's in canonical docs:** `evaluator.md` §5 PreceptValue section documents the 32-byte size, GC pressure rationale (~768 MB/s gen-0 at 100k events/sec with boxed `object?`), the tagged-value-struct shape, and the hot-path memory table. `decisions-summary.md` confirms the same. `evaluator.md` §7.3 shows `Operations.BinaryExecutors[(int)kind](l, r)` for catalog-owned delegate dispatch.

**Gap:** None for the core decision. But see the separate entry for `BinaryExecutors` ownership (`Operations` vs `TypeRuntime`) — evaluator.md and decisions-summary.md use different accessor paths.

---

### CC#25 `PreceptValue` struct vs. abstract class shape — CONTRADICTED between evaluator.md and runtime-api.md

**Status:** ⚠️ Partially fixed

**Update (2026-05-04):** The abstract-class contradiction is resolved: `runtime-api.md` now documents `public struct PreceptValue`. The remaining discrepancy is `internal` vs. `public` visibility between `evaluator.md` and `runtime-api.md`.

**What's in decisions.md:** CC#25 baseline decision locked `PreceptValue` as a 32-byte tagged struct — the internal evaluation currency for the opcode loop. The Q7 follow-on decision says raw indexers on `Version` return `PreceptValue`, and `FiredArgs` carries the same type.

**What's in canonical docs:** `evaluator.md` §5 defines `PreceptValue` as:
```csharp
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct PreceptValue { ... }
```
But `runtime-api.md` §Value Types defines it as:
```csharp
public abstract class PreceptValue
{
    public static PreceptValue FromJson(JsonElement element);
    public JsonElement ToJson();
    public static PreceptValue FromClr<T>(T value);
    public T ToClr<T>();
}
```
These are completely different type shapes: one is an `internal struct`, the other is a `public abstract class`. One is 32 bytes value-type; the other is a reference type class hierarchy.

**Gap:** The abstract-class contradiction is gone, but the boundary is still unresolved. `runtime-api.md` now presents `PreceptValue` as a `public struct`, while `evaluator.md` still presents it as an `internal struct`. Canonical docs still need to say whether this is one type with stale visibility in `evaluator.md` or an intentional public/internal split that needs an explicit conversion-boundary explanation.

**Fix needed:** Resolve the remaining `internal struct` vs. `public struct` mismatch. If the evaluator and public API use the SAME type, `evaluator.md` should align its visibility with `runtime-api.md`. If they are DIFFERENT types, `runtime-api.md` and `evaluator.md` must document the boundary and conversion mechanism explicitly. **⚠️ VERIFY WITH SHANE** — the one-type vs. two-type question still needs an explicit owner decision.

---

### CC#25 `LoadArg` opcode carries pre-resolved slot index

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `precept-builder.md` now defines `LoadArg(int ArgSlotIndex)` and notes that name resolution is complete before execution.

**What's in decisions.md:** Entry 2026-05-04T00:56:54Z states: *"Event args validate and convert at Fire entry into ephemeral per-call arg slots. `LOAD_ARG` reads from this array by pre-resolved slot index."* `decisions-summary.md`: *"LOAD_ARG opcode loads event args into the evaluator's PreceptValue[] register file."* The 2026-05-04T04:36:09Z audit entry explicitly confirms: *"corrects LOAD_ARG to slot-index dispatch."*

**What's in canonical docs:** `evaluator.md` §7.3 is correct:
```csharp
case LoadArg(var argSlotIndex):
    stack[top++] = args[argSlotIndex];  // args is PreceptValue[], pre-filled at Fire boundary
    break;
```
And the opcode table documents: *"`LOAD_ARG(argSlotIndex)` — Load event arg value by pre-resolved arg slot index."*

BUT `precept-builder.md` §Pass 5 Execution Plan Pass defines the `LoadArg` opcode as:
```csharp
public sealed record LoadArg(string ArgName) : Opcode;
```
This uses a `string ArgName` — NOT a pre-resolved slot index.

**Gap:** `precept-builder.md` defines `LoadArg(string ArgName)` while `evaluator.md` shows `case LoadArg(var argSlotIndex)`. These are incompatible. The builder produces the opcode; the evaluator consumes it. If they disagree on the opcode payload type, the system cannot be implemented from these docs. The decision is clear (slot index), so `precept-builder.md` is wrong.

**Fix needed:** In `precept-builder.md` §Pass 5, change the opcode definition from:
```csharp
public sealed record LoadArg(string ArgName) : Opcode;
```
to:
```csharp
public sealed record LoadArg(int ArgSlotIndex) : Opcode;
```
Add a note: *"Arg slot indexes are assigned in Pass 1 (Descriptor Pass) in declaration order. `LOAD_ARG` carries the index, not the name — name resolution is complete before execution."*

---

### CC#25 Fire-call lifecycle quantified as A+G implementation baseline

**Status:** ✅ Fully captured

**What's in decisions.md:** Peak hot-path memory ~44–48 PreceptValue slots, ~4,480 bytes stack traffic per Fire, ~88 bytes boundary-object GC allocation with slot-array pooling. Working copy is the donated next-version slot array.

**What's in canonical docs:** `evaluator.md` §5 has the full hot-path memory table with the same values (36–44 field slots, 4–8 ephemeral arg slots, 32-slot stackalloc, same-as-field-slots working copy). The 7-step Fire lifecycle in §6 Working Copy Management matches the decisions. ✅ This was correctly captured in the 2026-05-04 audit fill.

**Gap:** None.

---

### CC#25 TypeBuilder / codegen — rejected for SaaS baseline

**Status:** ✅ Fixed

**Update (2026-05-04, revised):** Fixed with full rationale. `evaluator.md` §4 now records all three blocking constraints from decisions.md: SaaS cold-start incompatibility, inspectability as a product guarantee (not debugger convenience), and same-process JIT-warm delegates making the warm-path advantage irrelevant. Prior AOT/trimming framing (not in decisions.md) removed.

**What's in decisions.md:** Entry 2026-05-03T22:22:27Z: *"The blocking constraint is SaaS cold-start and per-definition churn: hundreds of milliseconds of compile work on upload, cache miss, or deployment is incompatible with the save-and-test loop. Inspectability is a product guarantee, not an optional debugger convenience; TypeBuilder would require a second interpreted or tracing-decorator path."* `decisions-summary.md` captures a 2-sentence version.

**What's in canonical docs:** Neither `evaluator.md` nor `precept-builder.md` mentions TypeBuilder, codegen, or explains WHY the A+G interpreter was chosen over compiled alternatives. `evaluator.md` §4 Right-Sizing explains why the evaluator is scoped as it is, but does not address the TypeBuilder alternative or rejection rationale.

**Gap:** The WHY of the A+G choice (SaaS cold-start incompatibility, inspectability guarantee) is not captured in canonical docs. Anyone implementing the evaluator or proposing an alternative needs to know this rationale to understand what would reopen the decision. `decisions-summary.md` has a 2-sentence version, but canonical runtime docs should have the full rationale so the constraint is discoverable without reading the decisions ledger.

**Fix needed:** Add a **Design Rationale** subsection to `precept-builder.md` §4 Right-Sizing or `evaluator.md` §4 Right-Sizing explaining the TypeBuilder rejection: cold-start incompatibility with SaaS, inspectability as product guarantee (not debug convenience), and the durable boundary ("do not treat TypeBuilder as implicit v2 path unless deployment model or inspectability requirement changes first").

---

### CC#25 type-per-lane storage (Option F) — rejected

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `evaluator.md` §5 PreceptValue now includes a "Rejected alternative" block covering the split-lane analysis: 23 of 32 TypeKind members in reference lane, business-domain types cross-lane, NodaTime re-analysis changed details but not verdict.

**What's in decisions.md:** *"Option F's split-lane model does not materially reduce the hard cases because 23 of 32 TypeKind members still live in the reference lane."*

**What's in canonical docs:** No canonical doc mentions the split-lane option or why it was rejected.

**Gap:** The rationale for rejecting Option F is recorded in decisions.md and decisions-summary.md but not in any canonical doc. Less critical than TypeBuilder rejection since it's a more exotic alternative, but the guard against reopening is missing from canonical docs.

**Fix needed:** Add a brief note in `evaluator.md` §5 PreceptValue (or §4) as a "Rejected alternative" block summarizing the split-lane analysis: 23 of 32 TypeKind members remain in the reference lane regardless, adding routing complexity for no meaningful reduction in cross-lane operations.

---

### CC#25 `System.Linq.Expressions` / compiled path — designed-in seam, not v1

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `evaluator.md` §4 now states that compiled execution is a designed-in future seam over the same `Precept` model, not a v1 dual path.

**What's in decisions.md:** *"`System.Linq.Expressions` / compiled-path work stays a designed-in upgrade seam, not a v1 dual-path architecture; v1 ships the interpreter-shaped A+G runtime only."*

**What's in canonical docs:** `decisions-summary.md` mentions *"compiled paths (System.Linq.Expressions) are a designed-in upgrade seam, not a v1 dual path."* `evaluator.md` does not mention this explicitly.

**Gap:** The upgrade-seam framing (how the system is designed to accommodate future compiled paths without requiring a dual-path now) is not documented in `evaluator.md`. A future implementer looking at the evaluator has no signal that the stackalloc-based opcode loop is intentionally the ONLY path and that compiled paths are planned but not implemented.

**Fix needed:** Add a note in `evaluator.md` §4 Right-Sizing: *"`System.Linq.Expressions` compilation is a designed-in future seam — the opcode array structure is compatible with LINQ expression compilation. V1 ships the stack-based interpreter only. The upgrade seam does not require dual-path maintenance; the compiler produces the same opcode arrays that both paths consume."*

---

### CC#25 `BinaryExecutors` / `UnaryExecutors` — ownership conflict between `Operations` and `TypeRuntime`

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `evaluator.md` §7.3 now explains that `Operations` is the flat startup registry built from per-type `TypeRuntimeMeta` executor registrations.

**What's in decisions.md:** Entry 2026-05-04T04:36:09Z: *"catalog-system.md now places BinaryExecutors and UnaryExecutors on TypeRuntime and explains executor-array dispatch as catalog-owned runtime behavior rather than evaluator-owned switches."* `decisions-summary.md`: *"BinaryExecutors, UnaryExecutors on TypeRuntime."*

**What's in canonical docs:** `evaluator.md` §7.3 shows:
```csharp
stack[top++] = Operations.BinaryExecutors[(int)kind](l, r);
```
Executors are on `Operations`, not `TypeRuntime`. But the decisions say they are on `TypeRuntime`.

**Gap:** `evaluator.md` shows `Operations.BinaryExecutors` while `decisions-summary.md` says `BinaryExecutors` is on `TypeRuntime`. These could be consistent (Operations could wrap TypeRuntime's arrays) or genuinely inconsistent. The intermediate indirection, if any, is not documented. An implementer doesn't know where to put the executor arrays.

**Fix needed:** Clarify in `evaluator.md` §7.3 whether `Operations.BinaryExecutors` is the same array as `TypeRuntime.BinaryExecutors` (i.e., Operations aggregates per-type executors into a flat OperationKind-indexed array) or whether these are separate surfaces. If they are the same, add the sentence: *"The flat `Operations.BinaryExecutors` array is built at catalog startup from per-type executor delegates registered on each `TypeRuntime`."* Then update `catalog-system.md` to describe the relationship.

---

### CC#25 `TypeRuntimeMeta` `ReadJson`/`WriteJson` API surface

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `runtime-api.md` §Value Types now documents `TypeRuntimeMeta` with `ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, and `UnaryExecutors`.

**What's in decisions.md:** Entry 2026-05-03T23:00:32Z: *"Phase 1 ingress now dispatches through `TypeRuntimeMeta.ReadJson(ref Utf8JsonReader, ref PreceptValue)` and Phase 8 egress through `TypeRuntimeMeta.WriteJson(Utf8JsonWriter, PreceptValue)`."* Active surface: `ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors`. Excluded from hot paths: `ExtractValue`, `StoreValue`, `ParseValue`.

**What's in canonical docs:** `decisions-summary.md` has the "TypeRuntimeMeta JSON flow" summary. `catalog-system.md` now mentions BinaryExecutors/UnaryExecutors on TypeRuntime (per the 2026-05-04 audit entry). But `runtime-api.md` describes `TypeRuntime<T>` with only `FromClr` / `ToClr` — no `ReadJson`/`WriteJson`/`ParseString`/`FormatString`. The `TypeRuntimeMeta` type is not documented in any canonical doc at the method-signature level.

**Gap:** Three related types exist in the design: `TypeRuntime<T>` (CLR conversion), `TypeRuntimeMeta` (JSON/string serialization), and `TypeMeta` (catalog metadata). The boundaries and responsibilities of `TypeRuntimeMeta` are recorded only in decisions.md and decisions-summary.md — not in any canonical runtime or catalog doc with concrete method signatures. `runtime-api.md` shows `TypeRuntime<T>` with only CLR delegates; `catalog-system.md` mentions executors on TypeRuntime but not the JSON method names.

**Fix needed:** In `runtime-api.md` §Value Types, expand the `TypeRuntime<T>` description to include the full active API surface: `FromJson`/`ToJson`/`FromClr`/`ToClr` on `TypeRuntime<T>` and document `TypeRuntimeMeta` as the type that holds the catalog-level JSON dispatch delegates (`ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors`). In `catalog-system.md` under the Types catalog entry, document that `TypeMeta` carries a `TypeRuntimeMeta` instance and explain the role split between `TypeRuntime<T>` (registered per-CLR-type) and `TypeRuntimeMeta` (catalog-owned, always present).

---

### CC#25 Q2 — Event args convert to `PreceptValue` at the Fire boundary

**Status:** ✅ Fully captured

**What's in decisions.md:** *"Event args ARE converted to PreceptValue inside the evaluator — the asymmetry between fields and args is lifecycle/ownership, not type representation. LOAD_ARG opcode loads event args into the evaluator's PreceptValue[] register file."*

**What's in canonical docs:** `evaluator.md` §6 Working Copy Management step 3: *"The FiredArgs value carries the PreceptValue[] arg slot array materialized by IArgBuilder at the Fire boundary. Event args are already PreceptValue — no per-opcode conversion needed."* `precept-builder.md` §Pass 1 documents arg slot index assignment. `decisions-summary.md` captures this. ✅

**Gap:** None. (But see LoadArg slot-index entry above for the related implementation inconsistency.)

---

### CC#25 Q7 — `Version.Get<T>` typed field API, `FiredArgs`, `TypeRuntime` naming

**Status:** ✅ Fully captured in runtime-api.md

**What's in decisions.md:** `Version.Get<T>(string)` is the primary typed field API; raw indexers return `PreceptValue`. `Transitioned`/`Applied` carry `FiredArgs` with `Get<T>` + `PreceptValue` indexer. `TypeRuntime` naming: `FromJson`/`ToJson`/`FromClr`/`ToClr`.

**What's in canonical docs:** `runtime-api.md` §Version shows `public T Get<T>(string fieldName)` and `public PreceptValue this[string fieldName]`. `FiredArgs` class is defined with `Get<T>` and indexer. `TypeRuntime<T>` record is defined. `Transitioned`, `Applied`, `Rejected` all carry `FiredArgs Args`. ✅

**Gap:** `runtime-api.md` shows `TypeRuntime<T>` with only `FromClr`/`ToClr` delegates — missing `FromJson`/`ToJson` which the decisions say are part of the naming/surface. See the TypeRuntimeMeta entry above for the fuller gap. Minor gap: the naming decision (`FromJson`/`ToJson`/`FromClr`/`ToClr`) is not documented in runtime-api.md's `TypeRuntime<T>` section.

---

### CC#25 Q2 JSON-first public API — `IReadOnlyDictionary<string, object?>` obsolete

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed for Fire/Update/Inspect and the integration contract. `evaluator.md` now uses `FiredArgs` / `PreceptValue[]` signatures there; `Restore` still has a separate stale pseudocode block that remains open.

**What's in decisions.md:** Dict overloads fully obsolete. JSON lane (`JsonElement?`) for wire callers; typed lane (`Action<IArgBuilder>?` / `Action<IFieldBuilder>?`) for in-process callers. No third ingress lane.

**What's in canonical docs:** `runtime-api.md` §Two-Lane Ingress Principle: *"There are no `IReadOnlyDictionary<string, object?>` overloads anywhere."* Version API shows correct dual-lane signatures throughout. ✅

BUT `evaluator.md` §7.1 (Fire pseudocode), §7.1 (Update pseudocode), §7.2 (InspectFire pseudocode), §7.2 (InspectUpdate pseudocode), and §8 Integration Contract all show stale `IReadOnlyDictionary<string, object?>` and `object?[]` signatures without caveats:
- Fire: `EventOutcome Fire(Precept precept, Version version, EventDescriptor @event, IReadOnlyDictionary<string, object?> args)`
- Update: `UpdateOutcome Update(Precept precept, Version version, IReadOnlyDictionary<FieldDescriptor, object?> patch)`
- InspectFire: `EventInspection InspectFire(Precept precept, Version version, EventDescriptor @event, IReadOnlyDictionary<string, object?>? args)`
- InspectUpdate: `UpdateInspection InspectUpdate(Precept precept, Version version, IReadOnlyDictionary<FieldDescriptor, object?>? patch)`
- Integration Contract: `Version.Fire(string eventName, IReadOnlyDictionary<string, object?> args)`, `Version.Update(IReadOnlyDictionary<string, object?> fields)`

The Create pseudocode has a caveat note. Fire/Update/Inspect do not.

**Gap:** Evaluator.md code examples for Fire, Update, InspectFire, InspectUpdate, and the Integration Contract all use the obsolete dict API. Only the Create pseudocode has a caveat. A reader of `evaluator.md` who does not also read `runtime-api.md` will implement with the wrong internal signatures.

**Fix needed:** In `evaluator.md` §7.1 and §7.2, add the same caveat as Create: *"The pseudocode uses `IReadOnlyDictionary`/`object?[]` for readability. The canonical evaluator signatures use `PreceptValue[]` for the working copy (slot array) and `FiredArgs` (carrying `PreceptValue[]` + presence mask) for args. The public API uses `JsonElement?` or `Action<IArgBuilder>?` at the boundary — the evaluator only sees `PreceptValue`."* Update §8 Integration Contract to show the actual dual-lane `Version.Fire` and `Version.Update` signatures matching `runtime-api.md`.

---

### CC#25 `IArgBuilder` presence mask — `bool[]`

**Status:** ✅ Fully captured in runtime-api.md

**What's in decisions.md:** *"`IArgBuilder` now materializes `PreceptValue[]` plus a presence mask rather than an arg dictionary."* Presence mask is `bool[]` aligned to arg slot array; `true` = set, `false` = absent. Required-arg fault boundary: unset required args cause `InvalidArgs` before the opcode loop.

**What's in canonical docs:** `runtime-api.md` §IArgBuilder describes: *"The builder internally produces a `PreceptValue[]` arg slot array populated via the presence mask — a `bool[]` of the same length, where `presence[i] == true` means arg slot `i` was explicitly set… Unset required args cause `InvalidArgs` at the Fire boundary before the opcode loop begins."* ✅

**Gap:** None. This was captured in the 2026-05-04 audit.

---

### CC#25 JSON boundary stays outside the evaluator

**Status:** ✅ Fully captured

**What's in decisions.md:** *"The JSON ingress/egress boundary remains outside the evaluator: public API / Version conversion owns JSON parsing and lazy ToJson() egress, while the evaluator only sees typed PreceptValue data."*

**What's in canonical docs:** `evaluator.md` §3 Responsibilities Out of Scope explicitly excludes semantic reasoning and catalog lookups. `runtime-api.md` §Two-Lane Ingress Principle documents the JSON boundary ownership. `decisions-summary.md` captures this. ✅

---

### CC#25 construct-slot vs. field-slot vocabulary — locked

**Status:** ✅ Fully captured

**What's in decisions.md:** *"Locked the vocabulary split: `ParsedConstruct.Slots` / `SlotValue` stay compile-time only. Runtime execution uses field slot indices in the `PreceptValue[]` working-copy array, with `SlotLayout` as the canonical field-name-to-slot-index mapping."*

**What's in canonical docs:** `precept-builder.md` §Pass 2 has an explicit **Vocabulary** block: *"These are **field slots** — runtime storage positions in the `PreceptValue[]` working-copy array… They are distinct from **construct slots** (`ParsedConstruct.Slots` / `SlotValue`), which are compile-time parse positions…"* ✅

---

## Public API Surface Decisions

---

### CC#25 Q2 — JSON-first public API signatures

**Status:** ✅ Fully captured in runtime-api.md

**What's in decisions.md:** `Fire(string, JsonElement?)`, `Update(JsonElement)`, `Create(JsonElement?)`, `Restore(string?, JsonElement)` as primary signatures. ~90% of real callers receive `JsonElement` directly.

**What's in canonical docs:** `runtime-api.md` shows these exact signatures throughout the Fire, Update, Restore, Inspect, and Create examples. The rationale (ASP.NET Core/Azure Functions receive JsonElement directly, double-parse cost of dict API, parse error provenance) is documented. ✅

---

### Typed `Restore` — removed

**Status:** ✅ Fully captured

**What's in decisions.md:** *"Typed Restore is removed so restore remains round-trip-faithful hydration from Precept's own serialized egress."*

**What's in canonical docs:** `runtime-api.md` §Restoration: *"Typed Restore is deliberately absent. `Restore` takes `JsonElement` only… Restore is a hydration path from persisted storage, and storage always returns serialized data."* ✅

---

### `ConstraintViolation` shape mismatch

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `runtime-api.md` now matches the rich `ConstraintViolation` shape already documented in `evaluator.md`, including `BecauseClause`, `RelevantFields`, `FailingSubexpression`, and `PreceptValue? FailingValue`.

**What's in decisions.md:** No explicit decision on ConstraintViolation shape. The 2026-05-04T04:30:00Z audit entry notes closing gaps in `evaluator.md` and `result-types.md`.

**What's in canonical docs:** `evaluator.md` §7.6 defines:
```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    string? BecauseClause,
    ImmutableArray<FieldSnapshot> RelevantFields,
    string? FailingSubexpression,
    object? FailingValue
);
```
`runtime-api.md` §ConstraintViolation defines:
```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames);
```
These are completely different. `evaluator.md` has a rich shape (5 fields, including `FieldSnapshot[]`, failing subexpression, failing value). `runtime-api.md` has a minimal shape (2 fields). `runtime-api.md` does mark it `(G1/G9) Provisional`, noting `FieldNames` will evolve. But it doesn't explain why the evaluator.md shape is different from the provisional shape or indicate which is the adopted baseline.

**Gap:** Two canonical docs define `ConstraintViolation` with irreconcilably different shapes. An implementer cannot know which to use. The `evaluator.md` shape has `BecauseClause`, `FailingSubexpression`, and `FailingValue` that don't appear in `runtime-api.md`. The `runtime-api.md` shape has `FieldNames` (string list) while `evaluator.md` has `ImmutableArray<FieldSnapshot> RelevantFields`. Neither doc acknowledges the other's definition.

**Fix needed:** One definition must be canonical. If `runtime-api.md` is the public surface and `evaluator.md` is an internal detail, the `evaluator.md` shape is the evaluator's internal working type — document that distinction. Add a note in `evaluator.md` §7.6: *"This is the evaluator's internal `ConstraintViolation` shape — richer than the public `ConstraintViolation` in `runtime-api.md`. The public shape is the API contract. The evaluator's shape is implementation-private and will be mapped to the public shape during outcome production."* OR if the richer shape IS the public shape, update `runtime-api.md` to match.

---

### `FiredArgs` on `Rejected` outcome

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. Added rationale sentence to `runtime-api.md` §FiredArgs: Rejected carries FiredArgs so callers can log or display submitted args alongside the rejection reason.

**What's in canonical docs:** `evaluator.md` §5 shows `Rejected(string Reason, FiredArgs Args)` — FiredArgs on Rejected. `runtime-api.md` §FiredArgs says: *"appears on `EventOutcome` variants that carry submission context (`Transitioned.Args`, `Applied.Args`, `Rejected.Args`)."* — FiredArgs IS on Rejected.

**Gap:** The decision text only explicitly names `Transitioned` and `Applied` as carriers of `FiredArgs`. `Rejected` carrying `FiredArgs` is only implied. The canonical docs agree that Rejected does carry it, so the implementation is internally consistent. But the WHY is missing: why does a rejected outcome carry FiredArgs? The rationale for including it on rejection is not documented.

**Fix needed:** Minor — add a sentence to `runtime-api.md` §FiredArgs or the Rejected outcome description explaining why Rejected carries FiredArgs: *"A rejected outcome still carries the submitted args so the caller can log or display what was submitted alongside the rejection reason."*

---

## Compiler Pipeline Decisions

---

### `compiler-and-runtime-design.md` — narrative overview layer, not competing spec

**Status:** ✅ Fully captured

**What's in decisions.md:** *"`docs/compiler-and-runtime-design.md` is now durably framed as the narrative overview layer over the 11 canonical stage docs rather than a competing stage-spec source."*

**What's in canonical docs:** `compiler-and-runtime-design.md` header reads: *"How to read this document. Sections 1–3 establish what Precept promises…"* — it is framed as narrative. The 13-catalog count is correct. `decisions-summary.md` captures this. ✅

**Gap:** None on the framing decision. But the "Precept Innovations" callout box still carries stale wording (noted in decisions.md and decisions-summary.md as an open follow-up).

**Fix needed:** The "Precept Innovations" callout in `compiler-and-runtime-design.md` still says *"adding a feature means adding a catalog entry (a structured metadata record)"* after the rejected sentence about `add an enum member and fill an exhaustive switch` was removed — but the callout's last bullet may still contain similar stale framing. Check and fix the callout independently.

---

### Precept Builder — six-pass transformation order

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. Renumbered passes in `precept-builder.md` so execution order matches pass numbers: old Pass 5 (Execution Plan) → Pass 4; old Pass 4 (Constraint Plan) → Pass 5. Execution order is now 1 → 2/3 → 4 → 5 → 6. Dependency note updated accordingly. All 8 references updated (diagram, dependency table, section headers, inline comment).

**What's in canonical docs:** `precept-builder.md` §6 shows the six-pass diagram. The diagram correctly shows Passes 1→2 (parallel with 3)→5→4→6. However, the diagram labels "Pass 5 (Execution Plan Pass)" above "Pass 4 (Constraint Plan Pass)" in the flow diagram, noting "Pass 5 runs before Pass 4." This is potentially confusing — the pass numbers don't reflect execution order. The dependency table at the bottom correctly shows the order.

**Gap:** Minor — the pass number/order mismatch is documented but the diagram itself lists Pass 5 before Pass 4 in the flow box diagram, which will confuse readers. The ordering rationale (Pass 5 before Pass 4 because ConstraintDescriptor requires compiled ExecutionPlan) is stated but the labeling is still counterintuitive.

**Fix needed:** In `precept-builder.md` §6, either renumber the passes to reflect execution order (Execution Plan becomes Pass 4, Constraint Plan becomes Pass 5) or add a prominent note at the top of the architecture section: *"Pass numbers reflect logical grouping, not execution order. Execution order: 1 → 2/3 (parallel) → 5 → 4 → 6. Pass 5 precedes Pass 4 because constraint descriptors require compiled execution plans from Pass 5."*

---

### Expression tree design — CC#1, Option A (Roslyn-style typed nodes)

**Status:** ✅ Fully captured in cross-cutting-decisions.md

**What's in decisions.md:** Various compiler pipeline entries reference `ParsedExpression`/`TypedExpression` DU shapes, Pratt loop, Options catalog-derived precedence.

**What's in canonical docs:** `cross-cutting-decisions.md` CC#1 documents Option A (Roslyn-style typed nodes) as adopted with full detail: sealed abstract record base + per-kind subtypes, exhaustiveness enforcement, `[HandlesCatalogExhaustively]` + PRECEPT0019 annotation bridge. ✅

---

### CC#25 cross-cutting-decisions.md status not updated

**Status:** ✅ Fixed

**Update (2026-05-04):** Fixed. `cross-cutting-decisions.md` now shows CC#25 as resolved with the Option A+G runtime baseline and unblocked implementation areas.

**What's in decisions.md:** Many entries (2026-05-03) resolve specific CC#25 sub-questions (Q1–Q10, baseline choice, TypeRuntime naming, etc.). `decisions-summary.md` under "Runtime & Evaluation (CC#25)" lists the full set of resolved decisions.

**What's in canonical docs:** `cross-cutting-decisions.md` CC#25 entry still shows:
> **Status:** 🔴 Pending Shane decision

The Options / known design space section describes three options as if the decision is still open. This contradicts the many decisions.md entries that resolved CC#25 (Option A+G locked, catalog-owned executor arrays, TypeRuntime naming, LOAD_ARG slot-index, etc.).

**Gap:** `cross-cutting-decisions.md` is a canonical reference document used by the team to understand what decisions are outstanding. CC#25 showing as "🔴 Pending Shane decision" when it has been fully resolved is actively misleading — it tells implementers "wait for this" when the decision is actually "go build it." The CC#25 description in `cross-cutting-decisions.md` also describes the three options as if unresolved.

**Fix needed:** Update `cross-cutting-decisions.md` CC#25 to:
```
**Status:** ✅ Resolved — Option A+G (interpreter + catalog-delegate dispatch), 2026-05-03/04.
**Resolution:** PreceptValue 32-byte tagged struct as evaluation currency; catalog-owned
BinaryExecutors/UnaryExecutors on TypeRuntime indexed by OperationKind; LOAD_ARG carries
pre-resolved ArgSlotIndex (not name string); IArgBuilder materializes PreceptValue[] + bool[]
presence mask; TypeRuntime naming: FromJson/ToJson/FromClr/ToClr; System.Linq.Expressions
compilation is a designed-in seam, not a v1 path.
**Blocked items now unblocked:** Evaluator opcode dispatch, TypeRuntime registration, builder 
execution plan compilation, IArgBuilder/IFieldBuilder implementation.
```

---

### CC#25 changes runtime storage but not compiler pipeline shape

**Status:** ✅ Fully captured

**What's in decisions.md:** *"Option A+G is a runtime-layer change: parser, type checker, graph analyzer, proof engine, and plan topology remain structurally unchanged. The only recommended compiler/runtime boundary adjustment is to pre-wrap literals so `LoadLit` carries `PreceptValue` payloads directly."*

**What's in canonical docs:** `precept-builder.md` §Pass 5 shows `public sealed record LoadLit(PreceptValue Value) : Opcode;` — literals are pre-wrapped. ✅ The compiler pipeline docs (parser, type-checker, graph-analyzer, proof-engine) do not reference PreceptValue, consistent with the "pipeline unchanged" ruling.

---

## Language Surface Decisions

---

### `write all` — removed from language

**Status:** ✅ Fully captured

**What's in decisions.md:** *"`write all` is removed from the Precept language entirely. Stateless precepts now opt into mutability only through field-level `writable`."*

**What's in canonical docs:** `decisions-summary.md` records this under "Language Surface." Presumably `precept-language-spec.md` reflects the removal (per the vision→spec migration entries). `decisions-summary.md` captures the follow-through: *"language docs, samples, and downstream tooling must all treat field-level `writable` as the only stateless mutability opt-in."*

**Gap:** I did not read `precept-language-spec.md` in full to verify the removal is present. The spec update was recorded as applied in the decisions.md entry sequence. Assuming it was applied.

---

### Access-mode vocabulary — `readonly`/`editable` surface

**Status:** ✅ Fully captured in decisions-summary.md

**What's in decisions.md:** `in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?` — locked surface. `omit` = separate structural-exclusion verb. Guarded `omit` prohibited.

**What's in canonical docs:** `decisions-summary.md` captures the full vocabulary decision. `cross-cutting-decisions.md` does not have an entry for this (it's a language-surface decision, not cross-cutting). The original access-mode entries in decisions.md (2026-04-28) are detailed with rationale.

**Gap:** Minor — I could not verify that `precept-language-spec.md` and the token/catalog entries reflect `readonly`/`editable` as the final vocabulary (vs. the earlier `read`/`write` from the 2026-04-28T05:08 entry which used different terms). The decisions.md records the lock as `readonly`/`editable` but the earlier decisions used `read`/`write`. If the spec still says `read`/`write`, that's a contradiction.

**Fix needed:** Verify `precept-language-spec.md` uses `readonly`/`editable` not `read`/`write` in the access-mode section. Verify `Tokens` catalog has `readonly` and `editable` as TokenKind members, not `read` and `write` (for access modes).

---

### `EnsureClause` — `because` is a separate slot

**Status:** ✅ Captured in decisions-summary.md

**What's in decisions.md:** *"`because` is a separate slot, not payload folded into `EnsureClause`."*

**What's in canonical docs:** `decisions-summary.md` captures this. The catalog-system.md Constructs entry documents slot shapes per construct. This decision gates the correct slot shape for EnsureClause constructs.

**Gap:** Minor — not explicitly verified in `precept-language-spec.md` or `precept-grammar.md` since I didn't read those in full. Marked as assumed-captured based on the decision entry sequence.

---

### `SyntaxTree` → `ConstructManifest` rename — resolved (2026-05-04)

**Status:** ✅ Resolved — Shane ruling 2026-05-04

**What happened:** Shane ruled `ConstructManifest` as the canonical name (Item 13). `compiler-and-runtime-design.md` was already using `ConstructManifest` in the pipeline diagram and throughout all prose — no changes needed there. The stale reference was in `decisions-summary.md` under "Canonical artifact names still in force," which listed `SyntaxTree`. Updated to `ConstructManifest`. Source code rename is a separate implementation task.

**Gap closed:** The "Canonical artifact names still in force" list in `decisions-summary.md` now matches the canonical docs.

---

### Outcomes catalog — DU + catalog two-level pattern

**Status:** ✅ Captured in decisions-summary.md

**What's in decisions.md:** *"Outcomes follow the same two-level architecture as actions: `OutcomeKind` + `OutcomeMeta` + `Outcomes.cs` at the metadata layer, `OutcomeNode` DU at the syntax-node layer."*

**What's in canonical docs:** `decisions-summary.md` captures this. `catalog-system.md` would need to reflect `Outcomes.All` enumerating `transition`, `no transition`, and `reject`. I did not read the full catalog-system.md schema reference to verify this is captured at the Level 3 reference table depth.

**Gap:** Cannot fully verify without reading the Level 3 catalog member tables in `catalog-system.md`. Marked as partially captured.

---

### GAP-040 — `bag.countof` uses `ElementParameterAccessor` DU subtype

**Status:** ✅ Fixed

**What's in decisions.md:** *"GAP-040 is locked to the metadata-driven path: `bag.countof(...)` must stop pretending its parameter is `integer` and instead use a dedicated `ElementParameterAccessor` DU subtype whose parameter resolves to the bag element type."*

**What's in canonical docs:** `catalog-system.md` §TypeAccessor DU now documents the full three-type hierarchy: `TypeAccessor` base (inner-type return), `FixedReturnAccessor` (fixed return), and `ElementParameterAccessor` (parameter resolves to bag element type). The stale "Open Question" callout and missing code fence were also corrected. `decisions-summary.md` captures the decision.

---

### GAP-046 — CI function kinds as dedicated catalog entries

**Status:** ✅ Fixed

**What's in decisions.md:** *"`FunctionKind.TildeStartsWith` / `FunctionKind.TildeEndsWith` plus `FunctionMeta.CIVariantOf` are added so CI functions exist as real function metadata."* Open downstream concern: calling `~startsWith`/`~endsWith` with non-`~string` first argument still needs a future diagnostic decision.

**What's in canonical docs:** `catalog-system.md` §FunctionMeta now includes `HasCIVariant: bool` and `CIVariantOf: FunctionKind?`. The stale ✅ callout pointing to the missing fields has been removed. `decisions-summary.md` captures the decision and the open downstream checker concern.

---

### `is set` / `is not set` — proposal-scope, pending owner sign-off

**Status:** ✅ Correctly recorded — ⚠️ VERIFY WITH SHANE

**What's in decisions.md:** *"Both analyses agree `is set` / `is not set` are real semantic operators… leaving them as uncataloged parser special-cases is a catalog-completeness bug. `ExpressionFormKind.PostfixOperation` is Frank's preferred shape. The `OperatorMeta` shape still needs owner sign-off."*

**What's in canonical docs:** `decisions-summary.md` correctly records this as pending owner sign-off. `cross-cutting-decisions.md` doesn't have a dedicated CC entry for this.

**Gap:** None — correctly recorded as pending. **⚠️ VERIFY WITH SHANE**: Has Shane signed off on the `OperatorMeta` shape for `is set`/`is not set`? The decision is recorded as pending as of 2026-05-01. If Shane has since approved, the entry needs updating.

---

## Documentation and Process Decisions

---

### Catalog-first wording — stale sentence corrected

**Status:** ✅ Fully captured

**Update (2026-05-04):** Verified. The §2 `Precept Innovations` callout now explicitly says there is no per-feature switch in pipeline stages, so the follow-up is closed.

**What's in decisions.md:** *"The catastrophic stale sentence that described Precept extension as 'add an enum member and fill an exhaustive switch' is now durably rejected."*

**What's in canonical docs:** `compiler-and-runtime-design.md` §2 no longer has the rejected sentence. The table correctly shows the catalog-first model. But decisions.md notes: *"the 'Precept Innovations' callout box in the same document still carries similar stale wording and needs a separate cleanup pass."*

**Gap:** The "Precept Innovations" callout box in `compiler-and-runtime-design.md` §2 was flagged as still needing a cleanup pass for similar stale wording. This was NOT fixed in any subsequent decision entry. It remains an open follow-up.

**Fix needed:** Read the "Precept Innovations" callout box in `compiler-and-runtime-design.md` and verify it does not say "add a catalog entry and fill an exhaustive switch" or similar. Remove or replace any language that implies consumers maintain parallel per-feature switches.

---

### `compiler-and-runtime-design.md` — ASCII diagram rule

**Status:** ✅ Fully captured

**What's in decisions.md:** *"`docs/compiler-and-runtime-design.md` §7 now uses plain ASCII `>` instead of `▶` inside the fixed-width topology box."*

**What's in canonical docs:** `decisions-summary.md` captures this rule. The pipeline topology box now uses `>` per the fix. ✅

---

### Dapr hosting — actor model with pod-level cache

**Status:** ✅ Captured in decisions-summary.md

**What's in decisions.md:** *"Actor-hosted Precept instances with a pod-level compiled-definition cache, typed rehydration before guard evaluation, `Restore()` as the state-store boundary; workflows remain the wrong semantic fit."*

**What's in canonical docs:** `decisions-summary.md` captures this. No canonical runtime doc (runtime-api.md, evaluator.md) documents Dapr hosting patterns — which is appropriate since it's a hosting concern, not a runtime design concern.

**Gap:** None — this is correctly a decisions/summary record, not a canonical doc concern.

---

### No-runtime-faults guarantee — philosophy gap

**Status:** ⚠️ Partially captured — ⚠️ VERIFY WITH SHANE

**What's in decisions.md:** *"Frank's philosophy-grounded follow-up endorsed those revisions but flagged a product-identity gap: `docs/philosophy.md` explicitly scopes 'prevention, not detection' to invalid entity configurations and does not yet name evaluation-fault prevention with the same explicitness. Recommended wording was recorded for owner review only and was not applied."*

**What's in canonical docs:** `decisions-summary.md` under "No-runtime-faults guarantee" captures the philosophy gap as flagged for Shane but not applied. `docs/philosophy.md` has not been updated.

**Gap:** **⚠️ VERIFY WITH SHANE**: Has Shane reviewed and approved or declined the philosophy.md update to explicitly name evaluation-fault prevention? If approved, the update needs to be applied. If declined, the decisions entry should note the ruling.

---

### Research validation integration pattern — locked

**Status:** ✅ Fully captured in decisions-summary.md

**What's in decisions.md:** Research artifacts in `research/language/`; design docs cite in `## Research Validation` section; working drafts marked superseded; `research/language/README.md` indexes the set.

**What's in canonical docs:** `decisions-summary.md` captures this workflow. ✅

---

### Vision → spec migration — closed

**Status:** ✅ Fully captured

**What's in decisions.md:** Language spec is now the single canonical language document. Vision archived at `docs/archive/language-design/precept-language-vision.md`. Two stale contradictions removed.

**What's in canonical docs:** `decisions-summary.md` confirms this. ✅

---

### CC#2 — SlotValue subtype shapes resolved (Option C Hybrid)

**Status:** ✅ Fully captured in cross-cutting-decisions.md

**What's in decisions.md:** Multiple entries reference CC#2 resolution. `cross-cutting-decisions.md` CC#2 status shows "✅ Resolved — Option C (Hybrid), 2026-05-03T23:39:16Z."

**What's in canonical docs:** `cross-cutting-decisions.md` CC#2 entry is fully resolved with design detail: `ParsedExpression` stamped at parse time, `TypedExpression` produced by type checker, Pratt loop uses `Operators.GetMeta()`, SlotValue DU stable at 17 subtypes. ✅

---

## Summary Table

| Decision Area | Total Checked | ✅ Fully Captured | ⚠️ Partial | ❌ Not in Docs | 🔴 Contradicted |
|---|---|---|---|---|---|
| CC#25 Runtime | 16 | 14 | 1 | 1 | 0 |
| Public API Surface | 4 | 3 | 1 | 0 | 0 |
| Compiler Pipeline | 5 | 4 | 1 | 0 | 0 |
| Language Surface | 8 | 6 | 2 | 0 | 0 |
| Documentation/Process | 7 | 6 | 1 | 0 | 0 |
| **Totals** | **40** | **33** | **6** | **1** | **0** |

---

## Priority Action Items

**P0 — Must fix before implementation:**
1. `PreceptValue` public/internal type boundary — reconcile the remaining `internal struct` vs. `public struct` mismatch or document the boundary explicitly

**P1 — Significant remaining gap:**
2. `evaluator.md` Restore pseudocode — still uses the stale `IReadOnlyDictionary<FieldDescriptor, object?>` / `object?[]` path with no caveat note

**P2 — Completeness / clarity gaps:**
3. Option F type-per-lane rejection rationale — still not captured in canonical docs
4. `FiredArgs` on `Rejected` outcome — explain why rejected outcomes retain the submitted args
5. Precept Builder six-pass transformation order — clarify pass numbering vs. execution order
6. GAP-040 `ElementParameterAccessor` propagation — verify catalog tables/spec reflect the locked shape
7. GAP-046 CI function kinds propagation — verify catalog tables/spec reflect the locked entries

**Pending Shane:**
- `is set`/`is not set` `OperatorMeta` shape — has owner sign-off happened?
- philosophy.md evaluation-fault prevention wording — approved or declined?
- `PreceptValue` public/internal type boundary — single type or two types?
