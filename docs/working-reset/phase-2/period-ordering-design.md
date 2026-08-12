---
status: Locked 2026-08-11 — **the owner set it, and these are his words.** He was told the design had been assessed lockable and deliberately left unlocked, and that two defects behind it were unfiled. His reply, complete and verbatim — *"yes lock the design and log the bugs."* No agent set this status. It is recorded with the quotation so a reader can check it against what he actually said rather than take an agent's label for authority. Before that it read `Externally-Grounded — 2026-07-28`, with owner rulings recorded 2026-07-29.
lock-scope: The lock covers **the eight decisions in § Decisions and nothing else.** They are enumerated at § Is this lockable § What the lock settles. **It does not say the code is done, because none of it is built.** Three code items and the six § Ship now repairs stay outstanding — they are implementation obligations carrying acceptance criteria, not unanswered questions, and § Is this lockable § What the lock does not settle lists them. § Ship now item 1, the binding path where a declared qualifier does not constrain the value, must land before the operators ship or the prevention claim is false on arrival; the lock does not discharge that sequencing obligation. **Two of the eight are short on the precedent leg and say so rather than inventing one** — Decision 6 has no precedent and records that there is none, and Decision 8 cites the project's own rationale rule rather than an instance of a decision being repaired this way. Both are disclosed in § Is this lockable and neither was papered over to get the lock.
open-questions: Four of the five open questions are now closed, two by the rulings in § Owner rulings, one by the owner amending the alignment directive in canon, and one because it turned out to be a defect rather than a question. § Open questions carries the disposition of each. The design's own first-draft recommendation was refuted by its red-team pass; § The short version carries the corrected one.
phase-target: Document reconciliation is phase 2 (readiness-plan row `N82`) and is **done**, 2026-07-29 — see [`period-ordering-reconciliation.md`](period-ordering-reconciliation.md). The repairs in § Ship now are unassigned bug-fix work that needs no phase gate. The ordering operator's mechanism now has a phase — `compiler-readiness-plan.md` § 1 and § 4 record it as folded into phase 5.
comparable-systems-research-status: partial, and **the gate is waived for this decision** — see § Owner rulings, ruling 2. NodaTime 3.3.3 surveyed to Stage-1 depth by reflection against the shipped assembly and its XML documentation, plus an original measurement of `Period.CreateComparer` over 170,000 pairs and the adversarial review's independent re-run over 407,972. Rust and PostgreSQL cited from primary documentation, accessed 2026-07-28. java.time and Joda-Time are asserted from general knowledge and are **not** verified in this pass. Python `dateutil`, Rust `chrono`, Go and ECMAScript Temporal are unsurveyed. `research/INDEX.md:48` registers that gap and calls a broader survey a prerequisite for any irreversible temporal-surface decision; the owner waived it here on the ground that those systems are not comparators for this decision, because Precept ships NodaTime's `Period` rather than choosing between temporal models. The gap itself stays open in `research/INDEX.md`.
sources-consulted:
  - docs/philosophy.md — the guarantee, the refusal, and who authors a precept
  - docs/language/precept-language-spec.md § 0.1 — the eleven design principles
  - docs/language/precept-language-spec.md § 0.8 — the authoring audience
  - docs/language/precept-language-spec.md:1159, :1449, :1692, :1693, :1696 — modifier desugaring, temporal comparison, period modifiers, min/max exclusions
  - docs/language/temporal-type-system.md:68, :244, :264 — contents, the NodaTime alignment directive, the approximation stance
  - docs/language/temporal-type-system.md:738, :801, :805, :811, :821, :844, :858, :866 — duration guidance, accessors, constraints, refusals
  - docs/language/temporal-type-system.md:955, :1328, :1331, :1340, :1342, :1345, :1346 — zoneddatetime refusals and period arithmetic receivers
  - docs/language/temporal-type-system.md:1177, :1192 — the date ± period context-resolution rows
  - docs/language/temporal-type-system.md:1480-:1485 — locked Decision 14
  - docs/language/temporal-type-system.md:1496, :1498 — locked Decision 16
  - docs/language/temporal-type-system.md:1624, :1713, :1736, :1745, :1749, :1776, :1818 — category closure, the deferred interval domain, collection rows, work lists
  - docs/language/business-domain-types.md:403, :405-:408 — what `of 'date'` and `of 'time'` admit
  - docs/language/business-domain-types.md:674, :1145-:1155, :1212-:1215, :1228 — quantity conversion, fixed-length tables, guard narrowing, `.basis`
  - docs/language/business-domain-types.md:1693, :1699, :1812, :1819, :1821, :1847, :1848, :1898 — D3, D14, the subset rule, single-basis cancellation, duration exemption, exchangerate
  - docs/language/collection-types.md:65, :147, :379, :386, :530-:533, :541-:545, :553, :586-:590, :1098 — orderability and its drifted copies
  - docs/compiler/type-checker.md:884, :886 — TemporalUnit subset discharge, the basis-path cancellation gap
  - docs/compiler/proof-engine.md:777 — TemporalUnit subset on the assignment path
  - docs/compiler/diagnostic-system.md:206, :420 — PRE0114's model wording, the three-way default-fold verdict
  - src/Precept/Language/Types.cs:186, :193, :420, :470, :489, :493-:503, :530-:534, :570, :608, :655, :673 — accessors, traits, the period type entry
  - src/Precept/Language/Type.cs:28, :39-:58 — TypeTrait, QualifierAxis
  - src/Precept/Language/Operations.cs:267, :276, :285, :294, :303, :320, :382, :386, :404, :408, :412, :654-:674, :738-:743, :866-:897, :1018-:1026, :1074-:1109, :1134-:1144 — period arithmetic, period equality, choice ordering, money/quantity/price comparisons
  - src/Precept/Language/OperationKind.cs:151-:152 — the two period equality operations
  - src/Precept/Language/Operation.cs:13-:22 — QualifierMatch
  - src/Precept/Language/ProofRequirement.cs:74-:91, :113-:118 — PeriodDimension, QualifierCompatibilityProofRequirement
  - src/Precept/Language/DeclaredQualifierMeta.cs:70-:93 — TemporalUnit and TemporalDimension
  - src/Precept/Language/Time/TemporalUnits.cs:21-:44 — the seven-unit entry table and its keying
  - src/Precept/Language/Modifiers.cs:16-:22 — nonnegative / positive / nonzero applicability
  - src/Precept/Pipeline/ProofEngine.Qualifiers.cs:118-:173, :262-:267, :497-:513, :526-:528, :585 — qualifier compatibility, the single-basis predicate, expression resolution, the do-not-extend comment
  - src/Precept/Pipeline/ProofEngine.QualifierNarrowing.cs:54-:77, :140-:143 — TemporalUnit subset, reassignment invalidation
  - src/Precept/Pipeline/ProofEngine.Composition.cs:224-:241, :601 — TryGetRelationalFact, relational sign
  - src/Precept/Pipeline/ProofEngine.Satisfiability.cs:250, :271 — the types NarrowByConstraint names, the NotEquals no-op
  - src/Precept/Pipeline/ProofEngine.Strategies.cs:107-:134, :950, :1057, :1067 — contextual literal inference, NegateOp and its ToDecimal gate
  - src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:1022-:1055, :1122-:1143, :1202-:1211, :1218-:1223, :1244-:1251 — the trait gate, accessor qualifiers, the propagation TODO
  - src/Precept/Pipeline/TypeChecker.Expressions.AssignmentQualifiers.cs:267, :743-:748 — the existing ElementQualifiers pattern, the second subset copy
  - src/Precept/Pipeline/TypeChecker.Expressions.cs:538 — DerivePeriodLiteralBasis
  - src/Precept/Pipeline/TypedExpressionMagnitude.cs:22-:28, :142-:155 — ProjectPeriodSign and its own disclaimer
  - src/Precept.Analyzers/Precept0010TraitOperationConsistency.cs:263, :300-:317, :376 — PRECEPT0010b/d and the literal name-match suppression
  - src/Precept.Analyzers/CatalogAnalysisHelpers.cs:415-:419 — the existing attribute-reading precedent
  - src/Precept/Precept.csproj:15 — the NodaTime version pin
  - NodaTime 3.3.3 assembly and NodaTime.xml — Period, Duration, CreateComparer, ToDuration, Normalize
  - Rust standard library, `std::cmp::PartialOrd` — conditional trait implementations; accessed 2026-07-28
  - PostgreSQL documentation § 8.5 Date/Time Types and § 9.9 Date/Time Functions — interval storage and the justify_* functions; accessed 2026-07-28
  - research/INDEX.md:48 — the registered comparator-survey gap
  - research/architecture/compiler/temporal-type-hierarchy-survey.md:96-:101, :309-:313, :669-:673, :701-:722 — six systems surveyed, period comparison omitted from all of them
  - research/language/expressiveness/temporal-type-strategy.md:8-:10, :17 — the synthesis and its own admission of scope
  - research/language/expressiveness/period-basis-separator-survey.md:52 — its stated exclusions
  - CLAUDE.md § Per-Decision Rationale, § The samples are examples not a specification, § Catalog System
  - docs/working-reset/phase-1/the-walk.md:482-:504 — the owner's mark on the ruling
  - docs/working-reset/compiler-readiness-plan.md:54-:59, :168-:172, :186-:190 — row N82 and the standard for a question put to the owner
  - docs/working-reset/phase-2/period-ordering.md — the prior analysis this design supersedes
  - docs/working-reset/phase-2/period-ordering-adversarial-review-2026-07-28.md — the independent adversarial read; § What the adversarial review found records what it changed here, what went to the owner, and what is still build work
  - docs/Working/bugs.md:457, :478-:481 — BUG-047 and its withdrawn sub-point
  - samples/equipment-lease-agreement.precept:28-:36, :77, :85 — the lease classification and its unwritten rule
  - samples/contractor-invoice-settlement.precept:30-:41, :80-:84 — worked time and the accessor comparison
  - samples/non-profit-membership-renewal.precept:32-:36 — the second unbounded grace period
---

# Ordering comparisons on `period`

## How to read this document

It is long because the question turned out to have three answers underneath it, and separating them is most of the work. If you read only part of it, read § Owner rulings, then § The short version, then § Ship now, then § What the adversarial review found, then § Open questions.

**§ Owner rulings comes first for a reason.** Everything after it was written before the owner ruled, and two of its sections — § The short version's "The recommendation, in two parts" and "The case against building it, at its strongest", and § Open questions items 2 and 5 — put a choice to him that he has since made. That working is kept rather than rewritten, because the argument on both sides is the useful part and a reader should see what was weighed. But it is no longer the document's recommendation, and reading it as though the question were still open would be reading the document backwards.

Two documents in this project both have a locked decision numbered 14 about `period`, and they say different things. Wherever one is meant, this document names the file. `temporal-type-system.md` Decision 14 is the ordering refusal. `business-domain-types.md` D14 is the assignment rule for the `in` qualifier. They are unrelated.

**This document's first draft recommended building the ordering operator, on the argument that closing the unsound workaround would leave authors with no way to compare two periods. Its red-team pass refuted that argument by compiling the counterexample, and the recommendation below is the corrected one.** The refutation is reproduced in full at § The claim that was wrong, because it is the most useful thing this pass produced and the owner should see the working, not just the conclusion.

---

## Owner rulings

Three rulings — two given 2026-07-29 and the lock given 2026-08-11 — recorded in the owner's words with the reasoning he gave. A ruling whose reason is written down wrongly gets re-litigated, so the reasoning below is his and is marked where it differs from what an agent recommended.

**The first two rulings did not lock this document; the third does.** § Open questions records the disposition of all five questions, and § Is this lockable states what the lock covers and what it leaves outstanding.

### Ruling 1 — build both halves

He was asked plainly whether he wanted ordered collections of pinned periods, with the two halves separated: the scalar comparison already compiles today as `A.days <= B.days` and computes the identical predicate, while `.min` on a `set of period in 'days'` is closed with no workaround an author can be asked to rely on. His answer: **"build both."**

That settles two of the open questions. Item 2 — whether the operator ships at all — is answered yes. Item 5 — scalar only, or scalar and collection — is answered both. The full path is authorised.

**What that pulls in.** The collection half is what forces Decision 2, because the piece it needs is keyed on expression shape rather than on type: `.min`/`.max` legality is a trait of the element type itself, so the flat `TypeTrait.Orderable` flag has to become a qualifier-aware condition on the type entry, with the blast radius across `period`, `money`, `quantity`, `price` and `choice` that Decision 2 records. Decision 4 rides on the same change. The "smaller answer" sketched in § Open questions item 2, and the scalar-only option in item 5, are both closed.

**The question he answered was the capability question, not the readability one.** § The claim that was wrong establishes that for two scalar fields the operator changes a spelling. The adversarial review's § 5 establishes that for a collection there is no other route at all — Precept has no element-wise projection, so the trait can never be satisfied from outside the element type, and the only workaround that compiles is a `queue of period in 'days' by integer` whose correctness invariant lives in the author's head. The review's own § 5 conclusion is that the collection half is not an add-on to the scalar operator but the thing that decides whether the operator matters, and that the wrong outcome is the middle — ordering pinned periods internally while withholding the spelling. "Build both" is the outcome that avoids the middle.

### Ruling 2 — the comparator-survey gate is waived, and the reason is not the one the review gave

`research/INDEX.md:48` records a known gap: *"`temporal-type-strategy.md` engages NodaTime heavily but lacks survey-grade comparator engagement with Joda-Time / java.time / Python datetime / Rust chrono. A broader survey is a Phase 11 prerequisite for any irreversible temporal-surface decision."*

He waived it. His words: **"waive it, we're built on noda."**

**His argument is not the review's, and the difference matters.** The adversarial review's § 7.3 recommended waiving on the ground that the direct measurement is stronger evidence for this specific rule than a comparator survey would be. That is a fine argument about evidential weight, and it is not the one that carries. The gate exists because *comparators* have not been surveyed — and for this decision they are not comparators. Precept does not choose between temporal models. It ships NodaTime's `Period` as the runtime representation of `period`, with the dependency pinned at `src/Precept/Precept.csproj:15`. What Joda-Time, java.time, Python `dateutil`, Rust `chrono`, Go or ECMAScript Temporal does with its own period type has no bearing on whether Precept can order the value it actually holds. The evidence that decides this rule is what NodaTime's `Period` does — and that is the one thing this pass surveyed to depth, by reflection against the shipped 3.3.3 assembly, plus the original measurement and the adversarial review's independent nine-calendar re-run.

