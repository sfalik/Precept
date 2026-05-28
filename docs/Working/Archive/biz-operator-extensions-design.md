---
status: Promoted 2026-05-27 — implemented in commits `5d945711`, `e71e09d2`, `08fae0ef`, `fdf33095` (Phase 5 W-D). Canonical content lives in `docs/language/business-domain-types.md § Compound Types and Dimensional Cancellation` (money ÷ price → quantity), `§ Discrete Equality Narrowing` (BIZ-08), `docs/compiler/proof-engine.md` (DimensionalProduct strategy + discrete-equality narrowing strategy), `docs/compiler/diagnostic-system.md § PRE0157` (IncompatibleDimensionalProduct), `src/Precept/Language/Operations.cs` (`MoneyDividePrice`, `QuantityTimesQuantity` with `DimensionalProductProofRequirement`). This archive retains the four-leg rationale and the inline F# / JSR-354 / NodaTime / CUE / Liquid Haskell comparator survey for future audit reference. Original Locked history: Locked 2026-05-26 — refreshed 2026-05-26 (precept-reviewer remediation: PRE0152 renumbered to PRE0157 to avoid collision with W-H's already-shipped MaxplacesCurrencyQualifierNotStatic; § 0.4 property 7 mis-citation corrected to § 0.6 Proof philosophy + § 0.4 trailing paragraph attributing absence of widening to property 1; PRE0071 reuse vs new-code question addressed in Decision 3; Cancelling(u1, u2) defined via UCUM DimensionVector algebra; CUE excerpt replaced with verbatim "greatest lower bound" from /docs/references/spec/)
phase-target: Phase 5 (proof engine extensions and business-domain operator completeness)
comparable-systems-research-status: partial — inline survey per decision; each high-stakes decision carries verbatim excerpts from at least one external system with access date
sources-consulted:
  - docs/philosophy.md — Precept's core commitments, including Principle 6 (explicit domain meaning over primitive convenience)
  - docs/language/precept-language-spec.md § 0.1 — the eleven design principles
  - docs/language/precept-language-spec.md § 3A — Language Semantics (constraint surfaces, mutation atomicity, outcomes)
  - docs/language/precept-language-spec.md § 0.6 — Proof Engine Design Contract (interval reasoning, divisor safety, qualifier compatibility)
  - docs/language/business-domain-types.md — canonical money/quantity/price/exchangerate doc; § Discrete Equality Narrowing; § Semantic Rules; § Compound Types and Dimensional Cancellation
  - src/Precept/Language/Operations.cs — current binary-operation catalog; verified entries for MoneyDivideQuantity, PriceTimesQuantity, QuantityTimesQuantity, MoneyDividePeriod, MoneyDivideDuration
  - src/Precept/Pipeline/ProofEngine.Strategies.cs — TryDeclarationAttributeProof modifier arm, TryGuardInPathProof, TryFlowNarrowingProof
  - src/Precept/Pipeline/ProofEngine.Intervals.cs — IntervalOfNarrowed, ExtractFieldInterval; numeric-interval narrowing infrastructure
  - samples/it-helpdesk-ticket.precept — Severity / Urgency / Priority choice-narrowing repro context
  - samples/insurance-claim-adjudication.precept — money + quantity usage exemplar
  - docs/Working/Archive/choice-inner-and-ordered-propagation-design.md — Phase 4 precedent for choice-shape proof engine work; established that "ordered" lives on the type reference (TypedChoiceElement) and accessor results propagate via TypedExpression slots
  - F# Units of Measure documentation (Microsoft Learn) — type-level dimensional algebra
  - JSR-354 (java.money) javadoc — MonetaryAmount arithmetic surface
  - NodaTime Duration documentation — Duration ÷ Duration → double precedent for "type / type → different type"
  - CUE language reference — value disjunction narrowing
  - Liquid Haskell tutorial — refinement narrowing on equality

---

# Business-domain operator extensions: money/price cancellation, dimensional homogeneity, discrete equality narrowing

## Goal

When done, three things work that are currently rejected or silently wrong:
(1) `set TotalUnits = TotalCost / UnitPrice` type-checks and produces `quantity` with the price's denominator unit (F-LANG-BIZ-01).
(2) `quantity in 'kg' * quantity in 'm' → quantity` carries a proven dimensional product `kg·m` rather than an unconstrained compound, and `quantity in 'kg' * quantity in 'm'` consumed in a context expecting `quantity in 'kg'` is a compile-time error rather than a runtime surprise (F-LANG-BIZ-05).
(3) For `field Severity as choice of integer(1,2,3,4,5) ordered`, a rule or row guarded by `Severity == 1` permits a downstream expression whose safety depends on `Severity = 1` to discharge its safety obligation from the guard (F-LANG-BIZ-08).

## Scope

- **In scope**:
  - One new catalog entry: `OperationKind.MoneyDividePrice → Quantity` with a `QualifierChainProofRequirement` linking the money's currency to the price's currency and a `ResultQualifierPolicy` that propagates the price's denominator unit to the result.
  - A new `QualifierChainProofRequirement` arm on `OperationKind.QuantityTimesQuantity` asserting that the two quantities' dimensions compose to a known dimension under the existing UCUM-derived `DimensionVector` algebra (or, where the product is dimensionally meaningless in the curated business-domain set, the operation is rejected with a teachable diagnostic).
  - A new proof-engine strategy `TryDiscreteEqualityNarrowingProof` that consumes `$eq:F:V` markers from guard scopes and discharges numeric obligations whose subject is `F` against the singleton interval `[V, V]`. The strategy reuses the existing guard-decomposition pipeline; the marker shape mirrors the qualifier `$eq:` markers documented in business-domain-types.md § Discrete Equality Narrowing but for choice-domain fields.
  - One new diagnostic code: `PRE0157 IncompatibleDimensionalProduct` — emitted when `quantity * quantity` has operand dimensions that compose to a dimension outside the curated registry AND no downstream context narrows the result.
- **Out of scope**:
  - Full multi-term compound-unit algebra (Level C in business-domain-types.md). The curated business-domain dimension set (length, mass, volume, area, temperature, energy, pressure, count) is what the proof requirement validates against; physics-grade dimensional analysis remains permanently out of scope per business-domain-types.md.
  - Narrowing on `F in (list)` guard forms (D5 below resolves this).
  - Inequality narrowing (`F > 0`, `F <= 5`) — already covered by the existing interval narrowing in ProofEngine.Intervals.cs for non-choice fields; the new strategy is strictly for equality.
