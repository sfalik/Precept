# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-05-04T05:45:56Z: Audit-gap P2 clarifications recorded; compiler/runtime innovation callouts confirmed clean

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-p2-doc-fixes.md`.

- `docs/runtime/evaluator.md` §4 now records both the `TypeBuilder` rejection rationale and the stable compiled-path upgrade seam against the existing A+G execution contract.
- `docs/runtime/evaluator.md` §7.3 now clarifies that per-type `TypeRuntimeMeta.BinaryExecutors` / `UnaryExecutors` are registered into flat `Operations` arrays, preserving zero-knowledge O(1) dispatch inside the evaluator.
- `docs/compiler-and-runtime-design.md` required no edit for Item 14; all `Precept Innovations` callouts already match the single-interpreter, catalog-dispatch architecture.

---

### 2026-05-04T05:45:56Z: Decision ledger summary created as a non-canonical navigation aid

**By:** Scribe

**Status:** Recorded from inbox note.

**Merged source:** `frank-decisions-summary.md`.

- `docs/working/decisions-summary.md` was added as a scanning aid over `.squad/decisions.md`.
- The durable source of truth remains `.squad/decisions.md`; the summary is reference-only and does not supersede the ledger.

---

### 2026-05-04T05:44:10Z: `ConstraintViolation` public contract promoted to the 5-field rich shape

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-constraint-violation-promoted.md`.

- Shane's 2026-05-04 ruling promotes the 5-field `ConstraintViolation` shape from `docs/runtime/evaluator.md` §7.6 to the public runtime contract; the earlier 2-field minimal shape is superseded.
- `FailingValue` is `PreceptValue?`, not `object?`; CLR callers convert through `TypeRuntime<T>.ToClr` rather than through evaluator-owned boxing.
- `docs/runtime/runtime-api.md` now documents the public 5-field shape, and the evaluator docs frame the remaining work as implementation follow-through rather than as an unresolved contract question.

---

### 2026-05-04T05:31:45Z: Evaluator pseudocode and §8 integration contract aligned to `FiredArgs` / `PreceptValue` lanes

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-evaluator-pseudocode-fix.md`.

- `docs/runtime/evaluator.md` pseudocode now uses the actual CC#25 internal types throughout: `FiredArgs`, `PreceptValue[]`, `PreceptValue[]? patch`, and `version.Slots.ToArray()`.
- Update patch application and access-mode checks are slot-indexed, `FiredArgs.Empty` replaces the stale standalone `EmptyArgs`, and the old `object?[]` readability caveat is removed.
- §8 now documents the durable dual-lane public contract: both JSON ingress and CLR-builder ingress materialize `FiredArgs`, and the evaluator never consumes raw dictionaries or raw JSON.

---

### 2026-05-04T01:45:56Z: ConstructManifest name confirmed as the working-doc parser artifact label

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-construct-manifest-rename.md`.

- The working docs now use `ConstructManifest` consistently for the parser output artifact, matching the already-correct pipeline diagram and earlier canonical rename decisions.
- Scope remains documentation only; any source-code rename is a separate implementation task.

---

### 2026-05-04T01:08:14Z: Dual-interpreter model rejected; trace stays inside the single A+G interpreter

**By:** Shane (via Copilot)

**Status:** Recorded from inbox correction merge.

**Merged source:** `frank-trace-correction.md`.

- Rejected: a production A+G runtime paired with a separate LS/MCP tree-walk interpreter.
- Adopted instead: one stack-based opcode interpreter serves every consumer, with optional per-step trace emission for tooling and diagnostics.
- Trace record shape and LS/MCP consumption remain open implementation seams, but the architecture no longer permits a second semantic engine.

---