**What the waiver does not do.** It is a waiver for this decision, on this type, on that reason. It does not close the gap recorded at `research/INDEX.md:48`, and it does not carry to a future temporal decision where Precept would be picking a model rather than exposing NodaTime's — there the comparators would be comparators, and the gate would apply on its own terms.

### Ruling 3 — the design is locked, 2026-08-11

He was told two things: that § Is this lockable had assessed the design lockable and that the lock had deliberately been left unset because it is his to set, and that the two defects § Open questions item 3 describes were still not filed anywhere. His reply, verbatim and complete: **"yes lock the design and log the bugs."**

**What that locks.** The eight decisions in § Decisions, and only those. They are listed at § Is this lockable § What the lock settles.

**What it does not lock, and this matters more than the lock does.** None of the mechanism is built. The three code items and the six § Ship now repairs are outstanding, and § Is this lockable § What the lock does not settle lists them. The sequencing obligation stands with them: § Ship now item 1 — the binding path where a declared qualifier does not constrain the value — lands before the operators, because Decision 1's correctness argument reads a declared qualifier and assumes it holds. A reader who takes this lock as saying the code is finished has read it wrong.

**The bugs.** Both filed the same day in [`docs/Working/bugs.md`](../../Working/bugs.md), and both reproduced afresh against HEAD at filing time rather than carried across from this document. **`BUG-068`** is the false proof — a mixed-sign `period` literal default reported **Proved** against `nonnegative`. **`BUG-069`** is the missing obligation — a sign modifier on a `period` raising nothing at all when the value comes from an expression, which is the more serious of the two because refusing mixed-sign literals would close the first and leave this one untouched. § Open questions item 3 carries the detail, including one thing the earlier text there got wrong.

### Where that leaves the five open questions

Item 1 is closed as well, but not by these rulings: the owner amended alignment directive item 4 in canon, and the amended text is live at `temporal-type-system.md:244`. Item 3 turned out to be a defect rather than a question. § Open questions carries all five with their dispositions, and § Is this lockable states what remains.

---

## The short version

**The question.** Whether `period` supports `<`, `<=`, `>`, `>=` and `.min`/`.max`, and if so by what mechanism.

**What NodaTime's reason actually is, since that is what the owner asked.** NodaTime withholds ordering from `Period` because `Period` is a single type covering every combination of components — years, months, weeks, days, hours, minutes, seconds and three sub-second components — so no one order fits all of it. Its own documentation says so: *"Certain periods can't naturally be compared without more context - how 'one month' compares to '30 days' depends on where you start."* It then supplies two ways out: `Period.CreateComparer(LocalDateTime)`, which adds both periods to a reference date, and `Period.ToDuration()`, which gives an exact total for any period free of years and months. `IComparable<Period>` is a contract on a whole type and cannot be made conditional on what a particular value contains, so withholding it is the only option C# leaves NodaTime.

**That reason is real, and it does not reach a field declared `period in 'days'`.** Such a field holds one integer count of one named unit. Comparing two of them is base-independent: measured over 600 base dates covering every month, every month-end, leap-year February and non-leap century February, across all seven units and counts spanning ±40 years to ±400, **the count order and the calendar order never disagreed in the ISO calendar, and no two distinct counts ever landed on the same date.** ISO is the scope of that claim, and it is the only calendar Precept exposes; § What was measured records the counterexample that forced the scope to be stated. So the owner's ruling is implementable and sound.

**For scalar comparisons the operator is a change of spelling, not a change of capability, and that is the finding that matters.** On two fields declared `period in 'days'`, this compiles today, with zero type errors:

```precept
rule GracePeriod.days <= MaximumGrace.days because "A grace period of {GracePeriod} exceeds the {MaximumGrace} maximum the membership committee approved"
```

while this is refused:

```precept
rule GracePeriod <= MaximumGrace because "A grace period of {GracePeriod} exceeds the {MaximumGrace} maximum"
```

with `PRE0018 "Expected a period value here, but got 'period'"` — a message that never mentions ordering. And the two rules compute the identical predicate, because the operator's own reduction is *compare the single component counts*. So an author with matching single-unit scalar fields is not blocked. They are given an incoherent error and left to find the accessor themselves.

**For collections it is not a change of spelling, because the accessor route cannot reach them at all.** `.min` and `.max` are gated on a trait of the *element type itself*. On `field Deadlines as set of period in 'days'`, the rule `Deadlines.min.days > 0` fails at `.min` with `PRE0104 "Accessor 'min' requires a trait the element type does not provide"` before `.days` is ever reached, and the inverted spelling `Deadlines.days.min` fails with `PRE0020 "'.days' is not available on set fields"` — because Precept has no element-wise projection, no construct that turns a `set of period in 'days'` into a set of integers, and the no-iteration commitment is why none exists. The trait can therefore never be satisfied from outside the element type. The one workaround that compiles is a `queue of period in 'days' by integer` where the author projects `.days` into the key at every `enqueue`; nothing checks that the key supplied is the value's count, so the invariant that makes `.peek` the minimum lives in the author's head. Decision 4 states the collection rule and § Open questions item 5 puts the scalar-only-or-both choice to the owner. **A reader who takes only "a change of spelling" from this summary has read the scalar half of the answer.**

**The recommendation, in two parts.**

**Ship now, without an owner ruling** — six items, every one a defect against locked canon or against the compiler's own stated intent, and every one needed whichever way the ordering question goes. Four of them are not about periods at all; they are holes in the qualifier system that hit `money in 'USD'` and `quantity in 'kg'` identically. § Ship now enumerates them, with the reproduction for each.

**Then treat the ordering operator as a readability decision for scalars, taken on its merits, with the real cost visible.** It is sound; it is the owner's own ruling; and what it buys on two scalar fields is that a domain expert writes `GracePeriod <= MaximumGrace` instead of `GracePeriod.days <= MaximumGrace.days`. That is a genuine gain under Principle 6 — the second form discards every fact about the operands at the accessor boundary and compares two bare integers. Whether it is worth an irreversible addition to the language, removal of a public catalog enum member, an analyzer rewritten off a flags enum onto a discriminated-union property, and 32 mechanical requirement edits, is a judgement this design does not get to make for him. § The mechanism specifies it in full so that the answer "build it" needs no further design. **For collections the weighing is not readability at all** — it is whether pinned-period collections can be ordered by any route, and § Open questions item 5 is where that is put.

**The case against building it, at its strongest.** Neither of the two concrete author needs anybody identified requires it, and both are scalar. The ASC 842 short-term-lease test is a date comparison and compiles today. The contractor's worked-time minimum should be a `duration` field, which already orders. The accessor route works for scalar comparisons and, once § Ship now item 2 lands, is sound — it does not reach collections, which is the counterweight above. The rule refuses `period in 'weeks'` against `period in 'days'`, which is exact in every calendar NodaTime ships. And the coherence property the rule is chosen to protect has **no consumer in the compiler** — the first draft claimed the proof engine's normal form depends on it, and that claim was checked and is false.

---

## The claim that was wrong

The first draft of this document said, in three places, that an author holding two `period in 'days'` fields would have no way to compare them once the unsound accessor cases were closed. That was the whole case for building the operator. It is false, and the compiler says so.

Run at HEAD:

```precept
precept MembershipRenewal

field GracePeriod as period in 'days' default '30 days' editable
field MaximumGrace as period in 'days' default '90 days'

rule GracePeriod.days <= MaximumGrace.days because "A grace period of {GracePeriod} exceeds the {MaximumGrace} maximum the membership committee approved"
```

→ `success: true`, `0 type errors`. The only diagnostic is an unrelated warning that `MaximumGrace` has no write site.

Replace the rule with `rule GracePeriod <= MaximumGrace` and the same file returns `PRE0018 "Expected a period value here, but got 'period'"`.

The design's own accessor-completeness rule (§ Ship now item 2) keeps the first form legal, and legal for a stated reason: on a field pinned to a single unit, the accessed component *is* the whole value, so nothing is dropped. So the design cannot both keep that rule and claim the author is blocked.

Two further first-draft claims fell with it, and are struck: that the worked example "cannot be written in any form" and that the ceiling "lives nowhere". Both are contradicted by the compile above.

**What survives.** The author is not blocked; they are badly served. They meet a refusal message that is internally incoherent and never names the problem, and the route that works is one they have to discover. That is a real defect and § Ship now item 6 fixes it. It is a much weaker argument for an irreversible language addition than the one the first draft made, and the owner should weigh the weaker one.

---

## What the adversarial review found

`period-ordering-adversarial-review-2026-07-28.md`, in this folder, is an independent adversarial read of this document written on the owner's request, with the stated posture that the design was stepping into a hazard NodaTime avoided on purpose. It did not trust this document's self-reported red-team pass: it re-ran the central compile claims against the live compiler and re-ran the core measurement with fresh code. Every compile claim in § Ship now and in § The claim that was wrong reproduced at HEAD. The measurement reproduced too, and then broke in one place. The review's own recommendation — build the operator, both halves — is the reviewer's opinion and never was a lock. **The lock came later and from the owner**, on 2026-08-11, and it is recorded in § Owner rulings, ruling 3, with his words. The review's agreement with the outcome is not what makes it settled.

**Applied to this document in this pass.**

1. **The soundness claim is re-scoped to the ISO calendar**, with the Badi counterexample and its cause recorded, and calendar-system support added to § Operational dimensions as a re-verification trigger. § What was measured.
2. **The `zoneddatetime ± period` refusal is cited as the precondition it is** for the claim that this design takes no position on whether a day is 24 hours, together with what would fail for the time units if that refusal ever moved. § What was measured, plus a second re-verification trigger in § Operational dimensions.
3. **§ The short version's "a change of spelling, not a change of capability" is scoped to scalar comparisons**, and the collection case — where the accessor route structurally cannot reach, because `.min`/`.max` legality is a trait of the element type and Precept has no element-wise projection — is stated in the summary rather than left to Decision 4 and § Open questions item 5.

**Routed to the owner — and since resolved by him, 2026-07-28 and 2026-07-29.**

4. **The amendment to alignment directive item 4** at `temporal-type-system.md:244`. The review drafted proposed wording, keyed on NodaTime's stated obstacle being inapplicable to the restricted case rather than on Precept wanting the feature, so that `zoneddatetime` stays refused for its own separate reason — two total orderings exist and canon declines to pick one. **The owner amended the directive, in his own shorter wording, and the amended text is live in canon**: *"4. When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type. One exception: a `period` pinned to a single unit orders against the same single unit — see Decision 14."* The exception names no exclusions because a named exception needs no fence — a composite is not pinned to a single unit, a mixed pair is not the same single unit, and `zoneddatetime` is not a `period`. Recorded with the rest of the reconciliation in [`period-ordering-reconciliation.md`](period-ordering-reconciliation.md) § 1. § Open questions item 1 is closed.
5. **The verdict on the comparator-survey gate.** The reviewer's view was to waive it, on the grounds that the direct measurement is stronger evidence for this specific rule than a survey would be, and that PostgreSQL — the one surveyed system that does order intervals — agrees with Precept's canon about why the components cannot be merged. **The owner waived it, on a different reason**: those systems are not comparators here, because Precept ships NodaTime's `Period` rather than choosing between temporal models. § Owner rulings, ruling 2, carries his reasoning; § Open questions item 4 is closed.

**Still open as build work.** The review lists six blockers before this design could lock, in dependency order. Two of them are the documentation corrections applied above and are done. The sixth is item 4 above and is done. **Three are code, and none of the three is done here** — each is an implementation obligation with an acceptance criterion, not an unanswered question; § Is this lockable works through why that distinction decides the lock.

- **Code.** § Ship now item 1 — the binding path where a declared qualifier does not constrain the value — must land first, because the whole correctness argument reads a declared qualifier and assumes it holds. Items 2, 3 and 4 of § Ship now travel with it. § Acceptance criteria 1–4.
- **Code.** The mixed-unit and composite refusals must be compile errors carrying the new messages (`PRE0165`, `PRE0166`), never a silently computed answer, with `PRE0018`'s incoherent message replaced in the same change. § Acceptance criteria 7, 9, 10.
- **Code.** Decision 6's type gate on the relational-fact path ships with the operators, not after. § Acceptance criterion 15.

**One review finding is not yet reflected above and is applied elsewhere in this document**: the review's § 8 corrects this design's Inventory and doc-update table for calling `QualifierRelation` and `OrderingCondition` "new catalogs". They are new metadata shapes inside, and serving, the existing `Types` and `ProofRequirement` catalogs. § Inventory and § Doc-update enumeration now say so.

**One correction the review made to its own reading**, kept here because it is why its recommendation reversed partway through: its first pass leaned on "the operator adds legibility, not capability" and preferred the smaller answer; probing the collection case refuted that premise for collections. It is the mirror of this document's own first-draft error. The first draft claimed a capability gap where there was only a spelling gap; the review's first pass claimed only a spelling gap where there is a real capability gap. Both were found by compiling the case rather than reasoning about it.

---

## What was measured

Three claims in this area had been repeated for months without being tested.

**Is a same-unit comparison independent of the reference date?** `Period.CreateComparer(base).Compare(N u, M u)` was evaluated for all seven units over 600 base dates — every month, every month-end, leap-year February, non-leap century February (2100), at midnight and 23:59:59 — for counts spanning ±40 years, ±60 months and ±400 of everything else, **including negative counts and zero**. Zero comparisons decreased as the count rose, and no two distinct counts ever mapped to the same date, in any unit. Directly: `Compare(N u, M u) == sign(N − M)` everywhere, with no mismatches.

**The reason, in the ISO calendar, does not depend on measurement.** `b + N months` lands inside calendar month `month(b) + N`; ISO calendar months are disjoint and run in time order; and month-end clamping moves the day *within* the target month, never out of it. So more months always lands later, whatever `b` is and however long those months happen to be. The same argument covers years.

