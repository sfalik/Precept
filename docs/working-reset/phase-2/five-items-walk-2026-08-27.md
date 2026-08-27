# The five items put to Shane before sign-off, and what he ruled

**Date**: 2026-08-27 · **Branch**: `spike/Precept-V2-Radical-reset` · **HEAD when the walk started**: `25893265`

The finding-by-finding diff of the cold read against the register
([`cold-read-register-diff.md`](cold-read-register-diff.md)) collected five things to put in front of the
owner before he signs phase 2 off. All five were walked with him on 2026-08-27, one at a time. This
records what he said and what was done.

## The ruling that reframed the walk

Partway through the third item he stopped it: *"obviously it can't yet, we haven't built it yet. this is
getting ridiculous going around in circles. What is the point of phase 2, I thought we were just trying to
get the design aligned to the want document?"*

He is right, and it is phase 2's own goal: *"Find every place where the canonical documents on this branch
and the want document say different things, resolve each in writing with a reason."* Measured against
that:

- **A build-state observation is not a phase 2 item.** That the compiler has not yet built something canon
  describes is design-versus-build. It says nothing about whether canon and the want document agree. **This
  half is his and it stands.**
- **Two canonical documents disagreeing with each other was then read as outside the phase too.** That was
  the assistant's extension, not his, and **he corrected it the same day**: *"when did I rule that two
  documents disagreeing with each other is not a problem? as I understand it, the goal is ensure
  consistency between the want doc and the canonical docs"*. His reason settles it — if two canonical
  documents disagree, canon has no single position, so "canon agrees with the want document" is not a
  well-formed claim. Consistency inside canon is a precondition for the alignment this phase establishes.
- **What decided the timing**: no later phase reaches these. Phase 3 resolves contradictions among the
  language documents by restating the language, and none of the findings live there — they are in the
  runtime and compiler documents. Phase 10's tests come from phase 3's numbered claims and phase 12
  re-runs that trace. No phase builds the runtime. Asked *"if we don't do wide now, when would we do
  it?"*, the answer was never, and he ruled: **"then wide now."**

The five items were reclassified twice — once on the first reading, and again when the extension was
withdrawn. The classifications below are the corrected ones.

## Item by item

### 1. An outcome one document declares and another declares impossible — FIXED

`result-types.md:139` declares `UpdateOutcome.ConstraintsFailed` and `:152` defends it as an ingress
failure; `compiler-and-runtime-design.md:863` said "There is no `*ConstraintsFailed` outcome in any of
these families, and there cannot be one." Eight sites across four documents say ingress-yes,
post-mutation-no; that one sentence said neither, without qualification. Its own reason — a configuration
committed with a rule broken — does not reach the surviving variant, which is refused before the patch is
applied.

**His words: *"we should fix it"*.** Taken as the sentence fix rather than a rename of the variant; the
rename stays available and was not ruled on. The sentence now scopes itself to constraint failures found
after a mutation, and a following paragraph names the ingress variant and points at `result-types.md`. The
reason was left exactly as it was, because it is the same claim item 3 is about and re-justifying it here
would have settled that question in passing.

*Classification: canon against canon — in scope under the corrected reading, and fixed.*

### 2. The inspection table still showing the pass the document says was removed — FIXED

`evaluator.md:1933` carried a Constraints column reading "Evaluate" for both Commit and Inspect — the
post-mutation sweep, which the same document denies at `:694`, `:717`, `:912`, `:1680`, `:2034` and
`:2126`. It survived the register's largest family for a measurable reason: that family's test wants a
constraint noun and an evaluation verb near each other, and this table puts the noun in the header row and
the verb in the body rows, so they never share a line.

**His words: *"do it"*.** Done, and running the same sweep by hand for the same structural blind spot
turned up three more survivors in the same document:

- `:146`, a responsibility row promising "constraint-by-constraint detail" from inspection, which `:912`
  says is undecided. Reworded to keep the ingress check results `:917` genuinely promises and to point at
  the open question for the rest.
