---
status: Externally-Grounded
phase-target: Proof-engine MVP, §1a single-fact slice (compiler-readiness plan v3, 2026-07-12)
comparable-systems-research-status: partial — inline survey with verbatim excerpts from in-tree primary-source mirrors (`research/references/solver-free-static-analysis/`, `research/references/octagon-domain/`) plus the in-tree feasibility study `docs/Working/proof-engine-linear-solver-feasibility-2026-07-12.md`
sources-consulted:
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md: §1.7 (the ruled §1a seams), §1.6 (single-hop rail), §1.8 (⊥-arm/dict-write rails), §2.1 (Holes 2/3), §2.4 (F9 order-sensitivity caveat), §4 (per-slice table)
  - docs/Working/compiler-readiness-plan-2026-07-12.md: §1a slice stub + effort band; ruling log (ledger #3 §1b deferred, spec:256 override NOT authorized); acceptance frame (full-compile-only, ⊥/single-hop/certificate invariant cells); Stage-1 methodology
  - docs/Working/certificate-steps-membership-2026-07-12.md: the settled Slice-0 certificate vocabulary — 11 premise kinds, 21 step kinds; Decision 6 (§1a is the one flagged likely growth); Falsifier 2 (growth accounting); Open question 1 (the do-not-fabricate-step discipline for unruled sub-arms)
  - docs/Working/proof-engine-linear-solver-feasibility-2026-07-12.md: §1 (the §1a fact shape incl. coefficient examples, one-at-a-time discipline), performance measurement (:42), §2 (§1b contrast — what combination means)
  - docs/language/precept-language-spec.md: §0.1 eleven principles (:88-124); §0.6 proof-system responsibilities item 1/2 (:206-207) and the relational-reasoning status paragraph (:256, quoted verbatim below); proof philosophy #1-#7 (:219-231); §0.4 no-search property
  - docs/compiler/proof-engine.md: :8 (status row — §1a "designed, not yet implemented"), §13 (prove-or-reject MVP block :2607-2613, §1b deferral :2615-2617, stale strategy/sample counts :2604/:2621)
  - docs/philosophy.md: prevention / inspectability / determinism commitments (via CLAUDE.md § Product Philosophy and spec §0.1; no philosophy edit proposed)
  - src/Precept/Pipeline/ProofEngine.cs: GuardConstraint :31-39 (incl. IsArg :36-39); FieldToFieldConstraint :45-48; NumericSubjectRef :83-86; ScopedNumericFact :88-93; IsArg consumption sites :708/:793; obligation collection walks rule/ensure conditions under ConstraintContext :236-248; binary-op obligations come only from catalog ProofRequirements :306-307 (the circularity argument's ground)
  - src/Precept/Language/Operations.cs: unary Negate metas :46-56; +/−/× operation metas carry NO ProofRequirements — only the divide/modulo family does (:117-170) — so fact-shaped conditions mint no dischargeable obligations (§ Circularity)
  - src/Precept/Pipeline/ProofEngine.Strategies.cs: TryGuardInPathProof :713-787 (staleness :769-771, OR-branch rule :763); ExtractGuardBranches :794; ExtractGuardLeafConstraints :886-931 (arg leaves :917-931); TryFlowNarrowingProof :1078-1149 (subtraction gate :1091-1094, memberwise-precursor staleness :1101-1107); RuleSourcedFieldToFieldBranches :1159-1168; ExtractFieldToFieldLeaf :1240-1248; GuardRelationImpliesObligation :1250-1275 (truth table :1263-1274)
  - src/Precept/Pipeline/ProofEngine.Intervals.cs: IntervalOfNarrowed :22-145 (transfer dispatch :102-132, conditional union :135-140, typed-constant magnitude normalization :164-168); TryResolveNumericBoundValue :393-416 (declared bounds read NormalizedDeclaredMin/Max :405-406); ApplyStaticUnitScaling :423-452; TryIntervalContainmentProofNarrowed :489-517 (IsEmpty backstop :506-509); BuildNarrowedIntervals :519-657 (guard-branch loop :553-577, gc filter :559, OR union :579-606, sibling exclusions :609-627, relational fold :629-654, ⊥ suppression :646-652); RelationalHalfLine :659-703
  - src/Precept/Pipeline/ProofEngine.Composition.cs: CollectTrustedNumericFacts :243-275 (guarded-rule filter :255-261); TryGetNumericConstraintFact :293-321; TryGetStaticNumericValue :323-343 (unit normalization); CollectUnconditionalRelationalFacts :531-548; RelationContradictsSubjectBounds :550-593
  - src/Precept/Pipeline/ProofLedger.cs: ProofObligation :24-83 (ReassignedBefore :38-55); ProofStrategy enum :100-115 (11 members — drift-ledger input)
  - research/references/solver-free-static-analysis/tvpi-two-variable-per-inequality-simon-king-howe-hosc2010.txt: restricted-inequality-form domain (verbatim excerpt below)
  - research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt: ±x±y≤c restricted form (verbatim excerpt below)
  - samples/loan-application.precept: authoring conventions (multi-factor guards, money rules, transition rows)
  - samples/shopping-cart.precept: authoring conventions (composite `set` arithmetic, reject-row ladders)
  - live precept_compile probes (2026-07-13): BudgetEnvelope guarded-sum variant → today Unresolved, PRE0078, computed [0.0..5500.0]; CapacityPlan multi-term-rule divisor → today Unresolved, PRE0083; both 0 type errors (syntax-valid); both samples now in-doc (§ Audience and Teachability); CapacityPlan's divisor confirmed to compile as the left-assoc tree ((Capacity - Committed) - Pending) — the parse fact grounding the seam-2 whole-site match rule
---

# §1a slice design — single-fact multi-term proof (the linear-term fact + the term-multiset matcher)

**How to read this document.** "§1a" is the readiness plan's label for this slice: teaching the proof engine to use **one authored condition about a sum or difference of several values** — a guard like `when Spent + Commit.Amount <= 5000.0` or a rule like `rule Committed + Pending < Capacity` — as a proof fact. Today the engine only understands conditions about a *single* value (`Total <= 900`) or a *single pair* (`X >= Y`); a condition over a sum contributes nothing, so the very guard the author wrote to keep a total in bounds does not prove the total stays in bounds. Two plain terms used throughout: a **term** is one value named in the sum (a field or an event argument, optionally negated); a **term multiset** is the bag of those terms with their signs, ignoring order and grouping — `Spent + Commit.Amount` and `Commit.Amount + Spent` are the same multiset. **Single-fact** means each authored condition is used *alone*: the engine never combines two conditions to derive a bound neither states (that is "§1b", deferred by owner ruling and not touched here).

This is a design-review-tier pass: the load-bearing seams were already ruled in the locked architecture (`compiler-readiness-plan-2026-07-12-architecture.md` §1.7); this document verifies each ruled seam against engine source at file:line, enumerates the whole input family with explicit dispositions, settles the slice-level decisions the architecture delegated, and **parks** — does not settle — the items that are new public surface or that conflict between two pieces of canon. It was produced under an autonomous overnight contract: nothing here is locked, no canonical doc was edited, and every open question is framed neutrally for the owner.

## Goal

When this slice ships, the two live probes below flip: `BudgetEnvelope`'s guarded `set Spent = Spent + Commit.Amount` compiles **Proven** with computed interval `[0.0 .. 5000.0]` (today: Unresolved, `[0.0 .. 5500.0]`, PRE0078), and `CapacityPlan`'s divisor `Capacity - Committed - Pending` discharges from `rule Committed + Pending < Capacity` (today: Unresolved, PRE0083) — both asserted via full `Compiler.Compile(...)`, with single-hop, staleness, and ⊥ invariant cells all rejecting as they must.

## Scope

### Ruled seams (cited, not re-decided — implementation against locked architecture)

Per the spec-first rule, these are **not** open decisions; the architecture rules them and this design implements them (architecture §1.7, verbatim): *"One new fact record (a linear-term fact: coefficient/term pairs + operator + bound, reusing the existing `NumericSubjectRef`), a new leaf arm in `ExtractGuardLeafConstraints` (`Strategies.cs:886`) recognizing `A + B op literal` (and rule shape `A + B <= C` normalized), and one **syntactic term-multiset matcher** (commutative/associative normalization only — no algebraic rearrangement in the MVP) used at two seams: the interval-containment result (`Intervals.cs:489`, intersect the result interval with the fact's bound when the site *is* the guarded sum) and flow-narrowing (`Strategies.cs:1078/1250`, generalize the two single-leaf reads to term multisets). **Staleness:** a composite-term fact is stale if **any** constituent appears in `ReassignedBefore` (`ProofLedger.cs:51`) — memberwise. **One-hop preserved**; the fact narrows the composite term, not the individual fields (splitting `A+B<=100` into per-field bounds is unsound)."*

All anchors re-verified against committed HEAD 2026-07-13: `ExtractGuardLeafConstraints` is at `Strategies.cs:886`; `TryIntervalContainmentProofNarrowed` at `Intervals.cs:489`; `TryFlowNarrowingProof` / `GuardRelationImpliesObligation` at `Strategies.cs:1078/:1250`; `ReassignedBefore` at `ProofLedger.cs:51` (doc comment :38-55). (Per the plan's stable-vs-rots split, these anchors ground the *design*; the build-time edit plan re-grounds them, since Slice 0 reshapes this substrate first.)

### In scope

- The **linear-term fact** record: signed coefficient/term pairs + comparison operator + constant bound, terms as `NumericSubjectRef` (field or event-arg identity, `ProofEngine.cs:83-86`), plus source-span/producing-construct provenance (the Slice-0 certificate premise contract).
- The **term-multiset normalizer + matcher**: flatten a `+`/`-` expression tree of subject references into a sorted signed multiset; matching is multiset equality. Commutative/associative normalization only.
- **Consumption seam 1 — interval containment**: when the obligation site's multiset equals an in-scope fact's multiset, intersect the *computed result interval* with the fact's licensed half-line before the band check (`Intervals.cs:489-517`).
- **Consumption seam 2 — flow narrowing**: generalize the single-pair relation match (`Strategies.cs:1091-1099`, `:1240-1248`) to signed multisets — facts held in moved-left internal form `(M, ⊕, 0)`, whole-site match with a same-order and a reversed (`negate(M)` ⇒ `InvertOp`) arm — feeding the same finite truth table (`:1263-1274`). One rule, stated in full under Semantic Rules.
- **Fact sources**: row/hook/handler guards (branch-wise, via the existing DNF branch extraction `Strategies.cs:794`) and **unconditional** rules (guard-null filter, `Composition.cs:255-261`; blocked-rule/self-unsatisfiability discipline extended to the multi-term shape).
- **Memberwise staleness**; arg/field term identity discrimination (building on the Slice-0 Hole-2 base fix).
- **Soundness cells**: single-hop preservation, ⊥ discipline at the new seam, OR-branch discipline, determinism/order pinning (the F9 caveat discharged by construction).
- Diagnostics residual text: the `Unresolved` missing-condition names the composite shape; a rephrase hint for near-miss arrangements.
- MCP DTO/hover/doc sync and corpus/`# EXPECT:` reconciliation (every slice inherits these; plan §5).

### Out of scope

- **§1b multi-fact combination** — combining two separately-declared facts to derive a bound neither states alone. Owner-deferred (ledger #3, option C; `spec:256` two-hop override **NOT authorized**). Nothing in this slice reads one fact through another.
- **Algebraic rearrangement in matching** — free-form algebra between differently-arranged authored forms is rejected by the ruled seam ("no algebraic rearrangement in the MVP"). The narrow delegated question of *one* move-all-terms-left canonicalization pass is **parked as Open question 2**, not silently included or excluded.
- **Splitting a composite fact into per-field bounds** — `A + B <= 100` never tightens `A` alone. Unsound (`B` could be negative-capable or the split double-counts freedom); ruled out by §1.7.
- **Non-linear terms** — a term that is itself a product of two subjects (`A * B`), a function call, an accessor read, or a conditional does not mint a fact term. The composite's *result interval* still benefits from those nodes' existing transfers via `IntervalOfNarrowed`; only fact recognition is restricted to linear subject terms.
- Aggregates (`sum`/`min`/`max`/`average`) — separate owner-routed `/design` (ledger #4).
- The independent re-checker (§5b) — deferred (architecture Decision B retracted).

### Deferred to future (with pointers)

- Interior-node application of composite facts (applying `A + B <= K` to the `A + B` *subterm* of a larger site such as `(A + B) / 2`) — see Decision 4's tradeoff; revisit if corpus evidence shows whole-site matching forfeits common idioms.
- Ensure-sourced composite facts (state-/event-anchored ensures minting multi-term facts) — see the family table row; the ruled seam names guards and rules only.
- Literal-coefficient terms and mixed authored forms — parked (Open question 2).

## Philosophy Alignment

Principles are spec §0.1's eleven, by their spec names (`precept-language-spec.md:92-124`).

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | The flagship prevention idiom — a guard written to keep a total inside its cap — becomes provable, so the definition is accepted on proof rather than rejected despite being safe; unprovable composites still reject (prove-or-reject, ledger #1). | N/A | N/A |
| 2. One file, complete rules | Y | Every fact source is an authored guard or rule in the one `.precept` file (proof philosophy #4, spec:227 "All proof facts derive from the `.precept` definition"); no external oracle. | N/A | N/A |
| 3. Deterministic semantics | Y | Normalization is a deterministic sort; matching is multiset equality; multiple matching facts intersect, and intersection is commutative — same definition ⇒ same verdict regardless of declaration order (Decision 4's order pin; discharges the architecture §2.4 F9 caveat). | N/A | N/A |
| 4. Full inspectability | Y | Each composite discharge is a certificate derivation (premises quote the authored guard/rule leaf by span; the match/narrow step renders as one line) — pending the parked step-kind ruling (Open question 1). | The step kind that renders the match is new public vocabulary and is parked, not settled here. | Until Open question 1 is ruled, no Proven verdict may be minted through this path (universal-certificate rule, architecture §1.1) — a hard build-order dependency, accepted. |
| 5. Keyword-anchored readability | N | N/A — no grammar, token, or construct change; guards and rules over sums already parse today (verified: both probes compile with 0 type errors). | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | Y | Money/quantity-typed composite facts reuse the pinned normalization boundary (`TryGetStaticNumericValue`, `Composition.cs:323-343`), so a `'5000.00 USD'` bound participates as its normalized magnitude with authored values preserved for display. | N/A | Price facts with a dynamic denominator mint no fact (conservative decline — same posture as `Intervals.cs:44-52`). |
| 7. Compile-time-first static checking | Y | Adds compile-time proof power over existing syntax; nothing moves to runtime; unprovable stays rejected. | N/A | Syntactic matching forfeits some sufficient-but-rearranged guards (ruled tradeoff, §1.7) — answered with an actionable rephrase hint. |
| 8. Approximation honesty | Y | The licensed half-line intersection is exact interval arithmetic on the engine's actual computed interval; the `Unresolved` residual prints the computed range and the missing composite condition — over-approximation stays visible. | N/A | N/A |
| 9. Mandatory rationale | N | N/A — no constraint surface changes; rules feeding facts already carry `because`. | N/A | N/A |
| 10. Totality | Y | More fault-prone sites (divisors over multi-term differences) become provably safe from authored facts; no new expression form, so no new undefined behavior. | N/A | N/A |
| 11. Static completeness | Y | The new discharge path is prove-or-decline: every cell in the family table either discharges by a stated sound rule or explicitly declines (never silently passes); the ⊥/staleness/single-hop cells are failing-test-first. | A matcher bug that over-matches would be a fail-open; guarded by the exact-multiset-equality contract plus the acceptance criteria's negative cells. | Under-matching (forfeited discharges) accepted over any over-matching. |

**Companion commitments.** *Stateless-first-class*: fact sources are guards and unconditional rules — both exist identically in stateless precepts (event-handler guards, global rules); nothing here requires a state machine. *Domain-expert-primary-author*: the whole slice exists because the domain expert's natural sentence — "committed plus pending must stay under capacity" — is authored as one rule/guard, and the engine honors it as written instead of demanding a decomposition into per-field bounds that may not even be expressible.

## Language Design Grounding

**Scope note**: this slice adds **no** token, keyword, construct, modifier, type, operator, accessor, or expression form — guards and rules over sums already parse and type-check today (both worked probes compile with 0 type errors). What changes is proof *power* over existing surface, plus (parked) one certificate-vocabulary item. A brief field grounding is carried anyway because the *choice of recognizable fact shape* is a language-adjacent commitment:

Restricting the fact shape is the classic move in solver-free static analysis. The Octagon domain restricts to "invariants of the form (±x ± y ≤ c), where x and y are program variables and c is a real constant" (in-tree mirror `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt`, Miné, HOSC 2006 / arXiv:cs/0703084; access 2026-07-13). TVPI restricts to "systems of linear inequalities where each inequality has at most two variables … a sweet-point in the performance-cost tradeoff between the faster Octagon domain and the more expressive domain of general convex polyhedra" (in-tree mirror `research/references/solver-free-static-analysis/tvpi-two-variable-per-inequality-simon-king-howe-hosc2010.txt`, Simon/King/Howe, HOSC 2010; access 2026-07-13). Precept takes the same discipline — a deliberately restricted linear form with a cheap decision procedure — but **diverges on where the form comes from**: octagon/TVPI *infer* relations within a fixed shape; Precept's facts are **author-declared templates** used one at a time, so the shape is whatever the author wrote (any number of ±1 terms), and the "analysis" is a syntactic match plus one interval intersection — no closure, no fixpoint, no widening (spec §0.4). The in-tree feasibility study states the §1a contract directly: *"Each such fact still participates in proof **one at a time**: when the engine needs to discharge an obligation … it may consult a single linear fact plus the fields' declared intervals — never two facts combined"* (`proof-engine-linear-solver-feasibility-2026-07-12.md:22`).

**Precept-specific application**: implements spec §0.6 responsibility item 2 — "**Relational reasoning** over numeric expressions involving multiple fields" (spec:207) — which the spec already mandates; today's implementation covers only the single-pair subset (the spec:256 status paragraph). Must not conflict with: proof philosophy #1 (soundness over completeness, spec:221), the single-hop lock (spec:256 — quoted in full under Open question 3), and §0.4 (no search).

## Audience and Teachability

Included although no new authored syntax ships: the slice changes which definitions compile and what the rejection text says.

**Worked example** (syntax validated live via `precept_compile`, 2026-07-13 — 0 type errors; today the containment obligation is `Unresolved`, computed `[0.0 .. 5500.0]`, PRE0078; under this design it is `Proven`, `[0.0 .. 5000.0]`):

```precept
precept BudgetEnvelope

# A department budget envelope: committed spend accumulates event by event
# and must never exceed the envelope's cap.

field Spent as decimal default 0.0 min 0.0 max 5000.0 maxplaces 2

state Open initial
state Closed terminal

event Commit(Amount as decimal min 0.0 max 500.0 maxplaces 2)
event Close

from Open on Commit when Spent + Commit.Amount <= 5000.0
    -> set Spent = Spent + Commit.Amount
    -> no transition
from Open on Commit
    -> reject "Committing {Commit.Amount} would push spending past the 5,000.00 envelope (already committed: {Spent})"
from Open on Close -> transition Closed
```

The author's guard *is* the proof: the row runs only when `Spent + Commit.Amount <= 5000.0`, and the assigned value is exactly `Spent + Commit.Amount`, so the assignment provably fits `max 5000.0`.

**Second worked example — the rule-sourced divisor flagship** (syntax validated live via `precept_compile`, 2026-07-13 — 0 type errors; today the divisor obligation is `Unresolved`, PRE0083 "Division is unsafe: '((Capacity - Committed) - Pending)' can be zero", plus three PRE0158 no-write-site warnings from the deliberately-minimal sample; under this design the divisor obligation discharges from the rule via the flow seam):

```precept
precept CapacityPlan

# A capacity plan: committed and pending demand must stay under capacity,
# so remaining headroom is provably positive when used as an allocation divisor.

field Capacity as decimal default 100.0 min 0.0 max 1000.0 maxplaces 2
field Committed as decimal default 0.0 min 0.0 max 1000.0 maxplaces 2
field Pending as decimal default 0.0 min 0.0 max 1000.0 maxplaces 2
field LoadFactor as decimal default 0.0 maxplaces 6

rule Committed + Pending < Capacity because "demand must stay under capacity"

state Planning initial
state Balanced terminal

event Rebalance(Demand as decimal min 0.0 max 1000.0 maxplaces 2)

from Planning on Rebalance
    -> set LoadFactor = Rebalance.Demand / (Capacity - Committed - Pending)
    -> transition Balanced
```

The author's rule *is* the proof: `Committed + Pending < Capacity` moved left is `Capacity − Committed − Pending > 0`, which is exactly the divisor — the seam-2 worked trace. (The probe also confirms the parse the seam-2 site gate must handle: the divisor compiles as the left-assoc tree `((Capacity - Committed) - Pending)`, not as a two-sided split matching the rule's sides.)

**Error message.** The plausible misuse: the author guards the sum against the wrong constant (`<= 5200.0`) or forgets the guard. Proposed `Unresolved` residual (today's PRE0078 text is "Numeric computation exceeded the representable range on field 'Spent'"):

> PRE0078: Cannot prove 'Spent' stays within its declared 0.00 to 5000.00 after this set — computed range 0.00 to 5500.00. Missing: a condition keeping Spent + Commit.Amount at or below 5000.00 before the write (for example: `when Spent + Commit.Amount <= 5000.0`).

And the rephrase hint, when a condition over the same values exists in a form the engine does not match (the ruled "actionable rephrase message", §1.7):

> The condition `5000.0 - Spent >= Commit.Amount` names these values but is not in a form the compiler uses as a proof fact. Rephrase it as a bound on the sum: `when Spent + Commit.Amount <= 5000.0`.

Both name the field, the authored bound, the computed range, and the exact authored fix — no engine vocabulary ("multiset", "obligation", "discharge").

**10-minute teaching path.**
1. `docs/philosophy.md` § prevention paragraph (1 min).
2. The multi-term fact section this slice adds to `docs/compiler/proof-engine.md` (§ Doc-update enumeration) — "a guard or rule about a sum proves writes of that sum" with the example above (4 min).
3. `samples/shopping-cart.precept` — composite `set` arithmetic in a real cart (2 min).
4. Hover the guarded `set` line / call `precept_proofs` and read the certificate (2 min).

## Legibility conventions used below

- **Signed term multiset** — the bag of (sign, subject) pairs from flattening a `+`/`-` tree, order and grouping erased. `norm(e)` names the flattening.
- **Half-line** — the one-sided range a comparison licenses: `<= K` licenses `(−∞, K]`, `>= K` licenses `[K, +∞)`, `== K` the point `[K, K]`.
- **⊥ (bottom)** — the empty range; a contradiction. `NumericInterval.Contains(⊥)` returns true, which is why an unsuppressed ⊥ over-proves (plan § Terms).
- **In scope (of an obligation)** — a fact is in scope when its source governs the obligation's row/site: the row's own guard branch, or an unconditional rule.
- **"§1b" / "§2" / "§6"** — readiness-plan labels: multi-*fact* combination (deferred), case-by-case narrowing (separate slice), constant-rules-into-bounds (separate slice).
- **Flow-narrowing seam** — the existing strategy that discharges a requirement on a *difference* (`A − B >= 0`) from a declared relation (`A >= B`); "the truth table" is its finite operator-pair table (`Strategies.cs:1263-1274`).
- **Moved-left form** — the internal representation of a two-sided fact `L ⊕ R` as one signed multiset compared to zero: `(norm(L) ⊎ negate(norm(R)), ⊕, 0)` — the identity `L ⊕ R ⟺ L − R ⊕ 0` the shipped table already embodies. Only subject terms move; constants never cross the comparison.
- **Containment seam** — the existing strategy that computes a result interval for an assignment and checks it fits the target's declared band (`Intervals.cs:489-517`).

## Semantic Rules

### The fact

```
LinearTermFact  ::=  ( Terms: {(c₁,T₁) … (cₙ,Tₙ)},  ⊕,  K,  Provenance )
    cᵢ ∈ {+1, −1}                      (MVP mint; the record carries decimal slots — Decision 1)
    Tᵢ  = NumericSubjectRef            (field | event-arg identity, ProofEngine.cs:83-86)
    ⊕   ∈ {<, <=, >, >=, ==}           (≠ mints no fact — not an interval)
    K   = static decimal               (via TryGetStaticNumericValue, incl. unit normalization)
    n   ≥ 2                            (n = 1 stays on the existing GuardConstraint path)
    Provenance = SourceSpan + producing construct (guard leaf | rule)   (Slice-0 premise contract)
```

### Normalization (the matcher's normal form)

```
norm(e)  =  sorted signed multiset of the subject leaves of a maximal +/− tree
            norm(a + b)  = norm(a) ⊎ norm(b)
            norm(a − b)  = norm(a) ⊎ negate(norm(b))
            norm(−e)     = negate(norm(e))      (unary negation, OperatorKind.Negate —
                                                 the same sign reading binary − already does,
                                                 with no left operand; TypedUnaryOp exists at
                                                 HEAD, Operations.cs:46-56)
            norm(subject-ref) = {(+1, subject)}
            undefined otherwise (any non-subject, non-± leaf ⇒ the tree mints/matches no fact)
```

Sorting key: (subject kind, name, event name), ordinal. Commutative/associative only: no cancellation (`A + B − B` does **not** simplify to `A` — its multiset `{A, B, −B}` simply matches nothing but itself), no distribution, no constant motion across the comparison (parked — Open question 2).

### Recognition (which authored comparisons mint a fact)

```
side₁ ⊕ side₂  mints  (norm(sideₛ), ⊕′, K)   when
    one side is a pure +/− tree of ≥2 subject refs (the subject side, s)
    the other side is a static numeric value K (constant folding + unit normalization,
        Composition.cs:323-343)
    ⊕′ = ⊕ if the subject side is the left side, else InvertOp(⊕)   (the existing
        literal-on-left convention, Strategies.cs:902-907)
```

Sources: (a) guard leaves, branch-wise, inside the existing DNF extraction (`ExtractGuardBranches`, `Strategies.cs:794`) — a multi-term comparison is **one leaf** of its AND/OR branch; (b) **unconditional** rules (`Rules[i].Guard is null`, mirroring `Composition.cs:255-261` — a guarded rule holds only under its guard and must not become a global fact). A rule whose subject-side multiset over the fields' declared intervals provably cannot satisfy the bound (interval-arithmetic emptiness) is self-unsatisfiable and contributes **no** fact (the multi-term extension of `RelationContradictsSubjectBounds`, `Composition.cs:550-593`; the lateral unsatisfiable-rule diagnostics fire on their own surface).

The **field-vs-field rule shape** `A + B <= C` (subject terms on both sides): the flow-narrowing seam consumes it via the two-sided match below; whether it *also* mints a containment-seam fact (bound = one hop into the related side's bare declared interval) is a slice-level, soundness-bearing decision — settled as **Decision 9** (yes, with `K = declared-max(C)` for `<=`/`<`, resp. declared-min for `>=`/`>`, read via the bare declared interval only — the shipped `RelationalHalfLine` discipline, `Intervals.cs:629-654` — never the dict being built, never a rule-folded interval; the §1.6 rail: `ExtractFieldInterval` must not learn to fold constant rules).

### Consumption seam 1 — interval containment

```
fact (M, ⊕, K) in scope of obligation o,  non-stale
norm(site(o)) = M                        (whole-site match, after ResolveSubject)
──────────────────────────────────────────────────────────────
result(o) := result(o) ∩ halfline(⊕, K)   applied AFTER the per-field fold and the
                                          site arithmetic, on the result interval only;
                                          then the unchanged band check runs
if result(o) ∩ halfline(⊕, K) = ∅  ⇒  contribute nothing (fall through, never Proven-via-⊥;
                                       the IsEmpty backstop Intervals.cs:506-509 stands)
```

Strictness closure at the boundary reuses the shipped integer-vs-decimal dispatch (`NarrowByConstraint`'s boundary rule / `RelationalHalfLine`'s integer flooring, `Intervals.cs:659-703`): a strict `<` on a decimal-backed composite closes at `K` (sound superset over-wide by the point {K}); an all-integer composite steps to `K∓1`.

**Magnitude-space alignment (typed-constant bounds).** The intersection `result(o) ∩ halfline(⊕, K)` is legal only if both operands live in the same magnitude space. They do, because every magnitude on both sides of this intersection passes through the one pinned normalization boundary (base-unit space): `K` normalizes at recognition via `TryGetStaticNumericValue` (`TypedConstantNormalizer.NormalizeQuantity/NormalizePrice`, `Composition.cs:330-340`); the result interval's inputs are already normalized on every read path — field declared bounds read `NormalizedDeclaredMin/Max` (`Intervals.cs:405-406`), typed-constant operand magnitudes normalize inside interval extraction (`Intervals.cs:164-168`), and static-unit expressions are placed into normalized space by `ApplyStaticUnitScaling` (`Intervals.cs:423-452`). So a guard bound authored in `lb` against fields declared in `kg` compares kg-base magnitudes on both sides — same-space by construction, and the acceptance matrix carries an explicit cross-unit cell (criterion 17) rather than trusting this argument untested (the architecture §1.5 caution: magnitude-space verification before shipping cross-space comparisons). The one shape where normalization is *not* static — a price bound with a dynamic denominator — mints no fact (the family-table decline, mirroring `Intervals.cs:44-52`).

**OR branches**: the fact participates branch-wise. The seam consumes a composite fact only when **every** DNF branch of the governing guard carries a matching non-stale fact on the same multiset; the licensed region is the **union** of the per-branch half-lines (the weakest bound that holds in every branch) — the same discipline as the existing cross-branch union (`Intervals.cs:579-606`) and the guard-in-path rule "Every OR branch must independently prove" (`Strategies.cs:763`).

**Never into the dict**: composite facts are never written into the per-field narrowed-interval dictionary, so they can never be read back by `IntervalOfNarrowed`, never interact with the dict-write ⊥ rail, and never create the F9 order sensitivity ("multi-term facts that read a partially-narrowed sibling", architecture §2.4) — the fact reads only the finished result interval. When several non-stale facts match the same site multiset, all are intersected; intersection is commutative and associative, so declaration order cannot change the verdict. (Intersecting two facts *about the same multiset* is the existing per-subject fold discipline, not §1b combination — combination means deriving a bound on a term **neither** fact states, and no rule here does that.)

### Consumption seam 2 — flow narrowing (guard-sum matching)

Generalizes the two single-leaf reads (`GetFieldName(binaryOp.Left/Right)`, `Strategies.cs:1097-1098`; `ExtractFieldToFieldLeaf`, `:1240-1248`) to signed multisets, feeding the **unchanged** truth table. The complete rule, stated once (this replaces any per-side matching — the fact is held in one internal form and matched against the whole site):

```
Internal form (moved-left):  every fact this seam consumes is held as (M, ⊕, 0):
    two-sided fact L ⊕ R  (each side a pure +/− tree of subjects)
        ⇒ M = norm(L) ⊎ negate(norm(R))            (always threshold 0)
    constant-bounded fact (M₀, ⊕, K)  participates iff K = 0  ⇒ M = M₀
        (K ≠ 0 constant-bounded facts serve seam 1 only — the finite table is a
         threshold-0 table and gains no rows in this slice)

Site gate:  s = the resolved obligation subject (the same ResolveSubject bridge as today,
    Strategies.cs:1082-1095), accepted when norm(s) is defined with |norm(s)| ≥ 2
    (n = 1 stays on the existing single-pair path; the shipped gate's "subtraction of
     two fields" is exactly the two-term case of this gate — the flagship divisor
     `Capacity - Committed - Pending` is a left-assoc subtraction tree whose norm is
     {+Capacity, −Committed, −Pending}, which the old a−b/two-side split cannot reach)

Match (the multiset generalization of the shipped sameOrder/reversed pair,
    Strategies.cs:1257-1261):
    norm(s) = M           ⇒ effective op = ⊕
    norm(s) = negate(M)   ⇒ effective op = InvertOp(⊕)
──────────────────────────────────────────────────────────────
requirement on s per the finite table (effective op, requirement) — Strategies.cs:1263-1274,
    e.g. (>, >) with threshold 0 ⇒ discharged; unchanged rows, no new table entries
```

**Worked flagship trace**: `rule Committed + Pending < Capacity` is two-sided, so its internal form is `M = {+Committed, +Pending, −Capacity}` with `< 0`. The divisor site `Capacity - Committed - Pending` has `norm(s) = {+Capacity, −Committed, −Pending} = negate(M)`, so the reversed arm fires with effective op `InvertOp(<) = >` — the table row `(>, NotEquals) at threshold 0` discharges the divisor obligation.

**Why the moved-left internal form is inside the ruled ceiling, not new algebra.** The architecture's ceiling is "commutative/associative normalization only — no algebraic rearrangement" (§1.7). Moved-left form is not a rearrangement *search* — it is the single, fixed identity `L ⊕ R ⟺ L − R ⊕ 0` that IS the shipped mechanism's entire semantics, already public in two places: the certificate vocabulary defines the step as "`RelationImplication(X ⊕ Y ⟹ req on X−Y)`" (certificate doc :198), and spec:256's justification reads "`A >= B` guarantees `A − B >= 0`". The shipped reversed-orientation match (`reversed ⇒ InvertOp`, `Strategies.cs:1257-1261`) is likewise this identity read right-to-left. At multiset width the same two facts become: internal form = subject terms moved left (the identity), and the reversed arm = `norm(s) = negate(M)`. **No constant ever crosses the comparison** under this rule — both sides of a two-sided fact are pure subject trees by recognition, so nothing here overlaps Open question 2's Extension A (which is about *recognizing* mixed constant±subject sides, a genuinely parked recognition widening). This within-ceiling reading is an argued judgment of this pass, flagged for owner confirmation at the slice-boundary review (Decision 5 carries the legs).

### Staleness (memberwise) and identity

```
∃ (cᵢ, Tᵢ) ∈ Constituents(fact, shape) :
    Tᵢ.Kind = Field  ∧  Tᵢ.Name ∈ ReassignedBefore(o)   ⇒  fact not usable for o
```

**`Constituents` is pinned per consumption shape** (the quantified set is not "whatever M happens to be"):

- **Flow seam**: the full moved-left multiset — for a two-sided fact `L ⊕ R`, every subject of *both* sides (`norm(L) ⊎ negate(norm(R))`). This is the multiset generalization of the shipped both-operand check (`Strategies.cs:1105-1107` checks left AND right).
- **Containment seam, one-sided fact** (`M ⊕ K`): every subject of `M`.
- **Containment seam, two-sided fact consumed via the related side's declared bound** (fact `A + B <= C` licensing `K = declared-max(C)` on site `{A, B}` — Decision 9): every subject of the *subject side* **and the related side's fields** (`C` included). Including the related side is the conservative choice: it is trivially sound (dropping a fact only forfeits a discharge, never over-proves), and it reads §1.7's "any constituent" at face value — every field the fact names is a constituent. The relaxation (excluding `C` on the snapshot argument — at fact-establishment time `A + B ≤ C_old ≤ declared-max(C)`, and `declared-max(C)` is a static constant unaffected by later writes to `C`, so the derived bound survives a `C` reassignment as long as `A`/`B` are unwritten) is *also* sound but is a strictly-later refinement; falsifier 3 watches whether the coarse grain rejects real idioms.

Any constituent reassigned earlier in the chain kills the whole fact (ruled, §1.7). Arg terms never appear in `ReassignedBefore` (it holds reassigned *fields*, `ProofLedger.cs:38-55`) — an arg is bound once per event, so arg terms are never stale. Term identity is full `NumericSubjectRef` equality (Kind + Name + EventName): a field and an event arg sharing a name are **different terms** and never match each other — inheriting the arg/field discrimination the Slice-0 Hole-2 fix installs at the narrowing site (`GuardConstraint.IsArg` exists at HEAD, `ProofEngine.cs:36-39`; the consult at `Intervals.cs:559` is the Slice-0 fix this slice builds on).

### Circularity (rule-sourced facts consumed without a per-obligation blocked set)

The rule-side collector mirrors `CollectUnconditionalRelationalFacts` (`Composition.cs:531-548`), which runs **without** the per-obligation blocked-rule set — and that code's own circularity argument ("a relation gives the subject a sign from the RELATED field's declared interval, never from the subject's own unproven obligation", `Composition.cs:531-538`) does **not** transfer to a composite fact that licenses a range on the very site under proof. The architecture demands the fold posture's circularity argument be made per-slice, not assumed (§1.6 tradeoff leg: the §6 case "owes its own circularity argument in the slice's adversarial review, not an assumed transfer"). The §1a argument, made directly:

1. **Facts are premises, never lemmas.** A composite fact's truth is established by governance — the unconditional rule is enforced on every configuration (spec §0.1 principle 1 via the runtime contract), and a guard-sourced fact holds because the guard gates the row. No fact's admissibility depends on any obligation's *verdict*; the support graph has edges only from authored premises to obligations, never obligation-to-obligation through a fact.
2. **Self-consumption is structurally impossible for the ruled fact shape.** Rule conditions DO get obligations walked over them (`ProofEngine.cs:236-240`, `WalkExpression` under `ConstraintContext`), so this needed verification, not assertion: obligations on binary ops come only from catalog `ProofRequirements` (`ProofEngine.cs:306-307`), and the `+`/`−` operation metas carry **none** (verified: `Operations.cs` gives `IntegerMinusInteger`, `DecimalTimesDecimal` etc. no `ProofRequirements`; only divide/modulo-family ops carry the divisor obligation, `Operations.cs:117-170`). A fact-shaped condition is *by definition of `norm`* a pure `±` tree of subjects against a static bound (or two such trees) — it contains no divide, no call, no accessor — so **no Numeric or IntervalContainment obligation can arise inside a fact-producing rule's own condition**. The only obligations walked out of a fact-shaped condition are Presence obligations (`ProofEngine.cs:285-301`), which composite facts never discharge.
3. **Mutually-supporting rule pairs are impossible for the same reason.** A rule that mints a composite fact contains no obligation this slice can discharge (point 2), and a rule whose condition carries a dischargeable obligation (e.g. a divide) has `norm` undefined at that node and mints no fact. Fact-producing and obligation-bearing are mutually exclusive shapes, so no cycle `R₁-fact → obligation-in-R₂ → R₂-fact → obligation-in-R₁` can close: at most one direction exists (a fact from R₁ discharging an obligation inside a non-fact-shaped R₂ — the flagship CapacityPlan *is* this benign one-directional shape when the divisor lives in a rule).

Disposition: the not-per-obligation-blocked-set posture is therefore sound for this slice **as long as recognition and `norm` stay coupled** — if a later slice ever lets a fact-shaped condition contain a fault-prone node (e.g. literal-coefficient terms via `*`? no: `*` carries no obligation either — but division-shaped coefficients would), the structural argument must be re-made. Test-shaped: acceptance criterion 20 asserts a fact-producing rule's condition mints no Numeric/IntervalContainment obligations, and the family table carries the two cells explicitly.

### Proof obligations

**None added.** This slice adds discharge power for *existing* obligation kinds (`IntervalContainmentProofRequirement`, `NumericProofRequirement`); no new `ProofRequirement` catalog entry, no new obligation site. The certificate for the new discharge is the parked item (Open question 1); the **premise** vocabulary needs no growth — a multi-term comparison is one guard/rule leaf, quoted whole by the existing `GuardCondition`/`RuleCondition` premise kinds (certificate doc, Decision 1 kinds 5-6).

### Soundness preservation claim

- **Principle 7 (compile-time-first) and 10 (totality)** hold: no new expression form, no new evaluation path; the slice only converts some `Unresolved` verdicts to `Proven` when a sound derivation exists, and every derivation is one interval intersection or one finite-table row over facts the author declared.
- **Principle 11 (static completeness)** holds because every rule above is prove-or-decline: recognition failure, normalization failure, staleness, an absent branch, or an empty intersection each *decline* (leaving the obligation to reject), never pass. The one soundness-critical novelty — matching — is exact multiset equality, which cannot over-apply a fact to a site with different terms; and the licensed half-line is precisely the authored comparison's own range, whose truth on the row is guaranteed by the guard gating the row (or the rule governing every configuration). Splitting into per-field bounds — the known unsound move — is structurally absent (no dict writes).
- **Single-hop (spec:256)** holds: each derivation consults exactly one fact; the only interval read beyond the fact is the site's own computed interval or, for the two-sided rule shape, the related side's *bare declared* interval (the shipped one-hop read, `Intervals.cs:629-632`).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The linear-term fact and the matcher are **pipeline-internal inference records** in `ProofEngine` — the same layer as `GuardConstraint`/`FieldToFieldConstraint`/`ScopedNumericFact` (`ProofEngine.cs:31-93`), which they generalize. They are not catalog metadata: they encode no per-member domain knowledge (no keyword, no operator semantics — operator semantics stay in the Operations catalog; interval transfers stay on `BinaryOperationMeta.IntervalTransfer`, dispatched generically at `Intervals.cs:102-112`). The **one** catalog-layer item is the certificate step kind for the match/narrow inference — spec-owned public vocabulary in the `CertificateSteps` catalog — and that is exactly the parked owner decision (Open question 1), consistent with catalog-before-code: the member is ruled first, then emitted.

**Reuse verification (thin wiring over existing machinery, verified at source).**
- DNF branch extraction: reused as-is (`ExtractGuardBranches`, `Strategies.cs:794`; AND cross-product / OR union `:823-855`). The new leaf arm sits inside `ExtractGuardLeafConstraints` (`:886`) beside the existing field/arg/count arms.
- Static bound evaluation + unit normalization: reused (`TryGetStaticNumericValue`, `Composition.cs:323-343` — "For Quantity/Price typed-constants, normalize to base unit").
- Guard-null and blocked-rule discipline for rule-sourced facts: reused (`CollectTrustedNumericFacts` filters, `Composition.cs:253-261`; `RelationContradictsSubjectBounds`, `:550-593`).
- Result-interval computation: unchanged (`IntervalOfNarrowed` + `ApplyStaticUnitScaling`, `Intervals.cs:499-501`); the seam adds one intersection after it.
- Half-line construction and strictness closure: reused (`RelationalHalfLine` shape + `NarrowByConstraint` boundary dispatch — `Intervals.cs:659-703`, `Satisfiability.cs:256`).
- Truth table: unchanged (`Strategies.cs:1263-1274`); only its operand reads widen (to the moved-left whole-site multiset match, seam-2 rule).
- Staleness carrier: unchanged (`ProofObligation.ReassignedBefore`, `ProofLedger.cs:38-55`); the memberwise loop is the multi-term analogue of the shipped two-field check (`Strategies.cs:1105-1107`).
New work is: the fact record, `norm` (one recursive flatten + sort), the two match-and-consume hooks, rule-side recognition, and the certificate wiring (post-ruling). No fold is duplicated; no parallel interval arithmetic exists.

**Cross-component propagation.**
- Runtime (pipeline): the new fact record + matcher + two seam hooks; the certificate builder emits through the Slice-0 shape once Open question 1 is ruled. No parser, type-checker, or evaluator change.
- Tooling (LS): no new feature; hover renders the new discharges through the Slice-0 certificate rendering (`RichHoverFactory`) automatically once the step kind exists. **None** beyond that.
- MCP: no new tool or DTO shape; `precept_proofs`/`precept_compile` verdicts flow through the Slice-0 `ProofVerdict`/certificate projection. `docs/tooling/mcp.md` touched only if the certificate vocabulary grows (Open question 1 outcome).
- **Breaking changes**: none to public API/diagnostic codes; previously-rejected definitions now compile (a strictly accepting change), and residual/rephrase message text changes ride the normal corpus/`# EXPECT:` reconciliation. If Open question 1 rules a new step kind, that is a certificate-vocabulary addition (additive, but public — the certificate doc's Falsifier 2 accounting).

### External architectural precedent

Comparator: **restricted-form linear domains (Octagon / TVPI)** — the architectural problem is identical: how much linear expressiveness to admit while keeping checking cheap and legible. Octagon fixes the form at two unit-coefficient variables: "invariants of the form (±x ± y ≤ c)" with "graph-based algorithms for all common abstract operators—O(n³) time cost" including "a normal form algorithm to test equivalence" (octagon mirror, cited above). TVPI admits arbitrary coefficients on two variables as "a sweet-point in the performance-cost tradeoff" (TVPI mirror, cited above). **Precept takes**: the restricted-form discipline and the normal-form-for-equivalence idea (our `norm` is the trivial, sortable case). **Precept diverges**: no inferred closure — octagon's O(n³) closure IS multi-fact combination, which the owner deferred as §1b (ledger #3); Precept's facts are author-declared, consumed one at a time, so the per-obligation cost is a multiset comparison over a handful of terms (the feasibility study prices it: "a term-multiset comparison over at most ~6 terms and a handful of exact-decimal arithmetic operations, each costing 2–15 ns", `proof-engine-linear-solver-feasibility-2026-07-12.md:42`). This divergence is the deliberate identity choice — legibility and single-hop over inference power — already locked at spec:256 and re-affirmed in the plan's ruling log.

## Inventory of what will be built

| Artifact | Path | Content |
|---|---|---|
| Fact record | `src/Precept/Pipeline/ProofEngine.cs` (beside `GuardConstraint`/`FieldToFieldConstraint`, :31-93) | `LinearTermFact` — signed coefficient/term pairs (`ImmutableArray<(decimal Coefficient, NumericSubjectRef Subject)>`), `OperatorKind`, decimal bound, provenance (span + producing construct) |
| Normalizer/matcher | `src/Precept/Pipeline/ProofEngine.Intervals.cs` or a small shared partial | `norm` (flatten ± tree → sorted signed multiset; decline on non-subject leaf) + multiset-equality match; one helper shared by both seams |
| Guard-side recognition | `src/Precept/Pipeline/ProofEngine.Strategies.cs` | New leaf arm in `ExtractGuardLeafConstraints` (:886) minting `LinearTermFact` branch-wise; branch sets carry composite facts alongside `GuardConstraint`s (DU/base per CLAUDE.md's DU rule — no nullable-field paper-over) |
| Rule-side recognition | `src/Precept/Pipeline/ProofEngine.Composition.cs` | Multi-term sibling of `CollectUnconditionalRelationalFacts` (:531-548) with the guard-null filter (:255-261) + multi-term self-unsatisfiability extension of `RelationContradictsSubjectBounds` (:550-593) |
| Containment-seam hook | `src/Precept/Pipeline/ProofEngine.Intervals.cs` | Post-arithmetic result-interval intersection in `TryIntervalContainmentProofNarrowed` (:489-517), all-branches + union discipline, ∅ ⇒ decline |
| Flow-seam widening | `src/Precept/Pipeline/ProofEngine.Strategies.cs` | Moved-left internal form + whole-site multiset match (seam-2 rule; generalizes the site gate :1091-1095, the relation reads :1240-1248, and the sameOrder/reversed pair :1257-1261) feeding the unchanged truth table (:1263-1274); memberwise staleness over the full moved-left multiset generalizing :1105-1107 |
| Staleness | same files | Memberwise `ReassignedBefore` check at both seams |
| Certificate wiring | per Open question 1 ruling | Emission of the ruled step kind (or widened payload) through the Slice-0 builder; **blocked until ruled** |
| Diagnostics text | `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` | Residual naming the composite condition; rephrase hint for recognized-terms-unrecognized-form |
| Tests | `test/Precept.Tests/` + `test/integrationtests/diagnostics/*.precept` | Full-compile positive/negative matrix per Acceptance criteria; `# EXPECT:` samples derive from corrected canon after owner sign-off (per the Stage-1 methodology gate — **not** authored in this pass) |
| Docs | per Doc-update enumeration | proof-engine.md section; spec wording (owner-gated, Open question 3); tooling docs if vocabulary grows |

### The family enumeration (every cell, explicit disposition)

Shapes × sources × seams — the whole family, not the tripped-over instances:

| Axis | Cell | Disposition |
|---|---|---|
| Term count | n = 2 | In (flagship). |
| | n ≥ 3 | In — same rules; no arbitrary cap (bounded by authored expression size; no search). |
| Term kind | field + field | In. |
| | field + arg | In (flagship guard); identity by full `NumericSubjectRef`. |
| | arg + arg | In (guard-sourced only; rules cannot name args — rule conditions are field-scoped). |
| | same-named field vs arg | **Never match each other** (soundness cell; Slice-0 Hole-2 base). |
| | non-subject term (product of subjects, function call, accessor, conditional, `.count`) | No fact minted / no match — decline; the site's interval still benefits from existing transfers. `.count` composites deferred (count seam has its own carrier model). |
| Signs | pure sums, mixed `+`/`−` | In (signed multiset). |
| | unary negation (`when -A + B <= K`; a site containing a `−e` term) | In — `norm(−e) = negate(norm(e))`, the one-line purely-syntactic extension (same sign reading as binary `−`); test row (criterion 18). |
| Coefficients | ±1 | In (MVP mint). |
| | literal coefficient (`0.8 * Estimate`) | **Parked** — Open question 2 (record slots ship; minting gated). |
| Operator | `<=`, `<`, `>=`, `>` | In — half-lines with integer/decimal strictness closure. |
| | `==` | In — point license `[K, K]`. |
| | `!=` | Out — a hole, not an interval; no fact (matches the interval model; sign-domain `!=` facts stay on the existing compositional path). |
| Bound side | subject-side left or right | In — `InvertOp` convention (existing, `Strategies.cs:902-907`). |
| | mixed constant+subject on one side (`5000.0 - Spent >= Amount`) | **Parked** — Open question 2 (move-all-terms-left); until ruled: no fact + rephrase hint. |
| Bound type | plain decimal/integer literal | In. |
| | quantity/money typed constant | In via static normalization (`Composition.cs:323-343`). |
| | price with dynamic denominator | No fact — conservative decline (mirrors `Intervals.cs:44-52`). |
| Two-sided | subjects both sides (`A + B <= C`, `A + B >= C + D`) | In at the flow seam (moved-left internal form, seam 2 rule); containment seam consumes via one hop into the related side's bare declared interval (single-hop rail) — **Decision 9**, incl. the multi-term related side reading and summing several declared intervals. |
| Source | row / state-hook / event-handler guard | In, branch-wise. |
| | rule/ensure own `when` guard (`ConstraintContext`) | In for the flow seam (it already reads these, `Strategies.cs:1114-1119`); containment seam follows its existing context set (`Intervals.cs:523-529` has no `ConstraintContext` arm today — parity noted in the drift ledger, disposition: match existing seam behavior, do not widen contexts in this slice). |
| | unconditional rule | In (guard-null filter; self-unsatisfiability + blocked-rule discipline). |
| | guarded rule | No fact (conditional truth — `Composition.cs:255-261` discipline). |
| | unconditional event/state ensure | **Deferred** — the ruled seam names guards and rules; ensure-sourced composite facts get their own cell when/if pulled in (pointer left in proof-engine.md section). |
| | sibling reject-row negation of a composite guard | **Deferred** — negating a multi-term leaf is `ExclusionNarrowing` at multiset width; not in the ruled seam; the sibling path keeps its existing single-leaf behavior (multi-leaf negation already forfeits, the sound forfeit). |
| Guard structure | fact leaf under AND | In (leaf of every branch it conjoins into). |
| | fact leaf under OR | In only branch-wise; discharge requires every branch to carry a match; licensed region = union. |
| | fact under `not (…)` | Out — negated composite leaf mints nothing (mirrors the single-leaf negation restrictions, `Strategies.cs:945-959` handling simple cases only). |
| Site match | site = exact multiset | In (whole-site match after `ResolveSubject`). |
| | site strictly contains the fact's terms (`Total + Amount + Fee` vs fact on `Total + Amount`) | No match; rephrase-hint-eligible. **Deferred**: interior-node application. |
| | site is a permutation/regrouping | In (that is precisely comm/assoc normalization). |
| Staleness | any constituent field in `ReassignedBefore` | Fact dropped (memberwise; per-constituent test rows). |
| | fact used by the same action that assigns the sum (`set Spent = Spent + Amount`) | Fresh — `ReassignedBefore` holds *prior* writes only; the flagship depends on this (test row). |
| | second write in the same chain | Stale for obligations after the first write (test row). |
| ⊥ | licensed ∩ computed = ∅ | Decline, never Proven (backstop `Intervals.cs:506-509` + explicit test). |
| | fact self-unsatisfiable under declared bounds | No fact + lateral diagnostics own the report. |
| Single-hop | fact `A+B<=100`, site `A` alone | Never narrows (no per-field split). |
| | facts `A+B<=100` and `B>=20`, site `A` | Never derives `A<=80` (§1b combination — rejected cell, failing-test-first). |
| | fact `A+B<=C`, rule `C<=100`, site `A+B` | Bound reads `C`'s **declared** interval only; the constant rule on `C` is not folded through (the §1.6 rail; two-hop stays undischarged). |
| Multiple facts | two facts on the same multiset | Both intersect (same-subject fold discipline, order-independent — not combination). |
| Circularity | fact consumed by an obligation arising inside its own producing rule's condition | Structurally impossible — a fact-shaped condition (pure ± tree vs static bound) contains no obligation-minting node; `+`/`−`/`*` carry no catalog `ProofRequirements` (§ Circularity, verified at `Operations.cs`); asserted test-shaped (criterion 20). |
| | mutually-supporting rule pairs (`R₁`'s fact discharges inside `R₂` and vice versa) | Structurally impossible — fact-producing and dischargeable-obligation-bearing are mutually exclusive condition shapes (`norm` undefined at any fault-prone node); one-directional discharge (fact from `R₁` into a non-fact-shaped `R₂`) is the benign flagship shape. |

## Decisions

*Spec-first note applying to all decisions*: grepped `precept-language-spec.md`, `proof-engine.md`, `business-domain-types.md` for multi-term/linear/relational fact shape. The spec **mandates the capability** (item 2, spec:207) and **locks the single-hop rail** (spec:256); the architecture **rules the seams** (§1.7); `proof-engine.md` §13 records §1a as designed-not-built. None of them settles the slice-level shapes below — those are the delegated design space this pass fills. Anything a cited ruling already settles appears under *Ruled seams*, not here.

---

### Decision 1: The fact record is a pipeline-internal `LinearTermFact` with decimal coefficient slots, minting ±1 only in the MVP

**Stakes**: medium.

- **Rationale**: the ruled shape is "coefficient/term pairs + operator + bound" (architecture §1.7), so the record carries decimal coefficient slots from day one — no reshape when coefficient recognition is later ruled in (Open question 2). Minting is restricted to ±1 because the ruled recognition example is the bare sum (`A + B op literal`) and every flagship idiom in the corpus probes is unit-coefficient; widening minting is a pure recognition change, not a record change. Placement beside `GuardConstraint`/`FieldToFieldConstraint` (`ProofEngine.cs:31-93`) because the record is inference bookkeeping, not domain metadata — the catalog owns operator/type knowledge; this record cites subjects and a constant.
- **Tradeoff accepted**: a record field (coefficient) that the MVP never sets to anything but ±1 — mild dead weight, accepted to avoid a breaking record reshape at the first extension.
- **Alternatives considered**: (a) *sign-only record (bool Negated)* — rejected: contradicts the ruled "coefficient/term pairs" shape and forces a reshape for the already-documented coefficient examples (`0.8 * Estimate <= Actual`, feasibility doc :22). (b) *A catalog-level fact type* — rejected: no per-member metadata exists to catalog; it would be a catalog entry with one member (the smell inverse — cataloging non-language-surface internals). (c) *Reusing `ScopedNumericFact` widened with a term list* — rejected: `ScopedNumericFact` is the compositional sign path's carrier with anchor-scope semantics (`ProofEngine.cs:88-93`); overloading it blurs two admissibility models (anchored scope vs branch scope).
- **Precedent**: the existing graded fact records (`GuardConstraint` → `FieldToFieldConstraint` → `ScopedNumericFact`) each added one dimension in place; the feasibility study's §1a definition ("a *linear fact* — a rule whose sides may combine several fields with constant multipliers", `proof-engine-linear-solver-feasibility-2026-07-12.md:22`).
- **Sources consulted for this decision**: architecture §1.7 — "One new fact record (a linear-term fact: coefficient/term pairs + operator + bound, reusing the existing `NumericSubjectRef`)"; `ProofEngine.cs:83-86` — "`private readonly record struct NumericSubjectRef(NumericSubjectKind Kind, string Name, string? EventName = null);`"; feasibility doc :22 (quoted above).

---

### Decision 2: The normal form is a sorted signed multiset; matching is exact multiset equality — no cancellation, no simplification

**Stakes**: medium (soundness-bearing: the matcher is the one place over-matching could fail open).

- **Rationale**: comm/assoc normalization is the ruled ceiling ("commutative/associative normalization only — no algebraic rearrangement in the MVP", §1.7); a sorted signed multiset is its canonical form, and equality on that form is decidable in one linear pass — no search (spec §0.4). Refusing cancellation (`{A, B, −B}` ≠ `{A}`) keeps the matcher purely syntactic: cancellation is algebra (it asserts `B − B = 0`, which is true, but admitting *any* semantic identity opens the door the ruled parenthetical closes), and a declined match costs only a forfeited discharge, never soundness.
- **Tradeoff accepted**: sites/facts that differ by a semantically-trivial identity do not match; the author rephrases (the ruled tradeoff: "syntactic matching forfeits some sufficient guards, with an actionable rephrase message").
- **Alternatives considered**: (a) *normalize-with-cancellation* — rejected: algebra beyond the ruled ceiling and a slippery slope with no principled stopping point short of full linear normalization (which is Open question 2's territory, owner-gated). (b) *tree-isomorphism matching (no flattening)* — rejected: fails on re-association (`(A + B) + C` vs `A + (B + C)`), which the ruled seam explicitly wants matched.
- **Precedent**: normal-form-based equivalence testing in restricted linear domains — the Octagon domain ships "a normal form algorithm to test equivalence of representation" (octagon mirror, excerpt in § Architecture Grounding); associative-commutative matching is the standard term-rewriting discipline for exactly this order/grouping erasure (named as background; the load-bearing citations are the in-tree mirrors).
- **Sources consulted for this decision**: architecture §1.7 (the parenthetical, quoted in Ruled seams); `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt` — "a normal form algorithm to test equivalence of representation"; spec §0.4 via the certificate doc's admissibility framing (spec:225 criterion 3 — hard caps, no unbounded search).

---

### Decision 3: Fact sources are guard branches and unconditional rules, under the existing trust disciplines — nothing new is trusted

**Stakes**: medium.

- **Rationale**: the ruled seam names exactly these two sources (guard leaf arm + "rule shape … normalized", §1.7). Each inherits its source's shipped admissibility discipline verbatim: guards contribute branch-wise (a fact under OR proves only if every branch carries one — `Strategies.cs:763` "Every OR branch must independently prove"); guarded rules contribute nothing (`Composition.cs:255-261` — "A guarded rule holds only under its guard — it is NOT an unconditional fact"); self-unsatisfiable rules contribute nothing (`RelationContradictsSubjectBounds` generalized: the fact's licensed region against the terms' declared intervals is ∅ ⇒ no fact). Ensure-sourced and sibling-reject-negation composite facts are explicitly deferred cells (family table) because the ruled seam does not name them and each carries its own admissibility question (event-scope anchoring; multi-leaf negation).
- **Tradeoff accepted**: ensure-authored composite conditions (`in Funded ensure A + B <= K`) do not yet discharge — a forfeited power the family table records with a pointer, preferable to widening trust without a ruled seam.
- **Alternatives considered**: (a) *include ensures now* — rejected: not in the ruled seam; event-ensure narrowing currently exists only on the guard-in-path strategy (`Strategies.cs:746-758`), not the containment seam, so inclusion would also widen a context asymmetry this slice should not touch (drift ledger, entry 5). (b) *include sibling-reject negations* — rejected: negating a composite leaf is exclusion narrowing at multiset width, a distinct inference with its own ⊥ discipline; the shipped sibling path already takes the sound forfeit on multi-leaf shapes.
- **Precedent**: §6's design takes the same reuse-the-trust-discipline posture for constant rules (architecture §1.6 — reuse `CollectTrustedNumericFacts` parse + blocked-rule discipline).
- **Sources consulted for this decision**: `Composition.cs:255-261` (quoted above); `Strategies.cs:763` (quoted above); `Strategies.cs:746-758` (event-ensure narrowing exists on guard-in-path only); architecture §1.6 ("reusing the parser `CollectTrustedNumericFacts`/`TryGetNumericConstraintFact` … Honor the blocked-rule/self-unsatisfiability discipline").

---

### Decision 4: Containment-seam consumption is a post-arithmetic intersection on the result interval only — never a dict write, whole-site match only

**Stakes**: medium (soundness-bearing; also discharges the F9 order caveat).

- **Rationale**: the ruled seam says "intersect the result interval with the fact's bound when the site *is* the guarded sum" (§1.7). Placing the intersection after the per-field fold and the site arithmetic — and never writing composite facts into the narrowed dict — yields three properties at once: (i) **single-hop by construction** (nothing downstream can read the fact through a field's interval); (ii) **F9 discharged by construction** — the architecture's carried caveat ("the caveat returns the moment the fact shape widens — multi-term facts that read a partially-narrowed sibling", §2.4) cannot arise because the fact never reads any sibling's narrowed interval, only the finished result; multiple matching facts intersect commutatively, so no order is load-bearing; (iii) **the ⊥ rails stay untouched** — no new dict-write site exists for the F7 rail to cover, and an empty intersection declines under the existing `IsEmpty` backstop (`Intervals.cs:506-509`) plus an explicit decline rule.
- **Tradeoff accepted**: whole-site matching only — `(A + B) / 2` does not benefit from a fact on `A + B` in the MVP (interior-node application deferred; family table). Accepted: the flagship idiom is the whole-site write/divisor, and interior application would thread matching through `IntervalOfNarrowed`, a bigger seam with its own review.
- **Alternatives considered**: (a) *narrow the per-field dict from the composite fact* — rejected: that IS the unsound per-field split §1.7 forbids. (b) *apply facts inside `IntervalOfNarrowed` at every node* — rejected for the MVP: more power, but it moves matching into the recursion where the F9 order question becomes real (a node's interval would depend on facts whose terms' intervals are being narrowed in the same walk) and where a bug would be a silent over-prove deep in arithmetic; deferred with a pointer. (c) *pre-arithmetic seeding of a composite pseudo-variable* — rejected: introduces a parallel interval carrier for sums (forked numeric model; reuse rule).
- **Precedent**: the shipped relational fold's discipline — read one hop, intersect, suppress ⊥, never feed the dict being built (`Intervals.cs:629-654`, incl. ":646-652" — "An empty intersection is a proven contradiction … writing ⊥ here would let the containment reader discharge every obligation via Contains(⊥) => true. Suppress").
- **Sources consulted for this decision**: architecture §1.7 (quoted); architecture §2.4 — "the caveat **returns the moment the fact shape widens** (multi-term facts that read a partially-narrowed sibling…): then collection/fold order becomes semantically load-bearing and must be pinned"; `Intervals.cs:646-652` (quoted); `Intervals.cs:506-509` — "An empty result interval is infeasible (⊥), not a vacuously-contained value".

---

### Decision 5: Flow-seam facts are held in moved-left internal form and matched whole-site against the unchanged truth table; only the operand reads widen to multisets

**Stakes**: medium (soundness- and ceiling-bearing: this decision carries the moved-left internal form the seam-2 rule states).

- **Rationale**: the ruled seam is "generalize the two single-leaf reads to term multisets" (§1.7) — the finite table (`Strategies.cs:1263-1274`) already encodes the sound (relation, requirement) pairs at threshold 0 and is not touched. The generalization is stated as **one rule over one internal form** (seam 2's block): a two-sided fact `L ⊕ R` is held as the moved-left signed multiset `(norm(L) ⊎ negate(norm(R)), ⊕, 0)`, and matching compares `norm(site)` against `M` (⇒ `⊕`) or `negate(M)` (⇒ `InvertOp(⊕)`). This preserves every table row's justification verbatim, because the table's soundness argument ("`A >= B ⟺ A − B >= 0`", spec:256) is term-count-independent — and the moved-left form is precisely that identity, not new algebra: it is already the shipped mechanism's public semantics (certificate doc :198 defines the step as "`RelationImplication(X ⊕ Y ⟹ req on X−Y)`"), and the `negate(M)` arm is the multiset generalization of the shipped sameOrder/reversed pair (`Strategies.cs:1257-1261`). A per-side match (`norm(a)=norm(L) ∧ norm(b)=norm(R)` on a site split `a − b`) was considered and **rejected as the stated rule** because it cannot derive the flagship — `Capacity - Committed - Pending` parses left-assoc, so no split of the site tree yields the fact's sides — while the whole-site moved-left match derives it directly (the worked trace in seam 2). No constant crosses the comparison (both sides of a two-sided fact are pure subject trees by recognition), so the ruled "no algebraic rearrangement" ceiling is not breached; the within-ceiling reading is flagged for owner confirmation at the slice-boundary review. Memberwise staleness covers the full moved-left multiset — both sides — generalizing the shipped both-operand check (`:1105-1107`).
- **Tradeoff accepted**: the site gate widens from "a subtraction node over two fields" to "any resolved site whose `norm` is defined with ≥2 terms" — a strictly larger gate whose new admissions are all instances of the same identity; sites that are semantically differences but not `±` trees (`A + (−1 * B)` with a literal coefficient) still decline — rephrase hint.
- **Alternatives considered**: (a) *per-side matching on a site split `a − b`* — rejected as the stated rule: fails the flagship (left-assoc parse; see Rationale) and silently loses the reversed-orientation arm; it would have required a separate prose rescue, i.e. two rules pretending to be one. (b) *new table rows for composite shapes* — rejected: no new (operator, requirement) pairs exist; adding rows would duplicate the table with cosmetic variants. (c) *routing composite divisor facts through the sign-composition path instead* — rejected: the sign path's carrier (`ScopedNumericFact`) is single-subject and anchor-scoped; the flow seam is the shipped home for relation-shaped discharge. (d) *admitting constant-bounded facts with K ≠ 0 to this seam* — rejected for the MVP: the shipped table is a threshold-0 table; generalizing thresholds is new table semantics, and K ≠ 0 facts already serve seam 1.
- **Precedent**: the shipped single-pair mechanism itself (spec:256's live behavior — "A divisor over a *subtraction* of two related fields — `Z / (X − Y)` from `rule X > Y` — is provably non-zero … and discharges"), whose implementation is already a moved-left match at width 2 (gate `Strategies.cs:1091-1095`, orientation pair `:1257-1261`).
- **Sources consulted for this decision**: `Strategies.cs:1263-1274` (table, e.g. "`(OperatorKind.GreaterThan, OperatorKind.GreaterThan) when requirement.Threshold == 0 => true`"); `Strategies.cs:1091-1095` (shipped subtraction gate), `:1257-1261` (sameOrder/reversed), `:1105-1107` (both-operand staleness); certificate doc :198 (`RelationImplication` semantics); spec:256 (quoted verbatim under Open question 3); architecture §1.7 (quoted); live probe (the flagship site compiles as `((Capacity - Committed) - Pending)`, verified 2026-07-13).

---

### Decision 6: Term identity is full `NumericSubjectRef` equality; memberwise staleness kills the whole fact

**Stakes**: medium (soundness-bearing).

- **Rationale**: staleness is ruled memberwise (§1.7: "a composite-term fact is stale if **any** constituent appears in `ReassignedBefore` — memberwise"), with the quantified constituent set pinned per consumption shape in § Staleness (flow seam: the full moved-left multiset, both sides; containment two-sided: subject side plus the related side — conservative inclusion); the sound justification is that the fact's truth was established over the *pre-write* values, and any constituent's reassignment severs that link for the whole conjunction — partial retention would assert a relation over mixed pre/post values nobody declared. Identity must be the full subject ref (Kind + Name + EventName), never the bare name: the arg/field name collision is a live Slice-0 fail-open (architecture Hole 2), and this slice must not reintroduce it at multiset width. Arg terms are never stale (`ReassignedBefore` carries reassigned *fields*, `ProofLedger.cs:38-55`; args bind once per event).
- **Tradeoff accepted**: coarse — a reassignment that provably re-establishes the fact (e.g. `set A = 0` with fact `A + B <= K`, `B <= K` declared) still kills it; the author re-guards. Sequenced fact refresh is future work (falsifier 3 watches this).
- **Alternatives considered**: (a) *per-term staleness (drop only the stale term)* — rejected: unsound (changes the fact's meaning). (b) *re-derive the fact against post-write intervals* — rejected: that is compute-then-check territory, separately parked by the owner (ledger #5).
- **Precedent**: the shipped memberwise precursor for two terms (`Strategies.cs:1105-1107` — "If either operand was reassigned earlier in this chain, that relation is stale and must not discharge"); `ReassignedBefore`'s spec grounding (`ProofLedger.cs:42-44`, quoting spec §0.6 item 7).
- **Sources consulted for this decision**: architecture §1.7 (quoted); `ProofLedger.cs:38-55` (doc comment quoted in part above); `ProofEngine.cs:36-39` — "a constraint over an arg must not be resolved against a same-named field's declared interval"; architecture §2.1 Hole 2.

---

### Decision 7: Diagnostics stay on existing codes; the residual names the composite condition and a rephrase hint covers near-misses

**Stakes**: low.

- **Rationale**: no new diagnostic category exists — the failure modes are the existing containment/divisor residuals (PRE0078/PRE0083 et al.); what changes is the missing-condition text (now able to name a composite guard as the fix) and one additive hint sentence when the definition contains a condition over the same term set in an unrecognized arrangement (detected cheaply: same subject *set*, failed multiset/recognition match). The hint is the ruled "actionable rephrase message" (§1.7 tradeoff leg).
- **Tradeoff accepted**: message-text churn rides the corpus/`# EXPECT:` reconciliation (plan §5); no new code means no new registry row to teach.
- **Sources consulted for this decision**: architecture §1.7 ("Tradeoff: syntactic matching forfeits some sufficient guards, with an actionable rephrase message"); live probe output (PRE0078/PRE0083 texts, 2026-07-13).

---

### Decision 8: No new certificate premise kinds; the step for the match is parked, and no Proven verdict ships through this path until it is ruled

**Stakes**: medium (it binds build order).

- **Rationale**: the Slice-0 certificate vocabulary's premise kinds already quote whole guard/rule leaves by span (`GuardCondition`, `RuleCondition` — certificate doc Decision 1, kinds 5-6), and a multi-term comparison is one leaf — so premises need zero growth, verified against the eleven-kind list. The *step* that records "this site is the guarded sum; intersect with its licensed range" is a genuinely new inference the settled vocabulary flags as the one likely §1a growth and deliberately does **not** pre-settle (certificate doc Decision 6: "§1a multi-term facts widens `ConditionNarrowing`'s subject from a single field to a term multiset and may need one genuinely new inference (the commutative/associative term-match) — this is the **one flagged likely growth**, and it lands via its own slice's catalog-first change"). Under the universal-certificate rule (architecture §1.1) and the settled doc's do-not-fabricate discipline (its Open question 1: a path "must not mint a certificate that names a step kind whose recompute rule they fail"), the slice therefore may not mint `Proven` through the new path until the owner rules the step's shape — Open question 1 below, with neutral options.
- **Tradeoff accepted**: a hard sequencing dependency — the certificate ruling gates the slice's shippable half, even though the engine work is otherwise independent.
- **Alternatives considered**: (a) *emit through an existing kind as-is* (`ConditionNarrowing` with a single-field subject, `DirectMatch`, or — at the flow seam — the unwidened two-field `RelationImplication`) — rejected: records an inference the recompute rule cannot honestly replay; exactly the dishonesty the settled doc parked its own Open question 1 over. The block applies at **both** seams (Open question 1 states both dispositions needed). (b) *ship the slice Unresolved-only until ruled* — viable staging inside the slice, noted in Dependencies (rejection paths carry certificates whose steps stop at the still-legal arithmetic/containment kinds).
- **Precedent**: the settled doc's own PARKED rows (its coverage table marks unrulable sub-arms PARKED rather than emitting a dishonest step).
- **Sources consulted for this decision**: `certificate-steps-membership-2026-07-12.md` — Decision 1 (kinds 5-6 renderings), Decision 6 (quoted above), Falsifier 2 ("if §1a's term-multiset work cannot express its matching inference as a payload widening of `ConditionNarrowing` and needs **two or more** new step kinds — Decision 6's growth-source accounting was wrong"), Open question 1's closing discipline (quoted above); architecture §1.1 ("**Every** verdict carries a certificate").

---

### Decision 9: A two-sided fact also mints a containment-seam fact, with K read one hop from the related side's bare declared interval — including a multi-term related side summing its fields' declared intervals

**Stakes**: medium (soundness-bearing: this is the one place a fact's constant is *derived* rather than authored).

- **Rationale**: the ruled recognition includes the two-sided rule shape ("and rule shape `A + B <= C` normalized", §1.7), and the containment seam is where the flagship *write* idiom lives — `set Total = A + B` under `rule A + B <= C` should prove containment when `declared-max(C)` fits the target band. The bound is `K = declared-max(C)` for `<=`/`<` (resp. declared-min for `>=`/`>`), read via the **bare declared interval only** — exactly the shipped `RelationalHalfLine` discipline (`Intervals.cs:629-654`; the fold "reading Y's bound ONE HOP via the bare non-relational ExtractFieldInterval — never the dict being built", `Intervals.cs:629-632`). For a **multi-term related side** (`A + B >= C + D`), K is the sum of the related fields' declared bounds — a mild extension of the shipped one-field read that preserves single-hop because **declared bounds are premises, not facts**: summing two declared intervals reads authored declarations (`DeclaredBound` premises in certificate terms), not derived facts, so no fact is read through another fact and the anti-transitivity lock (spec:256) is untouched. An unbounded related field on the relevant edge yields no K — the fact declines (the shipped fold's identity-on-unbounded posture).
- **Tradeoff accepted**: the derived K is one hop coarser than the authored relation (it uses `C`'s declared edge, not `C`'s value), so some safe writes stay unproven when `C` is narrowed elsewhere — the price of the single-hop rail; and the multi-term related-side read sums several declared intervals, slightly widening the surface a soundness bug could hide in (answered by dedicated acceptance rows, criterion 19).
- **Alternatives considered**: (a) *flow-seam-only consumption of two-sided facts* — rejected: forfeits the flagship containment idiom (`set Total = A + B` under `rule A + B <= C`) for no soundness gain; the one-hop declared-bound read is already shipped machinery for width 1. (b) *reading the related side via its rule-narrowed interval* — rejected: that is exactly the two-hop chain the §1.6 rail forbids (`ExtractFieldInterval` must not learn to fold constant rules; spec:256 override not authorized). (c) *single-field related sides only (decline `A + B >= C + D`)* — viable and strictly safer, but rejected: interval addition of declared bounds is the same premise-read repeated, the family enumeration discipline demands the cell get a principled disposition rather than an arbitrary width cap, and the decline would be indistinguishable-from-arbitrary to the author (the rephrase hint could not name a principled fix).
- **Precedent**: the shipped one-field `RelationalHalfLine` read (`Intervals.cs:629-654, 659-703`) — the same "relation + related side's declared edge ⇒ half-line for the subject" inference at width 1; interval addition of declared bounds is the existing `AddTransfer` arithmetic (`Operations.cs:1332-1349` family) applied to premise intervals.
- **Sources consulted for this decision**: architecture §1.6 (single-hop rail, quoted in Ruled seams) and §1.7 ("rule shape `A + B <= C` normalized"); `Intervals.cs:629-654` (fold), `:659-703` (`RelationalHalfLine` incl. unbounded-edge identity); spec:256 (anti-transitivity, quoted under Open question 3); certificate doc Decision 1 (`DeclaredBound` premise kind — declared bounds enter certificates as premises).

## Falsifiers

1. If corpus or early-author evidence shows the mixed authored form (`5000.0 - Spent >= Commit.Amount`) is written at least as often as the sum form — i.e., the rephrase hint fires routinely on real definitions — the strict-recognition floor was the wrong default and Open question 2's move-all-terms-left option should be re-put to the owner with that evidence.
2. If the certificate ruling (Open question 1) cannot express the match inference in **one** new-or-widened member — if it needs two or more new step kinds — the settled vocabulary's growth accounting is wrong per its own Falsifier 2, and the closure decision there re-opens (do not accrete members ad hoc).
3. If memberwise staleness makes a recurring real idiom undischargeable (guard → intermediate write → dependent write patterns rejecting despite being safe), the staleness grain is too coarse — revisit with the parked compute-then-check fork (ledger #5), not by weakening memberwise-ness inline.
4. If the corpus compile budget regresses measurably from matching (against the feasibility study's nanosecond-scale prediction, `proof-engine-linear-solver-feasibility-2026-07-12.md:42`), the whole-site match placement needs re-profiling before any interior-node extension is considered.

## Acceptance criteria (test-shaped, design-level — the `# EXPECT:` matrix derives from corrected canon after owner sign-off, per the Stage-1 gate)

All via `Compiler.Compile(...)` / `CompileExpectingError`; never type-checker-only `Check` (plan §5).

1. **Flagship guard**: `BudgetEnvelope` (worked example) compiles with the `Spent` containment obligation `Proven`, computed interval `[0.0 .. 5000.0]`; removing the guard yields `Unresolved` with the residual naming `Spent + Commit.Amount` and the example guard text.
2. **Flagship rule (flow seam, reversed arm)**: `CapacityPlan` (second worked example) compiles with the divisor obligation discharged from `rule Committed + Pending < Capacity` — this exercises the `norm(site) = negate(M)` arm with `InvertOp` (the site is the fact's moved-left negation); deleting the rule re-rejects (PRE0083 family). A companion cell exercises the same-order arm (`norm(site) = M`, e.g. a requirement on `Committed + Pending − Capacity < 0`-shaped site or an equivalent authored orientation).
3. **Permutation/regrouping**: guard `when Commit.Amount + Spent <= 5000.0` and site `(Spent) + (Commit.Amount)` still prove (comm/assoc cell).
4. **No per-field split**: with only `rule A + B <= 100`, a write of `A` alone into `max 80` stays `Unresolved` (fact never narrows a single field).
5. **No combination (§1b stays out)**: `rule A + B <= 100` + `rule B >= 20` does **not** discharge a write of `A` into `max 80`; `rule A + B <= C` + `rule C <= 100` does not bound `A + B` by 100 (bound reads `C`'s declared interval only).
6. **Memberwise staleness**: for each constituent — `set A = …` earlier in the chain kills fact `A + B <= K` for a later site; the flagship same-action write (`set Spent = Spent + Amount`) stays fresh; a second dependent write after the first is stale; **reassigning the related side of a two-sided fact** (`set C = …` before a site consuming fact `A + B <= C` at either seam) kills the fact (the pinned conservative `Constituents` rule — related side included).
7. **Arg/field discrimination**: an event arg named identically to a field never matches the field's term (and vice versa) — no cross-discharge (builds on the Slice-0 Hole-2 fix).
8. **OR-branch discipline**: a composite fact present in only one OR-branch does not discharge; present in both with bounds K₁ < K₂, the licensed bound is K₂ (union).
9. **⊥ cells**: a fact contradicting the operands' declared bounds (`A,B min 10` each, fact `A + B <= 5`) contributes nothing — the obligation stays `Unresolved`/rejected, never `Proven`; no path mints `Proven` via `Contains(⊥)`.
10. **Guarded-rule exclusion**: `rule A + B <= K when Flag` contributes no fact.
11. **Superset mismatch + hint**: site `A + B + C` with fact on `A + B` stays undischarged and the residual carries the rephrase/decomposition hint naming the mismatch.
12. **Non-linear term decline**: `when A * B + C <= K` mints no fact (and no crash); the site's interval math is unchanged.
13. **Strictness closure**: integer-typed composite with `<` steps the bound (`K−1`); decimal composite closes at `K` (sound superset) — one cell each.
14. **Certificate coverage (gated on Open question 1)**: every verdict minted through the new path carries only cataloged premises/steps; the flagship `Proven` certificate premises are the guard leaf (`GuardCondition`, span-cited) and the declared bounds; a recompute spot-check re-derives the intersection.
15. **Determinism/order pin**: permuting rule/guard declaration order with two facts on the same multiset yields identical verdicts and intervals.
16. **Corpus**: all 77 samples green after reconciliation; bench within budget (Definition of Done #4).
17. **Cross-unit / cross-space bounds**: a quantity composite whose guard bound is authored in a non-base unit against fields declared in another unit of the same dimension (e.g. guard `<= '50 lb'`, fields bounded in `kg`) proves/declines on the base-unit magnitudes — same verdict as the equivalent base-unit authoring; a money composite with a typed-constant bound behaves identically; a price bound with a dynamic denominator mints no fact (decline cell, mirroring `Intervals.cs:44-52`).
18. **Unary negation**: `when -A + B <= K` mints the fact `{−A, +B} <= K` and a site `B − A` (or any norm-equal arrangement) discharges; the disposition is `norm(−e) = negate(norm(e))`, one cell per seam.
19. **Two-sided containment consumption (Decision 9)**: `set Total = A + B` under `rule A + B <= C` proves when `declared-max(C)` fits `Total`'s band and stays `Unresolved` when `C` is unbounded above (no K — decline); the multi-term related side (`rule A + B >= C + D`) reads and sums the related fields' declared bounds — one positive and one unbounded-decline cell.
20. **No circular self-support (structural)**: a fact-producing rule's condition mints no Numeric/IntervalContainment obligations (inventory-shaped assertion over the compiled obligation set); a non-fact-shaped rule containing a divisor over the fact's terms (the CapacityPlan shape) is discharged by the *other* rule's fact — the benign one-directional cell.

## Dependencies

- **Upstream (hard)**: Slice 0 — the `ProofVerdict` DU + certificate format (this slice's verdicts and certificates ride that shape); the Hole-2 arg/field discriminator fix and Hole-3 staleness fix (this slice's identity/staleness rules build on the corrected base); the provenance-threading obligation (premises must be span-citable). **The Open question 1 certificate-step ruling** — gates minting `Proven` through the new path (Decision 8); the engine work minus certificate emission can proceed in parallel, but the slice cannot ship its accepting half unruled.
- **Upstream (owner)**: Open questions 2 (recognition breadth) and 3 (spec wording confirmation) — Q2 only bounds *extensions* (the ruled floor builds regardless); Q3 gates the canonical-doc correction step that precedes acceptance-criteria lock (Stage-1 methodology step 3).
- **Downstream**: §2 event-input narrowing composes (arg-term facts gain point/interval narrowing on a corrected base); §6's constant-rule fold shares the trust disciplines; the §5b re-checker (someday) replays whatever step shape Q1 rules; the deferred cells (ensure sources, interior nodes, coefficient minting) each have a stated pointer.

## Doc-update enumeration (proposed — NOT applied in this pass; canonical-doc edits are owner-gated per the Stage-1 methodology)

Per the CLAUDE.md routing table:

- `docs/compiler/proof-engine.md` — **owned doc**. Add the §1a strategy section when built (fact shape, the two seams, memberwise staleness, single-hop preservation, the family table's dispositions); move §1a out of the §13 "designed, not yet implemented" list at ship time; plus the drift corrections below (independent of this slice's build).
- `docs/language/precept-language-spec.md` — the :256 status-paragraph wording widening (Open question 3; quoted there; owner confirms before any edit). No new language surface ⇒ no catalog entry, no type-doc change.
- `docs/compiler/diagnostic-system.md` — only if the residual/rephrase text is classified as a message-shape change worth registry notes (Decision 7 keeps existing codes; expected: no change beyond message examples).
- `docs/language/catalog-system.md` + `docs/tooling/mcp.md` + `docs/tooling/language-server.md` — **conditional on Open question 1**: only if a new/widened `CertificateSteps` member is ruled (then: catalog inventory note + certificate-vocabulary docs, same commit as the member).
- `docs/Working/compiler-readiness-plan-2026-07-12.md` — slice status flip at ship time (working doc, normal lifecycle).
- Corpus/`# EXPECT:` reconciliation — same-commit sample/contract updates for every message or verdict this slice changes (plan §5).

### Drift ledger (canonical docs this slice owns vs committed HEAD, classified)

| # | Doc claim | Code/ground truth | Classification → proposed action |
|---|---|---|---|
| 1 | `proof-engine.md:2604` — "the full ProofEngine body is implemented with all six strategies" | `ProofStrategy` has **11** members (`ProofLedger.cs:100-115`) | **Unintended drift** (stale count) → correct the sentence to the current strategy set (or point at the enum as source of truth). |
| 2 | `proof-engine.md:2621` — "validate five-strategy coverage against all 20 sample files in `samples/`" | 11 strategies; **77** samples (`ls samples/`) | **Unintended drift** (stale counts in a stale validation item) → refresh or retire the item; the MVP plan's own coverage gates supersede it. |
| 3 | `proof-engine.md:9` — Source row lists `ProofEngine.cs`, `ProofLedger.cs` | The engine spans 10 partial files (`ProofEngine.*.cs`) | **Unintended drift** (incomplete source map) → widen the Source row. |
| 4 | `proof-engine.md:8` + §13 — §1a "designed, not yet implemented" | Matches code (no multi-term recognition exists; verified by probe: guard/rule sums contribute nothing) | **Intended-not-yet-built, honestly stated** → leave stated; flip at ship time only. |
| 5 | spec:256 — relational-reasoning status paragraph describes the single-pair shape only (`rule X >= Y … and its siblings`) | Accurate to HEAD today; will under-describe the fact shape once §1a ships. Spec item 2 (:207) already mandates the wider capability | **Intended-not-yet-built wording gap** → proposed widening drafted under Open question 3; owner confirms; no edit in this pass. |
| 6 | Architecture §2.1 Hole 2 — "`GuardConstraint` … keys purely on `gc.Field` with **no arg-vs-field discriminator**" | The *record* now carries `IsArg` (`ProofEngine.cs:36-39`), minted at the arg leaves (`Strategies.cs:919-943`) and consulted at four sites (`ProofEngine.cs:708/:793`, `Satisfiability.cs:124/:310`) — but **not** at the narrowing loop (`Intervals.cs:557-574`), so the hole itself is still live exactly where named | **Working-doc nuance, not canonical drift** → note for the Slice-0 builder: the fix seam is smaller than the doc implies (add the consult; the record is ready). No canonical edit owed. |
| 7 | Containment-seam guard contexts (`Intervals.cs:523-529`) lack the `ConstraintContext` arm the guard-in-path/flow seams have (`Strategies.cs:728-733/:1114-1119`) | Asymmetry in code, not documented anywhere as intentional | **Surfaced observation** (neither doc-drift nor this slice's to widen) → recorded in the family table (rule-own-`when`-guard row); a future slice/owner decides whether the containment seam should read constraint-context guards. |

## Operational dimensions

- **Security**: N/A — no new source-text ingestion; recognition runs over already-typed expressions.
- **Observability**: new discharges surface through the Slice-0 certificate/hover/`precept_proofs` path (premises span-cited; the match step per Open question 1); rejections print the computed interval + the composite missing-condition, so a mis-match is diagnosable by comparing the printed multiset against the authored guard.
- **Evolvability**: no new external-standard dependency; typed-constant bounds reuse the pinned UCUM normalization boundary (spec §0.6 item 5; `Composition.cs:323-343`) — a UCUM table change shifts fact bounds exactly as it shifts the existing single-subject facts, visibly via authored-value rendering.

## Open questions (all parked for the owner — neutral framing; nothing here is settled by this pass)

### Open question 1 — the certificate step for the term-multiset match (the flagged NEW public vocabulary; parked, not settled)

**Canonical area checked**: `docs/Working/certificate-steps-membership-2026-07-12.md` (the settled Slice-0 vocabulary) — its Decision 6 *names this exact item as the one flagged likely growth* and assigns it "to the §1a slice, Falsifier 2 bounds it"; its Decision 4 fixes membership at twenty-one; its Falsifier 2 tolerates **at most one** §1a growth. `docs/language/catalog-system.md` / spec:225 mandate the catalog but do not enumerate members. No canonical doc settles the shape; growing or widening the membership is a public-vocabulary change the owner rules.

**Both seams need a certificate disposition — a ruling that covers only one leaves the other blocked.** Two distinct inferences arise: (i) the **containment seam's** term-match + intersection (*"the obligation site's terms are exactly the fact's terms after order/grouping erasure, so the site's computed range is intersected with the fact's licensed range"*); (ii) the **flow seam's** multiset discharge — Decision 5 widens `RelationImplication`'s operand reads, but the settled Slice-0 recompute rule for `RelationImplication` is fixed at two-field sides ("`RelationImplication(X ⊕ Y ⟹ req on X−Y)`", certificate doc :198). A multiset discharge emitted through the *unwidened* kind would fail its own recompute rule — exactly the dishonesty Decision 8 forbids — so a ruling that unblocks only the containment seam leaves flow-seam `Proven` verdicts blocked. Options, neutral, each stating what **both** seams emit:

- **(a) Payload widenings of both existing kinds, no new member** — `ConditionNarrowing`'s subject becomes "a field **or** a signed term multiset" (containment; recompute rule gains the normalization-equality premise-check), and `RelationImplication`'s sides become multisets (flow; the finite table is unchanged). Cost: **two** public recompute-rule changes; both members' currently-crisp contracts soften, and the multiset-equality check is arguably its own inference smuggled into each.
- **(b) One new step kind serving both seams** — a term-match/composite member with one bounded recompute rule (flatten, sort, compare; then intersect for containment, or replay the unchanged finite table for flow), with `ConditionNarrowing` and `RelationImplication` left untouched at their single-subject/two-field contracts. Grows membership to twenty-two — exactly the single growth Falsifier 2 budgets. Cost: public-vocabulary growth, a new renderer template, and one member whose recompute rule has two conclusion modes.
- **(c) Mixed per-seam** — one new kind for the containment match plus the `RelationImplication` payload widening for the flow seam (or the mirror-image pairing). Cost: both a new member *and* a widened contract, and the match inference is recorded in two shapes.

**Falsifier-2 accounting note (for the ruling, so it does not trip retroactively)**: the settled doc's Decision 6 growth accounting names only the `ConditionNarrowing` payload widening plus at most one new kind — a `RelationImplication` payload widening appears nowhere in it. Under (a) or (c) the owner should state explicitly whether that widening counts against Falsifier 2's budget (it is a public recompute-rule change either way); under (b) the accounting is exactly the budgeted single growth.

Until ruled: the engine work may land, but **no `Proven` verdict is minted through the new path at either seam** (Decision 8; universal-certificate rule). A ruling of (b) or (c) also implies the conditional doc updates (catalog inventory, mcp.md, language-server.md).

### Open question 2 — recognition breadth: move-all-terms-left canonicalization and literal-coefficient terms (parked; internal canon tension surfaced, not resolved)

**Canonical area checked**: architecture §1.7 — which contains **both** of these, in tension:

> "one **syntactic term-multiset matcher** (commutative/associative normalization only — no algebraic rearrangement in the MVP)"

> "Alternatives rejected: algebraic rearrangement (`Cap - Total >= Amount` matching `Total + Amount <= Cap`) — illegible 'the engine does algebra sometimes'; deferred, with a rephrase hint. (**Synthesis note: one move-all-terms-left normalization pass is small and both reuse and legibility lenses would support it — left to the slice.**)"

The plan stub repeats the first ("commutative/associative normalization only, no algebra"). This design builds the conservative floor either way (pure ± subject-chains vs a static constant; Decision 2's matcher is identical under both outcomes) and parks the two extensions:

- **Extension A — move-all-terms-left** (recognize `5000.0 - Spent >= Commit.Amount` by one canonicalization pass moving subjects left / constants right with sign flips). *Boundary note*: this is distinct from the flow seam's moved-left **internal form** (seam 2 / Decision 5), which moves only *subject* terms of an already-recognized two-sided fact and never crosses a constant over the comparison; Extension A is a *recognition* widening for mixed constant±subject sides, and it alone is what stays parked here. *For*: the synthesis note's own case (small, supported by both lenses); the rephrase hint becomes unnecessary for the commonest near-miss; the certificate can render the canonicalized fact beside the authored quote, answering the legibility objection. *Against*: it is rearrangement across the comparison — the very thing the locked parenthetical excludes; "the engine does algebra sometimes" is the named illegibility risk; the rephrase hint is the already-ruled honest alternative.
- **Extension B — literal-coefficient terms** (`0.8 * Estimate + Fee <= Cap` minting `{(0.8, Estimate), (1, Fee)} <= Cap`). *For*: the ruled record shape carries coefficient slots; the in-tree feasibility study's §1a definition includes "constant multipliers" with the example "`0.8 * Estimate <= Actual`" (`proof-engine-linear-solver-feasibility-2026-07-12.md:22`); matching stays syntactic (coefficient equality). *Against*: §1.7's ruled recognition example is the bare sum (`A + B op literal`); coefficient terms widen the authored-surface proof-power without a ruled seam naming them; decimal-equality of coefficients introduces a representation question (is `0.80` the same coefficient as `0.8`?) that deserves its own look.

Either extension is a pure recognition widening over an unchanged record/matcher — adoptable later without reshape. Owner rules in/out per extension; "defer both, revisit on falsifier 1 evidence" is an expected outcome.

### Open question 3 — spec fact-shape wording widening at spec:256 (surface for owner confirmation; no depth-bound override needed)

**Canonical area checked**: `precept-language-spec.md:256`, quoted verbatim (the load-bearing sentences):

> "**Relational reasoning (item 2) and divisor safety (item 3) — rule-sourced relations live.** A declared **unconditional** field-to-field rule (`rule X >= Y because "…"` and its `>`/`<=`/`<` siblings) now participates in proof exactly as a same-row `when X >= Y` guard does … The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4. This single-hop bound is deliberately retained: the engine never combines two separately-declared facts to derive a bound neither states alone (multi-hop reasoning). An author who needs a combined bound declares its consequence as one rule, which then participates in proof directly (the relational-reasoning mechanism this paragraph describes, item 2) — the honest, legible fix, in place of the engine chasing a chain."

**The read this design proposes for confirmation** (not applied): §1a needs a **fact-shape wording widening only**, no override of the locked depth bound —

1. The capability target already exists: item 2 (spec:207) mandates "Relational reasoning over numeric expressions **involving multiple fields**"; spec:256 is the *status* paragraph describing the implemented single-pair subset.
2. The single-hop lock is untouched: a multi-term fact is **one** declared fact; "never combines two separately-declared facts" remains fully in force (the §1b deferral, ledger #3, stands; this design's acceptance criteria 4-5 test exactly that).
3. The paragraph's own escape hatch *presupposes* this slice: "An author who needs a combined bound **declares its consequence as one rule, which then participates in proof directly**" — a declared consequence over several values (`rule Committed + Pending < Capacity`) is precisely the multi-term single fact; today it does *not* participate (verified by live probe), so the sentence's promise is only redeemed by §1a.

Proposed wording direction (owner edits/approves at the canonical-correction step): extend the paragraph's fact-shape description from "field-to-field rule (`rule X >= Y`…)" to the linear single-fact shape (sums/differences of fields and event args against a static bound, and the two-sided form), keeping the single-pass/depth-bounded/anti-transitivity sentences verbatim. If the owner reads spec:256's lock as *shape*-restricting (single-pair only) rather than *depth*-restricting, this becomes a Tier-3 conversation instead — surfaced here precisely so that call is the owner's.

---

## Status note (why Externally-Grounded, not Locked)

Every ruled seam was verified against committed HEAD at file:line; every slice-level decision carries stakes-appropriate legs; the three open questions are genuinely open (one new public vocabulary item — now explicitly covering **both** seams' certificate dispositions, one internal canon tension, one canonical wording confirmation) and are the owner's to rule at the slice-boundary review. Produced by a single autonomous design pass, then revised in place after adversarial review (both 2026-07-13, same contract): the revision consolidated the flow-seam rule into one moved-left-form statement with a worked flagship trace, added the circularity argument for rule-sourced facts (verified against the Operations catalog and obligation walk), pinned the staleness constituent set per consumption shape, promoted the two-sided containment consumption to Decision 9, argued magnitude-space alignment at file:line, added the unary-negation cell, the second flagship sample, and acceptance criteria 17-20. **One argued judgment is expressly flagged for owner confirmation**: the moved-left internal form is read as *inside* the §1.7 "no algebraic rearrangement" ceiling (the argument in seam 2 / Decision 5 — it is the shipped table's own identity at multiset width, with no constant crossing the comparison); if the owner reads the ceiling otherwise, the flow seam's whole-site match must be re-put as an open question. No canonical doc edited, no code changed, nothing committed — the doc must not be treated as settled until the owner reviews the open questions, the flagged ceiling reading, and the drift-ledger proposals. Per the Stage-1 methodology, the canonical-doc corrections (drift entries 1-3, Open question 3) precede acceptance-criteria lock, and the `# EXPECT:` failing-test matrix derives only from the corrected canon after owner sign-off.