### 2026-05-04T04:36:09Z: Deep content audit filled seven specificity gaps in canonical docs

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-deep-content-audit.md`.

- Filled seven specificity gaps across `docs/runtime/evaluator.md`, `docs/language/catalog-system.md`, and `docs/runtime/runtime-api.md`, while confirming adjacent runtime surfaces that were already correct.
- `evaluator.md` now gives `PreceptValue` a full performance-and-memory section (GC rationale, the 32-byte tagged-value rationale, and the hot-path memory picture around 44–48 slots / ~4,480 bytes), expands Fire to a 7-step lifecycle, corrects `LOAD_ARG` to slot-index dispatch, and replaces stale `object?` executor examples with canonical `stackalloc PreceptValue[32]` examples.
- `catalog-system.md` now places `BinaryExecutors` and `UnaryExecutors` on `TypeRuntime` and explains executor-array dispatch as catalog-owned runtime behavior rather than evaluator-owned switches.
- `runtime-api.md` now defines the arg presence mask concretely as a `bool[]` aligned to the arg slot array and documents the required-arg fault boundary.
- Open design questions stay explicit in the canonical docs: `PreceptValue` FieldOffset layout, `ArgDescriptor.SlotIndex`, and the executor registration / assembly mechanism.

---

### 2026-05-04T04:30:00Z: Full CC#25 / CC#2 decisions audit closed the remaining five canonical doc gaps

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-audit-report.md`.

- Audited CC#25 Q1–Q10, CC#2, and the `PreceptValue` slot-storage follow-through across canonical runtime and compiler docs; confirmed Q1, Q2, Q4, Q7, and Q10 were already covered.
- Closed the lagging doc gaps in `docs/runtime/evaluator.md`, `docs/runtime/result-types.md`, `docs/runtime/precept-builder.md`, `docs/compiler-and-runtime-design.md`, and `docs/working/cross-cutting-decisions.md`.
- Durable audit rule: after runtime API updates, re-audit `result-types.md`, `evaluator.md`, and the cross-cutting register together so locked decisions do not leave stale `object?`, dictionary, or pending-status language behind.
- Open flags stay explicit: `TypeRuntime<T>` documentation reconciliation and the non-expression `SlotValue` shape conflicts still need owner direction.

---

### 2026-05-04T04:02:05Z: Catalog gap register migration completed and archived

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files).

**Merged sources:** `frank-chunk4-gaps.md`, `frank-chunk4-unplaced-gaps.md`.

- The 43 entries from the catalog gap register are now fully triaged: 23 pending gaps were attributed into canonical Open Question blocks across 9 docs, 3 resolved-in-source gaps were marked closed, 5 already-captured gaps were confirmed in place, and gap #39 was promoted to a first-class open-question block.
- Cross-cutting routing is durable: 8 entries stay owned by the cross-cutting register, while 4 out-of-scope items remain runtime/MCP/tooling design questions rather than catalog metadata gaps.
- The working register was retired: `docs/working/catalog-gap-register.md` now lives at `docs/working/Archived/catalog-gap-register-migrated.md`, preserving the original content plus a migration notice.

---

### 2026-05-04T03:26:10Z: CC#25 Q7 acceptance revision locked

**By:** Scribe

**Status:** Recorded from the full Q7 acceptance inbox merge.

**Merged sources:** `frank-cc25-q7-typed-api.md`, `frank-cc25-q7-accepted.md`, `frank-cc25-q7-challenges.md`, `copilot-cc25-q7-ingress-egress.md`, `copilot-directive-20260503-231016.md`, `copilot-directive-20260503-231158-no-string-json-overloads.md`.

- Q7 is now fully accepted. `Version.Get<T>(string)` is the primary typed field API, raw indexers return `PreceptValue`, and `Transitioned` / `Applied` carry `FiredArgs` with the same `Get<T>` + `PreceptValue` indexer pattern for event-arg egress.
- `TypeRuntime` naming is final: `FromJson` / `ToJson` / `FromClr` / `ToClr`. `TypeRuntime<T>` is the zero-boxing CLR ingress/egress path, and typed `Get<T>` / `Set<T>` dispatch through those delegates.
- Typed ingress is fluent and AOT-safe: `Fire()` / `Inspect()` use `Action<IArgBuilder>`, `Create()` uses `Action<IFieldBuilder>`, and `IArgBuilder` now materializes `PreceptValue[]` plus a presence mask rather than an arg dictionary.
- JSON boundaries stay `JsonElement`-only. No string convenience overloads exist anywhere on the JSON API surface, and typed `Restore` is removed so restore remains round-trip-faithful hydration from Precept's own serialized egress.
- The JSON ingress/egress boundary remains outside the evaluator: public API / `Version` conversion owns JSON parsing and lazy `ToJson()` egress, while the evaluator only sees typed `PreceptValue` data.
- This supersedes the earlier provisional note that `IReadOnlyDictionary<string, object?>` would survive as a convenience extension lane.

