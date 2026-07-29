# Phase 2 — the period-ordering ruling carried into the canonical documents

**Run** 2026-07-28 on top of HEAD `8b2b579b`, branch `spike/Precept-V2-Radical-reset`. **Nothing is
committed; the tree is left dirty.** No sub-agents were spawned. `docs/philosophy.md` was not edited.
Nothing under `docs/archive/`, `src/` or `tools/` was edited.
[`period-ordering-design.md`](period-ordering-design.md) keeps its `Externally-Grounded` status and its
recommendation untouched — it was already modified and uncommitted in the tree when this pass started,
by the pass that wrote it, and this pass did not add to that diff.

**What this pass is.** The owner ruled on 2026-07-26 that periods pinned to the same single unit order,
against locked Decision 14 in `temporal-type-system.md`. The ruling had never reached the documents, and
two canonical sites still asserted the opposite — six of them, once the documents were read through, and
§ 4 below has the other four. This pass carries the ruling in, and records the directive amendment the
owner approved. It is register row
[`N82`](contradiction-register.md), the last row failing exit condition 6 of phase 2.

**What this pass is not.** It builds nothing and it claims nothing is built. The compiler still refuses
`rule A <= B` on two `period in 'days'` fields, and every edit below says so or points at the one that
does. The mechanism — a qualifier-conditional ordering capability on the type entry — is new language
surface, goes through `/design`, and is not touched here.

---

## What was verified against the live compiler

Four claims are asserted in the edits below. Each was compiled at this HEAD through `precept_compile`,
not read out of a document:

| Definition | Result |
|---|---|
| `rule GracePeriod <= MaximumGrace` on two `period in 'days'` fields | `PRE0018 "Expected a period value here, but got 'period'"` — 1 type error |
| `rule GracePeriod.days <= MaximumGrace.days` on the same two fields | `success: true`, 0 type errors |
| `rule Deadlines.min.days > 0 when Deadlines.count > 0` on `set of period in 'days'` | `PRE0104 "Accessor 'min' requires a trait the element type does not provide"` |
| `rule Deadlines.days.min > 0 when Deadlines.count > 0` on the same field | `PRE0020 "'.days' is not available on set fields"` |

---

## Site by site

### 1. `docs/language/temporal-type-system.md:244` — alignment directive item 4

The owner's approved replacement, written verbatim and nothing else:

> 4. When NodaTime has no natural ordering on a type, Precept has no natural ordering on that type. One
>    exception: a `period` pinned to a single unit orders against the same single unit — see Decision 14.

"Natural ordering" is kept. It is the precise term for the order a type carries itself, as against an
order obtained by supplying a comparer — NodaTime does ship `Period.CreateComparer(LocalDateTime)`, so
dropping the word would make the first sentence arguably false. The phrase also describes
`zoneddatetime` four more times in this document, and it now reads consistently with them. The exception
names no exclusions, because a named exception needs no fence: a composite is not pinned to a single
unit, a mixed pair is not the same single unit, and `zoneddatetime` is not a `period`. The other four
items in the list are untouched.

### 2. `docs/language/temporal-type-system.md` — Decision 14, rewritten in place

Not deleted and not renumbered — it is cited by number from `collection-types.md`, from three other
tables inside its own document, and now from the directive. The heading changed, because "No ordering on
`period`" is no longer what it says:

- was: **14. No ordering on `period` — `==` and `!=` only**
- now: **14. `period` orders only when both operands are pinned to the same single unit (revised 2026-07-26)**

The contents entry at `:68` follows the heading, anchor and all. It was the only anchor link to this
decision anywhere in the repository — every other citation, in this document and in `collection-types.md`,
is by number and still resolves. § 4's specification edit adds a second anchor link, written against the
new heading.

The revision date in the heading follows the document's own convention for a decision that has been
reworked (`(v2.1 revision)`, `(rewritten 2026-04-17)`, `(revised v4)`).

What the rewritten decision says, following § 7.2 of
[`period-ordering-adversarial-review-2026-07-28.md`](period-ordering-adversarial-review-2026-07-28.md):

1. **The rule**, stated first and outside the four legs: `==`/`!=` unconditionally; `<`, `<=`, `>`, `>=`
   if and only if both operands resolve, on the temporal-unit axis, to a basis naming exactly one
   component and the same component on both sides. Mixed units, composite bases including against
   themselves, `of 'date'`, `of 'time'` and unqualified `period` are compile-time errors, not computed
   answers.
