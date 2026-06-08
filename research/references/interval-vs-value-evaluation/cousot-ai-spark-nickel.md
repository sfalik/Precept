# Source mirror — Cousot AI (POPL'77), Miné octagon (interval non-relational limit), SPARK contracts, Nickel contracts

Mirrored for the interval-vs-value-evaluation survey. Access date 2026-06-05.

## Cousot & Cousot 1977, "Abstract Interpretation: A Unified Lattice Model..."
Source grade: Primary (POPL 1977, pp. 238–252). Page: https://www.di.ens.fr/~cousot/COUSOTpapers/POPL77.shtml (accessed 2026-06-05)

> "Abstract interpretation of programs consists in using that denotation to describe computations in another universe of abstract objects"
> "so that the resulta of abstract execution give some informations on the actual computations" [sic — typos in source page]
> "It is shown that the program properties obtained by an abstract interpretation of a program are consistent with those obtained by a more refined interpretation"

Bibliographic: Conference Record of the Fourth ACM SIGPLAN-SIGACT Symposium on Principles of Programming Languages (POPL), pp. 238–252, Los Angeles, 1977. (The fetched page text reads "Sixth"/"1977"; the canonical citation is the Fourth POPL, 1977.)

## Miné, "The Octagon Abstract Domain" (interval domain is non-relational / cheap-but-imprecise)
Source grade: Primary (HOSC 2006; arXiv:cs/0703084). Mirrored full text at
`research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt` (lines cited below). Accessed via local mirror 2026-06-05.

Interval vs polyhedron domains and their representable invariant shapes (lines 95–104):
> "the lattice of intervals (described in Cousot and Cousot's ISOP'76 article [5]) and the lattice of polyhedra ... They represent, respectively, invariants of the form (v ∈ [c1 ; c2 ]) and (α1 v1 + · · · + αn vn ≤ c) ... Whereas the interval analysis is very efficient—linear memory and time cost—but not very precise, the polyhedron analysis is much more precise (Figure 2) but has a huge memory cost"

Relational invariant beyond the interval domain (lines 105–110):
> "the correctness of the program in Figure 1 depends on the discovery of invariants of the form (a ∈ [−m, m]) where m must not be treated as a constant, but as a variable—its value is not known at analysis time. Thus, this example is beyond the scope of interval analysis."

Octagon shape (lines 15–17, 29):
> "invariants of the form (±x ± y ≤ c), where x and y are program variables and c is a real constant."

Best-approximation figure caption (lines 243–244):
> "Fig. 2. A set of points (a), and its best approximation in the interval (b), polyhedron (c), and octagon (d) abstract domains."

Integer-as-real soundness framing (lines 38–40):
> "Our method works well for reals and rationals. Integer variables can be assumed, in the analysis, to be real in order to find approximate but safe invariants."

## SPARK / GNATprove — precondition = caller obligation; author must supply contracts
Source grade: Primary (AdaCore SPARK User's Guide).
https://docs.adacore.com/spark2014-docs/html/ug/en/source/subprogram_contracts.html (accessed 2026-06-05)

> "The precondition of a subprogram specifies constraints on callers of the subprogram."
> "When a caller is analyzed with GNATprove, it checks that the precondition of the called subprogram holds at the point of call."
> "The postcondition of a subprogram specifies partly or completely the functional behavior of the subprogram."
> "This verification is modular: GNATprove considers all calling contexts in which the precondition of the subprogram holds for the analysis of a subprogram."
> "It may be necessary to add a precondition to the subprogram for analyzing its callers."
> "The default precondition of `True` used by GNATprove when no explicit one is given may not be precise enough, unless it can be analyzed in the context of its callers."

(Cross-ref: `proof-engine-interval-arithmetic-survey.md` § SPARK Ada / GNATprove documents the "Interval" outcome bucket — "checks ... proved by a simple static analysis of bounds ... based on type bounds of sub-expressions" — and that Ada subtype range constraints like `type Score is range 0 .. 100` give GNATprove its initial interval bounds.)

## Nickel — contracts evaluate concrete values (the value-evaluation pole)
Source grade: Primary (official Nickel user manual).
https://nickel-lang.org/user-manual/contracts (accessed 2026-06-05)

> "To a first approximation, contracts are assertions. They check that a value satisfies some property at run-time."
> "When evaluating `x`, the following steps are performed: 1. Evaluate `1 + 1`. 2. Check that the result is a number."
> "If this contract would perform all the checks immediately, forcing the evaluation of most of the configuration, we would lose the benefits of laziness. Thus, _we want contracts to be lazy as well_."
