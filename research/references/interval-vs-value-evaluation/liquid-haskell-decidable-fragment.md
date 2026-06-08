# Source mirror — Liquid Haskell refinement-type course (decidable fragment, SMT subtyping, precondition discipline)

Mirrored for the interval-vs-value-evaluation survey. Access date 2026-06-05.
Source grade: Primary for the course lecture (Vazou, UCSD/lh-course, authoritative author docs); ESOP 2021 tutorial paper (Jhala & Vazou, arXiv:2010.07763) cited as Primary by identifier (PDF body not text-extractable at fetch time — see Threats to Validity).

## Liquid Haskell course — Lecture 01: Refinement Types — https://nikivazou.github.io/lh-course/Lecture_01_RefinementTypes.html (accessed 2026-06-05)

On the predicate grammar (the decidable fragment):
> "Predicates... [include] variables, booleans, numbers, boolean operators, equality, linear arithmetic, [and] uninterp. functions"

On why the restriction (decidability + efficiency):
> "Liquid Haskell takes great care to ensure that type checking is decidable and efficient. To achieve this, it has to be careful to generate verification conditions that are decidable and efficiently checkable by the SMT solver."

On subtyping reducing to an SMT validity check:
> "The subtyping rule for base types... is the following: {v:b | p₁} ⊆ {v:b | p₂}, if p₁ 'implies' p₂ for all values v of type b. To check implications, we use the below two implication rules that are based on the SMT solver."
> "The T-Sub rule requires checking ∀v:b. p₁ ⇒ p₂ via SMT."

On the precondition/caller-obligation discipline:
> "div... [has a] refined type for the division operator that specified that the second argument must be non-zero."
(Signature shown: `div :: Int -> {v:Int | v /= 0} -> Int` — the caller must establish the non-zero precondition or type-checking fails.)

## Cross-reference: identical mechanics documented in the internal proof-engine survey

`research/architecture/compiler/proof-engine-interval-arithmetic-survey.md` § Liquid Haskell records (citing the ESOP 2021 tutorial, arXiv:2010.07763) that Liquid Haskell "does not use interval abstraction... Instead, it uses refinement predicates over the theory of linear arithmetic... The SMT solver (Z3) decides satisfiability of these formulas exactly," and that division safety is obtained "by typing the divisor as `{v:Int | v /= 0}`."
