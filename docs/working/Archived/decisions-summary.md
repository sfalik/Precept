# Design Decisions Summary

> Summary of all decisions recorded in `.squad/decisions.md`. Each entry is a one-to-three-sentence capsule. For full rationale, read the corresponding entry in decisions.md.
> Last updated: 2026-05-04

---

## Runtime & Evaluation (CC#25)

### PreceptValue tagged struct — production runtime baseline
`PreceptValue` is a 32-byte tagged struct used as the universal evaluation currency. This eliminates ~768 MB/s gen-0 GC pressure at 100k events/sec vs. boxed `object?`. Option A+G (interpreter + catalog-delegate dispatch) is the v1 runtime; compiled paths (`System.Linq.Expressions`) are a designed-in upgrade seam, not a v1 dual path.

### TypeBuilder / codegen — REJECTED
TypeBuilder was analyzed and explicitly rejected. Blocking constraints: SaaS cold-start (hundreds of ms to compile on upload/cache-miss), and loss of per-step inspectability (a product guarantee, not a debug convenience). Do not treat TypeBuilder as the implicit v2 path unless deployment model or inspectability requirements change first.

### Type-per-lane storage (Option F split-lane) — REJECTED
Split-lane storage was revised and rejected. 23 of 32 `TypeKind` members still live in the reference lane; the lane split adds routing complexity for no meaningful reduction in cross-lane operations. Unified `PreceptValue` wins.

### Fire-call lifecycle — quantified baseline
Peak hot-path memory: ~44–48 `PreceptValue` slots, ~4,480 bytes stack traffic per Fire, ~88 bytes boundary-object GC allocation with slot-array pooling. Working copy is the donated next-version slot array, not a throwaway buffer.

### Construct-slot vs field-slot vocabulary — locked
`ParsedConstruct.Slots` / `SlotValue` are compile-time only. Runtime execution uses `SlotLayout` for field-name-to-slot-index mapping built at `Precept.From()`. Always say **construct slots** vs **field slots** explicitly when discussion spans both layers.

### Event args convert to `PreceptValue` at Fire boundary
Event args are schema-defined and typed. The evaluator consumes them as `PreceptValue` via `LOAD_ARG` opcode (slot-index dispatch, not name lookup). Arg data converts into ephemeral per-call arg slots at Fire entry; field data converts into persistent field slots at version construction/restore.

### `IArgBuilder` / `IFieldBuilder` typed ingress
Typed ingress is fluent and AOT-safe. `Fire()` / `Inspect()` accept `Action<IArgBuilder>`; `Create()` accepts `Action<IFieldBuilder>`. `IArgBuilder` materializes a `PreceptValue[]` arg slot array plus a `bool[]` presence mask (aligned to the arg slot array; `true` = set, `false` = absent).

### `IReadOnlyDictionary<string, object?>` convenience lane — CLOSED
Dictionary overloads are fully obsolete. Wire-format callers use `JsonElement`; in-process typed callers use the fluent builders. No third ingress lane exists anywhere on the public API.

### Single interpreter with diagnostic trace — no dual-path
There is one interpreter. The A+G stack-based opcode executor serves ALL consumers — production AND LS/MCP authoring feedback. Dual interpreters were explicitly rejected: two runtimes that must agree is a correctness liability; tooling that uses a different engine will diverge and mislead authors. Instead, the opcode loop emits per-step diagnostic records when trace mode is enabled. Trace record shape and attachment mechanism are open implementation seams.

### Dual-path eval-stack allocation — not adopted
A+G ships a single interpreter-shaped runtime. `stackalloc PreceptValue[32]` is the canonical evaluation stack pattern; no dual-path stack-allocation strategy was adopted for v1.

---

## Public API Surface

### JSON-first public API (Q2 / CC#25)
`JsonElement` is the primary type for all data/args parameters. Primary signatures: `Fire(string, JsonElement?)`, `Update(JsonElement)`, `Create(JsonElement?)`, `Restore(string?, JsonElement)` — all on `Version` or `Precept`. ~90% of real callers (ASP.NET Core, Azure Functions) receive `JsonElement` directly; the old dictionary API forced double-parse on every wire-format caller.

