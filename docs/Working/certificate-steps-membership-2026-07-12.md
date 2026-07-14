---
status: Externally-Grounded
phase-target: Proof-engine MVP Slice 0 (compiler-readiness plan v3, 2026-07-12)
comparable-systems-research-status: strong — cites `research/architecture/compiler/witness-checking-soundness-architecture-survey.md` (Stage-1, primary-source mirrors in `research/references/witness-checking/`)
sources-consulted:
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md: locked proof-engine architecture — §1.1 verdict DU, §1.2 certificate structure, §1.3 format-only, §1.4 witness grid, §1.9 two-arm fix, §3 Decision C1
  - docs/Working/compiler-readiness-plan-2026-07-12.md: Slice 0 steps, exit criteria, doc obligations
  - docs/language/precept-language-spec.md: §0.1 eleven principles; §0.4 execution-model properties; :225 (proof philosophy #3 — the CertificateSteps catalog mandate)
  - docs/compiler/proof-engine.md: :2422-2432 (admissibility-by-certificate decision), :2611-2612 (verdict DU + format-only summary)
  - docs/language/catalog-system.md: catalog pattern (four parts), meta-shape rule, naming convention, inventory statement
  - docs/philosophy.md: prevention/inspectability/approximation-honesty commitments; authoring audience
  - CLAUDE.md: catalog-before-code, derive-don't-duplicate, DU-over-nullable-fields rules (Decision 5 leg)
  - src/Precept/Pipeline/ProofEngine.cs: TryDischarge cascade :1054-1135; IncorporateForwardingFacts :1141-1198; ApplyTrustedRuleFacts call site :153; UnsatisfiableInitialState lateral scan :179-193; SeedCountInterval OR-branch union :695-734
  - src/Precept/Pipeline/ProofEngine.Strategies.cs: TryLiteralProof :10-37; TryNumericDefaultBoundProof :48-81; TryDeclarationAttributeProof :85-228 (FunctionReturnSatisfies :163-168/:230-243; FixedReturnAccessor.ReturnNonnegative arm :176-184; guaranteed-presence fallback :218-225); index-bounds type-derived lower bound (IsTypeDerivedNonnegative) :439/:514-532; TryCollectionGrowthProof :706-711; TryGuardInPathProof :713-787 (stale-fact/reassignment guard :728-733); ExtractGuardBranches :794; TryFlowNarrowingProof :1078-1149 (flow-narrowing arm :1114-1119); GuardRelationImpliesObligation :1250-1275; WalkForContains key-presence walk :1295-1332
  - src/Precept/Pipeline/ProofEngine.Intervals.cs: IntervalOfNarrowed :22-145 (unary/function transfer dispatch :114-132); ApplyStaticUnitScaling :423-452 (price-space inversion :445-446; precision trim :454-462); TryIntervalContainmentProofNarrowed :489-517; BuildNarrowedIntervals :519-657; RelationalHalfLine :670-703 (integer flooring); NegateConstraintToInterval :819-840
  - src/Precept/Pipeline/ProofEngine.Composition.cs: TryCompositionalConstraintProof :13-69; ApplyTrustedRuleFacts :76-112; ResolveNumericSubjectSignSet :478-529; TryRelationalSignForField :601-665; TypedConditional sign-set union :438-439; SignSetSatisfiesRequirement/TryMapComparisonToSignSet :765-804; ResolveNumericSubjectModifiers (type-implied modifier fold — `AddRange(field.ImpliedModifiers)`) :667-689
  - src/Precept/Language/Types.cs: TypeMeta.ImpliedModifiers — exchangerate ⇒ positive :674; timezone/currency/unitofmeasure/dimension ⇒ notempty :516/:587/:626/:642
  - src/Precept/Pipeline/TypeChecker.cs: implied-modifier population from the Types catalog ("Implied modifiers from the Types catalog (D3: catalog-driven, no inline logic)") :689-693
  - src/Precept/Pipeline/ProofEngine.QualifierNarrowing.cs: TemporalUnitComponentsSubset (TemporalUnit subset discharge) :54-77
  - src/Precept/Pipeline/ProofEngine.Qualifiers.cs: symbolic same-source equality :175-187; multi-hop qualifier derivation (axis fallbacks, transitive ResultQualifier policies) :269-394
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs: NarrowByConstraint integer/decimal boundary dispatch :256-279; lateral structural scans (PRE0082/0153/0154/0155) :27-28, PRE0159 :331-333
  - src/Precept/Pipeline/ProofEngine.Lengths.cs: TryLengthContainmentProof :25; TryCountContainmentProof :333; interpolated-string length widening (InterpolatedStringLengthInterval/HoleLengthInterval/DecimalDigitWidth) :138-208; string-function length caps (FunctionKind switch — catalog-placement gap) :250-291
  - src/Precept/Pipeline/ProofEngine.Diagnostics.cs: interval/count message rendering :117-168
  - src/Precept/Pipeline/ProofLedger.cs: ProofObligation :24-83; ProofDisposition :94-98; ProofStrategy :100-115
  - src/Precept/Language/ProofRequirement.cs: AuthoredMin/Max :184-197; count endpoints :227-235; KeyPresenceProofRequirement (no key payload) :243-247; ProofRequirementMeta DU-as-identity pattern :276-380
  - docs/Working/Archive/relational-rules-and-bounds-design-2026-06-02.md: Locked 2026-06-02, promoted to proof-engine.md/spec §0.6 — single-pass 0-or-1-hop relational reasoning
  - docs/Working/compiler-readiness-plan-2026-07-12.md: single-hop/anti-transitivity note :66-67; decision ledger #3 (§1b two-hop override NOT authorized)
  - docs/Working/compiler-readiness-plan-2026-07-12-architecture.md: §1.6 (relational half-line reads Y's bare declared interval only); §2.2 (systematic fail-open sweep); §4 (per-slice certificate-step table)
  - research/architecture/compiler/witness-checking-soundness-architecture-survey.md: certificate-granularity findings (DRAT vs Alethe poles), C1 recommendation and falsifier
  - research/references/witness-checking/drat-format-drat-trim-wetzler-heule-hunt-2016.txt: DRAT design goals + polynomial-time step checking (verbatim excerpts below)
  - research/references/witness-checking/alethe-smt-proof-format-barbosa-blanchette-fleury-fontaine-arxiv2107.02354.txt: coarse-step pressure-on-checkers excerpt (verbatim below)
  - samples/loan-application.precept: real authoring conventions (fields, guards, ensures, transition rows)
  - samples/invoice-line-item.precept: stateless/computed-field conventions
  - live precept_compile probes (2026-07-12): guarded ExpenseAccount → Proved, computedInterval [0..1000]; unguarded variant → Unresolved, PRE0078, computedInterval [0..1100]
---

# CertificateSteps catalog membership + the certificate format (§5a design pass, Slice 0)

**How to read this document.** A *certificate* is the show-your-work note attached to every proof verdict: a short derivation the author can read in hover and the `precept_proofs` tool, and that a future independent checker could replay. A certificate has **premises** (quotes of things the author actually declared — a bound, a guard, a rule — each cited by source line) and **steps** (each applying exactly one primitive inference, and carrying its own conclusion so it can be recomputed from the things it cites). "§5a" is the project's label for *the certificate format work*; "§5b" is the label for the *independent re-checker*, which is deferred and not part of this design. This document settles the one thing the locked architecture left open: **which premise kinds and step kinds the closed vocabulary actually contains**, plus two consequences (whether the vocabulary grows per-slice, and how fine-grained the arithmetic step is).

## Goal

When Slice 0 lands, every proof verdict the engine mints carries a certificate whose every premise is one of the eleven premise kinds and whose every step is one of the twenty-one `CertificateStepKind` members defined here — demonstrated by the corpus compiling with zero uncataloged certificate content and the worked example below rendering as shown.

## Scope

- **In scope**: the closed membership of the `CertificateSteps` catalog (step kinds); the closed premise vocabulary (`CertificatePremise`); the certificate record shape those two populate; the coverage mapping from every *existing* discharge path to premises + steps; the growth policy for later MVP slices; arithmetic-step granularity.
- **Out of scope (already locked — cited, not re-decided)**: the `ProofVerdict` three-case discriminated union (architecture §1.1, owner-ruled 2026-07-12); the certificate *structure* — premises quoting author declarations by span, steps carrying recomputable conclusions from cited children, built during discharge as bookkeeping (architecture §1.2); **format only — no live re-checker, no checker-gated mint** (architecture §1.3, Decision B retracted); the vocabulary's *source* being a new spec-enumerated catalog rather than the `ProofStrategy` enum (architecture §3, Decision C1, owner-ruled); the witness validation gate and 12-family witness grid (architecture §1.4 — the witness is a separate `ProvenViolating` payload, not certificate content); the Presence/KeyPresence configuration-witness cell stays at its deferred-post-MVP default — this design adds **no** configuration-witness machinery.
- **Deferred to future**: the §5b independent re-checker (consumes this format; deliberately not designed here); any octagon-style relational-step extensions from the locked relational-rules design (that design's steps land with its own slice, through this catalog's normal growth path — see revised Decision 6); Presence/KeyPresence configuration witnesses.
- **Explicit exclusion**: the lateral structural scans — unsatisfiable/tautological guard, vacuous/contradictory/unsatisfiable rule, unsatisfiable initial state (PRE0082/0153/0154/0155/0159 and kin, `ProofEngine.Satisfiability.cs`; `ProofEngine.cs:179-193`) — emit diagnostics from whole-definition scans, not obligation verdicts. Under architecture §1.1 ("verdict = obligation outcome"), they carry no certificates, so Option A's "every verdict carries a certificate" is not misread as covering them.

## Philosophy Alignment

| Principle | Affected? (Y/N) | How served (1 sentence + cite) | Tension (1 sentence or N/A) | Tradeoff (1 sentence or N/A) |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Certificates document *why* a prevention verdict holds; the format-only decision (architecture §1.3) means they never gate minting, so prevention strength is unchanged. | N/A | N/A |
| 2. One file, complete rules | Y | Every premise kind quotes a declaration from the single `.precept` file (or the state graph derived from it). Spec-owned catalog facts (function-return guarantees, accessor guarantees, operation transfers) enter only through step *payloads* citing the catalog member — the catalog IS the language spec in machine-readable form (`catalog-system.md`), not an ambient/external fact source; no premise or step admits anything outside the authored file plus the language spec (Decision 3). | N/A | N/A |
| 3. Determinism | Y | Certificate construction is a deterministic fold over the discharge's own values; same definition ⇒ same certificate. | N/A | The discharge's internal fact records carry no source spans today, so premise construction requires widening them with provenance at collection time — a bounded, deterministic addition priced in Architecture Grounding § Reuse verification (provenance obligation). |
| 4. Full inspectability | Y | This is the principle the whole vocabulary serves: "proven ranges, source attribution, and what the engine could not prove must all be surfaceable" (spec §0.1 #4); every step renders to one author-readable line. | N/A | N/A |
| 5. Keyword-anchored readability | N | N/A — certificates are compiler *output*, not authored syntax; no grammar change. | N/A | N/A |
| 6. Explicit domain meaning | Y | The `UnitNormalization` step makes unit rescaling a visible derivation line instead of silent arithmetic; premises show authored values (`AuthoredMin/Max`), not normalized internals. | N/A | N/A |
| 7. Compile-time-first static checking | Y | Unchanged in force; certificates record the compile-time reasoning without adding or removing any check. | N/A | Certificates are built for proven obligations too — allocation cost the boolean cascade didn't pay (accepted in architecture §1.2). |
| 8. Approximation honesty | Y | Steps carry the engine's *actual* abstract values (intervals, sign sets) — over-approximation is visible in the certificate rather than hidden behind "proved"; `Unresolved` certificates state what was established and what is missing. The `SignResolution` meet is recorded as the engine actually computed it — an empty meet never mints a step (the engine falls back to `Unknown` instead), so the certificate never claims a sign narrower than what was proven. | N/A | N/A |
| 9. Mandatory rationale (`because`) | N | N/A — no constraint surface changes; premises quote rules/ensures including their existing rationale context. | N/A | N/A |
| 10. Static semantic checking | Y | The catalog's closed vocabulary is itself statically enforced (CS8509 exhaustive `GetMeta`, catalog DU analyzers PRECEPT0025/0026) — an uncataloged step kind cannot compile. | N/A | N/A |
| 11. Static completeness | Y | Neutral-to-positive: the format makes a future §5b replay possible; it introduces no new expression form and mints no verdict, so no new fault path exists. | The vocabulary must not *pretend* soundness the MVP lacks: with no live checker, a wrong certificate ships uncaught until §5b (accepted in architecture §1.3). | Accepted per architecture §1.3 — Slice-0 engine fixes, not replay, carry MVP soundness. |

**Companion commitments.** *Stateless-first-class*: every premise/step kind works identically for stateless precepts (the only state-dependent kinds — `ReachabilityFact`, `RejectRowGuard`, `PriorAction` — simply never arise there; nothing requires a state machine). *Domain-expert-primary-author*: the per-kind rendering templates are written in the author's vocabulary (field names, authored values, line numbers), and the Audience and Teachability section shows a domain-plausible certificate end-to-end.

## Language Design Grounding

*Scope note*: this vocabulary is **compiler-output surface** (hover, `precept_proofs`), not authored syntax — no token, keyword, construct, or expression form changes. It is still spec-owned public vocabulary, so the grounding is carried anyway.

**General field.** Machine-checkable proof formats converge on one architecture: a closed set of primitive inference rules, each step recomputable in bounded time from what it cites. The SAT world's DRAT format states the design goals directly: "1) It should be easy to emit clausal proofs …; 2) proofs should be compact …; 3) proof validation should be efficient; and 4) all techniques used in state-of-the-art SAT solvers should be expressible in the format… Each clause addition step should preserve satisfiability, which should be computable in polynomial time." (`research/references/witness-checking/drat-format-drat-trim-wetzler-heule-hunt-2016.txt`, mirror of Wetzler/Heule/Hunt 2016, arXiv:1610.06229; access 2026-06-06.) The SMT world's Alethe format documents the failure mode of the opposite pole — coarse steps: "the Farkas' coefficient of the linear arithmetic rule are provided as arguments. This puts pressure on proof checkers and reconstruction in proof assistants to support all the variants or at least the most general one (at the cost of efficiency)" and its performance checker "will not be structured around a small, trusted kernel" (`research/references/witness-checking/alethe-smt-proof-format-barbosa-blanchette-fleury-fontaine-arxiv2107.02354.txt`, mirror of Barbosa/Blanchette/Fleury/Fontaine, arXiv:2107.02354; access 2026-06-06). The in-tree Stage-1 survey (`research/architecture/compiler/witness-checking-soundness-architecture-survey.md`) already distilled the principle: *"a witness that merely names the strategy ('proved by IntervalContainment') is coarse-grained and useless for checking; a witness that carries the narrowed intervals and the containment arithmetic is fine-grained and cheaply re-checkable."*

**What Precept takes**: the fine-grained (DRAT-pole) discipline — every step one primitive inference, conclusion recomputable in bounded time, no step that requires search to verify. **What Precept diverges from**: DRAT/Alethe steps are logician-facing (clauses, pivots); Precept's steps must *also* render as one plain-English line for a domain expert, so the kinds are named after domain-legible inferences (a guard narrowing a range, a sum of ranges, a fits-check) rather than logic-calculus rules — legibility is a co-equal admissibility criterion per the spec: "legible and independently re-checkable — it emits a certificate drawn from a small, spec-enumerated vocabulary (the `CertificateSteps` catalog) that an independent checker can replay and the author can read" (`precept-language-spec.md:225`).

**Precept-specific application.** This design implements spec `:225` proof philosophy #3 (which *names* the `CertificateSteps` catalog but does not enumerate it) and `proof-engine.md` Decision 1 (`:2422-2432`, admissibility by certificate) — checked for prior settlement; neither enumerates members. It must not conflict with spec §0.4 (no fixpoints, no search): every step's recompute rule below is a single bounded computation.

## Audience and Teachability

**Worked example.** A domain-plausible expense account (syntax validated live via `precept_compile`, 2026-07-12 — compiles clean, obligation `Proved`, computed interval `[0 .. 1000]`):

```precept
precept ExpenseAccount

field Total as decimal default 0 min 0 max 1000

state Open initial
state Closed terminal

event AddExpense(Amount as decimal min 0 max 100)
event Close

from Open on AddExpense when Total <= 900
    -> set Total = Total + AddExpense.Amount
    -> no transition
from Open on Close -> transition Closed
```

The certificate this design produces for the `set Total = Total + AddExpense.Amount` bound obligation *(proposed rendering; today the engine stores only `Strategy: IntervalContainment` and the computed interval)*:

```
Proven — Total stays within its declared 0 to 1000.

Premises
  P1  Total declares min 0, max 1000               (line 3)
  P2  Amount declares min 0, max 100               (line 8)
  P3  this row runs only when Total <= 900         (line 11)

Steps
  S1  ConditionNarrowing   Total: [0..1000] restricted by P3 → [0..900]
  S2  IntervalArithmetic   [0..900] + [0..100] = [0..1000]     (decimal +, from P1-narrowed and P2)
  S3  BandContainment      [0..1000] fits Total's declared [0..1000] ✓   (P1)
```

Each line is recomputable by hand: S1 is an intersection, S2 is endpoint addition, S3 is two comparisons.

**Error message.** The plausible misuse: the author forgets the guard. Validated live: the unguarded variant compiles to `Unresolved`, computed interval `[0 .. 1100]`, emitting PRE0078 today. Under this design the `Unresolved` verdict's certificate renders the honest gap *(proposed; today's PRE0078 text is "Numeric computation exceeded the representable range on field 'Total'")*:

```
PRE0078: Cannot prove 'Total' stays within its declared 0 to 1000 after this set —
computed range 0 to 1100. Missing: a condition keeping Total at or below 900 before
the add (for example: when Total <= 900).
```

This serves the domain expert because it names the field, the authored bound, the computed range, and the exact authored fix — no compiler vocabulary ("interval", "obligation", "discharge") appears in the message.

**10-minute teaching path.**
1. `docs/philosophy.md` § What makes it different — "Full inspectability" paragraph (1 min).
2. The certificate section this design adds to `docs/compiler/proof-engine.md` (§ Doc-update enumeration) — the premises/steps model with the example above (4 min).
3. `samples/loan-application.precept` — see guards and bounds that produce these certificates (3 min).
4. Hover any `set` line in the VS Code extension / call `precept_proofs` (2 min).

**Reviewer note**: the worked example is a real authored scenario (expense cap), not a compiler-test fragment, and was compiled live.

## Legibility conventions used below

- *Fold* = the loop that intersects known facts into a field's narrowed range (architecture's term, defined there).
- *⊥ (bottom)* = the empty range — a contradiction, "no value can satisfy this".
- *Sign set* = the engine's coarse value classification into negative / zero / positive combinations (`NumericSignSet`, `ProofEngine.cs:64-75`); "nonzero" is the set {negative, positive}, which is **not** an interval (it has a hole at 0).
- *Measure* = which quantity a range describes: a field's **value**, a string's **length**, or a collection's **count**.
- *Octagon-style* = constraints of the shape "one field ± another field ≤ a constant" — reasoning that relates PAIRS of fields, named after the octagon abstract domain. Precept's deferred two-hop/relational-closure extensions (§1b) are octagon-style; the current single-hop relational reasoning is not (it reads one field's own bare declared interval, never a field-pair sum/difference bound).
- *DNF union* = rewriting a guard into a list of OR-alternatives, each an AND-list of simple conditions — "disjunctive normal form" — then combining results across the OR-alternatives by union (the weakest bound that holds in every alternative).
- "§1a / §2 / §3 / §6" = the later MVP capability slices (multi-term guard facts / case-by-case narrowing / money arithmetic / constant-rules-into-bounds), per the readiness plan.

