# Phase 7 Slice 2 — Execution Plan (price × quantity cross-unit cancellation) — 2026-05-31

**Status**: Active
**Companion docs**:
- Locked design: [`price-cross-unit-cancellation-design-2026-05-31.md`](price-cross-unit-cancellation-design-2026-05-31.md) (Locked 2026-05-31)
- Evidence/decision trail: [`price-cross-unit-cancellation-2026-05-31.md`](price-cross-unit-cancellation-2026-05-31.md)
- Research: [`cross-unit-conversion-arithmetic-survey.md`](../../research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md)
- Phase hub: [`compiler-readiness-plan-2026-05-24.md`](compiler-readiness-plan-2026-05-24.md) § Phase 7 slice log
**Execution mode**: spike-branch (`spike/Precept-V2-Radical`) — no PR; **this plan doc is the execution hub**, the readiness-plan slice log points to it.
**Scope gate**: closes the live cross-unit hole at compile-time + catalog + inspection; the runtime value-application is pinned for the runtime phase.

## Phase summary

| Phase | Goal | Items | Decisions | Effort | Status |
|---|---|---|---|---|---|
| 1 | Buildable-now — catalog metadata + proof-engine surfacing + hover + diagnostics + tests + doc-sync (**incl. documenting the runtime reduction-rule requirement in `evaluator.md`**) | 7 | 0 (design locked) | **M (~2–3 days)** | **Active** |

**This slice has one executable phase.** The runtime value-application (actually applying the factor) is **out of Slice 2's scope** — it is built in the readiness-plan **runtime phase (Phase 11)**, governed by the requirement this slice *documents* in `evaluator.md` + the locked design's § Semantic Rules. Slice 2's runtime deliverable is the **documentation of the requirement**, not the build — so the durable carrier of the runtime obligation is canonical docs, not a tracked execution phase.

## Decisions captured

- **Design Decisions 1–4 locked 2026-05-31** (`price-cross-unit-cancellation-design-2026-05-31.md`): (1) extend D8 auto-convert to price cancellation, target-directed; (2) **allow-all-with-surfacing** (no exactness gate — `in↔ft` works; exact/approximate surfaced in inspection); (3) exclusion = absolute positions only (→ Slice 4); (4) conversion metadata lives in the UCUM atom catalog.
- **Owner P8 affirmation (2026-05-31)**: inspection-surfacing, backed by the `maxplaces`-exactness type guarantee, satisfies P8's "visible in the type system" clause; no type-level approximate-`money` marker built now (the design's 2nd Falsifier watches the future case).
- **No open language-surface decisions remain for execution** — the surface is fully locked.

## Open decisions

- **None gate Phase 1.** One low-stakes execution-time choice (not a gate): the exact wording of the hover/inspection "approximate" label — settle in execution, doc in `language-server.md`.
- **Runtime build dependency** (not a Slice-2 gate): the evaluator reduction rule is built in the readiness-plan **runtime phase (Phase 11)** against the requirement this slice documents in `evaluator.md`. No decision pending — purely a dependency; the build is out of Slice 2's scope.

---

## Phase 1 (heavyweight, current) — Buildable-now: compile-time + catalog + inspection

**Goal**: `price × quantity` cross-unit cancellation is correct and surfaced at compile-time + catalog + inspection; the live hole's compile-time behavior is closed and tested; the runtime value-application is documented as an obligation.

**Items in scope** (from the design Inventory):
1. **Catalog metadata** on the UCUM atom catalog: `ScaleToBaseFactor` (exact rational — uses the existing `UcumExactFactor`), `ScaleIsRational` (false for π/log atoms — for surfacing), `IsRatioScale` (true for every current unit — wired, no-op until Slice 4).
2. **Proof engine**: extend the existing `QualifierChainProofRequirement` discharge in `ProofEngine.Qualifiers.cs` to read `IsRatioScale` (no-op now); compute the exact/approximate classification **for surfacing, not gating**. No new `ProofRequirementKind`.
3. **Inspection/hover** (language server): surface the conversion (which unit → which, the factor) and its exact/approximate status.
4. **Diagnostics**: broaden `PRE0114` wording to name the real-world mismatch (domain-targeted), per the design's Audience error-message.
5. **Tests** (`test/Precept.Tests/Operations/PriceTimesQuantityTests.cs`): the matrix below.
6. **Doc-sync** (the doc-update obligations below).