### `Version.Get<T>` typed field API (Q7 / CC#25)
`Version.Get<T>(string)` is the primary typed field accessor; raw indexers return `PreceptValue`. `Transitioned` / `Applied` carry `FiredArgs` with the same `Get<T>` + `PreceptValue` indexer pattern for event-arg egress. No string convenience overloads exist on the JSON API surface.

### `TypeRuntime` naming — final
`TypeRuntime` API surface: `FromJson` / `ToJson` / `FromClr` / `ToClr`. `TypeRuntime<T>` is the zero-boxing CLR ingress/egress path; typed `Get<T>` / `Set<T>` dispatch through those delegates.

### `TypeRuntimeMeta` JSON flow — `ReadJson` / `WriteJson`
Hot JSON path dispatches through `TypeRuntimeMeta.ReadJson(ref Utf8JsonReader, ref PreceptValue)` and `TypeRuntimeMeta.WriteJson(Utf8JsonWriter, PreceptValue)`. Active surface: `ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors`. `ExtractValue` / `StoreValue` / `ParseValue` are excluded from Fire/Inspect/Update hot paths.

### Typed `Restore` — removed
Typed `Restore` is not on the public surface. Restore is round-trip-faithful hydration from Precept's own serialized egress only; no typed-input restore lane exists.

### JSON boundary stays outside the evaluator
JSON parsing and lazy `ToJson()` egress are owned by the public API / `Version` conversion layer. The evaluator only sees typed `PreceptValue` data.

---

## Catalog System

### Catalog-driven thesis — foundational architecture principle
Domain knowledge is declared in catalogs; pipeline stages are generic machinery that reads it. This is not the traditional compiler model — it is the inverse. Pipeline stages must not encode per-member domain knowledge in switches; that belongs in catalog metadata. Explicitly rejected: the stale sentence that described adding a language feature as "add an enum member and fill an exhaustive switch."

### `TypeMeta` / `TypeRuntime` — catalog-owned JSON serialization and executor dispatch
`TypeMeta` gains catalog-owned JSON reader/writer delegates and executor arrays (`BinaryExecutors`, `UnaryExecutors` on `TypeRuntime`). Collection-field serializers compose from structural collection logic plus element-type delegates at build time. Persistence and execution behavior belong on catalog metadata — never on per-`TypeKind` consumer switches.

### Accessor layer — YAGNI until a concrete need appears
Generic `ParsedConstruct(ConstructMeta, SlotValue[], SourceSpan)` output is sufficient; dispatch is by slot-value shape, not construct identity. Typed accessor helpers are not built speculatively. Alternatives must be reconsidered before adding this layer.

### `ConstructMeta.Slots` dropped from radical parser design
Named parse positions live only as `Tag("name", rule)` nodes in `Grammar`. Tooling and doc consumers derive ordered named captures via `ExtractNamedCaptures(ParseRule)` at catalog startup. A second catalog field would be mirrored truth.

### `RoutingFamily` vs `ConstructFamily` terminology — locked
Four parse-scope buckets are "routing families"; shared-leader disambiguation groups are `ConstructFamily` entries. Header and Direct constructs never belong to a `ConstructFamily` (they have unique leading keywords and need no disambiguation metadata). Do not use unqualified "family" in this design area.

### Outcomes catalog — DU + catalog two-level pattern
Outcomes follow the same two-level architecture as actions: `OutcomeKind` + `OutcomeMeta` + `Outcomes.cs` at the metadata layer, `OutcomeNode` DU at the syntax-node layer. `no transition` composition rule is domain knowledge and belongs in metadata, not parser/tooling hardcodes. `Outcomes.All` must enumerate `transition`, `no transition`, and `reject`.

### GAP-035 — choice literal dispatch to `ChoiceLiteralTokens` metadata
Add nullable `TypeMeta.ChoiceLiteralTokens`; `ParseChoiceValue` derives signed numeric branch and literal-token validity from catalog metadata. Eliminates remaining `elemToken.Kind` identity switches from parser choice-literal dispatch. `TypeTrait.ChoiceElement` gates declaration validation; `ChoiceLiteralTokens` is the parse-time dispatch contract.

### GAP-040 — `bag.countof` uses `ElementParameterAccessor` DU subtype
`bag.countof(...)` parameter resolves to the bag element type via a dedicated `ElementParameterAccessor` DU subtype. The flat `ParameterType` axis is a three-shape problem: no parameter, fixed parameter type, element-type parameter. `TypeKind.Element` sentinel and boolean flags are both rejected.

