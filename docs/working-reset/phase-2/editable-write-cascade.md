# The editable write, and what has to happen when a rule reaches it

**Status**: Verdict — 2026-07-27, at HEAD `7bff9ee6`
**Question**: someone edits an editable field to a value that is legal for the field and makes a rule
false. The rule may name the field directly, or may name a computed field derived from it, through
several steps. What must the engine do, and what must the compiler do?

**Evidence**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`,
`docs/working-reset/phase-1/the-walk.md`, and `docs/working-reset/phase-1/want-document-provenance.md`
for deciding which lines of the want document are yours. No catalog, no sample, no compile.

---

## 1. The answer in three sentences

**Nothing has to be evaluated after the value lands.** For a direct edit the configuration the
operation would commit is a function of the state before the edit and the patch alone — no dispatch,
no actions, and computed fields cannot form cycles — so every rule the edit could falsify, including
one that reaches the field through a chain of computed fields, can be evaluated *before* anything is
written, by substituting the incoming value into the derivation chain.

**What the evidence does not settle is whether the engine evaluates those rules at all.** Your own
sentence at `want:143` says it does; `want:199`, `:171` and `:173` say the runtime holds no rule by
evaluating it; both are your words, they are fifty lines apart in one document, and nobody has put
them side by side to you.

**And the part you are worried about is real, but it is not about timing.** If the engine evaluates
those rules and the compiler cites that evaluation as what discharges the obligation, then on the edit
route no definition is ever refused because of an edit and the rule is held by a runtime check rather
than by a proof — which is the substance of what you removed, moved earlier and narrowed to one
operation. If instead the engine checks only the field's own declared modifiers and the compiler must
prove the rule holds for every value those modifiers admit, then it is not the sweep, and the price is
that definitions using `editable` alongside a relational rule stop compiling.

---

## 2. Where the five readings agreed

Five people worked this from different starting points — one reading only your want document, one
reading only the canonical documents, one working out what is mechanically necessary, and two arguing
opposite sides deliberately. Each was then challenged by someone trying to break the result. Four
things came out the same regardless of starting point, and that is the strongest evidence in the set.

**One — an editable write is a write site, and it carries preservation obligations.** Every reading
reached it, and it is not an inference: walk item 35 records you being asked plainly whether a rule
must be preserved wherever a field it mentions is written, "including construction rows, state actions
and editable-field writes", and answering "yes". Your own `want:20` says obligations attach to every
write site of every field a rule mentions. This is closed.

**Two — the obligation reaches through computed fields.** A rule that names `X` where `X <- f(F)` is a
rule that writing `F` can break. Walk item 8 lists this as one of the three routes by which a rule
"mentions" a field, and canon states the same expansion independently at `precept-language-spec.md:1968`
("with transitive expansion to the concrete stored fields they depend on"). Item 8's tick is marked on
the activation-guard route specifically; the computed route "stands as derived" rather than separately
answered, but nobody found an argument against it and the soundness reason is direct.

**Three — a pass over the finished working copy is not coming back.** No reading proposed one. The
disagreement is entirely about what happens *before* the write.

**Four — the cascade is finite and computable at compile time.** Computed fields cannot form cycles
(`spec:1359`, `:1745`) and there are no loops (`spec:158`), so the set of computed fields an edit can
change, and the set of rules those touch, is a fixed property of the definition text.

**What the agreement does not cover.** It establishes that an obligation exists at the edit write site.
It says nothing about what *discharges* it. That is the whole of the remaining disagreement, and no
amount of agreement on the four points above touches it.

---

## 3. Where they disagreed, and what I think is better supported

### 3.1 Does ingress at an editable write evaluate anything wider than the field's own modifiers?

Two readings said yes (following `want:143`), three said no (following canon at HEAD). Three separate
arguments were offered for "no". Two of them do not survive, and the third is weaker than it looks.

**The mechanical argument for "no" fails.** It runs: ingress is execution phase 1, computed fields are
re-derived at phase 7, so a check at phase 1 that reads a computed field reads a value the operation is
about to replace — therefore a correct check must sit after phase 7, therefore it is the sweep. This is
wrong, and the error matters because it was presented as forced by the phase order rather than chosen.
At phase 1 the engine holds the incoming value and the defining expression of every computed field.
Substituting the incoming value into those expressions terminates, because they cannot form cycles, and
yields exactly the values phase 7 would produce. A rule reaching the field through five derivation
steps can be rewritten into a predicate over stored fields and evaluated at phase 1 with nothing
written. So the phase order does not force anything. Whether the engine *should* run such a check is a
decision, not a consequence.

**The canon argument for "no" is not independent evidence.** Canon at HEAD says it three times, and I
checked where each sentence came from:

| Sentence | First appears |
|---|---|
| `spec:274` — "only the field's own declared contract … and nothing wider" | `5e853c0e`, 2026-07-27 |
| `spec:1976` — "there is no second pass over the finished working copy" | `5e853c0e`, 2026-07-27 |
| `runtime-api.md:402` — "an editable field is an ordinary write site" | `c3a9be21`, 2026-07-27 |

All three were written yesterday, by the phase-2 correction pass, to implement this reading. And the
sentence `5e853c0e` replaced was the *old* model — "Because mutations execute on a working copy that is
discarded if any constraint fails (§3A.4), an invalid configuration never persists" — which rested on
the sweep you removed. So canon has never held an independent position on this question. Before
yesterday it said the sweep; since yesterday it says nothing wider; there is no third version to appeal
to. Any argument of the form "canon already answers this" is quoting a document written to answer it
one way.

**Walk item 16 does not settle it either.** It is the strongest ruling in the record and three of the
five readings used it, but the worked case put to you was "an account with `rule Balance >=
-OverdraftLimit`, both values supplied from outside, and **an event that lowers the limit**". That is an
event handler, where a `when` guard is available to close the proof, and the option you rejected was
"the runtime evaluates the rule **on every operation** and discards the change if it breaks" — the
post-mutation pass. Neither the case nor the rejected option is a direct edit. Item 16's own answer
text draws the distinction explicitly: "evaluating every rule mentioning an editable field at the
editable write **is a premise you name** (want:143); re-evaluating rules against the post-mutation
configuration on every operation is not — you deny it outright (want:199)." Reading item 16 as closing
the edit case reads it against its own words.

**On the other side, one argument that was used for "yes" also fails, and it should come out of
circulation.** Both the second independent re-check (`recheck-second-pass.md` § 4.8) and one of the
challenges argue that your own worked example forces the wide reading, because `want:36` declares
`RepaymentCapPercent … editable` and `want:40` states a non-linear rule over it, so under canon your own
example would not compile. Those two lines are not yours. `want-document-provenance.md` traces both to
`73f4db7d`, the Copilot-co-authored amendment of 2026-07-19, and lists them among the eleven lines it
declares unsafe to quote. Your original example declares exactly one editable field,
`DailyWithdrawalLimit`, and **no rule statement mentions it** — `want:129` says so in your own words
("no *other* proof in the file consumes them"). That cuts both ways and both should be said: it removes
the counterexample, and it also means the wider half of `want:143` is never exercised by anything you
wrote. Your narrower description of the same field at `want:106` — "writes are validated against the
field's modifier-rules at the ingress point" — is what your example actually shows, and for that field
the wide reading and the narrow reading coincide.

**Where that leaves it.** `want:143` is yours, unedited in substance (`72c2e183:131`; the later pass
changed "door" to "ingress" and nothing else). `want:199`, `:171` and `:173` are yours. They pull
against each other on this one operation. Canon cannot break the tie because canon is downstream of the
tie. This is unresolved, and only you can resolve it.

### 3.2 If the engine does evaluate those rules, is that the sweep returning?

Here I think one side is clearly better supported, and it is the side against the reading I would
otherwise have found sympathetic.

The argument that a check at the write is *not* the sweep rests on its timing and its failure
behaviour: it runs before anything is applied, it judges the incoming value rather than a finished
working copy, and it refuses rather than building an invalid configuration and discarding it. All true.
But the same argument's own strongest technical point destroys it. To cover a rule that reaches the
field through computed fields, the check must compute the configuration the operation would commit. Once
it does that and refuses when the rule is false, the compiler's remaining obligation — "the check
passed, and the pre-state rules held, and the field's modifiers hold, therefore the rule holds at
commit" — is the identity. It closes for every rule the check evaluates, linear and non-linear alike,
because a product of two numbers you have in hand is not hard to evaluate; non-linearity obstructs
*static* proof, not arithmetic. So no definition is ever refused on account of an edit, and for the
edit route the compiler has proven nothing about any relational rule. What holds the rule is the check.

One thing does survive the collapse and it is worth stating fairly, because it is the only real
difference. The compiler still owes a genuine proof: that the set of rules the check evaluates is
*complete* — that no rule the edit could falsify was left out. That is the cascade computation in
section 5, and it is not trivial. The sweep never needed it, because it evaluated everything. So the
two are not literally the same machine.

But that difference is about how the engine knows what to check, not about what holds the rule. On the
edit route, under this reading, a rule is held because the engine evaluated it and refused — which is
`want:199`'s subject, `want:171`'s "no deferral", and the sentence at `philosophy.md:33` that "nothing
re-evaluates the rules after the change, and nothing needs to". You should not adopt the wide reading
believing it leaves the proof model intact. It does not. It is defensible on its own terms — it is your
sentence, and it buys authors a great deal — but adopt it knowing that on this one route the guarantee
is carried by a check rather than by a theorem.

### 3.3 What is left genuinely open, and what would settle it

Not "more reading". Every document has been read against this and the two that could settle it
disagree with each other in your handwriting. What settles it is you answering the question in
section 6. There is no document that can be consulted instead, and the phase-2 correction pass has
already written one answer into canon, so leaving it unanswered means the written answer stands by
default.

---

## 4. What it costs, each way

### If the engine checks only the field's own declared contract

**What stops compiling.** Any precept where a field is declared `editable` and a rule relates it to
another field, unless one of these closes the proof: the field's own declared bounds make the rule true
for every value they admit; a per-state guarded access mode (`in S when G modify F editable`,
`spec:1079` rule 4a) pins the other operands so the bounds suffice; or the field's admissible values are
few enough to check one at a time — a boolean, an enum, a small bounded integer. Outside those,
`field A as decimal positive editable` with `rule A <= B` is refused, and so is anything of that
shape. The common domain case — an expert adjusts a limit, and a rule relates that limit to something
else — is refused unless the author does extra work. That is the real cost and I do not want to
under-state it.

**What has to be built.** The cascade computation in the compiler: from each editable field, the
forward closure over the computed-field dependency graph, then the rules reaching any of those through
condition, activation guard, or computed reference. A preservation obligation created at the edit write
site — `soundness-and-coverage.md` § 3.2 enumerates where obligations are created and has **no row for
the update patch**, so that has to be added. And a rejection message that names the bound or the guarded
access mode that would close it, per `want:179`.

**What the author writes.** A tighter bound that entails the rule; or a guarded access mode pinning the
other operands; or they drop `editable` and route the change through an event with a `when` guard. The
event route always works and costs one event declaration. This is the same trade you already accepted
at walk item 16 — "more definitions are refused, and authors write guards they did not have to write
before" — extended to one more operation.

### If the engine evaluates the rules the edit can falsify, before applying it

**What stops compiling.** Nothing that compiles today, on account of an edit. That is both the appeal
and the thing to be suspicious of.

**What has to be built.** The same cascade computation — it is needed either way, because the engine
has to be told which predicates to run. Plus: the compiled definition must carry, per editable field,
the rules rewritten into predicates over stored fields with the derivation chain substituted; the
engine must evaluate them at phase 1; the refusal outcome has to widen — `spec:1925` currently defines
the ingress failure as a value violating "a constraint declared on the slot it enters", and refusing a
patch because a rule about some *other* field came out false is not that, even if the outcome keeps its
name; and the certificate has to record both the check set and the completeness proof, per `want:210`.

**What the author writes.** Nothing.

**What you give up.** `want:171`'s no-deferral on the edit route. `want:173`'s uniform guarantee
survives only in the author-facing sense you meant, with two mechanisms underneath, one of which is the
engine deciding at runtime whether a rule holds. And `philosophy.md:33` needs a rewording, since the
check does evaluate a rule against a configuration it computed — earlier than the sweep did, but it
evaluates it.

---

## 5. What the mechanics establish, whichever way you rule

None of this is a matter of intent. It is what the language's own structure forces.

**The cascade.** Let there be an edge from stored field `F` to computed field `C` whenever `C`'s
defining expression names `F`. Edit `F`. The computed fields whose values can change are exactly those
reachable from `F` by one or more edges — a computed field not reachable from `F` reads nothing that
moved. The rules whose truth can change are exactly those naming `F` or any reachable computed field,
through the rule's condition, its activation `when`, or a computed reference. No other rule can move,
because evaluation is deterministic and it held before.

**Ordering is forced in exactly one place.** Inside recomputation: if `C2`'s expression names `C1` and
both are reachable, `C1` must hold its new value first, or `C2` is derived from a stale operand. So
recomputation proceeds in a dependency order of the graph restricted to the reachable set. Such an order
exists precisely because the graph is acyclic. Nothing else is ordered — rules are predicates, they
read and write nothing, so once their operands are settled they can be evaluated in any order.

**Two canonical gaps found while establishing that, both verified today.** First, **no document says
recomputation happens in dependency order.** `spec:2011` says only "every computed field is re-derived";
`precept-builder.md:386` declares `ComputedSlots` as a bare array of slot indices; `evaluator.md:502`
says "For each slot in `SlotLayout.ComputedSlots`". A grep for dependency or topological ordering across
`docs/language/`, `docs/runtime/`, `docs/compiler/` and `compiler-and-runtime-design.md` returns nothing
on this. Since a computed field may name another computed field, this is mechanically required and
unwritten. Second, **two documents state different scope rules for computed expressions.**
`spec:1359` and `:1362` allow any field that would not form a cycle; `name-binder.md` § 7.5 treats a
reference to a field declared *after* the current one as unresolved, "enforcing the rule that computed
fields may only reference fields declared before them". Under the name-binder's rule, declaration order
is already a valid recomputation order and no sort is needed. Under the spec's rule it is not. Which one
is intended decides whether recomputation needs a sort, so it is worth settling before that phase is
built.

**All of the scheduling is compile-time.** The dependency graph, the reachable set for each editable
field, the rule set for each, the recomputation order, and whether each rule is provable — all fixed by
the definition text. The language admits no dependency edge whose existence depends on a value: a
conditional inside a computed expression contributes all of its operands, which is the same conservative
reading the cycle check already uses. Only the arithmetic is run-time. This is the single assumption the
rest rests on, and it holds only while the language has no construct that makes "which field does this
one read" a data question.

**One property that belongs to `Update` alone.** Between phase 1 and commit, an `Update` runs only the
patch and recomputation — no dispatch, no exit or entry actions, no state change
(`runtime-api.md:400`, `spec:2003`–`:2012`). So the configuration it would commit is a closed-form
function of the state before and the patch. `Fire` has no such property: dispatch, actions and the state
change intervene, so a check before them would have to guess. That asymmetry is why a check at the write
is even conceivable for edits and is not for events — and it is also why the collapse in § 3.2 bites
here and nowhere else.

**How it grows.** Per edit, recomputation costs the summed size of the reachable computed expressions;
rule evaluation, if any, costs the summed size of the reachable rules. Each computed field is evaluated
once. **The cost does not grow with the depth of the derivation chain** — a chain of depth five costs
what five independent computed fields cost. Depth constrains the sequence, never the amount; it would
enter the cost only if an implementation re-evaluated a computed field once per dependent, which is a
defect, not a consequence. What actually grows uncomfortably is the **obligation count**: one obligation
per (editable field, reachable rule) pair, worst case the number of editable fields times the number of
rules. Because the pairing runs over the *transitive* closure, a sparse dependency graph still produces a
dense pairing when one rule sits at the bottom of a deep chain fed by several editable fields. That, not
depth and not the raw rule count, is what makes a definition expensive.

---

## 6. The question that is yours

Walk item 35 already settles that an editable write owes a rule's preservation obligation. This is only
about what discharges it.

> **When a field is declared `editable` and a rule relates it to other fields, what discharges that
> rule's preservation obligation at the edit?**

**Option A.** The field's own declared bounds, plus a per-state guarded access mode where the author
writes one, plus checking every admissible value where the domain is small enough. The engine checks
nothing wider than the field's declared contract. Where none of those closes the proof, the definition
is refused and the message names what would close it.

**Option B.** An evaluation, at the write and before anything is applied, of the rules the edit could
falsify, with the derivation chain substituted so computed-mediated rules are covered — cited by the
compiler as the premise that closes the write site, and recorded in the certificate. Definitions are not
refused on account of an edit.

**Option C.** Neither: a field a relational rule mentions may not be declared `editable` at all. The
refusal lands on the declaration rather than on the rule, and the author routes the change through an
event.

No recommendation attached. Two things to carry into the decision: the cascade computation in section 5
has to be built under all three options, so it is not a cost that distinguishes them; and canon at HEAD
already states Option A, written yesterday, so not answering leaves Option A standing.
