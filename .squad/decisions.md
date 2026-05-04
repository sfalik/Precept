# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

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

**Status:** Merged, deduplicated, inbox cleared (4 files; parallel follow-up analyses converged on the same split).

**Merged sources:** `frank-cc25-treewalk`, `frank-cc25-spanstack`, `frank-cc25-jsonreader`, `frank-cc25-optionc`.

- Durable dual-consumer model: production Fire/Inspect/Update uses the A+G typed-opcode runtime, while LS/MCP interactive tooling keeps a `TypedExpression` tree-walk path for rich per-node traces and sub-50 ms authoring feedback.
- JSON-native or span-stack evaluation is rejected as the production stack currency: every serious precedent deserializes to typed values before computation, and parse/format cost swamps any zero-copy story for numeric work.
- Good ideas harvested from the rejected variants stay additive: explicit TypeKind→CLR mapping metadata and string-stack / JSON-friendly techniques remain valid for the interactive tooling path without reopening the production runtime decision.

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


### 2026-05-03T02:52:51Z: Catalog-driven consumers stay generic; accessor layer deferred


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files)


**Merged sources:** `frank-catalog-driven-pipeline`, `copilot-directive-2026-05-02T22-52-51Z-accessor-layer-yagni`.


- Option F remains the consumer-facing parser shape: generic `ParsedConstruct(ConstructMeta, SlotValue[], SourceSpan)` output is sufficient because downstream dispatch is by slot-value shape, not construct identity.

- Durable consumer contract: the language server remains zero-per-construct and MCP stays above raw parse output, consuming catalogs, the semantic model, diagnostics, and runtime APIs rather than AST node classes.

- Shane's ruling closes the remaining convenience-layer question: do not build typed accessor helpers speculatively; the accessor layer is YAGNI until a concrete consumer need appears, and alternatives must be reconsidered before adding it.


---


---


### 2026-05-03T02:52:51Z: Catalog-driven pipeline thesis extended through lexer, parser, and builder


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `frank-pipeline-upstream-coverage`.


- The catalog-driven pipeline argument now explicitly covers the upstream stages: the lexer is already ~95% catalog-driven, and the radical parser reaches ~85% catalog-driven dispatch with the Pratt loop as the irreducible kernel.

- The precept builder is the strongest proof-of-concept stage for the inversion because it is almost entirely structural assembly; its remaining irreducible work is cross-construct name resolution rather than per-construct domain logic.

- If the builder is genericized, `ConstructMeta` may grow a `ModelContribution` metadata hook so construct-to-model projection stays catalog-native instead of being re-encoded in builder switches.


---


---


### 2026-05-03T01:07:30Z: Outcomes catalog ruling reversed to DU + catalog two-level pattern


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files; superseded original ruling normalized)


**Merged sources:** `frank-outcomes-catalog`, `frank-outcomes-catalog-revised`.


- Frank's initial DU-only ruling for outcomes is now durably reversed: outcomes take the same two-level architecture as actions, with `OutcomeKind` + `OutcomeMeta` + `Outcomes.cs` at the metadata layer and the `OutcomeNode` discriminated union retained at the syntax-node layer.

- The decisive catalog-system reason is the `no transition` composition gap: token-level outcome categories enumerate `No` and `Transition` separately, but consumers need one outcome-level abstraction for `no transition`; that composition rule is domain knowledge and therefore belongs in metadata rather than parser/tooling hardcodes.

- Durable consumer contract: `Outcomes.All` must enumerate the three real outcome variants (`transition`, `no transition`, `reject`) with syntax/lead-token metadata, while parsing and typed-model work continue to use the DU for structural shape.


---


---


---


### 2026-05-03T01:07:30Z: Routing-family terminology split locked for parser-radical docs


**By:** Scribe


**Status:** Merged, inbox cleared (1 file)


**Merged sources:** `frank-family-terminology`.


- Frank locked the terminology split in `parser-radical.md`: the four parse-scope buckets are always "routing families", while the shared-leader disambiguation groups are always `ConstructFamily` entries.

- Durable catalog rule: Header and Direct constructs never belong to a `ConstructFamily` because unique leading keywords need no family-level disambiguation metadata; only `In`, `To`, `From`, and `On` participate in `Families.All` / `FamilyDispatch`.

- Carry-forward wording rule: avoid the unqualified word "family" in this design area because it ambiguously conflates parse scope with shared-leader disambiguation.


---


---


---


### 2026-05-03T01:07:30Z: Grammar-hierarchy markers split structural anchors from slot badges


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `elaine-diagram-icon-revision`.


- Elaine locked the §0.9 icon revision: `◆` marks `ConstructFamily` sub-group headers as structural rows, while `[A]` and `[O]` mark per-construct action/outcome slot badges.

- Durable readability rule: structural grouping markers and per-construct annotation badges must use different visual classes, not different members of the same circled-digit icon family.

- Alignment contract: widening the badge column to `[A][O]` must keep the syntax column visually fixed across TransitionRow, StateAction, EventHandler, and non-badged rows.


---


---


---


### 2026-05-03T01:07:30Z: Radical AST hybrid option recorded pending Shane ruling


**By:** Scribe


**Status:** Merged, inbox cleared (1 late-arriving file; proposal remains pending owner ruling)


**Merged sources:** `frank-ast-radical-options`.


- Frank's radical-AST options pass prefers Option F: the parser produces generic `ParsedConstruct` values keyed by catalog metadata, while consumers use thin typed accessor functions and MCP stays fully typed at the external boundary.

