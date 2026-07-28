# Periods and ordering — what the ruling settles, and what is left to decide

This is an analysis, not a design. The owner ruled on 2026-07-26 that periods pinned to the same single unit order, overriding locked Decision 14 in `temporal-type-system.md`. The semantics are settled and are not re-argued here. What follows is what is actually true about implementing that ruling, what it costs, what canon says that nobody has yet reconciled with it, and the small number of things the owner still has to choose. New language surface goes through `/design`; this document exists so that the design pass starts from facts rather than from a first draft.

---

## The four things worth reading first

**1. The ruling can be implemented. Every single-unit basis gives a determinate order — but not for the reason previously written.** A `period in '<unit>'` value is one integer count of one named unit. Comparing two of them compares two integers: total, deterministic, no reference date, no conversion factor. There is no basis where this fails. The argument that was previously offered — that days, weeks, hours, minutes and seconds are fixed-length so the counts measure physically equal units — is contradicted by canon twice (`business-domain-types.md:1151`, `:1838`–`:1839`: days are *"No (23 or 25 hours near DST)"*, weeks are *"No (contains variable-length days)"*) and it is not needed. Three days is never longer than five days whatever a day's length varies to.

**2. Ordering a months- or years-based period orders the declared count, not the elapsed time — and that is already true of equality.** `business-domain-types.md:1699` accepts, as D3's stated tradeoff, that `period in 'months'` on a date difference that is not on a month boundary *"will silently truncate toward start."* `business-domain-types.md:1819` confirms D3 and D14 apply at the same time on the same field. So two `period in 'months'` fields — one set from Jan 1 to Jan 31 (30 days elapsed, decomposes to 0 months), one from Feb 1 to Mar 1 (28 days elapsed, decomposes to 1 month) — compare `0 < 1` while the real spans go the other way. Ordering does not create that; the value threw the information away before any comparison happened, and `==` has the same property. But "nothing is approximated" cannot be said in front of D3, and the honest description of the feature is *periods pinned to one unit order by their count in that unit*.

**3. Option 1 is not free. It conflicts with a locked directive that nobody has put on the table.** `temporal-type-system.md:244` is item 4 of the NodaTime alignment directive: *"When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type."* NodaTime's `Period` has no `IComparable` — that is the ruling's own deciding evidence. Item 4 is stated about the type, not about a comparison mechanism, so "same-basis ordering never calls `Period.CompareTo`" answers Decision 14 without reaching it. Directive item 4 governs all eight temporal types, so amending it is not a local edit. This has to be settled before the design pass, not inside it.

**4. There is no build path that does periods alone.** `PRECEPT0010d` requires that any type carrying same-type ordering operations also carries `TypeTrait.Orderable` (`Precept0010TraitOperationConsistency.cs`, documented at lines 28–29), and its only escape is a suppression for operations gated on a *modifier* — how `choice of T(...) ordered` is allowed to carry ordering without the flag. Qualifier-gated ordering is not modifier-gated, so it trips the check. And `.min`/`.max` read that flat flag with no qualifier input (`TypeChecker.Expressions.Callables.cs:1026–1045`), so putting `Orderable` on `Period` would immediately grant `.min`/`.max` on unqualified `set of period`, contradicting `collection-types.md:530`. The per-declaration ordering capability is therefore a precondition, not a follow-on — and canon already owes the same thing to `money`, `quantity` and `price` (`collection-types.md:541`, `:543`, `:544`).

---

## What was ruled, and what it covers

Commit `338ecafa`, recorded in the owner's name on 2026-07-24, and confirmed on 2026-07-26 in his words: *"mine, I ruled that we should make same-basis periods orderable, which was against canon."* The ledger text the commit wrote reads:

> "`in`-qualified periods are **orderable exactly when both operands share one declared single basis** (`period in 'days'` vs `period in 'days'`), scalar and collection alike. Cross-basis (`days` vs `months`) and unqualified periods are **not** orderable and must be rejected at the comparison site."

Two operands both known to be pinned to one and the same single unit. Ordering operators on them, and `.min`/`.max` on a collection of them. It does not cover unqualified periods — `field GracePeriod as period default '10 days'`, which is what `samples/equipment-lease-agreement.precept:36` actually writes — nor `period of 'date'`, nor `period of 'time'`, nor composite bases.

