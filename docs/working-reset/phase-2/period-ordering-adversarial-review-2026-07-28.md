# Adversarial review of `period-ordering-design.md`

**Status:** Opinion — 2026-07-28. Written by Frank (Lead/Architect), on the owner's explicit
request. Advisory only — not a locked design, and no edits made to
`period-ordering-design.md`. The owner's framing, verbatim in intent: he had ruled that
Precept follows NodaTime because its designers rarely make a choice without a good reason,
he is tempted by same-unit period ordering, and he is nervous that "we are going to
introduce something that was purposely left out of NodaTime because it seems easy but is
actually very hard when you get into the details." The review's default posture was
therefore adversarial: assume the design is stepping into a hazard NodaTime deliberately
avoided, and go looking for it.

**Method:** Read the design document in full. Did not trust its self-reported red-team
pass: independently re-ran its central compile claims against the live compiler via
`precept_compile` — the headline `GracePeriod.days <= MaximumGrace.days` vs bare
`GracePeriod <= MaximumGrace` pair, and all six § Ship now reproductions. Independently
confirmed NodaTime's stated rationale for withholding `IComparable` from `Period` (docs,
`CreateComparer` remarks, maintainer discussion). Then re-ran the design's core measurement
with fresh code against NodaTime 3.3.3 — `Period.CreateComparer(base)` on same-single-unit
pairs across all seven catalogued units — extended to **nine calendar systems** where the
design measured only Gregorian: Gregorian, Julian, Islamic-Civil, Hebrew (civil and
scriptural numbering), Persian, Um Al-Qura, Coptic, and Badi. 407,972 comparisons. Also
grepped `src/Precept/` for `CalendarSystem` to establish whether non-Gregorian calendars
are reachable from Precept at all. The probe project was deleted after the run; the code
shape is described inline where its result is cited.

---

## 1. What NodaTime actually does, and why — confirmed

Verified against the shipped NodaTime 3.3.3 assembly by reflection: `Period` implements
`IEquatable<Period>` and **no** comparison surface in any form — no `IComparable`, no
`IComparable<Period>`, no `op_LessThan`. `Duration` implements all of them.

The maintainers' stated reason, from the `Period.CreateComparer` documentation and Jon
Skeet's public explanations: *"Certain periods can't naturally be compared without more
context — how 'one month' compares to '30 days' depends on where you start."* The two
escape hatches NodaTime supplies are `CreateComparer(LocalDateTime)` — add both periods to
a reference date and compare the results — and `ToDuration()`, which is exact for any
period free of years and months.

The structural point the design makes about this is correct and matters: `IComparable<T>`
is a whole-type contract in C#. NodaTime cannot express "orderable when the value happens
to hold exactly one component" — the type covers every combination of ten components, so
withholding the interface is the only option the language leaves them. **The ambiguity
NodaTime is refusing is the heterogeneous case — "1 month" against "30 days" — not
"5 days" against "4 days".** The design quotes NodaTime's reason accurately, and the
hazard the owner fears is exactly the one Decision 14 in `temporal-type-system.md:1487`
already records.

## 2. The design's core claim, tested

The proposed rule: ordering is admitted only when **both operands are pinned to one and
the same single unit** (`period in 'days'` against `period in 'days'`), and the reduction
is a comparison of the two component counts — no conversion, no reference date, no
position on how long a day or month is.

Every claim I checked reproduced at HEAD:

- `rule GracePeriod.days <= MaximumGrace.days` on two `period in 'days'` fields compiles
  clean **today** (0 type errors). The bare `rule GracePeriod <= MaximumGrace` on the same
  fields fails with `PRE0018 "Expected a period value here, but got 'period'"`. So the
  design's own corrected conclusion is right: the operator is a change of spelling, not of
  capability, and its first draft's "authors are blocked" argument was correctly refuted
  by its own red-team pass.
- All six § Ship now defects reproduce exactly as written: the dequeue-into-pinned-field
  path raises **zero** qualifier obligations; `LeaseTerm.months <= 12` on
  `period in 'years + months'` compiles clean and is unsound (`'1 year'` has
  `.months == 0`); `.min`/`.max` on a fully pinned `set of money in 'USD'` is refused with
  "(Currency: unresolved)" while unqualified collections pass silently;
  `field Bad as period of 'time' default '3 years'` compiles with no diagnostic;
  `field B as date <- A + '12 months'` is rejected with `PRE0056` telling the author to
  write what they wrote; and `field J as period nonnegative default '1 months + -30 days'`
  compiles with its obligation reported **Proved** — a false proof, live today.
