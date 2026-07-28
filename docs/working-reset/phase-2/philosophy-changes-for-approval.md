# `docs/philosophy.md` — the phase 2 changes, for approval

| Field | Value |
|---|---|
| What this is | Every change to `docs/philosophy.md` that phase 2 wants, written out for approval or refusal. **Nothing has been applied.** `docs/philosophy.md` is untouched and still reads exactly as it did at HEAD; the diff at the end was produced from two copies outside the repository. |
| Why it is a document and not a commit | `CLAUDE.md`: philosophy changes require deliberation, are never auto-synced, and are never bundled into an implementation change without discussion. The plan's Phase 2 section names the philosophy edits as one of two things you sign off personally. |
| Measured against | `docs/philosophy.md` at HEAD `5e853c0e` — the file is clean in the working tree and has not changed since 2026-07-12 (`33dde428`), so every line number below is HEAD's. |
| What forces the changes | `docs/Working/what-i-want-2026-07-16.md` (the want document, bylined yours), and the two rulings you gave on 2026-07-26 recorded in `docs/working-reset/compiler-readiness-plan.md` — walk item 23 (philosophy states the target; the code is validated against it) and walk item 15 (arithmetic overflow is exempted from prove-or-reject for the first release). |
| Starting point | The two `P` rows in [`sweep-correction-list.md`](sweep-correction-list.md) § 1.2. They were a starting point and not the whole list: six more places in the same file say the same thing in different words, and one further contradiction is left unproposed and flagged for you at the end. |
| How to answer | Per change. They are independent except where a section says otherwise, and refusing any one of them leaves the rest applicable. |

## The changes at a glance

| # | Line | What it says now, in short | Why it has to change | Class |
|---|---|---|---|---|
| 1 | 19 | `Fire` evaluates all constraints against the resulting configuration and commits only if every one holds | The want document removes that evaluation. This is the sentence the want document itself flags for you (want:215) | Required |
| 2 | 20 | `Update` — "the same constraint enforcement applies" | Points at change 1's mechanism, and an edit's mechanism is not the same one | Required |
| 3 | 33 | a rule is "always checked", or "checked only when a declared condition is met" | "Checked" is the word for a runtime evaluation that no longer happens | Wording |
| 4 | 51 | "the change and the check are the same act" | Asserts a per-operation rule evaluation as the enforcement mechanism | Required |
| 5 | 53, 61 | overflow listed among the compile-time impossibilities | Walk item 15 exempted overflow; walk item 23 says keep the target and say where it is not met | Honesty note — a different kind of change from the rest |
| 6 | 55 | the compiler requires a runtime-supplied value to *carry* a constraint | Only two of the want document's four premise classes. `Deposit` and `Withdraw` in the want's own example are proven the other two ways | Required |
| 7 | 68 | "the runtime structurally prevents any operation from producing a result that violates it" | Attributes the prevention to the runtime | Wording — the sentence is ambiguous rather than wrong |
| 8 | 69 | "the transition that produces an invalid data configuration is rejected before it commits" | Produce-then-reject-before-commit is the removed mechanism, stated in a comparison bullet | Required |

Five changes (1, 2, 4, 6, 8) are needed for the document to stop describing a mechanism the want document replaced. Two (3, 7) are wording that will read as the old mechanism to anyone who knows it, but that no reader would call a contradiction. One (5) is not about the mechanism at all and is the only one where the honest answer might be to do nothing — its section says why.

---

## Change 1 — line 19, the `Fire` bullet

**Now:**

> - **Fire**: execute a transition. The engine validates input arguments, selects a matching transition row via guards, executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.

**Proposed:**

> - **Fire**: execute a transition. The engine validates input arguments against the constraints declared on them, selects a matching transition row via guards, executes mutations, and commits. It does not re-check the entity's rules against the result, and does not need to: the compiler proved, before any entity existed, that every transition preserves every rule, given the argument constraints and guards the author declared. An argument that fails its declared constraint is refused before any mutation runs; a row whose guard the entity does not satisfy is never offered. The invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.

**What forces it.** The want document, § Certificates, line 199: *"Once the compiler has proven every rule, the runtime does **not** re-evaluate them against the post-mutation configuration."* And § The runtime's role, line 165: *"What runs at runtime are the declared premises (ingress validation of args and editable fields, guards) that the compiler's proofs depend on, and nothing beyond them."* The want document names this exact sentence at line 215 as needing *"a deliberate, owner-approved rewording."*

**What is lost or risked.**