### GAP-046 — CI function kinds as dedicated catalog entries
`FunctionKind.TildeStartsWith` / `FunctionKind.TildeEndsWith` plus `FunctionMeta.CIVariantOf` are added so CI functions exist as real function metadata, not only as `HasCIVariant` side effects. Parser behavior unchanged; the `~` null-denotation path derives CI-capable names from base-function `HasCIVariant`.

### `is set` / `is not set` — proposal-scope catalog work (pending owner sign-off)
Both are real semantic operators requiring postfix-aware metadata, `Arity.Postfix`, precedence, operand constraints, and result typing. Leaving them as uncataloged parser special-cases is a catalog-completeness bug. `ExpressionFormKind.PostfixOperation` is Frank's preferred shape. The `OperatorMeta` shape still needs owner sign-off before implementation.

---

## Compiler Pipeline

### `compiler-and-runtime-design.md` — narrative overview layer
This doc is the narrative overview over the 11 canonical stage docs, not a competing stage-spec source. Live parser contract: generic `ParsedConstruct(ConstructMeta, SlotValue[], SourceSpan)`; `TypeKind` resolves in the type checker; 13 catalogs including `ExpressionForms`.

### Catalog-driven pipeline extends to lexer, parser, and builder
Lexer is ~95% catalog-driven; radical parser reaches ~85% catalog-driven dispatch with Pratt loop as the irreducible kernel. Builder is the strongest proof-of-concept: mostly structural assembly; irreducible work is cross-construct name resolution. If genericized, `ConstructMeta` may grow a `ModelContribution` metadata hook.

### Parser rebuild (Path C) — active recommendation on design-risk grounds only
Path C (rebuild) remains active because it avoids stashed-guard, split-modifier, and variant-action design gaps. The schedule argument is withdrawn — rebuild and targeted improvements are roughly schedule-equivalent at current AI-assisted velocity. If Path B is reconsidered, stashed-guard pattern must be locked first.

### Parser.cs — approved `partial class` split for Slice 27
Three files: `Parser.cs` (shell/vocabulary/dispatch), `Parser.Declarations.cs` (declaration grammar and slot/type machinery), `Parser.Expressions.cs` (Pratt loop, atom parsers, expression helpers). `ParseSession` is a `ref struct` so helper-class alternatives are ruled out. `KeywordsValidAsMemberName` stays on outer `Parser` class (ref structs cannot own static fields).

### Annotation-bridge enforcement pattern — `[HandlesCatalogMember]` / `[HandlesCatalogExhaustively]`
Parser handlers advertise responsibility with `[HandlesCatalogMember]` (renamed from `[HandlesForm]`). PRECEPT0019 checks handler coverage across `Parser`, `TypeChecker`, `Evaluator`, `GraphAnalyzer`. `[HandlesCatalogExhaustively(typeof(T))]` is for distributed handlers; CS8509 is retained for centralized switches. Lexer does not get `[HandlesCatalogExhaustively]` (it produces token values from lookup tables, never dispatches on `TokenKind`).

### TypeChecker pre-slice requirements — locked
Pre-Slice 0: land full shape-only `SemanticIndex` and typed-record hierarchy first. Field storage is array-primary with a derived frozen name index (declaration order survives). Widening is single-hop; binary fallback order is deterministic (left, right, both); identifier resolution priority: bindings > args > fields. `TypedEditDeclaration` is placeholder-only.

### Checker D-15–D-25 design points — locked
Numeric literals bottom-up with context retry; event handlers get event-arg scope; function overload resolution follows one deterministic pipeline; `FieldScopeMode` gates forward references; Slice 6 stays unsplit; `TypedTransitionRow.ResolvedArgs` rejected as anti-mirroring. All 11 checker slices are implementation-ready with no unresolved design blockers.

### Collection-types catalog and parser — B1/B2/C3/G5 locked
B1: keep secondary action kinds in `Actions.All`, add `ActionMeta.PrimaryActionKind`, derive `ByTokenKind` from primary actions only. B2: explicit switch-arm maintenance, not stretching `[HandlesCatalogExhaustively]` onto `ActionSyntaxShape`. `remove F at N` handled before value parsing; `AppendBy` disambiguation stays syntactic; `countof`/`peekby` stay as member-name-legal compound accessors.