---

### 2026-05-04T03:26:10Z: CC#25 Q7 dictionary convenience lane closed

**By:** Shane (via Copilot)

**Status:** Recorded from inbox closeout.

**Merged source:** `copilot-cc25-q7-dict-extension-obsolete.md`.

- `IReadOnlyDictionary<string, object?>` convenience overloads and extension methods are fully obsolete. They are not part of the main API, not a test-only helper lane, and not a future convenience surface.
- Wire-format callers use `JsonElement`; in-process typed callers use the fluent builders. No third ingress lane remains.

---

### 2026-05-03: CC#25 Q2 — Event Args + JSON-First Public API (LOCKED)

**By:** Scribe

**Status:** Recorded from spawn manifest plus inbox merge closeout.

**Merged sources:** `frank-57` (already durable in ledger), `frank-json-first-api.md`, `frank-59` (inline manifest result).

**Decision:** Q2 is resolved. Event args ARE converted to PreceptValue inside the evaluator — the asymmetry between fields and args is lifecycle/ownership, not type representation. LOAD_ARG opcode loads event args into the evaluator's PreceptValue[] register file.

**Public API amendment (JSON-first):** The public API switches to JsonElement as the primary type for all data/args parameters.

Primary signatures:
```csharp
EventOutcome  Fire(string eventName, JsonElement? args = null)       // on Version
UpdateOutcome Update(JsonElement fields)
EventOutcome  Create(JsonElement? args = null)                       // on Precept
RestoreOutcome Restore(string? state, JsonElement fields)
```

Dictionary overloads (IReadOnlyDictionary<string, object?>) are demoted to convenience extension methods for tests/in-process callers only.

**Rationale:** ~90% of real callers (ASP.NET Core, minimal APIs, Azure Functions) receive JsonElement directly from the framework. The dictionary API forced double-parse on every wire-format caller. JsonElement flows straight from HTTP request body to Fire() with zero intermediate allocations. Parse errors carry position info from the original payload. The dictionary API loses that provenance.

**Doc impact:** runtime-api.md will be updated in the implementation PR that ships this change (not now — docs track what exists in the runtime).

**Accepted by:** Shane Falik

---

### 2026-05-04T00:56:54Z: CC#25 construct-slot vs field-slot vocabulary boundary locked

**By:** Scribe

**Status:** Recorded from spawn manifest (Shane accepted; paired with same-pass inbox merge closeout)

**Merged sources:** `frank-56`, `frank-q1-slots` (manifest record).

- Locked the vocabulary split between parser-time construct slots and runtime field slots: `ParsedConstruct.Slots` / `SlotValue` stay compile-time only.
- Runtime execution uses field slot indices in the `PreceptValue[]` working-copy array, with `SlotLayout` as the canonical field-name-to-slot-index mapping built during `Precept.From()`.
- Durable wording rule: when discussion crosses parser and runtime layers, explicitly say **construct slots** vs **field slots** because the two concepts do not share lifecycle, representation, or owner.

---

### 2026-05-04T00:56:54Z: CC#25 event args convert to `PreceptValue` at the Fire boundary

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-q2-event-args`.

- Event args are schema-defined and typed; the evaluator consumes them as `PreceptValue` via `LOAD_ARG`, so the asymmetry is about ingress timing, not runtime representation.
- Field data converts into persistent field slots at version construction/restore, while event args validate and convert at Fire entry into ephemeral per-call arg slots.
- The remaining open seam is allocation strategy for that arg slot array; it is not a design question about whether args become `PreceptValue`.

---

### 2026-05-03T23:00:32Z: CC#25 TypeRuntimeMeta JSON flow locks to symmetric `ReadJson`/`WriteJson` API

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-readwrite-json-api`.

