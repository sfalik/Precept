---
status: Locked 2026-06-01
phase-target: Phase 8 (Diagnostic-emission architecture), Slice 3b (+ 3b-count sub-slice)
comparable-systems-research-status: partial — Ada/SPARK range-check-vs-overflow distinction surveyed inline; reuses in-tree proof-engine comparator set (proof-engine.md §4)
sources-consulted:
  - docs/language/precept-language-spec.md §0.1 (Principles 7/10/11) — totality + static completeness mandate that every overflow/range/length/count fault class be compile-time-prevented
  - docs/language/precept-language-spec.md:1650 — InvalidModifierBounds (min>max) is a declaration-internal check, distinct from value-vs-bound
  - docs/language/precept-language-spec.md:2106 — interval containment normalizes UCUM units before comparison to avoid false-positive NumericOverflow
  - docs/compiler/proof-engine.md §"Decision 3" — obligations stamped by type checker, instantiated (not identified) by the proof engine
  - docs/compiler/proof-engine.md §"Decision 5" — modifier→ProofSatisfaction mapping is catalog-declared; minlength/maxlength/mincount/maxcount use Accessor("length"/"count"), not SelfValue
  - docs/compiler/proof-engine.md §"Obligation Generation Contract" (495-507) — emission-path asymmetry is the documented source of "declared constraint silently not enforced" bugs
  - docs/compiler/proof-engine.md:1672,1680 — IntervalContainment→NumericOverflow on result-interval exceedance; LengthContainment literal-only by design
  - docs/compiler/proof-engine.md:153 — SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC as the surveyed verification comparators
  - src/Precept/Language/ProofRequirement.cs:182-217,264-328 — ProofRequirementMeta DU; Numeric carries null code (1:many); IntervalContainment→NumericOverflow; Length/CountContainment records
  - src/Precept/Pipeline/ProofEngine.Diagnostics.cs:106-139,321-337 — GetNumericRequirementDiagnosticCode (Numeric 1:many dispatch); IntervalContainment/Length diagnostic rendering
  - src/Precept/Pipeline/ProofEngine.Lengths.cs:21-42 — TryLengthContainmentProof (live, literal-only) + TryCountContainmentProof (stub => null)
  - src/Precept/Pipeline/ProofEngine.Analysis.cs:401-490 — CollectDefaultObligations / CollectArgDefaultObligations build only IntervalContainment (no length/count branch)
  - src/Precept/Language/Actions.cs:272-327 — set-action IntervalContainment + LengthContainment generators (the stamping templates)
  - src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:524-723 — ValidateDefaultAgainstNumericModifiers (SelfValue-only; never touches length/count) + ValidateModifierValues
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:53-59,205-230 — dischargedAtProofStage stamp-vs-emit split + TypedConditional carve-out
  - src/Precept/Pipeline/SemanticIndex.cs:201-206 — TypedListLiteral.Elements (the static count primitive); TypeChecker.cs:610-613 — DeclaredMin/MaxCount on TypedField
  - src/Precept/Pipeline/ProofEngine.Strategies.cs:249-317 — SatisfactionCovers (count/length satisfactions are coverage-only, never obligation-generating)
  - src/Precept/Language/Diagnostics.cs:684,691 — NumericOverflow + OutOfRange meta both DiagnosticStage.Type (relabel targets)
  - src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs:99,152 — CountBoundViolation mis-placed in Gate 2 (no emission site); LengthBoundViolation legitimately Gate 2
  - src/Precept/Language/FaultCode.cs:44-54 — NumericOverflow/OutOfRange/LengthBoundViolation/CountBoundViolation all [StaticallyPreventable]
  - src/Precept/Language/ProofRequirementKind.cs:4 — stale "eleven" doc-comment (13 kinds)
---

# Declared-Bound Obligation Ownership — Numeric, String-Length & Collection-Count (+ Qualifier Residual)

Supersedes and expands **Slice 3 Decision 3**; resolves **Slice 3 Decision 5** (OutOfRange) toward *keep*; reaffirms **Slice 3 Decision 6** (MaxPlacesExceeded stays Type). Locked design for **Phase 8 Slice 3b**, with the collection-count proof-strategy build sequenced as sub-slice **3b-count**.

This design covers the **complete declared-bound family** — numeric (`min/max/positive/nonnegative/nonzero`), string-length (`minlength/maxlength`), and collection-count (`mincount/maxcount`) — not just the numeric third. A prior draft covered numeric only; adversarial review caught that the structurally-identical length and count families carried the same silent-gap pathology, and that `CountBoundViolation` was a **dead `[StaticallyPreventable]` code** (a standing Principle-11 violation). All three families are classified and closed here.

## Goal
When done, every "a declared value/result violates a declared bound" condition — numeric, string-length, or collection-count, **including `notempty`** (the length/count ≥ 1 lower-bound case) — is owned by exactly one stage and emits exactly one code, with no double-emit, no field-vs-arg split, no silent gap on a statically-decidable value, and **no dead `[StaticallyPreventable]` code**; demonstrated by a per-family per-cell single-emission test matrix over {field default, arg default, computed field, set-action} × {literal, typed-constant, resolvable interpolated/list-literal, non-constant}.

