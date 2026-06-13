> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.

# Spec-Conformance Suite Plan — 2026-06-01

**Status**: Draft
**Companion docs**: [`conformance-suite-design-2026-06-01.md`](conformance-suite-design-2026-06-01.md) (Locked 2026-06-01) · [`../../research/architecture/spec-conformance-test-suite-survey.md`](../../research/architecture/spec-conformance-test-suite-survey.md) (Cited) · derives a new slice of [`compiler-readiness-plan-2026-05-24.md` § Phase 7](compiler-readiness-plan-2026-05-24.md); absorbs [`spec-conformance-audit-2026-05-30.md`](spec-conformance-audit-2026-05-30.md) as its first findings.
**Scope gate**: turns Phase 7 (the "total language conformance sweep") into an executable, permanent artifact, and front-loads the v1 runtime's acceptance spec. Does **not** gate anything downstream on completion — the phase is owner-paced and runs until the owner judges the language matches its spec (Phase 7's locked exit).

## Phase summary

| Phase | Goal | Items in scope | Decisions required before kickoff | Effort | Status |
|---|---|---|---|---|---|
| 0 | Infrastructure & conventions — the scaffolding every area builds on | attributes, meta-tests, harvester, fixture, traits, fence convention, contributing doc, runtime clock-seam note | none (all locked in design) | M (3–5d) | **Active — next** |
| 1 | First area: **BusinessDomainTypes** — prove the whole method end-to-end on the highest-value area | ~30 features; pins A1/A2/A3/A4/E4 + B/C-class register rows | derived-AC triage cadence; existing-7000 relationship; agent-authoring control (all surface here first) | XL (~10–15d incl. gap fixes) | Planned (heavyweight) |
| 2 | **TemporalTypes** area | ~30 features | TZDB pin confirmed; Phase-1 method retro | L–XL | Stub |
| 3 | **PrimitiveTypes + CollectionTypes + Expressions** | ~50 features | — | XL | Stub |
| 4 | **Constructs + Lexical + Grammar** | ~40 features | — | L–XL | Stub |
| 5 | **Diagnostics + Stateless + Proof** | ~30 features | `[Covers]` exclusion list (couples to Phase 9); Phase 8/9 board reconciliation | L | Stub |
| R | **Runtime-facet un-skip / TDD** — drive `component=runtime` green | all runtime ACs authored in 0–5 | gated on the v1 runtime build (renumbered Phase 12), not this plan | XL | Stub (future phase) |

*The phase count is not fixed — areas may split or merge as deep-reads surface their true feature counts (Decision 4 § Method). The plan grows area-by-area; only Phase 0 + Phase 1 are heavyweight here (guard 3).*

## Decisions captured

All locked in the design (`conformance-suite-design-2026-06-01.md`, 2026-06-01) — do not re-litigate:

- **D1** One feature-organized suite in its **own lane, allowed red**; `component`(compiler/runtime) + `feature` traits; worked **feature-by-feature to green**. The existing 7000 stay green-gated (suite is *not* in that gate).
- **D2** Three-edge coverage (Catalog↔Impl edge-1; Spec↔Impl edge-2a harvest + edge-2b prose census; Spec↔Catalog edge-3 reconciliation).
- **D3** Three org axes: domain **folders**, `[Covers]` **coverage checklist** (reverse backstop), **content-addressed `[SpecRef]`** (quote, never line number), `feature`/`component` **traits**.
- **D4** Feature AC-sheet is the AC-definition unit; **spec-first, catalog-reconciled** derivation; stated/derived AC tagging; derived-with-no-cite ACs route to owner.
- **D5** Negative coverage = specific `PRE####` at a specific stage, **hand-asserted** (never `--bless`/baseline).
- **D6** Combinatorics = category-partition equivalence classes + all-pairs; catalog-generated `[Theory]` data; full cross-product only for the dimension/unit/operator cluster.
- **D7** Runtime facet = `Skip`'d not-yet tests (`component=runtime`), un-skipped feature-by-feature at v1. No manifest/ratchet/leak-guard (allowed-red lane needs none).
- **D8** Spec examples carry fence annotations (`expect=ok`/`expect=PRE####`/`fragment`/`illustrative`); harvester reads them; un-annotated fence = lint failure; one-time `docs/language` annotation sweep (owner-approved, examples-not-prose).
- **D9** Runtime determinism = invariant culture (verify) + pinned TZDB + injectable per-fire `now()` clock; the clock seam is a recorded v1 runtime requirement.
- **Owner resolutions (2026-06-01)**: spec prose stays as-is (no RFC-2119 rewrite); `[Covers]` floor = ≥1 test for now; owner ratifies each area's feature list at the slice boundary; no quantitative coverage %.

