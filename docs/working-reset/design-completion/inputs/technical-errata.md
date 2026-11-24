# Errata for the three technical input files

Written 2026-11-23 against `5c4269bc`. The three files are `technical-proof-model.md`,
`technical-precedent.md` and `technical-canon-consistency.md`, all in this folder. They are kept unedited
as the record of what was read. This file lists every statement in them that a check against the
repository found wrong or too broad, with the correction and its evidence. The design-completion plan
cites this file wherever it relies on one of the three, and an entry here outranks the sentence it
corrects.

Source of every entry: `../review-confirmation/technical.md`, which re-read each cited line, and the
second outside reviewer's audit (`../outside-review-report-2-followup-2.md` § 2). Nothing here comes
from running the compiler, a test or a sample file. Short names: "want" is
`docs/Working/what-i-want-2026-07-16.md`; "spec" is `docs/language/precept-language-spec.md`; other
documents are named by file.

The inputs are lists of things to check. They are not three independent confirmations of one another,
and a statement in them that is not listed below has not thereby been verified.

---

## 1. `technical-proof-model.md`

### 1.1 A product with a field the handler does not write is not linear

**The input says** (:331): "A product with a constant, or with a field that no handler in question
writes, is linear for that handler's purposes."

**Correction.** A product with a known constant is linear. A product of two fields is not, even when
the handler writes neither or only one: the unwritten field still takes a different value in each
configuration the proof must cover, so it is an unknown and not a coefficient. Every rewrite the design
permits must say whether a coefficient is known, unchanged across the handler, or only bounded.

**Evidence.** The proof of a handler ranges over every configuration in which the rules hold, not over
one. Second reviewer: "An unchanged rate varies across legal entries; amount times rate is not jointly
linear" (`../outside-review-report-2-followup-2.md` § 2, row for :331-332).

### 1.2 Two non-zero `decimal` values can multiply to a stored zero

**The input says** (:332): "A divisor that is a product is non-zero when both factors are (true for
integer and decimal; false for `number`, primitive:667)."

**Correction.** It is false for any arithmetic with a smallest positive value, `decimal` and the
decimal-backed business types included. With at most 28 fractional places, `0.0000000000000001` times
itself is `10^-32`, which is stored as zero. For `integer` the mathematical statement is true, but a
fixed-width product must separately be shown not to overflow. A proof that a divisor is non-zero needs a
bound on magnitude, not only the signs of the factors. A conclusion of `nonnegative` survives rounding to
zero; a conclusion of "non-zero" or `positive` does not.

**Evidence.** `docs/working-reset/phase-5/where-a-divisors-non-zeroness-comes-from.md:17-21` gives the
decimal case. The earlier plan warns "the underflow is not confined to `number`"
(`docs/working-reset/compiler-readiness-plan.md:281`). `docs/language/primitive-types.md:239`: "Full
28-digit precision".

**The same flaw in canon.** `docs/compiler/proof-engine.md:1475-1476` states "Multiplication: positive ×
positive = positive" and "Division: positive ÷ positive = positive". Under the same rounding a quotient
or product of two positive values can be stored as zero. Those two lines are among the passages the
inference-step design replaces. (The confirmation file cites them as :1473-1474; the text is at
:1475-1476.)

### 1.3 The writer list has three destinations for a dequeue, not two

**The input says** (:132): "including the second field written by `dequeue … into` (spec:1997)".

**Correction.** `dequeue Q into D by S` writes the collection, `D`, and `S`. Write destinations are
derived from every permitted form of every action, optional captures included, not one entry per action
name.