2. **The corrected reason**, in the `Why` leg: NodaTime withholds ordering because `Period` is one type
   spanning every component combination and `IComparable<Period>` cannot be conditional on a value's
   contents. That reason is about the width of the type and does not reach a value known to hold one
   count of one unit. The measurement is quoted with its scope stated — **the ISO calendar**, the only
   calendar Precept exposes — and the Badi disagreement is named as a defect in the shipped library
   rather than a real disagreement between calendars. The `Why` leg also states the precondition the
   review's blocker 3 asks for: the claim to take no position on how long a day is holds because
   `zoneddatetime ± period` is a type error, so a period count is never put where it would have to
   answer an elapsed-hours question across a daylight-saving transition.
3. **Both original rejections survive** in the `Alternatives rejected` leg — reference-date ordering and
   approximate ordering — with the sentence that this rule is neither, because it converts nothing and
   consults no reference date. A third rejection is added and marked as new: ordering composite bases,
   rejected on measurement rather than argument (432 of 141,376 `years + months` pairs change order with
   the reference date; `'1 year + 1 month'` against `'13 months'` is equal from 2019-01-01 and less-than
   from 2020-02-29).
4. **What is refused for reach rather than correctness**, in the `Tradeoff` leg: `weeks` against `days`
   is exact — `Period.Normalize()` folds at 7:1 with no calendar argument — and same-composite time-only
   bases are exact through `Period.ToDuration()`. Both are refused to keep ordering consistent with the
   structural equality of Decision 15; admitting them would order `'1 week'` against `'7 days'` while
   `==` says they are different values, and `.min` over a collection holding both would have two
   candidates.

All four legs the project requires are present and were already the decision's shape: `Why`,
`Alternatives rejected`, `Precedent`, `Tradeoff`. The `Precedent` leg keeps NodaTime's missing
`IComparable` and adds the Precept-internal precedent — `money in 'USD'`, `quantity in 'kg'`,
`quantity of 'length'` and `choice of T(...) ordered` all order conditionally on a declaration.

A closing **Implementation state: not built** paragraph records what the compiler does today, with the
`PRE0018` text and the `A.days <= B.days` form that compiles and computes the same predicate, and points
at the collection row in `collection-types.md` for the case the accessor route cannot reach.

### 3. `docs/language/collection-types.md:533` — the bold assertion

Was: *"Periods do not order regardless of basis."* That sentence is gone. The row now states the rule,
then separates it from the collection question, then says what is true today:

- Two periods pinned to the same single unit do compare with `<`/`>` — Decision #14.
- That rule is about a pair of operands. `.min`/`.max` are gated on the element type itself, so the
  pinned-element rule does not follow from it; deriving `.min`/`.max` legality from the element's
  declared qualifier is a separate decision, not yet taken. (It is Decision 4 of the design, resting on
  its Decision 2 — neither locked.)
- Today `.min` on `set of period in 'days'` is refused with `PRE0104`, and no spelling gets around it:
  `.min.days` fails at `.min` before `.days` is reached, `.days.min` fails with `PRE0020`, and Precept
  has no element-wise projection.

The cell's mark changed from `✗ — type error` to `✗ — type error today; open`, so the mark does not
assert a settled answer the rule has not given.

### 4. Five further sites that asserted the opposite

These were not in the row's two-site scope, and they were reconciled anyway. Three of them cite Decision
#14 by number inside its own document and would have contradicted it flatly the moment the rewrite
landed — a canonical document disagreeing with its own decision two tables later. One is an addition
rather than a correction: the table of *supported* period operations had no ordering row at all. The
last is the language specification, which said the same thing in its own voice; it is the line a reader
is most likely to meet first, and the register's "two sites" framing had not counted it. Each edit is
one table cell or one sentence, and the bare line numbers below are in `temporal-type-system.md`.
**If the owner reads these as outside the scope he set, they revert independently of everything above.**