- Daylight saving time cannot enter the comparison's domain, but not for the reason a
  reader might assume. The cross-type table in `temporal-type-system.md` already makes
  `zoneddatetime ± period` a type error, and `date`/`datetime` arithmetic is zone-free, so
  a period count can never be forced to answer an elapsed-hours question. The design's
  "takes no position on whether a day is 24 hours" claim holds — but it holds **because of
  that existing canon refusal**, and the design does not cite it as the precondition it
  is. If `zoneddatetime ± period` were ever admitted, "2 hours" applied across a
  spring-forward transition is one elapsed hour while "1 hour" applied across a fall-back
  transition is two, and the same-unit count order would stop meaning "shorter elapsed
  span" for the time units. The dependency should be stated.
- Weeks: `Period.Normalize()` folds weeks into days at 7:1 unconditionally, with no
  calendar argument — confirmed by direct call. The design's tradeoff note (refusing
  `period in 'weeks'` against `period in 'days'` refuses an *exact* comparison) is honest
  and correct.

## 3. The counterexample: the universality claim is false as stated

The design claims, in § What was measured: *"the count order and the calendar order never
disagreed"*, and backs it with an argument offered as calendar-independent: *"`b + N
months` lands inside calendar month `month(b) + N`; calendar months are disjoint and run
in time order... So more months always lands later, whatever `b` is and however long those
months happen to be."* It measured only the Gregorian calendar.

I re-ran the measurement across nine calendar systems. Result: **407,972 comparisons,
48 order flips, 0 ties — every flip in the Badi (Bahá'í) calendar, `months` unit.**

Concrete instance. From base Badi 178-19-1 (month 19, day 1 of Badi year 178):

- `+ 18 months` → Badi 179-18-1, which is Gregorian 2023-02-07. Correct.
- `+ 19 months` → a **corrupt `LocalDate`** whose fields read year 0, month 32 — a date
  that throws `ArgumentOutOfRangeException` the moment it is converted or formatted, and
  which `Period.CreateComparer` silently ranks *earlier* than the 18-month result.

The root cause is a NodaTime defect, not a legitimate calendar disagreement: the Badi
calendar's intercalary days (Ayyám-i-Há) break the "months are sequential" assumption
NodaTime's arithmetic rests on, a representation problem its maintainers discussed in
GitHub issue #689 and resolved with a compromise (Ayyám-i-Há folded into month 18) that
leaves month arithmetic across the boundary broken in the shipped 3.3.3 assembly.

Two readings, and both matter:

1. **It is not a live hole in Precept.** `CalendarSystem` appears nowhere in
   `src/Precept/` — Precept is ISO-Gregorian only, and canon's comparison table says
   `date` ordering is "ISO calendar". Over everything Precept can currently express, the
   design's measurement stands, and my independent Julian, Islamic, Hebrew, Persian,
   Um Al-Qura, and Coptic runs extend it: zero flips, zero ties.
2. **But the design's proof sketch is refuted as a universal.** "Calendar months are
   disjoint and run in time order" is a property the shipped library itself fails to
   uphold in one of its calendars. The soundness claim must be re-scoped from an implicit
   "any calendar" to an explicit *"the ISO calendar, which is the only calendar Precept
   exposes"* — and **calendar-system support must be added to § Operational dimensions'
   evolvability list as a re-verification trigger**, alongside the ten-versus-seven
   component gap the design already records there. This is precisely the shape of hazard
   the owner predicted: an argument airtight for Gregorian, quietly assumed to
   generalize.

One further adversarial point the design half-concedes and should not be allowed to lose:
**its own Open question 1 is right that the alignment-directive question is an amendment
request, not a reading.** The proposed mechanism registers `<`, `<=`, `>`, `>=` on
`period × period` in the operations catalog, with refusal expressed as a failed proof
about particular operands. So the catalog will literally say the `period` type has
ordering operators — the surface shape that directive item 4 (*"When NodaTime has no
natural ordering on a type, Precept has no natural ordering on that type"*,
`temporal-type-system.md:244`) forbids by its letter. The design routes this to the owner
honestly. Nobody should shortcut that routing.

## 4. Verdict

**In between, but much closer to safe than the owner fears — and the design itself is the
main reason why.**

- **The hazard NodaTime avoided is real, and this design does not step into it.**
  NodaTime's refusal is about heterogeneous components. A same-single-unit count
  comparison converts nothing, assumes no unit's length, and — verified independently, in
  both the design's measurement and mine — agrees with the reference-date order at every
  base reachable from Precept. The design also measured the composite case honestly
  (432 order flips in 141,376 `years + months` pairs, reproduced in spirit by my
  `P1Y1M` vs `P13M` check: equal from 2019-01-01, less-than from 2020-02-29) and refuses
  composites for that measured reason rather than by analogy. The `Period`/`Duration`
  distinction is left intact; DST is fenced out by existing canon; weeks-versus-days is
  refused conservatively rather than cleverly.
- **The genuinely dangerous things in this area are the six pre-existing defects, and
  every one reproduces.** The binding-path qualifier hole (§ Ship now item 1) is the one
  that would make the ordering rule unsound on arrival, and the design correctly makes it
  a precondition. The false-**Proved** `nonnegative` obligation on
  `'1 months + -30 days'` is a live false proof today, ordering or no ordering. Ship
  § Ship now regardless of what happens to the operator.
- **Required corrections before any lock:**
  1. Re-scope the soundness claim to the ISO calendar and add calendar-system support as
     a re-verification trigger — the Badi counterexample falsifies the claim as written.
  2. Cite the existing `zoneddatetime ± period` refusal as a precondition of the "no
     position on day length" claim; without it, that claim fails for the time units.
  3. The alignment-directive amendment (Open question 1) genuinely needs an owner ruling,
     because the built shape violates directive item 4's letter even where it honors its
     spirit.
- **On whether to build the operator at all:** the design's own corrected finding is the
  strongest argument for restraint. It adds legibility, not capability —
  `A.days <= B.days` compiles today and computes the identical predicate. The "smaller
  answer" in Open question 2 (ship the bug fixes, rewrite Decision 14 to record that
  same-single-unit ordering is *sound and refused for reach, not for correctness*, keep
  the accessor route and fix its incoherent error message) captures most of the value
  with zero irreversible surface. If the owner's prior is "don't invent what NodaTime
  withheld," the smaller answer honors it while fixing the errors that are the actual
  author pain.