One sub-case looks new and is already answered. An unqualified period inside a guard that pins it: `business-domain-types.md:1741`–`:1755` establishes that an open field participates once a guard narrows it, and lists the period case by name — `when X.basis == 'hours + minutes'` pins X's basis. The ruling puts the check at the comparison site, so a guard that proves a single basis satisfies it the same way a declaration does.

The withdrawal (`e33be4b6`) claimed the owner had reaffirmed Decision 14. He had not. The doc edit that followed (`d9bc218b`) changed exactly one line, and the text it replaced read: *"Unit-qualified: all elements are single-basis periods. Ordering is by single component value (`.days`). Valid bases: `days`, `months`, `years`, `weeks`, `hours`, `minutes`, `seconds`."* Striking the withdrawal at `collection-types.md:533` is a revert, not new text, and the seven bases it lists are exactly the ones Option 1 admits.

---

## What survives of Decision 14, and what does not

Decision 14 (`temporal-type-system.md:1480`) has four parts.

- *"NodaTime's `Period` has no `IComparable`"* — **survives as a fact.** Same-basis ordering never calls a comparison on `Period`. But see the alignment directive below: the fact survives, and so does a separate commitment built on it.
- *"Is 1 month greater than 30 days? It depends on which month"* — **survives verbatim**, and becomes the reason for the restriction rather than the reason for the ban. It is the cross-unit case, which stays refused.
- **Both rejected alternatives survive.** Reference-date ordering is still rejected, and same-basis ordering is not it — the reference date provably cannot change the answer. Approximate ordering ("a month is about 30.44 days") is still rejected, and same-basis ordering is not it either — no conversion factor is applied. The ruling is compatible with Decision 14's reasoning rather than a repudiation of it.
- **The scope sentence changes.** *"`==` and `!=` only"* becomes *"`==` and `!=` only, unless both operands are known to be pinned to the same single unit."* The stated cost (*"Cannot sort by period or use `<`/`>` guards"*) stops being true for pinned periods.

**What does not survive without a separate decision from the owner: the alignment directive, item 4.** `temporal-type-system.md:244`. The strongest available reading is the one the ruling itself offers — a single-unit period's live component is a plain `Int32`, NodaTime orders `Int32`, so the comparison lands on a NodaTime type that *does* order, and the directive is honoured rather than broken. That is a reading, not what the line says today, and the line as written governs `timezone` and `zoneddatetime` too. Any amendment has to be written so it does not accidentally admit `zoneddatetime < zoneddatetime`, which canon refuses three times (`temporal-type-system.md:955`, `:1331`, `:1346`) because there are two defensible reductions — by instant or by wall clock — and canon makes the author name one. The distinguishing property is that a single-unit basis leaves exactly one component, so there is exactly one reduction. An amendment keyed on that keeps `zoneddatetime` out. Writing it is design-pass work; authorising it is not.

Two things in the same document already sat uneasily with the blanket version and are now consistent. `temporal-type-system.md:844` grants `nonnegative` on `period`, defined as *"value ≥ zero period"* and *"compared against `Period.Zero`"* — a `≥`, in the file that says ordering does not exist. And `collection-types.md:553` says qualifiers on `money`, `quantity` and `price` *"restrict elements to a homogeneous denomination and unlock ordering"* — the same shape of rule.

---

## What machinery already exists, and what does not

**The shape exists and is in use.** `money` and `price` order only within a matching currency (`business-domain-types.md:511`, `:901`), expressed in the catalog as `QualifierMatch.Same` plus a qualifier-compatibility proof requirement naming an axis (`Operations.cs:1018–1053` for the money comparisons). The axis periods need, `QualifierAxis.TemporalUnit`, already exists (`Type.cs:51`), and the type checker and proof engine already discharge it for periods by declaration, by literal and by guard (`type-checker.md:886`, `proof-engine.md:777`). So the *pattern* — comparison operations carrying a qualifier obligation on a named axis — is real and copyable.

**Three things are missing, and one of them is bigger than it looks.**

First, **the relation is wrong**. The money comparisons discharge `QualifierAxis.Currency` by exact value match. The `TemporalUnit` axis discharges by **subset**, not equality: `proof-engine.md:777` says so directly, and `type-checker.md:886` gives the rule as `source.Components ⊆ target.Components`, so `{hours}` satisfies `{hours, minutes}`. Ordering needs a third relation neither provides — both sides pinned to the same basis, and that basis naming exactly one unit. Copying `QualifierMatch.Same` onto `QualifierAxis.TemporalUnit` does not inherit the right comparison.

