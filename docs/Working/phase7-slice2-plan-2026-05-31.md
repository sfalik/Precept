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
| 1 | Buildable-now — catalog metadata + proof-engine surfacing + hover + diagnostics + tests + doc-sync | 6 | 0 (design locked) | **M (~2–3 days)** | **Active** |
| 2 | Runtime reduction rule — evaluator applies the exact-rational factor | 1 | 0 (gated on runtime existing) | S–M (~0.5–1 day once runtime exists) | Stub — deferred to the runtime phase |

## Decisions captured

- **Design Decisions 1–4 locked 2026-05-31** (`price-cross-unit-cancellation-design-2026-05-31.md`): (1) extend D8 auto-convert to price cancellation, target-directed; (2) **allow-all-with-surfacing** (no exactness gate — `in↔ft` works; exact/approximate surfaced in inspection); (3) exclusion = absolute positions only (→ Slice 4); (4) conversion metadata lives in the UCUM atom catalog.
- **Owner P8 affirmation (2026-05-31)**: inspection-surfacing, backed by the `maxplaces`-exactness type guarantee, satisfies P8's "visible in the type system" clause; no type-level approximate-`money` marker built now (the design's 2nd Falsifier watches the future case).
- **No open language-surface decisions remain for execution** — the surface is fully locked.

## Open decisions

- **None gate Phase 1.** One low-stakes execution-time choice (not a gate): the exact wording of the hover/inspection "approximate" label — settle in execution, doc in `language-server.md`.
- **Phase 2 gate**: the runtime evaluator must exist (it is currently a stub). Tracked as readiness-plan **Phase 11** (runtime gate). No design decision pending — purely a dependency.

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

---

## Phase 2 (lightweight stub) — Runtime reduction rule

**Goal**: the evaluator applies the exact-rational factor `k = ScaleToBase(u_q)/ScaleToBase(u_p)` in `decimal` (rounding at `maxplaces`), so cross-unit `price × quantity` produces the correct money value — making the compile-time `"Proved"` literally true.

**Scope**: the reduction rule from the design's § Semantic Rules; the un-skipped test gains a runtime-magnitude assertion (`4.00 USD/ft × 36 in → 12.00 USD`).

**Decisions required**: none (design locked) — gated only on the runtime evaluator existing.

**Effort**: S–M (~0.5–1 day once the runtime exists; `UcumExactFactor.Multiply`/`Divide` already preserve rationality).

**Doc-update obligation**: `docs/runtime/evaluator.md` — the conversion reduction rule (already documented as a pending obligation; mark Implemented when built).

**Status**: Stub — TBD pending the runtime phase (readiness-plan Phase 11). The obligation is documented in the locked design (§ Semantic Rules) + `evaluator.md`; the skipped test marks the gap until then.

---

## Definition of done

- **Phase 1 done** → cross-unit price × quantity is correct and surfaced at compile-time + catalog + inspection; the live hole's compile-time behavior is closed; all four test projects green; docs synced. (This is the executable scope now.)
- **Phase 2 done** (when the runtime ships) → the factor is actually applied; the test asserts the correct magnitude; `evaluator.md` flips to Implemented.
- **Slice 2 fully done** when both phases complete. Until Phase 2, Slice 2 is "compile-time + catalog + inspection complete; runtime value-application pinned."

## Discovered during planning (guard 7 — plan-touches vs. design `sources-consulted`)

The design frontmatter's `sources-consulted` lists `business-domain-types.md`, `precept-language-spec.md §0.1`, `philosophy.md`, the survey, the investigation, and `ProofEngine.Qualifiers.cs`/`Operations.cs`. The plan additionally touches the surfaces below. **None are design gaps** — every one is named in the design's **Inventory** and/or **Doc-update enumeration**; they are simply not aggregated into the frontmatter `sources-consulted` field. Listed here for the mechanical set-membership check:
- `src/Precept/Language/Ucum/` (the UCUM atom catalog + `UcumExactFactor.cs`) and `UnitDimensionHelper.cs` — design Inventory § Catalog.
- `tools/Precept.LanguageServer/` (hover) — design Inventory § Tooling + Architecture propagation.
- `DiagnosticCode.cs` / diagnostic emission — design Inventory § Diagnostics.
- `test/Precept.Tests/Operations/PriceTimesQuantityTests.cs` — design Inventory § Tests.
- `proof-engine.md`, `catalog-system.md`, `diagnostic-system.md`, `language-server.md`, `evaluator.md` — design § Doc-update enumeration.

(No genuinely-new surface uncovered — the design's body acknowledged all of these; the only gap is frontmatter under-aggregation, not an unread surface.)

## Plan update protocol

- After **Phase 1** lands: mark it done in the readiness-plan Slice 2 row; the slice's runtime portion (Phase 2) stays pinned.
- When the **runtime phase** opens: promote Phase 2 from stub to heavyweight; un-skip the runtime-magnitude assertion.
- If execution uncovers a surface the design didn't name: stop, flag it (design re-lock vs. discovered-during-planning), don't silently absorb.
