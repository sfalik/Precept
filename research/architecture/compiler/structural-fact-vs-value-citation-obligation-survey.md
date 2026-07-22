---
status: Cited — grounds the 2026-07-21 structural-fact citation-duty ruling in docs/Working/obligation-discharge-matrix-2026-07-19.md
authored: 2026-07-21
author: research (lifecycle-1)
topic: How proof-carrying / refinement / dependent / contract type systems draw the line between an immutable declaration-fixed type-structural fact (assumed from the typing context, never an obligation) and a value-fact established by an operation and preservable/invalidatable (must be cited/discharged) — grounding Precept's 2026-07-21 citation-duty ruling
external-engagement: strong
---

# Structural Fact vs Value Fact: What a Proof Must Cite vs What It May Assume

> When a proof consumes a fact, comparable systems split facts into two populations: an **immutable, declaration-fixed type-structural fact** — a value's base type, carrier set, dimension, or type invariant — which is *assumed from the typing context (Γ)* and never enters the proof as an obligation; and a **value-fact** — "this divisor is non-zero here," "this arg ≤ 250" — which is *established by an operation* and can be *invalidated by a later one*, so it must be discharged (and, where it can change, its preservation re-discharged). This survey establishes, with verbatim excerpts, which population each system puts a declaration-fixed type-level fact in, and reports the blurry cases where the line is genuinely not clean.

## Background

Precept's proof engine adopted a **citation duty** on 2026-07-21: any proof consuming a declared fact must name the obligation that **established** it and every obligation that **preserved** it, each discharged. That duty is satisfiable for value-facts (something establishes them; something can break them) but **structurally unsatisfiable for type-structural facts** — nothing establishes "this value is a USD currency amount" or "this period is date-dimensioned," and no operation can break it, so the establish-set and preserve-set are *empty*. An empty-set citation is not a discharged citation; it is a category error.

The pending owner ruling: should the citation duty treat a declared qualifier/modifier/dimension fact as **(a)** typing context — out of the duty's scope entirely, the way `x : USD-money` is a fact of the *environment* — or **(b)** require establishment/preservation machinery for it too (a synthetic "declared at line N, preserved by all N operations" citation)?

This is a **narrow, focused** question. It is not a re-survey of who generates obligations (that is `type-proof-stage-contract-survey.md`) nor of bounds-vs-value discharge (`interval-vs-value-evaluation-prior-art-2026-06-05.md`) nor of Precept's own compile/runtime boundary (`compile-time-guarantee-boundary-2026-06-10.md`). Those three establish adjacent ground and are cited, not repeated. The piece none of them answers directly: **for an immutable, declaration-fixed type-level fact, is it context (assumed) or an obligation (discharged)?**

### Spec-first anchor

Precept's spec already uses "structural fact," and the ruling's category is *narrower* than the spec's usage. `precept-language-spec.md:266`: the compiler "proves a **structural fact** — that the operand *carries* its constraint — never the concrete runtime value." `:270`: "the compiler's proof … rests on the structural fact that the value carries a declared constraint; governance makes that constraint true of the value at ingress."

The spec's "structural fact" is *general*: the operand carries **some** declared constraint (a modifier, a rule, an author guard, a statically-known safe value) — and several of those constraint sources are value-facts that governance must make true at ingress, i.e. exactly the population that *does* get established (by ingress) and preserved (by working-copy atomicity, §3A.4). The ruling's category is the **immutable subset**: the declared qualifier/modifier/dimension facts that are *fixed by the type* and that ingress does not "make true" because they are true by construction (a `money` field's currency dimension is not a runtime value governance checks — it is the field's type). So the spec's phrase and the ruling's category overlap but are not identical: the ruling asks how to treat the *type-structural residue* that has no establishment event at all.

## Methodology

