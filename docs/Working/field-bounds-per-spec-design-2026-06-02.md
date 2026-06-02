---
status: Externally-Grounded
phase-target: Phase 8 (emission architecture / soundness) — with one sub-item (relational-rule narrowing) carrying a Phase-target-TBD open question
comparable-systems-research-status: strong — grounded in research/language/references/constraint-composition.md (Zod/FluentValidation/Drools/Alloy/OCL/FHIR predicate-combinator survey with verbatim excerpts) plus the per-decision inline excerpts below
sources-consulted:
  - "docs/philosophy.md — prevention/determinism/totality/static-completeness commitments; the 'proves it will [succeed]' line and 'no unproven evaluation faults' the bound work must preserve."
  - "docs/language/precept-language-spec.md §0.1 — the eleven Design Principles (7 compile-time-first, 9 mandatory rationale, 10 totality, 11 static completeness) that the bound obligations must satisfy."
  - "docs/language/precept-language-spec.md §0.6 — Proof Engine Design Contract items 1 (numeric interval reasoning), 2 (relational reasoning over multiple fields), 6 (assignment range impossibility), and the soundness-over-completeness posture."
  - "docs/language/precept-language-spec.md §0.7 — the Compile-Time and Runtime Guarantee Contract: prove-or-reject, no deferral; the locked soundness frame."
  - "docs/language/precept-language-spec.md §2.4 — Field Modifiers: 'constraint modifiers are shorthand for rules'; the _Expr_ value slot; proof participation is a function of decidability, not syntactic form."
  - "docs/language/precept-language-spec.md §3.5 — Scope Rules: the modifier-value-expression row (all field names, forward refs, self-vacuous, mutual-satisfiable) and the evaluation-model derivation."
  - "docs/language/precept-language-spec.md §3.6 — Binary operators: numeric lane/widening rules (integer→decimal/number; decimal vs number is a type error) governing cross-lane bound comparison."
  - "docs/language/precept-language-spec.md §3.8 — Modifier validation: applicability table, modifier-value-validation table (InvalidModifierBounds / InvalidModifierValue / DuplicateModifier)."
  - "docs/language/precept-language-spec.md §3A.1 — Constraint Semantics: rules are field-scoped (no event args), collect-all evaluation."
  - "docs/compiler/proof-engine.md — Strategy 1 (Literal), Strategy 4 (flow-narrowing, subtraction-only, guard-driven), Strategy 8/9/10 (Interval/Length/Count containment), the IntervalContainmentProofRequirement record + normalization boundary, catalog-driven obligation instantiation."
  - "docs/Working/proof-engine-contract-grounding-2026-06-02.md — the per-fault-class breach table; identifies BUG-017/018/019 as the same 'obligation creation gated on provability' shape and the prove-or-reject fix principle."
  - "docs/Working/dynamic-modifier-bounds-research-2026-06-01.md — the canon-only derivation D1–D5 and the eight open questions (Q1 admission policy, Q2 data model, Q3 InvalidModifierBounds, Q4 vacuous/mutual, Q5 cross-lane, Q6 qualifier, Q7 computed-field bound, Q8 event-arg exclusion)."
  - "docs/Working/bugs.md — BUG-017 (computed-field unbounded-operand skip), BUG-018 (mincount/maxcount dead), BUG-019 (length non-literal skip)."
  - "research/language/references/constraint-composition.md — predicate-combinator theory; Zod .min()/.refine(), FluentValidation GreaterThan(x => x.Debt*2), Drools/Alloy/OCL/FHIR; desugaring + resugaring; value-narrowing classified as the absent capability."
  - "src/Precept/Pipeline/TypeChecker.cs:559-577 — declaredMinBound/etc extracted via TryGetComparableModifierValue; null bound is dropped silently."
  - "src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:462-480 — TryGetComparableModifierValue recognizes only NumberLiteral / TypedConstant / negated-NumberLiteral; field-reference (IdentifierExpression) falls through to `_ => null`."
  - "src/Precept/Pipeline/ProofEngine.Analysis.cs:452-454 — CollectComputedFieldBoundObligations: `if (interval.IsUnbounded) continue;` (BUG-017 root)."
  - "src/Precept/Language/Actions.cs:285-336 — GenerateIntervalContainmentObligations: numeric interval obligation created whenever target has bounds (sound); length obligation gated on `inputAction.InputExpression is TypedLiteral{String}` (BUG-019 root); no count obligation generated (BUG-018 root)."
  - "src/Precept/Pipeline/ProofEngine.Lengths.cs:21-42 — TryLengthContainmentProof returns null on non-literal; TryCountContainmentProof is `=> null` hard stub (BUG-018)."
  - "src/Precept/Pipeline/ProofEngine.Intervals.cs:67-70,175-181 — TypedFieldRef interval lookup uses ExtractFieldInterval, which reads only GetFieldBounds (declared modifiers); rule-derived bounds are NOT folded in."
  - "src/Precept/Pipeline/ProofEngine.Satisfiability.cs:38,270-298 — rules feed contradiction/vacuity scanning (PRE0155/0154/0159) but not obligation-discharge interval narrowing."
  - "src/Precept/Language/ProofRequirement.cs:190-225 — IntervalContainmentProofRequirement (decimal? bounds), LengthContainmentProofRequirement (int?), CountContainmentProofRequirement (int?) DU subtypes."
  - "src/Precept/Language/FaultCode.cs:44-54 + DiagnosticCode.cs:78-79,135-136,162-168 — NumericOverflow(78)/OutOfRange(79)/LengthBoundViolation(135)/CountBoundViolation(136); each [StaticallyPreventable]; CountBoundViolation is declared-preventable but never emitted."
  - "Fresh-build probe via Compiler.Compile(...).Diagnostics (scratch test, run + deleted) — confirmed all five observations on a current build; see § Current-state findings."
