# An outside read of `docs/philosophy.md`

**Status:** Opinion — 2026-07-27. Written by a reader who had never seen this project.
**Method:** Part 1 was written from `docs/philosophy.md` alone, before opening any other
file. Part 2 was written after reading `docs/language/precept-language-spec.md` §0.6/§0.7
and `docs/Working/what-i-want-2026-07-16.md`. Part 1 has not been edited in light of Part 2.

---

## Part 1 — the first read, before checking anything

### What I think this is

A domain-specific language and compiler for business entity rules, plus an embedded .NET
runtime. You write one `.precept` file that declares an entity's fields, its optional
lifecycle states, the events that move it, and the rules that must always hold about its
data. The compiler proves — before any entity exists — that no operation the definition
declares can leave a rule broken, and refuses to compile if it cannot prove that. The
compiled result is a library you host in your own application; storage and presentation
stay yours.

I can say that in my own words after one read, which is the first good thing about the
document. Most documents in this category leave me unable to state what the product does
without using their words back at them.

### What I believed, and why

**The closed-set argument (§ What Precept is, para 4).** This is the best paragraph in the
document, and it is the reason I believed the headline at all. It does not assert that
invalid data is impossible; it explains the mechanism that would make it so — only four
things can happen to an entity, an operation exists in the compiled engine only because
the compiler proved every rule survives it, and an operation nobody declared has no proof
and therefore no existence. That is an argument. I could attack it, and I do below, but
attacking it is possible because it is actually there. Everything else in the document
leans on this paragraph, and the document seems to know that.

**Prevention versus detection.** Cleanly made and concretely illustrated. The observation
that "the rule-owner's pain has never been the absence of a rule — it is the gap between
declaring a rule and having it hold on every path" is the sentence that made me think the
author has actually watched this fail in a real system, rather than having read about it.

**The comparator section.** This bought more credit than anything else, because of how it
treats the competition. Rules engines are "powerful for complex decision logic." Database
CHECK constraints "work." OPA and Cedar are "powerful for authorization and cross-cutting
policy." And there is a whole section listing what Precept does *not* replace. Documents
that are selling do not usually concede that the incumbent works. I read the rest of the
document more generously because of this section, which is worth noting as a mechanism:
the honesty here is spending credit the strong claims elsewhere need.

**"What cannot be proven does not compile."** Believable precisely because it names the
cost. A tool that claims total prevention and no false rejections is lying; this one says
outright that the price is definitions that will not build, and that the error message
names the missing premise. That is what an honest version of this claim looks like.

### What I did not believe, and where I stopped

**Arithmetic overflow (§ Prevention, para 4).** "Division by zero, arithmetic overflow,
empty collection access: not risks managed at runtime, but compile-time impossibilities."
I stopped on this and re-read it. Division by zero I believe: you need a proof that one
subexpression is non-zero, and the machinery described — field constraints, argument
constraints, transition guards, rules that already held — plausibly gets you there.
Empty-collection access I believe for the same reason. Overflow is a different animal. It
requires interval reasoning across every arithmetic node in every expression, including
chained multiplication, and it needs a declared upper *and* lower bound on every value
that can enter that chain. Proving it also means either rejecting a great many ordinary
definitions or having range inference far stronger than anything else the document
describes. The document offers no mechanism specific to it and no acknowledgement that it
is harder than the other two. Naming it in the same breath as division by zero is the one
place the document reads as if it is listing desirable properties rather than reporting
proven ones. This was my first "hold on" of the read, and it arrived unprompted.

**"There is no fifth thing."** There is. The document tells me so itself, two paragraphs
later: the engine "embeds in a .NET application as an ordinary library," storage is "the
application's business," and later, "the ORM writes the governed result." So the entity is
loaded from somewhere. The four listed operations — created, moved by an event, edited,
previewed — are all things that happen to an entity the engine already has. None of them
is *obtaining* one from a database row. A row written by a bulk import, by a report script,
by a hotfix, or by an older version of the definition file becomes an in-memory entity that
the engine never governed and, on the document's own account, does not re-check ("nothing
re-evaluates the rules after the change, and nothing needs to"). The closed set is closed
over the engine's own operations, which is a real and valuable property. It is not closed
over the entity's life. "Invalid configurations are structurally impossible," unqualified,
claims the second and has only established the first. I noticed this before anyone pointed
it out to me, and it is the thing I would have gone looking for in the spec first.

