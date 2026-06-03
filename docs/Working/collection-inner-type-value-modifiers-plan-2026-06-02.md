# Collection Inner-Type Value Modifiers — Plan — 2026-06-02

**Status**: Active
**Companion docs**: design [`collection-inner-type-value-modifiers-2026-06-02.md`](collection-inner-type-value-modifiers-2026-06-02.md) (Locked 2026-06-02 — Decisions 1–4, 5 Semantic Rules, 8 Acceptance criteria, Doc-update enumeration, Build-sequencing & reuse principles); [`relational-rules-and-bounds-plan-2026-06-02.md`](relational-rules-and-bounds-plan-2026-06-02.md) § Slice 3 (this plan executes that slice — cross-link, not duplicate); [`bugs.md`](bugs.md) (BUG-019 residual gap 1 — the 3 blocked samples).
**Scope gate**: closes BUG-019 residual gap 1 — a value read out of a collection (`.peek`/`.first`/`.last`/`.min`/`.max`, quantifier binding) into a bounded destination becomes provable. Phase 1 alone unblocks the 3 red samples; full-parity completes the locked design.

Spike-branch mode: commits land directly on `spike/Precept-V2-Radical`; this doc is the execution hub. Pause for review at each phase boundary.

## Phase summary

| # | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | String-length element bounds end-to-end (grammar → typing → carrier → string read-reach → string write-obligation → 3 samples green) | Design Acceptance 1–4, 8; closes BUG-019 residual gap 1 | none (design Locked) | M (3–4d) | Next |
| 2 | Numeric / quantifier / qualified parity (the rest of full-parity) | Design Acceptance 5–6; Falsifier-2 watch-cell | none (design Locked); build-confirm: qualified normalization | M (2–3d) | Stub |
| — | Deferred (grammar slot reserved): two-axis bounds on `P`/`K`/`V` | Design § Out-of-scope | — | — | Deferred |

## Decisions captured