- **Research question** — For each comparator: does an immutable, declaration-fixed type-level fact get **assumed from the typing context/environment (Γ)** and used without re-proof, or must it be **discharged as a proof obligation**? And where is that line genuinely blurry (a "structural" fact that a flow-narrowing, cast, or subtyping step can move)?
- **Search strategy** — Triaged the three named in-tree surveys first (they carry mirrored excerpts for Liquid Haskell, SPARK, Whiley, Dafny, F* that this survey reuses for the *mechanics* and extends on the *context-vs-obligation* axis). Then fetched fresh primary sources for the new angle: Harper's CMU dependent-type-theory course notes (the hypothesis/variable rule + context presupposition), the Rodin/Event-B STTT 2010 paper by the Event-B creators (the typing-invariant-generates-no-PO distinction), Leino's "Types in Dafny" (subset-type-at-assignment vs newtype-on-operations), eiffel.org Design-by-Contract (class invariant assume-at-entry/establish-at-exit), the TypeScript Handbook narrowing page and Whiley flow-typing sources (the blurry case). WebSearch/WebFetch over cs.cmu.edu, southampton.ac.uk, leino.science, eiffel.org, typescriptlang.org, ecs.wgtn.ac.nz. Load-bearing excerpts mirrored to `research/references/structural-fact-citation/`.
- **Inclusion criteria** — Systems that (a) have a notion of a value's *type* fixed at declaration AND (b) discharge some *correctness obligation* over values, so the assume-vs-discharge line is observable. Refinement-type checkers, dependent-type theories, VC-generating verifiers with subset/type invariants, contract systems with class invariants, and flow-typed languages all qualify.
- **Exclusion criteria** — The who-generates-obligations axis (covered in the stage-contract survey); the bounds-vs-value-folding axis (covered in the interval-vs-value survey); pure single-stage checkers with no obligation stage.
- **Source-grade mix** — Primary for Harper (CMU course notes, named author), Event-B/Rodin (peer-reviewed, creator-authored), Dafny "Types in Dafny" (designer-authored), TypeScript Handbook (official), Liquid Haskell (reused Vazou course mirror). Secondary for eiffel.org DbC (vendor doc) and the Whiley flow-typing WebSearch summaries (direct PDF/wiki fetch failed — flagged in Threats). One Tertiary: the F* environment-assumption reading is grouped with Liquid Haskell by analogy plus the prior survey, not a fresh F*-specific excerpt (flagged).
- **Time bounds** — Fetched 2026-07-21. Two source PDFs (Harper, Rodin) were retrieved as binary and extracted with `pdftotext`; full extractions retained at `research/references/structural-fact-citation/*.txt`.

## Findings

### The distribution in one table