**The runtime re-verifying the proof (§ Prevention, para 5).** I stopped and re-read this
too, for the opposite reason: it is a *stronger* claim than the surrounding text and it is
made in three sentences with no mechanism at all. Proof-carrying code between a compiler
and a separate runtime is a serious engineering commitment — there has to be a proof
artifact, a checker independent enough to be worth having, and some answer to what
verification costs at startup. The document asserts all of it and shows none of it. If it
is true it is one of the most impressive things here and deserves more than a paragraph.
If it is aspirational, it is the most damaging sentence in the document, because it is the
one a cautious reader would build an integration decision on.

**"Verified complete and consistent at compile time"** (comparator entry on rules engines).
Consistent I understand — satisfiability, contradiction detection, and the document
supports it elsewhere. *Complete* is undefined and, as far as I can tell, undefinable
without saying complete with respect to what. Complete rule sets are not a property a
compiler can check; you cannot know that the author declared every rule the domain needed.
This is a word that slipped in from marketing register and it weakens a sentence that did
not need it.

**"The primary author of a `.precept` file is the person who owns the rules — a domain
expert or business analyst."** Every DSL in the last forty years has claimed this, and
almost none of them achieved it. I am not saying it is false. I am saying the document
gives me no way to judge it, because it never shows me the language. Not one line of
syntax appears anywhere in this file. A single ten-line example would let me form my own
view in five seconds, and its absence in a document that makes this specific claim reads
as a choice.

### Where I skimmed

The comparator list, from the fourth entry on. It is good material but the shape repeats —
here is the neighbour, here is what it genuinely does, here is the axis Precept differs on
— and once I had the pattern I stopped reading closely. Three entries would land harder
than eleven.

"The word itself" I skimmed and would cut. Etymology of the product name is not an
argument, and placing it last means the document ends on its least substantial section.

### Strongest sentence

The closed-set justification: *"The set is closed because the guarantee requires it: an
operation exists in the compiled engine only because the compiler proved every rule
survives it, and an operation nobody declared has no such proof and no existence."*

It is the only sentence in the document that explains why the central claim could be true
rather than restating that it is. Everything persuasive here descends from it.

The most quotable sentence is a different one — *"The difference is not which bugs get
caught. It is which bugs can exist."* — and it is very good, but it is a summary of the
argument, not the argument.

### Weakest sentence

*"Division by zero, arithmetic overflow, empty collection access: not risks managed at
runtime, but compile-time impossibilities."*

One item in that list is not like the other two, and the sentence's construction —
three-item list, colon, flat declarative — is designed to make them feel equivalent. If
overflow turns out to be unproven, this sentence does more damage than its own subject
matter, because it teaches me that the document's lists are aspirational and I then have
to re-audit every other list in it. Which is what happened.

Runner-up: *"Invalid configurations are structurally impossible"* in § The hierarchy of
concepts. Standing alone there, stripped of the closed-set paragraph that earned it, it is
a bare assertion, and it is the sentence a reader will quote back when they find a row in
their database that violates a rule.

### Marketing rather than engineering

Less than I expected. Four places:

- "No existing tool provides that combination." Unfalsifiable as stated. The list that
  follows partly earns it, but the sentence is doing the work of the list.
- "This is where the 'contract' metaphor becomes literal." A flourish. Harmless, but it is
  in the middle of the most technically serious paragraph and it breaks the register.
- "The word itself" section entire.
- "Complete and consistent," discussed above.

The dominant register is engineering, and the document is noticeably better written than
this genre usually manages. The failures are not tonal — the tone is fine — they are
specific overclaims that a technical reader can name.