- Phase 1 ingress now dispatches through `TypeRuntimeMeta.ReadJson(ref Utf8JsonReader, ref PreceptValue)` and Phase 8 egress through `TypeRuntimeMeta.WriteJson(Utf8JsonWriter, PreceptValue)`, replacing `StoreValue` / `ParseValue` / `FormatValue` on the hot JSON path.
- Zero-boxing scope is locked precisely: scalar fields read and write the inline value region directly, while string, NodaTime, and collection values stay in the ref region and are written back by reference instead of re-boxed intermediaries.
- Ownership rules are durable: the call site advances to the value token and handles `null`, collection runtimes own structural array/object loops, and the active `TypeRuntimeMeta` surface is `ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, and `UnaryExecutors`, with `ExtractValue` / `StoreValue` / `ParseValue` excluded from Fire, Inspect, and Update hot paths.

---

### 2026-05-03T22:22:27Z: CC#25 runtime baseline is `PreceptValue` plus catalog-owned delegate dispatch

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (7 files; final recommendation and superseded explorations normalized).

**Merged sources:** `frank-cc25-recommendation`, `frank-cc25-neutral-rerun`, `frank-cc25-same-process-reanalysis`, `frank-cc25-boxing-and-dispatch`, `frank-cc25-il-emission-radical`, `frank-cc25-vm-free-analysis`, `frank-cc25-creative-options`.

- Durable runtime choice: production Fire uses Option A + G — a 32-byte `PreceptValue` tagged struct on the evaluation stack and `Version.Slots`, with catalog-owned unary/binary executor arrays indexed by `OperationKind` so the evaluator stays zero-knowledge.
- The decisive performance variable is representation, not dispatch: replacing boxed `object?` arithmetic with `PreceptValue` removes the projected ~768 MB/s gen-0 pressure at 100k events/sec while leaving delegate-array dispatch in the noise.
- `System.Linq.Expressions` / compiled-path work stays a designed-in upgrade seam, not a v1 dual-path architecture; v1 ships the interpreter-shaped A+G runtime only.
- Same-process deployment matters: catalog delegate arrays are JIT-warm and fixed for the process lifetime, so there is no plugin-style indirection penalty that would justify reopening dispatch around slower but more complex alternatives.

---

### 2026-05-03T22:22:27Z: CC#25 TypeBuilder-generated CLR types are rejected for the SaaS runtime baseline

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files; recommendation reversal captured durably).

**Merged sources:** `frank-cc25-typegen-analysis`, `frank-cc25-sourcegen-contrast`, `frank-cc25-saas-runtime`.

- TypeBuilder's warm-path throughput and earlier executor validation are real advantages, but they do not survive the actual product constraints driving CC#25.
- The blocking constraint is SaaS cold-start and per-definition churn: hundreds of milliseconds of compile work on upload, cache miss, or deployment is incompatible with the save-and-test loop, while A+G stays sub-millisecond to stand up.
- Inspectability is a product guarantee, not an optional debugger convenience; TypeBuilder would require a second interpreted or tracing-decorator path to recover per-step explanations that A+G already exposes naturally.
- Durable boundary: do not treat TypeBuilder or build-time codegen as the implicit v2 path unless the deployment model or inspectability requirement changes first.

---

### 2026-05-03T22:22:27Z: CC#25 type-per-lane storage loses to unified `PreceptValue`

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files; revised analysis supersedes the original Option F memo).

**Merged sources:** `frank-cc25-option-f-lanes`, `frank-cc25-option-f-lanes-revised`.

- Option F's split-lane model does not materially reduce the hard cases because 23 of 32 `TypeKind` members still live in the reference lane and the business-domain types remain cross-lane participants.
- Adding a wider business-value lane only recreates `PreceptValue`'s struct-copy cost without gaining the unified operation surface that makes A+G simple.
- The NodaTime/date-time correction changes details but not the verdict: the lane split still adds routing complexity for no meaningful reduction in cross-lane operations.

---

### 2026-05-03T22:22:27Z: CC#25 interactive tooling keeps traced tree-walk evaluation while production stays typed-opcode based

**By:** Scribe

**⚠️ SUPERSEDED — 2026-05-04.** This entry recorded an exploration, not the adopted design. The dual-consumer model was explicitly rejected by Shane. See the 2026-05-04 correction entries below. The correct durable decision is: **single interpreter with diagnostic trace** — one A+G stack-based opcode executor serves ALL consumers. See `evaluator.md` §11 Decision 8.

**Status (original):** Merged, deduplicated, inbox cleared (4 files; parallel follow-up analyses converged on the same split).

**Merged sources:** `frank-cc25-treewalk`, `frank-cc25-spanstack`, `frank-cc25-jsonreader`, `frank-cc25-optionc`.

- ~~Durable dual-consumer model: production Fire/Inspect/Update uses the A+G typed-opcode runtime, while LS/MCP interactive tooling keeps a `TypedExpression` tree-walk path for rich per-node traces and sub-50 ms authoring feedback.~~ **REJECTED — see correction.**
- JSON-native or span-stack evaluation is rejected as the production stack currency: every serious precedent deserializes to typed values before computation, and parse/format cost swamps any zero-copy story for numeric work.
- Good ideas harvested from the rejected variants stay additive: explicit TypeKind→CLR mapping metadata and string-stack / JSON-friendly techniques remain valid **only as inspiration, not as a separate interpreter path**.

---

### 2026-05-03T22:22:27Z: CC#25 extends the Types catalog with owned JSON serialization delegates

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-cc25-catalog-owned-storage`.