---

# Field Bounds Per Spec — Design

## Goal

When done, every bound modifier (`min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`) behaves as §2.4/§3.5 specify — including bounds whose value is a field reference — and every mutation or computed result that can violate a declared bound is either proven safe or rejected at compile time, so a precept that compiles without diagnostics cannot fault at runtime on a bound (Principle 11). Demonstrated by: a field-reference bound participating in proof exactly as the equivalent `rule` does; BUG-017/018/019 each producing a diagnostic instead of compiling clean; and a non-decidable bound producing an obligation diagnostic, never a silent pass.

## Scope

- **In scope**:
  - Literal and typed-constant bounds for all six bound modifiers (already partly built; the count/length non-literal gaps close here).
  - Field-reference bounds (`field Amount as integer min Floor`) — desugaring, scope, typing, and proof participation, per §2.4 + §3.5.
  - BUG-017: computed-field interval obligation must be created (and reject) when the operand interval is unbounded.
  - BUG-018: `mincount`/`maxcount` enforcement on collection mutations — the count-containment obligation generator + prover, reviving `CountBoundViolation`.
  - BUG-019: `minlength`/`maxlength` enforcement on non-literal string RHS — length obligation for non-literal assignments.
  - The "unprovable bound ⇒ emit, never skip" correction applied uniformly to the interval/length/count obligation collectors.
- **Out of scope**:
  - The runtime ingress half of governance (Create/Fire/Update/FromJson) — `NotImplementedException` today; the bound's runtime enforcement lands when the executable model lands (the contract-grounding doc resolves this as a build gap, not a contradiction). This design is compile-time only.
  - New keyword/type/operator surface. Bounds already exist; this design adds no token.
  - `maxplaces` semantics (a type-stage check, already sound per the contract grounding).
- **Deferred to future** (carried as open questions, not built here):
  - Whether a field-referencing **relational rule** (`rule X >= Y`) and a field-reference **bound** should narrow the *subject's interval* for downstream obligation discharge (observation B) — this is a proof-engine reach beyond what §0.6 item 2 currently obligates, and its admission policy (Q1) gates it.
  - Computed field as a bound value; cross-lane and qualifier-bearing field-reference bounds (research Q5/Q6/Q7).

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | A bound the engine cannot prove satisfiable now rejects rather than compiling clean (§0.7 prove-or-reject); BUG-017/018/019 are prevention holes being closed. | A field-reference bound whose referenced field is unbounded cannot be *statically* prevention-checked; only runtime-enforced (research D4). | Accept that such a bound is runtime-enforced governance, not compile-time prevention — but it must never silently weaken a *downstream* proof (it leaves dependents unresolved → emit). |
| 2. One file, complete rules | Y | A field-reference bound keeps the bound and its referent in the same file; no external oracle (§0.6 item 4). | N/A | N/A |
| 3. Determinism | Y | `Amount >= Floor` is a pure expression over current field values (§0.4 expression purity); same data → same verdict. | N/A | N/A |
| 4. Full inspectability | Y | Each bound desugars to an inspectable rule; the obligation + its disposition surface through the proof ledger (§0.6 item 6). The generated rationale for a field-ref bound is author-readable. | N/A | N/A |
| 5. Keyword-anchored readability | N | No new keyword; `min`/`max`/… already anchor the modifier. The bound value moves from literal-only to any expression — no grammar-shape change (§2.4 already types the slot `_Expr_`). | N/A | N/A |
| 6. Governance not validation | Y | The desugared rule is enforced structurally on every operation, not at a call boundary (§3A.1, §0.7 governance half). | N/A | N/A |
| 7. Compile-time totality | Y | The bound obligations are discharged before any instance exists; unprovable ⇒ diagnostic (§0.1 P7). | A field-ref bound on an unbounded referent yields no compile-time interval. | The bound is still legal (per §2.4 decidability-not-legality); it simply contributes no static range — the friction is a false-negative (author may need to bound the referent), the safe direction. |
| 8. Honesty about approximation | N | Bounds are exact comparisons; no approximation introduced. Quantity/price bounds already normalize via UCUM (§0.6 item 5) — unchanged. | N/A | N/A |
| 9. Mandatory rationale | Y | The field-reference bound carries a generated rationale exactly as the literal bound does (§2.4: "rule whose reason the compiler supplies"). | The generated wording for a non-literal bound is unspecified by canon (research gap 4). | Accept a mechanical generated wording (e.g. "Amount must be at least Floor"); an author wanting a custom reason writes the longhand `rule` (§2.4). |
| 10. Totality | Y | A bound predicate introduces no fault site itself; the work ensures bound *violations* by mutations/results are proven-or-rejected (§0.1 P10). | N/A | N/A |
| 11. Static completeness | Y | This is the central principle the work serves: every `[StaticallyPreventable]` bound fault (NumericOverflow, OutOfRange, LengthBoundViolation, CountBoundViolation) gets a live emission path; CountBoundViolation stops being dead code. | N/A | N/A |