**Value-bounding modifier census (the complete family — verified against `Modifiers.cs`):** numeric `min/max/positive/nonnegative/nonzero`; string `minlength/maxlength`; collection `mincount/maxcount`; and `notempty` (= `minlength 1` on strings / `mincount 1` on collections — `collection-types.md:754`). There is no other value-magnitude-bounding modifier and no fifth declared-bound `[StaticallyPreventable]` code (verified by enumerating every `ValueModifierMeta` in `Modifiers.cs:64-247`). `maxplaces` is a precision bound, dispositioned separately (D6/Slice 5). The remaining value modifiers — `Optional` (a *presence* check → `UnprovedPresenceRequirement`, the Presence proof family), `Ordered` (a structural ordering modifier → `UnprovedModifierRequirement`, the Modifier proof family), and `Default` (the value-carrier the bounds constrain — the subject of every obligation here, not itself a bound) — are **not magnitude bounds** and belong to separate families or roles; they are out of this design's scope by definition, not by omission. (All 14 `ValueModifierMeta` are thus accounted for: 5 numeric + 2 length + 2 count + `notempty` + `maxplaces` bounds, plus `Optional`/`Ordered`/`Default`.) The partition below is keyed on **bound shape** (lower/upper bound on a projected magnitude — value, length, or count), so `notempty` is not a special case but the `min = 1` instance of the length/count lower bound; any future bound-carrying modifier maps onto an existing shape rather than re-opening the enumeration.

## Scope
- **In scope (close now)**:
  - **Numeric**: relocate the type-stage default range-check (`OutOfRange`) to a proof-stamped `Numeric(SelfValue)` obligation; remove the type-stage emit; unify field+arg+all static shapes onto one emission. Close the computed-field-result-vs-bounds gap (`IntervalContainment` → `NumericOverflow`). [D1–D4]
  - **String-length**: stamp `LengthContainment` for string **field defaults** and **arg defaults** (literal string shape), closing the silent gaps; the live literal-only prover discharges them. Includes `notempty` on strings as the `minlength 1` case. [D8]
  - **Collection-count**: implement the stubbed `TryCountContainmentProof` (mirror the length prover on `TypedListLiteral.Elements`); stamp `CountContainment` for collection **field defaults** and **arg defaults** (list-literal shape); **revive the dead `CountBoundViolation`** and re-place it in the analyzer allow-list. Includes `notempty` on collections as the `mincount 1` case. Reaches `list`-typed defaults; `set`/`queue`/`stack`/`bag`/`log` reject `default [...]` upstream with PRE0018 (a pre-existing spec-vs-parser drift — `collection-types.md:85` claims all six accept list-literal defaults but only `list` does), so count closure is effectively `list`-only until that drift is reconciled (out of scope, named). [D9]
  - **Qualifier residual**: relocate the `UnprovedAssignmentQualifierCompatibility` type-stage residual onto the stamped `AssignmentQualifier` obligation everywhere the proof engine walks (`TypedConditional` stays a type-stage carve-out). [D5]
  - **Partition discipline**: each declared-bound family owns one obligation kind + one code; the `Numeric(count-accessor)` action-safety path stays disjoint from `CountContainment`. [D10]
  - **Stage relabels + doc-drift**: `NumericOverflow`/`OutOfRange` meta Type→Proof; `ProofRequirementKind` "eleven"→13; `proof-engine.md` location/strategy-number fixes; `CountBoundViolation` allow-list re-placement.
- **Out of scope — named, not silent** (the enumerated cells that need machinery beyond this slice):
  - **Non-literal *computed* string-length / collection-count narrowing** — a `field Name as string maxlength 10 <- First + Last`, or a computed collection result, cannot be bounded by the literal-only provers. Closing this needs **length/count interval (abstract) domains** analogous to the numeric interval domain — a Slice-5-class build (cf. the decimal-scale precision domain). Enumerated here as the one deferred declared-bound cell, with the same literal-only conservative boundary the numeric set-action path already accepts (proof-engine.md:1680). Tracked for a future slice; **not a silent hole** — the literal/default cells (the concrete bugs) are closed now.
  - `MaxPlacesExceeded` (PRE0067) — no compile-time proof obligation today; the decimal-scale precision proof is **Slice 5**. Reaffirms Slice 3 D6.
  - The *definite-mismatch* legs of `QualifierMismatch`/`DimensionCategoryMismatch`, `InvalidModifierBounds` (min>max), `BoundsRequireQualifier`, `BoundsQualifierMismatch`, `CountDimensionBoundsAmbiguous`, `InvalidModifierValue`, `MaxplacesCurrencyQualifierNotStatic`, temporal/format literal-content codes, `DegeneratePeriodComparison` — declaration-internal, parse/format, or provably-wrong; genuinely type-stage.
  - The `TypedConditional`-valued assignment-qualifier case — permanent type-stage carve-out (whole-action obligation cannot be sited per-branch).
- **Deferred to future**: the set-action `IntervalTransfer`-unbounded conservative holes; length/count interval domains (above); any later merge of the family codes (explicitly rejected — D1).

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Reviving `CountBoundViolation` + closing the length/numeric default gaps makes previously-undetectable bound violations compile-time impossible (spec §0.1 P1) | N/A | N/A |
| 2. One file, complete rules | N | Obligation placement is internal | N/A | N/A |
| 3. Determinism | N | Same definition → same diagnostics | N/A | N/A |
| 4. Full inspectability | Y | One owning stage per code makes `precept_compile` honest about where each diagnostic is produced | N/A | N/A |
| 5. Keyword-anchored readability | N | No surface change | N/A | N/A |
| 6. Governance not validation | Y | Bound enforcement moves from bypassable type-stage helpers / dead code into the proof obligation channel | N/A | N/A |
| 7. Compile-time totality | Y | Numeric computed-field results gain obligations; static length/count defaults gain obligations (spec:109) | A computed numeric field whose result can't narrow now needs author constraints | Accepted — exactly P7's "prove or require constraints" (D4) |
| 8. Honesty about approximation | N | No approximate/exact surface change | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | No constraint-authoring change | N/A | N/A |
| 10. Static semantic checking | Y | All checks stay static; they move stage but remain compile-time | N/A | N/A |
| 11. Static completeness | Y | **All four** `[StaticallyPreventable]` declared-bound codes (NumericOverflow, OutOfRange, LengthBoundViolation, CountBoundViolation) get a live compile-time diagnostic on statically-decidable values, **including `notempty` defaults** (the min=1 length/count case); the computed-numeric hole and the dead `CountBoundViolation` (both spec:111 violations) are closed | The non-literal *computed* length/count cell stays unprovable until interval domains exist | Accepted + **named** (not silent); same boundary numeric set-action already accepts |

