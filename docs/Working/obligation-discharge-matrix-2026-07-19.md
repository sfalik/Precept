# Obligation–Discharge Matrix — a definition mechanism for the proof surface

**Status**: Draft — 2026-07-19 (rev 4: rule-validity gate + per-contract decision procedures + storage/amendment/ratification rulings; rev 3 destined for canon, exact-power iff, respellability; rev 2 generalized per premise reviews; structure only, population pending)
**Destiny**: once populated and ratified, this document **is the canonical, exhaustive definition of the proof engine** and is promoted into `docs/` canon accordingly (owner ruling, 2026-07-19)
**Seed model**: `docs/Working/what-i-want-2026-07-16.md` ("the want doc") — the mechanism that set the direction; authoritative during drafting, superseded for the proof engine by this matrix at ratification
**Premise reviews applied**: `obligation-discharge-matrix-2026-07-19-reviews/` (review-1 factorization-soundness, review-2 definition-fitness, review-3 field-precedent). Amendments are cited by finding ID; the reviews are not restated here.

## Purpose

**The exhaustive definition of the proof engine** — cell by cell, with no silent cells. For every combination the proof surface admits, the matrix carries an explicit written answer to "what must the compiler do here?" A cell with no answer is a definition gap by construction. The want doc was the mechanism that put this on the right path: it supplies the generative model the drafting starts from, and the matrix is the form the finished definition takes — not a commentary on the want doc.

What makes a matrix a legitimate *definition* rather than a suite of examples (review-2 F1, lens 5): the generative content lives in this document — the Vocabulary, the case shapes, the minting rule, and each cell's schema and discharge contract are the generative sentences; the witnesses are their worked instances. A cell whose answer cannot be derived from those generative sections indicates a missing generative sentence to author **here** (owner-ruled), never an answer to assert ad hoc in a cell.

The matrix is structured top-down from the full language surface — every axis is sourced from a catalog, the spec, or the want doc (or explicitly marked as a prose taxonomy pending a catalog anchor, review-3 F3) — not generalized from a worked example. The numeric single-field slice that seeded rev 1 survives below as Witness Family 1, retyped as witnesses of cells, not as the source of the structure.

A second use is as a test basis — every defined cell converts mechanically into reject/discharge/near-miss tests — but that follows the definitional pass; see the final section.

## Authority

(review-2 F6 and lens 5, restated under the owner's 2026-07-19 ruling on this document's destiny.)

- **During drafting: the want doc defines; the matrix drafts against it.** Every defined cell's answer must be derivable from the want doc plus this document's own generative sections; a conflict between a cell and the want doc routes to the owner — it is never resolved silently in either direction.
- **At ratification: the matrix becomes the canonical proof-engine definition.** The want doc is superseded for the proof-engine surface; subsequent conflicts elsewhere in canon are corrected *to the matrix*. Ratification is an owner act (the promotion lifecycle), not a drafting outcome.
- **Generative sentences over cell assertions.** A cell answer underivable from the generative sections (Vocabulary, case shapes, minting rule, discharge contracts) is a missing generative sentence to author here with an owner ruling — never a bare cell answer. This keeps the definition generative (finite rules determining unboundedly many programs), which is what licenses calling a matrix a definition at all.
- **Citation duty during drafting**: every defined cell cites the want-doc sentence(s) — or the owner ruling — its obligation and premise availability derive from; the same duty edge cells already carry. At ratification these citations become the record of derivation.

## Vocabulary

