---
status: Externally-Grounded
phase-target: TBD — slice 4 of the obligation-discharge-matrix population (prerequisite: the owner rulings in § Open questions)
comparable-systems-research-status: partial — external precedent carried inline per decision from two in-tree Stage-1 research files (Astrée / abstract-domain soundness; certifying algorithms, Verasco, PCC/LCF), each with verbatim excerpt and mirrored primary sources
sources-consulted:
  - docs/philosophy.md — "Precept does not present approximation as exactness."
  - docs/language/precept-language-spec.md § 0.1 — the eleven principles (Principles 7, 8, 10, 11 quoted per decision)
  - docs/language/precept-language-spec.md § 0.4 — execution-model properties (purity, no loops, no reconverging flow)
  - docs/language/precept-language-spec.md § 0.6 — proof-layer obligations, proof philosophy, sequential proof flow, relational-reasoning implementation note
  - docs/language/precept-language-spec.md § 0.7 — the compile-time/runtime guarantee contract
  - docs/language/precept-language-spec.md:1354 — the three expression scopes and their evaluation model
  - docs/language/precept-language-spec.md:1967, :1969, :1971 — working-copy atomicity and the mutation-surface enumeration
  - docs/language/precept-language-spec.md:2117 — the operation's mutation sequence (exit actions → mutations → entry actions)
  - docs/language/primitive-types.md — lane tables, `maxplaces`, non-finite `number`, rounding-function faults, the overflow deferral
  - docs/language/collection-types.md — `mincount 1` accessor-safety discharge, quantifier governance
  - docs/language/business-domain-types.md — decimal magnitude backing, exact bound extraction
  - docs/compiler/proof-engine.md — catalog-driven obligation instantiation, strategies, `ProofSatisfactions`, flag-modifier interval folding
  - docs/compiler/soundness-and-coverage.md § 3.1 / § 3.2 — the per-`FaultCode` and per-creation-site disposition tables
  - docs/Working/obligation-discharge-matrix-2026-07-19.md — the matrix (vocabulary, case shapes, minting rule, validity arguments, ratification, amendments)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:187 — the 2026-07-21 site-identity ruling (an evaluation site is an evaluation occasion)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:22 — the drafting precedence (the want doc defines; the matrix drafts against it)
  - docs/Working/obligation-discharge-matrix-2026-07-19.md:349, :350 — the two amendment classes (power-widening; soundness-correction)
  - docs/Working/what-i-want-2026-07-16.md:104 — faults get the identical premise-and-certificate treatment
  - docs/language/precept-language-spec.md:256 — rule-sourced relations discharge a downstream fault-prone obligation
  - docs/language/collection-types.md:776 — the `mincount 1` accessor-safety discharge
  - docs/compiler/soundness-and-coverage.md:196, :229 — the two deferred owner-forks with named re-open triggers
  - live `precept_compile` at HEAD, 2026-07-21 — every `.precept` sample in this document, plus the acceptance-criteria probes; verdicts read from diagnostic codes and obligation records
  - docs/Working/what-i-want-2026-07-16.md — the owner-authored seed model
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md — the nineteen-file verification outcome and the unanswered questions
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/corpus-measurement-fault-family.md — the 1,045-obligation corpus measurement
  - docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json — the cell schema and its closed validity-argument enum
  - src/Precept/Language/ProofRequirement.cs — the requirement DU and its subjects
  - src/Precept/Language/ProofRequirementKind.cs — the thirteen obligation kinds
  - src/Precept/Language/FaultCode.cs — the fifteen-member closed fault registry
  - src/Precept/Language/Operations.cs — divisor and modulo requirement declarations; `IntervalTransfer` lambdas
  - src/Precept/Language/Functions.cs — `abs` / `pow` overloads and `ReturnNonnegative`
  - samples/production-order-tracking.precept — the corpus's flagship division shape and its authored workaround comment
  - samples/warranty-repair-request.precept — entry-hook and state-ensure syntax
  - samples/trafficlight.precept — exit-hook (`from S -> …`) syntax
  - research/architecture/compiler/proof-engine-interval-arithmetic-survey.md — Astrée's abstract-domain and floating-point soundness treatment
  - research/architecture/compiler/witness-checking-soundness-architecture-survey.md — certifying algorithms (CSR 2011), Verasco (POPL 2015), PCC/LCF
---

# The fault family's justification layer

**What this document is.** The obligation-discharge matrix requires every reasoning rule the proof engine uses to carry a *validity argument* — a short written case that following the rule cannot license a program that faults at runtime, judged against the evaluator's documented semantics. Seven such arguments exist today. An independent audit established that all seven were written for the constraint families, and that **no fault-prevention discharge path has a written argument at all**. This document designs the fault family's missing justification layer: what the arguments are, what each must establish, how the cell schema's closed argument list grows, and which questions a design pass may not settle.

**What it is not.** It is not an implementation plan. Implementation is parked. It also does not make the fault family ratifiable — § What this design does not deliver says exactly what still blocks it, and a substantial share of today's 175 `defined` fault cells move to `open` under this design's own checks.

**How to read it.** Every argument has a plain-English name, and that name — not a code — is the thing a cell cites and the schema stores. Short tags (`A0`, `P1`, `E3`) appear only inside the lattice table as a compact index; the prose always uses the names.

**A note on matrix line citations.** This document's `matrix:NNN` citations were written before the 2026-07-21 site-identity ruling was inserted at `obligation-discharge-matrix-2026-07-19.md:187`. That insertion added eleven lines, so every citation to a line **after** 187 is now eleven low — `matrix:339` (the soundness-correction class) is at `:350` today, `matrix:204` at `:215`, `matrix:192` at `:203`, `matrix:364` at `:375`. Citations at or before `:187` are unaffected. Text added or rewritten in this pass uses current line numbers; the pre-existing citations are left as written rather than silently renumbered, and re-anchoring them is a mechanical pass owed before promotion.

---

## Goal

When done, every fault-prevention discharge path the proof engine may use has a named, written truth-preservation argument stated against the evaluator's documented semantics; the cell schema mechanically checks that each fault cell cites a complete set of them and that no cell cites an argument outside its declared scope; and every path that **cannot** be argued today is recorded as such with the specific canon conflict or missing ruling that blocks it — demonstrated by a fault cell that fails validation for citing *Arg-bound interval arithmetic* on a division shape, which today passes.

## Scope

**In scope**

- The justification layer for the fault-prevention obligation family only — the family whose obligation is "the catalog-declared safety precondition at the evaluation site" (matrix:154).
- All thirteen `ProofRequirementKind` members and all fifteen `FaultCode` members, each given an explicit disposition.
- The mechanism by which the cell schema's closed validity-argument enum grows, and the scope-containment check that makes citation checkable rather than merely present.
- The class-(a) field-modifier burden and its relationship to the class-(d) induction.
- Whether literal constant-folding at a fault site is a licensed discharge path.
- Whether the fault family needs a transport rule written before its cells can ratify.

**Out of scope**

- The constraint families' seven existing arguments. They are not invalidated and not refactored here. One of them (*Guard normal-form match*) is shown to have a defect; that is routed, not fixed (§ Retired questions — the guard-argument soundness correction).
- Minting — which sites mint which obligations. The minting rule is matrix:171–188 and is amended only under matrix:187. This design consumes it.
- The certificate payload format, the `CertificateStepKind` / `CertificatePremise` vocabulary, and the certificate checker.
- Any code change. Implementation is parked.

**Deferred to future**

- Completing *Approximate-lane value entailment* — blocked on the `number`-lane conflict (§ Open questions, question 2).
- Completing *Pre-state constraint fact* — blocked on the premise-class question (§ Open questions, question 1).
- Completing *Collection-contents entailment* — no premise class ranges over a collection's current contents.
- Pinning the Layer-1 → Layer-3 interface to the `CertificatePremise` vocabulary — requires reading `certificate-steps-membership-2026-07-12.md`, which this pass did not read.
- Temporal-lane fault cells — no interval model exists for `NodaTime`-backed `Duration` / `Period`.