- `TypeMeta` gains catalog-owned JSON reader/writer delegates so serialization follows the same metadata-owned behavior pattern as execution dispatch.
- Collection-field serializers are composed once at build time from structural collection logic plus element-type delegates, keeping runtime streaming, reflection-free, and free of `JsonElement` or `object` fallback paths.
- Durable architecture rule: persistence behavior belongs on catalog metadata; do not reintroduce per-`TypeKind` consumer switches in serializer code.

---

### 2026-05-03T22:22:27Z: CC#25 changes runtime storage and literal loading, not the compiler pipeline shape

**By:** Scribe

**Status:** Merged, inbox cleared (1 file).

**Merged sources:** `frank-cc25-compiler-output-impact`.

- Option A+G is a runtime-layer change: parser, type checker, graph analyzer, proof engine, and plan topology remain structurally unchanged.
- The only recommended compiler/runtime boundary adjustment is to pre-wrap literals so `LoadLit` carries `PreceptValue` payloads directly instead of constructing them in the evaluator loop.
- Durable boundary: execution plans keep serializable catalog indices and never embed delegate instances.

---

### 2026-05-03T22:22:27Z: CC#25 Fire-call lifecycle is now quantified as the A+G implementation baseline

**By:** Scribe

**Status:** Merged, inbox cleared (1 file; reference walkthrough promoted to durable implementation baseline).

**Merged sources:** `frank-cc25-fire-data-flow`.

- The full Fire walkthrough establishes the hot-path memory picture for one event under A+G: peak live slot footprint is ~44-48 `PreceptValue` slots, total stack traffic is ~4,480 bytes per Fire, and the working copy is the donated next-version slot array rather than a throwaway buffer.
- With slot-array pooling, GC-visible allocation drops to the unavoidable boundary objects (about ~88 bytes in the walkthrough), while scalar evaluation itself stays zero-boxing throughout the pipeline.
- The walkthrough also locks the next implementation questions to six concrete seams: slot-array ownership transfer, eval-stack allocation strategy, JSON ingress/egress ownership, event-args representation, trace-path data structures, and multi-row working-copy pooling.

---

### 2026-05-03T14:59:24Z: Per-stage pipeline topology boxes stay ASCII-safe in compiler-and-runtime docs

**By:** Scribe

**Status:** Merged, inbox cleared (1 file)

**Merged sources:** `elaine-diagram-fix`.

- `docs/compiler-and-runtime-design.md` §7 now uses plain ASCII `>` instead of `▶` inside the fixed-width topology box so monospace alignment stays stable across renderers.
- Durable diagram rule: when box geometry depends on character columns, prefer ASCII arrowheads over ambiguous-width Unicode glyphs even if the Unicode form looks nicer in rich editors.


---

### 2026-05-03T14:59:24Z: ConstructManifest cleanup closed both tree-variable drift and stale doc type names

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)

**Merged sources:** `frank-tree-rename`, `frank-syntaxtree-doc-sweep`.