Each row: is a **declaration-fixed type-structural fact** assumed from context, or discharged as an obligation? And separately, does the system generate **establishment + preservation** machinery for the *value/invariant* facts (the population Precept's citation duty is designed for)?

| System | Declaration-fixed type-structural fact | Value/invariant fact (establish + preserve) | Is the line clean? |
|---|---|---|---|
| **Dependent types** (Idris/Agda/Coq; Harper) | **Assumed from Γ.** The variable rule `Γ, x:A, Γ′ ⊢ x:A` takes a value's type as a *hypothesis*; "the well-formation of the context is pre-supposed." A `Vect n` is not a goal to prove when it is *given*. | Propositions you must *prove* are separate goals (`Γ ⊢ M : P`); an equality/length proof is a term you construct. | Clean — types are context; proofs are goals. Blurred only by dependency (a type *mentions* a value). |
| **Refinement types** (Liquid Haskell; F*) | **Base type assumed from Γ.** Subtyping `{v:b\|p₁} ⊆ {v:b\|p₂}` keeps base `b` *fixed on both sides*; only the refinement `p₁⇒p₂` is sent to SMT. | The **refinement predicate** is the obligation; flow narrowing establishes tighter refinements along paths (the value-fact channel). | **Blurry** — a "refinement" is a type-level annotation *and* a flow-sensitive, narrowable value-fact. |
| **Event-B** (Abrial et al.) | **Assumed — generates NO proof obligation.** "the model results in no proof obligations since the invariant … is nothing stronger than a typing constraint." | **Full establish + preserve machinery.** An invariant "stronger than typing" gets an INIT PO (establishment) + one preservation PO per event that mutates the variable. | Clean *by design* — the tool's own line: typing = context, invariant-over-a-variable = discharged. |
| **Dafny** (Leino) | **Subset type: checked once at the assignment boundary, then assumed.** "an assignment … to a subset type[] is allowed provided the value … satisfies the predicate." Afterward the fact rides with the type. | The assignment-time check is the establishment obligation; later reads assume it. **Newtype**: the predicate is re-imposed on *operations* (a stricter, closer-to-value model). | Mostly clean; subset-vs-newtype is exactly the "check-once then assume" vs "re-check on every op" fork. |
| **Contract systems** (Eiffel; SPARK) | **Class/type membership assumed.** The class invariant "is implicitly added to both the precondition and postcondition of every exported routine" — *assumed on entry*. | **Establish + preserve.** Invariant must be established by every creation procedure and *re-established on exit* of every exported routine (the preservation obligation). | Clean — type membership is context; the mutable-object invariant is establish+preserve. |
| **Flow-typed languages** (TypeScript; Whiley) | **Declared union type is the context**; a *narrowed* type is flow-dependent. "the process of refining types to more specific types than declared is called narrowing." | The narrowed fact holds *only within the guarded branch* — the archetype of a value-fact that a later step invalidates. | **Blurry** — narrowing makes a "type" fact behave exactly like a value-fact (established by a guard, invalid outside it). |

### 1. Dependent types — the type is a hypothesis in the context, never a goal

Harper's structural rules state the four judgment forms and then the variable rule as a *structural* (assumed) rule, not a provable one [Primary; Harper, "Dependent Type Theory for Programming and Proving," CMU ATPL, accessed 2026-07-21; mirrored `research/references/structural-fact-citation/source-excerpts.md`]:

> "Contrarily, the well-formation of the context is pre-supposed in these situations! That is, the successive types of the variables in a context are assumed to be well-formed types in the prefix of the context up to, but not including, that variable."

The variable / hypothesis rule (`var-of`, Figure 1 "Structural Rules of Dependent Type Theory"):

> Γ 𝑥 ∶ 𝐴 Γ′ ⊢ 𝑥 ∶ 𝐴

with the accompanying principle:

> "Variables inhabit the types according to their declaration in the context. Consequently, the induced entailment between types is reflexive."

This is the cleanest statement of Precept's option (a): a value's *type* is a **hypothesis discharged by lookup in the context**, not a goal proved from prior obligations. And the example is directly analogous to Precept's structural facts:

> "a type such as 'sequence of length 𝑛' classifies finite sequences of values whose length is 𝑛 ≥ 0"

"Sequence of length n," "this collection is ordered," "this period is date-dimensioned" are the same kind of fact: a *classifier* carried by the type, established by the declaration, invalidated by nothing. In a dependent-type setting you do not open a subgoal "prove this is a Vect" when a `Vect n` is handed to you — the `var-of` rule closes it by context lookup. What you *do* prove are the **propositions** (a separate `Γ ⊢ M : P` goal, a constructed proof term). The line is clean: **types are context, propositions are goals.** The only thing that blurs it is dependency itself — because a type may *mention* a value (`Vect n` depends on `n`), a change to `n` changes the type, but that is the type-former tracking a value, not the base classifier becoming provable/refutable.

### 2. Refinement types — base type assumed, refinement predicate discharged (and narrowable)

Liquid Haskell's subtyping rule holds the **base type fixed** and discharges only the **refinement** [Primary; Vazou lh-course Lecture 01; reused mirror `research/references/interval-vs-value-evaluation/liquid-haskell-decidable-fragment.md`]:

> "The subtyping rule for base types … is the following: {v:b | p₁} ⊆ {v:b | p₂}, if p₁ 'implies' p₂ for all values v of type b. To check implications, we use the below two implication rules that are based on the SMT solver."

The base type `b` appears identically on both sides — it is *assumed from the environment*, never re-proved; only `p₁ ⇒ p₂` (the value-level refinement) becomes an SMT obligation. This is a two-population split inside one syntactic construct: the **base** (structural, assumed) and the **refinement** (value, discharged). F* uses the same environment-assumption model (its VCs are computed against the typing environment; base/structural typing is assumed while refinement VCs are discharged) — grouped here by analogy and the prior stage-contract survey rather than a fresh excerpt (see Threats).

The refinement side is **the blurry case for the ruling.** A refinement is written like a type (`{v:Int | v /= 0}`) but is *flow-sensitive*: Liquid Haskell narrows refinements along guarded paths, so a fact that *looks* type-structural (it is in the type annotation) is actually established by flow and can fail to hold on another path. This is precisely why Precept's citation duty is coherent for the *refinement/qualifier-as-value* reading and incoherent for the *dimension-as-structure* reading — refinement types show the two can wear the same syntax.

### 3. Event-B — the tool draws Precept's exact line, and generates the machinery only for the value side

Event-B is the single most on-point comparator because its proof-obligation generator *explicitly* refuses to generate an obligation for a typing fact and *does* generate establishment + preservation obligations for an invariant over a mutable variable [Primary; Abrial, Butler, Hallerstede, Hoang, Mehta, Voisin, "Rodin: An Open Toolset for Modelling and Reasoning in Event-B," STTT 12(6), 2010; DOI 10.1007/s10009-010-0145-y; mirrored].

A pure typing invariant → **no obligation at all**:

> "At this stage the model results in no proof obligations since the invariant inv1 is nothing stronger than a typing constraint."

An invariant *stronger than typing* → establishment (INIT) + preservation (per-event) obligations:

> "Note that while these invariants allow the type inference mechanism to infer the types of in and out, they are stronger than typing invariants since register is a variable and not a type."

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

Read against Precept's ruling this is decisive precedent: Event-B's proof-obligation *meta-theory* (it ships a soundness argument for its PO generator) treats **type/carrier-set membership as assumed context** (`in ⊆ SET` where `SET` is a carrier set is *typing*, no PO) and **a constraint over a mutable variable as establish + preserve** (`in ⊆ register`, where `register` is a variable, is *stronger than typing*, so it gets an INIT PO and one preservation PO per mutating event). The discriminator is exactly Precept's: **is the fact fixed by the type (carrier set), or is it a property of a value that operations can change?** The former is context; the latter is establish+preserve — which is precisely the shape of Precept's citation duty. Event-B does **not** manufacture an establishment/preservation obligation for the typing fact; it says "no proof obligations."

### 4. Dafny — check once at the type boundary, then assume; newtype re-checks on operations

Dafny's subset type establishes the predicate **at the assignment boundary** and then lets it ride with the type [Primary; Leino, "Types in Dafny" (krml243), accessed 2026-07-21; mirrored]:

> "An assignment from a subset type to its base type is always allowed. An assignment in the other direction, from the base type to a subset type, is allowed provided the value assigned does indeed satisfy the predicate of the subset type."

Once a value has subset type `T`, downstream code *assumes* the predicate (it is discharged once, at entry to the type). The **newtype** is the contrasting model — the predicate is re-imposed on the *operations*:

> "An important difference between the operations on a newtype and the operations on its base type is that the newtype operations are defined only if the result satisfies the predicate `Q`."

So Dafny gives both poles inside one language: **subset type = establish-once-then-assume** (the value-fact-with-a-single-establishment shape) and **newtype = re-check-on-every-operation** (a per-operation preservation shape). Neither is "assume forever with no establishment": both have a real establishment event (assignment, or operation-result check). This matters for Precept — Dafny's *value* constraints always have an establishment site; it is only the *base numeric type itself* (`int`, `real`) that is pure assumed context.

### 5. Contract systems (Eiffel, SPARK) — type membership assumed; class invariant is establish + preserve

Eiffel's class invariant is folded into every routine's contract and **assumed on entry**, then re-established on exit [Secondary; eiffel.org "Design by Contract (tm), Assertions and Exceptions," accessed 2026-07-21; mirrored]:

> "The class invariant, as noted, applies to all features. It must be satisfied on exit by any creation procedure, and is implicitly added to both the precondition and postcondition of every exported routine."

The AutoProof verification condition makes the assume/establish split explicit: "the conjunction of the precondition and the class invariant before invocation ensures the conjunction of the postcondition and the class invariant after invocation" — i.e. the invariant is an *assumption* in the precondition and a *goal* in the postcondition (establishment by creation procedures + preservation by every exported routine). Meanwhile the object's **class/type membership** is never a proof obligation — it is the context that makes the invariant meaningful. This is Event-B's split in an OO idiom: type = context, mutable-object invariant = establish + preserve. SPARK behaves the same way (subtype range constraints supply assumed bounds; pre/postconditions are the discharged obligations — reused from `interval-vs-value-evaluation-prior-art-2026-06-05.md`).

### 6. The blurry cases — where "structural" and "value" genuinely merge

The ruling's risk lives here, and the survey reports it honestly rather than smoothing it over:

- **Flow narrowing makes a type-level fact behave like a value-fact.** TypeScript [Primary; TS Handbook "Narrowing," accessed 2026-07-21; mirrored]: "the process of refining types to more specific types than declared is called narrowing," overlaid on "control flow constructs like if/else … which can all affect those types." A `number | string` parameter *is* `number` only *inside* the guarded branch — established by the guard, invalid outside it. Whiley does the same with `x is int` narrowing `int|null` (WebSearch summary; the Whiley type system "permits variables to … have multiple types within a function, and be retyped after runtime type tests"). Here a *type* fact is exactly a value-fact: established by an operation, invalidated by leaving the branch. **This is the population Precept's citation duty is built for** — and it is written in type syntax.
- **Refinement-as-narrowable** (Liquid Haskell/F*, §2): the same construct is a type annotation *and* a flow-established value-fact.
- **Subset-vs-newtype** (Dafny, §4): "check once at the boundary" vs "re-check on operations" is a real fork even for the *same* predicate.
- **Casts / subtyping steps** can move a fact a reader would call structural: an unchecked downcast, a coercion, or a subsumption step changes the assumed type. In every surveyed system this is a *guarded* move (a checked cast, an explicit conversion — Dafny's newtype "is not assignable to its base type without an explicit conversion") — i.e. the systems make the boundary-crossing an explicit establishment event rather than a silent assumption.

The honest reading: the line is clean **only for facts that no operation can touch** — a base type, a carrier set, a class membership, a numeric kind. The moment a fact is *narrowable, castable, or predicate-shaped over mutable state*, systems move it into the establish-(and-preserve) population — and several do so while it still wears type syntax. Precept's declared **dimension** facts (currency, date-dimension, ordered-ness) sit on the clean side (nothing narrows a `money` field's currency dimension); its **qualifier/modifier** facts are the ones to check case-by-case, because some (a governed band that ingress makes true) are value-facts in the spec's own §0.7 sense.

## Threats to Validity

- **F* environment-assumption reading is Tertiary.** No fresh F*-specific excerpt was captured; F* is grouped with Liquid Haskell on the base-assumed/refinement-discharged model by analogy plus the prior stage-contract survey. Low risk (F*'s type-and-effect system is well established to compute VCs against the typing environment) but flagged; the load-bearing refinement-split claim is carried by the Liquid Haskell Primary excerpt.
- **Whiley flow-typing excerpts are Secondary/WebSearch-summary.** Direct fetch of the ECS tech report PDF and the HandWiki page failed at fetch time; the narrowing claim rests on the WebSearch summary + the in-tree stage-contract survey's Whiley row (which is Primary for the VC-generation mechanics, Secondary for the flow-typing detail). The blurry-case conclusion does not hinge on Whiley alone — TypeScript (Primary) carries the flow-narrowing point.
- **Eiffel doc is Secondary (vendor).** eiffel.org is the language vendor's documentation, not a peer-reviewed source; the assume-at-entry/establish-at-exit claim is corroborated by the independent AutoProof VC formulation (research literature) in the WebSearch summary.
- **Selection bias toward the "clean" cluster.** The comparator set was seeded from the ruling's brief (dependent, refinement, Dafny/Whiley, Event-B, Eiffel/SPARK). A system that manufactures a synthetic establishment/preservation obligation for a *pure typing fact* would be evidence for option (b); none of the surveyed systems does, but the set is not exhaustive (KeY, VeriFast, ACL2, Isabelle's type classes were not surveyed). If two or more mainstream verifiers generate PO-for-typing, the distribution shifts.
- **Precept-internal grounding is doc-only.** The spec-anchor reading (`:266`/`:270`) and the §0.7 governance-makes-it-true framing rest on the committed spec text, not on running the proof engine; a doc/code drift would shift how many qualifier facts are genuinely "value-facts governance establishes" vs pure type structure.
- **Reused-mirror recency.** Liquid Haskell/SPARK/Whiley/Dafny mechanics reuse mirrors captured 2026-05-29/2026-06-05; not re-fetched here.

## Implications for Precept

The distribution answers the ruling's core question with a strong, consistent signal, and pinpoints where care is still needed.

1. **The dominant practice is option (a): a declaration-fixed type-structural fact is typing context, not an obligation.** Every surveyed system that has such a fact assumes it from Γ / the carrier set / the class membership and never generates an establishment-or-preservation obligation for it. Event-B says so verbatim ("no proof obligations since the invariant is nothing stronger than a typing constraint"); dependent types close it by the `var-of` hypothesis rule; refinement types hold the base type fixed on both sides of `⊆`; Eiffel/SPARK assume type/class membership as context. **No surveyed system manufactures a synthetic "established at declaration, preserved by every operation" citation for a pure typing fact.** That is affirmative precedent that option (b) — building establishment/preservation machinery for type-structural facts — is *not* how comparable systems draw the line, and would be inventing obligations the field treats as vacuous.
2. **The citation duty is correctly shaped for the value/invariant population — and that population is real and large.** Event-B's establish (INIT PO) + preserve (per-event PO) for an invariant over a mutable variable *is* Precept's citation duty (establishing obligation + preserving obligations, each discharged). Eiffel's establish-by-creation + preserve-by-every-routine is the same. So the duty is not wrong — it is aimed at exactly the facts the field discharges. The fix the ruling contemplates is not to weaken the duty but to **scope it**: it applies to facts with a real establishment event (ingress-governed bands, flow-narrowed qualifiers, growable/shrinkable collection cardinality) and is *out of scope* for facts fixed by the type.
3. **The spec's own §0.7 vocabulary already contains the discriminator.** The spec says governance "makes that constraint true of the value at ingress" (`:270`) — a fact that *ingress makes true* has an establishment event (ingress) and a preservation obligation (working-copy atomicity, §3A.4). A fact that is *true by construction of the type* (a `money` field's currency dimension) has neither. The clean test, matching Event-B's "stronger than a typing constraint" line: **does any operation (including ingress) make this fact true, or is it fixed by the type?** If an operation makes it true → citation duty applies. If the type fixes it → typing context, out of scope. This gives the ruling a principled, precedented boundary rather than an ad-hoc carve-out.
4. **Guard the blurry middle explicitly.** The one caution the evidence raises: a fact written in *qualifier/modifier syntax* is not automatically type-structural. Flow-narrowing (TypeScript/Whiley/Liquid Haskell) shows type-syntax facts that are flow-established value-facts; Dafny's subset-vs-newtype shows the same predicate can be establish-once or re-check-per-op. So the scoping test must key on **"can an operation establish or invalidate it,"** not on **"is it written where types go."** A Precept qualifier that ingress establishes (a governed band) is in-scope for the duty; a Precept dimension that nothing can change is out. Deciding this per qualifier/modifier/dimension family (not by syntactic position) is the precise, evidence-backed way to apply the ruling.