**Decisions required before kicking off**: none (design locked).

**Step-by-step execution** (per `/lifecycle-4-execute` rigor — enumerate + failing-tests-first, catalog-first, doc-sync in-commit):
1. **Enumerate the input space + write the failing-test matrix FIRST**:
   - `price in 'USD/ft' × quantity in 'in'` → cancels, no `PRE0114` (exact rational 1/12). *(red now)*
   - `price in 'USD/kg' × quantity in 'g'` → cancels, no `PRE0114` (already green post-Slice-1; assert stays green).
   - un-skip `CrossUnit_SameDimension_MustNotSilentlyCancel` → rewrite to assert **correct cancellation** (no `PRE0114`), not the old "must error".
   - an **irrational-factor** pair (if such a unit is cataloged, e.g. an angle pair) → cancels, no `PRE0114`, **surfaced approximate**. *(if no irrational unit is cataloged yet, assert the classification on `ScaleIsRational` directly + leave a note.)*
   - `price in 'USD/kg' × quantity in 'm'` (cross-dimension) → `PRE0114` fires. *(regression guard, present)*
   - `price in 'USD/each' × quantity in 'box'` (count, no factor) → still errors (`PRE0137`). *(regression guard)*
2. **Catalog-first**: add the three metadata fields to the UCUM atom catalog (`src/Precept/Language/Ucum/`); populate `ScaleToBaseFactor` from `UcumExactFactor`; set `ScaleIsRational` / `IsRatioScale`.
3. **Proof engine**: wire the `IsRatioScale` read + the exact/approximate surfacing classification onto the existing `QualifierChain` discharge.
4. **Hover/LS**: surface conversion + status.
5. **Diagnostics**: broaden `PRE0114` wording.
6. **Make the matrix green**; **doc-sync in the same commits**.
7. **Adversarial review** (`precept-reviewer`) of the diff before commit (proof-engine/catalog = soundness-critical).

**Dependencies**: Slice 1 (`b7c17482`, the `Dimension ← Unit` projection) shipped; `UcumExactFactor.cs` exists (the rational substrate).

**Exit criteria** (testable):
- `in↔ft`, `kg↔g` cross-unit pairs cancel clean (no `PRE0114`); `CrossUnit_SameDimension_MustNotSilentlyCancel` un-skipped and green asserting correct cancellation.
- An irrational-factor case cancels (no `PRE0114`) and is surfaced approximate (test asserts the classification).
- Cross-dimension (`kg × m`) and count (`each × box`) still error (regression tests green).
- `dotnet test` green across **all four** projects.
- Hover surfaces the conversion + exact/approximate status (LS test).
- Doc-sync complete (the obligations below).
- The cross-unit hole's compile-time behavior is closed (no silent wrong-dimension-only cancellation).

**Estimated effort**: **M (~2–3 days)** — catalog metadata + proof-engine surfacing are small given `UcumExactFactor` exists; the test matrix + hover + doc-sync are the bulk.