- **Obligation family** — what must be proven. **Want-derived**: rule establishment (want :18), rule preservation (want :19), fault prevention (want :104). *Not* catalog-enumerated today: the `ProofRequirement` DU (`docs/compiler/proof-engine.md:529-543`) carries numeric/presence/dimension/modifier/qualifier/containment/key-presence/index-bounds/dimensional-product/assignment-qualifier kinds — it has **no rule-establishment or rule-preservation subtype**; those catalog entries are to be added when the rule-obligation machinery is designed (review-1 F-5; packet §(a)1). The fault family, by contrast, *is* catalog-enumerated (same DU; `docs/compiler/soundness-and-coverage.md` §3.1 carries the per-FaultCode enumeration). Establishment's site set is construction (defaults + initial events, want :18) and — gated ⧖ Q5 — state-entry activation sites for ensures.
- **Premise classes** — the want doc's four (want :19): (a) field modifiers, (b) arg constraints, (c) the handler's guard, (d) all rules holding in the pre-state (the inductive hypothesis); plus **(e) ingress evaluation of the applicable rule set** at an editable-field write (want :143). Whether (e) is a fifth class or (a) generalized to the ingress point is an **owner-visible open item** (review-1 F-1); the matrix uses "(e)" as a label without prejudging the ruling.
- **Obligation schema** — an obligation stated with metavariables (field, bound, arg, expression shape) under the normalization below. Two programs sit in the same cell iff their minted obligations unify with the same schema, differing only in metavariable instantiation — a syntactic criterion, checkable without running the prover (review-2 F2).
- **Normalization** — schema matching and guard-fact matching are defined up to a stated normal form: comparison-direction flip (`a <= b` ≡ `b >= a`) and operand commutation for commutative operators. "Verbatim" is replaced by **normal-form-equal** throughout (review-2 F4). The full normal form (beyond these two rules) is an open item for the canonical doc. A cell's *base* is a representative of the schema class under this normalization; diagnostics that quote source quote the author's text, not the representative's (review-2 F8).
- **Discharge contract** — per premise class, and **exact**: an obligation discharges **iff** a premise instantiating an applicable class lets a defined strategy/step-kind derivation close the proof condition (owner ruling, 2026-07-19). The contract is class-and-strategy-relative, not addition-instance-relative (review-2 F1) — and it pins the accepted set exactly, not as a floor: a compiler that accepts a program the contract does not license is nonconforming even if the program is semantically sound. Rationale: a floor-only contract lets a smarter build accept more sound programs than the definition names, so two conforming compilers (or two versions) disagree on which files compile — breaking determinism and the uniform guarantee through the back door. Proof power is *defined*, not bounded below. **Each contract must also name its decision procedure or search bound** (owner-approved 2026-07-19; review-4 F3): how "no licensed derivation exists" is decided — e.g. normal-form match against the row's guard set, interval arithmetic over declared bounds, bounded enumeration of applicable premises. A contract without one leaves reject-side conformance untestable even in principle.
- **Sound-but-unprovable band** — the programs in a cell's class that are semantically sound but whose premises the discharge contract does not license (e.g. a guard that *implies* the WP but is not normal-form-equal to it). Under prove-or-reject these are rejected by design; the band is the cell's honest measure of proof power's cost.
- **Respellable** — a per-cell property the population pass must establish: **yes** iff every sound program in the cell's sound-but-unprovable band has a provable respelling within the surface (a spelling of the same business intent that the contract licenses — typically the suggestion schema's output). Respellable: yes → the power gap costs authoring friction only, which the want doc deliberately embraces ("forces authors to be more explicit," want :137). Respellable: no → the power gap is an expressiveness loss — a surface-shrink decision routed to the owner, since the guarantee is non-negotiable and the surface is the negotiable part (want :190).
- **Suggestion schema** — per applicable premise class, the diagnostic's required suggestion, stated as a schema ("add a guard normal-form-equal to `⟨WP⟩`"; "bound the args such that the interval combination satisfies `⟨WP⟩`"). When several classes can discharge, the diagnostic names **all applicable classes** (review-2 F3). **Owner ruling 2026-07-19**: an author-invoked editor quick-fix that inserts the suggested premise **counts as the author writing it** — the language server's code-action pipeline may carry these suggestions as one-click fixes. What the want doc's "never inserts the premise itself" (want :179) forbids is *silent or automatic* insertion; the sentence is scoped accordingly in the canon-correction pass.
- **Witness** — a concrete base / discharge-addition / near-miss instance recorded in a cell. Witnesses witness the definition; they are not the definition (review-2 F1).
- **Near-miss** — a discharge addition weakened past sufficiency (bound loosened past the arithmetic, guard on the wrong condition). Required outcome: still rejects, naming the **same obligation**. Both verdict polarities are first-class (review-3 F1, blocking).
- **Write plan** — the row's possibly-multi-write action sequence. Obligations are defined over the write plan; whether plan-cells decompose into per-write cells is ⧖ Q1 (packet), an openness in the coordinate system itself, made visible here rather than presupposed (review-1 F-3). Guard-fact transport through prior writes in a plan is a real derivation step (a certificate step, not a matrix column).
- **Derivation class** — the certificate step kinds: the closed 21-member `CertificateStepKind` vocabulary plus the 11 `CertificatePremise` kinds (`docs/Working/certificate-steps-membership-2026-07-12.md`, Decisions 1 and 4, :333/:389). The §1a/§1b boundary (`proof-engine-mvp-and-proof-phases-2026-07-12.md` §1a/§1b) is retained as a **capability-tier annotation** on a cell, not as the derivation vocabulary (review-1 F-7).
- **Strategy** — the predicate that realizes a discharge (`ProofStrategy` enum, `src/Precept/Pipeline/ProofLedger.cs:100`). The built/unbuilt status of the required strategy separates "provable under the model" from "proven today."