- **Deferred to future**:
  - Range-set narrowing on choice fields (`Severity == 1 or Severity == 2`) — possible extension of `$eq:` to `$in:F:{V1,V2}` when a future use case requires it.

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention not detection | Y | F-LANG-BIZ-01 fills an operator gap so authors don't fall back to bare-decimal arithmetic that the compiler cannot govern (`docs/philosophy.md` — "Prevention, not detection"); F-LANG-BIZ-05's dimensional-product proof prevents dimensionally-incoherent multiplications from silently producing a typed-but-meaningless compound; F-LANG-BIZ-08's narrowing prevents a class of currently-emitted false-positive obligations on safe expressions. | Choice-equality narrowing widens the proof surface and risks unsound widening if the marker propagation has holes. | Conservative discharge: a missing `$eq:` marker leaves the obligation undischarged (false-negative author friction), never accepts an unsafe path (soundness per `precept-language-spec.md § 0.6 Proof philosophy — Soundness over completeness`). |
| 2. One file, complete rules | N | N/A — these changes operate inside a single `.precept` file's compilation; no cross-file mechanics introduced. | N/A | N/A |
| 3. Determinism | Y | All three changes are catalog metadata + deterministic proof strategies; no solver, no time-dependent state (`precept-language-spec.md § 0.1 principle 3`). | N/A | N/A |
| 4. Full inspectability | Y | F-LANG-BIZ-05's new diagnostic surfaces dimensional incoherence at compile time with the specific operand dimensions; F-LANG-BIZ-08's narrowing markers are inspectable on hover (same `$eq:` infrastructure already used for qualifier narrowing, which surfaces via existing proof-attribution machinery). | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — no new keywords or surface syntax; catalog metadata only. | N/A | N/A |
| 6. Governance not validation | Y | F-LANG-BIZ-01 and F-LANG-BIZ-05 add to the structural enforcement surface — the compiler rejects rather than the runtime validating; F-LANG-BIZ-08 lifts more cases out of "runtime check" into "compile-time discharge." | N/A | N/A |
| 7. Compile-time totality | Y | F-LANG-BIZ-05 prevents undefined dimensional products (the previous catalog accepted any product and yielded a `quantity` whose dimension was effectively erased); F-LANG-BIZ-01 fills a totality gap where `money / price` previously had no operator. | F-LANG-BIZ-05's dimensional-product proof catches incoherent products but cannot prove arbitrary chained algebra (because we don't ship Level C); some legitimate physics products will still be rejected and require explicit-cast or refactor. | Accept: the curated business-domain dimension set is the design boundary; physics-grade chains belong outside Precept's domain per `business-domain-types.md § Why price and exchangerate are named types`. |
| 8. Honesty about approximation | Y | All three changes preserve `decimal`-backed exact arithmetic (`business-domain-types.md § decimal backing — no double in the business chain`); no IEEE 754 introduced. | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | N/A — these changes do not affect rule/ensure surfaces. | N/A | N/A |
| 10. Static semantic checking | Y | F-LANG-BIZ-01 adds a typing rule for `money / price → quantity`; F-LANG-BIZ-05 strengthens the static check on `quantity * quantity`; F-LANG-BIZ-08 extends static narrowing to discrete-equality scopes. | F-LANG-BIZ-08 increases the size of the discharge surface and the test matrix. | Accept: incremental, mechanically generated tests; the underlying guard-decomposition pipeline is the same one already used by TryGuardInPathProof. |
| 11. Static completeness | Y | F-LANG-BIZ-08's narrowing closes a known false-positive class — currently the proof engine fails to discharge safety obligations that ARE provable from the guard, forcing authors to add redundant `nonzero`/`positive` modifiers that the program already establishes via `Severity == 1`. Closing this brings Static Completeness closer to its stated commitment (`precept-language-spec.md § 0.1 principle 11`). | N/A | N/A |

**Companion commitments.** Stateless-first-class: unaffected — the three changes apply uniformly to stateful and stateless precepts. Domain-expert-primary-author: directly served by F-LANG-BIZ-01 (the author writes the natural arithmetic `TotalUnits = TotalCost / UnitPrice`); served by F-LANG-BIZ-05 (the author gets a sharper diagnostic on dimensional mismatch — "you multiplied kg by m, that's kg·m, which isn't a known business-domain dimension"); served by F-LANG-BIZ-08 (the author writes `when Severity == 1 ⇒ expr` and the compiler honours the narrowing instead of demanding redundant constraints).

## Language Design Grounding

**General language design — what the field says about this kind of construct.**

The three changes sit at the intersection of two well-studied language-design areas: (a) **type-level dimensional algebra** for the operator catalog work, and (b) **refinement-type narrowing on discrete domains** for the proof-engine work.

For (a), F# units of measure is the canonical comparator:

> "Units of measure are a kind of generic parameter, where the parameter itself represents a unit of measure such as metres or kilograms. By using units, you ensure that you don't combine values incorrectly in arithmetic computations such as adding a value in feet to a value in metres."
> — Microsoft Learn, "Units of Measure (F#)", accessed 2026-05-26 (https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure)

F# handles `quantity * quantity` by multiplying the unit annotations at the type level — `kg<kg> * m<m>` produces `<kg m>`, a synthetic compound unit that propagates through subsequent operations. The dimensional algebra is total in F#'s system (any product is allowed and produces a synthetic unit), but the system does not curate which compounds correspond to known physical dimensions. Precept's deliberate divergence: Precept curates the business-domain dimension set (length, mass, volume, area, temperature, energy, pressure, count) per `business-domain-types.md § UCUM dimension categories`. A product of `kg * m` is `kg·m`, which is a coherent physics compound but is NOT in the curated business set. Precept rejects rather than producing a typed-but-unusable compound; the author either narrows to a known dimension (e.g., `density × volume → mass` if both inputs are dimensionally typed) or refactors.

For `money / price → quantity` (F-LANG-BIZ-01), JSR-354 (java.money) is the closest comparator:

> "MonetaryAmount instances do not support binary operations between different currencies. Any such operation will throw a MonetaryException."
> — JSR-354 Specification, javadoc for `javax.money.MonetaryAmount`, accessed 2026-05-26 (https://javamoney.github.io/api.html)

JSR-354's `MonetaryAmount` carries `divide(Number)` and `multiply(Number)` but does NOT carry `divide(MonetaryAmount) → Number` or `divide(Quantity) → Price`. The Java standard library treats price as primitive and quantities as a separate concern (JSR-385 units-of-measurement is a sister spec; the two do not interoperate at the type level). Precept's divergence: Precept names `price` as a first-class type with currency-and-unit qualifiers (the inverse of Java's "everything is a MonetaryAmount") and the operator catalog therefore admits `money / price → quantity` as the dimensional-cancellation inverse of `price * quantity → money`. The decision to ship `money / price` rather than require the author to derive it via two-step `money / decimal * unit-conversion` is grounded in the precedent set by the existing inverse: `money / quantity → price` is already in the catalog (Operations.cs:469-476), so the inverse-of-an-inverse case is the natural completion.

For (b), CUE's value disjunction narrowing and Liquid Haskell's refinement narrowing on equality are the canonical comparators:

> "The _unification_ of values `a` and `b` is defined as the greatest lower bound of `a` and `b`."
> — CUE Language Specification, "Unification", accessed 2026-05-26 (https://cuelang.org/docs/references/spec/) — verbatim

CUE's lattice-based evaluation treats equality narrowing as unification at compile time: `severity: 1 | 2 | 3 | 4 | 5` unified with `severity: 1` yields `severity: 1`, and downstream constraints see the singleton. Precept's divergence: Precept's narrowing is scoped to a guard (`when Severity == 1`), not unified globally on the field. The scope is the row body / rule body where the guard is in scope, not the field's full lifetime. This matches Liquid Haskell's refinement-narrowing on case-scrutinee equality:

> "When we match against a constructor, we get to assume that the scrutinee equals that constructor in the corresponding branch."
> — Liquid Haskell Tutorial, "Refinement Types", accessed 2026-05-26 (https://ucsd-progsys.github.io/liquidhaskell-tutorial/)

Liquid Haskell narrows the scrutinee within a case branch to the matched constructor — exactly the shape of `when Severity == 1 ⇒ body` narrowing Severity to 1 within `body`. Precept's design borrows the scoped-narrowing semantics directly; the divergence from Liquid Haskell is mechanism, not concept (LH uses SMT to discharge; Precept uses syntactic guard-decomposition).

`research/language/README.md` indexes language-domain studies but does not yet contain a dedicated comparator file for refinement-narrowing-on-equality. Gap noted; this design's inline citations partially fill it.

**Precept-specific application.** This design touches:
- `precept-language-spec.md § 0.1` principles 1, 7, 10, 11 (Prevention, Compile-time totality, Static semantic checking, Static completeness) — F-LANG-BIZ-01/05 strengthen totality; F-LANG-BIZ-08 strengthens completeness.
- `precept-language-spec.md § 0.6 Proof Engine Design Contract` — F-LANG-BIZ-08 adds a discharge strategy under the existing "Numeric interval reasoning" + "Relational reasoning" umbrella.
- `business-domain-types.md § Discrete Equality Narrowing` — F-LANG-BIZ-08 generalises the existing `$eq:` marker mechanism from qualifier-axis (currency/unit/dimension) to choice-domain values.

## Audience and Teachability

**Worked example.** A domain expert authoring an inventory adjustment precept writes (F-LANG-BIZ-01 + F-LANG-BIZ-08 together):

```precept
field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
field UnitPrice as price in 'USD/each'
field TotalCost as money in 'USD'
field UnitsRefunded as quantity in 'each'
field RefundCap as integer default 100 nonnegative

from Processing on Refund
  when Severity == 1
    -> set UnitsRefunded = TotalCost / UnitPrice    # money / price → quantity  ✓
    -> set RefundCap = RefundCap - 1                 # safe — Severity==1 narrowing proves RefundCap > 0 from the rule below
    -> transition Refunded

rule RefundCap > 0 when Severity == 1
  because "Severity-1 refunds are capped and the cap must be available"
```

`TotalCost / UnitPrice` produces `quantity in 'each'` via the new `MoneyDividePrice` catalog entry — the currency-axis match is enforced as a proof obligation. The `RefundCap - 1` action's safety against underflow discharges from the rule body's narrowing under `Severity == 1`: the guarded rule `RefundCap > 0 when Severity == 1` combined with the row's guard `Severity == 1` lets the new discrete-equality narrowing strategy discharge the underflow obligation without an explicit `RefundCap nonzero` field-level modifier.

**Error message.** A domain expert writes `set Density = WeightKg * DistanceM` on fields `WeightKg as quantity in 'kg'` and `DistanceM as quantity in 'm'`:

```
PRE0157: 'kg' × 'm' produces dimension 'mass·length', which is not in the
business-domain dimension set. Multiplying these two quantities does not yield
a meaningful business value — density is mass per volume (kg/L), not mass times
distance. If you meant area, multiply two length quantities; if you meant
work/torque, both belong outside Precept's scope. Adjust the operand types
or use a domain-typed compound (e.g., 'quantity in kg/L').
```

The wording serves the domain-expert reader because (a) it names the operand dimensions in domain words ("mass", "length"), not in catalog-internal vocabulary; (b) it suggests the most-likely-intended business value (density) and shows why the actual product doesn't compute to it; (c) it points to the corrective path that stays in Precept ("use a domain-typed compound") and the corrective path that leaves Precept ("belong outside Precept's scope") with equal honesty.

**10-minute teaching path.**
1. `docs/language/business-domain-types.md § Operators table on price` — 2 minutes to see the existing `price * quantity → money` row and the new `money / price → quantity` companion.
2. `docs/language/business-domain-types.md § Compound Types and Dimensional Cancellation` — 4 minutes for the dimensional-product story and the curated business-domain dimension set.
3. `docs/language/business-domain-types.md § Discrete Equality Narrowing` — 3 minutes for the existing `$eq:` mechanism that the new choice-domain narrowing extends.
4. `samples/insurance-claim-adjudication.precept` — 1 minute to see money + quantity in context.

Total: 10 minutes. The teaching path stays within `business-domain-types.md` because that's where the canonical operator tables and narrowing semantics live; the design lifts content into that doc when it ships per the doc-update enumeration.

## Semantic Rules

**Typing rule for F-LANG-BIZ-01 (new `money / price → quantity`).**

```
  Γ ⊢ e1 : money(c1)      Γ ⊢ e2 : price(c2, u2)      c1 = c2
  ────────────────────────────────────────────────────────────
                  Γ ⊢ e1 / e2 : quantity(u2)
```

Premises: `c1 = c2` (currency axes match — discharged as a `QualifierChainProofRequirement` on `QualifierAxis.Currency`); `e2`'s magnitude proven nonzero (`NumericProofRequirement` per the existing divisor-safety contract). The result qualifier policy is `InheritDenominatorUnit` — a new policy variant or a reuse of `CompoundUnitCancellation` (D2 of this design resolves which).

**Typing rule for F-LANG-BIZ-05 (strengthened `quantity * quantity → quantity`).**

```
  Γ ⊢ e1 : quantity(u1, d1)      Γ ⊢ e2 : quantity(u2, d2)
  d_result = d1 ⊕ d2      d_result ∈ CuratedBusinessDimensions ∪ Cancelling(u1, u2)
  ────────────────────────────────────────────────────────────────────────────────
                    Γ ⊢ e1 * e2 : quantity(u1·u2, d_result)
```

Where `⊕` is dimension-vector addition (UCUM-derived) and `Cancelling(u1, u2)` is the set of unit pairs where one unit's UCUM dimension is the inverse of the other's (the existing `CompoundUnitCancellation` case). If `d_result` falls outside both sets, the operation emits `PRE0157 IncompatibleDimensionalProduct` — this is the strengthened static check.

**Proof obligation for F-LANG-BIZ-08 (new `TryDiscreteEqualityNarrowingProof` strategy).**

Given a numeric `ProofObligation` whose `Subject` is a field-reference `F` and whose `Context` has a guard containing `F == V` (where `V` is a literal in `F`'s choice domain), the strategy:

```
  Guard G contains (F == V_lit)      ObligationSubject = F
  RequirementShape = Numeric(Comparison, Threshold)
  V_lit satisfies (Comparison, Threshold)
  ────────────────────────────────────────────────────────
                     Obligation discharges
```

The strategy is conservative on guard shape: it discharges only on direct `F == V_lit` equality at the leaf of an AND-chain or as the full guard. Disjunctions require every branch to independently establish the same narrowed value (or compatible narrowed values whose union still satisfies the requirement). This matches the existing branch-walking discipline of `TryGuardInPathProof` (ProofEngine.Strategies.cs:618-657).

**Soundness preservation claim.** Principle 11 (Static completeness) is the principle most directly affected:
- F-LANG-BIZ-01: the new operator entry adds a typing rule, not an evaluation shortcut; the `decimal`-arithmetic backing and the existing `QualifierChainProofRequirement` mechanism ensure the result type and qualifier are derived from the operands, not inferred ambiently. Principle 11 holds because the new entry produces no new undefined evaluation path — the divisor-safety obligation is the same shape as the existing `MoneyDivideQuantity → Price` entry's obligation.
- F-LANG-BIZ-05: strengthening the proof requirement on an existing entry can only reject MORE programs, not accept more. Soundness is monotone under additional premises.
- F-LANG-BIZ-08: the new strategy is additive — if it discharges, the obligation was already provable from the guard; if it does not, control falls through to the existing strategies (which already correctly fail). The conservative discharge rule (per Principle: Soundness over completeness, `precept-language-spec.md § 0.6 Proof philosophy`) is preserved because the strategy only discharges when the guard provides a singleton equality that strictly implies the requirement; it never widens.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** All three findings are catalog-and-pipeline changes, in the right layers per Precept's catalog-driven architecture:

- F-LANG-BIZ-01: one new entry in `Operations.cs` (catalog metadata). The type checker, MCP vocabulary, and grammar derive from the catalog automatically; no pipeline-stage edits beyond what catalog-derivation provides.
- F-LANG-BIZ-05: modifies the proof requirement on an existing catalog entry (`QuantityTimesQuantity`) and adds a new diagnostic code in the diagnostic catalog. The proof engine consumes the new `QualifierChainProofRequirement` arm via existing infrastructure.
- F-LANG-BIZ-08: a new file `ProofEngine.DiscreteNarrowing.cs` (or a new strategy method inside `ProofEngine.Strategies.cs`) that hooks into the existing strategy chain. Marker shape extends the `$eq:` family already documented in `business-domain-types.md`.

This is the correct layering because catalog metadata is the source of truth for language surface (`CLAUDE.md` — "Catalog System (Non-Negotiable)"); the proof engine consumes catalog metadata and supplies discharge strategies. Putting any of these changes in pipeline-stage logic instead of catalog metadata would violate the catalog-first rule.

**Cross-component propagation:**

- Runtime (parser, type checker, evaluator, diagnostics):
  - Parser: None — no new tokens, keywords, or surface syntax.
  - Type checker: F-LANG-BIZ-01 adds a typing-rule arm via the catalog entry (Operations.cs index entry); F-LANG-BIZ-05 strengthens the type-check via the new ProofRequirement arm; F-LANG-BIZ-08 adds no typing-rule changes (proof-only).
  - Evaluator: F-LANG-BIZ-01 needs evaluator dispatch for the new `MoneyDividePrice` operation kind — `Amount / (Amount2 / Unit) = (Amount / Amount2)` as a `decimal`, then wrap in `Quantity(result, Unit)`. F-LANG-BIZ-05 and F-LANG-BIZ-08 are proof-time only.
  - Diagnostics: F-LANG-BIZ-05 adds `PRE0157 IncompatibleDimensionalProduct`; the other two reuse existing codes (`PRE0070`-family for qualifier mismatches, the existing safety-obligation codes for F-LANG-BIZ-08's discharge path).
- Tooling (syntax highlighting, completions, hover, semantic tokens): None for F-LANG-BIZ-01/05 beyond automatic catalog-derivation. F-LANG-BIZ-08's narrowing IS visible on hover via existing proof-attribution machinery (the new `$eq:` markers surface in the same hover surface as the existing qualifier `$eq:` markers).
- MCP (vocabulary, DTOs, tool output): F-LANG-BIZ-01 adds one operator entry to the catalog-derived MCP vocabulary (auto-derived from `Operations.All`). F-LANG-BIZ-05 adds `PRE0157` to the diagnostic registry exposed via `precept_diagnostic`. F-LANG-BIZ-08 surfaces in `precept_proofs` output via existing proof-attribution.

**Breaking changes.** None. F-LANG-BIZ-01 is purely additive (a previously-rejected expression now type-checks). F-LANG-BIZ-05 changes a `quantity * quantity` accept-then-erase-dimension into accept-with-dimension-or-reject; this could in principle reject previously-accepted programs, but per the grounding read the current acceptance is "accepted then dimension erased" — there are no in-tree samples that rely on dimension-erased quantity products (verified by grep on `samples/`; the only `quantity * quantity` usage is compound × cancellation via Level B, which is covered explicitly by the new `CuratedBusinessDimensions ∪ Cancelling(u1, u2)` rule). F-LANG-BIZ-08 is additive (a previously-undischarged obligation now discharges).

### External architectural precedent

The architectural problem F-LANG-BIZ-08 most directly faces — "extending interval narrowing to discrete domains" — is the same problem refinement type systems solve. The closest published comparator is Liquid Haskell's case-scrutinee narrowing:

> "When we match against a constructor, we get to assume that the scrutinee equals that constructor in the corresponding branch. This is one of the ways in which we incorporate program structure into refinement types."
> — Liquid Haskell Tutorial, "Boolean Measures" / "Refinement Types", accessed 2026-05-26 (https://ucsd-progsys.github.io/liquidhaskell-tutorial/05-datatypes.html)

What Precept takes: scope-bounded narrowing of a discrete-domain field to a singleton when an equality guard pins it. The narrowing flows into proof obligations within the scope and does not leak out.

What Precept deliberately diverges from: Liquid Haskell discharges obligations via SMT (Z3); Precept discharges via syntactic guard-decomposition. The SMT route would violate Precept's determinism principle (Principle 3 per `precept-language-spec.md § 0.1`) and the "no opaque solvers" commitment in `precept-language-spec.md § 0.6 Proof philosophy`. The absence of fixpoint/widening machinery — itself a consequence of property 1 (no loops) per the trailing paragraph of `precept-language-spec.md § 0.4` — keeps the discharge linear over guard branches rather than iterative. Precept's narrower mechanism handles the case that actually appears in business-domain precepts (equality guards on choice fields) without paying the SMT cost.

For F-LANG-BIZ-05's curated dimensional algebra, the architectural precedent for "accept some compound dimensions, reject others" is CUE's lattice:

> "The _unification_ of values `a` and `b` is defined as the greatest lower bound of `a` and `b`."
> — CUE Language Specification, "Values" § Unification, accessed 2026-05-26 (https://cuelang.org/docs/references/spec/) — verbatim. CUE's full lattice framing (each value is in a partial order; unification yields the greatest lower bound; values whose unification falls below `_|_` (bottom) are rejected) is the structural precedent — Precept's curated-business-domain rejection mirrors the "reject below bottom" semantics; dimension vectors outside the curated set are "below" the language's accepted domain.

CUE's lattice rejects values whose unification falls below `_|_` (bottom). Precept's curated-business-domain rejection is structurally similar: dimension vectors that fall outside the curated set are "below" the language's accepted domain. The architectural choice mirrors CUE's: define the lattice (curated set), reject anything not in it. The divergence is that CUE's lattice is open (users define their own constraints) whereas Precept's curated set is closed by design (per `business-domain-types.md § UCUM dimension categories`).

## Inventory of what will be built

### Catalog additions / modifications

- **`Operations.cs` new entry** — `OperationKind.MoneyDividePrice`:
  ```
  OperationKind.MoneyDividePrice => new BinaryOperationMeta(
      kind, OperatorKind.Divide, PMoney, PPrice, TypeKind.Quantity,
      "Money ÷ price → quantity (dimensional cancellation: currency cancels, denominator unit becomes result unit)",
      ResultQualifierPolicy: ResultQualifierPolicy.InheritPriceDenominatorUnit,  // new policy variant — see Decision D2
      ProofRequirements:
      [
          new QualifierChainProofRequirement(new ParamSubject(PMoney), QualifierAxis.Currency,
              new ParamSubject(PPrice), QualifierAxis.Currency,
              "Money currency must match price numerator currency"),
          new NumericProofRequirement(new ParamSubject(PPrice), OperatorKind.NotEquals, 0m,
              "Divisor must be non-zero"),
      ]),
  ```
- **`Operations.cs` modification** — `OperationKind.QuantityTimesQuantity` gains a new proof requirement:
  ```
  ProofRequirements:
  [
      new DimensionalProductProofRequirement(new ParamSubject(PQuantity), new ParamSubject(PQuantity),
          "Product dimension must be in the curated business-domain set or cancel to a known dimension"),
  ],
  ```
- **`ResultQualifierPolicy` enum** — new variant `InheritPriceDenominatorUnit` (D2 may instead reuse `CompoundUnitCancellation`).
- **`ProofRequirement` discriminated union** — new `DimensionalProductProofRequirement` subtype.
- **`OperationKind` enum** — one new member `MoneyDividePrice`.

### Proof engine additions

- **`ProofEngine.Strategies.cs`** — new strategy method `TryDiscreteEqualityNarrowingProof`:
  ```
  private static bool TryDiscreteEqualityNarrowingProof(
      ProofObligation obligation,
      SemanticIndex semantics)
  ```
  Consumes the same `guard` extraction pipeline as `TryGuardInPathProof` and `TryFlowNarrowingProof`. For each branch, looks for `F == V_lit` leaves where `F` is the obligation subject and the literal value `V_lit` satisfies the obligation's numeric comparison against threshold. Hooked into the strategy-chain in `ProofEngine.cs`.
- **`ProofEngine.Diagnostics.cs`** — new diagnostic-emitting path for `PRE0157 IncompatibleDimensionalProduct` consumed by the new `DimensionalProductProofRequirement`.

### Diagnostic catalog

- New code `PRE0157 IncompatibleDimensionalProduct` registered in `DiagnosticCode` enum and the diagnostic catalog with the wording shown in § Audience and Teachability.

### Tests

- `test/Precept.Tests/Operations/MoneyDividePriceTests.cs` — positive (currency match, nonzero divisor), negative (currency mismatch), negative (zero divisor).
- `test/Precept.Tests/Operations/QuantityProductDimensionTests.cs` — positive (mass × volume → density-like cancellation case, count × decimal scaling), negative (kg × m → PRE0157).
- `test/Precept.Tests/ProofEngine/DiscreteEqualityNarrowingTests.cs` — choice equality narrowing for safety obligations; F-LANG-BIZ-08 repro from the IT helpdesk sample.

### Samples

- Update `samples/insurance-claim-adjudication.precept` to use `money / price → quantity` in at least one row once the catalog entry lands (post-fix cleanup, distinct commit per the W-G precedent).

## Decisions

### Decision 1 — Add `MoneyDividePrice → Quantity` to the catalog

**Stakes**: high — adds a public-surface operator combination visible to external authors and consumed by MCP / tooling; reversal would mean retiring a publicly-available operator.

- **Rationale**: The catalog already includes `price * quantity → money` (Operations.cs:608, PriceTimesQuantity, with BidirectionalLookup) and `money / quantity → price` (Operations.cs:469, MoneyDivideQuantity). The missing `money / price → quantity` is the inverse-of-inverse case. Without it, an author who has a `TotalCost` and a `UnitPrice` and wants to derive `UnitsBought` cannot write the natural expression — they must either go through `decimal` arithmetic (losing qualifier integrity, violating Principle 6) or refactor their data model. The catalog gap silently pushes authors off the typed-arithmetic path, undermining the Domain-expert-primary-author commitment.
- **Tradeoff accepted**: The catalog grows by one entry plus the evaluator dispatch case. Catalog completeness has a maintenance cost — every new operator combination adds to the test matrix and to the MCP vocabulary the model has to keep coherent. Accept: the entry is the inverse-of-an-existing entry, so the test patterns are already established, and the MCP vocabulary already surfaces the related entries.
- **Alternatives considered**:
  - **Require the author to derive via `TotalCost / UnitPrice.amount * UnitPrice.unit`-style two-step.** Rejected — this loses the qualifier-chain proof (the compiler cannot verify the currency match between money and price; the `.amount` accessor strips the qualifier). The whole point of the typed-arithmetic design is that the compiler maintains the proof; pushing the author off-typed-path defeats that.
  - **Ship the entry without a `QualifierChainProofRequirement`** (let any money divide by any price). Rejected — currency mismatch (`'100 USD' / '5 EUR/kg'`) would silently produce a typed `quantity in 'kg'` whose magnitude is nonsense. Soundness violation.
  - **Defer to Phase 6.** Rejected — F-LANG-BIZ-01 is the smallest of the three findings and is a Phase 5 scope item; deferring delays adoption of the typed-arithmetic surface for the most common business case (price-based quantity derivation).
- **Precedent**: F# units of measure (cited above) admits all dimensional ratios at the type level; JSR-354 (java.money) does NOT carry this case but its omission is documented as a long-standing gap that JSR-385 (units) was supposed to bridge and did not. Within Precept, the existing inverse entries (`PriceTimesQuantity`, `MoneyDivideQuantity`) establish the precedent for inverse-of-inverse completion.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Operations.cs:469-476` — "OperationKind.MoneyDivideQuantity => new BinaryOperationMeta(kind, OperatorKind.Divide, PMoney, PQuantity, TypeKind.Price, "Money ÷ quantity → price", ProofRequirements: [ new NumericProofRequirement(new ParamSubject(PQuantity), OperatorKind.NotEquals, 0m, "Divisor must be non-zero"), ])"
  - `src/Precept/Language/Operations.cs:608-617` — "OperationKind.PriceTimesQuantity => new BinaryOperationMeta(kind, OperatorKind.Times, PPrice, PQuantity, TypeKind.Money, "Price × quantity → money (dimensional cancellation)", BidirectionalLookup: true, ResultQualifierPolicy: ResultQualifierPolicy.CompoundUnitCancellation, ProofRequirements: [ new QualifierChainProofRequirement(... QualifierAxis.Dimension, ... "Price dimension must match quantity dimension") ])"
  - `docs/language/business-domain-types.md § Operators table on money` — "money / quantity | price | Currency / non-currency → price derivation."
  - Microsoft Learn, "Units of Measure (F#)", accessed 2026-05-26 — "you ensure that you don't combine values incorrectly in arithmetic computations such as adding a value in feet to a value in metres" (https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure)
  - JSR-354 javadoc — accessed 2026-05-26 — "MonetaryAmount instances do not support binary operations between different currencies" (https://javamoney.github.io/api.html)
- **Strongest counter-evidence**: JSR-354's deliberate omission of `MonetaryAmount.divide(Quantity)` could be read as an "intentional simplification" argument — the Java standard library decided this case was rare enough to omit. Response: JSR-354's omission stems from its lack of a first-class price type (everything is `MonetaryAmount`), so there is no `MonetaryAmount.divide(Price)` to omit. Precept HAS a first-class `price` type, and the inverse `price * quantity → money` is already shipped; omitting `money / price → quantity` while shipping the multiplicative case is asymmetric in a way JSR-354's flat surface isn't. The counter-evidence does not apply to Precept's catalog shape.
- **Reversibility**: Hard. Once the entry ships and authors write `TotalUnits = TotalCost / UnitPrice` in their precepts, retiring it would break their definitions. Reversal would require a deprecation cycle plus per-sample migration.
- **Blast radius**: 1 catalog file (Operations.cs), 1 evaluator dispatch case, 1 MCP vocabulary auto-update, 1 doc table update (business-domain-types.md § Operators on money), 1 sample post-fix sweep.

### Decision 2 — `ResultQualifierPolicy` for `MoneyDividePrice`: new variant `InheritPriceDenominatorUnit` vs reuse `CompoundUnitCancellation`

**Stakes**: medium — affects internal catalog enum shape and the proof-engine's qualifier-result computation; not visible to external authors directly but visible in proof attribution and MCP qualifier-resolution output.

- **Rationale**: The result of `money / price → quantity` has a unit that's exactly the price's denominator. This is structurally distinct from `CompoundUnitCancellation` (which cancels a unit pair to produce a scalar or a sub-compound). Naming the policy explicitly makes the qualifier-resolution path inspectable and avoids overloading `CompoundUnitCancellation` to mean two different operations (cancellation in `price * quantity → money` versus inheritance in `money / price → quantity`).
- **Tradeoff accepted**: Enum-variant growth in `ResultQualifierPolicy`. Catalog enums grow over time as the operator surface grows; this is an expected maintenance pattern, not a smell.
- **Alternatives considered**:
  - **Reuse `CompoundUnitCancellation`.** Rejected — the semantics differ: `price * quantity` cancels the quantity's unit against the price's denominator; `money / price` doesn't cancel anything, it inherits the price's denominator as the result's unit. Overloading the policy name would obscure the proof reasoning when the proof engine emits attribution.
- **Precedent**: The existing `ResultQualifierPolicy` enum already has multiple variants (`InheritFromQualifiedOperand`, `CompoundUnitCancellation`, `CompoundDimensionElevation`, `CurrencyConversion`) — each one names a specific qualifier-flow shape. Adding `InheritPriceDenominatorUnit` follows the same naming-by-shape pattern.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Operations.cs:437,520,573,611,659,670` — existing `ResultQualifierPolicy` variants used in catalog entries: `InheritFromQualifiedOperand` (MoneyTimesDecimal), `CompoundUnitCancellation` (PriceTimesQuantity, QuantityTimesQuantity), `CompoundDimensionElevation` (PriceDivideQuantity), `CurrencyConversion` (ExchangeRateTimesMoney). Each is named for the specific qualifier-flow it implements.

### Decision 3 — F-LANG-BIZ-05: enforce dimensional homogeneity at the operation level (catalog) or via a separate proof obligation?

**Stakes**: high — this is the architectural choice between "catalog enumerates all valid product combinations" versus "catalog has one entry plus a generic proof requirement that validates the operands." Wrong choice cascades into the catalog's shape for years.

- **Rationale**: The catalog approach (one `QuantityTimesQuantity` entry plus a `DimensionalProductProofRequirement`) preserves Precept's catalog-first invariant while keeping the catalog at one entry. Enumerating per-dimensional-combination would mean 8 × 8 = 64 entries for the curated business-domain set, which violates the "never switch on `*Kind` enum identity to dispatch per-member behavior" rule from CLAUDE.md (each entry would exist "because the language says so" rather than because it carries distinct metadata). The proof-requirement approach treats dimensional composition as metadata about the operation, computed from the operands' qualifiers, not as a fixed enumeration.
- **Tradeoff accepted**: The proof engine needs a new `DimensionalProductProofRequirement` arm that knows how to compute the product dimension vector and validate it against the curated set. This is mechanically straightforward (the `DimensionVector` algebra is already implemented for UCUM) but adds one more `ProofRequirement` subtype. Acceptable.
- **Alternatives considered**:
  - **Catalog fan-out: 64+ per-combination entries.** Rejected — violates the catalog-first rule (the entries would not carry distinct metadata; they would all do the same thing modulo dimension labels) and would balloon the test matrix.
  - **Runtime check at evaluator time.** Rejected — violates Principle 1 (Prevention not detection). The whole point of governance is that dimensionally-incoherent products cannot be expressed; pushing the check to runtime makes it a validation surface, not a governance surface.
  - **Accept all dimensional products and erase the dimension on the result (current behavior).** Rejected — this is the current bug. The result has a unit string but no dimension proof; downstream operations on the result silently succeed even when they're meaningless. Principle 8 (Honesty about approximation) is implicated: a "kg·m" result that the compiler thinks is dimensionally typed but actually is not is a form of silent approximation about the type system's accuracy.
- **Precedent**:
  - CUE's lattice (cited above): "any two values have a unique least upper bound (their unification)" — values outside the lattice are bottom. Precept's curated set IS its lattice for dimensional products.
  - F# units of measure: accepts any product. Precept's deliberate divergence is the curated set; F#'s "physics is open" stance is the right tradeoff for F#'s general-purpose audience but wrong for Precept's business-domain audience.
- **Sources consulted for this decision**:
  - `src/Precept/Language/Operations.cs:570-573` — "OperationKind.QuantityTimesQuantity => new BinaryOperationMeta(kind, OperatorKind.Times, PQuantity, PQuantity, TypeKind.Quantity, "Quantity × quantity → quantity (dimensional cancellation)", ResultQualifierPolicy: ResultQualifierPolicy.CompoundUnitCancellation),"
  - `docs/language/business-domain-types.md § UCUM dimension categories` — the curated business-domain dimension list (length, mass, volume, area, temperature, energy, pressure) plus the count dimension.
  - `CLAUDE.md § Catalog System (Non-Negotiable)` — "Never switch on `*Kind` enum identity to dispatch per-member behavior."
  - CUE Language Specification, "Lattice", accessed 2026-05-26 — "any two values have a unique least upper bound (their unification) and a unique greatest lower bound" (https://cuelang.org/docs/references/spec/#lattice)
- **Strongest counter-evidence**: F# units of measure proves that an open dimensional algebra is workable in a typed language. If F# can do it, Precept could too — why curate? Response: F#'s audience is general programming (physicists, scientists, financial quants). Precept's audience is domain experts modeling business entities (`docs/philosophy.md § Who authors a precept`). The curated business set names dimensions the author already thinks in (mass, volume, energy); products outside that set (kg·m, kg²) are signals of intent mismatch in business contexts, not legitimate physics. The curation is value-add for the audience, not a limitation.
- **Reversibility**: Hard. Once `PRE0157` ships and authors learn to refactor their precepts to avoid it, relaxing the requirement later would change the language's acceptance set in a way that affects existing code only positively (more programs accepted) but would also change the diagnostic surface (PRE0157 disappears, replaced by silent acceptance).
- **Blast radius**: 1 catalog entry modification (Operations.cs), 1 new proof-requirement subtype, 1 new diagnostic code (PRE0157), 1 doc update (business-domain-types.md § Compound Types and Dimensional Cancellation), proof-engine test additions.
- **Why a new code, not PRE0071 reuse (per reviewer remediation 2026-05-26)**: PRE0071 `CrossDimensionArithmetic` already fires for `+`/`−` between operands of different physical dimensions (e.g., `kg + m`). It checks operand-level dimensional incoherence at addition-shaped operations, where the operation does NOT compose dimensions. The new PRE0157 fires for `*` (multiplication-shaped), where the operation DOES compose dimensions — and the result lands outside the curated business-domain set. Different semantics, different recovery shape: PRE0071's fix is "use one type or the other"; PRE0157's fix is "refactor to a known compound, or accept the value belongs outside Precept's domain." Reuse would require widening PRE0071's contract and changing its message template, which downstream tooling/MCP consumers depend on. A separate code keeps PRE0071's existing semantics intact. (Acknowledges asymmetry with `QuantityDivideQuantityCrossDimension` at Operations.cs:542, which today accepts cross-dimension `quantity / quantity → compound quantity` without emitting any incompatibility diagnostic — this design intentionally leaves division loose because reciprocal-dimension cancellation is the most common useful shape; division-emits is a separate future decision tracked alongside F-LANG-BIZ-11 boundary-precision work.)
- **`Cancelling(u1, u2)` definition (per reviewer remediation 2026-05-26)**: A pair of UCUM-derived `DimensionVector`s `v1, v2` cancels iff `v1 + v2` (element-wise vector addition) produces a vector whose non-zero components are entirely within the curated business-domain dimension set (length, mass, volume, area, temperature, energy, pressure, count, time-magnitude). Implementation: the existing `DimensionVector` algebra in `src/Precept/Language/Ucum/UcumAtom.cs` provides the per-dimension exponent vectors; the new `DimensionalProductProofRequirement` computes `v1 + v2` and inspects each non-zero element against the catalog of curated dimension names. The "common cancelling case" (e.g., `mass × (1/mass) → 1`) reduces to the zero-vector, which trivially satisfies the curated-set check.

### Decision 4 — F-LANG-BIZ-08: extend interval narrowing to discrete domains via a new strategy that reuses existing infrastructure

**Stakes**: high — affects the proof-engine architecture and the kinds of programs Precept accepts; visible to external authors via shifted diagnostic surface.

- **Rationale**: The existing `$eq:` marker mechanism (documented in `business-domain-types.md § Discrete Equality Narrowing § Mechanism`) was designed for qualifier-axis equality (currency, unit, dimension). Generalising it to choice-domain values is the natural extension — the marker becomes `$eq:F:V` for a field `F` with literal value `V`. The same guard-decomposition pipeline (`ApplyNarrowing`, branch walking, cross-branch accumulation) applies directly. Building a new mechanism would duplicate infrastructure; reusing the existing one keeps the proof engine architecturally coherent.
- **Tradeoff accepted**: The proof engine gains one more discharge strategy, which extends the test matrix. The strategy itself is small (~50 LOC; reuses the existing branch-walker). Acceptable given the false-positive friction the current gap creates.
- **Alternatives considered**:
  - **Build new `DiscreteIntervalNarrowing` infrastructure.** Rejected — the existing `$eq:` markers already work for the qualifier axis with the same mechanics; the only thing missing is consumption of those markers in the safety-obligation discharge path for choice-domain values. Building parallel infrastructure would be duplication.
  - **Use the existing numeric-interval narrowing (`ProofEngine.Intervals.cs`) and treat choice values as numeric points.** Considered carefully. For `choice of integer(1,2,3,4,5)`, this would work — narrow Severity to `[1,1]` and use the existing interval-discharge path. But for `choice of string("Low","Medium","High")`, the values aren't numeric and the interval machinery doesn't apply. A uniform mechanism that works across both integer-choice and string-choice domains is needed; `$eq:` is the right shape because it doesn't require a numeric line. Reject the interval-only approach.
  - **Discharge via SMT.** Rejected — violates Principle 3 (Determinism) and § 0.6 Proof philosophy's no-opaque-solvers commitment. The absence of widening (consequence of § 0.4 property 1 — no loops — per the section's trailing paragraph) further argues against an iterative fixpoint solver.
- **Precedent**:
  - Liquid Haskell case-scrutinee narrowing (cited above): "When we match against a constructor, we get to assume that the scrutinee equals that constructor in the corresponding branch."
  - CUE value disjunction narrowing (cited above): unification narrows a disjunctive value to the specific value matched.
  - Within Precept: `business-domain-types.md § Discrete Equality Narrowing` already documents the `$eq:` mechanism for qualifiers; extending to choice domain is the natural generalisation.
  - Phase 4 W-G work (`docs/Working/Archive/choice-inner-and-ordered-propagation-design.md` Decision D-3) established that choice metadata propagates via `TypedChoiceElement` slots on `TypedExpression` — the architectural seam for choice-aware proof obligations is already in place.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Strategies.cs:618-657` — `TryGuardInPathProof` — "Every OR branch must independently prove the obligation for it to discharge. ... foreach (var branchConstraints in branches) { var thisBranchProved = false; foreach (var gc in branchConstraints) { if (obligation.Requirement is NumericProofRequirement numeric) { if (GuardSubsumes(gc, numeric, obligation.Site)) { thisBranchProved = true; break; } } ..."
  - `src/Precept/Pipeline/ProofEngine.Strategies.cs:880-925` — `TryFlowNarrowingProof` — same branch-walking discipline applied to field-to-field relations.
  - `docs/language/business-domain-types.md § Discrete Equality Narrowing § Mechanism` — "1. Guard decomposition — `when X.currency == 'USD'` is decomposed into an equality assertion on the accessor. 2. Marker injection — A `$eq:X.currency:USD` marker is injected into the symbol table for the guarded scope. 3. Cross-branch accumulation — Already works. 4. Sequential assignment flow — `set X = Y` where Y is narrowed copies the markers ..."
  - `docs/language/precept-language-spec.md § 0.6 Proof philosophy` — "Soundness over completeness. The proof layer must never claim an expression is safe when it is not. ... no non-deterministic solvers ..."
  - `docs/Working/Archive/choice-inner-and-ordered-propagation-design.md` Decision D-3 — "every typed expression knows its full type identity, and the proof engine reads it" (the architectural precedent for choice-aware proof-time consumption).
  - Liquid Haskell Tutorial, "Refinement Types", accessed 2026-05-26 — "When we match against a constructor, we get to assume that the scrutinee equals that constructor in the corresponding branch" (https://ucsd-progsys.github.io/liquidhaskell-tutorial/05-datatypes.html)
  - CUE Language Specification, "Unification", accessed 2026-05-26 — "When two struct values are unified, the result is the most general value that is an instance of both" (https://cuelang.org/docs/references/spec/#unification)
- **Strongest counter-evidence**: A purely SMT-based discharge (Z3 or CVC5) would cover MORE cases — not just equality but any quantifier-free linear arithmetic over choice domains. Liquid Haskell takes that route. Response: the SMT route violates Determinism (Principle 3); the deterministic syntactic narrowing covers the case that actually appears in business-domain precepts (equality guards on choice fields) without paying the determinism cost. The "more cases" SMT covers are not cases Precept's audience writes.
- **Reversibility**: Hard. Once the strategy ships and authors stop adding redundant `nonzero`/`positive` modifiers (because the narrowing handles it), retiring the strategy would re-introduce diagnostics on programs that currently compile.
- **Blast radius**: 1 new proof-engine strategy method (~50 LOC in ProofEngine.Strategies.cs or a new ProofEngine.DiscreteNarrowing.cs), proof-engine test additions, 1 doc update (business-domain-types.md § Discrete Equality Narrowing — extend to choice-domain values).

### Decision 5 — F-LANG-BIZ-08: guard shapes that trigger discrete-equality narrowing

**Stakes**: medium — defines the surface authors can rely on; expanding later is easier than contracting.

- **Rationale**: Ship the minimal sound set first: `F == literal` (direct equality with a literal in F's choice domain), and `F == G` (field-to-field equality where both fields' choice domains intersect at a known value). Defer `F in (list)` and disjunctive equality to a future extension when a use case in `samples/` actually requires it. This matches the Phase 4 W-G discipline of shipping the minimal sound surface and deferring extensions.
- **Tradeoff accepted**: Authors who write `when Severity == 1 or Severity == 2 ⇒ expr` will see undischarged obligations even when `expr` is safe under both values. They will need to split the row or supply explicit constraints. Accepted because the OR-narrowing case is a non-trivial extension (it requires the strategy to compute the join of two singletons in the choice domain and verify the requirement holds across the join) and not currently needed by any in-tree sample.
- **Alternatives considered**:
  - **Ship `F in (V1, V2, V3)` and disjunctive equality from day one.** Rejected — extends the strategy's complexity and doesn't have a forcing sample.
  - **Ship inequality narrowing on choice domains (`Severity != 5`).** Rejected — inequality narrows to a set of N-1 values, which is a multi-point narrowing, not a singleton. The discharge logic against numeric requirements gets complicated (does every remaining value satisfy the requirement?). Defer until needed.
- **Precedent**: Phase 4 W-G shipped the minimal sound surface for choice-ordered comparisons and deferred literal-side inference to a follow-up (BUG-012). Same shipping discipline.
- **Sources consulted for this decision**:
  - `docs/Working/Archive/choice-inner-and-ordered-propagation-design.md` Decision D-4 — "ship F-LANG-COLL-02/03 in Phase 4, BUG-012 in Phase 5 — separated."
  - `samples/it-helpdesk-ticket.precept` — Severity / Urgency / Priority fields use direct equality (`Severity <= 1`, `Severity <= 2`) but not OR-equality; the most common guard shape in business-domain precepts is direct equality, which the minimal surface covers.

## Falsifiers

External-author-visible changes ship in F-LANG-BIZ-01 (new operator), F-LANG-BIZ-05 (new diagnostic PRE0157), and F-LANG-BIZ-08 (changed discharge surface — programs that previously emitted obligation diagnostics now compile). Falsifiers:

1. **If three or more samples in `samples/` need PRE0157 workarounds (explicit-cast or refactor) after F-LANG-BIZ-05 ships**, the curated business-domain dimension set is too narrow and should expand (review the post-v1 watchlist in `business-domain-types.md § UCUM dimension categories`: `'power'`, `'flow-rate'`, `'concentration'`).
2. **If `precept_compile` p99 latency exceeds 50ms on the median sample after F-LANG-BIZ-08 ships**, the discrete-equality narrowing strategy is too expensive and should be made opt-in or restricted to specific guard shapes.
3. **If a single domain expert in a usability test cannot articulate the PRE0157 diagnostic's recovery path within 2 minutes**, the wording is too compiler-internal and should be revised to use more domain vocabulary.
4. **If the `MoneyDividePrice` entry produces more than one MCP-vocabulary disambiguation question per quarter** (authors confused about which combination to use), the operator surface around price arithmetic is too dense and price/money operators should be reorganised in the canonical doc.
5. **If any choice-domain field type other than `choice of integer(...)` and `choice of string(...)` requires special-casing in the discrete-equality narrowing strategy**, the strategy's assumption of "any choice-domain value can serve as an equality narrowing target" was wrong and the surface should narrow to integer-choice only initially.

## Acceptance criteria

A vertical slice is complete when ALL of the following hold:

1. `set TotalUnits = TotalCost / UnitPrice` on `field TotalCost as money in 'USD'` and `field UnitPrice as price in 'USD/each'` compiles cleanly and produces `quantity in 'each'`. `precept_compile` returns no diagnostics. (F-LANG-BIZ-01 positive test.)
2. `set TotalUnits = TotalCost / UnitPrice` where `UnitPrice` is `price in 'EUR/each'` emits a currency-mismatch diagnostic from the `QualifierChainProofRequirement`. (F-LANG-BIZ-01 negative test.)
3. `quantity in 'kg' * quantity in 'L'` where the context expects nothing specific compiles cleanly only if mass·volume is in the curated set OR cancels; otherwise emits PRE0157. (F-LANG-BIZ-05 positive and negative tests.)
4. The IT helpdesk sample's `when Severity == 1` row body can include an action whose safety depends on `Severity = 1` and the safety obligation discharges without an explicit `nonzero`/`positive` modifier. (F-LANG-BIZ-08 positive test via `samples/it-helpdesk-ticket.precept` regression.)
5. The MCP `precept_operations` tool surfaces the new `MoneyDividePrice` entry; `precept_diagnostic` surfaces `PRE0157` with the audience-fit message; `precept_proofs` attributes the discharge of F-LANG-BIZ-08 obligations to the guard that established the narrowing. (Tooling integration test.)
6. `docs/language/business-domain-types.md § Operators table on money` includes the new `money / price` row; `docs/language/business-domain-types.md § Compound Types and Dimensional Cancellation` includes the curated-dimension-product rule; `docs/language/business-domain-types.md § Discrete Equality Narrowing` extends to choice-domain values. (Doc-update enumeration verified.)
7. The catalog-driven grammar generator emits the same `tmLanguage.json` output as before for token/keyword surface (no surface syntax change). (Catalog discipline verified.)

## Dependencies

- **Upstream**: Phase 4 W-G choice-shape propagation work must be complete (it is — `docs/Working/Archive/choice-inner-and-ordered-propagation-design.md` is archived and the `TypedChoiceElement` slot on typed expressions is in place). Discrete-equality narrowing on choice fields consumes the same choice-metadata carrier.
- **Downstream**: enables `samples/insurance-claim-adjudication.precept` and `samples/invoice-line-item.precept` to express price-based quantity derivation idiomatically. Enables the curated dimensional-product proof to be cited by future business-domain operator additions.

## Doc-update enumeration

Per `CLAUDE.md` § Documentation Sync routing table:

| Update | File |
|---|---|
| New operator in catalog → new row in operators table for `money` and for `price` | `docs/language/business-domain-types.md § money operators`, `§ price operators` |
| Strengthened proof requirement on `quantity * quantity` + new diagnostic | `docs/language/business-domain-types.md § Compound Types and Dimensional Cancellation` |
| New diagnostic code `PRE0157 IncompatibleDimensionalProduct` | `docs/compiler/diagnostic-system.md` |
| Extended discrete-equality narrowing to choice-domain values | `docs/language/business-domain-types.md § Discrete Equality Narrowing § Mechanism` (new bullet for choice-domain marker shape) |
| New proof strategy in the proof engine | `docs/compiler/proof-engine.md § Strategies` |
| Catalog inventory updated for new `OperationKind.MoneyDividePrice` + new `ResultQualifierPolicy.InheritPriceDenominatorUnit` + new `ProofRequirement.DimensionalProductProofRequirement` | `docs/language/catalog-system.md § Operations`, `§ ProofRequirements` |
| Spec § 3A typing rules — new typing rule for `money / price` and strengthened rule for `quantity * quantity` | `docs/language/precept-language-spec.md § 3A.1 Constraint Semantics` (subsection for operator typing rules if present; otherwise § 3.6 Expression Typing Rules) |

## Operational dimensions

- **Security**: N/A — no source-text ingestion changes; the diagnostic message is a fixed template, not user-provided text.
- **Observability**: F-LANG-BIZ-08's narrowing must surface in proof-attribution output (hover, `precept_proofs`) so authors can see which guard discharged which obligation. The existing proof-attribution infrastructure handles this if the new strategy emits the right metadata; verified by the acceptance criterion that `precept_proofs` attributes the discharge correctly.
- **Evolvability**: N/A — no external-standard dependency added. The curated business-domain dimension set IS the language-internal source of truth; UCUM provides the unit codes but not the curated dimension list.

## Open questions

None. All five decisions are resolved with stakes-appropriate legs. Falsifiers documented. Acceptance criteria test-shaped.