One credit given plainly, because an adversarial review should say when the adversary
behaved well: this design refuted its own first draft's central argument, published the
refutation with the working shown, and downgraded its own recommendation accordingly.
That is the opposite of trying to sneak something past. This pass found one real crack —
the calendar-universality claim — and it is fixable with a scope sentence and an
evolvability row; it does not touch anything Precept can express today.

---

## 5. Follow-up (2026-07-28, later the same evening): does the accessor route reach collections?

**The owner's question:** the review above leaned on the fact that
`A.days <= B.days` already compiles and computes the same predicate as the proposed
operator, so the operator adds legibility rather than capability. Does that equivalence
survive collections? If an author declares a collection of periods pinned to a single
unit and wants `.min`/`.max` or an ordering constraint over it, can the accessor route
deliver that — or does Precept's collection machinery require the **element type itself**
to be orderable, which an accessor producing an `integer` can never supply?

**Method:** read `docs/language/collection-types.md` in full for what "a collection
supports ordering" requires of its element type, then probed every candidate route
through the live compiler. Every compile result below is from `precept_compile` at HEAD,
not from reading documentation.

### What the collection machinery actually requires

Precept has no sort, no `orderby`, and no user-facing key-projection construct — a
`sortedset of T` was considered and rejected (`collection-types.md:1491`), and the
philosophy excludes iteration. Exactly three surfaces of the collection system depend on
element ordering:

1. **`.min` / `.max` accessors** — gated on a trait of the element type itself.
   `collection-types.md:144-:147`: *"`T` must be orderable"*, where orderable is defined
   by a list of types, not by any projection. This is `TypeTrait.Orderable` on the
   element type's catalog entry — the same flat flag the design's Decision 2 replaces.
2. **`queue of T by P` / `log of T by P`** — key-ordered kinds where the ordering lives
   on a *separate* declared key type `P`, which must itself be orderable. This is the one
   place Precept's collection ordering is projection-shaped, LINQ-`OrderBy`-like — but
   the projection is a second stored value the author supplies at every write, not an
   accessor the compiler applies.
