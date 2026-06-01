# Compiler Readiness Plan — 2026-05-24

**Status**: Active — Phases 1–6 complete; **Phase 7 (total language conformance sweep) active, kicking off 2026-05-30** and planned in detail; Phases 8–11 stubbed pending decisions and upstream-phase completion.
**Companion docs**:
- [`compiler-readiness-review-2026-05-24.md`](compiler-readiness-review-2026-05-24.md) — audit findings (the "what we found")
- This doc — phased execution plan (the "what we'll do")
- [`compiler-readiness-review-2026-05-24-appendices/`](compiler-readiness-review-2026-05-24-appendices/) — full sub-agent reports
**Scope gate**: blocks runtime implementation
**Owner-decision policy**: each phase opens only when its listed upstream decisions are settled. Triage is just-in-time, not all-up-front.
**Sample-edit constraint**: lifted (2026-05-30) — `samples/*.precept` may be edited directly by this plan's workstreams. Historical sample-side bugs are tracked in [`bugs.md`](bugs.md).

---

## Phase summary

| Phase | Goal | F-count | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | Doc foundation truthful + lifecycle skills + 16 Archive promotions | ~55 | 8 (✅ all settled 2026-05-24) | XL (~5-7 days) | ✅ **Complete 2026-05-24** (all 6 workstreams shipped; verification report at [`lifecycle-review-phase-1-2026-05-24.md`](lifecycle-review-phase-1-2026-05-24.md)) |
| 2 | Green baseline + no crashes + Operations.Resolve + MCP-crash family | **12+** | 2 | **L (~4-5 days)** | ✅ **Complete 2026-05-25** (all 7 steps shipped: 2.1–2.7; MCP wrapper backstop + temporal-literal verified clean + LS URI-case fix + Operations.Resolve + generic SyntaxReference test; 6107/6108 Precept.Tests pass with the 1 failure as new BUG-013; 411/411 LS tests pass; 67/67 Mcp tests pass; 291/291 analyzer tests pass) |
| 3 | Type system completeness | ~13 (F-LANG-BIZ-02 dropped → doc-only retire D10 + Frank's case-9 absorbed) | 4 (✅ all settled 2026-05-25, incl. F-LANG-BIZ-02 Position 3 post-research) | **M (~4-6 days)** | ✅ **Complete 2026-05-26** (catalog shape shipped 2026-05-25; runtime enforcement shipped 2026-05-26 in followup commit after `precept-reviewer` philosophy-first re-review surfaced three closures had landed catalog metadata without runtime enforcement — F-LANG-BIZ-06 ExchangeRate Positive, F-LANG-TEMP-03 Duration/Period nonnegative, F-LANG-BIZ-02 Position 3 maxplaces opt-in. Single architectural fix: magnitude-projection helper + `ValidateDefaultAgainstNumericModifiers` + `OutOfRange` (PRE0079) promoted from deferred. 6175/6176 Precept.Tests pass (1 pre-existing BUG-013); 23 new falsifier tests; F-LANG-BIZ-11 filed for Phase 4+ (boundary-precision; renumbered from BIZ-09 due to ID collision).) |
| 4 | Collection completeness + BUG-002 + F-LANG-BIZ-10 | **~16** | 7 (D1-D7 — all settled) | **L+ (~2.5-3 weeks)** | ✅ **Complete 2026-05-26** (all 10 workstreams W-A through W-J shipped across ~42 commits across 2 sessions; 6281/6282 Precept.Tests pass — only pre-existing BUG-013; 411/411 LS, 67/67 MCP, 291/291 analyzer; 8 precept-reviewer rounds caught real BLOCKERs each time; 3 locked design docs via `/lifecycle-2-design` — W-G choice-inner, W-E index-bounds, W-H currency.minorUnit; bugs.md flipped BUG-002, BUG-005, BUG-012, F-LANG-COLL-13 to Fixed; W-H canonical sample `insurance-claim-adjudication.precept` uses `maxplaces currency.minorUnit`. Final phase-close audit (`1e865ac1`) swept 2 stale BUG-002 sample workarounds, 8 transient finding-ID refs, and shipped W-H sample uplift.) |
| 5 | Proof engine satisfiability + BUG-004 + BUG-006 + FieldNeverSet/unification + BIZ operator extensions | **14** | 2 | XL (~3 sessions) | ✅ **Complete 2026-05-27** (all 6 workstreams W-A through W-F shipped across 20 commits; 7136/7136 tests pass across all 4 projects; 2 precept-reviewer rounds + remediations; 3 locked designs via `/lifecycle-2-design` — W-B FieldNeverSet, W-C satisfiability cluster, W-D BIZ operator extensions. **Workstream summary**: W-A BUG-004 event-ensure narrowing (`034a5976`); W-B F-LANG-GRAPH-04 FieldNeverSet + writable→editable unification — 8 slices, 4 commits, 27 sample fixes (`0d61f792` `12d7c422` `0feb135a` `2ac416b5`); W-C satisfiability cluster — SPEC-02 UnsatisfiableGuard wire-up + SPEC-03 ContradictoryRule + SPEC-04 VacuousRule + SPEC-05 TautologicalGuard + SPEC-12 UnreachableRowFact + BUG-006 cross-row interval composition (`a6c35e36` `ffb7288b` `c16be77b` `42cee783`); W-D BIZ operator extensions — BIZ-01 `money / price → quantity` + BIZ-05 `DimensionalProductProofRequirement` + PRE0157 + BIZ-08 discrete equality narrowing (`5d945711` `e71e09d2` `08fae0ef` `fdf33095`); W-E F-LANG-TEMP-04 always-false period comparison (`27dfe325`); W-F BUG-013 sample fixture restore (`766637c0`). Phase-close audit (`34d11e61` + `ed1f4193`) addressed 1 BLOCKER + 5 CONCERNs + 2 NITs from the reviewer punch list; pre-existing proof-engine.md strategy-chain doc gap closed in (`6a35369c`). 6 new diagnostic codes (PRE0153–PRE0158); 1 new ProofRequirementKind (DimensionalProduct); 1 new ProofForwardingFact variant (UnreachableRowFact); 1 new ResultQualifierPolicy (InheritPriceDenominatorUnit); 1 new ActionMeta property (WriteSemantics); 1 new ProofStrategy (DimensionalProduct); 1 retired ModifierKind (Writable) + TokenKind; NumericInterval gains Empty/Intersect/Difference; Integer arithmetic ops gain IntervalTransfer functions. Five §0.6 obligations move from Specification-only to Implemented.) |
| Phase 5 post-review remediation | 15 code-review findings + 3 surfaced soundness defects + cleanup | 22 | 2 (Slice 2 qualifier + dimensionless-product; Slice 4 satisfiability-attribution + reachability) | M (~1 session) | ✅ **Complete 2026-05-28** (commit `11599944` + sample-restore `332f76ac` + post-regression Slices 6–7 still local). Extra-high-effort `/code-review` on the Phase 5 spike branch surfaced 15 findings; remediated across Slice 1 (1a/1b/1c/1d direct bug fixes), Slice 3 (`PriceDenominatorInherited` `QualifierBinding` subtype wired across 5 consumer sites + PRE0137 lift from `!opComposesDimensions` gate), and Slice 5 (PRE0159 `UnsatisfiableRule` pre-pass + reachability-gated `FieldNeverSet`). Two locked designs via `/lifecycle-2-design`, now archived in `docs/Working/Archive/`. Three precept-reviewer audits cleared with remediations. A regression `/code-review` pass against the cumulative diff confirmed 0 of 15 originals survived and surfaced 3 NEW soundness defects (`BuildSiblingRejectExclusions` source-order + wildcard-row gate; `ScanRules` pair-sweep ignoring `when` guards) — fixed in Slice 6 with the guard mutual-exclusion pre-check and the source-order/wildcard checks. Slice 7 collapsed four duplicate fold-constraint sites onto a shared `FoldConstraintsInto` helper, removed dead code, cleaned doc-comment drift, downgraded `bugs.md` BUG-006 scope notes (literal-comparison sibling rejects only), and swept 6 stale narrative `# BUG-NNN` refs in samples. **7167/7167 tests pass** (was 7136 at Phase 5 close; +31 net new tests across the remediation slices). Two designs archived; `bugs.md` updated. |
| 6 | composite period basis (F-LANG-BIZ-07) | 1 (F-LANG-BIZ-09 closed) | 2 (D6.1 sample? D6.2 runtime-lowering deferral — both resolved in the plan) | M (~2.5–3.5d) | ✅ **Complete 2026-05-30.** All 4 workstreams shipped (W-A/W-B prior; W-C `fd147dc2`/`a42134e3`/`d2946dc0`; W-D scenario-test matrix + lease sample `31faa2ba` + single-basis whitespace-trim fix `02a1e44f`). Composite period basis (`period in 'hours + minutes'`) parses/validates/canonicalizes; `.basis`/`.dimension` accessors return composite/derived values; composite bases do not cancel single-unit denominators (D15); legal-basis-by-operation + D14 subset enforced. Full suite green (Precept.Tests 6597 + LS/MCP/Analyzers). Runtime `Period.Between` lowering deferred to the runtime phase (D6.2). Spec-conformance gaps surfaced during W-D (price×quantity resolver, exchangerate slash, PRE0073 — all *outside* composite-period scope) tracked in `spec-conformance-audit-2026-05-30.md` for a remediation phase. |
| 7 | **Total language conformance sweep** — every spec/catalog construct, type, operator, modifier, and diagnostic probed against the implementation; every confirmed spec↔impl gap fixed | open-ended / exploration-driven (audit is one seed — A1–A4 / B1–B9 / C1–C4 / D1–D4 from `spec-conformance-audit-2026-05-30.md`; also commissioned audits, probing, conformance tests; count grows as discovery continues) | per-row owner rulings (spec-internal contradictions; A1↔C1 shared-root grouping; Phase 9 diagnostic-completeness seam) | XL | **Active — kicking off 2026-05-30** |
| 8 | **Diagnostic-emission architecture** (DA-1 dedicated emission phase / DA-2 regularization audit / DA-3 counterexample witnesses — from the 2026-05-29 type↔proof-contract survey) | 3 (DA) | 2 (DA) | M→L | **Active — taken up 2026-05-31** |
| 9 | Diagnostic completeness (every declared `DiagnosticCode` emits-or-retires; bidirectional CI) | ~7 | 3 | M | Stub — TBD |
| 10 | API surface solidity (typed descriptors) | ~6 | 1 | M-L | Stub — TBD |
| 11 | Polish + cleanup + `/lifecycle-7-audit` skill | ~20 | 4 | M | Stub — TBD |
| 12 | Runtime gate verification | — | 0 | S | Stub — TBD |

**Overall estimate**: 7-11 weeks of focused work (was 6-10; Phase 2 grew). Phase 1 includes (a) 5 lifecycle skill builds + rename, (b) 16 Archive promotion obligations, (c) CONTRIBUTING.md lifecycle updates. Phase 2 grew from ~2-3 days to ~4-5 days after integrating 10 active bugs from `bugs.md` (sample-remediation work, 2026-05-24): 6 MCP-crash family bugs (BUG-003 period, -005 symptom, -007 domains, -008 duration, -010 now()+duration, -011 timezone+time), 1 MCP transport bug (BUG-009 payload limit), plus the original F-LANG-SPEC-10. Coordinated MCP-layer instrumentation pass catches the whole family in one fix. Phase 5 (proof engine satisfiability) remains the highest variance. **Phase 7 (total language conformance sweep, inserted 2026-05-30) is open-ended by design** — it runs slice after slice until the owner is satisfied the compiler is fully implemented and accurate to the spec; its size is bounded by what the probe matrix surfaces, not a week estimate, so the overall total is no longer a meaningful single number while Phase 7 is active.

**`bugs.md` as ongoing source**: sample-authoring work discovers bugs that surface in real authoring workflows (not in code-vs-doc audits). Plan integrates bugs.md as a standing input. Phase-kickoff protocol includes "re-read bugs.md for new entries since last integration."

**Bug-to-phase coverage map** (as of 2026-05-25, after Phase 2 commit `38712543`):

| Bug | Status | Phase | Rationale |
|---|---|---|---|
| BUG-001 | ✅ Fixed | (earlier) | Proof engine narrowing — shipped pre-plan |
| BUG-002 | ✅ Fixed | 4 (W-B) | Lookup `remove` key dispatch — collection completeness |
| BUG-003 | ✅ Fixed | 2 | Period typed-constant default crash |
| BUG-004 | Active | 5 | Proof engine event-ensure body narrowing — same family as BUG-001 |
| BUG-005 | ✅ Fixed | 2 symptom + 4 root (W-C) | Qualified inner types in lookup — full support shipped via F-LANG-COLL-06 |
| BUG-006 | Active | 5 | Proof engine guard + field-`max` interval composition |
| BUG-007 | ✅ Fixed | 2 | `precept_domains` MCP crash |
| BUG-008 | ✅ Fixed | 2 | Duration typed-constant default crash |
| BUG-009 | ✅ Fixed | 2 | MCP payload-size — in-process verified clean; wire-level wrapper backstop |
| BUG-010 | ✅ Fixed (crash) / open (type-inference) | 2 crash + 4 type-inference | `now() + '<duration>'` — structured diagnostic; full inference fix Phase 4+ |
| BUG-011 | ✅ Fixed | 2 | Timezone / time typed-constant default crash |
| BUG-012 | ✅ Fixed | 4 (W-G; pulled forward from Phase 5) | Ordered-choice + literal proof gap — typed-literal inference in `TryDeclarationAttributeProof` lifts modifier from binary-op sibling |
| BUG-013 | Active | n/a | `samples/Test.precept` missing; the test references a sample that was never carried forward. **Triage**: either re-add the fixture or rewrite the test inline — the test-side fix is trivial. |

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
- **Settled by**: commit `090764d3` (2026-05-25).

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

- **Decision**: **Position 3 — decoupled at the default surface, with explicit `maxplaces N` as the opt-in strict-mode.** `money in '<Cur>'` carries NO implicit `maxplaces` constraint derived from ISO 4217 minor units. Authors who want Joda-Money-style strictness write `money in 'USD' maxplaces 2` explicitly. The Phase-3-original "synthesize implicit Maxplaces N at modifier resolution" work is dropped entirely. What lands in Phase 3 is the doc-only retirement of D10 from `business-domain-types.md` + a new follow-up finding (F-LANG-BIZ-11, originally filed as F-LANG-BIZ-09 but renumbered 2026-05-26 due to ID collision with Phase 6 entity-scoped `units` block work) for boundary-precision enforcement at persistence + `transition apply` + external integration.
- **Rationale**: External research (`research/architecture/compiler/currency-precision-coupling-survey.md`) surveyed Joda-Money, JSR-354, NodaMoney, Stripe/Square/Adyen, COBOL, IFRS IAS 21, US GAAP ASC 830, SQL conventions. Standards bodies (IAS 21, ASC 830) are silent on currency-derived precision — they specify rounding *behavior*, not type-level *coupling*. JSR-354 deliberately decoupled currency identity from value precision after the Joda-Money authors and Java community surveyed the design space. NodaMoney's silent auto-round violates Precept's "honesty about approximation" principle. Intermediate calculations (tax = `1.50 USD * 0.0725` = `0.108750 USD`) genuinely need sub-cent precision that an implicit `maxplaces` would reject; the boundary is where precision must be enforced, not the type system.
- **Alternatives considered and rejected**:
  - **Position 1 — Structural constraint (D10 as designed)**: Implicit `maxplaces 2` on `money in 'USD'`. Rejected: contradicts industry practice, blocks legitimate intermediate-precision use cases, makes `money in '<Cur>'` non-uniform with `quantity of '<unit>'` (which has no implicit precision).
  - **Position 2 — Soft default with silent override**: Implicit `maxplaces N` that any explicit override silently replaces. Rejected: hides the decision at the type level, surprises authors reading code that doesn't show what precision is in effect.
- **Precedent**: JSR-354 (the post-Joda-Money Java Money standardization effort) explicitly decoupled currency identity from value precision. Joda-Money offers strict (`Money`) and lenient (`BigMoney`) variants — strictness is opt-in, not default. Stripe/Square/Adyen enforce minor-unit precision at the wire boundary (request/response validation), not in the application's type system. Half-even rounding is a behavioral default in IFRS/GAAP, not a type-level constraint.
- **Tradeoff accepted**: Authors who want strict per-currency precision must opt in via explicit `maxplaces N`. The sample-corpus tidy (one canonical sample + tutorial walkthrough showing the opt-in idiom) makes the pattern discoverable. **Philosophy-tradeoff to surface to owner**: Position 3 partially relaxes "Prevention, not detection" at the type-system level — D10's implicit constraint WAS a prevention mechanism. Under Position 3, prevention RELOCATES to the wire boundary (Phase 4+ via F-LANG-BIZ-11) where authors cross into external state. Prevention as a principle is preserved; its enforcement point shifts.
- **Doc actions for Phase 3 execution**:
  - `docs/language/business-domain-types.md` — retire D10 (13 hits including the full D10 § at lines 1718-1721 and Corollary 2 at line 1824 which references D10 by analogy)
  - File F-LANG-BIZ-11 (boundary-precision enforcement) for Phase 4+
  - Lift research artifact to `research/architecture/compiler/currency-precision-coupling-survey.md`
  - Sample-corpus tidy: one canonical sample uses explicit `maxplaces 2` idiom
- **Settled by**: Conversation 2026-05-25 + external research via `/lifecycle-1-research` skill mid-planning. Recorded in heavyweight Phase 3 plan (commit follows).

### Still open — gating Phase 4+

- ~~F-LANG-SPEC-01~~ — ✅ shipped in Phase 4 W-F as AI-slop cleanup (Principle 9 is locked spec text; the optional slot was a Copilot-co-authored departure that no author ever exercised). Detail: D5 in Phase 4 § Decisions captured.
- **F-LANG-BIZ-11** *(new finding, filed 2026-05-25 from F-LANG-BIZ-02 Position 3 spinoff; originally filed as F-LANG-BIZ-09 but renumbered 2026-05-26 due to ID collision with the Phase 6 entity-scoped `units` block work)*: **Boundary-precision enforcement for money values at persistence + `transition apply` + external integration**. Under Position 3, `money in 'USD'` no longer carries an implicit `maxplaces 2` at the type system; the prevention guarantee for currency-derived precision must therefore relocate to the wire/persistence boundary. Stripe/Square/Adyen all enforce per-currency minor-unit precision at the API boundary (per the survey `research/architecture/compiler/currency-precision-coupling-survey.md`); Precept should provide a structural mechanism that enforces precision rules at boundaries where money values cross between Precept-governed and external state. **Scope**: design pass needed to define the boundary surface (`transition apply`? persistence layer? both?), the API for declaring per-field boundary-precision rules, and the diagnostic surface for boundary violations. **Cross-link**: research artifact's Open Question #1. **Target phase**: 4 or 5, owner picks during triage; non-blocking for either phase's existing scope.
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
- Apply `docs/Working/Archive/contributing-updates-draft.md` per its embedded "Implementation notes for Phase 1" (archived 2026-05-25 — content has shipped into `CONTRIBUTING.md`)
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
- Sample-edit constraint: lifted (2026-05-30) — samples may be edited directly; historical sample-side bugs tracked in `bugs.md`.
- Workstream A.2 (`/lifecycle-5-promote`) is the critical-path dependency for Workstream E. Build A.2 first; B/C/D can run in parallel after.
- All 8 Phase-1 gating decisions are settled (see plan-level "Decisions captured so far"). No pre-execution triage required.

## Exit criteria
- [ ] All 8 Phase-1 decisions recorded in this doc's § "Decisions captured" and in the remediation doc § 1a.
- [ ] `catalog-system.md` count claims match `grep -c` of corresponding `*Kind.cs` files (Tokens, Types, Operators, Functions, Actions, Modifiers, Constructs, ConstructSlots, ExpressionForms, Constraints, ProofRequirements, Outcomes, Diagnostics, Faults — 14 catalogs).
- [ ] Every metadata-record shape claim in `catalog-system.md` matches the actual C# record definition (`Token.cs`, `Type.cs`, etc.).
- [ ] No "✅ Resolved" claim in `catalog-system.md` references code that doesn't exist (revert any that the owner decided not to implement; implement the rest in Phase 10 as part of API solidity work).
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
- **F-X-01**: delete `F5TempVerify.cs` outright (`SampleFieldStateRegressionTests.cs` already covers samples) OR promote to permanent `SampleCompilesCleanTests.cs` (drop the "TEMPORARY" docstring, update file count from "30" to "all `samples/*.precept`", remove dev-only language)?

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
- **F-X-01**: delete `F5TempVerify.cs` outright (`SampleFieldStateRegressionTests.cs` already covers samples) OR promote to permanent `SampleCompilesCleanTests.cs` (drop the "TEMPORARY" docstring, update file count from "30" to "all `samples/*.precept`", remove dev-only language)?

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

**Status**: ✅ **Complete 2026-05-26.** Catalog shape shipped 2026-05-25 (e07a084e + 572d16a6 + 6b2d1040); runtime enforcement shipped 2026-05-26 in followup commit. The `precept-reviewer` philosophy-first re-review on 2026-05-25 flagged that three closures (F-LANG-BIZ-06 ExchangeRate `ImpliedModifiers: [Positive]`, F-LANG-TEMP-03 Duration/Period in `ZeroBoundNumericTypes`, F-LANG-BIZ-02 Position 3 explicit-maxplaces opt-in) had landed as catalog metadata without runtime enforcement — the modifier-validation and proof-engine infrastructure operated on bare `decimal` and could not see into business-magnitude/temporal typed values. Single architectural fix: new `TypedExpressionMagnitude` helper projects magnitude from any TypedExpression (covering Money/Quantity/Price/ExchangeRate ValueTuples + NodaTime Duration ticks + NodaTime Period sign-projection); new `ValidateDefaultAgainstNumericModifiers` enforces declared and implied numeric modifiers against the resolved default; `OutOfRange` (PRE0079) promoted from deferred with multi-arg message template. 6175/6176 Precept.Tests pass (1 pre-existing BUG-013); 23 new falsifier + regression-guard tests in `DefaultValueModifierEnforcementTests.cs`. F-LANG-BIZ-11 filed for Phase 4+ boundary-precision (renumbered from F-LANG-BIZ-09 due to ID collision with Phase 6 entity-scoped `units` block work). Two sample bugs surfaced and fixed under sample-edit exception (`loan-application.precept` zero-default-vs-positive; `currency-exchange-rates.precept` redundant explicit `positive` on `exchangerate`).

## Findings in scope (~13)

**Decision executions (3):**
- F-LANG-PRIM-04 (doc clarification — error stays)
- F-LANG-TEMP-08 (remove `OperationKind.ZonedDateTimePlusPeriod` + `MinusPeriod` from catalog)
- F-LANG-PRIM-01 (already shipped; Phase 3 verifies)

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
- **3.1c F-LANG-PRIM-01 ratification**: verification only — confirm the work shipped at `docs/language/primitive-types.md § String Ordering — Out of Scope` and `precept-language-spec.md § 3.6`.

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
- [ ] `dotnet test --no-build` 0 failures across all 4 projects (except BUG-013); 3× run, no flakes
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
- [x] F-LANG-BIZ-11 (boundary-precision) filed
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
| 3.3b | `business-domain-types.md` (retire D10 13 sites + Corollary 2 rewrite); F-LANG-BIZ-11 finding; new research artifact |
| 3.3c | `business-domain-types.md` § Currency accessors + § Interpolation-slot resolution, `compiler/type-checker.md` |
| 3.3d | `business-domain-types.md` § ExchangeRate-implicit-positive |
| 3.3e/3.3f | `business-domain-types.md` or `bugs.md` (verify-then-file) |
| 3.4 | `catalog-system.md` § ContentValidation DU shape, `compiler/type-checker.md` (catalog discipline restored) |
| 3.5 | this plan (tracker), `bugs.md` (BUG-010 status) |

## Discovered during planning

1. **Frank's case-9 currency-member-access interpolation gap** absorbed into Step 3.3c (was case 9 of `frank-price-qualifier-full-analysis.md`). Adding currency accessors without extending interpolation-slot resolution would compound the silent gap. Step 3.3c grew ¼ day → 1-1.5 days; mirrors existing unit-slot resolution pattern.
2. **F-LANG-BIZ-02 implicit-precision premise was wrong** — dropped via external research (`/lifecycle-1-research` skill, full survey in research artifact). Position 3 ratified; D10 retires doc-only; F-LANG-BIZ-11 filed for Phase 4+ boundary enforcement. Phase 3 effort drops L→M.
3. **F-LANG-BIZ-08 narrowing is design-required for Phase 5** — confirmed proof engine has no choice-equality narrowing strategy today. Step 3.3f verify-then-file rather than absorbing into Phase 3.
4. **5 additional research follow-ups** from the currency-precision survey's Open Questions (multi-currency arithmetic safety, hyperinflationary drift, core-banking comparator gap, crypto, opt-in discoverability) — to be filed alongside the F-LANG-BIZ-11 lift.

Planning artifact: `/home/sfalik/.claude/plans/refactored-yawning-fern.md` (heavyweight Phase 3 plan, reviewer-audited 2026-05-25).

---

# Phase 4: Collection completeness + BUG-002 + F-LANG-BIZ-10 — ✅ Complete 2026-05-26

**Goal**: Every documented capability of the 9 collection types works as specified. The catalog's action-applicability metadata is actually enforced. Two-field quantifier bindings for ordered collections work. Qualified inner types parse. The grammar doc's vocabulary matches code. Plus: BUG-002 (lookup-remove key dispatch) and F-LANG-BIZ-10 (currency-derived `maxplaces`).

**Status**: ✅ Complete 2026-05-26. All 10 workstreams (W-A through W-J) shipped across ~42 commits across 2 sessions. 6281/6282 Precept.Tests (only pre-existing BUG-013); 411/411 LS, 67/67 MCP, 291/291 analyzer. 8 precept-reviewer rounds caught real BLOCKERs each time. 3 locked design docs went through `/lifecycle-2-design`.

**Findings closed** (16 + bundled bugs):
- F-LANG-COLL-02 (choice inner in collections) — shipped W-G as feature (was scoped as targeted diagnostic; promoted)
- F-LANG-COLL-03 (ordered-choice trait propagation) — shipped W-G; W-G remediation added D-3 Option A (`TypedMemberAccess.ChoiceMetadata` slot) per reviewer
- F-LANG-COLL-04 (.at(N) index-bounds proof obligation) — shipped W-E via `IndexBoundsProofRequirement(StrictlyBefore)`
- F-LANG-COLL-05 (log-by append uniqueness) — shipped W-E early via existing `KeyPresenceProofRequirement(RequireAbsence)`
- F-LANG-COLL-06 (qualified inner types) — shipped W-C; root-cause fix for BUG-005; new `TypedElementType` DU
- F-LANG-COLL-07 (queue-by/log-by quantifier `.value`/`.by`) — shipped W-D
- F-LANG-COLL-08 (action ApplicableTo enforcement) — shipped W-A; PRE0047/PRE0048 emission wired
- F-LANG-COLL-09 (insert/remove-at index-bounds) — shipped W-E; `IndexBoundsMode.AtOrBefore` for insert
- F-LANG-COLL-10 (`notempty` on lookup) — shipped W-F (doc-only) then lifted as feature in W-J
- F-LANG-COLL-11 (MissingOrderingKey rename) — shipped W-F: PRE0104 renamed to `RequiredTraitViolation`; new PRE0151 reserved for missing-`by`
- F-LANG-COLL-12 (Countof/Peekby tokens) — **closed as audit error** in W-F: tokens are live keyword tokens (Types.cs:245,281); no code change
- F-LANG-COLL-13 (clear + notempty lift on lookup) — shipped W-J after `/lifecycle-2-design` survey of comparator languages (Java/C#/Python/Rust/Swift/Kotlin/F#/Go all allow bulk clear)
- F-LANG-GRAM-01/02/03/04/05 + InitialEvent rename — shipped W-I after `/lifecycle-2-design` taxonomy reorganization
- F-LANG-CAT-08 (ProofRequirementKind catalog completeness) — shipped W-E: catalog count now 11 (IndexBounds = 11)
- F-LANG-SPEC-01 (mandatory `because`) — shipped W-F as cleanup (folded in mid-phase; was originally framed as "enforce or amend Principle 9" but investigation showed Principle 9 is locked spec text and the optional slot was an AI-co-authored departure)
- F-LANG-BIZ-10 (currency-derived `maxplaces currency.minorUnit`) — shipped W-H after `/lifecycle-2-design` + two precept-reviewer rounds; F-UP-BIZ-10-B (`UseInModifierValueContext` catalog flag) landed early per reviewer
- **BUG-002** — shipped W-B: new `RemoveByKey` ActionSyntaxShape; lookup-remove dispatches on key type
- **BUG-005** — fully closed by W-C's F-LANG-COLL-06 ship (was symptom-fixed in Phase 2)
- **BUG-012** — pulled forward from Phase 5 into W-G: typed-literal inference in `TryDeclarationAttributeProof` lifts modifier from binary-op sibling

**Decisions captured** (D1-D7, all settled):
- **D1** — Bundle collection proof obligations (COLL-04/05/09) into Phase 4 alongside the features. **Settled**: bundle (the obligations are the safety story for the features).
- **D2** — F-LANG-COLL-11 PRE0104 rename. **Settled**: rename + fresh code (PRE0151 reserved for missing-`by`).
- **D3** — F-LANG-COLL-12. **Settled**: closed as audit error; no code change.
- **D4** — F-LANG-BIZ-10 inclusion in Phase 4 W-H. **Settled**: include, gated on `/lifecycle-2-design` + precept-reviewer pass.
- **D5** — F-LANG-SPEC-01 (`because` on ensures). **Settled**: remove the optional slot (AI-slop cleanup; Principle 9 is locked spec text).
- **D6** — Catalog-strict `Add` (no widening to Log/List). **Settled**: catalog-strict; sample/test cleanup migrated to `append` verb.
- **D7** — Revert Lookup from `ClearApplicable` (W-A initial patch was a spec violation). **Settled**: revert; W-J then lifts via proper `/lifecycle-2-design` pass.

**Designs locked via `/lifecycle-2-design`**:
- `docs/Working/choice-inner-and-ordered-propagation-design.md` (W-G — F-LANG-COLL-02/03 + BUG-012)
- `docs/Working/index-bounds-proof-design.md` (W-E — F-LANG-COLL-04/09; pulls forward W-E's structural reshape that emerged mid-phase)
- `docs/Working/f-lang-biz-10-currency-derived-maxplaces.md` (W-H — currency-derived `maxplaces`)
- `docs/Working/clear-on-lookup-design.md` (W-J)
- `docs/Working/construct-kind-taxonomy-design.md` (W-I)

**Workstream commits** (in execution order):
- `392b9a89` — W-C (qualified inner types, root-cause for BUG-005)
- `cbd4564c` — W-J (clear + notempty lift on lookup)
- `54954e4d` — W-I (ConstructKind taxonomy)
- `6ce7d584` — W-D (queue-by quantifier binding)
- `844b10b4` — W-G (choice inner + ordered + BUG-012); `11b020a9` — W-G remediation (D-1 reshape + D-3 Option A + grammar sync)
- W-A + W-B + W-F (action applicability + BUG-002 + hygiene — earlier session)
- `44739a8b` — W-E F-LANG-COLL-05 + IndexBounds scaffolding
- `d91ac091` — W-E design lock (after `/lifecycle-2-design` proper pass replaced an earlier ungrounded draft)
- `12c670ac` — W-E slice 1 (Arguments slot on TypedMemberAccess); `c34359b1` — W-E slices 2-6 (catalog parameters, guard extension, discharge strategy, catalog wiring, diagnostic refinement); `a91ed96c` — W-E remediation (catalog-driven action dispatch)
- `b5107e09` — W-H design remediation (verbatim citations + meta-pattern falsifier + early catalog-flag landing); `df5c4cff` — W-H implementation
- `1e865ac1` — Phase 4 close-out audit remediation (BUG-002 sample sweep + transient ref scrub + W-H sample uplift)

**Effort**: 2.5-3 weeks actual (was originally estimated L ~1-1.5 weeks; grew to L+ when proof obligations bundled, then to ~2.5-3w when W-H added + 8 reviewer rounds added remediation passes).

**Calibration for Phase 5** (from the phase-close audit):
- BUG repros that name "field-vs-literal" vs "field-vs-field" as separate symptoms ARE separate fixes from the start. W-G's BUG-012 fix had to ship the literal-side strategy alongside the F-LANG-COLL-03 accessor work; treating them as one decision held.
- precept-reviewer + `/lifecycle-2-design` cadence works. 8 rounds caught real BLOCKERs each time; treating it as overhead rather than discipline would have produced shippable-looking work that failed close-out audit.

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
- ~~**BUG-012**~~ — **pulled forward into Phase 4 W-G** (2026-05-26). Shipped as typed-literal inference in `TryDeclarationAttributeProof`'s Modifier arm: when the obligation's subject resolves to a literal/typed-constant operand of a binary op, lift the modifier from the contextual sibling operand. `Severity <= 2` and `Tier <= "Low"` now prove cleanly; unordered choices still emit PRE0112; field-vs-field path unchanged. Sample restore: `samples/it-helpdesk-ticket.precept` reverted from equality-cascade to canonical ordinal form. Tests in `test/Precept.Tests/ProofEngine/OrderedChoiceLiteralTests.cs` (7).
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

# Phase 6: composite period basis (F-LANG-BIZ-07)

**Status**: ✅ Complete 2026-05-30 — all 4 workstreams (W-A/W-B prior; W-C `fd147dc2`/`a42134e3`/`d2946dc0`; W-D `31faa2ba`/`02a1e44f`). Full suite green. Runtime lowering deferred (D6.2).

**Goal**: A `period` field or event arg can declare a composite basis with `+`-separated components (`period in 'hours + minutes'`), the type checker validates and canonicalizes it, the `.basis`/`.dimension` accessors return the composite/derived values, and composite bases correctly do *not* cancel single-unit time denominators — all at compile time.

**Companion docs**:
- `docs/language/business-domain-types.md` § D4 + § Period basis (the locked amendment, commit `03dfc4da`) — the design.
- `research/language/expressiveness/period-basis-separator-survey.md` — grounds the `+` separator.
- `research/language/expressiveness/literal-whitespace-consistency-survey.md` — grounds lenient-whitespace + spaced-canonical.

**Findings in scope** (1): F-LANG-BIZ-07 (composite period basis). F-LANG-BIZ-09 already closed (§ D6) — this completes Phase 6.

## Decisions captured (locked in the D4 amendment, 2026-05-28)

- **Separator**: `+` (not `&`) — matches the value-literal combiner; one operator for temporal composition.
- **Whitespace**: lenient input (`'hours+minutes'` ≡ `'hours + minutes'`); canonical form is **spaced**.
- **Canonicalization**: coarse-to-fine component order (years→months→weeks→days→hours→minutes→seconds); any input order accepted, silently normalized.
- **Open-period `.basis`**: returns the runtime decomposition basis with zero-valued components omitted.
- **D14 composite extension**: a value is accepted iff its non-zero component set ⊆ the declared basis.
- **Composite legality by source operation**: every atom must be legal for the operation; illegal atoms emit `QualifierMismatch` per atom.
- **`of` rejects composite syntax** (dimension-class atoms only).
- **Interpolated composite bases deferred** (literal-only at first ship).
- **Three malformed-basis diagnostics reserved**: duplicate / unknown / empty component.

## Open decisions

- **D6.1 — Sample exercising composite basis?** No current sample uses composite basis. **Options**: (a) ship the feature with scenario-test coverage only, no sample; (b) add a sample (e.g., a billing/SLA precept using `period in 'years + months'`). **Recommended**: (a) — the feature is fully exercised by scenario tests. A sample can follow as a clean add. **Lands**: W-D kickoff.
- **D6.2 — Runtime `Period.Between` lowering**: out of scope this phase. **Decided**: the runtime evaluator is still stub (no `Period.Between` call exists in `src/`); composite-basis runtime lowering lands with the runtime phase (Phase 12+). This phase delivers the compile-time surface only — parsing, validation, canonicalization, accessors, cancellation discipline. Noted here so it is not silently dropped.

## Workstream blocks

| WS | Goal | Effort | Depends on | Status |
|---|---|---|---|---|
| W-A | Composite representation + parse/validate/canonicalize + 3 diagnostics | M (~1–1.5d) | — | (prior) |
| W-B | `.basis` / `.dimension` accessor resolution for composite | S (~½d) | W-A | (prior) |
| W-C | Composite-aware cancellation + legal-basis-by-operation enforcement | M (~1d) | W-A | ✅ **Complete** (commits `fd147dc2` `a42134e3` `7dd07f8b` `d2946dc0`) |
| W-D | Scenario-test matrix + doc-status verification | S (~½d) | W-B, W-C | ✅ **Complete** (51-fact composite test matrix; lease sample `31faa2ba`; NIT-3 single-basis whitespace-trim fix + NIT-4 exclusivity hardening `02a1e44f`; `&`-separator gone; composite compiles clean) |

**Total**: ~2.5–3.5 days serial; ~2–3 if W-B and W-C parallelize after W-A.

### Workstream W-A — Composite representation, parse, validate, canonicalize

**Goal**: `MapTemporalUnitQualifier` accepts and canonicalizes composite bases; malformed bases emit the three new diagnostics.

**Steps**:
0. **Close the pre-existing `PeriodDimension.Datetime` gap** (specced `(new)` in `business-domain-types.md` but never built). Add `Datetime` to the `PeriodDimension` enum (`src/Precept/Language/ProofRequirement.cs:66`). Wire it as a **return/comparison value only** — NOT as an `of`-acceptable input:
   - `.dimension` accessor return + the combined-dimension computation in step 2 produce `Datetime` for both-spanning periods.
   - `ExtractComparableValue` / proof-marker arms (`src/Precept/Pipeline/ProofEngine.Qualifiers.cs:247`) map `Datetime → "datetime"` so `when X.dimension == 'datetime'` narrows.
   - **Do NOT** add a `"datetime"` arm to `MapTemporalDimensionQualifier` (`TypeChecker.cs:377`): `period of 'datetime'` stays a `QualifierMismatch` (vacuous constraint — admits all components, proves nothing). The current rejection is correct and unchanged.
1. Extend `DeclaredQualifierMeta.TemporalUnit` (`src/Precept/Language/DeclaredQualifierMeta.cs:75`) to carry an ordered component set (e.g., `ImmutableArray<string> Components` + the existing `UnitName` retained as the canonical joined string for back-compat with single-basis consumers, or `UnitName` becomes the canonical composite string). Single-component stays a one-element set.
2. Rewrite `MapTemporalUnitQualifier` (`src/Precept/Pipeline/TypeChecker.cs:393`): split the value on `+` with whitespace trimming per component; for each component call `TemporalUnits.TryGet`; detect duplicate (PRE0160), unknown atom (PRE0161), empty segment (PRE0162); canonicalize to coarse-to-fine order; compute combined `PeriodDimension` (date-only → Date, time-only → Time, mixed → Datetime).
3. Add `DuplicateCompositeBasisComponent`, `UnknownCompositeBasisComponent`, `EmptyCompositeBasisComponent` to `DiagnosticCode.cs` (next free: PRE0160–0162); add factory entries to `Diagnostics.cs` with `FixHint`/`TriggerCondition`/`RecoverySteps`/`ExampleBefore`/`ExampleAfter`; wire emission at the `MapTemporalUnitQualifier` sites; register in `DiagnosticCoverageAllowLists.cs` only if any path lacks an emission site (expectation: all three emit, so no allow-list entry).
4. Confirm the parser passes the raw quoted qualifier string unmodified (expected: yes — single-basis already passes `"months"` whole; no `Parser.Types.cs` change anticipated). If the parser pre-tokenizes on `+`, that's a discovered gap — handle in this WS.

**Exit criteria**:
- `precept_compile` on `period in 'hours + minutes'`, `'hours+minutes'`, `'minutes + hours'` (non-canonical) all succeed; `.basis` resolves to canonical `'hours + minutes'`.
- `period in 'hours + hours'` emits PRE0160; `period in 'years + fortnights'` emits PRE0161; `period in 'years +'` emits PRE0162.
- `dotnet test` green; `Precept0027DiagnosticEmissionCoverage` analyzer clean (no missing-emission or stale-allow-list warnings for the 3 new codes).

**Doc-update obligations**: `docs/compiler/diagnostic-system.md` (3 new codes); `docs/language/catalog-system.md` (diagnostic count bump); `docs/compiler/type-checker.md` if the `DeclaredQualifierMeta.TemporalUnit` shape change is documented there.

### Workstream W-B — Accessor resolution

**Goal**: `.basis` returns the canonical spaced composite string; `.dimension` returns `'datetime'` for date+time-spanning composites.

**Steps**:
1. `.basis` accessor (`src/Precept/Language/Types.cs:480`, `FixedReturnAccessor("basis", …)`): ensure the resolved value is the canonical composite string. The proof-marker mapping (`src/Precept/Pipeline/ProofEngine.Qualifiers.cs:198,244` — `TemporalUnit { UnitName: var value } => value`) must surface the canonical composite spelling for `$eq:X.basis:…` markers.
2. `.dimension` accessor (`Types.cs:481`): return `Datetime` when the composite spans both date and time atoms (uses the combined `PeriodDimension` computed in W-A).

**Exit criteria**:
- `when X.basis == 'hours + minutes'` narrows correctly (proof marker matches canonical).
- `.dimension` on `period in 'days + hours'` returns `'datetime'`; on `period in 'years + months'` returns `'date'`.
- `dotnet test` green.

**Doc-update obligations**: none beyond the already-amended `business-domain-types.md` accessor rows (verify they match implementation).

### Workstream W-C — Composite-aware cancellation + legal-basis enforcement

**Goal**: composite bases do not cancel single-unit denominators (D15), and illegal atoms / non-subset assignments emit `QualifierMismatch`.

**Steps**:
1. Single-basis-assuming consumers must treat composite as "not single-basis": `src/Precept/Pipeline/TypeChecker.Expressions.cs:1322–1352` (period cancellation against price denominators) — a composite period must not cancel `price in 'USD/hours'`; emit/retain `CompoundPeriodDenominator`. Audit `ProofEngine.cs:602`, `ProofEngine.Qualifiers.cs:198/244`, `ProofEngine.Strategies.cs:236` for `UnitName` single-atom assumptions.
   - **Carry-forward from W-A review (NIT-1)**: PRE0074's trigger is `rightTemporalDims.Count > 1` and PRE0073 gates on `DerivedDimension == PeriodDimension.Date`. After W-A a composite is a *single* `TemporalUnit` qualifier with `Components.Length > 1` — so the `Count > 1` heuristic is **dead for composites** (can never fire) and the `== Date` gate skips date+time composites (now `Datetime`). Switch these to `Components.Length > 1` / `Components`-based checks. These ops are currently dormant (no `period`/`period` or `price`/`period` division in the operations catalog), so this is latent, not an active defect — but it must land in W-C.
   - **False-green trap (W-A review "strongest objection")**: when wiring the first `period in 'hours + minutes' * price in 'USD/hours'` cancellation test, confirm the test **fails before** the NIT-1 fix and passes after. If it passes against unmodified `TypeChecker.Expressions.cs:1340`, the dead `Count > 1` heuristic produced a false-green — investigate immediately.
2. Legal-basis-by-source-operation (Gap 4): a composite atom illegal for the source operation (e.g., `hours` on `date - date`) emits `QualifierMismatch` per illegal atom.
3. D14 composite subset (Gap 3): assignment whose non-zero component set ⊄ declared basis emits `QualifierMismatch`.

**Exit criteria**:
- `period in 'hours + minutes' * price in 'USD/hours'` emits `CompoundPeriodDenominator` (via a `Components.Length > 1` check, NOT the dead `Count > 1` heuristic — see step 1).
- **Acceptance side (the D9-surfaced gap):** `period in 'hours' * price in 'USD/hours'` **cancels** (no `UnprovedQualifierCompatibility`). Today it over-rejects — the price×period `QualifierChainProofRequirement` resolves the period's `TemporalDimension`, which a declared single `TemporalUnit` basis does not surface on that path; `period of 'time'` is the only working form. W-C must resolve the single-basis dimension *and* keep rejecting composite (the two halves of basis-aware cancellation). Pinned RED→GREEN by `PricePerHours_TimesPeriodInHours_NotYetProven` (flip its assertion when this lands). The naive "derive dimension from basis" fix alone is UNSOUND — it would make composite wrongly cancel; the single-basis check is the guard.
- `date - date as period in 'days + hours'` emits `QualifierMismatch` for `hours`.
- Assigning a Days-bearing value to `period in 'years + months'` emits `QualifierMismatch`.
- `dotnet test` green.

**Doc-update obligations**: verify `business-domain-types.md` § D15 cancellation + § Composite legality + D14 composite extension match implementation (already amended; confirm no drift).

**✅ Completion note (W-C):** Shipped across four commits on `spike/Precept-V2-Radical`.
- `fd147dc2` — **acceptance + composite rejection (multiplication path).** Single-basis `period in 'hours' * price in 'USD/hours'` now cancels (proof-engine `TemporalDimension ← single-basis TemporalUnit` fallback, gated `Components.Length == 1` for soundness); composite `* price` emits `CompoundPeriodDenominator` not the generic `UnprovedQualifierCompatibility`. Pinned test flipped (`PricePerHours_TimesPeriodInHours_NotYetProven` → `_Cancels`). False-green trap avoided — composite-reject test verified RED before, GREEN after.
- `a42134e3` — **legal-basis-by-source-operation (Gap 4).** `date - date as period in 'days + hours'` emits `QualifierMismatch` for `hours` (and symmetric cases); catalog-driven via `TemporalUnits.IsCalendarBased`.
- `7dd07f8b` — **research** grounding the divide-path keep/delete question (`research/architecture/divide-by-temporal-span-reachability.md`, `research/language/division-by-temporal-span-prior-art.md`): D15 mandates `money ÷ period` / `money ÷ duration`; prior art (NodaTime/java.time) divides by fixed `duration` but refuses calendar `period` — D15's period-divisor rows are a Precept extension beyond prior art (flagged as a future design item).
- `d2946dc0` — **compound-period divisor rejection (division path, re-aimed) + D14 subset (Gap 3) tests.** The carry-forward NIT-1 cleanup was found to guard the *wrong operand* (left, not the denominator); re-aimed so `money ÷ period in 'hours + minutes'` (and `quantity ÷ …`) emits `CompoundPeriodDenominator` per D15. PRE0073 left as-is with a dormancy comment (composites now fully caught by PRE0074). D14 subset rejection (`{days} ⊄ {years, months}`) confirmed already enforced by D9's subset check; tests added.

All four projects green (Precept.Tests 6593 / LS 413 / MCP 67 / Analyzers 291). `precept-reviewer` cleared each soundness-critical slice. No new diagnostic codes — reused `CompoundPeriodDenominator`, `QualifierMismatch`, `DurationDenominatorMismatch`. No `business-domain-types.md` drift found (§ D14/§ D15 already match).

**Deferred to W-D / later:** the divide-path PRE0073 retains its single-basis `DerivedDimension == Date` gate (dormant for the divide path — no enabled op has a Duration/Period numerator with a temporal-unit denominator; composites are caught by PRE0074 regardless). Re-aim it with a reachable test if a future op makes that shape live.

**Settled (not deferred):** the `money ÷ period` / `quantity ÷ period` divisor surface is a **deliberate, owner-authorized D15 extension** — single-basis period divisors are permitted (well-defined unit + count), compound divisors rejected (`CompoundPeriodDenominator`), duration exempt. Prior art (NodaTime/java.time) divides only by fixed `duration` and refuses calendar-period division; D15 consciously goes one step further while honoring the same no-total-for-a-compound-period constraint. No further design needed. Precedent + tradeoff record: the two committed research docs (`research/language/division-by-temporal-span-prior-art.md`, `research/architecture/divide-by-temporal-span-reachability.md`).

### Workstream W-D — Scenario tests + doc-status verification

**Goal**: the matrix is covered by scenario tests and the docs match shipped behavior.

**Steps**:
1. Scenario tests (`test/Precept.Tests/`, extend `CompositePeriodBasisTests.cs` from W-A): canonicalization (order-independence, spaced canonical), lenient whitespace, PRE0160/0161/0162 emission, D14 subset accept/reject, legal-basis-by-operation, cancellation-blocked, `.basis`/`.dimension` returns.
   - **Carry-forward from W-A review (NIT-4)**: the three malformed-basis tests use `CheckExpectingError` (asserts *presence*, not *exclusivity*). Strengthen them to assert no *other* error codes leak (exact-count or no-other-code), since they specifically exercise per-component recovery branching (`'years + + days'` must emit only Empty, not also Unknown/Duplicate).
   - **Carry-forward from W-A review (NIT-3)**: add a test asserting the single-basis path's lenient-whitespace widening is intentional — `period in ' months '` (inner padding) resolves clean (W-A trims before `TryGet`). Documents the one behavioral delta vs. pre-W-A.
2. **D6.1 resolved: ship the realistic sample.** Extend `samples/equipment-lease-agreement.precept` — `LeaseTerm as period optional` (line 33) → `LeaseTerm as period in 'years + months' optional`; the `Quote` event sets it to `'3 years + 6 months'`. `precept_compile` clean (LeaseTerm is a recorded field, not a date-arithmetic operand, so no `of 'date'` proof needed). Update this plan's D6.1 note from "recommend none" to "lease sample shipped."
3. Verify no doc drift: `business-domain-types.md` composite sections describe shipped behavior; flip any status notes.

**Exit criteria**:
- New test file covers every row of the decision matrix; `dotnet test` green at the new higher count.
- `precept_compile` on a composite example (and the sample if added) is clean.
- `grep` confirms no `&`-as-basis-separator remains in samples or docs.

**Doc-update obligations**: `docs/language/business-domain-types.md` (status confirmation); readiness-plan Phase 6 row → ✅ Complete; this Phase 6 section → status Complete with commit refs.

## Discovered during planning

The D4 amendment is an in-place spec edit (not a `/lifecycle-2-design` doc with `sources-consulted` frontmatter), so the code surface was mapped during this planning pass rather than enumerated at design time. Files the build will touch, none a design oversight:
- `src/Precept/Language/ProofRequirement.cs` — add `PeriodDimension.Datetime` (return/comparison value; pre-existing specced-but-unbuilt gap)
- `src/Precept/Language/DeclaredQualifierMeta.cs` — `TemporalUnit` shape extension
- `src/Precept/Pipeline/TypeChecker.cs` — `MapTemporalUnitQualifier` (composite parse + combined dimension); `MapTemporalDimensionQualifier` is **not** touched (`of 'datetime'` stays rejected)
- `src/Precept/Language/DiagnosticCode.cs`, `Diagnostics.cs` — 3 new codes + factories
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — only if an emission gap exists
- `src/Precept/Language/Types.cs` — `.basis`/`.dimension` accessors
- `src/Precept/Pipeline/ProofEngine.Qualifiers.cs`, `ProofEngine.cs`, `ProofEngine.Strategies.cs` — single-atom-assumption audit + `Datetime → "datetime"` comparison mapping
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — period/price cancellation
- `test/Precept.Tests/CompositePeriodBasisTests.cs` — new

## Definition of done

Phase 6 is complete when: every WS exit criterion holds; `dotnet test` green; the diagnostic-coverage analyzer is clean; `precept_compile` accepts the canonicalization/whitespace matrix and rejects the malformed/illegal cases with the right codes; `business-domain-types.md` composite sections describe shipped behavior with no drift; the Phase 6 row flips to ✅. Runtime `Period.Between` lowering is explicitly deferred to the runtime phase (D6.2) and is not a Phase 6 exit condition.

---

# Phase 7: Total language conformance sweep

**Status**: Active — kicking off 2026-05-30. **Exploratory and open-ended; not driven by any single document.** The [`spec-conformance-audit-2026-05-30.md`](spec-conformance-audit-2026-05-30.md) register is the *first seed*, not the spine — it is known to be non-comprehensive. Phase 7 also commissions fresh audits, builds conformance tests, probes the catalog surface, and adds new discovery modes and slices as findings accumulate. The phase ends on owner judgment, not on exhausting any one input.

## Goal

Every construct, type, operator, modifier, action, and diagnostic in the locked spec and catalogs behaves in the implementation **exactly as specified** — established by exhaustive, reproducible probing, and every confirmed mismatch fixed. The premise of the phase: the 2026-05-30 audit was a *spot check* that already surfaced a confirmed soundness/functional gap on the spec's own headline example (A1), an internal spec/catalog contradiction (A2), and a backlog of unverified enforcement and diagnostic-identity suspicions (B/C class). That hit rate on a partial pass implies the language is broadly under-tested against its own spec. This phase replaces spot-checking with **total coverage**.

**Authority rule (non-negotiable, inherited from the audit):** the spec (`docs/language/*.md`) and the catalogs are the source of truth. Any spec-vs-impl mismatch is an **implementation gap to fix** — not a spec edit — *unless* (a) the owner rules a specific spec line is itself wrong, or (b) two spec surfaces contradict each other (e.g. prose vs catalog `UsageExample`), in which case it is an **owner ruling**, not a find-and-fix. The A2 exchangerate flip-flop is the cautionary precedent: before labeling a gap, check whether the spec is internally consistent on the point.

## Method — register-driven, one row at a time

This phase runs on the audit's proven discipline, scaled to the whole language. The failure mode it exists to prevent is "synthesize across everything at once," which produces confident-but-wrong findings. Instead:

- **One row = one spec claim + one canonical declaration form + one direct MCP probe + one verdict.** Verdicts: `unverified` → `CONFIRMED` (spec quote + reproducing probe recorded) / `DROPPED` (canonical form actually works; agent used a non-canonical form) / `DOC-ONLY` (impl correct, doc stale) / `NEEDS OWNER RULING` (spec internally contradictory or possibly-wrong spec line).
- **No agent summary is treated as fact.** Every CONFIRMED carries a verbatim spec line (with the spec's own canonical form) and the probe output that reproduces the gap, recorded in the register so it is independently checkable.
- **Catalog-driven completeness.** Because the catalogs *are* the language spec in machine-readable form, the probe matrix is **derived from the catalog enumerations** — `Types`, `Operators`, `Modifiers`, `Constructs`, `Actions`, `ProofRequirements`, `Diagnostics`. Enumerate the catalog members; for each, probe every declaration/usage form the spec gives as canonical. This is what makes "the entire language" a finite, checkable set rather than a vibe.
- **Probe surface is the MCP server** (`precept_compile` / `precept_diagnostic` / `precept_types` / `precept_operations` / `precept_syntax`) — the same consumer-facing path the audit used. (Watch for build-staleness: after any `src/Precept` fix, the MCP server serves its last-spawn build until `/mcp reconnect precept`.)
- **Fix one at a time, test-first.** Each CONFIRMED gap → a failing scenario test reproducing it → the fix → re-probe to green → doc-sync in the same pass. No batching fixes across unrelated roots.

## Discovery sources (the audit is one of several)

The 2026-05-30 audit is **not comprehensive and is not the sole driver.** It is the first seed. Phase 7 surfaces gaps from a growing set of discovery modes, and the slice list grows as each mode turns something up:

- **The 2026-05-30 audit register** — the opening rows (below). A partial manual spot-check; valuable but known-incomplete.
- **Commissioned audits** — fresh, scoped sweeps over surfaces the 2026-05-30 pass never reached (and re-sweeps of areas it touched shallowly), each run with the register discipline so its output is checkable, not a trusted summary.
- **Exploratory probing** — the catalog-derived probe matrix (every Type / Operator / Modifier / Construct / Action / Diagnostic × its canonical spec forms), run against the MCP compile path.
- **Conformance tests as a discovery instrument** — writing scenario/conformance tests against the spec's stated behavior, where a failing or missing test *is* a finding, not just a regression guard. Tests built here become the durable proof the gap stays closed.
- **Whatever else surfaces gaps** — the discovery-mode list is open; new modes (sample-authoring stress, differential probing, spec re-reads) get added as slices when they prove useful.

What unifies them is the **discipline**, not the source: every finding from any mode lands as a register row with a verbatim spec claim, a reproduction, and a verdict, then gets fixed one at a time. The register is the living tracker for the whole phase; the 2026-05-30 audit is simply its first contributor.

### First seed: the 2026-05-30 audit register

This phase **absorbs** `spec-conformance-audit-2026-05-30.md` as its opening rows — that register is the durable tracker and nothing in it is lost:

- **A-class (functional gaps)** — A1 price×quantity (CONFIRMED), A2 exchangerate slash (owner-ruled SLASH canonical; impl fix + `to`→`/` doc/test sweep), A3 date+literal-quantity, A4 `kg/hour` compound (both unverified). The audit's own routing note already named "a NEW remediation phase (this audit doc is its driver)" for A-class — **that phase is this one.**
- **B-class (enforcement gaps — spec says error, compiler accepts; soundness holes)** — B1–B9, all unverified. Re-homed here from the diagnostic-completeness phase: these are *conformance* defects (the language doesn't reject what the spec says is illegal), which is this phase's charter, not the catalog-completeness lens.
- **C-class (diagnostic-identity gaps — rejected correctly but wrong/generic code)** — C1–C4. C-items entangled with A1's root cause (the PRE0114 "unresolved" qualifier-chain signature) ride here with the A-class fix; standalone catalog-wiring C-items may route to Phase 9. The **A1↔C1 shared-root-cause grouping check is the gating question** before sequencing fixes.
- **D-class (doc-stale — impl correct, doc out of date)** — D1–D4. DOC-ONLY track; owner confirms the spec is the stale side, then doc fix (includes the A2 `to`→`/` doc sweep).

## Slices

Work proceeds as a plain numbered sequence — **Slice 1, Slice 2, Slice 3, …** — added as we go. Each slice is one focused unit of work. We do not pre-enumerate them all: we define the next slice, do it, review at the boundary, then define the next. The sequence runs until the owner is satisfied the compiler is fully implemented and accurate to the spec.

Every slice, whatever its content, follows the same rigor: enumerate/probe → failing-test-first → fix → adversarial review → re-probe → doc-sync.

**The kinds of work a slice can be** (the menu we draw from — not a fixed order):
- Probe the catalog surface against the spec (every type / operator / modifier / construct / action / diagnostic × its canonical spec form) and record what fails.
- Commission a scoped audit — a fresh sub-agent sweep over a surface the 2026-05-30 pass missed, landing its findings as checkable rows (no trusted summaries).
- Fix one confirmed gap, test-first, and leave the test behind as the proof it stays closed.
- Build out conformance tests, where a missing or failing test is itself a finding.
- Reconcile a stale doc once the owner confirms the implementation is the correct side.
- Anything else that surfaces gaps — the menu is open.

### Slice log

| Slice | What | Status |
|---|---|---|
| 1 | Fix `price × quantity → money` not compiling — the spec's headline example errored `PRE0114` ("quantity dimension unresolved"). Root cause: qualifier resolvers lacked a `Dimension ← Unit` projection; added `TryProjectUnitToDimension`. Same-unit cancellation now works. **Caveat (owner decision, option C):** the fix matches at dimension granularity, so cross-unit (`'USD/kg' × quantity in 'g'`) now cancels silently — left open, tracked as Slice 2. | ✅ Done 2026-05-31 |
| 2 | **`price × quantity` cross-unit cancellation policy.** Owner chose **auto-convert within dimension** — extends D8's auto-conversion to price cancellation; the runtime applies the exact UCUM factor (target-directed to the price denominator). Research committed (`37971a4b`; survey [`cross-unit-conversion-arithmetic-survey.md`](../../research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md)). **Scope correction (2026-05-31):** the only exclusion is *absolute positions* — `quantity` amounts of °C/dB convert and price cleanly (scale-only conversion); absolute-reading math is carved out to Slice 4. **Decision 2 (allow-all-with-surfacing):** no exactness rejection — all commensurable conversions allowed (`in↔ft` works), exact-vs-approximate surfaced in inspection (P8 = visible, not forbidden). **Design Locked 2026-05-31** ([`price-cross-unit-cancellation-design-2026-05-31.md`](price-cross-unit-cancellation-design-2026-05-31.md)) — research (`37971a4b`) + 2 reviews (precept-reviewer READY-TO-LOCK + Frank) + owner P8 affirmation. **Phased plan:** [`phase7-slice2-plan-2026-05-31.md`](phase7-slice2-plan-2026-05-31.md) — one executable phase (catalog + proof surfacing + hover + tests + docs); the runtime value-application is carried as a **documented requirement** (`evaluator.md`) and built in the runtime phase, not a Slice-2 phase. **Re-scoped 2026-05-31 (post-enumeration):** the execute enumerate-step found cross-unit cancellation already works (Slice 1; proof matches dimension *name*, computes no factor), `ScaleToBaseFactor` already exists (`atom.Scale`), angle units are *rejected* (divergence), and the catalog silently holds rational approximations of irrational scales (π, stripped log). Slice 2 narrowed to **exact-conversion surfacing + the `ScaleIsRational` exactness guard**; **angle → Slice 5, log → Slice 6**. See the plan's § Enumeration findings & re-scope. **Exec committed `fe7d1187`** (ScaleIsRational guard + hover surfacing + PRE0114 reword + tests; precept-reviewer pass — fixed `[pH]` mis-flag + PRE0114 over-claim; 6621/417/67/291 green). E4 count-gap found en route (own slice). **Remaining:** Stage-5 promotion (D8/§168 + evaluator runtime requirement). | ✅ Exec done `fe7d1187` → promote (Stage 5) |
| 3 | `exchangerate` slash syntax — parse `'USD/EUR'` into from/to, remove the `to`-form, sweep the `to` form out of the type docs, the catalog example, and the old tests. (Owner ruled slash canonical 2026-05-30.) | Planned |
| 4 | **Absolute measurement positions — the `instant` analog for units.** Model absolute readings on affine/log scales — an absolute temperature (thermostat reading), an absolute level (`dBm`), pH — as a distinct *point* type with offset/reference-aware arithmetic, separate from `quantity` (which is always an *amount*). Surfaced from Slice 2: `quantity` amounts multiply and convert cleanly (kg, °C-of-change, dB-gain — scale-only); absolute positions can't be multiplied and need their own mechanism. **New language surface** (new type/construct) → full lifecycle (owner consultation → research → design) when taken up. Edge case business-wise; low priority. | Parked — new surface, not started |
| 5 | **Angle-unit cross-unit cancellation** (split from Slice 2). Angle units (`deg`/`rad`/`gon`/`'`/`''`) are rejected today (`PRE0114`) — classified to an empty dimension name (`UnitDimensionHelper.cs:48`), which **diverges from the locked design** (allow + surface). Fix: give angle a dimension identity so same- and cross-angle cancel; their scale is a rational approximation of π → surfaced **approximate** (via the `ScaleIsRational` flag added in Slice 2). | Planned (split from Slice 2) |
| 6 | **Log cross-unit cancellation policy** (split from Slice 2). `dB`/`Np`/`B` cancel today, but the UCUM log function is **stripped** (`UcumAtomCatalog.cs:466`) — the catalog holds *no real `dB↔Np` factor*, and the field's libraries *forbid* multiplying log units. The locked Decision 2 said "allow + surface," but there's no multiplicative scale to surface. **Needs a design ruling** (allow-with-what vs. restrict cross-log) before execution — a small `/lifecycle-2-design` amendment or owner ruling. | Parked — design ruling needed |

*(Append a row per slice as we go. This log is the running record of the phase.)*

## Decisions required

- **Is the price×quantity bug one bug or two?** It and a related "wrong diagnostic code" case both show the same "unresolved qualifier" signature, so they may share a single root cause. Check before sequencing fixes — if shared, one fix closes both. (Register rows: A1 and C1/PRE0073.)
- **The Phase 7 / Phase 9 line.** Which diagnostic problems are *conformance* (the language behaves wrong → fixed here) vs *catalog-completeness* (every declared code is wired or retired → Phase 9)? Proposed split recorded above; owner confirms.
- **Spec-contradicts-itself cases** — when two parts of the spec disagree, or a spec line may itself be wrong, that's an owner ruling, not a find-and-fix. Surfaced one at a time as they arise. (The exchangerate slash-vs-`to` case is already settled: slash wins.)
- **Doc-stale cases** — confirm the implementation is the correct side and the doc is just out of date, before fixing the doc instead of the code.

## Exit criteria

- [ ] Every catalog member (type, operator, modifier, construct, action, diagnostic) has been probed against its spec form and given a verdict — nothing left unchecked.
- [ ] Every spec example marked "✓ compiles" actually compiles (the spec's own headline examples pass).
- [ ] Every confirmed feature gap and every confirmed soundness hole (spec says reject, compiler accepts) is fixed, each with a test that failed before the fix and passes after, and the docs synced.
- [ ] Every case where the compiler reports a generic error instead of the spec's named one is fixed and re-probed.
- [ ] Every doc-stale case is owner-confirmed and the doc fixed (including the exchangerate slash sweep across the type docs, the catalog example, and the old `to`-form tests).
- [ ] No unverified rows remain in the finding register.
- [ ] Full suite green across all 4 projects after the fixes.
- [ ] **Owner judgment** — the owner is satisfied the compiler is fully implemented and accurate to the spec. This is the load-bearing gate: the checklist above is necessary, but the phase closes when the owner says the language matches its spec, not on a count.

## Cadence and completion

This phase is **open-ended by owner intent**: slice after slice after slice, for as long as it takes, until the owner is satisfied the compiler is fully implemented and accurate to the spec. There is no fixed finding count and no week budget — the register grows as enumeration and probing surface new rows, and the phase runs until it is exhausted *and* the owner signs off. Each slice follows the locked per-slice rigor (enumerate/probe → failing-test-first → fix → adversarial review → re-probe → doc-sync), and the plan pauses for review at each slice boundary rather than auto-advancing.

## Estimated effort

**XL / open-ended.** Coverage is the cost driver — the fix count is unknown until the probe matrix is built and the probes run. The audit's hit rate on a partial manual pass (1 confirmed functional gap + 1 spec contradiction + ~13 unverified suspicions) suggests a substantial backlog. Sequence as: enumerate → probe to verdicts → group by root cause → fix one at a time. Treat the register's resolution + owner sign-off as the unit of progress, not a day estimate.

---

# Phase 8: Diagnostic-emission architecture

*(Split from Diagnostic completeness 2026-05-31 — the emission-architecture work (DA-1/2/3) is a distinct job from the catalog-completeness sweep (now Phase 9) and is being taken up first, ahead of both Phase 9 and the close of Phase 7.)*

**Goal**: Consolidate and regularize *where* diagnostics are emitted into a unified, deterministic emission model, and enrich what an unproved obligation hands back to the author — all within Precept's locked determinism boundary (no IVL/SMT — `proof-engine.md` opaque-solver rejection).

Grounded in `research/architecture/compiler/type-proof-stage-contract-survey.md` and `flow-sensitive-check-placement-survey.md` (both `status: Cited`).

**Findings in scope** (3):
- **DA-1 — Dedicated diagnostic-emission phase** (answers `research/architecture/README.md` open-question #2). The survey found 3 of 4 production compilers (Kotlin K2 `CHECKERS` phase, Rust MIR reporting walk, Roslyn) defer *all* diagnostic emission to one terminal phase, decoupled from where the check computes. Precept emits scattered across the type and proof stages; the Phase 6 Site-A PRE0141 Type→Proof re-stage (`docs/Working/Archive/assignment-qualifier-discharge-placement.md`) is a one-off symptom. A unified emission phase makes "which stage emits this" a non-question. **Stakes: medium (a pipeline-shape change); needs a `/lifecycle-2-design` pass.**
- **DA-2 — Regularization audit: type-immediate checks that should be stamped obligations.** The assignment-qualifier check (Site-A) was the *lone* qualifier check done as a type-immediate emit instead of a stamped obligation (the `proof-engine.md` Decision-3 contract). Audit `DiagnosticCode.cs` / the type checker for *other* checks that are really proof obligations in disguise; regularizing them improves uniformity and may unlock narrowing/proof for them (as Site-A now does). **Small, discoverable — a grep-and-classify pass; Site-A handled its own instance in Phase 6, this finds the rest.**
- **DA-3 — Counterexample / witness richness for unproved obligations.** Dafny/CBMC hand the author a concrete counterexample on a failed proof; Precept emits the diagnostic but not the witness. Surfacing "X could be 0 here" for an unproved obligation sharpens the domain-expert experience and is squarely in the inspectability commitment — without an external solver. **Medium; aligns with `proof-engine.md` proof-attribution.**

**Optional grounding (research, horizon)**: a Whiley deep-dive — the closest structural analog (a language designed around a separate verification stage that self-derives, not VC/IVL) — would best inform DA-1/DA-3 if deeper grounding is wanted before the design pass. Not a build item.

**Decisions required**:
- DA-1: introduce a dedicated diagnostic-emission phase, or keep per-stage emission with the Site-A-style targeted re-staging? (design pass)
- DA-3: counterexample/witness surface — scope and shape (which obligation kinds; structured-data shape for tooling)?

**Suggested entry sequence**: DA-2 first (the grep-and-classify regularization audit — concrete, grounds DA-1's evidence base), then DA-1 (`/lifecycle-2-design` on the dedicated emission phase, optionally after the Whiley grounding), then DA-3. The companion architecture-direction conclusion — *stay catalog/stamp; no VC-gen/IVL; no incremental* — is recorded in `research/architecture/README.md` open-questions #1/#2.

**Status**: **Active — taken up 2026-05-31**, ahead of the Phase 9 completeness sweep and while Phase 7 continues. Independent of the in-flight Phase 7 work (the C-class diagnostic-identity / PRE0114 items are a conformance + completeness concern, not an emission-architecture one, and stay with Phase 7 / Phase 9).
**Estimated effort**: M→L (DA-2 small; DA-1 medium + a `/lifecycle-2-design` pass; DA-3 medium).

---

# Phase 9: Diagnostic completeness

**Goal**: Every diagnostic code declared in `DiagnosticCode.cs` is either emitted from a real code path, has scenario-test coverage that asserts it fires, or is explicitly retired. CI enforcement is bidirectional.

**Findings in scope** (~7):
- F-LANG-SPEC-06 (~startsWith/~endsWith first-arg-must-be-~string enforcement)
- F-LANG-SPEC-09 (ChoiceElementTypeMismatch / ChoiceMissingElementType emission OR retirement)
- F-LANG-SPEC-12 (OutOfRange constant-literal bounds check emission OR retirement)
- F-LANG-SPEC-13 (NonOrderableCollectionExtreme emission OR consolidation with TypeMismatch)
- Diagnostic scenario-coverage matrix completion (all declared codes have scenario tests beyond just structural reflection; reconcile the 148-vs-162 `DiagnosticCode`-count discrepancy between this plan and the audit as step zero)

**Decisions required**:
- F-LANG-SPEC-09: wire or retire ChoiceElementTypeMismatch / ChoiceMissingElementType?
- F-LANG-SPEC-12: wire OutOfRange constant-literal check or remove from spec § 3.10?
- F-LANG-SPEC-13: NonOrderableCollectionExtreme distinct emission or consolidate?

**Status**: Stub — detailed execution plan TBD pending the listed decisions. **Seam with Phase 7**: the conformance sweep (Phase 7) fixes spec↔impl behavior gaps including B-class enforcement holes and A1-entangled C-class diagnostic-identity items; this phase owns the *catalog-completeness* lens (every declared `DiagnosticCode` emits-or-retires). Re-scope its finding list against what Phase 7 closes before kickoff.

---

# Phase 10: API surface solidity (typed descriptors + analyzer extensions)

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

**Status**: Stub — detailed execution plan TBD pending Phase 9 completion and F-API-01 sequencing decision.
**Estimated effort**: M-L (~3-5 days — F-API-01 alone is a broad public-API change; the analyzer extensions are smaller).

---

# Phase 11: Polish + cleanup

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
- F-X-03 cleanup of removed defensive throws (Phase 10 may have settled — re-check)
- Any P2s where the implementer needs guidance.

**Status**: Stub — detailed execution plan TBD pending Phase 10 completion.
**Estimated effort**: M (~3 days — many small items, parallelizable).

---

# Phase 12: Runtime gate verification

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

**Status**: Stub — detailed verification plan TBD pending Phase 11 completion.
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
- [`bugs.md`](bugs.md) — sample-side bugs.

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