## The cell

A **defined cell** is (review-2 F1/F3/F4; review-3 F1):

1. **Obligation schema** — the site's proof condition with metavariables, under the stated normalization. For rule families: the WP of the rule through the write plan. For the fault family: the catalog-declared safety precondition at the evaluation site (see case shapes below).
2. **Discharge contract, per applicable premise class** — which classes can discharge, and by which strategy/derivation class. The applicable-class set is a per-cell fact derived from the axes (see the read-set axis).
3. **Suggestion schema, per applicable premise class** — naming all applicable classes when several can discharge.
4. **Witnesses** — at least one concrete triple:
   - **Base** — a representative program the compiler must flag; rejects naming the schema's obligation and the missing premise classes, with no other diagnostics (base minimality: the base rejects *only* for the target obligation — review-3 F4).
   - **Discharge addition(s)** — instances of the discharge contract; base + addition compiles clean via the expected strategy, premise list recorded.
   - **Near-miss, one per discharge addition** — the addition weakened past sufficiency; still rejects, same obligation named.
5. **Respellability** — the cell's `respellable: yes/no` verdict on its sound-but-unprovable band, with a representative band member and (if yes) its licensed respelling; `respellable: no` routes to the owner as a surface decision.

The cell triple (theorem, premises, derivation) is deliberately the same shape as the certificate record the want doc specifies (want :199). **Scoping** (review-3 F2): the cell triple is the certificate's *schema and index* — same theorem, same premise list, same step-kind vocabulary. The certificate additionally carries the fine-grained step-by-step derivation (want :206) that the cell only names; the matrix must never be read as fixing the certificate's payload granularity.

**The matrix as the measure of proof power.** Under no-deferral, "what the engine proves" and "what an author can write" are the same boundary; the exact discharge contracts draw it cell by cell. Power is measured from both sides — the contract from below (must prove exactly these), the near-miss rows from above (must not accept these) — and the sound-but-unprovable band between them is the cost, kept honest per cell by the respellability verdict. The contracts state *committed* power (what the definition obliges); the witness status column tracks *built* power separately — the two are never conflated.

**Verification asymmetry** (owner-approved 2026-07-19; review-4 F3): the accept half of the exact contract is certificate-verified — every acceptance carries a walkable derivation, checked at both call sites. The reject half is **not certifiable**: a rejection carries no certificate of non-derivability, so nothing at load or in production ever confirms a rejection was correct, and a compiler that silently under-proves (rejects licensed programs) passes every certificate check. Reject-side conformance is verified **only** by the cell-generated test matrix — base, near-miss, and deletion rows against the named decision procedures. The cell→test conversion is therefore part of the definition's verification machinery, not a convenience, and its coverage bar is set accordingly.

## Axes

Each axis names its source. No axis is induced from an example.

### 1. Obligation family (want-derived; catalog entries pending)

Establishment / preservation / fault prevention — want :18, :19, :104, as defined in Vocabulary. The structural family is deliberately **not** on this axis; it gets its own case shape (below), and the completeness claim is scoped accordingly (review-1 F-2).

### 2. Rule structure (prose taxonomy — catalog anchor pending; review-3 F3)

Single-field / relational / conditional (`when`-activated, want :40) / quantified (`collection-types.md:796`, ⧖ Q10) / non-linear (want :105, :135) / temporal (want :219, ⧖ Q14a). Rules are authored expressions, not catalog members, so this axis has no catalog enumeration today; the anchor should eventually be the expression-shape classification the rule-obligation design produces. Applies to rule families only.

### 3. Write-site category (source: `precept-language-spec.md:1971` + packet §(c)9)

The spec's mutation-surface enumeration ("event-driven transitions, stateless event hooks, direct field updates, and state entry/exit actions"), unpacked per the packet's confirmed write-site sweep:

- handler `set` (transition rows and stateless event hooks)
- **editable-field edit** (the second ingress point, want :141 — the cell class rev 1 structurally could not contain; review-1 F-1, blocking)
- entry hook / state action (state entry/exit)
- collection action (the `Actions` catalog, 15 members — `Actions.cs`; mutating actions are write sites for rules mentioning the collection)
- computed-field transitive (`<-`: every write to an input is a write site for rules mentioning the computed field — ⧖ S4/packet)

This axis is orthogonal to RHS shape; RHS shape applies only where an authored expression exists.

