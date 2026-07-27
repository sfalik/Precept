# Constraint modifiers on computed fields — what the compiler checks, and what it does not

**Status**: Measurement — 2026-07-26. Every result below was produced by `precept_compile` at HEAD
during this pass. Nothing here is quoted from a previous session.

**What this measures**: whether declaring a constraint modifier on a field whose value comes from an
expression produces any proof obligation, and whether a later proof can consume that modifier as a
premise anyway.

**What this is not**: a design. Several results below have no fix that does not add or change
language surface. Where that is true this document says so and stops.

## Read this first

Four of the seven numeric modifiers, both string length modifiers, and the collection count modifier
produce **no proof obligation at all** when the field's value comes from an expression. Three of
those unchecked modifiers are then read back as proven premises by a later proof. `maxplaces` is the
worst of the set in one specific way: it is checked against literal values only, it is checked
nowhere else in the language, and the proof engine does not contain the string `maxplaces` at all.

`min` and `max` are the exception. They route through the interval containment machinery and are
genuinely proven or genuinely refused.

The single sharpest result: `min 0` and `nonnegative` mean the same thing, and on the same computed
field one of them refuses the definition while the other compiles clean.

## The table

For a `decimal` computed field written `field C as decimal <modifier> <- Rate * Factor`.

| Modifier | Obligation produced | Consumable as a premise | Evidence |
|---|---|---|---|
| `nonzero` | **No** | **Yes** — `Divisor must be non-zero`, `Proved` by `DeclarationAttribute` | [A3](#a3), [C1](#c1) |
| `positive` | **No** | **Yes** — same divisor proof, `Proved` by `DeclarationAttribute` | [A4](#a4), [C3](#c3) |
| `nonnegative` | **No** | **Yes** — `Argument must be non-negative` for `sqrt`, `Proved` by `DeclarationAttribute` | [A5](#a5), [C2](#c2) |
| `min N` | **Yes** — `IntervalContainment` | n/a — it is established, so there is no unearned premise | [A6](#a6), [B2](#b2) |
| `max N` | **Yes** — `IntervalContainment` | n/a — same | [A2](#a2), [B2](#b2) |
| `maxplaces N` | **No** | **No** — nothing in the language consumes it | [A1](#a1), [C4](#c4), [D](#d-source-check) |
| `maxlength N` (string) | **No** | Not tested — no consumer found | [A7](#a7) |
| `minlength N` (string) | **No** | Not tested — no consumer found | [A7](#a7) |
| `mincount N` (collection) | **No** | **Yes** — `List must be non-empty`, `Proved` by `DeclarationAttribute` | [C5](#c5) |

So it is **not** a numeric problem. It reaches strings and collections too. The one thing that
works — interval containment — works on exactly two modifiers.

### A second dimension the brief did not ask about, which turns out to matter

The same measurement run against a **stored** field assigned from an expression inside an event
handler shows the hole is not confined to `<-` fields:

| Modifier | Stored field, literal default | Stored field, assigned from an expression | Computed field (`<-`) |
|---|---|---|---|
| `min` / `max` | obligation, `Proved` by `Literal` | obligation, `IntervalContainment` | obligation, `IntervalContainment` |
| `maxlength` / `minlength` | obligation, `LengthContainment` | obligation, `LengthContainment` | **nothing** |
| `nonzero` / `positive` / `nonnegative` | obligation, `Proved` by `Literal` | **nothing** | **nothing** |
| `maxplaces` | `PRE0067`, a type-checker literal check — not an obligation | **nothing** | **nothing**, even when the expression is a bare literal |
| `mincount` | not measured this pass | **nothing** (already filed as BUG-050 symptom B) | **nothing** |

Two readings fall out of that table:

- The **sign modifiers** (`nonzero`, `positive`, `nonnegative`) are unchecked against *any* expression,
  in a computed field or a stored one. They are checked only against a literal default.
- The **string length modifiers** are checked against expressions in a stored field and not in a
  computed field, so for strings the hole really is specific to `<-`.
- `maxplaces` is checked against literals and nothing else, anywhere.

## What rounding does — it makes things worse, not better

The brief asked whether `round(value, places)` closes the `maxplaces` hole. It does not, and the
measurement shows something stronger than "it does not help".

**Nothing is checked either way.** A correct rounding and an obviously wrong one compile
identically, both with an empty `proofObligations` array:

- `field C as decimal maxplaces 2 <- round(Rate * Factor, 2)` — clean, no obligations ([B3](#b3)).
- `field C as decimal maxplaces 2 <- round(Rate * Factor, 6)` — clean, no obligations ([B4](#b4)).

The second one declares at most two decimal places and computes six. The compiler cannot tell it
from the first, because it never looks at either.

**And calling `round` destroys the one check that does work.** With both operands bounded to
`[0 .. 10]`, the plain product proves and the rounded product does not — same file, same operands
([B5](#b5)):

```
Plain    <- A * B              computedInterval "[0 .. 100]"       Proved
Rounded  <- round(A * B, 2)    computedInterval "[−∞ .. +∞]"       Unresolved  →  PRE0078
```

`round` is opaque to the interval machinery. It throws away every bound its argument had. So an
author who reaches for `round` to make a value well-behaved gets a value the compiler knows *less*
about than the one they started with, and if that field also carries `min`/`max` the definition is
refused.

**A `rule` does not close it either.** `rule C == round(C, 2) because "…"` parses and compiles, and
adds no proof obligation ([B6](#b6)). It is runtime governance, not a compile-time proof.

## What an author can write today that is actually provable

For a computed `decimal` that must stay within two decimal places: **nothing.**

There is no form that compiles clean *because something was proven*. Every clean compile in this
category is clean because nothing was checked. The distinction the brief asked for has an empty
column on one side:

| Form | Compiles clean | Because something was proven? |
|---|---|---|
| `field C as decimal maxplaces 2 <- A * B` | yes | **no — zero obligations** |
| `field C as decimal maxplaces 2 <- round(A * B, 2)` | yes | **no — zero obligations** |
| `field C as decimal maxplaces 2 <- round(A * B, 6)` | yes | **no — zero obligations**, and it is wrong |
| `field C as decimal maxplaces 2 <- 3.9525` | yes | **no — zero obligations**, and it is wrong |
| `field C as decimal maxplaces 2 <- A * B` plus `rule C == round(C, 2)` | yes | **no — the rule adds no obligation** |
| `field C as decimal min 4 max 9 <- A * B` (operands bounded) | yes | **yes** — `IntervalContainment`, `Proved`. But this bounds the *magnitude*, not the number of decimal places. It is not the constraint the author wanted. |

The last row is the only genuinely proven thing available, and it answers a different question.

**Closing this needs new language surface or new machinery — it is not a bug fix.** There is no
existing construct that expresses "this expression yields at most N decimal places" in a form the
proof engine can discharge, and `round` currently loses information rather than supplying it. That
routes to `/design`. This document stops here.

## What I compiled, and what came back

Every block below is the exact text passed to `precept_compile` and the exact JSON returned,
trimmed only of the `summary` and `eventHandlers` fields where they carry nothing.

### A — does declaring the modifier on a computed field produce an obligation?

#### A1

The original observation from the brief, re-verified rather than trusted.

```precept
precept ComputedMaxplaces

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field Adjustment as decimal maxplaces 2 <- Rate * Factor
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

Confirmed. No obligation of any kind.

#### A2

The control that shows the machinery works at all for stored fields and literal defaults.

```precept
precept StoredBaselineOk

field A as decimal maxplaces 2 default 1.99 editable
field B as decimal nonzero default 1.0 editable
field C as decimal positive default 1.0 editable
field D as decimal nonnegative default 1.0 editable
field E as decimal min 5 default 6.0 editable
field F as decimal max 10 default 6.0 editable
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"Numeric","disposition":"Proved","strategy":"Literal","description":"Default value of 'B' must satisfy 'nonzero'"},
 {"kind":"Numeric","disposition":"Proved","strategy":"Literal","description":"Default value of 'C' must satisfy 'positive'"},
 {"kind":"Numeric","disposition":"Proved","strategy":"Literal","description":"Default value of 'D' must satisfy 'nonnegative'"},
 {"kind":"Numeric","disposition":"Proved","strategy":"Literal","description":"Default value of 'E' must satisfy 'min'"},
 {"kind":"Numeric","disposition":"Proved","strategy":"Literal","description":"Default value of 'F' must satisfy 'max'"}]}
```

Five modifiers, five obligations. Note that `maxplaces` on field `A` produced **no** obligation even
here — it is a type-checker literal check, not part of the proof system. Compiling the same file with
`default 1.999` gives `PRE0067` — *"Value has 3 decimal places, but field 'A' allows at most 2"* —
and still an empty `proofObligations` array.

#### A3

```precept
precept ComputedNonzeroOnly

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field C as decimal nonzero <- Rate * Factor
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

#### A4

```precept
precept ComputedPositiveOnly

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field C as decimal positive <- Rate * Factor
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

#### A5

```precept
precept ComputedNonnegOnly

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field C as decimal nonnegative <- Rate * Factor
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

#### A6

The same file with `min 0` instead of `nonnegative`. **These two declarations mean the same thing.**

```precept
precept MinZeroVsNonneg

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field ViaMinZero as decimal min 0 <- Rate * Factor
field ViaNonneg as decimal nonnegative <- Rate * Factor
```

```json
{"success":false,"diagnosticCount":1,
 "diagnostics":[{"line":5,"column":38,"severity":"error","code":"PRE0078",
   "message":"Numeric computation exceeded the representable range on field 'ViaMinZero'"}],
 "proofObligations":[
 {"kind":"IntervalContainment","disposition":"Unresolved",
  "description":"Interval containment: computed value of 'ViaMinZero' must be within declared bounds [0 .. +∞]",
  "computedInterval":"[−∞ .. +∞]","targetField":"ViaMinZero","declaredMin":0,"normalizedDeclaredMin":0}]}
```

One obligation, not two. `min 0` on line 5 is checked and the definition is refused. `nonnegative` on
line 6 — the identical constraint on the identical expression — produces nothing and would compile
clean on its own. The author's choice of spelling decides whether the compiler looks.

The mechanical reason is in `src/Precept/Pipeline/ProofEngine.Intervals.cs:405`, where the declared
bound is read from the field:

```csharp
var declarationBound = modifierKind switch
{
    ModifierKind.Min => field.NormalizedDeclaredMin ?? field.DeclaredMin,
    ModifierKind.Max => field.NormalizedDeclaredMax ?? field.DeclaredMax,
    _ => null
};
```

`Min` and `Max` are the only two modifiers that reach the interval machinery. The sign modifiers fall
through to `null`.

#### A7

Both string length modifiers on a computed field.

```precept
precept ComputedStringLen

field A as string default "hello" editable
field B as string default "world" editable
field C as string maxlength 3 <- A + B
field D as string minlength 100 <- A + B
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

`A + B` is `"helloworld"` — ten characters. `C` declares at most three and `D` declares at least a
hundred. Both are wrong, both compile clean, neither produces an obligation.

The stored-field control does produce one, which is what makes this specific to computed fields for
strings. `field C as string maxlength 3 default "hello"` gives `PRE0135` plus a `LengthContainment`
obligation; and assigning an expression to a stored field inside a handler —
`set T = A + B` where `T` is `string maxlength 3` — also gives `PRE0135` and a `LengthContainment`
obligation reading *"Length containment: T must have length in [0 .. 3]"*, `Unresolved`.

### B — the interval machinery, and what rounding does to it

#### B1

Interval containment genuinely works when the operands carry bounds.

```precept
precept BoundedOperands

field Rate as decimal min 2 max 3 default 2.5 editable
field Factor as decimal min 2 max 3 default 2.5 editable
field C as decimal min 4 max 9 <- Rate * Factor
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"IntervalContainment","disposition":"Proved","strategy":"IntervalContainment",
  "description":"Interval containment: computed value of 'C' must be within declared bounds [4 .. 9]",
  "computedInterval":"[4 .. 9]","targetField":"C","declaredMin":4,"declaredMax":9,
  "normalizedDeclaredMin":4,"normalizedDeclaredMax":9}, … default-value obligations …]}
```

This is the one thing in the whole measurement that is clean *because it was proven*.

#### B2

Unbounded operands, so the same obligation cannot discharge and the definition is refused.

```precept
precept ComputedMinOnly

field Rate as decimal default 1.5 editable
field Factor as decimal default 2.5 editable
field C as decimal min 5 <- Rate * Factor
```

```json
{"success":false,"diagnosticCount":1,
 "diagnostics":[{"line":5,"column":29,"severity":"error","code":"PRE0078",
   "message":"Numeric computation exceeded the representable range on field 'C'"}],
 "proofObligations":[{"kind":"IntervalContainment","disposition":"Unresolved",
  "description":"Interval containment: computed value of 'C' must be within declared bounds [5 .. +∞]",
  "computedInterval":"[−∞ .. +∞]","targetField":"C","declaredMin":5,"normalizedDeclaredMin":5}]}
```

Refused, correctly. This is the behaviour the other modifiers should have and do not.

#### B3

Rounding to the declared number of places. This is the form an author would reach for.

```precept
precept ComputedMaxplacesRound

field Rate as decimal default 1.55 editable
field Factor as decimal default 2.55 editable
field C as decimal maxplaces 2 <- round(Rate * Factor, 2)
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

**Nothing was checked.** Not "it was checked and passed" — the `proofObligations` array is empty.

#### B4

The same file rounding to six places while declaring two. This is wrong on its face.

```precept
precept ComputedMaxplacesRoundWrong

field Rate as decimal default 1.55 editable
field Factor as decimal default 2.55 editable
field C as decimal maxplaces 2 <- round(Rate * Factor, 6)
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

Byte-identical outcome to B3. The compiler cannot distinguish the correct rounding from the incorrect
one. A bare literal behaves the same way — `field D as decimal maxplaces 2 <- 3.9525` compiles clean
with zero obligations, so even the literal check that catches `default 1.999` does not reach a `<-`
field.

#### B5

Rounding erases the bounds. Same file, same operands, one call to `round` between them.

```precept
precept RoundErasesBounds

field A as decimal min 0 max 10 default 1.55 editable
field B as decimal min 0 max 10 default 2.55 editable
field Plain as decimal min 0 max 100 <- A * B
field Rounded as decimal min 0 max 100 <- round(A * B, 2)
```

```json
{"success":false,"diagnosticCount":1,
 "diagnostics":[{"line":6,"column":43,"severity":"error","code":"PRE0078",
   "message":"Numeric computation exceeded the representable range on field 'Rounded'"}],
 "proofObligations":[
 {"kind":"IntervalContainment","disposition":"Proved","strategy":"IntervalContainment",
  "description":"Interval containment: computed value of 'Plain' must be within declared bounds [0 .. 100]",
  "computedInterval":"[0 .. 100]","targetField":"Plain",
  "declaredMin":0,"declaredMax":100,"normalizedDeclaredMin":0,"normalizedDeclaredMax":100},
 {"kind":"IntervalContainment","disposition":"Unresolved",
  "description":"Interval containment: computed value of 'Rounded' must be within declared bounds [0 .. 100]",
  "computedInterval":"[−∞ .. +∞]","targetField":"Rounded",
  "declaredMin":0,"declaredMax":100,"normalizedDeclaredMin":0,"normalizedDeclaredMax":100},
 … default-value obligations …]}
```

`A * B` is known to lie in `[0 .. 100]`. `round(A * B, 2)` — which cannot leave that range — is known
to lie in `[−∞ .. +∞]`. Rounding a value the compiler understands produces a value it does not.

#### B6

A rule stating the constraint the modifier cannot express.

```precept
precept RulePlaces

field A as decimal min 0 max 10 default 1.55 editable
field B as decimal min 0 max 10 default 2.55 editable
field C as decimal maxplaces 2 <- A * B

rule C == round(C, 2) because "Computed value {C} must have at most two decimal places"
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 … four default-value obligations for A and B only …]}
```

The rule parses and is counted (`1 rules` in the summary). It contributes no proof obligation. It is a
runtime check, not a compile-time proof.

### C — is the unchecked modifier consumed as a premise anyway?

#### C1

`nonzero` on a computed field, consumed by the divisor proof. This re-confirms BUG-048 at HEAD with
both operands editable.

```precept
precept ComputedNonzeroConsumed

field Rate as decimal default 1.5 editable
field Factor as decimal default 1.5 editable
field D as decimal nonzero <- Rate - Factor
field E as decimal <- 100 / D
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"Numeric","disposition":"Proved","strategy":"DeclarationAttribute",
  "description":"Divisor must be non-zero"}]}
```

`1.5 - 1.5` is exactly zero in the default configuration, and `E` divides by it. One obligation in the
whole file, `Proved`, and its premise is a declaration nothing established.

#### C2

`nonnegative` on a computed field, consumed by the square-root proof. This one is not in BUG-048 —
that bug is written around the divisor.

```precept
precept ComputedNonnegConsumedClean

field Rate as number default 1.5 editable
field Factor as number default 4.0 editable
field C as number nonnegative <- Rate - Factor
field R as number optional

event Compute()

on Compute
    -> set R = sqrt(C)
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"Numeric","disposition":"Proved","strategy":"DeclarationAttribute",
  "description":"Argument must be non-negative"}]}
```

`1.5 - 4.0` is `-2.5`, and the file takes its square root. Zero diagnostics.

#### C3

`positive` on a computed field also discharges the divisor proof.

```precept
precept ComputedPositiveConsumed

field Rate as decimal default 1.5 editable
field Factor as decimal default 1.5 editable
field D as decimal positive <- Rate - Factor
field E as decimal <- 100 / D
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"Numeric","disposition":"Proved","strategy":"DeclarationAttribute",
  "description":"Divisor must be non-zero"}]}