**That argument is not calendar-independent, and this document's earlier drafts offered it as though it were.** An adversarial re-run of the measurement across nine calendar systems — Gregorian, Julian, Islamic-Civil, Hebrew in both civil and scriptural numbering, Persian, Um Al-Qura, Coptic and Badi — found **48 order flips in 407,972 comparisons, every one of them in the Badi (Bahá'í) calendar on the `months` unit.** From base Badi 178-19-1, `+ 18 months` gives Badi 179-18-1, which is Gregorian 2023-02-07; `+ 19 months` gives a corrupt `LocalDate` whose fields read year 0, month 32 — a value that throws the moment it is formatted or converted, and that `Period.CreateComparer` silently ranks *earlier* than the 18-month result. The cause is a NodaTime defect rather than a real disagreement between calendars: the Badi calendar's intercalary days (Ayyám-i-Há) break the sequential-months assumption NodaTime's arithmetic rests on, and the compromise its maintainers settled on — folding Ayyám-i-Há into month 18 — leaves month arithmetic across that boundary broken in the shipped 3.3.3 assembly. The counterexample and its provenance are recorded in `period-ordering-adversarial-review-2026-07-28.md` § 3.

**So the claim is scoped: the count order and the calendar order agree in the ISO calendar, which is the only calendar Precept exposes.** `CalendarSystem` appears nowhere in `src/Precept/`, no declaration form names a calendar, and `temporal-type-system.md:1324` gives `date` ordering as *"ISO calendar"* in canon's own voice. The same nine-calendar run found zero flips and zero ties in the other eight systems, so nothing here narrows the claim over any value a Precept definition can express. What it narrows is the *argument*: "calendar months are disjoint and run in time order" is a property the shipped library fails to uphold in one of its own calendars, so it cannot be stated as a universal and then relied on. § Operational dimensions carries calendar-system support as a re-verification trigger.

**The comparison never converts a month into anything, and it takes no position on whether a day is 24 hours — but the second half of that holds because of an existing canon refusal, not on its own.** `temporal-type-system.md:1195` and `:1342` make `zoneddatetime ± period` a type error — *"Periods can't be added directly to a zoned datetime — navigate to `.datetime` first"* — and `date` and `datetime` arithmetic is zone-free. So a period count can never be put where it has to answer an elapsed-hours question. If that refusal were ever relaxed the claim would fail for the time units: "2 hours" applied across a spring-forward transition is one elapsed hour, while "1 hour" applied across a fall-back transition is two, so same-unit count order would stop meaning "the shorter elapsed span". That refusal is a precondition of this design, not a neighbouring fact, and § Operational dimensions carries it as the second re-verification trigger.

A separate sweep covered every pair drawn from `{years, months}` together — what a composite `period in 'years + months'` admits. There, **432 of 141,376 pairs flip their order with the reference date.** `P1Y1M` against `P13M` is equal from base 2019-01-01 and less-than from base 2020-02-29, because the first truncates twice and the second once. Composite calendar bases are genuinely base-dependent, and the rule refuses them for that reason rather than by analogy.

**Does ordering agree with equality under the rule?** Every pair of single-unit periods across all seven units and counts −50…50 was checked. Zero violations, for a structural reason: a field declared `period in 'days'` admits values whose only non-zero component is days, so equal counts means an equal value. All eleven zero constructions in NodaTime — `Period.Zero`, `FromDays(0)`, `FromHours(0)`, `FromMonths(0)` and the rest — are mutually equal and share a hash code, so the zero case does not break it.

**One limit on that property, found by the red-team pass and not by the measurement.** NodaTime's `Period` has **ten** components; `TemporalUnits.cs:21-:44` declares **seven**. `Period.FromMilliseconds(500)` has every one of the seven at zero and is not equal to `Period.Zero`. The assignment rule is a subset test, and the empty set is a subset of every declared basis — so a value carrying only sub-second components would satisfy any basis and project to a count of zero in every unit Precept can see. Two such values would then compare equal and be structurally unequal. This is **not reachable today**: `field B as period default '500 milliseconds'` is refused with `PRE0053`, and `Period.Between` appears nowhere in `src/`. But it becomes reachable the moment period-producing operations are evaluated, because `Period.Between(9:00:00, 9:00:00.500)` returns exactly such a value. **The coherence property holds over the value space Precept can currently express, and the design must say so in those terms rather than as an unqualified claim.**

**What NodaTime actually does.** Verified by reflection against the shipped 3.3.3 assembly. `Period` implements `IEquatable<Period>` and arithmetic operators; no `IComparable`, no `IComparable<Period>`, no `IComparisonOperators`, no `CompareTo`, no `op_LessThan`. `Duration` implements all of them. `Period.ToDuration()` succeeds on any period free of years and months — so `business-domain-types.md:1847`'s claim that totalling a multi-unit period *"would require Precept-invented arithmetic that NodaTime deliberately refuses"* is true of years and months and false of everything else. `Period.Normalize()` folds weeks into days at 7:1 and cascades time components upward but leaves years and months alone, so it is not a canonicalization anything can be ordered by. There is no 12:1 years-to-months conversion anywhere.

---

## Ship now

Six items. None needs an owner ruling; each is a defect against locked canon or against the compiler's own stated intent; each was reproduced at HEAD. **Four of the six are not about periods.** They are holes in the qualifier system that hit `money` and `quantity` identically, and they would need fixing if `period` were removed from the language tomorrow.

**1 — A declared qualifier does not constrain the value on the binding path.** This compiles clean, with zero errors and **no qualifier obligation raised at all**:

```precept
field Q as queue of period
field X as period in 'days' optional

from Ready on Take when Q.count > 0
    -> dequeue Q into X
```

An arbitrary period reaches a field declared `period in 'days'`. The same shape reproduces on `money`: a `queue of money` dequeued into a `money in 'USD'` field raises no currency obligation. Direct assignment *is* checked — setting an unqualified period into a pinned field raises `PRE0141` — so this is one path, not a general absence. The reassignment-invalidation check that keeps a `when X.basis == 'days'` narrowing fact from going stale has the same blind spot: it reads the assignment action's field name and never sees a binding destination.

**Every design in this area reads a declared qualifier and assumes it holds.** So does `business-domain-types.md` D14. This is the first thing to fix.

**2 — The component-accessor comparison is unsound and gets no diagnostic.** `LeaseTerm.months <= 12` where `LeaseTerm` is `period in 'years + months'` compiles clean today and is wrong: `'1 year'` has `.months == 0`, so a twelve-month test passes a one-year lease. One shipped sample takes this route — `contractor-invoice-settlement.precept:84` writes `on Issue ensure WorkedTime.hours > 0` on a `period of 'time'` field, which legally holds `'90 minutes'`, whose `.hours` is 0, so it refuses to bill ninety minutes of work.

The fix: a period component accessor is admissible **in an ordering comparison** only when the accessed component is the whole value — that is, when the receiving expression is pinned to that single unit. `Grace.days < Limit.days` on two `period in 'days'` fields stays legal, because nothing is dropped. Component *equality* (`A.months == 0`) stays legal; it is a determinate structural fact canon already blesses at `temporal-type-system.md:821`. The asymmetry is deliberate and should be documented: `LeaseTerm.months >= 1` would be refused while `LeaseTerm.months == 1` passes, and the second is misleading in the same way the first was refused for. Note also that the check must reach through arithmetic — `LeaseTerm.months + 0 <= 12` compiles today and would bypass a rule phrased only about a member access appearing as an operand.

**3 — `.min` and `.max` drop the element's qualifier.** `rule PricedAmounts.min < PricedAmounts.max` on `field PricedAmounts as set of money in 'USD'` is refused with *"(Currency: unresolved)"* on a fully pinned collection; the same shape on `set of quantity in 'kg'` gives *"(Unit: unresolved)"*. Meanwhile `.min` on an **unqualified** `set of money` is accepted with no diagnostic at all, which `collection-types.md:541` says is a type error. Wrong in both directions, and nothing to do with periods.

The value is computed correctly and thrown away at one place: `ProofEngine.Qualifiers.cs:512` asks the *field* for its qualifier when the expression is a member access, and a `set` has none — the `in 'USD'` lives on the element type. `TypedMemberAccess.ElementQualifiers` already exists and is already populated. The pattern that reads it already ships at `TypeChecker.Expressions.AssignmentQualifiers.cs:267`.

**4 — `of 'date'` and `of 'time'` are not enforced.** `field Bad as period of 'time' default '3 years'` compiles with no error and no obligation, while the `in` form correctly emits `PRE0068`. The `TemporalDimension` axis has no assignment enforcement at all. This is a bug against `business-domain-types.md:405-:408` and `temporal-type-system.md:858`, and it means `period of 'time'` currently guarantees nothing.

**5 — `date + '<n> months'` is rejected with a message telling the author to write what they wrote.** `field B as date <- A + '12 months'` returns `PRE0056 "Dates must be written as YYYY-MM-DD. Use '12 months'"`. It reproduces in a computed field with no comparison anywhere, and for `'30 days'` as well as months, so the fault is in how the expected type is taken, not in comparison context. `temporal-type-system.md:1177` and `:1192` give the type rule for `date ± period`. **This breaks the literal spelling of Decision 14's own prescribed workaround** — *"Compare concrete dates instead: `StartDate + GracePeriod < Deadline`"* — and every route this design points authors down goes through date arithmetic. Only the period-*field* spelling works.

**6 — `PRE0018`'s message on a period comparison.** *"Expected a period value here, but got 'period'"* is what an author gets today for the exact thing under discussion. It is internally incoherent and never mentions ordering. Whatever the ordering answer is, this must say what is wrong and what to write instead.

**And one that needs a ruling but not this design.** `field J as period nonnegative default '1 months + -30 days'` compiles today with the obligation reported **Proved**. That value moves forward from 2021-01-01 and backward from 2021-02-01 — Decision 14's own argument arriving in the sign question. Refusing mixed-sign literals does not close it, because `period − period` produces them (`'1 month' - '30 days'` is `P1M-30D`) and `field D as period nonnegative <- A - B` raises **no obligation at all**. § Open questions item 3. **Both are now filed** — `BUG-068` and `BUG-069` in `docs/Working/bugs.md`, 2026-08-11.

---

## Philosophy Alignment

Every principle from `precept-language-spec.md § 0.1` has a row. The rows describe the ordering operator, which is the part of this design that needs weighing; § Ship now is bug-fix work and is not evaluated here.

| Principle | Affected? | How served | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | A period comparison the compiler accepts has an answer that cannot vary with anything; every other one is refused at the comparison. | The rule's correctness rests on a declared qualifier constraining the value, and § Ship now item 1 shows one path where it does not. | Ordering cannot ship before that closes. Building it first would make the prevention claim false on arrival. |
| 2. One file, complete rules | Y | A period ceiling on an `editable` field can be stated in the same file as the field. | The ceiling can already be stated, via the component accessor — see § The claim that was wrong. What the operator adds is the spelling, not the possibility. | The gain is legibility, and the row should not be read as more than that. |
| 3. Deterministic semantics | Y | Same definition and data give the same verdict, with no reference date and no conversion. Ordering agrees with equality over the values Precept can express, so `.min` never has two candidates. | The agreement holds over the seven catalogued units. NodaTime's `Period` has ten components, and a value carrying only sub-second ones would tie without being equal. | Not reachable today. It becomes reachable if period-producing operations are evaluated, and the design must state the property in those terms rather than absolutely. |
| 4. Full inspectability | Y | The refusal names which operand is not pinned and what its declared basis is. | N/A | N/A |
| 5. Keyword-anchored readability | Y | No new keyword, construct or operator spelling. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | Y | **The principle that most favours building it.** The alternative that works today compares two bare integers, with every fact about the operands discarded at the accessor boundary. Ordering keeps the domain type in the expression. | For the *proof engine* the accessor form is better, not worse: integers are the domain its interval reasoning is built for, and Decision 6's type gate permanently excludes the operator form from relational reasoning. | The operator is more readable and less analysable. That is the trade, and it is real. |
| 7. Compile-time-first static checking | Y | Whether both operands are pinned to the same single unit is decided from declaration metadata at check time. | N/A | N/A |
| 8. Approximation honesty | Y | Nothing is approximated. Comparing two counts of one unit applies no conversion factor and assumes no unit's length. And the measurement establishes more than that: for a single unit the count order *is* the ISO calendar order at every base, so `Grace < Limit` genuinely means "Grace is a shorter span". | A `period in 'months'` value may itself have been truncated when produced — `business-domain-types.md:1699` accepts that. Ordering does not create the truncation but makes it visible in a new place. | The documentation should say the operator compares spans, because that is what was proved, and separately note the truncation that the *value* already carries. The first draft's instruction to say "count, not length" understated what the measurement showed. |
| 9. Mandatory rationale (`because`) | N | N/A — no new construct carries a rationale clause; comparisons live inside `rule` and `ensure`, which already require one. | N/A | N/A |
| 10. Totality | Y | An integer comparison of two counts. No conversion, no multiplication, no accumulation, so no overflow and no undefined case. | N/A | N/A |
| 11. Static completeness | Y | No new evaluator fault path. | A period ordering operation registered in the catalog would be picked up by the proof engine's relational-fact path, which has no type gate, and fed into interval arithmetic built on a projection its own source disclaims for this use. | The type gate (Decision 6) ships in the same change and is priced into the build. |

**Companion commitments.** *Stateless-first-class* is unaffected — every example here works in a precept with no states. *Domain-expert-primary-author* is the commitment the operator serves: the comparison a domain expert wants to write about two lease terms is `LeaseTerm <= MaxTerm`. It is also the commitment § Ship now item 6 serves most directly, since the message an author meets today teaches nothing.

---

## Language Design Grounding

### General language design

**What the field does with a type that has more than one defensible order.** Three established answers. Withhold the order and make the caller name a reduction — NodaTime's `ZonedDateTime`, offering `.ToInstant()` and `.LocalDateTime`. Normalize to a canonical form and order that — what SI-based measurement libraries do, and what Precept already does for `quantity of 'length'`. Or **refine the type so only the orderable subset is expressible, and put the order on the refinement.**

The third is what this design proposes, and it has a direct precedent in Rust's conditional trait implementations. Rust cannot put `PartialOrd` on `Vec<T>` unconditionally, because whether a vector orders depends on what it contains. Its answer is an implementation available only under a bound, checked at each use site. From the `std::cmp::PartialOrd` documentation (accessed 2026-07-28), verbatim:

```rust
impl<T, A1, A2> PartialOrd<Vec<T, A2>> for Vec<T, A1>
where
    T: PartialOrd,
    A1: Allocator,
    A2: Allocator,
```