**Doc-update obligations** (per CLAUDE.md routing; land in-commit with the code):
- `docs/language/business-domain-types.md` § price / § D8 — extend D8 to price cancellation; the allow-all-with-surfacing rule; the absolute-position exclusion; **the `:168` "Unit conversion is explicit" disambiguation** (visible/traceable, not manual syntax).
- `docs/compiler/proof-engine.md` § QualifierChain — the `IsRatioScale` side-condition + the exact/approximate surfacing classification (not a gate).
- `docs/language/catalog-system.md` § (UCUM/unit catalog) — the `ScaleToBaseFactor` / `ScaleIsRational` / `IsRatioScale` metadata.
- `docs/compiler/diagnostic-system.md` — the broadened `PRE0114` wording.
- `docs/tooling/language-server.md` — hover surfaces the conversion + status.
- `docs/runtime/evaluator.md` — **document the runtime reduction-rule requirement thoroughly** (the slice's runtime deliverable): `k = ScaleToBase(u_q)/ScaleToBase(u_p)` applied in `decimal`, target-directed to the price denominator unit (D8 rule 1), rounding at `maxplaces`; for `u_q = u_p`, `k = 1`. Mark it a **pending obligation** to be implemented in the runtime phase. The doc — not a tracked execution phase — is the durable carrier of this requirement.

---

## Runtime requirement (documented this slice; built in the runtime phase)

The reduction rule that **applies** the factor is **not** built in Slice 2 — the evaluator is a stub. Slice 2's obligation is to make sure the requirement is **well documented** so the runtime phase implements against a clear spec, and the requirement cannot get lost:

- **Where it lives**: the locked design's § Semantic Rules (reduction rule + soundness) + `docs/runtime/evaluator.md` (Phase 1 doc-sync obligation above) + the skipped `CrossUnit_SameDimension_MustNotSilentlyCancel` test (marks the gap).
- **The requirement**: on evaluation of `price × quantity` where the operand units differ within a dimension, convert the quantity to the price's denominator unit by the exact-rational factor `k` (`UcumExactFactor.Multiply`/`Divide` preserve rationality), then cancel, rounding the money result at the field's `maxplaces`. For same-unit operands `k = 1`.
- **When built**: the readiness-plan **runtime phase (Phase 11)** picks this up — no decisions pending, purely the runtime dependency. At that point the skipped test gains a runtime-magnitude assertion (`4.00 USD/ft × 36 in → 12.00 USD`) and `evaluator.md` flips to Implemented.

This is a deliberate choice (your direction): the runtime obligation is carried by **documentation**, not by a parallel execution phase that would otherwise sit indefinitely as a stub.

---

## Definition of done

- **Slice 2 done = Phase 1 complete**: cross-unit price × quantity is correct and surfaced at compile-time + catalog + inspection; the live hole's compile-time behavior is closed; all four test projects green; docs synced **including the runtime reduction-rule requirement documented in `evaluator.md`**.
- The **runtime value-application is out of Slice 2's scope** — it is the readiness-plan runtime phase's job, governed by the documented requirement. (When it lands: the factor is applied, the skipped test gains a magnitude assertion, `evaluator.md` flips to Implemented — but that is *that* phase's done-condition, not Slice 2's.)

## Discovered during planning (guard 7 — plan-touches vs. design `sources-consulted`)

The design frontmatter's `sources-consulted` lists `business-domain-types.md`, `precept-language-spec.md §0.1`, `philosophy.md`, the survey, the investigation, and `ProofEngine.Qualifiers.cs`/`Operations.cs`. The plan additionally touches the surfaces below. **None are design gaps** — every one is named in the design's **Inventory** and/or **Doc-update enumeration**; they are simply not aggregated into the frontmatter `sources-consulted` field. Listed here for the mechanical set-membership check:
- `src/Precept/Language/Ucum/` (the UCUM atom catalog + `UcumExactFactor.cs`) and `UnitDimensionHelper.cs` — design Inventory § Catalog.
- `tools/Precept.LanguageServer/` (hover) — design Inventory § Tooling + Architecture propagation.
- `DiagnosticCode.cs` / diagnostic emission — design Inventory § Diagnostics.
- `test/Precept.Tests/Operations/PriceTimesQuantityTests.cs` — design Inventory § Tests.
- `proof-engine.md`, `catalog-system.md`, `diagnostic-system.md`, `language-server.md`, `evaluator.md` — design § Doc-update enumeration.

(No genuinely-new surface uncovered — the design's body acknowledged all of these; the only gap is frontmatter under-aggregation, not an unread surface.)

## Plan update protocol

- After **Phase 1** lands: mark Slice 2 done in the readiness-plan slice log; the runtime application is carried as a documented requirement (`evaluator.md`), not a tracked stub.
- When the **runtime phase (Phase 11)** opens: it implements the documented reduction-rule requirement and un-skips the runtime-magnitude assertion — as part of *that* phase, not a re-opening of Slice 2.
- If execution uncovers a surface the design didn't name: stop, flag it (design re-lock vs. discovered-during-planning), don't silently absorb.