- The earlier `tree` → `manifest` cleanup and the follow-up `SyntaxTree` type-name sweep are now treated as one closed rename-follow-through track for Precept-owned docs and examples.
- Durable boundary: keep legitimate Roslyn `SyntaxTree`, generic parse-tree prose, and graph-theory `dominator tree` references untouched, but use `ConstructManifest` / `manifest` for the flat Precept parser artifact where that rename has shipped.
- The requested docs (`docs/compiler/type-checker.md`, `docs/compiler/README.md`) plus adjacent active surfaces were swept clean, and the earlier doc-follow-up from the variable-rename pass is no longer an open item for that targeted set.


---

### 2026-05-03T14:59:24Z: EnsureClause reason text stays in its own BecauseClause slot

**By:** Scribe

**Status:** Recorded from spawn manifest (no inbox file)

**Merged sources:** `frank-30` (spawn manifest direct verdict).

- `because` is a separate slot, not payload folded into `EnsureClause`; `BecauseClause = 13` already exists and `RuleDeclaration` remains the reference shape.
- `StateEnsure` and `EventEnsure` treating `because` as anything other than a dedicated slot is a catalog defect to correct, not an accepted alternate model.
- Durable modeling rule: when ensure syntax carries explanatory reason text, that reason is represented by its own named slot.


---

### 2026-05-03T14:59:24Z: Event modifiers remain individually slotted as InitialMarker

**By:** Scribe

**Status:** Recorded from spawn manifest (no inbox file)

**Merged sources:** `frank-31` (spawn manifest direct verdict).

- Keep `InitialMarker` as the individual named slot for the current event-modifier surface; do not invent a collective event-modifier slot abstraction.
- `terminal` remains `StateModifierMeta`, not an event modifier, and the present catalog has only one `EventModifierMeta` member.
- Durable catalog rule: only group event modifiers behind a collective slot when multiple real event-modifier members exist and share metadata-driven behavior.


---

### 2026-05-03T14:37:24Z: Grammar anatomy section stays representative and now covers the missing slot/routing archetypes

**By:** Scribe

**Status:** Merged, inbox cleared (1 file)

**Merged sources:** `elaine-concrete-anatomy-coverage`.

- `docs/language/precept-grammar.md` §3 stays representative rather than exhaustive; anatomy examples exist to cover distinct slot and routing archetypes, not every construct kind.
- The selected expansion set is `PreceptHeader`, `RuleDeclaration`, `AccessMode`, `StateAction`, and `EventHandler`; `OmitDeclaration` and `EventEnsure` remain intentionally omitted because their slot shapes are already legible from the chosen set.
- Durable framing rule: describe §3 as coverage of distinct slot/routing archetypes.

---


---

### 2026-05-03T14:18:15Z: SyntaxTree rename target preference locked to ConstructManifest


**By:** Scribe


**Status:** Merged, owner preference captured, inbox cleared (1 file + direct owner preference)


**Merged sources:** `frank-syntaxtree-rename`, `owner-preference-constructmanifest`.


- Shane's recorded preference supersedes Frank's ParsedSource recommendation: if `SyntaxTree` is renamed, the preferred target is `ConstructManifest`.

- Durable status: this pass captures naming guidance only; no source or documentation rename was executed here.

- `ParsedSource` remains recorded as the superseded advisory alternative rather than the current preferred target.


---


---

### 2026-05-03T14:18:15Z: Compiler overview confirmation notes deduplicated into existing sync decisions


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files)


**Merged sources:** `frank-compile-sketch-fix`, `frank-overview-doc-full-pass`.


- Frank's full-pass notes confirm the canonical top-level artifact names still in force for the current design set: `TokenStream`, `SyntaxTree`, `ParsedConstruct`, `SemanticIndex`, `StateGraph`, `ProofLedger`, `Compilation`, and `Precept`.

- The compile-sketch corrections are now durably represented in the existing overview-sync decisions: the current compiler-stage artifact name remains `SyntaxTree`, the `ParsedConstruct` shape belongs in explanatory comments and prose, and SlotValue subtype mismatches stay surfaced as inherited open questions rather than silently resolved.

- No new architecture divergence was introduced beyond the already-recorded follow-up: the grammar generator remains future-tense, and the separate “Precept Innovations” callout wording cleanup is still outstanding.


---


---

### 2026-05-03T14:02:40Z: Grammar design reference established as the canonical language-design guide


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `elaine-grammar-doc`.


- `docs/language/precept-grammar.md` is now the durable grammar reference for Precept language developers and designers.

