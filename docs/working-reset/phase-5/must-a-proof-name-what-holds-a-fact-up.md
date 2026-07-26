# Must a proof name what holds each fact up?

**Prepared for Shane, 2026-07-26. Raised at the phase-1 walk, item 6, where he said it had never been put
to him plainly and he was not prepared to rule without more context. Reaches phase 4's certificate
contract and phase 5's premise rules.**

**Reading time: about six minutes.** One thing to say first, because it changes how much of this is
actually open — see "What your catalog ruling already settles" near the end.

---

## The question in one sentence

When a proof leans on "this rule was already true before the operation", must the proof **name** the
thing that makes the rule true at creation and everything that keeps it true — or is it enough that those
were checked somewhere, with the compiler tracking them separately?

## What is not in question

That both have to be checked. A proof that uses "the rule held before" is worth nothing unless the rule
really always holds, which needs something to make it true when the entity is created and needs every
operation that could break it to be shown not to. Your want document gives both — the base case at
want:18, the step at want:19. Nobody is proposing to drop either.

What is open is only whether the proof **writes them down**.

## A worked example

This compiles clean at HEAD today:

```precept
precept Account

field Balance as decimal default 0.0
field OverdraftLimit as decimal nonnegative max 10000 default 500.0

rule Balance >= 0.0 - OverdraftLimit because "an account may not go below its agreed overdraft"

event Withdraw(Amount as decimal positive max 1000)
event ReduceLimit(NewLimit as decimal nonnegative max 10000)

on Withdraw when Balance - Withdraw.Amount >= 0.0 - OverdraftLimit
    -> set Balance = Balance - Withdraw.Amount

on ReduceLimit when Balance >= 0.0 - ReduceLimit.NewLimit
    -> set OverdraftLimit = ReduceLimit.NewLimit
```

*(Worth noting in passing: the rule this file is entirely about produces **no proof obligation at all**
today — reproduced at HEAD. That is the gap your item 16 ruling closes, not this question.)*

Now suppose `Withdraw`'s proof wants to use the fact that the rule held before the withdrawal.

**Under option one**, the certificate for that step reads roughly:

> Using: the rule held before this operation.
> Made true at creation by: the default values `0.0` and `500.0` satisfying it.
> Kept true by: `Withdraw` (its guard), `ReduceLimit` (its guard).

**Under option two**, it reads:

> Using: the rule held before this operation.

and the compiler separately holds a record that the rule's creation case and both handlers were checked.

## What each costs

**This one does not change what compiles.** Both are sound, and the same definitions are accepted under
either. The cost lands in three other places.

**Certificate size.** Option one's certificate grows with the number of operations that touch a rule. A
rule mentioned by ten handlers carries ten names at every proof step that uses it, in every handler that
uses it. Option two's certificate is flat.

**How local the check can be.** This is the real difference. Under option one, checking a handler means
following the names out to every other handler in the file — the list of what preserves a rule is a claim
about the whole definition, so no single handler can be verified alone. Under option two the checker can
work through one handler at a time and consult the separate record for coverage.

Note what this does **not** buy: option one does not make the check local either. Naming the preserving
operations still means the check spans the whole file — it just means the certificate says so out loud.
That was the substance of the correction recorded on 2026-07-23, and it is an argument *inside* option
one, not a reason to prefer it.

**What the load-time gate can confirm by itself.** want:204 has the runtime refuse to govern under a
definition whose certificate does not verify. Under option one, the gate reads the dependency straight
off the certificate. Under option two, the gate has to work out for itself what should have been checked,
and compare.

## What your catalog ruling already settles

At the phase-1 walk, item 1, you confirmed that coverage rests on the catalogs — the checker walks the
definition, asks the catalog what each thing owes, and confirms nothing is missing. Your words: "the idea
to use the catalog was mine."

**That is the same job the naming was doing.** If the checker independently derives the full list of what
a definition owes, it does not need the certificate to tell it which operations preserve a rule — it
computes that and checks the certificate covers it. Naming would be a second copy of an answer the
checker already has, and second copies drift.

So the question narrows to something much smaller: **is there anything the checker cannot work out for
itself that the certificate would have to carry?** One candidate — a proof step that uses a fact
established by a *particular* earlier step rather than by a declaration, where "which step" is not
recoverable from the definition alone. If phase 5's derivation rules produce steps like that, those steps
need naming and the rest do not. If they do not, option two is enough and the catalog does the coverage
work you already assigned it.

## What I would put to you, rather than the original two options

Not "name everything" or "name nothing", but: **name nothing by default, and let phase 5 identify the
specific step kinds where the fact cannot be recovered from the definition and must be carried.** That
keeps certificates small, keeps one answer rather than two, and uses the catalog for the job you gave it.

This is a recommendation, not a finding — it rests on the claim that the checker can derive the
obligation list, which is exactly what phase 9 has to make true and what the walk recorded as a concern
worth stating rather than a settled fact.

## What blocks on your answer

Phase 4 writes what a certificate must contain. Phase 5 writes the premise rules. Neither can be finished
without this, but neither starts with it — phase 4 is two to three weeks out.