## Conclusions

**Conclusion — The field overwhelmingly treats an immutable, declaration-fixed type-structural fact as typing context (assumed from Γ / the carrier set / class membership), NOT as an obligation; establishment/preservation machinery is generated only for facts an operation can establish or invalidate. This is affirmative precedent for the ruling's option (a), with the caveat that "type-structural" must be tested by invalidability, not by syntactic position.**

- **Rationale.** Across dependent types (the `var-of` hypothesis rule; context "pre-supposed"), refinement types (base type fixed on both sides of `⊆`, only the refinement discharged), Event-B (typing constraint → *no* proof obligation, verbatim; invariant-over-a-variable → INIT + per-event preservation POs), Dafny (subset predicate checked at the assignment *boundary* then assumed), and contract systems (class/type membership assumed at routine entry, invariant established + preserved), the split is consistent and the discriminator is uniform: **is the fact fixed by the type, or can an operation change it?** Not one surveyed system emits a synthetic establishment/preservation obligation for a fact nothing can change — Event-B's generator explicitly declines to. An empty-set citation for a type-structural fact would therefore be inventing an obligation the field treats as vacuous.
- **Alternatives considered and rejected.** (i) *Option (b) — build establishment/preservation machinery for type-structural facts too.* Rejected: no surveyed system does this; Event-B, the one system that ships a PO-generation soundness meta-theory, generates *nothing* for a typing constraint. Manufacturing "declared at N, preserved by all N ops" citations for facts nothing can break is vacuous bookkeeping with no precedent. (ii) *Scope the duty by syntactic position (anything in qualifier/modifier slots is exempt).* Rejected: flow narrowing (TypeScript/Whiley) and refinement narrowing (Liquid Haskell) prove that facts in type syntax can be flow-established value-facts; Dafny's subset-vs-newtype shows the same predicate can need per-op preservation. Syntactic position is the wrong key. (iii) *Collapse the distinction (treat all declared facts identically).* Rejected: every surveyed system maintains the two populations precisely because conflating them either over-generates vacuous obligations or under-checks mutable invariants.
- **Precedent.** Harper `var-of` + "context pre-supposed" (dependent types, Primary); Liquid Haskell base-fixed subtyping (Primary); Event-B "no proof obligations since … nothing stronger than a typing constraint" + establish/preserve POs for variable invariants (Primary, creator-authored); Dafny subset-at-assignment vs newtype-on-operations (Primary); Eiffel class-invariant assume-at-entry/establish-at-exit (Secondary); TypeScript/Whiley flow narrowing as the blurry counter-evidence (Primary/Secondary).
- **Tradeoff accepted.** Adopting option (a) means the citation duty is *silent* about type-structural facts — a reader auditing a proof will not see "USD dimension: assumed" spelled out as a discharged citation, the same way a Coq proof does not restate the context or Event-B does not emit a typing PO. The cost is that the *completeness* of the type-structural layer must be guaranteed elsewhere (the type checker / catalog), not by the citation ledger; the duty covers only what an operation can establish or break. This is the same division of labor Event-B, dependent types, and Eiffel all accept — soundness of the typing layer is a *separate* guarantee from the obligation ledger, not an entry in it.