- Durable tradeoff to retain: this keeps new constructs parser-zero-touch, but it gives up direct C# pattern matching on per-construct node classes in favor of `ConstructKind` dispatch plus accessor calls.

- Recorded fallback: if exhaustive node-pattern ergonomics are judged non-negotiable, Option C (source-generated typed nodes) is the explicit backup path; owner ruling is still pending.


---


---


---


### 2026-05-03T00:15:16Z: Radical parser drops `ConstructMeta.Slots` as a separate field


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files; overlapping slot notes normalized)


**Merged sources:** `frank-slots-dropped`, `frank-slots-verdict`.


- The radical parser design now explicitly removes `ImmutableArray<ConstructSlot> Slots` from `ConstructMeta`; named parse positions live only as `Tag("name", rule)` nodes in `Grammar`.

- Tooling and documentation consumers must derive ordered named captures from the authoritative grammar tree via `ExtractNamedCaptures(ParseRule grammar)` at catalog startup instead of maintaining a parallel slot list.

- Durable architecture rule: when the grammar tree already expresses named parse positions, a second catalog field is mirrored truth and should be deleted rather than synchronized.


---


---


---


---


### 2026-05-03T00:15:16Z: Parser rebuild recommendation narrowed to risk-only grounds


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files; reassessment normalized)


**Merged sources:** `frank-parser-rebuild-decision`, `frank-rebuild-reassessment`.


- Path C remains the active recommendation, but only because it avoids the still-open stashed-guard, split-modifier, and variant-action design gaps; the original schedule argument is now explicitly withdrawn.

- Under current AI-assisted team velocity, a parser rebuild and targeted parser improvements are treated as roughly schedule-equivalent, so remaining guidance must be framed as design-risk sequencing rather than throughput.

- If Path B is reconsidered, the stashed-guard pattern must be locked first because it is the deepest unresolved break in the slot-sequential parser model.


---


---


---


---


### 2026-05-03T00:15:16Z: George pipeline cross-review corrections recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `george-pipeline-review-complete`.


- George's cross-review corrects the parser-analysis record: `is set` / `is not set` precedence is already catalog-derived, while `.` and `(` remain the intentional Pratt-loop hardcodes.

- Ordering and priority are now durable: `StructuralBoundaryTokens` derivation is P0, uniform action-shape `Statement` nodes must land before checker Slice 5, and `ParseFieldDeclaration` unification waits on split-modifier metadata design.

- Additional blockers stay explicit: outcome parsing still lacks a catalog path, `TypeMeta.LiteralRange?` plus `ContentValidation` still gate Slice 4, and variant-action shape parsers still carry inline kind-identity checks that need metadata.


---


---


---


---


### 2026-05-02T21:58:21Z: GAP-046 design locked to dedicated CI FunctionKind entries


**By:** Scribe


**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)


**Merged sources:** frank-gap046-design.


- GAP-046 is now durably locked to the catalog-complete path: add FunctionKind.TildeStartsWith / FunctionKind.TildeEndsWith plus FunctionMeta.CIVariantOf so CI functions exist as real function metadata rather than only as HasCIVariant side effects.


- Parser behavior stays intentionally unchanged; the ~ null-denotation path still derives CI-capable names from base-function HasCIVariant, while hover/completion/MCP follow-through must project from the new CI entries themselves.


- The open downstream checker concern remains explicit: calling ~startsWith / ~endsWith with a non-~string first argument still needs a future diagnostic decision when the real CIFunctionCallExpression handler lands.


---


---


---


---


### 2026-05-02T21:58:20Z: Canonical checker review resolutions D-15 through D-25 recorded


**By:** Scribe


**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)


**Merged sources:** frank-george-canonical-response.


- Frank's canonical response now has its own durable ledger record: widening is single-hop only, binary fallback order is deterministic (left, right, both), numeric literals stay bottom-up with context retry, event handlers get event-arg scope, and identifier resolution priority is bindings > args > fields.


- The response also locks the remaining checker-shape decisions George forced open: FieldScopeMode gates forward references, function overload resolution follows one deterministic pipeline, Slice 6 stays unsplit, TypedTransitionRow.ResolvedArgs stays rejected as anti-mirroring, and TypedEditDeclaration is placeholder-only for future stateless-edit work.


- Net result: all 11 checker slices are implementation-ready with no unresolved design blockers.


---


---


---


---


### 2026-05-02T21:58:19Z: Research validation integration pattern and Slice 4 range check locked


**By:** Scribe


**Status:** Recovered from merged inbox, deduplicated, inbox cleared (1 file)


**Merged sources:** frank-research-crossref.


- Research-validation artifacts are now durably patterned: the full validation file lives in research/language/, the design doc cites it in a `## Research Validation` section, the working draft stays in docs/working/ but is marked superseded, and research/language/README.md indexes the validation set.


- The same decision also adds out-of-range numeric literal checking to checker Slice 4: once expectedType resolves a literal, the checker validates the value against the representable range exposed by type metadata.


- This establishes a reusable design-validation workflow instead of leaving research cross-reference work as one-off process drift.


---


---


---


---


### 2026-05-02T20:05:35Z: GAP-040 bag `countof` parameter locked to DU accessor metadata


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-gap040-decision`.


- GAP-040 is locked to the metadata-driven path: `bag.countof(...)` must stop pretending its parameter is `integer` and instead use a dedicated `ElementParameterAccessor` DU subtype whose parameter resolves to the bag element type.


- The flat `ParameterType` axis is now explicitly treated as a three-shape problem (`no parameter`, `fixed parameter type`, `element-type parameter`); the element-type case does not get a boolean flag or `TypeKind.Element` sentinel because both would create illegal or non-language-level states.


- Downstream consumers should pattern-match on the accessor subtype, keep MCP/tooling serialization as a thin projection of that metadata, and update bag-accessor assertions so `countof` renders as an element-typed accessor rather than `integer`.


---


---


---


---


### 2026-05-02T20:05:34Z: Frank type-checker review response accepted and locked


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-typechecker-review-response`.


