# Source excerpts — structural-fact-vs-value citation/obligation survey

Mirror of load-bearing external excerpts for
`research/architecture/compiler/structural-fact-vs-value-citation-obligation-survey.md`.
Fetched 2026-07-21. Preserves quotes against URL rot; the survey cites this file.

Two source PDFs were retrieved as binary and extracted with `pdftotext`; the full
extractions are retained alongside this file is not required — the load-bearing
excerpts are transcribed verbatim below with line references into the extraction.

---

## Dependent type theory — Harper, "Dependent Type Theory for Programming and Proving" (Spring 2026, CMU ATPL course notes)

Source: <https://www.cs.cmu.edu/~rwh/courses/atpl/pdfs/dependency.pdf> (accessed 2026-07-21). Primary (named author, CMU course notes by Robert Harper).

The four judgment forms (extraction lines 53–56):

> "1. Γ ⊢ 𝐴 type, stating that 𝐴 is a type in context Γ;
> 2. Γ ⊢ 𝐴 ≡ 𝐴′ type, stating that 𝐴 and 𝐴′ are equal types in context Γ;
> 3. Γ ⊢ 𝑀 ∶ 𝐴, stating that 𝑀 is a term of type 𝐴;
> 4. Γ ⊢ 𝑀 ≡ 𝑀 ′ ∶ 𝐴, stating that 𝑀 and 𝑀 ′ are equal elements of type 𝐴."

The context is pre-supposed, not proved (extraction lines 63–65):

> "Contrarily, the well-formation of the context is pre-supposed in these situations! That is, the successive types of the variables in a context are assumed to be well-formed types in the prefix of the context up to, but not including, that variable."

The variable / hypothesis rule `var-of` (extraction lines 134–137, Figure 1 "Structural Rules of Dependent Type Theory"):

> `var-of`
> Γ 𝑥 ∶ 𝐴 Γ′ ⊢ 𝑥 ∶ 𝐴

Accompanying structural principle (extraction line 145):

> "3. Variables inhabit the types according to their declaration in the context. Consequently, the induced entailment between types is reflexive."

The type-structure-as-classifier example (extraction lines ~19, Introduction):

> "a type such as 'sequence of length 𝑛' classifies finite sequences of values whose length is 𝑛 ≥ 0, and arrow types such as those between sequences of length 𝑛 and sequences of length 2×𝑛 may …"

Phase separation framing (Introduction):

> "Such languages are said to be phase separated in that type checking is regarded as a static, or compile-time, or verification-time, notion, whereas execution is regarded as a dynamic, or run-time, notion."

---

## Event-B — Abrial, Butler, Hallerstede, Hoang, Mehta, Voisin, "Rodin: An Open Toolset for Modelling and Reasoning in Event-B" (STTT vol. 12(6), 2010)

Source: <https://www.southampton.ac.uk/~tsh2n14/publications/journals/rodin-sttt2010.pdf> (accessed 2026-07-21). Primary (peer-reviewed journal, authored by the Event-B creators). DOI 10.1007/s10009-010-0145-y.

A pure typing invariant generates NO proof obligation (extraction lines 407–411):

> "At this stage the model results in no proof obligations since the invariant inv1 is nothing stronger than a typing constraint."

An invariant stronger than typing DOES generate preservation POs (extraction lines 413–431):

> "Now we add variables to represent the set of people who are in the building (in) and those that are outside the building (out). These are typed and constrained to be subsets of register through the following invariants:
> inv2 in ⊆ register
> inv3 out ⊆ register
> Note that while these invariants allow the type inference mechanism to infer the types of in and out, they are stronger than typing invariants since register is a variable and not a type."

> "The resulting model now gives rise to 6 proof obligations in total; 3 of these are to verify that the initialisation establishes invariants inv2 to inv4 and 3 are to verify that the register event maintains invariants inv2 to inv4."

Well-definedness obligation family (extraction lines 340–341):

> "The proof obligations being verified for the example are invariant preservation, refinement and well-definedness."

Supplementary (WebSearch summary, 2026-07-21, corroborating the WD family): "A WD condition is a predicate describing when an expression or predicate can be safely evaluated. For instance, the WD condition for x div y is y ≠ 0." — ScienceDirect "Proof Obligation" topic overview.

---

## Dafny — Leino, "Types in Dafny" (krml243, leino.science)

Source: <https://leino.science/papers/krml243.html> (accessed 2026-07-21). Primary (named author = Dafny designer, K. Rustan M. Leino).

