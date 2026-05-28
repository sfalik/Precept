---
status: Locked 2026-05-27
phase-target: post-phase-5-review-remediation
comparable-systems-research-status: strong
sources-consulted:
  - docs/language/business-domain-types.md (Approximation Stance §168, §180; UCUM dimension categories §397) — canonical Precept stance on dimensional algebra, curation, and counting-unit non-interchangeability
  - src/Precept/Language/Operations.cs (MoneyDividePrice at line 487 with ResultQualifierPolicy at line 490; QuantityTimesQuantity at line 600 with DimensionalProductProofRequirement at line 606)
  - src/Precept/Pipeline/SemanticIndex.cs:234-262 (QualifierBinding DU, six existing subtypes)
  - src/Precept/Pipeline/TypeChecker.Expressions.cs (MapQualifierBinding lines 980-1000; ShouldSkipPairwiseQualifierChecks lines 1136-1141)
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:343-356 (ResolveBinaryQualifierAxis — additional QualifierBinding consumer site)
  - src/Precept/Pipeline/ProofEngine.Qualifiers.cs (TryDimensionalProductProof lines 46-60; ResolveQualifierFromExpression statement form ~line 332, expression form ~line 469)
  - tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:1002-1017 (LS hover — additional QualifierBinding consumer site)
  - src/Precept/Language/DiagnosticCode.cs (PRE0137 CrossCountingUnitOperation; PRE0157 IncompatibleDimensionalProduct)
  - src/Precept/Language/Diagnostics.cs (PRE0137 and PRE0157 factory entries)
  - research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md (Stage-1 research on F# UoM, Boost.Units, Frink, Haskell units, Kennedy's free-abelian-group theory)
---

# Qualifier Inheritance for Money ÷ Price, and Counting-Unit Discipline Under Multiplication

## Goal

Close two gaps in Precept's qualifier and dimensional algebra surfaced by the post-Phase-5 code review:

1. `MoneyDividePrice` declares `ResultQualifierPolicy.InheritPriceDenominatorUnit` (`Operations.cs:490`) but no `QualifierBinding` subtype exists and no resolver arms exist. The result of `money in 'USD' ÷ price in 'USD/each'` has no Unit-axis qualifier flowing downstream. The catalog policy is dead metadata.
2. `QuantityTimesQuantity` (`Operations.cs:600`) accepts two `count`-dimensional operands silently — `5 each × 3 box` passes because both have `DimensionVector.None` and the curated `count` alias accepts dimensionless products. This contradicts the documented rule in `business-domain-types.md:397` that counting units are **not interchangeable**, which is already enforced for addition via PRE0137.

Goal: make the declared catalog policy load-bearing (Decision A); extend the existing counting-unit non-interchangeability rule from `+` to `×`/`÷` so the multiplicative case matches the additive case already documented and enforced (Decision B).

## Scope

In scope:

- New `QualifierBinding` subtype for `InheritPriceDenominatorUnit`; resolver arms in both `ResolveQualifierFromExpression` forms in `ProofEngine.Qualifiers.cs`.
- `MapQualifierBinding` arm in `TypeChecker.Expressions.cs` for the new subtype.
- Updates to **every** `QualifierBinding` consumer site (five sites identified — see Architecture Grounding).
- Tightening of `TryDimensionalProductProof` to emit `PRE0137 CrossCountingUnitOperation` (NOT a PRE0157 variant) when both operands are dimensionless and carry differing named units. Reuses the existing diagnostic identity.

Out of scope:

- No new keywords, no surface syntax changes, no operator additions.
- No change to `QuantityTimesPeriod`, `QuantityTimesDuration`, `QuantityTimesDecimal`, or `QuantityTimesQuantity` for *non*-dimensionless operands.
- No change to the curated business-domain dimension set or `DimensionCatalog` membership.
- No change to currency-axis behavior of `MoneyDividePrice` (existing `QualifierChainProofRequirement` covers currency cancellation).

## Philosophy Alignment

| # | Principle | Coverage |
|---|---|---|
| 1 | Prevention not detection | **Y** — Decision B converts a silent pass into a compile-time error; Decision A completes a check whose absence lets downstream proofs over the result silently fail to constrain. |
| 2 | One file, complete rules | N/A — no rule-surface change. |
| 3 | Determinism | **Y** — Decision A ensures the same expression produces the same Unit-axis qualifier downstream every time. Decision B removes a silent under-specification path. |
| 4 | Full inspectability | N/A — both decisions operate inside the compiler pipeline; nothing new is hidden. |
| 5 | Keyword-anchored readability | N/A — no new keyword. |
| 6 | Governance not validation | N/A — compile-time enforcement; no runtime boundary. |
| 7 | Compile-time totality | **Y, load-bearing.** Decision B replaces a totality hole (`each × box` silently typed as `quantity`) with a definite verdict. Decision A makes the inherited unit available at compile time so downstream `expect`s over the result are total. |
| 8 | Honesty about approximation | **Y, load-bearing.** `business-domain-types.md:168` already commits: "cross-dimension multiplication that doesn't cancel is **rejected** (compile error), not silently approximated." `:397` extends that stance to counting units: "that shared dimension class does **not** make them interchangeable." Decision B brings the multiplicative case into compliance with the documented stance. |
| 9 | Mandatory rationale | N/A — no new construct that takes a `because`. |
| 10 | Static semantic checking | **Y** — both decisions are static-checking extensions. |
| 11 | Static completeness (no runtime faults from well-typed programs) | **Y** — Decision B closes a path where `each × box` typed cleanly but produced a value whose downstream meaning is undefined; Decision A closes a path where the type system claimed a `quantity` existed but couldn't name its unit. |

**Companion commitments**: Domain-expert primary author — both decisions surface unit identity at the precept surface (`quantity in 'each'`) so the author reads what flows. Stateless-first-class — N/A (no state machinery involved).

## Language Design Grounding

**Precept's stated position** (already locked in canonical docs, not freshly invented):

`docs/language/business-domain-types.md:168` (Approximation Stance):

> *"`quantity` — Exact within the unit-of-measure system. UCUM-derived; arithmetic preserves dimensional integrity. Cross-dimension multiplication that doesn't cancel is **rejected** (compile error), not silently approximated. Unit conversion is explicit."*

`docs/language/business-domain-types.md:180` (Curation philosophy):

> *"Compile-time dimensional reasoning is exact within the UCUM algebra and curated against the business-domain dimension set. … Products outside the curated set are rejected with `IncompatibleDimensionalProduct` (PRE0157), not silently typed as an unconstrained compound — the type system says 'I don't recognize this product as a business dimension,' rather than 'this is a `quantity` of unclear shape.'"*

`docs/language/business-domain-types.md:397` (Counting units — the direct precedent for Decision B):

> *"Counting units (`each`, `box`, `case`, `pack`, `pallet`, `dozen`) live in the shared count dimension: they are registered with `DimensionVector.None` and normalize with factor-one semantics. That shared dimension class does **not** make them interchangeable or universally convertible. Cross-counting-unit operations such as `qty_each + qty_box` therefore fail with PRE0137 `CrossCountingUnitOperation`, not PRE0071 — PRE0071 is for different physical dimensions (for example `kg` vs `m`), while PRE0137 is for same-dimension count quantities whose explicit unit codes differ and have no universal UCUM conversion factor. Conversion between counting units still requires explicit multiplication by a typed business conversion factor (e.g., `quantity in 'each/case'`)."*

Decision B is the multiplicative analogue of the rule the doc already states for addition. PRE0137 already exists for `qty_each + qty_box`; the silent-pass for `qty_each × qty_box` is the enforcement gap, not the intended behavior. Decision B closes the gap with the diagnostic the doc names.

**Broader-field engagement** — the in-tree research `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` surveys F# Units of Measure, Boost.Units, Frink, Haskell `units`, and Kennedy's free-abelian-group theory. The relevant finding (from that file, lines 87-101): **F# UoM accepts arbitrary multiplicative composition.** `float<m> * float<kg>` produces `float<m kg>` — F# does NOT reject. Pint, Frink, and Haskell `units` exhibit the same composition-freely behavior. Kennedy's theory describes the free abelian group over unit names, which is the algebraic foundation for the "compose freely" stance.

**Precept's divergence is deliberate**: Precept curates rather than composes freely. The curation philosophy is explicit in `business-domain-types.md:180` and is a Precept-specific choice grounded in the domain-expert audience (the author should read "this product is a known business dimension" or "this is rejected" — never "this is a compound shape the system doesn't classify"). Decision B is consistent with this curation; it does not introduce novel territory.

**No surveyed system rejects `each × box`** at compile time. Precept is novel on this axis. The novelty is grounded in Precept's curated-business-dimension philosophy (principle 8 + `:180`'s explicit "not silently typed as an unconstrained compound" commitment) and in the symmetric enforcement that already exists for the additive case via PRE0137.