**Tradeoffs accepted (rows with non-N/A tension):** Principles 1, 7, 9. The unifying tradeoff (1 & 7): a field-reference bound against an unbounded referent is admitted as a *legal, runtime-enforceable* constraint but contributes no compile-time interval; this is justified because §2.4 explicitly makes decidability govern *proof participation*, not *legality*, and the soundness-safe direction (leave dependents unresolved → emit) is preserved — the engine never over-proves. For Principle 9, the generated-rationale wording for non-literal bounds is mechanical rather than bespoke; justified because §2.4 already provides the escape hatch (write the longhand `rule … because`).

**Companion commitments.** Stateless-first-class: bounds and the bug fixes apply identically to stateless precepts (no state dependency); BUG-017/019 repros are stateless. Domain-expert-primary-author: `min Floor` reads as plain domain vocabulary ("the amount must be at least the floor") — arguably *more* readable than forcing a literal duplicate of a value already named by a field; the error message (below) is written in domain terms.

## Language Design Grounding

> Surface note: this design touches a modifier's **value position** (literal → any field-scoped expression). It does not add a token, keyword, type, operator, or construct. Grounding is provided because the value-position change is a (small) language-surface change.

**General language design.** Declaration-local, type-attached value constraints whose bound can reference *another field* are mainstream in constraint/validation languages, and the field surveys the comparable systems:

- **Zod** composes scalar bounds (`.min()`, `.max()`) on a field and cross-field relations via `.refine()` — two syntactically distinct combinators (constraint-composition.md §2: *"Type-local constraints (`positive()`, `min()`, `max()`) are composed as a method chain. Cross-field constraints are composed via `.refine()`."*). Precept deliberately **unifies** these: a field-reference bound is the *same* `min` modifier with a field-valued argument, and it desugars to the same rule a `.refine()` would express. Precept can unify because §2.4 already defines the modifier as rule-shorthand — the scalar/relational split Zod needs at the API level collapses to one desugaring at Precept's.
- **FluentValidation** already expresses the field-reference bound directly: `RuleFor(x => x.Income).GreaterThan(x => x.Debt * 2)` (constraint-composition.md §3) — the bound argument is a lambda over *another* property. This is the closest precedent to `min Floor`: a per-field bound whose value is another field. Precept takes the co-location and the field-valued bound; it diverges by resolving at compile time (FluentValidation resolves at validation call time) and by proving the relation statically where decidable, which FluentValidation never attempts.
- **Drools** (`annualIncome >= existingDebt * 2`, constraint-composition.md §4) and **OCL** (`self.income >= self.debt * 2`) both put a field-to-field relation in a single named/keyed constraint — the desugared form of a Precept field-reference bound.

The PLT framing (constraint-composition.md § Formal Concept) is **predicate combinator design** over a Boolean lattice: each bound is a predicate `P: EntityState → Bool`, and a field-reference bound is just a predicate whose free variables include a second field. The desugaring is the **resugaring** discipline (Pombrio & Krishnamurthi, cited in constraint-composition.md): the violation must attribute to the modifier site, not the synthesized rule. constraint-composition.md explicitly classifies **value narrowing** as the absent capability (*"No narrowing of *value* constraints … a medium-high type-checker change"*) — which is exactly the deferred observation-B question.

Research file: `research/language/references/constraint-composition.md` is the on-point domain study (predicate combinators, scope theory, desugaring). The dynamic-modifier-bounds research (2026-06-01) is the Precept-specific canon derivation.

**Precept-specific application.** This touches §2.4 (constraint modifiers are rule shorthand; the `_Expr_` value slot — *extends*, by making the non-literal expression actually work), §3.5 (modifier value-expression scope — *implements* the already-written rule-condition scope), §0.6 items 1–2 + 6 (numeric interval + relational reasoning + assignment-range impossibility — *implements* the obligation kinds), §0.7 (prove-or-reject — the soundness frame the bug fixes restore). It risks conflicting with nothing locked: `business-domain-types.md` D6 rejects a `units {}` block (unrelated); there is no locked rejection of field-reference bounds (research gap 6, verified in full read).

## Audience and Teachability