### Diagnostics — `UnexpectedKeyword` vs `InvalidCallTarget` — distinct
`UnexpectedKeyword` = declaration keyword in value-expression position. `InvalidCallTarget` = non-callable expression followed by `(...)`. Both are parse-stage Error severity with explicit emit sites.

### Language-consistency gap fixes — GAP-024 through GAP-033 closed
GAP-024 spec support for TypeQualifier on bag/list/log; GAP-025/026/028 catalog mismatches; GAP-029/030/031 parser hardcodes replaced with catalog-derived sets/lookups; GAP-032 proof requirements for `pow(integer, integer)`; GAP-033 stale `Notempty` documentation. Parser vocabulary and precedence helpers must derive from catalog metadata; `.` and `(` may remain intentional hardcodes (grammar structure, not surfaced operators).

### `EnsureClause` — `because` is a separate slot
`because` is a separate `BecauseClause` slot, not payload folded into `EnsureClause`. `StateEnsure` and `EventEnsure` treating `because` as anything other than a dedicated slot is a catalog defect.

### Event modifiers — individually slotted as `InitialMarker`
`InitialMarker` is the individual named slot for event-modifier surface. `terminal` remains `StateModifierMeta`, not an event modifier. Do not invent a collective event-modifier slot abstraction until multiple real event-modifier members exist.

---

## Language Surface

### `write all` — REMOVED from language
`write all` is removed entirely. Stateless precepts opt into mutability only through field-level `writable`. Supersedes any earlier record that `write all` survived as stateless sugar.

### Access-mode vocabulary — locked surface
Surface: `in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?`. `omit` is the separate structural-exclusion verb (removes field from structural schema). Access modes keep the field present and only constrain mutability. Guarded `omit` is prohibited (would make structural field presence data-dependent). Redundant access declarations are compile errors under `RedundantAccessMode`.

### `subset` / `disjoint`
Keep only for `set of <choice>` where the compiler can prove the closed-domain guarantee. Squash for open types (quantifiers already cover the runtime-only case).

### Bounded quantifiers — compatible with philosophy (owner-review only)
Bounded quantifiers are philosophically compatible (they unfold over statically finite collections) but philosophy text must not change without explicit owner sign-off.

### No-runtime-faults guarantee — spec aligned
Principle 10: compiler must prove safety or emit an obligation diagnostic. Principle 11: clean-compile guarantee extends across type, arithmetic, access, and range fault classes. Runtime traps are defensive redundancy for compiler-proven-unreachable paths only. A philosophy gap remains: `docs/philosophy.md` does not yet name evaluation-fault prevention as explicitly as invalid-entity-configuration prevention (flagged for owner, not auto-applied).

### Grammar-hierarchy markers — structural anchors vs slot badges
`◆` marks `ConstructFamily` sub-group headers (structural rows); `[A]` / `[O]` mark per-construct action/outcome slot badges. Different visual classes — not different members of the same icon family.

### ASCII diagrams preferred over Unicode in fixed-width topology boxes
Use plain ASCII `>` instead of `▶` in fixed-width topology boxes; monospace alignment breaks with ambiguous-width Unicode glyphs.

### `SyntaxTree` → `ConstructManifest` rename — executed (2026-05-04)
Shane ruled `ConstructManifest` as the canonical parser output artifact name. `ParsedSource` is the superseded advisory alternative. Rename executed: all canonical docs now use `ConstructManifest`; source code rename is a separate implementation task.

---

## Documentation

### `docs/language/precept-grammar.md` — canonical language-design guide
Established as the durable grammar reference. Lead with what the grammar is not; use flat constructs / keyword anchoring / named slots as the structural spine; keep linguistic model and grammar invariants in their own sections; quick-reference appendix for lookup mode. ASCII hierarchy diagrams preferred over Mermaid-style node graphs.

### Grammar anatomy section — representative, not exhaustive
§3 covers distinct slot and routing archetypes. Selected expansion set: `PreceptHeader`, `RuleDeclaration`, `AccessMode`, `StateAction`, `EventHandler`. `OmitDeclaration` and `EventEnsure` intentionally omitted (their slot shapes are legible from the chosen set).

