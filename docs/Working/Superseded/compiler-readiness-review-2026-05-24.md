> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.

# Compiler Readiness Review — 2026-05-24

**Status**: Complete — awaiting owner triage on P0s
**Author**: Claude (sequential 12-stage review per `~/.claude/plans/before-we-move-on-whimsical-gray.md`, with 4 parallel sub-agent deep-reads on `docs/language/`)
**Scope gate**: blocks runtime implementation
**Parallel work**: sample authoring runs in a separate session; sample-side bugs go to [`bugs.md`](bugs.md). Do not modify samples from this session.
**Appendix files** (full sub-agent reports): [`compiler-readiness-review-2026-05-24-appendices/`](compiler-readiness-review-2026-05-24-appendices/)

---

## 1. Executive summary

**Headline**: The compiler pipeline implementation itself is in good shape — the major architectural commitments hold (catalog-driven dispatch, separation of stages, exhaustiveness analyzers, the proof engine's interval-and-qualifier story). The drift is concentrated in two surfaces:

1. **Documentation lags implementation badly** — Status fields claim "pending" for shipped features (Parser slices 1-26 documented as 1-4; primitive/business-domain types as "Designed"/"Proposal" despite shipping; tooling-surface.md status table months stale); doc/code count mismatches across all 14 catalogs in `catalog-system.md`; ~16 of 22+ analyzer rules undocumented; `<-` BackArrow token missing from spec § 1.1.
2. **Spec promises features the implementation doesn't deliver** — § 0.6 Proof Engine contract lists 13 guarantees; **5 are silently unimplemented** (contradictory/vacuous rule detection, dead/tautological guard detection, sharpened routing diagnostics); § 0.5 names ~11 graph-analyzer modifiers (`guarded`, `entry`, `isolated`, `universal`, `milestone`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`) — **none exist** (owner decision 2026-05-24: defer all 10 graph-analyzer modifiers; **drop `milestone` entirely** as undocumented synonym of shipped `required`); `set of money in 'USD'` qualified inner types unparseable; mandatory `because` on ensures not enforced despite non-negotiable principle; `'3 days'` won't resolve to `Duration` in `instant` context; `CollectionOperationOnScalar`/`ScalarOperationOnCollection` diagnostics defined but **never emitted anywhere**; `Operations.Resolve` documented with full code sample but doesn't exist.

**Plus a real production bug**: invalid temporal input (`'2026-13-01'`) crashes the MCP server via unhandled exception (F-LANG-SPEC-10).

**Findings counts**:
- **P0 (blocks production-ready / runtime work)**: **13**
  - F-LANG-CAT-02 (AmbiguousDispatch false-resolved), F-LANG-CAT-08 (ProofRequirementKind 5→10)
  - F-LANG-SPEC-01 (because on ensures), F-LANG-SPEC-02 (UnsatisfiableGuard unwired), F-LANG-SPEC-10 (temporal crash)
  - F-LANG-TEMP-01, F-LANG-TEMP-02 (`'3 days'`/`'2 weeks'` in instant context)
  - F-LANG-COLL-04 (.at(N) bounds), F-LANG-COLL-05 (log-by uniqueness), F-LANG-COLL-06 (qualified inner types), F-LANG-COLL-07 (two-field quantifier binding), F-LANG-COLL-08 (PRE0047/0048 never emitted)
  - F-LANG-GRAM-02 (EventRowReject name)
- **P1 (significant bug or drift on Implemented surface)**: ~52
- **P2 (polish, minor gap)**: ~30
- **P3 (informational)**: 3
- **Total**: ~98

**Bottom line**: The user's stated concern — "I keep finding things that were not fully implemented" — is corroborated systematically. The biggest risk for the runtime phase isn't the compiler's correctness; it's that the runtime work will inherit a documentation surface that doesn't reflect what's actually been built, which will compound during runtime authoring. Recommendation: burn down P0s and Wave 1 P1s (doc Status truth-up) before opening the runtime gate.

---

## 1a. Owner Decisions Captured (Wave 0)

Decisions made during the 2026-05-24 triage conversation. Drives the in/out-of-MVP scope.

### Scope of MVP
**Decision**: The runtime MVP must accommodate **all planned features**, with the single exception of graph-analyzer modifier extensions (F-LANG-SPEC-08). Every other documented spec capability is in MVP.

This means in-scope: F-LANG-COLL-06 (qualified inner types), F-LANG-COLL-07 (two-field quantifier binding), F-LANG-COLL-08 (action ApplicableTo enforcement), F-LANG-COLL-04/05/09 (proof-engine extensions for collection ops), F-LANG-SPEC-02/03/04/05/06 (proof-engine satisfiability + CI bidirectionality), F-LANG-SPEC-10 (temporal-crash fix), F-LANG-TEMP-01/02/03/05/06/07 (temporal extensions), F-LANG-BIZ-02/03/04/06/07/09 (business-domain extensions including new `units` block construct), F-API-01 (typed descriptors), all per-stage P1/P2 findings, and the full catalog-system.md rewrite.

### Graph-analyzer modifiers (F-LANG-SPEC-08)
**Decision**: Defer all 10 graph-analyzer modifiers (`guarded`, `entry`, `isolated`, `universal`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`) to a future graph-analyzer roadmap. **Rationale**: confirmed none require runtime support — they're pure compile-time graph properties; metadata exposure (if needed later) is additive on descriptor types. Safe to ship as Compiler 1.1 after runtime is in.

**Action**: spec § 0.5 needs a rewrite pass to mark these features clearly forward-looking. Move to a separate "Future Graph Analyzer Roadmap" doc or a clearly-flagged sub-section.

### `milestone` modifier
**Decision**: **Drop entirely** from the spec — do not defer, do not implement. **Rationale**: confirmed via spec read (`precept-language-spec.md:186`) that `milestone` is an undocumented synonym of the already-shipped `required` modifier (same dominator analysis, no articulated semantic distinction). A future "milestone tracking / reporting" feature would be meaningfully different and would deserve its own design pass — and could naturally claim the `milestone` name then without collision.

**Action**: spec § 0.5 #5 rewritten to read "Dominator analysis. Required for `required`" — remove `milestone` reference. Sweep all `docs/language/` and `docs/compiler/` for `milestone` modifier references and remove.

### Decisions resolved 2026-05-24 (8 items)

| # | Finding | Decision |
|---|---|---|
| 1 | F-LANG-CAT-01 (HoverDescription) | Rewrite CC#19 entry: field shipped to 5 of 6 catalogs (Operator, Function, Type, Modifier, Action); deliberately NOT on TokenMeta — token-level hover routes through higher-level catalog the token resolves to. Promote `docs/Working/Archive/hover-design.md` (V7) + `interval-hover-design.md` (V1) to `docs/tooling/language-server.md § 7.4`. |
| 2 | F-LANG-CAT-02 (AmbiguousDispatch) | **Drop entirely.** Concept obsoleted by 3 architectural decisions: grammar split (Slice 8b — action/reject construct routing), Decision 5 (no state-agnostic handlers in stateful precepts), first-match-wins routing (spec § 1808). No `DiagnosticCode.AmbiguousDispatch` or `FaultCode.AmbiguousDispatch` will ship. Sweep 3 docs: `catalog-system.md` (3 places), `diagnostic-system.md:352`, `evaluator.md` (lines 437, 627, 1556, 1982 — delete; keep line 831 which is about inspection prospect, not dispatch). |
| 3 | F-LANG-CAT-20 (IsUserFacing) | Rewrite CC#16 entry: shipped via `TypeMeta.Token != null` structural equivalence (internal types `Error`/`StateRef` carry `Token: null`). Filter site: `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs:518`. No `IsUserFacing` field added — structural equivalent is permanent solution. |
| 4 | F-LANG-CAT-15 (Operations.Resolve) | **Implement** in Phase 2: 4-line wrapper `Operations.Resolve(op, lhs, rhs) = DisambiguateCandidates(FindCandidates(op, lhs, rhs))`. Move `DisambiguateCandidates` from `TypeChecker.Expressions.cs:922` (currently private) to `Operations.cs` (public). Update 5 TC call sites at lines 885, 891, 898, 907, 986. Spec § 5 then matches code. |
| 5 | F-LANG-CAT-06 (Construct Slot Model) | Rewrite in pointer-philosophy mode: precise "no block-structured constructs" claim (acknowledge interpolation `{...}` is the only brace-delimited form, holds nested expression not nested construct) + pointer to `ConstructSlot.cs` for slot shape + pointer to `docs/compiler/grammar-generator.md` for grammar generation (canonical) + accurate two-layer story (TextMate via Tokens × SemanticTokenTypes; LSP semantic tokens via SemanticIndex + interpolation traversal). |
| 6 | F-LANG-CAT-23 (Roslyn rules) | Pointer-philosophy rewrite of § Roslyn Enforcement Layer: categorized table (10 rows: Fault/diagnostic conventions, GetMeta exhaustiveness, per-catalog cross-reference, semantic enum zero-slot, pipeline exhaustiveness, operator/token integrity, catalog DU discipline, diagnostic emission gate, diagnostic test gate, allow-list hygiene) instead of per-rule enumeration. Lift why from `docs/Working/Archive/diagnostic-enforcement.md` (1227 lines) for Gates 1/2 and catalog DU discipline rationale. Pointer to per-rule `PreceptNNNN*.cs` files. |
| 7 | F-LANG-CAT-26 (SemanticTokenTypes status) | **14th catalog** per Slice 10 design (`docs/Working/Archive/language-server-implementation-plan.md:641, 645`). Add new § to `catalog-system.md` with conceptual content (one-field architecture rationale, two-consumer note) + pointer to `src/Precept/Language/SemanticTokenTypes.cs` for membership/meta. Sweep and resolve the 13-vs-14 narrative inconsistency. |
| 8 | F-LEX-02 (Four-leg rationale policy) | **Prospective only via `/lifecycle-design` skill.** Existing stage doc § 11 entries grandfather with Decision + Rationale; no required backfill. Archive-sourced decisions lift whatever depth source provides (no fabrication). CONTRIBUTING.md states policy explicitly. `/lifecycle-audit` may flag gaps but doesn't auto-require backfill. |

### Open decisions still required (gating later phases)

- **F-LANG-SPEC-01** (mandatory `because` on ensures): tighten enforcement (catalog flip on `Constructs.cs:133, 174`) OR amend Principle 9 to scope to rule only? Gates Phase 2 or 3.
- **F-LANG-TEMP-08** (`zoneddatetime ± period`): remove from `OperationKind.cs:91-92` (doc says compile error) OR remove rejection from doc? Gates Phase 3.

**Per-stage traffic light** (placeholder; ✅ healthy / ⚠️ drift / 🔴 issues):

| # | Stage | Docs | Impl | Tests | Notes |
|---|---|---|---|---|---|
| 1 | Lexer | ⚠️ (cross-refs) | ✅ | ✅ | 1 P1 doc / 2 P2 doc |
| 2 | Parser | 🔴 (stale Status) | ✅ | ✅ | 3 P1 / 1 P2 |
| 3 | NameBinder | ✅ | ✅ | ✅ | 1 P2 |
| 4 | TypeChecker | ⚠️ (Gap 1 stale) | ⚠️ (TypedConstants dispatch) | ✅ | 2 P1 / 2 P2 |
| 5 | GraphAnalyzer | ✅ | ✅ | ✅ | 0 new (1 F-PAR-04 family) |
| 6 | ProofEngine | ✅ | ✅ | ✅ | 1 P2 |
| 7 | Diagnostic system | ✅ | ✅ | ✅* | 0 (\* scenario depth in Stage 13) |
| 8 | Catalog system | ✅ | ⚠️ (audit delta needed) | ✅ | **1 P1 load-bearing** |
| 9 | Language surface coverage (docs/language/ ↔ impl) | 🔴 (**13 P0** across spec, catalog, types, collections) | ⚠️ (multiple unimpl features) | ⚠️ (1 failing + temp crash) | **76 findings — dominates review** |
| 10 | Compilation API + Runtime glue (compile-time) | ✅ | ⚠️ (string-keyed) | ✅ | 1 P1 (precursor to runtime) |
| 11 | MCP compiler tools | ✅ | ⚠️ (BUG-007) | ✅ | 1 P1 (corrected post-bugs.md integration) |
| 12 | LS compiler features | 🔴 (stale table) | ✅ | 🔴 (3 fail) | 2 P1 / 1 P2 |

---

## 2. Methodology

Sequential, single-session review carrying full upstream context. Each stage examined under three lenses (docs / implementation / tests) against CLAUDE.md non-negotiables:

- **Catalog discipline** — no parallel keyword lists; no kind-switch over enum identity for per-member behavior; DUs for varying shapes; never hand-edit `tmLanguage.json`.
- **Documentation sync** — Status field matches reality; 16-section template adhered to; per-decision rationale complete (Rationale, Alternatives, Precedent, Tradeoff).
- **Language-surface coverage** — for every feature documented in `docs/language/` (language spec, type docs, grammar), a catalog entry and implementation exists with test coverage; conversely, every implemented catalog entry / operator / function / type / modifier appears in the right `docs/language/` doc. Drift in either direction is logged.
- **MCP sync** — tool wrappers ~30 LOC; logic in core.
- **Test conventions** — xUnit + FluentAssertions; diagnostic codes have both structural and scenario coverage.

**Out of scope**:
- Runtime execution (`Runtime/Evaluator.cs` Fire/Update/Restore, `Version.cs` mutators, `Precept.cs` getters) — next phase.
- Samples — owned by parallel session.
- `docs/philosophy.md` edits — flag gaps for owner approval.
- VS Code extension TypeScript/UI — unless it consumes compiler output incorrectly.

**Baseline**: `dotnet build` clean (2 warnings in LS tests — see F-LS-NN). `dotnet test` baseline recorded below.

---

## 3. Findings

### P0 — must fix before runtime work

#### F-LANG-SPEC-01 — Mandatory `because` clause not enforced on `ensure` declarations (non-negotiable Principle 9 violated)
**Severity**: P0
**Area**: Language surface — precept-language-spec.md
**Evidence**: `docs/language/precept-language-spec.md:20, 106` declare as non-negotiable: "every rule and ensure must carry a syntactically required `because` clause." `src/Precept/Language/Constructs.cs:110` uses `SlotBecauseClause` (required) for `RuleDeclaration` — correct. `Constructs.cs:133, 174` uses `SlotOptBecauseClause` (IsRequired: false) for `StateEnsure` and `EventEnsure` — wrong. MCP `precept_compile` verified: `in Draft ensure Score >= 0` (no because) compiles clean with zero errors.
**Description**: This violates a non-negotiable principle elevated to an "IMPORTANT" callout near the top of the spec. The implementation only enforces it for `rule`. State and event ensures silently accept declarations missing a rationale clause. The premise of "one file complete rules with traceable reasoning" is undermined whenever an author writes a guarded ensure without explanation.
**Recommended fix**: Either (a) promote `because` to a required slot on `StateEnsure` and `EventEnsure` (catalog flip + parser already emits `ExpectedToken` for missing required slots), or (b) amend spec/principle to scope mandatory rationale to `rule` only. Option (a) is consistent with the stated principle.
**Effort**: S
**Depends on**: owner decision

#### F-LANG-SPEC-02 — Dead-guard detection (`UnsatisfiableGuard` PRE0082) is wholly unimplemented
**Severity**: P0
**Area**: Language surface — precept-language-spec.md
**Evidence**: `docs/language/precept-language-spec.md:209` (§ 0.6 #9) declares dead-guard detection as a core proof obligation. `DiagnosticCode.UnsatisfiableGuard = 82` defined in `src/Precept/Language/DiagnosticCode.cs:256`. Message template exists in `Diagnostics.cs:764`. **`src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs:74` explicitly lists `"UnsatisfiableGuard", // no emission site wired`**. MCP verified: `field Score as number default 50 min 0 max 100` + `from Draft on Submit when Score > 200 -> transition Submitted` compiles clean — unreachable row silently accepted.
**Description**: A flagship proof-engine guarantee — "rows with provably-false guards are flagged" — exists only as an unused enum value. Authors writing structurally unreachable rows receive no feedback. This breaks the "static completeness" promise of § 0.1 #11.
**Recommended fix**: Wire a proof-engine pass evaluating each `when` guard's interval against field constraints; emit PRE0082 when interval provably empty. Reuse `ProofEngine.Intervals.cs` infrastructure.
**Effort**: M
**Depends on**: none

#### F-LANG-SPEC-10 — Temporal validation diagnostics unwired; invalid date inputs CRASH the MCP server
**Severity**: P0 (production robustness)
**Area**: Language surface + Implementation — temporal type checker
**Evidence**: Spec §3.3 specifies content validation table for `date`/`time`/`instant` etc. `DiagnosticCode` reserves PRE0055-58 for these failure modes. `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs:69-72` lists all four as `"no emission site wired"`. MCP `precept_compile` verification: multiple invalid-date / invalid-time inputs (`'2026-13-01'`, `'2026-02-31'`, `'bad-date'`, `'25:00:00'`, `'2026-01-01T10:00:00'` for instant) all return `An error occurred invoking 'precept_compile'` — i.e., unhandled exception path. Simple valid cases work, so MCP isn't generally broken — temporal validation path specifically throws.
**Description**: Both a spec compliance gap AND a robustness defect. Invalid input does not produce `InvalidTypedConstantContent` (PRE0053, also unwired) or any structured diagnostic — it throws, which the MCP layer surfaces as an opaque "error occurred." Any author submitting a typo'd date crashes the compiler service.
**Recommended fix**: Wire structured validation that emits the appropriate `InvalidDateValue` / `InvalidDateFormat` / `InvalidTimeValue` / `InvalidInstantFormat` / `InvalidTypedConstantContent` diagnostic instead of throwing. Validators exist in `TypedConstantValidation.cs` — needs hookup and exception path closed.
**Effort**: M
**Depends on**: none

#### F-LANG-CAT-02 — `FaultCode.AmbiguousDispatch` claimed "✅ Resolved" but does not exist
**Severity**: P0
**Area**: Language surface — catalog-system.md
**Evidence**: `docs/language/catalog-system.md:1869-1872, 2442-2452` declares CC#13 as "✅ Resolved 2026-05-06" with all four implementation checklist items checked off. `grep AmbiguousDispatch` against `src/Precept/Language/FaultCode.cs` and `src/Precept/Language/DiagnosticCode.cs` returns **zero matches**. Cross-references to `docs/compiler/diagnostic-system.md` and `docs/runtime/evaluator.md` exist.
**Description**: The canonical catalog doc carries a load-bearing false claim. Either fire-dispatch ambiguity detection was never implemented despite the resolution checklist, or it was implemented under a different name and the doc wasn't updated. This is the most striking instance of the doc/code-drift pattern in the entire review — the doc literally says "done" for something that isn't there.
**Recommended fix**: Verify with owner whether the feature was intentionally renamed/dropped. If dropped: revert all four checkmarks to `[ ]` and reword the status. If pending: implement `FaultCode.AmbiguousDispatch` + matching `DiagnosticCode.AmbiguousDispatch` + arm in `Diagnostics.GetMeta` + Fail call site in evaluator.
**Effort**: S (status reversal) or M (full implementation)
**Depends on**: owner decision

#### F-LANG-CAT-08 — `ProofRequirementKind` documented as 5 members but actually has 10
**Severity**: P0
**Area**: Language surface — catalog-system.md
**Evidence**: `docs/language/catalog-system.md:260, 624, 1790` (all say "5") vs. `src/Precept/Language/ProofRequirementKind.cs` with **10 members**. The 5 new kinds are `QualifierChain`, `IntervalContainment`, `LengthContainment`, `CountContainment`, `KeyPresence`. Each has matching `ProofRequirement` subtype, `ProofRequirementMeta` subtype, and partial `ProofSatisfaction` subtype coverage. Schema Anatomy (`catalog-system.md:511-575`) shows 5 meta subtypes and 5 instance subtypes; reality is 10 of each.
**Description**: The entire mental model of the proof system in the doc is half the actual surface. Five new proof requirement kinds have shipped with full DU representation, and the canonical doc is silent. A reader (or AI agent) using `catalog-system.md` to understand what proof obligations the engine handles will conclude the system handles half the cases it actually does. This is at the heart of "documented but unclear what's implemented."
**Recommended fix**: Substantial rewrite of the `ProofRequirements` Schema Anatomy block and §11 Catalog Inventory: enumerate all 10 kinds; expand the Mermaid diagrams; describe the new instance shapes (most carry `TargetField`, `Declared*Min/Max` fields); update the "Valid subjects and requirement types per catalog entry" matrix to cover the new kinds; update the "Complete proof obligation inventory" table.
**Effort**: L
**Depends on**: none

#### F-LANG-COLL-08 — `CollectionOperationOnScalar` and `ScalarOperationOnCollection` codes defined but never emitted
**Severity**: P0
**Area**: Language surface — collection-types.md + Type checker
**Evidence**: `docs/language/collection-types.md` cites `CollectionOperationOnScalar` (PRE0047) and `ScalarOperationOnCollection` (PRE0048) **25+ times** across nine collection kinds for cross-kind action safety. `DiagnosticCode.cs:99-100` defines the codes. `Diagnostics.cs:457-465` defines the text. **`grep -rn` shows zero `Diagnostics.Create(DiagnosticCode.CollectionOperationOnScalar, …)` calls anywhere in `src/Precept/`.** The TypeChecker's action resolution path at `TypeChecker.Expressions.Callables.cs:107` reads `Actions.GetMeta(parsedAction.Kind).ProofRequirements` but **never reads `ApplicableTo`** — so applying `enqueue` to a `set` field, `push` to a `queue`, `clear` to a `log`, or any wrong-kind action produces no diagnostic at the action level.
**Description**: Half the documented type-error matrix is vacuous. The doc presents structural safety guarantees ("the type checker rejects `enqueue MyStack X`") that the implementation simply doesn't deliver. This is the single most load-bearing P0 in the collections layer — every reject case across nine collection types × five-to-six wrong actions each silently passes.
**Recommended fix**: In `ResolveAction` (TC.Callables.cs:102), after `ResolveActionTarget` resolves the field, look up `Actions.GetMeta(parsedAction.Kind).ApplicableTo` and verify the target's `TypeKind` is admitted. Emit `CollectionOperationOnScalar` for wrong-kind collection actions (e.g., `enqueue` on `set`); emit `ScalarOperationOnCollection` for scalar assignment on collection field. Add a regression matrix test (one per documented reject case in collection-types.md).
**Effort**: M
**Depends on**: none

#### F-LANG-COLL-06 — Qualified inner types in collections (`set of money in 'USD'`) cannot be parsed
**Severity**: P0
**Area**: Language surface — collection-types.md + Parser
**Evidence**: `docs/language/collection-types.md:535-596` devotes ~60 lines to qualified inner types: `set of money in 'USD'`, `set of quantity in 'kg'`, `set of quantity of 'length'`, `set of price in 'USD' of 'mass'`, `set of exchangerate in 'USD' to 'EUR'`. `src/Precept/Pipeline/Parser.Types.cs:203-230` (`ParseInnerTypeReference`) reads only a bare type token (optionally `~` prefix) — no call to `TryParseQualifiers`, no consumption of `in`/`of`/`to` qualifier prepositions. Zero tests under `test/` exercise these constructs; zero samples use them.
**Description**: A documented Level-A feature (per the doc structure) is unimplemented. Any author trying to use `set of money in 'USD'` will get a parser error.
**Recommended fix**: Extend `ParseInnerTypeReference` to invoke `TryParseQualifiers` against the looked-up `TypeMeta`. Propagate qualifiers through to the element type so dimension-qualified inner type `.min`/`.max` performs SI-base normalization. Add a sample (`samples/qualified-collection-inner-types.precept`) and tests.
**Effort**: L
**Depends on**: none

#### F-LANG-COLL-07 — `queue of T by P` two-field quantifier binding (`.value`/`.by`) not implemented
**Severity**: P0
**Area**: Language surface — collection-types.md + Type checker
**Evidence**: `collection-types.md:1175-1214` documents two-field binding shape: `binding.value` (T), `binding.by` (P). Examples: `claim.by == "low"`, `claim.value == TargetClaimId`. `TypeChecker.Expressions.Callables.cs:385-433` (`ResolveQuantifier`) pushes a single `(name, elementType, isCI)` tuple — no projection. `claim.value` / `claim.by` would attempt accessor lookup on the element type and fail with `InvalidMemberAccess`.
**Description**: Any rule of the documented shape `rule no claim in ClaimQueue (claim.by == "critical")` is a compile error today. The doc itself flags this in its Open Questions §1255.
**Recommended fix**: Extend quantifier binding shape — represent two-type-parameter collection element bindings as a structural projection with `value` (T) and `by` (P) accessors. Wire `ResolveIdentifier`/`ResolveMemberAccess` to recognize the projection. Add tests.
**Effort**: L
**Depends on**: none

#### F-LANG-COLL-04 — `.at(N)` index-bounds proof obligation is missing
**Severity**: P0
**Area**: Language surface — collection-types.md + Proof engine
**Evidence**: `collection-types.md:319, 399, 677-680, 1017` claims `.at(N)` requires `N >= 0 and N < F.count`. `src/Precept/Language/Types.cs:205-211 (Log), 229-235 (LogBy), 259-265 (List)` declare a single `NumericProofRequirement(self.count > 0, ..., "Index must be within bounds")` — a non-empty check, **not** an index-bounds check. A program writing `set X = Items.at(99)` on a 3-element list compiles cleanly as long as the list is non-empty.
**Description**: A documented compile-time safety guarantee is silently absent. The proof engine accepts out-of-bounds indices.
**Recommended fix**: Introduce `IndexBoundsProofRequirement` (subject = index expression, lower = 0, upper = `F.count` exclusive). Attach to `.at` on Log/LogBy/List. Wire proof engine to discharge from `when N >= 0 and N < F.count` guards. Update tests.
**Effort**: L
**Depends on**: none

#### F-LANG-COLL-05 — `log of T by P` append uniqueness has no static obligation
**Severity**: P0
**Area**: Language surface — collection-types.md + Proof engine
**Evidence**: `collection-types.md:366, 374, 413` claims `append F Expr by P` on `log of T by P` requires `when not (F contains P)` guard, with `UnguardedCollectionAccess` raised when absent. `src/Precept/Language/Actions.cs:145-152` (`AppendBy`) has **no `ProofRequirements`** — only an implicit type-shape requirement.
**Description**: An unguarded `append AuditLog X by P` against a duplicate `P` is statically accepted; would only surface (if at all) at runtime, contradicting the compile-time prevention claim.
**Recommended fix**: Add `KeyAbsenceProofRequirement` (or equivalent) to `ActionKind.AppendBy` requiring `not (F contains P)`. Extend `ProofEngine.Diagnostics.cs` to map undischarged to `UnguardedCollectionAccess` (or new `DuplicateOrderingKey` code). Add positive and negative tests.
**Effort**: L
**Depends on**: none

#### F-LANG-GRAM-02 — `EventRowReject` referenced 4× in grammar doc but doesn't exist
**Severity**: P0
**Area**: Language surface — precept-grammar.md
**Evidence**: `docs/language/precept-grammar.md:228, 427, 473, 901` all reference `EventRowReject`. `src/Precept/Language/ConstructKind.cs` has `EventRow` (12), `ConstructionRow` (19), `ConstructionRowReject` (20), `TransitionRowReject` (21) — **no `EventRowReject`**. The corresponding catalog entry is `ConstructionRowReject`. `Parser.cs:284-317` (`ResolveRejectVariant`) promotes `EventRow` → `ConstructionRowReject`. `SemanticIndex.cs:499` does use `TypedEventRowReject` (correct name on the typed side), adding to the confusion.
**Description**: The grammar doc's vocabulary is wrong. A doc reader searching the code for `EventRowReject` finds nothing — they must reverse-engineer that `ConstructionRowReject` is the parsed-side identity. Naming collisions across pipeline stages (parser uses `ConstructionRow*`, type checker uses `EventRow*Reject`) compound the problem.
**Recommended fix**: Rename `ConstructKind.ConstructionRowReject` → `ConstructKind.EventRowReject` in `ConstructKind.cs`. Update `Constructs.cs`, `Parser.cs`, `TypeChecker.Normalization.cs`, `GraphAnalyzer.cs:762`. Then grammar doc matches reality.
**Effort**: M
**Depends on**: none

#### F-LANG-TEMP-01 — `'3 days'` cannot resolve to `Duration` in `instant` context (flagship example broken)
**Severity**: P0
**Area**: Language surface — temporal-type-system.md + Implementation
**Evidence**: `temporal-type-system.md:570-585, 1138-1139` claim `instant ± '3 days'` resolves to `Duration.FromDays(3)`. `src/Precept/Language/Time/TemporalUnits.cs:25` declares `day` with `PeriodFactory: Period.FromDays` and `DurationFactory: null`. `TemporalQuantityParser.cs:41-50` dispatches strictly: if `PeriodFactory` is non-null it builds a Period, never a Duration. The result is always `Period`. `Operations.cs:322-332` instant supports only `instant ± duration` — `instant + Period` is a type error.
**Description**: A flagship example in the doc (`instant + '3 days'` for elapsed-time arithmetic) is unimplementable as written. Context-dependent unit resolution for `days`/`weeks` in timeline contexts is not wired through. Same issue applies to `'2 weeks'` (F-LANG-TEMP-02).
**Recommended fix**: Either (a) update `TemporalUnits.cs` to give `day` and `week` both factories and have parser take context-expected-type parameter, or (b) revise doc to declare `instant + 'N days'` a type error and `instant + 'N hours'` the only timeline-arithmetic path. Either is a real product decision.
**Effort**: M (option a) or S (option b — doc update)
**Depends on**: owner decision

### P1 — should fix in remediation phase

#### F-PAR-03 — Parser doc Status field stale by ~22 slices
**Severity**: P1
**Area**: Parser (docs)
**Evidence**: `docs/compiler/parser.md:5-7` declares "Implementation: Complete — Slices 1–4." Git log shows shipped work through Slice 26 (`Slice 26 — event arg default resolution`, `Slice 25 — field-default proof coverage`, etc.) and Slice 8b for construction rows.
**Description**: The most load-bearing field in a stage doc — the Status header — is many months out of date. This single drift point is the canonical example of the "documented but unclear what's implemented" problem the user has been hitting. The doc body talks about Pratt expression parsing as a resolved decision (line 7, 296-300) so the body has been maintained; only the Status field was missed.
**Recommended fix**: Update Status to reflect actual implementation state. Either drop slice numbering (it's a transient construct that decays fast) or list the slice range that's actually shipped. Cross-reference git log to confirm. Likely: `**Implementation:** Implemented; tracked slices 1–26 shipped`.
**Effort**: S
**Depends on**: none

#### F-PAR-02 — Parser doc has unresolved Open Question flagged 2026-05-04
**Severity**: P1
**Area**: Parser (docs)
**Evidence**: `docs/compiler/parser.md:359-361` — `> **Open Question:** ConstructManifest as a graph-analyzer input ... The pipeline overview needs to decide whether that edge is obsolete ... *Flagged: 2026-05-04*`
**Description**: Three-week-old open question on an "Implemented" stage. The question depends on a "pipeline overview" doc that does not exist (corroborates F-LEX-01). Either the question is now answerable from code (does the graph analyzer actually consume `ConstructManifest` directly?) or it's a real architectural ambiguity that needs resolution before the runtime gate.
**Recommended fix**: Trace the actual dependency in code (`src/Precept/Pipeline/GraphAnalyzer.cs` — does it read `ConstructManifest` or only `SemanticIndex`?). Update the doc with the answer; remove the open question.
**Effort**: S (likely just trace+document)
**Depends on**: none

#### F-PAR-01 — Stale Slice-0 TODOs in parser code reference shipped milestone
**Severity**: P1
**Area**: Parser (impl)
**Evidence**: `src/Precept/Pipeline/Parser.cs:370` — `// TODO(allow-list): remove PRE0015 from allow-list after Slice 0 ships`; `Parser.cs:396` — `// TODO(allow-list): remove PRE0013 from allow-list after Slice 0 ships`.
**Description**: Slice 0 shipped long ago (git log shows Slices 1–26 all shipped). These TODOs are stale: PRE0015 (`PreEventGuardNotAllowed`) and PRE0013 (`OmitDoesNotSupportGuard`) are now permanent diagnostic gates, not temporary allow-list entries. The "allow-list" terminology no longer maps to current architecture. Additionally, neither code is documented in `docs/compiler/parser.md` — only 2 mentions exist in `diagnostic-system.md`.
**Recommended fix**: (1) Delete both TODO comments and rewrite the surrounding comment to describe the permanent guard-gate behavior. (2) Document the guard-gate failure modes in `parser.md` § Failure Modes (`PreEventGuardNotAllowed`, `OmitDoesNotSupportGuard`).
**Effort**: S
**Depends on**: none

#### F-LEX-01 — Lexer doc cross-references two non-existent files
**Severity**: P1
**Area**: Lexer (docs)
**Evidence**: `docs/compiler/lexer.md:740` references `docs/compiler/pipeline-overview.md` (does not exist); `docs/compiler/lexer.md:742` references `docs/language/type-system.md` (does not exist).
**Description**: The lexer doc is "Implemented" but its § 15 Cross-References table links to two files that don't exist. Per CLAUDE.md doc-sync rule, broken cross-references on an Implemented surface are drift. The likely intended targets are `docs/compiler/README.md` for the pipeline overview, and the per-type docs in `docs/language/` (`primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`) for type-system content.
**Recommended fix**: Update the two table rows to point to the actual replacement docs.
**Effort**: S
**Depends on**: none

### P2 — polish / defer permitted

#### F-TC-01 — TypeChecker doc claims ContentValidation impl pending, but it shipped
**Severity**: P1
**Area**: TypeChecker (docs) — drift on Implemented surface
**Evidence**: `docs/compiler/type-checker.md:859-882` declares "Gap 1: ContentValidation DU on TypeMeta — HIGH ... Status: Design locked, implementation pending (separate PR)." But `src/Precept/Language/Type.cs:128-189` defines the full DU: `ContentValidation` abstract record plus 6 sealed subtypes (`RegexValidation`, `NodaTimeValidation`, `ClosedSetValidation`, etc.). `TypedConstantValidation` consumer at `src/Precept/Language/TypedConstantValidation.cs:6` references it.
**Description**: The "Catalog Gaps (part of § 13)" section of the type checker doc is treated as canonical by readers. A gap labeled HIGH that's actually closed misleads reviewers about implementation completeness. This is exactly the "documented but unclear what's implemented" pattern.
**Recommended fix**: Either (a) move Gap 1 from § Catalog Gaps to a § Resolved section with the ship date and a code pointer, OR (b) delete the gap entry. Verify whether the per-TypeKind dispatch fallback (warned about at line 882) was removed when ContentValidation landed — if not, see F-TC-04.
**Effort**: S
**Depends on**: none

#### F-TC-04 — Per-TypeKind dispatch `GetFormsForType` looks like catalog-discipline drift
**Severity**: P1
**Area**: TypeChecker (impl) — catalog discipline
**Evidence**: `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs:285-297` — `private static SegmentForm[]? GetFormsForType(TypeKind type) => type switch { TypeKind.Money => MoneyForms, TypeKind.Quantity => QuantityForms, … }`. Also a hardcoded `Dictionary<TypeKind, string>` `InterpolationUnsupportedGuidance` at lines 98-107 listing per-type guidance text.
**Description**: This is the exact "switch on `*Kind` enum identity to dispatch per-member behavior" anti-pattern CLAUDE.md flags as a non-negotiable violation: each arm exists "because the language says so" — Money has Money-forms, Quantity has Quantity-forms, etc. The dispatch shape (TypeKind → segment-form table) belongs in catalog metadata on `TypeMeta` (likely paired with the now-shipped `ContentValidation`). The doc itself anticipated this at `type-checker.md:882`: "If [ContentValidation] not landed before Slice 4, use a hardcoded per-TypeKind dispatch table with a TODO referencing this gap." ContentValidation has landed (see F-TC-01) — the temporary dispatch should now migrate to catalog metadata.
**Recommended fix**: (1) Add `InterpolationForms` (or rename) to `TypeMeta` and to the relevant `ContentValidation` subtypes. (2) Move per-type guidance text into `TypeMeta` or `ContentValidation.FormatDescription`. (3) Rewrite `GetFormsForType` as a property/method on `TypeMeta`, eliminating the switch. (4) Audit TC for other surviving per-TypeKind switches with the same shape.
**Effort**: M
**Depends on**: none (but pairs cleanly with the F-TC-01 doc cleanup)

#### F-NB-01 — Empty `BuildDictionaries()` placeholder method
**Severity**: P2
**Area**: NameBinder (impl)
**Evidence**: `src/Precept/Pipeline/NameBinder.cs:188-191` — `public void BuildDictionaries() { /* Already built during Pass 1 into working dictionaries */ }`; called from `NameBinder.cs:32`. Doc acknowledges this as a "structural placeholder for the pass boundary" (`name-binder.md:366`).
**Description**: Dead body. Either the placeholder serves an architectural purpose (signaling phase transitions for future maintainers) or it's deletable. Leaving an empty method that the doc explicitly calls a "placeholder" is a smell — it implies work intended but not completed.
**Recommended fix**: Decide: (a) delete the method and the call, OR (b) keep it but add a real responsibility (e.g., materialize the final immutable dictionaries from working builders, separating Pass-1 mutation from Pass-2 read). Option (b) would let other code take a dependency on "Pass 1 has fully completed" as a structural fact rather than relying on call ordering. Owner decision.
**Effort**: S
**Depends on**: none

#### F-LANG-SPEC-AGGREGATE — `precept-language-spec.md` sub-agent report (13 findings)
**Severity**: P1 (aggregate; per-finding severities P0–P2)
**Area**: Language surface — precept-language-spec.md (2,038 lines)
**Evidence**: Full sub-agent audit covers **3 P0 (above)**, **5 P1**, **5 P2**. Full report archived at [`compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md`](compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md). P1/P2 highlights beyond the P0s:
- **F-LANG-SPEC-03** (P1): Contradictory rule detection (§ 0.6 #7) — no diagnostic code, no impl. Verified: two contradictory rules on same field silently accepted.
- **F-LANG-SPEC-06** (P1): `~startsWith`/`~endsWith` first-arg-must-be-`~string` not enforced — Functions.cs declares `(string, string) → boolean`; `TypeChecker.Validation.CI.cs` only enforces inverse direction. CI discipline is one-directional.
- **F-LANG-SPEC-07** (P1): Runtime Evaluator entirely `NotImplementedException` (Fire/Update/InspectFire/InspectUpdate/Restore + Version.cs counterparts). This is the next-phase work the review is gating — flagged here so it's on the readiness ledger as an explicit precondition for declaring § 3A complete.
- **F-LANG-SPEC-08** (P1): § 0.5 names ~11 modifiers (`guarded`, `entry`, `isolated`, `universal`, `milestone`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`) — **none exist in `ModifierKind.cs`**. Spec § 0.5 header claims "§4 is implemented." **Owner decision 2026-05-24**: defer 10 of 11 as future graph-analyzer roadmap (no runtime support needed; safe to ship later); **drop `milestone` entirely** as it was an undocumented synonym of the shipped `required` modifier — spec § 0.5 #5 should be rewritten to read "Dominator analysis. Required for `required`" only.
- **F-LANG-SPEC-04, -05, -09, -11, -12, -13** (P2): vacuous/tautological rule + tautological guard detection unimplemented; ChoiceElementTypeMismatch + OutOfRange + CollectionOperationOnScalar + NonOrderableCollectionExtreme codes defined but unwired; `<-` BackArrow omitted from § 1.1/§ 1.5/§ 2.1 tables.
**Description**: The single most striking pattern: § 0.6 (Proof Engine contract) lists 13 numbered guarantees — **5 of them (Items 7, 8, 9, 10, 12) are documented as if implemented but have no detection logic.** § 0.5 (Graph Analyzer contract) similarly names a modifier surface that has not been built. The spec presents a more complete language than the implementation delivers.
**Recommended fix**: See per-finding entries in the appendix. The temporal F-LANG-SPEC-10 is a real production bug (crash on bad input) and should ship immediately as part of Wave 2.
**Effort**: L (sum across all 13 findings; many are S–M individually)
**Depends on**: owner decisions on which spec promises to keep vs revise

#### F-LANG-COLL-AGGREGATE — collection-types.md / precept-grammar.md sub-agent report (17 findings)
**Severity**: P1 (aggregate; per-finding severities span P0–P3)
**Area**: Language surface — collection-types.md (1,517 lines) + precept-grammar.md (939 lines)
**Evidence**: Full sub-agent audit covers 17 findings: **6 P0 (above)**, **7 P1**, **4 P2**, **3 P3**. P1 highlights:
- F-LANG-COLL-09 — `insert F at N` / `remove F at N` proof obligations don't constrain the index (Actions.cs:154-180); checks only non-emptiness.
- F-LANG-COLL-10 — `notempty` on `lookup` doc/catalog contradiction (`Types.cs:711` says `NotemptyApplicable: false`; doc says applies).
- F-LANG-COLL-11 — `MissingOrderingKey` (PRE0104) is repurposed: doc reserves it for missing-`by` on `append`, but TC uses it for any `RequiredTraits` violation (`.min`/`.max` on non-orderable inner type).
- F-LANG-GRAM-01 — "14 construct kinds" doc claim; code has **15** (`ConstructionRow` extra).
- F-LANG-GRAM-03 — "18 ConstructSlotKind values" doc claim; code has **20** (`SuccessOutcome`, `EventEntryList` extra).
- F-LANG-GRAM-04 — EventDeclaration slot decomposition documented incorrectly (says `IdentifierList + ArgumentList + InitialMarker`; actual is single `EventEntryList`).
- F-LANG-GRAM-06 — "14 ExpressionFormKind values" doc claim; code has **15** (`InterpolatedTypedConstant` extra).
**Description**: Structural design correct; enumeration accuracy stale; proof-obligation depth documented at a higher level than the catalog encodes. Most load-bearing fix: F-LANG-COLL-08 (action-applicability enforcement).
**Recommended fix**: See per-finding entries. The collection-types findings cluster in proof-obligation depth and parse-time qualifier propagation. The grammar-doc findings are mostly enumeration-count fixes.
**Effort**: M (S each per finding; sum is one focused session)
**Depends on**: F-CAT-01 (catalog discipline sweep) — overlaps with several P1s

#### F-LANG-TEMP-AGGREGATE — type-system docs sub-agent report (16 findings)
**Severity**: P1 (aggregate; per-finding severities P0–P2)
**Area**: Language surface — primitive-types.md + temporal-type-system.md + business-domain-types.md
**Evidence**: Full sub-agent audit covers **2 P0 (F-LANG-TEMP-01 above and F-LANG-TEMP-02)**, **8 P1**, **6 P2**. P1 highlights beyond F-LANG-TEMP-01/02 and the Status drifts (F-LANG-01..03):
- F-LANG-TEMP-03 — `nonzero`/`nonnegative` on `duration` field not in catalog applicability (Modifiers.cs:16-21 omits `Duration` from `ZeroBoundNumericTypes`); doc explicitly says `nonzero` ships as "natural proof source" for duration division.
- F-LANG-TEMP-05 — Mixed temporal quantities `'1 day + 12 hours'` documented as valid; `TemporalQuantityParser.cs:53-54` rejects with TEMP005 "cannot mix calendar units and time units."
- F-LANG-TEMP-08 — `zoneddatetime ± period` declared as valid in `OperationKind.cs:91-92` / `Operations.cs:396-402`; doc says "compile-time error, must navigate via `.datetime`."
- F-LANG-BIZ-02 — ISO 4217 implicit `maxplaces` (D10): doc claims auto-precision (USD=2, JPY=0, BHD=3); not implemented (no `GetImpliedMaxplaces` / `ImplicitMaxplaces` code path; `MoneyValue.cs:7` acknowledges in comment).
- F-LANG-BIZ-03 — `currency` accessors `.name`, `.minorUnit`, `.numericCode`, `.symbol` documented (`business-domain-types.md:524-530`); `Types.cs:520-530` declares Currency meta with **no `Accessors` array**.
- F-LANG-BIZ-04 — `CurrencyCatalog` public API in doc (`Default` singleton, `Get`, `TryGet`, `GetByNumericCode`, `IsValid`, `All` as `IReadOnlyList`, `DataVersion`) entirely absent from `src/Precept/Language/CurrencyCatalog.cs:82-84` which exposes only `static FrozenDictionary<string, CurrencyEntry> All`.
- F-LANG-BIZ-07 — Composite period basis `'years&months'` (`&` separator, Decision D4) unimplemented; `TemporalQuantityParser.cs:16` splits on `+` only.
**Description**: Temporal and business-domain layers have the highest density of "documented but not implemented" drift in the entire review. These directly substantiate the user's stated experience of "continually finding things that were not fully implemented" — and specifically in the type system, which they called out.
**Recommended fix**: See per-finding entries. The temporal findings need owner decisions (extend implementation vs revise doc); the business-domain findings are mostly impl gaps that can be closed against the existing doc (D10 maxplaces, currency accessors, CurrencyCatalog wrapper API).
**Effort**: L (sum across all 16 findings)
**Depends on**: owner decisions on the design vs impl tradeoffs (F-LANG-TEMP-01/02/05/08)

#### F-LANG-CAT-15 — `Operations.Resolve` documented with full code sample but does not exist
**Severity**: P1
**Area**: Language surface — catalog-system.md
**Evidence**: `docs/language/catalog-system.md:1322-1352` shows a complete code sample of `public static OperationMeta? Resolve(OperatorKind op, Type lhs, Type rhs)`. Line 1241 and §5 reference it as a load-bearing API surface. **`grep -nE "static.*Resolve" src/Precept/Language/Operations.cs` returns no matches.** Actual API: `FindUnary` and `FindCandidates` only; TypeChecker does its own dispatch on top.
**Description**: A central API "resolve operator + operand types → operation" exists as detailed code in the spec but isn't implemented. The type checker reaches into `FindCandidates` and dispatches inline. Either Resolve was deliberately replaced by FindCandidates+inline dispatch (doc fix needed) or it was meant to land and didn't (impl fix needed).
**Recommended fix**: Replace §5 "Resolution" sub-section with the actual API (`FindUnary` + `FindCandidates`) and explain that qualifier dispatch lives in the type checker. If Resolve was intended to land, implement it.
**Effort**: S (doc fix) or M (impl)
**Depends on**: owner intent

#### F-LANG-CAT-01 — `TokenMeta.HoverDescription` claimed "✅ Resolved" but field doesn't exist
**Severity**: P1
**Area**: Language surface — catalog-system.md
**Evidence**: `catalog-system.md:2459, 2464` claim `TokenMeta.HoverDescription` added via CC#19 ("✅ Partially resolved — closed 2026-05-06"). `src/Precept/Language/Token.cs:35-69` defines `TokenMeta` with no such field.
**Description**: Same pattern as F-LANG-CAT-02 — claimed-resolved CC item that is absent from code. Smaller blast radius than the FaultCode case but still a "documented as done, isn't" symptom.
**Recommended fix**: Either add the field + populate it in `Tokens.GetMeta` arms, OR revert the status to Pending. Owner ruling.
**Effort**: S (status reversal) or M (impl)
**Depends on**: owner decision

#### F-LANG-CAT-20 — `TypeMeta.IsUserFacing` claimed "✅ Resolved" but field doesn't exist
**Severity**: P1
**Area**: Language surface — catalog-system.md
**Evidence**: `catalog-system.md:974-976` ("✅ Resolved (CC#16): `IsUserFacing` is a first-class catalog field...Default `true`. `Error` and `StateRef` are `false`.") vs. `Type.cs:207-252` which has no such field.
**Description**: Same pattern as F-LANG-CAT-01 and F-LANG-CAT-02. Three CC# resolutions documented as shipped that don't exist in code — strong signal that the CC resolution process isn't gating doc updates against actual implementation.
**Recommended fix**: Either implement or revert. Cross-cutting: investigate whether other CC#NN resolutions in the doc are similarly false.
**Effort**: S (status reversal) or M (impl)
**Depends on**: owner decision

#### F-LANG-CAT-19 — 9 aspirational event modifiers documented as if cataloged
**Severity**: P1
**Area**: Language surface — catalog-system.md
**Evidence**: `catalog-system.md:1518-1535` enumerates `entry, advancing, settling, completing, absorbing, guarded, isolated, universal` as event modifiers with `GraphAnalysisKind` mappings. `ModifierKind.cs:62` defines only `InitialEvent = 23`; no other event modifier exists. Doc line 1535 hedges "Future event modifiers are deferred" but the body text presents them as catalog members.
**Description**: A reader cannot separate spec from intent. The table format implies these are shipped catalog metadata. This is the exact "documented but not implemented" pattern.
**Recommended fix**: Move the 8 unimplemented event modifiers to a "Planned" sub-section under Future Opportunities, OR clearly mark each row "Designed, not implemented."
**Effort**: S
**Depends on**: none

#### F-LANG-CAT-AGGREGATE — Catalog-system.md has 26 audit findings (full report archived)
**Severity**: P1 (aggregate; per-finding severities vary P0–P2)
**Area**: Language surface — catalog-system.md
**Evidence**: Full sub-agent report covers 26 findings (2 P0 above, 16 P1, 8 P2) across all 14 catalogs. Headline counts: 14 of 14 catalogs have at least one count discrepancy. Largest count drifts: ProofRequirementKind 5→10 (+100%), DiagnosticCode 78/106→148 (+90%), ConstructSlotKind 17→20, ConstructKind 12→15, FaultCode 13→15, ExpressionFormKind 14→15, OperationKind 198→203.
**Description**: Beyond the headline P0s above (F-LANG-CAT-02, -08), the doc has extensive shape drift on metadata records (5+ undocumented `DiagnosticMeta` fields, undocumented `ResultQualifierPolicy` enum, undocumented `SlotVocabulary` enum 13 members, etc.), entirely undocumented analyzer fleet (~16 of 22+ analyzers), and a fully-stale "Construct Slot Model" section describing a non-existent `SlotKind` enum. See full sub-agent report for per-finding detail.
**Recommended fix**: Treat catalog-system.md as a single ~L-effort rewrite pass. Recommend extracting per-catalog count audits into a structured table that's easier to keep current. The 26 findings should be triaged in one focused session; many are quick S-effort fixes once the rewrite starts.
**Effort**: L (one focused day to address all 26)
**Depends on**: owner ruling on each "✅ Resolved" claim (revert vs implement)

#### F-X-01 — `F5TempVerify` dev-only test file in committed CI tree, causing 7 baseline failures
**Severity**: P1
**Area**: Cross-cutting (tests + sample regression intersection)
**Evidence**: `test/Precept.Tests/F5TempVerify.cs:12-14` — class docstring: `"TEMPORARY: F5 verification pass — compile all 30 sample files and report residual diagnostics. Remove this file after F5 verification is complete."` The 30-sample reference is itself stale — samples corpus is now 64. Test is responsible for 7 of 11 baseline failures (samples 02, 21, 22, 25, 26, 27, 28 fail to compile clean).
**Description**: A test file explicitly labeled "TEMPORARY ... remove after F5 verification is complete" has been left in the committed tree past its expiry. Its presence inflates failure counts (sample bugs surface as test failures) and conflates two concerns: (a) sample correctness (parallel-session territory; see `docs/Working/bugs.md`) and (b) actual test regressions. The right surface for sample-side regression is the dedicated `SampleFieldStateRegressionTests.cs` already covering all samples.
**Recommended fix**: Either (a) delete `F5TempVerify.cs` outright, OR (b) promote it to permanent `SampleCompilesCleanTests.cs` with the dev-only language removed and the file count updated to "all `samples/*.precept`". Owner decision.
**Effort**: S
**Depends on**: none

#### F-X-02 — Doc/code drift pattern on Status fields is systemic across stages
**Severity**: P1 (the underlying instances; this is a meta-finding)
**Area**: Cross-cutting (docs)
**Evidence**: 6+ stage docs declare Status fields that lag the implementation:
- `parser.md:5-7` "Implementation: Complete — Slices 1–4" (actual: Slices 1–26 shipped) — F-PAR-03
- `parser.md:359-361` 3-week-old Open Question on Implemented stage — F-PAR-02
- `type-checker.md:861` "Gap 1: pending (separate PR)" (actual: shipped) — F-TC-01
- `primitive-types.md:8` "type checker implementation pending" (actual: shipped) — F-LANG-01
- `business-domain-types.md:7` "Proposal — not yet implemented" (actual: shipped) — F-LANG-02
- `temporal-type-system.md:7` Doc maturity "Draft" + Status "Implemented" — F-LANG-03
- `tooling-surface.md:1088-1091` multiple rows stale — F-LS-01
**Description**: Status fields decay because no automated process detects drift, and there's no per-PR check that says "if you ship a feature, update the Status of the doc that declares it pending." The pattern is the *primary* mechanism by which the user "continually finds things that were not fully implemented." The fix is two-fold: (a) burn down each instance, and (b) institute a check that prevents recurrence.
**Recommended fix**: (1) Burn down individual P1s (F-PAR-03, F-TC-01, F-LANG-01..03, F-LS-01). (2) Add a CONTRIBUTING.md rule + PR checklist item: "If you ship work that changes a doc's Status, update the Status in the same PR." (3) Consider a periodic audit script or analyzer that flags `Status: Draft|Designed|Pending|Planned` on docs whose linked code paths look implemented.
**Effort**: M (per-instance burn-down) + S (CONTRIBUTING addition)
**Depends on**: the individual P1s

#### F-X-03 — Defensive enum-exhaustion throws are a systemic pattern across stages
**Severity**: P2 (the pattern; individual instances vary)
**Area**: Cross-cutting (impl)
**Evidence**: 7+ sites where the code switches on an enum (`ConstructSlotKind`, `OutcomeArgumentKind`, `GraphAnalysisKind`, `DiagnosticCode`/proof-requirement-type, `TypedErrorExpression` D26 invariant, D5 `SecondaryExpression` invariant):
- `Lexer.cs:131` — mode-stack alternating-parity invariant (acceptable; documented)
- `Parser.cs:480` — `No sentinel for {slot.Kind}` (F-PAR-04)
- `Parser.Expressions.cs:710` — `Unknown OutcomeArgumentKind` (F-PAR-04)
- `TypeChecker.cs:1312` + `TypeChecker.Normalization.cs:39,74` — D26 invariants (F-TC-02)
- `TypeChecker.Expressions.Callables.cs:254,274,311` — D5 invariants (F-TC-03)
- `GraphAnalyzer.cs:206` — `Unhandled GraphAnalysisKind`
- `ProofEngine.Diagnostics.cs:123` — `Unexpected proof requirement type`
**Description**: All are defensive runtime throws asserting an enum-exhaustion or design invariant that *should* be compile-time guaranteed. The codebase already has `[HandlesCatalogExhaustively]` and `[HandlesCatalogMember]` analyzers — but they evidently don't yet cover all the enums where the throw pattern appears, OR the analyzers aren't enforced strictly enough to allow the throws to be removed.
**Recommended fix**: Audit which enums have analyzer coverage. Extend coverage to all enums used in compiler dispatch switches. Once each switch is analyzer-protected, the `throw` arms become unreachable and can be removed (or downgraded to a `Debug.Fail` for development-time safety).
**Effort**: M
**Depends on**: F-CAT-01 (catalog discipline sweep can fold this in)

#### F-X-05 — Archive promotion backlog: 16 stranded Stage-4 promotions
**Severity**: P1 (aggregate; each individual promotion is S-L effort)
**Area**: Cross-cutting (Stage-4 promotion failures across the project)
**Evidence**: Systematic Archive scan (2026-05-24) of all 47 files in `docs/Working/Archive/` found 16 design/implementation docs containing substantial why-content that was never lifted to canonical docs. 4 surfaced during the 8-decision triage (hover-design, constructor-semantics, diagnostic-enforcement, language-server-implementation-plan); 12 surfaced during the systematic scan. Full audit at [`compiler-readiness-review-2026-05-24-appendices/audit-archive-promotion-backlog.md`](compiler-readiness-review-2026-05-24-appendices/audit-archive-promotion-backlog.md).
**Description**: This is the load-bearing pattern behind the user's stated experience of "continually finding things that were not fully implemented." In every case, implementation DID complete — the canonical doc just never received the why-content from the design doc that motivated the implementation. The 16 promotions span hover architecture, grammar split / dispatch obsolescence, analyzer fleet, SemanticTokenTypes catalog, qualifier-aware completion UX, Type-Grammar slot classification, two-layer value architecture, UCUM scale table, catalog compliance taxonomy, and more.
**Top 3 most consequential**:
1. `typed-constants-and-proof-coverage-plan.md` (L) — Type-Grammar Slot Classification architecture; touches 4 canonical docs; single largest stranded design surface
2. `catalog-compliance-audit.md` (M) — Pattern A-H taxonomy + Missing Catalog Fields master list; rosetta stone for why catalog-system.md is shaped the way it is
3. `quantity-normalization-design.md` (L) — Two-layer value architecture + UCUM scale table + runtime intake-boundary normalization
**Recommended fix**: Phase 1 includes all 16 promotions as sub-tasks. Use `/promote --backfill` (once skill is built) to clear them systematically. Some can be parallelized. Aggregate effort: ~3-4 days for all 16 if parallelized, ~5-7 days sequentially.
**Effort**: L (aggregate)
**Depends on**: `/promote` skill (build first in Phase 1)

#### F-X-04 — Cross-stage sample regression: 7 samples fail to compile clean
**Severity**: P1 (cross-references parallel-session work)
**Area**: Cross-cutting (sample regression; routed to parallel session)
**Evidence**: Baseline `dotnet test` shows `F5TempVerify.Sample_CompilesClean` failing for: `02-prior-auth-appeal-compiled.precept`, `21-saas-subscription-billing.precept`, `22-saas-trial-to-paid.precept`, `25-saas-customer-success.precept`, `26-saas-customer-onboarding.precept`, `27-saas-user-provisioning.precept`, `28-saas-license-management.precept`.
**Description**: These are sample-side issues, which the parallel session owns. **Do not modify samples from this session.** Routed to `docs/Working/bugs.md` (parallel session may already be tracking via the BUG-NNN system). The compiler is doing what it's been told — the question is whether any of these failures point to a *compiler* bug (false positive, missing feature) vs a *sample* bug (incorrect usage). The parallel session is best positioned to triage.
**Recommended fix**: Cross-reference each failing sample in `docs/Working/bugs.md`. Parallel session's existing process applies.
**Effort**: parallel-session-owned
**Depends on**: parallel sample-authoring work

#### F-LS-01 — `tooling-surface.md` status table stale on multiple rows
**Severity**: P1
**Area**: Language server (docs)
**Evidence**: `docs/compiler/tooling-surface.md:1088-1091` declares:
- "Semantic tokens Pass 2 | Reads `SemanticIndex` | **Blocked on TypeChecker**" — but TypeChecker is fully implemented (Stage 4 verified)
- "Completions | Catalog-driven with `SlotContext` | Partially implemented (basic keyword completions)" — but git log shows shipped `Slice 1`, `Slice 2`, `Slice 3` of catalog-driven completions plus `comprehensive completions overhaul` + `action-chain continuation`
- "Hover | Catalog + `SemanticIndex` | Partially implemented (keyword hover only)" — but the full semantic-card hover architecture shipped (13+ construct cards, all marked ✅ Done with commit refs) per `docs/Working/Archive/hover-design.md` ("V7 synced to shipped behavior, B4 locked as-built in `29cd9938`")
**Description**: The current-implementation-state column of the LS surface table is many months stale. Same drift pattern as F-PAR-03 (parser Status field) and F-LANG-01..03 (language doc Status fields). For LS consumers (or for Claude agents trying to understand what's safe to rely on), this table is misleading.
**Recommended fix**: Audit each row against current implementation. Update the `Current Implementation State` column with concrete pointers (file paths, class names). Where genuinely "Partial," enumerate the specific remaining gaps in a § Gaps subsection rather than leaving "Partial" without detail.
**Effort**: M (audit + rewrite ~6 rows)
**Depends on**: none

#### F-LS-02 — 3 LS integration tests fail in baseline with TaskCanceledException
**Severity**: P1
**Area**: Language server (tests)
**Evidence**: Baseline run failures:
- `DiagnosticPublishIntegrationTests.DidOpen_InvalidSource_PublishesDiagnostics` — TaskCanceledException
- `DiagnosticPublishIntegrationTests.DidClose_OpenDocument_PublishesEmptyDiagnosticsForUri` — TaskCanceledException
- `DiagnosticPublishIntegrationTests.DidChange_OutOfOrderVersions_PublishesNewestDiagnosticsOnly` — TaskCanceledException at line 134
Correlated build warning: `test/Precept.LanguageServer.Tests/DiagnosticPublishIntegrationTests.cs:162` — VSTHRD003 "Avoid awaiting or returning a Task representing work that was not started within your context as that can lead to deadlocks."
**Description**: The deadlock-risk analyzer flagged the exact file where 3 tests fail with timeout-style exceptions. Either (a) the tests have a real concurrency bug in test setup (await pattern flagged by VSTHRD003 is the cause), or (b) the LS itself has a publication race condition the tests are detecting. Either way: failing baseline tests on the LS publish path block declaring LS production-ready, since the publication path is the load-bearing LSP feature.
**Recommended fix**: (1) Investigate the VSTHRD003 site — `DiagnosticPublishIntegrationTests.cs:162`. Is the `await` of a non-context Task the cause of the cancellation? (2) Trace whether the LS itself races on out-of-order publish (real bug) or whether the test harness is unreliable (test bug). (3) Tests must pass in baseline before runtime work begins.
**Effort**: M
**Depends on**: none

#### F-LS-03 — 2 TODOs in LS code
**Severity**: P2
**Area**: Language server (impl)
**Evidence**:
- `tools/Precept.LanguageServer/SlotPositionResolver.cs:213` — "TODO: derive from ActionSyntaxSlot.Vocabulary annotations once that catalog gap is filled"
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs:2785` — "TODO: PriceIn is polymorphic — should offer both currency and unit completions."
**Description**: Two open TODOs in LS code. The first depends on a catalog gap (vocabulary annotations) — likely related to F-CAT-01 (catalog audit). The second is a specific completion-quality gap. Neither blocks production-readiness, but both should be tracked.
**Recommended fix**: Add an issue (or fold into F-CAT-01 work for the vocabulary one). For PriceIn polymorphic completion, expand to handle both currency and unit completions.
**Effort**: S each
**Depends on**: F-CAT-01 (for vocabulary item)

#### F-API-01 — Typed field-descriptor refactor pending across compile-time and runtime API surfaces
**Severity**: P1
**Area**: Compilation API + compile-time Runtime glue
**Evidence**: 7 TODOs reference the same unfinished refactor across two milestones:
- `src/Precept/Runtime/Inspection.cs:70,74,76,92` — "TODO D8/R4: FieldName and FieldType become a typed field descriptor" / "FieldName: string ... TODO D8/R4: field descriptor" / "FieldType: string ... TODO D8/R4: carried by descriptor" / "FieldNames: IReadOnlyList<string> ... transitive expansion"
- `src/Precept/Runtime/SharedTypes.cs:35,47` — "TODO G1/G9: ReferencedFields and ConstraintViolation.FieldNames are ..." / "should reference FieldDescriptor — currently provisional flat list"
- `src/Precept/Runtime/UpdateOutcome.cs:18` — "FieldNotEditable(string FieldName, ...) // TODO D8/R4: field descriptor"
- Also visible in `src/Precept/Pipeline/SemanticIndex.cs:22` — `TypedFieldRef.FieldName: string` (not yet a descriptor)
**Description**: The public API surface — both compile-time inspection types and the typed semantic model — uses string-keyed field references where typed descriptors are planned. Consumers (MCP, LS, future runtime) must currently string-match. The refactor is well-scoped (clearly named milestones D8/R4 and G1/G9) and has been deferred. The runtime work is the next phase, so the descriptor refactor is naturally a precursor to it.
**Recommended fix**: Land the typed descriptor refactor *before* the runtime work consolidates around the current shape — otherwise the runtime work will inherit string-keyed references and the refactor cost will multiply. Treat as a precondition for opening the runtime gate.
**Effort**: L (broad public-API change across compile-time and runtime)
**Depends on**: owner decision (sequence ahead of runtime work, or accept and ship runtime against strings)

#### F-LANG-01 — `primitive-types.md` says "type checker implementation pending" but TC ships primitives
**Severity**: P1
**Area**: Language surface coverage (docs/language/ ↔ implementation)
**Evidence**: `docs/language/primitive-types.md:8` declares Status "Implementation state | **Designed — type checker implementation pending**". The TypeChecker is fully implemented and handles all six primitives (`string`, `integer`, `decimal`, `number`, `boolean`, `choice`) — verified in Stage 4 review (test surface includes `TypeCheckerTypedConstantTests.cs` 92 tests, `TypeCheckerExpressionTests.cs` 79 tests, plus dedicated tests for each domain).
**Description**: This is the canonical instance of the pattern the user described: "incompletely implemented" appears in the Status field of a doc whose content is in fact fully shipped. A reader (or AI agent) consulting `primitive-types.md` to decide what's safe to use would conclude primitives can't be relied on yet — exactly backwards from reality.
**Recommended fix**: Update Status to "Implemented in `src/Precept/Pipeline/TypeChecker*.cs`; see `docs/compiler/type-checker.md`." Match the wording shape used by `temporal-type-system.md:8` (which correctly says "Implemented in `src/Precept/Language/Time/` and wired into typed-constant validation").
**Effort**: S
**Depends on**: none

#### F-LANG-02 — `business-domain-types.md` says "Proposal — not yet implemented" but money/quantity/etc. shipped
**Severity**: P1
**Area**: Language surface coverage
**Evidence**: `docs/language/business-domain-types.md:7` declares Status "Implementation state | **Proposal — not yet implemented; depends on temporal type system (Issue #107)**". But `TypeChecker.Expressions.TypedConstants.cs:285-297` dispatches `MoneyForms`, `QuantityForms`, `PriceForms`, `ExchangeRateForms`, `CurrencyForms`, `UnitOfMeasureForms`, `DimensionForms` — every business-domain type has shipped form-parsing. `MoneyValidator.cs`, `CurrencyValidator.cs`, `ExchangeRateValidator.cs`, `PriceValidator.cs`, `QuantityValidator.cs` all exist with full test surfaces.
**Description**: Same drift pattern as F-LANG-01 but higher impact — business-domain types are advanced features users would specifically check the doc for. The doc says "Proposal" — readers would not attempt to use these types.
**Recommended fix**: Update Status to "Implemented" with a list of subtypes and code pointers. Move "Depends on temporal type system" from blocking dependency to satisfied dependency (temporal is also implemented). Doc maturity should advance from "Draft" to "Active" or "Implemented".
**Effort**: S
**Depends on**: none

#### F-LANG-03 — `temporal-type-system.md` doc maturity "Draft" inconsistent with shipped impl
**Severity**: P1
**Area**: Language surface coverage
**Evidence**: `docs/language/temporal-type-system.md:7` Status "Doc maturity | **Draft**" but line 8 "Implementation state | **Implemented in `src/Precept/Language/Time/` and wired into typed-constant validation**". Internal inconsistency: the impl ships but the doc is Draft.
**Description**: A "Draft" doc describing implemented behavior is a drift signal — either the doc is incomplete (content missing) or the maturity field is stale. CLAUDE.md doc-sync rule: "When any code change ships, doc must be updated in the same edit pass."
**Recommended fix**: Promote maturity to "Active" or "Canonical Design" (matching `collection-types.md:7`'s style). If specific gaps remain that justify "Draft," enumerate them in a § Gaps subsection.
**Effort**: S
**Depends on**: none

#### F-LANG-04 — Failing test references non-existent SyntaxReference pattern
**Severity**: P1
**Area**: Language surface coverage (impl + tests)
**Evidence**: `test/Precept.Tests/SyntaxReferenceTests.cs:157-163` — `ConstructorPattern_ExistentialFields_DslSnippet_CompilesClean()` calls `SyntaxReference.CommonPatterns.Single(p => p.Name == "Constructor Pattern (Existential Fields)")`. **No such pattern exists** in `src/Precept/Language/SyntaxReference.cs` — the only constructor pattern present is `"Constructor Pattern (Atomic Creation)"` at line 230. The test fails on baseline with `InvalidOperationException` from `.Single(...)`.
**Description**: A pattern was likely renamed (Existential Fields → Atomic Creation), but the test was not updated. The fact that this test has been failing in baseline since at least the start of the review is exactly the "buggy / incompletely implemented" smell the user described. Discovered as part of the 11 baseline test failures.
**Recommended fix**: Audit `SyntaxReference.cs` for the intended pattern. Either (a) rename the test method and update the lookup string to `"Constructor Pattern (Atomic Creation)"`, OR (b) add an "Existential Fields" pattern back if it was deliberately separate and was lost in a refactor. Owner needs to decide which is correct.
**Effort**: S
**Depends on**: owner decision (rename vs restore)

#### F-CAT-01 — Re-verify Frank's catalog compliance audit (2026-05-09); produce a delta
**Severity**: P1
**Area**: Catalog system (cross-cutting, non-negotiable)
**Evidence**: `docs/Working/Archive/catalog-compliance-audit.md` — 27 catalog-discipline violations across the pipeline (Parser 13, NameBinder 1, TypeChecker 6, ProofEngine 5, GraphAnalyzer 2). Audit was archived (move-to-Archive commit `538a4a16`) but **no resolution column or delta doc exists**. Spot-checks during this review confirmed 3 violations were addressed: `IsStateWildcard` / `IsFieldBroadcast` are now real token metadata used by `Parser.cs:261, 797, 887`; `CountQualifierUnitCodes` moved into `UnitDimensionHelper`. But that's 3 of 27 verified — the rest are unknown status.
**Description**: A 27-item catalog-compliance audit getting archived without an explicit "all-clear" delta is itself a catalog-discipline drift signal. The architectural commitment says "catalogs are the language specification in machine-readable form" — partial enforcement undermines the guarantee. Frank's audit found Critical-severity items (e.g., "Action suffix markers hardcoded in parser helpers", "Qualifier domain knowledge in unit tables") that, if still present, are P0 for declaring the compiler production-ready.
**Recommended fix**: Re-run Frank's audit framework against current code. Produce a delta doc: for each of the 27 violations, mark `Fixed | Partial | Persists | Re-opened`. For each `Persists` or `Partial`, lift it into this remediation doc as a P0 (Critical) or P1 (High) finding using F-CAT-NN. Use the spike-9 `precept-reviewer` agent for the audit if helpful — its system prompt embeds the catalog discipline rules.
**Effort**: L (one focused day to re-audit + write delta)
**Depends on**: none; gates declaring catalog discipline "production-ready"

#### F-PRF-01 — Sample-count reference stale in proof-engine.md
**Severity**: P2
**Area**: ProofEngine (docs)
**Evidence**: `docs/compiler/proof-engine.md:2374` — "validate five-strategy coverage against all **20 sample files** in `samples/` before committing to no sixth strategy." `samples/*.precept` is now 64 files (>3× the count referenced).
**Description**: The validation item is stale. Either the validation has been done implicitly (via integration tests), or it remains an outstanding task at a larger scale. Worth confirming, since the doc explicitly conditions the "five-strategy is sufficient" decision on this validation.
**Recommended fix**: Confirm whether `ProofEngineIntervalIntegrationTests.cs`, `ProofEngineTemporalChainTests.cs`, etc., constitute the validation. If so, mark Item 3 resolved with a pointer to the test files. If not, schedule the validation pass.
**Effort**: S (audit), then potentially M (validation pass if needed)
**Depends on**: none

#### F-TC-02 — D26 invariant assertions are runtime throws (3 sites)
**Severity**: P2
**Area**: TypeChecker (impl) — defensive invariant assertions
**Evidence**: `TypeChecker.cs:1312`, `TypeChecker.Normalization.cs:39`, `TypeChecker.Normalization.cs:74` — three `throw new InvalidOperationException("D26 violated: ...")` sites asserting the design invariant that `TypedErrorExpression` presence implies at least one `Error`-severity diagnostic.
**Description**: D26 is a critical design invariant — it's the safety net ensuring the TC never produces error-typed nodes without a user-visible diagnostic. Currently enforced by runtime throws at three sites; a violation would surface only on the (already pathological) execution path that produces error nodes without diagnostics. Better enforcement would prevent the construction at the type level (e.g., factory function that pairs `TypedErrorExpression` construction with diagnostic emission) so the invariant cannot be broken.
**Recommended fix**: Consider a `TypedErrorExpression.Create(...)` factory that requires a `Diagnostic` argument and registers it as a side effect. Then the throws become unreachable and can be removed. Alternatively: keep the throws as safety nets but downgrade severity in a code comment explaining they enforce a structural invariant.
**Effort**: M
**Depends on**: none (low priority)

#### F-TC-03 — D5 SecondaryExpression null-check throws (3 sites)
**Severity**: P2
**Area**: TypeChecker (impl) — null-check on post-parser typed model
**Evidence**: `TypeChecker.Expressions.Callables.cs:254, 274, 311` — `throw new InvalidOperationException("D5: SecondaryExpression for X must not be null")` for `CollectionValueBy`, `InsertAt`, `PutKeyValue`.
**Description**: The typed semantic model carries nullable `SecondaryExpression`, even though for these three action shapes it's structurally required. Throwing at use-site is defensive but means the invariant is checked late. A DU split (`TypedActionShape` per CLAUDE.md DU rule — varying shapes use sealed subtypes, not nullable flat fields) would let these branches receive a non-nullable typed parameter.
**Recommended fix**: Split `TypedAction` (or whichever type carries `SecondaryExpression`) into sealed subtypes per shape so the field is non-nullable where structurally required, nullable only where genuinely optional. Removes 3 throws. Note: type-checker.md:884 explicitly acknowledges `TypedActionShape on ActionMeta` as a deprioritized gap — this finding revisits that decision.
**Effort**: M
**Depends on**: none

#### F-PAR-04 — Defensive enum-exhaustion throws should use compile-time guarantee
**Severity**: P2
**Area**: Parser (impl) — same pattern as F-LEX-NN defensive throws
**Evidence**: `src/Precept/Pipeline/Parser.cs:480` — `_ => throw new InvalidOperationException($"No sentinel for {slot.Kind}")` in `MakeSentinel`'s switch on `ConstructSlotKind`. `Parser.Expressions.cs:710` — `throw new InvalidOperationException($"Unknown OutcomeArgumentKind: {meta.ArgumentKind}")` in similar dispatch on `OutcomeArgumentKind`.
**Description**: The defensive throws exist because the switches must handle every enum member but C# doesn't enforce exhaustiveness on enum switches. The catalog system has a `[HandlesCatalogExhaustively]` attribute (per Stage 1 exploration — surfaced by an analyzer test that walks the catalog and asserts coverage). These switches should be annotated so coverage is enforced at build time, eliminating the need for runtime guards.
**Recommended fix**: Audit all switches over catalog-derived enums (`ConstructSlotKind`, `OutcomeArgumentKind`, `ExpressionFormKind`, etc.) and ensure each carries `[HandlesCatalogExhaustively]`. If the attribute doesn't currently cover all these enums, extend it. Fold into Stage 8 catalog-discipline sweep.
**Effort**: M
**Depends on**: Stage 8 catalog audit

#### F-LEX-02 — Per-decision rationale incomplete in stage doc § 11
**Severity**: P2
**Area**: Lexer (docs) — likely cross-cutting to all stage docs
**Evidence**: `docs/compiler/lexer.md:609-664` — 9 design decisions, each documented as Decision + Rationale only. Missing: Alternatives considered, Precedent, Tradeoff (the other three legs CLAUDE.md requires).
**Description**: CLAUDE.md "Per-Decision Rationale (Non-Negotiable)" requires all four legs for locked design decisions. Applicability to retrospective documentation of already-shipped code is ambiguous in CLAUDE.md, but the policy should be stated explicitly one way or the other and applied uniformly. **This finding likely repeats across every stage doc** — fold into a single cross-cutting policy decision in Stage 13 rather than fixing per-doc.
**Recommended fix**: Owner decision needed — (a) backfill all four legs into every stage doc § 11, or (b) carve out a rule that retrospective Decision+Rationale is sufficient for shipped code, with the full four-leg requirement applying only to proposals/Working/ docs.
**Effort**: S (policy decision) — execution scales M-L if backfilling.
**Depends on**: cross-cutting policy decision

#### F-LEX-03 — Doc line-count parenthetical drifted from code
**Severity**: P2
**Area**: Lexer (docs)
**Evidence**: `docs/compiler/lexer.md:750` says `(~687 lines)`; actual `src/Precept/Pipeline/Lexer.cs` is 775 lines.
**Description**: Cosmetic. The line-count parenthetical in § 16 is by nature a maintenance burden. Either drop it or refresh it. Suggest dropping — it adds nothing the reader can't see from the file itself.
**Recommended fix**: Remove the parenthetical; line counts don't belong in stable docs.
**Effort**: S
**Depends on**: none

---

## 4. Per-stage health summaries

*(one paragraph per stage; populated as stages complete)*

### Stage 1 — Lexer
**Verdict**: Healthy. The lexer is exemplary catalog-driven discipline — every keyword, operator, and punctuation token is derived from `Tokens.All` at static init; `Tokens.Keywords`, `TwoCharOperators`, `SingleCharOperators`, `TwoCharOperatorStarters`, `PunctuationChars` are all derived tables. No parallel keyword lists; no `kind switch` dispatching per-member behavior. All 8 lex-stage diagnostic codes (`InputTooLarge`, `InvalidCharacter`, `UnterminatedStringLiteral`, `UnterminatedTypedConstant`, `UnterminatedInterpolation`, `UnrecognizedStringEscape`, `UnrecognizedTypedConstantEscape`, `UnescapedBraceInLiteral`) have scenario coverage in `LexerTests.cs`. No `TODO`/`FIXME`/`HACK`. The one `throw new InvalidOperationException` (`Lexer.cs:131`) is a documented defensive guard against an upstream-prevented overflow — acceptable.

**Findings**: 3 minor (1 P1, 2 P2). Implementation is rock-solid; findings are doc-side only.

### Stage 2 — Parser
**Verdict**: Implementation healthy, doc is significantly drifted. Architecture is exemplary catalog-driven dispatch: `Constructs.ByLeadingToken`, `DisambiguationEntry.DisambiguationTokens`, `Modifiers.All`, `Actions.All`, `Types.All`, `Operators.All` all derived; no parallel keyword sets. `ParseSlotValue` switches on `ConstructSlotKind` but each arm dispatches to a slot-kind-specific parser — that's structural, not the per-member-behavior catalog violation. Expression parsing via Pratt-style operator-precedence is the "irreducible algorithmic core" and is correctly justified as such (parser.md:296-300). Test surface is dense: 22 files / 410 tests organized by slice and concern (expressions, scoped constructs, direct constructs, coverage gaps).

**Findings**: 3 P1 (all docs/comment drift), 1 P2 (defensive throws — consolidate with Stage 8 catalog sweep). No implementation bugs found. Doc Status field is the most striking finding — it claims "Slices 1–4" but Slices 1–26 are shipped.

### Stage 3 — NameBinder
**Verdict**: Healthy. Two-pass design (collection then resolution) cleanly separated; uses `SymbolResolution` DU for resolved-vs-unresolved targets. All 9 diagnostic codes (`DuplicateFieldName/StateName/EventName`, `CircularComputedField`, `UndeclaredArg`, `BindingShadowsField`, `UndeclaredField/State/Event`) emitted by the binder are referenced in `NameBinderTests.cs` (41 tests, 759 LOC). Doc 16-section template complete; all cross-references resolve. No TODO/FIXME/HACK, no `NotImplementedException`.

**Findings**: 1 P2 (empty placeholder method).

### Stage 4 — TypeChecker
**Verdict**: Mostly healthy. Largest stage by far (10 files, 7.8k LOC, 34 test files, ~800 tests). Test coverage strong, organized by semantic domain (assignments, field state, expressions, transitions, modifiers, currency/unit, quantities, etc.). No TODO/FIXME/HACK in TC code. Doc 16-section template complete. One real catalog-discipline drift in TypedConstants (F-TC-04), one doc/code drift on the "Catalog Gaps" section (F-TC-01), and two defensive-invariant patterns worth revisiting (F-TC-02, F-TC-03). The TypedConstants per-TypeKind dispatch is the only catalog-discipline P1 found so far in the entire pipeline.

**Findings**: 2 P1, 2 P2.

### Stage 5 — GraphAnalyzer
**Verdict**: Healthy. 863 LOC, 820 LOC of tests (35 tests, 83 references to the 10 diagnostic codes — strong scenario coverage). Doc has 16-section template (with an inserted "§ 4a Structural Preconditions" — minor deviation, acceptable). No TODO/FIXME/HACK. One defensive throw at `GraphAnalyzer.cs:206` for an unhandled `GraphAnalysisKind` enum arm — same pattern as F-PAR-04 (fold into the cross-cutting `[HandlesCatalogExhaustively]` sweep, not a separate finding).

**Findings**: 0 new (1 instance of F-PAR-04 family in this stage).

### Stage 6 — ProofEngine
**Verdict**: Healthy. Largest doc in the compiler (2,500+ lines, 16-section template + detailed Implementation Status). 4,165 LOC across 8 files. 14+ test files exclusively for proofs. The doc's "Implementation Status" sub-section explicitly tracks 10 items — all resolved except (3) sample-count validation (now stale, F-PRF-01) and validation reminders that don't block production-readiness. Initial-state satisfiability (was blocking) is implemented at `ProofEngine.Analysis.cs:47`. No TODO/FIXME/HACK in code. One defensive throw at `ProofEngine.Diagnostics.cs:123` — same `[HandlesCatalogExhaustively]` family.

**Findings**: 1 P2 (+ 1 F-PAR-04 family instance).

### Stage 7 — Diagnostic system
**Verdict**: Healthy. 148 diagnostic codes declared in `DiagnosticCode.cs`; **all 148 are emitted** somewhere in the compiler+tooling implementation (zero unused codes); **all 148 are referenced in test files** (some via reflection-driven catalog tests, others via direct enum reference). Doc status correctly "Implemented" (the earlier survey's "mislabeled Draft" claim was wrong — verified at `diagnostic-system.md:7-8`). Doc deviates from the canonical 16-section template — uses topic-named sections instead — which is acceptable for cross-cutting infrastructure.

**Caveat**: "Referenced in tests" is necessary but not sufficient for true scenario coverage — a code can appear in a `NotContain` assertion (negative coverage) without ever being scenario-asserted to fire. A precise scenario-coverage matrix is Stage 13 work.

**Findings**: 0.

### Stage 8 — Catalog system
**Verdict**: Mostly healthy at the *system* level — `Tokens`, `Constructs`, `Types`, `Operators`, `Functions`, `Actions`, `Modifiers`, `ProofRequirements`, `Ucum/UcumCatalog`, `CurrencyCatalog`, `TemporalDomains` all exist as discriminated-union catalogs with companion test files. `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` attributes exist for exhaustiveness enforcement. `tmLanguage.json` is generated (per the architectural rule, never hand-edited). `docs/language/catalog-system.md` is 2,481 lines and Status "Implemented".

**However**: Frank's 2026-05-09 audit found 27 catalog-discipline violations across the pipeline. The audit was archived but no delta or all-clear was produced. Spot-checks confirmed several violations were addressed; the rest are unknown. **This is the load-bearing P1 of the entire review** — see F-CAT-01.

**Findings**: 1 P1 (re-audit), plus the related F-TC-04 (the `GetFormsForType` TypeKind switch is exactly a Pattern-D violation from Frank's audit).

### Stage 9 — Language surface coverage (docs/language/ ↔ implementation)
**Verdict**: 🔴 **This is the load-bearing stage of the entire review.** Initial pass surfaced 4 P1 doc-status drift findings (F-LANG-01..04). The exhaustive deep-read of all 8 docs by 4 parallel sub-agents (full reports archived as appendices) found a much larger surface: **72 additional findings (13 P0, 34 P1, 22 P2, 3 P3)** across `precept-language-spec.md`, `primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`, `precept-grammar.md`, `catalog-system.md`, `README.md`.

**Pattern**: the user's experience of "continually finding things that were not fully implemented" is corroborated systematically. Three failure modes:
1. **Counts wrong** — 14 of 14 catalogs in `catalog-system.md` have at least one count discrepancy; spec § 0.6 lists 13 proof guarantees with 5 unimplemented; § 0.5 names ~11 modifiers that don't exist.
2. **"✅ Resolved" claims false** — F-LANG-CAT-01 (`TokenMeta.HoverDescription`), F-LANG-CAT-02 (`FaultCode.AmbiguousDispatch`), F-LANG-CAT-20 (`TypeMeta.IsUserFacing`) all marked resolved with implementation checklists but fields don't exist.
3. **Documented features unimplemented** — `set of money in 'USD'` qualified inner types unparseable; `claim.value`/`claim.by` two-field quantifier binding missing; `'3 days'` in `instant` context doesn't produce `Duration`; mandatory `because` on ensures not enforced; `UnsatisfiableGuard` (PRE0082) has no emission site; cross-kind action diagnostics (PRE0047/0048) defined but **never emitted anywhere**.

**Real production bug surfaced**: F-LANG-SPEC-10 — invalid temporal input (`'2026-13-01'`, `'25:00:00'`, etc.) crashes the MCP server through unhandled exception path instead of producing structured diagnostics. Verifiable via `mcp__precept__precept_compile`.

**Findings**: 4 P1 from initial pass + 13 P0 + 34 P1 + 22 P2 + 3 P3 from sub-agents = **76 total**. This stage dominates the review's P0 count.

**Appendix files** (full sub-agent reports):
- [`compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md`](compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md) — 13 findings, 3 P0
- [`compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md`](compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md) — 16 findings, 2 P0
- [`compiler-readiness-review-2026-05-24-appendices/audit-collections-grammar-docs.md`](compiler-readiness-review-2026-05-24-appendices/audit-collections-grammar-docs.md) — 17 findings, 6 P0
- [`compiler-readiness-review-2026-05-24-appendices/audit-catalog-system.md`](compiler-readiness-review-2026-05-24-appendices/audit-catalog-system.md) — 26 findings, 2 P0

### Stage 10 — Compilation API + compile-time Runtime glue
**Verdict**: Healthy at the orchestration level (`Compiler.Compile` is 151 LOC of pure pipeline composition — no surprises, no TODOs). The `Compilation` record, `SemanticIndex` typed DU, `EnrichGraphWithProofStatus` post-pass — all clean. The one architectural debt is string-keyed field references throughout the API surface (compile-time and runtime), with a known-scoped refactor pending (F-API-01). This is the only stage where the deferred work is unambiguously labeled (D8/R4, G1/G9) and well-bounded.

**Findings**: 1 P1 (the typed-descriptor refactor).

### Stage 11 — MCP compiler tools
**Verdict**: Mostly healthy at the wrapper level, but **the audit was incomplete** — it only spot-checked the tools for catalog discipline and ~30-LOC compliance; it did not actually invoke each tool. The parallel sample-remediation session (2026-05-24) discovered **BUG-007**: `precept_domains` tool crashes on any scope argument. Plus **BUG-009**: `precept_compile` has an undocumented payload-size limit at ~12-15 KB. Both are MCP-layer bugs that the original Stage 11 audit missed.

**Audit-process lesson**: spot-checking source files doesn't replace actually invoking the tools. The "Stage 11 healthy" verdict was based on code review, not behavioral verification. Future MCP audits should include `mcp__precept__precept_*` probes against every registered tool.

**Findings**: **1 P1 (BUG-007 — corrected post-bugs.md integration)**. BUG-009 lands in Phase 2 as part of the broader MCP-crash family fix.

### Stage 12 — Language server compiler features
**Verdict**: Mixed. Implementation surface looks broad (CallContextResolver, CursorSemanticResolver, DiagnosticEnricher/Projector, DocumentState/Store, SlotPositionResolver, SymbolNavigation, CompletionHandler, etc.) with substantial recent investment (multiple completion slices, hover iterations). But the canonical status doc (`tooling-surface.md`) is many months stale (F-LS-01), and 3 integration tests fail in baseline with TaskCanceledException correlated with a build-time VSTHRD003 deadlock-risk warning on the same file (F-LS-02). 2 TODOs in code (F-LS-03). The grammar generator and `tmLanguage.json` regeneration are confirmed (status table row 1 is accurate).

**Findings**: 2 P1 (stale doc table + failing baseline tests), 1 P2 (TODOs).

---

## 5. Cross-cutting health

### 5.1 Catalog discipline
- `Tokens`, `Constructs`, `Types`, `Operators`, `Functions`, `Actions`, `Modifiers`, `ProofRequirements`, `Ucum/UcumCatalog`, `CurrencyCatalog`, `TemporalDomains` all exist as DU catalogs.
- `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` / `[CatalogDU]` attributes exist.
- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` exists and looks generated (header matches generator output; git history references "feat(slice-grammar-gen, slice-language-server)" and `feat(catalog): SemanticTokenTypes — 14th catalog`).
- **Gap**: Frank's 2026-05-09 27-violation audit was archived without a delta — see F-CAT-01.

### 5.2 Diagnostic scenario coverage
- 148 codes declared in `DiagnosticCode`. All 148 emitted in implementation (zero unused). All 148 referenced in test files (some structurally via reflection; some in negative `NotContain` assertions, some in positive scenario assertions).
- **Gap**: A precise scenario-vs-negative coverage matrix would require reading every test. Not done in this pass — recommended as remediation-phase work. Baseline reflection-test coverage is sufficient to certify structural completeness.

### 5.3 Doc/code drift (the load-bearing pattern)
- The most systemic finding of this review. See F-X-02 for the consolidated meta-finding.
- 6+ individual P1 instances logged across Parser, TypeChecker, three language docs, and the LS surface table.
- Root cause: no enforcement that a PR shipping a feature updates the corresponding doc's Status field.

### 5.4 Defensive-throw pattern
- 7+ sites use runtime `throw new InvalidOperationException` to enforce design invariants. See F-X-03.
- Path forward: extend `[HandlesCatalogExhaustively]` coverage so the throws become unreachable, then remove.

### 5.5 Sample regression
- 7 samples fail to compile clean (F-X-04). **Parallel session owns sample edits — routed to `docs/Working/bugs.md`.**
- Not modified from this session.

### 5.6 Philosophy alignment
- `docs/philosophy.md` not read in this pass (CLAUDE.md prohibits edits without owner approval; the review surfaces gaps for owner attention rather than fixing them).
- Spot-check: the philosophy principles (Prevention not detection, One file complete rules, Determinism, Compile-time structural checking, Honest about approximation, etc.) appear to be honored by the compiler implementation. No gap surfaced this pass.
- **Action**: If the runtime work changes the constraint model or surface area, re-read `philosophy.md` and flag any gap before merging.

---

## 6. Recommended remediation sequencing

> **Status**: skeleton; final counts and order will be confirmed once Stage 9 sub-agent deep-reads return. Headings reflect the planned shape.

### Wave 0 — P0 owner-decision triage (S, hours)
**Goal**: Get binding owner decisions on the small number of P0s that need direction before any work begins.

Each of these is "spec promises X / implementation doesn't deliver X" — owner must decide implement vs revise-spec:
- F-LANG-CAT-01 (`TokenMeta.HoverDescription` claimed-resolved): revert status or implement
- F-LANG-CAT-02 (`FaultCode.AmbiguousDispatch` claimed-resolved): revert status or implement
- F-LANG-CAT-20 (`TypeMeta.IsUserFacing` claimed-resolved): revert status or implement
- F-LANG-SPEC-01 (because on ensures): tighten enforcement or amend principle
- F-LANG-SPEC-08: **DECIDED 2026-05-24** — defer 10 graph-analyzer modifiers to future roadmap (no runtime support needed); drop `milestone` entirely from spec § 0.5 #5 (undocumented synonym of `required`)
- F-LANG-TEMP-01/02 (`'3 days'`/`'2 weeks'` in instant context): context-aware parser or revise spec
- F-LANG-TEMP-08 (`zoneddatetime ± period`): remove catalog op or remove doc rejection
- F-LANG-TEMP-05 (mixed temporal quantities): relax parser or revise doc
- F-LANG-BIZ-07 (composite period basis `&` separator): build or drop from doc *(still owner-pending — needs discussion before Phase 6 fully closes)*
- F-LANG-BIZ-09 (`units` block): **DECIDED 2026-05-28** — close as already-answered by `business-domain-types.md § D6`. The locked spec already rejects a dedicated `units { }` block ("complex language feature for what amounts to multiplication") and provides the canonical pattern: entity-scoped unit identifiers live in `unitofmeasure` fields, conversion factors live in compound-unit `quantity` fields (`quantity in 'each/case'`). The pattern works in practice — `samples/inventory-item.precept` uses interpolated qualifiers (`quantity in '{StockingUnit}/{PurchaseUnit}'`) for runtime-configurable unit configurations, with dimensional cancellation verified at compile time. `unitofmeasure § Registry scopes` table synced to match. A potential verbosity-friction gap (interpolation syntax density; numerator-vs-denominator discipline) is noted for future revisit if it surfaces as real pain.

Without these decisions, Waves 1-3 will stall. Recommend a 1-hour synchronous review of the appendix files with the owner.

### Wave 1 — Doc-Status truth-up (S/M, days)
**Goal**: Eliminate the systemic doc/code-drift pattern that is the user's headline pain.

Burn down in any order — each is independent:
- F-PAR-03 — parser.md Status: Slices 1–4 → actual
- F-PAR-02 — parser.md unresolved Open Question (3 weeks stale)
- F-PAR-01 — Parser.cs Slice-0 TODOs (PRE0015, PRE0013)
- F-TC-01 — type-checker.md "Gap 1 pending" → resolved
- F-LANG-01 — primitive-types.md Status
- F-LANG-02 — business-domain-types.md Status
- F-LANG-03 — temporal-type-system.md Doc maturity
- F-LANG-04 — SyntaxReferenceTests stale pattern lookup (fixes 1 baseline failure)
- F-LS-01 — tooling-surface.md status table
- F-LEX-01 — lexer.md broken cross-references
- F-LEX-03 — lexer.md line-count drift
- F-LANG-CAT-AGGREGATE — all catalog count drifts and CC-resolution false-claims in `catalog-system.md` (single focused day)
- F-LANG-GRAM-01/03/04/06 — grammar doc enumeration corrections
- F-LANG-SPEC-11 — `<-` in spec § 1.1/§ 1.5/§ 2.1 tables

Plus institutional fix (F-X-02): add CONTRIBUTING.md rule + PR checklist item.

### Wave 2 — Baseline test failures + production-bug fixes (S/M, days)
**Goal**: Green test baseline AND no crashes on bad input before runtime work.

- F-LANG-04 — already in Wave 1 (clears 1 baseline failure)
- F-LS-02 — 3 LS DiagnosticPublishIntegrationTests fail with TaskCanceledException (investigate VSTHRD003 deadlock-risk warning at the same file)
- F-X-01 — F5TempVerify dev-only test file (deleting/promoting clears 7 sample failures from compiler test run; sample bugs route to parallel session's `bugs.md`)
- F-X-04 — parallel-session sample regressions (cross-reference, do not modify)
- **F-LANG-SPEC-10 (P0)** — wire temporal validation diagnostics so invalid date inputs produce structured errors instead of crashing the MCP server. **Real production bug.**
- F-LANG-CAT-15 — `Operations.Resolve` doc-vs-code reconciliation (S: doc fix preferred; M: implement)

After Wave 2: `dotnet test` is 0 failures; MCP server doesn't crash on bad input.

### Wave 3 — Catalog discipline re-verification + collection-types impl gaps (L, several days)
**Goal**: Confirm the architectural non-negotiable; close the documented-but-unimplemented collection-types gaps.

Catalog discipline:
- F-CAT-01 — re-run Frank's 2026-05-09 audit framework; produce a Fixed/Partial/Persists/Reopened delta for every one of the 27 violations
- F-TC-04 — `GetFormsForType` TypeKind switch (Pattern-D violation)

Collection-types P0 implementation:
- **F-LANG-COLL-08 (P0)** — wire action `ApplicableTo` enforcement; emit PRE0047/0048 for cross-kind action errors. This is the largest single missing diagnostic surface in the language.
- **F-LANG-COLL-06 (P0)** — qualified inner types (`set of money in 'USD'`) — extend `ParseInnerTypeReference` with `TryParseQualifiers`
- **F-LANG-COLL-04 (P0)** — `.at(N)` index-bounds proof obligation
- **F-LANG-COLL-05 (P0)** — `log of T by P` append uniqueness obligation
- **F-LANG-COLL-07 (P0)** — `queue of T by P` two-field quantifier binding (`claim.value`/`claim.by`)
- **F-LANG-GRAM-02 (P0)** — rename `ConstructionRowReject` → `EventRowReject`
- F-LANG-CAT-08 (P0) — rewrite `catalog-system.md` § 11 to cover all 10 `ProofRequirementKind` members

Proof engine spec gaps:
- **F-LANG-SPEC-02 (P0)** — wire `UnsatisfiableGuard` (PRE0082) detection
- F-LANG-SPEC-03 — contradictory rule detection (P1)
- F-LANG-SPEC-04, -05 — vacuous rule + tautological guard detection (P2)
- F-LANG-SPEC-06 — `~startsWith`/`~endsWith` first-arg-must-be-`~string` enforcement (P1)

This is the largest wave by effort; expect multiple days.

### Wave 4 — Compile-time API ergonomics (L)
**Goal**: Stable API surface before runtime work consolidates around it.

- F-API-01 — typed field descriptor refactor (D8/R4 + G1/G9 milestones). **This is the explicit precondition for opening the runtime gate** — landing it after runtime work consolidates around strings would multiply rework cost.

### Wave 5 — Defensive-throw consolidation (M)
**Goal**: Eliminate runtime invariant throws by extending compile-time enforcement.

- F-X-03 — extend `[HandlesCatalogExhaustively]` analyzer coverage to all enums switched in compiler dispatch
- F-PAR-04, F-TC-02, F-TC-03 — individual instances become obsolete as analyzer coverage extends

### Wave 6 — Polish (P2 items, defer permitted)
- F-NB-01 — empty `BuildDictionaries()` placeholder
- F-PRF-01 — proof-engine.md sample-count reference
- F-LEX-02 — per-decision rationale completeness policy decision (cross-cutting)
- F-LS-03 — 2 small TODOs in LS

**Stage 9 sub-agent findings**: Folded into Waves 1-3 above per severity. Full per-finding detail in the four appendix files referenced in Stage 9 of § 4.

---

## 7. Definition of "production-ready compiler" — exit criteria for runtime gate

The compiler is declared production-ready (and the runtime gate opens) when **all** of the following are true:

- No P0 findings open.
- All P1 findings either resolved or explicitly accepted with owner sign-off.
- Every stage doc declared "Implemented" matches code at the section level.
- Catalog discipline sweep produces zero violations.
- Every `DiagnosticCode` enum member has both a structural test (already covered by `DiagnosticCatalogTests` reflection) **and** at least one scenario test asserting the code fires under the right condition.
- No `NotImplementedException`, `TODO`, `FIXME`, `HACK`, or `XXX` in compile-time paths without a tracked issue link.
- `dotnet test` green across all four test projects.
- `dotnet build` zero warnings (or warnings explicitly accepted in a finding).
- MCP wrappers all ≤ ~30 LOC of non-serialization logic.
- `tmLanguage.json` regenerated and verified not hand-edited.

---

## 8. Pointers to parallel work

- **Sample-side issues** surfaced during compile-runs are recorded as one-line pointers in this doc and full entries in [`bugs.md`](bugs.md) (parallel session owns sample edits).
- **Sample corpus expansion plan**: [`sample-corpus-expansion-plan.md`](sample-corpus-expansion-plan.md) — informational only; not in scope here.

---

## Appendix A — Baseline build / test snapshot (2026-05-24)

### Build
- `dotnet build`: succeeded; 0 errors; 2 warnings:
  - `test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs:1092` — VSTHRD200 (Async suffix missing)
  - `test/Precept.LanguageServer.Tests/DiagnosticPublishIntegrationTests.cs:162` — VSTHRD003 (avoid awaiting Task from another context — deadlock risk)

### Tests
| Project | Total | Passed | Failed |
|---|---|---|---|
| Precept.Mcp.Tests | 46 | 46 | 0 |
| Precept.Tests | 6,007 | 5,999 | **8** |
| Precept.Analyzers.Tests | 291 | 291 | 0 |
| Precept.LanguageServer.Tests | 411 | 408 | **3** |
| **Total** | **6,755** | **6,744** | **11** |

### Failing tests (11)
**Compiler/language tests (8 in Precept.Tests):**
- `SyntaxReferenceTests.ConstructorPattern_ExistentialFields_DslSnippet_CompilesClean` — REAL compiler/snippet failure; routes to Stage 9 review. **F-LANG-01 candidate.**
- `F5TempVerify.Sample_CompilesClean` × 7 — samples 02, 21, 22, 25, 26, 27, 28. F5TempVerify is dev-only (already flagged P2 test smell). The underlying sample failures are parallel-session territory — routed to `bugs.md`. **F-X-NN candidate** (test infrastructure: dev-only file in CI).

**Language server tests (3 in Precept.LanguageServer.Tests):**
- `DiagnosticPublishIntegrationTests.DidOpen_InvalidSource_PublishesDiagnostics` — TaskCanceledException
- `DiagnosticPublishIntegrationTests.DidClose_OpenDocument_PublishesEmptyDiagnosticsForUri` — TaskCanceledException
- `DiagnosticPublishIntegrationTests.DidChange_OutOfOrderVersions_PublishesNewestDiagnosticsOnly` — TaskCanceledException at line 134
- All three timeout-based integration tests; likely correlated with the VSTHRD003 deadlock-risk warning on line 162 of the same file. **F-LS-NN candidate.**

## Appendix B — Finding ID prefixes

| Prefix | Stage |
|---|---|
| F-LEX | Lexer |
| F-PAR | Parser |
| F-NB | NameBinder |
| F-TC | TypeChecker |
| F-GA | GraphAnalyzer |
| F-PRF | ProofEngine |
| F-DIAG | Diagnostic system |
| F-CAT | Catalog system |
| F-LANG | Language surface coverage (docs/language/ ↔ implementation) |
| F-LANG-SPEC | Language surface — `precept-language-spec.md` (sub-agent) |
| F-LANG-PRIM | Language surface — `primitive-types.md` (sub-agent) |
| F-LANG-TEMP | Language surface — `temporal-type-system.md` (sub-agent) |
| F-LANG-BIZ | Language surface — `business-domain-types.md` (sub-agent) |
| F-LANG-COLL | Language surface — `collection-types.md` (sub-agent) |
| F-LANG-GRAM | Language surface — `precept-grammar.md` (sub-agent) |
| F-LANG-CAT | Language surface — `catalog-system.md` (sub-agent) |
| F-API | Compilation API + compile-time Runtime glue |
| F-MCP | MCP compiler tools |
| F-LS | Language server compiler features |
| F-X | Cross-cutting |