3. **Quantifier predicates (`each`/`any`/`no`)** — locked design, **not yet implemented**
   (`collection-types.md:802`). When they land, their bodies will evaluate ordinary
   comparisons per element, so they inherit whatever the scalar comparison rules are.

### The probes

**Route 1 — `.min` on a pinned period collection: refused, and the accessor cannot save
it.** Both of these fail today with
`PRE0104 "Accessor 'min' requires a trait the element type does not provide (e.g., .min/.max require Orderable)"`:

```precept
field Deadlines as set of period in 'days'
field Shortest as period in 'days' optional

from Open on Recompute when Deadlines.count > 0
    -> set Shortest = Deadlines.min          # PRE0104
```

```precept
rule Deadlines.min.days > 0 when Deadlines.count > 0 because "..."   # PRE0104
```

The second probe is the decisive one: the author cannot write `.days` "inside" the
`.min`, because the expression fails at `.min` before `.days` is ever reached. The trait
check is on the **element type**, evaluated before any downstream accessor. And the
inverted spelling fails too — `Deadlines.days.min` gives
`PRE0020 "'.days' is not available on set fields"` — because Precept has no element-wise
projection: there is no construct that turns a `set of period in 'days'` into a set of
integers. The language's no-iteration philosophy is precisely why no such construct
exists.

**Route 2 — the keyed queue, the one genuine workaround, compiles today:**

```precept
field Ladder as queue of period in 'days' by integer
field Next as period in 'days' optional

event Add(Grace as period in 'days')

from Open on Add
    -> enqueue Ladder Add.Grace by Add.Grace.days
    -> no transition

from Open on Take when Ladder.count > 0
    -> set Next = Ladder.peek        # the minimum, by construction
    -> no transition
```

Verified: 0 type errors, and the `.count > 0` obligation discharges. This is real — an
author can maintain a period collection in count order by projecting the count into the
`by` key at each write, and `.peek` is then the minimum. But look at what it costs: the
collection must be a queue (not a set or list); the ordering key is a bare `integer`
carrying no record of what it counts — the exact Principle 6 failure the design document
identifies in the scalar accessor route, now stored permanently in the entity's data
rather than appearing transiently in one expression; the author must remember to write
the same projection at every `enqueue`, and nothing checks that the key they supply
actually is `.days` of the value — `enqueue Ladder Add.Grace by 7` compiles identically.
The invariant that makes `.peek` mean "minimum" is a convention in the author's head,
which is the kind of thing this language exists to eliminate.

**Route 3 — a running-minimum scalar** (`when Add.Grace.days < Shortest.days -> set
Shortest = Add.Grace`) compiles and is sound, but it is a hand-maintained fold: it only
works when every element arrives through an event the author controls, and it cannot
survive element removal.

**Route 4 — "the list must be strictly increasing": not expressible for any element
type today**, period or otherwise. It needs adjacent-element predicates, which need
quantifiers, which are not implemented. So this particular want is a collection-surface
gap, not a period gap, and is out of scope for the ordering question.

### Verdict on the follow-up

**The owner is right, and the design document already knew it — the point deserves more
weight than either my review or the design's summary gave it.** The scalar equivalence
("the operator is a change of spelling") is true and verified, but it does not extend to
collections: `.min`/`.max` legality is decided by a trait on the element type itself,
Precept has no projection construct that could apply `.days` element-wise, and the trait
can therefore never be satisfied from outside the type. The design document carries this
fact — Decision 4 derives `.min`/`.max` legality from the element's qualifier, and Open
question 5's "the collection half forces the four-type capability" is this exact point —
but its own § The short version framing ("a change of spelling, not a change of
capability") is scalar-only and reads as if it covered everything. It does not, and a
reader skimming the summary would miss it.

**What this changes in my recommendation, stated plainly:**

- For **scalar** comparisons, nothing changes: the accessor route is real, sound once
  § Ship now item 2 lands, and the "smaller answer" (rewrite Decision 14, fix the
  messages, ship no operator) remains a legitimate cheap outcome **if the identified
  needs stay scalar** — which today they are: no sample file declares a collection of
  periods, so collection demand is currently hypothetical.
- For **collections**, the accessor route structurally cannot deliver, and the keyed
  queue is a workaround with the invariant living in the author's head. If pinned-period
  collections with `.min`/`.max` are wanted, the design's collection half —
  qualifier-conditional orderability on the element type (Decisions 2 and 4) — is the
  only sound route.
- **One halfway house should be named and rejected:** shipping `.min`/`.max` on
  `set of period in 'days'` while still refusing the scalar `<` operator. It is
  technically coherent — `.min` returns a `period in 'days'` whose `.days` the author
  can then compare — but it makes the compiler order periods internally while telling
  the author periods do not order, which is the alignment directive's concern honored in
  spelling and violated in substance. Once the same-single-unit ordering relation exists
  anywhere in the system, refusing the operator spelling buys nothing: the semantic
  question ("does Precept order pinned periods?") has already been answered yes.
- So the real decision structure for the owner is sharper than the design's Open
  question 5 presents it: **the collection half is not an add-on to the scalar operator;
  it is the thing that decides whether the operator matters.** If collection ordering is
  ever wanted, build the design's full path (both halves, with the three corrections
  from § 3 above). If it is not, the smaller answer stands and the accessor route
  suffices for everything anyone has actually asked for. The wrong outcome is the
  middle: paying the semantic cost of a period ordering relation and then withholding
  its spelling.

