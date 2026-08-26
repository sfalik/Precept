# The 2026-07-19 additions to the want document — for marking

**What this is.** `docs/Working/what-i-want-2026-07-16.md` is your statement of what the compiler
should do. Three commits made it. The first two are yours in substance. The third — `73f4db7d`,
2026-07-19, *"docs(working): deep-review + non-linear RepaymentCapPercent amendment"* — added new
material three days after the document's date. There is no conversation record for that day, so the
repository cannot say whether you asked for any of it. That is what this document asks you.

**Thirteen items. About twenty minutes.** They are ordered by how much rests on them, heaviest
first, so if you stop early you will have covered what matters. The last four take a few seconds
each. One item is already answered and is included only so the record is complete.

**What your marks are used for.** Anything you mark *not mine* comes out of canon, or has its
support replaced, wherever canon rests on it — the places are named under each item. Anything you
mark *mine* stays, and the passages that cite it become properly warranted. *Don't remember* is a
real answer and means the item goes to the same treatment as *not mine*: it cannot be quoted as
yours.

---

## Answered 2026-08-26 — all thirteen, and the question was smaller than this document assumed

**The owner reviewed and approved the want document, this amendment included.** Asked whether he
added the repayment cap, his words were: *"i don't remember, I didn't write it by hand, but worked
with the agent to revise the sample"*, and then: **"lets assume that I didn't write it directly, but
did review it."** Asked afterwards why the content was being disputed at all, he said he had already
determined that he reviewed and approved everything in the want document.

**So the content of the amendment carries his approval and is not in question.** The material was
produced in a working session he was part of and reviewed. Nothing in it is deleted, nothing that
rests on it loses its footing, and no canonical passage needs its support withdrawn.

**What phase 1 actually established, and what it did not.** The provenance analysis at
[`../phase-1/want-document-provenance.md`](../phase-1/want-document-provenance.md) established a
documentary fact: which commit typed which lines. Its conclusion that `73f4db7d` is "not safe to
quote" means those lines are not safe to present as the owner's *verbatim words*, because the
phrasing is an agent's. **It does not mean the content lacks his authority**, and it was read that
way in the first version of this document. That reading does not follow, and used generally it would
mean nothing the owner approves could ever be settled.

**The one thing that is genuinely wrong**, and it is narrow: want `:135` is written in the first
person — "On non-linear rules I want the line drawn in the open… I'm only saying it out loud" — as
though the owner wrote those words. He did not. That is a phrasing problem inside the want document,
not a defect in anything downstream of it.

**What follows:**

- **Nothing comes out of canon.** `docs/language/precept-language-spec.md:270` stands, and the record
  that authorised it — "Every clause is the want document's" — is accurate.
- **The `.precept` example stands** and stays as it is.
- **One citation is re-based.** `docs/compiler/soundness-and-coverage.md:261` grounds a decision on
  want `:72`, a line of the worked example, when the decision is recorded independently as the owner's
  ruling of 2026-07-23. The citation should point at the ruling. One line.
- **Where canon quotes want `:135` as the owner's own words**, it is reworded to cite the want
  document rather than to quote him. The claim is unaffected.
- **Phase 5 still decides where the discharge fragment stops.** That was always its scope. The
  non-linear case is an input to that decision, not a crisis, and not an open contradiction.

The per-item detail below is retained because its "if this is not yours" sections are an accurate map
of what rests on what, which is useful regardless. **Read those as dependency information, not as
work to be done** — the answer to every item is that the owner reviewed and approved it.

---

## 1. The compiler will not do algebra on a rule that multiplies or divides two fields — the author must write a guard repeating the condition, or the definition is refused

**Added at line 105** — a sixth entry in the bullet list under the worked example, which had five:

> - **`RepaymentCapPercent`** — the non-linear obligation. The added rule `MonthlyRepayment <=
> OverdraftLimit * RepaymentCapPercent` multiplies two fields, so no single write closes it by
> substitution the way `Withdraw`'s guard closes the linear rule. `PlanRepayment` sets
> `MonthlyRepayment` to a *quotient* and the obligation compares it against a *product* — four free
> variables and nothing to cancel — so premise class (c) has to carry it: the guard restates the
> post-state condition verbatim (`-Balance / PlanRepayment.Months <= OverdraftLimit *
> RepaymentCapPercent`) and the proof closes against what the author wrote, not against algebra the
> compiler won't attempt. And because the rule mentions `OverdraftLimit`, `ReduceLimit` inherits the
> same obligation from the other side — lowering the limit shrinks the product even though
> `MonthlyRepayment` is untouched, so it too must restate the condition (`MonthlyRepayment <=
> ReduceLimit.NewLimit * RepaymentCapPercent`) or be rejected. This is the symmetric write site
> again, but non-linear: substitution alone never closes it, and the ideal compiler rejects any
> unguarded non-linear rule rather than reach for a solver it does not carry.

**If this is not yours.** `docs/language/precept-language-spec.md:270` is a normative paragraph
saying exactly this, and it was written from two lines: this one and line 135. Line 135 you have
already said is not yours, so this line is the only remaining support the paragraph has. If it goes
too, the paragraph comes out of the specification and is replaced by something narrower. What
survives either way is prove-or-reject as a general posture (your line 171) and "no re-evaluation
after mutation" (your line 199) — so a paragraph saying "a rule the compiler cannot prove is
refused" would still be warranted. What would not survive is everything specific to this case: that
the only premise the compiler will accept is a guard or argument constraint repeating the condition
word for word, that it will not attempt algebra, and that no solver stands behind it. Those are
proof-model design decisions. The trace that found this is at
`docs/working-reset/phase-2/trace/language-documents.md:409`.

Work sized against it: `compiler-readiness-plan.md:1359` sizes the first proof fragment partly on
"one non-linear case closed by a guard that restates the post-state condition verbatim."

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 2. The same policy stated as a general rule, in your voice

**Added at lines 135–136** — a new paragraph after "This is also where the feasibility question will
concentrate":

> On non-linear rules I want the line drawn in the open. A relational rule whose post-state condition
> is non-linear — a product or quotient of fields, like the repayment cap above — is proven only when
> a guard or arg constraint makes that condition directly checkable, so the proof closes against what
> the author wrote. Where no such premise exists, the compiler rejects the rule with a teachable
> message and names the guard that would close it. It is never governed by a post-mutation sweep, and
> never deferred to a solver the compiler doesn't carry. This is the same discipline as everywhere
> else in this document — prove from a declared premise or reject — I'm only saying it out loud for
> the non-linear case, because substitution closes the linear rules so quietly that it could leave
> the impression the harder ones come along for free. They do not: the author guards them, linearizes
> them, or the compiler refuses them.

**Already answered — 2026-08-26. Your mark: not mine.** Included so the record is complete. Do not
re-answer it.

**☑ not mine** — marked 2026-08-26

---

## 3. Dropping the repeated condition from a guard must be a compile error, and neither a solver nor a check after the write may stand in

**Added at line 123** — a fourteenth row in the "What must not compile" table, which had eleven
before this commit:

> | Guard `PlanRepayment` on `Balance < 0.0` alone, dropping the restating conjunct | Cannot prove the
> cap rule: the product is non-linear, so substitution can't close it and no post-mutation sweep or
> solver stands behind it — the guard must state the post-state condition directly, or the rule must
> be linearized |

**If this is not yours.** This row is what turned the policy in items 1 and 2 into something
testable. `docs/working-reset/phase-2/contradiction-register.md:429` records the reason the
specification paragraph was written at all: this row is one of the must-not-compile cases, "so phase
10 needs a canonical statement to write a test against." Remove the row and that reason goes with
it. The plan turns every row of that table into a criterion and a failing test, so this is one test
that would not be written.

One thing this row also does, separately from the non-linear question: it states in the mutation
table that no check after the write stands behind the guarantee. That claim has independent support
in your own line 199, and it is not affected by how you mark this.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 4. A second business rule in the worked example — a cap that multiplies two fields together

**Added at line 40.** Your example declared exactly one rule, and it is linear:

> `rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set because
> "A planned repayment may not exceed the capped fraction of the overdraft facility"`

