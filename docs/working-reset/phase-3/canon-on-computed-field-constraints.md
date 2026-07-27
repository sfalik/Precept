# What canon says about constraints declared on computed fields

**Status:** Draft enumeration — 2026-07-26
**Method:** Read-only enumeration of the canonical documents. Nothing was compiled; no source file under `src/` was read; no sample `.precept` file was used as evidence. Catalogs were not cited as evidence of completeness.
**Documents read:** `docs/language/precept-language-spec.md`, `docs/language/primitive-types.md`, `docs/language/business-domain-types.md`, `docs/language/temporal-type-system.md`, `docs/language/collection-types.md`, `docs/language/precept-grammar.md`, `docs/language/catalog-system.md`, `docs/philosophy.md`. `docs/compiler/` was read for stated intent only, and every conclusion drawn from it is marked as pipeline intent rather than a language statement.

Each conclusion below is marked as one of:

- **Canon says** — a canonical document states it in so many words.
- **Canon implies** — no document states it, but a chain of stated things forces it. The chain is written out so a reader can check every link.
- **Canon is silent** — no canonical document addresses it, and no chain of stated things settles it.

---

## The question

An author writes

```precept
field X as decimal maxplaces 2 <- A * B
```

The field's value is derived by the formula. It is never assigned. The author has also declared a constraint modifier on it — here `maxplaces`, but the same question arises for `nonzero`, `min`, `max`, `positive`, and `nonnegative`.

What do the canonical documents say this declaration means? Specifically: is it permitted, what makes the constraint true, does canon distinguish a constraint that describes a derived value from one that restricts a written value, what does canon say about the precision of the arithmetic that produces the value, and does any rule anywhere key on "computed" rather than on the field's type?

---

## 1. Is declaring a constraint modifier on a computed field permitted at all?

### Canon says: yes, the grammar admits it, in three separate places

The `field` production puts the modifier list before the derivation arrow, so the two are grammatically adjacent by design, not by accident.

> `field Identifier ("," Identifier)* as TypeRef FieldModifier* ("<-" Expr)?`
>
> — `docs/language/precept-language-spec.md:909`

> Multi-name shorthand: `field A, B, C as string` declares three fields of the same type. **Modifiers appear before the computed expression arrow.** The `<-` introduces a computed expression.
>
> — `docs/language/precept-language-spec.md:912` (emphasis added)

The grammar document restates the same thing as one of three surface forms of `field`, and it is the *only* one of the three that shows modifiers and a derivation together:

> ```
> field  Name  as  Type                     → stored field
> field  Name  as  Type  [modifiers]        → stored field with constraints/defaults
> field  Name  as  Type  [modifiers]  <- Expr → computed field
> ```
>
> — `docs/language/precept-grammar.md:518-520`

So the third form — modifiers *and* a computed body — is written into canon as a named surface form of the construct, not as an accident of an optional-slot grammar.

### Canon says: two modifiers are excluded, and neither is a constraint modifier

The spec excludes `editable` from computed fields, and says so four times:

> 8. **`editable` on a computed field** is a compile error (`ComputedFieldNotWritable`).
>
> — `docs/language/precept-language-spec.md:1085`

> \| `editable` \| flag (access modifier) \| Field baseline is directly editable across all states (unless overridden per-state); **invalid on computed fields and event args.** …
>
> — `docs/language/precept-language-spec.md:1145`

> \| `editable` (access modifier) \| any **non-computed** field type at field-declaration site AND per-state `modify F editable` site \| computed fields (`ComputedFieldNotWritable`); event arguments (`EditableOnEventArg`) \|
>
> — `docs/language/precept-language-spec.md:1667`

> \| `editable` on computed field \| A computed field carries the `editable` modifier \| `ComputedFieldNotWritable` \|
>
> — `docs/language/precept-language-spec.md:1726`

One further modifier is excluded, `default`, and again canon names it explicitly:

> \| Computed with default \| Field has both `<-` and `default` \| `ComputedFieldWithDefault` \|
>
> — `docs/language/precept-language-spec.md:1739`