---

## 6. Follow-up (same evening): is a conditionally-sound catalog operator unprecedented?

**The owner's pushback, in substance:** adding the ordering operations to the catalog —
is this really so bad? Surely the catalog already contains things that are only true
within a scoped subset of cases, with the compiler enforcing the scope so the bad cases
are structurally impossible. If that pattern already exists, "no NodaTime ordering means
no Precept ordering" is not an absolute rule, and scoping period ordering to
same-single-unit operands is Precept behaving normally, not an exception being carved
out.

**Method:** read the `Operations` catalog and the comparison sections of the language
docs for operators that exist on a type while being legal only for a restricted operand
subset, then verified each candidate's enforcement through `precept_compile` — because
"the docs say it's refused" and "the compiler refuses it" have already diverged once in
this file (§ Ship now item 4).

### The precedent exists, in at least five places, and it is the catalog's normal shape

**0 — division, the founding instance.** The owner supplied this anchor himself, and it
verifies exactly as he described. Division exists in the catalog unconditionally, for
every numeric type, despite division-by-zero being a real hazard — the philosophy itself
lists division-by-zero among the compile-time impossibilities. Verified live:
`field C as decimal <- A / B` with an unconstrained divisor is a compile **error** —
`PRE0083 "Division is unsafe: 'B' can be zero in the computed expression for field 'C'"`,
with the obligation *"Divisor must be non-zero"* reported **Unresolved**. Declare
`field B as decimal nonzero` and the same file compiles, the obligation discharged
**Proved (DeclarationAttribute)**. So the language's very first answer to "an operator
with a hazard" was never "refuse the operator" — it was "the operator exists, and every
use must sit inside the subset the proof system can establish is safe." One honest
distinction, which cuts *in favor* of the period rule: division's hazard is a runtime
fault at a single value, so its obligation is about a value's range and must sometimes
be discharged by guards; a cross-unit period comparison's hazard is a **silently wrong
answer** — no fault, no signal — which is stronger grounds for compile-time refusal, and
its condition is decided entirely from declarations at check time, making it *easier* to
enforce than division's, not harder.

**1 — `money` comparison.** The four ordering operations exist in the catalog
unconditionally (`Operations.cs:1018` `MoneyLessThanMoney`, `:1036`
`MoneyLessThanOrEqualMoney`, and their siblings), each carrying a
`QualifierCompatibilityProofRequirement` — *"Operands must have matching currency
qualifiers"*. Verified live: `rule A <= B` on `money in 'USD'` against `money in 'EUR'`
is a compile **error** — `PRE0114 "Cannot combine 'A' (Currency: 'USD') and 'B'
(Currency: 'EUR')"`. Same currency compiles. Cross-currency ordering is exactly as
undefined as cross-unit period ordering — it has no answer without extra context (an
exchange rate there, a reference date here) — and Precept's answer was not "money does
not order." It was "money orders, and the compiler refuses the pairs that have no
answer."

**2 — `quantity` comparison.** Same shape (`Operations.cs:525`, `:535`): verified that
`quantity in 'kg'` against `quantity in 'm'` is refused with `PRE0114`, while
`quantity of 'length'` against `quantity of 'length'` compiles with the obligation
**Proved** and conversion-aware semantics (`'5 km' >= '5000 m'` is a real comparison).
So the catalog even contains a *graded* version of conditional soundness: exact-match
where conversion is undefined, conversion-aware where a dimension makes it sound.