All from the Locked design — none re-litigated here:
- **D1** — declared inner-type modifier syntax (`queue of string maxlength 200`), not inferred-from-write-sites.
- **D2** — full scalar parity (any modifier the inner scalar type accepts as a field), not length-only. *(Sequenced string-first per the design's Build-sequencing section; parity is completed in Phase 2.)*
- **D3** — `notempty` in inner-type position = per-element non-empty string, distinct from collection `notempty`/`mincount`; the two coexist.
- **D4** — field-reference element bounds inherit BUG-020's resolution; literal element bounds (the 3 samples) don't depend on it.
- **Catalog + code reuse (verified at lock, against source):** reuse `ValidateValueModifiers`/`Modifiers.ApplicableTo` (element `TypeKind` as subject), `TryLengthContainmentProof` + the numeric interval path, the `TypedElementType` DU with `TypedField`'s bound vocabulary, PRE0033/PRE0135/OutOfRange. New code = parser position + element extraction + 3 read-reach plug-ins.

## Open decisions

**None gate execution** — the design is Locked. Two **build-confirm** items (resolved direction, verify at build; not design choices — from the design's "Resolved at lock" section):
- **Write-site rejection code** — reuse `LengthBoundViolation`/`OutOfRange`; `diagnostic-system.md` touched only if the build surfaces a concrete need for an element-specific message.
- **`default [...]` element-literal check** — rides the existing default-against-rules path (spec §0.6 item 11); verify at build.
- **(Phase-2 watch, not a gate)** money/quantity element bound read via `.min`/`.max` into a qualified destination — the least-tested parity cell (`IntervalOfNarrowed` currency/unit normalization). If it misbehaves, the string-first split already de-risks it (the 3 samples are string-only and shipped in Phase 1).

## Heavyweight phase block — Phase 1 (current): string-length element bounds

**Goal**: a collection's string inner type can carry `maxlength`/`minlength`; those bounds are validated per element, proven at string write-sites, and carried at string read-sites — so the 3 blocked samples compile clean and BUG-019 residual gap 1 closes.

**Items in scope**: Design Acceptance criteria 1 (3 samples clean), 2 (typing reject), 3 (write-site reject), 4 (string read discharges), 8 (no regression). String/length modifiers only (`maxlength`, `minlength`, and `notempty` in its per-element-string meaning, D3).

**Decisions required before kicking off**: none (design Locked).

**Step-by-step execution** (enumerate → failing-test matrix first → gate → delegate to fresh worktree agent → adversarial review → integrate, per `/lifecycle-4-execute`):

1. **Enumerate + probe** the element-modifier input space on the freshly-built compiler (grammar accept/reject, typing per-element, write-site, read-site, the 3 samples) — confirm current behavior (element read = unbounded today).
2. **Failing-test matrix first (TDD)** in `test/Precept.Tests/`:
   - Typing: `set of integer maxlength 5` → PRE0033 at the element-modifier span; `queue of string maxlength 200` → clean.
   - Write-site: `queue of string maxlength 5` + `enqueue Q E.Name` with `E.Name` unbounded / `maxlength 10` → `LengthBoundViolation`; control `E.Name maxlength 5` → clean.
   - Read-site: `queue of string maxlength 200` + `set Dst(maxlength 200) = Q.peek` → clean; narrow `Dst` to `maxlength 100` → re-emit.
   - Regression: a collection inner type with no modifier behaves as before (element read unbounded); corpus green.
3. **Grammar / parser**: accept a trailing value-modifier list on `CollectionInnerType`, after the existing type-qualifier. Reconcile the collection-level production so the qualifier *and* modifier list both bind to the inner type (spec §2.3 currently binds `TypeQualifier?` at the collection level) — `set of money in 'USD' nonnegative` binds both to the element. (`src/Precept/Pipeline/Parser.Types.cs` — collection-inner parse path.)
4. **Type checker**: `BuildTypedElementType` (`TypeChecker.cs:122-151`) extracts the element modifier set and per-element-validates via the existing `ValidateValueModifiers`/`IsTypeApplicable` (`TypeChecker.Validation.Modifiers.cs`) with the element `TypeKind` as subject (Rule 1, PRE0033 per element). **No parallel validator.**
5. **Carrier**: add the `DeclaredValueBounds` companion (length fields first) to the `TypedElementType` DU subtypes (`SemanticIndex.cs:349-372`), reusing `TypedField`'s bound vocabulary (`DeclaredMinLength`/`DeclaredMaxLength`, 396-406). **DU extension, not nullable-flatten, not a parallel bound representation.**
6. **String read-reach**: fill the dead `MemberAccessStringLengthInterval` hook (`ProofEngine.Lengths.cs:217-223`) to return `band(maxlength/minlength)` from the receiver's element bound; the routing arm (121-122) already directs element reads here, and the existing `TryLengthContainmentProof`/`StringLengthIntervalOf` consume it unchanged (Rule 4, string half).
7. **String write-site obligation**: generate a per-element `LengthContainment` obligation at the element-introducing actions, **sharing the scalar set-action requirement construction** (`Actions.cs`) parameterized on bound-source (field bound vs element bound) — **do not fork a parallel generator**. Derive the governed-action set from `ActionMeta` (growing/establishes-value effect) so `EnqueueBy`/`AppendBy` value axes are covered without a hand list (Rule 3, string half).
8. **Samples**: add the element `maxlength` to the queue/list inner type in `it-helpdesk-ticket`, `restaurant-waitlist`, `utility-outage-report` so the `.peek`/accessor reads discharge (authorized by this plan).
9. **Adversarial diff review** (soundness-critical: type system + proof engine) before commit — verify catalog+code reuse (no parallel data/validator/carrier; DU-subtype dispatch), the read-reach/ingress matched-pair soundness, and that the change is monotone (strictly fewer diagnostics on unchanged source).

**Dependencies**: BUG-019 string-length interval machinery (`StringLengthIntervalOf`, `TryLengthContainmentProof`) — already shipped (4e4e2961). The `TypedElementType` DU — already shipped. No upstream blockers.

**Exit criteria** (testable):
- The Phase-1 failing-test matrix passes; the 3 blocked samples compile clean (`SampleCompilesCleanTests` green — red count 3 → 0).
- `set of integer maxlength 5` emits PRE0033 at the element-modifier span; an over-bound string write-site rejects; a same-or-wider-bounded read discharges; a narrowed destination re-emits.
- `dotnet test` green across all 4 projects; numeric-constant golden snapshots byte-identical; no new diagnostics on unchanged collection source (monotone check).
- Adversarial review confirms reuse (no parallel modifier list/validator/carrier; shared obligation generator; DU-subtype dispatch).
- BUG-019 residual gap 1 flipped to closed in `bugs.md` (BUG-019 → Fixed).

**Estimated effort**: M (3–4d) — grammar/parser ~1d; typing+carrier ~1d (high reuse); string read-reach + write-obligation ~1d; samples + tests + review ~0.5–1d.

**Doc-update obligations** (per CLAUDE.md routing; land in this phase):
- `docs/language/precept-language-spec.md` — §2.3 grammar (restructure the collection-level production so qualifier + modifier list bind to the inner type); §2.4 note modifiers may sit in inner-type position; modifier↔type table note (consulted per-element); remove the §0.1-area residual-gap sentence (~line 249) once the 3 samples are green.
- `docs/language/collection-types.md` — Inner Type System: new "Element value modifiers" subsection (length first); Constraint Catalog: distinguish element value bounds from cardinality; update the "Element-level constraints → use a quantifier" callout (772) to add the inner-type-modifier path; cross-ref the `capacity`-rejection (different axis).
- `docs/compiler/proof-engine.md` — Strategy 9 member-access row (1702): element-type length bounds now read (no longer "always unbounded"); the string write-site element obligation.
- `docs/compiler/type-checker.md` — `BuildTypedElementType` element-modifier extraction + per-element PRE0033 validation; accessor read-reach typing.
- `docs/tooling/language-server.md` / `docs/tooling/mcp.md` — hover/semantic-token/attribution + type-DTO projection of the element bound (string half).
- `docs/Working/bugs.md` — close BUG-019 residual gap 1; BUG-019 → Fixed.
- NOT in this phase: numeric/quantifier/qualified projection (Phase 2); `diagnostic-system.md` (only if a new code lands).

## Lightweight phase stub — Phase 2 (next): numeric / quantifier / qualified parity

**Goal**: complete full scalar parity — numeric (`min`/`max`/`nonnegative`/…), quantifier-binding, and qualified (`money`/`quantity`) element bounds participate in typing, write-proof, and read-reach.
**Scope**: numeric read-reach (new `TypedMemberAccess` arm in `ProofEngine.Intervals.cs:IntervalOfNarrowed`); quantifier-binding interval seeding (`x` carries `band(m)` in the predicate); numeric/flag write-site obligations (shared generator); the money/quantity normalization watch-cell (Design Falsifier 2); Design Acceptance 5 (numeric parity read) + 6 (quantifier binding carries bound). Acceptance 7 (runtime element governance) is **gated on the runtime stub** — contract-only until the runtime lands; note, don't block.
**Decisions required**: none (design Locked); build-confirm the qualified-normalization cell (if it misbehaves, it was already de-risked by shipping string-first).
**Effort**: M (2–3d).
**Status**: Stub — TBD pending Phase 1 completion (the grammar/carrier/typing foundation and the shared write-obligation generator are prerequisites).

## Definition of done

- Phase 1 + Phase 2 exit criteria met; Design Acceptance 1–6 and 8 pass (7 noted as runtime-gated).
- Full scalar parity live: element value bounds validated, governed (contract), write-proven, and read-carried for length, numeric, flag, and qualified inner types.
- The 3 blocked samples green (Phase 1); BUG-019 fully Fixed.
- Catalog + code reuse held (no parallel modifier list/validator/carrier; shared obligation generator; DU-subtype dispatch) — confirmed by per-phase adversarial review.
- Doc-syncs landed; the relational-rules-and-bounds plan's Slice 3 row marked complete.

## Discovered during planning

Plan-touch sources not verbatim in the design's frontmatter `sources-consulted` (per the skill's plan-touches ⊆ design-sources check) — all **named in the design's Inventory / Architecture Grounding and verified by the lock-time `precept-reviewer` pass**; surfaced here for completeness, not design oversights:
- `src/Precept/Pipeline/Parser.Types.cs` — the collection-inner parse path (design Inventory row 1 names it; frontmatter listed the spec grammar, not the parser file).
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — `ValidateValueModifiers`/`IsTypeApplicable` (design Rule 1 + reviewer verified at 51-92; frontmatter listed `TypeChecker.cs`).
- `src/Precept/Language/Modifiers.cs` — `ApplicableTo` catalog data (design Architecture "reuse `Modifiers`"; reviewer verified 147-205).
- `src/Precept/Language/Actions.cs` — the shared obligation generator + `ActionMeta` growing-action derivation (design Rule 2/Inventory; reviewer verified `EnqueueBy`/`AppendBy`).
- `test/Precept.Tests/**` — new element-modifier test fixtures (Phase 1 matrix).

None requires a design re-lock — the design's Inventory and Architecture Grounding already scope each surface; this section records the exact file paths the build edits.

## Plan update protocol

After each phase: check off in this doc, mark the phase row ✅ with the commit hash, update `bugs.md` (BUG-019 residual), and pause for review at the phase boundary (no auto-advance). When Phase 1 lands, promote Phase 2 from stub to heavyweight. Surface any mid-build design gap as an Open Decision routed back to `/lifecycle-2-design` — do not patch the Locked design inline. Mark the relational-rules-and-bounds plan's Slice 3 row complete when both phases land.