Second, **the existing single-basis rule is not as enforced as it looks**. `business-domain-types.md:1847` locks the same "one declared unit" line for a different operation: *"A `period` cancels a single-unit time denominator **only when the period has a single matching basis**. `period in 'hours'` cancels `price in 'USD/hours'`. `period in 'hours + minutes'` does **not** … Converting a multi-basis period to a single unit would require Precept-invented arithmetic that NodaTime deliberately refuses."* That is the right precedent to reuse — same type, same line, already locked, and it means ordering restricted to a single basis needs no second override while ordering extended past it does. But `type-checker.md:884` records the positive half as a known gap: *"A period declared only by **basis** (`period in 'hours'`) does **not** yet cancel … a known gap against § D15"*, and *"Cancellation discharge is currently at **dimension** granularity and does not yet reject denominator/legal-basis mismatches for declared operands."* So the rule is canon; the enforcement on the basis axis is not built. The reuse is a pattern, not working machinery.

Third, **the flat orderability flag**, covered in point 4 of the headline. This is the item that determines the size of the build.

One thing that helps: a per-declaration ordering capability is not a new idea in this codebase. `choice of T(...) ordered` already turns ordering on by declaration (`primitive-types.md:335–336`), and the type checker already adds `Orderable` for it at check time, two lines from where the period case would go.

---

## The options

Nothing in `samples/` compares two periods with any operator, so every option is strictly widening — no existing file stops compiling. The one magnitude test anybody wrote on a period is `on Issue ensure WorkedTime.hours > 0` at `samples/contractor-invoice-settlement.precept:84`, which works because it compares an integer accessor, not a period.

The counterweight, which the previous draft withheld: **there are seven period fields across the 78 samples, and not one of them is a single-unit `in`-qualified period.** Five are unqualified (`hotel-reservation-management:40`, `equipment-lease-agreement:36`, `library-inter-library-loan:44`, `non-profit-membership-renewal:36`, `vehicle-registration-renewal:33`), one is `of 'time'` (`contractor-invoice-settlement:35`), one is `in 'years + months'` (`equipment-lease-agreement:33`). Option 1 as ruled orders nothing anyone has written, and its worked example has to invent the declarations. That is not an argument that Option 1 is wrong — the samples illustrate and do not measure — but the same evidence was being used to size the wider options and it should be reported on both sides.

### Option 1 — one declared unit, both sides (what was ruled)

Two periods compare when both are known to be pinned to one and the same single unit, by declaration or by guard. Everything else is refused.

**What stops compiling:** nothing that compiles today. What stays refused: any comparison where either side is unqualified, is `of 'date'` or `of 'time'`, or carries a composite basis; and any comparison between two different units.

```precept
precept ServiceAgreement

field NoticePeriod   as period in 'days' optional
field MinimumNotice  as period in 'days' default '30 days'
field BillingCycle   as period in 'months' default '1 months'
field GracePeriod    as period default '10 days' editable

# Accepted under Option 1 — both sides pinned to days
rule NoticePeriod >= MinimumNotice when NoticePeriod is set because "Notice of {NoticePeriod} is shorter than the {MinimumNotice} minimum"

# Refused — different units
rule NoticePeriod <= BillingCycle because "Notice cannot exceed one billing cycle"

# Refused — GracePeriod has no declared basis
rule GracePeriod <= MinimumNotice because "Grace cannot exceed the notice minimum"
```

**Refusal messages.** The current message is `temporal-type-system.md:866`: *"Periods can't be compared with `<` or `>` — is 1 month greater than 30 days? It depends on which month."* That stays right for one of the three refusals and is wrong for the other two, because it says the comparison is impossible when a fix is available. Three messages, one per reason:

- *Neither side names a unit* — "`GracePeriod` and `MinimumNotice` can't be compared with `<` or `>` until each one says which unit it counts in. Declare it — `field GracePeriod as period in 'days'` — or narrow it where you compare: `when GracePeriod.basis == 'days'`."
- *Different units* — "`NoticePeriod` counts days and `BillingCycle` counts months, so `<` has no answer — is 1 month greater than 30 days? It depends on which month. Compare dates instead: `StartDate + NoticePeriod < StartDate + BillingCycle`."
- *More than one unit possible* — "`LeaseTerm` is declared `in 'years + months'`, so one value can mix two units and there is no single number to compare. Pin the field to one unit (`period in 'months'`), or compare one component: `LeaseTerm.months`." The same message covers `of 'date'` and `of 'time'`, which say which components are *allowed*, not that there is exactly one.