## Semantic Rules

**Certificate well-formedness (the format).** A certificate is a pair (premises, derivation):

```
Certificate  ::=  (Premises: P₁ … Pₙ,  Steps: S₁ … Sₘ,  Conclusion)
Premise Pᵢ   ::=  one CertificatePremise case, carrying SourceSpan + the extracted fact
Step Sⱼ      ::=  one CertificateStepKind member, citing children ⊆ {P₁…Pₙ} ∪ {S₁…Sⱼ₋₁},
                  carrying its own Conclusion (an abstract value or a decided fact)
```

Steps form a tree/DAG over earlier steps and premises (the locked "recomputable conclusion from cited children", architecture §1.3). The obligation's *site expression* (the typed expression under proof, already on `ProofObligation.Site`) is shared context: literal leaves of the site need no premise — they are read from the site itself.

**Observation (not new scope)**: today the string-function length caps dispatch on a `FunctionKind` switch in engine code (`ProofEngine.Lengths.cs:250-291`) rather than a `LengthTransfer` attached to `FunctionOverload` the way `IntervalTransfer` is. This is a catalog-placement gap worth closing in a future slice — a per-function switch in pipeline code is the pattern CLAUDE.md's catalog rules forbid — but it does not block Slice 0: the `IntervalArithmetic` step's recompute rule for this arm is stated normatively in the CertificateSteps catalog meta itself, so the certificate is honest about the transfer even while the engine's own dispatch has not yet moved to the catalog.

**Recompute rules (one per step kind — the checker contract).** Each rule is a single bounded computation, no search (spec §0.4):