**No-write rows** are not one value but three (review-1 F-8): reject rows (no data obligation; inapplicability-vs-refusal semantics, want :154 — though reject-row *interpolations* are fault evaluation sites, see case shapes), transition-only rows (ensure-activation obligations ⧖ Q5), and unmatched inapplicability (not a row at all).

### 4. RHS read-set shape (replaces rev 1's overwrite/RMW axis; review-1 F-4)

For sites with an authored expression, classify the RHS by its read set: reads {nothing (constant) / args only / the written field / other rule-mentioned fields / rule-unmentioned fields} — combinations permitted. Premise (d)'s availability **derives from the RHS read-set intersected with the rule's mention set**: pre-state rule facts are available exactly over the fields the plan reads pre-state plus the mentioned fields it leaves unwritten. Rev 1's "overwrite excludes premise (d) structurally" is false in general (counterexample: `set Total = Cap` under `rule Total <= Cap` — an overwrite that premise (d) closes); overwrite-vs-RMW was the special case where the only interesting read is the written field. The three numeric witnesses below survive as three read-set classes.

### 5. Type family (source: the four language type docs + the `Types` catalog)

Primitive (`docs/language/primitive-types.md`) / temporal (`temporal-type-system.md`) / business-domain (`business-domain-types.md`) / collection (`collection-types.md`). Members are enumerated by the `Types` catalog (32 members, `Types.cs`; queryable via `precept_types`) — the matrix references the families, the catalog owns the membership.

**Establishment pre-configuration** (review-1 F-8): establishment WP is taken over the **default configuration** for fields the initial event does not write (want :18, :106).

## Per-family case shapes (a discriminated union of axis sets)

The five axes are not one orthogonal product; they are a family-indexed schema (review-1 F-6) — a DU of axis sets, per this project's own design idiom:

| Case shape | Obligation | Axes that apply | Discharge |
|---|---|---|---|
| **Rule families at authored write sites** (establishment, preservation) | WP of the rule through the **write plan** (⧖ Q1 for decomposition) | family × rule structure × write-site category × RHS read-set × type | premise classes per the discharge contract; strategies/step kinds |
| **Fault family** | the **catalog-declared safety precondition at the evaluation site** (`ProofRequirement` DU, `proof-engine.md:515-543`) — the site may sit in a guard or a reject-row interpolation with **no write at all** (want :72: `{-Balance / PlanRepayment.Months}` in a reject row) | family × operation kind (from the ProofRequirements catalog, replacing rule structure) × evaluation-site category (replacing write-site) × type | premise classes (b)/(c)/(a) per catalog `ProofSatisfactions`; want :104 |
| **Editable-ingress cells** | the field's modifier-rules plus every rule mentioning the field, at the edit (want :143) | family × rule structure × {editable edit} × type (no RHS axis — the incoming value is arbitrary) | **ingress evaluation** — premise class (e), or (a) generalized; the choice is an owner-visible open item (want :143; review-1 F-1) |
| **Structural family** (reachability, dead end, dead row) | no WP, no premise class — a second, honestly-typed **weaker case shape**: flagged base → required rejection; fixing structural edit → clean. No premise/strategy columns. Keyed to the `ProofForwardingFact` kinds (`proof-engine.md:214-240`: Reachability, DominancePath, EventCoverage, TerminalCompleteness, DeadEndState) | structural-fact kind × (state/row) shape | a structural edit (an inbound row, a `terminal` marking, a satisfiable guard). **Dead rows are ⧖ Q6** and are a genuine cross-cutter: a guard contradicting a *rule* consumes the matrix's premise vocabulary in the satisfiability scan even though it mints no WP (review-1 F-2) |

**Scope of the completeness claim**: the schema/contract machinery covers the data half of the want's promise; the structural half is covered by the weaker case shape only (want :149: "the proof engine's half of the promise governs data; the graph analyzer governs shape"). The claim below quantifies over both, each under its own case shape.

## The minting rule — an explicit input

(review-1 F-5.) Which (constraint, site) pairs mint obligations is **prior to** the matrix: "no silent cells" is only as strong as the generative rule for cell candidates. The minting function's parts, cited where settled and ⧖ where open:

- **Symmetric attachment**: a rule's obligations attach to every write site of every field the rule mentions (want :20; settled).
- **Mention-set definition**: must include guard-position occurrences, desugared cross-field modifiers (`A min B`), and derived-field dependents — ⧖ S4 (packet §(a)5; canonical-doc vocabulary item).
- **Activation sites**: whether attachment extends to constraint-*activation* sites (a no-write transition into a state whose ensure the unchanged data violates) — ⧖ Q5 (packet).
- **Computed-field transitivity**: every write to a `<-` input is a write site for rules mentioning the computed field — ⧖ S4/packet §(c)9.
- **Fault minting**: catalog-stamped at evaluation sites (`proof-engine.md` § Catalog-Driven Obligation Instantiation, :515; settled and shipped for the fault family).