**Companion commitments.** Stateless-first-class: unaffected. Domain-expert-primary-author: *improved* — one consistent code per bound-violation class across numeric/length/count and across default/computed, instead of the current "OutOfRange here, NumericOverflow there, silence on string/collection defaults, dead code on collection counts."

## Audience and Teachability

No language surface changes (no token/keyword/type/operator/construct) — Language-Design-Grounding omitted per the skill's allowance. Author-visible diagnostic identity changes, so the error-message discipline applies:

- Numeric default: `field DiscountPct as number max 100 default 150` → **PRE0079** "Default value 150 for field 'DiscountPct' violates declared 'max'" (wording preserved; only the producing stage moves).
- String default (newly caught): `field Code as string minlength 5 default "ab"` → **PRE0135** "String value has 2 character(s) but field 'Code' requires length in [5..10]" (the live set-action wording, now also on defaults).
- Collection default (newly caught — revives dead code): `field Tags as list of string mincount 3 default ["a","b"]` → **PRE0136** "Collection has 2 element(s) but field 'Tags' requires count in [3..∞]".

Each message names the declared value and the violated bound in the author's own terms (the field name + the modifier), not compiler-internal vocabulary.

## Semantic Rules

**Numeric default-bound stamping/discharge** (unchanged from the numeric draft):

```
  D has modifier m ; m ⤳ Numeric(SelfValue, ⊕, b) ; D has default e
  ───────────────────────────────────────────────────────────────────  [stamp-numeric-default]
        stamp  NumericProofRequirement(SelfValue, ⊕, bound(b,D))  on  e
  magnitude(e)=v (static, unit-normalized) ; ¬(v ⊕ b)  ⟹  emit OutOfRange (default-context dispatch)
```

**Numeric / set-action / computed-field result** (IntervalContainment, D4 extends to computed fields):

```
  field F:τ bounds [lo,hi] ; F <- e  (or set F = e) ; IntervalOf(e)=[a,b] bounded
  ─────────────────────────────────────────────────────────────────────────────────  [stamp-result-bound]
        stamp IntervalContainmentProofRequirement(F, lo, hi) on e
  [a,b] ⋢ [lo,hi]  ⟹  emit NumericOverflow
```

**Length / count default-bound** (the parallel siblings — D8/D9):

```
  field F (string) bounds [minL,maxL] ; F default e ; e is a string literal "s"
  ──────────────────────────────────────────────────────────────────────────────  [stamp-length-default]
        stamp LengthContainmentProofRequirement(F, minL, maxL) on e
  len("s") ∉ [minL,maxL]  ⟹  emit LengthBoundViolation

  field F (collection) bounds [minC,maxC] ; F default e ; e is a list literal [..]
  ──────────────────────────────────────────────────────────────────────────────  [stamp-count-default]
        stamp CountContainmentProofRequirement(F, minC, maxC) on e
  |elements(e)| ∉ [minC,maxC]  ⟹  emit CountBoundViolation
```

**Count prover (the new strategy — D9)** mirrors the live length prover exactly:

```
TryCountContainmentProof(req, site):
  if site is not TypedListLiteral list: return null      // non-literal collection ⇒ unresolved (conservative)
  n = list.Elements.Length
  if req.DeclaredMinCount is set and n < min: return false
  if req.DeclaredMaxCount is set and n > max: return false
  return true
```

**Code-selection / family-partition rule (D1 + D10).** Each declared-bound family selects code by *family*, and numeric additionally by *subject category*:

```
  numeric,  declared value (default)        →  OutOfRange       (PRE0079)
  numeric,  computation (set/computed)      →  NumericOverflow  (PRE0078)
  string-length                             →  LengthBoundViolation (PRE0135)
  collection-count (declared mincount/maxcount) → CountBoundViolation (PRE0136)
  collection non-emptiness for a mutating action (dequeue/pop/at) → Unguarded* / IndexBoundsGuard   [unchanged, disjoint]
```

**Soundness preservation claim.**
- **P11 (static completeness):** *strengthened across the whole declared-bound family.* Before: the numeric computed-field path and the entire `CountBoundViolation` code were faults with no linked diagnostic (spec:111 violations), and string/collection defaults were silently unchecked. After: every `[StaticallyPreventable]` declared-bound code has a live compile-time diagnostic on statically-decidable values. The one residual (non-literal *computed* length/count) is the documented literal-only conservative boundary — the same posture numeric set-actions already take for `IntervalTransfer`-unbounded results — and is named, not silent.
- **P7 / P10:** checks remain static and compile-time; only producing stage and coverage change. No new expression form is introduced.
- **No regression for constants:** numeric constant defaults discharge the same magnitude against the same normalized bound, emitting the same code+message (hard gate, Acceptance 1).
- **No new collision (D10):** `CountContainment` (declared count bounds) is disjoint from `Numeric(count-accessor)` (action-safety guards) — verified: the count-accessor satisfactions are coverage-only (`ProofEngine.Strategies.cs:249-317`) and never generate `CountBoundViolation`.