## Audience and Teachability

**Worked example** (covers both decisions in 9 lines):

```precept
precept Reorder
  field Budget    as money in 'USD' default '0 USD'
  field UnitCost  as price in 'USD/each'
  field UnitsToBuy as quantity in 'each' <- Budget / UnitCost
  field Cartons   as quantity in 'box'  default '12 box'
  field WrongTotal as quantity <- UnitsToBuy * Cartons   // PRE0137 — each × box not interchangeable
  state Open initial
```

Line 4 demonstrates Decision A: `Budget / UnitCost` produces a `quantity in 'each'` because the price's denominator unit (`each`) flows to the result. The downstream type-check on `UnitsToBuy : quantity in 'each'` now succeeds.

Line 6 demonstrates Decision B: even though `UnitsToBuy` and `Cartons` are both dimensionless (count-aliased), their named units differ — `each` ≠ `box` — and the product is rejected with PRE0137, the *same diagnostic* the author would see for `UnitsToBuy + Cartons`.

**Error message** (PRE0137 message — already exists, message tuned to cover both `+` and `×`):

```
PRE0137 — Cannot multiply 'each' × 'box': counting units in the same
'count' dimension are not universally convertible. The same rule applies
to '+': 'qty_each + qty_box' is also rejected (you would not silently
add eaches and boxes).

Fix hint: If you meant "convert boxes to eaches first," express the
conversion explicitly (e.g., `Cartons * 12 each/box`). The compound
'each/box' conversion factor is typed and inspectable.
```

**Teaching path** (≤10 minutes for a domain-expert author):

1. Read `business-domain-types.md § Approximation Stance` (the 5-line table at line 165 and the surrounding paragraph at 180): "Cross-dimension multiplication that doesn't cancel is rejected" — establishes the curation philosophy.
2. Read `business-domain-types.md § UCUM dimension categories` (1 paragraph at line 397): explains that counting units share the `count` dimension but are not interchangeable.
3. Look at the worked example above: line 4 shows unit inheritance; line 6 shows the rejection. The two patterns mirror each other — units flow through arithmetic where the math composes cleanly; rejected where the math is ambiguous.

## Semantic Rules

### Rule QR-A (Qualifier Inheritance: Money ÷ Price)

For `e₁ : money in 'C'` and `e₂ : price in 'C/U'`, the expression `e₁ ÷ e₂` types as `quantity in 'U'` with:

- Currency-axis qualifier on the result: **none** (cancelled by `QualifierChainProofRequirement`).
- Unit-axis qualifier on the result: **inherited from `e₂`'s denominator unit `U`**.
- Dimension-axis qualifier on the result: **inherited from `e₂`'s denominator dimension** (whatever `U` projects to; `count` for dimensionless `U` like `'each'`).

The inheritance flows through arbitrarily nested binary ops — `(Budget - Spent) / UnitCost` produces a `quantity in 'each'` if `UnitCost : price in 'USD/each'`. This is the same transitivity guarantee the other `QualifierBinding` subtypes provide.

### Rule QR-B (Counting Units Are Not Interchangeable Under Multiplication)

For `e₁, e₂ : quantity` where `dim(e₁) = dim(e₂) = DimensionVector.None` (both are dimensionless), the expression `e₁ × e₂` is well-typed only if:

- Both `e₁` and `e₂` carry the **same** Unit-axis qualifier name, OR
- Neither carries a Unit-axis qualifier name (both bare `quantity` with no `in`-clause), OR
- One operand elaborates to the other's unit via the existing literal-elaboration rules.

If both carry different Unit-axis names (e.g., `'each'` and `'box'`), emit **PRE0137 `CrossCountingUnitOperation`** — the *same diagnostic* that already fires for the additive case `qty_each + qty_box`. The trigger condition extends from "addition/subtraction on counting units" to "any arithmetic combination of counting units with differing names." The recovery path is the same: explicit conversion factor `quantity in 'each/box'`.

Cancelling pairs are unaffected — `quantity in 'kg' × quantity in '1/kg'` resolves to `count` because the dimensional vectors cancel; this rule fires only when both operands are *already* dimensionless before the product (vector = `None`) and their unit names differ.

**Soundness preservation**: Principles 7 (totality), 10 (static semantic checking), and 11 (static completeness) all strengthen — the product previously produced a silent `quantity` of unclear named-unit content; it now either produces a `quantity` whose unit name is well-defined or fails with a definite diagnostic.

## Architecture Grounding

### Precept-internal placement

**Layer placement**:

- **Catalog (`Operations.cs`)** already declares the policies needed. `ResultQualifierPolicy.InheritPriceDenominatorUnit` exists on `MoneyDividePrice` at line 490; `DimensionalProductProofRequirement` exists on `QuantityTimesQuantity` at line 606. Decision A wires an *existing* policy to its consumer; no new policy added. Decision B does not change the catalog — it tightens the proof requirement's discharge condition.
- **Semantic index (`SemanticIndex.cs:234-262`)** is the home of the `QualifierBinding` DU. Decision A adds one sealed subtype next to the six existing ones (`InheritedQualifier`, `SameQualifierRequired`, `CompoundUnitCancellationRequired`, `QualifiedOperandInherited`, `CurrencyConversionRequired`, `CompoundDimensionElevationRequired`).
- **Type checker** — Decision A touches the `MapQualifierBinding` site (`TypeChecker.Expressions.cs:980-1000`) and **also** the two other `QualifierBinding` consumer sites: `ShouldSkipPairwiseQualifierChecks` (`TypeChecker.Expressions.cs:1136-1141`) and `ResolveBinaryQualifierAxis` (`TypeChecker.Expressions.AssignmentQualifiers.cs:343-356`). Both currently have `_` catch-alls that would silently mis-handle the new subtype if not updated.
- **Proof engine (`ProofEngine.Qualifiers.cs`)** — Decision A adds arms in both `ResolveQualifierFromExpression` forms (statement form ~line 332, expression form ~line 469). **Decision B does not touch the proof engine** — the unit-name discrimination check lives at the type-checker emission site (`TypeChecker.Expressions.cs:1223-1240` lifted out of the `!opComposesDimensions` gate). `TryDimensionalProductProof` and `ProofEngine.Diagnostics.cs:153-172` (which maps the proof requirement to PRE0157) are unchanged.
- **Tooling (`RichHoverFactory.cs:1002-1017`)** — hover-time qualifier resolution switches on `QualifierBinding` subtypes; the `_` catch-all returns "left ?? right" which would give a wrong hover answer for the new subtype. Update needed.
- **Diagnostics (`Diagnostics.cs:1247-1254`)** — Decision B tunes PRE0137's `TriggerCondition` text to cover the broader trigger family. The diagnostic code itself does not change.

**Cross-component propagation table**:

| Component | Impact |
|---|---|
| Compiler (parser) | None — no syntax change. |
| Compiler (type checker — `MapQualifierBinding`) | Decision A: add arm for `InheritPriceDenominatorUnit` → `PriceDenominatorInherited`. |
| Compiler (type checker — `ShouldSkipPairwiseQualifierChecks`) | Decision A: add unconditional `PriceDenominatorInherited => true` arm. The pairwise gates at lines 1180 / 1207 are typed for `Money + Money` and `Quantity + Quantity` respectively; the `Money / Price` operand combination never matches either gate, so `true` vs `false` is functionally equivalent. We choose `true` as the most defensive arm (mirrors how `QualifiedOperandInherited` skips unconditionally; the parallel `CompoundDimensionElevationRequired` uses conditional skipping because its operands DO reach the `Quantity + Quantity` gate). Currency-axis sameness is verified separately by `QualifierChainProofRequirement`. |
| Compiler (type checker — `ResolveBinaryQualifierAxis`) | Decision A: add arm so the assignment-side qualifier resolution returns the inherited unit on the Unit/Dimension axes (not Absent). Without this, acceptance criterion 6 fails. |
| Compiler (type checker — pairwise check refactor at `:1207-1241`) | Decision B: split the existing `!opComposesDimensions` gate so the cross-counting-unit check (PRE0137) runs uniformly across `+ - × ÷`, while the cross-dimension check (PRE0071) stays gated to additive ops. |
| Compiler (proof engine — `ResolveQualifierFromExpression`) | Decision A: add arms in both forms. **Decision B does NOT touch the proof engine.** |
| Compiler (proof engine — `TryDimensionalProductProof`, `ProofEngine.Diagnostics.cs`) | No change. `DimensionalProductProofRequirement` continues to discharge against the curated catalog; PRE0157 emission path unchanged. |
| Runtime (evaluator) | None — neither decision changes runtime behavior. |
| Tooling (LS — `RichHoverFactory`) | Decision A: add arm so hover on `Budget / UnitCost` shows `quantity in 'each'`. |
| Tooling (semantic tokens, completions) | None directly; semantic tokens derive from token catalog, not `QualifierBinding`. |
| MCP (`precept_compile`, `precept_diagnostic`) | No new diagnostic code. Decision B reuses PRE0137 (already in MCP vocabulary); the TriggerCondition text change surfaces automatically via `Diagnostics.GetMeta`. Decision A produces no new diagnostic. **MCP vocabulary unchanged.** |
| Diagnostics (`Diagnostics.cs:1247-1254`) | Decision B: update PRE0137's `TriggerCondition` text to cover the broader trigger family (comparison, same-match function call, `+ - × ÷`). **Add** `ExampleBefore` / `ExampleAfter` fields (currently null — these will be populated for the first time to show a multiplicative case). The diagnostic code, message template, and severity are unchanged. |

