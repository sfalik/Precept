# Modifier-Name Axis-Overlap Plan — 2026-06-03

**Status**: Active
**Companion docs**: design [`modifier-name-axis-overlap-2026-06-03.md`](modifier-name-axis-overlap-2026-06-03.md) (Locked 2026-06-03 — Decision 1 retarget + Decision 2 analyzer, 6 build items, Falsifiers, Doc-update enumeration); grounding [`research/language/collection-element-aggregate-scope-syntax.md`](../../research/language/collection-element-aggregate-scope-syntax.md); the element-modifiers design [`collection-inner-type-value-modifiers-2026-06-02.md`](collection-inner-type-value-modifiers-2026-06-02.md) (its Supporting Decision 3 is retargeted by this plan).
**Scope gate**: removes the sole collection-vs-element modifier-name overlap (`notempty`) and enforces it structurally; makes the per-element-modifier binding catalog-generic (retires the `*Kind`-dispatch smell in `BuildElementValueBounds`).

Spike-branch mode: commits land directly on `spike/Precept-V2-Radical`; this doc is the execution hub. Pause for review at each slice boundary.

## Phase summary

| # | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | The semantic change end-to-end: `notempty` → string-only, generic element-bound binding, `mincount 1` discharge, sample migration, doc-sync — corpus green | Design items 1,2,3,5,6 | migration-rewrite (build-confirm) | M (2–3d) | Next |
| 2 | No-overlap Roslyn analyzer (structural guardrail) | Design item 4 / Decision 2 | none | S (0.5–1d) | Stub |

## Decisions captured