## Architecture Grounding

### Precept-internal placement
**Layer placement.** All three families belong in the **proof obligation channel** (catalog-stamped requirement → engine discharge) per Decision 3. Numeric default bounds use the `Numeric` kind (1:many, carries the comparison operator natively and supports the OutOfRange/NumericOverflow partition); string-length and collection-count use their **dedicated** `LengthContainment`/`CountContainment` kinds (single fixed code each, literal-only provers) — this is the families' existing structure (set-action length already uses `LengthContainment`, `Actions.cs:313`), extended to defaults. The numeric-uses-`Numeric` vs length/count-uses-dedicated-kind asymmetry is deliberate and justified: numeric needs the operator generality + the two-code partition; length/count are single-code containment checks. Code identity stays catalog-mediated (the `Numeric` 1:many `null` is the sanctioned extension point for the new OutOfRange arm; the dedicated kinds carry their fixed code in meta).

**Cross-component propagation.**
- **Runtime (type checker / proof engine / diagnostics):** type checker stamps numeric/length/count default obligations + stops the type-stage numeric default emit + stamps the qualifier residual where walkable; proof engine gains the OutOfRange dispatch arm, the computed-field IntervalContainment collector, the implemented count prover, and the length/count default stamping in the collectors. `Diagnostics.cs` ×2 stage relabels.
- **Tooling:** hover already keys on obligation-presence (Slice 3a) — relocated/new obligations surface; verify. No completions/token change.
- **MCP:** `precept_compile` per-stage error counts shift (numeric codes Type→Proof); new diagnostics appear for previously-clean length/count default violations (a *correctness* gain). Note in `mcp.md`.
- **Analyzers:** `CountBoundViolation` moves from Gate-2 (emitted-with-test, currently false) to a legitimate Gate-2 entry once it has a real emission site + emission test — or its allow-list entry is removed if no longer needed (`DiagnosticCoverageAllowLists.cs:99`).

**Breaking changes.** No public-API/catalog-name change. Author-visible: interpolated-numeric defaults switch NumericOverflow→OutOfRange (correction); arg numeric defaults stop double-emitting; string/collection default violations newly emit (were silently accepted); collection-count violations newly emit at all. Pre-release, no external consumers.

### External architectural precedent
Ada/SPARK is the precedent for both the range-vs-overflow split and the per-constraint-family obligation model: Ada raises `Constraint_Error` for an out-of-range *value* (distinct from numeric overflow) **and** for index/length constraint violations on arrays/strings, with SPARK/GNATprove discharging each constraint family as a separate proof obligation rather than one fused check. proof-engine.md:153 already adopts SPARK as Precept's nearest verification comparator. Precept **takes** the per-family obligation structure (numeric range, length, count as distinct obligations/codes) and **diverges** by using bounded literal/interval strategies instead of an SMT-discharged predicate (Decision 1's bounded-strategy commitment) — which is exactly why the length/count provers are literal-only and non-literal computed narrowing is a separately-scoped interval-domain build, not a free SMT consequence.

## Inventory of what will be built
- **`ProofEngine.Diagnostics.cs` `GetNumericRequirementDiagnosticCode`** — new arm: `FieldDefaultContext`/`ArgDefaultContext` (a `Context`-axis discriminator, distinct from the existing `Site`-shape arms; non-overlapping because default contexts never carry collection/sqrt/divisor Numeric obligations) → `OutOfRange`.
- **Type-checker numeric default stamping** — at `TypeChecker.cs:799-815`/`:985-1004`, replace `ValidateDefaultAgainstNumericModifiers` with stamping `NumericProofRequirement(SelfValue,…)` per applicable modifier (preserve implied-modifier handling for fields). Reduce `Modifiers.cs:524-614` to the magnitude/normalization helpers the discharge reuses.
- **`ProofEngine.Analysis.cs` collectors** — `CollectDefaultObligations`/`CollectArgDefaultObligations`: numeric defaults route through the stamped `Numeric` obligation (drop the IntervalContainment-for-defaults usage); **add a `LengthContainment` branch** for string defaults with declared length bounds + literal string; **add a `CountContainment` branch** for collection defaults with declared count bounds + list literal. **`notempty` participates in both branches as a `min = 1` lower bound** — since `notempty` sets neither `DeclaredMinLength` nor `DeclaredMinCount` (`TypeChecker.cs:566,572` key on `Minlength`/`Mincount` only), the branch must detect `ModifierKind.Notempty` and fold a `min = 1` length/count bound into the stamped requirement (string → length≥1, collection → count≥1). New computed-field IntervalContainment collector (numeric, D4).
- **`ProofEngine.Lengths.cs`** — implement `TryCountContainmentProof` (mirror `TryLengthContainmentProof`, on `TypedListLiteral.Elements.Length`). Fix the stale comment.
- **`AssignmentQualifiers.cs`** — set `dischargedAtProofStage` true for field/arg-default, bound, computed-expr sites; leave false only for `TypedConditional`.
- **`Diagnostics.cs`** — `NumericOverflow`/`OutOfRange` meta `Type → Proof`.
- **`ProofRequirementKind.cs:4`** — "eleven" → "thirteen". **`proof-engine.md:1680`** — `TryLengthContainmentProof` lives in `ProofEngine.Lengths.cs` (not `Intervals.cs`); reconcile the strategy-number vs kind-ordinal mismatch (1678-1686 / 464-465). **`DiagnosticCoverageAllowLists.cs:99`** — re-place `CountBoundViolation`.
- **Tests** — per-family per-cell single-emission matrix; numeric computed-field coverage; the previously-`BeEmpty` count test (`ProofEngineStringCollectionBoundTests.cs:265`) flips to assert emission; length default emission; constant-default behavior-identical golden; qualifier residual + conditional carve-out.