### Would I keep reading?

Yes. I would go straight to the language specification, and I would go there with two
questions I would want answered before I did anything else: does anything prove overflow,
and what happens to an entity loaded from a database that something else wrote. That the
document produced two sharp, checkable questions rather than a vague unease is itself a
compliment to it.

---

## Part 2 — after checking against the specification

*(written after reading `docs/language/precept-language-spec.md` §0.1, §0.3, §0.5, §0.6,
§0.7, §0.8 and `docs/Working/what-i-want-2026-07-16.md`)*

The short version: the specification is a much more careful document than the philosophy,
and almost every place the philosophy overreaches, the specification already contains the
sentence that would fix it. This is not a case of a document promising things the project
has not thought about. It is a case of a document dropping qualifications its own
specification writes out in full, sometimes in the specification's own words, sometimes
with the specification explicitly saying *why* the qualification must be stated. That
distinction matters for how bad this is: nobody here is confused. The philosophy is
just quieter than the work behind it.

Findings are ordered by how much they would change a reader's decision.

### 1. Overflow — the specification disowns the claim three times, and says why

Philosophy, § Prevention:

> Division by zero, arithmetic overflow, empty collection access: not risks managed at
> runtime, but compile-time impossibilities.

and:

> A definition that compiles without diagnostics therefore has none of these defects: …
> no division by zero, no overflow, no empty-collection access …

Specification §0.1, principles 7, 10 and 11 each carry the same bolded sentence:

> **Overflow is currently exempted from this principle**, by an explicit ruling deferring
> the arithmetic overflow model past the first release; see §0.7.

And §0.7 devotes a paragraph to it:

> **Overflow is where this does not hold today, and saying so is the point of this
> paragraph.** … The compiler does not prove the absence of overflow, and the runtime
> carries no guard against it, so a definition that compiles clean can still overflow when
> an expression is evaluated. The paragraph above states the target the language is being
> built toward and that target is not being weakened to match; **this note records the one
> place the target is not yet met, so no reader takes the list above as a description of
> current behaviour.**

I was told before writing this that the disclosure question is reserved to the owner, and
it is not my call. But I was also told to say if I think it is a problem, so: yes, and the
specification makes the case better than I can. The specification does not merely note the
exemption — it states the purpose of noting it, which is *so that no reader takes the list
as a description of current behaviour*. The philosophy publishes that exact list, to a more
external audience, without the note. Whatever the right resolution, it cannot be that the
specification's stated reason for the disclosure applies to the internal document and not
the outward-facing one. The specification's own framing also shows the cheap way out: it
distinguishes the target from current behaviour and refuses to weaken the target. A
philosophy document is allowed to state targets. It is not allowed to state them in the
present indicative with no marker, which is what this does.

Note also that the specification has already found the wording that would let the
philosophy keep its ambition: "states the target the language is being built toward and
that target is not being weakened to match." One sentence of that shape costs the document
nothing rhetorically.

### 2. The fifth thing exists, and both source documents name it

Philosophy, § What Precept is:

> It is created in its initial state with its declared defaults. An event moves it,
> carrying typed arguments. A field its definition declares editable is edited. And any of
> this can be previewed … **There is no fifth thing.**

Specification §0.7, "The boundary of the guarantee":

> Data that enters *outside* the contract is not re-proven. **Restored state is trusted as
> valid at the time it was persisted: hydration is fast and does not re-validate**, exactly
> as a direct database read trusts the rows it loads … Such state is reconstituted, not
> re-governed on load, and no later operation re-governs it either.

`what-i-want-2026-07-16.md`:

> Ingress is exactly two points: **event args** … and **editable-field writes**.
> **Restore/rehydration is *not* ingress** — data coming back from persistence is trusted
> and unchecked, because it was governed when it was written.

Hydration is the fifth thing, it is a real operation on a governed entity, and both
documents behind the philosophy name it explicitly. I want to be precise about which
sentence is the problem, because I do not think it is the headline.