> \| `field Total as decimal <- Amount * Rate` and `default 0` \| A computed field cannot also have a default — `ComputedFieldWithDefault`. Remove the `default`. \|
>
> — `docs/language/primitive-types.md:719`

Neither `editable` nor `default` is a constraint modifier. §2.4 draws that line itself:

> The flag modifiers `optional`, `editable`, and `ordered` are *not* rule shorthand — they declare nullability, per-state write access, and ordinal-comparison capability respectively, none of which is a value constraint.
>
> — `docs/language/precept-language-spec.md:1140`

### Canon implies: constraint modifiers are permitted on computed fields

The chain: the grammar admits any `FieldModifier*` before `<-` (`spec:909`, `spec:912`, `precept-grammar.md:518-520`); canon enumerates the exclusions from that position and there are exactly two, `editable` and `default` (`spec:1085`, `spec:1667`, `spec:1739`); neither is a constraint modifier (`spec:1140`). A constraint modifier on a computed field is therefore permitted by omission from an enumerated exclusion list.

That is an inference from an *enumeration*, and its strength depends on the enumeration being complete. Canon gives one reason to think the enumeration was written deliberately rather than left partial: the same table that lists `editable`'s exclusion (`spec:1667`) is the modifier-to-type compatibility table, and every other row in it names types only. Someone writing that table decided, per row, whether to name computed-ness. They named it once.

### Canon is silent: nothing states affirmatively that a constraint modifier on a computed field is meaningful

Permitted-by-omission is the whole of it. No canonical document contains a sentence of the form "a constraint modifier declared on a computed field means X," and none contains an example of a computed field carrying a constraint modifier. The two computed-field examples canon does show carry no constraint modifier:

> `field Total as decimal <- Amount * Rate` (in the context of rejecting a companion `default`)
>
> — `docs/language/primitive-types.md:719`

> \| `ComputeExpression` \| Computed field body expression \| `<- UnitPrice * Quantity` \|
>
> — `docs/language/precept-grammar.md:548`

The dedicated "Computed field validation" table in the spec — the one place canon collects what is checked about a computed field — has five rows and none of them concerns a constraint:

> \| Self-reference \| … \| `CircularComputedField` \|
> \| Transitive cycle \| … \| `CircularComputedField` \|
> \| Expression type mismatch \| Computed expression type doesn't match field type \| `TypeMismatch` \|
> \| Computed with default \| Field has both `<-` and `default` \| `ComputedFieldWithDefault` \|
> \| Computed as write target \| `set` action targets a computed field \| `ComputedFieldNotWritable` \|
>
> — `docs/language/precept-language-spec.md:1732-1741`

Type agreement between the formula and the declared type is checked. Agreement between the formula's possible results and the declared *constraints* is not on this list.

---

## 2. What is supposed to make the constraint true?

### Canon says: a constraint modifier is a rule

> **Constraint modifiers are shorthand for rules.** A constraint modifier — `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces` — desugars to the equivalent `rule` with a generated rationale. `field Qty as number min 5` is shorthand for `field Qty as number` plus `rule Qty >= 5 because "Qty must be at least 5"`.
>
> — `docs/language/precept-language-spec.md:1135`

The spec repeats the equivalence when it explains scope:

> A *rule condition* — and a *constraint modifier*, which is rule shorthand (§2.4) — is checked against the complete working copy *after* all mutations, so declaration order is irrelevant and any field is in scope.
>
> — `docs/language/precept-language-spec.md:1354`

### Canon says: a rule is checked after computed fields are recomputed, and a failure discards the operation

The operation execution order puts recomputation immediately before constraint evaluation, and constraint evaluation immediately before the commit-or-discard decision:

> 7. **Computed-field recomputation.** After all mutations and before any constraint is evaluated, every computed field is re-derived from the working copy (§3.5, expression scope).
> 8. **Constraint evaluation.** Every applicable rule and ensure is evaluated against the working copy after all mutations complete, collect-all (§3A.1, and the paragraph above).
> 9. **Commit or discard.** The working copy is promoted only if every constraint passed; otherwise it is discarded whole (the paragraph above).
>
> — `docs/language/precept-language-spec.md:2004-2006`

Recomputation is also listed as one of the eight ways data changes during an operation, so canon does treat the `<-` derivation as a write to the field's slot:

> \| Computed-field recomputation \| the `<-` declaration (§3.5); recomputed in every operation, including `Restore` \|
>
> — `docs/language/precept-language-spec.md:1985`

### Canon says: a constraint that mentions a computed field is a coherent thing that can be violated

The attribution section takes it as given that a constraint can reference a computed field and can fail because of one:

> Computed fields referenced in constraints are also targets, with transitive expansion to the concrete stored fields they depend on.
>
> — `docs/language/precept-language-spec.md:1961`

This is about which fields a violation message names. It presumes the violation can happen.

### Canon implies: a modifier on a computed field is enforced at runtime by recompute-then-check-then-discard

The chain: the modifier is a rule (`spec:1135`); rules are evaluated at phase 8 against the working copy, after recomputation at phase 7 (`spec:2004-2005`); if any rule fails the working copy is discarded whole (`spec:2006`); a constraint that references a computed field is an ordinary constraint whose violation attributes through to the stored fields the formula reads (`spec:1961`).

So there is an answer, and it is: the operation that produced the offending inputs is rejected in its entirety. Nothing is written; the entity keeps its prior configuration.

### Canon is silent: on how this squares with the guarantee that fault prevention is complete at compile time

§0.7 makes two statements. The first covers "no result outside a declared bound" and places it entirely at compile time:

> **Fault prevention — established entirely at compile time.** A precept that compiles without diagnostics cannot produce a runtime fault — no division by zero, no overflow, no empty-collection access, **no result outside a declared bound.** The compiler delivers it by discharging, at every fault-prone operation, an obligation that each operand *carries* a sufficient constraint — a declared modifier or rule, an author guard, or a statically-known safe value. … If it cannot, it **rejects the definition** … It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral.
>
> — `docs/language/precept-language-spec.md:266` (emphasis added)

The second covers everything that arrives from outside:

> **Governance — enforced at runtime on external input.** Every declared constraint is enforced on **every value entering the entity from outside the definition** — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it.
>
> — `docs/language/precept-language-spec.md:268` (emphasis added)

A computed value is not a value entering from outside, so the second sentence does not reach it. The first sentence names "no result outside a declared bound" but describes the mechanism as an obligation discharged "at every fault-prone operation" over *operands* that carry constraints — it never names the computed-field derivation as one of those operations, and §0.6's corresponding obligation is written in terms of assignment:

> 6. **Assignment range impossibility.** An assignment expression provably outside the target field's constraint range is a compile-time error.
>
> — `docs/language/precept-language-spec.md:208`

Whether a `<-` derivation is an "assignment" for the purpose of item 6 is nowhere stated. §3A.4:1985 does count recomputation as a writer, which pulls in that direction; §2.4's desugaring counts the modifier as a rule, which pulls the other way, because a rule is checked at runtime rather than proved over a write site. Canon does not resolve which reading governs.

The consequence is a real gap, not a wording quibble. Under the runtime reading (the inference above), a computed field whose formula can exceed its declared bound compiles, and the failure shows up as a rejected operation at runtime — which is exactly the "compiles a fault-prone operation in the hope a runtime check catches it" that §0.7:266 says never happens. Under the compile-time reading, the definition should be rejected outright. **Canon states both premises and never states which one wins for this case.**

### Pipeline intent (not a language statement): the compiler documents choose the compile-time reading for `min`/`max`, and the runtime reading for `maxplaces`

`docs/compiler/` is describing a pipeline rather than the language, so what follows is evidence of intent, not canon on meaning.