- Frank formally accepted 5 of George's 6 findings, with Finding 5 reclassified as a non-finding because GAP-032 (`pow(integer, integer)` proof requirements) was already fixed.


- The response locks all 5 implementation pre-requisites as mandatory before or during the slice plan: no new operation indexes, a pre-Slice 0 shape commit, array-primary field storage plus a derived frozen name map, `ActionSecondaryRole` stamping for `TypedInputAction`, and per-slice `[HandlesCatalogMember]` stub migration.


- The revised checker plan also records durable shape choices for the remaining open design points: `ContentValidation` becomes a DU, resolution must always return partial typed results via `TypedErrorExpression`, qualifier propagation lives on typed binary expressions, and `MethodCallExpression` / interpolated forms are assigned explicit slice ownership.


---


---


---


---


### 2026-05-02T19:49:00Z: GAP-035 choice literal dispatch locked to ChoiceLiteralTokens metadata


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 late-arriving file)


**Merged sources:** `frank-gap035-decision`.


- GAP-035 is now locked to the catalog-complete path: add nullable `TypeMeta.ChoiceLiteralTokens` metadata rather than a `NumericLiteral` trait or a documented parser exception.


- `ParseChoiceValue` must derive both the signed numeric branch and literal-token validity from `Types.ByToken[elemToken.Kind].ChoiceLiteralTokens`, eliminating both remaining `elemToken.Kind` identity switches from parser choice-literal dispatch.


- `TypeTrait.ChoiceElement` remains the declaration-validation gate, while `ChoiceLiteralTokens` becomes the parse-time dispatch contract; couple them with an invariant test and mark GAP-035 fixed only after the parser rewrite is verified.


---


---


---


---


### 2026-05-02T19:48:45Z: TypeChecker pre-slice requirements and BinaryIndex semantics locked


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `george-typechecker-preslice-design`.


- The checker now has a pre-Slice 0 contract: land the full shape-only `SemanticIndex` and typed-record hierarchy before numbered slices, and keep field storage array-primary with a derived frozen name index so declaration order survives.


- Runtime lookup direction is locked to the existing `Operations.FindCandidates` / `FindUnary` APIs: `BinaryIndex` already returns multi-candidate arrays, so money/quantity overloads must disambiguate qualifier-matched entries and emit `QualifierMismatch` on failure.


- `TypedInputAction.SecondaryExpression` must carry an explicit secondary-role discriminator for evaluator dispatch, and GAP-032 / `pow(integer, integer)` is recorded as already closed rather than a live blocker.


---


---


---


---


### 2026-05-02T19:42:08Z: Collection-types plan review rounds R1/R2 synchronized


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (12 files; multi-pass review normalized)


**Merged sources:** `frank-review-r1`, `george-review-r1`, `soup-review-r1`, `frank-review-r2`, `george-review-r2`, `soup-review-r2`, `frank-review-2`, `george-review-2`, `soup-review-2`, `frank-plan-v3-complete`, `frank-plan-v4-complete`, `frank-plan-v5-complete`.


- R1/R2 review passes converged on the same mandatory corrections: add `TokenKind.To` end-to-end, align codes 95–98 names and stage ownership with the spec, remove the spurious `ExpressionFormKind.LookupAccess` plan work, and route every proposed test into real existing files rather than phantom catch-all test files.


- The revision chain now preserves exact downstream obligations: update `DiagnosticsTests.cs` stage-group member data, fix hardcoded token counts and member-name regressions for `countof` / `peekby`, keep prefix `~startsWith` / `~endsWith` syntax correct, and spell out the `Countof` / `Peekby` member-name exceptions wherever token metadata is asserted.


- Durable planning rule: every slice must name real file targets, update existing hardcoded counts and member-data helpers, and keep plan/spec/catalog terminology synchronized before implementation begins.


---


---


---


---


### 2026-05-02T19:42:07Z: Collection-types final blocker stack and revision path recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (6 files; final-review stack normalized)


**Merged sources:** `frank-final-review`, `george-final-review`, `soup-final-review`, `frank-final-plan-revision-v2`, `frank-plan-revision-complete`, `elaine-final-review`.


**Deduplicated/skipped:** `frank-plan-review`.


- The final review stack agrees the plan is close but still blocked by surgical hazards rather than design rework: PRECEPT0019 parser annotations for new expression forms, wrong pseudocode symbols (`TokenKind.Assign`, `Statement(SourceSpan)`, `BinaryExpression`), missing updates to real test inventories, and incorrect parser-routing assumptions around `for` and `remove ... at`.


- Frank's revision passes also cleaned the plan spine itself: stale Phase 3/runtime scope was removed, dependency ordering was narrowed to the real slice ranges, and the catalog-first lookup-access direction stayed explicit with `for` pinned to a dedicated binding tier instead of vague pseudo-constants.


- Elaine's final UX pass downgraded remaining hover/MCP copy issues to follow-on tooling polish, so the plan now reads as mechanically repairable rather than conceptually blocked.


---


---


---


---


### 2026-05-02T19:42:06Z: Collection-types catalog and parser design decisions locked


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (11 files; architecture questions normalized)