**What it costs elsewhere.** Not nothing, as previously claimed. It needs the amendment to `temporal-type-system.md:244` described above, and it needs the per-declaration ordering capability built before anything ships. What it does *not* need is a second override: `business-domain-types.md:1847` already draws the one-declared-unit line on this exact type, so Option 1 extends a locked rule rather than fighting one.

**A narrowing available inside Option 1.** Given point 2 of the headline, the owner could restrict ordering to units with a fixed absolute length — `hours`, `minutes`, `seconds` — so that a comparison always reflects elapsed time. The argument against: declaring `period in 'months'` is the author's statement that months is the unit the business counts in, and ordering by month count is ordering in the unit they chose; the truncation is already accepted for assignment and equality; and refusing months and years would remove the case authors most obviously want. The argument for: it is the only version where "A comes before B" means what a reader assumes. This is a real fork and it lives inside the ruling rather than outside it.

### Option 2 — also order composite bases whose units share a fixed ratio

The previously-drafted Options 2 and 3 ("free of years and months" and "years + months") do not survive contact with canon as separate options. `'days + hours'` contains no years or months and still cannot be totalled, because canon says a day is 23 to 25 hours near DST (`business-domain-types.md:1151`, `:1838`). The coherent version is one option in three groups, where every unit converts to every other by a ratio that never varies:

- `{seconds, minutes, hours}` — canon marks all three fixed-length (`business-domain-types.md:1146`–`:1148`, `:1836`–`:1837`).
- `{days, weeks}` — a week is seven days whatever a day's length. Canon states the ratio only on the `duration` conversion path (`temporal-type-system.md:141`, "1 week = 168 h"); it does not state it for `period`. Worth noting rather than assuming.
- `{months, years}` — twelve months per year, grounded in `temporal-type-system.md:1726`, *"Fiscal/business calendars | Excluded | ISO calendar only."*

Within a group, order by the total; across groups (`'months + days'`, `'days + hours'`), refuse. The owner can authorize one group and not the others; the cost is the same shape per group.

**What stops compiling:** less than Option 1 refuses. Both real author cases become writable:

```precept
field WorkedTime   as period of 'time' default '0 hours'
field LeaseTerm    as period in 'years + months' optional
field MaxLeaseTerm as period in 'years + months' default '5 years'

# Refused under Option 1, accepted under Option 2 (clock group)
on Issue ensure WorkedTime > '2 hours' because "Invoices below the two-hour minimum are not billable"

# Refused under Option 1, accepted under Option 2 (calendar group)
rule LeaseTerm <= MaxLeaseTerm when LeaseTerm is set because "Lease term {LeaseTerm} exceeds the {MaxLeaseTerm} maximum"
```

**What it costs.** Two things, and they are not small.

First, it contradicts `business-domain-types.md:1847` directly. That decision refuses to convert a multi-basis period to a single unit, in this exact type, on the ground that it *"would require Precept-invented arithmetic that NodaTime deliberately refuses."* Totalling `'1 hour + 30 minutes'` into 90 minutes is that arithmetic. Overriding it is a second owner decision on top of the first. (One factual check the design pass owes: NodaTime's `Period.Normalize` is understood to fold weeks into days and normalize the time components while leaving years and months alone. If so, canon's stated *reason* is imprecise about NodaTime for two of the three groups. The *decision* still binds either way; only the reasoning would need repair.)

Second, ordering and equality would disagree on values of the same declared type. `'90 minutes'` and `'1 hour + 30 minutes'` are both legal in a `period of 'time'` field; `'1 year'` and `'12 months'` are both assignable to `period in 'years + months'` under the subset rule at `business-domain-types.md:1829`. Under Option 2 each pair compares as neither less-than nor greater-than, and is `!=` under locked Decision 15 in `temporal-type-system.md:1487`, whose stated cost is already *"`7 days == 1 week` is `false`."* Either that stands as an accepted oddity or Decision 15 gets rewritten for the qualified case.

**Refusal messages under Option 2.** Two remain:

- *No declared unit at all* — same as Option 1's first message.
- *Units from different groups* — "`LeaseTerm` mixes months with days. Months have no fixed length in days — 28 to 31 — so the two parts can't be added into one number to compare. Compare within one family (`'years + months'`, `'weeks + days'`, `'hours + minutes'`), or compare a single component: `LeaseTerm.months`."

---

## Everything that changes

**Documentation.** Fifteen line references across three canonical files state the struck position. Each row is one line, so the count and the list agree.

| File | Line | What it says |
|---|---|---|
| `temporal-type-system.md` | 68 | Contents entry for Decision 14 |
| `temporal-type-system.md` | 244 | Alignment directive item 4 — *"When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type."* Governs all eight temporal types; needs an owner-authorized amendment, not a reconciliation |
| `temporal-type-system.md` | 805 | "Not supported" table — `<`, `>`, `<=`, `>=` |
| `temporal-type-system.md` | 866 | The teachable message an author sees |
| `temporal-type-system.md` | 1328 | Comparison-rules table, `period` row — "No natural ordering" |
| `temporal-type-system.md` | 1345 | Cross-type table — `period < period` |
| `temporal-type-system.md` | 1480 | Decision 14 itself — heading and all four parts |
| `temporal-type-system.md` | 1736 | *"The five with a natural ordering additionally support `.min`/`.max`"* |
| `temporal-type-system.md` | 1745 | Collection inner-type table, `period` row |
| `temporal-type-system.md` | 1749 | The `.min`/`.max` error text for `set of period` |
| `temporal-type-system.md` | 1776 | Type-checker work list — "period ordering rejection" |
| `temporal-type-system.md` | 1818 | Language-server work list |
| `collection-types.md` | 147 | Definition of "orderable" — omits `period` |
| `collection-types.md` | 533 | *"Periods do not order regardless of basis"* — revert to the pre-`d9bc218b` text |
| `precept-language-spec.md` | 1449 | *"`period`, `timezone`, and `zoneddatetime` support only `==`/`!=`"* — a flat statement in the spec's own voice |

**Compiler documents that change and were not previously listed:**

- `type-checker.md:886` and `proof-engine.md:777` — both state the `TemporalUnit` discharge as subset. Ordering needs a third relation and both must say so.
- `type-checker.md:884` — records the basis-path cancellation gap. Whatever the design does about the basis path affects this paragraph.
- `diagnostic-system.md` — owns every diagnostic code and message; each option adds or changes at least one.

**Lines that must be checked and must not move:**

- `collection-types.md:530` (unqualified `period`) and `:531` (`period of 'date'`) — refused under both options.
- `collection-types.md:532` (`period of 'time'`) — correct under Option 1, changes under Option 2.
- `collection-types.md:386` and `:1098` — the `by P` ordering key is restricted to *"numeric types and `choice of T(...) ordered`."* Periods do **not** gain a key type from this ruling; the risk runs the other way, because a flat `Orderable` flag on `Period` could admit `queue of string by period` against both lines. Note that canon is already inconsistent with itself here: `collection-types.md:65` shows `field AuditLog as log of string by instant`, an `instant` key, which those two lines forbid. Worth fixing while the area is open, but a separate matter.
- `temporal-type-system.md:801` — the `DegeneratePeriodComparison` warning covers only `==`/`!=` between non-overlapping constants. Whether an ordering counterpart is wanted is a decision the design should take deliberately.
- `precept-language-spec.md:1696` — `min`/`max` modifiers exclude all temporals. Ordering does not bring bounds with it; `duration` already orders and cannot be bounded. The asymmetry becomes visible once `rule LeaseTerm <= '24 months'` is legal and `field LeaseTerm as period in 'months' max '24 months'` is not. Defensible, but restate it rather than leave it to be discovered.
- `temporal-type-system.md:955`, `:1331`, `:1346` — the `zoneddatetime` refusals. The directive amendment must not admit these.

**Consequences that are not documentation:**