```

#### C4

`maxplaces` on a computed field, read by two downstream computed fields. Nothing consumes it.

```precept
precept ComputedMaxplacesConsumed

field Rate as decimal default 1.55 editable
field Factor as decimal default 2.55 editable
field C as decimal maxplaces 2 <- Rate * Factor
field D as decimal maxplaces 2 <- C * 1
field E as decimal maxplaces 4 <- C + C
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

#### C5

`mincount` on a computed collection, consumed by the non-empty proof.

```precept
precept ComputedListCount

field Items as list of string mincount 1 editable
field Other as list of string editable
field Derived as list of string mincount 1 <- Other
field F as string optional

event Compute()

on Compute
    -> set F = Derived.first
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[
 {"kind":"Numeric","disposition":"Proved","strategy":"DeclarationAttribute",
  "description":"List must be non-empty"}]}
```

`Other` carries no count constraint. `Derived` declares `mincount 1`, nothing establishes it, and
`.first` is proven safe from it.

### The stored-field measurements

#### Sign modifiers, stored field, assigned from an expression

```precept
precept StoredSignAssignOnly

field A as decimal default 1.5 editable
field B as decimal default 2.5 editable
field TNonzero as decimal nonzero optional
field TNonneg as decimal nonnegative optional
field TPositive as decimal positive optional

event Go()

on Go
    -> set TNonzero = A - B
    -> set TNonneg = A - B
    -> set TPositive = A - B
```