> … a **computation** result (set-action or computed-field expression) that exceeds a field's bounds is `NumericOverflow`. … `MaxPlacesExceeded` (PRE0067) remains type-owned (**no compile-time precision proof yet**).
>
> — `docs/compiler/diagnostic-system.md:163` (emphasis added)

> **Computed-field bound containment.** A computed numeric field carrying explicit declared bounds gets an `IntervalContainmentProofRequirement` against its own bounds — created **whenever the field has bounds**, including when the result interval is unbounded. An unbounded result stays `Unresolved` at discharge and surfaces as `NumericOverflow`, rather than silently passing as if the bound were vacuously satisfied.
>
> — `docs/compiler/proof-engine.md:604`

> \| `NumericOverflow` \| `NumericOverflow` \| `IntervalContainment` (computed-field bound) \| **in-scope, not-yet-implemented** …
>
> — `docs/compiler/soundness-and-coverage.md:200`

So the pipeline documents split the family: numeric interval bounds on a computed field are intended to be a compile-time containment obligation, and `maxplaces` is intended to have no compile-time proof at all. Neither split is stated in any `docs/language/` document. A reader of the language documents alone cannot recover it.

---

## 3. Does canon distinguish a constraint that *describes* a derived value from one that *restricts* it?

### Canon is silent

No canonical document draws this distinction, names it, or gestures at it. There is no sentence anywhere in `docs/language/` or `docs/philosophy.md` that separates "this constraint restricts what may be written" from "this constraint asserts something the formula must already produce."

Everything canon says about what a constraint *does* is written in the vocabulary of a value arriving and being checked. §0.7:268 says constraints are enforced "on every value entering the entity from outside the definition … at the moment it enters." §3A.1 defines a rule as "global data truth — constraints that hold after every mutation" (`spec:1857`), which is closer to a description than a restriction, but the section never applies it to a derived value. The desugaring at `spec:1135` turns `min 5` into `rule Qty >= 5`, a predicate — and a predicate is neutral between describing and restricting, which is precisely why the desugaring does not settle the question.

### The `maxplaces` case specifically

Canon says what `maxplaces` is, twice, in the same words:

> **`maxplaces`:** Validation constraint, not auto-rounding. `field X as decimal maxplaces 2` — **assigning** `1.999` is a constraint violation. Only applicable to `decimal`.
>
> — `docs/language/primitive-types.md:249` (emphasis added)

> \| `maxplaces N` \| `decimal` only \| At most N decimal places. Validation constraint, not auto-rounding. \|
>
> — `docs/language/primitive-types.md:581`

Both sentences define the constraint by what it does to an assignment. On a computed field there is no assignment — `spec:1740` makes `set` on a computed field a compile error (`ComputedFieldNotWritable`). So the definition canon gives for `maxplaces` has no case that applies here.

The business-domain document enumerates where `maxplaces` is checked, and the enumeration is closed and exhaustive on its face — "checked at three points" — and all three points are writes or ingress:

> **`maxplaces` enforcement scope:** `maxplaces` is checked at three points:
> 1. **Literal assignment (compile time):** `set Cost = '1.999 USD'` where `Cost` is `money in 'USD'` is a compile-time error — the literal's decimal places are statically known.
> 2. **Event-arg input (runtime boundary):** `precept_fire`/`precept_update` validate event arg values at the input boundary. `{ "Amount": "1.999 USD" }` for a `maxplaces 2` field is rejected before the engine runs.
> 3. **Arithmetic result assignment (runtime):** `set Cost = UnitPrice * Qty` … produces exactly `'3.999 USD'` … If `Cost` is declared as `money in 'USD' maxplaces 2`, this is a constraint violation at `set` time. The author must apply `round()` explicitly: `set Cost = round(UnitPrice * Qty, 2)`.
>
> — `docs/language/business-domain-types.md:1581-1584`

Point 3 is the closest canon comes to the computed-field case, and it is a `set` action, not a `<-` derivation. It is the same arithmetic — a product assigned to a `maxplaces`-bounded destination — reached by the one route a computed field cannot take. Canon gives the author a fix for point 3 (`round()` at the assignment) and gives no fix for the derivation form, because it never discusses the derivation form.