## Decisions

> Decisions **D1–D7** (numeric family + qualifier residual + reaffirmations) are unchanged from the reviewed numeric draft and retain their four-leg rationale; summarized here, full text below. **D8–D10** are the length/count expansion this revision adds.

### Decision 1: Keep both `OutOfRange` (PRE0079) and `NumericOverflow` (PRE0078); partition numeric by declared-value vs computation
**Stakes**: high
- **Rationale**: the same numeric error currently emits either code by accident of shape/source; partition by subject category (declared value → OutOfRange; computation → NumericOverflow) matches FaultCode wording and keeps both `[StaticallyPreventable]` codes live (fault map undisturbed).
- **Tradeoff accepted**: two codes for one family — mitigated by the value-vs-computation intuition.
- **Alternatives considered**: unify on NumericOverflow (rejected — "default violates max" is not an overflow; perturbs fault map); unify on OutOfRange (rejected — NumericOverflow genuinely names representable-range overflow on results).
- **Precedent**: Ada `Constraint_Error` vs overflow (proof-engine.md:153 comparator set).
- **Sources consulted**: `diagnostic-system.md:611-612` (FaultCode wordings); grepped `precept-language-spec.md`/`business-domain-types.md` — **spec silent** on an OutOfRange-vs-NumericOverflow *when*-rule (only spec:1650 modifier-bounds + spec:2106 normalization), so genuine design decision, not re-litigation.
- **Strongest counter-evidence**: Slice 3 D5 deferred keep-vs-retire to Phase 9. *Response*: the confirmed arg double-emit forces the decision now; owner authorized pulling it forward.
- **Reversibility**: Hard (author-visible codes). **Blast radius**: Diagnostics.cs stages, dispatch, stamping, tests, three docs.

### Decision 2: `OutOfRange` carried by a stamped `Numeric(SelfValue)` obligation via the existing 1:many dispatch; type-stage default emit removed
**Stakes**: high
- **Rationale**: `Numeric` is the catalog's designated 1:many carrier; Decision 5 already maps every numeric modifier to `ProofSatisfaction.Numeric(SelfValue,⊕,bound)`. Adding an OutOfRange dispatch arm realizes D3's literal "OutOfRange → NumericProofRequirement" with no new kind, and unifying field+arg+all-static-shapes kills the double-emit, split, and resolvable-interpolated gap in one move.
- **Tradeoff accepted**: the discharge must replicate the helper's magnitude normalization + implied-modifier handling (logic moves, not deleted).
- **Alternatives considered**: new `BoundContainment` kind (rejected — duplicates Numeric discharge); reuse IntervalContainment with context code (rejected — only models [min,max], not positive/nonzero/!=).
- **Precedent**: `GetNumericRequirementDiagnosticCode` already dispatches Numeric to 5 codes by site (`ProofEngine.Diagnostics.cs:321-337`).
- **Sources consulted**: that dispatch switch; proof-engine.md Decision-5 table (`min(N)→[Numeric(SelfValue,>=,DeclarationValue)]`); `Modifiers.cs:561-614` (the relocated per-modifier read).
- **Strongest counter-evidence**: proof-engine.md:505 warns emission asymmetry causes silent-non-enforcement. *Response*: this *removes* the type-stage source and consolidates onto one; the 3c analyzer then enforces single-ownership.
- **NIT-noted**: the new arm discriminates on `Context` (FieldDefaultContext/ArgDefaultContext), a different axis than the existing Site-shape arms — non-overlapping (default contexts never carry collection/sqrt/divisor Numeric obligations).
- **Reversibility**: Hard. **Blast radius**: TypeChecker default sites, Modifiers.cs, ProofEngine.Diagnostics/Analysis, proof-stage tests.

### Decision 3: `NumericOverflow` stays the IntervalContainment code for computations; relabel meta Type→Proof; remove IntervalContainment's default-bound usage
**Stakes**: medium
- **Rationale**: IntervalContainment is "result must fit within field bounds" (proof-engine.md:579); after D2 defaults route through Numeric, IntervalContainment cleanly owns computations. Its meta stage is mislabeled Type despite proof-only emission (`Diagnostics.cs:684`) — correcting it is prerequisite to the 3c analyzer being green with zero allow-list.
- **Tradeoff accepted**: interpolated-numeric defaults switch NumericOverflow→OutOfRange (a correction).
- **Alternatives considered**: leave the mislabel for 3c (rejected — 3c is enforcement; meta must be correct first).
- **Precedent**: proof-engine.md:1672; Slice 3a relabels.
- **Sources consulted**: `Diagnostics.cs:684`; `ProofEngine.Analysis.cs:405,456` (decommissioned default IntervalContainment).
- **Reversibility**: Easy (label)/Hard (collector). **Blast radius**: Diagnostics.cs, Analysis.cs, stage tests.