```rust
impl<T> PartialOrd for [T]
where
    T: PartialOrd,
```

```rust
impl<T> PartialOrd for Option<T>
where
    T: PartialOrd,
```

The divergence is that Rust's condition ranges over *type parameters* while Precept's would range over a *qualifier* — a declaration-level annotation the checker tracks, not a type parameter. That difference is what lets `quantity of 'length'` order with conversion while `money in 'USD'` orders only against another `'USD'`, and it is also the weakness in this design's reading of the alignment directive (§ Open questions item 1), because `period in 'days'` is not a distinct type in Precept's type system.

**PostgreSQL is the direct counterexample to "no system orders calendar intervals", and what it actually does is more interesting than the first draft claimed.** `interval` is orderable — the documentation says *"the usual comparison operators shown in Table 9.1 are available for the date/time types"* and that *"intervals can only be compared to other values of the same data type"*. But PostgreSQL's account of *why* the components are separate is the same account Precept's canon gives (accessed 2026-07-28, verbatim):

> "Internally, `interval` values are stored as three integral fields: months, days, and microseconds. These fields are kept separate because the number of days in a month varies, while a day can have 23 or 25 hours if a daylight savings time transition is involved."

And it exposes the fixed conversions as **explicit functions the author calls**, not as a property of the type: `justify_days` is documented as *"Adjust interval, converting 30-day time periods to months"* and `justify_hours` as *"Adjust interval, converting 24-hour time periods to days"*.

**What the comparison operator itself does with a months-versus-days mismatch is not stated in the documentation reachable in this pass, and this design does not assert it.** The first draft claimed PostgreSQL compares by a fixed 30-day convention; that claim is withdrawn as uncited. What is established is that PostgreSQL orders intervals, agrees with Precept's canon about why the components cannot be merged, and makes the author name the conversion when one is wanted.

**Other systems.** java.time's `Period` implements `TemporalAmount` and not `Comparable`, while `Duration` implements `Comparable<Duration>`; Joda-Time is the same. Both are **asserted from general knowledge and not verified in this pass**, and in any case share a designer with NodaTime, so they would not be independent confirmation. ISO 8601 durations, Python `dateutil.relativedelta`, Rust `chrono`, Go and the ECMAScript Temporal proposal are unsurveyed anywhere in this repository.

**The gap, stated plainly.** `research/INDEX.md:48` records that a broader temporal comparator survey is *"a Phase 11 prerequisite for any irreversible temporal-surface decision."* The ordering operator is irreversible by the skill's own classification. The gap is not closed. The measurement in § What was measured is the first direct evidence this project has produced, and it is still one library.

### Precept-specific application

- **`precept-language-spec.md:1449`** states in the spec's own voice that `period` supports only `==` and `!=`. It was already not literally true: `temporal-type-system.md:844` grants `nonnegative` on `period` as *"value ≥ zero period"* and *"compared against `Period.Zero`"*, and `precept-language-spec.md:1159` says modifiers desugar to the equivalent rule.
- **`temporal-type-system.md` Decision 14** is what changes. Both its rejected alternatives — reference-date ordering and approximate ordering — survive and are still rejected. This rule is neither.
- **`temporal-type-system.md:244` item 4** is the one place this design cannot proceed on its own authority. § Open questions item 1.
- **`business-domain-types.md:1847`** already draws the one-declared-unit line on this type for a different operation.
- **`collection-types.md:553`** already says qualifiers on `money`, `quantity` and `price` *"restrict elements to a homogeneous denomination and unlock ordering."*

---

## Audience and Teachability

### Worked example

A membership renewal. Note honestly that `samples/non-profit-membership-renewal.precept:36` declares `field GracePeriod as period default '30 days' editable` — **unqualified** — so this example changes that declaration to get the comparison. That is part of the feature's cost, not a neutral illustration.

```precept
precept MembershipRenewal

# Calendar — host populates these from the chosen level and frequency
field EnrolledOn as date optional
field TermStartDate as date optional
field TermEndDate as date optional
field GracePeriod as period in 'days' default '30 days' editable
field MaximumGrace as period in 'days' default '90 days'

# Rules
rule GracePeriod <= MaximumGrace because "A grace period of {GracePeriod} exceeds the {MaximumGrace} maximum the membership committee approved"
```

Today the rule is refused. The same rule written as `GracePeriod.days <= MaximumGrace.days` compiles, computes the same predicate, and is the form an author must currently discover for themselves.

### Error message

The misuse a domain expert will commit is declaring the field the natural way and then trying to bound it — `field GracePeriod as period default '30 days' editable` followed by the rule above.

> **PRE0165** — 'GracePeriod' does not say what it counts in, so it might hold months or years, and how long those are depends on when they start. Comparing it with 'MaximumGrace' is refused. Say what 'GracePeriod' counts in — for example `field GracePeriod as period in 'days'` — and the comparison is allowed.

It names the field, says in ordinary words why the answer is not available, and gives the declaration to write. It uses no compiler vocabulary. **One caveat the message text must handle:** the unpinned operand may be a field, an event parameter, a collection element, or `.min` of one, and "change the declaration" points somewhere different in each case. One template with four fix locations, not one sentence.

Compare what an author gets today for the same shape: `PRE0018 "Expected a period value here, but got 'period'"`.

### 10-minute teaching path

1. `docs/language/temporal-type-system.md` § `period` — what a period is, and the seven units. Three minutes.
2. The same file, Decision 14 as rewritten — when two periods compare and when they do not. Two minutes.
3. `samples/non-profit-membership-renewal.precept` — the example above in a real file. Two minutes.
4. `docs/language/business-domain-types.md` § D14 — what `in 'days'` means for the values a field accepts. Three minutes.

An author who never reads step 4 still writes correct definitions; the compiler tells them when the declaration is not specific enough, and the message says what to add.

---

## Semantic Rules

### Typing

Ordering on `period` is admitted when both operands resolve, on the temporal-unit axis, to a basis naming exactly one component, and the component is the same on both sides.

```
  Γ ⊢ a : period{basis = U}     Γ ⊢ b : period{basis = V}
  |U| = 1     |V| = 1     U = V
  ─────────────────────────────────────────────────────────
              Γ ⊢ a ⊗ b : boolean         ⊗ ∈ { <, <=, >, >= }
```

`basis` is `DeclaredQualifierMeta.TemporalUnit.Components`, resolved from: a field declaration; an event parameter declaration (verified reachable — `event Add(P as period in 'days')` declares cleanly and `Add.P` carries the qualifier); a period literal's non-zero components; a branch-scoped narrowing fact from `when X.basis == '<literal>'`; or **a period-producing operation's declared result basis**, which does not exist today and which § Inventory adds. Where no basis resolves, the premise fails. `period of 'date'` and `period of 'time'` carry a `TemporalDimension` qualifier on a different axis and never satisfy the premise: a category names which components are *allowed*, not that there is exactly one.

The result basis of the period-producing operations is stated here rather than left to § Inventory, because it decides whether `field Gap as period <- B - A` is ever orderable:

| Operation | Result basis |
|---|---|
| `period + period`, `period − period` | the union of the operands' component sets — so single-unit plus the same single unit stays single-unit and remains orderable; anything else does not |
| `date − date`, `datetime − datetime` | none. `Period.Between` with default units produces components from years down, so no single unit is guaranteed. Refused. |
| `time − time` | the time category, `of 'time'` — not a single unit. Refused. |

The collection form is the same premise applied to the element type against itself:

```
  Γ ⊢ c : set of period{basis = U}      |U| = 1
  ──────────────────────────────────────────────
        Γ ⊢ c.min : period{basis = U}
```

`.min` and `.max` return the element as stored, basis intact.

### Evaluation

```
E[a ⊗ b]  →  E[count_U(a) ⊗ count_U(b)]     where basis(a) = basis(b) = {U}
```

`count_U` is the value's single non-zero component, read directly from the NodaTime `Period`. No conversion, no multiplication, no accumulation.

**Note what this makes explicit.** The reduction is `a.U ⊗ b.U`, which is exactly what the component accessor already computes. That is why the operator is a change of spelling — see § The claim that was wrong.

### Proof obligations

Each of the four ordering operations carries one `QualifierCompatibilityProofRequirement` on `QualifierAxis.TemporalUnit`, naming a new relation, **same single component**. Distinct from the two that exist on this axis:

| Position | Relation | Where it lives today |
|---|---|---|
| Assignment into a pinned field | the value's non-zero components are a subset of the declared basis | `ProofEngine.QualifierNarrowing.cs:54-:77` and `TypeChecker.Expressions.AssignmentQualifiers.cs:743-:748` — two copies |
| Binary operation, any axis | whole-record equality plus field-identity | `ProofEngine.Qualifiers.cs:118-:173` — which has **no** `TemporalUnit` branch at all |
| Comparison (new) | both bases name exactly one component and it is the same one | does not exist |

The middle row is why attaching the existing requirement type would be wrong: it would discharge by comparing whole basis records, admitting `'years + months'` against `'years + months'` — the case measured at 432 order flips in 141,376 pairs.

### Soundness preservation

- **Principle 7.** The premise is decided from declaration metadata at type-check time.
- **Principle 10.** The reduction is an integer comparison with no partial case — strictly stronger than comparing totals, which must multiply and can overflow.
- **Principle 11.** No new evaluator fault path, **conditional on Decision 6's type gate** and on § Ship now item 1.
- **Principle 3.** Established by measurement, over the value space Precept can currently express; see the sub-second limit in § What was measured.

---

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The condition under which a type's ordering operations are available belongs in catalog metadata on the type entry. It is exactly what `CLAUDE.md § Catalog System` says the catalog must carry: the language decides it, several stages consume it, and the current arrangement — a flat per-`TypeKind` flag plus a hardcoded exception for ordered choice, duplicated across two methods — is the parallel-copy pattern the rule exists to prevent.

The evidence that the current arrangement has already failed is in the canonical documents. `collection-types.md:147` defines "orderable" by listing types. `:386` and `:1098` restate it more narrowly for `by P` keys and have drifted so far that they forbid `log of string by instant`, which the same file shows as a worked example at `:65` and `:379`. And `:147` lists `exchangerate` as orderable while `business-domain-types.md:1898` makes exchangerate ordering a compile error — with the catalog agreeing against `:147` (`Types.cs:673` gives it `EqualityComparable` only). **Three copies, two wrong, one of the disagreements inside a single file.**

**Cross-component propagation.**

- **Runtime (parser, type checker, evaluator, diagnostics):** the type checker's `.min`/`.max` gate reads the condition instead of the flat flag; the proof engine gains the new relation and the relational-fact type gate; four operations and three diagnostics are added; two messages are replaced. The evaluator gains an integer comparison.
- **Tooling:** syntax highlighting — **None**, no new token. Hover and completions consume orderability **nowhere** today; searching the tooling projects returns no hits. So this is new consumption rather than change.
- **MCP:** `CatalogFormatters.cs` prints type traits generically, so the change is visible in `precept_types` output; it should print the condition in author words.

**Breaking changes.** `TypeTrait.Orderable` is removed from the public catalog enum. Three diagnostic codes added; `PRE0018` and `PRE0104` message text changes. No `.precept` file stops compiling from the ordering change; § Ship now item 2 does newly refuse one sample line.

### External architectural precedent

Rust's conditional trait implementation, quoted verbatim in § Language Design Grounding. What Precept takes: the shape — availability declared where the type is declared, checked where it is used, rather than a flat per-type flag. What Precept diverges on: the condition ranges over a qualifier rather than a type parameter, so it must name both an axis and a relation where Rust names only a bound.

The comparator confirms the *placement*, not the mechanism, and it is honest to say the mechanism is not something Rust needs: Rust's type system makes `Vec<i32>` and `Vec<String>` genuinely different types, and Precept's does not make `period in 'days'` and `period in 'months'` different types. That gap is the substance of § Open questions item 1.

---

## Inventory of what will be built

### Catalog

**No new catalog is created.** Every item below is an entry, a member or a metadata shape inside a catalog that already exists — `Types`, `Operations`, `OperationKind`, `ProofRequirement`. An earlier draft of this section called `QualifierRelation` and `OrderingCondition` "new catalogs"; the adversarial review's § 8 corrected that, and it is corrected here. `QualifierRelation` is a discriminated union used as metadata *by* the `Types` and `ProofRequirement` catalogs, the same way `PeriodDimension` (`ProofRequirement.cs:74`) already serves `DimensionProofRequirement` without being a catalog itself. `OrderingCondition` is a new property on every entry of the existing `Types` catalog, replacing a flags-enum member with a richer shape. Both are required by the collection half — Decisions 2 and 4 — which the owner's ruling puts in scope; Decision 1 alone would need neither.

- `src/Precept/Language/QualifierRelation.cs` — a new file holding shared metadata vocabulary, not a new catalog. A discriminated union base plus four sealed subtypes: `ExactValue` (currency, timezone), `SameDimensionConvertible` (quantity), `ComponentSubset` (the period assignment rule), `SameSingleComponent` (the period comparison rule). Each carries a `SubjectNoun` and an `ExampleDeclaration` in author words, so diagnostics and MCP output read off the catalog. Whether it sits in its own file or inside `Type.cs` is a file-layout choice.
- `src/Precept/Language/Type.cs` — new `OrderingCondition` discriminated union, the shape the `Types` catalog's new per-entry property takes: `AlwaysOrdered`, `NeverOrdered`, `OrderedWhenModified(ModifierKind)`, `OrderedWhenQualified(QualifierAxis, QualifierRelation)`. `TypeTrait.Orderable` removed.
- `src/Precept/Language/Types.cs` — `OrderingCondition` on every type entry. `period` → `OrderedWhenQualified(TemporalUnit, SameSingleComponent)`; `money`/`price` → `OrderedWhenQualified(Currency, ExactValue)` (price also on `Unit`); `quantity` → `OrderedWhenQualified(Unit, SameDimensionConvertible)`; `choice` → `OrderedWhenModified(Ordered)`; numerics and `date`/`time`/`instant`/`datetime`/`duration` → `AlwaysOrdered`; the rest `NeverOrdered`. `exchangerate` → `NeverOrdered`, settling the contradiction at `collection-types.md:147` against that line. The `.min` and `.max` accessor declarations at `:186` and `:193` swap `RequiredTraits: TypeTrait.Orderable` for a `RequiresOrdering` flag — and these are the **only** non-`None` uses of `RequiredTraits` in the whole catalog.
- `src/Precept/Language/ProofRequirement.cs` — `QualifierCompatibilityProofRequirement` gains a `Relation` member. **32 construction sites** in `src/`, all in `Operations.cs`, get their relation written out; none changes behaviour.
- `src/Precept/Language/OperationKind.cs` and `Operations.cs` — four new members and four new entries.
- `src/Precept/Language/Operations.cs` — declared result bases per the table in § Semantic Rules, on `:285` (`date − date`), `:320` (`time − time`), `:412` (`datetime − datetime`), `:382`/`:386` (`period ± period`).

