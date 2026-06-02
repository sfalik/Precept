---
status: Externally-Grounded
phase-target: Phase 7 (total language conformance sweep) — relational reasoning (§0.6 item 2) + the three bound-containment soundness breaches are conformance defects; couples to Phase 8/9 at the value-level-obligation emit seam
comparable-systems-research-status: partial — per-decision inline survey (CUE lattice propagation, Zod/Pydantic declaration-local bounds, FluentValidation cross-field, Alloy/Drools relational facts); grounded against the in-tree `research/language/references/constraint-composition.md` survey
sources-consulted:
  - "docs/philosophy.md — prevention/determinism/totality commitments; the carry-a-constraint paragraph (line 55, 'not a second line of defense')."
  - "docs/language/precept-language-spec.md §0.1 — the eleven Design Principles (lines 88–111); Principle 7/9/10/11 verbatim."
  - "docs/language/precept-language-spec.md §0.4 — Execution Model Properties (no loops / no reconverging flow / expression purity; lines for items 1,3,6)."
  - "docs/language/precept-language-spec.md §0.6 — Proof Engine Design Contract: item 1 numeric interval reasoning, item 2 relational reasoning over multiple fields, soundness-over-completeness, sequential proof flow."
  - "docs/language/precept-language-spec.md §0.7 — The Compile-Time and Runtime Guarantee Contract: fault-prevention 'no deferral', governance operation-blind, composition, the boundary."
  - "docs/language/precept-language-spec.md §2.4 — Field Modifiers: 'constraint modifier desugars to the equivalent rule', proof participation = decidability not syntactic form (line 1112)."
  - "docs/language/precept-language-spec.md §3.5 — Scope Rules: modifier-value-expression row (line 1343) + evaluation-model derivation (rule-condition scope, self-ref vacuous, mutual-ref satisfiable)."
  - "docs/language/precept-language-spec.md §3.8 — Modifier validation: InvalidModifierBounds (`min` > `max`), applicability table (min/max → numeric; minlength/maxlength → string; mincount/maxcount → collections)."
  - "docs/language/business-domain-types.md line 426 — bound-expression interpretation enumerates only typed-constant and number-literal forms (doc-sync target)."
  - "docs/compiler/proof-engine.md — Strategy 4 flow-narrowing + GuardRelationImpliesObligation triple table (subtraction-only, guard-driven; lines 1330,1397); IntervalContainmentProofRequirement decimal? shape (lines 580–596); Obligation Generation Contract (lines 497–507); Pass 1.5 ComposeRulePredicateWithFieldBounds + BuildNarrowedIntervals (lines 483,487)."
  - "src/Precept/Pipeline/ProofEngine.Analysis.cs:452-454 — BUG-017: `if (interval.IsUnbounded) continue;` skips the computed-field bound obligation."
  - "src/Precept/Pipeline/ProofEngine.Lengths.cs:41-42 — BUG-018: `TryCountContainmentProof(...) => null;` stub."
  - "src/Precept/Pipeline/ProofEngine.Lengths.cs:21-32 — BUG-019: length discharge resolves only TypedLiteral string RHS ('Non-literal sites cannot be statically resolved')."
  - "src/Precept/Language/FaultCode.cs:50-54 + Faults.cs:40-42 + Diagnostics.cs:1264-1282 — CountBoundViolation/LengthBoundViolation fault+diagnostic exist; CountBoundViolation is dead (no obligation emitted)."
  - "research/language/references/constraint-composition.md — value narrowing flagged as 'the absent capability' (lines 23,279-283); CUE/Zod/Pydantic/FluentValidation/Alloy/Drools comparators with excerpts."
  - "docs/Working/dynamic-modifier-bounds-research-2026-06-01.md — the reconciled research; §0.7 settlement of admission (Q1 SETTLED), D3/D4, and the open inventory Q2/Q5/Q6/Q7."
---

# Relational rules and their bound-modifier sugar in compile-time proof

## Goal

When done, a relational rule that compares two fields (`rule X >= Y because "…"`) contributes a provable interval to `X` so a downstream fault-prone operation can discharge against it — and the bound modifiers (`min`/`max`/`minlength`/`maxlength`/`mincount`/`maxcount`), being rule sugar (§2.4), inherit that treatment — while the three containment obligations that today are skippable (computed-field unbounded-operand BUG-017, `mincount`/`maxcount` BUG-018, non-literal length-RHS BUG-019) are emitted unconditionally and discharge-or-emit. Demonstrated by: a reorder precept where `rule Available >= Reserved` lets `Stock - Reserved` prove `>= 0`; and three regression precepts where the breached obligations now reject the unprovable case.

## Scope

- **In scope**:
  - The mechanism by which a relational rule `X op Y` (and its bound-modifier sugar) narrows the subject field's interval so downstream interval/divisor/non-negative/range obligations can discharge — bounded and acyclic per §0.4.
  - Closing the three soundness breaches BUG-017, BUG-018, BUG-019 under the unifying principle that the containment obligation is created unconditionally and discharge may return unresolved.
  - The data-model question (Q2) for how a relational/field-reference bound is represented as an obligation, presented as an open decision with options.
- **Out of scope**:
  - New language *surface*. Relational rules (`rule X >= Y`) and the bound modifiers already exist in the spec and grammar; this design adds no keyword, type, operator, modifier, or construct. The field-reference *bound form* (`min OtherField`) is already spec-legal per §2.4/§3.5 — this design makes it *participate in proof*, it does not introduce it.
  - SMT / general fixpoint solving (§0.6 philosophy item 3; §14 No SMT Solver).
  - Runtime evaluation changes (governance already enforces these constraints at ingress per §0.7; the runtime fault traps for CountBoundViolation/LengthBoundViolation already exist).
