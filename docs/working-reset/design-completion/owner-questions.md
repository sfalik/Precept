# Questions for Shane

Six questions need you. Each was checked against your recorded words, the want document you approved,
and canon, and nothing in them answers it. In each one the options differ in something an author writing
a definition, or a caller using one, can see. They are in the order the design work needs them. You can
answer any of them at any time, in a sentence; nothing here is on a schedule.

After the six is a list of the questions that were considered and found already answered, with the words
that answer each, so you can see what was not sent and why. It ends with four items that agents decided
were engineering, which no words of yours answer.

Short names: "the want document" is `docs/Working/what-i-want-2026-07-16.md`; "the spec" is
`docs/language/precept-language-spec.md`; "the walk" is `docs/working-reset/phase-1/the-walk.md`, the
record of the sitting on 2026-07-26 in which each ruling claimed as yours was put to you and your answer
was marked. The check behind this page is `review-confirmation/owner-questions.md`.

One caution on the examples. They use forms the spec already allows, and they describe what the design
would say under each option. None of them was run through the compiler, which cannot settle a design
question.

---

## Question 1. May a state have several entry actions, and if so, in what order do they run?

**Your earlier words.** None. The spec records it as never decided: "when a state carries more than one
state action, whether all of them fire and in what order (declaration order is the obvious candidate but
is unstated) ... Until these are ruled, no analysis may assume an ordering" (spec:2207). The walk lists
it as waiting on you (`docs/working-reset/phase-1/the-walk.md:837-839`). The order of the three phases
(exit actions, then the row's own actions, then entry actions) is already fixed and is not part of this
question. The same answer applies to exit actions.

**The example.** An author writes two entry actions for the same state:

```precept
to Review -> set Fee = 10
to Review -> set Fee = Fee * 2
```

**The options.**

1. *All run, in the order written.* The fee is 20 on entering Review. If the author swaps the two lines,
   the old fee is doubled and then overwritten, and the fee is 10. The order of lines changes the stored
   value.
2. *At most one entry action list per state.* The second line is a compile error, and the author
   combines them into one. This option also refuses the comma form combined with a single-state form,
   for example `to A, B -> ...` together with `to A -> ...`, because the spec expands the comma form into
   one declaration per state "preserving source order" (spec:968). State actions also "support an
   optional pre-verb `when` guard" (spec:968), so this option refuses `to Review when X -> ...` together
   with `to Review when not X -> ...`, an ordinary way to write two cases, unless the design counts
   guards that exclude each other as one list.
3. *All run, but two actions on one state may not touch the same field.* The example above is a compile
   error because one action writes `Fee` and the other reads and writes it. Two actions that set
   unrelated fields are allowed, and their order cannot matter.

**What waits.** The part of "what an operation is" that defines what entering a state does, and
everything that proves a rule across a state change. **What does not wait:** the ordinary path of a row
that has no state actions, which is the first section written, and the arithmetic work.

---

## Question 2. Do a state's entry actions run when an entity is created in that state?

**Your earlier words.** None. The spec: construction "names entry *ensures* at construction without
naming entry actions", and no analysis may assume "that entry actions run at construction" (spec:2207).
The walk lists it with question 1. That the state's rules must hold at creation is already settled
(spec:2090) and is not part of this question.

**The example.**

```precept
field Visits as integer default 0
state Draft initial
to Draft -> set Visits = Visits + 1
```

**The options.**

1. *They run at creation,* after the initial event's own actions. A newly created entity shows
   `Visits = 1`.
2. *They do not run at creation.* A newly created entity shows `Visits = 0`. The first later entry into
   Draft makes it 1. An author who wants the counter to start at 1 sets it in the initial event.

**What waits.** The definition of creation, and the proof that every rule holds for a newly created
entity when the initial state has entry actions. **What does not wait:** the same as question 1.

---

## Question 3. Do entry and exit actions run when a row transitions from a state back into the same state?

**Your earlier words.** None. The spec: "§2.2 composition rule 5 says the `omit` reset *does* apply to a
self-transition, but state actions are unaddressed" (spec:2207). An ordinary `no transition` is not part
of this question; it already runs "without triggering entry/exit actions" (spec:1923).

**What canon leans toward.** Two existing lines point to option 1 without deciding it. Fields a state
marks `omit` are reset "on any transition into an `omit` state (including self-transitions)" (spec:1082).
And the reason `no transition` exists is to avoid entry and exit actions, which suggests that an explicit
transition has them. Those lines are canon's, not yours.

**The example.**

```precept
to Review -> set Rounds = Rounds + 1
from Review on Revise -> transition Review
```

**The options.**

1. *They run.* Each `Revise` adds one to `Rounds`. This matches the `omit` reset.
2. *They do not run.* `Rounds` is unchanged by `Revise`. Any `omit` field on Review is still reset, so a
   self-transition would reset some data on entry and run no entry actions.

**What waits and what does not:** the same as question 1.

---

## Question 4. Is `integer` a fixed 64-bit number whose every operation must be proved in range, or a number with no upper limit? And is `decimal` without an upper limit, as your bank example appears to require?

**Your earlier words.** "I am considering arbitrary precision but no decision yet" (2026-07-20,
`inputs/history-rulings.md:160`). Those words were not recorded about `integer` alone. The note they come
from files them under the number model as a whole, meaning how Precept represents every numeric value,
under the heading "number model — STILL DEFERRED". Five weeks earlier, on 2026-06-15, a change to how
`decimal` is represented had been explored, two arbitrary-precision forms among the choices, and parked
with none chosen: "redesigning it (for example, moving to arbitrary-precision numerics) is the unmade
decision" (`docs/Working/compiler-readiness-plan-2026-06-16.md:165`). So your "no decision yet" may cover
`decimal` as well as `integer`. Canon states the open decision for `integer`: "a post-MVP decision, not
yet ruled" (`docs/language/primitive-types.md:697`). Your later "yes" on overflow was about when the
overflow proof gets built, not about which kind of number either type is (the walk, lines 394-421).

The sheet has two parts. The first is a question about `integer`. The second asks you to confirm
something about `decimal` that agents read out of your bank example and that you have not said yourself.

**Part one, `integer`. The example.** An author writes a visit counter with no upper bound:

```precept
field Visits as integer default 0 nonnegative
event Visit
from Open on Visit
  -> set Visits = Visits + 1
  -> no transition
```

**The options.**

1. *Fixed 64-bit, proved or rejected.* The definition is refused, with a message that names the missing
   upper bound. The author adds a `max` to the field or a guard to the row. Callers keep receiving a
   64-bit integer.
2. *No upper limit.* The definition is accepted as written, and overflow no longer exists for `integer`.
   Callers can receive an integer larger than 64 bits, so the type the API and the JSON form use for
   `integer` changes.

**Part two, `decimal`. Please confirm or correct.** Your approved bank example has no `integer` that
accumulates, but it has a `decimal` that does. `Balance` has no upper bound (want:34), `Deposit` adds an
amount with no upper bound to it (want:53), and you wrote "Every handler is provable" (want:98, and
want:101 for `Deposit`). Under a `decimal` of any fixed size that sum could pass the largest value the
type holds, so it could not be proved. The example therefore appears to require that `decimal` addition
cannot overflow. The two readings:

1. *`decimal` has no upper limit.* `Deposit` is accepted as you wrote it. A `decimal` can hold a value
   beyond today's 28-digit range, so the type the API and the JSON form use for `decimal` changes, and
   canon's "Overflow is an error" for `decimal` addition (`docs/language/primitive-types.md:236`)
   changes. Addition cannot round to stay in range, because "Exact stays exact" (`docs/philosophy.md:30`).
2. *`decimal` stays a fixed size.* `Deposit` as you wrote it is refused, with a message that names the
   missing upper bound on `Balance`, until the author adds a `max` or a guard. Callers keep the `decimal`
   they receive today.

The design work goes ahead on reading 1, because that is what your example says on its face. Say if that
is not what you meant. Nothing in this part is assumed to be your decision until you confirm it.

**One thing your answer does not cover.** Canon ties dates and instants to the `integer` answer: it says
that whichever way the overflow model is ruled, date and instant arithmetic follows the same answer
(`docs/language/primitive-types.md:701`; the same at `docs/language/temporal-type-system.md:626`). That
sentence changes. Dates and instants have a fixed range whichever you pick, and no unlimited form exists
for them, so arithmetic on them is proved in range or rejected either way. Your answer will not be read
as covering them.

**What waits.** The overflow section for `integer`, and anything that proves a rule about an `integer`
sum or product. **What does not wait:** the meaning of every other operator and "what an operation is".
The `decimal` work does not wait either; it proceeds on reading 1 and is redone if you choose reading 2.

---

## Question 5. May a rule mention the current time, or does time belong only in guards and actions?

**Your earlier words.** In the want document: "Do rules that reference the current moment exist at all,
or does time belong only in guards (evaluated at event time, like any other premise)? Deliberately left
open" (want:219). When it turned out that canon already answers it, you said: "it is undecided, add it to
the list" (`docs/working-reset/phase-2/five-items-walk-2026-08-27.md:121`). Guards and actions that read
the clock are not in question.

**Why it touches the philosophy.** The philosophy says "a rule whose correctness depends on when it is
evaluated says so in the definition" (`docs/philosophy.md:28`), which assumes such rules exist. It also
says a rule holds because "there is no moment at which it could fail" (`docs/philosophy.md:42`). A rule about the clock can become false while nothing happens. So two of the three options need
your approval for a philosophy edit, and no agent will make one.

**The example.** Canon's own (`docs/language/temporal-type-system.md:577`):

```precept
rule now() - IncidentAt <= '72 hours'
  because "HIPAA: incident must be filed within 72 hours"
```

**The options.**

1. *Refused.* A rule may not mention `now()`. The author moves the condition into the guards of the
   events that must happen in time, for example the guard on `File`. The philosophy sentence about rules
   that depend on the clock changes.
2. *Allowed, meaning "true at the moment each operation completes".* The compiler proves, for every row,
   that the rule is true when that row finishes. For the 72-hour rule that proof exists only if each row
   on the entity carries a guard stating the deadline, so the author writes those guards or the
   definition is refused. An incident left untouched for 73 hours is in violation with no operation in
   flight. An operation attempted at hour 73 is refused by the author's own guard on that row, which the
   runtime evaluates as it does any guard. The runtime never evaluates the rule itself and the compiler
   adds no guard nobody wrote, so this option keeps "never handed to the runtime" (want:171) and "does
   not re-evaluate them against the post-mutation configuration" (want:199). The philosophy's "no moment"
   wording changes, because the rule can be false between operations.
3. *Allowed only where it survives the clock.* The rule is proved like any other, assuming the clock
   only moves forward. A rule that gets truer as time passes, such as `IncidentAt <= now()`, is accepted.
   The 72-hour rule is refused at compile time because nothing can prove it survives time passing. For
   most deadline rules the author ends up where option 1 puts them, with no change to the language.

**What waits.** Only the part of the design that says how a rule mentioning the clock is proved, and
what the proof that rules always hold says about time passing with no operation. **What does not wait:**
every other rule form.

---

## Question 6. When a caller edits an `editable` field directly, and a rule mentions that field, what is checked?

**Your earlier words.** The want document you approved says: "For an editable-field write, it's the
field's modifier-rules *and* every rule that mentions the field — an edit is an arbitrary incoming value
that no static proof can cover, so evaluating those rules at the ingress point is exactly the premise
that closes the editable write sites" (want:143). On 2026-08-26 you had the narrower wording taken out of
canon, and the record says you "did not want to settle the underlying question now"
(`docs/working-reset/phase-2/trace-walk-rulings.md:42-46`; that record paraphrases you).

**What this sheet is asking.** Your approved document already chooses option 1 below. An earlier plan
told you your own texts "pull opposite ways" on this. They do not: the want document forbids
re-evaluating rules "against the post-mutation configuration" (want:199), and checking an incoming edit
before it is committed is a different thing, which the document itself calls an ingress check (want:129).
So the question is whether you confirm option 1 or want one of the others.

**The example.** Your bank example. `RepaymentCapPercent` is `editable` (want:36) and appears in
`rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` (want:40). An account has a limit of
2,000 and a planned repayment of 400. A caller edits the cap from 0.25 to 0.10. The new ceiling is 200,
so the edit would break the rule.

**The options.**

1. *Check the edit before it commits, against the field's own constraints and every rule that mentions
   the field.* Your example, with the presence test added to the `ReduceLimit` guard (see the answered
   list), is accepted. The caller's edit is refused with the repayment-cap rule and its `because` text,
   and the account is unchanged.
2. *Prove at compile time that no permitted edit can break a rule, and refuse the definition where that
   cannot be proved.* Your example is refused at compile time, because `positive max 1.0` cannot prove
   the cap rule holds after an arbitrary edit. The author narrows when the field may be edited. A caller
   never sees a rule failure from an edit.
3. *Refuse `editable` on any field a rule mentions, unless option 2's proof succeeds.* Your example is
   refused, and the author replaces the edit with an event that carries a guard.

No option lets an invalid version be stored. The difference is whether your example is accepted, and
whether a caller can receive a refused edit.

**Two details the design will state under option 1,** so "the value checked" is not ambiguous: an edit
that changes two fields at once is checked as one change, and a computed field derived from an edited
field is recomputed before the rules are checked.

**What waits.** The part of the design that says what is checked when a caller edits a field, and the
proof for fields a caller may edit. **What does not wait:** the checks on event arguments, the design of
event rows, and all the earlier work: what an operation is, what each operator means, and what counts as
a rule.

---

## Considered and found already answered

Nothing below is being asked. Each line in this first group gives the words that answer it. The second
group, at the end, holds four items that are decided as engineering and not by any words of yours. If you
disagree with any line, a sentence from you reopens it.

Whether `decimal` addition can overflow was on this list in an earlier version. It is now part two of
question 4, because the answer was an agent's reading of your bank example and not your words.

- **Is an event `ensure` an ingress check or a premise?** Both: it "validates the caller-provided event
  arguments" (spec:1895), and "The premises are also what the runtime enforces" (want:26).
- **Does `from any` mean one proof per source state?** You answered "yes" to "one written line, several
  paths in, one proof per path" (the walk, lines 147-151).
- **Should approximation be marked per operation instead of by type?** The philosophy you approved later
  says "visibly, in the type system and the public API. Exact stays exact" (`docs/philosophy.md:30`).
- **A non-linear rule that field bounds alone would prove.** The want document: "proven only when a guard
  or arg constraint makes that condition directly checkable" (want:135). Your example's sign proof
  through the division (want:104) is kept, and removing the cap conjunct loses only the cap proof.
- **What "every state is reachable" and "no dead ends" require.** "Every state is reachable" (want:151);
  "no dead end an entity enters and cannot leave" (`docs/philosophy.md:44`). The design proves a real
  run into each state and a real way out for every entity that can be in it. How to prove it is
  engineering.
- **Must every state be able to reach an end state?** Canon says yes: a dead end is a reachable
  non-terminal state with "no structural path to any terminal"
  (`docs/compiler/graph-analyzer.md:439`). Your own document is worded more loosely, and read alone it
  gives the other answer: it rejects "A state you can enter but never leave" (want:152), and in a pair of
  states that only lead to each other, each state can be left. So the want document's wording alone would
  accept a cycle with no end state, and canon refuses it. Canon's stricter reading is kept for two
  reasons. The first is a decision recorded as yours on 2026-07-12, in an agent's paraphrase and not in
  your quoted words: "`terminal` becomes a required declaration of intent"
  (`inputs/history-rulings.md:166`). The second is the philosophy: "Required states lie on every path to
  completion" (`docs/philosophy.md:44`), which assumes every entity has a completion to reach. What an
  author sees: two states that only lead to each other, with no route to a `terminal` state, are refused.
  The consequence most likely to surprise you: a definition with states that declares no `terminal` state
  at all has every reachable state refused as a dead end, so an entity that cycles forever, such as a
  subscription moving between Active and Suspended, cannot be written. The second outside reviewer would
  have asked you this one; the confirmation check found canon answers it. It is repeated in one line when
  you approve the design of how proofs work, and it is yours if you want it relaxed.
- **Bounds written on date and instant fields.** Canon already offers two ways that need no new syntax
  (`docs/language/temporal-type-system.md:628`). A conversation is owed only if someone proposes a new
  bound an author would write.
- **How a quantifier binds over a collection with two types.** The spec defines it: "a two-field
  projection (`.value` → `T`, `.by` → `P`)" (spec:1372).
- **`.min` and `.max` on periods pinned to one unit.** "build both"
  (`docs/working-reset/phase-2/period-ordering-design.md:85-99`).
- **The presence test missing from your `ReduceLimit` guard** (want:75, and the message at want:81).
  `MonthlyRepayment` is optional, and both lines read it without testing that it is set. For the message
  at want:81 the answer is yours: you were shown the spec sentence "An interpolation hole is a read: an
  `optional` field read at a hole enrolls a presence obligation exactly as it would in any value
  position" (now spec:1561) and answered "mine" (the walk, lines 467-480). For the guard at want:75 the
  requirement is canon's and not a ruling of yours: it is the general rule that same sentence relies on,
  that an optional field read in any value position must be shown to be set. So the guard has to read
  `MonthlyRepayment is not set or ...`. The rule itself applies only `when MonthlyRepayment is set`
  (want:40), so allowing the reduction when no repayment is planned is the only reading. The want
  document is not edited; the corrected baseline is recorded beside it.
- **Restoring data written under an older definition.** "the restore path itself expands to handle
  schema evolution" (want:141). Restoring under the same definition stays trusted and unchecked. The
  mechanics are engineering.
- **Who constructs a `Fault`.** "do we need to decide this now? seems like something we can defer until
  we start building the runtime" (`docs/working-reset/compiler-readiness-plan.md:273`). It stays
  deferred. It is the one design decision this plan leaves to the build.
- **Using earlier rows' failed guards, the current state, or a computed field's formula in a proof.**
  These differ only in how much the compiler can prove, and you ruled the design states "the full proof
  power" (`inputs/history-rulings.md:43`).
- **Correcting the memory note that still teaches the old period-ordering ruling.** Your later words
  decide its content ("mine, I ruled that we should make same-basis periods orderable",
  the walk, lines 482-504). It is corrected and mentioned, not asked.

### Decided as engineering, not by your words

These four are not answered by anything you said. Agents classified them as engineering. They are listed
separately so that classification is not mistaken for an answer of yours.

- **Must a proof name what establishes and preserves each fact it uses?** Your last words reserved this:
  "I'm not prepared to rule right now, would need more context and/or research" (the walk, lines
  201-202), and a paper was prepared for you to read,
  `docs/working-reset/phase-5/must-a-proof-name-what-holds-a-fact-up.md`. That reads as a decision you
  meant to take later, so say so if you still want it, and it becomes a seventh question. The two designs
  differ in two ways. The first is what the compiler's stored proof record contains: under one, each use
  of "this rule held before the operation" lists what made the rule true at creation and every operation
  that keeps it true; under the other, the record holds only the use, and the compiler keeps a separate
  list of what was checked. The second is how the stored record is checked: the walk says the choice
  decides "whether a definition can be checked a handler at a time or only across the whole file" (lines
  205-207). Neither difference reaches an author or a caller, for these reasons. The paper says "This one
  does not change what compiles. Both are sound, and the same definitions are accepted under either."
  Under both designs the compiler must prove that creation makes each rule true and that every operation
  keeps it true (want:18-19), so a definition that fails is refused under both, and the refusal is
  reported against the same place, the operation that could not be shown to keep the rule. Checking one
  handler at a time or the whole file is a property of the checker that reads the stored record. That
  checker reports nothing to an author. A caller meets it only when a definition is loaded, where both
  designs refuse a definition whose record does not verify (want:204). This reasoning is an agent's, read
  from those three records; the paper does not discuss where a refusal is reported. You are told the
  result in one line when you approve the design of how proofs work. It becomes a question for you if the
  finished design shows the two would report or explain anything differently to an author or caller.
- **Where must a divisor's non-zeroness come from?** At the walk you said this was "never put to me"
  (lines 388-392), and a paper was prepared for you:
  `docs/working-reset/phase-5/where-a-divisors-non-zeroness-comes-from.md`. Two non-zero values can
  multiply to a stored zero, so "non-zero times non-zero is non-zero" is false and neither option uses
  it. Under the first option a division is proved only from something declared on the divisor itself, so
  an author who divides by a computed product writes a guard in every handler that divides by it. Under
  the second the compiler may reason through the multiplication while keeping track of how small each
  value can get, and refuses only where the product could be too small to store, naming the bound that
  would fix it. Both are sound, and the paper calls the second "strictly more permissive". So the options
  differ in how much the compiler can prove, and your ruling that the design states "the full proof
  power" (`inputs/history-rulings.md:43`) is taken to close it in favour of the second. It does change
  which definitions compile, which is why it is spelled out here. The paper is about five minutes if you
  want to check that reasoning.
- **How many digits a `decimal` division keeps.** Canon gives "Full 28-digit precision" for division
  (`docs/language/primitive-types.md:239`) and half-to-even for explicit `round`. The division line is
  under review for one reason: it describes today's fixed-size `decimal`, which part two of question 4
  may change. It becomes a question for you only if the design changes the number of digits a division
  keeps from canon's 28.
- **The shape of results a caller receives** (a refused argument, a proof record that fails its check
  when a definition is loaded, where a preview result says its proof came from). You have no recorded
  words on these. Canon fixes what they must carry ("it refuses and says why", `docs/philosophy.md:46`).
  They are decided with written reasons after you approve the design of how proofs work, and shown to
  you when you approve the finished design.
