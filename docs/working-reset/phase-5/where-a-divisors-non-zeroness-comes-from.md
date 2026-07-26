# Where must a divisor's non-zeroness come from?

**Prepared for Shane, 2026-07-26. Raised at the phase-1 walk, item 14, where he said it had never been
put to him. Belongs to phase 5's discharge procedure for the fault family.**

**Reading time: about five minutes.** Every compile result below was reproduced at HEAD today and is
marked as such. They show what the current compiler does, never what it should do.

---

## The question in one sentence

To prove a division safe, does the compiler need the non-zeroness declared **on the divisor itself**, or
may it work the divisor out from the things it is made of?

## The thing that makes this awkward

Two numbers can both be genuinely non-zero and still multiply to exactly zero. Not through a trick —
through the type running out of room. `decimal` holds numbers down to about `0.0000000000000000000000000001`.
Multiply two values of `0.0000000000000001` and the true answer is a hundred million times smaller than
that, so what gets stored is zero. The approximate `number` lane has the same behaviour further out.

So "non-zero times non-zero is non-zero" is false, and cannot be what the compiler relies on. That much
is not a choice. What is open is what the author has to write instead, and it is more expensive than it
first appears.

## What today's compiler does

**It already refuses to reason from the parts.** Reproduced at HEAD:

```precept
field A as decimal nonzero default 0.0000000000000001 editable
field B as decimal nonzero default 0.0000000000000001 editable
field Product as decimal <- A * B
...
    -> set Result = Numerator / Product
```

> `PRE0083` — Division is unsafe: 'Product' can be zero in event handler 'Compute'

That is the strict behaviour, live today. So choosing strictness costs nothing against where we are.

**But the fix an author would reach for is unsound, and the compiler accepts it.** Adding `nonzero` to
the computed field — the obvious response to that message — makes the same file compile with zero
diagnostics, and the divisor obligation comes back **Proved**:

```precept
field Product as decimal nonzero <- A * B
```

Nothing checks that claim. `Product` is still zero whenever the operands are small enough, and the
division still faults. This is BUG-048, and today's reproduction has been added to it. **The single
modifier is what flips the file from correctly refused to wrongly accepted.**

That is the real state of things: the strict rule is in force, the escape hatch from it is unsound, and
an author following the diagnostics lands in the unsound place.

---

## Option one — non-zeroness must be declared on the divisor itself

A division discharges only from something attached to the value being divided by: a modifier on that
field, a condition testing that value, or a literal. Reasoning through a multiplication is never allowed.

**What the author writes instead.** They cannot declare it on the computed field — that is the unsound
path above, and it has to be closed either way. They have two honest options: guard the handler on the
divisor's value (`when Product != 0`), or stop deriving the divisor and make it a stored field with its
own declared constraint, governed as values enter.

**What it costs, stated as what stops compiling.** Any division whose divisor is a computed field stops
compiling until the author adds a guard at every site that divides by it. For a value computed once and
divided by in several handlers, that is a guard in each of them, all saying the same thing.

**What the author sees:**

> Cannot prove `Product` is non-zero. `A * B` can be zero even when `A` and `B` are not, because the
> result can be too small for `decimal` to hold. Add `when Product != 0` to this handler.

## Option two — the compiler may reason through the arithmetic, with the size accounted for

The compiler tracks how small a value can get, not just whether it can be zero. If `A` is at least
`0.01` and `B` is at least `0.01`, their product is at least `0.0001`, which `decimal` holds comfortably,
and the division is proven. If the operands' declared bounds allow them close enough to zero that the
product could underflow, it refuses — naming the bound that would fix it.

**What it costs.** Nothing stops compiling that compiles today; this is strictly more permissive. The
cost is in the machinery and the argument: the compiler needs a lower bound on magnitude for each
operand, and phase 6 owes a written argument that the underflow arithmetic is right for each lane —
`decimal`, `money`, `quantity`, and the approximate `number` lane, which behaves differently. Getting that
argument wrong is a silent unsound accept of exactly the kind above.

**What the author sees:**

> Cannot prove `Product` is non-zero. `A` can be as small as `0` and `B` as small as `0`, so their
> product can be too small for `decimal` to represent. Declaring `min 0.01` on `A` and `B` would make it
> provable.

---

## What is not being asked

Whether "non-zero times non-zero is non-zero" is acceptable reasoning. It is not, on any lane, and
neither option uses it.

Whether the modifier-on-a-computed-field hole gets closed. It does — that is BUG-048 and it is a defect
under either option.

## The honest summary

Option one is where the compiler already is, and it is sound. Its cost is that the natural way to express
"this derived value is never zero" is not available to the author, so they repeat a guard at every
division site.

Option two is what a domain expert would expect — they declared their inputs can't be small, so the
product can't be tiny — and it removes the repetition. It costs a piece of machinery that has to be right
per lane, and it is the kind of machinery that is wrong quietly rather than loudly.

**Not a decision for today, and it does not block anything before phase 5.** If you want it deferred,
option one is the safe resting place and option two can be added later without invalidating anything
built under option one.

---

## Prior art — added 2026-07-26

He asked whether there was any external research on this before ruling. There was not, so it was
commissioned: [`research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md`](../../../research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md).

Three things came back that bear on the choice.

**The question was already recorded in canon and left open.** `docs/language/primitive-types.md:667`:
*"Two non-zero finite `number` operands can multiply to exactly `0.0` … it breaks divisor safety
directly: `nonzero` on every operand does not yield a non-zero product. Recorded as an open question,
not yet routed."* So this is a known gap being closed, not a new discovery.

**The comparator that matters does what option one does, and names option two's machinery as the hard
part.** SPARK/GNATprove proves a divisor non-zero and models floating-point arithmetic as rounded rather
than real, so it cannot reason from operand non-zeroness either. Its own guide says the difficult checks
are the ones that *"don't depend simply on computing coarse static bounds for the result of
computations"* — which is precisely the magnitude reasoning option two needs. That is a cost signal, not
a prohibition.

**The representation choice removes the case rather than answering it.** A product of two non-zero
arbitrary-precision rationals is never zero — there is no smallest positive value to fall below. The
same holds for arbitrary-precision integers. So this question and the open arithmetic-model choice for
`integer` are closer to one decision than two: settling the representation first can make this one
vacuous, while settling this one does not constrain the representation.

**What the research explicitly does not settle**: what a magnitude-bound analysis costs to build, and
whether its diagnostic can be explained to a domain expert. Both are judgement calls it leaves to you.
It also examined only two comparators — Astrée, Frama-C, Polyspace and KeY were not looked at, and
Astrée is built for exactly this class of property.