- **Deferred to future**:
  - Q5 cross-lane field-reference bound typing, Q6 qualifier-bearing field-reference bound (`BoundsQualifierMismatch` extension), Q7 computed-field-as-bound — each marked `Stakes: exploratory` below; each has a governing canonical section but no current-pass resolution.
  - Vacuous/mutual-bound diagnostic policy (Q4) — relates to PRE0154/PRE0155, not to the soundness obligations this design ships.
  - The doc-sync to `business-domain-types.md` line 426 and §3.8 (enumerated in Doc-update enumeration; not edited this pass).

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Closing BUG-017/018/019 makes the unprovable bound case a compile-time rejection rather than a runtime trap that can fire (§0.7 "no deferral", spec line 262). | Relational narrowing must not *over*-prove or it converts prevention into a false safety claim. | Accept more "supply a constraint" friction (false negatives) to never emit a false "safe". |
| 2. One file, complete rules | Y | The narrowing fact derives only from constraints declared in the same `.precept` (§0.6 philosophy item 4 "one file, complete proof facts"). | N/A | N/A |
| 3. Determinism | Y | The mechanism is bounded relational closure over declared intervals — no solver, same definition → same dispositions (§0.1 Principle 3; §0.4 no widening). | N/A | N/A |
| 4. Full inspectability | Y | Each narrowed interval carries its contributing constraint as structured attribution (§0.6 philosophy item 6); the relational rule appears in the proof witness. | N/A | The witness must name the *rule* that narrowed, not just the literal bound — a richer attribution shape. |
| 5. Keyword-anchored readability | N | No syntax change; `rule`/`min`/`max` keywords unchanged. | N/A | N/A |
| 6. Governance not validation | Y | The bound is enforced at runtime ingress operation-blind regardless of decidability (§0.7 governance, spec line 264) — proof participation is a separate concern. | N/A | N/A |
| 7. Compile-time-first static checking | Y | A relational rule now contributes a provable range (§0.6 item 1/2); the engine proves-or-declines, never guesses (§0.1 Principle 7). | Bounded closure is incomplete — some genuinely-safe programs will need an explicit guard. | Accept incompleteness (author friction) over unsoundness, per §0.6 item 1. |
| 8. Approximation honesty | N | Relational reasoning over exact intervals introduces no approximate lane; decimal magnitudes stay exact (proof-engine quantity-normalization note). | N/A | N/A |
| 9. Mandatory rationale | Y | A relational rule carries an authored `because`; the bound-modifier sugar carries a generated rationale (§0.1 Principle 9; §2.4). | The generated rationale wording for a *field-reference* bound (`min Floor`) is unspecified (research gap 4). | Accept a generic generated wording ("X must be at least Floor") until a wording decision lands. |
| 10. Totality | Y | A downstream fault-prone op whose operand is bounded only by an *unbounded-operand* relation is rejected with a constraint-supply diagnostic, never compiled hopeful (§0.1 Principle 10; §0.7). | N/A | N/A |
| 11. Static completeness | Y | BUG-017/018/019 are exactly "a fault class linked to a diagnostic that is not actually emitted" — closing them restores the §0.1 Principle 11 bridge for CountBoundViolation/LengthBoundViolation/OutOfRange. | N/A | N/A |

**Tradeoff detail (Principles 1/7/10).** The relational-narrowing mechanism is deliberately *incomplete but sound*: it discharges only the relations whose closure over declared intervals is decidable, and declines the rest (the author supplies a guard or a bound on the referenced field). This accepts false-negative friction — an author may have to add `min 0` to the referenced field even when the program is "obviously" safe — to guarantee the engine never emits a false "proved safe", which would be a runtime fault and a prevention-guarantee breach. This is the §0.6 item-1 posture applied to the new relation source.

**Companion commitments.** *Stateless-first-class*: relational rules and bounds are field-scoped data constraints, fully applicable to stateless precepts; nothing here depends on a state machine. *Domain-expert-primary-author*: the feature requires no new vocabulary — a domain expert who writes `rule Available >= Reserved because "…"` already expresses the intent; this design makes that already-natural rule do proof work it currently doesn't.

## Language Design Grounding

This design does **not introduce** language surface — relational rules and the six bound modifiers are already spec-defined (§2.4, §3.5, §3A.1) and parse today. What follows grounds the *semantics* of making a relational constraint participate in interval proof, because that is where comparable systems diverge sharply.

**General language design — relational constraint propagation.** The capability at issue is *value narrowing*: deriving a tighter interval for one field from a constraint relating it to another. `research/language/references/constraint-composition.md` names this directly as Precept's one missing composition capability:

> "Constraint propagation | Narrowing through assignment | TypeScript type narrowing, Kotlin smart casts | **Partial.** Nullable narrowing through `when` guards is implemented. Value-range narrowing is not." (constraint-composition.md line 23)

> "**Gap:** No narrowing of *value* constraints. If `when CreditScore >= 680`, the runtime knows `CreditScore` is at least 680 in the action body, but the type checker does not exploit this for optimistic analysis. … classified as Batch 3 (static reasoning expansion)." (constraint-composition.md lines 282–283)

The broader field offers two relevant models:

- **CUE** (the comparator `compiler-and-runtime-design.md §2` already grounds Precept's catalog choice against) treats every value as a point in a lattice and propagates constraints to a fixpoint via unification: a field constrained `>=0` and `<=10` unifies to the interval `[0,10]`, and references propagate transitively until no value changes. CUE's strength is completeness; its cost is that propagation is a fixpoint computation that can be non-terminating or expensive, and the "why" of a narrowed value is a unification trace, not a legible rule list. *(CUE language spec, "Lattice" / "Unification": values form a lattice under subsumption; unification `a & b` is the greatest lower bound; evaluation reduces to a fixpoint over the constraint graph. https://cuelang.org/docs/references/spec/ — accessed 2026-06-02.)*
- **Zod / Pydantic / FluentValidation** take the opposite tack: declaration-local bounds (`z.number().min(0)`, `Field(ge=0)`, `.GreaterThan(0)`) are *literal* and do not propagate field-to-field at all; cross-field comparison is a separate non-narrowing predicate (`.refine(d => d.income >= d.debt*2)`), checked but never used to tighten another field's domain (constraint-composition.md lines 142–148, 164–176).

**What Precept takes and what it diverges from.** Precept takes CUE's idea that a relational constraint *can* narrow a field's interval, but **deliberately diverges from CUE's fixpoint**: §0.4 forbids loops, reconverging flow, and widening — "Standard interval arithmetic, bounded relational closure, and single-pass validation are directly applicable without the lattice infrastructure that general-purpose analyzers require. The absence of widening is a feature." The mechanism must therefore be a **single-pass bounded relational closure**, not an iterate-to-fixpoint propagation: a relation `X >= Y` may narrow `X` *once* from `Y`'s already-declared interval, and the closure depth is bounded (it does not chase `Y >= Z >= W …` to a fixpoint). This is the §0.6-item-2 contract ("relational reasoning over numeric expressions involving multiple fields") realized within the §0.4 no-fixpoint envelope — strictly weaker than CUE, strictly stronger than Zod's no-propagation, and legible (the witness is a rule list, satisfying §0.6 item 3's rejection of opaque solvers). From Zod/Pydantic Precept takes the declaration-local *literal* bound as the already-shipped base case, and generalizes the RHS from literal to field-reference per §2.4 line 1112.

**Precept-specific application.** Touches §0.6 item 1 (numeric interval reasoning) and item 2 (relational reasoning — the obligation this implements); extends Strategy 4 (`proof-engine.md` line 1397), which today is guard-driven and subtraction-only, toward declared-rule-driven and `>=`/`<=`-comparison-driven. Risks conflicting with §0.4 if implemented as a fixpoint — the design explicitly forbids that. No principle in §0.1 is overridden; the field-reference bound form is already permitted by §2.4/§3.5 (no Tier-3 spec conflict — confirmed below).

## Audience and Teachability

**Worked example** (inventory reorder — a plausible operations-domain entity; syntax verified against the live compiler):

```precept
precept ReorderLine

field OnHand as integer nonnegative default 0
field Reserved as integer nonnegative default 0
field BatchCost as money in 'USD' nonnegative default '0 USD'
field UnitCost as money in 'USD' nonnegative default '0 USD'

rule OnHand > Reserved because "there must be unreserved stock on hand before a per-unit cost is meaningful"

state Open initial
in Open modify OnHand, Reserved, BatchCost editable

event Recost
from Open on Recost
    -> set UnitCost = BatchCost / (OnHand - Reserved)
    -> no transition
```

The division `BatchCost / (OnHand - Reserved)` must prove its divisor is non-zero. The relational rule `OnHand > Reserved` establishes `OnHand - Reserved ≥ 1`, making the division provably safe — a relation over two fields discharging a downstream obligation. This is the *target* this design enables: today the proof engine narrows a divisor only from a single field's own `positive`/`nonzero` modifier, not from a relation over a compound expression, so this currently emits `PRE0083` (divisor unsafe) and the author must add `when (OnHand - Reserved) != 0`; after this design the declared rule discharges it and the guard is unnecessary. (Note the §2.4 form: `because` is on the `rule`, never on a modifier — `nonnegative`, not `min 0 because …`.)

**Error message** (the misuse: relying on a relation whose referenced field is unbounded). A domain expert writes `field Floor as number` (no bound) and `field Amount as number min Floor`, then divides by `Amount`:

```
PRE0xxx: Cannot prove 'Amount' stays away from zero for the division on line 9.
  'Amount' is bounded by 'min Floor', but 'Floor' itself has no declared lower bound,
  so 'Amount >= Floor' does not establish a safe range.
  Add a bound to 'Floor' (e.g. `min 1`), or guard the division with `when Amount != 0`.
```

This serves the domain expert (not the compiler engineer) because it names the *business field* whose missing constraint is the real cause (`Floor`), states the relation in the author's own terms (`Amount >= Floor`), and offers two concrete repairs in DSL syntax — not "obligation unresolved at site". It reflects §0.7's "names what would make the operation provably safe". **This message is the target this design introduces.** Current state (verified `precept_compile`, MCP reconnected 2026-06-02): `field Amount as number min Floor` is *silently accepted and unenforced* — no diagnostic at all, and even `min <undeclared>` produces no error (see [[BUG-020]]). So the design closes two gaps here: it makes the field-ref bound *enforced*, and it makes an unprovable dependent operation *emit* this message instead of compiling clean.

**10-minute teaching path** (competent domain expert):
1. `docs/language/precept-language-spec.md §2.4` — constraint modifiers are rule shorthand (2 min).
2. `samples/loan-application.precept` — a real precept with `rule … because` and field bounds (3 min).
3. `docs/language/precept-language-spec.md §0.6` items 1–2 — what "proof" means and why an unbounded reference can't prove a range (3 min).
4. The error message above — how to read a "supply a constraint" diagnostic (2 min).

## Semantic Rules

**Reduction / desugaring.** A bound modifier desugars to a relational rule (§2.4 line 1112), and this design adds no new reduction — it makes the *existing* desugared form proof-bearing:

```
field X as T min Y     ⇝   field X as T  +  rule X >= Y because "<generated>"
field X as T max Y     ⇝   field X as T  +  rule X <= Y because "<generated>"
(minlength/maxlength → rule X.length op Y ; mincount/maxcount → rule X.count op Y)
```

**Narrowing rule (the new semantics).** For a relational rule `X op Y` where `op ∈ {>=, >, <=, <}` and `Y` resolves to a field reference with a declared interval `⟦Y⟧`:

```
  rule  X >= Y      ⟦Y⟧ = [ylo, yhi]      yhi may be +∞, ylo may be −∞
  ───────────────────────────────────────────────────────────────────── (R-NARROW-GE)
        ⟦X⟧ ⊓ [ylo, +∞)        — X's lower bound is tightened to ylo

  rule  X <= Y      ⟦Y⟧ = [ylo, yhi]
  ───────────────────────────────────────────────────────────────────── (R-NARROW-LE)
        ⟦X⟧ ⊓ (−∞, yhi]        — X's upper bound is tightened to yhi
```

**Bounded, single-pass, acyclic (the §0.4 constraint).** `⟦Y⟧` is the interval `Y` has *before* this narrowing step — its declared modifiers and any literal-derived bounds — **not** the result of recursively narrowing `Y` from a third field. Closure depth is bounded (the implementation chooses 0 or 1 transitive hops; see Decision 2). Self-reference (`X >= X`) and mutual reference (`X >= Y` + `Y >= X`) conjoin to `X == Y` and never form a fixpoint, exactly as §3.5 line 1343 states ("mutual reference is satisfiable … not a cycle"). When `⟦Y⟧`'s relevant bound is infinite (`ylo = −∞` for R-NARROW-GE), the narrowing is the identity — `X` gains nothing — and any downstream obligation depending on `X`'s lower bound stays unresolved.

**Proof obligations.** This design touches three obligation families, all already in the `ProofRequirement` catalog DU:
- `IntervalContainmentProofRequirement` (ProofRequirementKind.IntervalContainment) — BUG-017 must *emit* it for a computed field whose operand interval is unbounded, then let discharge return unresolved (today `ProofEngine.Analysis.cs:452-454` `continue`s before emitting).
- `CountContainmentProofRequirement` (ProofRequirementKind.Count) — BUG-018 must emit it on every collection-growing mutation of a `mincount`/`maxcount` field, and `TryCountContainmentProof` (`ProofEngine.Lengths.cs:41`) must be implemented rather than returning `null`.
- `LengthContainmentProofRequirement` (ProofRequirementKind.Length) — BUG-019: discharge must handle non-literal RHS (return unresolved → obligation diagnostic) instead of silently passing when the RHS is not a `TypedLiteral`.

The relational-narrowing mechanism itself may either (a) extend `IntervalContainmentProofRequirement` with a field-reference bound variant, or (b) discharge through a Strategy-4-style relational path — this is the open Decision 2 (Q2). It maps to no *new* `ProofRequirementKind` either way; relational reasoning is item 2 of the §0.6 contract, already declared.

**Soundness preservation claim.**
- **Principle 7 / 10 / 11** hold because R-NARROW-* only ever *tightens* an interval from an already-established bound and is the identity when the source bound is infinite — it can never widen a field's provable range, so it cannot manufacture a false "proved safe". A downstream obligation that the narrowed interval does not cover stays `Unresolved` and emits its diagnostic (§0.7 "no deferral"). The three breaches are pure under-emission today (an obligation that should be created is skipped); creating it unconditionally and letting discharge fail strictly *adds* rejections, never removes one — monotonically safe.
- **Principle 3** holds because closure is single-pass and bounded (§0.4), so the disposition is a deterministic function of the declared intervals — no solver, no iteration order dependence.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** Two distinct layers:
1. *Obligation emission* (type checker / `ProofEngine.Analysis.cs` + `Actions` collectors) — the BUG-017/018/019 fixes live here: the `IntervalContainmentProofRequirement` / `CountContainmentProofRequirement` / `LengthContainmentProofRequirement` must be *created unconditionally* per the proof-engine Obligation Generation Contract (`proof-engine.md` lines 497–507: "Emit obligations for all declared constraints with proof semantics … silently omitting obligation emission for some constraint families is a governance gap"). This is metadata-driven emission, not a per-type switch.
2. *Discharge* (`ProofEngine.Strategies.cs` / the relational path) — the narrowing mechanism lives here, alongside Strategy 4. It belongs in pipeline code (not catalog metadata) because it is *reasoning* over catalog-declared obligations, exactly where Strategy 4 already sits; the catalog declares *what* must be proved (the `ProofRequirement` DU), the strategy is *how*.

The breaches are emission-side gaps; the relational-narrowing is a discharge-side capability. Keeping them on the correct side honors the Obligation Generation Contract's rule 4 ("keep the architecture metadata-driven on both sides … asymmetry between discharge (catalog-driven) and emission (hardcoded) is the source of 'declared constraint silently not enforced' bugs") — BUG-017/018/019 are precisely that asymmetry.

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** Type checker — must stamp the now-emitted obligations (count/length/interval) on the relevant mutation/computed sites. Proof engine — new relational-narrowing discharge + the three emission fixes. Evaluator — **None** (governance + the existing CountBoundViolation/LengthBoundViolation/OutOfRange fault traps already enforce at runtime; §0.7). Diagnostics — `CountBoundViolation` becomes live (was dead); `LengthBoundViolation` fires for non-literal RHS; the relational-unprovable case reuses the existing divisor/range obligation diagnostics with richer attribution.
- **Tooling (highlighting, completions, hover, semantic tokens):** Hover/proof-attribution gains relational-rule provenance (a narrowed interval now cites the contributing `rule`). No grammar/token/completion change.
- **MCP (vocabulary, DTOs, tool output):** `precept_compile` proof-obligation output gains the newly-emitted count obligations and relational-narrowing dispositions; no new DTO shape if Decision 2 chooses the relational-path option, a DU variant if it chooses the data-model-extension option (`CompileToolDtos.cs` projection check).

**Breaking changes.** None to public contract. `CountBoundViolation` (FaultCode = 15, DiagnosticCode) already exists in the catalog — making it live is wiring, not a new code. No catalog member rename, no diagnostic renumber, no MCP vocabulary change. A program that *previously compiled* because BUG-017/018/019 suppressed an obligation may now correctly fail to compile — that is the intended soundness fix, not a breaking-change to a guarantee (the prior behavior was a guarantee *violation*).

### External architectural precedent

The architectural problem is: *how does a constraint language propagate a relational constraint into a field's provable range without a general solver?* The sharpest comparator is **CUE**, whose lattice/unification model is the canonical "propagate constraints to narrow values" design:

> CUE unifies values via the greatest-lower-bound operation over a lattice; a field's value is the unification of all constraints on it, computed by evaluating the constraint graph to a fixpoint. (CUE spec, "Values"/"Unification"; https://cuelang.org/docs/references/spec/ — accessed 2026-06-02.)

**What Precept takes:** the core idea that a relational constraint tightens a field's domain (CUE's `a & b` greatest-lower-bound is exactly the `⊓` in R-NARROW-*). **What Precept deliberately diverges from:** CUE computes to a *fixpoint* over the whole constraint graph; Precept's §0.4 forbids fixpoint/widening, so the narrowing is single-pass and depth-bounded. The divergence is principled and already grounded in-tree: `compiler-and-runtime-design.md §2` grounds the catalog choice against CUE, and §0.4 states the no-widening choice is "a feature" because "widening is the primary source of precision loss" in general analyzers. Precept accepts CUE's narrowing *result shape* (interval tightening) while rejecting its *evaluation strategy* (fixpoint) — buying legibility and termination at the cost of completeness, the standard Precept trade.