- Durable document-shape rules: lead with what the grammar is not, use flat constructs / keyword anchoring / named slots as the structural spine, keep the linguistic model and grammar invariants in their own sections, and preserve a quick-reference appendix for lookup mode.

- Presentation rule: syntax-rich grammar references should prefer ASCII hierarchy/anatomy diagrams over Mermaid-style node graphs.


---


---

### 2026-05-03T14:02:40Z: Catalog-driven thesis deviations remain explicit tooling gaps only


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `frank-thesis-deviation-audit`.


- Frank's full sweep across the 11 canonical stage docs found no silent architectural drift: the catalog-driven thesis is thoroughly embedded across the design set.

- The only real deviations remain explicit tooling gaps already called out in source docs: the hand-authored TextMate grammar and the hardcoded MCP `firePipeline` array.

- Carry-forward follow-up: modifier grouping in MCP should derive from metadata shape instead of hardcoded grouping keys, and the grammar generator remains the highest-leverage cleanup.


---


---

### 2026-05-03T14:02:40Z: compiler-and-runtime overview synced to the canonical stage docs


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `frank-compiler-doc-sync`.


- `docs/compiler-and-runtime-design.md` is now durably framed as the narrative overview layer over the 11 canonical stage docs rather than a competing stage-spec source.

- The live parser contract is the generic `ParsedConstruct(ConstructMeta, SlotValue[], SourceSpan)` shape; `TypeKind` resolves in the type checker, SemanticIndex back-pointers target `ParsedConstruct`, and the overview now counts 13 catalogs including `ExpressionForms`.

- Open questions are inherited rather than silently resolved here, including the expression-tree shape and the remaining SlotValue/catalog reconciliation items.


---


---

### 2026-05-03T14:02:40Z: Catalog-first wording corrected in compiler-and-runtime-design.md


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `frank-catalog-description-fix`.


- The catastrophic stale sentence that described Precept extension as “add an enum member and fill an exhaustive switch” is now durably rejected.

- Correct wording: adding a language feature means adding a catalog entry (structured metadata); pipeline stages stay generic; C# completeness enforcement lives at catalog declaration time through metadata shape completeness, not downstream per-feature switches.

- Explicit follow-up remains open: the “Precept Innovations” callout box in the same document still carries similar stale wording and needs a separate cleanup pass.


---


---

### 2026-05-04T15:15:33Z: Philosophy v6 locked with prevention framing and developer-commitment POV

**By:** Scribe

**Status:** Merged, inbox cleared (18 files; deduped 0).

**Merged sources:** "elaine-philosophy-rewrite.md", "elaine-philosophy-v2.md", "elaine-philosophy-v3.md", "elaine-philosophy-v4.md", "elaine-philosophy-v5.md", "elaine-philosophy-v6.md", "frank-api-minispec-decisions.md", "frank-clrtype-discovery.md", "frank-philosophy-advisory.md", "frank-philosophy-amendment.md", "frank-preceptvalue-boundary.md", "frank-preceptvalue-internal.md", "frank-registration-surface-rethink.md", "frank-v4-review.md", "peterman-philosophy-advisory.md", "peterman-v4-review.md", "steinbrenner-philosophy-advisory.md", "steinbrenner-v4-review.md".

- Elaine's philosophy track now records the full rewrite chain through v6: reviewer fixes from Frank, Steinbrenner, and Peterman landed in v5, then the audience shifted from direct domain-expert address to developer-commitment framing in v6.
- Reviewer convergence locked two durable copy rules for `docs/philosophy.md`: use Precept's real nouns (`compiled precept`, `runtime`, `definition`) instead of implementation jargon like `engine`, and address developers as adopters/builders while keeping domain-user pain as the beneficiary frame.
- docs/philosophy.md is now locked at v6: the Prevention, not detection bullet states the structural no-window guarantee in business-logic and business-process terms, and Compile-time structural checking now explicitly names dead-end states, unsatisfiable guard combinations, and workflow-topology proof.
- Review status is durably recorded: Frank, Steinbrenner, and Peterman all approved Elaine v4 with notes, Elaine-24 performed the final POV shift in v6, and Elaine-25 applied the locked two-bullet edit to docs/philosophy.md.