## What would change this conclusion

- If **two or more mainstream verifiers** are found that generate an explicit establishment-and-preservation proof obligation for a *pure typing/carrier-set fact* (not an invariant over mutable state), the "field uniformly treats type-structural facts as context" claim weakens toward option (b).
- If Precept's **declared qualifier/modifier/dimension facts turn out to be predominantly ingress-established value-facts** (governance makes them true) rather than type-fixed structure, then most of them fall *inside* the citation duty already and the ruling's "empty-set" problem shrinks to a small residue — changing the scope of the carve-out, not its direction.
- If **flow narrowing / casts** in Precept can move a fact a reader calls "dimension-structural" (e.g. a coercion that re-dimensions a `money` value), the clean/blurry boundary shifts and more facts need the invalidability test — strengthening caveat (4) into a central concern.

## Open Questions

- **Per-family classification of Precept's declared facts.** The survey supplies the *test* (can an operation establish or invalidate it?) but not the *verdict* for each Precept qualifier/modifier/dimension family. Applying the test to `money` currency dimension vs a governed numeric band vs collection ordering vs `notempty` is Precept-internal work for the ruling's design pass, not a comparator question.
- **Where the type-structural completeness guarantee lives if the duty is silent about it.** Option (a) says the citation ledger does not cover type-structural facts; the soundness of *that* layer then rests on the type checker + catalog. Whether Precept's type checker already fully guarantees the type-structural layer (so the duty can safely ignore it) is an implementation-state question.
- **F* and additional verifiers (KeY, VeriFast).** Not excerpt-grounded here; a follow-up could confirm F*'s environment-assumption model with a primary excerpt and test whether any additional verifier generates PO-for-typing (the falsifier above).