### Pipeline

- `ProofEngine.Qualifiers.cs` — `QualifiersAreCompatible` becomes a dispatch on the relation subtype, removing two existing axis-identity dispatches (`if (axis == QualifierAxis.TemporalDimension)` at `:126`, `if (axis == QualifierAxis.Unit || axis == QualifierAxis.Dimension)` at `:141`), which are the pattern `CLAUDE.md § Catalog System` forbids. `QualifiersSymbolicallyEqual` survives as a fallback under every relation.
- `ProofEngine.Qualifiers.cs:509-:513` — one case inserted ahead of the field fallback, reading `TypedMemberAccess.ElementQualifiers`. This is § Ship now item 3.
- `ProofEngine.QualifierNarrowing.cs:54-:77` and `TypeChecker.Expressions.AssignmentQualifiers.cs:743-:748` — the two duplicated subset implementations repointed at the single relation evaluator and deleted.
- `ProofEngine.Composition.cs:224-:241` — `TryGetRelationalFact` gains a type gate.
- `TypeChecker.Expressions.Callables.cs:1022-:1046` and `:1122-:1143` — the two duplicated trait blocks become one shared helper reading the ordering condition and the element's declared qualifier; the ordered-choice case folds into it.
- `ProofEngine.Strategies.cs` — contextual basis inference for a period literal that derives no basis, sited beside the existing choice-literal inference at `:107-:134`.
- The component-accessor completeness check (§ Ship now item 2).

### Analyzer

- `Precept0010TraitOperationConsistency.cs` — reads the ordering condition. The suppression's literal string comparison at `:376` (`reqCreation.Type?.Name == "ModifierRequirement"`) is replaced by an attribute on the requirement record types; the precedent is in the same project at `CatalogAnalysisHelpers.cs:415-:419`.

### Diagnostics

`PRE0165 PeriodComparisonNeedsPinnedUnit` (Proof, Error), `PRE0166 PeriodComparisonUnitMismatch` (Proof, Error), `PRE0167 PeriodComponentComparisonIncomplete` (Type, Error); replacements for `PRE0018`'s period path and `PRE0104`'s period element type; a `.precept` scenario file in `test/integrationtests/diagnostics/` for each new code.

### Tooling

`tools/Precept.Mcp/CatalogFormatters.cs` prints the condition; `tools/Precept.MatrixTools/FaultAxisEnumerator.cs:254` follows the rename; `tools/Precept.LanguageServer/` begins consuming the condition in hover and completion.

---

## Decisions

### Decision 1: Two periods order when both are known to be counted in one and the same single calendar unit

**Stakes**: irreversible. **Both conditions are now met.** This decision was written conditional on § Open questions items 1 and 4 — the alignment directive being settled and the research gate being satisfied or waived. The owner amended directive item 4 in canon (`temporal-type-system.md:244`), and he waived the research gate on 2026-07-29 for the reason recorded in § Owner rulings, ruling 2. The legs below were already complete so that "build it" needs no further design; nothing outside them is now holding the decision.

- **Rationale**: it is the owner's ruling, and it is sound over what Precept can express. The measurement establishes that a single-unit comparison agrees with the ISO calendar order at every base date, in every unit, including negatives — ISO being the only calendar Precept exposes, and the scope the claim carries for the reason given in § What was measured. It is the only rule considered under which ordering agrees with equality, which is what keeps `.min` from having two candidates. It needs no position on whether a day is 24 hours, because it converts nothing and because canon already refuses `zoneddatetime ± period` (§ What was measured records that refusal as a precondition rather than a neighbouring fact). And it has no overflow case, because it compares two component counts rather than accumulating a total.
- **Tradeoff accepted**: three, and the second is the one the first draft missed.
  1. It refuses comparisons that are sound. `period in 'hours + minutes'` against the same has an exact total by `Period.ToDuration()`, and this rule refuses it.
  2. **It refuses `period in 'weeks'` against `period in 'days'`, and that comparison is exact.** `Period.Normalize()` performs the 7:1 fold unconditionally with no calendar argument, because a week is seven days in every calendar system NodaTime ships. This is the sharper version of the first tradeoff and the first draft did not name it.
  3. It refuses both concrete author needs as those files are written today, and it changes a spelling rather than adding a capability for authors who pin their fields.
- **Alternatives considered**:
  - *Order any period provably free of years and months, comparing totals* — NodaTime's own line, the widest sound rule, and the one that admits both tradeoffs 1 and 2. Rejected because ordering would then disagree with equality: `'1 day' <= '24 hours'` and `'1 day' >= '24 hours'` are both true while `'1 day' == '24 hours'` is false, and `.min` over a collection holding both becomes non-deterministic. **The honest weight of that objection is smaller than the first draft claimed** — see Counter-evidence.
  - *Refuse ordering entirely and keep Decision 14* — a legitimate outcome, and § Open questions item 2 keeps it live. It costs nothing now that § Ship now item 2 keeps the accessor route open and sound.
  - *Give period a `.duration` accessor and order that* — canon's own move for `zoneddatetime`. Rejected because the accessor must produce a `Duration`, which is 64-bit nanoseconds by definition, carrying an overflow obligation that is undischargeable for any non-constant period: `min`/`max` field constraints exclude every temporal type (`precept-language-spec.md:1696`), and the machinery that would change that is deferred at `temporal-type-system.md:1713`.
  - *Reference-date ordering* — already rejected at `temporal-type-system.md:1483`, and its shape already exists as `SomeDate + SomePeriod` compared as dates.
- **Precedent**: `business-domain-types.md:1847` already draws the one-declared-unit line on this exact type for a different operation. `collection-types.md:553` already establishes qualifier-conditional ordering for three other types. Rust's conditional trait implementation, quoted verbatim in § Language Design Grounding, is the external structural precedent.
- **Sources consulted for this decision**:
  - `docs/language/temporal-type-system.md:1480-:1485` — *"### 14. No ordering on `period` — `==` and `!=` only… **Alternatives rejected:** Reference-date ordering (adds complexity). Approximate ordering (incorrect…)"*. Grepped `precept-language-spec.md`, `business-domain-types.md`, `temporal-type-system.md`, `proof-engine.md` and `type-checker.md` for prior settlement: the spec settles that period does not order, and this design changes that under the owner's 2026-07-26 ruling at `docs/working-reset/phase-1/the-walk.md:492-:494` — *"mine, I ruled that we should make same-basis periods orderable, which was against canon."*
  - `docs/language/business-domain-types.md:1821` — *"a value is accepted iff its non-zero component set is a subset of the declared basis… Zero-valued components are ignored."*
  - `docs/language/collection-types.md:586-:590` — *"Equality inside a dimension-qualified collection is conversion-aware. Adding `'5000 m'` to a set already containing `'5 km'` is a no-op — they are the same element"*.
  - NodaTime 3.3.3, `M:NodaTime.Period.CreateComparer` — *"Certain periods can't naturally be compared without more context - how 'one month' compares to '30 days' depends on where you start."* Accessed by reflection against the shipped assembly, 2026-07-28.
  - NodaTime 3.3.3, `M:NodaTime.Period.ToDuration` — *"For periods that do not contain a non-zero number of years or months, returns a duration for this period assuming a standard 7-day week, 24-hour day, 60-minute hour etc."*
  - Rust `std::cmp::PartialOrd` — `impl<T> PartialOrd for [T] where T: PartialOrd,`. Accessed 2026-07-28.
  - PostgreSQL § 8.5 — *"Internally, `interval` values are stored as three integral fields: months, days, and microseconds. These fields are kept separate because the number of days in a month varies, while a day can have 23 or 25 hours if a daylight savings time transition is involved."* Accessed 2026-07-28.
  - Original measurement, § What was measured.
- **Strongest counter-evidence**: **the coherence property this rule is chosen to protect has no consumer in the compiler.** The first draft claimed the proof engine's normal form assumes `not (a <= b)` is interchangeable with `b < a` and names `period` among the types it assumes it of. That was checked and is false: `TypeKind.Period` occurs nowhere in `ProofEngine*.cs`; the only site performing that rewrite is `NegateOp` at `ProofEngine.Strategies.cs:1057`, and every path into it is gated on `ToDecimal` (`:1067`), which accepts `decimal`, `int` and `long` only. `NarrowByConstraint`'s own comment at `ProofEngine.Satisfiability.cs:250` enumerates the types it handles and period is not among them. **And Decision 6 guarantees no consumer appears**, since its whole content is a type gate keeping period comparisons out of that path. So the design chose the narrowest of four candidate rules to preserve a property whose only real consumer is `.min` determinism on collections — which § Open questions item 5 concedes nothing demands. **Response**: `.min` determinism is a real Principle 3 obligation and the collection half is inside the owner's ruling, so the property is not free-standing. But the objection is correct that the first draft overstated it, and it is why tradeoffs 1 and 2 above now carry their full weight rather than being answered by the coherence argument alone. If the owner values `period in 'weeks'` against `period in 'days'` more than `.min` determinism on a collection nobody has written, the wider rule is the better one and this decision is wrong. **Since the ruling, `.min` determinism is a consumer that will exist** — the collection half is being built, so the property is no longer being preserved for a hypothetical. The weeks-versus-days boundary is a separate question and he did not rule on it; falsifier 3 keeps it live and names what would reopen it.
- **Reversibility**: `Effectively-irreversible-post-ship` — a comparison operator on a public type cannot be withdrawn once authors write it. Before ship it is cheap: solo developer, pre-release, nothing external depends on the current behaviour.
- **Blast radius**: catalogs — `Types`, `Operations`, `OperationKind`, `ProofRequirement`, plus the new `QualifierRelation`. Docs — `temporal-type-system.md` (twelve sites), `collection-types.md` (six), `precept-language-spec.md` (one), `type-checker.md`, `proof-engine.md`, `diagnostic-system.md`, `catalog-system.md`. Samples — none from this decision; § Ship now item 2 changes one line in `contractor-invoice-settlement.precept`. External consumers — none exist.

### Decision 2: The ordering condition is catalog metadata on the type entry, and `TypeTrait.Orderable` is removed

**Stakes**: high

- **Rationale**: the flat per-`TypeKind` flag cannot express "orders when qualified", and the codebase shows what happens when it cannot. The flag is on for `money`, `quantity` and `price`, so `.min` on an unqualified `set of money` is accepted with no diagnostic while `collection-types.md:541` calls it a type error. It is off for `period`, so `.min` is refused on every period collection including a pinned one. Both directions wrong, same cause. A condition on the type entry answers both, and answers the `by P` key question and the hover question from the same place.
- **Tradeoff accepted**: it turns a period ruling into a change touching four types and every type entry, and it makes the PRECEPT0010 analyzer read a discriminated-union-valued catalog property rather than a flags enum — the riskiest piece of work here and the one whose size is least certain.
- **Alternatives considered**: *Keep the flag and add a third per-instance override beside the ordered-choice one.* Rejected on two grounds: it is a per-`TypeKind` special case in pipeline code, and it does not work — the override would have to be duplicated in two methods that are already verbatim copies, and the existing single exception on the neighbouring resolution path carries a comment forbidding exactly this extension.
- **Precedent**: `choice of T(...) ordered` already makes ordering conditional on a declaration; `Types.cs:420` declares choice without the flag while `Operations.cs:866-:897` gives it four ordering operations. The mechanism exists, spelled as a hardcoded exception rather than as metadata.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs:1031-:1036` — `var effectiveTraits = elementMeta.Traits; if (GetElementTypeRef(receiver, ctx) is TypedChoiceElement { Ordered: true }) effectiveTraits |= TypeTrait.Orderable;`
  - `src/Precept/Pipeline/ProofEngine.Qualifiers.cs:526-:528` — *"Acceptable as a single arm; must not be extended to other accessor operations (.first / .last / .at(N) / .min / .max) — those belong on the catalog-driven path."*
  - `src/Precept/Language/Types.cs:570`, `:608`, `:655`, `:489` — `money`, `quantity`, `price` carry `Orderable | EqualityComparable`; `period` carries `EqualityComparable`.
  - `docs/language/collection-types.md:541` — *"Unqualified `money`: `.min`/`.max` are type errors"*, against a live probe showing no diagnostic. Grepped `catalog-system.md` and `collection-types.md` for prior settlement on how orderability is declared — canon states the *outcome* per type and never states a mechanism, so the mechanism is genuinely undecided.
- **Strongest counter-evidence**: a smaller path is real. Four operations plus a broadened analyzer suppression plus a `TemporalUnit` branch delivers scalar period ordering in roughly a hundred lines without touching the other three types. **Response**: that path delivers the scalar half only, because the value coming out of `.min` must carry its qualifier and the place it is lost is keyed on expression shape rather than on type. § Open questions item 5 put the scalar-only option to the owner rather than assuming the collection half was wanted, **and he ruled for both halves on 2026-07-29** — so the smaller path does not deliver what was authorised and this decision is in scope.
- **Reversibility**: `Hard` — the enum member is public catalog surface consumed by MCP output.
- **Blast radius**: every type entry in `Types.cs`; the enum in `Type.cs`; the PRECEPT0010 analyzer; `CatalogFormatters.cs`; `FaultAxisEnumerator.cs:254`; three test methods in `TypesTests.cs` covering 27 theory cases, which break at compile; five of eleven tests in `Precept0010Tests.cs`.

### Decision 3: The relation belongs on the proof requirement, not on the axis

**Stakes**: high

- **Rationale**: the temporal-unit axis needs two different relations in two different positions — subset on assignment, same-and-single on comparison. If the relation is a property of the axis, those cannot coexist. Naming it on the requirement also turns the existing quantity behaviour from an enum-identity test into a declared relation.
- **Tradeoff accepted**: 32 existing construction sites must each state their relation. Giving the member a default avoids breakage but stops the catalog being forced to declare it, which is the discipline being invoked.
- **Alternatives considered**: *A per-axis relation table* — rejected, the same axis needs two. *Encode the relation in the requirement's `Description` string* — rejected as a parallel definition in prose.
- **Precedent**: `business-domain-types.md:1212` already says the relation varies by axis. This decision extends canon's observation, because canon had not met a case where one axis needs two.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Qualifiers.cs:118-:173` — `QualifiersAreCompatible`, with the `TemporalDimension` branch at `:126` and the `Unit || Dimension` branch at `:141`, and **no** `TemporalUnit` branch.
  - `src/Precept/Pipeline/ProofEngine.QualifierNarrowing.cs:54-:56` — *"// D14 composite basis: the TemporalUnit axis discharges by SUBSET, not equality"*.
  - `docs/language/business-domain-types.md:1212` — *"identity axes (currency, unit, dimension, from/to currency) discharge on exact value equality; the **period basis** axis discharges by D14 subset (the value's non-zero component set ⊆ the required basis)."* Grepped `proof-engine.md`, `type-checker.md` and `business-domain-types.md` for prior settlement on where the relation lives — canon states relations per axis and is silent on a per-position split.