```
IntervalArithmetic(op, measure, [a,b], [c,d]) ⇓ transfer_op([a,b],[c,d])     — cites the owning catalog
                                                 member's transfer: a binary op (BinaryOperationMeta.
                                                 IntervalTransfer), a unary op (UnaryOperationMeta.
                                                 IntervalTransfer), or a function overload (FunctionOverload.
                                                 IntervalTransfer); point intervals for leaves. Length-measure
                                                 compositions with no catalog-member transfer (interpolated-
                                                 string sum-of-segments with numeric-hole digit-width
                                                 widening; string-function caps) have their transfer rules
                                                 stated normatively in the CertificateSteps catalog meta itself
UnitNormalization(unit, [a,b])               ⇓ affine(scale, offset)([a,b])  — UCUM static factors; for Price,
                                                 the affine step is the inversion Scale(1/scale) (price-space
                                                 rescale); every result is trimmed to 24-digit banker's-rounded
                                                 precision (MidpointRounding.ToEven) as part of the same
                                                 normative rule, not a separate step
ConditionNarrowing(cond, [a,b])              ⇓ [a,b] ∩ range(cond)           — half-line/point of one
                                                 condition; integer-vs-decimal boundary dispatch: an integer
                                                 subject narrows by an exact half-step (V±1), a decimal-backed
                                                 subject (Number/Money/Quantity/Price/…) closes at V — a sound
                                                 superset over-wide by the single point {V}
ExclusionNarrowing(cond, [a,b])              ⇓ [a,b] ∩ range(¬cond)          — single-leaf cond only; same
                                                 integer/decimal boundary dispatch as ConditionNarrowing
RelationalNarrowing(X op Y, declared(Y))     ⇓ subject ∩ halfline(op, declared(Y))
                                                 — declared(Y) is Y's BARE declared interval (single hop); for
                                                 a strict operator on an integer subject, the half-line floors/
                                                 ceils to the next integer (ylo+1 / yhi−1)
CaseSplit(arms with assumptions)             ⇓ ⋃ᵢ arm-conclusionᵢ  (interval union  |  sign-set union)  |
                                                 ∧ᵢ armᵢ-establishes-P  (all-arms-establish)
ActionEffect(action, count-interval)         ⇓ interval advanced by the action's catalog effect delta
Comparison(value-or-range-or-sign-set, ⊕, threshold) ⇓ holds / violates / undecidable — the sign-set operand
                                                 form uses the finite table (SignSetSatisfiesRequirement)
BandContainment(interval, [lo,hi])           ⇓ fits (lo ≤ min ∧ max ≤ hi) | outside (max < lo ∨ min > hi)
                                                 | overlaps — the three-way containment conclusion
ConstraintImplication(A ⊕₁ c₁ ⟹ A ⊕₂ c₂)     ⇓ finite implication table (GuardSubsumes semantics)
RelationImplication(X ⊕ Y ⟹ req on X−Y)      ⇓ finite table (Strategies.cs:1263-1274 truth table)
SignArithmetic(op, s₁, s₂)                   ⇓ sign-set transfer of the cited operation
SignResolution(cited sign facts)             ⇓ finite intersection (meet) of the cited DeclaredBound,
                                                 RuleCondition/EnsureCondition, RelationDerivedSign, and
                                                 CatalogFact (type-implied modifier — the third CatalogFact
                                                 arm) facts for one bare field/arg subject; legal only when
                                                 the meet ≠ ∅
                                                 (the engine returns Unknown on an empty meet, so an
                                                 empty-meet step is never minted)
RelationDerivedSign(X op Y, declared(Y))     ⇓ sign per the finite relation-to-sign table, conditioned on the
                                                 related field's declared bound's decidable sign; withheld
                                                 (no step) when the relation contradicts the subject's own
                                                 declared bounds (half-line ∩ subject declared interval = ∅)
CatalogFact(catalog member)                  ⇓ bounded catalog lookup — one of three arms, each read
                                                 directly from the catalog: (i) the cited Functions overload
                                                 (ReturnNonnegative / IntervalTransfer); (ii) FixedReturnAccessor.
                                                 ReturnNonnegative; (iii) the cited Types-catalog member's
                                                 TypeMeta.ImpliedModifiers entry — a type-implied modifier
                                                 (exchangerate ⇒ positive, Types.cs:674; timezone / currency /
                                                 unitofmeasure / dimension ⇒ notempty, Types.cs:516/587/626/642)
DirectMatch(premise-fact, required-fact)     ⇓ syntactic identity after normalization (single premise) — the
                                                 matched fact and the required fact must be the SAME fact,
                                                 including any key/identity component the requirement carries
QualifierAgreement(q₁, q₂, mode)             ⇓ comparison of q₁ and q₂ under the recorded mode: Equality
                                                 (ordinal string equality) | TemporalUnitSubset (source basis
                                                 components ⊆ target basis components) | SymbolicSameSource
                                                 (same source-field path)
QualifierResolution(ordered hop list)        ⇓ replay the cited hops in order — each hop one bounded policy
                                                 application (declared-on-arg/constant, axis fallback,
                                                 compound-price projection, or the cited operation's same-
                                                 qualifier/inherited/cancellation/conversion policy) — the
                                                 final hop's output is the conclusion
DimensionComposition(d₁, d₂)                 ⇓ product dimension ∈ curated DimensionCatalog
AssignmentCoverage(write-sites, fact)        ⇓ finite ∧ over the enumerated write sites, with two stated
                                                 normative sub-rules: (a) per-site "carries the fact" is
                                                 itself a Modifiers-catalog implication — the site's source
                                                 modifier's ProofSatisfactions covering the requirement
                                                 (SatisfactionCovers, Composition.cs:49-62) — so each site
                                                 cites its Modifiers member (or carries a per-site
                                                 ConstraintImplication/CatalogFact child); (b) the checker
                                                 re-derives the write-site enumeration from the definition —
                                                 a bounded whole-definition scan for assignments to the field
                                                 — and the step's conclusion is void if any assignment is
                                                 not an interpolated typed constant (the engine's own
                                                 conservative contract, Composition.cs:71-74)
UnreachableRow(reachability-fact)            ⇓ row's from-state ∉ reachable set of the cited StateGraph
```

**Verdict linkage (bookkeeping, not minting).** The discharge cascade mints the verdict (locked §1.3); the certificate records the derivation the cascade actually performed. `Proven` ⇐ a derivation concluding the requirement holds; `ProvenViolating` ⇐ a `BandContainment`/`Comparison` step concluding *outside/violates* (witness attached separately per §1.4); `Unresolved` ⇐ the partial derivation plus the printed missing condition (free text, not a step).

**Soundness preservation claim.** Principles 7/10/11 are preserved because this design adds **no expression form, no typing rule, and no verdict authority**: certificates are constructed from values the folds already hold (`BuildNarrowedIntervals` products, `IntervalOfNarrowed` results, `ComputedInterval` — `ProofLedger.cs:31`), and a certificate-construction bug cannot flip a verdict (format-only, architecture §1.3). Principle 4 (inspectability) is strengthened; Principle 8 (approximation honesty) is strengthened because steps expose the abstract values as computed, over-approximation included.

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The step vocabulary is **catalog metadata** (`src/Precept/Language/`) — it is spec-owned language surface per spec `:225`, and Decision C1 rules the catalog as its source. The certificate *builder* is pipeline code in `ProofEngine` — thin wiring that records what each discharge path already computes; per-kind knowledge (description, rendering template, recompute-rule identity) lives in `CertificateStepMeta`, not in builder switches. No behavior is placed in pipeline code that the catalog could own.

**Reuse verification (thin wiring, not a parallel definition).** The builder consumes values verified present at discharge:
- authored bound displays: `AuthoredMin`/`AuthoredMax` (`ProofRequirement.cs:184-197` — "raw authored values as written by the user … used exclusively for diagnostic display");
- count endpoints: `CountLower`/`CountUpper` (`ProofRequirement.cs:227-235`);
- computed result intervals: `TryIntervalContainmentProofNarrowed`'s `computedInterval` out-param (`ProofEngine.Intervals.cs:489-502`), already persisted as `ProofObligation.ComputedInterval` (`ProofLedger.cs:31`);
- narrowing inputs: guard branches (`ExtractGuardBranches`, `Strategies.cs:794`), per-branch `NarrowByConstraint` applications (`Intervals.cs:571-572`), sibling-reject intersections (`Intervals.cs:616-627`), relational half-lines (`Intervals.cs:633-654`);
- per-node arithmetic: the `IntervalOfNarrowed` recursion (`Intervals.cs:102-133`) — the builder threads a step-recording callback (or returns (interval, step) pairs) through this walk rather than re-walking the expression;
- unit rescale: `ApplyStaticUnitScaling` (`Intervals.cs:423-452`);
- graph facts: `ReachabilityFact` consumption (`ProofEngine.cs:1149-1161`).
No fold logic is duplicated. Two kinds of new work exist, and both are priced here — the earlier draft's claim that "the only new computation is the recording itself" was an overclaim (adversarial review, 2026-07-13):

1. **The recording itself** — threading step construction through the walks above.
2. **Provenance threading (stated build obligation).** The discharge's internal fact records hold **no source spans and no source identity** today: `GuardConstraint` is (field, op, value) only (`ProofEngine.cs:31-39`), `FieldToFieldConstraint` is a bare field pair (`:45-48`), `ScopedNumericFact` carries only `AnchorState`/`AnchorEvent` (`:88-93`), the sibling-reject exclusion dictionary is field→interval only (`BuildSiblingRejectExclusions`, `Intervals.cs:711-720`), and the prior-action tracking sets (`ReassignedBefore`/`CountEstablishedBefore`/`CountInvalidatedBefore`) are bare field names (`ProofLedger.cs:34-82`). Premises are required to carry `SourceSpan + the extracted fact` (§ Semantic Rules), so the `GuardCondition`, `RuleCondition`, `EnsureCondition`, `RejectRowGuard`, and `PriorAction` premises **cannot be span-cited from the values held today**. Slice 0 therefore widens these records with span + producing-construct identity (which guard leaf / rule / ensure / sibling reject-row / prior action produced each fact) **at collection time** — a mechanical field addition at each extraction site, not new inference. A certificate must never carry a fabricated or defaulted span; acceptance criterion 12 tests this.

**Cross-component propagation.**
- Runtime (pipeline): `ProofLedger`/`ProofObligation` reshape — the certificate rides the `ProofVerdict` DU cases (Slice-0 step 1); the `ProofStrategy` enum is subsumed for explanation purposes (its dispatch role inside `TryDischarge` remains an implementation detail; step 6 of the plan already retires `DiagnosticStage` in the same slice).
- Tooling (LS): `RichHoverFactory` renders premises + steps (plan Slice 0 step 7).
- MCP: `CompileToolDtos` gains the certificate projection; `precept_proofs` output documents the closed step vocabulary from `CertificateSteps.All` (never a parallel list); `docs/tooling/mcp.md` updated same commit.

**Breaking changes.** Yes — the ledger surface reshape, already budgeted and owner-ruled in architecture §1.1 (Decision A tradeoff: "a breaking reshape of the ledger surface — every consumer (MCP DTOs, hover) + ~3,600 test fixtures migrate in one slice"). This design adds member *names* to that public surface (see Falsifiers).

### External architectural precedent

Comparator: **DRAT/LRAT (SAT proof certificates)** — the fine-grained pole. "Each clause addition step should preserve satisfiability, which should be computable in polynomial time" (DRAT mirror, cited in full under Language Design Grounding). Precept takes: one-primitive-per-step with bounded recompute. Precept diverges: DRAT has essentially *one* step kind (clause addition under a checkable redundancy property) because SAT is one logic; Precept's obligations span six abstract domains (value intervals, lengths, counts, sign sets, qualifiers, graph reachability), so the closed set is wider (21) but each member keeps the DRAT property. Counter-pole guarded against: **Alethe's** coarse steps ("puts pressure on proof checkers … at the cost of efficiency", mirror cited above) — no Precept step kind is allowed to mean "by strategy X". Internal precedent: the **ProofRequirements** catalog (`ProofRequirement.cs:276-380`) — the same DU-as-identity, closed, spec-enumerated pattern this catalog mirrors.

## Inventory of what will be built

Catalog (per `catalog-system.md` § Pattern Definition, four parts + naming convention):