**Breaking changes**: None to public surface. The DU expansion is internal. The diagnostic-message-text change on PRE0137 is author-visible but the diagnostic code is unchanged.

### External architectural precedent

In-tree precedent is the load-bearing argument: `QualifierBinding` already has six subtypes, each with consumer arms across the five sites listed above. Decision A is mechanical extension of an established pattern. Decision B reuses an established diagnostic identity (PRE0137) for an analogous failure mode.

External: F# UoM, Boost.Units, Frink, Pint, Haskell `units` all **accept** `each × box`-style compositions (per `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md`). Precept is novel in rejecting. The novelty is acknowledged and is grounded in the curation philosophy already documented in `business-domain-types.md:168`/`:180`/`:397` — not in any external precedent. This is the *deliberate* divergence point from comparator systems.

## Inventory of what will be built

Slice 3 implementation will touch the following files. The QualifierBinding-related changes form a coherent group — every consumer arm is updated in the same pass to maintain DU exhaustiveness:

1. **`src/Precept/Pipeline/SemanticIndex.cs:234-262`** — add `public sealed record PriceDenominatorInherited : QualifierBinding;` next to `CompoundDimensionElevationRequired`.

2. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`**:
   - **Line ~990 (`MapQualifierBinding`)**: add `if (meta.ResultQualifierPolicy == ResultQualifierPolicy.InheritPriceDenominatorUnit) return new PriceDenominatorInherited();`
   - **Line ~1136 (`ShouldSkipPairwiseQualifierChecks`)**: add arm `PriceDenominatorInherited => true` (the inherited-unit-from-denominator case does not require operand-side unit pairwise sameness; currency is cancelled, unit is inherited, so the pairwise check is inapplicable).

3. **`src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:343-356`** (`ResolveBinaryQualifierAxis`): add arm so the assignment-side resolution returns the inherited unit on the Unit/Dimension axes (calls the same `ResolvePriceDenominatorQualifier` helper as the proof-engine arm to keep both paths consistent). Without this arm, assignment-target qualifier resolution returns `Absent` and the type checker's qualifier-flow downstream is broken.

4. **`src/Precept/Pipeline/ProofEngine.Qualifiers.cs`** (Decision A only):
   - **Statement form (~line 332)**: add arm for `PriceDenominatorInherited`.
   - **Expression form (~line 469)**: same arm.
   - Add `ResolvePriceDenominatorQualifier(priceOperand, axis, semantics)` helper — projects the price's compound qualifier onto its denominator unit/dimension. Lives next to `TryProjectCompoundPrice`.
   - **`TryDimensionalProductProof` (lines 46-60) is NOT modified.** The proof engine continues to discharge `DimensionalProductProofRequirement` against the curated catalog as before; PRE0157 emission via `ProofEngine.Diagnostics.cs:153-172` is unchanged.

4b. **`src/Precept/Pipeline/TypeChecker.Expressions.cs:1207-1241`** (Decision B). Refactor:
   - The existing nested block (`!opComposesDimensions` gate at line 1209 containing both the cross-dimension check at 1216-1220 emitting PRE0071, and the dimensionless-unit-name check at 1223-1240 emitting PRE0137) splits into two independent checks at the same call site.
   - Cross-dimension check (PRE0071) **stays gated** to `!opComposesDimensions` — additive arithmetic only.
   - Cross-counting-unit check (PRE0137) **lifts out** of the gate — runs for `+ - × ÷` uniformly when both operands are dimensionless with differing unit names.
   - The existing helpers (`TryGetQualifierDimensionVector`, `GetUnitCodeFromQualifiers`, `GetOperandName`) are reused; no new helper needed.

5. **`tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:1002-1017`** — add arm for `PriceDenominatorInherited` so hover on `Budget / UnitCost` shows the inherited `quantity in 'each'` rather than the catch-all "left-or-right" answer.

6. **`src/Precept/Language/Diagnostics.cs`** — tune PRE0137's message and `ExampleBefore` / `ExampleAfter` to cover both `+` and `×`/`÷`. The diagnostic code itself does not change; only the message and examples evolve.

7. **`test/Precept.Tests/`** — proof-engine and type-checker tests covering: (a) inheritance flows two levels deep through `(Budget - Spent) / UnitCost`; (b) `each × box` rejected with PRE0137; (c) `each × each` passes (count² aliases to count); (d) `kg × 1/kg` continues to pass (cancelling pair); (e) `kg × m` continues to fail with PRE0157 (unchanged behavior); (f) hover output on the inheritance case shows the inherited unit.

8. **No catalog enum renumbering**, no new `DimensionCatalog` entries, no new `ResultQualifierPolicy` values.

## Decisions

### Decision A — Wire `InheritPriceDenominatorUnit` end-to-end via new `PriceDenominatorInherited` `QualifierBinding` subtype, updating all five consumer sites

**Stakes**: medium (observable diagnostic surface change is null; the change is an internal DU expansion with five consumer arms to update).

**The choice**: Add `PriceDenominatorInherited : QualifierBinding`. Update `MapQualifierBinding`, `ShouldSkipPairwiseQualifierChecks`, `ResolveBinaryQualifierAxis`, both `ResolveQualifierFromExpression` forms in `ProofEngine.Qualifiers.cs`, and `RichHoverFactory`'s hover-time switch. The proof-engine arm and the assignment-side arm both call a new shared helper `ResolvePriceDenominatorQualifier(priceOperand, axis, semantics)` that projects the compound `'C/U'` qualifier onto its denominator on the requested axis.

**Rationale**: The catalog already declares the policy; without the binding, the catalog metadata is dead. The shape — "result inherits one half of a compound qualifier from one operand" — matches `CompoundDimensionElevationRequired` exactly (mirror image: numerator vs denominator). Five consumer arms must be updated in the same pass because every existing `QualifierBinding` subtype is handled at all five sites and silent fall-through to a `_` catch-all would degrade downstream proofs and hover output.

**Tradeoff accepted**: The `QualifierBinding` DU grows from six to seven subtypes; each addition multiplies the consumer-arm fan-out by N. With two compound-inheritance cases now (the existing `CompoundDimensionElevationRequired` and the new `PriceDenominatorInherited`), a future refactor that introduces a `CompoundInherited(side: Numerator|Denominator)` subtype with an internal discriminator becomes attractive — but with only two cases the per-rule subtype is clearer and reads better at every consumer arm.

**Alternatives considered**:

- *Alternative A1: reuse `CompoundDimensionElevationRequired`.* Rejected. Its resolver arm derives from the compound-quantity *numerator*, not from a price *denominator*. A reused subtype would force every consumer arm to branch on `ResolvedOp` or `binOp.Left.ResultType` to pick the derivation path — pushing dispatch inside the arm body, the exact anti-pattern the DU eliminates.
- *Alternative A2: encode the policy as data on a generic `CompoundInherited(side: Numerator|Denominator)` subtype.* Rejected for now. With two cases the per-rule subtype is clearer. Revisit if a third or fourth compound-inheritance case lands.
- *Alternative A3: skip the binding entirely and synthesize the result qualifier in `TypeChecker.Expressions.cs` directly on the `TypedBinaryOp`.* Rejected. Bypasses the proof engine's resolver, which is the canonical site for transitive qualifier resolution across nested binary ops. `(Budget - Spent) / UnitCost` wouldn't inherit because the outer expression sees a `TypedBinaryOp` whose `ResultQualifier` is `null`.
- *Alternative A4: update only the proof-engine arms; leave the type checker and LS hover sites on the `_` catch-all.* Rejected. The catch-all in `ResolveBinaryQualifierAxis` returns `Absent`, breaking acceptance criterion 3 for the assignment-side qualifier resolution. The catch-all in `RichHoverFactory` returns "left-or-right" which is incorrect hover output. The catch-all in `ShouldSkipPairwiseQualifierChecks` defaults to "do not skip," which may emit spurious PRE0070-class diagnostics for legitimate `money / price`.

**Precedent**:

- In-tree: `CompoundDimensionElevationRequired` (`SemanticIndex.cs:262`) with its resolver arms at `ProofEngine.Qualifiers.cs:363-373` (statement) and `:495-502` (expression). Decision A is the mirror image — denominator-side inheritance.

**Sources consulted (with verbatim excerpt)**:

- `src/Precept/Pipeline/SemanticIndex.cs:257-262`:
  > *"/// Proof that price divided by compound-quantity needs dimension elevation resolution. / The price's denominator dimension must match the compound-quantity's denominator dimension, / and the result carries the compound-quantity's numerator unit. / public sealed record CompoundDimensionElevationRequired : QualifierBinding;"*
- `src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:343-356` (the previously-undisclosed consumer site):
  > *"Switches on QualifierBinding subtypes via expression-arm pattern matching. Default arm: `_ =&gt; new(axis, QualifierResolutionKind.Absent, null)` — without an explicit `PriceDenominatorInherited` arm, the assignment-side qualifier resolution returns Absent, defeating Decision A's downstream guarantee."*
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs:1002-1017` (the previously-undisclosed consumer site):
  > *"Hover dispatch switches on QualifierBinding subtype. The `_` catch-all returns 'left-or-right', producing wrong hover answer for compound-inheritance cases."*