- The old sentence let a reader verify the guarantee for themselves in one step: the engine checks, then commits. The new one asks the reader to accept that a proof performed earlier, by a program, covers every case. That is a harder thing to believe on first reading, and this document's job is partly to be believed on first reading.
- It states a negative — "does not re-check" — in a positioning document. A reader comparing Precept against a validator may read that as something removed rather than something no longer needed. The alternative is to drop the negative and write only the positive ("…executes mutations, and commits; the rules hold because the compiler proved every transition preserves them"), which reads better and hides the change from anyone who knew the old text. I have proposed the explicit version because the change is worth being visible, but the quieter version is a legitimate choice and needs only that one sentence swapped.
- The sentence now claims the compiler proves every rule of every transition. It does not do that today. Under walk item 23 that is the intended posture, but this is the place where the distance between the document and the compiler is largest, and it is worth knowing that is what you are approving.
- **This is the change the composition seam hangs off.** If you take Option A in that section, one clause is added here; the exact wording is given there.

---

## Change 2 — line 20, the `Update` bullet

**Now:**

> - **Update**: edit a field directly. The same constraint enforcement applies. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.

**Proposed:**

> - **Update**: edit a field directly. The incoming value is held to the constraints declared on that field before it is written; the rules that mention the field hold because the compiler proved this write preserves them, exactly as it proved it of every transition. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.

**What forces it.** "The same constraint enforcement applies" points at whatever change 1 says, so it moves whether or not it is edited. It cannot be left pointing at the new text unaltered, because an edit's mechanism is genuinely not the same as a transition's: the want document, line 141, makes an editable-field write one of the two ingress points, and there is no guard and no event argument involved.