- **Strongest counter-evidence**: no counter-evidence found after looking in `ProofEngine.Qualifiers.cs`, `ProofRequirement.cs`, `Operations.cs`, `business-domain-types.md § D9`/`§ D14` and `proof-engine.md`. The nearest objection is cost, which is 32 mechanical edits.
- **Reversibility**: `Easy` — an additional record member with a default.
- **Blast radius**: `ProofRequirement.cs`; 32 sites in `Operations.cs`; four construction sites in tests; `proof-engine.md` and `type-checker.md`.

### Decision 4: `.min` / `.max` legality is the comparison relation applied to the element's declared qualifier against itself

**Stakes**: medium

- **Rationale**: `.min` orders a collection's elements against each other, and every element is drawn from one declared element type. So "is ordering meaningful here" is "does the relation hold of this qualifier against itself". Deriving it means the table canon maintains by hand never gets written into code.
- **Tradeoff accepted**: the derivation is only as good as the relation evaluator; a bug there is a bug in every row at once.
- **Alternatives considered**: *A separate one-sided condition per type* — rejected as a second definition of the same fact. *Keep the hand-written table in the type checker* — rejected; it is the fourth copy of the orderable list.
- **Precedent**: the four business-domain rows at `collection-types.md:541-:544` and the eleven temporal rows at `:525-:535` were each checked against this derivation. `money in 'USD'` ✓, unqualified `money` ✗, `quantity of 'length'` ✓ with conversion (both sides project to the same dimension name), `quantity in 'kg'` ✓, `price in 'USD' of 'mass'` ✓ under both conditions, `period in 'days'` ✓, `period in 'years + months'` ✗, `period of 'time'` ✗, unqualified `period` ✗. Every row comes out as canon states it.
- **Sources consulted for this decision**:
  - `docs/language/collection-types.md:541-:545` — *"`quantity of 'length'` | ✓ — conversion-aware"* and *"Unqualified `quantity`: `.min`/`.max` are type errors"*.
  - `docs/language/collection-types.md:553` — *"`money`, `quantity`, and `price` support qualifiers that restrict elements to a homogeneous denomination and unlock ordering"*. Grepped `collection-types.md` for prior settlement on how the collection rule is derived — canon states the outcome per row and never states a derivation.
- **Strongest counter-evidence**: no counter-evidence found after checking every row of `collection-types.md:525-:545` against the derivation and reading `TypeChecker.Expressions.Callables.cs:1022-:1055`.
- **Reversibility**: `Easy`.
- **Blast radius**: the two duplicated trait blocks in `TypeChecker.Expressions.Callables.cs`; `collection-types.md`.

### Decision 5: In a comparison, a period literal that derives no basis takes the basis of the other operand

**Stakes**: medium

