# Compiler Readiness Plan — 2026-05-24

**Status**: Draft — Phases 1-2 planned in detail; Phases 3-10 stubbed pending decisions and Phase 1-2 completion.
**Companion docs**:
- [`compiler-readiness-review-2026-05-24.md`](compiler-readiness-review-2026-05-24.md) — audit findings (the "what we found")
- This doc — phased execution plan (the "what we'll do")
- [`compiler-readiness-review-2026-05-24-appendices/`](compiler-readiness-review-2026-05-24-appendices/) — full sub-agent reports
**Scope gate**: blocks runtime implementation
**Owner-decision policy**: each phase opens only when its listed upstream decisions are settled. Triage is just-in-time, not all-up-front.
**Sample-edit constraint**: do NOT modify `samples/*.precept` from this workstream — parallel session owns samples; cross-reference via [`bugs.md`](bugs.md).

---

## Phase summary

| Phase | Goal | F-count | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | Doc foundation truthful + lifecycle skills + 16 Archive promotions | ~55 | 8 (✅ all settled 2026-05-24) | XL (~5-7 days) | ✅ **Complete 2026-05-24** (all 6 workstreams shipped; verification report at [`lifecycle-review-phase-1-2026-05-24.md`](lifecycle-review-phase-1-2026-05-24.md)) |
| 2 | Green baseline + no crashes + Operations.Resolve + MCP-crash family | **12+** | 2 | **L (~4-5 days)** | ✅ **Complete 2026-05-25** (all 7 steps shipped: 2.1–2.7; MCP wrapper backstop + temporal-literal verified clean + LS URI-case fix + Operations.Resolve + generic SyntaxReference test; 6107/6108 Precept.Tests pass with the 1 failure as new BUG-013; 411/411 LS tests pass; 67/67 Mcp tests pass; 291/291 analyzer tests pass) |
| 3 | Type system completeness | ~13 (F-LANG-BIZ-02 dropped → doc-only retire D10 + Frank's case-9 absorbed) | 4 (✅ all settled 2026-05-25, incl. F-LANG-BIZ-02 Position 3 post-research) | **M (~4-6 days)** | ✅ **Complete 2026-05-25** (all 14 findings closed; 6136/6137 Precept.Tests pass; 411/411 LS tests pass; 291/291 analyzer tests pass; BUG-010 fully fixed; D10 retired; F-TC-04 last TypeKind switch eliminated; F-LANG-BIZ-09 filed for Phase 4+) |
| 4 | Collection completeness + BUG-002 | **~16** | 2 | L | Stub — TBD |
| 5 | Proof engine satisfiability + BUG-004 + BUG-006 + BUG-012 + FieldNeverSet/unification | **~14** | 2 | XL | Partial — F-LANG-GRAPH-04 planned, rest stubbed |
| 6 | `units` block + composite basis | 2 | 0 | L | Stub — TBD |
| 7 | API surface solidity (typed descriptors) | ~6 | 1 | M-L | Stub — TBD |
| 8 | Diagnostic completeness | ~7 | 3 | M | Stub — TBD |
| 9 | Polish + cleanup + `/lifecycle-7-audit` skill | ~20 | 4 | M | Stub — TBD |
| 10 | Runtime gate verification | — | 0 | S | Stub — TBD |

**Overall estimate**: 7-11 weeks of focused work (was 6-10; Phase 2 grew). Phase 1 includes (a) 5 lifecycle skill builds + rename, (b) 16 Archive promotion obligations, (c) CONTRIBUTING.md lifecycle updates. Phase 2 grew from ~2-3 days to ~4-5 days after integrating 10 active bugs from `bugs.md` (parallel sample-remediation session, 2026-05-24): 6 MCP-crash family bugs (BUG-003 period, -005 symptom, -007 domains, -008 duration, -010 now()+duration, -011 timezone+time), 1 MCP transport bug (BUG-009 payload limit), plus the original F-LANG-SPEC-10. Coordinated MCP-layer instrumentation pass catches the whole family in one fix. Phase 5 (proof engine satisfiability) remains the highest variance.

**`bugs.md` as ongoing source**: parallel sample-authoring sessions continue to discover bugs that surface in real authoring workflows (not in code-vs-doc audits). Plan integrates bugs.md as a standing input. Phase-kickoff protocol includes "re-read bugs.md for new entries since last integration."

**Bug-to-phase coverage map** (as of 2026-05-25, after Phase 2 commit `38712543`):

| Bug | Status | Phase | Rationale |
|---|---|---|---|
| BUG-001 | ✅ Fixed | (earlier) | Proof engine narrowing — shipped pre-plan |
| BUG-002 | Active | 4 | Lookup `remove` key dispatch — collection completeness |
| BUG-003 | ✅ Fixed | 2 | Period typed-constant default crash |
| BUG-004 | Active | 5 | Proof engine event-ensure body narrowing — same family as BUG-001 |
| BUG-005 | Active (symptom-fixed) | 2 symptom + 4 root | Qualified inner types in lookup — full support is F-LANG-COLL-06 |
| BUG-006 | Active | 5 | Proof engine guard + field-`max` interval composition |
| BUG-007 | ✅ Fixed | 2 | `precept_domains` MCP crash |
| BUG-008 | ✅ Fixed | 2 | Duration typed-constant default crash |
| BUG-009 | ✅ Fixed | 2 | MCP payload-size — in-process verified clean; wire-level wrapper backstop |
| BUG-010 | ✅ Fixed (crash) / open (type-inference) | 2 crash + 4 type-inference | `now() + '<duration>'` — structured diagnostic; full inference fix Phase 4 |
| BUG-011 | ✅ Fixed | 2 | Timezone / time typed-constant default crash |
| BUG-012 | Active | 5 | Ordered-choice + literal proof gap — same proof-engine-strategy family as BUG-004 / BUG-006 |
| BUG-013 | Active (parallel-session owned) | n/a | `samples/Test.precept` missing; the test references a sample the parallel session didn't carry forward. **Parallel-session triage**: either re-add the fixture or rewrite the test inline. Not assigned to a numbered phase because the test-side fix is trivial and the sample-side ownership lives outside this plan. |

---

## Decisions captured so far (Wave 0)

From 2026-05-24 triage — **all 8 Phase-1-gating decisions settled**:

**Project-level**:
- **MVP scope** = all 98 findings except graph-analyzer modifier extensions
- **Graph-analyzer modifiers (F-LANG-SPEC-08)** = defer 10, drop `milestone` entirely
- **Drop `milestone`** from spec § 0.5 #5 — undocumented synonym of shipped `required`
- **Doc lifecycle framework adopted**: 7-stage lifecycle (Research, Design, Plan, Execute, Promote, Review, Maintain) with 5 lifecycle skills (`/lifecycle-1-research` through `/lifecycle-7-audit`)
- **Pointer-philosophy for canonical docs**: enumerable content goes to pointers + code; only conceptual why-content is hand-written in canonical
- **`precept-reviewer` agent stays as agent**, spawned by lifecycle skills at key transitions; `/lifecycle-6-review` is the user-explicit end-of-lifecycle completion check

**Phase 1 decisions** (full per-decision rationale in `compiler-readiness-review-2026-05-24.md § 1a`):
1. F-LANG-CAT-01 (HoverDescription) — Rewrite CC#19: shipped to 5 catalogs, deliberately not Token; promote hover-design + interval-hover-design to language-server.md
2. F-LANG-CAT-02 (AmbiguousDispatch) — **Drop entirely**; concept obsoleted by 3 architectural decisions
3. F-LANG-CAT-20 (IsUserFacing) — Rewrite CC#16: shipped via `Token != null` structural equivalence
4. F-LANG-CAT-15 (Operations.Resolve) — **Implement** in Phase 2 (4-line wrapper + move DisambiguateCandidates)
5. F-LANG-CAT-06 (Construct Slot Model) — Pointer-philosophy rewrite; point to grammar-generator.md
6. F-LANG-CAT-23 (Roslyn rules) — Categorized table (10 rows) + lift why from diagnostic-enforcement.md
7. F-LANG-CAT-26 (SemanticTokenTypes) — **14th catalog**; add § + resolve 13-vs-14 inconsistency
8. F-LEX-02 (Four-leg rationale) — Prospective only via `/lifecycle-2-design`; grandfather existing; no required backfill

### Resolved in Phase 2 (2026-05-25)

- ✅ F-LANG-04 — **rewrote test as catalog-driven generic `[Theory]`** instead of either option. Replaced the broken `ConstructorPattern_ExistentialFields_DslSnippet_CompilesClean` with two generic tests (`CatalogSnippet_CompilesClean` + `CatalogSnippet_HasErrors`) driven by new `CommonPattern.IsFragment` / `AntiPattern.IsFragment` / `AntiPattern.BadCompilesClean` catalog metadata. Future renames touch only the catalog. See Phase 2 commit `38712543`.
- ✅ F-X-01 — **promoted to permanent.** `F5TempVerify.cs` renamed to `SampleCompilesCleanTests.cs`; "TEMPORARY" docstring dropped; class now ships as the strict full-clean guarantee for `samples/*.precept` (complementary to the existing `SampleFieldStateRegressionTests` which checks only D130/131/132/143). 75 tests, all pass. See Phase 2 commit `38712543`.

### Resolved for Phase 3 (2026-05-25)

All three Phase-3-gating decisions are settled. Each carries the four-leg rationale (Rationale + Alternatives + Precedent + Tradeoff) per the CLAUDE.md per-decision rationale rule.

**F-LANG-PRIM-01 — String ordering**

- **Decision**: String ordering (`<` `>` `<=` `>=` on `string` / `~string`) is **intentionally out of scope**. Type-error at the spec level. Idiomatic substitutes: `choice of T(...) ordered` (for tier/rank), `startsWith` (for prefix matching), numeric/temporal types (for inherently-orderable domains).
- **Rationale**: Locale-aware string comparison is a runtime concern that's intrinsically environment-sensitive (collation rules, ICU version, normalization forms). Precept's compile-time-only proof model cannot ground claims about ordering without committing to a specific collation; committing to one would lock the language to a runtime that's wrong for many domains.
- **Alternatives considered and rejected**:
  - **Ordinal string `<` `>` shipped**: rejected — ordinal-only comparison surprises authors (e.g., `"B" < "a"` because uppercase B is U+0042, lowercase a is U+0061); makes the simple form a footgun.
  - **Locale-aware string `<` `>` shipped**: rejected — runtime dependency, non-deterministic across hosts, violates the "Determinism" philosophy principle.
  - **`~string` (CI) supports ordering**: rejected — would imply collation-aware ordering, same runtime-dependence problem.
- **Precedent**: Documented in `research/language/expressiveness/` (6 research files surveying how other languages handle this); spec § 3.6 expression-typing table; `primitive-types.md` § String Ordering — Out of Scope (four-leg rationale block).
- **Tradeoff accepted**: Tier/rank domains require the slightly more verbose `choice of string(...) ordered` shape (or equivalent integer ordering) instead of inline string comparison. Bug-012 surfaces a residual proof-engine gap when the field-vs-literal case isn't proved — tracked separately for Phase 5.
- **Settled by**: Parallel session commit `090764d3` (2026-05-25).

**F-LANG-PRIM-04 — `nonnegative` + `positive` mutex severity**

- **Decision**: **Error stays.** Doc gets updated to say "use `positive` OR `nonnegative`, not both" — no catalog change. The current mutex in `Modifiers.cs:89` (`MutuallyExclusiveWith: [ModifierKind.Positive]` on Nonnegative) remains.
- **Rationale**: Both forms are structural modifiers that participate in proof obligations; combining them is redundant because `positive` already implies `nonnegative` per the existing `Subsumes` relation. Erroring on the redundant combination is honest about intent — the author wrote two things and only one is load-bearing. A warning would tolerate noise that adds no information.
- **Alternatives considered and rejected**:
  - **Option A — Warn, not error** (recommended by reviewer but rejected by owner): would require new `RedundantModifier` warning diagnostic + dropping the mutex + a redundancy check that fires on `Subsumes`-related modifier pairs. More machinery to maintain a tolerance for noise.
  - **Split: keep mutex on accidental double-spec but warn when one subsumes another**: rejected as over-engineered for a small surface.
- **Precedent**: The `Subsumes` relation on `Positive` (`Modifiers.cs:94-95`) already declares `Positive ⊇ Nonnegative ⊇ Nonzero` — this is structural-meaning-level, not a permissions check. The mutex is the surface-syntax level: don't write both. The two layers can co-exist (Subsumes drives proof obligations, mutex drives surface checks).
- **Tradeoff accepted**: Authors who mean "this is a positive number with the doubled-up emphasis of being non-negative" must pick one form. Doc + LS hover should explain the relationship; otherwise this is a one-time author-education moment.
- **Doc action for Phase 3 execution**:
  - `docs/language/primitive-types.md` — clarify the "use `positive` OR `nonnegative`, not both" rule under the modifier surface
  - `docs/language/catalog-system.md` — note that the `Subsumes` relation is meaning-level (drives proof obligations) and the `MutuallyExclusiveWith` relation is syntax-level (drives surface checks); the two are distinct concerns and can co-exist

**F-LANG-TEMP-08 — `zoneddatetime ± period`**

- **Decision**: **Doc wins.** Remove `OperationKind.ZonedDateTimePlusPeriod` + `OperationKind.ZonedDateTimeMinusPeriod` from `src/Precept/Language/OperationKind.cs` (lines 91-92) and the corresponding `GetMeta` arms in `Operations.cs` (lines 396-401). `zoneddatetime ± period` becomes a compile error per the existing doc. Authors must navigate through `.datetime` first.
- **Rationale**: Adding a `period` (calendar-aware: months, years) to a `zoneddatetime` is genuinely ambiguous because calendar arithmetic doesn't compose cleanly across timezones (DST transitions, leap-second windows, calendar rules that depend on the zone). NodaTime's discipline forces the navigate-through-`.LocalDateTime`-then-`.InZoneLeniently()` pattern for exactly this reason; Precept inherits that discipline.
- **Alternatives considered and rejected**:
  - **Option B — Catalog wins; update doc to describe defined semantics**: rejected — requires authoring + documenting DST-edge-case behavior; subtle bug magnet; readers who write `zdt + '1 month'` would not realize implicit DST conversion is happening.
- **Precedent**: NodaTime's API design separates `Period` (calendar) from `Duration` (instant) and refuses to compose `Period` with `ZonedDateTime` directly. The doc has stated this since the temporal-type-system design (see `docs/language/temporal-type-system.md:919, 1158, 1305`); the catalog entries are vestigial and contradict the documented intent.
- **Tradeoff accepted**: Author must write `(myZdt.datetime + myPeriod).inZone(myZdt.timezone)` for calendar-aware zoned arithmetic — slightly more verbose, but makes the DST-handling choice explicit. Aligns with the "Honesty about approximation" philosophy principle: approximate behavior (DST-edge handling) must be visible in the source.
- **Code action for Phase 3 execution**:
  - Remove the two `OperationKind` enum members (renumber if needed; check uses)
  - Remove the two `GetMeta` arms in `Operations.cs`
  - Check `OperationsTests.cs` for any test that exercised these — remove or repurpose
  - Check proof engine, evaluator, and any samples for usage; samples shouldn't reference this pattern but verify

**F-LANG-BIZ-02 — Implicit `maxplaces` per currency (Position 3, settled post-research)**

- **Decision**: **Position 3 — decoupled at the default surface, with explicit `maxplaces N` as the opt-in strict-mode.** `money in '<Cur>'` carries NO implicit `maxplaces` constraint derived from ISO 4217 minor units. Authors who want Joda-Money-style strictness write `money in 'USD' maxplaces 2` explicitly. The Phase-3-original "synthesize implicit Maxplaces N at modifier resolution" work is dropped entirely. What lands in Phase 3 is the doc-only retirement of D10 from `business-domain-types.md` + a new follow-up finding (F-LANG-BIZ-09) for boundary-precision enforcement at persistence + `transition apply` + external integration.
- **Rationale**: External research (`research/architecture/compiler/currency-precision-coupling-survey.md`) surveyed Joda-Money, JSR-354, NodaMoney, Stripe/Square/Adyen, COBOL, IFRS IAS 21, US GAAP ASC 830, SQL conventions. Standards bodies (IAS 21, ASC 830) are silent on currency-derived precision — they specify rounding *behavior*, not type-level *coupling*. JSR-354 deliberately decoupled currency identity from value precision after the Joda-Money authors and Java community surveyed the design space. NodaMoney's silent auto-round violates Precept's "honesty about approximation" principle. Intermediate calculations (tax = `1.50 USD * 0.0725` = `0.108750 USD`) genuinely need sub-cent precision that an implicit `maxplaces` would reject; the boundary is where precision must be enforced, not the type system.
- **Alternatives considered and rejected**:
  - **Position 1 — Structural constraint (D10 as designed)**: Implicit `maxplaces 2` on `money in 'USD'`. Rejected: contradicts industry practice, blocks legitimate intermediate-precision use cases, makes `money in '<Cur>'` non-uniform with `quantity of '<unit>'` (which has no implicit precision).
  - **Position 2 — Soft default with silent override**: Implicit `maxplaces N` that any explicit override silently replaces. Rejected: hides the decision at the type level, surprises authors reading code that doesn't show what precision is in effect.
- **Precedent**: JSR-354 (the post-Joda-Money Java Money standardization effort) explicitly decoupled currency identity from value precision. Joda-Money offers strict (`Money`) and lenient (`BigMoney`) variants — strictness is opt-in, not default. Stripe/Square/Adyen enforce minor-unit precision at the wire boundary (request/response validation), not in the application's type system. Half-even rounding is a behavioral default in IFRS/GAAP, not a type-level constraint.
- **Tradeoff accepted**: Authors who want strict per-currency precision must opt in via explicit `maxplaces N`. The sample-corpus tidy (one canonical sample + tutorial walkthrough showing the opt-in idiom) makes the pattern discoverable. **Philosophy-tradeoff to surface to owner**: Position 3 partially relaxes "Prevention, not detection" at the type-system level — D10's implicit constraint WAS a prevention mechanism. Under Position 3, prevention RELOCATES to the wire boundary (Phase 4+ via F-LANG-BIZ-09) where authors cross into external state. Prevention as a principle is preserved; its enforcement point shifts.
- **Doc actions for Phase 3 execution**:
  - `docs/language/business-domain-types.md` — retire D10 (13 hits including the full D10 § at lines 1718-1721 and Corollary 2 at line 1824 which references D10 by analogy)
  - File F-LANG-BIZ-09 (boundary-precision enforcement) for Phase 4+
  - Lift research artifact to `research/architecture/compiler/currency-precision-coupling-survey.md`
  - Sample-corpus tidy: one canonical sample uses explicit `maxplaces 2` idiom
- **Settled by**: Conversation 2026-05-25 + external research via `/lifecycle-1-research` skill mid-planning. Recorded in heavyweight Phase 3 plan (commit follows).

### Still open — gating Phase 4+

- F-LANG-SPEC-01 (`because` on ensures): enforce or amend Principle 9? Gates Phase 2 or 3 implementation work — currently unaddressed in the active plan; flagging for Phase 4 triage.
- **F-LANG-BIZ-09** *(new finding, filed 2026-05-25 from F-LANG-BIZ-02 Position 3 spinoff)*: **Boundary-precision enforcement for money values at persistence + `transition apply` + external integration**. Under Position 3, `money in 'USD'` no longer carries an implicit `maxplaces 2` at the type system; the prevention guarantee for currency-derived precision must therefore relocate to the wire/persistence boundary. Stripe/Square/Adyen all enforce per-currency minor-unit precision at the API boundary (per the survey `research/architecture/compiler/currency-precision-coupling-survey.md`); Precept should provide a structural mechanism that enforces precision rules at boundaries where money values cross between Precept-governed and external state. **Scope**: design pass needed to define the boundary surface (`transition apply`? persistence layer? both?), the API for declaring per-field boundary-precision rules, and the diagnostic surface for boundary violations. **Cross-link**: research artifact's Open Question #1. **Target phase**: 4 or 5, owner picks during triage; non-blocking for either phase's existing scope.
- 5 additional research follow-ups from the currency-precision survey's Open Questions section (opt-in syntax discoverability beyond samples; multi-currency arithmetic safety verification; hyperinflationary / non-ISO drift policy; Temenos T24 / core-banking comparator gap; crypto / non-fiat assets) — owner triages individually if/when each becomes blocking.
- 15 additional decisions listed in `compiler-readiness-review-2026-05-24.md` § 6 (cited per-phase as work approaches).

---

# Phase 1: Doc foundation truthful

## Goal
Every doc in `docs/language/`, `docs/compiler/`, and load-bearing per-stage docs accurately describes what the implementation does. No stale Status fields. No false "✅ Resolved" claims. Spec § 0.5 cleanly separates shipped from forward-looking. `catalog-system.md` count discrepancies eliminated. Add an institutional check so this kind of drift doesn't recur.

This phase is doc-only (with the exception of any CC# items the owner decides to *implement* rather than *revert* — those become Phase 1.5 work). No code changes to pipeline / runtime / language server.

## Workstream tracker

| Workstream | Description | Status |
|---|---|---|
| A | Lifecycle skills + CONTRIBUTING.md + plan cleanup | ✅ Complete (commit `4c65ec08`, 2026-05-24) |
| B | Catalog-system.md rewrite (Decisions 1, 2, 3, 5, 6, 7 + F-LANG-CAT-AGGREGATE) | ✅ Complete (this session; 15-catalog convention adopted) |
| C | Stage doc Status truth-ups (parser, type-checker, lexer, tooling-surface, primitive/business/temporal types) | ✅ Complete (this session) |
| D | Spec § 0.5 rewrite + graph-analyzer-roadmap.md + § 1.1/§ 1.5/§ 2.1 BackArrow + grammar doc enumeration | ✅ Complete (this session) |
| E | 16 Archive promotions via `/lifecycle-5-promote --backfill` | ✅ Complete (this session; 13 ✅ Promoted, 2 📌 Header-only, 1 🔄 Relocated) |
| F | `/lifecycle-6-review --strict` verification | ✅ Complete (this session) — report at [`lifecycle-review-phase-1-2026-05-24.md`](lifecycle-review-phase-1-2026-05-24.md) |

## Findings in scope (~40)

**Catalog-system.md rewrite** (single biggest item — ~26 findings, one focused day):
- F-LANG-CAT-01 — `TokenMeta.HoverDescription` (status flip OR implement)
- F-LANG-CAT-02 — `FaultCode.AmbiguousDispatch` (status flip OR implement)
- F-LANG-CAT-03 — `ConstructKind` count 12→15
- F-LANG-CAT-04 — `ConstructSlotKind` count 17→20
- F-LANG-CAT-05 — `ConstructSlot` 4 undocumented fields + `SlotVocabulary` enum (13 members) undocumented
- F-LANG-CAT-06 — stale "Construct Slot Model" § (decision-dependent)
- F-LANG-CAT-07 — `ExpressionFormKind` count 14→15
- F-LANG-CAT-08 — `ProofRequirementKind` count 5→10 + meta + instance + satisfaction subtypes
- F-LANG-CAT-09 — `ProofRequirementMeta.DiagnosticCode` field undocumented
- F-LANG-CAT-10 — `ProofSatisfaction` DU shape drift
- F-LANG-CAT-11 — `DiagnosticCode` count 78/106→148
- F-LANG-CAT-12 — `DiagnosticMeta` 5 undocumented fields + `SuggestionSource` enum
- F-LANG-CAT-13 — `FaultCode` count 13→15
- F-LANG-CAT-14 — `FaultMeta` undocumented `Severity`/`RecoveryHint`
- F-LANG-CAT-15 — `Operations.Resolve` (decision-dependent)
- F-LANG-CAT-16 — `OperationKind` count 198→203
- F-LANG-CAT-17 — `BinaryOperationMeta` 3 undocumented fields + `ResultQualifierPolicy` enum
- F-LANG-CAT-18 — `ModifierMeta.DesugarsToRule` undocumented
- F-LANG-CAT-19 — aspirational event modifiers flagged as catalog members (consistency with § 0.5 deferral)
- F-LANG-CAT-20 — `TypeMeta.IsUserFacing` (status flip OR implement)
- F-LANG-CAT-21 — `TypeMeta` 2 undocumented fields (`ImpliedQualifiers`, `RequiredBoundQualifierAxes`)
- F-LANG-CAT-22 — `QualifierAxis` 9→10 (adds `PriceIn`) + `QualifierShape.OfRequiresCurrencyIn`
- F-LANG-CAT-23 — Roslyn rules (decision-dependent)
- F-LANG-CAT-24 — `ActionMeta.DynamicObligationGenerator` undocumented
- F-LANG-CAT-25 — `Diagnostic` struct undocumented `Args`/`RelatedSpans`
- F-LANG-CAT-26 — `SemanticTokenTypes` catalog status (decision-dependent)

**Stage doc Status truth-ups** (one paragraph each):
- F-PAR-03 — `parser.md` Status: "Implementation: Complete — Slices 1–4" → actual (Slices 1-26)
- F-PAR-02 — `parser.md` 3-week-stale Open Question on Implemented stage (trace code, resolve)
- F-PAR-01 — `Parser.cs:370, 396` stale Slice-0 TODOs for PRE0015/PRE0013
- F-TC-01 — `type-checker.md` "Gap 1 pending" → resolved (ContentValidation shipped)
- F-LANG-01 — `primitive-types.md` Status: "Designed — type checker implementation pending" → "Implemented"
- F-LANG-02 — `business-domain-types.md` Status: "Proposal — not yet implemented" → "Implemented (with documented gaps)"
- F-LANG-03 — `temporal-type-system.md` doc maturity "Draft" + status "Implemented" inconsistency
- F-LS-01 — `tooling-surface.md` status table 4 stale rows (semantic tokens Pass 2 "blocked on TypeChecker", completions/hover "partially implemented", preview "placeholder")
- F-LEX-01 — `lexer.md` 2 broken cross-references (`pipeline-overview.md`, `type-system.md`)
- F-LEX-03 — `lexer.md` "(~687 lines)" parenthetical drift

**Spec § 0.5 rewrite** (defer + drop `milestone`):
- F-LANG-SPEC-08 — implement the deferral decision: shrink § 0.5 to what's implemented (BFS reachability, terminal, dead-end, required dominator, irreversible reverse-reachability) and move the 10 deferred modifiers (`guarded`, `entry`, `isolated`, `universal`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`) to a "Future Graph Analyzer Roadmap" section or sibling doc
- Drop `milestone` reference at `precept-language-spec.md:186` — rewrite § 0.5 #5 to "Dominator analysis. Required for `required`"
- Sweep `docs/language/` and `docs/compiler/` for any other `milestone` modifier references

**Spec § 1.1 / § 1.5 / § 2.1 — `<-` BackArrow**:
- F-LANG-SPEC-11 — add "Computed field arrow" row to spec § 1.1 Operators; add `<-` to § 1.5 scan priority list; note in § 2.1 that `<-` is a structural separator (not in expression precedence)

**Grammar doc enumeration corrections**:
- F-LANG-GRAM-03 — "18 ConstructSlotKind values" → 20 (add `SuccessOutcome`, `EventEntryList`)
- F-LANG-GRAM-04 — EventDeclaration slot decomposition: rewrite from `IdentifierList + ArgumentList + InitialMarker` to single `EventEntryList`
- F-LANG-GRAM-05 — split appendix `Outcome / ActionChain` row into three (`Outcome`, `RejectClause`, `SuccessOutcome`)
- F-LANG-GRAM-06 — "14 ExpressionFormKind values" → 15 (add `InterpolatedTypedConstant`)
- F-LANG-GRAM-07 — remove "expression tree representation is deferred" framing (it's implemented in `ParsedExpression.cs`)
- F-LANG-GRAM-08 — "13 catalogs" → ≥14 (add `Outcomes` to § 9 catalog list)

**Type-system doc fixes**:
- F-LANG-PRIM-02 — `maxplaces` "decimal-only" → "applies to decimal AND money/quantity/price/exchangerate"
- F-LANG-PRIM-03 — `min`/`max` cross-lane signature wording clarification
- F-LANG-TEMP-06/07 — timezone error message specifics (could land in this phase OR Phase 3 with temporal extensions — recommend Phase 3 to bundle with other temporal work)

**Collection doc fix**:
- F-LANG-COLL-10 — `notempty` on `lookup`: remove from doc (lookup uses `KeyPresenceSafety` instead) — assumes catalog-correct ruling; alternative ruling moves this to Phase 4

**Spec § 0.6 status note**:
- Add explicit "Implementation status" sub-section under § 0.6 listing the 5 unimplemented obligations (F-LANG-SPEC-02/03/04/05 + dependent §0.6 #12) — fold into Phase 5 when shipped, but flag now so readers don't assume those guarantees are live.

**Institutional fix (drift prevention)**:
- F-X-02 — add to `CONTRIBUTING.md`:
  - Rule: "If you ship work that changes a doc's Status field or implementation state, update the Status in the same PR."
  - PR checklist item: "Doc Status fields touched by this PR (or N/A)"
  - Consider a periodic audit script that greps for `Status: Draft|Designed|Pending|Planned` on docs whose linked code paths look implemented.

## Decisions required before Phase 1 starts

**All 8 Phase-1 gating decisions are resolved as of 2026-05-24.** See the plan-level "Decisions captured so far (Wave 0)" section above and the per-decision detail in `compiler-readiness-review-2026-05-24.md § 1a`. Phase 1 is unblocked.

## Step-by-step execution

Phase 1 has 6 parallelizable workstreams. Skills enable later workstreams (build them first); the rest can run in parallel.

### Workstream A — Lifecycle skills + CONTRIBUTING.md (enables others — build first)

**Effort**: ~2-3 days (build sequence + CONTRIBUTING)

#### Step A.1 — Rename `/research` → `/lifecycle-1-research` (~30 min)
- Rename directory: `.claude/skills/research/` → `.claude/skills/lifecycle-1-research/`
- Update `SKILL.md` frontmatter `name:` field
- Sweep references: `grep -rn "/research" CLAUDE.md docs/ .claude/ tools/`
- Update description per `docs/Working/lifecycle-skill-drafts.md`

#### Step A.2 — Build `/lifecycle-5-promote` (~3-4 hr, LOAD-BEARING)
- Create `.claude/skills/lifecycle-5-promote/SKILL.md` from `docs/Working/lifecycle-skill-drafts.md` § `/lifecycle-5-promote`
- Implement `--backfill` mode (used for Archive promotion in Workstream E)
- Implement archive-header enforcement
- Test against one trivial Archive doc (e.g., one of the simpler promotions like `frank-bounds-qualifier-audit.md`)

#### Step A.3 — Build `/lifecycle-2-design` (~3-4 hr)
- Create `.claude/skills/lifecycle-2-design/SKILL.md` from draft
- Implement four-leg enforcement
- Implement doc-touch enumeration auto-fill from CLAUDE.md routing table
- Internally spawns `precept-reviewer` at lock-time

#### Step A.4 — Build `/lifecycle-3-plan` (~3-4 hr)
- Create `.claude/skills/lifecycle-3-plan/SKILL.md` from draft
- Implement heavyweight-current+next / lightweight-stubs-later structure enforcement
- Implement decisions-as-gates surfacing
- Internally spawns `precept-reviewer` against plan

#### Step A.5 — Build `/lifecycle-6-review` (~3-4 hr)
- Create `.claude/skills/lifecycle-6-review/SKILL.md` from draft
- Implement 7-step verification workflow
- Implement `--strict` and `--accept-debt` flags
- Used to verify Phase 1 itself at end (meta-consistent)

#### Step A.6 — Apply CONTRIBUTING.md updates (~30-45 min)
- Apply `docs/Working/contributing-updates-draft.md` per its embedded "Implementation notes for Phase 1"
- Cross-link from CLAUDE.md "Documentation Sync (Non-Negotiable)" section
- Verify all 5 lifecycle skill names appear in CONTRIBUTING.md

### Workstream B — Catalog-system.md rewrite

**Effort**: ~1-2 days (single longest doc-side item)

Apply Decisions 1, 2, 3, 5, 6, 7 to `docs/language/catalog-system.md`:
- **Decision 1**: rewrite CC#19 (HoverDescription) per the resolved entry
- **Decision 2**: drop CC#13 (AmbiguousDispatch) entirely from 3 locations (lines 1871, 2442, 2447-2451)
- **Decision 3**: rewrite CC#16 (IsUserFacing) per the resolved entry
- **Decision 5**: pointer-philosophy rewrite of Construct Slot Model § (lines 2041-2070)
- **Decision 6**: pointer-philosophy rewrite of Roslyn Enforcement Layer § (10-row categorized table + lifted why from `diagnostic-enforcement.md`)
- **Decision 7**: add new § for SemanticTokenTypes as 14th catalog; resolve 13-vs-14 inconsistency

Plus the broader F-LANG-CAT-AGGREGATE rewrite (count corrections across all 14 catalogs, metadata-record shape corrections, undocumented-enum surfaces from the audit appendix).

### Workstream C — Stage doc Status truth-ups (parallelizable)

**Effort**: ~½ day total (each is S, parallelizable)

Files: `parser.md` (F-PAR-01, -02, -03), `type-checker.md` (F-TC-01), `lexer.md` (F-LEX-01, -03), `tooling-surface.md` (F-LS-01), `primitive-types.md` (F-LANG-01), `business-domain-types.md` (F-LANG-02), `temporal-type-system.md` (F-LANG-03). Per-file: update Status fields; fix broken cross-references; resolve unresolved Open Questions.

### Workstream D — Spec § 0.5 + § 1.1/§ 1.5/§ 2.1 + grammar doc

**Effort**: ~1 day

- **§ 0.5 rewrite**: shrink to shipped capabilities; move 10 deferred modifiers (`guarded`, `entry`, `isolated`, `universal`, `sealed after`, `writeonce`, `advancing`, `settling`, `completing`, `absorbing`) to `docs/language/graph-analyzer-roadmap.md` (new file); rewrite § 0.5 #5 to drop `milestone`
- **§ 1.1 / § 1.5 / § 2.1**: add `<-` BackArrow to Operators table, scan-priority list, and structural-separator note (F-LANG-SPEC-11)
- **`precept-grammar.md` enumeration corrections**: F-LANG-GRAM-01, -03, -04, -05, -06, -07, -08
- **`precept-language-spec.md` § 0.6**: add Implementation Status sub-section listing the 5 unimplemented obligations (F-LANG-SPEC-02..05)

### Workstream E — 16 Archive promotions (parallelizable, needs Workstream A.2 first)

**Effort**: ~3-4 days if parallelized; ~5-7 days sequential

After `/lifecycle-5-promote` is built (Workstream A.2), use `--backfill` mode for each:

**4 already-known promotions** (from Decisions 1, 2, 6, 7):
1. `hover-design.md` + `interval-hover-design.md` → `docs/tooling/language-server.md § 7.4` (M)
2. `constructor-semantics.md` → spec § Grammar + `parser.md` + `type-checker.md` (M; drop AmbiguousDispatch story across docs)
3. `diagnostic-enforcement.md` + `diagnostic-enforcement-implementation-notes.md` → `catalog-system.md § Roslyn Enforcement Layer` (overlaps with Workstream B)
4. `language-server-implementation-plan.md` Slice 10 → `catalog-system.md § SemanticTokenTypes` (overlaps with Workstream B)

**12 new promotions** (from Archive scan, full detail in [appendix](compiler-readiness-review-2026-05-24-appendices/audit-archive-promotion-backlog.md)):

Shipped-with-why-stranded (7):
5. `catalog-compliance-audit.md` → `catalog-system.md` + per-stage docs (M)
6. `frank-bounds-qualifier-audit.md` → `business-domain-types.md` + `type-checker.md` + `diagnostic-system.md` (S)
7. `frank-qualifier-deferred-scoping.md` → `proof-engine.md § 5` + `type-checker.md` + `catalog-system.md` (M)
8. `pipeline-audit-fix-plan.md` → `CONTRIBUTING.md § Build & Test` (S) — "Release-Only Builds (Non-Negotiable)"
9. `research-conditional-construction.md` → relocate to `research/language/` + cite from spec § 1880 (S)
10. `typed-constants-and-proof-coverage-plan.md` → `literal-system.md` + `type-checker.md` + `business-domain-types.md` + `temporal-type-system.md` (L) — **single largest stranded design**
11. `elaine-typed-literal-autocomplete-ux.md` + `kramer-typed-literal-impl-plan.md` → `language-server.md § 7.3` (M, paired)

Partially-promoted (5):
12. `completions-bugs.md` → `language-server.md § 7.3` + `type-checker.md` (M)
13. `frank-catalog-obligation-audit.md` → `proof-engine.md § Obligation Generation Contract` (new heading) + `catalog-system.md` (S)
14. `mcp-dto-free-design.md` → `docs/tooling/mcp.md § Design Rationale` (S)
15. `quantity-normalization-design.md` → `runtime/evaluator.md` + `proof-engine.md` + `business-domain-types.md` + new `runtime/` content (L)
16. `syntax-coloring-fix-design.md` → `language-server.md § 7.2` (S)

### Workstream F — Verification (~½ day, runs last)

Run `/lifecycle-6-review --strict` against Phase 1 deliverables:
- All 16 Archive docs have `**Promoted to:** ...` headers
- All 8 Phase 1 decisions reflected in canonical docs
- All catalog-system.md count claims match `grep -c` of corresponding `*Kind.cs` files
- All metadata-record shape claims match actual C# records
- No "✅ Resolved" claim references code that doesn't exist
- All Stage doc Status fields reflect actual state
- `grep -rn "milestone" docs/language/precept-language-spec.md docs/compiler/` returns zero modifier-context results
- Spec § 0.5 enumerates only shipped capabilities; roadmap doc covers deferred
- Grammar doc enumeration counts match enum member counts
- All 4 lifecycle skill SKILL.md files created and validated
- CONTRIBUTING.md has lifecycle section + four-leg policy + routing table

Output: completion report. If clean: Phase 1 complete. If 🔴: remediate before proceeding to Phase 2.

## Dependencies
- Sample-edit constraint: do not touch `samples/*.precept` from this workstream — parallel session owns samples; cross-reference via `bugs.md`.
- Workstream A.2 (`/lifecycle-5-promote`) is the critical-path dependency for Workstream E. Build A.2 first; B/C/D can run in parallel after.
- All 8 Phase-1 gating decisions are settled (see plan-level "Decisions captured so far"). No pre-execution triage required.

## Exit criteria
- [ ] All 8 Phase-1 decisions recorded in this doc's § "Decisions captured" and in the remediation doc § 1a.
- [ ] `catalog-system.md` count claims match `grep -c` of corresponding `*Kind.cs` files (Tokens, Types, Operators, Functions, Actions, Modifiers, Constructs, ConstructSlots, ExpressionForms, Constraints, ProofRequirements, Outcomes, Diagnostics, Faults — 14 catalogs).
- [ ] Every metadata-record shape claim in `catalog-system.md` matches the actual C# record definition (`Token.cs`, `Type.cs`, etc.).
- [ ] No "✅ Resolved" claim in `catalog-system.md` references code that doesn't exist (revert any that the owner decided not to implement; implement the rest in Phase 7 as part of API solidity work).
- [ ] All per-stage Status fields (`parser.md`, `type-checker.md`, `lexer.md`, `tooling-surface.md`, `primitive-types.md`, `business-domain-types.md`, `temporal-type-system.md`) reflect actual implementation state.
- [ ] `grep -rn "milestone" docs/language/precept-language-spec.md docs/compiler/` returns zero results in modifier context.
- [ ] Spec § 0.5 enumerates only shipped capabilities; roadmap doc (or sub-section) covers the 10 deferred modifiers.
- [ ] Grammar doc enumeration counts match `*Kind.cs` enum member counts.
- [ ] `CONTRIBUTING.md` has the doc-Status-update rule and PR checklist item.
- [ ] No new sample-side changes in this phase.

## Estimated effort

By workstream:
- **Workstream A** (lifecycle skills + CONTRIBUTING): ~2-3 days (sequential — each skill is M)
- **Workstream B** (catalog-system.md rewrite): ~1-2 days (single longest doc-side item)
- **Workstream C** (stage doc Status truth-ups): ~½ day (parallelizable)
- **Workstream D** (spec § 0.5 + grammar + § 1.1/1.5/2.1): ~1 day
- **Workstream E** (16 Archive promotions): ~3-4 days if parallelized; ~5-7 days sequential
- **Workstream F** (verification): ~½ day

**Phase 1 total**: ~5-7 days if Workstreams B, C, D, E run in parallel after Workstream A completes the skills; ~7-10 days if more sequential.

Workstream A.2 (`/lifecycle-5-promote`) is the critical-path dependency for Workstream E. Build it first, then E can start in parallel with B/C/D.

---

# Phase 2: Green baseline + no crashes

## Goal
`dotnet test` passes 100% across all four test projects. The MCP server does not crash on invalid input. No dev-only files in the committed test tree. The compiler can be invoked from any consumer (LS, MCP, CLI) without throwing unhandled exceptions on author-error inputs. **The compile-path robustness sweep catches all MCP-layer crashes systematically, not just temporal validation.**

This phase is small in finding count but high in confidence yield: it removes the noise that masks real regressions and clears 5+ MCP-crash production bugs that have accumulated.

## Findings in scope (12+ items, expanded from 6 after bugs.md integration 2026-05-24)

### Original compiler-readiness findings (6)
- **F-LANG-SPEC-10 (P0)** — Temporal-content validation diagnostics unwired; invalid date inputs crash the MCP server via unhandled exception path. Production bug.
- **F-LS-02 (P1)** — 3 `DiagnosticPublishIntegrationTests` fail with `TaskCanceledException`, correlated with `VSTHRD003` deadlock-risk warning at the same file.
- **F-X-01 (P1)** — `F5TempVerify.cs` dev-only test file in committed tree (causes 7 of 11 baseline failures by running against samples).
- **F-LANG-04 (P1)** — `SyntaxReferenceTests.ConstructorPattern_ExistentialFields_DslSnippet_CompilesClean` references non-existent `SyntaxReference` pattern (1 of 11 baseline failures).
- **F-LANG-CAT-15 (P1)** — `Operations.Resolve` decision was **implement** — 4-line wrapper + move `DisambiguateCandidates` from `TypeChecker.Expressions.cs:922` to `Operations.cs`; update 5 TC call sites at lines 885, 891, 898, 907, 986.
- **F-LANG-SPEC-13 partial** — `NonOrderableCollectionExtreme` (PRE0065) emission audit; the related `CollectionOperationOnScalar` (PRE0047) lands in Phase 4 (F-LANG-COLL-08).

### bugs.md MCP-crash family (6) — coordinated fix pass
All six share the same symptom: `precept_compile` (or `precept_domains` for BUG-007) returns `"An error occurred invoking ..."` with no PRE-code, no diagnostic. Different code paths, same MCP-layer leakage. **Fix as a coordinated sweep**: instrument the MCP tool wrappers to never return raw "An error occurred invoking" — every exception path must be caught and converted to a structured diagnostic.

- **BUG-003** — `period` field with typed-constant default crashes (`field G as period default '1 year'`). Root: typed-constant resolution path in period-default normalization.
- **BUG-005** — `lookup of K to money in '<Currency>'` crashes at compile time. Root: parser accepts the type then mis-handles the trailing qualifier, crashing downstream. (Symptom fix here in Phase 2 — surface a clean diagnostic; root-cause fix in Phase 4 — actually support qualified-money lookup values.)
- **BUG-007** — `precept_domains` MCP tool crashes on any scope. Root: server-side fault inside `tools/Precept.Mcp/Tools/DomainsTool.cs` or its DTO assembly. This means the MCP tool review (Stage 11 of the audit) was wrong to call MCP "0 findings, healthy" — there's a P1 bug in the tool.
- **BUG-008** — `duration` field with typed-constant default crashes. Same code-path family as BUG-003.
- **BUG-010** — `now() + '<duration>'` expression crashes. Same code-path family — temporal-literal evaluation in arithmetic context.
- **BUG-011** — `timezone` and `time` fields with typed-constant default crash (`field DefaultTz as timezone default 'America/New_York'`, `field DefaultStart as time default '09:00'`). Same code-path family as BUG-003 / BUG-008 / BUG-010 — typed-constant temporal-literal handling. Almost certainly the single fix that covers BUG-003/008/010 also covers BUG-011.

### bugs.md MCP transport (1)
- **BUG-009** — `precept_compile` has an undocumented payload-size limit at ~12-15 KB. Files >14 KB crash with the same MCP-layer symptom. Likely buffer size in MCP stdio transport, JSON serialization limit, or compiler memory limit. Needs investigation in `tools/Precept.Mcp/Tools/CompileTool.cs` and the MCP wrapper config.

## Decisions required before Phase 2 starts

2 from the gating list above (unchanged):
- **F-LANG-04**: was the pattern renamed (recommend: rename test method to match `"Constructor Pattern (Atomic Creation)"`), or deliberately separate and lost (re-add an "Existential Fields" pattern)?
- **F-X-01**: delete `F5TempVerify.cs` outright (parallel session's `SampleFieldStateRegressionTests.cs` already covers samples) OR promote to permanent `SampleCompilesCleanTests.cs` (drop the "TEMPORARY" docstring, update file count from "30" to "all `samples/*.precept`", remove dev-only language)?

## Step-by-step execution

### Step 2.1 — Settle the 2 Phase-2 decisions
**Effort**: 15 minutes.

### Step 2.2 — Compile-path robustness sweep (REPLACES the original temporal-only fix)
**Files**:
- `tools/Precept.Mcp/Tools/CompileTool.cs` — wrap entry point with try/catch that converts unhandled to structured `MCP error response with diagnostic context`
- `tools/Precept.Mcp/Tools/DomainsTool.cs` — investigate BUG-007 root cause (likely DTO assembly issue or missing catalog dependency); same try/catch wrapper
- `tools/Precept.Mcp/` other tools — apply same try/catch wrapper pattern preventively
- `src/Precept/Language/Time/TemporalParser.cs` — fix the underlying throw (F-LANG-SPEC-10, BUG-003, BUG-008, BUG-010, BUG-011 all likely share this code path); wrap with try/catch that emits structured diagnostic
- `src/Precept/Pipeline/TypeChecker.Expressions.TypedConstants.cs` — validator dispatch
- `src/Precept/Language/TypedConstantValidation.cs` — validator infrastructure
- `src/Precept/Language/Diagnostics.cs` — wire diagnostic emission paths
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — remove the 5 temporal diagnostic codes from "no emission site wired" list (lines 68-72)
**Approach**:
1. **MCP wrapper instrumentation first** — every tool entry point gets a try/catch that translates raw exceptions into structured MCP error responses with the exception type, message, and where-it-happened context. Even if root-cause fixes lag, no consumer sees "An error occurred invoking" again.
2. **Root-cause fix for the temporal literal family** (F-LANG-SPEC-10, BUG-003, BUG-008, BUG-010, BUG-011): trace where the exception originates (likely NodaTime parser throws on invalid input, OR a normalizer path); wrap with try/catch and translate to structured diagnostic. Cover invalid-date (`'2026-13-01'`), invalid-time (`'25:00:00'`), invalid-instant, period default (`'1 year'`), duration default (`'14 days'`), `now() + '<duration>'` arithmetic, timezone default (`'America/New_York'`), time default (`'09:00'`).
3. **Root-cause fix for qualified-money-in-lookup symptom** (BUG-005 symptom): emit a clean `PRE0009`-style diagnostic when the parser hits `lookup of K to money in '<Currency>'` instead of crashing. (Full support for the construct lands in Phase 4.)
4. **Root-cause fix for `precept_domains`** (BUG-007): investigate and fix the underlying tool failure.
5. **Payload-size investigation** (BUG-009): identify the ~12-15 KB threshold; either remove the limit, raise it substantially, or document it explicitly with a clean error message when exceeded.
6. **Add scenario tests** for every fixed bug in the appropriate test project.
**Validation**:
- No input to any MCP tool returns `An error occurred invoking ...`. Verified via `mcp__precept__precept_compile` probe battery (the 12+ inputs from F-LANG-SPEC-10, BUG-003, BUG-005, BUG-008, BUG-009 (large file), BUG-010, BUG-011).
- `precept_domains` returns valid JSON for all scope arguments.
- Allow list updated; analyzer (`Precept0027DiagnosticEmissionCoverage`) doesn't regress.
- bugs.md entries for BUG-003, BUG-005 (symptom only), BUG-007, BUG-008, BUG-009, BUG-010, BUG-011 move from Active to Fixed with "Fixed by" notes.
**Effort**: L (2-3 days — was M when scoped to F-LANG-SPEC-10 only; expanded for the bug family)

### Step 2.3 — Investigate and fix LS publish-integration test failures (F-LS-02)
*(unchanged from prior scoping)*
**Effort**: M (1 day)

### Step 2.4 — Resolve F5TempVerify (F-X-01)
*(unchanged from prior scoping)*
**Effort**: S (1-2 hours)

### Step 2.5 — Fix SyntaxReferenceTests (F-LANG-04)
*(unchanged from prior scoping)*
**Effort**: S (30 min)

### Step 2.6 — Implement Operations.Resolve (F-LANG-CAT-15)
Decision 4 in Wave 0 was **implement**. Files:
- `src/Precept/Language/Operations.cs` — add `public static BinaryOperationMeta? Resolve(OperatorKind op, TypeKind lhs, TypeKind rhs)` wrapper
- Move `DisambiguateCandidates` from `TypeChecker.Expressions.cs:922` (currently private) to `Operations.cs` (public static)
- Update 5 TC call sites at lines 885, 891, 898, 907, 986 to use `Operations.Resolve(...)` instead of `DisambiguateCandidates(Operations.FindCandidates(...))`
- Add `Operations.ResolveTests` in `test/Precept.Tests/Language/OperationsTests.cs` — couple of basic cases (exact match, qualifier-disambiguation fallback)
- Verify spec § 5 code sample now compiles and works
**Effort**: S-M (~3-4 hours)

### Step 2.7 — Verify the baseline is green
**Approach**:
- Run `dotnet test --no-build` from repo root. Verify all 4 test projects pass.
- Run 10× to catch flakes.
- Run `dotnet build` and verify 0 warnings.
- Run `mcp__precept__precept_compile` against a battery of inputs from bugs.md repros to verify no crashes (including large files for BUG-009).
- Run `mcp__precept__precept_domains` against all scopes to verify BUG-007 fix.
**Validation**: clean baseline confirmed; recorded in remediation doc Appendix A.

## Decisions required before Phase 2 starts

2 from the gating list above:
- **F-LANG-04**: was the pattern renamed (recommend: rename test method to match `"Constructor Pattern (Atomic Creation)"`), or deliberately separate and lost (re-add an "Existential Fields" pattern)? Need to inspect git history if owner doesn't recall — `git log -p src/Precept/Language/SyntaxReference.cs` may reveal.
- **F-X-01**: delete `F5TempVerify.cs` outright (parallel session's `SampleFieldStateRegressionTests.cs` already covers samples) OR promote to permanent `SampleCompilesCleanTests.cs` (drop the "TEMPORARY" docstring, update file count from "30" to "all `samples/*.precept`", remove dev-only language)?

Plus, since Phase 1's F-LANG-CAT-15 decision was "implement," `Operations.Resolve` is a Phase 2 sub-task (Step 2.6 below).

## Dependencies
- **Phase 1 must complete first.** Docs need to be truthful before code work; otherwise the temporal validation fix might add code that contradicts a still-stale `temporal-type-system.md` claim.
- Sample-edit constraint: do not touch `samples/*.precept`. Sample failures persist after Phase 2 if they're sample-side bugs.

## Exit criteria
- [ ] `dotnet test` returns 0 failures across all 4 projects: `Precept.Tests`, `Precept.LanguageServer.Tests`, `Precept.Mcp.Tests`, `Precept.Analyzers.Tests`. Run 10× without flake.
- [ ] `dotnet build` returns 0 warnings.
- [ ] **No MCP tool returns `An error occurred invoking ...` for any input.** Verified via probe battery covering F-LANG-SPEC-10 (5 invalid temporal inputs) + BUG-003 + BUG-005 + BUG-007 + BUG-008 + BUG-009 (large file) + BUG-010 + BUG-011 (timezone + time defaults).
- [ ] `precept_domains` returns valid JSON for all scope arguments (BUG-007 fixed).
- [ ] Files >14 KB compile via `precept_compile` without crashing (BUG-009 fixed or threshold raised + documented).
- [ ] `F5TempVerify.cs` either deleted or renamed with permanent docstring.
- [ ] `SyntaxReferenceTests.cs:157` passes (no more `InvalidOperationException` from `.Single(...)`).
- [ ] `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` updated: 5 temporal diagnostic codes removed from the "no emission site wired" list.
- [ ] `Operations.Resolve` shipped; 5 TC call sites updated; spec § 5 code sample compiles.
- [ ] `bugs.md` entries for BUG-003, BUG-005 (symptom only — full fix in Phase 4), BUG-007, BUG-008, BUG-009, BUG-010, BUG-011 moved from Active to Fixed with "Fixed by" notes citing commits.
- [ ] Sample-side failures, if any remain, are routed to `bugs.md` (not modified from this workstream).

## Estimated effort
- Step 2.1 (decisions): 15 min
- Step 2.2 (compile-path robustness sweep — F-LANG-SPEC-10 + BUG-003 + BUG-005 symptom + BUG-007 + BUG-008 + BUG-009 + BUG-010 + BUG-011): 2-3 days
- Step 2.3 (LS test investigation + fix): 1 day
- Step 2.4 (F5TempVerify): 1-2 hours
- Step 2.5 (SyntaxReferenceTests): 30 min
- Step 2.6 (Operations.Resolve implementation): 3-4 hours
- Step 2.7 (verification): 30 min

**Phase 2 total**: **4-5 days** (was 2-3 days when scoped to F-LANG-SPEC-10 only; expanded after bugs.md integration to cover the full MCP-crash family).

---

# Phase 3: Type system completeness

**Goal**: Every documented capability of the primitive, temporal, and business-domain type systems is exercised by tests and works as the spec claims. The last per-`TypeKind` dispatch in `TypeChecker.Expressions.TypedConstants.cs` is replaced with catalog-driven dispatch (catalog discipline).

**Status**: ✅ **Complete 2026-05-25.** All 14 findings closed, 6136/6137 Precept.Tests pass (1 pre-existing BUG-013), 411/411 LS tests pass, 291/291 analyzer tests pass. BUG-010 fully fixed; D10 retired (Position 3); F-TC-04 last TypeKind dispatch eliminated; F-LANG-BIZ-09 filed for Phase 4+.

## Findings in scope (~13)

**Decision executions (3):**
- F-LANG-PRIM-04 (doc clarification — error stays)
- F-LANG-TEMP-08 (remove `OperationKind.ZonedDateTimePlusPeriod` + `MinusPeriod` from catalog)
- F-LANG-PRIM-01 (already shipped by parallel session; Phase 3 verifies)

**Temporal extensions (5):**
- F-LANG-TEMP-01/02 (context-aware `TemporalQuantityParser`)
- F-LANG-TEMP-03 (nonzero/nonnegative on duration + period)
- F-LANG-TEMP-05 (mixed temporal quantities — relax conditional on context)
- F-LANG-TEMP-06/07 (timezone error message + RecoverySteps)

**Business-domain types (6):**
- F-LANG-BIZ-02 — **dropped per Position 3 decision**; replaced by doc-only retire-D10 (Step 3.3b)
- F-LANG-BIZ-03 (currency accessors `.name`/`.minorUnit`/`.numericCode`/`.symbol`) — **absorbs Frank's case-9** interpolation-slot resolution gap
- F-LANG-BIZ-04 (`CurrencyCatalog` public API)
- F-LANG-BIZ-05 (quantity × quantity non-cancelling rejection — verification)
- F-LANG-BIZ-06 (exchangerate implicit `positive`)
- F-LANG-BIZ-08 (discrete equality narrowing — verification; likely files Phase 5)

**Catalog discipline (1):**
- F-TC-04 (per-`TypeKind` dispatch in typed-constants → catalog-driven; also closes adjacent `InterpolationUnsupportedTypes` FrozenSet smell)

## Step-by-step execution

### Step 3.1 — Decision executions (½ day, S)

- **3.1a F-LANG-PRIM-04 doc clarification**: `docs/language/primitive-types.md` (sections at lines ~197, 230, 265) — add "use `positive` OR `nonnegative`, not both" note under each numeric-type modifier list. `docs/language/catalog-system.md` § Modifiers — add the meaning-vs-syntax layered-concerns note (`Subsumes` drives proof-obligation discharge + `RedundantModifier`; `MutuallyExclusiveWith` drives `ConflictingModifiers` error; both fire on `nonnegative + positive` intentionally).
- **3.1b F-LANG-TEMP-08 catalog cleanup**: `src/Precept/Language/OperationKind.cs:91-92` delete `ZonedDateTimePlusPeriod` + `MinusPeriod`; `src/Precept/Language/Operations.cs:396-401` delete the two GetMeta arms. Pre-execution doc-touch grep: `grep -rn "ZonedDateTime.*Period\|operation count" docs/` to catch any spec-level docs needing update.
- **3.1c F-LANG-PRIM-01 ratification**: verification only — confirm parallel-session work shipped at `docs/language/primitive-types.md § String Ordering — Out of Scope` and `precept-language-spec.md § 3.6`.

### Step 3.2 — Temporal extensions (2-3 days, L)

- **3.2a F-LANG-TEMP-01/02 (1-1.5 days)**: Add optional `TypeKind? expectedType` param to `TemporalQuantityParser.Parse` (`src/Precept/Language/Time/TemporalQuantityParser.cs:11-64`). Pass through `TemporalValidator.Validate()` (`TemporalValidator.cs:13`). Context flows: `Period` accepts any NodaTime-Period-admitted unit; `Duration` accepts any unit form; null preserves existing ambiguity heuristic. Tests: `field G as period default '3 days'` clean; `set X = now() + '365 days'` clean (closes BUG-010 type-inference residual).
- **3.2b F-LANG-TEMP-03 (¼ day)**: `src/Precept/Language/Modifiers.cs:16-21` — add `Duration` + `Period` to `ZeroBoundNumericTypes`. Spot-check `ProofEngine.Strategies.cs` discharge paths (should already work since duration/period are numeric under the hood).
- **3.2c F-LANG-TEMP-05 (¼ day)**: `TemporalQuantityParser.cs:54` — TEMP005 emission becomes conditional on `expectedType` being null/ambiguous. Mixed forms like `'1 day 2 hours'` accept under known context; preserve PRE0091 fallback for ambiguous.
- **3.2d F-LANG-TEMP-06/07 (¼ day)**: `TemporalParser.cs:113-119` — better inline message (suggest IANA `Region/City` format). `Diagnostics.cs` — add RecoverySteps to the timezone-routing diagnostic.

### Step 3.3 — Business-domain types (2-2.5 days, M)

- **3.3a F-LANG-BIZ-04 (½ day)**: `src/Precept/Language/CurrencyCatalog.cs` — add `Default`/`Get`/`TryGet`/`GetByNumericCode`/`IsValid`/`DataVersion`. MCP doc-sync check: decide whether `DataVersion` lights up in `precept_domains scope=currencies`. Extend existing `CurrencyCatalogTests.cs` (5 `[Fact]`s already cover `.All`).
- **3.3b F-LANG-BIZ-02 retire D10 (¼ day, doc-only)**: 13-site D10 hit map in `business-domain-types.md` (lines 86, 503, 603, 1538, 1543, 1718-1721 full §, 1737, 1818, 1822, 1824 Corollary 2 rewrite, 2025 teachable-example). Corollary 2 stands on its own (rates bidirectional; zero/negative meaningless) — drops the broken D10 analogy. Step 3.3d ratifies what Corollary 2 already states.
- **3.3c F-LANG-BIZ-03 + Frank's case-9 (1-1.5 days, M)**: Part 1 — add 4 `FixedReturnAccessor` entries on Currency in `src/Precept/Language/Types.cs`. Part 2 — typed-constant interpolation-slot resolution: extend `TypeChecker.cs:MapInterpolatedQualifier` to resolve currency-member-access slots (mirror unit-slot pattern). Tests: same-currency `'{A.amount} {A.currency}'` clean; cross-currency emits PRE0068.
- **3.3d F-LANG-BIZ-06 (¼ day)**: `src/Precept/Language/Types.cs` ExchangeRate entry — add `ImpliedModifiers: [ModifierKind.Positive]`. Uses existing infrastructure (Currency.notempty precedent).
- **3.3e F-LANG-BIZ-05 (½ day, verification)**: scenario tests for quantity × quantity cross-dimension. If specific diagnostic missing, file for Phase 5.
- **3.3f F-LANG-BIZ-08 (½ day, verification)**: equality-narrowing scenario tests. Likely files Phase 5 (proof-engine strategy addition).

### Step 3.4 — F-TC-04 catalog-driven typed-constant dispatch (1 day, M)

- Extend `ContentValidation` DU subtypes in `src/Precept/Language/Type.cs:139-197` with per-subtype `InterpolationForms` field.
- Wire form tables into per-type `ContentValidation` declarations in `src/Precept/Language/Types.cs`.
- Delete `GetFormsForType` kind-switch (`TypeChecker.Expressions.TypedConstants.cs:285-297`); replace call at line 482 with catalog lookup.
- Close adjacent smell: `InterpolationUnsupportedTypes` FrozenSet (`TypeChecker.Expressions.TypedConstants.cs:92-96`) — replace `Contains(type)` with catalog lookup (`ContentValidation?.InterpolationForms is null`).
- All existing typed-constant tests must pass identically — refactor is structural; behavior identical.

### Step 3.5 — Verification + commit (½ day, S)

- `dotnet build` 0 warnings/errors; `dotnet test` × 3 runs, no flakes
- MCP probe battery (9 inputs ratifying TEMP-01/02/03/05/06/07/08 + BIZ-02 Position 3 + BIZ-03 + BIZ-06)
- Catalog-discipline grep: no `GetFormsForType`, no `type switch` per-Kind dispatch in TypedConstants
- Sample-corpus tidy: `samples/insurance-claim-adjudication.precept` + tutorial walkthrough use explicit `maxplaces 2` opt-in idiom (sample-edit-constraint exception documented in commit body)
- One Phase 3 commit + tracker update (Phase 3 row ✅ Complete; BUG-010 fully Fixed)

## Decisions required

**None.** All 4 Phase-3-gating decisions are settled (see § "Resolved for Phase 3" above for four-leg rationale on each).

**Open execution-time decisions** (non-gating, defaults below):
- F-LANG-BIZ-04 `Default` semantics → recommend `null` (Precept doesn't pick currency on author's behalf)
- F-LANG-BIZ-02 canonical-sample idiom → recommend `samples/insurance-claim-adjudication.precept` (sample-edit-constraint exception load-bearing for discoverability)
- F-LANG-TEMP-03 Period applicability → recommend YES (Periods can be zero/negative in NodaTime)
- F-LANG-BIZ-05/BIZ-08 gap reporting → if larger than Phase 3 scope, file for Phase 5 (don't expand)
- F-LANG-TEMP-05 mixed-quantity period semantics → recommend permit (NodaTime supports it)

## Dependencies

- Phase 1 ✅ complete; Phase 2 ✅ complete
- Sample-edit constraint: only the one canonical sample (`insurance-claim-adjudication.precept`) gets touched in Phase 3 for F-LANG-BIZ-02 discoverability; BUG-002 / BUG-012 sample workarounds persist until Phase 4/5

## Exit criteria (testable, ≥13 conditions)

- [ ] `dotnet build` 0 warnings, 0 errors
- [ ] `dotnet test --no-build` 0 failures across all 4 projects (except BUG-013, parallel-session-owned); 3× run, no flakes
- [ ] MCP probe battery (9 inputs) all return expected outcomes
- [ ] `OperationKind` no longer contains `ZonedDateTimePlusPeriod` / `MinusPeriod`
- [ ] `Modifiers.ZeroBoundNumericTypes` contains `Duration` and `Period`
- [ ] `CurrencyCatalog` exposes 6 new public surface methods/properties
- [ ] Currency TypeMeta has 4 accessors
- [ ] Typed-constant interpolation resolves currency-member-access slots (Frank's case-9 closed)
- [ ] ExchangeRate TypeMeta has `ImpliedModifiers: [Positive]`
- [ ] **`money` TypeMeta has NO `ImpliedModifiers`** (Position 3 ratification)
- [ ] D10 retired from `business-domain-types.md` (grep returns 0)
- [ ] Research artifact at `research/architecture/compiler/currency-precision-coupling-survey.md`
- [ ] F-LANG-BIZ-09 (boundary-precision) filed
- [ ] Sample-corpus opt-in idiom: at least 1 canonical sample uses `maxplaces 2`
- [ ] `GetFormsForType` switch eliminated; `InterpolationUnsupportedTypes` eliminated
- [ ] `bugs.md` BUG-010 fully in ## Fixed
- [ ] Doc-touches landed per CLAUDE.md routing table (primitive-types.md, business-domain-types.md, temporal-type-system.md, catalog-system.md, diagnostic-system.md, type-checker.md)

## Estimated effort

**M (~4-6 days)**. Was L 5-7 days; +1 day absorbed Frank's case-9 into 3.3c; then −1.5 days when F-LANG-BIZ-02's implicit-modifier work dropped under Position 3. The largest single subtask in 3.3 (implicit per-currency maxplaces) was also the only risk hotspot; both went away.

| Step | Day-band |
|---|---|
| 3.1 — Decision executions | ½ day, S |
| 3.2 — Temporal extensions | 2-3 days, L |
| 3.3 — Business-domain types | 2-2.5 days, M |
| 3.4 — Catalog-driven dispatch | 1 day, M |
| 3.5 — Verification + commit | ½ day, S |

## Doc-update obligations (per CLAUDE.md routing table)

| Sub-step | Docs to update |
|---|---|
| 3.1a | `primitive-types.md`, `catalog-system.md` |
| 3.1b | `temporal-type-system.md` (verify), `catalog-system.md` (operation count), pre-execution grep for spec-level docs |
| 3.2a | `temporal-type-system.md`, `compiler/type-checker.md` |
| 3.2b | `temporal-type-system.md`, `catalog-system.md` § Modifiers |
| 3.2c | `temporal-type-system.md` |
| 3.2d | `compiler/diagnostic-system.md` |
| 3.3a | `business-domain-types.md` § CurrencyCatalog, `catalog-system.md`, `tools/Precept.Mcp/CatalogFormatters.cs` + `tooling/mcp.md` if `DataVersion` surfaces |
| 3.3b | `business-domain-types.md` (retire D10 13 sites + Corollary 2 rewrite); F-LANG-BIZ-09 finding; new research artifact |
| 3.3c | `business-domain-types.md` § Currency accessors + § Interpolation-slot resolution, `compiler/type-checker.md` |
| 3.3d | `business-domain-types.md` § ExchangeRate-implicit-positive |
| 3.3e/3.3f | `business-domain-types.md` or `bugs.md` (verify-then-file) |
| 3.4 | `catalog-system.md` § ContentValidation DU shape, `compiler/type-checker.md` (catalog discipline restored) |
| 3.5 | this plan (tracker), `bugs.md` (BUG-010 status) |

## Discovered during planning

1. **Frank's case-9 currency-member-access interpolation gap** absorbed into Step 3.3c (was case 9 of `frank-price-qualifier-full-analysis.md`). Adding currency accessors without extending interpolation-slot resolution would compound the silent gap. Step 3.3c grew ¼ day → 1-1.5 days; mirrors existing unit-slot resolution pattern.
2. **F-LANG-BIZ-02 implicit-precision premise was wrong** — dropped via external research (`/lifecycle-1-research` skill, full survey in research artifact). Position 3 ratified; D10 retires doc-only; F-LANG-BIZ-09 filed for Phase 4+ boundary enforcement. Phase 3 effort drops L→M.
3. **F-LANG-BIZ-08 narrowing is design-required for Phase 5** — confirmed proof engine has no choice-equality narrowing strategy today. Step 3.3f verify-then-file rather than absorbing into Phase 3.
4. **5 additional research follow-ups** from the currency-precision survey's Open Questions (multi-currency arithmetic safety, hyperinflationary drift, core-banking comparator gap, crypto, opt-in discoverability) — to be filed alongside the F-LANG-BIZ-09 lift.

Planning artifact: `/home/sfalik/.claude/plans/refactored-yawning-fern.md` (heavyweight Phase 3 plan, reviewer-audited 2026-05-25).

---

# Phase 4: Collection completeness

**Goal**: Every documented capability of the 9 collection types works as specified. The catalog's action-applicability metadata is actually enforced. Two-field quantifier bindings for ordered collections work. Qualified inner types parse. The grammar doc's vocabulary matches code.

**Findings in scope** (~16, expanded after bugs.md integration):
- F-LANG-COLL-04 (.at(N) index-bounds proof obligation)
- F-LANG-COLL-05 (log-by append uniqueness proof obligation)
- F-LANG-COLL-06 (qualified inner types `set of money in 'USD'`, `set of quantity of 'length'`, etc.) — **root-cause fix for BUG-005 symptom** (Phase 2 ships clean diagnostic; Phase 4 actually supports the construct)
- F-LANG-COLL-07 (queue of T by P two-field quantifier binding `.value`/`.by`)
- F-LANG-COLL-08 (action `ApplicableTo` enforcement — emit PRE0047/0048)
- F-LANG-COLL-09 (insert/remove-at index-bounds proof obligations)
- F-LANG-COLL-11 (MissingOrderingKey rename + allocation)
- F-LANG-COLL-02 (choice-in-collection-inner targeted diagnostic)
- F-LANG-COLL-03 (ordered-choice trait propagation verification)
- F-LANG-COLL-12 (Countof/Peekby inert tokens — remove or wire per decision)
- F-LANG-GRAM-01 (vestigial `ConstructionRow` enum value — delete or document)
- F-LANG-GRAM-02 (rename `ConstructionRowReject` → `EventRowReject`)
- F-LANG-CAT-08 partial (`ProofRequirementKind` rewrite — the doc side; this phase ships new proof requirement code that the rewrite reflects)
- **BUG-002** — `remove` on lookup expects value type instead of key. Fix: extend action catalog with key-removal shape for lookups, OR change `remove` dispatch on lookup to expect key type. Quality bar; worth fixing before lookup becomes more visible in tutorials. Workaround in samples uses `put k = 0` (orphan zero-valued entries).

**Decisions required**:
- F-LANG-COLL-11: rename PRE0104 to `RequiredTraitViolation` and allocate fresh code for missing-`by`, OR route missing-`by` through the new `CollectionOperationOnScalar` enforcement (Wave 4 from F-LANG-COLL-08)?
- F-LANG-COLL-12: wire `Countof`/`Peekby` as keywords or remove from `TokenKind.cs`/`Tokens.cs`?

**Status**: Stub — detailed execution plan TBD pending Phase 3 completion and the 2 listed decisions.
**Estimated effort**: L (~1-1.5 weeks — F-LANG-COLL-06 is itself multi-day; F-LANG-COLL-08 needs a regression matrix test; F-LANG-COLL-07 needs new binding-shape infrastructure).

---

# Phase 5: Proof engine satisfiability extensions

**Goal**: Close § 0.6 of the spec — the proof engine delivers all 13 documented obligation types. Dead/contradictory/vacuous/tautological detection ships. Collection proof obligations (.at bounds, log-by uniqueness, insert/remove-at bounds) ship. Field-level dead-code detection (`FieldNeverSet`) ships, co-shipping with access-modifier unification (`writable` → `editable`).

**Findings in scope** (~14, expanded after bugs.md integration + 2026-05-25 design lock):
- F-LANG-SPEC-02 (dead-guard detection — `UnsatisfiableGuard` PRE0082)
- F-LANG-SPEC-03 (contradictory rule detection — new diagnostic)
- F-LANG-SPEC-04 (vacuous rule detection)
- F-LANG-SPEC-05 (tautological guard detection)
- F-LANG-SPEC-12 (sharpened routing diagnostics — depends on F-LANG-SPEC-02 + -05)
- F-LANG-COLL-04/05/09 (collection proof obligations — could land in Phase 4 alongside the relevant collection feature; bundling depends on team preference)
- F-LANG-BIZ-01 (money/price cancellation — proof for qualifier chain)
- F-LANG-BIZ-05 (quantity × quantity policy enforcement verification)
- F-LANG-TEMP-04 (always-false period literal comparison — small constant-folding analyzer)
- **BUG-004** — Proof engine ignores event ensures for transition-row body narrowing. Fix: extend guard-extraction switch in `ProofEngine.Strategies.cs` so event ensures on the row's event contribute their `is set` predicates to body narrowing. Mirrors the BUG-001 fix shape. Trivial-to-small.
- **BUG-006** — Proof engine doesn't combine guard narrowing with field-level `max` for arithmetic interval inference. Fix: extend the interval-narrowing strategy to compose guard-derived field bounds with field-modifier-derived bounds across rows. Design-required; same architectural family as the new dead-guard / contradictory-rule machinery.
- **BUG-012** — Ordinal comparison between an ordered-choice field and a choice-literal (`Severity <= 2`, `Tier <= "Medium"`) cannot be proved; the `Both choice operands must be declared ordered` obligation falls through to Unresolved when one operand is a literal. Fix: either (a) lift the `Ordered` modifier from the contextual choice-set type when one operand is a literal and the other a typed ordered-choice field, or (b) add a typed-literal strategy that infers the modifier from the operand's expected type. Small-to-medium; self-contained to the proof engine; no language surface change. Same architectural family as BUG-004 / BUG-006 (proof-engine strategy extension).
- **F-LANG-GRAPH-04 (FieldNeverSet + access-modifier unification)** — fully planned 2026-05-25. Design: [`field-never-set-diagnostic.md`](field-never-set-diagnostic.md) v2. See § F-LANG-GRAPH-04 execution slices below.

**Decisions required**:
- F-LANG-TEMP-04: implement always-false period comparison warning or drop spec promise?
- F-LANG-BIZ-01: add `MoneyDividePrice → Quantity` to catalog or accept asymmetric algebra?
- F-LANG-GRAPH-04: **all 6 design decisions locked** — see design doc.

**Status**: Partial plan — F-LANG-GRAPH-04 has detailed execution slices below; remaining findings stubbed pending Phase 4 completion. **This is the highest-variance phase** — each new proof obligation involves design questions about completeness, soundness, and counterexample reporting. May spawn additional Phase 5.1, 5.2 work.
**Estimated effort**: XL (~2-3 weeks across all findings; F-LANG-GRAPH-04 is L within that envelope, ~5-7 days).

## F-LANG-GRAPH-04 execution

Design: [`field-never-set-diagnostic.md`](field-never-set-diagnostic.md) v2 (Locked 2026-05-25). Six locked decisions ground the work; all four legs filled. Ships two coupled deliverables: `FieldNeverSet` Warning diagnostic via a new graph-analyzer sub-pass, and an access-modifier unification refactor that retires `ModifierKind.Writable`, removes the keyword `writable`, and consolidates field-level write capability under `editable` (Access Modifier, now valid at both field-declaration and per-state `modify` row sites).

### Slice tracker

| # | Slice | Depends on | Effort | Parallelizable |
|---|-------|------------|--------|----------------|
| 1 | Access-modifier unification (catalog + parser + lexer + tokens) | — | M (~1d) | No |
| 2 | Sample corpus mechanical rename (`writable` → `editable`) | 1 | S (~½d) | No |
| 3 | `ActionMeta.WriteSemantics` catalog property | — | S (~½d) | Yes (with 1) |
| 4 | `FieldNeverSet` diagnostic catalog entry + count corrections | — | S (~½d) | Yes (with 1, 3) |
| 5 | `FieldWriteSiteAnalyzer` implementation | 1, 3, 4 | M (~1-1.5d) | No |
| 6 | Sample corpus `FieldNeverSet` trip sweep | 2, 5 | M (~1-2d, high variance) | No |
| 7 | Doc updates (full enumeration from design § Doc-update enumeration) | 1, 3, 4, 5 | M (~1d) | Yes (with 6) |
| 8 | Verification + `/lifecycle-6-review --strict` | 6, 7 | S (~½d) | No |

### Slice 1: Access-modifier unification

Eliminate `ModifierKind.Writable` and the keyword `writable`; consolidate write-capability declaration under `editable` (Access Modifier) at both field-declaration and per-state `modify` positions. Per design Decision 5.

Files: `ModifierKind.cs` (remove `Writable = 15`), `Modifiers.cs` (drop `Writable` arm, expand `Write` arm with `ApplicableDeclarationSites`), `AccessModifierMeta` (add `ApplicableDeclarationSites` field), `TokenKind.cs` (remove `Writable`), `Tokens.cs` (drop `Writable` entry), `Lexer.cs` (drop keyword recognition), `Parser.*.cs` (accept `editable` at field-declaration position), `DiagnosticCode.cs` (`WritableOnEventArg` → `EditableOnEventArg`, preserve ordinal), `Diagnostics.cs` (update message + examples). Regenerate `tmLanguage.json` from the catalog — do not hand-edit.

Tests: `ModifierKindTests.Writable_IsRemoved`, `TokenKindTests.Writable_IsRemoved`, `AccessModifierMetaTests.Write_ValidAtFieldDeclaration`, `Write_ValidAtModifyRow`, `Parser/FieldModifierParsingTests` extensions.

Exit: catalog reports `ModifierKind.Writable` undefined; `precept_modifiers` MCP no longer mentions `writable`.

### Slice 2: Sample corpus mechanical rename

`git grep -l '\bwritable\b' samples/` → `sed -i 's/\bwritable\b/editable/g' samples/*.precept` → diff review → `SampleCompilesCleanTests` clean. Watch for `writable` in comments where meaning is documentation, not keyword.

Exit: zero `writable` keywords in corpus; 75 samples compile clean.

### Slice 3: `ActionMeta.WriteSemantics` catalog property

Make write-site classification catalog-driven. Per design Decision 6.

Files: new `ActionWriteSemantics.cs` (`enum { None | EstablishesValue | MutatesContents | ClearsContents }`), `Action.cs`/`ActionMeta.cs` (add `WriteSemantics` field), `Actions.cs` (populate every entry).

Classification table:
- `EstablishesValue`: `set`, `put`, `add`, `enqueue`, `append`, `insert`
- `ClearsContents`: `clear`, `remove`, `removeAt`, `dequeue`, `pop`
- `None`: `reject`, `transition`, `no transition`

Tests: `ActionMetaTests.WriteSemantics_PopulatedForEveryActionKind`, `ActionMetaTests.WriteSemantics_Classification` (parameterized).

Exit: `precept_operations` (or actions equivalent) surfaces `WriteSemantics`; catalog tests pass.

### Slice 4: Diagnostic catalog entry + count corrections

`DiagnosticCode.cs`: add `FieldNeverSet = 150,` after current top `McpToolInternalError = 149`. `Diagnostics.cs`: full `GetMeta` entry (Severity.Warning, Stage.Graph, Category.Structure, message `"Field '{0}' has no write site — it can only hold its declared default or remain unset"`, FixHint, TriggerCondition, RecoverySteps, ExampleBefore/After, RelatedCodes: `[UnreachableState, UnhandledEvent]`). `DiagnosticCoverageAllowLists.cs`: register `FieldNeverSet` in `Gate2AllowList`.

**Bundled doc count drift fix** (pre-existing 148→149 drift in `catalog-system.md` is also corrected here):
- `docs/language/catalog-system.md` lines 290, 716, 1989, 1993 — bump count to **150**
- `docs/Working/compiler-readiness-review-2026-05-24.md` — bump count to 150

Tests: `DiagnosticCatalogTests` reflection gate covers via iteration; `Precept.Mcp.Tests` verifies `precept_diagnostic("FieldNeverSet")` returns full metadata.

Exit: catalog reports 150 diagnostics everywhere; MCP tool surfaces the new code.

### Slice 5: `FieldWriteSiteAnalyzer` implementation

New partial-class extension `src/Precept/Pipeline/GraphAnalyzer.FieldWriteSites.cs` following the existing `Parser.Actions.cs`/`ProofEngine.Strategies.cs` convention. `AnalyzeFieldWriteSites(ctx)` walks the type-checked program:

1. Initialize empty write-site set per declared field
2. Transition-row actions: filter via `ActionMeta.WriteSemantics == EstablishesValue`
3. State-entry hooks: same filter
4. Computed-field declarations: implicit write
5. Field-level Access Modifier: any field with `ModifierKind.Write` (= `editable`) at field-declaration site
6. Per-state `modify F <mode>` rows: filter via `AccessModifierMeta.IsWritable == true`
7. Construction-event arg → field assignments (reuse `TypeChecker.Validation.FieldState.cs` enumeration if convenient)
8. Emit `FieldNeverSet` for each field with empty write-site set

Wire into `GraphAnalyzer.cs` after reachability + completeness, before serialization.

Tests: `FieldWriteSiteAnalyzerTests` (aggregator unit tests per shape); `FieldNeverSetEmissionTests` (`Trips_OnDeclaredButNeverSet`, `NoTrip_OnEachWriteSiteShape` parameterized over 8 shapes, `Trips_OnOptionalNoDefaultNoWrites`, `Trips_OnClearOnlyField`, `Trips_OnRemoveOnlyField`).

Exit: all analyzer tests pass; expect new warnings on samples until Slice 6.

### Slice 6: Sample corpus `FieldNeverSet` sweep

Run analyzer against every `samples/*.precept` via `precept_compile`. For each warning, classify and fix:
- Dead field (no purpose) → delete
- Forgotten governance (should have been wired into Create or set in transition) → wire it
- Should be computed → convert to `field X as T <- expr`
- Data-only by intent → flag for deferred `FieldNeverRead` design; do not annotate this slice (FieldNeverSet doesn't false-positive on these)

Known starting candidates (from this session's exploratory work):
- `samples/production-order-tracking.precept:24` — `Priority` IS set via `Create.Priority`; will NOT trip `FieldNeverSet`; deferred to `FieldNeverRead` design.

Other tripping samples enumerated by running the analyzer at slice start.

Exit: `SampleCompilesCleanTests` (75 samples) passes — zero `FieldNeverSet` warnings.

**High-variance slice**: if sweep surfaces > 15 samples needing fixes, escalate scope.

### Slice 7: Doc updates

Per design § Doc-update enumeration:

For the diagnostic:
- `docs/compiler/diagnostic-system.md` — add `FieldNeverSet`; rename `WritableOnEventArg` → `EditableOnEventArg`
- `docs/compiler/graph-analyzer.md` § 6 — document `FieldWriteSites` sub-pass

For the unification:
- `docs/language/precept-language-spec.md` § 2.2 (lines ~996-1017) — rewrite to single `editable` Access Modifier
- `docs/language/precept-language-spec.md` line 272 — drop `Writable` from keyword table; adjust `Write` description
- `docs/language/precept-language-spec.md` lines 1647-1651 — rename `WritableOnEventArg` → `EditableOnEventArg`
- `docs/language/primitive-types.md` — verify field-declaration examples reference `editable`
- `docs/language/catalog-system.md` § Modifier Catalog — drop `Writable` from Value Modifier section; add field-declaration applicability to `Write` in Access Modifier section

For the analyzer / catalog property:
- `docs/language/catalog-system.md` § Action Catalog — document `WriteSemantics`
- `docs/compiler/type-checker.md` — note `ActionMeta.WriteSemantics` if doc enumerates `ActionMeta` fields

Exit: `/lifecycle-6-review --strict` reports no doc-update gaps.

### Slice 8: Verification

- `dotnet test` all suites green
- `precept_compile` across `samples/` — zero `FieldNeverSet` warnings
- `precept_diagnostic("FieldNeverSet")` — full metadata; `precept_diagnostic("WritableOnEventArg")` — not-found
- `/lifecycle-6-review --strict` clean on PR
- Spot-check regenerated `tmLanguage.json`
- Manual smoke in VS Code: `field X as string editable` works; `field X as string writable` is a parser error

Exit: all 12 design acceptance criteria pass; PR mergeable.

### Risks

- **Slice 6 scope creep**: sweep may surface unexpected dead fields. Budget M-L; escalate if > 15 samples affected.
- **Grammar regen surfaces issues**: `tmLanguage.json` regeneration may expose latent generator bugs. Regen + diff before commit; log as separate bug if generator misbehaves.
- **`WritableOnEventArg` rename misses references**: grep full codebase for `WritableOnEventArg` before merging Slice 1.

---

# Phase 6: `units` block + composite period basis

**Goal**: Two new declarative features ship: entity-scoped `units` block (F-LANG-BIZ-09) and composite period basis with `&` separator (F-LANG-BIZ-07).

**Findings in scope** (2):
- F-LANG-BIZ-07 (composite period basis `'years&months'` — parser + qualifier-value handling)
- F-LANG-BIZ-09 (entity-scoped `units` block — new declarative construct: catalog entry, parser, name binder integration, type checker integration, doc updates)

**Decisions required**: None — both auto-decided by "build everything in MVP."

**Status**: Stub — detailed execution plan TBD pending Phase 5 completion. F-LANG-BIZ-09 is a meaningful new construct — full Construct catalog entry, parser dispatch, slot model, samples needed.
**Estimated effort**: L (~1 week — `units` block is the most "new feature"-shaped item in MVP).

---

# Phase 7: API surface solidity (typed descriptors + analyzer extensions)

**Goal**: The compile-time public API surface uses typed descriptors, not string-keyed field references. Defensive-throw pattern removed in favor of compile-time enforcement via extended `[HandlesCatalogExhaustively]` coverage.

**Findings in scope** (~6):
- F-API-01 (typed field descriptor refactor — D8/R4 + G1/G9 milestones — affects `Inspection.cs`, `SharedTypes.cs`, `UpdateOutcome.cs`, `SemanticIndex.TypedFieldRef`)
- F-X-03 (extend `[HandlesCatalogExhaustively]` analyzer coverage)
- F-PAR-04 (defensive `InvalidOperationException` in Parser dispatch — becomes unreachable after F-X-03)
- F-TC-02 (D26 invariant runtime throws → factory-enforced construction)
- F-TC-03 (D5 SecondaryExpression invariant → DU subtype split)
- F-LEX-02 (per-decision rationale policy enforcement — if owner ruled "all four legs required even retroactively," this phase backfills the rationale across stage docs)

**Decisions required**:
- F-API-01: ship as standalone refactor before runtime (this phase), or open the runtime phase with it? Owner ruling needed at planning time.

**Status**: Stub — detailed execution plan TBD pending Phase 6 completion and F-API-01 sequencing decision.
**Estimated effort**: M-L (~3-5 days — F-API-01 alone is a broad public-API change; the analyzer extensions are smaller).

---

# Phase 8: Diagnostic completeness

**Goal**: Every diagnostic code declared in `DiagnosticCode.cs` is either emitted from a real code path, has scenario-test coverage that asserts it fires, or is explicitly retired. CI enforcement is bidirectional.

**Findings in scope** (~7):
- F-LANG-SPEC-06 (~startsWith/~endsWith first-arg-must-be-~string enforcement)
- F-LANG-SPEC-09 (ChoiceElementTypeMismatch / ChoiceMissingElementType emission OR retirement)
- F-LANG-SPEC-12 (OutOfRange constant-literal bounds check emission OR retirement)
- F-LANG-SPEC-13 (NonOrderableCollectionExtreme emission OR consolidation with TypeMismatch)
- Diagnostic scenario-coverage matrix completion (all 148 codes have scenario tests beyond just structural reflection)

**Decisions required**:
- F-LANG-SPEC-09: wire or retire ChoiceElementTypeMismatch / ChoiceMissingElementType?
- F-LANG-SPEC-12: wire OutOfRange constant-literal check or remove from spec § 3.10?
- F-LANG-SPEC-13: NonOrderableCollectionExtreme distinct emission or consolidate?

**Status**: Stub — detailed execution plan TBD pending Phase 7 completion and the 3 listed decisions.
**Estimated effort**: M (~3 days — each diagnostic is small, but scenario-coverage matrix completion is broad).

---

# Phase 9: Polish + cleanup

**Goal**: Burn down the remaining P2 polish items. Remove dead code, defensive cleanups, doc cosmetics, deprecated tokens.

**Findings in scope** (~20):
- F-NB-01 (empty BuildDictionaries placeholder — delete or repurpose)
- F-LEX-03 (lexer.md "(~687 lines)" parenthetical — already in Phase 1, but flagged here if not)
- F-PRF-01 (proof-engine.md sample-count reference stale "20 → 64")
- F-LS-03 (2 small LS TODOs)
- All remaining P2 findings from per-stage reviews (defensive-throw cleanup, minor doc fixes, terminology cleanup)
- F-CAT-01 (re-verify Frank's 2026-05-09 catalog audit — produce Fixed/Partial/Persists/Reopened delta for every one of the 27 violations; lift any Persists/Partial to new F-CAT findings)

**Decisions required** (4):
- F-NB-01: delete BuildDictionaries() or repurpose to materialize immutable dictionaries?
- F-LANG-CAT-23: Roslyn rules doc location (Phase 1 may have settled this — re-check)
- F-X-03 cleanup of removed defensive throws (Phase 7 may have settled — re-check)
- Any P2s where the implementer needs guidance.

**Status**: Stub — detailed execution plan TBD pending Phase 8 completion.
**Estimated effort**: M (~3 days — many small items, parallelizable).

---

# Phase 10: Runtime gate verification

**Goal**: Confirm the compiler is production-ready. Open the runtime gate.

**Findings in scope**: None new — this phase is verification only.

**Decisions required**: None.

**Steps** (preview):
1. Full test suite green across all 4 projects, 10 runs without flake.
2. `dotnet build` zero warnings.
3. MCP server smoke-test battery (compile a representative sample of `.precept` files, verify each returns valid JSON).
4. Doc/code reconciliation pass: every Status field on every doc reflects actual implementation state.
5. Catalog discipline final sweep: catalog count tests pass; no parallel keyword lists; no hand-edited `tmLanguage.json`.
6. Diagnostic scenario-coverage matrix is complete (every code has both structural and scenario tests).
7. F-CAT-01 delta confirms zero P0/P1 catalog discipline violations remain.
8. Owner sign-off — runtime gate opens.

**Status**: Stub — detailed verification plan TBD pending Phase 9 completion.
**Estimated effort**: S (~1 day verification work).

---

## Definition of "compiler production-ready" (runtime gate exit criteria)

The compiler is declared production-ready when **all** of the following hold:

- All 98 review findings closed (status updated in this plan and in remediation doc).
- `dotnet test`: 0 failures across all 4 projects.
- `dotnet build`: 0 warnings.
- MCP server: 0 unhandled exceptions on any input.
- Every doc in `docs/language/` and `docs/compiler/` declared "Implemented" matches code at the section level.
- Catalog discipline sweep (F-CAT-01 delta): zero violations.
- Diagnostic scenario-coverage: every `DiagnosticCode` enum member has both structural test (reflection-based, already covered by `DiagnosticCatalogTests`) AND at least one scenario test asserting fire-conditions.
- No `NotImplementedException` in compile-time paths (Runtime/Evaluator.cs Fire/Update/Restore stubs are now the runtime work — not "compile-time").
- No `TODO`/`FIXME`/`HACK`/`XXX` in compile-time paths without a tracked issue link.
- MCP wrappers all ≤ ~30 LOC of non-serialization logic.
- `tmLanguage.json` regenerated and verified not hand-edited.
- All spec § 0.6 obligations documented as implemented in the spec are actually implemented.
- Spec § 0.5 cleanly separates shipped from forward-looking.
- F-API-01 typed descriptor refactor shipped (so runtime builds against the final API shape).
- Owner sign-off: "ready to start runtime work."

---

## Companion artifacts

- [`compiler-readiness-review-2026-05-24.md`](compiler-readiness-review-2026-05-24.md) — the audit (what we found).
- [`compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md`](compiler-readiness-review-2026-05-24-appendices/audit-precept-language-spec.md) — sub-agent: language spec.
- [`compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md`](compiler-readiness-review-2026-05-24-appendices/audit-type-system-docs.md) — sub-agent: primitive/temporal/business-domain.
- [`compiler-readiness-review-2026-05-24-appendices/audit-collections-grammar-docs.md`](compiler-readiness-review-2026-05-24-appendices/audit-collections-grammar-docs.md) — sub-agent: collections + grammar.
- [`compiler-readiness-review-2026-05-24-appendices/audit-catalog-system.md`](compiler-readiness-review-2026-05-24-appendices/audit-catalog-system.md) — sub-agent: catalog system.
- [`bugs.md`](bugs.md) — sample-side bugs (parallel session).

---

## Plan update protocol

When a phase completes:
1. Update the Phase summary table: change Status to "Complete" with date.
2. Update the relevant findings in `compiler-readiness-review-2026-05-24.md` to mark closed.
3. If new findings surfaced during execution, add to the appropriate later phase.

When a Phase 3-10 phase is about to kick off:
1. Promote the stub to a detailed heavyweight section (matching Phase 1-2 shape).
2. Confirm all upstream decisions are settled.
3. Re-verify scope against the current finding state (some items may have been addressed incidentally).