```json
{"success":true,"diagnosticCount":0,"diagnostics":[],"proofObligations":[]}
```

`A - B` is `-1.0`. It is written into a field declared non-zero, a field declared non-negative, and a
field declared positive. Two of those three are violated. Zero diagnostics, zero obligations. **This
is the result that shows the sign modifiers are not a computed-field problem** — they are unchecked
against any expression, wherever it appears.

#### `min` on the same shape, for contrast

The identical file with `field TMin as decimal min 5 optional` and `set TMin = A * B` gives
`PRE0078` and an `IntervalContainment` obligation reading *"Interval containment: TMin must stay
within declared bounds [5 .. +∞]"*, `Unresolved`. Refused, correctly.

#### `maxplaces` on a stored field, assigned from an expression

```precept
precept StoredMaxplacesAssign

field A as decimal default 1.55 editable
field B as decimal default 2.55 editable
field Target as decimal maxplaces 2 optional

event SetLiteral()
event SetComputed()

on SetLiteral
    -> set Target = 1.999

on SetComputed
    -> set Target = A * B
```

```json
{"success":false,"diagnosticCount":1,
 "diagnostics":[{"line":11,"column":21,"severity":"error","code":"PRE0067",
   "message":"Value has 3 decimal places, but field 'Target' allows at most 2"}],
 "proofObligations":[]}
```