**Merged sources:** `george-plan-review`, `frank-b1-analysis`, `copilot-b1-decision`, `frank-b2-analysis`, `frank-b2-catalog-exhaustive`, `frank-b2-reanalysis`, `frank-rubber-duck-exhaustively`, `frank-c3-verdict`, `frank-g5-rubber-duck`, `frank-countof-peekby-naming`, `frank-slice9-correction`.


- B1 is now locked to the catalog-complete path: keep secondary action kinds in `Actions.All`, add `ActionMeta.PrimaryActionKind`, and derive `ByTokenKind` from primary actions only so startup stays crash-free without hiding real language surface from catalog consumers.


- B2 is resolved as explicit switch-arm maintenance, not by stretching `[HandlesCatalogExhaustively]` onto `ActionSyntaxShape` or `ConstructSlotKind`; the annotation bridge remains the right tool for distributed handler coverage, not local parser shape switches.


- C3/G5/slice-9 clarifications are durable: `remove F at N` is handled before value parsing rather than via unreachable shape routing, `AppendBy` disambiguation stays syntactic instead of adding redundant catalog metadata, lexer keyword recognition remains fully catalog-driven, and `countof` / `peekby` stay as member-name-legal compound accessors.


---


---


---


---


### 2026-05-02T19:42:05Z: Collection-types documentation and wording corrections merged


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (4 files)


**Merged sources:** `docs-audit`, `elaine-review-2`, `elaine-spec-fixes-complete`, `elaine-catalog-fixes-complete`.


- The documentation audit locks the reference pattern: spec summaries are acceptable only when they explicitly defer to canonical type docs; the highest drift-risk duplication surfaces remain diagnostics and repeated `contains` semantics, which must cross-reference the spec instead of silently forking it.


- Elaine's doc/hover blockers were closed in both spec and catalog text: collection type descriptions, `append` wording, and the missing codes 99–106 diagnostics are now recorded as corrected rather than still implicit TODOs.


- Durable doc-sync rule for this workstream: language-surface wording changes are only done when spec, hover/catalog text, and the plan's referenced diagnostic tables all agree on the same user-facing story.


---


---


---


---


### 2026-05-02T19:42:04Z: Diagnostics semantics and emission for codes 11 and 12 locked


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (5 files)


**Merged sources:** `elaine-diagnostics-review`, `elaine-diagnostic-distinction`, `frank-diagnostic-activation`, `frank-gap-019-024-analysis`, `george-diagnostic-emission`.


- `UnexpectedKeyword` and `InvalidCallTarget` are now durably treated as distinct parse failures: the former is a declaration keyword in value-expression position, while the latter is a non-callable expression followed by `(...)`.


- The recorded contract is parse-stage Error severity, catalog/spec wording sync, catalog-derived keyword detection from `Tokens.Keywords.Values`, and an explicit `InvalidCallTarget` emit in the infix `LeftParen` branch rather than a silent break.


- The paired GAP-024 analysis stays with this bundle because it locked the same architectural principle: bag/list/log TypeQualifier support belongs in the spec surface, not as a parser rollback, since qualifier semantics are orthogonal to collection kind.


---


---


---


---


### 2026-05-02T19:42:03Z: Language-consistency gap fixes batch recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (12 files; gap-fix stream normalized)


**Merged sources:** `frank-gap024-resolved`, `frank-gap-025-026-028-fixed`, `frank-gap029-fixed`, `frank-gap031-fixed`, `george-gap030-fixed`, `george-gap032-fixed`, `frank-gap033-fixed`, `ink-gap-code-fixes`, `frank-iter7-results`, `frank-iter8-catalog-results`, `george-iter8-results`, `frank-g1-rename`.


- The gap ledger now durably records closed fixes across spec, catalog, parser, and runtime surfaces: GAP-024 spec support for TypeQualifier on bag/list/log, GAP-025/026/028 catalog mismatches, GAP-029/030/031 parser hardcodes replaced with catalog-derived sets/lookups, GAP-032 proof requirements for `pow(integer, integer)`, and GAP-033 stale `Notempty` documentation.


- Iteration 7 and Iteration 8 converge on the same architectural rule: parser vocabulary and precedence helpers must derive from catalog metadata, while structural constructs such as `.` and `(` may remain intentional non-catalog hardcodes because they are grammar structure rather than surfaced operators.


- PRECEPT0023c's `MultiLead` → `MultiSequence` rename and Ink's wider gap-fix batch now sit in the same durable audit trail; remaining unresolved items are downstream TypeChecker/Evaluator work, not unknown language-surface decisions.


---


---


---


---


### 2026-05-02T19:42:02Z: Dapr hosting research and bounded-quantifier philosophy note merged


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (4 files)


**Merged sources:** `frank-dapr-research`, `george-dapr-research`, `frank-philosophy-q1-amendment`, `frank-subset-disjoint-squash`.


- Both Dapr analyses converge on the same only-credible distributed-hosting shape: actor-hosted Precept instances with a pod-level compiled-definition cache, typed rehydration before guard evaluation, and `Restore()` as the state-store boundary; workflows remain the wrong semantic fit for Precept entity execution.


- Frank's proposed §0.4.1 amendment stays an owner-review item only: bounded quantifiers are philosophically compatible because they unfold over statically finite collections, but philosophy text must not change without explicit sign-off.


- Frank's `subset` / `disjoint` verdict is now durable alongside that philosophy note: keep them only for `set of <choice>` where the compiler can prove the closed-domain guarantee, and squash them for open types where quantifiers already cover the runtime-only case.


---


---


---


---


