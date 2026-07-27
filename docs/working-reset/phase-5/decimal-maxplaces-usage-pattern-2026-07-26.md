# When should a decimal field use `maxplaces`?

**Prepared for Shane, 2026-07-26. Requested directly on 2026-07-26 to clarify whether financial
`decimal` fields should always carry `maxplaces`, or whether scale should be imposed only at settlement
and other domain boundaries.**

---

## The answer in one sentence

Do not put `maxplaces` on every financial `decimal` field. Put it on an input or stored result when the
domain contract limits that value's scale, leave intentional intermediate calculations unconstrained,
and call `round(..., places)` before assigning a higher-precision result to a constrained field.

## What `maxplaces` means

`maxplaces` is an invariant on a field or event argument. It is not a rounding instruction.

Assigning `1.999` to a field declared as `decimal maxplaces 2` violates the constraint. Precept does not
silently change the value to `2.00`. The `MaxPlacesExceeded` diagnostic, `PRE0067`, records the same
contract: a value with too many decimal places is rejected.

This meaning is stated in:

- [`primitive-types.md` § `decimal`](../../language/primitive-types.md#decimal), lines 226–249
- [`primitive-types.md` § Constraint Catalog](../../language/primitive-types.md#constraint-catalog),
  lines 581–597
- [`precept-language-spec.md` § 2.4](../../language/precept-language-spec.md), lines 1135–1158
- [`business-domain-types.md`](../../language/business-domain-types.md), lines 1577–1590

## What is not in question

Values whose domain contract limits scale should carry that limit. A tax rate that permits no more than
four decimal places, a settled USD amount that permits no more than two, or an event argument received
under such a contract are proper uses of `maxplaces`.

Rounding also has to be explicit when a full-precision calculation is moved into one of those values.
`round(value, places)` returns a `decimal` and uses banker's rounding. The language does not hide that
approximation inside assignment.

The question was whether the same constraint should be copied onto every financial intermediate. The
canonical documents answer no.

## Why intermediate calculations should usually remain unconstrained

The business-domain design states the reason directly:

> Intermediate calculations legitimately carry higher precision than the settlement currency's minor
> unit — forcing an implicit constraint at the field level would create spurious violations for
> perfectly valid arithmetic.

**Citation corrected 2026-07-26.** That sentence is real and sits at `business-domain-types.md:1764` —
but line 1764 is inside the *Rationale* paragraph of a decision block whose heading reads
`### ~~D10~~ — retired (Position 3: currency identity and value precision are decoupled)`. It is struck
through and marked retired, so it must not be cited as live canon.

**The conclusion survives without it**, on a line that is not retired —
[`business-domain-types.md:167`](../../language/business-domain-types.md):

> `money` | **Exact** | Backed by `decimal`; no float drift. Precision is the author's responsibility,
> opt-in via `maxplaces N`. ISO 4217 minor-unit data is available as metadata (`Currency.minorUnit`) but
> is **not** [applied implicitly].

Precision being opt-in and the author's responsibility is the whole basis for the rule below, and line
167 states it directly. Cite that.

**A question this raises for phase 2, not for this document.** What "retired" means here is not stated
anywhere. The body of D10 still asserts its conclusion in the present tense, and "Position 3" appears
exactly twice in the document — in that heading and nowhere else. So retired plausibly means renumbered
or absorbed rather than reversed, but canon does not say, and a struck-through decision whose content is
still being quoted as authority is the kind of thing that goes wrong quietly.

This matters because `decimal / decimal` retains full 28-digit precision. An accrued amount, ratio, or
allocation can therefore need more places while it is being calculated than the final value may store.
Constraining the intermediate does not make the calculation safer. It rejects an exact intermediate
before the author reaches the place where the domain actually calls for rounding.

The general money guidance makes the same distinction: calculations remain exact, while rounding is
explicit and appropriate for final billing amounts
([`business-domain-types.md`](../../language/business-domain-types.md), lines 167–176).

## The practical rule

Use `maxplaces` when the value itself has a scale contract:

- an input whose accepted representation is limited;
- a stored tax, rate, price, settlement, billing, or ledger value whose domain definition limits scale;
- an event argument governed by the same rule;
- a value crossing an external boundary that requires a fixed maximum scale.

Omit it when additional precision is intentional:

- intermediate arithmetic;
- ratios and allocation factors;
- accrued values that will be settled later;
- division results before their final rounding decision.

When a higher-precision value moves into a constrained value, round at that assignment:

```precept
field InterestRate as decimal maxplaces 6
field AccruedInterest as decimal
field SettledInterest as money in 'USD' maxplaces 2

set SettledInterest = round(AccruedInterest, 2)
```

The example shows two separate decisions. `InterestRate` carries a six-place contract because that is a
property of the stored rate. `AccruedInterest` carries no scale limit because it is an intermediate.
`SettledInterest` carries the two-place contract, and the assignment names the rounding that makes the
calculated value fit it.

## What remains open

The usage rule is settled in the canonical documents. What is not established by this research is
whether the compiler and diagnostics already enforce every part of it correctly, or whether a
precision-specific authoring pattern should be added to tooling. The patterns tool returned no
accessible guidance beyond the documents cited here.

An illustrative sample says, “Omit maxplaces on intermediate or ratio fields where higher precision is
intentional” in
[`insurance-claim-adjudication.precept`](../../../samples/insurance-claim-adjudication.precept), lines
19–28. That agrees with the canonical design, but the samples are examples rather than a specification.
No sample currently calls `round()`.

## Why this matches Precept's philosophy

Precept keeps exact values exact and requires approximation to be visible. An automatic scale reduction
on assignment would conceal a domain decision and could change a value without the author saying so.
An explicit `round()` at the point where a constrained settlement or external value is produced makes
that decision inspectable
([`philosophy.md`](../../philosophy.md), lines 22–25).

The resulting convention is deliberate rather than blanket: constrain scale where the domain requires
it, preserve precision while calculating, and round explicitly where the domain permits approximation.