## Sources

- **Harper, R. "Dependent Type Theory for Programming and Proving."** CMU ATPL course notes, Spring 2026. <https://www.cs.cmu.edu/~rwh/courses/atpl/pdfs/dependency.pdf>. Primary (named author). Accessed 2026-07-21. Mirrored: `research/references/structural-fact-citation/harper-dependency-atpl-2026.txt` + excerpts in `source-excerpts.md`.
- **Abrial, J-R.; Butler, M.; Hallerstede, S.; Hoang, T.S.; Mehta, F.; Voisin, L. "Rodin: An Open Toolset for Modelling and Reasoning in Event-B."** Int. J. Software Tools for Technology Transfer 12(6), 2010. DOI 10.1007/s10009-010-0145-y. <https://www.southampton.ac.uk/~tsh2n14/publications/journals/rodin-sttt2010.pdf>. Primary (peer-reviewed, creator-authored). Accessed 2026-07-21. Mirrored: `research/references/structural-fact-citation/rodin-event-b-sttt-2010.txt` + `source-excerpts.md`.
- **Leino, K.R.M. "Types in Dafny" (krml243).** <https://leino.science/papers/krml243.html>. Primary (Dafny designer). Accessed 2026-07-21. Mirrored: `source-excerpts.md`.
- **Eiffel — "ET: Design by Contract (tm), Assertions and Exceptions."** eiffel.org. <https://www.eiffel.org/doc/eiffel/ET-_Design_by_Contract_%28tm%29%2C_Assertions_and_Exceptions>. Secondary (vendor doc). Accessed 2026-07-21. Mirrored: `source-excerpts.md`.
- **TypeScript Handbook — "Narrowing."** <https://www.typescriptlang.org/docs/handbook/2/narrowing.html>. Primary (official handbook). Accessed 2026-07-21. Mirrored: `source-excerpts.md`.
- **Whiley — Pearce & Noble, "Structural and Flow-Sensitive Types for Whiley"** (ECSTR10-23) + "Flow-sensitive typing" (HandWiki). Secondary (direct fetch failed; WebSearch summaries — see Threats). Accessed 2026-07-21.
- **Vazou, N. "Refinement Types" (lh-course Lecture 01).** Primary. Reused mirror: `research/references/interval-vs-value-evaluation/liquid-haskell-decidable-fragment.md`.