### `docs/language/collection-types.md` — canonical collection-types reference
Covers shipped surface (`set`, `queue`, `stack`), actions, accessors, constraints, emptiness safety, inner-type behavior, `~string`, and diagnostic anchors. Also documents the design frontier: proposed quantifier predicates, `unique`, `notempty`, `subset`, `disjoint`, with eight open owner-sign-off questions. `bag`, `log`, `map` are high-priority candidates; `sortedset`/`priorityqueue` medium; `deque` low.

### Vision → spec migration — closed
Language spec is now the single canonical language document. Vision doc is archived at `docs/archive/language-design/precept-language-vision.md`. Two stale contradictions removed from spec (`with` listed as structural preposition; stale "root editability" wording).

### `compiler-and-runtime-design.md` catalog-first wording — corrected
The stale sentence describing extension as "add an enum member and fill an exhaustive switch" is durably rejected. Correct: adding a feature means adding a catalog entry; pipeline stages stay generic. A follow-up remains for the "Precept Innovations" callout box which still carries similar stale wording.

### Research validation integration pattern — locked
Research artifacts live in `research/language/`; design docs cite them in a `## Research Validation` section; working draft in `docs/working/` is marked superseded; `research/language/README.md` indexes the validation set.

### Doc-sync rule — spec, catalog text, and diagnostic tables must agree simultaneously
Language-surface wording changes are only done when spec, hover/catalog text, and the plan's referenced diagnostic tables all agree on the same user-facing story.

### Catalog gap register — migrated and archived
43 entries fully triaged. 23 pending gaps attributed into canonical open-question blocks across 9 docs; 3 resolved-in-source gaps marked closed; 5 already-captured confirmed. Register retired to `docs/working/Archived/catalog-gap-register-migrated.md`.

### Canonical doc audits — CC#25 Q1–Q10 and CC#2
Confirmed coverage of Q1, Q2, Q4, Q7, Q10 in canonical docs. Closed lagging gaps in `evaluator.md`, `result-types.md`, `precept-builder.md`, `compiler-and-runtime-design.md`, and `cross-cutting-decisions.md`. Durable audit rule: after runtime API updates, re-audit `result-types.md`, `evaluator.md`, and the cross-cutting register together.

---

## Tooling & Infrastructure

### Catalog-driven thesis deviations — only explicit tooling gaps
No silent architectural drift found across 11 canonical stage docs. Only real deviations: hand-authored TextMate grammar and hardcoded MCP `firePipeline` array. Follow-up: modifier grouping in MCP should derive from metadata shape; grammar generator remains the highest-leverage cleanup.

### Language server stays zero-per-construct
MCP stays above raw parse output, consuming catalogs, the semantic model, diagnostics, and runtime APIs rather than AST node classes. Language server remains zero-per-construct.

### Dapr hosting — actor model with pod-level cache
Both analyses converge: actor-hosted Precept instances with a pod-level compiled-definition cache, typed rehydration before guard evaluation, `Restore()` as the state-store boundary. Workflows are the wrong semantic fit for Precept entity execution.

---

## Process & Documentation Standards

### Record problems durably — not only in ephemeral output
Shane's durable directive: when implementation uncovers problems, agents must write them into the working plan or decisions inbox. Problems left only in ephemeral output are lost.

### `[HandlesCatalogMember]` rename — canonical
Renamed from `[HandlesForm]`. `[HandlesCatalogExhaustively(typeof(T))]` for distributed handler coverage; `[HandlesCatalogMember(kind)]` for per-member claims. Legacy `[HandlesForm]` wording is retained only as historical rename context.

### Canonical artifact names still in force
`TokenStream`, `ConstructManifest`, `ParsedConstruct`, `SemanticIndex`, `StateGraph`, `ProofLedger`, `Compilation`, and `Precept`.

### Combined Design v2 — structural revision and gap patch
13 stage-contract tables converted to labeled prose; two artifact tables merged; "How to read this document" added; §8 split; §9 moved to appendix. 10 missing design specifics added: action-shape model, constraint activation indexes, constraint evaluation matrix, constraint exposure tiers, proof strategy enumeration, proof/fault chain formula, earliest-knowable kind assignment, named anti-patterns, compile-time vs lowered artifact table, implementation action items.

### Philosophy changes require explicit owner approval
`docs/philosophy.md` must not be edited without explicit owner sign-off. If the runtime can do something the philosophy doesn't describe, or the philosophy claims something the runtime can't do, surface the gap and wait for direction.