### 2026-05-01T20:06:10Z: Catalog-member annotation rename locked; no exhaustiveness gaps found


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-handlesform-rename`.


- Frank-10's rename is now canonical squad state: use `[HandlesCatalogMember]` for per-member claims alongside `[HandlesCatalogExhaustively(typeof(T))]`; legacy `[HandlesForm]` wording is retained only as historical rename context.


- Historical ledger wording was updated inline so earlier Slice 4, Slice 27, and annotation-bridge records stay readable after the rename without implying the old attribute still exists.


- Frank-9's full sweep of catalog enum types found no currently-unannotated distributed-dispatch gaps: existing consumers already line up with the correct enforcement mode, with CS8509 retained for centralized switches and `[HandlesCatalogExhaustively]` reserved for real distributed handlers.


---


---


---


---


### 2026-05-01T19:50:46Z: Lexer exhaustiveness annotation scope resolved


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-lexer-exhaustiveness`.


- Frank closed the annotation-scope question for the lexer: `Lexer` produces `TokenKind` values from catalog-driven lookup tables and never dispatches on `TokenKind`, so `[HandlesCatalogExhaustively]` would be the wrong contract.


- The correct safety net already exists in catalog metadata and lookup tables (`Tokens.Keywords`, operator tables, punctuation tables); production coverage stays catalog-driven rather than method-annotation-driven.


- The real follow-up remains the future evaluator implementation: when D8/R4 introduces expression-form dispatch in `Evaluator`, that same commit must add `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` plus per-form handler annotations.


---


---


---


---


