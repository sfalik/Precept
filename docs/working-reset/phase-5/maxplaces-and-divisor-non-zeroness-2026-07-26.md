# How does `maxplaces` affect divisor non-zeroness?

**Prepared for Shane, 2026-07-26. This connects the decimal `maxplaces` usage guidance to the open
question in [`where-a-divisors-non-zeroness-comes-from.md`](where-a-divisors-non-zeroness-comes-from.md).
It does not decide whether a proof must use a fact declared on the divisor or may derive that fact from
the divisor's inputs.**

---

## The answer in one sentence

Leaving intentional intermediates free of `maxplaces` makes the underflow case easier to reach and
harder to rule out from declarations, while adding `maxplaces` can provide a useful magnitude bound
only when combined with non-zeroness and full arithmetic accounting; neither usage rule decides where
a divisor proof must obtain its non-zeroness.

## What the two questions constrain

`maxplaces N` limits scale: a value may have at most `N` digits after the decimal point. It does not
round a value, and it does not by itself say that the value is non-zero.

The divisor question is about magnitude as well as non-zeroness. A finite decimal value can begin
non-zero and become stored zero when arithmetic produces a result below the type's representable
floor. Knowing only that each input is non-zero is therefore insufficient.

These concerns meet because scale can sometimes imply a magnitude bound. For an exact decimal value,
the combination `nonzero maxplaces 4` excludes values between zero and `0.0001`: the smallest permitted
non-zero magnitude is `0.0001`. `maxplaces 4` alone does not provide that conclusion because zero is
still allowed.

This qualification matters. It is not quite right to say that a non-zero value with very few decimal
places can be arbitrarily close to zero. Under `maxplaces 4`, it cannot. It may still be small for the
domain, and values with many decimal places need not be small at all, but scale plus non-zeroness does
give a coarse lower bound on magnitude.

## An unconstrained intermediate can approach the decimal floor

The earlier `maxplaces` guidance says to leave ratios and deliberate intermediates unconstrained when
they need the full precision of the calculation. That remains the right scale-contract guidance. It
also leaves those fields able to hold any decimal scale the representation permits, including values
very close to zero.

This illustrative fragment is not a reproduction of current compiler acceptance. It shows the
arithmetic case that a sound proof procedure has to cover:

```precept
field BaseRatio as decimal nonzero default 0.0000000001 editable
field Dilution as decimal nonzero default 0.000000000001 editable

field StageOne as decimal <- BaseRatio / 1000000
field StageTwo as decimal <- StageOne * Dilution
field FinalRate as decimal <- StageTwo / 10

field Numerator as decimal default 1 editable
field Result as decimal <- Numerator / FinalRate
```

Starting from the defaults:

1. `BaseRatio` is `0.0000000001`, or `1e-10`.
2. `StageOne` is `1e-16`.
3. Multiplying by `Dilution`, which is `1e-12`, produces `1e-28`, around the decimal precision floor
   described in the divisor document.
4. Dividing that result by `10` asks the representation to store `1e-29`, below that floor, so the
   stored `FinalRate` can be zero.
5. The later division by `FinalRate` is then unsafe.

Every source value in the chain is genuinely non-zero. Every operation is ordinary arithmetic. No
`maxplaces` violation occurs because none of these intermediate fields has that constraint. Explicit
rounding at a settlement boundary would not help: the dangerous value is being used as a divisor
before any such boundary is reached.

Removing `maxplaces` did not create decimal underflow. The finite representation created it. The
absence of a scale bound does, however, make the set of permitted values include magnitudes all the
way down to the representation floor. A proof that tries to reason through the arithmetic then has no
declared scale quantum from which to derive a useful lower magnitude bound.

In that precise sense, the recommended unconstrained-intermediate pattern makes this hazard worse for
proof: it permits the difficult cases rather than excluding them. It is not a reason to reverse the
guidance, because constraining every intermediate would reject legitimate high-precision results for
an unrelated domain reason.

## What a scale-constrained divisor buys

Consider a stored divisor whose domain really does limit scale:

```precept
field ContractRate as decimal nonzero maxplaces 4 default 0.0001 editable
field Numerator as decimal default 1 editable
field Result as decimal <- Numerator / ContractRate
```

If the field's constraints are governed when values enter it, `ContractRate` cannot be zero and cannot
have a non-zero magnitude below `0.0001`. That is materially stronger than `nonzero` alone. It rules
out underflow having already turned the stored divisor into zero before this direct division.