1. Four ordering entries alongside the two period equality entries the catalog already carries (`OperationKind.cs:151–152`, `Operations.cs:738–743`), on the `TemporalUnit` axis, with a same-and-single relation that does not exist yet.
2. A per-declaration ordering capability for the collection accessor path, replacing the flat trait. **Not deferrable** — see headline point 4. It is already owed to unqualified `money` (`collection-types.md:541`), `quantity` (`:543`) and `price` (`:544`).
3. `Precept0010TraitOperationConsistency` needs a new exemption category. Its existing suppression keys specifically on a `ModifierRequirement` in the operation's proof requirements; a qualifier-gated ordering operation carries a `QualifierCompatibilityProofRequirement` instead and trips the check as written.
4. **Guards and rules over periods would be legal and would prove nothing.** `temporal-type-system.md:1713` defers `NumericInterval` for duration and period. Without it no period guard can discharge a period obligation, so `when NoticePeriod >= MinimumNotice` narrows nothing downstream. Separately, `diagnostic-system.md:420` states the intended default-fold rule as three-way — *"a default that is unknown or cannot be folded rejects as well, because it leaves the base case of the induction open"* — which means a rule the folder cannot evaluate over periods is refused, not ignored. The same paragraph notes the implementation currently emits only on a proven-false fold, so this is intent, not present behaviour. Either way it belongs in the sequencing decision below, not in a footnote.
5. `BUG-047` is unaffected in its primary defect and its `positive`-on-`period` sub-point is already withdrawn (`docs/Working/bugs.md:457`, withdrawn 2026-07-26 on this ruling). The broken sign projection — `'1 days + -24 hours'` reading as positive — survives only for unqualified and composite periods, where it already was.
6. No sample migration. Nothing breaks.

---

## What has to be decided

**Question 1 — how wide.** The ruling is Option 1, and Option 1 is what the rest of canon supports: `business-domain-types.md:1847` already draws the one-declared-unit line on `period`, so no second override is required. Option 2 is an extension beyond the ruling, and it costs an override of `:1847` plus an accepted disagreement between ordering and equality on values of one declared type. Option 2 buys the two comparisons authors visibly wanted (`WorkedTime > '2 hours'`, `LeaseTerm <= MaxLeaseTerm`) and can be authorized one unit-group at a time. Inside Option 1 there is the narrowing described above: restrict to `hours`/`minutes`/`seconds` so that ordering always tracks elapsed time, at the cost of the months and years cases.

**Question 2 — the alignment directive.** `temporal-type-system.md:244` item 4 has to be amended or explicitly overridden before the design pass runs, and it governs all eight temporal types. The available reading — a single-unit period's live component is an `Int32`, which NodaTime does order, so the directive is honoured — is the ruling's own argument and is worth taking, but it is a change to a locked directive and it must be worded so `zoneddatetime` stays refused.

**Question 3 — sequencing against the period interval domain.** Ordering can ship before `NumericInterval` for period exists, in which case comparisons are legal and contribute no proof facts, and rules over periods may be refused by the fold rather than accepted. Or the interval domain is built first and ordering lands usable. This is a real fork with a cost either way, and it decides whether the feature is usable on delivery.

**Not a question, recorded so it is not asked again.** Whether the ordering rule gets written once for all four types or twice is already directed. The ruling's own downstream list says *"close the set-of-money unqualified `.max` false-clean under the same rule"* and *"design them together."* The build makes it stronger than a preference: there is no shippable period-only branch, because the flat trait cannot be left in place. One line in the design, not a fork.

---

## What the critics found

Both critiques returned "needs-correction." Twenty-six findings; the substantive ones changed the document, several were duplicates across critics, and three were wrong or unconfirmable.

### Findings that changed the document

