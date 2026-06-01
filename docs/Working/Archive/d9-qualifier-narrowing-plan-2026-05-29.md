# D9 Open-Field Qualifier Narrowing — Build Plan — 2026-05-29

> **Archived 2026-05-31** — D9 build plan; all slices shipped and the carried-forward W-C `.basis`-cancellation debt closed (`fd147dc2`). Historical reference; do not edit.

**Status**: Active
**Companion docs**: design `docs/Working/d9-qualifier-narrowing-design.md` (Locked 2026-05-29); analysis `docs/Working/d9-qualifier-narrowing-analysis-2026-05-28.md`; readiness plan `docs/Working/compiler-readiness-plan-2026-05-24.md` (Phase 6 — link this plan from the Phase 6 D9 row).
**Scope gate**: delivers the spec § D9 promise (guard-driven discrete-equality qualifier narrowing) repo-wide; unblocks the W-B/W-C composite-period premises (composite `.dimension`/`.basis` guards both compile and narrow).

## Phase summary

| Slice | Goal | Decisions | Effort | Parallel? | Status |
|---|---|---|---|---|---|
| **S1** | Core narrowing mechanism on identity axes needing no catalog change (currency/unit/from/to) | D1, D2, D3 (locked) | M (2–3d) | critical path | ✅ `4e5f837c` |
| **S2a** | Partition-validation prerequisite: `ReturnsQualifier` on `.dimension`, partition-aware dimension-literal validation (PRE0053 fix) | D4 (locked) | M (2d) | **‖ S1** (file-disjoint) | ✅ `fc10db45` |
| **S2b** | `.dimension` discharge wiring (quantity/price/uom/period) + period `.dimension` derivation + CONCERN-2 regression | D4, D6, D7 | S–M (1–2d) | after S1 ∧ S2a | ✅ `f662aad3` |
| **(0)** | Temporal-denominator price parsing (`price in 'USD/hours'`) — § D15 NodaTime vocabulary; prerequisite surfaced building S3 | — | S | prereq | ✅ `5d071ffa` |
| **S3** | Period `.basis` narrowing — **D14 subset assignment** discharge (D15 single-basis *cancellation* is NOT here — it is the W-C tightening) | D5 (locked) | M (2d) | after S1 (‖ S2b, coordinated) | ✅ `5d071ffa` |
| **S4** | Diagnostics narrowed-vs-required + sample + doc-sync (§ D9 `$eq:` mechanism rewrite) | — | S–M (1–2d) | tail; draft ‖, finalize last | ✅ `5949de4b` (spec) `04bf5f0e`+`75b83980` (diagnostics) `75b83980` (sample) |

**S4 as-built notes.**
- § D9 `$eq:`/`StaticValueKind`/`ApplyNarrowing` fiction rewritten to the real proof-engine fact mechanism (`5949de4b`).
- Narrowed-vs-required wording landed on the compatibility, qualifier-chain, and assignment diagnostics (`04bf5f0e`, `75b83980`).
- Sample `samples/contractor-invoice-settlement.precept` (`75b83980`) — open-money currency narrowing + temporal-denominator pricing.
- **Caught + fixed a self-inflicted false green** (`01ed818f`): the (0) cancellation tests used the type-checker-only `CheckExpectingClean` helper, hiding a proof-stage failure. Rewritten to full-pipeline `Compiler.Compile`.

**Discovered gap (owner decision — NOT fixed).** Per § D15 `period in 'hours'` should cancel `price in 'USD/hours'`, but it does **not**: the `price × period` `QualifierChainProofRequirement` resolves the period's `TemporalDimension`, and a declared `TemporalUnit` basis does not surface one on that path. `period of 'time'` is the working form. Pinned by `PricePerHours_TimesPeriodInHours_NotYetProven`. Likely a small `ResolveQualifierOnAxis` fallback (TemporalUnit → derived TemporalDimension) but soundness-relevant — flag for a dedicated slice.

## Parallelization (answering "can we run parallel work in this phase?")

Yes — there is one clean parallel pairing and two coordinated ones. The constraint is **file overlap on the proof-engine discharge surface**, not logical dependency.