---

### Decision B — Emit PRE0137 (CrossCountingUnitOperation) from the type checker for `quantity × quantity` (and `÷`) when both operands are dimensionless with differing named units

**Stakes**: medium (observable diagnostic surface change — same code as the additive case, but a new trigger family).

**The choice**: Extend the **existing** PRE0137 emission site in `TypeChecker.Expressions.cs:1223-1240` to fire for multiplication and division — not just additive/comparison ops. The dimensional-product proof requirement (`TryDimensionalProductProof`) and PRE0157's emission path are **not touched**. The unit-name discrimination check lives entirely in the type checker.

Implementation shape: today the cross-counting-unit check (`TypeChecker.Expressions.cs:1223-1240`) is nested inside the `!opComposesDimensions` gate (`:1209`) alongside the cross-dimension check (PRE0071 emission at `:1216-1220`). The refactor splits the gate: PRE0071 stays gated to `!opComposesDimensions` (cross-dimension only makes sense for additive ops), but the dimensionless-unit-name check lifts out of the gate so it runs for `+ - × ÷` uniformly. The two checks have always been semantically independent — they were nested only because both happened to live in the additive-ops branch.

```csharp
// In TypeChecker.Expressions.cs around line 1207-1241:
//
// existing PRE0071 check stays inside `!opComposesDimensions` (cross-dimension only
// makes sense for additive arithmetic; multiplication legitimately composes dimensions).
if (qualifierLeft.ResultType == TypeKind.Quantity && qualifierRight.ResultType == TypeKind.Quantity
    && enforcePairwiseQualifierChecks
    && !opComposesDimensions)
{
    // ... existing PRE0071 CrossDimensionArithmetic check (unchanged) ...
}

// LIFTED: dimensionless-unit-name check runs for + - × ÷ uniformly.
if (qualifierLeft.ResultType == TypeKind.Quantity && qualifierRight.ResultType == TypeKind.Quantity
    && enforcePairwiseQualifierChecks)
{
    if (TryGetQualifierDimensionVector(leftQualifiers.Value, out var leftVector)
        && TryGetQualifierDimensionVector(rightQualifiers.Value, out var rightVector)
        && leftVector.Equals(DimensionVector.None)
        && rightVector.Equals(DimensionVector.None))
    {
        var leftUnitCode = GetUnitCodeFromQualifiers(leftQualifiers.Value);
        var rightUnitCode = GetUnitCodeFromQualifiers(rightQualifiers.Value);
        if (!string.IsNullOrWhiteSpace(leftUnitCode)
            && !string.IsNullOrWhiteSpace(rightUnitCode)
            && !StringComparer.OrdinalIgnoreCase.Equals(leftUnitCode, rightUnitCode))
        {
            ctx.Diagnostics.Add(Diagnostics.Create(DiagnosticCode.CrossCountingUnitOperation, span,
                GetOperandName(qualifierLeft), leftUnitCode,
                GetOperandName(qualifierRight), rightUnitCode));
            return;
        }
    }
}
```