**3 — `choice` ordering, the closest analogue.** The comparison operations exist on the
choice type (`Operations.cs:866` `ChoiceLessThanChoice`, `:882`
`ChoiceLessThanOrEqualChoice`) and carry a **modifier** requirement — *"Both choice
operands must be declared ordered"*. Verified live: `rule Severity <= "high"` on a
`choice of string(...) ordered` field compiles with the obligation **Proved
(DeclarationAttribute)**; the identical rule on an un-`ordered` choice is a compile
error (`PRE0112`). This is precisely the proposed period shape: a type with no
unconditional ordering, whose ordering operations exist in the catalog and are legal
only when the *declaration* opts into the subset where an order is well-defined.

**4 — the period-as-duration coercion, inside the temporal system itself.** Canon's
approximation-stance table (`temporal-type-system.md`, § Approximation Stance) rejects
period-as-duration coercion "when the period contains calendar-variable units (years,
months)" — meaning the coercion *exists* and is legal for the fixed-length subset. And
Decision 16 (`temporal-type-system.md:1496`) builds `time + duration` bridging on
exactly the argument the ordering design makes: *"for sub-day units, `Duration` and
`Period` represent identical physical quantities — an hour is always exactly 3600
seconds."* Canon has already accepted, in a locked decision, that a NodaTime-wide
hazard does not reach a provably-safe subset, and built surface on that basis.

### Where the analogy is honest and where it thins

**What holds:** "the operator must be universally sound before it may exist" has never
been Precept's rule. The catalog's established pattern for magnitude-bearing domain
types is: register the operations, attach a requirement naming the sound subset, refuse
everything outside it at compile time — and division shows the same shape predates the
domain types entirely. Scoped period ordering is that pattern applied once more, with a
requirement ("both bases name the same single component") that is
*more* conservative than money's, and — unusually — one whose soundness was established
by direct measurement rather than assumed. On this axis the owner is right, and the
framing "an exception being carved out" is wrong: the design's own Decision 2/3
machinery (`OrderingCondition`, `QualifierRelation`) is an attempt to make the existing
pattern explicit catalog metadata instead of scattered special cases.

**What does not hold, and must be said plainly — though it is smaller than it looks:**

1. **The alignment directive is not a soundness rule, so soundness precedent cannot
   discharge it by itself.** `temporal-type-system.md:244` item 4 is a *deference* rule:
   when NodaTime's designers withheld something, Precept treats the withholding itself
   as information. `money`, `quantity`, and `choice` are not NodaTime types — no
   upstream designer withheld anything from them — so their precedent shows the
   *mechanism* is Precept-idiomatic without touching the directive. What actually
   answers the directive is § 1 of this review: NodaTime's stated reason is the
   whole-type `IComparable` contract, C# gives NodaTime no way to express "orderable
   when the value holds one component," and Precept's qualifier system *can* express
   exactly that. The withholding is real information, the information is "the type is
   too wide," and the qualifier narrows the width. The precedent then shows the
   narrowing has an established idiom. Together that is a strong case for **amending**
   directive item 4 — but it is an amendment (the design's Open question 1 concedes
   this), and it is the owner's to make, not this review's. The amendment also needs
   wording that keeps `zoneddatetime` unordered, which is refused for a different
   reason: two total orderings exist and canon declines to pick one. "NodaTime's stated
   obstacle does not apply to the restricted subset" does that; "we restrict types all
   the time" does not.
2. **"Structurally impossible" is currently aspirational for every one of these types,
   equally.** The owner's phrase "irrespective of the proof power of the compiler
   currently" cuts both ways: § Ship now item 1 (verified in § 2 above) shows a binding
   path where a `queue of money` dequeues into a `money in 'USD'` field with **no**
   currency obligation raised. So the money precedent's enforcement has the same hole
   the period rule would inherit, today. This does not weaken the precedent — it says
   the precedent and the proposal stand or fall together on the same fix, which is
   exactly why the design makes item 1 a precondition.
3. **One difference of degree, for honesty's sake.** For money the unsound pairs are
   *cross-declaration* (USD field vs EUR field). For periods the design's rule must also
   police *within*-declaration width — a composite `period in 'years + months'` is
   unorderable against itself, per the measured 432 order flips. So the period
   requirement is finer-grained than any existing one: it inspects the shape of one
   basis, not just agreement between two. The design knows this — it is why Decision 3
   puts the relation on the requirement rather than the axis — but it means the period
   entry is the *most* conditional entry in the catalog, not just another instance of
   the pattern.

