---
status: Superseded by: docs/Working/relational-narrowing-core-design-2026-06-03.md
superseded-reason: Merged into the authoritative Slice 2c-i design. The soundness core (guarded-rule drop, satisfiability isolation, depth-1 read-Y-declared, pair-fact representation) converged with the review-informed design and is corroborated/kept. The merge ADOPTED this doc's injection mechanism (inject into the discharge-time `narrowed` dictionary + trusted-fact stream; leave `ExtractFieldInterval` untouched) over the shared-function `applyRelational` flag, on source-verified structural-safety grounds (Satisfiability.cs:96/183/269 read bare ExtractFieldInterval and never pass a narrowed dict, so the scans are structurally isolated). The merge did NOT adopt this doc's `ScopedRelationFact(Field|Arg, anchors)` shape for the rule source — spec §1866 ("Rules operate in field scope. They cannot reference event arguments") makes arg-orientation and anchors YAGNI for a rule; the existing field-only `FieldToFieldConstraint` is reused instead. This doc's Q1 (sign-set fold-composition) is carried forward as an explicit build-time adversarial probe.
phase-target: Phase 7 (total language conformance sweep) — Slice 2c-i of the relational-rules-and-bounds plan; realizes §0.6 item 2 (relational reasoning over multiple fields) for the rule source
comparable-systems-research-status: strong — grounded in `research/architecture/compiler/relational-constraint-representation-survey.md` (octagon strong-closure / difference-bound-matrix pair-representation, with Miné/CUE/DBM primary-source excerpts); the bounded-closure / no-widening envelope is C1 of that survey
sources-consulted:
  - "docs/Working/relational-rules-and-bounds-design-2026-06-02.md — PARENT design (Locked 2026-06-02): Decision 2 = B (relation = pair-fact, discharge via field-to-field path, no DTO change), Decision 4 (single-pass depth-bounded, octagon-closure-minus-widening), R-NARROW-GE/LE, the demonstrator, Falsifiers 1/3/4."
  - "docs/Working/relational-rules-and-bounds-plan-2026-06-02.md — § Heavyweight phase block Slice 2c-i: the probe-grounded core gap (Composition.cs:180-208 field-op-static-only; ProofEngine.cs:88-93 ScopedNumericFact decimal Value; Intervals.cs:202-219; Strategies.cs:1213-1238 shared core), the 8-test matrix, depth-1 default knob, Falsifier corpus gate."
  - "research/architecture/compiler/relational-constraint-representation-survey.md — A-vs-B classification; octagon strong closure = bounded O(n³) Floyd–Warshall over a fixed variable set, no widening; difference logic relation-as-fact; CUE A+fixpoint; witness = rule list (§0.6 item 3); depth-bound = the discrete analogue of one closure pass."
  - "docs/language/precept-language-spec.md §0.1 — eleven Design Principles (Principle 7 compile-time-first, 10 totality, 11 static completeness verbatim)."
  - "docs/language/precept-language-spec.md §0.4 — Execution Model Properties: no loops (item 1), no control-flow branches (2), no reconverging flow (3), expression purity (6); closing paragraph 'bounded relational closure … single-pass … absence of widening is a feature' (line 174)."
  - "docs/language/precept-language-spec.md §0.6 — Proof Engine Design Contract: item 1 numeric interval reasoning, item 2 relational reasoning over multiple fields, item 7 contradictory-rule detection, item 8 vacuous-rule; proof philosophy 1 (soundness over completeness), 3 (no opaque solvers), 6 (attribution required), 7 (sequential proof flow)."
  - "docs/language/precept-language-spec.md §2.4 line 1136 — 'min 5 and rule X >= 5 are interchangeable to the proof engine. Proof participation is a function of a constraint's decidability, not its syntactic form … a relational constraint is decidable only insofar as the referenced fields' own bounds make it so.'"
  - "docs/language/precept-language-spec.md §3.5 line 1376 — bare identifier always names a field/binding, never an arg (D20/Option C); §3.5 line 1343 mutual reference satisfiable / not a cycle."
  - "docs/philosophy.md — prevention-not-detection; the carry-a-constraint paragraph (line 55, 'the runtime enforcement is what makes that carried constraint true, not a second line of defense')."
  - "src/Precept/Pipeline/ProofEngine.cs:88-93 — ScopedNumericFact(NumericSubjectRef Subject, OperatorKind Comparison, decimal Value, …) — the magnitude-only fact vocabulary that cannot carry X >= Y; :31-48 GuardConstraint / FieldToFieldConstraint records; :433-509 the WalkActions sequential-flow stamping (ReassignedBefore)."
  - "src/Precept/Pipeline/ProofEngine.Composition.cs:144-208 — CollectTrustedNumericFacts iterates semantics.Rules[i].Condition through TryGetNumericConstraintFact (the wiring point); :180-208 TryGetNumericConstraintFact handles only field-op-static (both arms call TryGetStaticNumericValue); :307-399 ResolveNumericSignSet / ResolveNumericSubjectSignSet (the sign-set consumer); :499-538 SignSetSatisfiesRequirement / TryMapComparisonToSignSet."
  - "src/Precept/Pipeline/ProofEngine.Strategies.cs:1078-1131 TryFlowNarrowingProof (guard-sourced, IsSubtractionOp-only, ReassignedBefore-gated); :1213-1238 GuardRelationImpliesObligation (the shared field-to-field triple table); :1145-1211 ExtractFieldToFieldBranches / ExtractFieldToFieldLeaf."
  - "src/Precept/Pipeline/ProofEngine.Intervals.cs:16-145 IntervalOfNarrowed (the narrowed-dictionary read path; TypedFieldRef at :67-76 consults `narrowed` before ExtractFieldInterval); :202-220 ExtractFieldInterval (+ FlagLowerBound fold); :489-621 TryIntervalContainmentProofNarrowed / BuildNarrowedIntervals (the guard-sourced interval narrowing dictionary)."
  - "src/Precept/Pipeline/ProofEngine.Satisfiability.cs:89-189 IsConstraintProvablyTrue / BranchHasEmptyFieldInterval — the SATISFIABILITY consumer of ExtractFieldInterval (contradiction / vacuous / unsatisfiable-guard diagnostics): the consumer where tightening an interval would FALSELY REJECT."
  - "src/Precept/Language/NumericInterval.cs — closed [Min,Max] struct; Intersect/Union/Subtract; sentinel ±∞ as decimal.MinValue/MaxValue; IsUnbounded."
---

# Relational narrowing core (Slice 2c-i): independent derivation

## Goal

