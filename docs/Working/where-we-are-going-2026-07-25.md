# Where we're going

**Status**: Draft — 2026-07-25. Companion to `where-are-we-2026-07-25.md`.
Written before cutting the new branch, so anyone landing in this folder knows what happened
and where the work moved.

## What this folder is

`docs/Working/` holds about eighty documents accumulated since May. Most of it is finished
business or abandoned turns. It is being left in place rather than cleaned, because the new
work does not read from it — see below. `where-are-we-2026-07-25.md` describes the state it
records. Treat everything here as history unless the new plan cites it by name.

Three things in it stay alive: `what-i-want-2026-07-16.md`, which Shane wrote and which
everything derives from; `bugs.md`; and `obligation-discharge-matrix-2026-07-19.md`, though
only for the six ways it slices the problem, not for its contents.

## What went wrong, in one paragraph

The canonical spec was frozen on 2026-07-13 and superseded three days later by the want doc,
which was never carried back into it. So for two months every piece of work has derived from a
document the canonical docs contradict. On top of that, the effort to define the proof engine
was trying to finish a list that could never be finished — the shapes a rule's condition can
take are independent properties, not alternatives, so "have we covered them all" had no answer.
Product code has not changed since 2026-06-05.

## The approach

**The work moves to `spike/Precept-V2-Radical-induction`**, cut from `spike/Precept-V2-Radical`.
The lineage is in the name; the suffix names the question the branch exists to answer — can the
induction model in the want doc actually be built. If it can, the branch becomes canon. If it
cannot, the name still says what was being tested.

**The canonical docs get rewritten there.** Everything the new work cites comes from that branch
and nowhere else. If the definition turns out to be buildable it is promoted; if not, there is
somewhere to stand.

A branch rather than a copied folder: the canonical documents reference each other by path 127
times, and this repository already contains 34 live citations of material a rule says never to
cite. Two copies kept apart by a rule in a prompt is the arrangement that has already failed.

**Write the discharge procedure as a walk over the expression forms the language allows**, with
a case for every form and nothing falling through. The shapes stop being a list to complete and
become wherever the procedure branches. Completeness becomes the property an exhaustive switch
has, which the build already checks elsewhere in this codebase.

**Generate the coverage grid rather than writing cells by hand.** A generated cell cannot cite a
rule that does not exist, which is what killed the last attempt at populating it.

**Ask whether it can be built at every stage, decide once at the end.** Earlier stages record
what they learn and do not rule. Two exceptions with a real stop are named below.

## The phases

Each ends where Shane looks at something and says yes.

| # | Goal | Finished when |
|---|---|---|
| 0 | Make the instructions true | Every instruction file an agent loads says only true things, and no two disagree. Covers the `CLAUDE.md` corrections, the three documents still calling the catalogs authoritative, the writing rule actually copied into the agent and skill files, and `witness` renamed to `counterexample` throughout |
| 1 | Work out what is actually decided, and by whom | Every decision since the freeze carries one of three verdicts — traced to something Shane wrote, contradicted by it, or no trace found. No fourth verdict permitted |
| 2 | Settle every place canon and the want doc disagree | Each contradiction found has a written resolution and a reason; the search procedure is written down and re-runnable. Nobody can prove the list complete, and it does not claim to be |
| 3 | Write the corrected statement of what the language is, and build the catalog drift check | Every list in a form a program can read; every normative statement numbered; a build check that fails when a list and its catalog disagree either way, demonstrated failing in both directions. **The catalog work splits here** — this half needs only the shape of a list, so it comes early; filling the catalogs with the settled proof model needs the model, so it waits for phase 9 |
| 4 | Draft the certificate step list and count it | The list exists and is dated. **This is a real stop**: everything after assumes the remaining judgement is a couple of dozen careful arguments. If the count comes back much larger, the plan is wrong and needs re-cutting rather than more effort |
| 5 | Settle the obligation families and the discharge procedure | Every declared expression form reaches exactly one case, nothing falls through, enforced by the build check from phase 3. Every case either derives from a named premise or refuses with a stated message. Not signed off in the session it was written |
| 6 | Write one argument per discharge rule and per step kind | Zero rules without an argument, zero arguments naming no rule. Each records attacks by at least two people who did not write it |
| 7 | Generate the coverage grid | No blank cells; every cell cites a rule on the closed list; re-running on unchanged inputs produces an identical file |
| 8 | Confirm, narrow, or revert | Three counts with their lists — already handled by working code, needing new code, refused. Shane confirms we carry on, narrows what the language admits, or reverts the branch |
| 9 | Fill the catalogs with the settled model | The discharge rules, step kinds, obligation families and write sites are all catalogued, and adding a member to any of them without teaching its consumers fails the build, demonstrated by a committed test. `CLAUDE.md`'s catalog suspension removed |
| 10 | Turn every numbered claim into a criterion and a failing test | Four counts, all zero: claims with no test, tests tracing to no claim, tests not driving the full compile path, tests red for a reason other than their stated one |
| 11 | Build until the frozen suite is green | The suite is green and the diff against the freeze commit shows no test deleted, renamed, skipped or weakened — only tests moving red to green |
| 12 | Confirm it is finished, by people who did not build it | The claim-to-test trace re-run shows nothing dropped; every input either compiles or produces diagnostics and terminates; and a named list of the places examined for cases where the compiler continues without proving |