### Prior in-tree surveys extended (reused, not duplicated)

- `research/architecture/compiler/type-proof-stage-contract-survey.md` — who generates obligations vs assumes context (Whiley/Dafny/Frama-C/SPARK/F*/Viper/Liquid Haskell/Rust/Roslyn/K2). Reused for the mechanics; this survey adds the immutable-type-fact-context-vs-obligation axis.
- `research/architecture/compiler/interval-vs-value-evaluation-prior-art-2026-06-05.md` — Liquid Haskell divisor `{v≠0}`, CUE values-become-bounds, SPARK Interval / subtype-range-as-bound. Reused for the refinement/SPARK rows.
- `research/architecture/compiler/compile-time-guarantee-boundary-2026-06-10.md` — Precept's own structural-fact-vs-concrete-value boundary and the §0.7 governance-makes-it-true framing. Reused for the spec-anchor bridge.

### Precept canonical anchors

- `docs/language/precept-language-spec.md:266`, `:270` — "structural fact — that the operand carries its constraint," "governance makes that constraint true of the value at ingress." Canonical.
- `docs/language/precept-language-spec.md` §3A.4 — working-copy atomicity (the preservation mechanism for ingress-established value-facts). Canonical.

## Inbound citation

This survey is load-bearing for the 2026-07-21 citation-duty ruling (treat type-structural facts as typing context — option (a) — vs build establishment/preservation machinery — option (b)). The ruling / consuming design pass should cite this file for the distribution of practice and the invalidability-not-syntactic-position scoping test. Filed as `Active — horizon groundwork`; the consuming decision lives in the ruling's spec/design edit, not here.
