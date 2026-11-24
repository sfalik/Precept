# Finishing the Precept design: why it did not converge, what "finished" means, and a shorter path

Written 2026-11-13 against branch `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository was edited. This report takes the goal as given: a complete and consistent design first; acceptance criteria, tests and code afterwards. It does not propose moving to code and does not use the compiler's behaviour to settle anything.

---

## One-page summary

### What went wrong

1. **The unfinished part of the design was never the part being worked on.** What the want document asks for that canon does not yet contain is the proof model: which obligations a definition owes and where, what a proof may rely on, how an obligation is closed or refused, what a certificate holds and what the checker checks. In canon that model exists as a promise repeated in four documents and a document that calls itself a skeleton (`docs/compiler/soundness-and-coverage.md:7`, with sections 2, 4 and 5a reading "Intent — fill per slice"). The reset plan put that work at phases 4 to 7, behind a phase that reconciles every canonical document and a phase that numbers every normative statement. Phase 2 ran a month and the proof model was not touched.

2. **"Done" was defined as "a fresh reader finds nothing new" across about 31,000 lines.** Every new way of looking found more: the pattern search found 145, a cold reader found 19 more, four scripts found 14 more, and one kind of check is still unbuilt (plan:1035–1106). That search does not approach zero, because the documents state the same facts in several places.

3. **Most of what phase 2 could not close is not design in the sense of your want document.** Of the open findings, four are disagreements with the want document. The other twenty-seven are C# record shapes, diagnostic code counts and pass numbering stated differently in two compiler or runtime documents (plan:1059–1100). Those documents hold 216 C# code blocks. They are in "canon", so your point that canon must have one position applied to them, correctly. The cheaper answer is to take that material out of what the design is measured against, not to reconcile it.

4. **Nothing except your word could close a question, and every agent claim needed another agent to check it.** Each rule behind that was a fair response to a real failure. Together they made each question produce a write-up, a review, and sometimes a review of the review, and then come to you anyway.

5. **Each earlier diagnosis was accurate and each answered with more process run by the same agents.** The plan's own stop rule (three failed attempts means stop and ask you) never fired: the attempts column for phase 2 still reads 1 (plan:194).

### What "finished design" means, and how it is checked

A fixed, small set of documents: the philosophy, the language documents, **one** proof-model document of about fifteen pages, one page of structural rules, and the runtime's behaviour as an author or caller sees it. Compiler and runtime internals are labelled implementation notes and are outside the set.

- **Complete** means four finite lists are full: every claim in the want document points at the sentence in the design that states it (about seventy rows); the proof-model document answers eight named questions; every construct in the language's own lists is either given a way to be proven or a stated refusal; every open question has your answer, a recorded default, or your word that it waits.
- **Consistent** means: each fact is stated in one place (checked by script); two independent agents, given only the design and a set of worked `.precept` examples, work out by hand the same obligations and the same accept-or-reject result for each, including all fourteen rows of your "what must not compile" table; and agents told to write a definition the design accepts but that can break a rule fail to produce one.

None of this compiles anything. The examples are worked on paper against the written design.

### The proposed path, four to seven weeks

1. Stop phase 2 unsigned. Archive the thirteen-phase plan and its records. Replace with one page.
2. Fence the design set (two to three days) and build the want-claim map.
3. Write the proof model in one document, one author with the whole design set in view (one to two weeks).
4. Check it with worked examples and attack rounds, at most three rounds (one to two weeks).
5. Produce the list of what the first proof set refuses, each with an example and its message. You read that list and decide: accept, widen, or narrow the language.

You get one batch of questions now, then one page a week with at most three questions, each as a `.precept` example with a default that is taken if you do not answer.

### Decisions needed from you

To start:

1. May phase 2 stop without sign-off, and the thirteen-phase plan be archived?
2. May the compiler-internals and runtime-internals documents be marked "implementation notes, not part of the design" and left unreconciled until those parts are built?
3. Where the design is unclear on whether a proof may rely on something, may the default be "refuse", recorded on a short list you can overturn?
4. When an editable field is edited, does the engine check every rule that mentions the field against the proposed value before accepting it, as line 143 of your want document says? (Yes or no.)
5. May a rule mention the current time, or does time belong only in guards? (Canon currently says rules may; your want document left it open.)
6. Two non-terminal states that lead only to each other: is that a dead end? (Your want document says no; the graph analyzer document says yes.)

Can wait a week:

7. Does `on <Event> ensure` stay as a check on event arguments at entry, alongside guards?
8. Do a state's entry actions run at construction, and on a transition back into the same state?
9. Your worked example's `ReduceLimit` guard reads the optional `MonthlyRepayment` without checking it is set, which the language refuses. May the example be corrected and shown to you?

---

## Part 1 — Why the design did not converge

### 1.1 The hard part was scheduled last

The want document's centre is lines 14–26: proof by induction, a base case, a step, four kinds of premise, obligations at every write of every field a rule mentions, and rejection naming the missing premise. Lines 197–213 add the certificate and its checker.

Where canon states this today:

- `precept-language-spec.md:268` restates the promise in one paragraph. Its section 5, "Proof Engine", is twenty lines that point elsewhere (`:2177–2197`).
- `docs/compiler/proof-engine.md` is 2,653 lines describing the engine as built: obligations come only from requirements the catalog attaches to operators, functions, accessors and actions (`:379–385`), there are thirteen kinds, and neither establishment nor preservation is one. It says so itself at `:2614`: "there is no general carrying of 'every rule held before this operation' into a handler's discharge."
- `docs/compiler/soundness-and-coverage.md` is named as "the canonical home for how the prove-or-reject guarantee is made sound" (`:15`). Its status line is "Design (skeleton)". The verdict, counterexample and certificate-format sections are one-paragraph statements of intent. Its coverage tables list fault obligations only; there is no row for "a handler preserves a rule" or for an edit to an editable field.

So the design the want document asks for is about ten to fifteen pages that have not been written. Two attempts were made before the reset: the obligation matrix, which tried to list the model cell by cell across six properties multiplied together (4,592 cells in one bundle), and the establishment and preservation design, which failed to lock three times in two days (plan:497–500). The reset plan drew the right lesson about shape (walk the expression forms; do not multiply properties, plan:1653–1684) and then placed the work behind phases 2 and 3.

Phase 3 alone asks for every normative statement in the language documents to be numbered — the plan's own estimate is between 546 and 2,205 of them and "likely much nearer the top" (plan:1460–1466) — plus machine-readable lists and a build check. That is preparation for acceptance criteria, which by your own ordering comes after the design.

Why it was ordered this way: two standing rules, "reconcile stale documents before feeding agents" and "the canonical documents are what completeness is measured against." Both are sound. Applied to everything labelled canonical, they made the cost of starting the proof model proportional to 31,000 lines, when the proof model depends on a small part of them: the language semantics in spec section 3A (the eight writers at `:1984–1995`, the execution order at `:2003–2012`, the constraint kinds at `:1859–1895`) and each type's arithmetic properties.

### 1.2 The finish line was an open-ended search

Phase 2's exit condition 4 asks that a reader who did not build the register finds no disagreement that is not already a row (plan:995). The record:

- Sized at 35–55 disagreements; the register reached 145 rows.
- A cold reader then found 28, of which 19 were in no row (`phase-2/cold-read-register-diff.md`, "The numbers").
- Four scripts built the next day found 19, of which 14 were new (plan:1089).
- One kind of check, an ordering stated twice, is still unbuilt (plan:1104).
- When a careful reader was tested against twelve known items, it missed four in its own territory (memory, `project_reset_phase_1_2_2026_07_28.md`, "Reverse check").
- The cold read covered only the compiler and runtime documents, because phase 3 was going to reread the language documents (plan:372). So "none of the findings are in the language documents" partly means nobody looked there the same way.

A check of the form "nobody finds anything" cannot close on a body of text that repeats itself. The phrase "every handler was proven to preserve every rule" or its close variants appears in three documents; statements denying the post-mutation pass appear in nine (my grep counts). Each repetition is a place for the next edit to miss. Phase 2 changed 926 lines of canon and removed 954, across 22 files (`git diff --shortstat b93d1db1 HEAD`), and produced 17,749 lines of records doing it.

I do not conclude from this that a written design cannot be finished. I conclude that this one was being checked against the wrong thing: prose against prose, across everything, instead of a small design against worked cases.

### 1.3 Canon mixes three kinds of content

1. What the language means and what the product guarantees. This is design, and it is yours.
2. How the compiler and runtime are organised inside: record members, pass order, how many diagnostic codes exist. The code will own these when it is written.
3. The state of the current build: "implemented", "corrected 2026-05-28", "gap against the target". The language specification's own proof-engine contract carries a block of this (`precept-language-spec.md:235–258`).

The findings still open under condition 4 sort almost entirely into the second kind: `FieldDescriptor` with seven members against eight, `Compilation` with two memberships, 147 codes declared and 164 stated, a rule range given two ways, a builder's passes numbered two ways (plan:1064–1080), and from the scripts `ConstraintDescriptor`, `ConstraintInfluenceMap`, `TokenMeta` and others (plan:1096–1100).

On 2026-08-27 you said that if two canonical documents disagree, canon has no single position. That is right. The assistant then had two ways to honour it: reconcile all of it, or stop calling the second kind of content canon for the purpose of this work. Only the first was offered to you.

### 1.4 Only your word could settle anything

The memory folder holds your corrections over five months. Samples are not evidence; compiler behaviour is not evidence; catalogs are not evidence; "locked" labels are agent-written; the main session may not be the last word on a fact, a check, or an analysis. Each came from a real failure and I would keep nearly all of them.

Their combined effect is visible in the file names of phase 2: `review-of-the-philosophy-edits.md`, `review-of-the-philosophy-review.md`, `philosophy-cold-read-second.md`; a 41-item walk to establish which decisions were yours; a trace of about 250 changed hunks back to your words, of which nine were checked (plan:142). That is accounting for who said what. It is not design.

Two things made it worse. Agents turned settled or out-of-scope matters into questions for you: four of four checked on 2026-07-24 were already answered, and four of five on 2026-08-27 were outside the phase (memory, `feedback_agents_overproduce_owner_decisions.md`). And reasoning by agents could not be trusted to close a question on its merits: five soundness arguments written without attack rounds were all refuted in one pass (memory, `feedback_soundness_arguments_need_adversarial_rounds.md`).

What was missing is a way to settle a design question that is neither your word nor the unfinished compiler. I propose one in Part 2: worked examples evaluated on paper by agents who have not seen each other's answers.

### 1.5 Scope grew inside a reconciliation phase

Phase 2's goal is one sentence (plan:958). Inside it ran: a rewrite of the philosophy with three reviews; a nineteen-row fault model across nine documents; a full design for ordering periods, a new language capability, at 879 lines with a 727-line adversarial review, locked on 2026-08-11; a wording pass on "MVP"; the trace; the cold read; the scripts. Each was defensible. Nothing said "this is separate work; park it."

### 1.6 The plan became unreadable

The plan says its tracker is "the page you open each morning" and runs "about a hundred lines" (plan:9–10). Section 1 is 160 lines, about forty of them describing the times the section itself went stale. The board row for phase 2 is a single table cell of roughly a thousand words (plan:194). The plan is 2,278 lines. One decision write-up, on overflow and divisors, is 2,751 lines.

### 1.7 Why acting on the earlier diagnoses did not help

| Diagnosis | What it got right | What it prescribed | Why that did not work |
|---|---|---|---|
| 2026-07-01 (memory file) | The plan's finish line was "survives another review" | Tidy the plan, one status page, answer the gates, ship a slice | A third plan and a plan about the plan followed; the proof model was still undefined |
| 2026-07-25 (`where-are-we-2026-07-25.md`) | The status page was wrong on five of seven items; the soundness document is a skeleton | Thirteen phases with checkable finish lines | The checkable conditions were about records (rows have commits, patterns return nothing). The one that mattered was an open-ended search. The skeleton it identified stayed a skeleton |
| 2026-07-28 (memory, "the loop diagnosis") | Each pass found the previous pass's reference point unverified | Close with defects recorded | The condition was widened the same month and thirty more findings arrived |

All three added controls to be applied by the agents inside the loop. None reduced the amount of text that had to agree with itself, and none moved the unwritten part of the design forward in the order.

### 1.8 What I am inferring

- That the rules of evidence caused the bottleneck is my reading of the memory files together. No file says it.
- That most open findings are "not design" depends on where the line is drawn. It is your line to draw (question 2).
- That a stronger model helps: it helps with holding the whole design set in view and with working examples by hand. It does not help if the arrangement stays the same.

---

## Part 2 — What a complete and consistent design is

### 2.1 The design set

| Part | Where | State today |
|---|---|---|
| Commitments | `docs/philosophy.md`, 115 lines | Locked 2026-07-27. Good. |
| The target for the compiler and runtime split | `docs/Working/what-i-want-2026-07-16.md`, 221 lines | Yours. Has three small internal problems (2.4). |
| What an author can write and what it means | `docs/language/`, about 11,400 lines | Good shape by the measure in `CLAUDE.md` (every place the catalogs matched a decision was here). Not yet read cold the way the compiler documents were. Carries build-status text that should come out. |
| The proof model | One document. Today split across spec 0.6, 0.7 and 5, `proof-engine.md`, `soundness-and-coverage.md`, and `compiler-and-runtime-design.md` section 1 | **Unwritten.** This is the real remaining work. |
| Structural rules: reachable, dead end, dead row, all as errors | Spec section 4 is one line pointing at the stage document | Mostly decided; one definition in conflict (question 6); needs one normative page. |
| Runtime behaviour as seen from outside: operations, outcomes, order of execution, entry checks, restore, inspection, the load-time certificate gate | Spec 3A and `docs/runtime/runtime-api.md` | Largely aligned by phase 2. Open items are listed in `docs/runtime/README.md` section "Open questions". |

Outside the set, labelled as implementation notes: the stage documents' internals, `evaluator.md` (2,183 lines for a component that is not built), `precept-builder.md`, descriptor and result type shapes, diagnostic code lists, tooling. They are not deleted. They are reconciled when the thing they describe is built.

### 2.2 Complete: four finite lists

1. **Want-claim map.** Each claim in the want document, about seventy, with the file and line in the design set that states it. A blank row is a gap. A second agent re-derives the list of claims independently and the two lists are compared.
2. **Proof-model contents.** The document answers eight questions:
   - What obligations a definition owes, and at which points: rules at construction and at every operation that writes a field they mention; fault obligations at each evaluation; state constraints on every route into the state (your rulings of 2026-07-26, plan:1572–1600).
   - What a proof may rely on at a given point: the four premise kinds, and the answer to each open premise question.
   - How an obligation is closed: the permitted steps, each named.
   - What happens otherwise: the three results (proven, proven unsafe with a counterexample, unproven) and the message for each, including the suggested premise (want:179).
   - What a certificate records.
   - What the checker confirms, and at which two places it runs.
   - The argument that the induction holds over this language, naming state residency, computed fields, `omit` resets, entry and exit actions and first-match rows (plan:1716), and stating the assumption about restored data (spec:278–280).
   - The structural rules.
3. **Coverage table.** For each member of the language's own lists (fifteen expression forms, the actions, the eight writers, the constraint kinds, the types), either the step that handles it or the refusal and its message. Generated from the lists, so nothing can be left out silently.
4. **Open-question list.** Every entry has your answer, a default taken, or your word that it waits.

### 2.3 Consistent: three checks, none of them reading prose against prose

1. **One home per fact.** Canon already has this convention (plan:1092–1096). The four scripts in `phase-2/checks/` (417 lines) are pointed at the design set only.
2. **Worked-example agreement.** A suite of `.precept` examples with expected results: your example and its fourteen mutations, plus one example for each permitted step, each refusal, and each default. Two agents who have not seen each other's work, given only the design set, write out the obligations and the result for each example. They must agree with each other and with the expected result. A disagreement names the sentence that allowed two readings, and that sentence is fixed. Done when a fresh pair agrees across the suite with no edit in between.
3. **Attack rounds.** Agents are told to write a definition the design accepts in which some sequence of operations breaks a rule or faults, traced by hand through the execution order. A finding must be a concrete definition and trace. Each attacker is assigned different language features so they do not all look in the same place. At most three rounds.

Stated plainly: agents working examples by hand is weaker than a machine check, and agents of the same model can share a blind spot. The certificate checker is the eventual backstop, and it is code, so it comes later. This is the strongest check available while staying in design.

### 2.4 Three problems inside the want document itself

- The example's `ReduceLimit` guard reads the optional `MonthlyRepayment` with no presence test (want:75). The language documents treat an unguarded read of an optional as refused (`precept-language-spec.md:1561`; `soundness-and-coverage.md:226`). Line 110 says the example is accepted.
- Line 143 says an edit to an editable field evaluates every rule that mentions the field at entry. Lines 171 and 199 have been read as pulling against that. You declined to settle it in passing on 2026-08-26.
- The document does not mention `ensure`. Your 2026-07-26 ruling that every route into a state owes a proof of that state's constraints (plan:1588) shows you accept state constraints, so I read this as silence, not disagreement. `on <Event> ensure` is the part that needs your word (question 7).

---

## Part 3 — The path

### 3.1 Keep, drop, archive

**Keep as the design set:** as in 2.1.

**Keep as input to the proof model, read once by its author:** your rulings from the 2026-07-26 walk (plan:1568–1600 and 291–340); the prepared write-ups in `docs/working-reset/phase-5/` (the three short ones are clear and have worked examples); `phase-4/what-a-refusal-claims.md`; the list of what survived from the matrix (plan:1653–1684); the locked period-ordering design. The earlier establishment and preservation drafts may be mined for cases but carry no authority.

**Keep as tools:** the four check scripts.

**Archive:** the thirteen-phase plan; the phase 1 register and walk file; all of phase 2's records; the older plans and the rest of `docs/Working/` except the want document and `bugs.md`.

**Drop from the path:** numbering every normative statement, the machine-readable lists and the build check (phase 3); they belong to acceptance criteria. The separate count of certificate steps (phase 4) falls out of writing the proof model. One argument per rule with two attackers each (phase 6) becomes the attack rounds over a short list of steps. The generated grid (phase 7) becomes the coverage table.

**Replace the plan with one page:** the six parts of the design set and their state; the four lists and their counts; defaults taken; questions for you.

### 3.2 Phase 2

Stop it, unsigned, with a short note saying why. The 145 corrections already made stay in the documents. For what is open:

- The twenty-seven disagreements between compiler or runtime documents: not worked. Those documents leave the design set (question 2). If you answer no to question 2, the fallback is to delete the restated type declarations and counts from all but one owning document, which the plan says resolves eleven of the fifteen scripted findings (plan:1096).
- The four against the want document: questions 6 and 7; the `ensure` silence goes in the claim map; which artefact carries the certificate is answered in the proof model (want:199 says "alongside the compiled definition").

### 3.3 The eleven open questions in the plan's section 4

| Question | Whose it is | What bears on it |
|---|---|---|
| Where the set of provable formulas stops | Yours, but not as an abstract choice | Default: the smallest set that proves your example and rejects its table. Supported by your words "a minimal proof set for the MVP" and "the compiler works with the expression as written" (plan:279, 1595). You see the result as the refused list in step 5. |
| Must a proof name what establishes and keeps each fact true | Not yours unless you want it | The write-up says it does not change what compiles. Want:199 lists the pre-state rule among the premises a certificate names. Default: name the rule used; its establishment and preservation are their own obligations. Reversible. |
| Where a divisor's non-zeroness comes from | Default exists | The write-up calls "declared on the divisor itself" sound and "the safe resting place"; widening later invalidates nothing. |
| What closes a rule when an editable field is edited | **Yours** (question 4) | Want:143 answers yes in your words. If you say no, the alternative is that the compiler proves it from declarations or refuses the definition. |
| May a proof use that an earlier row did not match | Default exists | Refuse for now; your example does not need it. Add later. |
| May a computed field's formula establish a constraint on it | Probably answered | Want:22 says a modifier is a rule; want:20 puts a rule's obligation on every write of fields it mentions. That reads as "the constraint is owed a proof, not assumed." Needs one fresh check against canon, not a ruling. `phase-3/canon-on-computed-field-constraints.md` has the reading. |
| May "the entity is in state S" be used as a premise | Default exists | Refuse for now; the plan already keeps the conservative restriction (plan:285). |
| Does `from any` mean one row per state | Probably answered | Comma lists expand per state (spec:2203), and you ruled that each path to an expression gets its own proof (plan:1593). Default: expand per state. |
| State actions: how many, and when entry actions run | **Yours** (question 8) | The `omit` reset applies on a self-transition and entry constraints apply at construction (spec:2207). A default consistent with both exists if you prefer not to rule. |
| Arithmetic model for `integer` | Yours, later | Tied to overflow, which you placed after the MVP. Nothing in the first proof set needs it if the divisor default stands. |
| May a rule mention the current time | **Yours** (question 5) | Canon has a locked decision allowing it (`temporal-type-system.md:568`); your want document left it open (want:219). The induction has no step for time passing. Changing canon's decision needs your word. |

Three need you. Two are probably answered by text you already approved. Five take a refuse-for-now default under question 3. One waits.

### 3.4 Order of work and time

| Step | Work | Time |
|---|---|---|
| 0 | You answer the starting questions. Archive. One-page plan. | 1–2 days |
| 1 | Label the documents outside the set. Move build-status text out of the specification's normative sections. Build the want-claim map. Apply your answers to the four want disagreements. Correct the example. | 2–3 days |
| 2 | Write the proof model as one document. One author with the full design set in view, the eight headings fixed in advance, defaults applied. Target fifteen pages. | 1–2 weeks |
| 3 | Example suite, paired agreement, attack rounds; revise. At most three rounds. | 1–2 weeks, overlapping step 2 |
| 4 | Coverage table and the refused list. Your read. | 3–5 days |
| 5 | Fold the want document's claims into the specification so one document states the design. | 2–3 days |

Four to seven weeks. The spread is step 3. If round three still turns up a new kind of problem, work stops and comes to you as a question about narrowing the language. The count that triggers this is kept by a script over the attack records, not by the agent running the work.

### 3.5 How decisions reach you

- Now: the starting batch.
- Weekly, one page: the counts of the four lists, the defaults taken that week, at most three questions. Each question is a `.precept` example, what each answer makes the compiler say, and the default if you do not answer. The plan already set this standard (plan:342–346).
- Before any question reaches you, a fresh agent checks whether your existing text answers it and whether it belongs to the current step.
- You read two things in full: the proof-model document and the refused list.

### 3.6 Using the stronger model

- One author holds the philosophy, want document, language documents and inputs together. The language documents are about 905 KB of text; check the model's context limit before assuming they fit in one session, and if they do not, the type documents are loaded by section as needed.
- Agents do not write documents for other agents to review. Checkers produce worked examples and counterexample attempts, not prose reviews.
- Independent agents are spent where independence matters: the paired derivations and the attackers.

---

## Part 4 — Risks, and what is lost

**The proof model may not converge.** It has failed to lock before. The difference here is a smaller target, a fixed table of contents, examples with known answers, and a stop after three rounds. If it stops, that is the answer to the feasibility question your want document sets up at line 133, and the response is to narrow the language, which line 189 already names as the negotiable part.

**The first proof set will refuse a lot.** Refuse-by-default plus a minimal set of steps means many constructs cannot appear in rules that need proving. You see this as the refused list. It may be unwelcome.

**Paper checks can miss what a machine would catch.** Stated in 2.3.

**The internals documents will drift further.** Leaving them unreconciled means whoever builds each stage must treat them as notes. Some rework follows. The cost of the alternative is on record.

**Your rule about canon is narrowed.** "The canonical documents are what we measure against" would apply to the design set and stop applying to internals documents. That is your call.

**Provenance tracking is given up.** Nothing in this path traces each sentence to your words. In its place: you read the proof model and the refused list yourself.

**Defaults accumulate.** If the defaults list passes about fifteen entries, stop and review it with you.

**The language documents have not had a cold read.** The claim that they are in good shape rests on the catalog comparison and on phase 2's pattern search. Step 1 should include one, limited to disagreements that affect the proof model.

**The estimate is soft.** It rests on the proof model being about fifteen pages. If the list of permitted steps is long, step 3 grows with it.

---

## Sources

Read in full: the want document; the plan's sections 1–5, "How this plan is executed", phase 2's goal and exit condition 4, phases 3–7; `phase-2/five-items-walk-2026-08-27.md`; the three prepared write-ups in `phase-5/`; `docs/compiler/soundness-and-coverage.md` to line 310; spec sections 0.6, 0.7, 3A.1, 3A.4, 4, 5 and open questions; `docs/README.md`, `docs/language/README.md`; the previous analysis; memory index and fourteen memory files; `docs/Working/where-are-we-2026-07-25.md`.

Read in part: `proof-engine.md` (status, architecture head, sections 13–14); `cold-read-register-diff.md` (first sixty lines; the rest via the plan's summary); `graph-analyzer.md` dead-end section; `docs/compiler/README.md` open questions.

Not read: `docs/philosophy.md` body; the contradiction register; `cold-read-findings.md`; the trace; the walk file; the type documents; `catalog-system.md`; the runtime documents; the older plans; the matrix and the establishment and preservation drafts beyond their headers; the largest memory file (`project_boundary_define_what_i_want_2026_07_16.md`). I did not verify the count of want-document claims (seventy is an estimate), the exact line of the rule refusing an unguarded optional read in a guard (I cite the interpolation rule at spec:1561 and the coverage row, which state the same principle), or whether the check scripts ran over the language documents.