A change to any ⧖ part changes which cells exist; the matrix records the dependency instead of absorbing an answer.

## Rule validity — the definition argues its own soundness (owner-approved 2026-07-19)

(review-4 F2.) The certificate checker verifies that proofs *replay* the rules; nothing downstream verifies the rules themselves — and under the exact contract, a defective rule is not a bug a compiler may conservatively decline: the definition mandates implementing it. The definition is therefore the trusted computing base, and it must argue its own soundness:

- **Every generative rule carries a validity argument** — each discharge contract, each certificate step kind's recompute rule, and each transport rule (guard-fact transport through prior writes, vacuity-by-activation, sign reasoning): a short written argument that the rule is truth-preserving with respect to the evaluator's actual runtime semantics (arithmetic, rounding, presence, ordering), or a citation to one. Precedent: Event-B ships its obligation-generation rules with published soundness meta-theory; intent citations (the want doc) establish what a rule is *for*, not that it is *true*.
- **Ratification gate**: no cell ratifies whose derivation cites an argument-less rule. The validator checks argument *presence* per cited rule; argument *adequacy* is reviewed like any generative sentence — owner-read, adversarially checkable.
- A validity argument found wrong after ratification is precisely the **soundness-correction** amendment case (Amendments, below) — the witness program demonstrating the unsoundness accompanies the correction.

(review-2 F5.) Every cell of the product — not just the non-empty ones — carries exactly one of:

- **Defined** — schema, contracts, suggestion schemas, and witnesses stated; **cites the want-doc sentence(s) it derives from** (review-2 F6).
- **Deferred** — provable under the model, machinery deliberately out of scope — cited (e.g. §1b, ⧖ Q7).
- **Open** — the model has not decided — cited (a want open question or a packet Q-item). Rev 1's "structural gap" label is retired; those cells are **open (Q10)** etc.
- **Empty** — the cell is pruned, with a required **one-line pruning derivation** written in the cell (e.g. "empty: no-write reject row × preservation — writes nothing, frame-preserves every data rule; ensure-activation excluded ⧖ Q5"). Pruned cells are written, not absent — an incorrectly-pruned cell must be findable, never a silent gap wearing an "empty" badge.
- **Conflicted** — two canon sources answer differently: cite both, route to owner (e.g. `omit` × rules — canon asserts both reset-to-default and structurally-absent semantics; packet G5, ⧖ Q13). A matrix that cannot record the canon-asserts-both-answers state cannot detect its recurrence.

**Completeness claim (scoped)**: the proof engine is exhaustively defined when every cell of the family-indexed product carries a disposition — data cells under the schema/contract shape, structural cells under the weaker shape — with content or citation. Cells the enumeration produces that neither the want doc nor this document's generative sections can answer are the definition gaps, found mechanically and routed to the owner.

## Composition check — the deletion method retained

(review-2 F7; review-1 F-9.) Cells are defined in isolation; the compiler is defined on whole files. The addition framing is stronger *per cell* (it pins rejection, suggestion, and fix together); it does not subsume the want's deletion method, which exercises every obligation with all other premises co-present — the framings are complementary, and no strict-superiority claim is made.

- **Compositionality input**: "obligations are minted and discharged per site; premises are scoped per handler; no cell's disposition depends on any other cell" is a want-doc-level claim the matrix consumes — currently implied, not stated; flagged for the canonical doc to state and cite.
- **Per family, one composed exemplar file** with a deletion pass (the want's own §"What must not compile" table, want :110-131, is the rule-family exemplar), alongside the per-cell witnesses. Interaction defects — an addition discharging one obligation while breaking another, a new guard creating a dead row — are caught here, not per cell.
- **Global note — the consumed/unconsumed-premise asymmetry** (want :129): a premise appearing in no cell's premise list is unconsumed — enforced at its ingress, load-bearing for nothing; deleting it weakens the spec without breaking any proof. This is a derived per-file property (the certificate's load-bearing marking, want :210), noted once — not a cell.

## Witnesses