**If this is not yours.** `docs/language/precept-language-spec.md:270` quotes this rule verbatim as
the example the whole non-linear paragraph is built around, so it would need a different example or
the paragraph goes. Three test files encode it as a known-answer fixture —
`test/Precept.MatrixTools.Tests/BankExampleTests.cs:26`, `ObligationEnumeratorTests.cs`,
`EstablishmentTests.cs` — and `samples/Test.precept:10` carries it. All of those are prototype
scaffolding and cheap to change.

The larger point is that with this line gone, **nothing you wrote contains a non-linear rule at
all.** Your example is one linear rule. Every argument in the repository about how the compiler
handles multiplication of two fields traces back to this line and to items 1 and 2.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 5. A sixth field, editable, with the new cap rule constraining it

**Added at line 36.** Your example declared five fields:

> `field RepaymentCapPercent as decimal default 0.25 positive max 1.0 editable`

**If this is not yours.** Nothing in `docs/language/`, `docs/compiler/` or `docs/runtime/` declares
this field, so no canonical document changes. What it affects is the editable-write question on your
waiting list. Twice, an argument was built on the fact that your own example declares an editable
field that a relational rule mentions, and that under current canon your example therefore would not
compile. That argument was already withdrawn at
`docs/working-reset/phase-5/editable-write-cascade.md:118` once this line's provenance was
established — and the withdrawal notes something you should know either way: **your original example
declares one editable field, `DailyWithdrawalLimit`, and no rule statement mentions it.** So nothing
you wrote exercises the wide reading of your own line 143.

There is support pointing the other way, and it is worth stating: your line 129, which you did
write, describes `DailyWithdrawalLimit`'s modifiers being evaluated at the editable-write point, so
you were thinking about editable fields and proof when you wrote the example. What you did not do
was put a relational rule over one.

Same three test files and the sample carry this line.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 6. A refusal message that performs a division

**Added at lines 71–72** — a second `PlanRepayment` row, below the one you wrote:

> ```
> from Active on PlanRepayment when Balance < 0.0
>   -> reject "A monthly repayment of {-Balance / PlanRepayment.Months} would exceed the capped
>      fraction ({RepaymentCapPercent}) of the overdraft facility (limit {OverdraftLimit})"
> ```

**If this is not yours.** This is the only arithmetic inside any message anywhere in the document.
Your four refusal messages read bare fields and arguments and nothing else.

`docs/compiler/soundness-and-coverage.md:261` carries a decision recorded as your ruling of
2026-07-23 — that an expression inside a refusal message which could fault (divide by zero, overflow)
must be caught at compile time like any other. Its stated grounding is this line: *"Grounded in the
want doc's own worked example, which puts `{-Balance / PlanRepayment.Months}` inside a refusal
message and relies on `Months positive` to make it safe."* If this line is not yours, that sentence
loses its grounding and the decision stands or falls on the 2026-07-23 ruling alone, which is
recorded separately. Three further entries in the phase-1 register argued the same way from the same
line — `docs/working-reset/phase-1/decision-register-FINDINGS.md:54`.

Support pointing the other way: your line 104 says faults get "the identical premise-and-certificate
treatment," which establishes that fault obligations exist. It does not say a message is one of the
places they arise.

There is real build work behind this: message-hole fault obligations are marked in-scope and not yet
implemented.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 7. A twelfth must-not-compile row: dropping the cap check from the repayment guard

**Added at line 121:**