---

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention not detection | Y | Every fault discharge must name a written truth-preservation case, so an accepted program's safety rests on an argued rule rather than on strategy code nobody checked (matrix:194 — "the definition is therefore the trusted computing base, and it must argue its own soundness") | The design surfaces several paths that cannot be argued today; under prove-or-reject those become rejections, so prevention costs expressiveness now | Programs that compile today (number-lane divisions, `decimal *` nonzero chains) become rejections until the blocking rulings land |
| 2. One file, complete rules | N | The justification layer is about the compiler's own reasoning, not about where an author's rules live; nothing here adds an external fact source (spec §0.6 proof philosophy #4 — "All proof facts derive from the `.precept` definition") | N/A | N/A |
| 3. Deterministic semantics | Y | Each argument names a *decision procedure* — a finite check — and the approximate lane's argument requires outward-rounded endpoints computed deterministically, so two conforming compilers accept the same set (matrix:43 — "it pins the accepted set exactly, not as a floor") | An analyzer whose division arithmetic differs from the evaluator's would be nonconforming; that identity is asserted, not verified | We accept an unverified analyzer-matches-evaluator dependency, named inside each affected argument rather than hidden |
| 4. Full inspectability | Y | A certificate that cites *Approximate-lane value entailment* tells the reader, in the artefact, that this division's safety was decided under IEEE rounding (spec §0.1 #4 — "Inspectability extends to proof reasoning") | Nineteen argument names is more surface for a human reviewer to hold than seven | Accepted: the alternative (one name) makes the ratification gate a constant, which is the observed slice-3 failure |
| 5. Keyword-anchored readability | N | No token, keyword, or layout rule changes | N/A | N/A |
| 6. Explicit domain meaning | Y | Business-domain magnitudes ride the decimal clauses of *Exact-lane value entailment* because all seven use `decimal` backing (`primitive-types.md:251`); quantity/price carry an extra UCUM-normalization side condition | UCUM normalization's exactness is undocumented, so the side condition cannot be discharged today | Recorded as a doc obligation on `business-domain-types.md`, not papered over |
| 7. Compile-time-first static checking | Y | The layer is entirely compile-time; nothing is deferred (want:171 — "A business rule whose enforcement the compiler cannot prove complete is rejected — never handed to the runtime") | Overflow's model is unruled, so every arithmetic entailment row is conditional on a ruling that has not happened | We state the conditionality on each row rather than assert the rows unconditionally |
| 8. Approximation honesty | Y | The approximate lane gets its **own named argument** that every `number`-lane fault cell must cite, so the approximation is visible in the certificate and not inherited silently from an exact-lane paragraph (`philosophy.md:25` — "the contract must say so plainly so inspection, enforcement, and consuming code all operate against the same truth") | That argument cannot be completed: spec:110 forbids silent NaN while `primitive-types.md:650` says the lane inherits NaN | Every `number`-lane fault cell is `open` or `conflicted` until the `number`-lane conflict is ruled — including 100% of `sqrt` obligations |
| 9. Mandatory rationale (`because`) | N | No constraint surface changes; `because` interpolations are evaluation sites the transport rule classifies, but the `because` requirement itself is untouched (spec §0.1 #9) | N/A | N/A |
| 10. Totality | Y | The adequacy argument's whole job is to check that the stamped predicate actually makes the evaluator's fault unreachable, which is the Principle-10 claim itself (spec §0.1 #10 — "Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`") | Directly conflicted on the `number` lane (the `number`-lane conflict), and five adequacy gaps are found where a documented fault has no stamped precondition | Principle 10 is **not** delivered for the `number` lane today; this design says so rather than claiming coverage |
| 11. Static completeness | Y | The `FaultCode`-axis walk gives every one of the fifteen statically-preventable fault modes an explicit disposition, so a fault with no minting site is visible instead of invisible (`FaultCode.cs:9-55`; `soundness-and-coverage.md:187-204`) | Principle 11 cannot be claimed for arithmetic overflow (unruled model), the `number` lane (the `number`-lane conflict), or `FunctionArgConstraintViolation` (no machinery) | We state the principle as **not yet delivered** on those three axes rather than qualifying the principle to fit what is built |

**Rows with a live tension, stated as accepted tradeoffs.**

*Principle 8 and 10 (the `number` lane).* The design refuses to let a `number`-lane fault discharge borrow an exact-lane paragraph. That is the honest reading of approximation honesty, and it costs real programs: `sqrt` lives exclusively in the `number` lane (`primitive-types.md:284`), so every `sqrt` non-negativity obligation sits inside a blocked argument. The alternative — writing an argument that silently assumes finiteness — would be exactly the "silent approximation inside an exact-looking path" the philosophy names as the thing that weakens author reasoning.

*Principle 11 (completeness).* Three named holes stay open. Declaring them "conservative boundaries" would qualify the principle to fit the build; under prevention an unprovable case is a rejection or an open hole, never a skipped one. Recording them as open is what keeps delivered-versus-claimed honest.

*Principle 1 and 3 (rejections and the exact-power contract).* Narrowing the licensed set is the expensive half of matrix:339's soundness-correction class: it breaks certificate replay for affected cells. We accept that cost because the alternative — leaving *Arg-bound interval arithmetic* cited on the division shapes it explicitly disclaims — is a false proof with a paper trail.

**Companion commitments.** *Stateless-first-class*: unaffected — every argument here is stated over evaluation sites and write plans, and a stateless precept has both; the transport rule's site classification includes the stateless event hook as an ordinary in-plan case (spec:1971 lists "stateless event hooks" among the mutation surfaces). *Domain-expert-primary-author*: the layer is invisible to the author except through rejections. That is the pressure point — a narrower licensed set means more "cannot prove" diagnostics, and the suggestion schema is what keeps them teachable. § Audience and teachability carries the one error message the narrowing produces most often.

---

## Language Design Grounding

**This design introduces and modifies no language surface** — no token, keyword, type, operator, modifier, construct, accessor, or expression form. The section is therefore not triggered by guard 2, and is included only for the one thing that *is* author-visible.

What is author-visible is the **accepted set**: under matrix:43 the discharge contract "pins the accepted set exactly, not as a floor," so shrinking the argued scope shrinks what compiles. The field's name for this is the difference between an analysis's *soundness* and its *precision*, and the comparator that matters is treated in § Architecture Grounding rather than restated here, since the decision is architectural (where the justification lives) rather than syntactic (what the author writes).

`research/language/README.md` maps language-domain studies to expressiveness surveys; the relevant studies for this design are in `research/architecture/compiler/`, not `research/language/`, because the subject is the proof engine's internal justification structure. No gap in `research/language/` is implied.

## Audience and Teachability

**Not a language-surface change**, so guard 3 is not triggered. One paragraph anyway, because the design changes what the domain expert sees.

The only author-facing consequence is more rejections in two shapes: a nested divisor on an exact lane, and any fault site on the `number` lane. Both already have a respelling, and the corpus already shows an author reaching for it — `samples/production-order-tracking.precept:27-29` carries the annotation *"PlannedQuantity is initialized positive at construction and never changes; the proof engine cannot follow that across all transitions, so the divisions below are explicitly guarded"*, and `:142` is the guard.

The diagnostic wording the nested-divisor rejection needs:

The code is **PRE0083**, verified live at HEAD on 2026-07-21 by compiling the exact program below through `precept_compile`. Today's message is one line:

```
PRE0083  Division is unsafe: '(Produced / Planned)' can be zero on event
         'Recompute' from state 'Active'
```

The wording this design needs keeps that code and that first line, and adds the teaching body:

```
PRE0083  Division is unsafe: '(Produced / Planned)' can be zero on event
         'Recompute' from state 'Active'.

  set Ratio = 100 / (Produced / Planned)
                     ^^^^^^^^^^^^^^^^^^
  This divisor is itself a division. Dividing rounds — a quotient of two
  non-zero values can round to exactly zero — so bounds on Produced and
  Planned do not make the outer division safe.

  Guard the quotient directly:
      when (Produced / Planned) != 0
```

That wording names what the author wrote and what would fix it, and never says "interval", "entailment", or "lane". **But the suggested fix does not work at HEAD** — see § Tooling and verification state, which records the compile result and routes the gap. The teaching path is `docs/language/primitive-types.md § decimal` (the 28-digit note), then the guard rows of `samples/production-order-tracking.precept` — under five minutes.

---

## Legibility note

Three terms are load-bearing and are glossed once here rather than repeatedly below.

- **Weakest precondition** — the condition that must be true *before* a write plan runs for a constraint to hold *after* it. Used only when contrasting the constraint families' obligation with the fault family's.
- **Interval** — a `[low, high]` pair the compiler computes for an expression. "Sound" means the runtime value is always inside it.
- **Frame and kill** — plain names for two halves of one question: *frame* = "nothing in between touched this, so the fact survives"; *kill* = "something wrote it, so the fact is dead."

Argument names are the vocabulary. Where a compact tag appears (`P2`, `E1`), the name is always adjacent.

---

## Semantic Rules

This design adds no expression form and no typing rule. It states the **discharge rules** the proof engine may use for the fault family, and the soundness case for each. Notation below: `σ` is a runtime configuration; `⟦e⟧σ` is the value the evaluator computes for expression `e` in `σ`; `R(v)` is a stamped requirement's predicate applied to value `v`; `S` is an evaluation site; `π(S)` is the sequence of mutations the operation performs before `S`.

### The shape of a fault discharge

```
  adequacy(site, R)      provenance(φ, S)      transport(φ, π(S))      entail(φ ⊢ R)
  ─────────────────────────────────────────────────────────────────────────────────
                       discharge(site, R)  —  the evaluator's fault at site is unreachable
```

Read in words: the stamped predicate is *adequate* (proving it really does prevent the fault); some fact φ is *true at the site*; φ *survives* the mutations that ran before the site; and φ *implies* the predicate under the arithmetic the evaluator actually performs. All four premises are required; each is a separate named argument, and a defect in any one of them is a false proof.

### Rule 1 — Adequacy (Catalog precondition adequacy)

```
  ∀ σ.  R(⟦subject⟧σ)  ⟹  evaluator raises no fault of class F at this site
  ────────────────────────────────────────────────────────────────────────────
                       adequate(site, R, F)
```

Decided by a finite table walk over the **`FaultCode` axis** — fifteen rows (`FaultCode.cs:9-55`), each naming the evaluator's documented fault condition and the stamped predicate(s) claimed sufficient for it — not over the minting sites. A minting-site walk cannot find a fault that has no minting site, which is the exact failure mode this rule exists to catch.

### Rule 2 — Provenance

```
  Γ ⊢ φ  earned-at  S
```
where the earning judgment has exactly one form per provenance argument. The two forms that carry no authored premise are stated explicitly so the citation rule below is satisfiable:

```
  e is a literal representable in lane L        overload(f) declares ReturnNonnegative
  ──────────────────────────────────────        ────────────────────────────────────
     Γ ⊢ ⟦e⟧σ = denote(e)  earned-at  S            Γ ⊢ ⟦f(x)⟧σ ≥ 0  earned-at  S
        (Literal denotation)                       (Catalog-declared callee attribute)
```

### Rule 3 — Transport (Frame and kill across the operation's mutation prefix)

```
  Γ ⊢ φ earned-at plan-entry     deps(φ) ∩ targets(π(S)) = ∅
  ──────────────────────────────────────────────────────────
                      Γ ⊢ φ holds-at S
```

`π(S)` is the **operation's** mutation prefix, not the row's. Per spec:2117 an operation is: guard evaluation → exit actions → row mutations → entry actions → computed-field recomputation → constraint evaluation. `deps(φ)` is the read-closure of φ's mention set under matrix:176's three legs, closed under computed-field inputs (matrix:185) and including collection mutations (spec:254). A non-empty intersection means φ is **dead**, not merely unproven.

**This rule is stated about the wrong objects and owes a restatement** (recorded, not attempted here). The owner ruled on 2026-07-21, verbatim at `docs/Working/obligation-discharge-matrix-2026-07-19.md:187`: *"**Site identity — an evaluation site is an evaluation occasion, not a syntactic position** (owner ruling, 2026-07-21). One written expression reached by several routes mints one fault obligation *per route*, each proved from the facts that route establishes."* The rule above was written on the syntactic-position reading — `S` is a text position and `π(S)` is *the* mutation prefix of *the* operation containing it. Under the ruling neither is well-defined for an expression reached by more than one route. Precisely what a restatement has to change:

- **`π` takes a route, not a site.** An expression inside a residency ensure reached by two entering transitions has two prefixes; `π(S)` names neither. The transport premise becomes `deps(φ) ∩ targets(π(S, r)) = ∅` and must be discharged once per route `r`, not once per position.
- **"earned-at plan-entry" needs the same index.** There is no single plan entry for a multi-route occasion, so the provenance premise must be earned on the same route the transport premise is discharged on, or the two halves refer to different executions.
- **The citation rule's "exactly one transport name" reasoning is void.** It is justified below by *"every site has exactly one mutation prefix once the prefix is operation-scoped"* — which is exactly the syntactic-position assumption. Either the cell coordinate gains a route axis, or a cell carries one transport citation per route.
- **The cell coordinate follows.** Cells are keyed by syntactic position today; under the ruling the unit is the occasion, so keying is per (position × route).

Writing the replacement rule is design work and is deliberately not done here. Until it is done, every fault cell's transport citation rests on a rule the ruling has invalidated, and § What this design does not deliver carries this as an owed item.

### Rule 4 — Entailment

```
  Γ ⊢ φ holds-at S        α(φ) ⊑ domain      ∀v ∈ γ(α(φ)). R(v)
  ─────────────────────────────────────────────────────────────
                        Γ ⊢ R discharged-at S
```

`α` abstracts a concrete fact into the entailment vocabulary (an interval, a presence value, a declared attribute); `γ` concretizes back. The soundness condition the abstraction argument must establish is the standard one: `γ(α(φ)) ⊇ ⟦φ⟧` — the abstract fact admits every value the concrete fact admits. That direction is what makes an over-approximation safe; the failure mode it forbids is an abstraction that *narrows*, which would let a discharge exploit values the concrete fact never guaranteed.

The live instance is documented: `proof-engine.md:606` — *"`nonnegative` ⇒ lower bound `0`; `positive` ⇒ lower bound `0` (a sound over-approximation of `> 0`); `nonzero` contributes nothing (it is a hole at `0`, not a lower bound)."* An interval domain cannot represent a punctured set, so `nonzero` abstracts to ⊤. That is safe while it only weakens, and unsound the moment anything reads the interval as *the* fact — and `!= 0` is this family's dominant predicate.

### The citation rule (what a fault cell must cite)

Every fault-family contract entry cites:

- **at least one provenance name** (the premise-free names — *Literal denotation*, *Catalog-declared callee attribute*, *In-chain collection growth* — count, so the rule is satisfiable for discharges consuming no authored premise);
- **exactly one transport name** — but note the reason given for "exactly one" (*there is one, and every site has exactly one mutation prefix once the prefix is operation-scoped*) is **no longer sound**: under matrix:187's site-identity ruling an occasion has one prefix per route, so this clause is one of the items the transport restatement owes;
- **at least one entailment name** (not "exactly one": `IndexBounds` has two subjects over two domains, and a mixed-lane compound operand needs two lanes);
- **the conjunct-wise decomposition name** whenever the stamped predicate is a conjunction over more than one subject;
- **the fact-abstraction name** whenever a cited provenance fact is not already in the cited entailment argument's input vocabulary;
- **the adequacy name**, only if adequacy is put inside the validity-argument requirement — which canon's own scoping sentence (matrix:203) excludes by default, so this clause is dormant (§ Retired questions).

Plus a separate, mandatory obligation that is not a citation-shape check: a cell citing *Carried modifier fact* or *Pre-state constraint fact* must **name the establishing obligation and every preserving obligation**, each itself discharged. Without it the shape check passes a cell whose premise nothing earned, and matrix:94's verified false proof is reproduced under a new name.

### Soundness preservation claim

- **Principle 7 (compile-time-first static checking)** holds because every rule above is a finite check with a named decision procedure and no deferral: an undischarged obligation rejects. It holds *conditionally* on the deferred overflow model — every arithmetic entailment row is stated as conditional rather than asserted.
- **Principle 10 (totality)** holds on the exact lanes (`integer`, `decimal`, and the decimal-backed business magnitudes) because the entailment rows that survive are exactly those whose predicate is preserved under the evaluator's documented rounding. It **does not hold** on the `number` lane today: spec:110 forbids silent NaN and `primitive-types.md:650` says the lane inherits NaN, and no argument can reconcile them. That is the `number`-lane conflict, routed.
- **Principle 11 (static completeness)** holds for the ten `FaultCode` members dispositioned **in** by `soundness-and-coverage.md:187-204`, conditional on the adequacy walk finding the stamped predicates sufficient. It **does not hold** for `NumericOverflow`'s representable-range lane (deferred by build-order per `soundness-and-coverage.md:200`) or for `FunctionArgConstraintViolation` (`:196` — *"(no per-argument constraint machinery exists)"*). Four members — `TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, `FunctionArityMismatch` — are `explicitly-out` of the proof engine's surface by construction (`:205-209`) and are prevented by the type checker; they are named here so the axis stays exhaustive rather than silently short.

---

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The arguments are **prose in the matrix** (the definition layer); the argument *names* and their declared applicability scopes are **structured data in the cell schema** (the checking layer). Neither is compiler code and neither is a C# catalog entry today.

Why there and not adjacent: the matrix is the trusted computing base for this surface — matrix:194 states *"The certificate checker verifies that proofs replay the rules; nothing downstream verifies the rules themselves … The definition is therefore the trusted computing base, and it must argue its own soundness."* Putting the argument in compiler code would make the thing being justified also the justification. Putting it in a C# catalog is premature: matrix:331 already fixes the trigger — *"The generative layer enters the C# catalogs at constraint-obligation design time"* — and the constraint-obligation machinery does not exist (matrix:37: the requirement DU *"has no constraint-establishment or constraint-preservation subtype"*).

The *scope* declaration is data rather than prose because prose scope is not checkable and was measurably not checked: boundary:56 — *"Every cell in eleven of the nineteen files is a division shape … The cited authority explicitly disclaims the case it is cited for. Only two files disclosed this exclusion."*

**Cross-component propagation**

- **Runtime (parser, type checker, evaluator, diagnostics):** None. No obligation is added or removed; no diagnostic code changes. What changes is which discharges the *definition* licenses, which becomes a compiler change only when implementation resumes.
- **Tooling (syntax highlighting, completions, hover, semantic tokens):** None today. A later consequence, not part of this design: if hover ever surfaces proof attribution per spec §0.1 #4, the cited argument name is the natural provenance string.
- **MCP (vocabulary, DTOs, tool output):** None. `precept_compile`'s obligation projection is unchanged.
- **`tools/Precept.MatrixTools`:** Affected. A drift test (matrix prose ↔ schema enum) and a scope-containment check are added to the day-one cell validator matrix:330 already specifies.

**Breaking changes.** Yes, two, both inside the definition's own artefacts and neither on a shipped public surface:

1. `cell.schema.json`'s `contractEntry.validityArguments` changes from an array of strings to an array of `{name, scopeCheckedAgainst}` objects, and the fault coordinate object gains `lane`, `operatorClass`, and `requirementKind` fields. The coordinate object is `"additionalProperties": false`, so this is a migration touching all 202 existing fault cells.
2. Retrofitting declared scope onto the existing seven arguments **shrinks the licensed set** and therefore breaks certificate replay for affected cells. That is matrix:339's soundness-correction class and is routed to the owner (the scope-containment retrofit question), not shipped as part of the widening.

### External architectural precedent

**Astrée — the abstract-domain soundness discipline, and specifically the float/exact split.** From the in-tree survey [`research/architecture/compiler/proof-engine-interval-arithmetic-survey.md:230` — *"For floating-point arithmetic, Astrée models all rounding errors, accumulation effects, and the behavior of `−∞`, `+∞`, and `NaN` values through arithmetic and comparisons. The tool claims to be 'sound for floating-point computations.'"*; and `:221` — *"Astrée uses **abstract interpretation** with a combination of abstract domains … **Domain composition** — the domains are composed (reduced product) to improve precision while maintaining soundness."*].

What Precept takes: the separation of the *domain* from the *transfer functions* from the *concrete semantics they must be sound against* — which is exactly this design's provenance / transport / entailment split — and Astrée's insistence that the floating-point treatment be a modelled thing rather than an assumption. Astrée is a sound analyzer *because* it models NaN and infinity through arithmetic **and comparisons**, not despite them.

Where Precept diverges: Astrée resolves the exact/approximate question by *modelling* the approximate lane and staying sound over it. Precept cannot copy that move wholesale, because Precept has an additional commitment Astrée does not: Principle 8, approximation honesty, requires the line to be *visible in the public surface*. So Precept splits the argument by lane and makes every `number`-lane cell cite the approximate one by name, accepting a narrower accepted set in exchange for a certificate that says which arithmetic decided it. Astrée also has widening operators for loops; Precept has none and needs none (spec §0.4 property 1 — *"Expression trees are finite and acyclic. This eliminates the need for fixpoint computation and widening operators"*), which is why this design's transport rule is a single forward frame-and-kill pass rather than a fixpoint.

**Certifying algorithms — why the justification must be per-rule and independently checkable.** [`research/architecture/compiler/witness-checking-soundness-architecture-survey.md:48`, quoting McConnell, Mehlhorn, Näher, Schweitzer, *Certifying Algorithms*, Computer Science Review 5(2):119–161, 2011, DOI 10.1016/j.cosrev.2010.09.009, mirrored to `research/references/witness-checking/certifying-algorithms-mcconnell-mehlhorn-naher-schweitzer-csr2011.txt` — *"A certifying algorithm is an algorithm that produces, with each output, a certificate or witness … that the particular output has not been compromised by a bug."*] The matrix already adopts this frame for proofs. This design extends it one level up: the *rules* the certificate replays also need a justification, and matrix:196 grounds that in Event-B's published soundness meta-theory.

**Verasco — the precedent for splitting the justification by difficulty rather than writing one.** [same survey, `:84`, quoting Jourdan, Laporte, Blazy, Leroy, Pichardie, *A Formally-Verified C Static Analyzer*, POPL 2015, mirrored to `research/references/witness-checking/verasco-jourdan-laporte-blazy-leroy-pichardie-popl2015.txt` — *"This is, currently, the only instance of verified validation a posteriori in Verasco. While simpler abstract domains can be verified directly with reasonable proof effort, verified validation is a perfect match for this domain, avoiding proofs of difficult algorithms and enabling efficient implementations of costly polyhedral computations."*] Verasco verifies its simple domains directly and result-checks its hard one. The survey's own reading (`:90`) is *"verify the cheap/simple domains directly; result-check the hard ones with a certificate."* This design's analogue: argue the cheap paths (literal denotation, declared attributes, presence) directly and completely; record the hard ones (approximate lane, collection contents, the seam between fact and interval) as *named and blocked* rather than argued weakly. Writing a weak argument for a hard path is the failure Verasco's division of labour avoids.

---

## Inventory of what will be built

Nineteen named arguments across four layers, plus the schema and validator changes. Nothing in this inventory is code in `src/`.

### The lattice

**Layer 0 — Adequacy** (placement question retired — canon already scopes the validity-argument requirement; § Retired questions)

| Tag | Name | What it establishes |
|---|---|---|
| A0 | **Catalog precondition adequacy** | If the stamped predicate holds of the value the named subject resolves to, the evaluator raises no fault of the associated class at that operation — walked on the fifteen-member `FaultCode` axis |

**Layer 1 — Provenance: what makes a fact true at the site**

| Tag | Name | Premise class | Status |
|---|---|---|---|
| P1 | **Ingress-governed argument fact** | (b) | writable now |
| P2 | **Carried modifier fact by establishment and preservation** | (a) | writable as an argument; its premise is **not minted today** |
| P3 | **Fired-guard fact at an evaluation site** | (c) | writable now — three clauses, two exclusions |
| P4 | **Literal denotation at an evaluation site** | none | writable now — numeric clause and string clause |
| P5 | **Pre-state constraint fact at an evaluation site** | (d) | **not writable** — the premise-class question |
| P6 | **Catalog-declared callee attribute** | none | writable now — lane-discriminated (this is the fix for the `abs`-on-`number` defect) |
| P7 | **In-chain collection growth** | none | writable now — the fact is created by a prior action, not authored |

**Layer 2 — Transport**

| Tag | Name | What it establishes |
|---|---|---|
| T1 | **Frame and kill across the operation's mutation prefix** | Whether a fact earned at operation entry is still true at this site — indexed by the operation's mutation sequence (spec:2117), not by one row's action list. **Owes a restatement**: the 2026-07-21 site-identity ruling (matrix:187) makes the site an evaluation occasion, so the index is per route, not per position — § Semantic Rules, Rule 3 states what must change |

**Layer 3 — Entailment: why true facts imply the stamped predicate**

| Tag | Name | Requirement kinds closed | Status |
|---|---|---|---|
| E1 | **Exact-lane value entailment** | `Numeric`, `IntervalContainment` on `integer` / `decimal` / money / quantity / price | writable, **conditional on the overflow ruling** |
| E2 | **Approximate-lane value entailment** | same kinds on `number` | **blocked** — the `number`-lane conflict |
| E3 | **Derived-measure entailment** | `LengthContainment`, `CountContainment`, `Numeric` with a `count`/`length` accessor subject, the upper receiver of `IndexBounds` | writable — collection-count clause and string-length clause |
| E4 | **Presence entailment** | `Presence` | writable |
| E5 | **Declared-attribute entailment** | `Dimension`, `Modifier`, `QualifierCompatibility`, `QualifierChain`, `DimensionalProduct` | writable, one unclosed dependency |
| E6 | **Collection-contents entailment** | `KeyPresence` (both polarities) | **not writable** — no premise class ranges over contents |
| E7 | **Catalog interval-transfer entailment** | any `Numeric` / `IntervalContainment` whose operand is a call carrying an `IntervalTransfer` | writable, one instance already suspect |

**Composition**

| Tag | Name | What it establishes |
|---|---|---|
| C1 | **Conjunct-wise precondition decomposition** | A conjunction of independently-provable parts discharges when every part does — and the bright line against multi-hop combination |
| C2 | **Fact abstraction into the entailment vocabulary** | The provenance→entailment seam: `γ(α(φ)) ⊇ ⟦φ⟧`, per fact shape, with its loss stated |

### Coverage over all thirteen `ProofRequirementKind` members

Numeric → E1 / E2 / E3 / E7 · Presence → E4 · Dimension → E5 · Modifier → E5 · QualifierCompatibility → E5 · QualifierChain → E5 · IntervalContainment → E1 / E2 / E7 · LengthContainment → E3 · CountContainment → E3 · KeyPresence → E6 (blocked) · IndexBounds → C1 + E1 + E3 · DimensionalProduct → E5 · AssignmentQualifier → P3 + E5.

That last one is an adjudication, not a filing convenience. `ProofRequirementKind.cs:52` reads verbatim: *"an **open** field (no declared qualifier on Axis) assigned to a qualified target must, **under a guard**, narrow to the target's qualifier value on the axis."* There is no declared-source arm — its fact source is the handler guard, so it routes through the guard provenance argument and only its closure is declaration-shaped.

### Coverage over all fifteen `FaultCode` members

| `FaultCode` | Disposition here | Reaching argument |
|---|---|---|
| `DivisionByZero` | in | A0 + provenance + T1 + E1/E2/E7 |
| `SqrtOfNegative` | in, but **100% blocked** — `sqrt` is `number`-lane-only | E2 (blocked, the `number`-lane conflict) |
| `UnexpectedNull` | in | E4 |
| `CollectionEmptyOnAccess` | in for indexed/`.at`; `lookup for K` deferred per `soundness-and-coverage.md:197` | E3, C1; key-presence reads → E6 (blocked) |
| `CollectionEmptyOnMutation` | in | E3, P7 |
| `QualifierMismatch` | in | E5 |
| `OutOfRange` | in — the assignment/default containment family, subject is an **expression** not an operand | E1/E2 |
| `LengthBoundViolation` | in | P4 (string clause) + E3 (string clause) |
| `CountBoundViolation` | in | E3 (collection clause) + P7 |
| `NumericOverflow` | **split** — the declared-bound containment lane is in (E1); the representable-range lane is deferred by build-order (`soundness-and-coverage.md:200`) and is conditional on the deferred overflow model | E1, conditionally |
| `FunctionArgConstraintViolation` | **out of reach of every argument here** — no machinery exists; owner-fork per `soundness-and-coverage.md:196` | none — the `FunctionArgConstraintViolation` fork |
| `TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, `FunctionArityMismatch` | **explicitly out** of the proof engine's surface by construction — decided by the type checker / name binder before the proof engine runs (`soundness-and-coverage.md:205-209`) | none, correctly |

Stating the four `explicitly-out` rows is not padding: matrix:223 forbids silent cells (*"Pruned cells are written, not absent"*), and a fault axis that simply omits them cannot be checked for totality.

### The five adequacy gaps the `FaultCode` walk surfaces

Each is a finding, none is settled here, and none is reachable by a discharge argument — a *perfect* proof of the stamped predicate still admits the fault.

1. **Modulo has no documented divisor precondition.** `primitive-types.md:207` is `| integer % integer | integer | Remainder. |`; the decimal (`:240`) and number (`:275`) rows carry no note. The catalog nevertheless stamps one — `Operations.cs:133`, `:226`, `:257` all construct `NumericProofRequirement(…, OperatorKind.NotEquals, 0m, "Divisor must be non-zero")`. Documented semantics and catalog disagree. Per the rules of evidence the catalog is not design authority, so the disposition is **UNKNOWN** plus a doc obligation on `primitive-types.md`.
2. **Temporal divisors compare a `NodaTime` value against a decimal threshold.** `Operations.cs` stamps `NumericProofRequirement(ParamSubject(PDuration|PPeriod), NotEquals, 0m)`. A `Period`'s calendar components are variable-length; what "`Period != 0m`" means is not defined in `primitive-types.md`, `temporal-type-system.md`, or `proof-engine.md`. **UNKNOWN.**
3. **Overflow's representable-range lane has no stamped precondition.** boundary:38 — *"Overflow has no catalog-declared site whatsoever."* Canon splits the mode in two (`soundness-and-coverage.md:200`); the declared-bound half is stamped and live, the representable-range half is not.
4. **`maxplaces` is a value predicate with no requirement kind.** `primitive-types.md:249` — *"**Validation constraint, not auto-rounding.** `field X as decimal maxplaces 2` — assigning `1.999` is a constraint violation."* Decimal division at 28 digits routinely violates it: `samples/production-order-tracking.precept:33` declares `field YieldPercent as decimal default 0.0 nonnegative maxplaces 2` and `:144` assigns a quotient to it. `maxplaces` appears in no `ProofRequirementKind` member and in no `ProofSatisfactions` row (`proof-engine.md:782-793`, all ten rows read). Meanwhile spec:266 names *"no result outside a declared bound"* as a prevented fault class. **This is a concrete member of the residue the thirteen-kind coverage table cannot see** — which is exactly why the adequacy walk runs on the fault axis instead.
5. **The rounding functions have documented faults and no stamped precondition.** `primitive-types.md:658-664` tabulates `+∞ | Runtime fault — integer overflow` and `NaN | Runtime fault — NaN is not a number` for `floor`/`ceil`/`truncate`/`round`, and `:666` says *"Statically preventable when the proof engine can bound the expression range."* `Functions.cs` declares no `ProofRequirements` on any of them. These are also the **`number`→exact-lane bridge** — the one place non-finiteness escapes the approximate lane into `integer`/`decimal` — so the `number`-lane conflict is not confined to values that stay on the `number` lane.

Two further catalog inconsistencies the walk raises, both recorded rather than settled: `pow` declares its non-negative-exponent requirement only on the integer/integer overload (`Functions.cs:186-189`) while `primitive-types.md:683` lists `pow (integer exponent)` among the closed decimal-overload functions, so `pow(0, -1)` on the decimal lane is an unstamped division by zero; and `mid(s, start, length)` documents *"`start` and `length` must be positive `integer`"* (`primitive-types.md:679`) with no requirement declared.

### Schema and validator changes

- `cell.schema.json`: `validityArguments` items become objects; a `$defs/validityArgument` registry generated from the matrix carries each argument's declared applicability tuple; the fault coordinate object gains `lane`, `operatorClass`, `requirementKind`; premise class `"e"` is removed (it is live drift — `cell.schema.json:41-43` still calls it *"an open owner item"* while matrix:38's 2026-07-20 ruling retired it); the discharge `kind` enum gains `field-modifier` and `constraint-declaration` members (its current five have no member for either, which is why boundary:101 reports field-modifier discharges recorded as `arg-modifier` — under this design's reduction that conflation is precisely the one that must not persist); `openDependencies`' `"^[QS][0-9]+[a-z]?$"` pattern widens or the new dependencies get identifiers, since the overflow deferral, the totality conflict and the `decimal *` doc obligation cannot be recorded structurally today.
- `tools/Precept.MatrixTools`: a name-set drift test (matrix bold lead-ins ↔ schema enum, failing in both directions) and a scope-containment check.

---

## Decisions

### Decision 1: The fault family's justification is four layers of independently-cited arguments, not six parallel ones and not one

**Stakes**: high

- **Rationale**: The three questions the existing seven arguments answer at once — where a fact comes from, whether it survives to the site, and whether it implies the predicate — are orthogonal, and bundling them makes the arithmetic burden get restated per fact source and then drift. The drift is already measured in the repository: *Arg-bound interval arithmetic* (matrix:206) carries lane and division exclusions inside a class-(b) paragraph, while *Guard normal-form match* (matrix:204) covers the same arithmetic from class (c) with no lane caveat at all, even though a guard-sourced fact about a `number`-lane quotient has the identical rounding problem. Factoring states the rounding burden once. The fourth layer (adequacy) exists because a *perfect* proof of an inadequate predicate is still a runtime fault, and no premise class and no discharge argument can own that failure mode.
- **Tradeoff accepted**: Nineteen names against seven more than doubles a surface whose adequacy is reviewed by a human reading it (matrix:197 — argument *adequacy* is owner-read). The count is forced by the material (thirteen requirement kinds across six semantic domains, seven fact sources, one transport rule, two composition rules), but "forced" is not "affordable", and the design optimises against duplication — a machine's concern — at some cost to the thing that actually catches a bad argument, which is a person reading carefully.
- **Alternatives considered**:
  - *One argument per premise class (four).* Rejected: no home for adequacy, no home for composition, and the arithmetic burden must still be restated per class — the measured drift, unfixed.
  - *One argument per `ProofRequirementKind` (thirteen).* Rejected: the kinds classify *what is checked*, not *what makes the check sound*. `Numeric` alone spans divisor-nonzero, `sqrt` non-negativity, `pow` exponent and `count > 0` across three lanes plus five business-domain and two temporal operand types. And the axis is not total — boundary:35 records five of the thirteen kinds with no catalog site, covering 44% of minted obligations, and `maxplaces` (adequacy gap 4) has no kind at all.
  - *One argument with six legs.* Rejected: the schema's unit of citation is a name (`cell.schema.json:237`) and the gate checks presence per cited rule (matrix:197). One name makes every fault cell cite the same string, so the gate degenerates to a constant — the observed slice-3 failure.
  - *Extend the existing seven in place.* Rejected: re-scoping a licensed argument to cover cases its own text excludes redraws the licensed set, which matrix:339 reserves for soundness corrections. Adding names is monotone (matrix:338) and honest about which change is which.
- **Precedent**: Astrée's separation of abstract domain from transfer functions from concrete semantics, and Verasco's division of labour between directly-verified simple domains and result-checked hard ones (both quoted in § Architecture Grounding). Precept-internal: matrix:49 already carves this axis out, naming the fault family's site question *"a fault-family question about where values come from, not a decomposition of the preservation obligation."*
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:206` — *"the argument covers the exact lanes (`integer`, `decimal`) only … an RHS containing division rounds even in `decimal` (28-digit quotient, `primitive-types.md:239`), so division shapes sit outside this rule as stated."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:197` — *"no cell ratifies whose derivation cites an argument-less rule. The validator checks argument *presence* per cited rule; argument *adequacy* is reviewed like any generative sentence."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:56` — *"**Every cell in eleven of the nineteen files is a division shape** … The cited authority explicitly disclaims the case it is cited for. Only two files disclosed this exclusion."*
  - `docs/compiler/proof-engine.md:517` — *"**The proof engine does NOT maintain its own list of what needs to be proved.** Obligations are declared in catalog metadata and stamped onto typed expressions by the type checker"* — the sentence that makes adequacy a separate layer.
  - **Spec-first check**: grepped `precept-language-spec.md` §0.6 for a prior settlement of how proof rules are justified. §0.6's proof philosophy #3 settles *certificate* legibility (*"A proof strategy is admissible if and only if it satisfies all four criteria: (1) legible and independently re-checkable…"*) but says nothing about justifying the rules a certificate replays; that gap is what matrix:194 names. **Spec is silent on the decomposition** — legitimate design decision.
- **Strongest counter-evidence**: matrix:339 — *"Shrinking the licensed set is permitted **only** under [soundness-correction]. Always a major definition version; always breaks certificate replay for affected cells."* The scope-containment half of this design does shrink the set, so the "additions are cheap" framing is only half true. Response: the addition of names is genuinely monotone and shipped as a widening; the scope retrofit is separated out and routed to the owner as a soundness correction with boundary:56's measurement as the witness (the scope-containment retrofit question). The two halves are not bundled.
- **Reversibility**: `Hard`. Once cells cite names, re-decomposing means re-authoring every fault contract entry. Not `Effectively-irreversible` because nothing has shipped to an external author — the artefacts are internal to the definition.
- **Blast radius**: the matrix's § Validity arguments section; `cell.schema.json`; all 202 fault cells (re-authored by construction — see § What this design does not deliver); `tools/Precept.MatrixTools` validator; no `src/` catalog, no diagnostic code, no MCP surface.

---

### Decision 2: The adequacy argument is walked on the `FaultCode` axis, not on the minting-site axis

**Stakes**: high

- **Rationale**: A minting-site walk structurally cannot find a fault that has no minting site — and that is exactly the case for the representable-range overflow lane, for `maxplaces`, for the rounding functions' non-finite faults, and for `FunctionArgConstraintViolation`. The correct denominator is the closed fault registry, walked *toward* the sites. Canon already carries that walk: `soundness-and-coverage.md` §3.1 is a fifteen-row table dispositioning every member, and matrix:38 routes there explicitly. Anchoring adequacy on the catalog-site count (101, per boundary:19) would also be wrong by a second route: boundary:35 records that 44% of minted obligations come from kinds with no catalog site and are constructed in pipeline code, so the adequacy table must cover the seven dynamic minting mechanisms too.
- **Tradeoff accepted**: The fault axis is coarser than the site axis — one `FaultCode` row can cover many operations with different operand types (`DivisionByZero` spans three primitive lanes, five business-domain pairs and two temporal ones). So the table needs a per-operand-type sub-walk inside some rows, and that sub-walk has no independent enumeration pinning it. We accept a two-level table over a one-level table that is provably incomplete.
- **Alternatives considered**:
  - *Walk the 101 catalog-declared sites.* Rejected: incomplete by construction per boundary:35 and boundary:38, and pinned as incomplete by a test.
  - *Walk `ProofRequirementKind`.* Rejected: it enumerates what is checked, not what can go wrong; `maxplaces` and representable-range overflow have no member.
  - *Walk both and take the union.* Rejected as the primary form — it hides which axis is authoritative, and the union's totality argument would rest on the weaker axis. The site walk survives as a *cross-check* row inside each fault row, which is where it belongs.
- **Precedent**: `soundness-and-coverage.md` §3.1/§3.2 already runs both walks with a written disposition legend (`:172-177`) and an explicit note on why the four `explicitly-out` rows are listed at all. This design adopts that structure rather than inventing one.
- **Sources consulted for this decision**:
  - `src/Precept/Language/FaultCode.cs:3-7` — `public sealed class StaticallyPreventableAttribute(DiagnosticCode code) : Attribute` — and `:9-55`, fifteen members each carrying the attribute; the closed registry.
  - `docs/compiler/soundness-and-coverage.md:196` — *"`FunctionArgConstraintViolation` | … | *(no per-argument constraint machinery exists)* | **deferred** — owner-fork: remove-scaffolding vs. build."*
  - `docs/compiler/soundness-and-coverage.md:200` — *"the arithmetic-overflow (representable-range) lane is GATE-O, post-MVP … **The declared-bound containment obligation is in**; the overflow enforcement lane is deferred by build-order."*
  - `docs/compiler/soundness-and-coverage.md:205-209` — *"`TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, and `FunctionArityMismatch` are prevented, but *not by the proof engine* … They are `[StaticallyPreventable]` faults with live prevention; they are simply out of the proof engine's obligation surface by construction. Listing them keeps the axis exhaustive."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:38` — *"The fault family, by contrast, *is* catalog-enumerated (same DU; `docs/compiler/soundness-and-coverage.md` §3.1 carries the per-FaultCode enumeration)."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:35` — *"of 1,045 obligations HEAD minted across `samples/`, 464 — 44% — are of kinds with no catalog site."*
  - **Spec-first check**: grepped `precept-language-spec.md` §0.6/§0.7 and `proof-engine.md` for a prior settlement of the adequacy question — whether a stamped predicate is *sufficient* for the fault it claims to prevent. §0.7 states the composition (*"the compiler's proof that the operation cannot fault rests on the structural fact that the value carries a declared constraint"*) but assumes the carried constraint is sufficient rather than requiring it be shown. **Spec is silent on adequacy as a checkable property.**
- **Strongest counter-evidence**: matrix:192 scopes the validity-argument requirement narrowly — *"'Rule' in this section means a **generative rule of this definition** — a discharge contract, a step kind's recompute rule, a transport rule."* Catalog stamping is none of the three; it is a *minting* rule, and the minting section is never subjected to the requirement. Response: I do not override that scoping — where the adequacy argument attaches is routed to the owner (the adequacy-placement question), and the citation rule degrades cleanly if it is ruled out of the requirement. What the design does settle is only the *axis* the walk runs on, which is a question the matrix does not answer either way.
- **Reversibility**: `Easy`. The walk's axis is a property of one table; re-running it on a different axis is mechanical.
- **Blast radius**: the matrix's argument section; a new adequacy table; doc obligations on `primitive-types.md` (modulo divisor semantics, rounding-function preconditions, `maxplaces`'s proof status) and on `temporal-type-system.md` (what `Period != 0m` means). No cell coordinate changes.

---

### Decision 3 — **withdrawn**; moved to § Open questions as an owner-routed narrowing

Re-indexing transport from the row's action list to the whole operation's mutation sequence makes programs that compile today into rejections. That shrinks the licensed set, which matrix:350 reserves to the owner and permits **only** as a soundness correction. A design pass may not settle it. The full reasoning and every citation are preserved verbatim in § Owner-routed narrowings → **The transport index**.

---

### Decision 4: Literal constant-folding at a fault site is a licensed discharge path, cited to a new argument — not to the existing defaults argument

**Stakes**: medium

- **Rationale**: Two parts. *Licensed*: matrix:38/:39 draw an explicit definitional line — a **premise class** is "a fact the author wrote that a derivation consumes"; a **discharge mechanism** is one of "the named ways an obligation gets closed. They include constant folding…", listed with no family scope. A literal instantiates no premise class, so it is outside the domain of matrix:154's `(b)/(c)/(a)` column rather than omitted from it. The schema already says the same in its own words (`cell.schema.json:217`) and carries `default-constant-fold` in the discharge kind enum. And spec:205's *"proven-zero divisors are hard errors"* cannot be emitted without folding a literal divisor at a fault site. *New argument, not matrix:210*: the existing fold argument runs its truth-preservation through a **construction** step — *"construction builds the hollow version with defaults applied … so the runtime value of a field no construction row writes is the very literal the declaration states."* At a fault site the literal **is** the operand (`X / 2`, `X % 10`); there is no construction, no default environment, no unwritten-field carry. Citing it here would cite an argument whose load-bearing middle step does not occur.
- **Tradeoff accepted**: A second fold argument that looks superficially like the first invites a reader to treat them as duplicates and merge them later. The scope declarations (§ Decision 7) are what keep them apart mechanically.
- **Alternatives considered**:
  - *Cite matrix:210.* Rejected for the reason above — the scope error this whole design diagnoses.
  - *Treat literal-fold as unlicensed for faults pending a ruling.* Rejected: it would make `x / 2` unprovable and therefore rejected under spec:229's unresolved-is-rejected rule, which no reading of the philosophy supports; and it is the corpus's dominant path — corpus:16 records 18 of 30 divide-by-zero obligations discharging this way.
  - *Fold the literal's source text rather than its materialized value.* Rejected as unsound on the `number` lane — a literal below the smallest positive double materializes as `+0.0`, so "the text is not `0`" does not establish nonzero.
- **Precedent**: `cell.schema.json:289` already carries `default-constant-fold` in the discharge kind enum, and `proof-engine.md:765` documents `TryLiteralProof`'s deliberate scope — *"`TryLiteralProof` covers `NumericProofRequirement` only … Literal values never statically establish presence."* The mechanism exists and is scoped; what is missing is its argument.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:39` — *"**Discharge mechanisms** — the named ways an obligation gets closed. They include constant folding, interval derivation over declared bounds, guard matching and substitution, and — added by the 2026-07-20 ruling — **ingress evaluation**."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:210` — *"The `default` surface takes a literal value, and construction builds the hollow version with defaults applied (`precept-language-spec.md:2036`), so the runtime value of a field no construction row writes is the very literal the declaration states."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json:217` — *"Empty when the discharge needs no authored premise at all (constant folding over defaults)."*
  - `docs/language/primitive-types.md:230` — *"Exact base-10 fractional representation. No floating-point artifacts. `0.1 + 0.2 == 0.3` is true."*
  - `docs/language/primitive-types.md:265` — *"IEEE 754 double-precision floating point. Approximate — `0.1 + 0.2 ≠ 0.3`."*
  - `docs/language/primitive-types.md:686` — *"`integer` is fixed-width 64-bit."* (the representability side condition)
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/corpus-measurement-fault-family.md:16` — *"Of the 30 divide-by-zero obligations in the whole corpus, 18 discharge by folding a literal divisor — no authored premise at all."*
  - **Spec-first check**: `precept-language-spec.md:205` — *"Two-tier: proven-zero divisors are hard errors; divisors with no compile-time nonzero proof are obligation diagnostics"* — and `:213` — *"sharing one default environment and one constant-fold evaluator."* The spec settles that folding happens and that it is two-sided; it does not scope the mechanism by obligation family. **Spec is silent on family scope**, so this is a legitimate decision about which argument covers it, not a re-litigation.
- **Precedent for the string clause**: `ProofRequirement.cs:201-204` — *"a string value being assigned to a bounded string field must have its character length within the declared minlength/maxlength. **Conservative strategy: literal string assignments are checked statically**"* — and corpus:72, `LengthContainment | 298`, the single largest strategy in the corpus. The numeric-only fold argument would leave 28.5% of minted obligations discharging through a path with no name; the string clause closes that. Its named dependency is the code-unit/character question: `Functions.cs:263` calls `left` *"Leftmost N code units"* while `Types.cs` calls `.length` *"Character count"*.

---

### Decision 5 — **withdrawn**; moved to § Open questions as an owner-routed narrowing

Re-pricing a field modifier so that a declaration no longer discharges on its own — it must be established, preserved, and transported — shrinks the fault family's most-used premise. Same reason as Decision 3: matrix:350 reserves shrinking to the owner. The full reasoning and every citation are preserved verbatim in § Owner-routed narrowings → **The price of a field modifier**, which is to be ruled together with the premise-class question, since this document's own analysis couples them.

---

### Decision 6: Every `number`-lane fault path gets its own named argument, and that argument cannot be completed today

**Stakes**: high

- **Rationale**: Three findings force the split. (i) *Rounding*: every `number` operation rounds, so an interval computed with exact arithmetic need not contain the runtime result; the side condition is endpoints computed exactly then widened **outward** before comparison. (ii) *Underflow*: a product or quotient of nonzero finite doubles can round to `±0.0`, so "both operands nonzero ⇒ product nonzero" is sound on `integer` and **unsound** here. (iii) *Non-finite algebra*: `NaN` is unordered against every bound, so an interval that says `[a, b]` while the value is `NaN` is wrong **in the accept direction**. Governance does not exclude non-finiteness uniformly either — which modifier the author wrote determines what is ruled out (`min N` **and** `max M` excludes both `NaN` and `±∞`; `nonnegative`/`positive` exclude `NaN` and `−∞` but admit `+∞`; `nonzero` excludes **neither**, because `NaN != 0` is true). Since `proof-engine.md:786` maps `nonzero` to `Numeric(SelfValue, NotEquals, Constant(0))` with no lane discrimination, the catalog as documented licenses treating a `number nonzero` field as a safe divisor while it may hold `NaN`. And the whole argument then hits a canon conflict it cannot resolve (the `number`-lane conflict), so it is recorded as blocked rather than written weakly.
- **Tradeoff accepted**: 100% of `sqrt` non-negativity obligations sit inside the blocked argument (`primitive-types.md:284` — *"`sqrt()` lives exclusively in the `number` lane"*), so the unblocked scope of this design is materially smaller than the exact-lane coverage suggests. We take that over a completed-looking argument that assumes finiteness silently.
- **Alternatives considered**:
  - *Extend the exact-lane argument with a `number` clause.* Rejected: it reproduces exactly the bundling failure of matrix:206, and the approximation would be invisible in the certificate — a Principle 8 violation.
  - *Assume finiteness as a global side condition.* Rejected: unsound, per the finiteness-source table above, and it would make the design's own `abs`-on-`number` defect invisible again.
  - *Reject the `number` lane for fault-prone operations entirely.* Not chosen and not rejected — it is one honest closure of the `number`-lane conflict and is presented there neutrally.
- **Precedent**: Astrée is the direct comparator and does the opposite of assuming — `proof-engine-interval-arithmetic-survey.md:230`: *"For floating-point arithmetic, Astrée models all rounding errors, accumulation effects, and the behavior of `−∞`, `+∞`, and `NaN` values through arithmetic and comparisons."* A sound analyzer over IEEE models NaN through **comparisons**, which is precisely where Precept's `nonzero` modifier fails.
- **Sources consulted for this decision**:
  - `docs/language/primitive-types.md:265` — *"IEEE 754 double-precision floating point. Approximate — `0.1 + 0.2 ≠ 0.3`."*
  - `docs/language/primitive-types.md:277` — `| number == number | boolean | IEEE 754 comparison. |` — and `:278` — `| number < number | boolean | Orderable. |`
  - `docs/language/primitive-types.md:650` — *"IEEE 754 `double` can represent `+∞`, `-∞`, and `NaN`. Precept's `number` lane inherits these representations."*
  - `docs/language/primitive-types.md:658` — *"The proof engine may be able to prove non-finiteness is impossible for specific expressions, but the runtime must handle it defensively."*
  - `docs/language/primitive-types.md:284` — *"`sqrt()` lives exclusively in the `number` lane."*
  - `docs/compiler/proof-engine.md:786` — the `ProofSatisfactions` row `nonzero | Numeric(SelfValue, NotEquals, Constant(0))`, with no lane discrimination.
  - `docs/philosophy.md:25` — *"Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes … Precept therefore draws a hard line between exact and approximate behavior and requires that line to be visible in the type system and public surface."*
  - `src/Precept/Language/Functions.cs:80` — `new([new(TypeKind.Number, "value")], TypeKind.Number, ReturnNonnegative: true)` — the `abs` number overload that made a lane-free declared-attribute argument unsound.
  - **Spec-first check**: `precept-language-spec.md:110` (Principle 10) and §0.6's proof philosophy #1 were both checked for a prior settlement of the `number`-lane fault contract. Principle 10 forbids silent NaN; §0.6 #1 requires the safe direction. Neither states how the two reconcile with `primitive-types.md:650`. **Canon answers differently in two places** — that is a `conflicted` disposition per matrix:224, routed as the `number`-lane conflict rather than settled.
- **Strongest counter-evidence**: spec:110's own qualifier — *"Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable"* — read together with `primitive-types.md:658`'s *"the runtime must handle it defensively"*, suggests the two texts are compatible: the runtime is defensive, the compiler proves. Response: defensive redundancy is about *traps on unreachable paths*. In the case at hand nothing traps — a `number nonzero` divisor holding `NaN` passes ingress, the fact is earned, the entailment discharges, and `y / x` produces `NaN` which flows onward **as a value**. That is the silent-NaN case Principle 10 names, reached through a clean compile, not a trap firing.
- **Reversibility**: `Hard`. If the `number`-lane conflict is ruled by narrowing Principle 10, the approximate argument is completed and every `number`-lane cell moves from `open` to `defined` — a widening, cheap. If the `number`-lane conflict is ruled by redefining the modifiers to mean *finite and* the predicate, the finiteness-source table changes and both the argument and the affected cells are re-authored.
- **Blast radius**: every `number`-lane fault cell; all `sqrt` cells; the `float`-adjacent rows of the exact-lane argument's table; `primitive-types.md` (a number-literal parsing rule is owed, and the `decimal *` rounding note); potentially `philosophy.md` and spec §0.1 Principle 10 if the `number`-lane conflict is closed by narrowing — which is owner-only territory.

---

### Decision 7: The schema's closed argument list grows by generation-plus-drift-test, and citation is checked for **scope containment**, not only presence

**Stakes**: high

- **Rationale**: Presence checking is provably too weak. matrix:197 specifies presence, and boundary:56 measured the result: every citing file passed presence while eleven of nineteen cited an argument that disclaims their case in its own text. So each argument carries, as data beside its prose, a declared applicability tuple — `{layer, obligationFamilies, premiseClasses, requirementKinds, lanes, operatorClasses, sitePosition}` — and the validator rejects a citation whose cell coordinates fall outside it. *Arg-bound interval arithmetic* would declare `lanes: [integer, decimal]`, `operatorClasses: [+, −, *]`, and every division-shaped fault cell citing it would then fail **mechanically**. Growth of the name list follows the project's own catalog discipline: the matrix prose is the source, the schema enum is derived, and a drift test asserts set equality in both directions.
- **Tradeoff accepted**: The check can only be as good as the coordinates a cell records, and the fault coordinate object records none of the three axes the design's central distinctions turn on. `cell.schema.json:113-121` requires `caseShape, obligationFamily, operationKind, evaluationSiteCategory, typeFamily`, and `typeFamily` is `["primitive","temporal","business-domain","collection"]` — so `integer`, `decimal`, `number`, `string` and `boolean` are all one value and **the exact/approximate split is invisible to the validator**. `operationKind` and `evaluationSiteCategory` are free strings with no enum to check against, and there is no `requirementKind` coordinate at all. So the containment check is not merely a validator addition; it requires new coordinate fields on an object declared `"additionalProperties": false`, and a migration of all 202 cells. We accept that migration cost because a check that cannot see the lane cannot catch the measured failure.
- **Alternatives considered**:
  - *Hand-maintain the enum alongside the prose.* Rejected — a parallel copy of derivable content, the failure the catalog principle exists to prevent (CLAUDE.md: "Never hand-edit `tmLanguage.json`"), and matrix:329 already states the discipline for the sibling artefact.
  - *Keep presence-only checking and rely on owner review for scope.* Rejected on the measurement: nineteen independent authoring passes and nineteen independent verification passes did not catch it; a twentieth human read is not the control.
  - *Encode scope in prose and check it by review.* Rejected for the same reason — prose scope was already there and was already not checked.
- **Precedent**: `tools/Precept.MatrixTools` already carries this exact shape: boundary:19 records *"twelve tests in `test/Precept.MatrixTools.Tests/FaultAxisEnumeratorTests.cs`"* pinning the fault-axis enumeration's total, per-catalog split, per-kind split and eight representative rows. The drift test is the same pattern on a different artefact.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json:237` — *"Names are the bold lead-ins of the matrix's argument paragraphs, before any parenthetical; the enum mirrors that section and grows with it."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json:113-121` — the fault coordinate object's `required` list and `typeFamily` enum (the missing lane axis).
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json:219` — *"The named derivation that closes the proof. **Intended anchor once the rule-obligation catalog entries land: the CertificateStepKind / CertificatePremise vocabulary; free text until then.**"* — the derivation field is still free text, so a per-rule presence check has no rule identifiers to iterate; pinning argument names does not by itself make matrix:197's gate executable.
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/cell.schema.json:41-43` — premise class `"e"` still described as *"an open owner item"*, retired by matrix:38's 2026-07-20 ruling: live drift to fix in the same pass.
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:329` — *"**Human-readable cell tables are generated from the data** and never hand-edited (the `tmLanguage` discipline), drift-checked by a test."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:338` / `:339` — the two amendment classes, verbatim, which classify the two halves of this change differently.
  - **Spec-first check**: the canonical spec does not govern the cell schema; the governing text is the matrix's Storage and Amendments sections, both read in full. matrix:330 already tasks the day-one validator with *"a validity argument present for every rule the cell's derivation cites"* — presence. Extending to scope is an addition the matrix does not forbid and does not state.
- **Strongest counter-evidence**: matrix:339 — the retrofit shrinks the licensed set, so it is a soundness correction: *"Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness."* Response: accepted without qualification. The retrofit is **not** shipped as part of the name-list widening; it is routed as the scope-containment retrofit question with boundary:56's measurement as the witness. Naming and adding the arguments is monotone and proceeds; enforcing containment on the pre-existing seven waits for the owner.
- **Reversibility**: `Hard` for the coordinate additions (202-cell migration; `additionalProperties: false`). `Easy` for the drift test.
- **Blast radius**: `cell.schema.json`; all 202 fault cells plus every constraint-family cell that carries a coordinate object; `tools/Precept.MatrixTools` (validator + a new test file); the matrix's Storage section. No `src/` change, no diagnostic, no MCP surface.

---

### Decision 8: Fault cells cite at least one *provenance* name, and the premise-free provenance sources are named so the rule is satisfiable

**Stakes**: medium

- **Rationale**: A shape rule that cannot be satisfied by a discharge the engine actually performs is not a check, it is a trap. Three live discharge classes consume no authored premise: literal folding (283 corpus discharges under `Literal`), catalog-declared callee attributes (8 under `DeclarationAttribute`), and in-chain collection growth (1 under `CollectionGrowth`). Making the provenance layer a set of *fact sources* in the premise-class sense would leave all three uncitable. So the layer is defined as "what makes the fact true at the site", which admits premise-free sources as first-class members, and the schema already has the vocabulary for it (`cell.schema.json:217`: *"Empty when the discharge needs no authored premise at all"*). The self-discharging strategies that need no fact at all — qualifier compatibility (252) and dimensional product (8) — route to the declared-attribute entailment argument with the *catalog-declared callee attribute* provenance name, since their truth-maker is the declaration the type checker resolved.
- **Tradeoff accepted**: "Provenance" now covers two different kinds of thing — facts the author wrote and facts the language guarantees. That is a slightly looser concept than "premise class", and a reader could take it as licence to invent new premise-free sources. The mitigation is that the list is closed and each member carries its own argument.
- **Alternatives considered**:
  - *Require at least one provenance name only when a premise class is cited.* Rejected: it makes the shape check optional exactly where §Decision 6's `abs`-on-`number` defect lives, i.e. in the premise-free class.
  - *Add a fifth premise class for callee attributes and type-qualifier facts.* Rejected: matrix:38's *"There are **four**"* is a locked owner ruling, and these obligations need no *fact source* at all — only an entailment. Preserving the four unstrained is the cheaper and more accurate answer, and it disposes of the first half of boundary:79 without inventing surface.
  - *Let the entailment layer stand alone for premise-free discharges.* Rejected: the transport question still has to be answered for them (a callee-attribute fact about `abs(Score)` is about `Score`'s *current* value and is killed by a prior write to `Score`), and dropping provenance would drop the hook that makes that visible.
- **Precedent**: `proof-engine.md:628` already names in-chain growth as its own strategy — *"**CollectionGrowth (Strategy 11)** … discharges only collection `count > 0` obligations — from a prior grow in the action chain rather than a guard"* — so a premise-free, action-created fact source is already a shipped concept; it simply has no argument.
- **Sources consulted for this decision**:
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/corpus-measurement-fault-family.md:70-81` — the strategy table: `Literal | 283`, `QualifierCompatibility | 252`, `DeclarationAttribute | 8`, `DimensionalProduct | 8`, `CollectionGrowth | 1`.
  - `docs/compiler/proof-engine.md:777-778` — *"`abs(X)` is non-negative regardless of `X`, so `sqrt(abs(X))` does not require a user-declared `nonnegative` modifier"*, and *"`CollectionCountAccessor` uses this path because collection counts can never be negative."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:38` — *"**Premise classes** … There are **four**, all from the want doc (want :19)."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:79` — the type-qualifier question: *"Is there a fifth class, does class (a) widen beyond modifiers, or are these type-checking rather than proof surface?"*
  - **Spec-first check**: grepped `precept-language-spec.md` §0.6 and `proof-engine.md` for whether callee-return metadata is a premise. `proof-engine.md` documents it as a *strategy* (Strategy 2's two non-negative-result paths), never as a premise class. **Spec is silent**; the matrix's four-class list is the governing text and is preserved.

---

## Worked failures — the behavioral claims, with samples

Every sample below is grounded in verified `samples/` syntax (entry hook `to S -> set …` from `warranty-repair-request.precept:40`; exit hook `from S -> clear …` from `trafficlight.precept:29`; construction hook `on E -> set …` from `what-i-want-2026-07-16.md:49-51`).

### Tooling and verification state

An earlier revision of this document said `precept_compile` was non-functional (a `PRE0149 … Could not load file or assembly 'NodaTime'` on every input) and that nothing below had been validated. **That caveat is retired: the server works.** What has since been verified, and what has not, stated separately.

**Verified by live `precept_compile` at HEAD on 2026-07-21.** Every `.precept` sample printed in this document — the four fenced programs, both full programs exactly as printed, and the two fragments compiled inside a minimal enclosing definition — parses and type-checks clean. Verdicts below are read from returned diagnostic codes and obligation records, never from a prose summary, per matrix:375.

**Three results that change what this document can claim.**

1. **The nested-divisor rejection is real, and its code is `PRE0083`.** The program in § Audience and Teachability rejects with `PRE0083 Division is unsafe: '(Produced / Planned)' can be zero on event 'Recompute' from state 'Active'` — two occurrences, one per division. (An earlier revision printed `PRE0006`, which is the lexer's unrecognized-string-escape diagnostic; corrected.)
2. **This document's own recommended author fix does not work.** Adding `when (Produced / Planned) != 0` to that row leaves `PRE0083` firing on the outer divisor. The guard shape the diagnostic mockup tells the author to write does **not** discharge the obligation at HEAD. Shipping an error message that recommends a non-fix would be worse than shipping no suggestion. **This is an open design question the document does not answer**: is the whole-quotient guard shape intended to be licensed — i.e. is this a missing derivation (a power-widening, cheap under matrix:349) — or is the correct suggestion a different respelling entirely? Until that is answered, the suggestion text in § Audience and Teachability is unsupported.
3. **Two acceptance criteria predicted to pass do pass — but they cannot distinguish this design from a known bug.** Criteria 7 and 8 (§ Acceptance criteria) expected "accepts today", and all four probe programs do compile clean. But they discharge via `DeclarationAttribute` off a `nonzero` / `nonnegative` **qualifier modifier**, not off the guard or the transport path the design credits. Qualifier modifiers are the verified-defective path — boundary:111: *"Qualifier modifiers (`nonzero`, `positive`, `nonnegative`) mint no write-site obligation; bound modifiers (`min`, `max`) do."* So the accepts are explained by the known bug and say nothing about the transport rule. **Criteria 7 and 8 are not satisfied**, and must not be recorded as satisfied. What they *do* establish is recorded there.

**Still not verified.** The shipped analyzer's division arithmetic against the evaluator's; `certificate-steps-membership-2026-07-12.md`, still unread; and every corpus-wide claim carried from the boundary report, which is cited, not re-measured.

### Case 1 — the entry-hook transport hole (the defect the withdrawn Decision 3 addresses)

```precept
precept EntryHookTransport

field Divisor as integer default 5 nonzero
field Slots as integer default 0 nonnegative

state Warm initial
state Cold terminal

event Open initial
event Go

on Open
    -> set Divisor = 5

from Warm on Go when Divisor != 0
    -> set Divisor = 0
    -> transition Cold

to Cold -> set Slots = 100 / Divisor
```

The guard fires with `Divisor = 5`. The row writes `Divisor = 0`. The entry hook on `Cold` then divides by it. Under a **row-scoped** transport rule the hook is a different plan whose prefix is empty, so the guard fact transports and the cell ratifies — an accept-when-must-reject. Under the **operation-scoped** rule of Decision 3, the prefix contains the row's write to `Divisor`, the fact is dead, and the cell is `open`. The author's fix is to guard at the hook or to stop writing zero.

The mirror case uses the exit hook and breaks the *first-action-is-safe* assumption in the other direction:

```precept
from Warm -> set Divisor = 0

from Warm on Go when Divisor != 0
    -> set Slots = 100 / Divisor
    -> transition Cold
```

The guard reads the pre-state (`5`), the exit action runs first per spec:2117, and the row's first action divides by zero. A rule that classifies "first action's right-hand side" as having an empty prefix is wrong whenever an exit hook exists — and that same sentence appears verbatim in the **already-ratified** *Guard normal-form match* argument (matrix:204: *"for a single-write plan there are none"*). That is the guard-argument soundness correction.

**Verified at HEAD, 2026-07-21 — and the two halves behave differently.** Compiled with the modifiers stripped, so the guard is the only candidate premise: the **exit-hook mirror compiles clean**, divisor obligation `Proved`, `strategy: GuardInPath` — the guard fact really is transported across the exit hook's write, and the accepted program divides by zero. That is a genuine witness, and it is the one § Owner-routed narrowings uses. The **entry-hook case rejects** with `PRE0083 Division is unsafe: 'Divisor' can be zero in state hook for 'Cold'`; HEAD does not transport a row guard into a state hook at all. As printed *with* the `nonzero` modifier, both halves compile clean — but off `DeclarationAttribute`, i.e. off the qualifier-modifier defect, not off transport. So case 1's entry-hook program is a witness for the modifier price, not for the transport index; only the mirror witnesses the transport index.

### Case 2 — declared-attribute entailment on the approximate lane (the defect Decision 6 fixes)

```precept
precept NumberLaneEntailment

field Score as number default 0.0
field Root as number default 0.0

state Active initial

event Open(Value as number) initial
event Recompute

on Open
    -> set Score = Open.Value

from Active on Recompute
    -> set Root = sqrt(abs(Score))
    -> no transition
```

`Open.Value` is an unconstrained `number`, so it may be `NaN` (`primitive-types.md:650`). `abs(NaN) = NaN`, `sqrt(NaN) = NaN`, and `Root` holds `NaN` — the silent NaN Principle 10 names. The catalog declares `ReturnNonnegative: true` on the *number* overload of `abs` (`Functions.cs:80`), so a lane-free declared-attribute argument discharges it. Decision 6 splits the provenance (catalog-declared callee attribute) from the entailment (which is lane-discriminated), so this routes to the blocked approximate-lane argument and the cell is `open`. Note the mechanical scope check alone would **not** catch this: a declared-attribute argument would honestly declare all three lanes, because it genuinely is lane-independent for `Dimension` and `QualifierCompatibility`. The fix has to be the decomposition, not the tuple.

### Case 3 — the nested divisor on an exact lane (what the rounding table costs)

```precept
from InProduction on ProduceBatch when PlannedQuantity > 0
    -> set ProducedQuantity = ProducedQuantity + ProduceBatch.Quantity
    -> set YieldPercent = ProducedQuantity * 100.0 / PlannedQuantity
```

This is `samples/production-order-tracking.precept:142-144` verbatim, and the design licenses it: the guard fact's mention set is `{PlannedQuantity}`, the prefix's write is to `ProducedQuantity` (the `set ProducedQuantity = …` on `:143`), the intersection is empty, transport applies, and the leaf entailment closes. What the design does **not** license is the nested form `100 / (A / B)` on any exact lane — because `1 / 2 == 0` on `integer` (`primitive-types.md:206`, *"Integer division (truncates)"*) and because a small enough decimal quotient flushes to exactly zero at 28 digits. The respelling is `when (A / B) != 0`.

### The rounding results in one table

| Operand shape | `>= 0` / `<= 0` | `> 0` / `< 0` | `!= 0` |
|---|---|---|---|
| leaf (field / arg / literal) | sound | sound | sound |
| `+ −` on `integer` or `decimal` | sound¹ | sound¹ | sound¹ |
| `*` on `integer` | sound¹ | **excluded** — see below | **excluded** — see below |
| `*` on `decimal` | sound¹ | **excluded** | **excluded** |
| `/` on `integer` | sound (truncation is sign-preserving) | **unsound** (`1/2 == 0`) | **unsound** |
| `/` on `decimal` | sound (rounding is sign-preserving) | **unsound** (flush to zero) | **unsound** |
| `%` any lane | **excluded** — remainder sign convention undocumented | excluded | excluded |
| anything on `number` | **blocked** (the `number`-lane conflict); with finiteness, needs outward-rounded endpoints | blocked | blocked — underflow kills it even with finiteness |

¹ conditional on the deferred overflow model (`primitive-types.md:686`).

Three notes the table compresses:

- **`decimal *` is excluded, and the reason is stronger than "undocumented".** `primitive-types.md:238` is `| decimal * decimal | decimal | |` — an empty notes cell — while `:239` fixes the ceiling at *"Full 28-digit precision."* A decimal carries a scale of at most 28, so `0.0000000000000000000000000001 * 0.1` has true value `1e-29`, is not representable, and rounds to exactly `0`. Two nonzero decimals can multiply to zero. Excluded, and the doc note is owed.
- **`integer *` is excluded on the same evidence, applied consistently.** `primitive-types.md:205` is `| integer * integer | integer | |` — an equally empty notes cell — while `:203` gives `+` the note *"Checked overflow"* and `:686` says the overflow model is *"not yet ruled"*. Under wrapping semantics `4294967296 * 4294967296` is exactly `0` and the enclosing division faults; under checked semantics it throws — a runtime fault from a program the compiler accepted. **Both readings make the accept wrong**, so a footnote deferring to the overflow model cannot carry the row. Excluded until the overflow model is ruled.
- **`maxplaces` is not a rounding source** (`primitive-types.md:249` — *"Validation constraint, not auto-rounding"*), but it *is* a value predicate with no requirement kind and no entailment argument. That is adequacy gap 4, not a row in this table.

---

## Falsifiers

Guard 7 applies: the design narrows which programs compile, which is behaviour external authors see.

1. **If, after the arguments are written, more than one in five sound fault-prone programs in `samples/` has no licensed respelling**, the exclusion set is over-strict and the exact-lane argument's division rows should be revisited (probably by licensing a bound-away-from-zero interval rather than the point exclusion). matrix:350 makes this a hard ratification gate, not an optional check.
2. **If a fault cell can be constructed that passes the scope-containment check and still licenses a runtime fault**, the tuple's axes are the wrong ones and the check is theatre. Case 2 above is the near-miss: it fails only because of the decomposition, not the tuple.
3. **If the drift test between matrix prose and the schema enum cannot be written without a fragile regex** — i.e. if the bold-lead-in convention (`cell.schema.json:237`) does not survive the arguments actually being written — the "prose is the source, enum is derived" mechanism is wrong and the names should live in data with the prose citing them.
4. **If writing the operation-scoped transport rule requires a third index** (beyond write plan and operation mutation sequence) to state the declaration-position sites, the transport rule is doing too many jobs and should split into an in-operation rule and a declaration-position rule.
5. **Site identity is now ruled toward "evaluation occasion" (matrix:187), and the transport rule as written survives unchanged.** By this falsifier's own terms that is evidence the rule was underspecified — a correct rule should be sensitive to what a site *is*. The falsifier has fired: § Semantic Rules, Rule 3 records the restatement it owes.

---

## Acceptance criteria

Test-shaped. "Cell validator" means the day-one validator matrix:330 specifies, extended per Decision 7.

1. **Name-list drift, both directions.** A test in `test/Precept.MatrixTools.Tests/` extracts bold lead-ins from the matrix's § Validity arguments and asserts set equality with `cell.schema.json`'s validity-argument registry. Adding an argument to the prose without registering it **fails**; registering a name with no argument paragraph **fails**.
2. **Scope containment rejects the measured failure.** A fixture cell with `operatorClass: "/"` and `validityArguments: ["Arg-bound interval arithmetic"]` **fails validation**, naming the lane/operator mismatch. The same cell citing the exact-lane argument on a `+` shape **passes**. (Today the first case passes — that is boundary:56.)
3. **Citation shape.** A fault contract entry citing zero provenance names **fails**; one citing only *Literal denotation* + transport + an entailment name **passes** (the premise-free arm of Decision 8). A cell whose stamped predicate is `IndexBounds` and which cites no conjunct-wise decomposition name **fails**.
4. **Earned-premise citation.** A cell citing *Carried modifier fact* without naming an establishing obligation and every preserving obligation **fails**. Constructing matrix:94's false-proof shape as a cell **fails validation** rather than ratifying.
5. **Fault-axis totality.** A test asserts every one of the fifteen `FaultCode` members appears exactly once in the adequacy table with a disposition drawn from `{in, explicitly-out, deferred, unknown}`; adding a sixteenth member to `FaultCode.cs` **fails** the test until dispositioned.
6. **Requirement-kind totality.** A test asserts every one of the thirteen `ProofRequirementKind` members is reachable by at least one entailment argument or is explicitly recorded as blocked with a named blocker.
7. **The transport probe compiles and is verdicted from diagnostic codes.** Case 1's entry-hook program and its exit-hook mirror are compiled through `precept_compile`; the verdict is read from diagnostic codes and obligation records per matrix:364, never from a prose summary. Expected today: **accepts** (the defect). This is the witness program matrix:198 requires before the soundness correction of the guard-argument soundness correction can be routed with evidence. — **Run 2026-07-21. NOT satisfied.** Both programs as printed accept, but with `strategy: DeclarationAttribute`: the accept comes off the `nonzero` qualifier modifier, which is the separately-verified defective path (boundary:111), not off transport. The criterion cannot discriminate the transport rule from the known bug and must not be marked passed. Re-run with the modifiers stripped **does** discriminate: the exit-hook mirror accepts with `strategy: GuardInPath` (a real transport witness), and the entry-hook program **rejects** with `PRE0083`. The criterion is therefore restated as: *the modifier-free mirror accepts via `GuardInPath`* — which is the form that must be carried to the owner as the witness program.
8. **The finiteness probe.** Case 2's program is compiled and verdicted the same way; and a `field D as number nonzero` divisor program is compiled to confirm whether the `nonzero` modifier is treated as excluding `NaN`. Expected today: **accepts** both. — **Run 2026-07-21. NOT satisfied.** Both accept, as predicted, but neither accept is attributable to the design's claim. Case 2 discharges `Proved / DeclarationAttribute` off `abs`'s catalog `ReturnNonnegative` attribute; the `number nonzero` divisor discharges `Proved / DeclarationAttribute` off the qualifier modifier — again the defective path. The probes confirm the *reachability* of the `number`-lane hole (a `nonzero` divisor that may hold `NaN` compiles clean) and nothing about the lane decomposition. Not marked passed.
9. **Respellability is measured, not asserted.** Per matrix:350, the exact-lane and derived-measure arguments' contracts are run against the sample corpus and every fault obligation is classified licensed-as-written / needs-a-respelling / no-licensed-respelling, before either argument's family verdict ratifies.
10. **Doc-sync.** Each of the five adequacy gaps has either a canon statement resolving it or an `Open questions` entry in the owning type doc; a test is not possible here, so this is a `/promote`-time checklist item.

Criteria 7 and 8 **have now run** — `precept_compile` works, and § Worked failures → Tooling and verification state records what the runs showed. Both are recorded **not satisfied**: the programs accept, but off the qualifier-modifier path, so neither criterion can distinguish this design from the verified defect. One further criterion is added by those runs:

11. **The suggested author fix actually discharges.** The guard shape § Audience and Teachability tells the author to write — `when (A / B) != 0` around a nested divisor — must make the program compile. **Run 2026-07-21: it does not.** `PRE0083` still fires on the outer divisor. Until the licensing question in § Worked failures → Tooling and verification state is answered, the diagnostic's suggestion text is unsupported and must not ship.

---

## Dependencies

**Upstream — must be in place first**

- Owner rulings on the two questions that block an argument outright: the premise-class question (blocks *Pre-state constraint fact*) and the `number`-lane conflict (blocks *Approximate-lane value entailment*). Adequacy's placement is no longer among them — canon already scopes the requirement (§ Retired questions).
- Owner rulings on the two withdrawn narrowings — the transport index and the price of a field modifier — both routed with compiled witnesses (§ Owner-routed narrowings).
- **A restatement of the transport rule** against the ruled site identity. matrix:187 makes the site an evaluation occasion, so the rule as written indexes the wrong objects; § Semantic Rules, Rule 3 enumerates what must change. This is design work, not cleanup, and it is upstream of every fault cell's transport citation.
- The denominator decision: the scope-containment check runs against cell coordinates, and it cannot be total over a coordinate space that is not in the repository.
- **Satisfied**: a working `precept_compile`. The tool is restored and the probes have run; see § Worked failures → Tooling and verification state.

**Downstream — what this enables**

- Slice 4 of the matrix population, which boundary:175 blocks on exactly the two questions this design's premise-class question and its argument set address.
- The `convert-cells` test-generation path for the fault family — though not on its own: boundary:138 records zero executable rows for all nineteen fault families because no canonical weakest-precondition key exists for a catalog-stamped precondition. A justification layer does not produce one.
- The eventual C# catalog entries for the generative layer, whose trigger matrix:331 already names (constraint-obligation design time).

---

## Doc-update enumeration

Per the CLAUDE.md routing table.

| Doc | What changes |
|---|---|
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Validity arguments | The nineteen arguments, written; each with its decision procedure and named dependencies |
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Storage | The scope-declaration data and the drift test added to the day-one validator's list |
| `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Per-family case shapes (the fault row, matrix:154) | The discharge column's clarification that premise-free mechanisms are outside its domain — **owner's edit**, flagged not made |
| `docs/Working/…-cells/cell.schema.json` | Argument registry; `lane` / `operatorClass` / `requirementKind` coordinates; discharge-kind enum members for field modifier and constraint declaration; premise class `"e"` removed; `openDependencies` pattern widened |
| `docs/compiler/soundness-and-coverage.md` § 3.1 / § 3.2 | Cross-reference to the adequacy argument; the five adequacy gaps recorded as re-open triggers where they are not already |
| `docs/compiler/proof-engine.md` § Strategy 2 | Doc-drift correction: `proof-engine.md:606`'s "genuinely lies within these bounds" describes an intended invariant that nothing currently mints |
| `docs/language/primitive-types.md` | Five doc obligations: `decimal *` and `integer *` rounding/overflow behaviour; the modulo divisor precondition; the number-literal parsing rule; `maxplaces`'s proof status |
| `docs/language/temporal-type-system.md` | What `Period != 0m` / `Duration != 0m` means, or that the comparison is not defined |
| `docs/language/business-domain-types.md` | Whether UCUM base-unit normalization is exact (the extraction-is-exact statement at `:426` covers extraction, not conversion) |
| `docs/language/collection-types.md` | Cross-reference from the `mincount 1` accessor-safety sentence to its establishment obligation, once the premise-class question is ruled (the `mincount 1` case falls out of it) |
| `docs/language/precept-language-spec.md` § 0.6 | If the `number`-lane conflict is closed by narrowing Principle 10, the totality statement and §0.6's implementation notes both move — **owner-only** |
| `tools/Precept.MatrixTools` + `test/Precept.MatrixTools.Tests/` | Drift test, scope-containment check, fault-axis and requirement-kind totality tests |

---

## Operational dimensions

**Security** — N/A. The design touches no source-text ingestion surface; no lexer, parser or MCP input path changes.

**Observability** — Affected. The named argument is the natural provenance string for proof attribution: spec §0.1 #4 requires that *"proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling"*, and a certificate that cites *Approximate-lane value entailment* by name is how an author learns that a particular division's safety was decided under IEEE rounding rather than exact arithmetic. Nothing in this design ships that surfacing; it makes the string exist.

**Evolvability** — Required and addressed; the design depends on three external standards.

- **IEEE 754.** The approximate-lane argument is stated entirely against IEEE double semantics (`primitive-types.md:265`, `:650`). IEEE 754 is stable and its 2008/2019 revisions did not change double-precision arithmetic, rounding-mode defaults, or NaN comparison semantics. Version pinning is implicit in .NET's `double`. What would break the argument is not the standard changing but the *host* changing rounding mode; the argument must therefore state round-to-nearest-even as an assumption rather than assume it silently.
- **UCUM.** The exact-lane argument's quantity/price clause depends on base-unit normalization being exact (`proof-engine.md:602`). UCUM revisions add and retire unit codes; a retired code changes which programs type-check, not the exactness of the arithmetic. The migration story is the existing one for `business-domain-types.md` and is not made worse here — but the *exactness* claim is currently undocumented and is listed as a doc obligation.
- **NodaTime.** Temporal-lane fault cells are `open` in this design partly because `Duration` and `Period` have no interval model and partly because the stamped `!= 0m` comparison is undefined against a `NodaTime`-backed value. Any NodaTime major version that changes `Period` normalization would change what that comparison means — which is an argument for defining it in Precept's own terms rather than inheriting it.

---

## Open questions

**Three.** An earlier revision listed twelve. An independent triage found that only four of the twelve genuinely needed an owner ruling, and one of those four — site identity — was ruled on 2026-07-21 and is now propagated through the document rather than asked here. The eight that do not need a ruling are recorded at the end of this section with the canon text that retires each, so nobody re-opens them.

Each question below is written to be rulable on its own, without reading the rest of this document. After them: two decisions **withdrawn** from § Decisions because they narrow what compiles, which matrix:350 reserves to the owner.

### Question 1 — May a declared constraint or field modifier be used as a fact when proving that a fault cannot occur, and at what price?

**Plain statement.** When the compiler proves a division safe, may it use "the file declares `rule PlannedQuantity > 0`" or "the field is declared `nonzero`" as a fact? And if yes, must the proof also show that the declared thing is actually established at construction and preserved at every write site that could break it?

**The conflicting texts, verbatim.**

- `docs/Working/obligation-discharge-matrix-2026-07-19.md:154` (the fault family's row, discharge column): **"premise classes (b)/(c)/(a) per catalog `ProofSatisfactions`; want :104"** — the pre-state-constraint class, (d), is absent.
- `docs/Working/what-i-want-2026-07-16.md:104`: **"**`PlanRepayment`** — the fault family. Business rules are not the only obligations: faults (division by zero, overflow, out-of-range) get the identical premise-and-certificate treatment."**
- `docs/language/precept-language-spec.md:256`: **"A declared **unconditional** field-to-field rule (`rule X >= Y because "…"` and its `>`/`<=`/`<` siblings) now participates in proof exactly as a same-row `when X >= Y` guard does: it contributes a provable bound to the subject's interval and discharges a downstream fault-prone obligation."** Recorded fairly: this describes implementation state, not an independent normative rule — but it is canon asserting class-(d) fault discharge as intended surface.

**The option set is not symmetric, and this is the load-bearing point.** Admitting the premise class costs one ruling and no doc corrections. Excluding it costs **two canon corrections**, because the matrix's omission is not an independent settlement — it is the matrix drafting against the want doc. `docs/Working/obligation-discharge-matrix-2026-07-19.md:22`, verbatim: **"**During drafting: the want doc defines; the matrix drafts against it.** Every defined cell's answer must be derivable from the want doc plus this document's own generative sections. A conflict between a cell and the want doc routes to the owner — it is never resolved silently in either direction."** So a ruling that excludes the class must correct `what-i-want-2026-07-16.md:104` ("identical premise-and-certificate treatment") **and** `precept-language-spec.md:256` ("discharges a downstream fault-prone obligation"). Excluding is the expensive direction on paper cost as well as on expressiveness.

**The price, if it is admitted.** `docs/Working/obligation-discharge-matrix-2026-07-19.md:92`, verbatim: **"Using a constraint as a fact is paid for by establishing it at construction and preserving it at every write site of every field it mentions (the validity argument for premise (d) states this dependency). If one site is missed, the fact is unearned, and every proof anywhere that consumed it is void — while each of those proofs remains individually valid and replayable. The failure is silent, non-local, and looks exactly like success."**

**What the analysis contributes, neutrally.** The withdrawn Decision 5 (below) shows that class (a) — the field modifier, which matrix:154 *does* admit — is earned by exactly the induction (d) rests on. So the listing does not keep the induction out of the fault family; it admits it through a narrower door. A ruling that excludes (d) while admitting field-(a) buys no soundness, only less expressiveness. That is a constraint on the option set, not a recommendation.

| | **Option 1 — (d) available at the locked price** | **Option 2 — (d) excluded, field-(a) narrowed to match** | **Option 3 — (d) and field-(a) available only under a local citation obligation** |
|---|---|---|---|
| What it says | (d) joins the fault row; field-(a) is explicitly priced identically per matrix:92 | (d) stays out; class (a) restricted to **arg** modifiers and to fields whose entire write-site set is a governed ingress door | Option 1 plus: no derivation may cite (d) or field-(a) unless the certificate **names** the establishing obligation and every preserving obligation, each discharged |
| Cost | Every (d)- and field-(a)-citing discharge inherits matrix:88's unresolved hole — *"nothing detects an obligation that was never minted"* — and matrix:94's verified false-proof shape becomes licensed by the definition, its soundness resting on preservation machinery matrix:37 says has no catalog subtype | Large expressiveness loss on the family's most-used path (boundary:57). The corpus already shows the forced shape: `samples/production-order-tracking.precept:27-29` is an author's own annotation about guarding around a fact the engine cannot follow — that becomes permanent, not a workaround. Also conflicts with spec:256 **and** want:104, requiring canon corrections in both | Certificate-format change and a definition-version bump; certificates grow; and the citation set must be derived **independently of the minting code** or the check is circular (matrix:98) |
| Consistency with want:104 | full | direct conflict — needs a want-doc correction | full |
| Unblocks | the ~40 (d)-citing cells (boundary:63) plus the majority citing (a) | nothing new; forces re-authoring of most `defined` cells | same as Option 1, and structurally excludes matrix:94's false proof rather than deferring it |

**Coupling.** Whichever option lands also rules field-(a), so this question and the withdrawn Decision 5 are one ruling, not two. The *carried modifier fact* argument as written is compatible with Options 1 and 3 and **incompatible with Option 2** — under Option 2 it is rewritten and narrowed to ingress-only fields. Independent of the ruling: the six `CompositionalConstraint` corpus discharges (corpus:28) stay unverified, because they run through the strategy the verified defect makes unsound.

### Question 2 — The `number` lane: two canon texts answer the same question differently

**Plain statement.** Can a value on the `number` lane be `NaN` or infinite in a program that compiles clean? One numbered principle says no; the type doc says the lane inherits IEEE's representations. Both are canon. No validity argument can be written for any `number`-lane fault discharge until one of them moves.

**The conflicting texts, verbatim.**

- `docs/language/precept-language-spec.md:110` (Principle 10, Totality): **"Every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`. The evaluation surface has no undefined behavior. For any expression that *could* fault at runtime — division by zero, overflow, empty collection access — the compiler must either prove safety or emit a diagnostic requiring the author to supply constraints that make safety provable. A precept that compiles without diagnostics has no unproven arithmetic or access faults. Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable."**
- `docs/language/primitive-types.md:650`: **"**Non-finite `number` inputs:** IEEE 754 `double` can represent `+∞`, `-∞`, and `NaN`. Precept's `number` lane inherits these representations."** And `:658`: **"The proof engine may be able to prove non-finiteness is impossible for specific expressions, but the runtime must handle it defensively."**

**Live evidence that the conflict is reachable, not theoretical** (compiled at HEAD, 2026-07-21, verdicted from the returned obligation records): a definition with `field D as number default 1.0 nonzero`, a construction row `set D = Open.Value` taking an unconstrained `number` argument, and a division `N / D` compiles with **zero errors**, the divisor obligation reported `Proved`, `strategy: DeclarationAttribute`. `NaN != 0` is true, so `nonzero` excludes nothing on this lane; the divisor may hold `NaN` and the quotient flows onward as a value. Nothing traps.

**Two closures, neither chosen.**

- **Finite-and-the-predicate.** Define `nonzero` / `positive` / `nonnegative` on the `number` lane to mean *finite and* the predicate, checked as such at ingress. Cost: modifier meaning becomes lane-dependent, which is itself a surface-honesty question; the catalog's `ProofSatisfactions` table gains lane discrimination it does not have today (`proof-engine.md:786` maps `nonzero` with no lane discrimination).
- **Rescope Principle 10.** Limit its no-silent-NaN clause to the exact lanes and make the `number` lane's non-finite behaviour explicit in the contract — which is what `philosophy.md:25` asks for — at the cost of a visible narrowing of a numbered principle.

**This is the only question in this list that can force an edit to a numbered principle or to `docs/philosophy.md`.** CLAUDE.md routes both to the owner rather than to a design pass. Until it is ruled, every `number`-lane fault cell is `open`, and that includes 100% of `sqrt` obligations (`primitive-types.md:284` — *"`sqrt()` lives exclusively in the `number` lane."*).

### Question 3 — The fault family's denominator: regenerate the coverage map, or drop the exhaustive claim

**Plain statement.** The matrix's whole value is that it is exhaustive — every coordinate carries an explicit answer. For the fault family, most of the coordinates that back that claim are not in the repository. Either put them there, or stop claiming them.

**The current state, verbatim from `docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:23`:** **"**That 9,292-cell disposition map is not in the repository.** It exists only as counts in the disposition pass's summary. So the defensible statement is: 202 cells are recorded as checkable data; a further several thousand coordinates were dispositioned in a pass whose output was not persisted and therefore cannot be validated, cited, or swept when a ruling lands. Several cell files lean on that map in prose — three of them fold twenty or more evaluation-site categories into a handful of 'representative' cells and point at it — so a non-trivial share of the claimed coverage currently rests on a document that is not here. Group 17 alone folds 20 categories into 4 representatives, leaving 48 of its stated 60 coordinates with no disposition the validator can see, while every sibling file enumerates its categories one cell each."**

**And `:177`:** **"**A decision on the fault family's denominator.** Either the 9,292-cell disposition map is regenerated as committed data — so totality is checkable and a landed ruling can be swept mechanically — or the coverage claim is restated as '202 recorded cells' and the folded categories are enumerated as real cells. The current state, where a meaningful share of coverage rests on an uncommitted document, cannot ratify."**

**Why it lands on this design.** The scope-containment check this document proposes runs against cell coordinates. It cannot be total over a coordinate space that does not exist as data, so the check's coverage claim is bounded by whichever way this is ruled. Cost either way: regenerating is bulk work whose output must then be validated; dropping the claim means the folded categories are written out as real cells, which is also bulk work but produces checkable data rather than a second uncommitted artefact.

---

### Owner-routed narrowings (withdrawn from § Decisions)

Both items below were written as settled decisions. Both **shrink the set of programs that compile**, and that is the owner's call, not a design pass's. The governing text, `docs/Working/obligation-discharge-matrix-2026-07-19.md:350`, verbatim:

> **Soundness-correction** — a licensed derivation discovered to be semantically unsound: it accepts a program that can violate its constraint at runtime. Shrinking the licensed set is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness. Never folded silently into a widening or a refactor.

Widening is the cheap direction — `:349`: *"Monotone: everything previously licensed stays licensed. Minor definition version; previously issued certificates remain valid."* Nothing in the two items below is monotone.

Each item keeps its original reasoning, alternatives, precedent and citations **verbatim**. Only the status changed.

#### The transport index (was Decision 3)

**What it would narrow.** Today a fact earned in a row's guard is transported into a state hook, and into the row's own first action, as if the row's action list were the whole mutation prefix. Re-indexing transport to the operation's mutation sequence kills facts that survive today, so programs that compile at HEAD become rejections.

**The witness program, compiled and verdicted from records at HEAD, 2026-07-21.** The exit-hook mirror of § Worked failures case 1, with the field modifiers removed so that the guard is the only candidate premise:

```precept
precept ExitHookTransportNoModifier

field Divisor as integer default 5
field Slots as integer default 0

state Warm initial
state Cold terminal

event Open initial
event Go

on Open
    -> set Divisor = 5

from Warm -> set Divisor = 0

from Warm on Go when Divisor != 0
    -> set Slots = 100 / Divisor
    -> transition Cold
```

`precept_compile` returns **zero diagnostics**, with the divisor obligation `Proved`, `strategy: GuardInPath`. The exit hook writes `Divisor = 0` before the row's first action divides by it, so the accepted program divides by zero at runtime. This is the witness matrix:350 requires, verdicted from a strategy record rather than a prose summary. It is also the witness for the *Guard normal-form match* soundness correction (see § Retired questions), whose sentence *"for a single-write plan there are none [preceding assignments]"* it falsifies directly.

**Cost of ruling either way.** Ruling **for** the narrowing: a major definition version, certificate replay broken for every fault cell, and authors who wrote a guard at the row now needing one at the hook. Ruling **against** it: the program above stays accepted, i.e. the definition licenses a runtime division by zero — which is the one thing the fault family exists to prevent.

**Interaction with the site-identity ruling.** The narrowing is stated in terms of a syntactic site's mutation prefix. matrix:187 makes the site an occasion, so even if the owner rules for the narrowing, the rule has to be restated per route before it can be written down (§ Semantic Rules, Rule 3 enumerates what changes). The two are best ruled together.

The original decision text, preserved:

##### Preserved verbatim — *Decision 3: Transport is indexed by the operation's mutation sequence, not by the row's action list*

**Stakes**: high

- **Rationale**: An operation contains three sequential mutation surfaces, not one. spec:2117 states it verbatim: *"It has the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing."* A transport rule scoped to the row's action list therefore reports "nothing wrote this" at a state-hook site whose value the row *did* write, and reports "prefix is empty, transport is vacuous" at a row's first action whose value the *exit* hook wrote. Both accept a program that faults. The concrete case is § Worked failures, case 1. Because the transport rule is cited by every fault cell, getting its index wrong is a universal accept-when-must-reject defect, and it is the single most valuable thing this design fixes.
- **Tradeoff accepted**: Two indices now coexist — the **write plan** (matrix:49: *"the row's action sequence"*), which governs *preservation-obligation granularity* for the constraint families, and the **operation mutation sequence**, which governs *fact transport* for the fault family. Two indices are more to hold than one, and a reader may conflate them. We accept that over redefining "write plan", which is locked and which governs something else.
- **Alternatives considered**:
  - *Redefine "write plan" to be operation-scoped.* Rejected — and not available to a design pass: matrix:49's definition is a locked owner ruling with its own reasoning (atomicity over configurations). Widening it would silently change preservation-obligation granularity for the constraint families, which is not this design's subject. If the owner prefers one index, that is a ruling, not a design choice.
  - *Scope the fault family to single-surface operations.* Rejected: matrix:186 mints fault obligations at post-write sites by construction, so scoping them away leaves minted obligations with no contract — a silent cell, forbidden by matrix:223.
  - *Leave transport unwritten and mark all post-write fault cells `open`.* Rejected: boundary:111's verified defect shows post-write consumption of a declaration-sourced fact is currently *unsound*, not merely unproven, so declining to write the rule does not make the problem go away — it leaves it undocumented.
- **Precedent**: Astrée and every forward abstract interpreter propagate an abstract state through the *executed* statement sequence, not through a syntactic sub-unit of it (`proof-engine-interval-arithmetic-survey.md:235` — *"At each assignment, the abstract value of the right-hand side is evaluated in the current abstract state, and the variable is updated"*). Precept-internal: spec:233's kill rule and spec:164's frame rule are both already stated; this decision only fixes what "preceding" ranges over.
- **Sources consulted for this decision**:
  - `docs/language/precept-language-spec.md:2117` — *"guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing."*
  - `docs/language/precept-language-spec.md:1971` — *"This guarantee applies to all mutation surfaces: event-driven transitions, stateless event hooks, direct field updates, and **state entry/exit actions**."*
  - `docs/language/precept-language-spec.md:164` — *"Each assignment in a row sees the state left by all preceding assignments."* (the frame half — note it quantifies over one **row**, which is the defect)
  - `docs/language/precept-language-spec.md:233` — *"When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored."* (the kill half)
  - `docs/language/precept-language-spec.md:1967` — *"Constraints are evaluated against the working copy after all mutations complete."* — which makes constraint and `ensure` conditions **post-plan** sites, not pre-write ones.
  - `docs/language/precept-language-spec.md:1354` — *"A *default value* is materialized in **declaration order during construction** … A *computed expression* is derived from the **final configuration** … A *rule condition* — and a *constraint modifier*, which is rule shorthand (§2.4) — is checked against the complete working copy *after* all mutations."* — the canon answer for the declaration-position sites the rule must also classify.
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:49` — *"**Write plan** — the row's action sequence"* — the locked definition this decision deliberately does not touch.
  - `samples/trafficlight.precept:29` — `from FlashingRed -> clear EmergencyReason` — verified exit-hook syntax.
  - `samples/warranty-repair-request.precept:40` — `to ReadyToReturn -> set ShippingLabelSent = true` — verified entry-hook syntax.
  - **Spec-first check**: grepped `precept-language-spec.md` and `proof-engine.md` for whether fact transport across hook boundaries is settled. §0.6 item 7 (sequential proof flow) settles transport *within a chain*; nothing states what happens across the exit-hook / row / entry-hook boundary. **Spec is silent on cross-surface transport** — this decision states it from spec:2117's execution order rather than inventing it.
- **Strongest counter-evidence**: matrix:49's own atomicity argument — *"the handler mutates a working copy that is committed atomically (`precept-language-spec.md:1967`), so no reader, no later handler, and no persistence ever sees a mid-handler state. A mid-handler state is not a reachable configuration."* Read quickly, that says intermediate states do not exist and transport across them is a non-question. Response: atomicity protects *configurations*, not *evaluations*. spec:1967 discards a failing working copy; a discarded working copy does not un-divide by zero, and spec:110 requires every expression to evaluate to a result. matrix:186 mints obligations at those very sites, so the matrix already treats intermediate evaluations as real.
- **Reversibility**: `Hard`. Every fault cell cites the transport name; changing its index re-opens all of them. Also coupled to site identity — and that coupling is now live: the owner ruled the site an *occasion* on 2026-07-21 (matrix:187), so the transport rule is stated about the wrong objects and needs restating (provenance and entailment survive; the bridge between them does not). § Semantic Rules, Rule 3 enumerates what must change.
- **Blast radius**: all 202 fault cells; the matrix's argument section; a soundness correction owed against the ratified *Guard normal-form match* argument (the guard-argument soundness correction), since its sentence *"for a single-write plan there are none [preceding assignments]"* is false whenever an exit hook exists. No `src/` change, no diagnostic code, no MCP surface.

#### The price of a field modifier (was Decision 5)

**What it would narrow.** Today `field D as decimal nonzero` discharges `X / D` anywhere in the file, off the declaration alone. Under this item the declaration alone proves nothing: the fact must be established at construction, preserved at every write site of every field the modifier's desugared constraint mentions, and transported to the site. That is the fault family's **most-used** premise (boundary:57), so this is the largest single narrowing on the table.

**The witness program, compiled and verdicted from records at HEAD, 2026-07-21.** § Worked failures case 1 exactly as printed. The load-bearing lines (the full program is in § Worked failures; this is an excerpt, not a compilable fragment):

```
field Divisor as integer default 5 nonzero
…
from Warm on Go when Divisor != 0
    -> set Divisor = 0
    -> transition Cold

to Cold -> set Slots = 100 / Divisor
```

`precept_compile` returns **zero diagnostics**, divisor obligation `Proved`, `strategy: DeclarationAttribute`. The row writes `0` into the `nonzero` field and the hook divides by it. Deleting the `nonzero` modifier makes the same file **reject** with `PRE0083` — so, as boundary:111 puts it, *"the only spelling that discharges is the one nothing enforces."*

**Cost of ruling either way.** Ruling **for** the re-pricing: the fault family's ratifiability becomes non-local — it depends on constraint-preservation minting, whose catalog subtypes do not exist (matrix:37) — and the majority of the 175 `defined` fault cells move from `proven-today` to *provable under the model, unresolved today*. Ruling **against** it: the program above stays licensed by the definition, not merely by a shipped bug.

**Rule this together with Question 1.** This document's own analysis couples them: a field modifier is the pre-state-constraint premise instantiated on a small mention set, so a ruling that excludes that premise class while leaving field modifiers axiomatic is not coherent unless field modifiers are *separately* declared axiomatic — which is itself a ruling.

The original decision text, preserved:

##### Preserved verbatim — *Decision 5: Class (a) — a field modifier — is premise class (d) instantiated at a small mention set, and is discharged by establishment plus preservation plus transport*

**Stakes**: high

- **Rationale**: A field modifier is text on a declaration; the claim a fault cell needs is about a *value at an occasion*. Given `field D as decimal nonzero` and a site `X / D`, the declaration does not establish that D's value is nonzero when the evaluator divides — D's value is whatever the last write left. want:22 settles what a modifier is (*"Modifiers are sugar for rules"*), matrix:181 already applies that reduction inside the matrix for cross-field modifiers, and matrix:92 already fixes the price of consuming a constraint as a fact. So class (a) is not a cheaper premise class; it is the same price on a smaller mention set. Three consequences follow, and all three are load-bearing: the mention set is quantified over, not the declaring field (a `max Ceiling` modifier's mention set is `{Floor, Ceiling}`); transport is not waivable on the ground that the post-state satisfies the constraint (boundary:111's verified defect is exactly a mid-plan declaration-sourced re-derivation); and the argument's premise is not minted today, so every cell consuming a class-(a) fact is *provable under the model, unresolved today* and may not record `proven-today` on the strength of a clean compile.
- **Tradeoff accepted**: The fault family's ratifiability becomes **non-local** — it depends on constraint-preservation minting, which lives in the constraint families and whose catalog subtypes do not exist (matrix:37). That is uncomfortable, and it is the honest reading: matrix:92 already states the same non-locality for premise (d), and the alternative is the verified unsoundness.
- **Alternatives considered**:
  - *Treat field modifiers as axiomatic — type-level, like a currency qualifier.* Rejected on the semantics. A currency qualifier is carried by the value's representation and cannot be changed by an assignment the type checker accepts. `nonnegative` is a predicate over a magnitude that any arithmetic assignment can falsify: `set Reading = -5` into a `nonnegative` field is **well-typed**. The two are not the same kind of declaration.
  - *Restrict class (a) to arg modifiers only.* Not rejected — it is one arm of the premise-class question, and this design does not settle it. It is recorded as incompatible with the argument as written.
  - *Discharge class (a) from the declaration alone, with transport waived.* Rejected: that is what the shipped compiler does, and boundary:111 records the resulting defect — *"a `nonzero` field whose construction row writes 0, divided by at a state hook, compiles with zero diagnostics … **the only spelling that discharges is the one nothing enforces**."*
- **Precedent**: matrix:181 — *"`field Floor as decimal max Ceiling` desugars to a constraint mentioning **both** fields, so a write to `Ceiling` carries the obligation just as a write to `Floor` does."* The reduction is already the matrix's own move; this decision only applies it to the fault family.
- **Sources consulted for this decision**:
  - `docs/Working/what-i-want-2026-07-16.md:22` — *"**Modifiers are sugar for rules.** `nonnegative` on `OverdraftLimit` is a compact spelling of `rule OverdraftLimit >= 0` … There is one constraint mechanism underneath — the premise classes above are spellings, not separate systems."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:92` — *"Using a constraint as a fact is paid for by establishing it at construction and preserving it at every write site of every field it mentions … If one site is missed, the fact is unearned, and every proof anywhere that consumed it is void."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:37` — the requirement DU *"has **no constraint-establishment or constraint-preservation subtype**. Those catalog entries are to be added when the constraint-obligation machinery is designed."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19.md:84` — *"The contracts state *committed* power … The witness status column tracks *built* power separately. The two are never conflated."*
  - `docs/Working/obligation-discharge-matrix-2026-07-19-cells/slice-3-boundary-report.md:111` — *"Qualifier modifiers (`nonzero`, `positive`, `nonnegative`) mint no write-site obligation; bound modifiers (`min`, `max`) do."*
  - `docs/language/precept-language-spec.md:268` — *"Every declared constraint is enforced on every value entering the entity from outside the definition … at the moment it enters, **before any computation derives from it**."* — the asymmetry that makes arg modifiers cheap and field modifiers expensive.
  - **Spec-first check**: grepped `precept-language-spec.md`, `business-domain-types.md` and `proof-engine.md` for a prior settlement of whether a field modifier is a standing fact. `proof-engine.md:606` documents the shipped behaviour (flag-modifier lower bounds folded into operand intervals) but the shipped compiler is not design authority here and has the verified defect. **Spec is silent on the modifier-truth-at-a-site question.**
- **Strongest counter-evidence**: `proof-engine.md:606`'s closing sentence — *"Because a `nonnegative`/`positive` field's value genuinely lies within these bounds, the fold can only tighten operand intervals and help discharge — it never creates a rejection."* That is a documented assertion that the modifier IS a standing fact about the value. Response: the premise of that sentence ("genuinely lies within these bounds") is what boundary:111's witness falsifies — nothing mints the write-site obligation that would make it true. The sentence describes an intended invariant, not an enforced one, and it is a doc-drift item this design records.
- **Reversibility**: `Hard`. Every class-(a)-citing fault cell's contract entry and built-status recording changes, and the retroactive correction to groups 2, 4 and 12's `proven-today` recordings (boundary:145) follows from it.
- **Blast radius**: the majority of the 175 `defined` fault cells (class (a) is, per boundary:57, the most-used fault premise); the matrix's argument section; a doc-drift correction on `proof-engine.md:606`; coupled to the premise-class question, since a ruling that excludes (d) while admitting field-(a) is not coherent unless field modifiers are separately declared axiomatic.

---

### Retired questions

Eight of the twelve questions the earlier revision carried do not need an owner ruling. Each is recorded here with the canon text that retires it, so it is not re-opened by the next pass.

| Question | Why it is retired | The canon text that retires it |
|---|---|---|
| Where the adequacy argument lives | Canon already scopes the validity-argument requirement, and this document does not override it. The adequacy argument therefore sits outside the cited list by default, and § Semantic Rules' citation rule is written to degrade cleanly when it does. No ruling is needed to proceed. | `docs/Working/obligation-discharge-matrix-2026-07-19.md:203` — *"'Rule' in this section means a **generative rule of this definition** — a discharge contract, a step kind's recompute rule, a transport rule. It is not the `rule` construct, and not the general 'constraint' sense."* Catalog stamping is a **minting** rule, which that sentence excludes. |
| The integer overflow model | Already settled — as **deferred**. The arithmetic rows of the exact-lane argument stay explicitly conditional on it; that is the correct recording of a deferral, not an open question this document owes. | `docs/language/primitive-types.md:686` — *"Whether arithmetic overflow is handled by compile-time proof-or-reject (prove no operation can exceed the representable range, else reject the definition) or by adopting an arbitrary-precision representation (removing overflow as a failure mode) is a post-MVP decision, not yet ruled. Until it is, `integer` arithmetic that could exceed the 64-bit range carries no live compile-time guarantee."* |
| The soundness correction against *Guard normal-form match* | Its fork was **route now with a model-derived witness, or wait for a working compile tool**. The tool works, and the witness is now compiled and verdicted from a strategy record (§ Owner-routed narrowings, the transport index). The fork collapses; what remains is executing the procedure canon already states, not ruling anything. | `docs/Working/obligation-discharge-matrix-2026-07-19.md:350` — *"always routed to the owner with the **witness program** demonstrating the unsoundness."* |
| Whether the scope-containment retrofit is a widening or a soundness correction | Same shape as the row above, and merges with it: canon already classifies a change that shrinks the licensed set, and a compiled witness is now producible. Both retrofits route as soundness corrections under the same sentence; neither needs a new ruling to classify it. | `docs/Working/obligation-discharge-matrix-2026-07-19.md:350` — *"Shrinking the licensed set is permitted **only** under this class."* (and `:349` — *"Monotone: everything previously licensed stays licensed"* — for the name-list addition, which is a widening and proceeds) |
| Whether `because` / message-interpolation holes enrol | Canon already carries this as a deferred owner-fork **with a named re-open trigger**. A question already ledgered with its trigger is not re-asked here. | `docs/compiler/soundness-and-coverage.md:229` — *"Constraint `.Message` interpolation holes (e.g. `because "bad {10/0}"`) \| value-fault vs. presence obligations \| **deferred** — owner-fork: whether message holes enroll, and for which fault classes … **Re-open trigger: owner ruling on the message-hole fork.**"* |
| Whether `mincount 1` may be consumed as a premise without its own establishment obligation | It is Question 1 on the container axis, not a separate question. Canon says the spelling discharges and is not narrowed; whether the fact is *earned* is exactly the premise-class ruling. It falls out of Question 1 and is swept with it. | `docs/language/collection-types.md:776` — *"A statically-literal `mincount 1` discharges `.min`/`.max`/`.peek`/`.peekby`/`.first`/`.last` access safety on the kinds that surface those accessors."* |
| What base minimality requires | The matrix already states it, in the leading clause: no other diagnostics at all. The parenthetical gloss is narrower, and the cell schema pins `noOtherDiagnostics` to `"const": true` in agreement with the leading clause. Reconciling the parenthetical is a drafting edit to the matrix, not an owner ruling. | `docs/Working/obligation-discharge-matrix-2026-07-19.md:77` — *"**Base** — a representative program the compiler must flag. It rejects naming the schema's obligation and the missing premise classes, with no other diagnostics (base minimality: the base rejects *only* for the target obligation)."* |
| `FunctionArgConstraintViolation` | Canon already carries it as a deferred owner-fork with a named re-open trigger, and states that the "build" arm is new language surface routed through `/design`. No argument in this document reaches it, and re-asking the fork here adds nothing. | `docs/compiler/soundness-and-coverage.md:196` — *"*(no per-argument constraint machinery exists)* \| **deferred** — owner-fork: remove-scaffolding vs. build. Re-open trigger: owner chooses 'build' → routes to `/design` (new language surface). No locked spec grounds it today."* |

**The ninth question — site identity — was ruled, not retired.** `docs/Working/obligation-discharge-matrix-2026-07-19.md:187`, verbatim: *"**Site identity — an evaluation site is an evaluation occasion, not a syntactic position** (owner ruling, 2026-07-21). One written expression reached by several routes mints one fault obligation *per route*, each proved from the facts that route establishes."* Its consequences are propagated through this document — chiefly § Semantic Rules Rule 3, which now records the transport restatement it owes.

---

## What this design does not deliver

Stated here rather than implied, because the honest disposition is not the flattering one.

**Zero of slice 3's 202 fault cells ratify unchanged, and the justification layer is not the only reason.** boundary:15 records 175 `defined`, 25 `open`, 2 `empty`. All 175 cite from the closed seven-name enum, every one of which is a constraint-family name this design puts out of scope for the fault family. So every fault contract entry is re-authored by construction — and Decision 7's coordinate additions make that re-author structural, not a string swap. **A substantial share of today's 175 `defined` cells move to `open` or `conflicted` under this design's own checks.** Any claim that a written justification layer unblocks the 175 would be false.

**Four blockers hold even if every argument here were written today.**

1. *The reject half stays untestable.* matrix:86: reject-side conformance is verified **only** by the cell-generated test matrix. boundary:138: `convert-cells` produces **zero** executable rows for all nineteen fault families, 174 of them for "no mechanized canonical WP key is recorded". A justification layer does not produce a canonical key for a catalog-stamped precondition.
2. *The denominator is not in the repository* (the denominator question).
3. *Site identity is ruled, and the transport rule owes a restatement.* matrix:187 makes an evaluation site an evaluation occasion. The transport rule in § Semantic Rules is stated about syntactic positions, so it is now stated about the wrong objects; § Semantic Rules, Rule 3 enumerates exactly what has to change. Writing the replacement is design work this pass deliberately does not do. Every fault cell cites transport, so nothing downstream of it can ratify until the restatement lands.
4. *Respellability is a hard gate and this design's verdicts are paper verdicts.* matrix:350: *"Paper-only verdicts do not ratify."* The exclusions push real programs into the sound-but-unprovable band; the claim that they respell into a guard is asserted, not measured. The corpus cannot settle it — corpus:15: *"Programs that would demonstrate expressiveness loss could not be in it — they were rewritten until they passed, or never written."*

Plus, independent of all of the above: four coordinates carry contradictory answers across files (boundary:125-132), six discharge witnesses do not produce an accepting program (boundary:113), and every recorded `strategy` field is an unverified source-reading with one provably wrong (boundary:146).

**Four arguments cannot be completed today**: *Pre-state constraint fact* (the premise-class question), *Approximate-lane value entailment* (the `number`-lane conflict), *Collection-contents entailment* (no premise class ranges over a collection's contents — matrix:38's four classes are all declaration- or guard-scoped, and `KeyPresence`'s absence polarity has no licensed fact source anywhere), and *Fact abstraction* (requires reading `certificate-steps-membership-2026-07-12.md` to know whether the closed 11-member `CertificatePremise` vocabulary can serve as the pinned interface — **this pass did not read that document**, and it is the largest unread dependency here). *Exact-lane value entailment* is conditional on the deferred overflow model; the adequacy argument's placement is the adequacy-placement question; and the temporal lanes have no interval model at all.

**Two further coverage gaps the design records rather than closes.**

- *Catalog interval-transfer entailment names a real hazard it does not fix.* `Operations.cs` attaches per-overload C# lambdas that compute result intervals (`DivideTransfer`, `NumberDivideTransfer`, and a `Clamp` transfer that produces an inverted — i.e. unsound — interval when the clamp bounds cross). These are generative rules under matrix:192 and owe validity arguments; giving them one name does not audit the lambdas.
- *No provenance argument covers a scalar written by an earlier action in the same plan.* After `set G = <expr>`, a later site reading `G` has no licensed provenance name. That is conservative rather than unsound — but matrix:186 mints obligations at exactly those sites, so the citation rule is unsatisfiable for a whole minted site class, and closing it needs a *substitution* provenance rule this design does not write.

**The strongest objection to this design, stated rather than defended.** The transport rule is a universal dependency: every fault cell cites it, so a defect in it breaks certificate replay for 100% of fault cells — the outcome Decision 1 used to reject the monolithic shape. The defence is narrow and will not be overstated: transport genuinely *is* shared, a defect there really does void every fault proof, and spreading transport reasoning across nineteen arguments would hide that fact rather than reduce it. **Factoring makes the shared dependency explicit and singly-maintained; it does not make it smaller.** The adversarial pass found the transport rule's index wrong on its first attempt, which is evidence both that the rule is the right place to look and that one design pass is not enough scrutiny for it.

**Verification state.** Done on 2026-07-21: `precept_compile` is working; the entry-hook and exit-hook witnesses, the `sqrt(abs(X))` witness and a `number nonzero` divisor probe are all compiled and verdicted from diagnostic codes and obligation records; every `.precept` sample printed in this document parses and type-checks clean. What those runs showed — including that acceptance criteria 7 and 8 are **not** satisfied, and that this document's own recommended author fix does not discharge — is in § Worked failures → Tooling and verification state.

**Still owed before anything is built on this**: the transport-rule restatement against the ruled site identity; read `certificate-steps-membership-2026-07-12.md`; verify the shipped analyzer's division arithmetic against the evaluator's; and close the five `primitive-types.md` / `temporal-type-system.md` / `business-domain-types.md` doc obligations enumerated above.