This does not mean `maxplaces` is a general divisor-safety constraint:

- `maxplaces` without `nonzero` still permits zero.
- The bound is on scale, not a domain-authored minimum magnitude. `maxplaces 4 nonzero` happens to
  imply `abs(value) >= 0.0001`, but it cannot express a different minimum such as `0.05`.
- A field with many decimal places may be large, so scale does not classify values as generally small
  or large.
- A scale-constrained input can participate in later arithmetic that underflows.
- Placing `maxplaces` on a computed field would not make an unrepresentable mathematical result
  representable. It is an invariant, not a rounding operation.

The last two points are the important ones for composed divisors.

## Scale-constrained parts can still compose to zero

Even when every input has both `nonzero` and `maxplaces`, a proof cannot merely repeat “each input has a
minimum quantum.” It has to calculate the lower magnitude of the complete expression and compare that
with the decimal floor.

```precept
field FactorA as decimal nonzero maxplaces 10 default 0.0000000001 editable
field FactorB as decimal nonzero maxplaces 10 default 0.0000000001 editable
field FactorC as decimal nonzero maxplaces 10 default 0.0000000001 editable

field DerivedDivisor as decimal <- FactorA * FactorB * FactorC
field Numerator as decimal default 1 editable
field Result as decimal <- Numerator / DerivedDivisor
```

Each factor's smallest permitted non-zero magnitude is `1e-10`. Their mathematical product can
therefore be `1e-30`, below decimal's roughly `1e-28` floor. The scale declarations make the necessary
calculation possible, but they do not make its answer safe.

The number of operations matters too. Fifteen non-zero factors constrained to two decimal places may
each be no smaller than `0.01`, yet their product can be `1e-30`. A proof procedure that composes
non-zeroness must account for the whole expression, not inspect each declaration independently.

The reverse is also true: a high-scale value is not necessarily dangerous.

```precept
field PreciseRate as decimal nonzero maxplaces 20 default 12.12345678901234567890 editable
```

This value has many decimal places but is nowhere near zero. `maxplaces 20` tells the compiler what
scales are permitted; it does not say that every permitted value is tiny.

## The tension in the authoring guidance

The two reasonable authoring rules do not solve the same problem:

- Leaving an intermediate unconstrained preserves legitimate precision, but supplies no scale-derived
  lower magnitude for a composed non-zeroness proof.
- Constraining a value because its domain contract limits scale can contribute a lower magnitude when
  paired with `nonzero`, but applying that constraint merely to help the proof would reject valid
  higher-precision values and would confuse a representation contract with a safety precondition.
- Explicit `round(value, places)` correctly marks an allowed approximation at a domain boundary, but
  rounding a prospective divisor can itself produce zero. Rounding is therefore not a substitute for
  proving the rounded divisor non-zero.

There is real tension here. “Leave intermediates unconstrained” preserves arithmetic that the domain
allows, while “derive non-zeroness through the expression” needs magnitude information that an
unconstrained intermediate often lacks. Blanket `maxplaces` would provide more information in some
cases, but for the wrong reason and without guaranteeing safety.

The clean distinction is that `maxplaces` should remain a domain scale contract. If a future composed
proof can legitimately use `maxplaces` together with `nonzero`, explicit minimum bounds, and
lane-specific underflow arithmetic, that is useful evidence. It does not justify adding `maxplaces`
where the domain has no scale limit.

## What this adds to the open question

This analysis does not choose between declared-on-the-divisor proof and composed proof.

It sharpens the cost of the composed option: unconstrained ratios and intermediates are intentional and
common, so the compiler cannot expect every arithmetic input to provide a scale-derived magnitude
floor. Where scale constraints do exist, it may use them only as one input to a complete
magnitude calculation. It must still account for explicit bounds, operation depth, rounding, and the
representation rules of the numeric lane.

It also sharpens the strict option: requiring a fact on the final divisor avoids reconstructing the
whole expression at the division site, but the current computed-field `nonzero` hole described in the
other document must not be mistaken for a sound declaration. The fact has to be governed on the value
that is actually divided by, not merely written as an unchecked modifier.

The resulting conclusion is deliberately limited:

> `maxplaces` and divisor non-zeroness are related because scale plus non-zeroness can imply a lower
> magnitude. They remain different contracts. Omitting `maxplaces` can admit the near-zero values that
> make composed proof difficult; adding it can narrow those values but cannot by itself prevent
> underflow. Neither fact settles where a division proof must obtain its non-zeroness.