The same document also classifies `maxplaces` as the only one of the six magnitude constraints with no proof-engine meaning:

> \| `nonnegative` \| `Field >= 0` \| `[0, +∞)` \| … \|
> \| `positive` \| `Field > 0` \| `(0, +∞)` \| … \|
> \| `nonzero` (#111) \| `Field != 0` \| excludes zero \| … \|
> \| `min N` \| `Field >= N` \| `[N, +∞)` \| … \|
> \| `max N` \| `Field <= N` \| `(-∞, N]` \| … \|
> \| `maxplaces N` \| **Runtime enforcement** \| **—** \| `field Amount as money in 'USD' maxplaces 2` \|
>
> — `docs/language/business-domain-types.md:1572-1577` (emphasis added)

So for five of the six, canon supplies a desugared predicate and a proof-engine interval. For `maxplaces` it supplies neither, and says "runtime enforcement" with an em-dash where the interval would be. That is a statement about mechanism. It is not a statement about what "runtime enforcement" means when the value being enforced against was produced by the engine itself two phases earlier.

### Canon implies, weakly: `maxplaces` on a computed field would fail the operation

The chain is the same one as item 2 — modifier is a rule (`spec:1135`), rules run at phase 8 after recomputation at phase 7, failure discards (`spec:2004-2006`) — combined with `business-domain-types.md:1577` saying `maxplaces` is enforced at runtime. That chain lands on: the operation is rejected.

The inference is weak in one specific way. Canon's enforcement-scope enumeration at `business-domain-types.md:1581` says `maxplaces` is checked at **three** points, and lists them. Computed-field recomputation is not among them. If that list is exhaustive as written, `maxplaces` on a computed field is checked nowhere and the modifier is inert. If the list is an illustration rather than an enumeration, the phase-8 chain applies. Canon does not say which. Two readings of the same document give opposite answers, and both are defensible from the text.

---

## 4. What does canon say about the arithmetic itself?

### Canon says: the result type of decimal arithmetic, and nothing about its scale except for division

The operator table for `decimal` gives result types, and mentions precision for exactly one operator:

> \| `decimal + decimal` \| `decimal` \| Stays in lane. Overflow is an error. \|
> \| `decimal - decimal` \| `decimal` \| \|
> \| `decimal * decimal` \| `decimal` \| \|
> \| `decimal / decimal` \| `decimal` \| **Full 28-digit precision.** Divisor safety applies. \|
> \| `decimal % decimal` \| `decimal` \| \|
>
> — `docs/language/primitive-types.md:236-241` (emphasis added)

The notes column for multiplication is empty. Canon states the scale behaviour of division and says nothing about the scale behaviour of addition, subtraction, multiplication, or remainder.

For business-domain division, canon is more explicit, and adds the author's obligation:

> \| `money / money` \| `decimal` \| Same currency; produces dimensionless ratio. **Result is full 28-digit `decimal` precision — scale is not capped.** `'1.00 USD' / '3.00 USD'` = `0.3333″3″` (28 digits). **Apply `round()` before assigning to a `maxplaces`-constrained field.** \|
>
> — `docs/language/business-domain-types.md:506` (emphasis added)

> \| `quantity / quantity` (same dimension) \| `decimal` \| … Result is full 28-digit `decimal` precision — scale is not capped. … Apply `round()` before assigning to a `maxplaces`-constrained field. \|
>
> — `docs/language/business-domain-types.md:663`

> **Division precision:** `money / money` and `quantity / quantity` produce a full 28-digit `decimal` result. Scale is not implicitly capped … Authors must apply `round()` before assigning a ratio result to any `maxplaces`-constrained field.
>
> — `docs/language/business-domain-types.md:1781`

Note the shape of the instruction each time: *before assigning*. The remedy canon offers is placed at an assignment site. A computed field has no assignment site at which to place it.

Canon also states the general no-implicit-rounding commitment:

> **Decimal arithmetic is the default for money operations.** Multiplication, division, and percentage operations preserve exact base-10 representation up to the author's declared precision. There is no implicit rounding. When rounding is required (e.g., final billing amounts), the author calls `round(...)` explicitly with an explicit rounding mode (default `half-even`).
>
> — `docs/language/business-domain-types.md:174`

And it gives one worked example of a product's scale, in passing, inside the `maxplaces` enforcement note:

> `set Cost = UnitPrice * Qty` where `UnitPrice = '1.333 USD/each'` and `Qty = '3 each'` produces exactly `'3.999 USD'` (exact `decimal` arithmetic — see D12).
>
> — `docs/language/business-domain-types.md:1584`

That is a single instance — three decimal places times zero decimal places yields three decimal places — offered as an illustration of exactness, not as a rule about scale.

### Canon is silent: on how a product's or sum's decimal places relate to its operands'

No canonical document states a scale rule for arithmetic. There is no equivalent of the SQL `NUMERIC(p,s)` result-scale table or the .NET decimal-multiplication scale rule anywhere in `docs/language/`. Canon states:

- that `decimal` is exact base-10 with no float drift (`primitive-types.md:82`, `:230`);
- that division produces 28 digits and is not capped (`primitive-types.md:239`, `business-domain-types.md:506`, `:663`, `:1781`);
- that there is no implicit rounding (`business-domain-types.md:174`, `:1997`).

It does not state what `maxplaces 2 * maxplaces 2` yields, whether a product's scale is the sum of its operands' scales, whether trailing zeros are significant, or whether an operand's declared `maxplaces` contributes any provable fact about the result's scale at all. Since `maxplaces` carries no proof-engine interval (`business-domain-types.md:1577`), there is not even a stated mechanism by which an operand's declared precision could reach a conclusion about the result's.

This silence is what makes item 3's `maxplaces`-on-a-computed-field question unanswerable rather than merely unanswered. Even if canon ruled that the constraint must be proved at compile time, there is no stated arithmetic from which to prove it.

---

## 5. Is there a stated rule about which modifiers apply to computed fields versus stored ones?

### Canon says: the compatibility tables key on type, with exactly one exception

The spec's modifier validation table has twelve rows. Eleven of them name only types in the "Applicable to" column. One names computed-ness:

> \| `editable` (access modifier) \| any **non-computed** field type at field-declaration site AND per-state `modify F editable` site \| computed fields (`ComputedFieldNotWritable`); event arguments (`EditableOnEventArg`) \|
> \| `nonnegative` \| `integer`, `decimal`, `number`, `money`, `quantity`, `price`, `exchangerate`, `duration`, `period` \| `string`, `boolean`, `choice`, collections, non-magnitude temporals (`date`/`time`/`instant`/`datetime`) \|
> \| `positive` \| (same as `nonnegative`) \| (same as above) \|
> \| `nonzero` \| (same as `nonnegative`) \| (same as above) \|
> \| `notempty` \| `string` (incl. per-element in a collection's inner-type position) \| … \|
> \| `min` / `max` \| `integer`, `decimal`, `number`, `money`, `quantity`, `price` \| … \|
> \| `minlength` / `maxlength` \| `string` \| … \|
> \| `mincount` / `maxcount` \| `set`, `queue`, `stack`, `log`, … \| scalars \|
> \| `maxplaces` \| `decimal`, `money`, `quantity`, `price`, `exchangerate` \| `integer`, `number`, `string`, `boolean`, `choice`, collections \|
> \| `ordered` \| `choice` \| all non-choice types \|
> \| `optional` \| any field type \| — (always valid) \|
>
> — `docs/language/precept-language-spec.md:1667-1677`

The note immediately following the table states, for the one other axis canon considered adding, that no second axis exists:

> **Per-element consultation (collection inner types).** When a value modifier sits in collection inner-type position … this same table is consulted with the **element** scalar type as subject. … **No second compatibility axis exists** — the element type is a scalar type, and a scalar type's modifier set is already defined here.
>
> — `docs/language/precept-language-spec.md:1679` (emphasis added)

> **Correction, checked 2026-07-26 against the full line.** The sentence "No second compatibility axis
> exists" is scoped to **collection inner types** — it is saying that consulting the table with a
> collection's element type as subject does not introduce a second axis, because the element type is
> itself a scalar type. It is *not* a general statement that no computed-versus-stored axis exists. Read
> in context it says nothing about computed fields at all. The surrounding observation still holds on its
> own evidence — both compatibility tables key on type, and `editable` is the only modifier that states a
> computed-ness restriction, stating it inline — but this quotation must not be cited as canon ruling the
> question out.


That sentence is about collection elements, not about computed fields. But it is canon stating, in its own voice, that the table has one axis and the axis is type. A computed axis would be a second one.

The constraint catalog in `primitive-types.md` is the same shape — twelve rows, "Applicable to" naming types only, and no mention of computed fields anywhere in the table or the validation rules beneath it:

> \| Constraint \| Applicable to \| Meaning \|
> \| `optional` \| any field type \| … \|
> \| `default V` \| any field type \| Initial value when no event has set it \|
> \| `nonnegative` \| `integer`, `decimal`, `number` \| Value ≥ 0 \|
> \| `positive` \| `integer`, `decimal`, `number` \| Value > 0 (subsumes `nonnegative`) \|
> \| `nonzero` \| `integer`, `decimal`, `number` \| Value ≠ 0 \|
> \| `min N` \| `integer`, `decimal`, `number` \| Value ≥ N \|
> \| `max N` \| `integer`, `decimal`, `number` \| Value ≤ N \|
> \| `maxplaces N` \| `decimal` only \| At most N decimal places. Validation constraint, not auto-rounding. \|
> …
>
> — `docs/language/primitive-types.md:572-584`

The per-type constraint lists in the other type documents are also type-keyed only, and none mentions computed fields: `primitive-types.md:247` (`decimal`), `business-domain-types.md:528` (`money`), `temporal-type-system.md:464` (`date`), `:738` (`duration`), `:842` (`period`), `collection-types.md:166` (`set`).

### Canon says: "computed" is a live axis elsewhere in the language, just not in the constraint tables

Canon uses computed-ness as a discriminator in five places, all of them about *writing* or *evaluation order*, none about constraints:

- `editable` is invalid on computed fields — `spec:1085`, `:1145`, `:1667`, `:1725`, `:1726`
- `set` cannot target a computed field — `spec:1702` ("Field must not be computed"), `:1740`
- `default` cannot coexist with `<-` — `spec:1739`, `primitive-types.md:719`
- a required field being materialized on a state crossing excludes computed fields from the obligation — `spec:1076` ("non-optional, no default value, **not computed**")
- computed expressions have their own scope rule, distinct from defaults and from rule conditions — `spec:1351-1354`

So the language does know how to say "this is different on a computed field." It says it whenever writing is involved. It never says it about a value constraint.

### Canon implies: constraint applicability is decided by type alone, and a computed field's constraints are governed by the same rows as a stored field's

The chain: both compatibility tables key on type (`spec:1667-1677`, `primitive-types.md:572-584`); the one sentence that sounds like a general ruling (`spec:1679`) is scoped to collection element types and does not reach this question — see the correction above; the one modifier with a computed-ness restriction states it inline in the table rather than in a separate rule (`spec:1667`), which is the pattern a reader would expect any other such restriction to follow.

### Canon is silent: on whether that is a decision or an omission

Nothing in canon records a decision that constraint applicability should be type-keyed only. There is no rationale, no rejected alternative, no tradeoff — the four things `CLAUDE.md` requires of a locked decision. The tables simply have one axis and no one wrote down why. So "type alone decides" is the reading the documents support, but it is a reading of a table's shape, not a stated rule, and a reader cannot tell whether the computed case was considered and admitted or never came up.

---

## What canon does not say

These are the specific questions a reader cannot answer from the canonical documents as they stand. Each is a genuine silence, not a hard-to-find answer.

1. **What a constraint modifier on a computed field means.** Permitted by omission from an exclusion list. No document states its meaning, gives an example, or names a diagnostic for it. (§1)

2. **Whether a computed field's declared bound is a compile-time obligation or a runtime check.** §0.7:266 says "no result outside a declared bound" is established entirely at compile time with no deferral. §0.6:208 frames the corresponding obligation as *assignment* range impossibility. §2.4:1135 desugars the modifier to a rule, and §3A.4:2005 runs rules at runtime. Canon states all of these and never states which governs a `<-` derivation. This is the most significant silence in the enumeration: the two answers differ in whether a definition compiles at all. (§2)

3. **Whether the `maxplaces` enforcement-scope list is exhaustive.** `business-domain-types.md:1581` says `maxplaces` "is checked at three points" and lists three write-or-ingress points. Computed-field recomputation is not one of them. If the list is closed, `maxplaces` on a computed field is inert. If it is illustrative, the phase-8 rule chain applies. Canon supports both readings. (§3)

4. **Whether canon distinguishes describing a derived value from restricting a written one.** It does not. The distinction is not named anywhere, and every definition canon gives of what a constraint does is written in terms of a value arriving. (§3)

5. **What `maxplaces` validates on a field nobody assigns to.** Its two canonical definitions (`primitive-types.md:249`, `:581`) both define it by what happens on an assignment. There is no assignment. (§3)

6. **The scale of any arithmetic result except division.** No scale rule for `+`, `-`, `*`, or `%` exists in canon. Division is stated (28 digits, uncapped). Multiplication has an empty notes cell. There is therefore no stated arithmetic from which a `maxplaces` claim about a derived value could ever be proved. (§4)

7. **Whether an operand's declared `maxplaces` says anything about a result's precision.** `maxplaces` carries no proof-engine interval (`business-domain-types.md:1577` — the entry is an em-dash). Canon supplies no other mechanism by which a declared precision could propagate through an expression. (§4)

8. **Where the author's remedy goes.** Canon's remedy for precision loss is `round()` applied "before assigning" (`business-domain-types.md:506`, `:663`, `:1584`, `:1781`). A computed field has no assignment site. Whether the author is expected to write `<- round(A * B, 2)` is nowhere stated, and no canonical example shows it. (§4)

9. **Whether a computed field's constraint failure is a sensible thing to report to an author.** §3A.4:2006 discards the whole operation on any constraint failure, and §3A.3:1961 attributes a computed field's violation through to the stored fields it depends on. Canon never discusses what an author is supposed to do about a definition whose own formula produces a value its own declaration forbids — a configuration that is not a data problem but a definition problem. (§2)

10. **Whether "type only" is a decision.** Both compatibility tables key on type, and nothing states whether that is deliberate, but no rationale, alternative, or tradeoff is recorded for excluding a computed axis. A reader cannot tell whether the case was ruled on or never arose. (§5)

---

## One thing the language documents do not carry, that the pipeline documents do

Recorded here because a reader of `docs/language/` alone would not find it, and because a later pass should decide whether it belongs in canon.

`docs/compiler/` states a resolution to silence #2 above, and splits the constraint family in doing so. `proof-engine.md:604` describes a computed numeric field's declared `min`/`max` bounds as generating an interval-containment obligation against the field's own bounds, with an unbounded result staying unresolved rather than passing vacuously. `diagnostic-system.md:163` names the resulting diagnostic (`NumericOverflow`) and partitions it explicitly to cover "set-action **or computed-field expression**" results. The same line states that `maxplaces` has "no compile-time precision proof yet." `soundness-and-coverage.md:200` marks the computed-field bound containment obligation "in-scope, not-yet-implemented."

So the pipeline documents intend: numeric interval bounds on a computed field are proved at compile time; `maxplaces` is not. That is a language-meaning decision — it determines whether a definition compiles — and it currently lives only in documents that describe how the compiler is built.
