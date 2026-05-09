## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- UCUM / ISO 4217 gap analysis (2026-05-09): current `Types.cs` still trails the locked catalog architecture. Currency needs a structured `CurrencyCatalog` with `MinorUnit`; UOM should expand to the canonical Tier 1 set now while the full UCUM grammar parser stays deferred; the dimension registry needs a spec audit before more tooling work lands.
- ProofEngine architecture is now understood as catalog-driven and mechanically sound; the remaining meaningful risks were miswired diagnostics, fallback semantics, and missing end-to-end coverage rather than missing strategy families.
- Large implementation plans must be sourced from live code, not only the spec. Search constructor sites exhaustively before estimating record-shape changes such as `TypedField` / `TypedArg` metadata additions.
- `ProofSatisfaction` / declaration-carrier metadata and the proof-ledger output contract are independent axes: both must exist before proof execution work can begin.
- Closed-vocabulary syntax belongs in parser-time recognition; open-ended expressions stay as trees for later semantic work. Symbol binding remains distinct from type inference.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, SemanticIndex for semantic features.
- Grammar-generator and tooling docs must track the catalog-driven architecture faithfully; speculative or stale wording becomes harmful quickly.

## Recent Updates

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