- `:1262` and `:1268`, an `ArrayPool` return path and a rollback code block both triggered by a constraint
  failure after mutations had begun. No such trigger exists. The trigger is no longer named; a note under
  the block says plainly that the mechanism may still be wanted for an engine fault caught mid-row or for
  inspection releasing its copy, and that nothing settles which. Indexed in `evaluator.md` § Open
  Questions and given a row in the runtime parking lot.
- `:125`, "enforce access modes and constraints" for Update — loose rather than wrong. Left alone, on his
  instruction.

*Classification: canon against canon — in scope under the corrected reading.*

### 3. Two executed rows resting on an obligation the compiler cannot state — OUT OF SCOPE

Two rows deleted the runtime's post-mutation check, both giving as their reason that every handler was
proven at compile time to preserve every rule. `proof-engine.md:489` enumerates the obligation vocabulary
and neither establishment nor preservation is in it; `:379` shows obligations minted only from nodes the
catalog stamped, never from a rule; `:2616` states the gap in canon's own voice and calls it a gap against
the target rather than a revision of it.

**Ruled out of phase 2** by the scope ruling above — it is design-versus-build. It remains owned: phase 5
names establishment and preservation as the base case and the step of one induction and makes the
induction argument a deliverable; phase 9 records that `ProofRequirementKind` carries no such member. No
canon edit was made. The cross-reference from the obligation list to the section stating the gap was
offered and not taken.

### 4. Two documents assigning schema migration to different owners — NOW A REGISTER OBLIGATION

`compiler-and-runtime-design.md:718` calls definition migration out of scope for v1 and says host
applications must manage it externally. `runtime-api.md:277`, `:906` and `evaluator.md:1757` say it is the
post-MVP job of the restore path itself. Neither register row nearby quoted the sentence that assigns the
work — one quoted a clause about the sweep, the other a clause about constraint evaluation, and the
sentence between them matched neither pattern.

**His instruction at the time: record it as a canon-against-canon note.** That was done as a § 5 row
covering the whole category of twelve. **It did not survive the day.** Once the extension excluding
canon-against-canon was withdrawn, the twelve became register obligations under exit condition 4 and the
§ 5 row was struck through. This one is named there as the heaviest of them: it is an ownership question
about a capability, not a wording slip.

*Classification: canon against canon — in scope under the corrected reading.*

### 5. A want line excluded on a condition that had since been met — ADDED TO § 4

The register excluded `what-i-want-2026-07-16.md:219` — whether a rule may reference the current moment,
which the want document declares open — under its own § 14.4 rule: canon saying something about a
question the want document declares open "goes to 'Waiting on Shane', not into this register." Canon does
say something. `temporal-type-system.md:560` declares `now()`, `:577` gives `rule now() - IncidentAt <=
'72 hours'` as a worked usage pattern, and `:568` locks a decision accepting that such a rule produces
different results at different times. The item never reached § 4, because the exclusion closed the
question before anybody checked whether canon had answered it.

**His words: *"it is undecided, add it to the list"*.** Added to § 4 as the fifteenth item, stalling
phase 5's fragment edge and phase 3 if the answer is that such rules are refused. Canon's locked decision
stands until he rules. The other line excluded by the same rule — the promise across precepts — was
checked and holds: the only canon mention is the name binder listing cross-precept resolution as out of
scope, which agrees with the want document rather than answering it.

*Classification: canon against the want document. The only one of the five that bore on sign-off as the phase was first scoped; under the corrected scope items 1, 2 and 4 bear on it too.*

## What this leaves

Phase 2's sign-off question is whether the register fairly records the disagreements it is answerable for
— and by the end of the day that was two shapes, not one: canon against the want document, and two
canonical documents against each other. Exit condition 4 was rewritten to say so, and sixteen findings
remain under it. The register needs a row shape it has never had, and the search procedure needs a pass
that reads canon against itself.

**The lesson worth keeping from the day** is not any of the five items. It is that the assistant turned a
complaint about one item into a scope ruling covering a whole category, wrote it into the plan in four
places as the owner's, and it took his direct challenge to get it out. A ruling is what he said, about
the thing he said it of.