"Invalid configurations are structurally impossible" is defensible as a statement about
what the engine can produce, and §0.3 of the specification makes the same unqualified
claim, so the philosophy is at least consistent with canon there. "There is no fifth thing"
is not defensible. It is a specific, checkable, false statement about the operation set,
made two paragraphs before the document itself tells me the entity lives in a database the
application owns. And it is the sentence a reader will test first, because the moment you
say a set is closed, the reader starts hunting for the omitted member. I found it in under
a minute, from the philosophy alone, without knowing the answer was reserved.

The specification also does something the philosophy does not, and it is the single most
credit-earning move in either document:

> **That makes one assumption, and it is worth stating as an assumption rather than leaving
> it inside a proof.** Every proof that leans on "this rule already held before the
> operation" is *proven* for data that arrived through the contract and *assumed* for data
> that did not. A host that writes a value into an entity by some route other than the
> contract has stepped outside the guarantee, and nothing inside the engine will notice.

That paragraph is why I would trust these people. It is also why the philosophy's silence
is strange: the hard version of the disclosure is already written, in publishable prose, by
the same author, and it makes the project look *better*, not worse. An engineer who reads
"nothing inside the engine will notice" concludes the team knows exactly where its edges
are. An engineer who reads "there is no fifth thing," then finds hydration, concludes the
opposite about everything else in the document.

### 3. Structural soundness is proven over *structural* paths, and the sharpening step is unbuilt

This one was not flagged to me as reserved, and I think it is the finding with the least
attention on it.

Philosophy, § Prevention:

> Every declared state is reachable — no position that exists on paper but that no sequence
> of operations can arrive at. Every non-terminal state has a way forward — no dead end an
> entity enters and cannot leave.

Specification §0.5, "Overapproximation rule":

> Structural graph analysis treats all edges as traversable regardless of `when` guards —
> it overapproximates reachability. This is sound: structural guarantees cannot account for
> guard-dependent path selection because guard satisfaction depends on runtime data. A
> modifier that claims "all paths visit this state" means all *structurally declared* paths,
> not all guard-satisfiable paths.

Overapproximating reachability is conservative in the safe direction for `required` — it
can refuse a true claim, which is friction, not unsoundness. But it runs the *other* way
for the two claims the philosophy makes. Treating every edge as traversable means the
analyzer believes more states are reachable than truly are, so a state whose only inbound
row carries a guard that can never be satisfied passes as reachable. Likewise a state whose
only outbound row is guard-dead passes as having a way forward. Those are exactly "a
position that exists on paper that no sequence of operations can arrive at" and "a dead end
an entity enters and cannot leave" — the two things the philosophy says cannot exist.

The fix for it is enumerated in the specification, and its status is the point:

> | 12 | Sharpened reachability/routing diagnostics from proven-dead guards |
> **Specification-only** — depends on #9 + #10 |

> Obligation 12 (sharpened reachability/routing from proven-dead guards) is the one
> remaining specification-only item; an author who depends on it today supplies the
> equivalent hand-written constraint.

So the philosophy's process guarantee rests, in its guard-dependent cases, on precisely the
one obligation the specification marks as not built. `what-i-want` states the strong version
too ("A state no path reaches is rejected"), but that document is explicitly an ideal-world
statement, and it is allowed to. The philosophy is not describing an ideal.

To be fair to the document: the structural cases — a state with no inbound row at all, a
non-terminal state with no outbound rows — *are* caught today, and those are the common
real-world defects. The claim is not empty. It is over-generalised by exactly one word, and
"structurally" is that word.

### 4. "Complete and consistent" — half of that is unsupported

Philosophy, comparator entry on rules engines:

> Precept's rules are bound to the entity, **verified complete and consistent** at compile
> time, and enforced on every operation.