### 2026-05-01T18:17:13Z: Parser.cs partial split approved for Slice 27


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-parser-split`.


- `partial class Parser` + `partial ref struct ParseSession` is the approved zero-behavior-change split mechanism; `ParseSession` being a `ref struct` rules out helper-class alternatives because they would force `ref` threading through 60+ methods.


- The structural seam is locked as three files: `Parser.cs` for shell/vocabulary/dispatch, `Parser.Declarations.cs` for declaration grammar and slot/type machinery, and `Parser.Expressions.cs` for the Pratt loop, atom parsers, and expression helpers.


- Attribute placement is part of the contract: `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` stays only on the primary `ParseSession` declaration, `[HandlesCatalogMember(...)]` (renamed from `[HandlesForm(...)]`) moves with the methods in `Parser.Expressions.cs`, and static vocabulary remains on the outer `Parser` class.


- Durable implementation caveat for Slice 27 and Slice 16: `ref struct` types cannot own static fields, so `KeywordsValidAsMemberName` belongs on `Parser`, while `ExpectIdentifierOrKeywordAsMemberName()` stays on `ParseSession` beside the `Dot` handler.


---


---


---


---


### 2026-05-01T18:17:13Z: Parser-gap Slice 4 corrections and recording directive synchronized


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (3 files; blockers normalized into one record)


**Merged sources:** `copilot-directive-record-problems`, `frank-plan-review`, `george-plan-b1b4-fixes`.


- Shane's directive is now durable: when implementation uncovers problems, agents must write them into the working plan or decisions inbox instead of leaving them only in ephemeral output.


- Frank blocked Slice 4 on four exact plan defects: two existing attribute files incorrectly marked `Create`, the wrong analyzer filename/status, and a stale `HandlesCatalogMemberAttribute.Kind` snippet (then named `HandlesFormAttribute.Value`) that did not match the real API.


- George corrected `docs/working/parser-gap-fixes-plan.md` so `HandlesCatalogExhaustivelyAttribute.cs`, `HandlesCatalogMemberAttribute.cs` (renamed from `HandlesFormAttribute.cs`), and `Precept0019PipelineCoverageExhaustiveness.cs` are treated as existing files, and the code sample now uses `.Kind`.


- This record supersedes earlier stale ledger wording: the canonical annotation bridge remains generic `[HandlesCatalogExhaustively(typeof(T))]` + `[HandlesCatalogMember(kind)]` (renamed from `[HandlesForm(kind)]`), not a parameterless `HandlesExpressionForms` marker.


---


---


---


---


### 2026-05-01T18:17:13Z: Multi-token presence operators escalated to proposal-scope catalog work


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files; scope pending owner decision)


**Merged sources:** `frank-multi-token-operator-scope`, `george-multi-token-operator-scope`.


- Both analyses agree `is set` / `is not set` are real semantic operators with precedence, operand constraints, result typing, and documentation surface, so leaving them as uncataloged parser special-cases is a catalog-completeness bug rather than a parser-correctness bug.


- Shared implementation obligations are now explicit: add postfix-aware operator metadata, introduce `Arity.Postfix`, keep `.` and method-call `(` outside `Operators.All` as structural forms, and prevent the duplicate-key crash that would occur if both presence operators keyed `Operators.ByToken` on `(TokenKind.Is, Postfix)`.


- Frank recommends treating this as GitHub-issue/design-review work rather than a hotfix and prefers a full-fidelity catalog representation plus `ExpressionFormKind.PostfixOperation`; George supplied the bounded call-site inventory and the `ByToken` hazard that must be handled in the same commit.


- Carry-forward state: proposal scope and rationale are locked, but the final `OperatorMeta` shape still needs owner sign-off.


---


---


---


---


### 2026-05-01T06:21:31Z: Annotation-bridge enforcement pattern recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (3 files; overlap normalized into one record)


**Merged sources:** `frank-annotation-bridge`, `george-annotation-bridge-plan`, `frank-class-marker`.


- Frank designed an annotation-bridge pattern for expression-form coverage: parser handlers advertise their responsibility with `HandlesCatalogMemberAttribute` (renamed from `HandlesFormAttribute`) instead of forcing an analyzer to reverse-engineer Pratt control flow.


- George's plan update locks that annotation bridge into Slice 4 rather than a follow-on: `HandlesCatalogMemberAttribute` (renamed from `HandlesFormAttribute`) lives beside `ExpressionForms`, PRECEPT0019 checks handler coverage across `Parser`, `TypeChecker`, `Evaluator`, and `GraphAnalyzer`, and Slice 13 stays the parser-routing assertion layer.


- Frank also locked the class-level opt-in marker for PRECEPT0019: use parameterless `[HandlesExpressionForms]` on pipeline classes from `src/Precept/HandlesExpressionFormsAttribute.cs`, while `[HandlesCatalogMember(ExpressionFormKind.X)]` (renamed from `[HandlesForm(ExpressionFormKind.X)]`) stays on methods to claim specific form coverage.


- Recommended enforcement is now three-layered: PRECEPT0007 keeps `ExpressionFormKind` exhaustiveness on catalog metadata, PRECEPT0019 checks that every form is claimed by handler annotations, and xUnit coverage tests verify end-to-end parser behavior.


- The durable design rule is to analyze stable metadata and attributes rather than parser implementation internals, so coverage enforcement survives refactors to switches, dictionaries, or helper methods.


---


---


---


---


### 2026-05-01T06:21:31Z: Parser-gap plan audit and coverage slice synchronized


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (3 files; overlap normalized into one record)


**Merged sources:** `frank-roslyn-analyzer-analysis`, `george-coverage-slice`, `george-plan-audit`.


- The parser-gap plan now carries Slice 13 (`ExpressionFormCoverageTests`) and expands Slice 4 so `LeadTokens` lives on `ExpressionFormMeta`; George sequenced the coverage slice after Slices 5 and 6 so it lands green from day one.


- Layer 1 compile-time coverage is locked to existing infrastructure: add `ExpressionFormKind` to `CatalogAnalysisHelpers.CatalogEnumNames` so PRECEPT0007 enforces explicit `GetMeta` arms. Standalone `GetLeadTokens()` + CS8509 and a new cross-method parser analyzer were both rejected.


- George's audit found the remaining plan hygiene fixes still worth carrying forward: add `src/Precept/Language/Operators.cs` to Slice 3's file inventory and remove or correct the dead `frank-expression-form-catalog-placement.md` reference. The previous missing-coverage-slice gap is now closed by Slice 13.


---


---


---


---


### 2026-04-29T05:34:09Z: Collection type expansion follow-up recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-collection-types-research`.


- Frank closed the remaining ordered-choice documentation gaps in `docs/language/collection-types.md`, so `choice(...) ordered` is treated consistently in the grammar, orderability framing, and comparison material.


- The doc now has `§ Proposed Additional Types`, evaluating six candidates with priority bands: `bag`, `log`, and `map` high; `sortedset` and `priorityqueue` medium; `deque` low.


- The new `§ Comparison With Other Collection Systems` cross-language table maps 14 capabilities across 9 ecosystems and reinforces restricted `map of choice(...) to V` as the strongest next collection-type research target.


---


---


---


---


### 2026-04-29T05:18:06Z: Collection types design doc authored and indexed


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (1 file)


**Merged sources:** `frank-collection-types-doc`.


- Frank authored `docs/language/collection-types.md` as the canonical collection-types reference, covering the shipped surface (`set`, `queue`, `stack`), actions, accessors, constraints, emptiness safety, inner-type behavior, `~string`, and diagnostic anchors.


- The new doc also preserves the current design frontier: proposed quantifier predicates plus collection-level modifiers such as `unique`, collection `notempty`, `subset`, and `disjoint`, with eight explicit owner-sign-off questions recorded before implementation.


- `docs/language/README.md` now indexes the new reference in the Documents table and reading order so collection guidance is discoverable from the language-doc hub.


---


---


---


---


### 2026-04-29T04:47:14Z: Vision→spec migration completed and vision archived


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (3 files merged, 1 duplicate skipped)


**Merged sources:** `frank-slice1-preamble`, `frank-slice2-semantic-gaps`, `frank-slice3-4-archive`.


**Deduplicated/skipped:** `frank-vision-archive-audit` (already captured in the 2026-04-29T01:09:17Z vision/spec audit record).


- Frank's vision→spec migration is now durably closed as a complete sequence: §0 Preamble landed with the 11 Design Principles, Language Model, Governance Not Validation, Execution Model Properties, and pre-implementation graph/proof contracts; §3A Language Semantics landed with constraint semantics, outcome/verdict semantics, violation attribution, mutation atomicity, entity construction, and inspection as a first-class operation.


- The migration preserved substance rather than rewriting it: overlapping graph-analysis material was merged into one contract section, mutation atomicity/inspectability were expanded without duplicating their earlier anchors, and the spec now carries the identity-bearing language philosophy that previously lived only in the vision doc.


- Slice 3–4 then removed the two stale contradictions (`with` still listed as a structural preposition, and "root editability" wording left over from retired `write all` semantics), archived `docs/language/precept-language-vision.md` to `docs/archive/language-design/precept-language-vision.md`, updated the spec Status table, and swept 12 cross-references so the archived path never existed half-wired on the branch.


- Net result: the language spec is now the single canonical language document, the vision is preserved as archive material only, and the earlier archive-readiness audit remains the durable rationale for why this migration sequence was necessary.


---


---


---


---


### 2026-04-29T04:47:14Z: No-runtime-faults principles aligned; philosophy gap flagged


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files)


**Merged sources:** `frank-principles-10-11-eval`, `frank-philosophy-eval`.