## Inventory of what will be built

- `src/Precept/Pipeline/ProofEngine.Analysis.cs` — BUG-017: remove the `if (interval.IsUnbounded) continue;` early-return (line 452-454) so the `IntervalContainmentProofRequirement` is emitted for a computed field with declared bounds even when the operand interval is unbounded; discharge then returns unresolved → diagnostic.
- `src/Precept/Pipeline/ProofEngine.Lengths.cs` — BUG-018: implement `TryCountContainmentProof` (line 41, currently `=> null`) to discharge against narrowed count intervals / declared mincount-maxcount and the growing-mutation count; BUG-019: change `TryLengthContainmentProof` (line 21-32) to return `null`/unresolved on non-literal RHS instead of falling through to a pass.
- `src/Precept/Pipeline/ProofEngine.Analysis.cs` + collection-mutation obligation collector — BUG-018 emission half: emit `CountContainmentProofRequirement` on every growing mutation (`add`/`append`/`insert`/`enqueue`/`push`/`put`) of a `mincount`/`maxcount` field (per Obligation Generation Contract rule 2). This wires the dead `CountBoundViolation` path.
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` (or a new relational helper) — the R-NARROW-* relational-narrowing discharge: a declared-rule-sourced field-to-field narrowing, single-pass and depth-bounded, feeding the interval used by IntervalContainment/divisor/non-negative/range obligations. Extends the Strategy-4 / `GuardRelationImpliesObligation` machinery from guard-sourced to rule-sourced and from subtraction-only toward `>=`/`<=` relations.
- Test stubs:
  - `test/Precept.Tests/ProofEngine/RelationalNarrowingTests.cs` — the reorder worked example proves; the unbounded-operand case fails with the constraint-supply diagnostic.
  - `test/Precept.Tests/ProofEngine/Bug017ComputedBoundTests.cs` — computed field with unbounded operand + declared bound now rejects.
  - `test/Precept.Tests/ProofEngine/Bug018CountBoundTests.cs` — `mincount`/`maxcount` violation now rejects; the dead `CountBoundViolation` is now reachable.
  - `test/Precept.Tests/ProofEngine/Bug019LengthRhsTests.cs` — non-literal RHS to a `minlength`/`maxlength` field now produces an obligation (rejects or proves), never silently passes.
  - Per-family integration tests per `proof-engine.md` Obligation Generation Contract rule 3 (numeric / string / collection).

## Decisions

### Decision 1: The bound is admitted as a runtime-enforced constraint; the *dependent operation* is rejected when the relation cannot prove its range — not the bound

**Stakes**: This is settled by canon, not a design decision. Recorded for traceability per guard 17.

§0.7 settles the admission question. A relational rule / field-reference bound is a *declared constraint*, enforced by governance at ingress "operation-blind … whether or not any fault-prone operation references the field" (spec §0.7 line 264). Decidability does not gate the bound's legality — §2.4 line 1112: "Proof participation is a function of a constraint's decidability, not its syntactic form." When a downstream fault-prone operation needs a range the relation cannot supply (referenced field unbounded), *fault prevention* rejects the operation: "it rejects the definition and names what would make the operation provably safe … there is no deferral" (§0.7 line 262). The rejection lands on the operation, not the bound.

- **Sources consulted for this decision**: `docs/language/precept-language-spec.md §0.7 line 262/264` — verbatim above; `§2.4 line 1112` — "Proof participation is a function of a constraint's decidability, not its syntactic form"; `docs/Working/dynamic-modifier-bounds-research-2026-06-01.md` § Reconciliation → Q1 "SETTLED" (the (a)/(b) fork dissolves; bound admitted+enforced, dependent operation rejected). Grepped §0.7, §2.4, §3.5 for prior settlement — the admission question *is* settled there; this is implementation against locked spec, not an open choice.

### Decision 2: How a relational/field-reference bound is represented as a proof obligation — data-model variant vs relational-path discharge

**Stakes**: medium

- **Rationale**: The mechanism must let a relational rule narrow a field interval within §0.4 (single-pass, no fixpoint). Two representations are viable and the choice affects one consumer surface (the MCP DTO projection) but is reversible.
- **Tradeoff accepted**: Either option carries a known downside (below); committing now without prototype timing data on representative samples is premature, so the *shape* is locked to two candidates and the pick is deferred to the build slice's enumerate/probe step.
- **Alternatives considered**:
  - *(A) Extend `IntervalContainmentProofRequirement` with a field-reference bound variant* (a DU split between a constant bound and a field-reference bound, per the catalog DU discipline). Pro: keeps interval containment the single home for "value within bounds"; the witness is uniform. Con: the bound is no longer a `decimal?` — every consumer that reads `DeclaredMin/Max` (Strategy 8, `TypedFieldRef` interval extraction, the MCP tool — `proof-engine.md` line 596 normalization-boundary note) must learn the variant; touches more surface.
  - *(B) Discharge field-reference bounds through a Strategy-4-style relational path* (leave `IntervalContainmentProofRequirement` literal-only; route the relation through the field-to-field machinery). Pro: localizes the change to the discharge strategy, no data-model churn, no DTO change. Con: today that path is guard-driven and subtraction-only (`proof-engine.md` lines 1330, 1397) — extending it to *declared-rule* sources and to `>=`/`<=` comparisons is genuinely new relational surface; the two relation sources (guard, rule) must share the narrowing core without duplicating it.
  - *(C) Reject field-reference bounds outright* — rejected: contradicts §2.4 line 1112 and §3.5 line 1343, which explicitly contemplate `A min B`; would be a Tier-3 spec override with no owner authorization.
- **Precedent**: CUE narrows via lattice unification (option A is closer to CUE's "the constraint IS part of the value's domain"); the Strategy-4 guard machinery already in `proof-engine.md` is the in-tree precedent for option B. The catalog DU discipline (`proof-engine.md` "Extend proof kinds through the catalog DU") favors A if the bound's *kind* differs; favors B if it is the *same* relational reasoning with a different source.
- **Sources consulted for this decision**: `docs/compiler/proof-engine.md` line 580-596 — `IntervalContainmentProofRequirement(… decimal? DeclaredMin, decimal? DeclaredMax …)` (the literal-only shape) + normalization-boundary consumer list; `proof-engine.md` line 1330 — "Scope: subtraction expressions only … Division is NOT covered"; line 1397 — Strategy 4 "binary comparison between two non-literal operands"; `docs/Working/dynamic-modifier-bounds-research-2026-06-01.md` § Reconciliation → Q2 "STILL OPEN … DU-variant-vs-relational-path is unaddressed"; `research/language/references/constraint-composition.md` line 23 — value narrowing "the absent capability". Grepped §0.6/§0.7 — §0.7 line 270 explicitly defers *mechanism* to `compiler-and-runtime-design.md` ("Mechanism — how the pipeline discharges these obligations … is in compiler-and-runtime-design.md"), confirming this is a genuinely open mechanism decision, not spec-settled.
- **Strongest counter-evidence**: The Obligation Generation Contract (`proof-engine.md` rule 4) warns that emission/discharge asymmetry is the bug source — which could argue for A (one obligation kind, one discharge) over B (a second discharge path). Response: B does not create asymmetry if guard-sourced and rule-sourced narrowing share one core; the asymmetry rule is about *catalog-vs-hardcoded*, not about two strategies, so it does not decide A-over-B.

### Decision 3: The three breaches are fixed by emit-unconditionally, discharge-may-fail — never gate emission on provability

**Stakes**: medium

- **Rationale**: BUG-017/018/019 share one root cause: an obligation that the declared constraint requires is *not created* when the case looks unprovable (017 `continue`s on unbounded interval; 018 never emits a count obligation; 019 only emits/discharges for literal RHS). The Obligation Generation Contract (`proof-engine.md` lines 497–507) makes creation unconditional and lets *discharge* return unresolved.
- **Tradeoff accepted**: More programs that compile today will stop compiling (the suppressed obligation now fires). This is the intended soundness fix, but it will surface in samples — accept the remediation cost.
- **Alternatives considered**:
  - *(A) Emit unconditionally, discharge may return unresolved* (chosen) — matches the Contract and §0.7 "no deferral".
  - *(B) Keep the provability gate but add a runtime trap* — rejected: §0.7 forbids "compiling a fault-prone operation in the hope a runtime check catches it"; the traps are defensive redundancy only (§0.1 Principle 11).
- **Precedent**: `proof-engine.md` Obligation Generation Contract rule 2 verbatim: "A field declared `field x as string minlength 3` must produce a length-containment obligation on every assignment to `x`; `field c as set of string maxcount 10` must produce a count-containment obligation on every mutation that grows `c`. … silently omitting obligation emission for some constraint families is a governance gap." This is the exact prescription for BUG-018/019.
- **Sources consulted for this decision**: `src/Precept/Pipeline/ProofEngine.Analysis.cs:452-454` — `if (interval.IsUnbounded) continue;` (BUG-017, verified in source); `src/Precept/Pipeline/ProofEngine.Lengths.cs:41-42` — `internal static bool? TryCountContainmentProof(… _, … __) => null;` (BUG-018, verified); `ProofEngine.Lengths.cs:14,21-32` — "Non-literal sites cannot be statically resolved" (BUG-019, verified); `src/Precept/Language/FaultCode.cs:53-54` — `[StaticallyPreventable(DiagnosticCode.CountBoundViolation)] CountBoundViolation = 15` (the dead fault now wired); `proof-engine.md` lines 497–507 Obligation Generation Contract. Grepped §0.6/§0.7 — §0.6 items 1/6, §0.7 "no deferral" settle that an obligation must be created and discharged-or-emitted; this is implementation against locked spec.
- **Strongest counter-evidence**: One could argue BUG-019's non-literal RHS is the *relational-narrowing* case (a non-literal RHS could be a field reference whose length interval is provable) — i.e. 019 is not "just emit unresolved" but "narrow then discharge". Response: correct, and that is exactly why 019 couples to Decision 2 — emitting the obligation unconditionally is the floor; the relational path can later *discharge* some non-literal RHS that today silently pass. The floor (emit, don't silently pass) is unconditionally right regardless of how much the relational path can prove.

### Decision 4: Closure is single-pass and depth-bounded — not a fixpoint

**Stakes**: medium

- **Rationale**: §0.4 forbids loops, reconverging flow, and widening; a fixpoint propagation would violate it and forfeit the determinism/termination guarantee. R-NARROW-* narrows `X` from `Y`'s pre-existing interval in one pass.
- **Tradeoff accepted**: Incompleteness — a chain `X >= Y`, `Y >= Z`, `Z min 0` may not propagate `Z`'s bound all the way to `X` if depth is capped at one hop; the author adds a direct bound or guard. Accept friction over fixpoint complexity.
- **Alternatives considered**:
  - *(A) Single-pass, depth 0–1* (chosen) — bounded, terminating, legible.
  - *(B) Iterate-to-fixpoint (CUE-style)* — rejected: violates §0.4; introduces termination/precision-loss concerns §0.4 calls out; opaque-witness risk vs §0.6 item 3.
- **Precedent**: §0.4 verbatim: "Standard interval arithmetic, bounded relational closure, and single-pass validation are directly applicable without the lattice infrastructure that general-purpose analyzers require. The absence of widening is a feature." CUE is the rejected fixpoint comparator (Architecture Grounding).
- **Sources consulted for this decision**: `docs/language/precept-language-spec.md §0.4` items 1/3 (no loops, no reconverging flow) + the closing paragraph "bounded relational closure … single-pass … absence of widening is a feature"; `docs/compiler/proof-engine.md` line 153 "If the six strategies prove insufficient … a seventh bounded strategy would be added — not a general solver"; CUE spec (accessed 2026-06-02). Grepped §0.4 — the no-fixpoint constraint is locked spec; single-pass is implementation against it.
- **Strongest counter-evidence**: A depth-1 cap may under-prove common transitive chains, generating friction the author finds arbitrary. Response: the cap is a falsifier (below) — if samples need >1 hop, raise the bounded depth to a fixed small N (still not a fixpoint); the §0.4 line is the no-fixpoint guarantee, not a specific depth.

### Decision 5: Cross-lane field-reference bounds (Q5) — disposition deferred

**Stakes**: exploratory

- **Exploratory because**: §0.7 is mechanism-neutral on numeric lanes; the governing sections are §3.2/§3.6 + `primitive-types.md` line 424 (the "semantically dangerous" lane-cross requiring a bridge). Resolving it requires confirming whether a field-reference bound of a different numeric lane is a type error or a coercion *at the modifier-bound position* — not settled by the sections checked.
- **Working hypothesis**: a field-reference bound inherits the comparison's lane rules — `decimal`-bounding-`number` requires the same explicit bridge a comparison requires; lane-cross without a bridge is a type error.
- **Decision needed before**: any sample uses a cross-lane field-reference bound, or the relational-narrowing path ships discharge for cross-lane relations.
- **Open questions**: Is the bridge required at the bound position the same `primitive-types.md` line 424 bridge, or does the modifier-bound position relax it?

### Decision 6: Qualifier-bearing field-reference bounds on money/quantity/price (Q6) — disposition deferred

**Stakes**: exploratory

- **Exploratory because**: `business-domain-types.md` line 426 enumerates only typed-constant and number-literal bound forms; what the bound-interpretation rule *becomes* for a field reference, and how `BoundsQualifierMismatch` (PRE0134) axis-matching computes against a referenced field's qualifier, is undefined.
- **Working hypothesis**: a cross-dimension/cross-currency field-reference bound is an *undefined comparison* (the PRE0133/PRE0134 family applies); a same-dimension UCUM field-reference bound is well-defined and governance enforces it (mirroring the existing same-dimension conversion exception).
- **Decision needed before**: a `quantity`/`money`/`price` field is bounded by another such field in a sample, or the data-model decision (Decision 2) freezes the bound representation for qualified fields.
- **Open questions**: Does line 426's "Bound expression interpretation" gain a field-reference clause? Is PRE0134 computed against the referenced field's declared qualifier?

### Decision 7: Computed field as a bound (Q7) — disposition deferred

**Stakes**: exploratory

- **Exploratory because**: §0.7 is silent on whether a *computed* field may serve as a constraint bound or how its interval is inferred for decidability; the governing material is §3.5's evaluation-model derivation + §0.6 interval reasoning, which do not settle it.
- **Working hypothesis**: a computed field is in scope as a bound (§3.5 "All field names"), and its interval is the inferred result-range of its computed expression — but only if that range is decidable, else the bound contributes nothing (same as an unbounded plain field).
- **Decision needed before**: a sample bounds a field by a computed field, or the relational path attempts to narrow from a computed-field reference.
- **Open questions**: Is a computed field's inferred interval a sound narrowing source, given BUG-017 shows computed-field interval inference is itself the breached path?

## Acceptance criteria

- A precept with `rule X >= Y because "…"`, `Y min 0`, and `set X = A - Y` (or a divisor/sqrt on `X`) **compiles clean** — the relational narrowing discharges the downstream obligation. (`RelationalNarrowingTests`)
- The same precept with `Y` unbounded **fails to compile** with a diagnostic naming `Y` as the field whose missing bound blocks the proof, and offering a bound-or-guard repair. (`RelationalNarrowingTests`)
- BUG-017: a computed field `field C as number min 0 max 100 <- Floor + 1` with `Floor` unbounded **emits an obligation and fails to compile** (today it silently passes via the `IsUnbounded` `continue`). (`Bug017ComputedBoundTests`)
- BUG-018: a precept that grows a `mincount 2 maxcount 5` collection past its bound, or whose default violates `mincount`, **fails to compile** with `CountBoundViolation`-class diagnostic; `CountContainmentProofRequirement` appears in `precept_compile` proof obligations. (`Bug018CountBoundTests`)
- BUG-019: a `set` of a non-literal RHS into a `minlength`/`maxlength` field **emits a length obligation** (proves via narrowing or fails unresolved) — never silently passes. (`Bug019LengthRhsTests`)
- Mutual bound (`A min B` + `B min A`) **compiles** (conjoins to `A == B`, satisfiable); self-reference (`X min X`) **compiles** (vacuous) — neither hangs or errors, confirming no fixpoint. (`RelationalNarrowingTests`)
- Per-family integration tests (numeric / string / collection) pass: constraint declared → obligation emitted → discharge succeeds-or-fails as expected. (`proof-engine.md` Obligation Generation Contract rule 3)
- Full suite green after sample remediation for any precept that newly fails.

## Dependencies

- **Upstream**: §0.7 (locked 2026-06-02) — the admission frame Decision 1 rests on. The reconciled research `docs/Working/dynamic-modifier-bounds-research-2026-06-01.md`. The Strategy-4 / `GuardRelationImpliesObligation` machinery (shipped). The `CountBoundViolation`/`LengthBoundViolation` faults + diagnostics (shipped, dead for count).
- **Downstream**: Enables §0.6 item 2 (relational reasoning over multiple fields) to be genuinely live; unblocks the field-reference-bound feature the research derived; couples to Phase 8 Slice 3 (BUG-017's value-level computed-context obligation is the Class-O emission the ownership architecture moves to proof-owned) and Phase 9 (the now-live `CountBoundViolation` exits the emit-or-retire seam as emit).

## Doc-update enumeration

Per the CLAUDE.md routing table — obligations for `/lifecycle-5-promote`, **not edited this pass**:
- `docs/compiler/proof-engine.md` § Proof Strategies / Strategy 4 — document the rule-sourced relational narrowing and its single-pass bound; § Design Rationale — lift Decisions 2 and 4.
- `docs/compiler/proof-engine.md` § ProofRequirement Catalog DU — if Decision 2 chooses the data-model variant, document the `IntervalContainmentProofRequirement` field-reference variant.
- `docs/compiler/diagnostic-system.md` — `CountBoundViolation` is now live (was dead); the relational-unprovable diagnostic wording.
- `docs/language/business-domain-types.md` line 426 — **doc-drift**: "Bound expression interpretation" enumerates only typed-constant and number-literal forms; must gain a field-reference clause (couples to Decision 6, deferred).
- `docs/language/precept-language-spec.md §3.8` — `InvalidModifierBounds` is literal-framed ("`min` value exceeds `max` value"); must address field-reference-bound ordering (couples to Q3, sharpened-not-settled).
- `docs/tooling/mcp.md` — if Decision 2 changes the obligation DTO, update the `precept_compile` projection note.
- `bugs.md` — flip BUG-017, BUG-018, BUG-019 to Fixed at execution.

## Operational dimensions

- **Observability** (triggered — touches proof/diagnostic surface): when a relational narrowing *fails* to discharge a downstream obligation, the author diagnoses it through the obligation diagnostic, which must (per §0.6 item 6) carry structured attribution naming the contributing rule and the field whose bound is missing — surfaced through `precept_compile` proof obligations and hover, not parsed from message text. The newly-live `CountBoundViolation` is observable in the proof-obligation output.
- **Security**: N/A — no source-text-ingestion surface change (no new tokens/parser paths; the bound form already parses).
- **Evolvability**: N/A — no new external-standard dependency (UCUM/NodaTime usage unchanged; relational narrowing operates on the already-normalized decimal magnitudes).

## Falsifiers

- If closing BUG-017/018/019 forces explicit-constraint workarounds in **three or more** `samples/` precepts that a domain expert would consider obviously safe, the emit-unconditionally floor is too aggressive for current samples and the diagnostic wording / narrowing reach must improve before ship (not the emit decision — that is sound — but its ergonomics).
- If the depth-1 closure cap (Decision 4) under-proves a transitive relation chain in two or more samples, raise the bounded depth to a fixed small N (still no fixpoint); if no fixed N suffices without a sample needing genuine fixpoint reasoning, the §0.4 envelope is being stretched and the feature scope is wrong.
- If the relational-narrowing path ever discharges an obligation that a runtime trap then fires on (a false "proved safe"), the mechanism is unsound — immediate redesign; this is the Principle-1 falsifier and the most important one.
- If Decision 2's chosen representation requires touching more than the enumerated consumer surfaces (Strategy 8, `TypedFieldRef` extraction, MCP DTO), the blast radius was mis-estimated and the other option should be reconsidered.

## Open questions

All open items are carried as `Stakes: exploratory` decisions (5, 6, 7) with explicit Decision-needed-before triggers, or are settled by canon (Decision 1) — none blocks the core mechanism (Decisions 2–4). Per the skill, this doc is `Externally-Grounded`, not `Locked`: Decision 2 (data-model vs relational-path) is locked to two candidate shapes but the final pick is deferred to the build slice's enumerate/probe step, and Decisions 5/6/7 are exploratory. The doc must not advance to `Locked` until Decision 2 resolves to one shape and Decisions 5/6/7 either resolve or are removed.