Consistency is real and specified: §0.6 obligation 7 rejects a definition whose rules admit
no satisfying configuration, and obligation 8 reports tautological ones — both marked
implemented. I could find no support anywhere for *completeness*. There is no definition of
what a rule set would be complete with respect to, and I do not think one is possible: a
compiler cannot know the author declared every rule the domain requires. This reads as a
word that arrived for rhetorical balance with "consistent." It should go, and losing it
costs the sentence nothing — "verified consistent at compile time" is already a claim no
rules engine in that comparison can make.

### 5. Editable-field ingress: the two source documents disagree, and the philosophy sits on the weaker one

Philosophy:

> every value that enters from outside — an event argument, an edited field — is held to
> its declared constraints at the moment it enters.

Specification §0.7, "Governance":

> Every value entering the entity from outside the definition — event arguments,
> construction inputs, direct field edits — is held to the constraints declared on the slot
> it enters … It is also *only* the field's own declared contract — **ingress makes the
> incoming value legal for the slot it enters, and nothing wider.**

`what-i-want`:

> For an editable-field write, it's the field's modifier-rules **and every rule that
> mentions the field** — an edit is an arbitrary incoming value that no static proof can
> cover, so evaluating those rules at the ingress point is exactly the premise that closes
> the editable write sites in the compiler's proof.