- Frank's evaluation of §0.1 Design Principles found Principles 10 and 11 incomplete rather than contradictory: Principle 10 previously treated runtime faults as acceptable "definite errors," and Principle 11 only promised compile-time elimination of type errors instead of all evaluator fault classes.


- The spec is now aligned with Shane's no-runtime-faults contract: Principle 10 requires the compiler to prove safety or emit an obligation diagnostic, Principle 11 extends the clean-compile guarantee across type, arithmetic, access, and range fault classes, and runtime traps are positioned only as defensive redundancy for compiler-proven-unreachable paths.


- The proof engine contract in §0.6 already supported the stronger guarantee through prove-safe / proved-dangerous / unresolved classification and obligation diagnostics; no proof-engine design change was needed because the principles were catching up to an already stronger compiler contract.


- Frank's philosophy-grounded follow-up endorsed those revisions but flagged a product-identity gap: `docs/philosophy.md` explicitly scopes "prevention, not detection" to invalid entity configurations and does not yet name evaluation-fault prevention with the same explicitness. Recommended wording was recorded for owner review only and was not applied.


- Net result: the spec now clearly states the no-runtime-faults promise, while the philosophy gap is durably recorded as a flag for Shane rather than an auto-applied philosophy change.


---


---


---


---


### 2026-04-29T01:39:22Z: Catalog extensibility plan v3 cleared for George


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (2 files)


**Merged sources:** frank-catalog-ext-plan-v2-review, frank-catalog-extensibility-plan-review.


- Frank's first review blocked the catalog extensibility plan on three structural fixes: split PreceptHeader into RoutingFamily.Header, add Slice 3b to the execution order after Slice 3, and commit Slice 3b to explicit wrong-family ConstructKind listings so CS8509 stays active.


- Frank's second review confirmed those first-round blockers were resolved and approved the architecture, but found two new surgical blockers in the revised text: an unbound k variable in the Slice 3b throw examples and a phantom ErrorStatement(current) call in Slice 5.


- The coordinator patched both plan defects in plan.md: the Slice 3b guard text no longer references an unbound pattern variable, and Slice 5 now specifies the real synthetic-error-node handling instead of a nonexistent helper.


- src/Precept/Language/Token.cs was added to the file inventory because Slice 6 changes TokenMeta.IsAccessModeAdjective there, and the GetMeta wildcard note remains a documented non-blocking follow-up.


- Net result: the plan is now at v3, blockers are cleared, and George can implement from the updated plan.


---


---


---


---


### 2026-04-29T01:09:17Z: Catalog extensibility audit and parser design evaluation recorded


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (3 files)


**Merged sources:** `frank-catalog-extensibility`, `frank-parser-design-eval`, `frank-vision-spec-audit`.


- Frank's catalog extensibility audit confirmed the lexer is already 100% catalog-driven for keywords, operators, and punctuation; the remaining extensibility risk is entirely in parser/catalog enforcement boundaries.


- Eight parser hardening gaps are now the durable follow-up list: `BuildNode()` wildcard, `ParseDirectConstruct()` wildcard, hardcoded `DisambiguateAndParse()` routing, `ParseActionStatement()` switch exhaustiveness, hardcoded `ExpressionBoundaryTokens`, missing `ConstructKind`↔declaration subtype enforcement, missing `ActionKind`↔statement subtype enforcement, and hardcoded access-mode adjectives.


- The preferred remediation path is catalog shape change rather than Roslyn analyzers: remove wildcard fallthroughs for CS8509 coverage, derive boundary tokens from `Constructs.LeadingTokens`, add `RoutingFamily` to `ConstructMeta`, and add `ActionSyntaxShape` to `ActionMeta`.


- Frank's parser design evaluation across v5-v8 approved v8 as the closed canonical baseline: current code matches the parser spec, `OmitDeclaration` is correctly split from `AccessMode`, `FieldTargetNode` is a DU, and the 5-layer parser architecture is complete. Working docs are now audit trail, not pending design debt.


- Frank's vision-versus-spec audit found two live contradictions (`with` still listed as a structural preposition in the vision doc, and stale “root editability” wording after `write all` removal) and concluded the vision doc should not be archived until its language-identity material is migrated into the spec.


---


---


---


---


### 2026-04-29T00:43:25Z: Parser remediation review batch approved and synchronized


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (4 files)


**Merged sources:** frank-audit-cross, frank-parser-review, frank-parser-v2-authored, soup-nazi-parser-coverage.


- Parser remediation slices R1-R6 are architecturally approved against the v8 catalog-driven parser design: top-level dispatch is catalog-owned, rule/state/event routing now flows through slot machinery where intended, preposition disambiguation is metadata-driven, and the cleanup removed the unauthorized header comment.


- The permanent parser reference is now authored in docs/compiler/parser-v2.md. It captures the catalog-driven dispatch model, 5-layer architecture, full 12-node declaration hierarchy, OmitDeclaration separation, FieldTargetNode DU, validation pyramid, and expanded parser diagnostics.


- Cross-surface consistency was re-aligned before the review closed: 8 inconsistencies were fixed across the spec, parser reference, slot comments, and token metadata so secondary sources match catalog-first primaries.


- Coverage for the 6 remediation slices is approved at 2034/2034 passing tests. The audit fixed the stale ConstructSlotKind count, replaced the obsolete StateDeclaration slot-count assertion with an exact slot-shape fact, and added EventDeclaration_HasInitialMarkerSlot as the new catalog regression anchor.


---


---


---


---


### 2026-04-28T06:41:30Z: Access-mode vocabulary locked and catalog fix landed


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (13 files)