> | Remove `PlanRepayment`'s cap conjunct (`-Balance / Months <= OverdraftLimit *
> RepaymentCapPercent`) | Cannot prove `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent`
> after `set MonthlyRepayment = -Balance / Months` — the quotient is unbounded above; restate the
> post-state condition in the guard |

**If this is not yours.** One fewer test in the phase-10 suite, and one fewer piece of support under
the specification paragraph in item 1. It says nothing item 3 does not already say; it applies it to
one handler. If you mark item 3 *mine*, this follows from it.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 8. A thirteenth must-not-compile row: the same, at the other write site

**Added at line 122:**

> | Remove `ReduceLimit`'s cap conjunct (`MonthlyRepayment <= NewLimit * RepaymentCapPercent`) |
> Cannot prove `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` — the rule mentions
> `OverdraftLimit`, so lowering it carries the obligation even though `MonthlyRepayment` is
> untouched; restate it in the guard |

**If this is not yours.** Same as item 7 — one test, and one piece of support.

The principle this row applies — that a rule's obligations attach to every write site of every field
the rule mentions, not just the one the author had in mind — is your line 20, and it is untouched by
how you mark this. This row is that principle applied to the added rule.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 9. A cap check bolted onto the repayment guard you wrote

**Changed at line 68.** Your version:

> `from Active on PlanRepayment when Balance < 0.0`

At HEAD:

> `from Active on PlanRepayment when Balance < 0.0 and -Balance / PlanRepayment.Months <=
> OverdraftLimit * RepaymentCapPercent`

`when Balance < 0.0` is yours. The `and …` is the addition.

**If this is not yours.** Nothing in canon rests on it. It is the line that makes the amended example
provable, so it is cited inside the phase-5 working papers as the demonstration of what "restate the
post-state condition in the guard" looks like (`phase-5/editable-write-design-space.md:423`). Those
are working papers, not canon. It goes with items 4 and 5 — if the rule is not yours, this is not
either.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 10. The same cap check bolted onto the limit-reduction guard you wrote

**Changed at line 75.** Your version:

> `from Active on ReduceLimit when Balance >= -ReduceLimit.NewLimit`

At HEAD:

> `from Active on ReduceLimit when Balance >= -ReduceLimit.NewLimit and MonthlyRepayment <=
> ReduceLimit.NewLimit * RepaymentCapPercent`

**If this is not yours.** Nothing in canon rests on it. Same situation as item 9.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 11. A refusal message that reads a field which may hold nothing

**Added at lines 80–81** — a third `ReduceLimit` row, with no guard on it:

> ```
> from Active on ReduceLimit
>   -> reject "Cannot reduce the overdraft limit to {ReduceLimit.NewLimit}: a planned repayment of
>      {MonthlyRepayment} would exceed the capped fraction ({RepaymentCapPercent}) of the reduced
>      facility"
> ```

`MonthlyRepayment` is declared `optional` at line 35, so on this unguarded row it may hold nothing
when the message renders.

**If this is not yours, nothing further changes — it has already been accounted for.** This is the
only read of an optional field inside a message anywhere in the document; your version never reads
`MonthlyRepayment` at all, in a message or out of one. Four entries in the phase-1 register turned
on this line, and all four were withdrawn on 2026-07-26
(`docs/working-reset/phase-1/decision-register-FINDINGS.md:56`). Three of them had argued that your
locked "refuse uniformly" decision on optional reads in messages refuses an example you say is
accepted. With this line's provenance established, that objection is gone and the locked decision is
undisturbed.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 12. A guard put on the catch-all row you left unguarded

**Changed at line 78.** Your version had no guard, so it caught everything the row above did not:

> `from Active on ReduceLimit`

At HEAD:

> `from Active on ReduceLimit when Balance < -ReduceLimit.NewLimit`

**Nothing rests on this.** It exists only to make room for the new unguarded row at lines 80–81 — the
two were added in one move. If you mark item 11 *not mine*, this comes out with it and the row goes
back to catching everything.

`☐ mine  ☐ not mine  ☐ don't remember`

---

## 13. One sentence reworded because the change above made it untrue

**Changed at line 154.** Your version (as it stood after the 2026-07-18 edit pass):

> `PlanRepayment` has no fallback row, so on a healthy balance the event is simply inapplicable

At HEAD:

> `PlanRepayment` offers no row on a healthy balance, so there the event is simply inapplicable

**Nothing rests on this.** Adding the row at lines 71–72 made "has no fallback row" false, so the
sentence was restated. The claim canon actually uses from this line — absence expresses
inapplicability, `reject` is reserved for refusals the user can act on — is yours, is in both
versions, and is untouched. It is what
`docs/working-reset/phase-2/confirmation-run.md:151` and `recheck-third-pass.md:367` cite. If item 11
comes out, your original wording becomes true again and can be restored.

`☐ mine  ☐ not mine  ☐ don't remember`