**Evidence.** `docs/language/collection-types.md:1205-1210`: "`-> dequeue ClaimQueue into CurrentClaim by
CurrentSeverity`" and "`CurrentSeverity` receives the dequeued element's ordering value". Spec:1990-1999
lists only the second field.

### 1.4 Reachability, inspection and restore are not "Full"

**The input says** (:253): "Reachability, dead ends, dominators — Full (spec:184-189, graph:76-88)."
And (:293, :295): "Inspect an event without firing it | Full" and "Restore with structural checks only |
Full".

**Correction.** None of the three is complete.

- *Required states.* Spec:430 defines `required` as "all initial→terminal paths visit this state
  (dominator)". `docs/compiler/graph-analyzer.md:489-496` checks only that the state "must dominate at
  least one terminal state". If Start reaches Paid only through Review and reaches Cancelled directly,
  the graph document's check passes and the spec's rule is broken. `docs/philosophy.md:44`: "Required
  states lie on every path to completion — no entity skips a mandatory step." The spec's wording is the
  target; this is a conflict to repair and not a question for the owner.
- *Reachability and dead ends.* See 2.1 below.
- *Inspection.* `docs/runtime/evaluator.md:837-843` runs a row's actions and recomputes computed fields
  for every row whose guard is not impossible, and only at :864-866 marks the row impossible because
  "An earlier row already wins". Spec:2006 says "the first matching guard wins and the remaining rows
  are not evaluated", and spec:2167 requires the fault guarantee to hold "during inspection exactly as
  during execution". So preview can evaluate a division whose proof assumed an earlier guard had failed.
- *Restore.* Want:141 trusts restored data "because it was governed when it was written".
  `docs/runtime/runtime-api.md:906` says a host restoring data written under an older definition "has
  stepped outside the guarantee, and nothing inside the engine will notice". A definition's certificate
  says nothing about which definition governed the stored data, so a rule tightened in a new definition
  is not established for old data.

### 1.5 The value of a field with no default is stated in canon

**The input says** (:122): "Whether that is the language's meaning or a convenience of the fold is not
stated anywhere I found."

**Correction.** Spec:2085 and :2094-2097 refuse construction without the required assignments and call an
early read one that "would observe an undefined value". The zero used when folding constants is not the
language's meaning, and an establishment proof may not use it as the entity's starting value.

### 1.6 Some periods are ordered

**The input says** (:183): "periods have no ordering (temporal:1485)".

**Correction.** Two periods pinned to the same single unit are ordered
(`docs/language/temporal-type-system.md:1329`: "Both operands must be pinned to the same single unit";
also :1483). The owner said "build both" for the scalar comparison and the collection accessors
(`docs/working-reset/phase-2/period-ordering-design.md:85-99`).

### 1.7 "Unresolved" cannot always be turned into a counterexample

**The input says** (:314): "for this class, 'unresolved' need never mean 'the compiler did not find it';
it can always mean 'it is not true, and here is a case'."

**Correction.** That holds over the rationals with premises that are consistent. It fails in three
cases. For integers, `2x <= 1` implies `x <= 0`, and the rational point `x = 1/2` is not a
counterexample (the precedent file gives this case itself at :92). When the premises contradict each
other, the conclusion holds and no violating case exists. At a search cap, spec:225 requires an honest
unresolved verdict. A violating assignment may also be one no run can reach, so it is a counterexample to
preservation from the stated rules and not a reachable business violation.

### 1.8 Identical text is not a proof by itself

**The input says** (:339): "Proofs by identical expression are sound under any deterministic arithmetic."

**Correction.** Only when every operand has the same value at both evaluations. Exit actions and earlier
actions in the chain run between the guard and a later write (spec:2007-2010). A guard that compares an
amount with the current cap proves nothing about the same text after an exit action has changed the cap.

### 1.9 The decimal example does not decide the integer model

**The input says** (:353): "So either the representation removes overflow for decimal and integer, or the
worked example and its acceptance are wrong as written."

**Correction.** The want document's `Deposit` adds two `decimal` values (want:34, :53-55, :101). It is
evidence about `decimal` only. The want document has no accumulating `integer`. The owner's words, "I am
considering arbitrary precision but no decision yet" (`history-rulings.md:160`), were recorded about the
number model as a whole and not about `integer` alone, so they may cover `decimal` too. The reading that
`Deposit` requires `decimal` addition never to overflow is an agent's, and question 4 of
`owner-questions.md` asks him to confirm it.

### 1.10 An element rule does not survive a removal "for free"

**The input says** (:369): "`each P` survives a removal for free ... `any P` survives an insertion for
free".

**Correction.** Only when the surviving elements, every other value `P` reads, and the rule's
applicability are unchanged by the whole action. A dequeue also writes its destinations (1.3). In the set
`{1, 2}` every element is at most the count; remove 1 and the remaining 2 exceeds the new count. The
preservation table must describe the whole action and what the predicate reads. A table keyed only by
action name and predicate form, which is how `technical-precedent.md:215` frames it, has the same gap.

**Evidence.** `docs/language/collection-types.md:826-842` lets an element predicate read the collection
or another field.

### 1.11 An edit is not the only place a rule is evaluated

**The input says** (:404): an edit "is the one place where a rule is evaluated at all".

**Correction.** Want:143 also has an argument's modifier-rules evaluated at ingress ("For an arg, that's
its modifier-rules"), and want:22 says a modifier on an argument is a rule.

### 1.12 A guard that closes a failed proof does not always exist

**The input says** (:412): "so a guard that closes the proof always exists and can be printed."

**Correction.** For an action that unconditionally assigns −1 to a `nonnegative` field, substitution
gives false. The only guard that closes the proof is always false, which makes a dead row, and canon
rejects dead rows (`docs/compiler/diagnostic-system.md:189`, `UnsatisfiableGuard`). The design promises a
suggested premise only when one is expressible, consistent with the rules, and would close the proof.
Otherwise it names the assignment or rule that must change.

---

## 2. `technical-precedent.md`

### 2.1 Satisfying an exit guard does not show a state can be left, and "not provably unreachable" is not "reachable"

**The input says** (:228): "to promise a state can be left, the compiler has to exhibit an input that
satisfies some exit row's guard". And (:226): "so that 'every state is reachable' is read as 'no state is
provably unreachable from what was declared'".

**Correction.** Both are weaker than what the owner approved. Want:151: "Every state is reachable ... A
state no path reaches is rejected." Want:152: "A state you can enter but never leave is rejected."
`docs/philosophy.md:44`: "no dead end an entity enters and cannot leave." An application that enters
Review only with approval false, where nothing in Review changes approval and the only exit requires
approval true, passes the input's test (the assignment approval = true satisfies the guard) and can never
leave. The design target is the standard in `../outside-review-report-2-followup.md` § 3: a feasible
finite run from a valid construction into every declared state, and from every reachable configuration in
a non-terminal state a feasible finite continuation to a different state. The impossibility result the
input cites concerns unbounded counters; it does not change which result may be certified.

### 2.2 Linear reasoning has a third outcome

**The input says** (:17): "There is no third outcome."

**Correction.** See 1.7. Integer-only consequences, contradictory premises and the search cap are each a
third outcome.

### 2.3 The premises proofs cite are not the whole set of run-time checks

**The input says** (:136): the run-time list "follows from the certificate format — collect every premise
cited by any certificate".

**Correction.** Two duties, both required. Every declared ingress constraint is enforced: want:143 ("For
an arg, that's its modifier-rules"), want:165 ("ingress validation of args and editable fields, guards"),
and want:129 names a declared bound that "no *other* proof in the file consumes". And every premise a
proof uses is justified (want:210). An argument declared `positive` that no proof uses must still refuse
minus one. If two valid proofs of one definition cite different bounds, the checks may not depend on
which proof the search found. The two duties can share one record; the checker must detect a missing
ingress check that no proof relied on.

**Evidence.** `docs/runtime/evaluator.md:1366-1369` evaluates every declared constraint on every supplied
argument.

### 2.4 Temporal overflow has premises to prove from

**The input says** (:197): "Overflow of temporal values has no premise to prove from".

**Correction.** `docs/language/temporal-type-system.md:628` and `docs/language/primitive-types.md:701`
both name "guard-only closure", and the first also names a declared representable-range fact. What is
missing is a bound written on the field. Neither alternative needs new syntax.

### 2.5 The cost claim measures a different program

**The input says** (:239): "exact linear reasoning is affordable on every keystroke".

**Correction.** The 44 ms figure it cites measures today's compiler on existing files, not the proposed
prover and checker. Substituting `n` assignments that each double a variable gives `2^n` occurrences of
it. The design owes written cost bounds and cap behaviour; measurement belongs to the build.

### 2.6 The diagnostic cannot always tell a non-inductive rule from a missing guard

**The input says** (:254): "the design must say so, distinguish that diagnostic from a missing guard".

**Correction.** A failed preservation step yields a configuration the declared rules allow, which need
not be reachable (the input's own :67). From that alone the compiler cannot know whether the author left
out a supporting rule or wrote an unsafe handler. The design separates three results: a run that can be
replayed from creation, a counterexample to preservation from the stated rules, and a search that
reached its cap. It does not claim to know the author's intent.

### 2.7 Not independently verified

The input's references to Ivy's restrictions, Dillig's optimality result, Necula's generation procedure
and a complexity bound for quantifier-free sets were not checked against the originals by either
reviewer. They are leads. A search that found no verifier for base-10 rounding is not evidence that none
exists.

---

## 3. `technical-canon-consistency.md`

### 3.1 The conflict inventory is incomplete, and 58 is not a count of independent questions

**The input says** (:232): "Total open | 58". And (:243) that the type documents and eleven others "were
searched, not read".

**Correction.** The inventory is an index to start from. Conflicts it misses, each checked at its line:

- `docs/language/business-domain-types.md:1917` says the value types have "No arithmetic operators, no
  validation logic"; :2016-2018 says to "surface a `ConstraintViolation`" and to "Compute via the typed
  value object's operator overload".
- :1823 calls `QualifierMismatch` "the proven-violation-only policy"; :1826 says "Without the guard,
  `QualifierMismatch` fires (unproven …)".
- :1878 enforces the implicit `positive` on exchange rates at "`set` time (runtime)", against want:165's
  "no re-checking of proven bounds on writes".
- :1719 says "No dedicated `units { }` block syntax"; :2037 relies on an "entity-scoped units block".
- `docs/runtime/precept-builder.md:771` says "entity state is a contiguous `object?[]`" and :819 shows
  `new Stack<PreceptValue>()`; `docs/runtime/evaluator.md:223` uses `PreceptValue[]` and :543-548 says "no
  heap allocation per evaluation".
- `docs/runtime/runtime-api.md:770-791` lists `list of T keyed by K` and `map K to T`, which are not
  current collection names. `stateref` in the same table is a real internal type
  (`docs/compiler/literal-system.md:234`) and stays.
- `docs/language/precept-grammar.md:855` calls the fault catalog "defensive coverage for paths the
  compiler proved safe", against want:165's "fault-prevention checks are omitted at evaluation time".
- `docs/compiler/soundness-and-coverage.md:200-201` says the lists "are closed at those source files",
  which the catalog notice in `CLAUDE.md` contradicts.
- The `required` conflict in 1.4.
- The owner's deferral of who constructs a `Fault` is "until we start building the runtime"
  (`docs/working-reset/compiler-readiness-plan.md:273`). Four canonical places say "until the runtime
  design is done": `docs/runtime/README.md:36`, `docs/runtime/fault-system.md:320`,
  `docs/runtime/evaluator.md:1629` and `:2015`.
  The choices about what context a `Fault` carries depend on that deferral ("there is nothing to decide
  until Q3 settles", `docs/runtime/fault-system.md:310`). They stay conditional and are recorded open
  beside it; sorting the deferrals must not settle them.

The total of 58 counts evidence rows, and the input itself folds some into others (:115). It does not
size the work or the owner's reading.

### 3.2 Only want:143 states the wider edit check

**The input says** (:38): "want:106 and want:129 repeat it."

**Correction.** Want:106 says an edit is "validated against the field's modifier-rules". Want:129 says
"the editable-field ingress evaluates them on every write". Neither mentions every rule that mentions the
field. Only want:143 does. The owner reserved that question on 2026-08-26
(`docs/working-reset/phase-2/trace-walk-rulings.md:42-46`).

### 3.3 Ten bank rows do not lack a named diagnostic

**The input says** (:52): "Ten of the fourteen rows in the want document's rejection table (want:114–123)
therefore have no named diagnostic."

**Correction.** `DivisionByZero` and the bounds codes exist (`docs/compiler/diagnostic-system.md:181`).
What is missing is a diagnostic for a handler that does not preserve a relational rule. Two of the rows
(want:121 and want:123) describe one edit, the removal of the restating cap conjunct. Each mutation is
mapped to the proof it loses and to the diagnostic proposed for it.

### 3.4 The dataflow exclusion and the induction do not conflict

**The input says** (:162): "The exclusion and the induction paragraph forty lines below it (`:2612`)
cannot both stand."

**Correction.** `docs/compiler/proof-engine.md:2606` excludes "tracking values across multiple
transitions". The inductive step is one handler applied once (:2612) and needs no such tracking. The real
gap is at :2614: "there is no general carrying of 'every rule held before this operation' into a
handler's discharge". The exclusion must not be used to forbid the reasoning across several operations
that the reachability standard in 2.1 needs.

### 3.5 Logarithmic units are not the only inexact case

**The input says** (:163): "Logarithmic units are 'outside that proof guarantee' with no stated outcome".

**Correction.** Two more cases are inexact. `docs/language/catalog-system.md:1209`: `ScaleIsRational` "is
`false` for units whose scale-to-base is irrational — the plane-angle family". And
`docs/language/business-domain-types.md:1848` says a duration "always yields an exact conversion to any
time unit", while :1853 and :2010 specify the conversion as a division; one second in minutes is 1/60,
which has no finite decimal form. The arithmetic model distinguishes identity conversions, exact rational
factors, approximate factors and rounded results, and states an outcome for each.

### 3.6 Canon is not silent on an overflowing instant

**The input says** (:170): a clean definition "can still overflow when an expression is evaluated" and
"no document says what the runtime does then".

**Correction.** `docs/language/temporal-type-system.md:626` says an out-of-range instant operation
"faults at runtime". That is not an acceptable answer under want:171, but it is a statement to replace,
not a silence.

### 3.7 An unmatched construction has a reason in canon

**The input says** (:188): "`Rejected` carries an authored reason ... and in this case no author wrote
one."

**Correction.** `docs/runtime/runtime-api.md:169` gives the synthesized reason "No construction row
matched arguments", and `docs/tooling/mcp.md:55-57` plans `NoMatchingRow`. The real question is whether
this caller gets "not offered" or an explained refusal (want:154).

### 3.8 Strategy numbering is not mechanical if a caller can see the numbers

**The input says** (:109): "**Mechanical** once one is picked."

**Correction.** `Prospect` is numbered implicitly from zero in `docs/runtime/result-types.md:171` and
explicitly from one in `docs/runtime/evaluator.md:402`. Where a caller can see a number, choosing it is a
design decision with reasons. Only the prose that follows is mechanical.

---

## 4. Checked and found not to be errors

So that nobody re-opens them: `technical-proof-model.md:129-133` (the next bullet already requires a row
entering a state to establish that state's ensures); :457 (a list entry setting two spec lines against
each other, asserting neither); `technical-precedent.md:87` and :252 on linear multipliers (a candidate
record, not a mandate); :40 and :180 on exact rationals (true of the prover's own arithmetic, which is
all they claim); `technical-canon-consistency.md:67` on the checker and :51, :86 on state ensures at
construction (real gaps, correctly described).