**The alignment directive** (`is-it-true` #3, `is-it-really-his` #2 — both, and correctly). `temporal-type-system.md:244` was missing and its absence made "Option 1 costs nothing elsewhere" false. Verified: line 244 is item 4 of the directive, worded about the type. It is now headline point 3, a row in the change register, its own paragraph in the Decision 14 section, and Question 2. The related sub-finding — that the previous draft cited `:244` in Option 3's cost paragraph for a proposition it does not contain (refusing Precept-invented arithmetic) — is correct; that citation is removed and the arithmetic point now cites `business-domain-types.md:1847`, which does say it.

**The fixed-length premise** (`is-it-true` #2, `is-it-really-his` #6 — both, correctly). Canon says days and weeks are not fixed-length at `business-domain-types.md:1151` and `:1838`–`:1839`, and the previous draft cited the second of those against Option 2 while asserting the opposite in its opening paragraph. The premise is dropped. Headline point 1 now makes the argument that survives its own citations: comparing two counts of the same unit is comparing two integers, whatever the unit's length varies to.

**D3 truncation** (`is-it-true` #1, correctly, as a blocker). `business-domain-types.md:1699` accepts silent truncation toward start, and `:1819` confirms D3 and D14 apply at once. "Nothing is approximated" is gone. The critic's counterexample is reproduced in headline point 2. One qualification the critic did not make: the truncation is a property of the value, not of the comparison — equality and assignment already carry it — so the finding does not show the ruling is unimplementable. It shows the feature must be described as ordering counts, not lengths. That distinction is now also offered to the owner as a possible narrowing inside Option 1.

**The machinery is thinner than claimed** (`is-it-really-his` #4, `is-it-true` #6 — both, correctly). Two separate errors. `proof-engine.md:777` and `type-checker.md:886` both state the `TemporalUnit` discharge as **subset**, so `QualifierMatch.Same` on that axis does not inherit the relation ordering needs — this is a third relation, not "a new place an existing test is applied." And `type-checker.md:884` records the basis-path cancellation as a known gap, so `CompoundPeriodDenominator` does not demonstrate working enforcement on the basis axis. Both corrections are in § What machinery already exists, and both proof-engine and type-checker docs are added to the change register.

**The trait forcing makes "twice" impossible** (`is-it-really-his` #1, correctly). Verified in source: `Precept0010TraitOperationConsistency.cs` documents `PRECEPT0010b`/`d` at lines 22–29, and its suppression tests `IsModifierGated` only. `TypeChecker.Expressions.Callables.cs:1026–1045` reads the flat flag with a per-instance override for ordered choice alone. Question 2 as previously posed is withdrawn and replaced by a statement; the ruling's own downstream text (`338ecafa`: *"design them together"*) is quoted. `is-it-true` #13's related point — that `quantity`'s unqualified refusal at `collection-types.md:543` was uncited — is fixed.

**Analyzer exemption is a build item** (`is-it-true` #11, correctly). Now consequence 3, stated concretely rather than as "whatever shape it takes."

**Proof-stage rejection risk** (`is-it-really-his` #3, correctly in direction). `diagnostic-system.md:420` does state the three-way fold verdict, and `temporal-type-system.md:1713` does defer the period interval. Promoted out of a numbered list into Question 3. One correction to the critic's framing: the unresolvable-fold rule applies to every type, not to periods specially, and the same paragraph says the implementation does not yet do it. What is specific to periods is that no interval domain exists for them at all, so no period guard can ever discharge a period obligation. That is the sharp version and it is what is written.

**Sample evidence withheld from Option 1** (`is-it-true` #5, `is-it-really-his` #8 — both, correctly). Verified: seven period fields, five unqualified, one `of 'time'`, one `in 'years + months'`; no single-unit `in`-qualified period exists in the corpus. The measurement now appears once, applied to all options, at the head of § The options.

**Refusal messages were described, not drafted** (`is-it-really-his` #10, correctly). Both options now carry actual wording, one message per distinct refusal reason.

**Label collision** (`is-it-true` #12, `is-it-really-his` #11 — both, correctly). Two documents each have a locked decision numbered 14 and 15 about `period`. Every reference now names its file, and the bare "D14"/"D15" forms are gone.

**Line citations** (`is-it-true` #9, `is-it-really-his` #12 — both, correctly). Verified and fixed: the single-basis cancellation rule is `business-domain-types.md:1847`, not `:1848` (which is "Duration is exempt"); the days and weeks fixed-length rows are `:1838`–`:1839`, not `:1837` (which is the `hours` row, reading "Yes"), and the phrase *"contains variable-length days"* lives at `:1151`, in the other table; the `min`/`max` modifier row is `precept-language-spec.md:1696`, not `:1694` (which is `nonzero`).

**Count of change sites** (`is-it-true` #10, correctly). Eleven rows carried fourteen line references. The register is now one line per row, fifteen rows including `:244`.

**Missing register entries** (`is-it-really-his` #9, correctly). `diagnostic-system.md`, `proof-engine.md:777`, `collection-types.md:386`/`:1098` and `temporal-type-system.md:801` are added — the last three as lines that must be checked and must not move, since they are hazards rather than edits.

### Where a critic was wrong, or could not be confirmed

**`is-it-really-his` #5 — "the `by P` check is literally a `TypeTrait.Orderable` test."** Not confirmed, and I believe it is wrong. Searching the pipeline for `TypeTrait.Orderable` returns three sites, all in `TypeChecker.Expressions.Callables.cs` and all in the accessor `RequiredTraits` check for `.min`/`.max`; there is no orderable-trait validation of a declared `by P` key type anywhere in `src/Precept/Pipeline/`. The declared key type appears to be carried through as `KeyType` without an orderability check at all — which is consistent with `collection-types.md:65` showing an `instant` key that the prose at `:386` forbids. The *conclusion* the critic drew is still worth acting on, because a flat `Orderable` flag is exactly the kind of thing a later `by P` check would read, so I kept it as a hazard note rather than as a described mechanism. The critic's earlier half of the same finding — that period does not "gain" a key type, because canon restricts P to numeric and ordered choice — is correct, and the previous draft's consequence 5 was backwards. It is removed.

**`is-it-true` #4** made the same correction about `by P` and is right on both halves, including the internal inconsistency at `collection-types.md:65`. Recorded above.

**`is-it-true` #7 — the three business types are not one rule.** Correct and now reflected: `money` (`business-domain-types.md:511`) and `price` (`:901`) require an exact currency match, but `quantity` at `:674` reads *"Same **dimension** required; auto-converts for comparison"*, and `collection-types.md:542` confirms `.min`/`.max` on `quantity of 'length'` normalize to the SI base unit with `'5 km' == '5000 m'` TRUE. So the pattern to copy is the money and price one. The critic's secondary observation is also confirmed — `Operations.cs:1056–1073` checks `QualifierAxis.Unit` identity for quantity comparisons, which disagrees with `:674`'s conversion-aware rule. Canon wins; that is catalog drift and a separate bug, noted here rather than folded into this decision.

**`is-it-true` #8 — "unqualified does not order falls out of prove-or-reject" is an inference, not canon.** The critic is right that no canonical line states what happens when *both* operands are open; `business-domain-types.md:1741`–`:1745` covers an open field used with a constrained one, and `:1823`–`:1827` covers literal, guard-narrowed and runtime-boundary sources. The claim is removed. What replaced it is narrower and checkable: the ruling itself says the check happens at the comparison site, and `temporal-type-system.md:886`/`proof-engine.md:777` show the open-period path is discharged by a `when X.basis == '…'` guard — so with no declaration and no guard there is nothing to discharge against. That is a statement about the mechanism, not a prediction about the proof engine.

**`is-it-true` #13, second half, and `is-it-really-his` #7 — the width question is already settled and should not be put back to the owner.** Half right. The scope of the *ruling* is settled and the document now says so plainly: Option 1 is the ruling, Option 2 is an extension requiring a second override. But the previous draft's option set was itself incoherent, so simply presenting the ruling would not have been enough. Old Option 2 was defined as "any period free of years and months," which would admit `'days + hours'` — and canon says a day is 23 to 25 hours (`business-domain-types.md:1838`), so that basis has no total. Old Options 2 and 3 are therefore merged into one option defined by fixed-ratio unit groups, which is both correct against canon and shorter. The owner still has a genuine choice about whether to authorize any extension, and now also has a narrowing available inside his own ruling that neither critic surfaced.

**`is-it-really-his` #13 — checks that came back clean.** Independently confirmed here and not re-litigated: the `d9bc218b` revert claim (one line changed; prior text as quoted; the seven bases match Option 1); the three commits as described; Decision 14's four parts read fairly, with both rejected alternatives quoted correctly; the guard sub-case closed by `business-domain-types.md:1741`–`:1755`; `temporal-type-system.md:844` granting `nonnegative` as a comparison against `Period.Zero`; `collection-types.md:553` on qualifiers unlocking ordering; and the catalog locations (`Type.cs:28`, `Type.cs:51`, `OperationKind.cs:151–152`, `Operations.cs:738–743`, `Modifiers.cs`). No new syntax is proposed anywhere in this document — every form shown already exists in canon.

**Did the critics establish that the ruling cannot be implemented?** No. Every single-unit basis yields one integer count, and comparing two integers is total and deterministic with no reference date and no conversion. What they established is that the previous justification was wrong in two ways, that a months- or years-based comparison orders counts rather than elapsed time, and that Option 1 carries costs — a locked directive amendment, a trait-model rebuild, and a sequencing decision against the deferred interval domain — that the previous draft priced at zero.