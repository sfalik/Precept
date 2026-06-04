# Dafny `DafnyPrelude.bpl` — set/multiset cardinality axioms (mirror)

> Snapshot of the load-bearing Boogie axioms governing how Dafny's verifier computes
> the cardinality of a set/multiset after adding one element. Mirrored to defend the
> survey's strongest precedent (membership-conditional `+0 or +1`) against URL rot.

**Source:** `DafnyPrelude.bpl`, vendored in `project-everest/vale` at
`tools/Dafny/DafnyPrelude.bpl` (the Dafny verifier's Boogie axiom prelude; the same
prelude ships in `dafny-lang/dafny`). Fetched 2026-06-03 from
`https://raw.githubusercontent.com/project-everest/vale/master/tools/Dafny/DafnyPrelude.bpl`.
Source grade: **Primary** (the verifier's own axiomatization).

## Set cardinality after adding one element (`Set#UnionOne` = add a single element)

```boogie
axiom (forall<T> a: Set T, x: T :: { Set#Card(Set#UnionOne(a, x)) }
  a[x] ==> Set#Card(Set#UnionOne(a, x)) == Set#Card(a));
axiom (forall<T> a: Set T, x: T :: { Set#Card(Set#UnionOne(a, x)) }
  !a[x] ==> Set#Card(Set#UnionOne(a, x)) == Set#Card(a) + 1);
```

`a[x]` is set membership (`x ∈ a`). The cardinality increases by 1 **only** when the
element is not already present (`!a[x]`); when it is present (`a[x]`) the cardinality is
**unchanged**. This is the membership-conditional `+0 or +1` delta verbatim.

## Set union cardinality (inclusion–exclusion)

```boogie
axiom (forall<T> a, b: Set T :: { Set#Card(Set#Union(a, b)) }{ Set#Card(Set#Intersection(a, b)) }
  Set#Card(Set#Union(a, b)) + Set#Card(Set#Intersection(a, b)) == Set#Card(a) + Set#Card(b));
```

`|a ∪ b| + |a ∩ b| = |a| + |b|` — the overlap (`Set#Intersection`) is what makes the
union cardinality less than the naive sum.

## Multiset cardinality after adding one element (`MultiSet#UnionOne`)

```boogie
axiom (forall<T> a: MultiSet T, x: T :: { MultiSet#Card(MultiSet#UnionOne(a, x)) }
  MultiSet#Card(MultiSet#UnionOne(a, x)) == MultiSet#Card(a) + 1);
```

A multiset add is **unconditionally** `+1` — no membership guard. This is the exact
set-vs-multiset asymmetry Precept's per-collection-kind determinism mirrors.