| Artifact | Path | Content |
|---|---|---|
| Kind enum | `src/Precept/Language/CertificateStepKind.cs` | 21 members (Decision 4) |
| Meta record | `src/Precept/Language/CertificateStepKind.cs` (or sibling) | `CertificateStepMeta` — DU-as-identity (mirrors `ProofRequirementMeta`): Kind, Description, RenderTemplate, RecomputeRule summary |
| Catalog class | `src/Precept/Language/CertificateSteps.cs` | `GetMeta` exhaustive switch (CS8509), `All` |
| Instance DU | `src/Precept/Language/CertificateStep.cs` | abstract `CertificateStep` + 21 sealed subtypes carrying per-kind payloads (child refs, abstract values, conclusion) |
| Premise DU (supporting type) | `src/Precept/Language/CertificatePremise.cs` | abstract `CertificatePremise` + 11 sealed subtypes (Decision 1); documented under catalog-system.md § Supporting Types |
| Output record | `src/Precept/Pipeline/ProofCertificate.cs` | `ProofCertificate(ImmutableArray<CertificatePremise>, ImmutableArray<CertificateStep>, …)` — carried by every `ProofVerdict` case |
| Abstract-value DU | `src/Precept/Language/CertificateValue.cs` (supporting type) | Interval / Point / SignSet / QualifierValue / DimensionVector, each tagged with Measure where applicable |
| Builder wiring | `src/Precept/Pipeline/ProofEngine.*.cs` | step-recording threaded through the discharge paths listed in the coverage table, including the post-cascade second pass (`ApplyTrustedRuleFacts`, `Composition.cs:76-112`) and qualifier-resolution hop recording. Reuse anchor: the sign fold's values are already computed at `Composition.cs:478-529` — the builder records them, it does not recompute them. |
| Catalog inventory listing | `docs/language/catalog-system.md` | add **CertificateSteps** to the language-definition group (count prose "fifteen catalogs" updates; the inventory doc is the source of truth) |
| Tests | `test/Precept.Tests/CertificateStepsCatalogTests.cs`, `ProofCertificateTests.cs` | catalog integrity (meta completeness, DU/enum 1:1); per-discharge-path certificate coverage (every path emits only cataloged kinds; every kind has ≥1 live emitter — no dead vocabulary); recompute spot-checks per kind |

**Coverage table (the whole-family enumeration — every existing discharge path → vocabulary used).** This is the completeness argument for Decisions 1 and 3; "every verdict carries a certificate" (architecture §1.1) forces coverage of *all* current paths, not only interval containment:

| Discharge path (source) | Premises | Steps |
|---|---|---|
| Literal proof (`Strategies.cs:10-37`) | — (site literal) | Comparison |
| Default-vs-bound — point arm (`Strategies.cs:55-61`) | DeclaredDefault, DeclaredBound | (UnitNormalization) + Comparison |
| Default-vs-bound — interval-magnitude arm (interpolated default, `Strategies.cs:63-81`) | DeclaredDefault, DeclaredBound (the interpolated slot's bound sources) | IntervalArithmetic (the `IntervalOf` recursion over the default, `:64`), (UnitNormalization) + Comparison (interval-edge form, `:71-79`) |
| Declaration attribute — dimension arm, exact match (`Strategies.cs:96-99`) | DeclaredQualifier (or GuardCondition) | DirectMatch |
| Declaration attribute — dimension arm, `Any`-subsumes (`Strategies.cs:99` — `PeriodDimension.Any` satisfies any required dimension) | DeclaredQualifier | **PARKED — Open question 1**: subsumption is not identity; a `DirectMatch` step must not be recorded for this sub-arm as-is |
| Declaration attribute — modifier arm, declared-on-field (`Strategies.cs:137-140`) | DeclaredModifier | DirectMatch |
| Declaration attribute — modifier arm, projection/lift sub-arms (`Strategies.cs:121-126` choice-metadata projection through an accessor/conditional result; `:128-135` sibling lift via the operation's same-set typing rule; `:146-150` element-type projection) | DeclaredModifier (of the sibling / owning declaration) | **PARKED — Open question 1**: each sub-arm involves a non-identity inference (a projection hop or a typing-rule lift) that `DirectMatch`'s same-fact recompute rule cannot honestly record |
| Declaration attribute — numeric/presence arm (`Strategies.cs:155-228`) | DeclaredBound, DeclaredPresence | CatalogFact + Comparison (function-return / fixed-accessor arms, `:163-184`); ConstraintImplication (mincount ⇒ count>0, `:186-202`); ConstraintImplication over the modifier-satisfaction walk (`:204-215`) — where the walk concatenates **type-implied** modifiers (`attributeField.Modifiers.Concat(attributeField.ImpliedModifiers)`, `:205`), the modifier fact is a CatalogFact (Types member, third arm), not a DeclaredBound premise; DirectMatch (guaranteed-presence fallback, `:218-225`) |
| Collection growth (`Strategies.cs:706-711`) | PriorAction | ActionEffect + Comparison |
| Guard-in-path, numeric (`Strategies.cs:713-787`) | GuardCondition, EnsureCondition, RuleCondition (guard source includes rule/ensure `when` guards via `ConstraintContext`, `:728-733`) | ConstraintImplication; CaseSplit(all-arms) over OR branches |
| Guard-in-path, presence (`Strategies.cs:777-781`) | GuardCondition | DirectMatch (+ CaseSplit) |
| Flow narrowing (`Strategies.cs:1078-1149`) | GuardCondition or RuleCondition (rule/ensure `when` guards included, `:1114-1119`) | RelationImplication (+ CaseSplit) |
| Qualifier compatibility / chain / guard-narrowing / assignment-qualifier (`ProofEngine.Qualifiers.cs`, `.QualifierNarrowing.cs`) | DeclaredQualifier, GuardCondition | QualifierResolution (+ per-side resolution of each operand's qualifier), QualifierAgreement (with recorded mode — Equality / TemporalUnitSubset / SymbolicSameSource), CaseSplit (for all-branch narrowing) |
| Dimensional product (`Strategies` dispatch → catalog) | DeclaredQualifier ×2 | DimensionComposition |
| Compositional constraint — sign path (`Composition.cs:13-25`, `:76-112`, `:478-529`, `:601-665`) — includes the post-cascade second pass `ApplyTrustedRuleFacts` (`:76-112`, called at `ProofEngine.cs:153`), which also builds certificates | DeclaredBound, RuleCondition, EnsureCondition | SignArithmetic (per op node), RelationDerivedSign, SignResolution (the meet across cited sign facts), CatalogFact (function/accessor sign guarantees; **type-implied modifiers** folded into the sign meet — `ResolveNumericSubjectModifiers` does `AddRange(field.ImpliedModifiers)`, `Composition.cs:674`, so e.g. a divisor of type `exchangerate` discharges nonzero with zero authored evidence via a CatalogFact citing `Types.cs:674`), CaseSplit (sign-set union for conditional sites, `:438-439`), Comparison (sign-set form, `SignSetSatisfiesRequirement`, `:765-779`) |
| Compositional constraint — interpolated-writes path (`Composition.cs:28-68`) | DeclaredBound (per source) | AssignmentCoverage — per the widened recompute rule: each write site cites its Modifiers member's ProofSatisfactions implication (`SatisfactionCovers` loop, `Composition.cs:49-62`), and the all-writes enumeration is a checker re-scan stated normatively (`:71-74`) |
| Interval containment (`Intervals.cs:489-517`, `:519-657`) | DeclaredBound, GuardCondition, EnsureCondition, RuleCondition, RejectRowGuard | ConditionNarrowing, ExclusionNarrowing, RelationalNarrowing, CaseSplit, IntervalArithmetic, UnitNormalization, BandContainment |
| Length containment (`Lengths.cs:25`) | DeclaredBound | IntervalArithmetic (measure=length), CaseSplit, BandContainment |
| Count containment (`Lengths.cs:333`; endpoints `ProofRequirement.cs:227-235`) | DeclaredBound, GuardCondition, RejectRowGuard, PriorAction | ConditionNarrowing, ExclusionNarrowing, ActionEffect, CaseSplit (OR-branch union in `SeedCountInterval`, `ProofEngine.cs:695-728`), BandContainment |
| Key presence (`TryKeyPresenceProof`, `Strategies.cs:668-698`; `GuardHasContainsCheck`/`WalkForContains`, `:1289-1332`; `KeyPresenceProofRequirement`, `ProofRequirement.cs:243-247`) | GuardCondition | DirectMatch — a single whole-guard step, **no CaseSplit**: the branch loop calls `GuardHasContainsCheck` on the WHOLE guard identically per iteration (`branchConstraints` unused, `Strategies.cs:692-696`) and `WalkForContains` recurses only into `And`, never `Or` (`:1303-1305`), so no per-arm sub-derivation exists; recording a CaseSplit here would fabricate arm structure the engine never computed. **honesty caveat**: `WalkForContains` matches on collection identity only (`fr.FieldName == fieldName`, `:1310-1311`/`:1322-1325`) and never compares the key operand; `KeyPresenceProofRequirement` carries no key payload. The engine today establishes only the weaker fact "the guard has a membership check on collection `F`", not "…on key `K`" — a `DirectMatch` step must record only the fact actually established. `F contains A` discharging an obligation about key `B` is a **candidate fifth fail-open**, routed to the Slice-0 systematic fail-open sweep (architecture §2.2); end-state contract: this path's `DirectMatch` carries the key comparison, or the verdict is not `Proven`. |
| Index bounds (`Strategies.cs:390`) | GuardCondition, DeclaredBound (author-declared `nonnegative` only) | ConstraintImplication + DirectMatch (+ CaseSplit); CatalogFact (Types member, third arm) when the nonnegative lower bound is **type-implied** rather than author-declared — `IsTypeDerivedNonnegative` reads `field.ImpliedModifiers` (`:514-532`), which has no authored span and so cannot be a DeclaredBound premise |
| Unreachable-row arm (`ProofEngine.cs:1176-1184`; honest fix per architecture §1.9) | ReachabilityFact | UnreachableRow |
| Dead-end→dead-end arm (`ProofEngine.cs:1186-1196`; becomes `Unresolved` per §1.9) | — | — (Unresolved certificate: empty derivation + plain missing-condition text "not analyzed — resolve the dead-end first") |

No current path requires a kind outside the twenty-one; no premise outside the eleven — with two honest qualifications from the 2026-07-13 adversarial review pass: (1) **type-implied modifiers** (`TypeMeta.ImpliedModifiers`, populated from the Types catalog at `TypeChecker.cs:689-693`) are a live evidence source with no authored span — the first draft's vocabulary had no legal way to cite them; they are now covered *inside* the twenty-one by `CatalogFact`'s third arm (Types-catalog member), never by a premise; (2) the projection/lift modifier sub-arms and the dimension `Any`-subsumes arm involve non-identity inferences that `DirectMatch`'s same-fact recompute rule cannot honestly record — their step assignment is **parked as Open question 1** (one candidate resolution grows the membership past twenty-one; see Falsifier 2).

## Decisions

*Spec-first note applying to all decisions*: grepped `precept-language-spec.md`, `proof-engine.md`, `catalog-system.md` for "certificate" — the spec **mandates the catalog's existence and its properties** (`:225`: "a small, spec-enumerated vocabulary (the `CertificateSteps` catalog) that an independent checker can replay and the author can read") and `proof-engine.md:2611-2612` records the format-only decision, but **no canonical doc enumerates members** — membership is genuinely open, exactly as architecture §3 states ("its *membership* is unruled — it owes the §5a design pass"). The Pre-Design Owner Consultation gate is satisfied upstream: the owner ruled Decision C1 and expressly delegated membership to this §5a pass (architecture §3, decision ledger entry quoted there).

---

### Decision 1: The premise vocabulary — eleven closed premise kinds

The `CertificatePremise` discriminated union has exactly these members, each quoting its source by span and carrying the extracted machine-readable fact:

1. **DeclaredBound** — a numeric/length/count bound modifier on a field or arg (`min`/`max`/`nonzero`/`positive`/`negative`/`nonnegative`/`minlength`/`maxlength`/`mincount`/`maxcount`); carries authored *and* normalized values (reuses `AuthoredMin/Max`, `ProofRequirement.cs:184-197`). Renders: "`Total` declares `max 1000` (line 3)".
2. **DeclaredModifier** — a structural modifier (`ordered`, …). Renders: "`Priority` is declared `ordered` (line 6)".
3. **DeclaredQualifier** — a declared axis value: unit, currency, price denominator, timezone, temporal dimension (`of 'kg'`, `in 'USD'`). Renders: "`Weight` is measured in `kg` (line 4)".
4. **DeclaredDefault** — the authored default value of a field/arg. Renders: "`Total` defaults to `0` (line 3)".
5. **GuardCondition** — one leaf condition of the governing guard (per branch): a row/hook/handler guard, or a rule/ensure's own `when` guard consulted during discharge (`ConstraintContext` arms, `Strategies.cs:728-733`/`:1114-1119`). Renders: "this row runs only when `Total <= 900` (line 11)".
6. **RuleCondition** — an unconditional rule's predicate (relational or constant). Renders: "rule declares `ApprovedAmount <= RequestedAmount` (line 37)".
7. **EnsureCondition** — an ensure's condition contributing narrowing (event-scoped ensure facts per `Strategies.cs:746-758`, state ensures per the composition path). Renders: "before this event runs, `Amount is set` must hold (line 47)".
8. **RejectRowGuard** — an earlier reject-row's guard on the same (state, event), cited for exclusion ("this row is reached only when that guard failed"). Renders: "the row above rejects when `Score < 600` (line 20), so here `Score >= 600`" *(the negation itself is the `ExclusionNarrowing` step, not the premise — the premise quotes the guard as written)*.
9. **PriorAction** — an earlier action in the same transition chain whose catalog-declared effect is used (grow ⇒ non-empty; count delta). Renders: "line 9 adds an element to `Items`".
10. **ReachabilityFact** — the one compiler-derived (not author-written) premise kind: a state's unreachability per the produced `StateGraph` (needed for the honest "never runs — nothing to prove" verdict, architecture §1.9). Renders: "state `Archived` can never be reached from `Open` (state graph)". The design is explicit that this premise cites a *derived artifact*; it is admissible because the graph is itself a deterministic derivation of the author's declared transitions (spec §0.5), and §1.9 locks exactly this citation ("the premise cites the produced `StateGraph`").
11. **DeclaredPresence** — quotes the declaration aspect that guarantees the field always has a value (default or required shape), span-cited; consumed by the guaranteed-presence fallback (`Strategies.cs:218-225`, `DeclaredPresenceMeta.Guaranteed`). Renders: "`Total` always has a value — it declares a default (line 3)".

**Stakes**: high (public vocabulary surfaced through hover/MCP).

- **Rationale**: the set is derived, not invented — it is the closure of "evidence sources actually consulted by the existing discharge paths" (coverage table above): declaration surfaces (1-4, plus 11 — DeclaredPresence), path conditions (5-8), sequential-chain facts (9), and graph facts (10). Each kind maps 1:1 to a distinct author-visible construct, which is what makes the rendering honest ("the guard says…" vs "the rule says…" are different author actions and must not be blurred). DeclaredPresence is span-citable — the declaration's presence shape (default value or required-ness) sits at a specific source line — which is why it is a premise and not a `CatalogFact` step; see Decision 3 for the parallel boundary on facts that have no author span.
- **Tradeoff accepted**: eleven kinds is more surface than a single generic "AuthoredFact(span, text)" — more catalog members to maintain. Accepted because a generic fact kind would push the *kind* of evidence into free text, un-typeable for the §5b checker and un-renderable per-kind.
- **Alternatives considered**: (a) *One generic `AuthoredFact`* — rejected: loses the premise-kind typing the checker needs (admissibility of a guard fact differs from a rule fact — staleness, scope) and collapses distinct author actions in rendering. (b) *Fold Ensure into Rule* ("both are constraints") — rejected: they have different scopes and different admissibility conditions (event ensures narrow only rows of that event, `Strategies.cs:750`); blurring them is exactly the coarse join the soundness lens forbids. (c) *A separate premise kind for negated reject-row facts* (premise carries the already-negated condition) — rejected: negation is an *inference*; premises must quote the author's text as written, all inference lives in steps (locked §1.2 shape). (d) *The requester's five-kind sketch* (authored-bound, guard-fact, rule-fact, reject-row-above, reachability-fact) — extended, not contradicted: the sketch omits ensure facts, declared modifiers/qualifiers, defaults, and prior actions, all of which existing paths consult (coverage table); without them, certificates for qualifier/default/growth discharges would have nothing to cite.
- **Precedent**: `ProofSubject` as a closed supporting DU beside a catalog (`ProofRequirement.cs:11-25`); DRAT's premise base being the original formula's clauses cited by index (mirror, cited above).
- **Sources consulted for this decision**: coverage table paths — `ProofEngine.Strategies.cs:713-787` ("a guard fact about a field reassigned earlier in this action chain is stale — it must not discharge"), `:728-733` (stale-fact/reassignment guard, `ConstraintContext` arms), `:746-758` (event-ensure narrowing), `:1114-1119` (flow-narrowing rule/ensure `when`-guard arm), `:218-225` (guaranteed-presence fallback consuming `DeclaredPresenceMeta.Guaranteed`), `ProofEngine.Intervals.cs:531-537` ("Sibling reject-row composition: … an earlier reject-row on the same (state, event) pair narrows the field values that can reach this row"), `:539-541` ("Unconditional field-to-field relations contribute a half-line bound…"), `ProofLedger.cs:38-50` (ReassignedBefore doc), `ProofEngine.cs:1149-1161` (ReachabilityFact/DeadEndStateFact), `ProofRequirement.cs:184-197` ("AuthoredMin/AuthoredMax carry the raw authored values as written by the user"); spec-first grep shown in the note above (spec `:225` mandates, does not enumerate).
- **Strongest counter-evidence**: the architecture's own sketch lists fewer premise kinds (§1.2 names "a bound, a guard, a rule, a reject-row-above"), suggesting a smaller set was intended. Response: §1.2's list is expressly illustrative ("the step kinds named in this doc are illustrative sketches for that pass to settle", §1.2 italic note), and the universal-certificate rule in the same section forces coverage of paths that list cannot cite.
- **Reversibility**: Easy pre-ship (pre-release, solo-dev; renames/merges are a one-slice mechanical change); Hard post-ship (MCP consumers parse premise kinds).
- **Blast radius**: catalogs — the new supporting DU only; docs — `catalog-system.md` § Supporting Types, `proof-engine.md`, `docs/tooling/mcp.md`; samples — none (no authored syntax); external consumers — `precept_proofs`/hover readers.

---

### Decision 2: Premises are a closed supporting DU inside the CertificateSteps catalog entry, not a second catalog

**Stakes**: medium.

- **Rationale**: Decision C1 ruled *one* new catalog ("a spec-enumerated `CertificateSteps` catalog", architecture §3). The premise vocabulary is structurally a supporting type of that catalog — it has no independent consumers, no per-member metadata beyond description/rendering, and exists only inside certificates — matching `catalog-system.md`'s existing supporting-type category ("They are not catalogs themselves — they have no `Kind` enum or `All` property — but they are part of the catalog system's vocabulary", § Supporting Types).
- **Tradeoff accepted**: premises are not independently enumerable via a catalog `All` — MCP documents them through the CertificateSteps catalog entry's doc/formatter rather than a dedicated enumeration. Acceptable: no consumer needs to enumerate premise kinds without steps.
- **Alternatives considered**: (a) *A sixteenth+seventeenth catalog pair (CertificatePremises)* — rejected: exceeds what C1 ruled and adds a catalog with no distinct consumers; (b) *premise kinds as members of CertificateSteps itself* — rejected: premises and steps are different shapes (quote-of-declaration vs inference) and the locked format separates them (§1.2).
- **Precedent**: `ProofSubject` (`ProofRequirement.cs:11-25`) — a closed DU supporting the ProofRequirements catalog, documented under catalog-system.md § Supporting Types.
- **Sources consulted for this decision**: `catalog-system.md:1007-1009` ("These record types are shared across multiple catalogs. They are not catalogs themselves…"); architecture §3 Decision C1 text ("the source is a new spec-enumerated `CertificateSteps` catalog (C1)").

---

### Decision 3: Catalog facts are cited by steps (payload = the owning catalog member), never quoted as premises

**Stakes**: high (draws the boundary between author-authored evidence and spec-owned inference machinery — a public-vocabulary decision with no prior canonical settlement).

- **Rationale**: premises quote the author's declarations by span; a catalog fact (a function-return guarantee, an accessor guarantee, an operation transfer, a type-implied modifier — `TypeMeta.ImpliedModifiers`, e.g. `exchangerate` implies `positive`, `Types.cs:674`) has no span in the author's file — it is the *inference machinery's own metadata*. The Decision 5 pattern generalizes: the step cites the member (derive-don't-duplicate) and the §5b checker reads the fact from the catalog, which IS the machine-readable language spec — so the trusted base is the spec, not the engine.
- **Alternatives considered and rejected**: (a) a distinct `CatalogFact` **premise** kind — rejected because it breaks the premise invariant (span-quoted authored text), forcing nullable spans or fake spans; it blurs the evidence taxonomy the renderer relies on ("the author declared…" vs "the language guarantees…"); and it duplicates the citation the step payload already carries. (b) restating the fact's content inline in the step (e.g. copying "returns nonnegative" as free text on every citing step) — rejected: drift risk, a parallel copy of catalog knowledge, the exact violation CLAUDE.md's derive-don't-duplicate rule names.
- **Precedent**: Decision 5 (`IntervalArithmetic` cites the Operations catalog member rather than restating its transfer); LRAT/DRAT cite premises by index rather than restating them (compactness goal 2, mirror excerpt under Language Design Grounding); `catalog-system.md`: "Their union IS the language specification in machine-readable form."
- **Tradeoff accepted**: a certificate is not fully self-contained — replaying a `CatalogFact` step requires the catalog. Accepted because the catalog is versioned spec, not an opaque runtime detail, and self-containment would mean copying spec content into every certificate — the drift risk above.
- **Strongest counter-evidence**: architecture §1.2's "premises quote the author's declarations verbatim by span" could be read as "all certificate evidence is authored." Response: the same section's universal-certificate rule (every verdict carries a certificate) forces coverage of discharge paths whose evidence is spec-owned, not author-owned (e.g. `abs` never returns negative); the locked text constrains what a *premise* is, not what a step's payload may cite.
- **Reversibility**: Easy pre-ship (pre-release, solo-dev; a `CatalogFact` step is a small, mechanical subtype); Hard post-ship (an external §5b checker built against this boundary would need to learn a new premise shape).
- **Blast radius**: catalogs — the `CatalogFact` step subtype only, no new catalog; docs — `catalog-system.md` § Supporting Types cross-reference, `proof-engine.md`; code — the renderer (`CatalogFact` line reads "a built-in guarantee: …"); no authored-syntax or sample impact.
- **Philosophy reconciliation**: this decision is what makes the Philosophy Alignment table's row 2 rewording true (§ Philosophy Alignment, above) — no premise or step admits anything outside the authored file plus the language spec; the catalog is the spec, not an ambient external source.

---

### Decision 4: The step vocabulary — twenty-one closed step kinds

`CertificateStepKind` members (recompute rules in § Semantic Rules; per-kind one-line plain meaning):

| # | Member | Plain meaning (rendering register) |
|---|---|---|
| 1 | `IntervalArithmetic` | "range ⊕ range = range" for one operation of the site (per Decision 5: cites the Operations catalog member; carries a Measure: value / length / count) |
| 2 | `SignArithmetic` | "a nonnegative plus a positive is positive" — one operation's sign-classification transfer |
| 3 | `UnitNormalization` | "`6 lb` is `2.72 kg`" — rescale a range by the static unit factor |
| 4 | `ConditionNarrowing` | "the guard/rule keeps `X` at or below 900, so `X` is `[0..900]`" — intersect with one condition's licensed range |
| 5 | `ExclusionNarrowing` | "the row above already rejected `X >= 10`, so here `X <= 9`" — intersect with the *complement* of a cited single-leaf condition |
| 6 | `RelationalNarrowing` | "`X <= Y` and `Y` never exceeds 100, so `X <= 100`" — half-line from a relation plus the related field's bare declared range (single hop, by definition) |
| 7 | `CaseSplit` | "either branch A or branch B holds; in A …, in B …; combining:" — per-arm sub-derivations under arm assumptions; combiner = range union, sign-set union, or all-arms-establish |
| 8 | `ActionEffect` | "line 9 adds an element, so the count is at least 1 / moves from `[a..b]` to `[a+1..b+1]`" |
| 9 | `Comparison` | "5 ≠ 0 ✓" / "`[0..900]` is entirely above 0 ✓" / "the sign set is entirely positive ✓" — one comparison against one threshold, over a value, a range, or a sign set |
| 10 | `BandContainment` | "`[0..1000]` fits the declared `[0..1000]` ✓" — the three-way fits / entirely-outside / overlaps conclusion |
| 11 | `ConstraintImplication` | "the guard requires `D > 0`, which already guarantees `D ≠ 0`" — one condition implies another (finite table) |
| 12 | `RelationImplication` | "`A >= B` guarantees `A − B >= 0`" — a two-field relation implies the requirement on their difference (finite table) |
| 13 | `DirectMatch` | "the guard already requires exactly this (`Amount is set`)" — one premise's fact is the required fact |
| 14 | `QualifierAgreement` | "both sides are in `kg`" — two qualifier facts compared under a recorded mode (equality, temporal-unit subset, or symbolic same-source) |
| 15 | `DimensionComposition` | "mass × count is mass — a known business dimension" |
| 16 | `AssignmentCoverage` | "every write to `F` comes from a source that carries `positive`" — finite enumeration of write sites |
| 17 | `UnreachableRow` | "state `Archived` can never be reached, so this row never runs — nothing to prove" |
| 18 | `SignResolution` | "combine everything known about a value's sign" — the meet (intersection) of the cited sign facts for one bare field/arg subject |
| 19 | `RelationDerivedSign` | "`X > Y` and `Y` is never negative, so `X` is positive" — one relation premise plus the related field's declared bound, per the finite relation-to-sign table |
| 20 | `CatalogFact` | "a built-in guarantee: `abs` never returns a negative number" / "an exchange rate is always positive" — one spec-owned fact cited from a Functions overload, a fixed-return accessor, or a Types-catalog member's implied modifier |
| 21 | `QualifierResolution` | "how the engine worked out which unit/currency this side carries" — the ordered hop list (declared / axis-fallback / compound-projection / operation policy) that resolves an operand's qualifier |

**Stakes**: high (public, spec-owned vocabulary; the §5b checker's rule set).

- **Rationale**: (i) *Completeness is forced*: "**Every** verdict carries a certificate" (architecture §1.1) over the *existing* engine means every current discharge path must render into cataloged steps at Slice 0 — the coverage table shows the twenty-one are the minimal set doing so; the requester's four-step sketch (interval-arithmetic, guard-narrows-interval, fits-check, case-split) covers only the interval-containment path and would leave qualifier, sign, relation-implication, presence, growth, and reachability verdicts with nothing legal to emit. (ii) *Granularity is chosen at the DRAT pole*: each kind's conclusion recomputes in one bounded operation from cited children (§ Semantic Rules), the property the in-tree survey identifies as the cheap-checking prerequisite; no member means "by strategy X". (iii) *Fused-but-decomposable kinds are deliberate*: `BandContainment` could be two `Comparison`s and `QualifierAgreement` a generic equality, but each fused kind renders as the single line an author actually thinks in ("fits the declared band ✓") while its recompute rule remains one bounded check — legibility gains, soundness loses nothing. (iv) *Four members were missed in the first pass and are added here after adversarial review verified them against engine source*: the sign-resolution layer has no legal step of its own (`ResolveNumericSubjectSignSet`'s meet over modifier/rule/relation facts, `Composition.cs:478-529`, plus the post-cascade second pass `ApplyTrustedRuleFacts`, `:76-112`) — hence `SignResolution` and `RelationDerivedSign` (relation-to-sign, `:601-665`); catalog-owned guarantees discharge premise-less today (`Strategies.cs:163-184`/`:230-243`, `Composition.cs:436-437`) with nothing legal to cite them — hence `CatalogFact` (see Decision 3 for the premise-vs-step boundary this closes); a second adversarial pass (2026-07-13) found a **third catalog-fact family** both earlier passes missed — type-implied modifiers, folded into the sign meet (`Composition.cs:674`), concatenated in the declaration-attribute walk (`Strategies.cs:205`), and read for index bounds (`:514-532`), all populated from the Types catalog at `TypeChecker.cs:689-693` — closed by widening `CatalogFact`'s recompute rule to a third arm (§ Semantic Rules), not by a new kind; and qualifier resolution is a real multi-hop derivation (axis fallbacks, compound-price projections, transitive `ResultQualifier` policies — `ProofEngine.Qualifiers.cs:269-394`), not a single lookup — hence `QualifierResolution`, with `QualifierAgreement` widened to a recorded comparison mode (`QualifierNarrowing.cs:54-77` for the TemporalUnit-subset arm, `Qualifiers.cs:175-187` for symbolic same-source).
- **Tradeoff accepted**: twenty-one members is a wider closed set than DRAT-style formats carry, and each new proof strategy must extend the catalog (a deliberate friction — Decision 6). Also accepted: `SignArithmetic`/`SignResolution`/`RelationDerivedSign` and `IntervalArithmetic` are different flavors of "arithmetic line" in renderings (mildly less uniform for readers) because sign sets are not intervals (nonzero = {negative, positive} has a hole at 0) and pretending otherwise would make recompute rules dishonest.
- **Alternatives considered**: (a) *Strategy-name-only steps* (one kind per `ProofStrategy` member) — rejected by the locked architecture itself ("not a derivation — nobody can replay 'proved by FlowNarrowing'", §1.2) and by the Alethe evidence. (b) *A micro-kernel set (~6 kinds: narrow, negate, arith, union, compare, match)* — the soundness lens's minimal set; rejected as the shipping shape because it forces qualifier/dimension/sign/coverage inferences through generic "match/compare" steps whose payloads then need open-ended encodings (the coarse join returns through the back door), and because fused domain-specific kinds render better; the micro-kernel survives as the *recompute-rule* layer (each of the 21 reduces to those primitives). (c) *Adding a `WitnessEvaluation` step kind* — rejected: the witness is a separate `ProvenViolating` payload (locked §1.1/§1.4); its validation replays through point-interval `IntervalArithmetic` + `Comparison` if ever rendered, needing no new kind. (d) *A fourth `NotApplicable`-style verdict kind for dead-end rows* — already rejected by architecture §1.9 (three-way surface); the dead-end arm is an `Unresolved` certificate with empty derivation. (e) *Folding `SignResolution`/`RelationDerivedSign`/`CatalogFact`/`QualifierResolution` into existing kinds* — rejected: each has a distinct recompute rule (finite sign-meet; relation-to-sign table; bounded catalog lookup; ordered hop replay) that a fused kind would obscure — the same fused-vs-micro balance already struck for `BandContainment`/`QualifierAgreement` in (iii), applied consistently.
- **Precedent**: DRAT's polynomial-time-checkable step discipline and Alethe's coarse-step warning (verbatim excerpts under Language Design Grounding); the in-tree survey's C1 conclusion ("a witness that carries the narrowed intervals and the containment arithmetic is fine-grained and cheaply re-checkable"); internally, `ProofRequirementMeta`'s 13-member DU-as-identity closed vocabulary (`ProofRequirement.cs:276-380`).
- **Sources consulted for this decision**: every coverage-table path anchor (Decision 1's leg lists the shared ones); additionally `ProofEngine.cs:1054-1135` (the full `TryDischarge` cascade — the set of paths needing coverage), `Strategies.cs:1263-1274` (the finite relation truth table `RelationImplication` replays: "(OperatorKind.GreaterThan, OperatorKind.GreaterThan) when requirement.Threshold == 0 => true …"), `Composition.cs:13-69` (sign path + interpolated-writes path: "If ANY assignment to this field is NOT an interpolated typed constant, returns empty (conservative)"), `Composition.cs:478-529`/`:601-665`/`:765-804` (sign-resolution meet, relation-to-sign table, sign-set requirement/comparison tables), `ProofEngine.cs:64-75` (`NumericSignSet` incl. `Nonzero = Negative | Positive`), `Intervals.cs:135-140` (conditional-arm union exists today — `CaseSplit` is a Slice-0 member, not §2-only), `Lengths.cs:25/:333`, `QualifierNarrowing.cs:54-77`, `Qualifiers.cs:175-187/:269-394`; spec-first grep per the note above.
- **Strongest counter-evidence**: the architecture doc's per-slice table assigns "`CaseSplit` certificate step" to the §2 slice (§4 table), implying CaseSplit is *not* Slice-0 — which would argue for a smaller Slice-0 set. Response: the same doc's §1.1 universal-certificate rule outranks the table's shorthand, and the engine *already* unions across OR-branches and conditional arms today (`Intervals.cs:549-606`, `:135-140`), so a Slice-0 certificate for any guarded OR discharge needs `CaseSplit` on day one; §2 *extends* the arm sources, it does not introduce the kind. This table's own shorthand is corrected in the Doc-update enumeration (below). (Searched for further counter-evidence in `proof-engine.md` §Decision 1 and the witness survey; found none against the membership itself.)
- **Key-presence honesty (found during the same review, not a new member)**: `DirectMatch`'s recompute rule requires identity of the *full* required fact — for key presence that includes the key operand. Today `WalkForContains` (`Strategies.cs:1295-1332`) never compares the key operand, and `KeyPresenceProofRequirement` (`ProofRequirement.cs:243-247`) carries no key payload, so the engine establishes only the weaker fact "the guard has a membership check on collection `F`." A certificate must never record identity the engine didn't establish. This is a **candidate fifth fail-open** (`F contains A` discharging an obligation about key `B`) — routed to the Slice-0 systematic fail-open sweep (architecture §2.2), not fixed in this design; see the coverage table's Key presence row for the end-state contract.
- **Reversibility**: Easy pre-ship (member renames/merges are mechanical: enum + meta + builder sites + fixtures); Effectively-irreversible-post-ship for *semantics* of a shipped member (an external checker built on a recompute rule breaks if the rule changes) — the reason the recompute rules are stated normatively in § Semantic Rules now.
- **Blast radius**: catalogs — one new catalog + two supporting DUs; code — `ProofEngine` builder wiring across the files in the coverage table; docs — `catalog-system.md` (inventory + supporting types), `proof-engine.md`, `mcp.md`, `language-server.md`, spec pointer at `:225`; tests — new catalog-integrity + coverage tests, fixture migration already budgeted by Slice 0; external consumers — `precept_proofs`/hover readers.

---

### Decision 5: One `IntervalArithmetic` step kind referencing the owning catalog member's transfer — not per-operator step kinds

**Stakes**: high (shapes the largest class of rendered lines and the checker's arithmetic contract).

- **Rationale**: the catalog system already owns the closed vocabulary of typed operator combinations (**Operations**) and each operation's interval transfer is attached there — a binary op's (`BinaryOperationMeta.IntervalTransfer`, dispatched generically at `Intervals.cs:102-112`), a unary op's (`UnaryOperationMeta.IntervalTransfer`, `:114-119`), or a function overload's (`FunctionOverload.IntervalTransfer`, `:122-132`, from the **Functions** catalog). Per-operator step kinds (`Add`/`Subtract`/`Multiply`/`Divide`/`Scale`/per-function) would be a **parallel copy of the Operations/Functions catalogs inside CertificateSteps** — the exact violation CLAUDE.md's "never maintain parallel keyword lists / derive, don't duplicate" rule names. One step kind whose payload cites the owning catalog member derives instead: rendering pulls the operator symbol from the Operators catalog (as `DescribeOperator` already does, `ProofEngine.cs:1008-1017`), and the §5b checker replays the *cited member's* transfer. Per-operator/per-function granularity arrives for free at the instance level — every step instance names its exact operation or function overload — without a second enumeration to keep in sync. §3 money then needs **zero** certificate-vocabulary work: new transfers wired in Operations automatically carry certifiable steps (matching the plan's "§3 money … per-op certificate `Arithmetic` steps" with no new kinds). Length-measure compositions that have no catalog-member transfer to cite (interpolated-string sum-of-segments with numeric-hole digit-width widening, `Lengths.cs:138-208`; string-function length caps, `:250-291`) still use this one step kind — their transfer rule is stated normatively in the CertificateSteps catalog meta itself rather than dispatched from a catalog member, which is the one place this decision's "cite the owner" posture cannot yet be fully realized (see the § Semantic Rules observation on the `Lengths.cs:253` `FunctionKind` switch — a catalog-placement gap noted, not fixed, in this design).
- **Tradeoff accepted**: the CertificateSteps catalog alone does not tell a reader which arithmetic operations exist — that answer lives in Operations/Functions, one dereference away. Accepted: that is precisely the derive-don't-duplicate posture; the alternative is drift between two (or three) enumerations.
- **Alternatives considered**: (a) *Per-operator step kinds* — rejected as a parallel enumeration (above), and it scales badly: Operations has dozens of money/quantity/price cells (`Operations.cs:437-559` per architecture §1.5), each of which would demand a mirrored step member. (b) *One fully generic `Transfer` step covering interval AND sign domains* — rejected: interval and sign transfers have different value payloads and different recompute rules; a domain tag on one kind is a classification field mapping 1:1 to behavioral subsets, which `catalog-system.md`'s meta-shape rule says should be distinct DU identities. (The Measure tag on `IntervalArithmetic` — value/length/count — is *not* that case: all three measures recompute identically, endpoint arithmetic on the same interval shape.)
- **Precedent**: LRAT/DRAT cite clauses by index rather than restating them per step (compactness goal 2, mirror excerpt above) — cite-the-owner rather than copy; internally, `ProofObligation` cites `ProofRequirement` instances rather than restating catalog metadata.
- **Sources consulted for this decision**: `Intervals.cs:102-112` ("`if (opMeta is BinaryOperationMeta bom && bom.IntervalTransfer is { } transfer)` … `return transfer(leftInterval, rightInterval);`" — the generic dispatch the step mirrors); `Intervals.cs:114-132` (the parallel unary-op and function-overload dispatch arms); `ProofEngine.cs:1008-1017` (`DescribeOperator` deriving the symbol from Operators/Operations); CLAUDE.md catalog rules ("Never maintain parallel keyword lists… derive, don't duplicate"); architecture §1.5 (the op-meta family enumeration); plan Slice-§3 text ("per-op certificate `Arithmetic` steps").
- **Strongest counter-evidence**: a self-contained checker argument — per-operator kinds would let the §5b checker be written against CertificateSteps alone, without consulting Operations. Response: the checker must implement interval arithmetic per operation either way; reading *which* transfer from Operations metadata does not enlarge its trusted base, because Operations **is** the machine-readable language spec (catalog-system.md: "Their union IS the language specification in machine-readable form").
- **Reversibility**: Easy pre-ship (splitting one kind into several is additive); Hard post-ship.
- **Blast radius**: contained — one step subtype's payload shape; rendering code; §3 money inherits automatically; no doc surface beyond the catalog entry.

---

### Decision 6: Closure boundary — full coverage of existing paths at Slice 0; later slices grow membership through catalog-before-code, no speculative stubs

**Stakes**: medium.

- **Rationale**: two forces fix the boundary. *Floor*: universal certificates (architecture §1.1) mean Slice 0 cannot ship less than the twenty-one — every existing path must emit legal steps on day one. *Ceiling*: a member with no live emitter is dead vocabulary — the certificate analogue of a `[StaticallyPreventable]` diagnostic no code emits, which this project treats as a hole, not a feature (the emission-coverage Gate-1 discipline, catalog-system.md § Roslyn Enforcement: "Adding a new diagnostic code without an emission site silently degrades user experience — the spec advertises a compiler check the compiler does not run"). So no stubs for unbuilt slices. Checked against each later slice's locked discharge design: **§3 money** adds transfers, not inference forms — covered by Decision 5 with zero new members. **§2 case-by-case** extends `CaseSplit` arm *sources* (conditional-expression arms, event-input narrowing) — the kind, its combiner semantics, and per-arm assumptions ship at Slice 0 because OR-branch and conditional-expression unions exist in the engine today (`Intervals.cs:549-606`, `:135-140`); §2's ⊥-arm skip rule is engine behavior whose certificate effect is "arm recorded as infeasible-contributes-base-interval" — payload, not a new kind. **§6 constant-rules** is `ConditionNarrowing` citing a `RuleCondition` — both ship at Slice 0. **Two known growth sources exist beyond Slice 0, both counted**: (1) **§1a multi-term facts** widens `ConditionNarrowing`'s subject from a single field to a term multiset and may need one genuinely new inference (the commutative/associative term-match) — this is the **one flagged likely growth**, and it lands via its own slice's catalog-first change, with the coverage test forcing the member and its emitter to arrive together. (2) **§1b octagon-style relational extensions** (deferred two-hop closure — the Scope section's deferral) are a second, *unscheduled* growth source: the archived relational-rules design locked single-pass 0-or-1-hop reasoning (`docs/Working/Archive/relational-rules-and-bounds-design-2026-06-02.md`, Locked 2026-06-02) and current canon reaffirms it (readiness plan single-hop/anti-transitivity note, decision ledger #3 — "§1b DEFERRED... two-hop override NOT authorized"); if ever authorized, its steps land through this catalog's normal growth path like any other. **No-conflict finding**: `RelationalNarrowing`'s recompute rule ("Y's bare declared interval, single hop") does NOT conflict with either growth source — it matches the locked single-hop rail (architecture §1.6: "the relational half-line continues to read Y only via its bare declared interval... `ExtractFieldInterval` must not learn to fold constant rules") and stays true even after §6 lands, because §6 narrows via `ConditionNarrowing` citing a `RuleCondition`, not via a second relational hop.
- **Tradeoff accepted**: the catalog will see per-slice churn (at least one probable addition at §1a, one deferred-and-unscheduled possible addition at §1b) rather than being final at Slice 0; each addition is a public-vocabulary change requiring the same catalog-before-code discipline. Accepted: honest growth beats speculative members whose shapes would be guessed before their slice's design settles them.
- **Alternatives considered**: (a) *Enumerate-and-stub future kinds now* (e.g., a `TermMatch` member with no emitter) — rejected: dead vocabulary (above), and it pre-empts the §1a slice's own design authority over its inference shape. (b) *Ship a minimal Slice-0 set and let early certificates use a catch-all step* — rejected: the catch-all IS the strategy-name-only label the architecture rejects (§1.2 alternatives).
- **Precedent**: the Gate-1/Gate-2 emission-and-test coverage model (catalog-system.md § Roslyn Enforcement Layer); the ProofRequirements catalog's history of growing one obligation kind at a time with its discharge ("The strategy set has grown one strategy at a time; each addition goes through `/design` review", `proof-engine.md:2424`).
- **Sources consulted for this decision**: architecture §1.5-1.8 (each later slice's discharge mechanics — §1.5 "zero engine code" catalog wiring; §1.7 "one syntactic term-multiset matcher (commutative/associative normalization only)"; §1.8 "extend the existing guard-branch DNF union (`Strategies.cs:794`, `Intervals.cs:549-606`)"); `Intervals.cs:135-140` (conditional union today); catalog-system.md Gate-1 rationale quoted above; `docs/Working/Archive/relational-rules-and-bounds-design-2026-06-02.md` (Locked 2026-06-02, single-pass 0-or-1-hop); `docs/Working/compiler-readiness-plan-2026-07-12.md` (single-hop/anti-transitivity note, ledger #3); architecture §1.6 (relational half-line single-hop reaffirmation).

---

### Decision 7: `CaseSplit` is a compound step — arms carry labeled assumptions and nested sub-derivations

**Stakes**: medium.

- **Rationale**: the locked format already makes steps cite children (architecture §1.3: "every step carries its own recomputable conclusion from cited children"); a case-split's children are naturally *labeled groups* (arm assumption + the arm's steps), and flattening them into a single sequence would lose which narrowing belongs to which arm — un-replayable and unreadable. The arm assumption is either a cited premise's condition or its negation (the implicit else); an arm whose narrowing is infeasible is recorded with its contribution per the engine's rail (base interval absent an explicit infeasibility derivation — architecture §1.8 ⊥-arm rule). The combiner is not always interval union: a conditional (`TypedConditional`) sign-set site combines its arms by sign-set union (`ResolveNumericSignSet`'s `|` of the branch sign sets, `Composition.cs:438-439`), so `CaseSplit`'s combiner is interval union, sign-set union, or all-arms-establish, chosen by the abstract domain of the site being split.
- **Tradeoff accepted**: the certificate record becomes a tree, not a flat list — marginally more complex DTO/rendering. Accepted: hover already renders nested content, and flat certificates would need arm-index bookkeeping that is strictly worse.
- **Alternatives considered**: flat step list with arm-tag fields — rejected: tags papering over a structural shape (the nullable-field smell, CLAUDE.md DU rule).
- **Precedent**: structured sub-proofs in proof formats (Alethe's `anchor`/subproof steps, per the mirrored paper's format description); internally, `ExtractGuardBranches`' branch-set shape (`Strategies.cs:789-799` — "AND nodes cross-product their children's branch sets; OR nodes union them").
- **Sources consulted for this decision**: architecture §1.3 ("recomputable conclusion from cited children") and §1.8 (⊥-arm: "a ⊥-arm conservatively contributes its un-narrowed base interval"); `Strategies.cs:789-799`; `Composition.cs:438-439` (`TypedConditional` sign-set union combiner).

---

### Decision 8: Sign-set reasoning gets its own `SignArithmetic` step kind (distinct abstract domain, not folded into intervals)

**Stakes**: medium.

- **Rationale**: the compositional strategy reasons in sign sets, and `Nonzero = Negative | Positive` (`ProofEngine.cs:73`) is not an interval — folding sign facts into interval steps would either lose the hole at zero (unsound recompute rule) or force disjoint-union intervals into the format (a new lattice the engine doesn't use). The certificate must carry the engine's *actual* abstract value (approximation honesty, Principle 8). The resolution-layer review (Decision 4) found the sign domain needs a small family, not one kind: `SignArithmetic` (per-operation sign transfer), `RelationDerivedSign` (a relation plus a related field's declared bound implies a sign), `SignResolution` (the meet across all cited sign facts for one subject), and `Comparison` widened to a sign-set operand (the finite `SignSetSatisfiesRequirement` table) — four distinct recompute rules over the same abstract domain, none foldable into the others without losing which specific inference produced the conclusion. The catalog-owned leaves those steps cite are `CatalogFact` steps, not premises — see Decision 3 for that boundary; this covers both function/accessor guarantees (e.g. `abs` never returns negative) and type-implied modifiers folded into the meet (`ResolveNumericSubjectModifiers` adds `field.ImpliedModifiers`, `Composition.cs:667-689` — e.g. `exchangerate` ⇒ `positive`, `Types.cs:674`).
- **Tradeoff accepted**: four sign-domain step kinds (plus the sign-set arm of `Comparison`) alongside `IntervalArithmetic`'s interval-domain family — more arithmetic-step flavors in renderings than a single unified "arithmetic" line. Accepted (Decision 4 tradeoff leg): the alternative is a dishonest recompute rule at the zero boundary.
- **Alternatives considered**: (a) drop sign certificates and mark compositional discharges with a coarse step — rejected (strategy-name-only regression); (b) generalize the interval type to unions of intervals — rejected: forks the engine's numeric model for bookkeeping's sake (reuse rule); (c) fold `RelationDerivedSign`/`SignResolution` into `SignArithmetic` — rejected (Decision 4, alternative (e)): each has a distinct recompute rule (per-operation transfer vs. relation-to-sign table vs. finite meet).
- **Precedent**: abstract-interpretation practice keeps sign and interval domains distinct (the sign domain is the textbook non-interval domain; the in-tree survey's solver-free sibling covers the domain taxonomy — `research/references/solver-free-static-analysis/` mirrors).
- **Sources consulted for this decision**: `ProofEngine.cs:64-75` (`Nonzero = Negative | Positive`); `Composition.cs:13-25/:102-110` (sign-set discharge: "`var signSet = ResolveNumericSignSet(subject, obligation.Context, trustedFactsArray, semantics); if (!SignSetSatisfiesRequirement(signSet, numeric))`"); `Composition.cs:478-529` (`ResolveNumericSubjectSignSet` — the three-source meet); `Composition.cs:601-665` (`TryRelationalSignForField` — the relation-to-sign table).

## Falsifiers

1. If rendering real corpus certificates shows domain experts (or the owner reading hover) cannot follow a `SignArithmetic` + `Comparison` chain where a single fused "sign summary" line would read clearly, the micro/fused balance is mis-set for the sign domain — merge sign steps into one fused kind.
2. If a growth need surfaces **beyond the two counted sources** (§1a term-matching and the deferred §1b octagon-style closure) — or if §1a's term-multiset work cannot express its matching inference as a payload widening of `ConditionNarrowing` and needs **two or more** new step kinds — Decision 6's growth-source accounting was wrong — re-open the closure decision rather than accreting members ad hoc.
3. If the §5b re-checker, when designed, needs *any* information not present in premises + steps + the obligation site to replay a derivation (e.g., it must re-run a fold to learn an intermediate interval), the vocabulary violated its own recomputability contract — the missing intermediate becomes a mandatory step payload.
4. If certificate construction measurably breaks the interactive budget (corpus compile moving from ~44 ms toward the ~3 ms/file worst-case bound becoming user-visible), the build-for-every-obligation posture (architecture §1.2 tradeoff) needs a lazy-materialization redesign.
5. If more than two step kinds ship with zero live emitters after Slice 0's coverage test lands, the membership was speculative and the dead members must be removed (Decision 6's no-dead-vocabulary rule).

## Acceptance criteria

1. `CertificateSteps.GetMeta` covers all 21 members (CS8509-enforced); `CertificateSteps.All.Count == 21`; catalog integrity test passes.
2. A coverage test compiles the full `samples/` corpus and asserts: every `ProofVerdict` carries a non-null certificate; every certificate premise is one of the 11 `CertificatePremise` cases; every step one of the 21 kinds — zero uncataloged content.
3. A liveness test asserts every `CertificateStepKind` member (except none — all 21) appears in at least one corpus or fixture certificate; a member with zero emissions fails the build's test gate.
4. The worked example compiles `Proved` with a certificate containing exactly a `ConditionNarrowing`, an `IntervalArithmetic` (citing the decimal add operation, Measure = value), and a `BandContainment` step, premised on the two `DeclaredBound`s and the `GuardCondition` — asserted via `Compiler.Compile(...)` (never type-checker-only `Check`).
5. The unguarded variant compiles `Unresolved` with a certificate whose steps stop at `IntervalArithmetic` → `BandContainment(overlaps)` and whose missing-condition text names the guard shape.
6. Recompute spot-check tests: for each step kind, one hand-constructed instance whose recorded conclusion equals an independent recomputation per the § Semantic Rules rule (a unit-level down payment on §5b, not a live checker).
7. The unreachable-row verdict renders "never runs — nothing to prove" with a `ReachabilityFact` premise and an `UnreachableRow` step; the dead-end→dead-end verdict is `Unresolved` with an empty derivation and the plain not-analyzed text (per architecture §1.9).
8. `precept_proofs` and hover render premises (with line numbers) and steps (with conclusions); documented in `docs/tooling/mcp.md` / `language-server.md`.
9. A compositional discharge (e.g. a divisor proven nonzero from `rule X > Y` + `Y min 0`) renders `RelationDerivedSign` → `SignResolution` → `Comparison` with recomputable sign sets at each step.
10. Qualifier certificates carry the comparison mode (Equality / TemporalUnitSubset / SymbolicSameSource) and each side's `QualifierResolution` (or `DeclaredQualifier` premise, when the axis is declared directly rather than derived).
11. No key-presence certificate asserts key identity: a coverage test asserts the `DirectMatch` payload's matched fact equals the required fact including the key, or the path yields no `Proven` verdict (pending the §2.2 sweep outcome).
12. **Premise-span authenticity**: a test asserts every premise in every corpus certificate carries a `SourceSpan` pointing at the authored construct it quotes (guard leaf, rule, ensure, sibling reject-row, prior action, declaration) — no defaulted, empty, or fabricated spans. This exercises the provenance threading of `GuardConstraint` / `FieldToFieldConstraint` / `ScopedNumericFact` / sibling-exclusion / prior-action records (Architecture Grounding § Reuse verification, obligation 2).
13. **Type-implied liveness**: a type-implied discharge (e.g. a divisor of declared type `exchangerate` proven nonzero with zero authored bound modifiers) renders a `CatalogFact` step citing the Types member's implied modifier; the liveness test (criterion 3) counts this arm distinctly, and a recompute spot-check (criterion 6) re-reads the implied modifier from `Types.GetMeta` per the third-arm rule.

## Dependencies

- **Upstream**: the `ProofVerdict` DU (Slice-0 step 1, ruled); the four fail-open fixes land in the same slice (a certificate must not be minted for a verdict a known hole produced falsely — sequencing inside Slice 0: fixes and format land together per the plan); architecture §1.9's two-arm relabel (the `UnreachableRow` kind exists for its honest arm).
- **Downstream**: §3/§2/§6/§1a slices emit through this vocabulary; the §4a witness renders beside (not inside) it; the deferred §5b re-checker consumes the recompute rules stated here; `soundness-and-coverage.md` (Stage-0c doc) describes this format.

## Doc-update enumeration

- `docs/language/catalog-system.md` — add **CertificateSteps** to the Language Definition group (inventory is the source of truth; update the "fifteen catalogs" prose), add `CertificatePremise`/`CertificateValue` under § Supporting Types, add a ProofEngine-integration note.
- `docs/compiler/proof-engine.md` — certificate format section (premises/steps model, the 21-member vocabulary pointer to the catalog, recompute-rule contract), verdict model update; correct any stage-6 status overclaim (plan Slice-0 doc obligations).
- `docs/language/precept-language-spec.md` `:225` — no text change needed (it already names the catalog); verify the pointer resolves once the catalog exists.
- `docs/compiler/diagnostic-system.md` — only via the Slice-0 emission-retirement work (no certificate-specific change).
- `docs/tooling/mcp.md`, `docs/tooling/language-server.md` — certificate projection in `precept_proofs` / hover.
- `docs/compiler/soundness-and-coverage.md` — authored to this certificate model (plan Slice-0 obligation).
- `docs/Working/compiler-readiness-plan-2026-07-12-architecture.md` §4 — per-slice table correction: "`CaseSplit` certificate step" moves from the §2 row to Slice 0 (the kind ships at Slice 0 per Decision 4's counter-evidence response; §2 only extends its arm sources).

## Operational dimensions

- **Security**: N/A — no new source-text ingestion; certificates are derived output over already-parsed input.
- **Observability**: certificates ARE the observability surface for proof reasoning — surfaced via hover, `precept_proofs`, and the diagnostic residuals; a mis-rendered certificate is diagnosable by comparing the step's recorded conclusion against its recompute rule (acceptance criterion 6's test shape).
- **Evolvability**: `UnitNormalization` depends on UCUM static factors already pinned by the existing normalization boundary (spec §0.6 item 5 — "Normalization is applied once at the TypeChecker extraction boundary"); the step cites authored units and the applied factor, so a UCUM table update changes conclusions visibly, not silently.

## Open questions

1. **Step assignment for non-identity modifier/dimension discharges** *(parked at adversarial review, 2026-07-13 — owner rules at the Slice-0 boundary review)*. Four existing sub-paths discharge through an inference `DirectMatch`'s recompute rule (syntactic identity — the SAME fact) cannot honestly record: the choice-metadata projection through an accessor/conditional result (`Strategies.cs:121-126`), the sibling lift proving a literal's `Ordered` from the *sibling* operand's declaration via the operation's same-set typing rule (`:128-135`), the element-type projection (`:146-150`), and the dimension `Any`-subsumption (`PeriodDimension.Any` satisfies any required dimension, `:99`). Options, framed neutrally:
   - **(a) Widen `DirectMatch`'s payload** with a recorded, normatively-enumerated projection hop (element-type projection | same-set sibling lift | `Any`-subsumption). Keeps twenty-one members; softens `DirectMatch`'s identity contract into "identity after a recorded hop" — the risk is re-admitting the coarse join through a hop taxonomy that grows untyped.
   - **(b) Route through `CatalogFact`** citing the owning catalog rule (the operation's same-set requirement; the choice type's `Ordered`) plus an existing combining step. Reuses existing kinds; but the combination itself ("sibling has it + operator types them same-set ⇒ literal has it") still needs a stated rule somewhere, and no existing step owns it.
   - **(c) Add one new step kind** (a projection/subsumption step with its own bounded recompute rule). Honest and narrow; grows the membership past twenty-one and trips Falsifier 2's growth accounting, so it is a membership change the owner must rule on.

   Until ruled, the coverage table marks these sub-arms **PARKED**; a certificate built for them before the ruling must not claim `DirectMatch`, and the affected sub-arms must not mint a certificate that names a step kind whose recompute rule they fail.

(Previously-considered candidates that are NOT open: whether §1a needs a new term-match kind — Decision 6 assigns it to the §1a slice, Falsifier 2 bounds it. The catalog-fact citation shape is resolved by Decision 3, and its **third arm** — type-implied modifiers, a completeness hole found at the 2026-07-13 adversarial review — is closed in-doc by the widened `CatalogFact` recompute rule. The candidate fifth fail-open in key-presence discharge is routed, not open — see the coverage table's Key presence row and architecture §2.2.)

---

## Status note (why Externally-Grounded, not Locked)

Every decision carries its full stakes-level legs. The doc is held at **Externally-Grounded** because the membership is owner-reviewable public vocabulary produced by agent passes: the owner locks it (or amends members) at the Slice-0 boundary review, per the project's slice-review discipline. Nothing here should be treated as settled until that review.

An adversarial review pass (2026-07-13) confirmed six findings against source; five are closed in-doc — the `CatalogFact` third arm for type-implied modifiers (the completeness hole), the provenance-threading build obligation (the "only new computation is recording" overclaim), the widened `AssignmentCoverage` recompute rule, the default-vs-bound interval-magnitude arm mapping, and the key-presence CaseSplit removal — and one is **parked as Open question 1** (step assignment for the non-identity modifier/dimension sub-arms), which the owner rules at the same Slice-0 boundary review. The doc therefore no longer claims zero open questions.