| Site | Was | Now |
|---|---|---|
| `precept-language-spec.md:1449` | "`period`, `timezone`, and `zoneddatetime` support only `==`/`!=` — ordering operators are type errors." | `period` is pulled out of that group: `==`/`!=` always, ordering only when both operands are pinned to the same single unit, every other pairing a type error, with a link to Decision 14 and the note that it is not built yet. `timezone` and `zoneddatetime` are left exactly as they were |
| `:800` | *(no row)* | A new row in the **supported** period-operation table: `<`, `>`, `<=`, `>=` produce `boolean`, only when both operands are pinned to the same single unit, with the pointer to Decision #14 "which also records that this is not built yet" |
| `:806` | "`<`, `>`, `<=`, `>=` \| Periods can't be compared with `<` or `>` … See Locked Decision #14." | The **Not supported** row is narrowed to any pairing other than two operands pinned to the same single unit, and lists what those are |
| `:1329` | "`period` \| ✓ \| **✗** \| **No natural ordering.** See Decision #14." | The comparison-rules row reads **Only when pinned**, with the condition spelled out |
| `:1346` | "`period < period` \| Periods can't be compared with `<` or `>`." | The cross-type row is narrowed to the unpinned case, with the reason |

---

## What was read and deliberately left alone

- **`temporal-type-system.md:867`**, the teachable-error row for `1 month > 30 days`: *"Periods can't
  be compared with `<` or `>` — is 1 month greater than 30 days? It depends on which month."* The
  example is a mixed-unit comparison, which the rule still refuses, and the second half of the message
  is exactly why. The first half over-generalizes, and it is left alone on purpose: this is prescribed
  message text, the design replaces this whole family with `PRE0165`/`PRE0166`, and rewording it here
  would produce a message that matches neither what ships today nor what is designed. Flagged rather
  than fixed.
- **`temporal-type-system.md:1735`–`:1754`**, the collection-inner-type section: it says `.min`/`.max`
  on `set of period` are type errors because the type carries no natural ordering. It discusses the
  **unqualified** element type only, where that is still correct. It never mentions `set of period in
  'days'`, so it makes no claim the rule contradicts. If the pinned-element question is ever decided,
  this section gains a row.
- **`collection-types.md:530`**, the unqualified `period` row: *"Equality-only (unqualified). Structural
  components are not directly comparable — `'1 month'` has no fixed length."* Correct under the rule and
  already marked as being about the unqualified case.
- **`collection-types.md:531`–`:532`**, the `period of 'date'` and `period of 'time'` rows: both say the
  category qualifier is "not an ordering qualifier". Correct under the rule, and for the reason the rule
  gives — a category names which components are *allowed*, not that there is exactly one. No edit.
- **`collection-types.md:147`**, the list defining "orderable": it omits `period` entirely, which is
  consistent with nothing being built. The design's Decision 2 would restate this line as a condition
  rather than a list, and that is build work. The pre-existing defect on that line — it lists
  `exchangerate` as orderable while `business-domain-types.md:1898` makes exchangerate ordering a
  compile error — is untouched and unrelated to periods.
- **`docs/Working/bugs.md:480`, `docs/Working/gap-ledger-2026-07-13.md:276`,
  `docs/Working/exhaustive-gap-analysis-2026-07-14/coverage/temporal.md:254`,
  `docs/working-reset/phase-1/the-walk.md:498`** all quote Decision 14's old title. Each is a record of
  what canon said at the time it was written, and repointing them would falsify the record. No edit.

## What did not check out, and is left for its owner

- **`docs/working-reset/compiler-readiness-plan.md:54`–`:66` and `:184`–`:190` are now stale.** The plan
  says of `N82`: *"the reconciliation never happened … Both sites still assert the opposite of the
  ruling, one of them in bold."* That is no longer true. The plan is the live tracking document and this
  pass did not edit it; whoever moves the board should correct those two passages and re-read exit
  condition 6, which this row was the last one failing. The plan also gained an unrelated
  period-ordering passage around `:1213` from other work while this pass was running; that passage is
  not this pass's, and the two stale ones are further up the file and untouched by it.
- **The register row's own recorded reason was already wrong before this pass**, and is left as a
  correction the plan already notes: it said `temporal-type-system.md` was untouched by any slice, and
  the document was touched twice for other rows.
- **BUG-047** is named by the register row as needing a re-check against the ruling. Not done here — it
  is a bug-tracking judgement, not a document reconciliation.
- **Nothing here closes the design.** Three code blockers stand between this and a locked mechanism: the
  binding path where a declared qualifier does not constrain the value, the refusal diagnostics that
  must replace `PRE0018`'s incoherent message, and the type gate on the proof engine's relational-fact
  path. They are enumerated in the design and in § 7.4 of the adversarial review.