### Verdict on the follow-up

**The owner is right about the pattern, and the review's § 3 point about directive
item 4 should be read in that light rather than as a blocker.** Precept's catalog
already contains at least five operators/coercions that exist only within a structurally
enforced sound subset — division behind the nonzero-divisor obligation, money and
quantity comparison, choice ordering behind the `ordered` modifier, and the
period-to-duration coercion for fixed-length units. Division is the founding instance
and the philosophy's own showcase; choice ordering is the same shape as the proposal
down to the proof requirement discharged from the declaration. Scoped period ordering would be consistent
with how Precept already does soundness, not an exception.

What survives of the deference concern is narrow and procedural: directive item 4 is
written as an absolute, the proposal contradicts its letter, and an owner-approved
amendment — worded so `zoneddatetime` stays out — is the honest way through, not a
reinterpretation. Since the owner is the directive's author and is now the one supplying
the argument against its absoluteness, that amendment is his to make whenever he wants
it. Nothing in this section changes § 5's conclusion about *when* to build: the
precedent makes the operator legitimate; the collection question decides whether it is
worth building.

---

## 7. Recommendation

This section synthesizes §§ 1–6 into one position. It is Frank's recommendation, not a
locked decision — the lock is the owner's, and item 3 below states exactly what it needs
from him.

### 7.1 Bottom line

**Build the ordering operators — `<`, `<=`, `>`, `>=` on `period` — scoped strictly to
two operands pinned to one and the same single unit, with every other pairing (mixed
unit, composite basis, category-qualified, unqualified) refused at compile time.** This
supersedes my first-pass leaning toward the "smaller answer" (no operator, accessor
route only), and § 5 is what changed it.

The case, by section:

- **§ 1–2:** the hazard NodaTime avoided is the heterogeneous-component case, and the
  same-single-unit rule genuinely sidesteps it rather than relocating it — verified
  independently, including the composite `years + months` order flips the rule refuses.
  NodaTime's withholding is forced by C#'s whole-type `IComparable` contract; Precept's
  qualifier system can express the condition C# cannot.
- **§ 5:** the accessor route (`A.days <= B.days`) is a genuine equivalent for scalar
  comparisons but **structurally cannot reach collections** — `.min`/`.max` legality is
  a trait of the element type itself, and Precept has no element-wise projection that
  could ever supply it from outside. The only workaround (a keyed queue with a bare
  integer key) stores its correctness invariant in the author's head. So the operator is
  not merely a nicer spelling; it is the only route to ordered collections of pinned
  periods. My first-pass verdict rested on "legibility, not capability," and that
  premise is false for collections.
- **§ 6:** "the operator exists, restricted to the provably sound subset" is not an
  exception being carved out — it is the catalog's normal shape, verified live in five
  places: division behind the nonzero-divisor obligation, money and quantity comparison
  behind currency/unit matching, choice ordering behind the `ordered` modifier, and the
  period-to-duration coercion for fixed-length units. Choice ordering is the same shape
  as this proposal down to the declaration-discharged proof requirement. And the
  distinction that matters cuts in favor: a cross-unit comparison's hazard is a silently
  wrong answer, which is *stronger* grounds for compile-time refusal than division's
  runtime fault, and its condition is decided entirely from declarations.

One honest qualifier on the collection premise: no sample file declares a collection of
periods today, so the collection need is anticipated rather than demonstrated. If the
owner reads that as "wait for demand," the smaller answer remains coherent — but § 5's
"wrong outcome is the middle" point then binds: do not later ship collection `.min`/`.max`
on pinned periods while withholding the scalar operator. Decide the semantic question
once, here.

### 7.2 What Decision 14 in `temporal-type-system.md` should say

Rewrite, per the design's Decision 8 (do not delete or renumber — it is cited by number
elsewhere). The rewritten decision should state:

1. **The rule:** `period` supports `==`/`!=` unconditionally. It supports `<`, `<=`,
   `>`, `>=` if and only if both operands resolve, on the temporal-unit axis, to a basis
   naming exactly one component and the same component on both sides. Everything else —
   mixed units (`in 'days'` vs `in 'months'`), composite bases (`in 'years + months'`
   even against itself), category qualifiers (`of 'date'`, `of 'time'`), unqualified
   `period` — is a compile-time error, not a computed answer.