Witness families below **witness the structure; they do not populate it** — full population is the next pass. Provenance marks: **✓v** = verified by live `precept_compile` on 2026-07-19 at HEAD `ed57d7fd` (engine pre-dates the rule-obligation build; `rule`-statement obligations are not yet minted, modifier-spelled bounds are). Unmarked = model-derived from the want doc. Near-miss rows are model-derived throughout (added in rev 2; no live runs claimed).

### Witness Family 1 — numeric single-field, handler `set` (live-verified slice)

Cell coordinates: preservation × single-field × handler `set` × (three RHS read-set classes) × primitive numeric. Derives from want :19 (premise classes), :20 (attachment), :22 (`max` desugars to `rule Total <= 1000`). Setting (stateless precept):

```precept
field Total as decimal max 1000 default 0.0
```

**Standing base-case obligation** (establishment × defaults): `default 0.0 ⊨ Total <= 1000`. Discharged by constant folding (`Literal`). **Proven today ✓v.** (No initial event exists in this slice; the establishment half over initial events is vacuous here.)

**Base A — RHS reads args only** (`event Add(Amount1 as decimal, Amount2 as decimal)`; `on Add -> set Total = Add.Amount1 + Add.Amount2`)

Obligation schema: WP `Amount1 + Amount2 <= 1000`. Read set ∩ mention set = ∅ and the written field is not read, so premise (d) has nothing to instantiate — availability derived per the read-set axis, not from "overwrite." Applicable classes: (b), (c). Suggestion schema names both.

| Addition | Class | Derivation | Status |
|---|---|---|---|
| `when Add.Amount1 + Add.Amount2 <= 1000` | (c) guard | guard fact normal-form-equal to the WP → §1a guard-match | provable under want; **Unresolved today ✓v** (multi-term fact shape unbuilt) |
| `Amount1 max 250, Amount2 max 750` | (b) arg bounds | `250 + 750 ≤ 1000` → `IntervalContainment` | **Proven today ✓v** |
| Near-miss: `Amount1 max 600, Amount2 max 600` | (b) | `600 + 600 = 1200 > 1000` — must still reject, same obligation | model-derived |
| Near-miss: `when Add.Amount1 <= 1000` | (c) | guard does not imply the WP — must still reject, same obligation | model-derived |