## Open decisions

Carried from the design's secondary open questions; categorized by the phase that first forces each. None alters a locked decision.

| Decision | Options | Recommended | Lands in | Gating phase |
|---|---|---|---|---|
| **Derived-AC triage cadence** — how owner-ruling findings are batched + what bar qualifies an AC as "derived" vs author opinion | (a) review at each area boundary, bar = "the analysis can state *why* the spec implies it, with the nearest cite"; (b) ad-hoc | (a) — fits the slice-boundary review discipline | `docs/contributing/conformance-suite.md` | **Phase 1** (first derived ACs appear) |
| **Existing-7000 relationship** — migrate conformance-shaped tests (e.g. `PriceTimesQuantityTests`) into the suite, or leave + accept overlap | (a) leave in place, suite is additive, dedup only on maintenance pain; (b) migrate | (a) — migration churn unjustified pre-release; the suites answer different questions (impl-anchored vs spec-anchored) | `docs/contributing/conformance-suite.md` | **Phase 1** (BusinessDomainTypes overlaps the existing price/quantity tests) |
| **Agent-authoring quality control** — how delegated test authoring avoids the misread-spec-then-fix-impl trap | (a) verbatim `[SpecRef]` quote + `SpecRefValidationTests` + adversarial re-walk of each area (two independent extractions diffed); (b) solo authoring | (a) — already the design's edge-2b discipline; formalize for all authoring | `docs/contributing/conformance-suite.md` | **Phase 1** (first delegated area) |
| **`[Covers]` exclusion list** — declared-but-unwired/retired `DiagnosticCode`s the meta-test must not demand a test for | (a) Phase 0 builds an empty exclusion hook; the list is populated as Phase 9 rules each code emit-or-retire; (b) no exclusion (meta-test red on unwired codes) | (a) — the hook is cheap; the list is Phase-9 work | `Infrastructure/CatalogCoverageMetaTests.cs` + Phase 9 | **Phase 0** (hook) / **Phase 5** (list) |
| **Phase 8 / Phase 9 board reconciliation** — this suite subsumes Phase 9's emission-coverage half; reconcile the readiness-plan board | (a) reconcile after Phase 1 proves the method; (b) reconcile now | (a) — let the method prove out before reshuffling the board | `compiler-readiness-plan-2026-05-24.md` | **Phase 5** (Diagnostics area) / post-Phase-1 note |

## Heavyweight phase blocks

### Phase 0 — Infrastructure & conventions

**Goal.** A runnable `Precept.Conformance.Tests` project in its own lane, with the attributes, meta-tests, harvester, fixture, traits, and fence-annotation convention in place — so Phase 1 is pure feature-authoring against a working harness.

**Items in scope.** D1 (project + lane + traits), D3 (`[SpecRef]`/`[Covers]` + validation), D8 (fence convention + lint, *convention only* — the sweep is per-area), D9 (`RuntimeDeterminismFixture`), edge-1 generator scaffold (D2/D6), the contributing doc, the runtime clock-seam note.

**Decisions required before kickoff.** None — all locked. (The `[Covers]` exclusion *hook* is built here as an empty list; populating it is Phase 5.)