These two cannot both be right, and the difference is not cosmetic. Take the worked example
in `what-i-want`: `rule Balance >= -OverdraftLimit`, with an editable field. Under the
specification's reading — the slot's own contract and *nothing wider* — an edit that lowers
`OverdraftLimit` is checked against `nonnegative` and nothing else, and the relational rule
can break with no proof having covered it and no re-check afterwards ("Nothing re-evaluates
the rules after the change, and nothing needs to"). Under `what-i-want`'s reading, every
rule mentioning the field runs at that ingress point, and the hole closes.

I am not asking the philosophy to resolve this; it is a specification question. But the
philosophy's sentence is the only place a reader is told what happens on an edit, and its
phrasing ("its declared constraints") reads naturally as the narrow one. If the narrow
reading is the intended one, the data guarantee has a hole at every editable field that
participates in a multi-field rule, and that is a much larger finding than anything else on
this list. If the wide reading is intended, §0.7's "and nothing wider" needs fixing. Either
way somebody should decide before the philosophy's sentence is read as settling it.

### 6. "At every moment" is contradicted by an open question

Philosophy, § What Precept is:

> the entity's data satisfies its declared rules **at every moment**, whether those rules
> involve lifecycle transitions, field constraints, or both.

`what-i-want`, § Open questions:

> **Time-referencing rules.** A rule like `ExpiryDate > today` can become false with no
> event in flight — nothing writes `today`, there is no ingress point for it, and the
> inductive model has no step where pure passage of time fits. Do rules that reference the
> current moment exist at all …? Deliberately left open.

The induction proves establishment at construction and preservation across operations.
Between operations, nothing is proven and nothing needs to be — unless a rule's truth value
can change without an operation, which is exactly what a rule referencing the current moment
does. "At every moment" is the strongest available phrasing of the guarantee and it is the
one phrasing this open question directly threatens. This is not an argument for weakening
it; it is an argument that the open question is more urgent than "deliberately left open"
suggests, because a public sentence already depends on the answer.

### 7. Runtime proof verification is real as a requirement, not yet as a description

Philosophy:

> The runtime does not take the compiler's word for any of this. Before an engine will
> govern under a definition, it verifies that definition's proof for itself. One that does
> not verify governs nothing — not an unproven definition, not a tampered one, not one left
> over from an older version of the file.

This is supported, but by documents of a particular kind, and the difference matters.
Specification §0.1 principles 10 and 11 refer to "the proof certificate the runtime verifies
once, when it loads the definition," and §0.6's admissibility criteria require a certificate
"drawn from a small, spec-enumerated vocabulary (the `CertificateSteps` catalog)." §0.6 is
labelled at its head as a **design contract** — "This section captures the language's
*requirements* for the proof engine" — and its implementation-status table covers obligations
1–13, none of which is the certificate checker. The detailed description lives in
`what-i-want`, which is `Status: Draft`, describes itself as "what I would want **in an ideal
world**," and says the task after it "will be to understand whether that ideal is feasible."

So: specified, wanted, precedented (the document cites Necula and Lee), and not stated
anywhere I read as built. The philosophy states it in the present indicative, in the same
register as everything around it. This was the sentence I stopped at on my first read for
being unusually strong and unusually unsupported, and checking the sources moved it from
"unsupported" to "supported as an intention." That is a meaningful improvement and it is
still not what the sentence says.

The same applies more broadly to the induction model. §0.7 asserts base case and inductive
step as the mechanism; §0.6's implementation-status table enumerates thirteen obligations
and establishment and preservation are not among them, so that table cannot be read as
evidence the induction is built. I did not go into the code, so I am not claiming it is not
— only that the specification, read as a whole, does not tell me it is, while the philosophy
does.

### 8. Non-linear rules — a qualification the philosophy technically covers but does not convey

Philosophy:

> Where a calculation depends on a value supplied at runtime, the compiler proves it safe
> from something the author declared — a constraint on the field, a constraint on the event
> argument, the guard on the transition, or a rule that already held before the operation
> ran.

That is a faithful summary of §0.7's four premise classes; the mapping is exact and I want
to record that the philosophy got this right. What it does not convey is §0.7's next
paragraph:

> A rule whose post-state condition is *non-linear* … is proven only when premise class
> (c), the handler's guard, or class (b), a constraint on an event argument, states the
> post-state condition **directly** … Where no such premise exists the compiler **rejects
> the handler**.

Formally this is inside the philosophy's claim — prove from a declared premise, or reject.
Practically it means the author of any rule multiplying two fields must restate that rule
verbatim in the guard of every handler that writes *either* field. That is a real authoring
cost, it is the cost most likely to surprise the domain expert the document says is the
primary author, and §0.7 flags it precisely because "the linear cases close so quietly that
the harder ones could look as though they come along for free." The philosophy is the
document where they look free. This is not an overclaim; it is the qualification most worth
one sentence.

### 9. Where the specification made me *more* confident than the philosophy did

Two things, recorded because a review that only lists faults is not an assessment.

**The authoring-audience claim, which I called closest to marketing on the first read, is
backed harder than I expected.** Specification §0.8 does not merely repeat it — it converts
it into five constraints on what the language is permitted to look like, and says so
outright: "The constraints below are not aspirational — they are *forcing functions* on
what the language is allowed to look like." Keyword-anchored grammar, mandatory `because`
clauses, diagnostics written in domain vocabulary rather than compiler vocabulary, no opaque
proof artifacts, no iteration. It also makes the claim reviewable: a design that assumes a
developer-tier reader without acknowledging the tradeoff is a blocker. I still think the
claim is unproven — it is a claim about humans, and only users settle it — but it is a
disciplined bet with visible costs, not a slogan. The philosophy should probably say one of
those five things out loud; naming a forcing function is more persuasive than naming an
audience.

**The rejection side is enumerated.** `what-i-want` contains a fourteen-row table of
one-line mutations of a worked example, each of which "**must be a compile-time rejection**
with a teachable message naming the missing premise," and the framing around it is the best
sentence in either document: "an accepted file demonstrates nothing by itself — the
compiler's power is visible only in what it refuses." That is a testable specification of
the guarantee, written by someone who understands that the interesting claim is the negative
one. It is also the single most convincing artifact I read, and no trace of it reaches the
philosophy.

### 10. One piece of housekeeping

`what-i-want` § Certificates ends with:

> **Philosophy flag (not resolved here):** `docs/philosophy.md` currently describes Fire as
> "evaluates all applicable constraints against the resulting configuration, and commits
> only if every constraint holds." Under this want, that sentence needs a deliberate,
> owner-approved rewording.

That sentence is no longer in `docs/philosophy.md`; the current text says the opposite
("Nothing re-evaluates the rules after the change, and nothing needs to"). The flag is
closed and can be struck when that document is next touched.

---

## Part 3 — the opinion

This is a good document with a specific, repeating defect.

What is good is not incidental. It states what the product is in the first sentence and I
could restate it after one read. It argues its central claim rather than asserting it —
the closed-set paragraph is genuine engineering reasoning and everything persuasive
descends from it. It names the price of the guarantee ("what cannot be proven does not
compile") instead of pretending there is none. And its treatment of the competition is
better than the competition's treatment of itself: database CHECK constraints "work," rules
engines are "powerful," and there is a whole section on what Precept does not replace. That
section is why I read the rest generously, and it is worth understanding as a mechanism —
the honesty in the comparators is buying the credit that the strong claims elsewhere spend.
Which is exactly why the overclaims are expensive: they are drawing on an account the
document filled itself.

The defect is that the philosophy is consistently *less careful than its own sources*. That
is an unusual failure mode and, I think, a fixable one, because it is not a thinking problem.
Every finding above except one is already answered somewhere in the specification, often in
prose ready to lift. §0.7 states the overflow exemption and says explicitly that it exists so
no reader mistakes the target for current behaviour. §0.7 states the hydration boundary and
then does the genuinely admirable thing of naming the residual assumption as an assumption.
§0.5 states that graph analysis reasons over structurally declared paths. §0.6 marks the one
unbuilt obligation. The philosophy carries none of it. The gap between the two documents is
not a difference of opinion about the facts; it is a difference of nerve about stating them.

And the nerve is misplaced, because the qualifications make the project look *better*. I do
not have a way to verify a compiler that claims total prevention. I do have a way to
evaluate a team that tells me where its guarantee stops, because that is a thing almost
nobody does, and a team that has located its own edges precisely has usually done the work.
"A host that writes a value into an entity by some route other than the contract has stepped
outside the guarantee, and nothing inside the engine will notice" is the sentence that would
make me trust this project. It is in the specification. It is not in the document that
outsiders read.

The one finding that is *not* already answered internally is number 5, the editable-field
ingress conflict, and I would look at that before anything on this page. The specification
says ingress is the slot's own contract and "nothing wider"; the want document says it is
the field's modifiers plus every rule mentioning the field. If the specification is the
intended reading, there is a hole in the data guarantee at every editable field in a
multi-field rule, with no proof covering it and — by the philosophy's own "nothing
re-evaluates the rules after the change" — nothing behind it. That is not a documentation
problem.

**Weakest thing in the document, on reflection.** Not the overflow sentence, though it is
the worst single claim. It is "There is no fifth thing." The overflow list is one item too
long in a document that is otherwise about targets, and a reader can forgive a target stated
flatly. A closed set with a missing member is different: it invites the reader to check, the
check takes a minute, and it fails. Once that fails, the reader re-audits everything else,
which is what I did and why this file is long.

**The one thing I would change.** Make the closed set say what it is closed over. Not by
adding a caveat to the headline — the headline is fine, and hedging it in the opening
paragraph would cost more than it buys. By finishing the argument in the paragraph that
makes it. The closed set is closed over what the engine does to an entity it holds; it was
never closed over the entity's whole life, and saying so where the mechanism is being
explained reads as precision, not retreat. Something in the shape of: *these four are
everything the engine can do to a governed entity; obtaining one from storage is not one of
them, and restored data is trusted as governed when it was written.* One clause, in the
strongest paragraph in the document, and the sentence that a careful reader currently
disproves in sixty seconds becomes the sentence that convinces them the author has counted.

Everything else on this list is a word: "structurally" before "reachable," "consistent"
without "complete," a marker on the overflow list of the kind §0.7 already writes. Those are
edits. The closed set is the one that is worth thinking about.

**Would I build on it?** On this evidence, I would take the meeting. The specification is
serious work, the rejection table is the kind of artifact people write when they intend to
be held to something, and the parts of the guarantee that are built appear genuinely built.
I would want the certificate checker's status in writing, and I would want to know which
reading of editable-field ingress is correct. Neither of those questions makes me close the
tab. The philosophy nearly did, and it should not be the weakest document in the stack.