**Merged sources:** `copilot-directive-2026-04-28T02-14-omit-vs-access-semantic`, `copilot-directive-2026-04-28T02-41-readonly-editable-vocab`, `copilot-directive-when-position`, `copilot-directive-writable-adjective`, `copilot-directive-writeable-spelling`, `frank-rule-7-closed`, `frank-vocab-B1`, `frank-vocab-B2`, `frank-vocab-B3`, `frank-vocab-B4`, `george-accessmode-guard-slot-fix`, `george-parser-complexity-reeval`, `george-parser-complexity-when`.


- Shane locked the access-mode surface as `in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?`, with `omit` preserved as the separate structural-exclusion verb. Earlier B1-B4 exploratory vocabulary rounds now collapse to this canonical surface; `->` and adjective-only forms are not the language.


- Durable semantic framing is now explicit: `omit` removes the field from the state's structural schema, while access modes keep the field present and only constrain mutability. Access-mode guards stay post-field, and the writable/writeable spelling debate is superseded by the locked `readonly`/`editable` pair.


- Implementation follow-through is locked: the access-mode body shape is verb + field target + access adjective + optional guard, catalog/token work needs `modify`, `readonly`, and `editable`, and the `AccessMode` disambiguation family is now `modify`/`omit` rather than the retired `read`/`write`/`omit` set.


- George's follow-through landed: `ConstructKind.AccessMode` now ends with `SlotGuardClause`, `DiagnosticCode.RedundantAccessMode` has catalog metadata, the stale `write all` description is removed, a regression test pins guard-slot presence/position, and the suite stayed green at 1809 passing tests.


---


---


---


---


### 2026-04-28T05:08:10Z: Access-mode and parser-design inbox batch canonicalized


**By:** Scribe


**Status:** Merged, deduplicated, inbox cleared (14 files)


**Merged sources:** `copilot-directive-2026-04-28T01-08`, `copilot-directive-extensibility-validation-20260427`, `frank-access-mode-design-round`, `frank-access-mode-docs-working`, `frank-r5-validation-layer-20260428`, `frank-r7-implementation-plan`, `frank-redundant-access-mode`, `frank-spec-grammar-fixes`, `george-access-mode-docs-working`, `george-access-mode-feasibility`, `george-lang-simplify`, `george-r4-parser-design`, `george-r6-review`.


- Access-mode design round locked the durable shape: guarded `read` is only valid as a writable-baseline downgrade, guarded `omit` stays prohibited because it would make structural field presence data-dependent, and the vocabulary remains `read` / `write` / `omit`. Frank's and George's working docs are the durable references for the reasoning behind those constraints.


- Redundancy handling is now uniform: dead named-field access declarations are compile errors under `RedundantAccessMode`, including `in S write F` on already-`writable` fields, unguarded `in S read F` on non-`writable` fields, and guarded `read` on non-`writable` fields. `RedundantGuardedRead` is retired; `omit` and broadcast `all` forms remain exempt; rule 7 is still open.


- Parser extensibility direction is validation-first, not generator-first: fail loudly when catalog metadata is incomplete, keep `_slotParsers` exhaustive, give rule bodies their own `RuleExpression` slot, keep `ensure` and `because` separate, and reject pre-event `when` on `from ... on` with a diagnostic instead of silently expanding the language surface.


- Design-loop status is now explicit: the v7 parser working doc remains the implementation-plan anchor, while language-simplification proposals were recorded as analysis input and only owner-approved surface changes should be treated as canonical.


---


---


---


---


### 2026-04-28T04:49:58Z: `write all` removed from language — stateless precepts use `writable` modifier


**By:** Shane (owner directive)


**Status:** Applied


**Merged sources:** `copilot-directive-write-all-removed`.


- `write all` is removed from the Precept language entirely. Stateless precepts now opt into mutability only through field-level `writable`; there is no root-level bulk access mode construct.


- This supersedes any earlier record that `write all` survived as stateless sugar.


- Stale references called out in the inbox covered the spec, vision doc, working docs, samples, and token/tooling vocabulary that still described root-level bulk access as live syntax.


- Canonical follow-through: language docs, samples, and downstream tooling must all treat field-level `writable` as the only stateless mutability opt-in.


---


---


---


---


### 2026-04-28T00:00:00Z: Combined Design v2 Structural Revision


**By:** Frank


**Status:** Applied


- Applied boundary reassessment: replaced "hard line / nothing crosses" claim with correct type dependency direction rule; clarified what crosses the lowering boundary.


- Readability/genre fixes: 13 stage-contract tables converted to labeled prose, two artifact tables merged, "How to read this document" added, §8 split, §9 moved to appendix, decision lead-ins added, problem statement added to §1, assertions moved to doc spine.


- No content dropped; all facts, contracts, and assertions preserved. Comparative tables retained where genuinely comparative.


- Motivation: Shifted from reference spec to design doc genre, making decisions and rationale explicit and readable.


---


---


---


---


### 2026-04-28T00:00:00Z: Combined Design v2 Gap Patch Complete


**By:** Frank


**Status:** Complete


- Added 10 missing design specifics to combined-design-v2.md: action-shape model, constraint activation indexes, constraint evaluation matrix, constraint exposure tiers, proof strategy enumeration, proof/fault chain formula, earliest-knowable kind assignment, named anti-patterns, compile-time vs lowered artifact table, implementation action items.


- Locked: three action shapes, precomputed constraint activation, closed proof strategies, explicit proof/fault chain ownership, five implementation action items.


- No philosophy gaps surfaced; all changes are implementation domain only.


---


---


---


---