The literal on line 11 is caught. The expression on line 14 is not — `1.55 * 2.55` is `3.9525`, four
decimal places into a field declaring two, and there is no diagnostic and no obligation for it.
`maxplaces` is a literal check and nothing more.

### D — source check

`maxplaces` does not appear anywhere in the proof engine:

```
$ grep -rni "maxplaces" src/Precept/Pipeline/Proof*
(no output)
```

The eleven files under `src/Precept/Pipeline/` beginning `ProofEngine.` and `ProofLedger.cs` contain
no reference to it in any casing. Every reference lives in the parser, the type checker's modifier
validation, and the catalog. That is the mechanical confirmation of the C4 result: `maxplaces` is
declared, checked against literals by the type checker, and is invisible to the proof system.

## What this means

Three sentences, as asked.

A constraint an author writes on a computed field is, for four of the seven numeric modifiers and for
both string length modifiers and the collection count modifier, read by nobody and checked by
nothing. Three of those unchecked constraints are then handed to a later proof as though they had
been established, so declaring `nonzero` on a computed field is currently the thing that turns a file
the compiler would correctly refuse into one it accepts. With the runtime post-mutation check being
removed there is nothing behind this, so today an author cannot write a computed decimal constrained
to two decimal places that the compiler actually verifies — the forms that compile clean do so
because nothing looked, and reaching for `round` makes the value less provable rather than more.

## What I could not test, and why

- **Whether the string length modifiers are consumable as a premise.** I found no proof in the engine
  that takes a string length as an input the way the divisor proof takes non-zero-ness, so there was
  nothing to point one at. Recorded as "not tested", not as "no". A search of
  `ProofEngine.Lengths.cs` for a consumer would settle it.
- **`money`, `quantity`, `price` and the other business-domain types.** Everything above is `decimal`,
  `number`, `string` and `list of string`. BUG-048 already records that a `decimal nonzero <- M1 / M2`
  over two `money` fields reaches the divisor proof, so the business-domain lanes are known to be
  involved, but I did not re-measure them this pass.
- **Whether an `Unresolved` interval bound on a computed field can be consumed downstream before it
  is discharged.** Every file where that could happen is refused by `PRE0078` anyway, so it cannot
  cause an unsound acceptance today. It would matter if the refusal were ever relaxed.
- **`mincount` on a stored field with a literal default.** Not measured; BUG-050 covers the
  reassignment case and that was enough for the table.
- **Runtime behaviour of any of this.** Nothing here was executed. Every result is what the compiler
  reports.