- **S1 ‖ S2a — genuinely parallel, disjoint files.** S1 is proof-engine (`QualifierNarrowingConstraint`, `ExtractGuardLeafConstraints` seed, `TryQualifierGuardNarrowingProof`, `ProofLedger`/`ProofEngine.cs`). S2a is type-checker + catalog (`Types.cs` accessor `ReturnsQualifier`, dimension-literal partition validation, `literal-system`/`type-checker` docs). They touch **no common file**, so they run concurrently with no merge risk. S2a is also a *prerequisite* for S2b (it makes `period.dimension == 'date'` compile) — doing it alongside S1 means S2b can start the moment S1 lands. **This is the recommended parallel pair.**
- **S2b ‖ S3 — logically independent, file-coordinated.** Once S1 lands, the `.dimension` discharge and the `.basis` discharge are independent axes (they don't interact). But both add arms to the same discharge strategy / `ProofEngine.Qualifiers.cs`. They can be parallelized in worktrees, but expect a small merge on the discharge dispatch. Lower-friction default: sequence them (S2b then S3) unless schedule pressure justifies the worktree split.
- **S4 drafting overlaps the tail.** Once S1 fixes the mechanism's shape, the doc-sync prose (spec § D9 rewrite, proof-engine.md strategy entry) and the realistic sample can be drafted in parallel with S2b/S3, then finalized after all axes land.
- **Per-slice soundness tests are authored with their slice** (not a separate parallel track) — that is the point of carrying them per-slice.

Critical path: **S1 → S2b/S3 → S4**, with **S2a riding alongside S1**. On a single-reviewer cadence the wall-clock win is mainly S2a-alongside-S1; the S2b‖S3 split only pays off if two execution streams run at once and the discharge-dispatch merge is accepted.

## Decisions captured

All seven design decisions are **Locked 2026-05-29** (see design doc § Decisions): D1 dedicated `QualifierNarrowingConstraint` fact (not extending decimal-only `GuardConstraint`); D2 dedicated discharge strategy, `ResolveQualifierOnAxis` stays declaration-pure; D3 axis-appropriate soundness gates (equality for identity axes, **D14 subset for basis** — the N2 correction); D4 catalog-driven partition validation (accessor-declared tag, dispatcher's existing context pass, no `TypeKind` switch, no `ClosedSetValidator` contract change); D5 `.basis` as discrete component-set, subset discharge; D6 period `.dimension` derivation via shared `ResolvePeriodDimension`; D7 `datetime` compare-but-inert, unitofmeasure `.dimension` in scope.

Re-grounding (2026-05-29): cross-step reassignment-invalidation (was the design's only open question, CONCERN-1) is **resolved** — it rides the shipped `ReassignedBefore` substrate (BUG-014/015/016); no new mechanism.

## Open decisions

**None blocking.** All design decisions are locked; the only execution-time choice is the S2b‖S3 sequencing (parallel-worktree vs sequential), which is a scheduling call, not a design gate — default sequential unless two streams are available.

## S1 — Core mechanism on identity axes (HEAVYWEIGHT)

**Goal.** `when X.<axis> == 'literal'` narrows an open `money`/`quantity`/`price`/`exchangerate` field so a downstream constrained operation discharges its qualifier obligation — soundly — for the axes that already carry `ReturnsQualifier` (currency, unit, from, to). No catalog change.

**In scope.** Design Inventory items 1–3 + assignment-site consult; the identity-axis subset of the § D9 marker table.

**Decisions required before kickoff.** None — D1/D2/D3 locked.

**Step-by-step.**
1. `src/Precept/Pipeline/ProofEngine.cs` (or a new `ProofEngine.QualifierNarrowing.cs`): add `record QualifierNarrowingConstraint(string Field, QualifierAxis Axis, string Value)`.
2. `ProofEngine.Strategies.cs`: a parallel branch extractor mirroring `ExtractGuardBranchesCore` AND/OR (OR → branch-set union; AND → cross-product) typed to `QualifierNarrowingConstraint`.
3. `ProofEngine.Strategies.cs` `ExtractGuardLeafConstraints`: seed arm matching `TypedBinaryOp{ Left: TypedMemberAccess{ ResolvedAccessor: FixedReturnAccessor{ ReturnsQualifier ≠ None } }, Op: Equals, Right: TypedTypedConstant }` → emit the fact. Equals-only; literal-RHS-only (negation/field-to-field emit nothing — D3).
4. `ProofEngine.Qualifiers.cs` (or adjacent): `TryQualifierGuardNarrowingProof(obligation, semantics)` — pulls the guard from `obligation.Context` (as `TryGuardInPathProof` does), requires the fact on **every** branch (all-branches-OR, D3), discharges iff `satisfies_equality(v, w)` for identity axes. **Consult `obligation.ReassignedBefore`**: if the narrowed field was reassigned earlier in the chain, do not discharge (rides shipped substrate — no new mechanism).
5. `ProofEngine.cs` dispatch: insert `TryQualifierGuardNarrowingProof` adjacent to `TryQualifierCompatibilityProof`; assert dispatch order in tests.
6. `TypeChecker.Expressions.AssignmentQualifiers.cs`: assignment-site consults the discharge result for open-field sources (Site A — reads the ProofEngine outcome, does not thread guard context into `ResolveAssignmentQualifierAxis`, preserving D2).
7. Tests (`test/Precept.Tests/ProofEngine/QualifierNarrowingTests.cs`, new): the identity-axis acceptance examples + the per-slice soundness rows — value-mismatch reject, OR-collapse reject, negation-narrows-nothing, reassignment-invalidation (set X mid-body), all-branches.

**Dependencies.** Numeric narrowing (F-LANG-BIZ-08) — structural template, shipped. `ReassignedBefore` substrate — shipped (`d3ed0a2d`).

**Exit criteria.**
- `precept_compile`: `when Payment.currency == 'USD' -> set UsdBalance = UsdBalance + Payment` (open money) compiles and discharges; `-> set EurBalance = … Payment` under the same guard is rejected (PRE0141).
- `when X.currency == 'USD' or X.currency == 'EUR'` → assignment to a USD-only field rejected (OR-collapse); `when X.currency != 'JPY'` → no discharge; `when … == 'USD' -> set X = otherOpen -> set Usd = … X` → no discharge (reassignment-invalidation).
- The same shape proves for `quantity.unit`, `exchangerate.from`/`.to`, `price.currency`/`.unit`.
- `dotnet test` green (incl. untouched numeric-narrowing + W-A tests); `dotnet build` 0 warnings; `Precept0027` diagnostic-coverage analyzer clean.
- **Pause for review at slice boundary.**

**Effort.** M (2–3d).

**Doc-update obligations.** None canonical in S1 (mechanism prose lands in S4); inline xmldoc on the new record/strategy only — transient-ref-free.

## S2a — Partition-validation prerequisite (HEAVYWEIGHT; runs ‖ S1)

**Goal.** Make `period.dimension == 'date'` (and the temporal partition generally) **compile** — the prerequisite for S2b's narrowing — via catalog-driven partition-aware dimension-literal validation, and give the `.dimension` accessors the `ReturnsQualifier` axis they currently lack.

**In scope.** Design Inventory items 4–5 (the `ReturnsQualifier`-on-`.dimension` + partition validation halves); the folded-in PRE0053 fix.

**Decisions required before kickoff.** None — D4 locked.

**Step-by-step.**
1. `src/Precept/Language/Types.cs`: add `ReturnsQualifier` to the `.dimension` accessors — quantity/price/unitofmeasure → `Dimension`, period → `TemporalDimension`. (These are catalog edits; the existing currency/unit/from/to accessors already carry their axis.)
2. Catalog-declared **partition tag** for dimension accessors (UCUM vs Temporal) — the selection signal, so the type checker reads the partition from accessor metadata, never a `TypeKind` switch (D4/G2).
3. Dimension typed-constant validation becomes partition-aware: route through the dispatcher's existing `targetType`/`context` pass (the NodaTime/Ucum/Quantity arms already consume it; the `ClosedSet` arm currently drops it). Temporal partition set `{date, time, datetime}` alongside the UCUM partition. No change to `ClosedSetValidator.Validate`'s context-free contract (D4/G1); `stateref` is the precedent for context-dependent validation.
4. Cross-partition rejection: `quantity.dimension == 'date'` and `period.dimension == 'mass'` are compile errors (reuse `InvalidDimensionString` 77 unless a probe shows a gap — A8/A9).
5. **CONCERN-2 regression tests** — adding `ReturnsQualifier` to `.dimension` flips two existing consumers on: hover (`RichHoverFactory.cs:995`) and interpolation-slot resolution (`AssignmentQualifiers.cs:694`). Assert the new behavior is correct (dimension axis resolves) and does not mis-resolve a non-dimension slot.

**Dependencies.** None on S1 (disjoint files) — that is why it parallelizes. S2b depends on this.

**Exit criteria.**
- `precept_compile`: `period.dimension == 'date' | 'time' | 'datetime'` compiles; `quantity.dimension == 'date'` → cross-partition error; `period of 'datetime'` still rejected (unchanged).
- Hover on `X.dimension` resolves the Dimension/TemporalDimension axis; interpolation `'{X.dimension}'` slot resolves without mis-resolving a non-dimension slot.
- `dotnet test` green; 0 warnings; analyzer clean. **Pause for review.**

**Effort.** M (2d).

**Doc-update obligations.** `docs/compiler/type-checker.md` (partition validation + `ReturnsQualifier`-on-`.dimension`); `docs/compiler/literal-system.md` (dimension partition as 2nd context-dependent case alongside `stateref`). (Spec § D9 / § dimension prose finalized in S4.)

## S2b — `.dimension` discharge wiring (✅ `f662aad3`)

**Status: Complete.** Goal: wire the discharge for the `.dimension` axis (quantity/price/uom/period) onto S1's mechanism, including period `.dimension` derivation (D6) and `datetime` compare-but-inert (D7). Decisions: D4/D6/D7 (locked).

**As-built notes (enumeration surfaced these against the locked plan):**
- The quantity/price/uom `.dimension` assignment discharge was **already delivered by S1** (axis-generic `QualifierCompatibility`) — no new work needed there. The only genuine gap was the **period** `DimensionProofRequirement`.
- The real period discharge site is `date ± period` / `time ± period` (`Operations.cs` `DatePlusPeriod`/`TimePlusPeriod` …), **not** `period + period` (which generates no dimension obligation). The discharge now consults the guard narrowing fact via `NarrowedPeriodDimensionFromGuard` mapping the narrowed spelling to `PeriodDimension` and feeding the existing `== Any || == Required` check; `'datetime'` → `Datetime` stays inert for free.
- **For S4:** the design's acceptance example #3 (`set D = D + X`, D an open *period*) is imprecise — `period + period` carries no dimension obligation. Correct it to `date/time ± open-period` when rewriting § D9.
- **Deferred (not this slice):** a same-axis AND-contradiction guard (`== 'date' and == 'time'`) discharges order-dependently — dead-code-only (unsatisfiable guard), parity with numeric narrowing's first-match; resolves when qualifier-contradiction detection (PRE0082) is built.

## S3 — Period `.basis` subset narrowing (STUB)

**Status: ✅ Complete `5d071ffa`.** Delivered `.basis` discrete-component-set narrowing: guard RHS canonicalized at the **type checker** (G4) via the W-A canonicalizer; **D14 subset discharge** (N2: `components(narrowed) ⊆ components(required)`) on the period **assignment** path; open-period basis (N1: the guard supplies the value). Decisions: D5 (locked).

**As-built notes (enumeration/probe surfaced these against the design prose):**
- The basis discharge target is **D14 composite-basis assignment** (`set <period in 'a+b'> = openPeriod`), the relation the design actually specifies (subset) — **not** the `price × period` cancellation the D5/N1 prose used as its example. That cancellation site is *dimension-level* (TemporalDimension), the same imprecision class as the S2b example-#3 carry-forward. Correct the D5/N1 example in the S4 § D9 rewrite.
- Building it required making the dormant D14 assignment constraint **real for periods** (the assignment-qualifier path omitted the `TemporalUnit` axis entirely) — i.e. the minimal "one piece of W-C." Period **literals** now derive their own basis so the tightening is provable on default/literal paths.
- **D15 single-basis *cancellation*** (`period in 'hours + minutes'` must NOT cancel `price in 'USD/hours'`) is a **separate tightening — W-C**, NOT this slice. The cancellation chain still discharges at dimension granularity (under-enforces declared-operand mismatches; pre-existing). See the (0) finding.
- **(0)** temporal-denominator price parsing (`price in 'USD/hours'`) was a prerequisite found en route — without it the design's own example couldn't be declared (PRE0075). Shipped in the same commit.
- 2 NITs carried forward: duplicated subset impl (type-checker `Components` vs proof-engine string-split); period literals now always carry a `TemporalUnit` qualifier (safe today; revisit if a new path resolves `TemporalUnit` off a non-field period expression).

## S4 — Diagnostics + samples + doc-sync (STUB)

**Status: Stub — TBD pending S1–S3.** Goal: narrowed-vs-required wording on PRE0141/0114/0113; realistic sample(s); doc-sync per the design's doc-update enumeration — `business-domain-types.md` § D9 mechanism rewrite (replace the unbuilt `$eq:`/StaticValueKind text), `proof-engine.md` (new strategy + fact), `type-checker.md`, `literal-system.md`, `diagnostic-system.md`, `catalog-system.md` if a partition accessor/diagnostic lands. Effort: S–M (1–2d). Drafting overlaps S2b/S3; finalize last.

## Discovered during planning

- New test files (`QualifierNarrowingTests.cs` and per-axis siblings) — additive, not in the design's `sources-consulted`; standard test-fixture growth, not a design gap.
- `src/Precept/Language/TypedConstantValidation.cs` + `ClosedSetValidator.cs` — added to the design's `sources-consulted` during the 2026-05-29 re-grounding; the plan touches them in S2a.

## Definition of done

Spec § D9 narrowing works for all listed axes (money.currency, quantity.unit/dimension, period.basis/dimension, price.{currency,unit,dimension}, exchangerate.{from,to}); the three § D9 acceptance examples compile-and-prove; every soundness row (value-mismatch, OR-collapse, negation, field-to-field, cross-branch, cross-step reassignment, declared-override, negated-conjunction, composite-datetime, Any-discharge, cross-partition) has a fail-before/pass-after test; `period.dimension == 'date'` compiles; the design's doc-update enumeration is satisfied; full suite green, 0 warnings, analyzer clean. The W-B/W-C composite-period premises are unblocked.

## Plan update protocol

Update the phase-summary status column as each slice lands; pause for owner review at each slice boundary before advancing (no auto-advance). If a slice surfaces a design gap (a source the design didn't cite, or a soundness row that can't be made to pass), stop and re-lock the design rather than patching in-flight.