- **Rationale**: `'0 hours'` parses to a period with every component zero, so `DerivePeriodLiteralBasis` yields no components and no same-single-component premise can be satisfied. Without this rule `rule WorkedTime > '0 hours'` cannot be written. It is sound because the zero period is genuinely the same value in every basis — all eleven zero constructions in NodaTime are mutually equal — and adding it to any date leaves the date unchanged.
- **Tradeoff accepted**: it is a special case a reader must know about to predict whether a literal comparison compiles. And the predicate available in code is "derives no basis", not "is zero" — those coincide only while the seven catalogued units cover everything Precept can express. § What was measured records the sub-second gap; if that ever closes, this rule must be re-stated on the parsed value.
- **Alternatives considered**: *Let any period literal take its basis from the sibling operand* — **rejected as unsound**: `rule Grace > '1 hours'` where `Grace` is `period in 'days'` would silently re-read a one-hour literal as one day. The narrowing must be on the value, not the spelling. *Require the author to write `'0 days'` to match* — rejected; at zero the units are interchangeable, so the compiler would be demanding a distinction that does not exist.
- **Precedent**: the proof engine already lifts a missing property from a contextual sibling operand for choice literals, at `ProofEngine.Strategies.cs:107-:134`. Same shape, same place.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/TypeChecker.Expressions.cs:538` — `DerivePeriodLiteralBasis`, building the basis from the non-zero components of the parsed `NodaTime.Period`.
  - `src/Precept/Pipeline/ProofEngine.Strategies.cs:107-:134` — the choice-literal contextual inference this copies.
  - `docs/language/business-domain-types.md:1821` — *"Zero-valued components are ignored — a `Period(Years: 2, Months: 0, Days: 0)` value is accepted against `period in 'years + months'`."* Grepped `business-domain-types.md § D14`, `literal-system.md` and `temporal-type-system.md` for prior settlement: canon settles the **declaration** case, which is why the declaration half of this problem is filed as a conformance fix rather than a decision — see the note below.
  - NodaTime 3.3.3, measured: all eleven zero constructions compare equal and share a hash code.
- **Strongest counter-evidence**: no counter-evidence found after looking in `TypeChecker.Expressions.cs`, `ProofEngine.Strategies.cs`, `business-domain-types.md § D14` and `literal-system.md`.
- **Reversibility**: `Easy`.
- **Blast radius**: `ProofEngine.Strategies.cs`; `business-domain-types.md § D14`.

> **Not a decision — a conformance fix.** `field WorkedTime as period in 'hours' default '0 hours'` emits `PRE0141` today. Under `business-domain-types.md:1821`, quoted above, `'0 hours'` has an empty non-zero component set and ∅ ⊆ `{hours}`, so it **should already compile**. That is implementation drift against a locked decision, not a choice to be made, and it belongs with § Ship now. The drift lives in `DerivePeriodLiteralBasis` returning null where the subset test should see an empty set.

### Decision 6: The proof engine's relational-fact path gains a type gate, in the same change

**Stakes**: high

- **Rationale**: a soundness hole that opens the moment a period ordering operation is registered, and invisible from any document. `TryGetRelationalFact` filters on expression shape and operator identity with **no type test**, so `rule A < B` on two period fields would become a field-to-field constraint and flow into decimal interval arithmetic that reads a period through `ProjectPeriodSign` — a projection whose own source comment says *"NOT suitable for min/max bound comparison. Period bounds need a separate design."* It ranks `'25 hours'` (25 000 000) above `'2 days'` (20 000 000), which no period order does.
- **Tradeoff accepted**: period comparisons contribute no facts to relational reasoning, so `when Grace < Limit` narrows nothing downstream. That is correct until a period interval domain exists — **and it is also why Decision 1's coherence argument has no consumer**, which Decision 1's Counter-evidence leg now states.
- **Alternatives considered**: *Make `ProjectPeriodSign` order-preserving on the admitted domain* — rejected; it is monotone on single-unit operands only by accident, because both sides then use the same multiplier, and nothing states or tests that. *Ship ordering and gate later* — rejected; the hole is live on day one.
- **Precedent**: no precedent — a defect found by this pass, fixed by a type test where none exists.
- **Sources consulted for this decision**:
  - `src/Precept/Pipeline/ProofEngine.Composition.cs:224-:241` — `var op = Operations.GetMeta(comparison.ResolvedOp).Op; if (op is not (OperatorKind.GreaterThan or …)) return false; fact = new FieldToFieldConstraint(leftRef.FieldName, op, rightRef.FieldName);`
  - `src/Precept/Pipeline/TypedExpressionMagnitude.cs:142-:155` — `if (p.Days != 0) return p.Days * 10_000_000m; if (p.Hours != 0) return p.Hours * 1_000_000m;`
  - `src/Precept/Pipeline/TypedExpressionMagnitude.cs:22-:28` — the source's own disclaimer. Grepped `proof-engine.md` and the spec for prior settlement on which types enter relational reasoning: `proof-engine.md` describes the path without naming a type restriction, so canon is silent and the code has none.
- **Strongest counter-evidence**: it could be argued the existing guards — `SignSetSatisfiesRequirement`'s early return on a non-zero threshold, and `period` being excluded from the ranged numeric types — already keep the projection out of verdicts. **Response**: `nonnegative`, `positive` and `nonzero` do apply to `period` (`Modifiers.cs:16-:22`), giving a period field a bound the relational path can consume. The guards are incidental, not designed for this.
- **Reversibility**: `Easy` — a type test.
- **Blast radius**: `ProofEngine.Composition.cs`; `proof-engine.md`.

### Decision 7: Ordering ships before the period interval domain, and period comparisons contribute no proof facts

**Stakes**: medium

- **Rationale**: waiting would defer a decided feature behind an undecided one, for a benefit that is small today. A comparison that establishes no downstream fact is still a rule that holds, enforced on every operation. And there is nothing yet for it to narrow: `min`/`max` field constraints exclude every temporal type, so no period bound obligation exists for a guard to discharge.
- **Tradeoff accepted**: `when Grace < Limit` narrows nothing. When the interval domain lands, comparisons written under this design begin contributing facts with no change to what anyone wrote.
- **Alternatives considered**: *Wait for the interval domain* — rejected. *Ship a full period interval domain here* — rejected as a much larger piece of work with its own design.
- **Precedent**: `duration` already ships fully orderable with the same gap — a provably false rule over two `duration` defaults compiles clean today with no `PRE0164`, while the identical rule on `decimal` fields is correctly refused. The fold gap is not period's and is not new.
- **Sources consulted for this decision**:
  - `docs/language/temporal-type-system.md:1713` — "Interval assignment constraint enforcement may extend to temporal types in the future (requires `NumericInterval` for duration/period — deferred)."
  - `docs/language/precept-language-spec.md:1696` — the `min`/`max` row excluding all temporals.
  - `docs/compiler/diagnostic-system.md:420` — *"a default that is unknown or cannot be folded rejects as well… The current implementation emits only on a proven-`false` fold."* Grepped `temporal-type-system.md`, `proof-engine.md` and `diagnostic-system.md` for prior settlement on sequencing: canon records the deferral and does not order the two pieces of work.
- **Strongest counter-evidence**: no counter-evidence found after looking in `temporal-type-system.md`, `diagnostic-system.md`, `proof-engine.md § Strategies`, and by probing the `duration` fold behaviour directly.
- **Reversibility**: `Easy`.
- **Blast radius**: `temporal-type-system.md:1713`; `proof-engine.md`.

### Decision 8: Decision 14 is rewritten rather than deleted, and records what survives

**Stakes**: medium

- **Rationale**: three of Decision 14's four parts survive intact and one changes. Deleting it would lose the reasoning and guarantee the question is rediscovered a fifth time — it has already been ruled, withdrawn, re-ruled and reopened. The rewrite states NodaTime's actual reason, records both rejected alternatives as still rejected, and says which comparisons are refused for reach rather than for correctness. **The rewrite is needed whichever way § Open questions item 2 goes**, and the two versions differ only in their opening sentence.
- **Tradeoff accepted**: the rewritten decision is longer than the original.
- **Alternatives considered**: *Delete and renumber* — rejected; `precept-language-spec.md` and `collection-types.md` both cite Decision 14 by number. *Leave it and add a carve-out sentence* — rejected; its stated reason is wrong as written, not merely incomplete.
- **Precedent**: `CLAUDE.md § Per-Decision Rationale` requires all four legs on a locked decision — *"A decision that states WHAT without WHY is incomplete. Flag it before it advances."* A decision whose reason has been shown wrong is incomplete until the reason is repaired.
- **Sources consulted for this decision**:
  - `docs/language/temporal-type-system.md:1480-:1485` — the current text in full.
  - `docs/working-reset/phase-1/the-walk.md:496-:504` — *"Decision 14 … is overridden by owner ruling. It is a locked four-leg decision, so the override is recorded as his, not re-argued."*
  - `docs/working-reset/compiler-readiness-plan.md:54-:59` — *"Both sites still assert the opposite of the ruling, one of them in bold."*
  - `CLAUDE.md § Per-Decision Rationale` — quoted above. Grepped `temporal-type-system.md` and `CONTRIBUTING.md § Doc Lifecycle` for prior settlement on how a superseded decision is handled: the lifecycle covers promotion and archival, not in-place repair of a wrong rationale, so this is silent.
- **Strongest counter-evidence**: no counter-evidence found after looking in `temporal-type-system.md`, `CONTRIBUTING.md § Doc Lifecycle` and `docs/archive/`.
- **Reversibility**: `Easy`.
- **Blast radius**: `temporal-type-system.md`; the cross-references at `collection-types.md:530`, `:533` and `precept-language-spec.md:1449`.

---

## Falsifiers

1. **If a `.precept` definition is found where two periods compare one way and `==` says something different**, the coherence property has failed and Decision 1 is wrong. It was measured at zero violations over the values Precept can express; one instance falsifies it.
2. **If an author is found writing `A.days < B.days` on two matching single-unit fields after the operator exists**, the operator did not replace the accessor route and the refusal messages are not teaching. The accessor form stays legal by design and should become rare.
3. **If three or more definitions have to change a `period in 'weeks'` or `period in 'hours + minutes'` declaration purely to get a comparison**, the single-unit boundary is too narrow and the rule should widen to the years-and-months-free family, accepting the ordering-versus-equality cost. This falsifier is written on weeks and composites deliberately, because those are the sound comparisons the rule refuses.
4. **If a period ordering comparison is ever observed contributing a fact to the relational reasoning path**, Decision 6's type gate has been bypassed or removed, and the ordering is unsound.
5. **If a domain expert in a usability test cannot get a period comparison to compile within ten minutes of hitting `PRE0165`**, the refusal message does not carry its own fix and the audience claim is falsified.

---

## Acceptance criteria

**For § Ship now** — these hold whichever way the ordering question goes.

1. `dequeue Q into X` where `Q` is `queue of period` and `X` is `period in 'days'` raises a qualifier diagnostic. The same shape on `queue of money` into `money in 'USD'` raises a currency diagnostic. Both compile clean today with no obligation raised.
2. `LeaseTerm.months <= 12` where `LeaseTerm` is `period in 'years + months'` is refused. So is `LeaseTerm.months + 0 <= 12`. `Grace.days < Limit.days` on two `period in 'days'` fields still compiles. `LeaseTerm.months == 1` still compiles.
3. `.min` on an unqualified `set of money` is refused; `.min` on `set of money in 'USD'` compiles and its result compares against another `money in 'USD'` with the currency obligation discharged. Both are wrong today, in opposite directions.
4. `field Bad as period of 'time' default '3 years'` is refused.
5. `field B as date <- A + '12 months'` compiles, with no comparison anywhere in the file, and so does the same expression with `'30 days'`.
6. `field WorkedTime as period in 'hours' default '0 hours'` compiles. It emits `PRE0141` today, against `business-domain-types.md:1821`.
7. A period ordering comparison — in a `rule`, in an `ensure`, and in a transition guard — produces a message naming the operands and the fix, and never the text *"Expected a period value here, but got 'period'"*.

**For the ordering operator.** The owner authorised both halves on 2026-07-29 (§ Owner rulings, ruling 1), so these are obligations on the build rather than a conditional list — including 13, which is the collection half.

8. `rule A <= B` where both fields are `period in 'days'` compiles with no diagnostic, and the comparison appears in the proof output as a discharged qualifier obligation on the temporal-unit axis. The same holds in an `ensure` and in a transition guard.
9. `rule A <= B` where `A` is `period in 'days'` and `B` is `period in 'months'` is refused with `PRE0166` naming both units.
10. `rule A <= B` where either field is unqualified, `of 'date'`, `of 'time'`, or a composite `in` is refused with `PRE0165`, and the message names which operand is not pinned.
11. `rule WorkedTime > '0 hours'` where `WorkedTime` is `period in 'hours'` compiles.
12. `field Gap as period <- A - B` where both are `period in 'days'` is orderable against another `period in 'days'`; `field Gap as period <- SomeDate - OtherDate` is not.
13. `.min` on `set of period in 'days'` compiles under a `.count > 0` guard and returns a value comparable against another `period in 'days'`. `.min` on `set of period`, `set of period of 'time'` and `set of period in 'years + months'` is refused, with a message not containing the word "trait".
14. Assignment into `period in 'years + months'` still discharges by subset after the relation moves onto the requirement — `field X as period in 'years + months' default '1 years'` still compiles. This is the regression Decision 3 is most likely to cause.
15. `rule A < B` on two period fields produces no field-to-field constraint in the proof engine's relational facts. A test asserts the absence.
16. The build fails if a type entry declares ordering operations without a matching ordering condition, and if an ordering condition names a relation no operation carries. `PRECEPT0010b` and `PRECEPT0010d` both pass on the shipped catalog.
17. No sample file changes except `contractor-invoice-settlement.precept`, whose `WorkedTime` declaration and its `ensure` are corrected under § Ship now item 2.

---

## Dependencies

**Upstream.** § Ship now items 1, 2, 3 and 4 are preconditions for the ordering operator, and items 1 and 3 are unavoidable regardless. Item 1 is the sharpest: Decision 1's entire correctness argument reads a declared qualifier and assumes it constrains the value, and one binding path defeats that today. Items 5 and 6 are not preconditions but are on every route this design points authors down.

**Owner decisions.** Decision 1 was conditional on § Open questions items 1 and 4. Both are settled — item 1 by the owner's amendment to alignment directive item 4, live at `temporal-type-system.md:244`, and item 4 by his waiver of 2026-07-29. **No owner decision is now outstanding on this design.** The one owner question left anywhere in this area is § Open questions item 3, which is a defect rather than a design question and does not gate anything here.

**Downstream.** Readiness-plan row `N82` closed on 2026-07-29 — the document reconciliation landed ([`period-ordering-reconciliation.md`](period-ordering-reconciliation.md)) and the mechanism is designed here. The qualifier-conditional ordering capability, once it exists, is what a `by P` key-type check would consult. A period interval domain, when built, makes period guards informative with no change to anything written under this design.

---

## Doc-update enumeration

Per the routing table in `CLAUDE.md § Documentation Sync`.

| Document | What changes |
|---|---|
| `docs/language/temporal-type-system.md` | Decision 14 rewritten (`:1480-:1485`) and its contents entry (`:68`); the not-supported operator row (`:805`); the teachable message (`:866`); the comparison-rules table (`:1328`); the cross-type table (`:1345`); the `.min`/`.max` sentence (`:1736`); the collection inner-type row (`:1745`); the `.min`/`.max` error text (`:1749`); the work lists (`:1776`, `:1818`); the component-accessor note (`:821`) gains the completeness rule; the approximation-stance row (`:264`) repaired or removed, since it cites a diagnostic that does not exist; `:244` item 4 **only if the owner amends it** |
| `docs/language/collection-types.md` | `:147` restated as a condition rather than a list, with `exchangerate` corrected; `:386` and `:1098` replaced by pointers to `:147`; the period rows `:530-:533`; the money/quantity/price rows `:541-:544` and the sentence at `:553` |
| `docs/language/precept-language-spec.md` | `:1449` reworded — it was already not literally true given `:1692`/`:1693` |
| `docs/language/business-domain-types.md` | § D14 gains the zero-literal comparison rule and a note that the declaration case was already settled at `:1821`; `:1847`'s stated reason repaired, since `Period.ToDuration()` exists and the refusal is real only for years and months |
| `docs/language/catalog-system.md` | **not a new catalog in the inventory** — the `Types` catalog's entry shape gains the `OrderingCondition` property, the `ProofRequirement` catalog's compatibility requirement gains its `Relation` member, and `QualifierRelation` is described as the shared metadata vocabulary both read. `TypeTrait` loses a member |
| `docs/compiler/type-checker.md` | `:886` — the temporal-unit discharge is now two relations by position; `:884` — the basis-path gap |
| `docs/compiler/proof-engine.md` | `:777` — same; the relational-facts section gains the type gate |
| `docs/compiler/diagnostic-system.md` | `PRE0165`, `PRE0166`, `PRE0167`; the replaced messages for `PRE0018` and `PRE0104`; a `.precept` scenario file in `test/integrationtests/diagnostics/` for each new code |
| `docs/runtime/evaluator.md` | the integer comparison the evaluator gains |
| `docs/tooling/mcp.md` | `precept_types` output shows the ordering condition |
| `docs/tooling/language-server.md` | hover and completion begin consuming it |
| `docs/working-reset/compiler-readiness-plan.md` | row `N82` |
| `README.md` | no claim about period ordering — **None** |

---

## Operational dimensions

- **Security** — N/A. No source-text ingestion path changes; no new literal form, no new parser rule.
- **Observability** — the comparison cannot fault, so there is no runtime failure to diagnose. At compile time the refusal names the operand and its declared basis; `precept_proofs` shows the qualifier obligation and its discharge. The caution in `CLAUDE.md § Use the MCP Tools First` about untrustworthy proof results concerns numeric obligations; the qualifier-obligation output was checked directly in this pass and matched the code.
- **Evolvability** — this design reads `Period`'s component properties and never calls `CompareTo`, `ToDuration` or `Normalize`, so it is insulated from the two NodaTime behaviours most likely to change. `src/Precept/Precept.csproj:15` pins `NodaTime` at `3.*`. If NodaTime added `IComparable<Period>`, this design would not change but alignment directive item 4 would need re-reading. If it renamed a component property, `TemporalUnits.cs` is the single place that breaks. Three further changes re-open the measurement itself, and each must trigger a re-run before anything built under this design can still be called sound:
  1. **Widening the seven-unit table toward NodaTime's ten components.** The gap between them is what makes the coherence property conditional (§ What was measured), and this is the change most likely to require re-stating Decision 1.
  2. **Admitting any calendar other than ISO.** The soundness claim is scoped to ISO because ISO is the only calendar Precept exposes. The nine-calendar run in § What was measured found the count order and the calendar order disagreeing 48 times in the Badi calendar, on a NodaTime defect the shipped 3.3.3 assembly still carries. If `CalendarSystem` ever reaches a Precept declaration, the measurement must be re-run in every calendar admitted, and the Badi behaviour resolved, before period ordering can be trusted there.
  3. **Admitting `zoneddatetime ± period`.** The claim that this design takes no position on whether a day is 24 hours rests on that operation being a type error today (`temporal-type-system.md:1195`, `:1342`). If it is ever admitted, the time-unit half of the ordering claim needs re-deriving against daylight-saving transitions.

---

## Open questions

Five were asked. **Four are closed and one was never a question.** Nothing on this list is waiting on an answer, and the owner locked the design on 2026-08-11 (§ Owner rulings, ruling 3) — a lock covering the eight decisions and none of the unbuilt code, as § Is this lockable sets out.

| # | Question | Disposition |
|---|---|---|
| 1 | Is the alignment directive satisfied, or does it need amending? | **Closed** — it needed amending, and the owner amended it. Directive item 4 at `temporal-type-system.md:244` now carries the named exception, in his wording |
| 2 | Should the ordering operator ship at all? | **Closed** by ruling 1, 2026-07-29 — *"build both."* It ships |
| 3 | What does a period's sign mean when its components disagree? | **Not a design question.** It is a live defect — a false proof reproducible at HEAD — and it is independent of ordering. See the item below for where it is tracked and for the one part of it that does need a decision |
| 4 | Does the comparator-survey gap have to close first? | **Closed** by ruling 2, 2026-07-29 — waived, on the ground that those systems are not comparators for this decision. § Owner rulings |
| 5 | Scalar only, or scalar and collection? | **Closed** by ruling 1 — both |

The five subsections below are kept as they were written, because the argument each carries is why the answer is what it is. Each now opens with its disposition.

**1. Is the NodaTime alignment directive satisfied by this reading, or does it need amending?**

> **Closed — it needed amending, and it has been amended.** The design's own conclusion below, that this is an amendment request rather than a reading, was right and the owner accepted it. The amended item 4 is live in canon and is quoted in § What the adversarial review found. The wording the owner chose is shorter than the review's draft and names a single exception with no fence, on the reasoning that a named exception needs none — a composite is not pinned to a single unit, a mixed pair is not the same single unit, and `zoneddatetime` is not a `period`. `zoneddatetime` therefore stays refused for its own separate reason, which is what the review's draft was built to guarantee.

`temporal-type-system.md:244` item 4: *"When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type."*

The reading this design rested on: NodaTime has no natural ordering on `Period` because `Period` spans every component combination, and `IComparable<Period>` cannot be conditional on what a value holds; `period in 'days'` is a narrower thing, narrower in exactly the way that removes the obstacle.

**The reading that refuses it, and it is stronger than the first draft allowed.** `period in 'days'` is *not a type* in Precept's type system. `TypeKind.Period` is the type; the basis is a qualifier record resolved per expression. And the mechanism this design proposes settles the question against itself: the four ordering operations would sit in `OperationKind` and `Operations` on `period × period`, and `PRE0165`/`PRE0166` are classified as **Proof** diagnostics — so the catalog would say `period` has `<`, `<=`, `>`, `>=`, and the refusal would be a failed proof about particular operands. That is the same shape as `money`, where `rule A <= B` on two unqualified `money` fields resolves the operator and fails a proof. Contrast `exchangerate`, which this design makes `NeverOrdered` with no operations at all.

So the honest position is: **this is an amendment request, not a reading.** The better argument for it is not that a qualifier makes a type, but that `collection-types.md:553` already makes ordering conditional on a qualifier for three types, and item 4 was written before that pattern existed. If an amendment is wanted, it must be worded so `zoneddatetime` stays refused — and `zoneddatetime` is refused for a different reason, that two orderings are each total and canon will not pick, so a clause keyed on "NodaTime's stated obstacle does not apply to this subset" keeps it out. The wording belongs in that conversation, not here.

**2. Should the ordering operator ship at all?**

> **Closed — yes, both halves.** Ruling 1, 2026-07-29: *"build both."* What follows is the weighing that was put to him, kept because it is the record of what he was choosing between. **The smaller answer below is closed** and should not be revived: the review's § 5 established that shipping collection `.min`/`.max` on pinned periods while withholding the scalar operator is the one outcome worse than either, and the ruling takes the whole path rather than the middle.

The corrected picture: the operator is sound, it is the owner's ruling, and on matching single-unit fields it changes `GracePeriod.days <= MaximumGrace.days` into `GracePeriod <= MaximumGrace`. Neither identified author need requires it — the ASC 842 test is a date comparison, and the contractor's field should be a `duration`. It refuses `period in 'weeks'` against `period in 'days'`, which is exact. And the coherence property it was chosen to protect has no consumer in the compiler.

What it buys is Principle 6: a domain expert compares two lease terms rather than two integers, and every fact about the operands survives into the expression instead of being discarded at the accessor boundary. That is a real gain and it is the whole gain.

**The smaller answer, if the waffle lands the other way:** ship § Ship now, rewrite Decision 14 to keep its answer and repair its reason, and record in it that same-single-unit ordering is **sound and refused for reach, not for correctness**, with the author's route being the component accessor on a matching basis. That closes row `N82`, needs no directive amendment and no research waiver, and stops this being rediscovered. Decision 8 is written to serve either outcome.

**One thing to be clear about either way**: the *ASC 842* need would also want the `equipment-lease-agreement` sample to gain a ceiling field. The rule shown at `:85`-style spelling —

```precept
rule ExpirationDate <= CommencementDate + ShortTermCeiling when LeaseClassification == "ShortTerm" and CommencementDate is set and ExpirationDate is set because "A short-term lease under ASC 842 runs twelve months or less — expiry {ExpirationDate} is more than twelve months after commencement {CommencementDate}"
```

— compiles clean with its obligations discharged, but `ShortTermCeiling` does not exist in that file and must be added. The first draft said no declaration change was needed; that was wrong. The literal spelling `CommencementDate + '12 months'` is what would avoid the extra field, and it is blocked by § Ship now item 5.

**3. What does a period's sign mean when its components disagree?**

> **Not a design question — a defect, and a second one behind it.** Filing this as an open question of this design was a mis-classification, and the correction matters because an open question stalls a lock while a defect does not. Nothing about the ordering rule depends on it: the ordering reduction compares two counts of one unit and never reads a sign, and a mixed-sign value cannot arise in a single-unit basis at all.

**The defect, reproduced at HEAD 2026-07-29.** `field J as period nonnegative default '1 months + -30 days'` compiles with one unrelated warning, zero type errors, and the obligation *"Default value of 'J' must satisfy 'nonnegative'"* reported **Proved**, strategy `Literal`. That is a false proof: the value moves forward from 2021-01-01 and backward from 2021-02-01, so there is no reading of `nonnegative` it satisfies. The mechanical cause is already written down — `ProjectPeriodSign` returns the sign of the first non-zero component and never looks at the rest (`TypedExpressionMagnitude.cs:22-:28` says so in its own summary: *"Mixed-SIGN periods … misclassify … NOT suitable for min/max bound comparison"*).

**The second defect behind it.** `field D as period nonnegative <- A - B`, with `A` and `B` both `period in 'days'`, compiles with **zero proof obligations** — verified at HEAD 2026-07-29. So refusing mixed-sign literals would not close the hole; `period − period` reaches the same place with nothing raised at all, and this one is not a misclassification but a missing obligation.

**Where defects are tracked. Both are now filed — 2026-08-11, on the owner's word.** `docs/Working/bugs.md`. The false proof is **`BUG-068`** and the missing obligation is **`BUG-069`**. Each was reproduced afresh against HEAD with `precept_compile` at filing time rather than carried across from this document, and each entry says how it differs from the neighbouring entries so nobody closes it as a duplicate.

The paragraph that stood here said neither was filed and that no other entry covered them. The first half is now out of date and the second half was **not quite right**, and the correction is worth keeping. The root cause of the false proof is indeed already written down inside **BUG-047** (`bugs.md:457`, *"its non-zero check reads only the leading component"*, with the mechanical-cause bullet at `:481` naming `ProjectPeriodSign` exactly), but BUG-047's scope is the period *divisor* path and neither case above is a division. What the earlier grep missed is **BUG-048**: its second bullet already records that `nonzero`, `positive` and `nonnegative` raise no establishing obligation against *any* expression, measured on the `decimal` lane. That is the same gap `BUG-069` reports on the `period` lane, and `BUG-069` says so in its own text rather than claiming novelty it does not have. The grep missed it because BUG-048 never uses the word *period*, which is the ordinary way a search for a mechanism misses an entry filed under a different type. The readiness plan's *"the `nonnegative` unsoundness"* row (`compiler-readiness-plan.md` § 3, and the fourth "Known risks" entry it points at) remains **a different defect** — `field Bal as decimal nonnegative max 100` reporting its interval as `[−∞ .. 100]` — and covers neither.

**What inside it genuinely needs a decision, and it is not this design's.** What non-zero-ness and non-negativity *mean* for a period whose components disagree in sign. Closing that needs either a refusal on `period − period` and unary negation, or a sign domain over period components — which is the interval work deferred at `temporal-type-system.md:1713`. BUG-047 records the same open point in its own terms (*"what non-zero predicate a `period` divisor must satisfy"*). That decision belongs with the interval domain, not here.

**4. Does the comparator-survey gap have to close first?**

> **Closed — waived, 2026-07-29.** *"waive it, we're built on noda."* The design's own position below was the weaker one: build § Ship now regardless, and do not promote the ordering rule to canon before the survey exists. The owner's reason overtakes it — those systems are not comparators for this decision, because Precept ships NodaTime's `Period` rather than choosing among temporal models, so what their period types do has no bearing on whether Precept can order the value it holds. Full reasoning and the waiver's scope in § Owner rulings, ruling 2.

`research/INDEX.md:48` calls a broader temporal survey *"a Phase 11 prerequisite for any irreversible temporal-surface decision."* Decision 1 is irreversible. Since the first draft, Rust and PostgreSQL have been cited from primary documentation — and PostgreSQL turned out to *agree with Precept's canon* about why interval components cannot be merged, while exposing the fixed conversions as functions an author calls. That is a better-grounded position than the first draft had, and it is still short of a survey: java.time and Joda-Time are asserted and unverified, and four more systems are unsurveyed. **The design's own position: build § Ship now regardless; do not promote the ordering rule to canon before the survey exists.** The gate is the owner's to waive.

**5. Scalar only, or scalar and collection?**

> **Closed — both, 2026-07-29.** Ruling 1. The consequence to carry forward is the one this subsection already names: the collection half forces the four-type capability, so Decision 2 is in scope with its full blast radius, and the counter-evidence leg's cheaper scalar-only path is not the authorised build.

The ruling covers both. The scalar half is genuinely period-scoped — four operations, one relation, roughly a hundred lines — and delivers everything the two identified needs would want, since both are scalar. The collection half forces the four-type capability, because the piece it needs is keyed on expression shape rather than on type.

No sample declares a collection of periods, money, quantity or price, so the collection half is not demanded by anything written. But `.min` determinism is the only genuine consumer of the coherence property that Decision 1 rests on, so taking the scalar half alone weakens Decision 1's rationale as well as narrowing it. And § Ship now item 3 fixes the money and quantity defects regardless of what happens to periods.

---

## Is this lockable

**This section was written as an assessment, before the lock, and it is kept as it was written.** It is the working the owner was given, so he could check the reasoning rather than take a verdict. **He read it and locked the design on 2026-08-11** — *"yes lock the design and log the bugs"* — and § Owner rulings, ruling 3, records that. The section is not the authority for the lock; his sentence is. Read on to see what he was agreeing to.

### The six blockers the adversarial review listed

The review's § 7.4 named six, in dependency order. Their state on 2026-07-29:

| # | Blocker | Kind | State |
|---|---|---|---|
| 1 | § Ship now item 1 — the binding path where a declared qualifier does not constrain the value — lands first | Code | **Not done.** Acceptance criterion 1; § Dependencies makes it a precondition |
| 2 | Re-scope the soundness claim to the ISO calendar; add calendar support as a re-verification trigger | Documentation | **Done.** § What was measured carries the scope and the Badi counterexample; § Operational dimensions carries the trigger as item 2 |
| 3 | Cite the `zoneddatetime ± period` refusal as a precondition of the "no position on day length" claim | Documentation | **Done.** § What was measured states it as a precondition rather than a neighbouring fact; § Operational dimensions carries it as trigger 3 |
| 4 | Mixed-unit and composite refusals are compile errors carrying `PRE0165`/`PRE0166`, with `PRE0018` replaced in the same change | Code | **Not done.** Acceptance criteria 7, 9, 10 |
| 5 | Decision 6's relational-fact type gate ships with the operators, not after | Code | **Not done.** Decision 6, acceptance criterion 15 |
| 6 | The directive amendment is recorded before or with the Decision 14 rewrite | Owner | **Done.** The owner amended item 4 and the Decision 14 rewrite landed with it; `temporal-type-system.md:244` and `:1481`, recorded in [`period-ordering-reconciliation.md`](period-ordering-reconciliation.md) |

A seventh correction, from the review's § 8 rather than its § 7.4 — the Inventory and doc-update table calling `QualifierRelation` and `OrderingCondition` "new catalogs" — is applied in § Inventory and § Doc-update enumeration.

### Whether an outstanding build item blocks a lock

**It does not, and the design already treats these three as build work rather than as questions.** Each of the three appears in § Acceptance criteria, which is the section that states what must be true of the built thing; § Dependencies states the order they must land in, with item 1 first because Decision 1's correctness argument reads a declared qualifier and assumes it constrains the value. None of them asks anything this design has not answered — what to build, in what order, and how to tell it worked are all written down.

Treating an unbuilt acceptance criterion as a lock blocker would invert the lifecycle: a design is locked so that a plan can be written against it and code can be built to it. If a design could only lock once its acceptance criteria were met, nothing would ever be designed before it was built. The distinction that decides a lock is whether an *unanswered question* remains — something a builder would have to decide for themselves, and could decide wrongly. On this design there is none.

**The one caveat that belongs with that.** The review wrote blocker 1 as "must land first", and it means it. A build that shipped the operators before the binding path is fixed would make the prevention claim false on arrival, which the Philosophy Alignment table's Principle 1 row says in its own words. That is a sequencing obligation on whoever plans the work, and it is recorded in § Dependencies. Locking does not discharge it.

### What the lock settles

Written before the lock as *what locking would settle*; the owner set the lock on 2026-08-11, so every item below is now settled rather than proposed.

- **Decision 1** — two periods order when both resolve, on the temporal-unit axis, to a basis naming exactly one component and the same component on both sides. Everything else refused at compile time. Irreversible after ship.
- **Decision 2** — `TypeTrait.Orderable` is removed and replaced by a condition on the type entry, across `period`, `money`, `quantity`, `price`, `choice` and every other entry. Public catalog surface; hard to reverse.
- **Decision 3** — the discharge relation lives on the proof requirement, not on the axis. 32 mechanical construction-site edits.
- **Decision 4** — `.min`/`.max` legality is the comparison relation applied to the element's declared qualifier against itself.
- **Decision 5** — a period literal deriving no basis takes the other operand's basis in a comparison.
- **Decision 6** — the relational-fact path gains a type gate, shipping with the operators.
- **Decision 7** — ordering ships before the period interval domain, and period comparisons contribute no proof facts.
- **Decision 8** — Decision 14 is rewritten rather than deleted. **Already executed**, 2026-07-29, ahead of the lock.

All eight carry the four legs `CLAUDE.md § Per-Decision Rationale` requires — rationale, alternatives rejected with reasons, precedent, tradeoff accepted — plus a counter-evidence leg, a reversibility mark and a blast radius. **Two are short on precedent, and the lock records that rather than hiding it.** Decision 6 has no precedent at all and says so, because the type gate fixes a defect this pass found rather than following anything anyone has done before; what grounds it instead is the defect itself, evidenced at `ProofEngine.Composition.cs:224-:241` and `TypedExpressionMagnitude.cs:142-:155` in its Sources leg. Decision 8's precedent is the project's own rationale rule — a rule about how decisions must be written, not an instance of a decision being repaired this way — so that leg is answered by a standard rather than by evidence of the practice. Neither gap was filled with an invented precedent to get the lock past the four-leg bar, and a reader who wants to re-open either has the honest reason to hand. Decision 1's counter-evidence leg names the condition under which the decision is wrong (that the owner values `period in 'weeks'` against `period in 'days'` more than `.min` determinism), prices it, and hands it to falsifier 3. That is a named live tension inside a locked decision, which is what the four-leg standard asks for rather than something it forbids.

### What the lock does not settle

**Read this list before acting on the lock.** Nothing here is built, and the lock says nothing about whether it is.

- **The three code items.** § Ship now item 1 — the binding path where a declared qualifier does not constrain the value — plus the refusal diagnostics that replace `PRE0018`, and the type gate on the relational-fact path. Each is an implementation obligation with an acceptance criterion, which is why none of them held the lock up; none is done, which is why the lock does not make the mechanism real. Item 1 lands first or Decision 1's correctness argument is false on arrival.
- **The § Ship now repairs.** Six defects, four of them not about periods. A lock does not fix them, and item 1 must be fixed before the operators ship.
- **§ Open questions item 3.** The question of what non-zero-ness and non-negativity mean for a period whose components disagree in sign. Independent of ordering; belongs with the deferred interval work. The two defects underneath it are filed as `BUG-068` and `BUG-069` and are not waiting on that question — they are wrong under every reading of it.
- **The weeks-versus-days boundary.** Refused for reach rather than correctness, with falsifier 3 naming what would reopen it.
- **Where the work is scheduled.** `compiler-readiness-plan.md` § 1 and § 4 record the mechanism as folded into phase 5. Phase 5's own stated scope is settling the obligation families and the discharge procedure, and the build happens at phase 11 — so "folded into phase 5" needs reading as where the obligation and discharge shape are settled, not where the code lands. Whoever plans the phases owns that, not this design.

### Assessment, and the lock that followed it

**The assessment was: lockable.** Every question this design put to the owner is answered, three of the review's six blockers are closed, and the other three are implementation obligations the design already carries as acceptance criteria with a stated order. The remaining risk is in the building, not in the deciding — and the design names the sequencing constraint that keeps the building honest.

**The owner locked it on 2026-08-11** — *"yes lock the design and log the bugs"* — and the front matter and § Owner rulings, ruling 3, carry that. The assessment above is an agent's argument and was never authority for a lock; his sentence is. What the lock reaches and what it leaves alone are the two lists above, and the shorter of them is the one that matters for anyone about to build: the three code items are not done.

---

## What this design settles, for the record

The rule and its decision procedure for every declaration form and every operand origin, including the result basis of period-producing operations. The mechanism, in file-level detail, and why the relation belongs on the requirement rather than the axis. The measurement that settles whether a same-unit comparison depends on a reference date, in both directions, and the one limit on it. The zero-literal rule and the narrowing that makes it sound. The type gate on the relational-fact path. Six repairs that need no ruling, four of which are not about periods. The refusal messages. The document reconciliation.

And the plain answer to the question the owner asked: NodaTime's reason is real, it is about the width of the type, and it does not reach a value known to be counted in one unit. For two scalar fields the comparison an author wants already works today under a different spelling, so there the operator buys legibility rather than capability, and that half should be decided on that basis. For a collection of pinned periods there is no other route at all — `.min`/`.max` legality is a trait of the element type and no accessor can supply it from outside — so that half is a capability question, and it is the one that decides whether the operator is worth building.

**And it has been decided.** 2026-07-29 — *"build both."* The research gate was waived the same day, on the ground that the unsurveyed systems are not comparators for a decision where Precept ships NodaTime's own `Period`. **The design was then locked on 2026-08-11** — *"yes lock the design and log the bugs."* § Owner rulings carries all three in his words, and § Is this lockable says what the lock reaches and what is still outstanding after it: the eight decisions are settled, and none of the code is written.
