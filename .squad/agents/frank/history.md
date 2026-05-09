## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Typed literal framework architecture locked on 2026-05-09T11:11:01-04:00: unified framework APPROVED. `ContentValidation` DU on `TypeMeta` is the catalog hook; `TypedConstantValidation.Validate(...)` is the single static dispatcher replacing the inline `ValidateContent`/`ValidateNodaTime`/`ValidateClosedSet` methods in `TypeChecker.Expressions.cs`. No `ITypedConstantValidator` interface — static methods per domain validator. Temporal parser lives in `src/Precept/Language/Time/`, architecturally parallel to UCUM. Seven temporal literal forms (date, time, datetime, instant, zoneddatetime, timezone, temporal quantity). Three missing temporal types get `ContentValidation` entries (instant, timezone, zoneddatetime). Duration/period quantity syntax (`'30 days'`, `'72 hours'`) gets a dedicated `TemporalQuantityParser`. New DU subtypes: `UcumValidation`, `QuantityValidation`. `NodaTimeValidation` gains `TemporalLiteralKind` discriminator. Full architecture in `docs/working/frank-typed-literal-framework.md`.
- UCUM / ISO 4217 gap analysis on 2026-05-09T10:56:10.942-04:00:current `Types.cs` still trails the locked catalog architecture. Currency needs a structured `CurrencyCatalog` with `MinorUnit`; the decisive correction on the spike branch is to replace the UOM closed set with the real UCUM parser architecture rather than extending the placeholder set.
- UCUM parser architecture locked on 2026-05-09T10:56:10.942-04:00: the spike branch does not defer grammar work. Build the real parser in `src/Precept/Language/Ucum/`, keep authoritative source data in `src/Precept/Data/Ucum/`, treat Tier 1 as discovery only, replace `ClosedSetValidation` for `unitofmeasure` with a shared UCUM-backed validation path, remove `time` from the UCUM dimension partition, keep `count` as an annotated dimensionless business alias, and promote `speed`/`force` into the curated `DimensionCatalog` as first-class vector aliases.
- ProofEngine architecture is now understood as catalog-driven and mechanically sound; the remaining meaningful risks were miswired diagnostics, fallback semantics, and missing end-to-end coverage rather than missing strategy families.
- Large implementation plans must be sourced from live code, not only the spec. Search constructor sites exhaustively before estimating record-shape changes such as `TypedField` / `TypedArg` metadata additions.
- `ProofSatisfaction` / declaration-carrier metadata and the proof-ledger output contract are independent axes: both must exist before proof execution work can begin.
- Closed-vocabulary syntax belongs in parser-time recognition; open-ended expressions stay as trees for later semantic work. Symbol binding remains distinct from type inference.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, SemanticIndex for semantic features.
- Grammar-generator and tooling docs must track the catalog-driven architecture faithfully; speculative or stale wording becomes harmful quickly.

## Recent Updates

### 2026-05-09T15:26:09Z — Typed-literal and UCUM architecture durably recorded
- Scribe merged Frank's typed-literal framework and UCUM parser architecture into `.squad/decisions.md` as the canonical implementation direction.
- Durable boundary: `ContentValidation` stays the metadata hook, `TypedConstantValidation.Validate(...)` stays the static dispatcher, and both temporal + UCUM parsing now have one approved shared-language architecture.


### 2026-05-09T11:17:00-04:00 — Event-arg member reference scope decision locked
- Chose `variable.parameter.property.precept` as the dedicated TextMate scope for event-arg member references (e.g., `LoadParcel.Recipient`). Replaces `variable.other.property.precept` which incorrectly placed arg members on the field axis.
- Rationale: event-arg members are on the **parameter axis** — they access properties on event parameters, aligning with `variable.parameter.precept` (the arg name scope). The `.property` segment distinguishes member access from the arg name itself.
- Single-site change in `tools/Precept.GrammarGen/Program.cs` (capture group 3 of `eventArgReference` pattern). No catalog change — this is a structural pattern scope.
- Kramer-7's compound selector workaround (`meta.event-arg-ref.precept variable.other.property.precept → #9AD8E8`) should be reverted when the proper scope is implemented — it becomes dead code.
- Full spec in `docs/Working/frank-arg-member-scope.md`; inbox decision in `.squad/decisions/inbox/frank-arg-member-scope.md`.

### 2026-05-09T09:49:38Z — TypeChecker catalog-fix spec recorded
- `frank-13` locked the four-site TypeChecker catalog-driven design: CI enforcement metadata, `Constraints.ByToken`, `Modifiers.ByAccessToken`, and `Modifiers.ByAnchorToken`, with explicit file slices and regression anchors for implementation.

### 2026-05-08T22:54:50Z — ProofEngine spec closeout recorded
- All 18 ProofEngine gaps were resolved in the canonical spec. The durable direction is bounded constant folding for initial-state satisfiability, explicit `ObligationContext` capture at instantiation time, and generic proof reading from catalog/declaration metadata rather than per-kind folklore.

### 2026-05-08T21:22:17-04:00 — PE-G1 / PE-G2 / PE-G3 decisions locked
- Strategy 2 is now `Declaration Attribute Proof`, qualifier compatibility is Strategy 5, and the output contract is the `ProofLedger` family collocated beside the pipeline output type.

## Historical Summary

- 2026-05-08 graph/proof work locked the GraphAnalyzer baseline, consumed the remaining proof-engine design questions, and synchronized the compiler design docs back to the real implementation direction.
- Earlier May work established the canonical parser and TypeChecker trajectory: `TransitionRowOutcome` naming, parse-time handling for closed vocabularies, NameBinder ownership of forward references, and the requirement that working proposals flow back into canonical docs and the decision ledger.
- Use `.squad/decisions.md` for the full per-batch chronology and `research/` / `docs/` for the surviving rationale behind each locked design.

### 2026-05-09T15:21:46Z — Scribe merged Frank's 2026-05-09 design notes
- `.squad/decisions.md` now carries Frank's durable rulings for the event-arg member scope, the typed-literal validation framework, and the UCUM parser architecture.
- The recorded throughline stays catalog-first: structural grammar scopes stay out of `TokenMeta`, typed-literal validation stays anchored on `ContentValidation`, and UCUM ships as a shared language subsystem rather than a closed-set placeholder.