All from the Locked design — none re-litigated:
- **D1 (Direction A)** — `notempty` retargets `StringAndCollectionTypes` → `StringOnly`; collections use `mincount 1`; `notempty` routes to the element ("each element non-empty").
- **Generic binding** — the BLOCKER fix is to replace the `BuildElementValueBounds` per-`ModifierKind` switch with generic satisfaction-shape binding (probe-confirmed: no catalog enrichment; mirrors `FlagLowerBoundFromMeta`). Removes the `*Kind`-dispatch anti-pattern; binds `notempty` for free.
- **`mincount 1` discharge** — required, non-separable: `mincount 1` must discharge `.peek`/`.first`/`.last` `count > 0` (today it doesn't), else dropping collection-`notempty` reopens the empty-access totality hole (P11).
- **No advisory** (owner, 2026-06-03) — `set of string notempty` binds per-element silently; PRE0033 only on a genuine element-type mismatch (`set of integer notempty`). The meaning-inversion risk is watched by the design's Falsifier, not diagnosed.
- **D2 analyzer** — a no-overlap analyzer (no `ValueModifierMeta.ApplicableTo` spans both a collection and a scalar kind).
- **Follow-up, not this plan** — broader catalog-discipline sweep (enroll surviving modifier-`Kind` pipeline switches in `Precept0019`, or a "no consumer-side `*Kind` dispatch" analyzer).

## Open decisions

**None gate execution** — design is Locked. One **build-confirm** (resolve at build, not a design choice):
- **shopping-cart migration rewrite.** `event Create(... CatalogItems as set of string notempty ...)` (`shopping-cart.precept:94`) — was the `notempty` intent *cardinality* ("the supplied catalog has ≥1 item") or *per-element non-empty* (each id non-empty), or both? The field it feeds (`Catalog`, L23) is never element-read, so access-safety is unaffected either way. Confirm intent at build: `mincount 1` (cardinality only — most likely, matching the historical meaning) vs `notempty mincount 1` (both). Recommended: `mincount 1` unless the sample's intent was clearly per-element.

## Heavyweight phase block — Slice 1 (current): the semantic change

**Goal**: `notempty` is string-only and binds per-element via generic catalog-driven binding; collection cardinality is `mincount 1` (with `.peek`/`.first`/`.last` discharge preserved); the corpus is green.

**Items in scope**: design items 1 (retarget), 2 (genericize `BuildElementValueBounds`), 3 (`mincount 1` discharge), 5 (sample migration), 6 (doc-sync). Soundness-critical (type system + proof engine) → adversarial diff review before commit.

**Decisions required before kicking off**: none (the migration-rewrite is a build-confirm, above).

**Step-by-step execution** (enumerate → failing-test matrix first → gate → delegate to a fresh agent → adversarial review → integrate, per `/lifecycle-4-execute`):

1. **Failing-test matrix first (TDD)** in `test/Precept.Tests/`:
   - `set of string notempty` + an empty-string element (`default [""]` or an ingress) → element-bound rejection (per-element non-empty).
   - `set of integer notempty` → PRE0033 (`InvalidModifierForType`, element-type mismatch).
   - `field C as set of string mincount 1` + `.peek`/`.first` → compiles with **no** `count > 0` guard (discharge); `mincount 0` (or no mincount) → does **not** discharge.
   - Regression: the shipped Phase-1/2 element-bound tests (`maxlength`/`min`/`max`/flags) stay green — the genericization is behavior-preserving.
2. **Retarget** `notempty` in `Modifiers.cs`: `ApplicableTo` `StringAndCollectionTypes` → `StringOnly`; drop the `count` `ProofSatisfaction` (collection-only, now irrelevant); keep the `length > 0` satisfaction.
3. **Genericize `BuildElementValueBounds`** (`TypeChecker.cs:206-228`): replace `switch (modifier.Kind)` with a loop over `Modifiers.GetMeta(kind).ProofSatisfactions`, dispatching on the satisfaction DU shape `(Projection, Comparison, Bound)` — `Accessor("length") + DeclarationValue` → length slot; `SelfValue + DeclarationValue` → numeric slot (via `TryGetComparableModifierValue`, UCUM split preserved); `Accessor("length"|"count") > Constant(0)` → `NotEmpty=true`; `SelfValue + Constant` → `NumericFlags`. Reuse `TryReadLengthLiteral`/`TryGetComparableModifierValue`. Mirror `FlagLowerBoundFromMeta` (`ProofEngine.Intervals.cs:253-267`). Behavior-preserving for existing modifiers; `notempty` now sets `NotEmpty=true` → folds to `minlength 1` (`ProofEngine.Analysis.cs:452-453`).
4. **`mincount 1` discharge wiring**: in the modifier-satisfaction walk that proves `count > 0` (`ProofEngine.Strategies.cs` `SatisfactionCovers` ~293-361, the `DeclarationValue→null` path ~319-322), discharge `count > 0` when the field's `mincount` is a statically-literal `N ≥ 1` (read `TypedField.DeclaredMinCount`); keep the conservative null default for non-literal cases.
5. **Migrate** `shopping-cart.precept:94` per the build-confirm (`mincount 1` or `notempty mincount 1`).
6. **Doc-sync** (same commit) — see Doc-update obligations.
7. **Adversarial diff review** (soundness-critical): verify the genericization is behavior-preserving + binds `notempty`, the `mincount 1` discharge is sound (and only for literal `N ≥ 1`), the retarget reverses no unintended behavior, monotone (no new diagnostics on unchanged source beyond the intended `notempty` flip).

**Dependencies**: the element-modifiers feature (Phase 1/2 — the element-bound carrier + read/write plumbing + the `BuildElementValueBounds` switch this genericizes) — shipped (`4a003b64`, `fcbaaf71`). No upstream blockers.

**Exit criteria** (testable):
- The Slice-1 failing-test matrix passes; the shipped Phase-1/2 element-bound tests stay green (behavior-preserving genericization).
- `set of string notempty` rejects an empty-string element; `set of integer notempty` → PRE0033; `mincount 1` discharges `.peek`/`.first`/`.last`; `mincount 0`/absent does not.
- `dotnet test` green across all 4 projects; numeric-constant golden snapshots byte-identical; corpus green after the one-line shopping-cart migration.
- No `switch (modifier.Kind)` remains in `BuildElementValueBounds` (the `*Kind`-dispatch smell removed); adversarial review confirms catalog-generic binding.
- The doc-sync sites updated (below).

**Estimated effort**: M (2–3d) — retarget ~trivial; `BuildElementValueBounds` genericization ~1–1.5d (the meaty, behavior-preserving refactor + tests); `mincount 1` discharge ~0.5d; migration ~trivial; doc-sync ~0.5d.

**Doc-update obligations** (per CLAUDE.md routing; land in this slice):
- `docs/language/precept-language-spec.md` — §2.3/§2.4 `notempty` row + notes (string-only; per-element in inner-type position; `notempty`-on-collection no longer valid → `mincount 1`); §631 ("discharged by `notempty`" → "by `mincount 1`").
- `docs/language/collection-types.md` — Constraint Catalog + the ~10 per-kind "discharged by `notempty`" lines (L334-335, 353, 426, 775, 1051-1052, 1066, 1144) + the `sortedset`-rejection rationale (~L1492, 1539) → reword to "discharged by `mincount 1` / a non-empty guarantee."
- `docs/compiler/type-checker.md` — `notempty` per-type validation now string-only; `BuildElementValueBounds` genericized (switch → satisfaction-shape binding).
- `docs/compiler/proof-engine.md` — `mincount 1` now discharges `count > 0` access safety.
- `docs/Working/collection-inner-type-value-modifiers-2026-06-02.md` — Supporting Decision 3 retargeted (collection-coexistence half reversed, element half resolved); Falsifier 5 risk removed. *(Working doc.)*
- `docs/Working/bugs.md` — only if a relevant entry exists (none expected).

## Lightweight phase block — Slice 2 (next): no-overlap analyzer

**Goal**: a build-time guarantee that no `ValueModifierMeta.ApplicableTo` spans both a collection kind and a scalar kind — the catalog can never reintroduce a both-axes keyword.

**Scope**: design item 4 / Decision 2. A Roslyn analyzer (sibling of `Precept0011`/`Precept0027` — `RegisterCompilationAction`, `Error` severity), reusing `CatalogAnalysisHelpers` to read each `ValueModifierMeta`'s `ApplicableTo`, flagging any whose target set intersects both `CollectionTypes` and a scalar type. Assign the next free `PRECEPT00NN` id.

**Decisions required**: none. (Lands cleanly *after* Slice 1 — once `notempty` is string-only, the analyzer passes; if run before, it would flag `notempty`, so it must not precede the retarget.)

**Exit criteria**: the analyzer fails the build on a synthetic both-axes test modifier and passes on the real catalog (post-retarget); analyzer-project tests green; `dotnet build`/`dotnet test` green.

**Effort**: S (0.5–1d).

**Status**: Stub — TBD pending Slice 1 (the retarget must land first, else the analyzer fails the build).

**Doc-update obligations**: `docs/compiler/diagnostic-system.md` (the new `PRECEPTxxxx` analyzer rule, if analyzer rules are catalogued there) + the analyzer project's README/tests.

## Definition of done

- Both slices' exit criteria met; design Acceptance criteria pass.
- `notempty` is string-only and binds per-element; collections use `mincount 1` (with discharge preserved); `BuildElementValueBounds` is catalog-generic (no `*Kind` switch); the no-overlap analyzer is live and green.
- Corpus green; full `dotnet test` green; doc-syncs landed; the element-modifiers design's Decision 3 retargeted.

## Discovered during planning

Plan-touch sources not verbatim in the design's `sources-consulted` (plan-touches ⊆ design-sources check) — all named in the design's Inventory/Rules and grounded by the lock-time probe/review; surfaced for completeness:
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` (`SatisfactionCovers`) — the `mincount 1` discharge site (design Rule 3 cited the `count > 0` obligation + the `DeclarationValue→null` path; this is the function to edit).
- `src/Precept.Analyzers/CatalogAnalysisHelpers.cs` + the `Precept00NN` analyzer file — Slice 2 (design Decision 2 named the analyzer + the `Precept0011`/`0027` pattern; the helper is the reuse surface).
- `test/Precept.Tests/**` + `test/Precept.Analyzers.Tests/**` — new test fixtures (Slice-1 matrix + the analyzer test).
None requires design re-lock — the design's Inventory scopes each surface.

## Plan update protocol

After each slice: check off in this doc, mark the phase row ✅ with the commit hash, pause for review at the slice boundary (no auto-advance). When Slice 1 lands, promote Slice 2 from stub to heavyweight. Surface any mid-build design gap as an Open Decision routed to `/lifecycle-2-design` — do not patch the Locked design inline.