### Decision 4: Close the computed-field-result-vs-bounds gap (numeric) — stamp IntervalContainment for computed fields → NumericOverflow
**Stakes**: high
- **Rationale**: a computed numeric field gets no bound obligation today (`ProofEngine.Analysis.cs:405` excludes `IsComputed`; no computed collector) — a `FaultCode.NumericOverflow` path with no linked diagnostic, which **Principle 11 forbids** (spec:111). Same IntervalContainment + IntervalOfNarrowed mechanism applies unchanged.
- **Tradeoff accepted**: largest behavior change — computed fields the engine can't narrow newly emit NumericOverflow, possibly needing sample constraints. Accepted: exactly P7's "prove or require constraints."
- **Alternatives considered**: defer (rejected on the comprehensiveness directive + it's a live soundness hole); narrow silently (rejected — that's the P11-forbidden status quo).
- **Precedent**: the set-action `GenerateIntervalContainmentObligations` (`Actions.cs:272-304`).
- **Sources consulted**: `ProofEngine.Analysis.cs:405` (the `IsComputed` exclusion); `spec:111` (P11).
- **Strongest counter-evidence**: may surface many sample diagnostics → "too strict." *Response*: fix is constraints/narrowing, not leaving the hole; Falsifier 1 measures it.
- **Reversibility**: Hard (but leaving it is a P11 violation). **Blast radius**: Analysis.cs / computed walk, discharge tests, possibly samples (owner authorization per sample-edit constraint).

### Decision 5: Relocate the assignment-qualifier residual onto the stamped obligation where the proof engine walks; `TypedConditional` stays a type-stage carve-out
**Stakes**: medium