2. **The corrected reason:** NodaTime withholds ordering because `Period` is one type
   spanning every component combination and `IComparable<Period>` cannot be conditional
   on a value's contents. That reason is real and does not reach a value known to hold
   one count of one unit — established by measurement (§ 2, and the design's § What was
   measured), scoped to the ISO calendar (§ 7.4 item 2).
3. **Both original rejections survive:** reference-date ordering and approximate
   ordering remain rejected. This rule is neither — it converts nothing and consults no
   reference date.
4. **What is refused for reach rather than correctness:** `weeks` vs `days` (exact in
   the ISO calendar) and same-composite time-only bases (exact via `ToDuration`) are
   refused to keep ordering consistent with structural equality. The design's falsifier
   3 (three or more real definitions forced to change declarations purely to compare)
   is the trigger for revisiting that boundary.

### 7.3 What I need from Shane before this locks

**An amendment to the alignment directive, `temporal-type-system.md:244` item 4.** The
current text — *"When NodaTime has no natural ordering on a type, Precept has no natural
ordering on that type"* — is contradicted by this proposal's letter regardless of how
sound the subset is (§ 3, § 6). I cannot amend it; it is the owner's directive and the
design's own Open question 1 correctly routes it to him. Proposed wording, offered as a
starting point:

> 4. When NodaTime has no natural ordering on a type, Precept has no natural ordering on
>    that type — **unless NodaTime's stated obstacle is the width of the type and a
>    Precept qualifier provably removes it.** A `period` pinned to a single unit orders
>    against the same single unit, because the ambiguity NodaTime names ("how 'one
>    month' compares to '30 days' depends on where you start") cannot arise between two
>    counts of one unit. This carve-out does not reach: composite or mixed-unit periods
>    (the ambiguity is real there — measured, not assumed); `zoneddatetime` (refused for
>    a different reason — two total orderings exist, instant and local, and Precept
>    declines to pick one; that refusal stands).

The load the wording must carry: the exception is keyed to *NodaTime's own stated
reason* being inapplicable, not to Precept wanting the feature — so `zoneddatetime`,
where the obstacle is genuine plurality of orderings rather than type width, stays
refused, and nothing else slips through on analogy.

Secondarily, Shane should give a verdict on the design's Open question 4 (the
comparator-survey research gate): waive it for this decision or require the survey
first. My view: the direct measurement (§ 2, both mine and the design's) is stronger
evidence than a comparator survey would be for this specific rule, and PostgreSQL — the
one system that does order intervals — agrees with Precept's canon about why components
cannot be merged. I would waive it with that rationale recorded.

### 7.4 Remaining blockers before Locked status

In dependency order:

1. **§ Ship now item 1 (binding-path qualifier hole) must land first.** The rule's
   entire correctness argument reads a declared qualifier and assumes it constrains the
   value; § 2 verified one path where it does not. The design already makes this a
   precondition — hold it to that. (Items 2–4 of § Ship now travel with it; all are
   defects regardless of the ordering outcome.)
2. **Re-scope the soundness claim to the ISO calendar** and add calendar-system support
   as a re-verification trigger in § Operational dimensions — the Badi counterexample
   (§ 3) falsifies the claim as currently written, even though it is unreachable from
   Precept today.
3. **Cite the `zoneddatetime ± period` refusal as a precondition** of the "no position
   on day length" claim (§ 2) — if that canon rule ever moved, the time-unit half of the
   ordering claim would need re-derivation.
4. **Mixed-unit and composite refusals must be compile errors with the new messages**
   (`PRE0165`/`PRE0166` per the design), never a silently computed answer — and the
   `PRE0018` incoherent message must be replaced in the same change, since it is the
   author-facing face of this whole area.
5. **Decision 6's relational-fact type gate ships in the same change** as the operators,
   not after — the design is right that the hole is live on day one otherwise.
6. **The directive amendment (§ 7.3) is recorded before or with the Decision 14
   rewrite** — the design cannot proceed on its own authority past
   `temporal-type-system.md:244`, and it says so itself.

With those six closed, my adversarial objections are exhausted. The proposal is not the
hazard the owner feared: the thing NodaTime left out is still left out — Precept would
be building the narrower thing NodaTime's designers could not express in their type
system, in the way Precept already builds everything else.