**Step-by-step execution.**
1. Create `test/Precept.Conformance.Tests/Precept.Conformance.Tests.csproj` **excluded from the default `dotnet test` solution-filter / CI gate** (its own lane). Verify: the existing `dotnet test` run does not include it; a deliberately-failing throwaway test in it does not turn the main gate red.
2. `Infrastructure/SpecRefAttribute.cs` (`doc, section, quote`) + `Infrastructure/CoversAttribute.cs` (params catalog-member identifiers) + the `component`/`feature` trait helpers. Verify: a sample test compiles carrying all three.
3. `Infrastructure/SpecRefValidationTests.cs` — every `[SpecRef]` quote resolves in the named doc/section, or the test is `derived`. Verify: a test with a bogus quote turns it red; rewording a real cited sentence turns it red (drift signal).
4. `Infrastructure/CatalogCoverageMetaTests.cs` — reflect over `Types.All`, `Operators.All`, `Modifiers`, `Constructs`, `Actions`, `ProofRequirements`, `DiagnosticCode`; fail on any member with zero `[Covers]`; honor an (initially empty) exclusion list. Verify: removing a member's only `[Covers]` test turns it red; an excluded member does not.
4b. **Emit the catalog-anchored feature skeleton** — from the same enumerations, generate the candidate feature-name list (~110–125 catalog-anchored names) + rough per-area counts, as a planning artifact (the `[Covers]` coverage checklist + a sizing sketch). This is **mechanical and free** (no prose read). It is explicitly **names, not ACs, and incomplete on emergent/cross-cutting features** (cancellation, narrowing, the E4-class buried behaviors) — those, and every feature's ACs, come from the per-area deep-read (Phase 1+), never from this skeleton. The skeleton sizes the program and seeds each area's `[Covers]` checklist; it does not define features.
5. `Infrastructure/FenceAnnotationLintTests.cs` (D8) — every ` ```precept ` fence in `docs/language/*.md` carries exactly one annotation. **Allowed red in the lane until the per-area sweeps complete.** Verify: an un-annotated fence is reported by name.
6. `Infrastructure/SpecExampleHarvest.cs` (edge-2a) — parse fence-annotated blocks + ✓/✗ rows; scaffold `fragment` blocks; skip `illustrative`; assert `expect=…`. Verify against a 3-fixture set (one `expect=ok`, one `expect=PRE####`, one `fragment`).
7. `Infrastructure/RuntimeDeterminismFixture.cs` (D9) — invariant culture, pinned NodaTime TZDB version, fixed per-fire instant; + a culture-varying meta-test stub. Verify: the fixture is referenced by a skipped runtime-facet smoke test.
8. Edge-1 generator scaffold — a `[Theory]` `MemberData` source that reads a catalog member's `UsageExample`/`QualifierShape` and a one-axis mutation for negatives (D6). Verify: generates ≥1 positive + ≥1 negative case for `price`.
9. Write `docs/contributing/conformance-suite.md` (method, attributes, three-edge model, traits, run model, D8 convention, D9 determinism).
10. Add the v1 runtime clock-seam requirement note to `docs/runtime/evaluator.md` (+ `runtime-api.md`): `now()` resolves through an injectable per-operation clock, sampled once per fire (Decision 9; not a semantics change).
11. Add the Phase 7 slice-log row to `compiler-readiness-plan-2026-05-24.md` and the CONTRIBUTING § Test projects entry.

**Dependencies.** None upstream. Blocks Phase 1.

**Exit criteria.**
- [ ] `Precept.Conformance.Tests` exists, builds, and is provably **outside** the default gate (a failing test in it leaves `dotnet test` green).
- [ ] `SpecRefValidationTests`, `CatalogCoverageMetaTests`, `FenceAnnotationLintTests` each demonstrably go red on their respective violation (shown via a throwaway fixture) and green when satisfied.
- [ ] The harvester passes its 3-fixture set (`ok`/`PRE####`/`fragment`).
- [ ] The edge-1 generator emits a positive + negative `price` case.
- [ ] `RuntimeDeterminismFixture` exists; one skipped runtime smoke test references it.
- [ ] `docs/contributing/conformance-suite.md` exists; `docs/runtime/evaluator.md` carries the clock-seam note; CONTRIBUTING + Phase-7 slice-log updated.

**Estimated effort.** M (3–5d).

**Doc-update obligations (per CLAUDE.md routing).**
- `docs/contributing/conformance-suite.md` — **new** (the method).
- `docs/runtime/evaluator.md` (+ `runtime-api.md`) — clock-seam v1 requirement (D9).
- `CONTRIBUTING.md` § Test projects — the new project + run model.
- `compiler-readiness-plan-2026-05-24.md` § Phase 7 slice log — the slice row.

### Phase 1 — BusinessDomainTypes area (first area, proves the method)

**Goal.** Every feature of the money/currency/quantity/price/exchangerate/unitofmeasure/dimension domain has its AC sheet and conformance tests; its compiler facet is driven green (each gap fixed as a Phase 7 sub-slice); its runtime facet is authored + `Skip`'d; its spec examples are annotated. A1/A2/A3/A4/E4 are permanently pinned.

**Items in scope.** ~30 features (the type family + dimensional cancellation, cross-unit conversion, the `in`/`of` system, admission-vs-arithmetic, exactness surfacing); the register's A1 (done — regression-pin it), A2 (exchangerate slash), A3 (date+literal-quantity), A4 (`kg/hour` compound), E4 (counting-unit silent cancel), and the entangled B/C-class rows.

**Decisions required before kickoff.** The three Phase-1-gated open decisions: derived-AC triage cadence, existing-7000 relationship, agent-authoring control. (Recommendations above; confirm at kickoff.)

**Step-by-step execution.** Per the design's § Method, feature-by-feature:
1. **Deep-read** `business-domain-types.md` (spec-first) → feature inventory with draft AC sets. Owner ratifies the feature list at this boundary.
2. **Catalog reconcile** the inventory against `Types`/`Operators` (+ the unit/dimension catalogs); record bidirectional orphans as findings.
3. **Annotate** this area's ` ```precept ` fences + ✓/✗ tables (D8 sweep, scoped to `business-domain-types.md`).
4. For each feature: write its AC sheet (`docs/Working/conformance-feature-sheets/<feature>.md`), then realize ACs as tests in `BusinessDomainTypes/`, then **drive `feature=X&component=compiler` green** — each unfixed gap becomes a Phase 7 fix sub-slice (test-first; the gap's failing test is the proof). Runtime-facet ACs authored + `Skip`'d against `RuntimeDeterminismFixture`.
5. Pin A1 (positive cancels clean) + E4 (negative: PRE0137 at type stage) explicitly; carry A2/A3/A4 fixes as their own sub-slices with register-row cites.

**Dependencies.** Phase 0 complete.

**Exit criteria.**
- [ ] `business-domain-types.md` fence-annotation sweep complete → `FenceAnnotationLintTests` green *for that file*.
- [ ] Every BusinessDomainTypes catalog member has ≥1 `[Covers]` test (the area's slice of `CatalogCoverageMetaTests` green).
- [ ] `feature=*&component=compiler` for the area is green **except** gaps explicitly carried as open Phase 7 sub-slices (each with a `[SpecRef]` + register cite).
- [ ] E4 and A1 each have their pinning compiler-facet tests (negative `PRE####`@type; positive cancels clean).
- [ ] Runtime-facet ACs for the area authored, `component=runtime`, `Skip`'d.
- [ ] Derived-AC findings list produced and owner-reviewed at the area boundary.
- [ ] Area feature-AC sheets committed.

**Estimated effort.** XL (~10–15d, dominated by fixing the A/B/C/E gaps the area surfaces — each its own test-first sub-slice).

**Doc-update obligations.**
- `docs/language/business-domain-types.md` — fence-annotation sweep (D8, examples only).
- `docs/Working/conformance-feature-sheets/*.md` — the area's AC sheets (Working artifacts).
- `docs/Working/spec-conformance-audit-2026-05-30.md` — mark A2/A3/A4/E4 rows as pinned/fixed as they close; note the living-tracker role transfers to the suite.
- Any canonical doc touched by a gap fix (e.g. `business-domain-types.md` for A2 slash, per that fix's own doc-sync) — per the gap's sub-slice.

## Lightweight phase stubs

- **Phase 2 — TemporalTypes.** Goal: temporal type family + arithmetic + qualifiers + `now()`/zone conformance. Scope ~30 features. Decisions: confirm the TZDB pin; apply Phase-1 method retro. Effort: L–XL. **Status: Stub — TBD pending Phase 1 completion.**
- **Phase 3 — PrimitiveTypes + CollectionTypes + Expressions.** Goal: primitives, collections (queue/stack/set/list + qualified inner types), operators/expression forms. Scope ~50 features. Effort: XL. **Status: Stub — TBD pending Phase 2.**
- **Phase 4 — Constructs + Lexical + Grammar.** Goal: precept/field/state/event/rule/ensure/transition declarations; lexer (§1) and grammar (§2). Scope ~40 features. Effort: L–XL. **Status: Stub — TBD pending Phase 3.**
- **Phase 5 — Diagnostics + Stateless + Proof.** Goal: diagnostic-identity conformance, stateless-precept first-class coverage, proof-requirement conformance. Scope ~30 features. Decisions: populate the `[Covers]` exclusion list (emit-or-retire, couples to Phase 9); reconcile the Phase 8/9 board. Effort: L. **Status: Stub — TBD pending Phase 4 + the Phase-9 seam.**
- **Phase R — Runtime-facet TDD.** Goal: un-skip `component=runtime` area-by-area and drive green against the built v1 runtime. Gated on the v1 runtime build (renumbered Phase 12), not this plan. Effort: XL. **Status: Stub — future phase.**

## Discovered during planning

Sources the plan touches that the locked design names in its body but did not separately excerpt-cite in `sources-consulted` (not design oversights):

- **Catalog enumeration kind-files** — `ModifierKind.cs`, `ConstructKind.cs`, `ActionKind.cs`, `ProofRequirementKind.cs`, `DiagnosticCode.cs`. The design's Inventory + Decision 3 name these enumerations as the `CatalogCoverageMetaTests` coverage checklist; the meta-test reflects over them in Phase 0. Documented coverage source, just not per-file excerpt-cited.
- **`samples/*.precept`** — area authoring (via the `precept-author` sub-agent) reads representative samples for canonical forms per CLAUDE.md DSL-authoring discipline. Reference material, not a design surface; touched read-only.

All other plan-touch sources (the new test project + infra files; `docs/language/*.md`; `docs/runtime/evaluator.md`/`runtime-api.md`; `docs/contributing/conformance-suite.md`; `CONTRIBUTING.md`; `compiler-readiness-plan-2026-05-24.md`; `spec-conformance-audit-2026-05-30.md`; `Types.cs`/`Operator.cs`) are in the design's `sources-consulted` or its doc-update enumeration.

## Definition of done

The plan is "complete" (and the Phase 7 scope gate's executable half is satisfied) when:
- Every spec domain area (Phases 1–5) has been deep-read, its feature list owner-ratified, its catalog reconciled, its AC sheets written, and its **compiler facet driven green** (gaps fixed or carried as explicit open sub-slices).
- `CatalogCoverageMetaTests` is green across all enumerations (every member `[Covers]`-ed or explicitly excluded).
- `FenceAnnotationLintTests` is green across all of `docs/language` (the annotation sweep complete).
- The runtime facet is fully authored + `Skip`'d (the v1 acceptance spec is in place).
- **Owner judgment** (Phase 7's load-bearing exit): the owner is satisfied the compiler matches its spec. The runtime facet's green is a *separate*, later gate (Phase R / the v1 runtime build).

## Plan update protocol

- **Per area (Phases 1–5):** pause at the area boundary; owner ratifies the feature list (kickoff) and reviews the derived-AC findings (close). Append the area's outcome + any new findings to this plan and to `spec-conformance-audit-2026-05-30.md`.
- **When a gap is found:** it becomes a Phase 7 fix sub-slice (test-first), tracked with a `[SpecRef]` + register cite; closed gaps flip their suite test green and their register row to fixed.
- **When an open decision resolves:** record it in § Decisions captured and delete it from § Open decisions.
- **When a later phase nears (within ~2 weeks):** promote its stub to a heavyweight block (guard 3).
- **Phase 8/9 reconciliation:** revisit after Phase 1 proves the method; update the readiness-plan board then.