**The one place I departed from a written direction, deliberately.** The want document at line 143 says an editable-field write evaluates *"the field's modifier-rules **and** every rule that mentions the field"* at ingress. I have not written that, because the reconciliation section at the end of `sweep-correction-list.md` records that your 2026-07-26 rulings replaced it: walk item 16 (a declared premise decides what the compiler proves, not where the value came from) and walk item 8 (writing a field a rule merely reads takes on that rule's obligation) together make an editable write an ordinary write site with ordinary proof obligations, and the wider ingress check was checked and found not to work — a rule reaching the edited field through a computed field would be evaluated against a value the operation then discards. So the proposed text says ingress holds the value to *the field's own declared constraints*, and the rules come from proof. **If that reads wrong to you, this is the sentence to say so on**, because the same reading is being written into the specification and the runtime documents by the other phase 2 slices.

**What is lost or risked.** The bullet grows from one short sentence to a longer one, in a list whose value is that it is scannable. And it now asserts something about `Update` that the current compiler does not do — the same aspiration risk as change 1, in a smaller place.

---

## Change 3 — line 33, "always checked"

**Now** (the changed fragment only; the rest of the paragraph is untouched):

> A rule can be unconditional (always checked) or guarded (checked only when a declared condition is met), but in either case, the engine enforces it structurally on every operation where it applies.

**Proposed:**

> A rule can be unconditional (holding of every configuration the entity can be in) or guarded (holding whenever a declared condition is met), but in either case, the engine enforces it structurally on every operation where it applies.

**What forces it.** Nothing forces it hard; this is the weakest of the eight. "Checked" is the vocabulary of an evaluation that runs, and after change 1 no such evaluation runs for rules. The want document, line 173, is the reason to bother: *"the guarantee is uniform — a rule holds, period."* A reader who has just been told rules are not re-evaluated, and then reads that a rule is "always checked", has to reconcile the two themselves.

**What is lost or risked.** "Always checked" is short and immediately clear; "holding of every configuration the entity can be in" is longer and slightly more abstract. If you would rather keep the punchier words and accept the small inconsistency, that is a reasonable call and nothing downstream depends on this change.

---

## Change 4 — line 51, "the change and the check are the same act"

**Now** (the changed sentence only, inside the "Prevention, not detection" paragraph):

> Whether the rule is about what the entity *is* or what the operation *brings in*, it evaluates atomically — the change and the check are the same act.

**Proposed:**

> Whether the rule is about what the entity *is* or what the operation *brings in*, there is no moment between the change and the rule holding: an operation exists in the compiled engine only because the compiler proved every rule survives it, and the values that operation carries in were held to their declared constraints on the way.

**What forces it.** "The change and the check are the same act" says there is a check, that it belongs to the operation, and that it is what makes the rule hold. All three stop being true. Want document line 199 removes the check; line 26 states what replaces it: *"The premises are also what the runtime enforces — governance, not deferred proof, because the compiler has proven those checks *sufficient*."*

**What is lost or risked.**

- The phrase being removed is one of the better lines in the document. It answers "when is the rule checked?" with "there is no when", in nine words. The replacement answers the same question with "before your entity existed", which is a stronger claim and a less immediate one.
- The sentence before and after it are untouched and stay true — "enforced structurally, on every operation that touches the entity", and "No operation can produce a result that violates a declared rule — the invalid configuration is not reachable." The paragraph still reads as one argument. I checked that specifically, because replacing a middle sentence is the easiest way to break a paragraph's spine of reasoning without noticing.

---

## Change 5 — lines 53 and 61, overflow

This one is not a correction to the mechanism and it is the only change here that is arguably better refused. Read the tension before the wording.

**The tension.** Two lines in `compiler-readiness-plan.md`, both written on 2026-07-26 from the same walk, point in slightly different directions:

> **Overflow is an authorized exemption, not a hole** (walk item 15). … Phase 2 reconciles `philosophy.md:53` and `precept-language-spec.md:110`, `:112`, which currently name overflow among compile-time impossibilities — under walk item 23 the aspirational claim may stand, but no document may state it as currently prevented.

> **`philosophy.md` is not corrected down to current behaviour** (walk item 23). It states the target; the code is validated against it. Phase 2's correction list drops the `philosophy.md:53` row.

And `sweep-correction-list.md` ends with: *"`philosophy.md:53` is not on this list, and stays off it. … It is not a correction row. Do not add it."*

Those are consistent if you read them as: do not weaken the claim; do say where it is not met. That is exactly what the specification now does — the fault list at `precept-language-spec.md` §0.7 still names overflow, and a new paragraph at `:270` says the model is deferred, the compiler does not prove it, the runtime does not guard it, *"and the paragraph above states the target the language is being built toward and that target is not being weakened to match."* Principles 10 and 11 carry one-sentence pointers to it. What I propose below is the same posture in philosophy, nothing more. **If you read the instruction as "leave philosophy alone entirely on overflow", refuse this change and nothing else in this document is affected.**

**Line 53 now:**

> The same absoluteness applies to every calculation in the definition. The compiler does not trust that an expression will succeed — it proves it will. Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities. If the compiler cannot prove an expression safe given the declared constraints, it rejects the definition with a specific message identifying what would make safety provable.

**Line 53 proposed** — nothing removed; one sentence added at the end:

> …it rejects the definition with a specific message identifying what would make safety provable. Arithmetic overflow is the one item on that list not met today: the overflow model is deliberately deferred past the first release, so overflow is at present proven by nothing at compile time and guarded by nothing at runtime (see `docs/language/precept-language-spec.md` § 0.7). The claim above states what the compiler is being built to deliver and is not weakened to match current behaviour; this sentence records the single place it does not yet hold.

**Line 61 now:**

> - **Compile-time structural checking.** The compiler catches structural problems — unreachable states, dead-end states, type mismatches, constraint contradictions, unsatisfiable guard combinations, division by zero, overflow, empty collection access, and more — before any entity exists. This is where the "contract" metaphor becomes literal…

**Line 61 proposed** — nothing removed; one sentence added:

> …and more — before any entity exists. Overflow is the exception noted above: it is on this list as the target and is not delivered in the first release. This is where the "contract" metaphor becomes literal…

Line 61 is the twin of line 53 and neither the correction list nor the plan mentions it. It lists overflow the same way; if 53 carries the note and 61 does not, a reader who meets 61 first still reads overflow as prevented today.

**What is lost or risked.**

- A caveat lands in the middle of the strongest paragraph in the document, and this is the paragraph people quote. Every other claim in it is unqualified, so the qualification draws attention out of proportion to the one item it applies to.
- It puts a dated statement into a document that otherwise has no time in it. When overflow is built, someone must remember to take both sentences out, and nothing checks that.
- It is a cross-reference from philosophy into the language specification, which the document does not currently do anywhere (its only outbound link is to a research file). If you would rather philosophy never point at the specification, the sentence works without the parenthesis, just less usefully.

---

## Change 6 — line 55, the carried-constraint paragraph

**Now:**

> When a calculation depends on a value supplied at runtime — an edited field or an event input — the compiler does not guess at that value. It requires the value to *carry* a constraint sufficient to prove the calculation safe, and the engine enforces that constraint the moment the value enters. The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, not a second line of defense.

**Proposed:**

> When a calculation depends on a value supplied at runtime — an edited field or an event input — the compiler does not guess at that value. It proves the calculation safe from something the author declared: a constraint on the field, a constraint on the event argument, the guard on the transition, or a rule that already held before the operation ran. Where none of those closes the proof, the definition does not compile, and the message names the premise that would close it. Constraints on incoming values are enforced the moment the value enters — that enforcement is what makes the premise true, not a second line of defense standing behind the proof.

**What forces it.** The paragraph describes one route to a proof — the value carries its own constraint — and the want document describes four. Line 19: *"Every handler *preserves* every rule, provable from four premise classes: (a) field modifiers, (b) arg constraints, (c) the handler's guard, and (d) all rules holding in the pre-state."* Two of the want document's own worked handlers are not provable the way line 55 describes: `Deposit` is proven from the inductive hypothesis (line 101 — *"No guard or arg bound alone gets there"*), and `Withdraw` is proven from its guard (line 102 — *"the guard is the proof"*). Taken literally, the current paragraph would demand a bound on `Balance` that the want document does not ask for. The same framing was corrected out of the specification by the S11 row and is separately recorded as superseded at `docs/compiler/soundness-and-coverage.md:87`.

The last clause of the current paragraph — runtime enforcement makes the premise true, and is not a second line of defense — is right and survives in the proposal, reworded only to match the wider premise account.

**What is lost or risked.**

- The proposal adds a sentence about compiler rejection naming the missing premise (want:24, want:179). That is behaviour the compiler does not have today, so it enlarges the document's aspiration by one item. It is here because a paragraph that lists four routes to a proof and does not say what happens when none of them works reads as though something is being skipped.
- "Carry" was a compact word doing a lot of work; a list of four premise kinds is longer and flatter to read.
- The proposal drops "The compiler proves a structural fact … never the value itself." That distinction is real and it is worth keeping somewhere, but it belongs to the carried-constraint framing and does not survive the move to four premise classes intact. If you want it kept, say so and it can be restated against the new framing rather than deleted.

---

## Change 7 — line 68, the validator comparison

**Now** (fragment):

> A FluentValidation rule runs when you call it. A Precept rule holds because the runtime structurally prevents any operation from producing a result that violates it.

**Proposed:**

> A FluentValidation rule runs when you call it. A Precept rule holds because no operation the engine can run is able to produce a result that violates it — the compiler proved that of every one of them before the engine existed.

**What forces it.** Nothing forces it. The sentence is ambiguous rather than wrong — the runtime does prevent the violation, in the sense that the only operations it will run are proven ones — but placed directly opposite "runs when you call it", the natural reading is that the prevention happens at the moment of the call. That is the reading the want document removes. Included so the comparison bullets do not quietly keep teaching the old model after the operation list has stopped.

**What is lost or risked.** The sentence gets longer in a bullet that earns its keep by being short, and it now makes a compile-time claim inside a paragraph that is otherwise about runtime behaviour. Refusing this costs nothing except that one sentence keeps its old flavour.

---

## Change 8 — line 69, the state-machine comparison

**Now** (fragment):

> Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — the transition that produces an invalid data configuration is rejected before it commits.

**Proposed:**

> Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — a transition that could leave the entity's data invalid is either refused by its guard when it is attempted, or refused by the compiler before the definition ever becomes an engine.

**What forces it.** This is the removed mechanism stated as plainly as line 19 states it: the transition runs, produces an invalid configuration, and is rejected before the commit. Under the want document there is no such sequence. The two things that do happen are in the want's own example — `Withdraw` over the limit is refused by a guard with an explanation (line 102), and a `Withdraw` with no guard at all does not compile (line 114). The correction list does not have a row for this line; it is one of the six extra places I found.

**What is lost or risked.** One clean clause becomes two, in the comparison a reader is most likely to be reading for a quick answer. And it says "could leave the entity's data invalid" where the old text said "produces an invalid data configuration" — the change from actual to potential is the accurate one under the new model, but it is a shade weaker to read.

---

## The composition seam — a choice, not a correction

**This is not a correction and no ruling settles it.** Nothing in `philosophy.md` currently contradicts the want document on this point; the document is simply silent, and after change 1 the silence starts to matter. It needs a decision from you either way, and both answers are defensible.

**What the seam is.** With the post-mutation evaluation gone, the reason a committed configuration is valid is that the compiler proved it. That proof was performed at some earlier time, by a program, on a definition file — possibly by a different build of the compiler, possibly on a file that has since changed. Something has to join *"a proof was performed"* to *"this running engine is governed by the definition that proof was about."* The want document does not leave that join to trust. From § Certificates:

> Alongside the compiled definition, the compiler emits a **certificate**: a checkable record of the proof — per obligation, the theorem, the premises used (which guard, which arg constraint, which pre-state rule), and the derivation. (want:199)

> **A load-time gate in the runtime** — the same checker, run once when a definition is loaded. The runtime refuses to govern under a definition whose certificate does not verify: no unproven, tampered, or stale definition can ever govern. The guarantee is rooted in verification the runtime performed itself, not in provenance it assumes. (want:204)

`docs/philosophy.md` says nothing about any of this. It does not mention certificates, describes nothing happening when a definition is loaded, and does not say what the engine does with a definition whose proof it cannot confirm. Under the current wording it did not need to: the constraint evaluation at line 19 was the visible reason to believe a committed result, and it ran on every operation regardless of who compiled the file or when. Change 1 removes that reason. The question is whether anything is put in its place in this document.

**One piece of evidence that bears on the choice**, and it is worth having before you decide: the want document's own philosophy flag at line 215 says the sentence needs rewording because *"the constraint evaluation moves to proof-plus-premise-checks, **verified by certificate**."* You wrote the flag with the certificate in it. That is not the same as deciding philosophy should describe certificates — the flag is about what replaces the mechanism, not about how much of the mechanism this document carries — but it leans one way and you should see it.

### Option A — philosophy names the certificate and the load-time refusal

The smallest form is one clause added to change 1:

> …and does not need to: the compiler proved, before any entity existed, that every transition preserves every rule, given the argument constraints and guards the author declared — **and the runtime confirmed that proof for itself, once, when it loaded the definition, refusing to govern under one whose proof does not check out.**

The fuller form is a short paragraph in "What makes it different", after line 57:

> The runtime does not take the compiler's word for any of this. The compiler emits a checkable record of each proof alongside the compiled definition — what was proven, from which declared premises, by what steps — and the runtime walks that record once, when the definition is loaded. A definition whose record does not check out does not govern anything: not an unproven one, not a tampered one, not one left over from an older version of the file. The confidence is in verification the engine performed itself, not in where the definition came from.

**What it costs.**

- It puts a dependency on unbuilt machinery into the document that defines what the product is. `docs/compiler/proof-engine.md:2612` records that the MVP builds no live re-checker and no checker-gated mint. Under walk item 23 an aspirational claim is allowed, so this is not dishonest — but "the runtime refuses to govern under a definition whose certificate does not verify" is an operational promise about how definitions are loaded and distributed, which is a different kind of claim from the rest of the document, and it is one a reader can test.
- It moves philosophy further toward describing delivery mechanism. Right now the four-operation list at lines 17–20 is nearly the only mechanism in the file; everything else is about what is guaranteed and how it is positioned. This adds a second mechanism, and it invites the next one.
- Whichever form you take, it commits the certificate format to being a described part of the product rather than an internal implementation detail that could be replaced later without touching this document.

### Option B — philosophy states the guarantee and leaves the join to the specification

Change 1 goes in as drafted, nothing is added, and the certificate lives in `precept-language-spec.md` § 0.7 and the runtime documents.

**What it costs.**

- The strongest claims in the document — "no invalid combination of lifecycle position and field data can persist" (line 7), "Invalid configurations are structurally impossible" (line 43) — end up resting on a join the document does not describe. A reader is told the compiler proved it and the runtime does not re-check; nothing says how the running engine knows it is governed by the definition that was proven. The obvious objection to the whole model is *you deleted the check, so what is left?*, and the document whose job is to answer objections would be the one place with no answer.
- The want document treats the certificate as the reason a lighter runtime is trustworthy at all, not as an implementation detail — "the guarantee is rooted in verification the runtime performed itself, not in provenance it assumes." Leaving it out means philosophy describes the lighter runtime without the thing that makes it defensible.
- The distance between philosophy and the specification grows in a place where they used to agree. After phase 2 the specification describes certificates and philosophy does not, and the next person doing a doc-sync pass will find the gap and have to decide this question again with less context than you have now.

---

## What I deliberately did not propose

**The trust boundary — and this is a real contradiction, not a wording matter.** Line 7 says "no invalid combination of lifecycle position and field data can persist" and line 43 says "Invalid configurations are structurally impossible", both without qualification. Under the old model those were nearly literally true even for data that never went through the contract, because the next operation's constraint evaluation re-governed whatever it found. Under the want document nothing re-governs it: line 141 makes restore and rehydration trusted and unchecked, and the corrected specification at `precept-language-spec.md:276` and `:278` now says so outright — *"no later operation re-governs it either: there is no post-mutation pass that would, and there are no runtime fault traps"*, and *"a host that writes a value into an entity by some route other than the contract has stepped outside the guarantee, and nothing inside the engine will notice."* So after phase 2 the philosophy states a guarantee without a boundary that the specification now states with one.

I did not propose wording for this because qualifying the headline guarantee of the identity document is exactly the kind of change the approval rule exists for, and because there is a defensible reading where nothing needs to change — the guarantee is about what the *engine* permits, a host that writes around the engine has left the product, and a summary document is entitled not to enumerate that. But you should decide which reading is yours, and it is a second question after the seam. **It is the one thing in this file I would put in front of you even if you refuse everything else.**

**Line 53's overflow claim, corrected downward.** `sweep-correction-list.md` is explicit: *"It is not a correction row. Do not add it."* Nothing here weakens the claim; change 5 adds a note beside it and deletes nothing. If change 5 goes in, the target list at 53 and 61 is untouched.

**Line 60, "constraints applied" in the inspectability bullet.** The want document changes what inspect answers — line 211, *"Inspect can cite provenance, not just verdicts."* But "the engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied" is not false under the new model; which constraints bear on an operation is still part of the answer, it is just cited from the proof rather than evaluated. Adding provenance to this bullet would be an enhancement, not a correction, and it is better raised when the inspect changes in the runtime documents are settled.

**Lines 75 and 84, "the runtime enforces the vocabulary's constraints structurally" and "validating that the entity's data satisfies its declared rules before anything is saved."** Both attribute enforcement loosely to the runtime, the same shade as change 7, but in bullets whose subject is the comparison rather than the mechanism, and both survive a careful reading. Left alone to keep the count of changes to what actually matters. If you take change 7 and want the same treatment applied consistently, these are the two that would follow.

**Anything in `docs/philosophy.md` outside the mechanism.** Positioning, the authoring audience, the word itself, and the comparison table's substance are untouched. No change here alters what Precept claims to be or who it is for.

**Any file other than this one.** `docs/philosophy.md` is unmodified. Nothing was committed. `docs/Working/bugs.md` and `docs/working-reset/phase-5/` were not opened.

---

## The unified diff

All eight changes as one patch, so it can be read without the prose. Produced with `git diff --no-index` against two copies of `docs/philosophy.md` held outside the repository; the tracked file was not modified to generate it. Context is one line each side, because the file's paragraphs are one line apiece and a wider context would be unreadable.

```diff
diff --git a/docs/philosophy.md b/docs/philosophy.md
index d69f4ef..c591cf3 100644
--- a/docs/philosophy.md
+++ b/docs/philosophy.md
@@ -18,4 +18,4 @@ A `.precept` file declares the entity's **fields** (scalar values and typed coll
 - **Inspect**: ask "what would happen if I fired this event?" for every event, from any state. Non-mutating. The answer is always available.
-- **Fire**: execute a transition. The engine validates input arguments, selects a matching transition row via guards, executes mutations, evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.
-- **Update**: edit a field directly. The same constraint enforcement applies. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.
+- **Fire**: execute a transition. The engine validates input arguments against the constraints declared on them, selects a matching transition row via guards, executes mutations, and commits. It does not re-check the entity's rules against the result, and does not need to: the compiler proved, before any entity existed, that every transition preserves every rule, given the argument constraints and guards the author declared. An argument that fails its declared constraint is refused before any mutation runs; a row whose guard the entity does not satisfy is never offered. The invalid configuration never exists. An event can also be explicitly forbidden — a deliberate prohibition, not just an unrouted trigger.
+- **Update**: edit a field directly. The incoming value is held to the constraints declared on that field before it is written; the rules that mention the field hold because the compiler proved this write preserves them, exactly as it proved it of every transition. Which fields can change depends on where the entity is in its lifecycle — the definition declares this, and the engine enforces it.
 
@@ -32,3 +32,3 @@ Precept models business entities. Within that:
 
-**Data and rules are the primary concern.** The entity's field values are what the engine protects. Every constraint expression in the runtime is a data expression — rules, guards, and ensures all evaluate field values and event arguments. The engine's job is to ensure that no invalid combination of field values can persist. Rules are the broadest enforcement surface — they hold regardless of lifecycle position and regardless of which operation ran. A rule can be unconditional (always checked) or guarded (checked only when a declared condition is met), but in either case, the engine enforces it structurally on every operation where it applies. Guarded rules do not weaken the guarantee; they make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.
+**Data and rules are the primary concern.** The entity's field values are what the engine protects. Every constraint expression in the runtime is a data expression — rules, guards, and ensures all evaluate field values and event arguments. The engine's job is to ensure that no invalid combination of field values can persist. Rules are the broadest enforcement surface — they hold regardless of lifecycle position and regardless of which operation ran. A rule can be unconditional (holding of every configuration the entity can be in) or guarded (holding whenever a declared condition is met), but in either case, the engine enforces it structurally on every operation where it applies. Guarded rules do not weaken the guarantee; they make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.
 
@@ -50,7 +50,7 @@ The full guarantee the engine provides is about **configurations** — the pair
 
-  Two guarantees make this absolute — one about data, one about process. The first: every rule declared on the entity's data is enforced on every operation, unconditionally or under precise guard conditions that reflect the business scenario. A rule that says "the approved amount cannot exceed the coverage limit when the policy is in force and the claim type is Standard" is enforced structurally — on every operation that touches the entity. Whether the rule is about what the entity *is* or what the operation *brings in*, it evaluates atomically — the change and the check are the same act. No operation can produce a result that violates a declared rule — the invalid configuration is not reachable. The second: the compiler proves the business process itself is structurally sound. Every declared state is reachable — there are no lifecycle positions that exist on paper but that no sequence of operations can reach. Every non-terminal state has a path forward — there are no dead ends where an entity gets stuck with no way to advance. Required states are guaranteed to lie on every path to completion — no entity can skip a mandatory step. These are business process bugs: a claim that can never reach "Approved" because no transition leads there, a loan that enters a state with no exit. The compiler proves these impossible at definition time — before any entity exists.
+  Two guarantees make this absolute — one about data, one about process. The first: every rule declared on the entity's data is enforced on every operation, unconditionally or under precise guard conditions that reflect the business scenario. A rule that says "the approved amount cannot exceed the coverage limit when the policy is in force and the claim type is Standard" is enforced structurally — on every operation that touches the entity. Whether the rule is about what the entity *is* or what the operation *brings in*, there is no moment between the change and the rule holding: an operation exists in the compiled engine only because the compiler proved every rule survives it, and the values that operation carries in were held to their declared constraints on the way. No operation can produce a result that violates a declared rule — the invalid configuration is not reachable. The second: the compiler proves the business process itself is structurally sound. Every declared state is reachable — there are no lifecycle positions that exist on paper but that no sequence of operations can reach. Every non-terminal state has a path forward — there are no dead ends where an entity gets stuck with no way to advance. Required states are guaranteed to lie on every path to completion — no entity can skip a mandatory step. These are business process bugs: a claim that can never reach "Approved" because no transition leads there, a loan that enters a state with no exit. The compiler proves these impossible at definition time — before any entity exists.
 
-  The same absoluteness applies to every calculation in the definition. The compiler does not trust that an expression will succeed — it proves it will. Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities. If the compiler cannot prove an expression safe given the declared constraints, it rejects the definition with a specific message identifying what would make safety provable.
+  The same absoluteness applies to every calculation in the definition. The compiler does not trust that an expression will succeed — it proves it will. Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities. If the compiler cannot prove an expression safe given the declared constraints, it rejects the definition with a specific message identifying what would make safety provable. Arithmetic overflow is the one item on that list not met today: the overflow model is deliberately deferred past the first release, so overflow is at present proven by nothing at compile time and guarded by nothing at runtime (see `docs/language/precept-language-spec.md` § 0.7). The claim above states what the compiler is being built to deliver and is not weakened to match current behaviour; this sentence records the single place it does not yet hold.
 
-  When a calculation depends on a value supplied at runtime — an edited field or an event input — the compiler does not guess at that value. It requires the value to *carry* a constraint sufficient to prove the calculation safe, and the engine enforces that constraint the moment the value enters. The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, not a second line of defense.
+  When a calculation depends on a value supplied at runtime — an edited field or an event input — the compiler does not guess at that value. It proves the calculation safe from something the author declared: a constraint on the field, a constraint on the event argument, the guard on the transition, or a rule that already held before the operation ran. Where none of those closes the proof, the definition does not compile, and the message names the premise that would close it. Constraints on incoming values are enforced the moment the value enters — that enforcement is what makes the premise true, not a second line of defense standing behind the proof.
 
@@ -60,3 +60,3 @@ The full guarantee the engine provides is about **configurations** — the pair
 - **Full inspectability.** At any point, you can preview every possible action and its outcome without executing anything. The engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied. You see not only what would happen, but why. Nothing is hidden.
-- **Compile-time structural checking.** The compiler catches structural problems — unreachable states, dead-end states, type mismatches, constraint contradictions, unsatisfiable guard combinations, division by zero, overflow, empty collection access, and more — before any entity exists. This is where the "contract" metaphor becomes literal: the compile step validates both the definition's data rules and its business process topology before a single instance is created.
+- **Compile-time structural checking.** The compiler catches structural problems — unreachable states, dead-end states, type mismatches, constraint contradictions, unsatisfiable guard combinations, division by zero, overflow, empty collection access, and more — before any entity exists. Overflow is the exception noted above: it is on this list as the target and is not delivered in the first release. This is where the "contract" metaphor becomes literal: the compile step validates both the definition's data rules and its business process topology before a single instance is created.
 
@@ -67,4 +67,4 @@ Several existing tools govern related but narrower concerns. In a single declara
 - **Hand-rolled service-layer governance:** In most .NET applications, entity lifecycle and data rules are scattered across multiple layers: validation in an input handler, state checks in a decision point, event handlers that mutate fields and trigger side effects, and application-layer conditionals that bypass the whole system. Each layer exists because the one before it wasn't enough. Precept consolidates that governance into a single declaration that can never be bypassed — no code path outside the contract, no window where an invalid configuration exists.
-- **Pure validators (FluentValidation, JSON Schema, Zod, Pydantic):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no structural guarantee against mutation. A FluentValidation rule runs when you call it. A Precept rule holds because the runtime structurally prevents any operation from producing a result that violates it. Precept provides the same rule enforcement, but bound to the entity and enforced structurally — not called by convention.
-- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position. They have no declared field model and no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values. That is the failure mode: a `CancelledOrder` in the right state but with `Total > 0` because no rule required otherwise. Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — the transition that produces an invalid data configuration is rejected before it commits.
+- **Pure validators (FluentValidation, JSON Schema, Zod, Pydantic):** These enforce data shape and field-level rules at a moment in time. They have no lifecycle model, no inspectability, and no structural guarantee against mutation. A FluentValidation rule runs when you call it. A Precept rule holds because no operation the engine can run is able to produce a result that violates it — the compiler proved that of every one of them before the engine existed. Precept provides the same rule enforcement, but bound to the entity and enforced structurally — not called by convention.
+- **Pure state machines (XState, Stateless):** These govern transitions and lifecycle position. They have no declared field model and no data integrity enforcement — an entity can pass through every valid transition and still hold corrupted field values. That is the failure mode: a `CancelledOrder` in the right state but with `Total > 0` because no rule required otherwise. Precept combines the lifecycle structure of a state machine with the data enforcement of a constraint engine — a transition that could leave the entity's data invalid is either refused by its guard when it is attempted, or refused by the compiler before the definition ever becomes an engine.
 - **Rules engines (Drools, NRules, InRule):** Rules engines execute logic reactively — you present facts and the engine infers conclusions or fires matching rules. They are powerful for complex decision logic and forward chaining. But rules are written external to the entity model, and the engine cannot verify at compile time that all rule conditions are satisfiable or non-contradicting. Precept's rules are declarations bound to specific entity configurations — compile-time verified as complete and consistent, structurally enforced on every operation, not inferred from rule evaluation.
```

---

## Both reserved questions, closed 2026-07-27

**The headline guarantee stays unqualified.** Ruled by the owner. `philosophy.md:11` and `:59` keep
"invalid configurations are structurally impossible" with no caveat attached, and the precondition
paragraph carries the condition on its own — that data must not be tampered with between the moment it is
persisted and the moment it is restored.

The reasoning that settled it: a precondition is not an exception, and a headline carrying a caveat whose
explanation sits four sections away is worse than either version. It gives a reader the doubt without the
resolution. The precondition paragraph states the condition once, in a place with room to say why
restoring is nonetheless safe.

**Overflow needs no note, and the question should never have reached him.** It rested on a wording error
of the assistant's. His ruling at walk item 15 defers the arithmetic overflow model **past the MVP**. The
phase-2 correction pass rendered that as "past the first release" in four places in the specification —
`:104`, `:110`, `:112`, `:272`. He does not intend to release the MVP, so the two are not the same claim:
a thing absent from a release is a gap you ship with, and a thing not yet in the first working compiler is
simply not built.

Read correctly, overflow is in scope and not yet implemented, which is exactly the arrangement walk item
23 already covers — the specification and philosophy state the target, and the code is validated against
them. So philosophy keeps overflow among the compile-time impossibilities, unqualified, and change 5 of
this document is **withdrawn rather than refused**: it answered a question that only existed because of
the error.

`docs/compiler/soundness-and-coverage.md:226` already had the right wording in the tree the whole time —
overflow marked **"in-scope, not-yet-implemented"** — in a document nobody consulted when the four
specification sites were written.
