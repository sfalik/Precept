# Which questions really need Shane — confirmation against the records

Checked 2026-11-23 on branch `spike/Precept-V2-Radical-design-completion` at `5c4269bc`. Nothing else in
the repository was edited.

**What was checked.** Every disposition in `outside-review-report-2-followup-2.md` § 1 (the fifteen sheets
of the plan's § 6.2, its two proposed conversations, and the "other apparent decisions" table), against
the records it cites: `inputs/history-rulings.md`, `phase-1/the-walk.md`, `phase-2/trace-walk-rulings.md`,
`phase-2/five-items-walk-2026-08-27.md`, `phase-2/want-amendment-marking.md`,
`phase-2/period-ordering-design.md`, `compiler-readiness-plan.md` § 4, the want document
(`docs/Working/what-i-want-2026-07-16.md`, "want" below), the memory files the rulings file quotes, and the
canonical lines each disposition relies on. "Plan" means `design-completion-plan.md`; "spec" means
`docs/language/precept-language-spec.md`.

**The test applied.** A question is Shane's only if no quoted word of his, no line of the want document
(which he approved whole: *"lets assume that I didn't write it directly, but did review it"*, and he "had
already determined that he reviewed and approved everything in the want document",
`want-amendment-marking.md:21-31`), and no canonical line that neither of those contradicts already
answers it, and if the options differ in something an author or a caller can see. Options that differ
only in how much the compiler can prove are design work: he has ruled the design states the full
intended proof power (*"it describes the full proof power, is that not clear from all the forensics and
what I want"*, `history-rulings.md:43`). I also checked the reverse failure: an agent reading one of his
rulings as covering more than he said it of.

**Result.** Six questions are genuinely his: sheets 1, 2, 4, 5, 6 and 7. The plan lists seven as certain;
sheet 3 (decimal overflow) is not one of them, because his approved worked example already decides it.
Everything else is answered or is engineering. I agree with Astra's dispositions except on three points,
recorded at the end: sheet 11 is already answered by canon, the runtime's public API shapes are
engineering rather than possibly his, and the memory-file correction needs no separate permission.

---

## Part 1 — the six questions that are his

### Sheet 1. What does a direct edit of an `editable` field evaluate, when a rule mentions that field?

**Why it is open.** On 2026-08-26 he had the "and nothing wider" narrowing removed from canon and, by the
record, "did not want to settle the underlying question now"; "nothing is added that answers the
question in either direction" (`trace-walk-rulings.md:42-46`). That record paraphrases him rather than
quoting him, but it is a ruling about this exact question, and it is later than or the same day as his
general approval of the want document. It narrows that approval for this one topic.

**The plan frames it wrongly.** Plan § 6.2 row 1 says his texts "pull opposite ways" (want:143 against
want:171 and want:199). They do not. Want:143 says "For an editable-field write, it's the field's
modifier-rules *and* every rule that mentions the field … evaluating those rules at the ingress point is
exactly the premise that closes the editable write sites". Want:129 reconciles it with want:199 in the
document's own words: "the editable-field ingress evaluates them on every write (that ingress check is the
declared premise …)". Want:199 forbids re-evaluating rules "against the post-mutation configuration";
checking an incoming edit before it commits is not that. So the sheet must say plainly that his approved
document already chooses the second option, that he reserved the choice on 2026-08-26, and ask him to
confirm or change it. Presenting three options "on equal terms" with an agent's claim that his own
document contradicts itself is the failure the plan's own rules forbid.

**Options, and what an author or caller sees.** Use his own example: `RepaymentCapPercent` is declared
`editable` (want:36) and appears in `rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`
(want:40). A caller edits it from 0.25 to 0.10 while a repayment of 400 against a limit of 2,000 is
planned.

1. Evaluate, at the edit and before commit, the field's own constraints and every rule that mentions it
   (want:143). The example compiles as written. The caller's `Update` comes back with the constraint
   failure naming the repayment-cap rule and its `because`; the version is unchanged.
2. Prove the rule from the field's declared bounds plus the access-mode guard, refusing the definition
   where that cannot be done. His example is refused at compile time: `positive max 1.0` cannot prove
   the cap rule after an arbitrary edit. The author must narrow when the field may be edited, and a
   caller never sees a constraint failure from an edit.
3. Refuse `editable` on a field that a rule mentions unless option 2 succeeds. His example is refused, and
   the author must replace the edit with an event carrying a guard.

No option lets an invalid version commit; the difference is whether his example compiles and whether
callers can receive a failed edit. Astra's point stands that the sheet should also show a patch writing
two fields and a rule that reads a computed field derived from the edited one, so "the value checked" is
unambiguous (the walk recorded at item 4, `the-walk.md:166-173`, that a computed field re-derived after
the ingress check is not covered by what that check evaluated).

### Sheet 2. Is `integer` fixed width with every operation proved in range, or arbitrary precision?

**His words leave it open.** *"I am considering arbitrary precision but no decision yet"*
(`project_boundary_define_what_i_want_2026_07_16.md:163`, quoted at `history-rulings.md:160`). The walk lists
it as waiting on him (`the-walk.md:841-844`), and canon says "a post-MVP decision, not yet ruled"
(`primitive-types.md:697`). His "yes" at walk item 15 (`the-walk.md:394-421`) deferred building the overflow
proof; the walk itself says "the deferral does not decide it".

**Not widened from the decimal case or narrowed by it.** His worked example has no accumulating integer
(`Months` is only a positive divisor, want:67), so nothing in the want document decides this.

**Options, and what an author or caller sees.** A visit counter written `set Visits = Visits + 1` with no
`max`:

1. Fixed 64-bit, proved or rejected. The definition is refused, with a message naming the missing upper
   bound or guard; the author must write `max` or a guard. Callers keep a 64-bit integer at the API.
2. Arbitrary precision. The definition compiles as written and overflow no longer exists for `integer`.
   Callers can receive integers larger than 64 bits, so the API and serialization type for `integer`
   changes.

Canon ties temporal overflow to this decision (`primitive-types.md:701`, `temporal-type-system.md:626`), but
option 2 is not available for temporal values, whose range is fixed. Whichever he picks, the temporal case
is proved or rejected; the sheet should say so, so his answer is not read as covering dates.

### Sheet 4. May a state declare several entry (or exit) actions, and if so, in what order do they run?

**Open in canon and never ruled.** Spec:2207: "when a state carries more than one state action, whether all
of them fire and in what order (declaration order is the obvious candidate but is unstated) … Until these
are ruled, no analysis may assume an ordering". The walk lists it as waiting on him
(`the-walk.md:837-839`). The order of the exit, row and entry phases is not open (spec:2003-2012).

**Options, and what an author or caller sees.** `to Review -> set Fee = 10` and, separately,
`to Review -> set Fee = Fee * 2`:

1. All run, in declaration order: the fee is 20. Swap the two lines and the old fee is doubled first and
   then overwritten, so the fee is 10. Order is visible in the stored value.
2. At most one entry action list and one exit action list per state; the second declaration is a compile
   error. Note that the comma form expands into independent declarations "preserving source order"
   (spec:968) and state actions accept a `when` guard, so this option also refuses `to A, B -> …` combined
   with `to A -> …`.
3. All run, but the compiler refuses two actions on one state where one writes what the other reads or
   writes, so order cannot change the result.

### Sheet 5. Do a state's entry actions run when an entity is created in that state?

**Open.** Spec:2207: construction "names entry *ensures* at construction without naming entry actions";
"no analysis may assume … that entry actions run at construction." Listed at `the-walk.md:837-839`. That
the entry and residency ensures must hold at creation is not in question (spec:2090).

**Options, and what an author or caller sees.** `state Draft initial` with `to Draft -> set Visits =
Visits + 1` and `Visits` defaulting to 0:

1. They run after the initial event's actions: the created version shows `Visits = 1`.
2. They do not run at creation: the created version shows `Visits = 0`, and the first later entry into
   `Draft` shows 1.

### Sheet 6. Do entry and exit actions run on an explicit transition from a state back into itself?

**Open.** Spec:2207: "§2.2 composition rule 5 says the `omit` reset *does* apply to a self-transition, but
state actions are unaddressed." Listed at `the-walk.md:837-839`. Ordinary `no transition` is not in
question: it runs "without triggering entry/exit actions" (spec:1923).

**Canon leans one way, and the sheet should show it.** Spec:1082 resets `omit` fields "on any transition
into an `omit` state (including self-transitions)", and spec:1923 gives the reason `no transition` exists as
avoiding entry and exit actions, which implies an explicit transition triggers them. These lines predate
the current open question (both from April 2026) and are canon's, not his; they make option 1 the
consistent reading but do not decide it, since spec:2207 itself says it is unruled.

**Options, and what an author or caller sees.** `from Review on Revise -> transition Review`, with
`to Review -> set Rounds = Rounds + 1`:

1. Exit and entry actions run: `Rounds` goes up by one on each `Revise`, consistent with the `omit` reset.
2. They do not run: `Rounds` is unchanged, while any `omit` field on `Review` is still reset — a self-
   transition then resets some data on entry but runs no entry actions.

### Sheet 7. May a rule mention the current time, or does time belong only in guards and actions?

**His words leave it open.** Want:219: "Do rules that reference the current moment exist at all, or does
time belong only in guards … ? Deliberately left open." Reaffirmed after canon was found to answer it:
*"it is undecided, add it to the list"* (`five-items-walk-2026-08-27.md:121`). Canon's answer
(`temporal-type-system.md:568`, the example `rule now() - IncidentAt <= '72 hours'` at :577) "stands until
he rules" (`compiler-readiness-plan.md:289`). Guards that read the clock are not in question.

**Both answers touch the approved philosophy.** `philosophy.md:28` says "a rule whose correctness depends on
when it is evaluated says so in the definition"; `philosophy.md:44` says a rule holds because "there is no
moment at which it could fail". Refusing such rules needs his approval to edit line 28; admitting the
72-hour rule as written conflicts with line 44, because a case left untouched for 73 hours violates it with
no operation in flight.

**Options, and what an author or caller sees.** The 72-hour filing rule:

1. Refused: rules may not mention `now()`; the author moves the condition into the guards of the events
   that must happen in time. Philosophy line 28 changes.
2. Admitted with the meaning "holds after every operation", not at every moment. The rule compiles; an
   entity can sit in violation between operations, and the next operation is refused or proved against
   it. Philosophy line 44's "no moment" needs rewording, and the induction needs a step for time passing.
3. Admitted, but proved under a monotone clock like any other rule. Rules that only become truer as time
   passes (`IncidentAt <= now()`) compile; the 72-hour rule is refused at compile time because nothing can
   prove it survives the clock. In effect option 1 for most deadline rules, without a language change.

The third option is not in the plan's sheet, and the sheet should include it.

---

## Part 2 — answered or engineering

### Sheet 3. Decimal overflow — answered by his approved example

Want:98: "Every handler is provable"; want:101: "`Deposit` — the inductive hypothesis: `Balance + Amount`
with `Amount ≥ 0` preserves the rule *only if the rule held before the event*. No guard or arg bound alone
gets there." `Balance` has no upper bound (want:34) and `Amount` only `nonnegative` (want:53). Under any
fixed decimal range, a balance at the largest representable value plus a deposit of one cannot be proved
safe, so `Deposit` would be refused. His approved text therefore requires that decimal addition cannot
overflow. Canon's "Overflow is an error" for decimal addition (`primitive-types.md:236`) must change under
the order of authority, as it disagrees with the want document. Engineering then owes the representation
and its boundary with callers (Astra's "separate mathematical numeric meaning from bounded external
representation"). This becomes his only if the design proposes refusing `Deposit` as written. The plan's
"Nothing he has said chooses" is wrong; his integer reservation (sheet 2) must not be widened to decimal.

### Sheet 8. Must a proof name what establishes and preserves each fact it uses — engineering

His words: *"this has not been put to me plainly. I'm not prepared to rule right now, would need more
context and/or research"* (`the-walk.md:201-202`). That defers; it does not keep the choice for himself.
Both options are sound and require both proofs (`compiler-readiness-plan.md:280`); they differ in what the
certificate carries, not in any verdict. His catalog ruling (*"the idea to use the catalog was mine"*,
`the-walk.md:101-112`) gives the coverage job to the checker's own enumeration. Want:199 requires the
certificate to record "the premises used (which guard, which arg constraint, which pre-state rule)" — both
options do. Decide it at step 10 and tell him the result in one line at the proof-model sign-off, because
he once said he would want context; it reaches him as a sheet only if the two designs would give a
different explanation to an author or caller.

### Sheet 9. Is an event `ensure` ingress validation or a premise — answered: both

Spec:1895: "The constraint validates the caller-provided event arguments"; spec:2089: ingress evaluates
"the modifiers on each arg and any `on <InitialEvent> ensure ...`" and a failure "is `InvalidArgs`".
Want:22: "a modifier on an arg is a compact spelling of a precondition on its event"; want:26: "The premises
are also what the runtime enforces". An ingress check, once passed, is a premise. Residual only as Astra
describes: if an event `ensure` reads an entity field (canon does not forbid it), want:143 puts relational
conditions an event needs in its guards; a design that changes an existing form's outcome between
`InvalidArgs` and a refusal would need him.

### Sheet 10. Decimal rounding — engineering first

`primitive-types.md:239`: "Full 28-digit precision"; explicit `round` is half-to-even
(`primitive-types.md:639-643`); `business-domain-types.md:174` has authors round final billing amounts
explicitly. Derive the model; bring him only a case where the same input gives two different stored amounts
under two defensible models.

### Sheet 12. Does `from any` expand per state — answered

Asked as "one written line, several paths in, one proof per path", he answered *"yes"*; "the weaker
alternative — one proof over what holds on every path — is not taken" (`the-walk.md:147-151`). That is the
spec's second option at spec:2203 ("treat `any` as a single unit whose available facts are what holds in
every source state"), so the facts question is closed. How rows are represented and how diagnostics are
counted is engineering.

### Sheet 13. Approximation per operation instead of the exact and approximate types — answered

The only record is a memory note: "He accepted in principle a philosophy change" (2026-06-15,
`history-rulings.md:159`, "unclear — never applied"). He later approved the 2026-07-28 philosophy rewrite
(`trace-walk-rulings.md:12-25`), which says "visibly, in the type system and the public API. Exact stays
exact; approximation is declared, never disguised" (`philosophy.md:30`). The later approval governs. Only a
new proposal to change the type split would reach him.

### Sheet 14. A non-linear rule that field bounds would prove — answered

Want:135: "proven only when a guard or arg constraint makes that condition directly checkable … the author
guards them, linearizes them, or the compiler refuses them"; in canon at spec:270. This is his approved
policy, part of the intended proof power, not a limit on it.

### Sheet 15. The reachability standard — answered

Want:151: "Every state is reachable"; `philosophy.md:44`: "no position that exists on paper but that no
sequence of operations can arrive at." How to prove it is engineering. A proposal to restrict what authors
may write, if one proves necessary, would be a language change and goes through the consultation rule, not
this sheet.

### Conversation: bounds on temporal point types — conditional

`temporal-type-system.md:628` names "bound vocabulary on temporal types, guard-only closure, or a declared
representable-range axiom". The last two need no new syntax. A conversation is owed only when someone
proposes new author-written bounds, under CLAUDE.md's consultation rule; not before.

### Conversation: quantifier binding and ordering keywords — answered or not needed

Spec:1372 already defines the binding: for `queue of T by P` "the binding variable is a two-field
projection (`.value` → `T`, `.by` → `P`)", inferred, not declared. The open note at
`collection-types.md:1296` is answered by the spec ("Implicit is simpler and consistent"). The ordering
keywords question is about "future ordering features" (`collection-types.md:1300`), and no step waits on it.

### Other matters

- **`.min`/`.max` on pinned periods.** *"build both"* and *"yes lock the design and log the bugs"*
  (`period-ordering-design.md:85-99`, `:879`; `compiler-readiness-plan.md:274`). `collection-types.md:533`
  ("a separate decision, not yet taken") is stale and gets corrected.
- **The presence test missing from his guard at want:75.** His ruling on optional reads (*"mine"*,
  `the-walk.md:467-480`) requires `MonthlyRepayment is not set or …`. The rule itself applies only `when
  MonthlyRepayment is set` (want:40), so allowing the reduction when no repayment is planned is the only
  reading. With the first-match complement available, the message hole at want:81 is then provably
  present. A one-line notice, not a question.
- **Using earlier rows' failed guards, state residency, a computed field's formula, magnitude reasoning for
  divisors, NaN.** Each differs only in proof power; the walk records the canon wording as "derived" and
  the divisor case as *"never put to me"* (`the-walk.md:377-392`). Design work under the full-power ruling.
- **Restore and schema change.** Want:141: restore is not ingress; "the restore path itself expands to handle
  schema evolution". The decision register notes an editor pinned "this" to restore
  (`decision-register-FINDINGS.md:78`), but he approved the document as it stands. Canon's other position
  (`compiler-and-runtime-design.md:718`, host-managed) must change.
- **Who constructs a `Fault`.** *"seems like something we can defer until we start building the runtime"*,
  then *"yes"* (`compiler-readiness-plan.md:273`). Stays deferred; one line tells him.
- **Full proof power, design before code, using the compiler as evidence.** Answered by his 2026-11-13
  words. A machine counterexample search during the design is answered by the sequence he set.
- **Builder metadata, general dead-end definition.** No owner words; the "pending owner ruling" label came
  from an agent commit (plan § 6.2, "Not asked").
- **The bounds-qualifier example in `'each'`.** `business-domain-types.md:212` and `:642` already use
  `quantity in 'each'`. Verify the particular example; no question.
- **Sign-offs and consent.** The plan, proof-model and finish sign-offs are his (*"don't substitute
  Frank's approval for mine"*, `history-rulings.md:184`), as are any philosophy edit and any permission or
  spending change. Those are approvals, not design questions.

---

## Part 3 — where I disagree with Astra

1. **Sheet 11 is answered by canon and should not be a sheet.** Astra rules "Owner only on this additional
   requirement", reading graph-analyzer's rule as something beyond want:152 and `philosophy.md:44`.
   `graph-analyzer.md:439-451` defines a dead end as a reachable non-terminal state with "no structural path
   to any terminal" at Error, and explains "the lifecycle the author declared cannot complete from there".
   Neither the want document nor the philosophy contradicts that: want:152 says what is rejected, not what
   is accepted, and `philosophy.md:44` adds "Required states lie on every path to completion". Under the
   order of authority, canon decides where nothing above it disagrees. Send it as a one-line notice at the
   design sign-off, with the consequence an author sees: a looping pair of states with no exit to a
   terminal is refused, and a stateful precept that declares no terminal state at all has every reachable
   state reported as a dead end. It becomes his only if someone proposes relaxing canon.
2. **The runtime's public API shapes are engineering, not possibly his.** Astra says real API alternatives
   (string versus structured `InvalidArgs`, `result-types.md:123`; throw versus structured result on
   certificate failure, `precept-builder.md:641`; the type carrying inspection provenance,
   `result-types.md:250`; a caller-supplied clock) "may be his". He has no recorded words on any of them;
   canon already fixes the governing requirements ("an unexplained refusal is not an admissible shape",
   `precept-builder.md:641`; "it refuses and says why", `philosophy.md:46`; want:211 provenance). Decide them
   with four-part reasons and show them in the design sign-off read. Sending them as sheets would add owner
   input that rule 5 asks to avoid.
3. **The memory-file correction needs no separate permission.** Astra keeps "the separate permission for the
   memory correction" (plan § 5, last paragraph). The content is already decided by his later quoted words
   (*"mine, I ruled that we should make same-basis periods orderable"*, `the-walk.md:482-504`; *"build
   both"*). Correcting a memory note to match his recorded words is housekeeping; mention it in the batch,
   do not ask.

## What this check did not cover

It did not reread all of canon, did not check every line number in Astra's report outside § 1 (one small
offset noticed: Astra cites spec:1922, the text is at :1923), and did not test any design option against
the compiler, which the owner's rules exclude as evidence.
