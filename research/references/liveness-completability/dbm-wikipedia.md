# Difference Bound Matrix (DBM) — source mirror

Mirrored 2026-06-05 from https://en.wikipedia.org/wiki/Difference_bound_matrix
and https://handwiki.org/wiki/Difference_bound_matrix (corroborating).
Load-bearing for `liveness-completability-verification-survey.md` C2 (the existential
guard∧constraint feasibility test is DBM emptiness — polynomial, solver-free).

## Definition

> "a **difference bound matrix (DBM)** is a data structure used to represent some
> convex polytopes called **zones**."

A DBM is an (n+1) × (n+1) matrix with rows and columns indexed by {0, x₁, …, xₙ}.
Each entry contains a constraint pair: an operator (< or ≤) and a value m. The entry
at position (C, R) represents the constraint C − R ≺ m.

## Constraint class (zones)

> equations of the form "x ≤ c, x ≥ c, x₁ ≤ x₂ + c and x₁ ≥ x₂ + c."

Single-variable bounds (intervals) plus pairwise difference bounds. This is exactly
Precept's interval (`min`/`max`) + single-pass field-to-field relational fragment.

## Emptiness / satisfiability test

> "testing for zone emptiness consists in checking whether the canonical DBM of the
> zone consists only of (<, −∞)."

> "It suffices to apply the Floyd–Warshall algorithm to the graph… If this algorithm
> detects a cycle of negative length, this means that the constraints are not
> satisfiable, and thus that the zone is empty."

## Complexity

Floyd–Warshall over n+1 nodes → **O(n³)** time, O(n²) space (standard; corroborated by
HandWiki and SDBM-paper summaries: "the satisfiability of DBM constraints can be
determined in O(n³) time and O(n²) space"). Polynomial — no SMT/opaque solver required.

## Boundary (where the cheap fragment ends)

Strided/congruence-extended DBMs (SDBM) tip to NP-hard: "the SDBM satisfiability problem
is NP-hard, so no polynomial-time algorithm is likely to exist" (arXiv:2405.11244).
Full linear arithmetic with ≥3-variable coupling → Presburger (super-exponential).
These are the forms that must stay OUT of Precept's solver-free G5b feasibility check.