(The arg-bound addition's `250/750` split is an instantiation witness of the class schema — the suggestion is the schema, never these constants; review-2 F3.)

**Base B — RHS reads the written field, decrease** (`on Subtract -> set Total = Total - Subtract.Amount`)

Obligation schema: WP `Total_pre - Amount <= 1000`. Read set includes the written field ⇒ premise (d) available.

| Addition | Class | Derivation | Status |
|---|---|---|---|
| `Amount as decimal nonnegative` | (d) `Total_pre <= 1000` + (b) `Amount >= 0` | `Total_pre - Amount <= Total_pre <= 1000` → §1a linear-with-hypothesis | provable under want; **Unresolved today ✓v** (pre-state rule not carried as a premise — the engine widens `Total` to `[−∞ .. +∞]` at the read) |
| Near-miss: `Amount as decimal max 100` (sign unconstrained) | (b) | a negative `Amount` still breaches — must still reject, same obligation | model-derived |

**Base C — RHS reads the written field, increase** (`event Grow(Amount as decimal positive)`; `on Grow -> set Total = Total + Grow.Amount`)

Obligation schema: WP `Total_pre + Amount <= 1000`. Premise (d) available but insufficient (`1000 + positive > 1000`); no arg bound closes it; (c) is the only applicable class — the applicable-class set is a per-cell fact.

| Addition | Class | Derivation | Status |
|---|---|---|---|
| `when Total + Grow.Amount <= 1000` | (c) guard | guard fact normal-form-equal to the WP → §1a guard-match | provable under want |
| Near-miss: `when Grow.Amount <= 1000` | (c) | ignores `Total_pre` — must still reject, same obligation | model-derived |

Same rule, three read-set classes, three WPs, three applicable-class sets — the read-set axis doing the work rev 1 attributed to overwrite-vs-accumulation.

**Respellability (all three bases): yes** (model-derived). Representative band member for Base A: `when Add.Amount1 <= 500 and Add.Amount2 <= 500` — sound (implies the WP) but not licensed (not normal-form-equal; conjunct-interval combination is not a defined derivation for this cell) — respelling: the suggestion schema's normal-form guard, or per-arg `max` bounds summing within 1000. Bases B and C analogously: every sound band member's business intent respells into the licensed guard or arg-bound form.

### Sketch witnesses (model-derived; no ✓v; no compile results claimed)

**Family 2 — relational two-field rule, symmetric write site** (preservation × relational × handler `set`; want :103). `rule Balance >= -OverdraftLimit`; site `set OverdraftLimit = ReduceLimit.NewLimit` (writes the *other* mentioned field). Schema: WP `Balance_pre >= -NewLimit`; (d) available over `Balance` (unwritten mentioned field). Addition: `when Balance >= -ReduceLimit.NewLimit` → substitution closes (want :103). Near-miss: `when Balance >= -OverdraftLimit` (guards the *old* limit) — still rejects, same obligation.

**Family 3 — editable-ingress cell** (preservation × {editable edit}; want :106, :141, :143). `field DailyWithdrawalLimit as decimal positive max 10000.0 editable`; `in Active modify DailyWithdrawalLimit editable`. Obligation: every write satisfies the field's modifier-rules plus every rule mentioning the field. Discharge: ingress evaluation — class (e)/(a)-generalized (open item). Near-miss: an ingress that evaluates the modifier-rules but omits a mentioning rule — must refuse the edit that breaks the rule (this near-miss is a *runtime-surface* obligation; its compile-time shadow is the certificate marking the ingress check load-bearing, want :129).

**Family 4 — fault family, arg-constraint discharge** (fault × division; want :104). Base: `set MonthlyRepayment = -Balance / PlanRepayment.Months` with `Months as integer` unconstrained — rejects: divisor can be zero. Addition: `Months as integer positive` → (b), divisor interval excludes 0. Near-miss: `Months as integer nonnegative` — zero still admitted; must still reject, same obligation.

**Family 5 — conditional rule, vacuity by activation** (preservation × conditional; want :40). `rule MonthlyRepayment <= Cap when MonthlyRepayment is set`; a write plan that leaves `MonthlyRepayment` unset in the post-state discharges vacuously (activation condition false) — a non-arithmetic derivation with no name in the §1a/§1b taxonomy, named by the certificate step kinds (review-1 F-7). Near-miss: a plan under which `MonthlyRepayment` may remain set — vacuity unavailable; must still reject, same obligation.

**Family 6 — structural cell, weaker case shape** (`ReachabilityFact`; want :125, :151). Flagged base: `state Suspended` with no inbound row → required rejection (unreachable state). Fixing structural edit: any inbound row → clean. No premise/strategy columns; both polarities still present (the near-miss analogue: an inbound row on a dead row — ⧖ Q6 — must not count as reachability).

## Edge cells — cells with no authorable addition

The disposition mechanism at work — each edge cell carries its disposition and citation, so "the want doc does not cover X" is always a written, findable statement:

| Base (sketch) | Why no addition discharges it | Disposition |
|---|---|---|
| A bound derivable only by combining two stated rules through a shared field | the only "addition" is the deferred multi-fact solver — not source an author can write; the author's hand-escape is restating the combined fact as a third rule (**respellable: yes**) | **Deferred** (§1b; ⧖ Q7) |
| `rule` quantified over a collection | canon classifies quantifier predicates as runtime governance (`collection-types.md:796`) — conflicting with no-deferral (want :171) | **Conflicted** → **open** (⧖ Q10; both sources cited) |
| `rule ExpiryDate > today` | no ingress point exists for the passage of time — nothing to attach a premise to | **Open** (want :219, deliberately open; ⧖ Q14a) |
| Rule mentioning a field omitted in some state | canon asserts both reset-to-default and structurally-absent semantics | **Conflicted** (⧖ Q13; packet G5) |

## Relationship to other work

- The rejection side of the want doc's "What must not compile" table (want :110-131) is this matrix's base column generalized, and doubles as the rule-family composed exemplar for the deletion pass.
- This matrix **is** the proof-engine half of the canonical documentation the decision packet's outline (§(e)) called for; it is not a feeder to a separate proof-engine prose doc. The concise identity/boundary doc the packet outlines (prove vs govern, the runtime contract) remains a separate, shorter distillation that cites this matrix for proof-engine semantics rather than restating them. The gap-analysis protocol (packet §(a)4) evaluates cells against this matrix once ratified.
- The Q-items this structure is gated on: Q1 (write-plan decomposition), Q5 (activation sites), Q6 (dead rows), Q7 (§1b), Q10/Q13 (surface rulings), S4 (mention-set vocabulary), plus the class-(e) open item. Each is marked ⧖ at its point of use; none is answered here.

## Storage (owner-accepted 2026-07-19)

Per the independent evaluation in `obligation-discharge-matrix-2026-07-19-reviews/review-5-cell-storage-evaluation.md`:

- **Cells are structured data, never compiled into `src/`.** One data file per family/case-shape (JSON; in-repo precedent: `exhaustive-gap-analysis-2026-07-14/assembled/*.cells.json`), stable cell IDs as citation anchors, beside this document — promoted with it at ratification. The compiler never looks a cell up (it derives from the generative rules), so cells in the catalog would be a hand-maintained parallel copy of derived closure — the failure the catalog principle exists to prevent.
- **Human-readable cell tables are generated from the data** and never hand-edited (the `tmLanguage` discipline), drift-checked by a test.
- **A cell validator runs from day one** (`DiagnosticCoverageScanner`-shaped): disposition totality over the product, citation present per cell, axis values from named vocabularies (cross-checked against built catalogs where anchors exist), one near-miss per discharge addition, respellability verdict present, WP recomputation where mechanized. The definition-version stamp for certificate coupling rides in the data.
- **The generative layer enters the C# catalogs at rule-obligation design time** (the trigger this document already names): establishment/preservation `ProofRequirement` subtypes, premise classes, `CertificateStepKind`/`CertificatePremise`, the expression-shape classification, contract and suggestion metadata. Cell data then re-anchors to the new enums; this document's generative prose re-scopes to cite the catalog rather than restate it.
- **Interim tension, accepted**: until those catalog entries land, the generative rules live only in this document's prose while the data instantiates them — a derivation gap only partially machine-checkable (the mechanized WP calculator is the mitigation). The ratification protocol (what the owner reads, what machines verify, what gets spot-checked) is a required pre-population deliverable and is not settled here.

## Amendments (owner-approved 2026-07-19)

Two amendment classes, and only two — every change to the licensed set is one of them:

- **Power-widening** — a new licensed derivation (a new spelling that proves). Definition-edit-first: the derivation enters this document (and, once they exist, the catalogs) with an owner ruling before any compiler implements it. Monotone: everything previously licensed stays licensed. Minor definition version; previously issued certificates remain valid. Candidate pipeline: cells' sound-but-unprovable bands are the standing inventory — a band member that authors keep hitting in real precepts is the signal to consider licensing it.
- **Soundness-correction** — a licensed derivation discovered to be semantically unsound (it accepts a program that can violate its rule at runtime). Shrinking is permitted **only** under this class. Always a major definition version; always breaks certificate replay for affected cells; always routed to the owner with the **witness program** demonstrating the unsoundness. Never folded silently into a widening or a refactor.

A compiler change that alters the accepted set without a corresponding definition amendment is nonconforming in either direction — that is the exact-power contract doing its job.

## Ratification protocol (owner-approved 2026-07-19)

What "ratified" means for a definition too large to read line-by-line — three layers, all required:

1. **The owner reads the generative sections in full** — Vocabulary, case shapes, minting rule, discharge contracts, amendment rules. These are the rules of the game; they determine every cell.
2. **Machines verify every cell** — the day-one validator (Storage, above): disposition totality, citations present and resolving, axis-value validity, near-miss pairing, respellability verdicts present, a validity argument present for every rule the cell's derivation cites (Rule validity, above), a decision procedure named per contract, WP recomputation where mechanized. No cell ships unchecked.
3. **Spot-checks** — cells the owner picks, cells picked at random, and every cell an adversarial review pass flags. Note that `respellable: no` and `conflicted` cells reach the owner as decisions by their disposition rules regardless of this layer.
4. **Respellability is measured, not asserted** (owner-approved 2026-07-19, hard gate): before any family ratifies, its discharge contracts are run against the real sample corpus (the 59 rule-bearing files in `samples/`), and every rule × write-site obligation in those files is classified — licensed as written / needs a respelling (record it) / no licensed respelling exists. A family's respellability verdicts must agree with what the corpus shows, or the disagreement routes to the owner. Paper-only verdicts do not ratify.

The owner's ratification therefore certifies the generative rules and the checking regime — an honest statement of what a human sign-off over hundreds of agent-populated cells can be. Bulk cell content is never presented as owner-read.

## Eventual use — test basis

After the definitional pass, every defined cell converts mechanically into tests: base → expected diagnostic naming the obligation and all applicable missing-premise classes, **and no other diagnostics** (review-3 F4); base + each addition → clean compile via the expected strategy, premise list recorded; base + each near-miss → still rejects, same obligation (review-3 F1). Per family, the composed exemplar gets its deletion pass. Discipline carried over from the failed 2026-07-14 probe: every assertion reads **diagnostic codes / obligation records from `precept_compile`**, never prose summaries; each cell's expected outcome is stated by the definition before the run; edge cells are ledgered, not asserted. Not started; the definitional pass comes first.