**Worked example** (apartment-rental domain — a deposit floored by one month's rent, both fields the domain expert already names):

```precept
precept RentalApplication
field MonthlyRent as money in 'USD' default '0.00 USD' positive
field SecurityDeposit as money in 'USD' default '0.00 USD' min MonthlyRent
event Submit(Rent as money in 'USD', Deposit as money in 'USD') initial
on Submit
    -> set MonthlyRent = Submit.Rent
    -> set SecurityDeposit = Submit.Deposit
```

`min MonthlyRent` says, in domain vocabulary, "the deposit can't be less than a month's rent." The author names the relationship with the field they already declared, instead of hardcoding a number that would drift from `MonthlyRent`.

**Error message** (a plausible misuse: bounding by a field that carries no lower bound, so a downstream computation can't be proven). For the **bug-fix** family the message a domain expert most plausibly triggers is the count one (BUG-018):

```
PRE0136  CountBoundViolation
  Adding to 'Attendees' can exceed its limit of 5 — this event may push the
  count past the maximum. Guard the action (e.g. `when Attendees.count < 5`)
  or raise the maxcount.
```

This serves the domain-expert reader because it names the field (`Attendees`), the declared limit (5) and the corrective action in their terms ("guard the action" / "raise the maxcount"), not "CountContainmentProofRequirement unresolved at obligation site."

**10-minute teaching path:**
1. `precept-language-spec.md §2.4` (Field Modifiers — the bound table + the rule-shorthand paragraph) — ~3 min.
2. `precept-language-spec.md §3.5` (the modifier-value-expression scope row) — ~2 min.
3. One sample with bounds, e.g. `samples/hotel-reservation-management.precept` (`field AdultCount as integer default 1 positive max 6`) — ~3 min.
4. The error-message recovery hint above (no doc read needed) — ~1 min.

## Semantic Rules

**Desugaring (reduction).** A bound modifier desugars to a guarded-equivalent rule with a generated rationale (§2.4):

```
field X as T min e   ⟹   field X as T  +  rule X >= e  because "<generated>"
field X as T max e   ⟹   field X as T  +  rule X <= e  because "<generated>"
field X as T minlength e ⟹ rule X.length >= e   (T = string)
field X as T maxlength e ⟹ rule X.length <= e   (T = string)
field X as T mincount e  ⟹ rule X.count  >= e   (T a collection)
field X as T maxcount e  ⟹ rule X.count  <= e   (T a collection)
```

`e` is any field-scoped expression (literal, typed constant, or field reference). The desugaring is uniform over `e`'s shape — this is the core claim §2.4 already makes.

**Typing.** The bound value-expression is type-checked in the field's scope; the comparison must be well-typed under §3.6:

```
  Γ ⊢ e : τ_e      τ_e comparable-to τ_field under §3.6 lane rules
  ─────────────────────────────────────────────────────────────────
       Γ ⊢ field X as τ_field min e   ok
```

For numeric fields, `integer` bound widens to a `decimal`/`number` field; `decimal` vs `number` is a type error absent a bridge (§3.6) — i.e. a field-reference bound obeys the same lane rules as the comparison it desugars to (research D5, Q5 confirms canon implies this but does not state it for the modifier position — see Open questions).

**Scope (§3.5).** The bound value-expression has rule-condition scope: all field names, any declaration order, forward references allowed, **event args excluded** (inherited from the parent rule's field-only scope, §3A.1). Self-reference is vacuous (`min X` ⟹ `X >= X`), mutual reference conjoins (`A min B` + `B min A` ⟹ `A == B`) — neither is a cycle (§3.5).

**Proof obligations.** Two obligation families, both already in the catalog DU (`src/Precept/Language/ProofRequirement.cs:190-225`):

1. *Containment* (the value placed in the field stays within the field's bound): `IntervalContainmentProofRequirement` (numeric), `LengthContainmentProofRequirement` (string), `CountContainmentProofRequirement` (collection). Created for **every** bounded mutation/computed-result regardless of whether the result interval is bounded; discharged by Strategy 8/9/10; **unresolved ⇒ emit** (the BUG-017/018/019 correction).
2. *Decidability of a field-reference bound* (does `X >= Floor` contribute a provable range to `X`?): governed by the §2.4 decidability test — `X`'s lower bound is provable only insofar as `Floor`'s own declared bounds make it so. When `Floor` is unbounded, the bound contributes the trivial range and any *downstream* obligation that needed `X`'s lower bound stays unresolved → emits.

**Soundness preservation.** Principles 7, 10, 11. Principle 11 holds because every bound fault class (`NumericOverflow`, `OutOfRange`, `LengthBoundViolation`, `CountBoundViolation`) gains a live obligation path that emits when safety is unprovable — closing the three breaches where the obligation was never created. Principle 10 holds because no bound modifier introduces an expression whose evaluation is undefined; the comparison `X op e` is total. Principle 7 holds because the engine proves what it can (decidable bounds) and rejects what it cannot prove safe (unbounded operand/RHS that can violate a declared bound), and **never** compiles a violable bound in the hope of a runtime check (§0.7 no-deferral) — the inversion that caused BUG-017/018/019.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Three layers, each matching where the language already places the concern:
- *Catalog* — no new `ProofRequirementKind`; the Interval/Length/Count containment subtypes already exist. (If observation B / field-reference-bound discharge needs a relational variant, that is a catalog DU addition — flagged as Open question Q2, not built here.)
- *Type checker* — bound extraction (`TryGetComparableModifierValue`) is where the literal-only restriction lives; field-reference support is added here. The desugaring-to-rule and scope are already type-checker responsibilities.
- *Proof engine* — obligation generators (`Actions.cs`, `ProofEngine.Analysis.cs`) and provers (`ProofEngine.Lengths.cs`) are where the three breaches live; the fix is "create the obligation unconditionally, let discharge return false/unresolved." This belongs in the proof engine because it is obligation discharge, not type checking.

Why not catalog metadata for the bug fixes? The breaches are not "the language says X"; they are obligation-creation gating bugs in pipeline code. The catalog already declares the requirement kinds and the `[StaticallyPreventable]` fault mappings correctly; the pipeline just fails to instantiate them. Pipeline placement is correct.

**Cross-component propagation:**
- Runtime (parser, type checker, evaluator, diagnostics): parser — None (the `_Expr_` slot already parses any expression). Type checker — field-reference bound resolution + scope/type validation; `InvalidModifierBounds` semantics for non-literal pairs (Open question Q3). Evaluator — None at compile-time scope (runtime enforcement is out of scope here). Diagnostics — `CountBoundViolation` (PRE0136) revived; `LengthBoundViolation`/`NumericOverflow`/`OutOfRange` newly reachable on more paths; a possible new "cannot prove result within bound" message for the unbounded case (Open question Q1 affects wording).
- Tooling (syntax highlighting, completions, hover, semantic tokens): hover/`precept_proofs` already surface obligations; a field-reference bound's obligation flows through the existing ledger — None beyond data. Completions inside a `min `/`max ` position could offer field names (additive, not required for correctness) — noted, not specified here.
- MCP (vocabulary, DTOs, tool output): None — the obligation DTOs already carry the containment requirement shapes; revived `CountBoundViolation` flows through the existing diagnostic projection.

**Breaking changes.** No public-contract break. Diagnostic *codes* are unchanged (reusing 78/79/135/136). Behaviorally, definitions that previously compiled clean and were silently unsound (the three bug repros, plus any field-reference bound that was silently dropped) will now emit — a *correctness* change, not an API break, and the desired direction. Corpus impact must be measured (Falsifiers).

### External architectural precedent

**CUE** (constraint propagation via lattice unification) is the most relevant comparator for the field-reference-bound discharge question. CUE evaluates a value as the unification (meet) of all constraints bearing on it; a reference to another field is resolved in the same lattice, and a constraint like `a: >=b` propagates `b`'s bounds into `a`'s when `b` is concrete (CUE docs, "References and Visibility" / "The Value Lattice": *a field's value is the greatest lower bound of all constraints applied to it*). Precept **takes** the principle that a field-reference bound is a constraint that *propagates* the referent's known range; it **diverges** by refusing the general fixpoint CUE permits — Precept's §0.4 no-loops/no-reconverging-flow forbids the iterative unification CUE runs, so Precept proves only the *bounded, acyclic* relational closure (a single conjunction of declared intervals), and where that closure is non-decidable (unbounded referent) it declines rather than iterating. This divergence is principled: CUE accepts opaque/iterative evaluation; Precept's inspectability + bounded-strategy commitments (§0.6 proof philosophy 3, proof-engine.md Decision 1 "Bounded Strategy Set vs SMT Solver") require the relational reasoning stay a finite, legible strategy.

## Inventory of what will be built

File-level. (The relational-narrowing item is gated by Open question Q1 and is *not* built unless authorized.)

**Field-reference bounds (literal-parity):**
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs` — `TryGetComparableModifierValue` (L462-480): add an `IdentifierExpression` (and member-access for `.length`/`.count`? — see Q-list) arm that resolves a field reference. Distinguish "constant-foldable bound" (current `decimal?` path) from "field-reference bound" (no static magnitude). The latter must NOT be dropped to `null` silently — it must either populate a field-reference bound representation or be routed per Q1.
- `src/Precept/Pipeline/TypeChecker.cs:559-577` — the `declaredMinBound`/etc extraction must carry the field-reference case forward (today a `null` bound is silently dropped). New `TypedField` carrier for a field-reference bound, OR the desugared-rule path (depends on Q2).
- Type/scope validation: field-reference bound resolves in field scope (reject event-arg refs → reuse the rule event-arg-exclusion diagnostic); lane-compatibility check per §3.6 (Q5).
- `InvalidModifierBounds` (`TypeChecker.Validation.Modifiers.cs:439-447`) — define behavior when one/both of `min`/`max` are field references (Q3).

**BUG-017 (computed-field unbounded operand):**
- `src/Precept/Pipeline/ProofEngine.Analysis.cs:452-454` — remove the `if (interval.IsUnbounded) continue;` short-circuit in `CollectComputedFieldBoundObligations`; create the `IntervalContainmentProofRequirement` whenever the field has bounds (mirroring the sound set-action path in `Actions.cs:285`). Strategy 8 already returns false on unbounded (`Intervals.cs:351,376`) → Unresolved → emit.

**BUG-018 (count containment):**
- `src/Precept/Language/Actions.cs` — new `GenerateCountContainmentObligations` (or extend the existing generator): for `add`/`append`/`enqueue`/`push`/`insert`/`put` (grow) and `remove`/`dequeue`/`pop`/`clear` (shrink) on a field with `mincount`/`maxcount`, emit a `CountContainmentProofRequirement`.
- `src/Precept/Pipeline/ProofEngine.Lengths.cs:41-42` — replace `TryCountContainmentProof => null` with a real discharge: prove the post-mutation count stays within bounds using the count-interval model (the §0.6-corrected grow/shrink boolean model is too weak for exact bounds; needs at least a count interval — see Dependencies). Unprovable ⇒ false/unresolved ⇒ `CountBoundViolation` (PRE0136) emitted.
- Revives dead `CountBoundViolation` (DiagnosticCode 136 / FaultCode 15).

**BUG-019 (length containment, non-literal RHS):**
- `src/Precept/Language/Actions.cs:308-311` — drop the `inputAction.InputExpression is TypedLiteral{String}` gate; generate the `LengthContainmentProofRequirement` for every string assignment to a length-bounded field.
- `src/Precept/Pipeline/ProofEngine.Lengths.cs:21-34` (`TryLengthContainmentProof`) — extend beyond the literal-character-count check to a string-length interval over the RHS (concatenation length = sum of operand length intervals; a field reference's length interval comes from its own minlength/maxlength). Unprovable ⇒ emit (Dependencies: string-length interval domain).

**Tests** (test-shaped; see Acceptance):
- `test/Precept.Tests/ProofEngine/` — new fixtures: `FieldReferenceBoundTests`, `BUG017ComputedUnboundedTests`, `BUG018CountContainmentTests` (revives the asserted-V1-gap in `ProofEngineStringCollectionBoundTests.cs:257-266`), `BUG019LengthNonLiteralTests`.
- Corpus: re-run `SampleCompilesCleanTests`; any sample newly emitting is a Falsifier check.

## Decisions

### Decision 1: A field-reference bound desugars to the equivalent relational rule, and proof participation follows decidability, not literal-ness

**Stakes**: medium (settled by spec; recorded for traceability — this is *implementation against locked spec*, not an open choice)

- **Rationale**: §2.4 states the desugaring directly and §3.5 states the scope directly; the implementation must match. Spec-first: this is not a design decision.
- **Tradeoff accepted**: a field-reference bound against an unbounded referent contributes no static interval (accepted per Principle 1/7 row above).
- **Alternatives considered**: (a) keep literal-only and reject field references — contradicts §2.4's `_Expr_` slot and §3.5's "may reference any field"; rejected as a spec violation. (b) Treat the bound as a separate construct from a rule — contradicts §2.4's "shorthand for rules"; rejected.
- **Precedent**: FluentValidation `GreaterThan(x => x.Debt * 2)`; Drools `annualIncome >= existingDebt * 2`; OCL `self.income >= self.debt * 2` (constraint-composition.md §3/§4/§5).
- **Sources consulted**: `precept-language-spec.md §2.4` — *"A constraint modifier … desugars to the equivalent `rule` … Proof participation is a function of a constraint's decidability, not its syntactic form."*; `§3.5` — *"it may reference any field regardless of declaration order, including fields declared later."*; spec grepped (§2.4/§3.5/§3.8) — the spec **settles** desugaring, scope, self/mutual cases. `constraint-composition.md §3` — *"`RuleFor(x => x.Income).GreaterThan(x => x.Debt * 2)`."*

### Decision 2: Unprovable bound ⇒ emit, never skip — applied uniformly to the interval/length/count obligation collectors (BUG-017/018/019)

**Stakes**: medium

- **Rationale**: §0.7 is the locked soundness frame — "It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral." The three breaches all gate *obligation creation* on provability, inverting prevention into detection. The sound set-action path (`Actions.cs:285`, creates the obligation unconditionally and lets unbounded discharge to false) is the model to mirror.
- **Tradeoff accepted**: definitions previously compiling clean may now emit; authors of unbounded computed operands / unguarded count mutations / unbounded-length concatenations must add a constraint or guard. False-negative friction in the safe direction (§0.6 soundness-over-completeness).
- **Alternatives considered**: (a) keep "unbounded ⇒ skip" as "acceptable conservative" — explicitly rejected by the contract grounding (*"That characterization was wrong — it is a soundness hole"*); contradicts Principle 11. (b) emit only a warning — a `[StaticallyPreventable]` fault that can actually fire at runtime is an error per §0.7, not a warning; rejected.
- **Precedent**: the already-sound numeric set-action path in the same codebase (`Actions.cs:285-304`); proof-engine.md Strategy 8 returning false on `IsUnbounded`.
- **Sources consulted**: `proof-engine-contract-grounding-2026-06-02.md` — *"All three share one fix principle: a declared bound whose satisfaction the engine cannot statically establish must reject (emit), never skip."*; `ProofEngine.Analysis.cs:452-454` — `if (interval.IsUnbounded) continue;` (verified); `Lengths.cs:41-42` — `TryCountContainmentProof … => null` (verified); `Actions.cs:308-311` — literal-only length gate (verified); spec grepped §0.7/§0.1 P10/P11 — settles the prove-or-reject direction; this decision is implementation against that locked frame.

### Decision 3: Revive `CountBoundViolation` rather than introduce a new count diagnostic

**Stakes**: low

- **Rationale**: `DiagnosticCode.CountBoundViolation` (136) and `FaultCode.CountBoundViolation` (15, `[StaticallyPreventable]`) already exist and are mapped; they are dead only because no obligation is created. Reuse keeps the catalog the source of truth and avoids renumbering.
- **Tradeoff accepted**: the existing message wording may need tightening for the mutation case (currently never seen by an author).
- **Sources consulted**: `DiagnosticCode.cs:166` / `FaultCode.cs:53-54` — `CountBoundViolation` declared `[StaticallyPreventable]` (verified); spec grepped — §3.8 modifier-value-validation table already names the count bounds; no new code needed.

### Decision 4: Bound extraction distinguishes a constant-foldable bound (existing `decimal?` path) from a field-reference bound (no static magnitude)

**Stakes**: medium

- **Rationale**: `IntervalContainmentProofRequirement` stores `decimal?` bounds (verified `ProofRequirement.cs:193-194`). A field reference has no static magnitude, so it cannot populate `DeclaredMin/Max` directly. The extraction must branch: constant bound → existing path; field-reference bound → either a new field-reference representation that the relational discharge consumes (Q2 path B) or routed per the admission policy (Q1). This decision commits only to the *branching*, not to which target shape (that is Q1/Q2).
- **Tradeoff accepted**: a second representation for bounds adds a shape; mitigated by the catalog DU discipline (a DU variant, not a nullable field on the flat record).
- **Alternatives considered**: (a) force field references through `decimal?` by evaluating them — impossible, no static value; rejected. (b) silently drop non-constant bounds (current behavior) — that is the bug; rejected.
- **Precedent**: the catalog's existing DU-per-shape discipline (`ProofRequirement` is a DU); CUE's reference-as-constraint model (Architecture Grounding).
- **Sources consulted**: `ProofRequirement.cs:190-198` — `decimal? DeclaredMin/Max` (verified); `TypeChecker.Validation.Modifiers.cs:462-480` — `_ => null` drops field references (verified); `dynamic-modifier-bounds-research-2026-06-01.md` D3 — *"the current `IntervalContainmentProofRequirement` shape stores only `decimal?` and cannot represent a field-reference bound"*; spec grepped — §2.4/§3.5 contemplate the field-reference form but do not specify the obligation data model (genuinely silent → see Q2).

## Acceptance criteria

- A field-reference bound type-checks and does not silently disappear: `field Floor as integer min 0 max 100 default 5` + `field Amount as integer min Floor default 10` compiles with the bound *recorded* (assertable via the proof ledger / `precept_compile` obligations), not dropped. **(test: FieldReferenceBoundTests)**
- A field-reference bound to an **event arg** is a compile error (rule event-arg-exclusion). **(test)**
- Self-reference `min X` is accepted (vacuous); mutual `A min B` + `B min A` is accepted (satisfiable) — no `CircularComputedField`-style error. **(test)**
- **BUG-017**: `field A as integer min 0 <- ...` (unbounded) feeding a bounded computed field emits `NumericOverflow`; the bounded-operand control still emits; a sufficiently-bounded operand compiles clean. **(test: BUG017ComputedUnboundedTests)**
- **BUG-018**: `field C as set of integer maxcount 1` + an `add` that can push past 1 emits `CountBoundViolation` (PRE0136); a guarded `when C.count < 1 -> add …` compiles clean. **(test: BUG018CountContainmentTests; flips the assertion at `ProofEngineStringCollectionBoundTests.cs:257-266`)**
- **BUG-019**: `field Name as string maxlength 10` + `set Name = First + Last` (unbounded-length operands) emits `LengthBoundViolation`; the literal-too-long control still emits; bounded-length operands whose sum ≤ 10 compile clean. **(test: BUG019LengthNonLiteralTests)**
- Corpus: `SampleCompilesCleanTests` re-run; either still green, or every newly-emitting sample is documented as a genuine latent bound violation (not a false positive). **(test + Falsifier check)**
- Docs updated per § Doc-update enumeration.

## Dependencies

- **Upstream**:
  - Open question Q1 (admission policy for non-decidable field-reference bounds) must be answered before the field-reference extraction target shape is built — it gates Decision 4's branch target.
  - BUG-019's non-literal length discharge and BUG-018's count discharge both need an **interval abstract domain** beyond the current model: a string-length interval (BUG-019) and a collection-count interval (BUG-018). The §0.6 grow/shrink note records that the count model is currently boolean (`count > 0`), not an interval — so exact `maxcount` proof needs the interval extension. This is the larger build inside BUG-018.
- **Downstream**:
  - Enables the deferred relational-narrowing (observation B) work if Q1 admits it.
  - Tightens §0.6's implementation-status table and the contract-grounding breach list (all three move from BREACH to holds).

## Doc-update enumeration

- `docs/compiler/proof-engine.md` — § containment strategies: Strategy 9 (Length) extends to non-literal RHS; Strategy 10 (Count) becomes live; the `TryCountContainmentProof` stub note removed; field-reference bound discharge documented (per Q1/Q2 outcome). § Implementation State.
- `docs/compiler/diagnostic-system.md` — `CountBoundViolation` (136) moves from dead to live; `LengthBoundViolation`/`NumericOverflow` reachable on new paths.
- `docs/language/precept-language-spec.md §0.6` — implementation-status: numeric interval / relational obligations now cover the count/length non-literal and computed-unbounded cases; remove the grow/shrink "boolean, not interval" under-proof caveat for count if the interval domain lands.
- `docs/language/precept-language-spec.md §3.8` — `InvalidModifierBounds` row gains the field-reference-pair semantics (per Q3); modifier-value-validation table notes field-reference bound handling.
- `docs/language/primitive-types.md` / `business-domain-types.md` — add a field-reference bound example alongside the literal ones (currently every example is a literal — research finding).
- `docs/Working/bugs.md` — BUG-017/018/019 move Active → Fixed when shipped.
- `docs/runtime/runtime-api.md` — None at compile-time scope (runtime ingress is out of scope).

## Falsifiers

External-author-visible (new diagnostics fire, error wording, behavior change), so falsifiers are required:

1. If re-running `SampleCompilesCleanTests` after the BUG-017/018/019 fixes turns **more than ~3** previously-green samples red and the reds are *not* genuine latent bound violations (i.e., the obligations are false positives), then the obligation generation is over-eager or the interval domain is too weak — relax toward proving more, not toward skipping.
2. If a domain expert in a usability test cannot read `min Floor` and the `CountBoundViolation` message and self-correct within 10 minutes, the audience-fit claim is falsified and the message/teaching path needs rework.
3. If implementing the count/length interval domain requires fixpoint iteration (a loop) to discharge realistic cases, that violates §0.4 no-loops and the bounded-strategy commitment — the feature shape is wrong and must be re-scoped to the acyclic closure only.
4. If field-reference bound support forces switching on a bound's *shape* (`kind switch { Literal => …, FieldRef => … }`) in more than the single extraction/discharge boundary, the DU modeling is wrong (per catalog discipline) and should be re-modeled.
5. If the admission policy chosen for Q1 (admit-runtime-only) produces a definition that compiles clean yet a downstream divisor/sqrt obligation that depended on the bound silently passes, that is a Principle-11 regression and the policy is falsified — it must leave the dependent unresolved → emit.

## Open questions

These are owner-level choices the canon genuinely leaves open (cross-referenced to the research Q-list). No baked-in recommendation.

- **Q1 — Admission policy for a non-decidable field-reference bound.** When the referenced field is unbounded (the bound contributes no static range): (a) admit it as a runtime-enforceable constraint that contributes no compile-time interval (downstream obligations that needed it stay unresolved → emit), or (b) reject it / route it to a longhand `rule`. The canon supports both readings (research D3 Reading A vs B; §2.4 decidability-governs-participation vs `business-domain-types.md` "a bound that cannot be evaluated at compile time is an unprovable governance claim"). This gates Decision 4's target shape and the relational-narrowing deferral.
- **Q2 — Obligation data model for field-reference bounds.** If admitted (Q1a): a new DU variant on `IntervalContainmentProofRequirement` carrying a field reference, or discharge through a relational path (Strategy-4-style, currently guard-driven + subtraction-only)? (research Q2.)
- **Q3 — `InvalidModifierBounds` for field-reference `min`/`max` pairs.** When `min Floor max Ceiling` and the ordering is not statically decidable: skip the check, fire only when *provably* `Floor > Ceiling` from declared intervals, or treat non-literal pairs as unorderable? (research Q3.)
- **Q4 — Relational-rule and field-reference-bound interval narrowing (observation B).** Should a field-referencing relational rule (`rule X >= Y`) or a field-reference bound narrow the *subject's interval* so downstream obligations (divisor, overflow) can discharge? Today neither does (`ExtractFieldInterval` reads only declared modifiers; rules feed only satisfiability). §0.6 item 2 obligates "relational reasoning over multiple fields" but the shipped reach is guard-driven and subtraction-only. Building this is a proof-engine expansion with its own phase target — confirm whether it is in this work item or a separate one.
- **Q5 — Cross-lane field-reference bound.** Does `field Amount as number min DecimalFloor` require the explicit bridge a `decimal`-vs-`number` comparison requires (§3.6), at the modifier position? (research Q5.)
- **Q6 — Qualifier-bearing field-reference bound on money/quantity/price.** Does `BoundsQualifierMismatch` (PRE0134) / the same-dimension UCUM conversion exception extend to a field-reference bound, and what does the bound-interpretation rule become when the bound is a field rather than a literal/typed-constant? (research Q6.)
- **Q7 — Computed field as a bound value.** Admit `min ComputedFloor`, and if so how is its interval inferred for decidability — or exclude it? (research Q7.)