Subset type: constraint checked at the assignment boundary (into the subset type), free the other direction:

> "An assignment from a subset type to its base type is always allowed. An assignment in the other direction, from the base type to a subset type, is allowed provided the value assigned does indeed satisfy the predicate of the subset type."

Newtype: constraint enforced on the operations themselves:

> "An important difference between the operations on a newtype and the operations on its base type is that the newtype operations are defined only if the result satisfies the predicate `Q`."

> "The newtype is distinct from and incompatible with other numeric types; in particular, it is not assignable to its base type without an explicit conversion."

---

## Eiffel — "ET: Design by Contract (tm), Assertions and Exceptions" (eiffel.org)

Source: <https://www.eiffel.org/doc/eiffel/ET-_Design_by_Contract_%28tm%29%2C_Assertions_and_Exceptions> (accessed 2026-07-21). Secondary (vendor/community documentation).

Class invariant is added to pre- and post-condition of every exported routine:

> "The class invariant, as noted, applies to all features. It must be satisfied on exit by any creation procedure, and is implicitly added to both the precondition and postcondition of every exported routine."

Assumed on entry, must be re-established on exit (paraphrase of the "good news / bad news" passage, verbatim fragments):

> "good news because it guarantees that the object will initially be in a stable state" [on entry] … "bad news because, in addition to its official contract as expressed by its specific postcondition, every routine must take care of restoring the invariant on exit."

AutoProof formulation (WebSearch summary of Eiffel AutoProof literature, 2026-07-21): "The AutoProof tool proves that the conjunction of the precondition and the class invariant before invocation ensures the conjunction of the postcondition and the class invariant after invocation." — corroborates assume-at-entry / establish-and-check-at-exit.

---

## TypeScript — "Narrowing", TypeScript Handbook

Source: <https://www.typescriptlang.org/docs/handbook/2/narrowing.html> (accessed 2026-07-21). Primary (official language handbook).

Definition of narrowing — flow-dependent refinement, not fixed at declaration:

> "TypeScript follows possible paths of execution that our programs can take to analyze the most specific possible type of a value at a given position. It looks at these special checks (called type guards) and assignments, and the process of refining types to more specific types than declared is called narrowing."

> "Much like how TypeScript analyzes runtime values using static types, it overlays type analysis on JavaScript's runtime control flow constructs like if/else, conditional ternaries, loops, truthiness checks, etc., which can all affect those types."

Example: a `number | string` parameter narrows to `number` inside `if (typeof padding === "number")` and to `string` on the other path — the type-level fact holds only within the branch.

---

## Whiley — flow-sensitive / structural typing (blurry case)

Sources: Pearce & Noble, "Structural and Flow-Sensitive Types for Whiley" (ECS tech report ECSTR10-23, <https://ecs.wgtn.ac.nz/foswiki/pub/Main/TechnicalReportSeries/ECSTR10-23.pdf>); "Flow-sensitive typing", HandWiki. Accessed 2026-07-21 (WebSearch summaries; direct fetch of the tech report PDF and HandWiki page failed at fetch time — see survey Threats to Validity).

WebSearch summary (2026-07-21): "the flow-sensitive and structural type system used in the Whiley language permits variables to be declared implicitly, have multiple types within a function, and be retyped after runtime type tests … testing a variable of type int | null with `x is int` narrows it to int in the true branch and null in the false."

Corroborated by the in-tree survey `research/architecture/compiler/type-proof-stage-contract-survey.md` (Whiley VC generation into WyAL, discharged by WyTP) — Whiley is both a flow-typed language (narrowing, blurry case) and a VC-generating verifier (type invariants discharged when a value enters the type).

---

## Liquid Haskell — environment/refinement split (reused from prior mirror)

Source: `research/references/interval-vs-value-evaluation/liquid-haskell-decidable-fragment.md` (Vazou, lh-course Lecture 01, Primary; captured 2026-06-05).

Subtyping rule keeps the base type `b` fixed and discharges only the refinement implication:

> "The subtyping rule for base types … is the following: {v:b | p₁} ⊆ {v:b | p₂}, if p₁ 'implies' p₂ for all values v of type b. To check implications, we use the below two implication rules that are based on the SMT solver."

The base type `b` is shared on both sides of `⊆` (assumed from the environment); only `p₁ ⇒ p₂` (the value refinement) is sent to the SMT solver as an obligation.