When done, a field-to-field relational rule `rule X >= Y because "…"` (and its `>`, `<=`, `<` siblings) contributes `Y`'s already-declared interval bound to `X`'s proof-time interval and sign-set, so a downstream divisor / overflow / range obligation that depends on `X` discharges — demonstrated by `set UnitCost = BatchCost / (OnHand - Reserved)` compiling clean when `rule OnHand > Reserved` is declared, with no author-added `when (OnHand - Reserved) != 0` guard — while never declaring an operation "safe" that can fault at runtime and never falsely rejecting a sound precept.

## Scope

- **In scope**:
  - A concrete **field-relation fact** shape carrying the pair `(X, op, Y)` (Decision 2 = B realized at the fact-record level), extracted from a declared rule whose condition is `field op field`.
  - The **R-NARROW** rule: how that fact tightens the subject field's interval (one `⊓` hop into the discharge-time read path) and its sign-set, and exactly where in the proof-engine interval/sign machinery it takes effect.
  - **Both discharge consumers**: the interval path (`IntervalOfNarrowed` via a discharge-time `narrowed` dictionary) and the sign-set path (`ResolveNumericSubjectSignSet` via the trusted-fact stream), plus the existing field-to-field flow-narrowing path (`GuardRelationImpliesObligation`) extended from guard-sourced to rule-sourced — one shared core, no fork.
  - Termination: single-pass, depth-1 (read `Y`'s *pre-narrowing* interval), no fixpoint.
- **Out of scope**:
  - New language *surface*. `rule X op Y` already parses; this design adds no keyword/type/operator/modifier/construct. (The field-reference *modifier bound* `min Floor` desugaring is Slice 2c-ii; the set-action lower-bound emission is 2c-iii.)
  - The emit-unconditionally breach fixes (BUG-017/018/019) — landed in slices 2a/2b; this slice consumes the floor they built.
  - SMT / general fixpoint solving (§0.6 philosophy 3; the survey's rejected CUE-fixpoint cell).
  - Depth > 1 transitive closure (a falsifier-gated knob, not built now).
  - Runtime evaluation changes (governance already enforces the relation at ingress; §0.7).
- **Deferred to future**:
  - Transitive chains (`X >= Y`, `Y >= Z`, `Z min 0`) reaching `X` — only if Falsifier 2 (samples need > 1 hop) trips; then raise to a fixed small N closure passes, still no fixpoint.
  - `+`-combination octagon facts (`X + Y ≤ c`) — the survey's full octagon power; Precept's two-field difference-style relations are the current ceiling.

## Philosophy Alignment

| Principle | Affected? | How served (1 sentence + cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | A declared relation makes a downstream divisor/overflow/range fault a compile-time impossibility rather than a runtime trap (philosophy line 55; §0.7 no-deferral). | The narrowing must never *over*-prove, or prevention becomes a false safety claim. | Accept "supply a constraint" friction (false negatives) to never emit a false "safe". |
| 2. One file, complete rules | Y | The relation fact derives only from a `rule` declared in the same `.precept` (§0.6 philosophy 4). | N/A | N/A |
| 3. Determinism | Y | Single-pass depth-1 closure over declared intervals — no solver, no iteration-order dependence (§0.1 Principle 3; §0.4 no widening). | N/A | N/A |
| 4. Full inspectability | Y | The narrowed interval carries the contributing rule as structured attribution (§0.6 item 13 / philosophy 6); the witness is a named-rule list (survey Implication 4). | N/A | The attribution shape must name the *rule*, not just the literal bound — richer witness payload. |
| 5. Keyword-anchored readability | N | No syntax change; `rule … because` keyword unchanged. | N/A | N/A |
| 6. Explicit domain meaning | N | Operates on already-typed/normalized magnitudes; introduces no new type or qualifier handling beyond what the comparison already has. | N/A | N/A |
| 7. Compile-time-first static checking | Y | A relation now contributes a provable range (§0.6 item 2); the engine proves-or-declines, never guesses (§0.1 Principle 7). | Bounded depth-1 closure is incomplete — some safe programs need an explicit guard/bound. | Accept incompleteness over unsoundness (§0.6 philosophy 1). |
| 8. Approximation honesty | N | Exact-interval reasoning; no approximate lane introduced. | N/A | N/A |
| 9. Mandatory rationale | Y | The relational rule carries an authored `because`; nothing here weakens it (§0.1 Principle 9). | N/A | N/A |
| 10. Totality | Y | A fault-prone op bounded only by an unbounded-operand relation stays unresolved → diagnostic, never compiled hopeful (§0.1 Principle 10; §0.6 item 3). | N/A | N/A |
| 11. Static completeness | Y | The relation closes the gap between "declared `OnHand > Reserved`" and "divisor `OnHand - Reserved` proven nonzero" — a well-typed clean compile no longer faults (§0.1 Principle 11). | N/A | N/A |

**Tradeoff detail (Principles 1/7/10).** The mechanism is deliberately *incomplete but sound*: it narrows `X` only from `Y`'s already-declared interval, declines transitive chains, and degrades to identity when `Y`'s relevant bound is infinite. This accepts false-negative friction (the author may need a direct bound on `Y`) to guarantee the engine never manufactures a false "proved safe" — the only outcome a prevention engine cannot tolerate (§0.6 philosophy 1).

**Companion commitments.** *Stateless-first-class*: relational rules are field-scoped data constraints, fully applicable to stateless precepts — nothing here touches the state machine. *Domain-expert-primary-author*: requires no new vocabulary; an author who writes `rule Available >= Reserved because "…"` already expresses the intent, and this design makes that natural rule do proof work it currently doesn't.

## Language Design Grounding

This design **does not introduce** language surface — `rule X op Y` is already spec-defined (§2.4, §3.5) and parses today. What follows grounds the *semantics* of making a declared relation participate in interval/sign proof.

**General language design — relational constraint propagation.** The capability is *value narrowing*: deriving a tighter domain for one field from a constraint relating it to another. The research survey classifies how real systems represent and discharge `x op y`:

- **Octagons** (Miné; Astrée's weakly-relational domain) keep `±x ± y ≤ c` as a first-class element of a shared difference-bound matrix and discharge by **strong closure** — a bounded O(n³) Floyd–Warshall pass over a *fixed* variable set, with widening a *separate* operator used only for loops (survey Gap 1; Miné Abstract, "a normal form algorithm … **and** a widening operator"). This is the established "relation-as-fact, bounded, legible" model.
- **Difference logic / DBM** is the degenerate octagon (differences only): `x − y ≤ c` is the stored object, satisfiability decided by shortest-path / negative-cycle, no search loop (survey Gap 2).
- **CUE** propagates by lattice unification (greatest-lower-bound) to a **fixpoint** over the constraint graph with explicit cycle-breaking (survey Gap 3; CUE spec "the unification of values a and b is … the greatest lower bound"). It gives the *result shape* Precept wants (`⊓`) but the *wrong evaluation strategy* (fixpoint).

**What Precept takes and diverges from.** Precept takes the relation-as-fact representation and the `⊓` result of octagon closure, and **deliberately diverges from the fixpoint**: §0.4 forbids loops, reconverging flow, and widening, so the narrowing is a *single pass at depth 1*. The survey's central result (C1) is that §0.4's "bounded relational closure" is not a Precept neologism — it maps onto octagon strong closure (total/terminating without widening), so Precept is implementing "octagons minus the loop-widening step." From CUE Precept rejects the fixpoint; from Zod/Pydantic (constraint-composition.md) it rejects the no-propagation stance. The result is strictly weaker than CUE, strictly stronger than Zod, and legible (witness = named-rule list, satisfying §0.6 philosophy 3). `research/language/README.md`'s constraint-composition reference names value narrowing as Precept's one missing composition capability; this design supplies it for the rule source.

**Precept-specific application.** Realizes §0.6 item 2 (relational reasoning over multiple fields) for the *rule* source; extends the existing guard-sourced field-to-field machinery (`GuardRelationImpliesObligation`) which today is guard-driven and subtraction-shaped. Risks conflicting with §0.4 only if implemented as a fixpoint — explicitly forbidden here. No §0.1 principle is overridden; §2.4 line 1136 already states modifier ≡ rule and that relational decidability is a function of the referenced field's bounds.

## Audience and Teachability

**Worked example** (inventory reorder — a plausible operations entity):

```precept
precept ReorderLine

field OnHand as integer nonnegative default 0
field Reserved as integer nonnegative default 0
field BatchCost as money in 'USD' nonnegative default '0 USD'
field UnitCost as money in 'USD' nonnegative default '0 USD'

rule OnHand > Reserved because "there must be unreserved stock before a per-unit cost is meaningful"

state Open initial
in Open modify OnHand, Reserved, BatchCost editable

event Recost
from Open on Recost
    -> set UnitCost = BatchCost / (OnHand - Reserved)
    -> no transition
```

The division `BatchCost / (OnHand - Reserved)` must prove its divisor nonzero. `rule OnHand > Reserved`, with both fields `nonnegative` (so `Reserved ≥ 0`, hence `OnHand ≥ 1` when the strict relation holds), establishes `OnHand - Reserved ≥ 1` — provably nonzero. Today this emits a divisor-unsafe diagnostic and the author must add a guard; after this slice the declared rule discharges it.

**Error message** (the misuse: relying on a relation whose referenced field is unbounded). The author declares `rule Amount >= Floor` with `Floor` unbounded, then divides by `Amount`:

```
PRE0083: Cannot prove the divisor '(Amount - 0)' is non-zero on line 9.
  'Amount' is related to 'Floor' by 'rule Amount >= Floor', but 'Floor' has no
  declared lower bound, so the relation does not pin 'Amount' away from zero.
  Add a bound to 'Floor' (e.g. 'field Floor as number min 1'), or guard the
  division with 'when Amount != 0'.
```

This serves the domain expert (not the compiler engineer): it names the *business field* whose missing constraint is the real cause (`Floor`), states the relation in the author's own terms (`Amount >= Floor`), and offers two concrete DSL repairs. (Wording-and-attribution polish rides with Slice 2c-ii's author-facing message; this slice's floor is that the obligation *emits* rather than silently passing.)

**10-minute teaching path:**
1. `docs/language/precept-language-spec.md §2.4` (line 1136) — a relational rule participates in proof identically to a modifier; decidability depends on the referenced field's bounds (2 min).
2. `samples/loan-application.precept` — a real precept with `rule … because` and field bounds (3 min).
3. `docs/language/precept-language-spec.md §0.6` items 1–2 — what "proof" means and why an unbounded reference can't pin a range (3 min).
4. The error message above — how to read a "supply a constraint" diagnostic (2 min).

## Semantic Rules

### Fact shape

A new fact record carries the *pair* (Decision 2 = B), sibling to the magnitude-only `ScopedNumericFact`:

```
ScopedRelationFact(
    NumericSubjectRef Subject,     // X — the field being narrowed
    OperatorKind     Comparison,   // op ∈ { >, >=, <, <= } in Subject-on-left orientation
    NumericSubjectRef Related,     // Y — the field whose declared interval supplies the bound
    string?          AnchorState,  // scope anchor (mirrors ScopedNumericFact)
    string?          AnchorEvent)
```

`Subject` and `Related` reuse the existing `NumericSubjectRef` (`Field | Arg`, with `EventName`). The record is structurally distinct from `ScopedNumericFact` precisely because its right side is a *field*, not a `decimal` magnitude (the gap named in plan §2c-i: `ProofEngine.cs:88-93` cannot represent `X >= Y`). This is the survey's "relation-as-fact" shape; the witness for a narrowed `X` carries the originating rule by index, satisfying §0.6 item 13 / philosophy 6.

### Extraction — and every rule form

Extraction extends the existing fact-stream wiring (`CollectTrustedNumericFacts`, `ProofEngine.Composition.cs:144-161`, which already iterates `semantics.Rules[i].Condition`). A new extractor `TryGetRelationFact(condition, anchor…, out fact)` fires when:

```
condition  is  TypedBinaryOp(left, op, right)
op ∈ { GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual }
left  is TypedFieldRef Lf   (or TypedArgRef)
right is TypedFieldRef Rf   (or TypedArgRef)
```

producing `ScopedRelationFact(subject = Lf, op, related = Rf, anchors)`. (A `field op static` rule continues through the existing `TryGetNumericConstraintFact`; the two extractors are tried in sequence — a comparison is one or the other, never both.)

**Rule-form handling — the load-bearing soundness case.** A rule is **not** always unconditional. The existing fact path already encodes this: `TryGetNumericEnsureFact` (`Composition.cs:164-178`) returns `false` for a guarded ensure, and `CollectTrustedNumericFacts` blocks a rule whose own obligation is unresolved. A `rule` carries an optional **`when` guard** (`semantics.Rules[i].Guard`, read by `TryFlowNarrowingProof` at `Strategies.cs:1085-1090`). A *guarded* rule `rule X >= Y when C` asserts the relation **only when C holds** — it is **not** an unconditional fact about the pair and **must not** be extracted as one (extracting it would let `X >= Y` narrow `X` on a path where `C` is false → over-prove). Therefore:

- **Unconditional rule** (`semantics.Rules[i].Guard is null`) → extract `ScopedRelationFact`. Sound: the rule holds on every operation (governance enforces it at ingress; philosophy line 35), so the relation is a global invariant.
- **Guarded rule** (`Guard is not null`) → **do not** extract an unconditional fact. (A future slice may scope the fact to the guard's anchor; out of scope here — the safe default is to decline, matching `TryGetNumericEnsureFact`.)
- **Self-relation** (`Subject == Related`, `rule X >= X`) → extract nothing (vacuous; narrowing `X` from its own interval is identity — see termination).
- **Anchored ensure** (`EventPrecondition` / `StateResident`, unguarded) → extract with the anchor, exactly as `TryGetNumericEnsureFact` already does for magnitude facts; the anchor scopes the fact to the matching context (`FactAppliesToContext`, `Composition.cs:474-497`).

### R-NARROW (the narrowing rule)

For a `ScopedRelationFact(X, op, Y, anchors)` that applies to the obligation's context, let `⟦Y⟧ = [ylo, yhi]` be `Y`'s interval **read without any relational narrowing** (its declared modifiers + flag-lower-bound, i.e. exactly what `ExtractFieldInterval(Y)` returns today). Then:

```
  fact X >= Y     ⟦Y⟧ = [ylo, yhi]        (ylo may be −∞)
  ──────────────────────────────────────────────────────── (R-NARROW-GE)
        ⟦X⟧discharge  :=  ⟦X⟧ ⊓ [ylo, +∞)

  fact X >  Y     ⟦Y⟧ = [ylo, yhi]
  ──────────────────────────────────────────────────────── (R-NARROW-GT)
        ⟦X⟧discharge  :=  ⟦X⟧ ⊓ [ylo + δ, +∞)      δ = 1 if X is integer-typed, else 0

  fact X <= Y     ⟦Y⟧ = [ylo, yhi]        (yhi may be +∞)
  ──────────────────────────────────────────────────────── (R-NARROW-LE)
        ⟦X⟧discharge  :=  ⟦X⟧ ⊓ (−∞, yhi]

  fact X <  Y     ⟦Y⟧ = [ylo, yhi]
  ──────────────────────────────────────────────────────── (R-NARROW-LT)
        ⟦X⟧discharge  :=  ⟦X⟧ ⊓ (−∞, yhi − δ]      δ = 1 if X is integer-typed, else 0
```

The `δ` for the strict case is sound for integers (no integer in `(ylo, ylo+1)`) and conservatively `0` for decimals (a decimal can be arbitrarily close to `ylo` from above, so `X > Y` only gives `X ≥ ylo` as a *closed* bound — a sound superset; it never claims `X > ylo` as a usable closed lower bound). This mirrors the integer-vs-decimal half-step dispatch already in `NarrowByConstraint` / `NegateConstraintToInterval` (`Intervals.cs:737-758`).

**Where it takes effect — two read paths, never the shared one.** The narrowing is injected at *discharge time* into the two obligation-discharge read paths, and explicitly **not** into the bare `ExtractFieldInterval` (because the satisfiability scan reads that — see the per-consumer proof):

1. **Interval path** — `BuildNarrowedIntervals` (`Intervals.cs:514`) already builds a per-field `narrowed` dictionary from guards/sibling-rejects that `IntervalOfNarrowed` consults at the `TypedFieldRef` case (`:67-76`). R-NARROW adds the relation-fact contribution to that dictionary: for each applicable `ScopedRelationFact`, set `narrowed[X] := (narrowed[X] ?? ExtractFieldInterval(X)) ⊓ relationalBound(⟦Y⟧)`. This feeds IntervalContainment / overflow / OutOfRange (the demonstrator's overflow-direction tests 3/4).
2. **Sign-set path** — `ResolveNumericSubjectSignSet` (`Composition.cs:365-399`) already folds `trustedFacts` into a subject's sign-set. R-NARROW maps an applicable `ScopedRelationFact(X, op, Y)` into a sign-set contribution **only when `⟦Y⟧`'s relevant bound is a known constant of decidable sign** (e.g. `Y ≥ 0` from `nonnegative` makes `X > Y` ⇒ `X` strictly positive ⇒ `Nonzero`/`Positive`). This feeds divisor `!= 0` / non-negative obligations — the demonstrator's `BatchCost / (OnHand - Reserved)` divisor (via the flow-narrowing path below).
3. **Flow-narrowing path** — `GuardRelationImpliesObligation` (`Strategies.cs:1213-1238`) already proves `(A - B) op 0`-shaped obligations from a *guard* relating `A` and `B`. The demonstrator's divisor is exactly `(OnHand - Reserved) != 0`. The rule-sourced relation feeds the **same** `GuardRelationImpliesObligation` core: `TryFlowNarrowingProof` (`:1078`) additionally pulls field-to-field constraints from unconditional rules (not only the row guard), subject to the same `ReassignedBefore` staleness gate. One shared core, no fork (Decision 2's "guard-sourced and rule-sourced share one core").

### Termination

Closure is **single-pass, depth-1**, by construction:

- **The depth bound is structural, not a counter.** `⟦Y⟧` in every R-NARROW rule is `ExtractFieldInterval(Y)` — `Y`'s **declared** interval, computed *without* consulting the relation fact stream. The narrowing never reads a `narrowed[...]` value for `Y`; it reads `Y`'s base bounds only. Therefore narrowing `X` cannot trigger narrowing `Y` from a third field, and there is no recursive descent through facts. Each obligation's discharge does **one** pass over the applicable relation facts, each fact contributing one `⊓`. The recursion that `IntervalOfNarrowed` performs is over the *expression tree of the obligation site* (finite, acyclic — §0.4 item 1/3), not over the fact graph.
- **Self-reference** (`rule X >= X`) extracts no fact (extraction declines `Subject == Related`), so it cannot loop; it compiles as vacuous (§3.5 line 1343).
- **Mutual reference** (`rule A >= B` + `rule B >= A`) extracts two independent facts. Discharging an obligation on `A` reads `⟦B⟧` (B's *declared* interval), and an obligation on `B` reads `⟦A⟧` (A's *declared* interval) — neither reads the other's *narrowed* value, so the two narrowings never feed each other. The conjunction `A ≥ B ∧ B ≥ A` is `A == B`, satisfiable, no fixpoint (§3.5 line 1343). Compiles and terminates.
- **Bound on work**: with `n` fields and `r` relation facts, each obligation discharge does O(r) `⊓` operations over O(1)-cost base-interval reads — total O(obligations × r), no iteration to convergence. This is the discrete analogue of one octagon closure pass (survey Implication 5); raising depth later means "run N passes," still terminating, never a fixpoint.

### Soundness preservation claim

- **Principle 7 / 10 / 11** hold because every R-NARROW rule only ever **tightens** `⟦X⟧` by `⊓` with a bound derived from `Y`'s already-established interval, and is the **identity** when that bound is infinite (`ylo = −∞` for GE: `⊓ [−∞,+∞)` changes nothing). A tightening can only *help* a discharge succeed or leave it unchanged; it can never widen `X`'s provable range, so it cannot manufacture a false "proved safe". An obligation the narrowed interval does not cover stays `Unresolved` → diagnostic (§0.7 no-deferral). **This is sound only because the bound comes from `Y`'s declared interval, which governance enforces at ingress** (philosophy line 55) — the relation is a true global invariant for an unconditional rule.
- **Principle 3** holds because the closure is single-pass and reads only declared intervals — disposition is a deterministic function of the declared facts, no iteration-order dependence (§0.4 no widening).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** This is **discharge-side** reasoning, in pipeline code (proof engine), not catalog metadata — it is *how* the engine proves over catalog-declared obligations, exactly where the existing Strategy-4 flow-narrowing and `BuildNarrowedIntervals` sit. The catalog declares *what* must be proved (the `ProofRequirement` DU, the operator/divisor obligations); the relational narrowing is *how* a declared rule discharges them. No new `ProofRequirementKind`, no catalog member, no DTO (Decision 2 = B). The one new internal type is the `ScopedRelationFact` record — a private proof-engine fact vocabulary extension, sibling to `ScopedNumericFact`, not a public surface.

**Cross-component propagation:**
- **Runtime (parser, type checker, evaluator, diagnostics):** Parser/type-checker — **None** (the rule already parses and type-checks). Proof engine — the `ScopedRelationFact` extraction + the three read-path contributions + the rule-sourced extension of `TryFlowNarrowingProof`. Evaluator — **None** (governance enforces the rule at ingress; §0.7). Diagnostics — **None new**; the relational-unprovable case reuses the existing divisor (PRE0083) / overflow / OutOfRange diagnostics, with richer attribution naming the contributing rule (rides with 2c-ii).
- **Tooling (highlighting, completions, hover, semantic tokens):** Hover/proof-attribution gains relational-rule provenance (a narrowed interval now cites the contributing `rule`); surfaced through the structured proof model, not message text (§0.6 philosophy 6). No grammar/token/completion change.
- **MCP (vocabulary, DTOs, tool output):** `precept_compile` proof-obligation output shows the relational discharge (a previously-unresolved obligation becomes proved); **no new DTO shape** — Decision 2 = B requires no `IntervalContainmentProofRequirement` / `CompileToolDtos.cs` change (Falsifier 4: if it does, the A-vs-B call was wrong).

**Breaking changes.** None to public contract. A precept that previously *failed* (divisor unsafe with a declared relation that should have discharged it) may now correctly compile — that is the intended completeness gain, in the safe direction.

### Complete map of interval/fact consumers and per-consumer soundness verdict

I grepped every reader of the interval I tighten and the fact stream I add to. There are **three distinct consumer families**, and the narrowing must be sound for each, in the direction that family can be wrong.

| # | Consumer (file:line) | What it reads | Direction it can be wrong | Verdict |
|---|---|---|---|---|
| C1 | `IntervalOfNarrowed` TypedFieldRef case (`Intervals.cs:67-76`) via `BuildNarrowedIntervals` (`:514`) → `TryIntervalContainmentProofNarrowed` (`:489`) | the discharge-time `narrowed[X]` dictionary | **over-prove**: an interval too *narrow* could make an overflow/OutOfRange obligation falsely discharge | **Sound.** R-NARROW only `⊓`s `narrowed[X]` with `Y`'s *declared* bound (an enforced invariant). The result is ⊆ the true reachable set of `X`, so containment can only correctly succeed; it never claims a smaller-than-true range that excludes a reachable faulting value, because the bound it adds is itself a true lower/upper bound on `X`. |
| C2 | `ResolveNumericSubjectSignSet` (`Composition.cs:365-399`) ← `trustedFacts` → `SignSetSatisfiesRequirement` (`:499`) feeding divisor `!=0` / non-negative | the sign-set `&=`-folded from facts | **over-prove**: a sign-set that *drops* a sign actually reachable (e.g. claims `Positive` when 0 is reachable) → false divisor-safe | **Sound.** A `ScopedRelationFact(X,op,Y)` contributes a sign only when `⟦Y⟧`'s relevant bound is a known constant of decidable sign; the contributed sign-set is the *true* implication (`X > Y ≥ 0 ⇒ X > 0`). The `&=` fold (`:392`) only ever *removes* signs that some constraint proves impossible — and the relation does prove them impossible — so it never drops a reachable sign. When `⟦Y⟧`'s bound is unknown, no sign is contributed (identity). |
| C3 | `IsConstraintProvablyTrue` (`Satisfiability.cs:96`), `BranchHasEmptyFieldInterval` (`:183`), the satisfiability scan at `:269` — **contradiction (§0.6 item 7), vacuous (item 8), unsatisfiable-guard / tautological-guard** diagnostics | the **bare** `ExtractFieldInterval(field)` (NOT the discharge `narrowed` dict) | **false-reject**: a *tightened* interval could make a satisfiable guard look empty (false UnsatisfiableGuard) or a non-vacuous rule look always-true (false VacuousRule) | **Sound *by construction* — the narrowing is NOT applied here.** R-NARROW injects only into the discharge-time `narrowed` dictionary and the trusted-fact stream consumed by C1/C2. The satisfiability scan calls the *bare* `ExtractFieldInterval`, which this design **leaves untouched** (it does not fold relation facts). So C3 sees exactly today's intervals — no new false-reject path. **This is the load-bearing placement decision** (Decision 3): tightening `ExtractFieldInterval` globally would corrupt C3. |

**The two-directional bar, discharged.** Never-over-prove is held by C1 + C2 (every tightening is a true bound; identity when unknown). Never-falsely-reject is held by C3 (the shared `ExtractFieldInterval` is not narrowed, so the contradiction/vacuous/guard-satisfiability diagnostics are unaffected). The one obligation I **flag** rather than fully close: see Open Questions Q1 (the sign-set `&=` fold interaction with a *pre-existing* fact on the same subject — argued sound below but worth a targeted adversarial probe at build).

### External architectural precedent

The architectural problem: *how does a constraint language propagate a two-field relation into a field's provable range without a general solver?* The sharpest comparators are **octagons** and **difference logic** (survey), which keep the relation as a shared fact discharged by bounded closure:

> "allows us to represent invariants of the form (±x ± y ≤ c) … This algorithm … runs in O(N³) time … **a widening operator to compute least fixpoint approximations**" (Miné, octagon paper, Abstract — closure and widening are *distinct*; widening is only for loops). Mirrored at `research/references/octagon-domain/…`.

**What Precept takes:** the relation-as-fact representation (`ScopedRelationFact` = the DBM cell `X − Y ≤ c`) and the `⊓` tightening (octagon meet). **What Precept deliberately diverges from:** the closure is run **once at depth 1**, not iterated, and there is **no widening** — because Precept forms no loops (§0.4), the widening octagons use for loop fixpoints is unnecessary. This is the survey's C1 ("octagons minus the loop-widening step"). CUE is the rejected fixpoint comparator: it gives the same `⊓` result via a fixpoint Precept's §0.4 forbids. The divergence is principled and already grounded in-tree (`compiler-and-runtime-design.md §2` grounds the catalog choice against CUE; §0.4 calls the absence of widening "a feature").

## Inventory of what will be built

- `src/Precept/Pipeline/ProofEngine.cs` — add `private readonly record struct ScopedRelationFact(NumericSubjectRef Subject, OperatorKind Comparison, NumericSubjectRef Related, string? AnchorState = null, string? AnchorEvent = null);` near `ScopedNumericFact` (:88-93).
- `src/Precept/Pipeline/ProofEngine.Composition.cs`:
  - `TryGetRelationFact(condition, anchorState, anchorEvent, out ScopedRelationFact)` — the `field op field` extractor; declines `Subject == Related` and guarded rules.
  - `CollectTrustedRelationFacts(...)` (mirror `CollectTrustedNumericFacts` :114) — gather unconditional-rule/anchored-ensure relation facts, blocking any whose own obligation is unresolved.
  - extend `ResolveNumericSubjectSignSet` (:365) to fold an applicable `ScopedRelationFact` into the sign-set when `⟦Related⟧` has a decidable-sign constant bound.
- `src/Precept/Pipeline/ProofEngine.Intervals.cs` — extend `BuildNarrowedIntervals` (:514) to add relation-fact contributions to the `narrowed` dictionary (`narrowed[X] := (narrowed[X] ?? ExtractFieldInterval(X)) ⊓ relationalBound(⟦Y⟧)`); a helper `RelationalBound(op, ⟦Y⟧, xType)` implementing R-NARROW-GE/GT/LE/LT with the integer `δ`. **`ExtractFieldInterval` itself is NOT modified** (C3 soundness).
- `src/Precept/Pipeline/ProofEngine.Strategies.cs` — extend `TryFlowNarrowingProof` (:1078) to additionally source field-to-field constraints from unconditional rules (not only the row guard), feeding the existing `GuardRelationImpliesObligation` (:1213) unchanged; apply the same `ReassignedBefore` staleness gate.
- Test stubs:
  - `test/Precept.Tests/ProofEngine/RelationalNarrowingTests.cs` — the 8-test matrix below.
  - additions to an existing satisfiability test file — assert the C3 invariant (a relation does **not** turn a satisfiable guard into UnsatisfiableGuard, nor a non-vacuous rule into VacuousRule).

## Decisions

### Decision 1: The fact shape is a sibling pair-fact record `ScopedRelationFact(X, op, Y)`, not a field-reference variant of `ScopedNumericFact`

**Stakes**: medium

- **Rationale**: A relation's right side is a *field*, not a magnitude. `ScopedNumericFact` carries `decimal Value` (`ProofEngine.cs:88-93`) and structurally cannot represent `X >= Y`. Decision 2 of the parent (= B) settles that a relation is a fact about the *pair* `(X, Y)`; a sibling record models that directly. The CLAUDE.md DU discipline ("use discriminated unions for varying shapes; don't paper over with nullable fields") forbids bolting a nullable `RelatedField` onto `ScopedNumericFact`.
- **Tradeoff accepted**: A second fact type means the fact-collection and fold sites handle two records rather than one — minor duplication of the iterate-and-apply scaffold.
- **Alternatives considered**:
  - *Add `string? RelatedField` to `ScopedNumericFact`* — rejected: a flat record with a nullable that flips the meaning of `Value` is exactly the shape the catalog/DU rule forbids; it conflates magnitude-fact and pair-fact.
  - *Reuse `FieldToFieldConstraint`* (`ProofEngine.cs:45-48`) — partially reused: that record is the *guard-decomposition* leaf already feeding `GuardRelationImpliesObligation`; the rule-sourced fact needs an anchor (state/event scope) and a subject/related orientation that `FieldToFieldConstraint` lacks, so the new record carries the scope while the flow-narrowing path still funnels through the shared `FieldToFieldConstraint`-shaped core.
- **Precedent**: The survey's relation-as-fact (B) representation — octagon/DBM cells are `(var, var, bound)` triples, not `(var, magnitude)` pairs. In-tree: `ScopedNumericFact` is the magnitude sibling; `FieldToFieldConstraint` is the guard sibling.
- **Sources consulted for this decision**: `ProofEngine.cs:88-93` — `ScopedNumericFact(… decimal Value …)`; `:45-48` — `FieldToFieldConstraint(LeftField, Comparison, RightField)`; parent design Decision 2 "a relation is a fact about the *pair* (X,Y)"; survey § "A-vs-B classification" (B = relation-as-fact). Grepped §0.6 — item 2 names relational reasoning as a contract responsibility but is silent on fact representation (mechanism deferred to compiler-and-runtime-design.md per §0.7), so this is a genuine mechanism decision, not spec-settled.
- **Strongest counter-evidence**: One fact type is simpler for the fold sites. Response: simplicity that conflates two semantically-distinct shapes is the anti-pattern the DU rule names; the sibling record is the smaller long-term cost.

### Decision 2: Narrowing is injected only into the discharge-time read paths (`narrowed` dictionary + trusted-fact stream), never into the shared `ExtractFieldInterval`

**Stakes**: high

- **Rationale**: `ExtractFieldInterval` is read by **two unrelated consumer families**: the obligation-discharge interval path (C1) *and* the satisfiability scan (C3: contradiction/vacuous/unsatisfiable-guard/tautological-guard diagnostics, `Satisfiability.cs:96/183/269`). A tighten that helps C1 discharge would, if applied to the shared function, *corrupt* C3 — making a satisfiable guard look empty (false `UnsatisfiableGuard`) or a non-vacuous rule look always-true (false `VacuousRule`). Confining the narrowing to the discharge-time `narrowed` dictionary (already the seam for guard/sibling narrowing) and the trusted-fact stream keeps C3 reading today's intervals — the never-falsely-reject half of the bar.
- **Tradeoff accepted**: The narrowing is duplicated across the interval read path and the sign-set read path (two injection sites) rather than centralized in `ExtractFieldInterval`. Accept two injection sites over one corrupted shared consumer.
- **Alternatives considered**:
  - *Fold relation facts into `ExtractFieldInterval`* — **rejected**: corrupts C3; manufactures false rejections; violates the never-falsely-reject half of the soundness bar. This is the decisive rejection.
  - *Apply at discharge only* (chosen) — localizes to the obligation-discharge seam, matching where `BuildNarrowedIntervals` already injects guard narrowing.
- **Precedent**: `BuildNarrowedIntervals` (`Intervals.cs:514`) is the existing in-tree pattern — guard and sibling-reject narrowing already live in a *discharge-time* dictionary that `IntervalOfNarrowed` consults, explicitly *not* in `ExtractFieldInterval`. This design follows that established split.
- **Sources consulted for this decision**: `Satisfiability.cs:96` `IsConstraintProvablyTrue` (reads `ExtractFieldInterval`, returns provably-true for vacuous/contradiction); `:183` `BranchHasEmptyFieldInterval` (UnsatisfiableGuard); `Intervals.cs:514-621` `BuildNarrowedIntervals` (the discharge-time dictionary `IntervalOfNarrowed` consults at `:67-76`); `:202-220` `ExtractFieldInterval` (the shared bare read). Grepped §0.6 items 7/8 — contradiction and vacuous detection are named contract responsibilities (currently spec-only per the status table, but the diagnostics exist and the scan runs), so corrupting their input is a live regression risk; spec is silent on *where* narrowing attaches (mechanism), so this is a mechanism decision.
- **Strongest counter-evidence**: Centralizing in `ExtractFieldInterval` would also benefit any future consumer uniformly. Response: that uniformity is precisely the hazard — C3 must *not* benefit, because for C3 a tighter interval is unsound (false reject). The split is not incidental; it is the soundness boundary.
- **Reversibility**: `Hard` — once attribution and tests depend on the discharge-time placement, moving it would require re-proving C3 safety. But the *direction* (discharge-only) is the sound one and unlikely to reverse.
- **Blast radius**: `ProofEngine.Intervals.cs` (BuildNarrowedIntervals), `ProofEngine.Composition.cs` (sign-set fold); zero change to `Satisfiability.cs`, zero to catalog/DTO/samples (additive discharge). External consumers: none (no public surface).

### Decision 3: A guarded rule contributes no unconditional relation fact; only unguarded rules (and unguarded anchored ensures) are extracted

**Stakes**: medium

- **Rationale**: `rule X >= Y when C` asserts the relation **only when `C` holds**. Extracting it as an unconditional fact would let `X >= Y` narrow `X` on a path where `C` is false — an over-prove (the worst outcome). The existing fact path already encodes this discipline: `TryGetNumericEnsureFact` (`Composition.cs:169`) returns `false` for a guarded ensure. Relation facts inherit the same rule.
- **Tradeoff accepted**: A guarded relation that *would* be safe to scope to its guard's anchor is conservatively dropped — false-negative friction. Accept incompleteness (the author adds an unconditional bound or guards the operation) over unsoundness.
- **Alternatives considered**:
  - *Extract guarded rules as anchored facts scoped to the guard* — deferred (out of scope): sound in principle but requires modeling the guard's truth-region as a fact scope, which is more than the core needs; the safe default (decline) ships first.
  - *Extract guarded rules unconditionally* — **rejected**: over-proves on the guard-false path; a direct soundness breach.
- **Precedent**: `TryGetNumericEnsureFact` (`Composition.cs:164-178`) declines guarded ensures; `CollectTrustedNumericFacts` (`:122-142`) blocks facts whose own obligation is unresolved. The sequential-flow `ReassignedBefore` gate (`ProofEngine.cs:509-523`, `Strategies.cs:1107`) is the analogous staleness discipline.
- **Sources consulted for this decision**: `Composition.cs:164-178` `TryGetNumericEnsureFact` (`if (ensure.Guard is not null) return false;`); `Strategies.cs:1085-1090` `TryFlowNarrowingProof` reads `semantics.Rules[ri.RuleIndex].Guard` (rules carry a guard); philosophy line 33 "A rule can be unconditional (always checked) or guarded (checked only when a declared condition is met)". Grepped §0.6 philosophy 7 (sequential proof flow) + §2.4 — confirms a rule's fact is conditional on its guard; this is implementation against the locked sequential-flow / governance semantics, not a fresh choice.
- **Strongest counter-evidence**: Dropping all guarded relations loses real reach (a guarded `rule X >= Y when Active` is common). Response: true, but the unconditional case is the common demonstrator and the sound floor; the anchored-guarded extension is a clean follow-on that does not change the core.

### Decision 4: Closure is single-pass, depth-1 — read `Y`'s declared interval, never its narrowed one

**Stakes**: medium (implementation against Locked parent Decision 4)

- **Rationale**: §0.4 forbids loops, reconverging flow, and widening; a fixpoint propagation would violate it and forfeit termination/determinism. R-NARROW reads `⟦Y⟧ = ExtractFieldInterval(Y)` — `Y`'s *declared* interval, computed without consulting any relation fact — so narrowing `X` cannot recurse into narrowing `Y`. Depth is bounded structurally (one `⊓` per fact), not by a counter.
- **Tradeoff accepted**: A transitive chain `X >= Y`, `Y >= Z`, `Z min 0` does not propagate `Z`'s bound to `X` at depth 1; the author adds a direct bound or the depth is raised (falsifier-gated). Accept friction over fixpoint complexity.
- **Alternatives considered**:
  - *Iterate-to-fixpoint (CUE-style)* — **rejected**: violates §0.4; introduces the termination/precision-loss concerns §0.4 names; opaque-witness risk (§0.6 philosophy 3).
  - *Single-pass depth-1* (chosen) — bounded, terminating, legible; the survey's "one octagon closure pass" analogue.
- **Precedent**: §0.4 line 174 verbatim — "bounded relational closure … single-pass … The absence of widening is a feature." Survey C1 — §0.4's bounded closure = octagon strong closure, total without widening. `BuildNarrowedIntervals` already reads `ExtractFieldInterval` (base) for each guard field, not a recursively-narrowed value — the depth-1 precedent in-tree.
- **Sources consulted for this decision**: §0.4 line 174 (bounded closure, no widening); survey C1 + Implication 5 (depth-bound = one closure pass); `Intervals.cs:552/587` (BuildNarrowedIntervals reads base `ExtractFieldInterval` per field). Grepped §0.4 — the no-fixpoint constraint is locked spec; single-pass is implementation against it.
- **Strongest counter-evidence**: Depth-1 under-proves common transitive chains. Response: the cap is Falsifier 2 — if samples need > 1 hop, raise to a fixed small N closure passes (still no fixpoint); §0.4's guarantee is no-fixpoint, not a specific depth.

## Falsifiers

- **F1 (the most important — Principle-1 falsifier).** If any relational narrowing ever discharges an obligation that a runtime trap then fires on (a false "proved safe"), the mechanism is unsound — immediate redesign. Standing guards: test 2 and test 5 (the over-prove cases must *reject*); the adversarial diff review must trace every new discharge for a false-safe.
- **F2 (depth).** If the depth-1 cap under-proves a transitive relation chain in **two or more** `samples/`, raise the bounded depth to a fixed small N (still no fixpoint); if no fixed N suffices without a sample needing genuine fixpoint reasoning, the §0.4 envelope is being stretched and scope is wrong.
- **F3 (false-reject / C3 corruption).** If introducing the relation fact turns a previously-satisfiable guard into a `UnsatisfiableGuard`, or a non-vacuous rule into `VacuousRule`, or otherwise newly-reds a genuinely-safe sample (> ~3 newly-red), the narrowing has leaked into the satisfiability consumer (C3) — Decision 2 was violated; confine the narrowing to discharge-time before advancing.
- **F4 (Decision 2 = B / no-churn).** If the rule-sourced discharge turns out to require an `IntervalContainmentProofRequirement` data-model or `CompileToolDtos.cs` change, the no-DTO-churn rationale that grounded B-over-A was wrong; reconsider A (parent Falsifier 4).

## Acceptance criteria

The enumerated input space = {relation op} × {downstream obligation} × {source-field boundedness} × {direction}:

| # | Input (rule + downstream op) | Expected | Falsifies |
|---|---|---|---|
| 1 | `rule OnHand > Reserved` + `BatchCost / (OnHand - Reserved)` (the demonstrator), both fields `nonnegative` | **compiles clean** (divisor provably ≥ 1) | reach too weak / over-reject |
| 2 | same shape, but `Reserved` **unbounded above** and division by **bare `OnHand`** (relation gives `OnHand > Reserved` but no lower pin on `OnHand` when `Reserved` can be negative) | **rejects**, names the unbounded field | over-prove (false safe) |
| 3 | `rule X >= Y`, `Y min 0`, then `Z / X` where `X` is `integer` | X gains lower bound 0; if 0 reachable (Y's min is 0, X ≥ 0 admits 0), **rejects** (X may be 0) | direction/over-prove |
| 4 | `rule X <= Y`, `Y max 100`, overflow obligation on `X` | X's upper bound tightened to 100 → overflow **discharges** (R-NARROW-LE) | LE direction |
| 5 | `rule X >= Y` with `Y` unbounded-below | identity narrowing → downstream lower-bound obligation **stays unresolved → rejects** | identity-degradation soundness |
| 6 | mutual `rule A >= B` + `rule B >= A` | **compiles** (conjoins to `A == B`, no hang) | fixpoint formed |
| 7 | self `rule X >= X` | **compiles** (vacuous, no hang) | fixpoint formed |
| 8 | `>` vs `>=` boundary: `rule X > Y` (strict, integer X) discharges `X - Y >= 1`; non-strict `rule X >= Y` does **not** discharge `(X - Y) != 0` on decimals | strict relation discharges strict + `>=1` integer; non-strict does not over-discharge | off-by-one in the `δ` / `GuardRelationImpliesObligation` reuse |
| C3 | a satisfiable guard `when A > 5` on a precept that also declares `rule A >= B` (B unbounded) | **no `UnsatisfiableGuard` / `VacuousRule`** newly emitted (the relation did not leak into the satisfiability scan) | Decision 2 leak |

- `dotnet test` green across all four projects; numeric-constant golden snapshots byte-identical (literal-bound and satisfiability paths untouched — the relational path is additive at discharge).
- Decision 2 = B confirmed: zero change to `IntervalContainmentProofRequirement` / `CompileToolDtos.cs`.
- `SampleCompilesCleanTests`: the relational-demonstrator-shaped samples stay green; > ~3 newly-red ⇒ reach gap (F3), fixed before advancing.

## Dependencies

- **Upstream**: parent design Locked 2026-06-02 (Decisions 2/4). Slices 2a/2b (done) — the emit-unconditionally floor + the interval/length/count domains the narrowing tightens *against*. The `GuardRelationImpliesObligation` + `BuildNarrowedIntervals` + `ScopedNumericFact` machinery (shipped). D20/Option C (bare identifier names a field, so `TryGetRelationFact`'s `TypedFieldRef` arms resolve to fields, not args).
- **Downstream**: Slice 2c-ii (field-ref modifier bounds `min Floor` desugar to a synthetic rule discharging through this core) and 2c-iii (set-action lower-bound), both gating on this core. Realizes §0.6 item 2 (relational reasoning over multiple fields) for the rule source.

## Doc-update enumeration

Per the CLAUDE.md routing table — obligations for `/lifecycle-5-promote`, **not edited this pass**:
- `docs/compiler/proof-engine.md` § Strategy 4 (Flow Narrowing) — document the rule-sourced relational narrowing (R-NARROW-GE/GT/LE/LT), the single-pass depth-1 bound, that guard-sourced and rule-sourced share one `GuardRelationImpliesObligation` core, and the discharge-time-only placement (not `ExtractFieldInterval`); § Implementation State.
- `docs/language/precept-language-spec.md §0.6` — item 2 (relational reasoning over multiple fields) implementation-status: now live for unguarded rule-sourced `>=`/`>`/`<=`/`<`.
- `docs/tooling/language-server.md` — hover proof-attribution gains relational-rule provenance.
- NOT in this slice: `business-domain-types.md` line 426 / spec §3.8 (promote-stage, 2c-ii); the diagnostic author-facing wording (rides with 2c-ii's message); MCP doc (no DTO change).

## Operational dimensions

- **Observability** (triggered — proof/diagnostic surface): when a relational narrowing fails to discharge, the author diagnoses via the obligation diagnostic carrying structured attribution naming the contributing rule and the field whose bound is missing — surfaced through `precept_compile` proof obligations and hover, not parsed from message text (§0.6 philosophy 6). A successful relational discharge appears in the proof model as a proved obligation citing the rule.
- **Security**: N/A — no source-text-ingestion surface change (the rule already parses; no new tokens/parser paths).
- **Evolvability**: N/A — no new external-standard dependency; the narrowing operates on already-normalized decimal magnitudes via the existing `NumericInterval` algebra.

## Open questions

- **Q1 (flag-only; argued sound, probe at build).** When a subject `X` already carries a magnitude fact *and* a relation fact in the same context, the sign-set fold `&=`s both (`Composition.cs:392`). The `&=` is a set-intersection that only removes signs each constraint independently proves impossible, so combining a true magnitude implication with a true relation implication is sound (both are true bounds; their intersection is a true tighter bound). I could not find a counterexample, but this is the one cell the per-consumer proof leans on a fold-composition argument rather than a single-fact argument — flagged for a targeted adversarial probe in the build's diff review (it does not block locking the *design*; it blocks claiming the *build* sound without the probe). This is not an open *design* decision — the mechanism is fixed; it is a verification obligation on the implementation.

(No open *design* questions remain. Q1 is a build-time verification obligation, not an unresolved decision. The design is `Externally-Grounded`: all decisions carry four-leg rationale with sources; the high-stakes Decision 2 carries counter-evidence, reversibility, and blast-radius; no `irreversible` decision, so no cooling-off applies; Falsifiers present for the external-author-visible discharge behavior.)
