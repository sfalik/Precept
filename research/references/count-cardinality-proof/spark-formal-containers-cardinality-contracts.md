# SPARK formal containers — capacity preconditions & membership-conditional set Length (mirror)

> Snapshot of the load-bearing SPARK contract text: how bounded formal containers prove
> they do not overflow capacity (a `Length < Capacity` **precondition** the caller
> discharges), and how a formal *set* `Insert` conditions the `Length + 1` on membership.
> Mirrored to defend the survey's "membership precondition + ghost flag" precedent.

**Source:** `AdaCore/SPARKlib`, `src/spark-containers-formal-doubly_linked_lists.ads` and
`src/spark-containers-formal-hashed_sets.ads`, fetched 2026-06-03 from the raw GitHub
master. Source grade: **Primary** (AdaCore's own annotated SPARK container specs, the
artifacts GNATprove proves against). Excerpts are quoted as returned by the fetch; the
`.ads` files are the authoritative source.

## Bounded list — capacity precondition on growth (`Append`, `Insert`)

```ada
--  Append (single element):
Pre    => (SPARKlib_Defensive => Length (Container) < Container.Capacity),
Post   => ... Length (Container) = Length (Container)'Old + 1 ...

--  Insert (single element):
Pre    => (SPARKlib_Defensive =>
             Length (Container) < Container.Capacity
             and then (Has_Element (Container, Before) or else Before = No_Element)),
Post   => (SPARKlib_Full => Length (Container) = Length (Container)'Old + 1),
```

The overflow guarantee is delivered by a **caller-discharged precondition** (`Length <
Capacity`), not by the analyzer forward-inferring the post-count. The list always grows
`+1` (duplicates allowed — list, not set).

## Formal hashed *set* — membership-conditional `Length + 1` (`Insert`)

Two `Insert` overloads. The first (with an `Inserted : out Boolean` ghost-style flag):

```ada
Pre  => (SPARKlib_Defensive =>
           Length (Container) < Container.Capacity
           or Contains (Container, New_Item))
--  Contract_Cases:
--    Contains (Container, New_Item)  => not Inserted and model unchanged (Length unchanged)
--    others                          => Inserted and Length = Length'Old + 1
```

The second (no flag — duplicate is an error):

```ada
Pre  => (SPARKlib_Defensive =>
           Length (Container) < Container.Capacity
           and then not Contains (Container, New_Item))
Post => Length (Container) = Length (Container)'Old + 1
        and Contains (Container, New_Item)
```

Reading: the set-add `+0 or +1` non-determinism is resolved by **requiring the caller to
establish membership status** at the call site. Overload 1 splits on `Contains` via
`Contract_Cases` and reports the actual outcome through the `Inserted` flag; overload 2
forbids the duplicate entirely via `not Contains` in the precondition. Either way the
analyzer never has to forward-infer `|s ∪ {x}|` from an unknown membership — the contract
makes membership a discharged precondition. The capacity precondition is also relaxed to
`Length < Capacity OR Contains` in overload 1, because a duplicate add cannot overflow.