> **Amended during 3b execution (`ecce5d51`)**: the relocation reaches **default + computed-expr** sites only. The proof engine does **not** walk min/max **bound expressions** (no carrier/consumer), so flipping bound sites would drop the diagnostic — they **stay type-emitted**. PRE0141 therefore has **two** legitimate type-stage carve-outs the 3c analyzer must allow: the `TypedConditional` value **and** bound expressions. Closing the bound-site residual needs a separate bound-expression walk (future, not this slice). Also: D5 proof-ownership is **not observable via `Diagnostic.Stage`** (PRE0141's meta stage is always Proof) — the observable signal is obligation-presence, which is how it is tested.

- **Rationale**: `dischargedAtProofStage` is true only for set-actions today, so defaults/bounds/computed-exprs emit PRE0141 inline (`AssignmentQualifiers.cs:223`). The discharge (`ProofEngine.QualifierNarrowing.cs`) is site-agnostic; extending stamping to walkable sites makes PRE0141 proof-owned. `TypedConditional` can't be sited per-branch (`AssignmentQualifiers.cs:53-59`) → permanent type-stage carve-out.
- **Tradeoff accepted**: PRE0141 not 100% proof-owned — one documented carve-out the 3c analyzer must allow.
- **Alternatives considered**: per-branch siting (rejected — disproportionate obligation-model change); leave whole residual type-stage (rejected — defaults/bounds/computed are walkable now).
- **Precedent**: the set-action stamp path (`AssignmentQualifiers.cs:205-214`).
- **Sources consulted**: `AssignmentQualifiers.cs:205-230` (stamp-vs-emit branch); `:53-59` (conditional limitation).
- **Reversibility**: Hard. **Blast radius**: AssignmentQualifiers.cs, Callables.cs, qualifier tests; the 3c carve-out.

### Decision 6: `MaxPlacesExceeded` stays Type-owned (reaffirm Slice 3 D6); out of scope here
**Stakes**: low
- **Rationale**: no compile-time proof obligation; non-static enforcement is runtime (`business-domain-types.md` D6); the compile-time precision proof is Slice 5. Reaffirmed so the implementer does not relocate it.
- **Tradeoff accepted**: precision checking stays split until Slice 5.
- **Sources consulted**: Slice 3 D6; the type-stage sweep (MaxPlaces fires at field/arg default + set-action `Callables.cs:184`).

### Decision 7: An unresolvable-magnitude default is not range-checked (preserve conservative behavior; documented limitation)
**Stakes**: low
- **Rationale**: an interpolated default whose magnitude isn't statically resolvable can't be evaluated to a point; 3b preserves today's conservative no-emit rather than a false "out of range." A genuinely-non-static default magnitude is a narrow residual, distinct from the now-closed resolvable cases.
- **Sources consulted**: `TypedExpressionMagnitude.cs:109-122` (false for unresolvable); `ProofEngine.Intervals.cs:64` (Unbounded for multi-slot interpolated).

### Decision 8: Close the string-length default gaps — stamp `LengthContainment` for field/arg string defaults
**Stakes**: medium
- **Rationale**: `LengthBoundViolation` (PRE0135) is a live `[StaticallyPreventable]` code with a working literal-only prover, but stamped *only* for set-actions (`Actions.cs:313`); string field/arg defaults are silently unchecked (live-confirmed: `field Code as string minlength 5 default "ab"` compiles clean). The fix mirrors the set-action stamping in the default collectors — the prover already discharges literal strings. No type-stage twin exists (the numeric default helper is SelfValue-only, `Modifiers.cs:577`), so there is no double-emit risk — only the gap to close.
- **Tradeoff accepted**: string defaults that violate length newly emit (were silently accepted) — a correctness gain that may touch samples (corpus-verified).
- **Alternatives considered**: route length through the `Numeric(length-accessor)` path (rejected — that path is coverage-only and would collide with the dedicated `LengthContainment` mechanism, exactly the collision D10 forbids); leave defaults unchecked (rejected — P11, silent gap).
- **Precedent**: the set-action `LengthContainment` generator (`Actions.cs:309-327`) is the in-tree template; `TryLengthContainmentProof` (`ProofEngine.Lengths.cs:21-34`) discharges literals already.
- **Sources consulted**: `ProofEngine.Lengths.cs:21-34` (the live literal-only prover); `Actions.cs:309-327` (set-action stamping template); `ProofEngine.Analysis.cs:401-490` (the default collectors that lack a length branch); live compile — string-minlength default compiled clean (the gap).
- **Strongest counter-evidence**: the literal-only prover can't handle interpolated/computed string defaults, so the "gap closure" is partial. *Response*: literal/typed-constant defaults are the common case and the concrete bug; the non-literal *computed* cell is the named deferred residual (length interval domain), consistent with proof-engine.md:1680's literal-only-by-design posture.
- **Reversibility**: Hard (new coverage). **Blast radius**: `ProofEngine.Analysis.cs` (length branch in both default collectors), length default tests, possibly samples.

### Decision 9: Revive the dead collection-count family — implement `TryCountContainmentProof` and stamp `CountContainment` for field/arg collection defaults
**Stakes**: high
- **Rationale**: `CountBoundViolation` (PRE0136) is `[StaticallyPreventable]` (`FaultCode.cs:53`) but **dead** — `CountContainmentProofRequirement` is constructed nowhere and `TryCountContainmentProof` is a `=> null` stub (`ProofEngine.Lengths.cs:41`), so a declared `mincount`/`maxcount` is silently unenforced (live-confirmed: `list of string mincount 3 default ["a","b"]` compiles clean). A dead `[StaticallyPreventable]` code is a standing **Principle-11 violation** (spec:111 — every such fault class must link to a live diagnostic). The static-count primitive (`TypedListLiteral.Elements.Length`, `SemanticIndex.cs:201-206`) and the declared bounds (`DeclaredMinCount/MaxCount` on `TypedField`, `TypeChecker.cs:612-613`) already exist; the prover is a near-exact mirror of the live length prover.
- **Tradeoff accepted**: revives a code that has never fired — collection-count violations newly emit, possibly touching samples; and it adds a proof strategy (modest, literal-mirror). Sequenced as sub-slice **3b-count** so the prover build + revival is verified independently of the numeric/length relocation.
- **Alternatives considered**: route `mincount`/`maxcount` through the `Numeric(count-accessor)` path (rejected — D10 collision; and `SatisfactionCovers` bails on `DeclarationValue` bounds, `ProofEngine.Strategies.cs:275-278`, so it can't carry the bound anyway); leave it dead + named-deferred (rejected — owner chose "close all of it," and a dead `[StaticallyPreventable]` code is the starkest P11 hole in the family).
- **Precedent**: `TryLengthContainmentProof` (`ProofEngine.Lengths.cs:21-34`) is the exact structural template (string `.Length` ↔ `Elements.Length`; `DeclaredMinLength/Max` ↔ `DeclaredMinCount/Max`; PRE0135 ↔ PRE0136).
- **Sources consulted**: `ProofEngine.Lengths.cs:41-42` (the `=> null` stub); `grep "new CountContainmentProofRequirement"` → zero sites; `SemanticIndex.cs:201-206` (`TypedListLiteral.Elements`); `ProofEngineStringCollectionBoundTests.cs:253-267` — the existing `BeEmpty` test is a **no-default `set` field** (`mincount 1 maxcount 10`, no default clause), so the literal-only prover correctly returns `null` and it must **keep** asserting `BeEmpty` (a correct conservative no-emit case); the revival is verified by a **new default-bearing `list` test**, not by flipping this one. `DiagnosticCoverageAllowLists.cs:99` (the mis-placed Gate-2 entry).
- **Strongest counter-evidence**: set-action collection assignment is blocked upstream by PRE0044 (list literals only in defaults), so the only live stamping site is defaults — is the strategy worth it for one site? *Response*: defaults are exactly where collection literals live, so it is the *right* site; and reviving a dead `[StaticallyPreventable]` code is P11-mandated regardless of site count.
- **Reversibility**: Hard. **Blast radius**: `ProofEngine.Lengths.cs` (prover), `ProofEngine.Analysis.cs` (count branch in default collectors), `DiagnosticCoverageAllowLists.cs` (re-place), the existing count test (flip), new count tests, possibly samples.

### Decision 10: Each declared-bound family owns one kind + one code; keep `CountContainment` disjoint from the `Numeric(count-accessor)` action-safety path
**Stakes**: medium
- **Rationale**: the catalog declares `mincount/maxcount/notempty` as `ProofSatisfaction.Numeric(Accessor("count"))`, but those are **coverage-only** facts (`ProofEngine.Strategies.cs:249-317`) — they discharge *action-safety* count requirements (dequeue/pop/at non-emptiness → `UnguardedCollection*`/`IndexBoundsGuard`), never generating `CountBoundViolation`. Declared `mincount`/`maxcount` on value-establishing sites must flow through the dedicated `CountContainment` kind, not the count-accessor Numeric path, or a single violation would produce two codes — the exact double-emit pathology this whole design removes. Symmetric rule for length.
- **Tradeoff accepted**: two count-related mechanisms coexist (action-safety Numeric-count-accessor vs declared-bound CountContainment) — acceptable because they govern genuinely different things (runtime mutation guards vs declared value bounds) and must not be fused.
- **Alternatives considered**: fuse count handling onto one path (rejected — conflates "collection non-empty before pop" with "field declares mincount 3"; different semantics, different codes).
- **Precedent**: the numeric partition (D1) — same principle, applied to the count family; the existing disjointness verified in the sweep.
- **Sources consulted**: `ProofEngine.Strategies.cs:249-317` (SatisfactionCovers is coverage-only); `ProofEngine.Diagnostics.cs:304-332` (count-accessor Numeric → Unguarded*/IndexBounds, never CountBoundViolation); `Actions.cs:113,137,209,252` (action-entry non-emptiness requirements).
- **Reversibility**: Easy (a discipline, enforced by the 3c analyzer). **Blast radius**: documentation + the 3c analyzer's per-family ownership map.

## Falsifiers
1. **If closing the numeric computed-field gap (D4) forces constraints into more than ~3 existing `samples/` files**, narrowing is too weak / the check too strict — ship D4 with a stronger narrowing pass or split it. (Compile the corpus after D4.)
2. **If any constant field/arg default changes its emitted message or code** vs the pre-3b golden, relocation broke behavior-preservation.
3. **If the Slice 3c ownership analyzer cannot reach green with exactly one documented carve-out** (the `TypedConditional` qualifier residual), the partition is leakier than designed.
4. **If reviving `CountBoundViolation` (D9) or the length default closure (D8) fires on more than ~3 existing samples**, either real latent bugs exist in the corpus (good — fix them) or the literal-count/length determination is over-eager (bad — re-scope); the count is decision-changing either way.
5. **If authors confuse PRE0079/PRE0078** (numeric) or expect a unified bound code across families, the per-family partition (D1/D10) is less intuitive than claimed — reconsider unification.
6. **If a second value-bounding modifier is found undispositioned after this slice ships** (the `notempty` miss being the first), the partition is keyed too tightly to modifier identity — recast it explicitly on *bound shape* (lower/upper bound on a projected magnitude: value/length/count) so new bound-carrying modifiers map onto an existing shape instead of re-opening the enumeration.

## Acceptance criteria
- **Numeric single-emission matrix**: {field default, arg default} × {literal, TypedTypedConstant, resolvable interpolated} below `min` → exactly one `OutOfRange`, stage `Proof`; no `NumericOverflow` on any default cell.
- **Numeric computed-field (D4)**: `field T as number max N <- e` with `IntervalOf(e)` exceeding `[..N]` → exactly one `NumericOverflow` (Proof); in-bounds computed field compiles clean.
- **Length (D8)**: `field/arg as string minlength M default "<short>"` → exactly one `LengthBoundViolation` (Proof); in-bounds default clean; set-action length unchanged.
- **Count (D9)**: a **new default-bearing** test `field Items as list of string mincount 2 default ["a"]` → exactly one `CountBoundViolation` (Proof); maxcount symmetric; in-bounds default clean; `TryCountContainmentProof` returns `false` on a violating list literal and `null` on a non-literal collection. The existing no-default test (`ProofEngineStringCollectionBoundTests.cs:253-267`) **keeps** asserting `BeEmpty` (correct conservative no-emit). Count closure is `list`-only (set/queue/stack/bag/log reject list-literal defaults via PRE0018 — named, out of scope).
- **`notempty` (D8/D9)**: `field as string notempty default ""` → exactly one `LengthBoundViolation` (Proof); `field as list … notempty default []` → exactly one `CountBoundViolation` (Proof); a non-empty default of either compiles clean.
- **Behavior-preservation golden**: constant numeric defaults produce byte-identical message+code (only stage differs).
- **Qualifier residual (D5)**: open-qualifier default/bound/computed emits PRE0141 from the **proof** stage; `TypedConditional` value emits it from the type stage (the only remaining type-stage PRE0141 site).
- **Stage relabels**: `GetMeta(NumericOverflow).Stage == Proof`, `… OutOfRange … == Proof`.
- **Dead-code revival**: `CountBoundViolation` has a real emission site + an emission test; its `DiagnosticCoverageAllowLists` placement is correct (Gate-2-legitimate or removed).
- **Doc-drift**: `ProofRequirementKind` reads "thirteen"; `proof-engine.md` cites `ProofEngine.Lengths.cs` for the length prover; strategy-number vs kind-ordinal reconciled.
- **Full suite green**; `precept_compile` per-stage counts shift as documented.

## Dependencies
- **Upstream**: Slice 3a (`f7e5dee6`), Slice 2 (`59fe2666`).
- **Downstream**: Slice 3c (ownership analyzer — depends on every value-level/declared-bound code being single-owned, with the one documented conditional carve-out). Slice 5 (precision proof) supersedes D6; the length/count interval domains (named-deferred residual) are a future sibling.

## Doc-update enumeration
- `docs/compiler/proof-engine.md` — Numeric 1:many OutOfRange arm; IntervalContainment owns computations incl. computed fields; LengthContainment/CountContainment now stamped for defaults; the implemented count prover; the family-partition / no-collision rule (D10); `ProofRequirementKind` count; the location/strategy-number fixes.
- `docs/compiler/diagnostic-system.md` — NumericOverflow/OutOfRange stage = Proof; the per-family partition; CountBoundViolation now live.
- `docs/tooling/mcp.md` — per-stage count shift + new length/count default diagnostics.
- `src/Precept/Language/ProofRequirementKind.cs` — "eleven" → "thirteen" (code-comment doc-sync).
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — `CountBoundViolation` re-placement (code change, tracked here for promotion verification).
- (Promotion-time) reconcile Slice 3 doc: D3 expanded-by / D5 resolved-by this design.

## Operational dimensions
- **Observability** (diagnostic surface): every relocated/revived check surfaces a diagnostic with an author-facing message; `precept_compile` reports them under stage `Proof`. The proof ledger records obligation + verdict, so a bound failure (numeric/length/count) is inspectable in the same channel as divisor/overflow obligations — strictly more observable than the prior inline type-stage emit or the dead count code.

## Open questions
None. The one residual (non-literal *computed* string-length / collection-count narrowing) is enumerated and named as a future interval-domain build (§ Scope), not an open question — its literal/default cells are closed now.