Only phases 0 and 1 get broken into smaller steps now. The rest stay as written until the
ground is visible.

**Phase 8 is not a decision to start.** Forking already commits us to trying. It is confirm,
narrow, or revert — and reverting costs the branch's work, not the product.

**Two rules with teeth.**

*A closed list stays closed.* If any phase adds or removes a member of a list an earlier phase
closed, everything downstream halts until Shane rules on whether to reopen it. Measured: the
fault family went thirteen, eleven, ten, nine, and each move threw away finished work.

*Failure to converge is an answer.* If a phase does not close after **three** attempts, and each
attempt surfaced a different problem rather than the same one narrowing, that is evidence about
the model rather than about effort. Work stops and it goes to Shane as a revert question. This is
also measured, and it is the rule nobody had: the establishment and preservation design ran three
passes across two days — roughly fifty agents, three and a half hours — with a different objection
each round, and each round was treated as needing one more pass.

## What closes when

"Closed" means no further change without Shane's ruling, and reopening halts everything
downstream. It is not the word "locked", which has meant three different things in this project
and is part of why we are here.

| At the end of | This stops moving |
|---|---|
| Phase 2 | The disagreements between canon and the want doc. Nothing after re-litigates them |
| **Phase 3** | **The language.** What you can write in a `.precept` file. The lists are machine-readable and the build checks them against the catalogs. Everything downstream assumes this |
| Phase 4 | The certificate step vocabulary — dated and counted |
| Phase 5 | The discharge procedure. Every expression form has its case, proved total by the build |
| Phase 7 | The proof model. The grid is generated and reproducible from the closed lists |
| Phase 9 | The catalogs. They hold the settled model and the build fails if a member is added without teaching its consumers. The `CLAUDE.md` suspension lifts here |
| Phase 10 | The test set, frozen at a named commit |
| Phase 12 | The compiler |

Promoting the branch to canon is not on this list because it gates nothing. Any time after
phase 8. `main` has had no commits since 2026-04-19, so it is a ceremony rather than an event.

## Where the plan actually stops

If this turns out not to be buildable it will not announce itself at phase 8 as a tidy verdict.
It will look like a phase that keeps not closing. So the real stopping points are:

- **Phase 4**, deliberately, and cheaply. Count the step list. Everything after assumes the
  remaining judgement is a couple of dozen arguments; if it is not, stop here rather than at
  month three.
- **Phases 5 and 6**, under the three-attempt rule above. This is where the risk actually lives,
  because it is the same work that has already failed three times under a different arrangement.
- **Phase 8**, as a confirmation rather than a gate.

## Decisions made 2026-07-25

**A branch, not a copied folder.** Reasons above.

**The load-time certificate check is in scope, and so is the checker it calls.** The runtime
side is nearly free — a call inside a method that already works, touching no entity data. The
checker is not free; it had been retracted from scope and priced as a large piece of work. It is
back in, because without it certificates get written and nothing ever replays them.

**Coverage rests on the catalogs.** A checker that replays recorded steps catches a wrong proof
but never a missing one. The fix is for the checker to walk the definition itself, ask the
catalog what each thing owes, and confirm the certificate covers all of it. That makes catalog
completeness part of what makes the promise true rather than a tidiness goal — which is the
catalog-driven approach working as intended: two consumers deriving from one declaration instead
of each keeping its own list. Phase 9's finish line is therefore the guarantee's finish line.

**A bounded search for counterexamples is worth building.** A prototype built while testing the
idea found a live unsound discharge on its first run (see below). It does not make soundness
countable — nothing does — but it turns "we argued and nobody objected" into "a machine tries to
break it on every commit." Numeric scalars first; that is about a fifth of the known defects and
exactly the area being defined now.

**The build phase carries no size estimate, and that is accepted**, on condition its goal and
finish line are clear. A second stop-or-continue point comes when the test count exists.

**Vocabulary.** `witness` is retired in favour of `counterexample`. `anchor` stays — it is this
project's word for a family of modifiers. `denominator` is correct in its arithmetic sense and
banned as a way of saying what we measure against. `arm` is replaced by `case`.

## Where this can still go wrong

**The certificate step list may not be short.** Everything after phase 4 assumes it is. Twenty-one
step kinds have been designed; none has been built. Phase 4 exists to find out early.

**Nobody has argued the induction itself.** The promise is that the initial event establishes every
rule and every operation preserves it. Over a simple state machine that is obvious. Over this
language — state residency, computed fields recomputed on every operation, `omit` resets on entry,
entry and exit chains, first-match rows — it is not, and no argument for it exists anywhere. If it
is wrong, phases 5 through 7 produce nothing of value.

**The proof procedure may be too slow to be admissible.** The spec makes not breaking single-file
recompile a condition of a strategy being allowed. The corpus compiles in about 107ms today. A
pass over every rule crossed with every write site is a different cost class, and phases 5 and 6
lock the strategy set with no measurement.

**One live unsound discharge, found 2026-07-25 and confirmed by hand.** `nonnegative` and `positive`
contribute nothing to a field's declared interval. `field Bal as decimal nonnegative max 100` reads
as `[−∞ .. 100]`; the compiler mints a containment obligation, marks it **Proved**, and a single
event drives the value to −10. Not a coverage gap — a proof that says yes about something false.
Closes the open question in BUG-021: one root cause, and it is the spelling.