PRE0137 is emitted directly from the type checker (the existing emission machinery — `Diagnostics.Create(DiagnosticCode.CrossCountingUnitOperation, ...)`). The proof engine's `DimensionalProductProofRequirement` continues to discharge unchanged: when both operands are dimensionless with matching unit names, `None × None = None = count` discharges cleanly. When the dimensional product has a non-curated vector (e.g., `kg × m → mass·length`), PRE0157 fires as before. The two diagnostics stay cleanly disjoint:

- **PRE0137 `CrossCountingUnitOperation`** — fires from type checker. Trigger: two quantity operands in the same `count` dimension with different explicit unit names, under any qualifier-equivalence-respecting operator (`+ - × ÷` and comparison/same-match-function-call).
- **PRE0157 `IncompatibleDimensionalProduct`** — fires from proof engine. Trigger: `quantity × quantity` whose composed dimension vector is not in the curated business-domain set. Operands have non-`None` dimensions; the product is the dimensional composition.

Authors writing `each + box` and `each × box` see the same PRE0137 code — the same fix (explicit conversion via `quantity in 'each/box'`) applies to both.

PRE0137's `TriggerCondition` text (`Diagnostics.cs:1253`) is updated to cover the broader trigger family. Draft text:

> *"Two `quantity` values combine via comparison, same-match function call, or arithmetic where both operands carry different explicit counting units (e.g., `'each'` vs `'box'`) in the shared `count` dimension. The shared dimension does not make the units interchangeable — there is no universal conversion factor between named counts."*

The current text mentions only "comparison or same-match function call." The new text broadens to include all qualifier-equivalence operators; the existing emission sites (`TypeChecker.Expressions.cs:1234` for additive/comparison via the same block, `TypeChecker.Expressions.Callables.cs:916` for same-match function calls) are already consistent with this trigger language.

**Rationale**: This is the canonical Precept position, already documented at `business-domain-types.md:397`: counting units share the `count` dimension but are *not* universally convertible. The doc explicitly names PRE0137 for the additive case. The current silent-pass for the multiplicative case is the enforcement gap, not the intended behavior. Decision B is mechanical consistency with documented philosophy — extending PRE0137's trigger from `+`/`-` to `×`/`÷`/etc. on counting units.

The diagnostic identity is the right one: PRE0137 is specifically "same-dimension count quantities whose explicit unit codes differ and have no universal UCUM conversion factor" (`business-domain-types.md:397`, verbatim). That description matches `each × box` exactly — the new arm is not a stretch.

PRE0157 (`IncompatibleDimensionalProduct`) is left untouched. PRE0157's identity is "product dimension is outside the curated business set" — `kg × m` produces mass·length, not a curated dimension. PRE0137 vs PRE0157 are now cleanly disjoint: PRE0157 fires when the product *dimension vector* is non-curated; PRE0137 fires when the product is dimensionless but the operand *unit names* differ. The reviewer-CONCERN about a single diagnostic blurring two distinct semantic surfaces is resolved by keeping the two diagnostics distinct.

**Tradeoff accepted**: Authors who write `each × box` intentionally — to mean "treat as raw count, don't ask me to convert" — must add an explicit conversion (e.g., multiply by a typed `1 each/box` literal). This is the same friction the language already imposes for `each + box`. The friction is the type system's value: it surfaces the conversion the author had in mind so the runtime can carry it as an inspectable artifact.

**Alternatives considered**:

- *Alternative B1: do nothing — keep silent pass.* Rejected. Encodes a falsehood in the type system (that `each` and `box` are interchangeable under multiplication, even though they're not interchangeable under addition). Inconsistent with `business-domain-types.md:397`'s already-stated rule.
- *Alternative B2: emit a warning, not an error.* Rejected. PRE0137's existing severity is `Severity.Error` (for `+`). Splitting severity by which arm of the same diagnostic fired breaks the consistency of the code's identity. Authors expecting the additive rule to apply to multiplication would be surprised.
- *Alternative B3: extend the rule to non-dimensionless cases (require name equality on every `quantity × quantity`).* Rejected as out-of-scope. The dimensional case (`kg × m`) is already governed by PRE0157's business-domain check; cancelling pairs (`kg × 1/kg`) need to remain passing. The dimensionless case is the specific gap.
- *Alternative B4: mint a new diagnostic PRE0160.* Rejected. PRE0137's identity ("same-dimension count quantities whose explicit unit codes differ") covers the new case exactly. Adding a separate code would force MCP consumers (and authors) to learn two codes for the same underlying rule.
- *Alternative B5: extend PRE0157 with a message variant for the dimensionless case.* Rejected after audit. PRE0157's identity is "product dimension not in curated set," which does not fit the new trigger (the product *is* in the curated set as `count`; the issue is the operand unit names). The reviewer correctly flagged that this would blur PRE0157's semantic surface. PRE0137 is the cleaner fit.

**Precedent**:

- **In-tree (canonical philosophy)**: `business-domain-types.md:397` already specifies PRE0137 for `qty_each + qty_box`. Decision B is mechanical extension of the same rule to multiplication.
- **In-tree (analogous enforcement)**: `QualifierCompatibilityProofRequirement` on `QuantityPlusQuantity` / `QuantityMinusQuantity` (`Operations.cs:525-526`, `:535-536`) already enforces unit-axis identity for additive operations.
- **Comparator counter-evidence (deliberate divergence)**: F# UoM, Boost.Units, Frink, Pint, Haskell `units` all accept arbitrary multiplicative composition (per `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md:87-101`). Precept diverges deliberately, grounded in `business-domain-types.md:180`'s curation commitment.

**Sources consulted (with verbatim excerpt)**:

- `docs/language/business-domain-types.md:397` (the load-bearing precedent):
  > *"Counting units (`each`, `box`, `case`, `pack`, `pallet`, `dozen`) live in the shared count dimension: they are registered with `DimensionVector.None` and normalize with factor-one semantics. That shared dimension class does **not** make them interchangeable or universally convertible. Cross-counting-unit operations such as `qty_each + qty_box` therefore fail with PRE0137 `CrossCountingUnitOperation`, not PRE0071."*
- `docs/language/business-domain-types.md:180` (the curation philosophy):
  > *"Compile-time dimensional reasoning is exact within the UCUM algebra and curated against the business-domain dimension set. … Products outside the curated set are rejected with `IncompatibleDimensionalProduct` (PRE0157), not silently typed as an unconstrained compound — the type system says 'I don't recognize this product as a business dimension,' rather than 'this is a `quantity` of unclear shape.'"*
- `docs/language/business-domain-types.md:168` (the multiplicative-rejection commitment):
  > *"Cross-dimension multiplication that doesn't cancel is **rejected** (compile error), not silently approximated."*
- `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md:87-101` (comparator counter-evidence):
  > *"Multiplication: always permitted; unit exponents add. `float<m> * float<kg>` → `float<m kg>`."* (F# UoM, paraphrased from the surveyed Microsoft Learn excerpt at that line range.)
- `src/Precept/Pipeline/ProofEngine.Qualifiers.cs:39-44`:
  > *"/// Discharges DimensionalProductProofRequirement: the multiplicative product of the two operand quantities' unit dimension vectors must resolve to a known curated business-domain dimension (per DimensionCatalog). The dimensionless count alias covers cancelling pairs (e.g., kg × (1/kg) → count)."*

## Acceptance Criteria

Decision A:

1. New `PriceDenominatorInherited : QualifierBinding` exists in `SemanticIndex.cs`.
2. `MapQualifierBinding` returns `PriceDenominatorInherited` when `meta.ResultQualifierPolicy == ResultQualifierPolicy.InheritPriceDenominatorUnit`.
3. For `field U as quantity <- M / P` where `M : money in 'USD'` and `P : price in 'USD/each'`, the proof engine resolves `U`'s Unit-axis qualifier to `each` and Dimension-axis to `count`.
4. The same resolution succeeds when wrapped: `field U as quantity <- (M - Spent) / P`.
5. The currency axis on the result of `M / P` resolves to `null` (cancelled). `ensure U.currency == 'USD'` over the resulting `quantity` fails to find a Currency qualifier (correct).
6. `ResolveBinaryQualifierAxis` returns the inherited unit on the Unit/Dimension axis (NOT `Absent`).
7. `RichHoverFactory` on `Budget / UnitCost` shows `quantity in 'each'`, not the catch-all "left-or-right" answer.
8. `ShouldSkipPairwiseQualifierChecks` returns `true` for the new subtype (no spurious PRE0070-class diagnostic on legitimate `money / price`).
9. No existing test regresses.

Decision B:

1. `precept_compile` on `field W as quantity <- E * B` where `E : quantity in 'each'` and `B : quantity in 'box'` emits **PRE0137 `CrossCountingUnitOperation`** from the type checker (not PRE0157 from the proof engine).
2. `precept_compile` on `field W as quantity <- E1 * E2` where both are `quantity in 'each'` succeeds (no diagnostic; proof engine discharges `None × None = count`).
3. `precept_compile` on `field W as quantity <- KG * INV_KG` where `KG : quantity in 'kg'`, `INV_KG : quantity in '1/kg'` continues to succeed (proof engine discharges the cancelling pair to `count`).
4. `precept_compile` on `field W as quantity <- KG * M` (mass × length) continues to emit **PRE0157** from the proof engine with the existing "not a business dimension" message (unchanged behavior).
5. `precept_compile` on `field W as quantity <- E / B` (division of dimensionless quantities with differing names) also emits PRE0137 — the lifted check covers both `×` and `÷`.
6. `precept_compile` on the same-match function-call site (`TypeChecker.Expressions.Callables.cs:916`) and the additive site (`+`/`-`) continue to emit PRE0137 with their existing message; no behavior regression at those sites.
7. Sample-corpus sweep: `precept_compile` on every file in `samples/` — no sample becomes invalid; if any does, the design records which one and confirms the diagnostic identifies a genuine error or fixes the sample in the same PR. **Pre-check on `samples/inventory-item.precept`** (which has `quantity × quantity` operations using interpolated qualifiers `quantity in '{StockingUnit}/{PurchaseUnit}'`): verify that `GetUnitCodeFromQualifiers` short-circuits or returns the interpolated form in a way that does NOT trigger PRE0137 spuriously. If `GetUnitCodeFromQualifiers` returns the braced string verbatim and string-comparison falsely flags it as a mismatch, the check needs an interpolated-qualifier short-circuit.

## Dependencies

- **Downstream: Slice 3 implementation.** Both decisions land in a single slice; the `QualifierBinding` consumer-site sweep is shared infrastructure and splitting would force two passes touching the same files.
- **No upstream dependencies.** The catalog already declares everything needed; PRE0137 already exists.

## Doc-update enumeration

When Slice 3 lands, update:

- `docs/compiler/proof-engine.md` — describe the new `PriceDenominatorInherited` binding in the qualifier-resolution section. No change to the dimensional-product proof section (PRE0157 emission via `TryDimensionalProductProof` is unchanged).
- `docs/compiler/type-checker.md` — describe the lifted PRE0137 check (covers `+ - × ÷` for dimensionless operands with differing unit names); note the split from PRE0071 (which stays gated to additive ops).
- `docs/compiler/type-checker.md` — describe the new `MapQualifierBinding` arm. **Note the existing drift**: this doc currently enumerates only 2 of the 6 (soon 7) `QualifierBinding` subtypes; the Slice 3 doc-sync should list ALL subtypes, not just add the 7th to a 2-item list.
- `docs/compiler/diagnostic-system.md` — update PRE0137's TriggerCondition to cover both additive and multiplicative cases; cross-link to PRE0157 to clarify the disjoint coverage (PRE0137: counting-unit name mismatch on either `+`/`-` or `×`/`÷`; PRE0157: product dimension outside curated set).
- `docs/language/business-domain-types.md` — extend the existing counting-unit paragraph at line 397 to note that multiplicative operations also fail with PRE0137 (currently the paragraph names only `+`).
- `docs/Working/qualifier-and-dimensionless-product-design.md` — Status field flips to `Promoted to: <links>` at promotion.

No `README.md`, `docs/philosophy.md`, or `docs/language/catalog-system.md` updates required (the diagnostic count doesn't change — PRE0137 already exists).

## Falsifiers

External-author-visible behavior change (PRE0137 now fires on multiplication). Observations that would force redesign:

- **F1**: If three or more samples in `samples/` produce spurious PRE0137 emissions where the author's intent was clearly meaningful count arithmetic, the rule is over-strict on multiplication. Likely fix: relax the rule to fire only when the result type lacks a unit annotation (i.e., let the assignment context guide acceptance).
- **F2**: If domain-expert usability testing shows confusion that PRE0137 covers both `+` and `×` (authors expect the multiplicative case to be a different diagnostic), revisit B4 — mint PRE0160 for the multiplicative case despite the duplication.
- **F3**: If the `ResolvePriceDenominatorQualifier` helper turns out to need different behavior on the proof-engine side vs the assignment-side path (acceptance criterion 6 fails after implementation), the "single shared helper" claim breaks down — split the helper and document the split inline.

## Open Questions

None blocking. Three implementer notes:

1. **Helper reuse**: the lifted PRE0137 check at the type-checker site (`TypeChecker.Expressions.cs:1207-1241`) uses existing helpers — `TryGetQualifierDimensionVector`, `GetUnitCodeFromQualifiers`, `GetOperandName`. No new helper needed for Decision B; the previously-proposed `ExtractUnitName` in the proof engine is dropped along with the proof-engine modification.
2. **Literal elaboration interaction**: `3 box` literal against a `quantity in 'each'` field — the literal's elaborated unit is `box`, the field's is `each`. The existing literal-elaboration pipeline attaches the unit code to the typed literal's qualifier metadata; `GetUnitCodeFromQualifiers` returns the literal's unit code as it would for a field reference. The cross-counting-unit check fires uniformly across literal-vs-field and field-vs-field cases.
3. **Interpolated qualifier short-circuit**: when a quantity field is declared with an interpolated qualifier like `quantity in '{StockingUnit}/{PurchaseUnit}'`, `GetUnitCodeFromQualifiers` may return the braced string verbatim or short-circuit to null. Acceptance criterion 7 obligates a pre-check on `samples/inventory-item.precept` to verify that interpolated qualifiers don't trigger PRE0137 spuriously. If they do, the lifted check needs a guard: skip the discrimination when either operand's unit code contains `{` (interpolated). This is a pre-existing concern that already governs the additive site at `TypeChecker.Expressions.cs:1234`; the lift inherits its handling.

## Slice coupling acknowledgment

Decisions A and B are mechanically independent (different files, different concerns: DU expansion vs type-checker gate refactor). A precept-reviewer audit flagged the coupling as a NIT — a future regression in either area could complicate bisectability. The decisions are bundled because both surface from the same code-review remediation pass, but the implementer should structure commits separately within the slice (one commit per decision) to preserve revert granularity.
