# Finishing the Precept design across all canonical documents

Written 2026-11-13 against `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository was edited. This plan takes your four rules as fixed: design first; all detail stays, internals included; duplication is removed by naming one owning statement and pointing the others at it; much less of your time.

---

## One-page summary

**What finishing means.** Every canonical document states the design you want, in full detail, and three things can be checked by someone who is not you:

1. *Nothing is promised and unwritten.* The proof model is written out (what a definition owes, what a proof may rely on, how an obligation is closed or refused, what a certificate holds, what the checker confirms), and every document that depends on it says the same thing. No section reads "fill per slice", no document is a stub, every open question has your answer, a recorded default, or your word that it waits.
2. *Each fact is stated once.* A table names the owning document for every record, list, count and ordering. Scripts confirm no second full statement exists anywhere in canon.
3. *The documents work when used.* Independent agents carry your bank example and its fourteen mutations by hand through every document, from source text to the runtime operation, and get the same answers as each other and as your want document. Other agents try to write a definition the design accepts that can still break a rule, and fail.

**The path, eight to twelve weeks.** The honest figure is closer to twelve; the reasons are in Part 3.

1. Week 1: build the ownership table, the map from your want document to canon, and the example set. You answer one batch of questions.
2. Weeks 2–4: one author writes the proof model. In parallel, other agents deduplicate the parts of canon the proof model does not touch. I recommend "beside", not "before" or "after"; reasons in Part 3.
3. Weeks 3–5: the proof model is checked on worked examples and attacked, three rounds at most.
4. Weeks 5–9: the proof model is carried into the documents it changes (proof engine, catalog system, builder, evaluator, runtime API, diagnostics, tooling), each fact written into its owning document once.
5. Weeks 9–11: the end-to-end hand walk, the final script run, and your two reads.

The thirteen-phase plan is replaced by one page. Phase 2 stops as a phase; its sixteen open findings become rows in the new work, not a sign-off you owe. Its records are frozen where they are and no longer maintained.

**The first week.** Agents produce the ownership table, the want map, a table of your rulings and where canon states each, and the example set. The proof-model author works your bank example by hand under the design as it stands. On Friday you get one page of numbers and a three-page table: each handler in your example, what it owes, what closes it, and which of the fourteen mutations the design as written today can and cannot explain.

**What you read over the whole effort.** One page a week. Twice, something longer: your example worked under the finished proof model (about four pages), and the list of what the design refuses (length unknown until it exists).

**Decisions needed from you.** Seven to start:

1. May phase 2 stop without a sign-off and the thirteen-phase plan be replaced by one page, with its records frozen and kept?
2. When an editable field is edited, are all rules that mention it checked against the proposed value before it is accepted, as your want document says at line 143?
3. Your philosophy says a rule may depend on the clock; the proof has no step for time passing. Keep such rules, meaning "true whenever an operation runs", or allow time in guards only?
4. Two non-terminal states that lead only to each other: is that a dead end? Your want document and philosophy say no; the graph analyzer document says yes.
5. Do a state's entry actions run when the entity is created, and when it transitions back into the same state? If a state declares several, do all run, in written order?
6. Where the design does not say whether a proof may rely on something, may the default be "it may not", recorded on a list you can overturn?
7. Your example's `ReduceLimit` guard reads the optional `MonthlyRepayment` without testing it is set. May the example be corrected and shown to you?

Two that can wait a few weeks:

8. Is the overflow and integer-width model part of the design you want finished now, or does it stay marked as coming after the first working compiler?
9. Does `from any` mean one row per state, the same as a comma-separated list? (Default: yes.)

---

## Part 1 — Inventory

### 1.1 The canonical set, measured

36 documents, 30,154 lines, 2.2 MB. That is roughly half a million tokens (my estimate at four characters per token; measure before assuming any single agent session can hold it).

| Area | Documents | Lines | State |
|---|---|---|---|
| `philosophy.md` | 1 | 115 | Finished. Locked with your approval 2026-07-28 (`philosophy.md:3`). |
| `docs/language/` | 9 | 11,263 | Detailed and largely finished for what an author can write. The specification's own proof section is 20 lines that point elsewhere (`precept-language-spec.md:2177–2197`), and its graph section is one line (`:2171–2173`). |
| `compiler-and-runtime-design.md` | 1 | 1,030 | Finished as an overview; restates facts owned elsewhere. |
| `docs/compiler/` | 12 | 9,913 | Front-end stages detailed. `soundness-and-coverage.md` calls itself a skeleton (`:7`, `:14`). `proof-engine.md` (2,653 lines) describes the engine as built. |
| `docs/runtime/` | 7 | 5,189 | Detailed designs for unbuilt components. `descriptor-types.md` is a stub (217 lines) for types three other documents declare in full. |
| `docs/tooling/` | 4 | 2,280 | States design (hover, preview, MCP proof output); `extension.md` is a stub. |
| `glossary.md`, `agent-onboarding.md` | 2 | 364 | Restate facts; `agent-onboarding.md:39` restates the stage order. |

Status fields are themselves inconsistent. `docs/runtime/README.md` lists the builder and evaluator as "Stub"; both documents declare "Doc maturity: Full". `docs/language/README.md` lists the temporal and business-domain documents as "Draft"; both declare "Full". The status taxonomy mixes two questions (is the document complete; is the code built) and only the first is a design question.

### 1.2 What is unwritten

The proof model. Evidence:

- `soundness-and-coverage.md` is named the home for how the guarantee is made sound (`:15`). Its verdict, counterexample, certificate-format, checker and safety-rail sections each open "Intent — fill per slice" (`:124, :133, :146, :155, :319, :334`). Its coverage tables list fault obligations only.
- `proof-engine.md:2612` states the induction as the model the engine serves, and the next paragraph says the engine does not do it: "there is no general carrying of 'every rule held before this operation' into a handler's discharge."
- `catalog-system.md:103` records that the certificate-step catalog does not exist and that the obligation kinds include neither establishment nor preservation.
- `precept-language-spec.md:268` states the promise (base case, inductive step, four kinds of premise) in one paragraph. Nothing states how it is carried out.

Counted by term across the nine documents that should carry the model, "induct" appears 11 times in total and "base case" 11 times; "certificate" appears 64 times, 24 of them in the skeleton document. The model is referred to everywhere and specified nowhere.

Writing it will change other documents, and that work is also unwritten: the obligation and certificate entries in `catalog-system.md`; most of `proof-engine.md`; how the builder turns a certificate's premise list into the checks the runtime runs (`precept-builder.md`); entry checking for edits (`evaluator.md`); the load-time gate (`runtime-api.md`); the new refusal messages (`diagnostic-system.md`); what hover and the MCP tools report (`docs/tooling/`). Also unwritten: where the checker's compiler stage sits (`docs/compiler/README.md`, open-questions table); the structural rules as errors in one normative place; and the five writers with no catalog entry (`precept-language-spec.md:1999`).

Two earlier attempts exist and may be mined for cases but carry no authority: the obligation matrix and the establishment-and-preservation design, which failed to lock three times in two days (plan:498).

### 1.3 Where the same fact is stated more than once

Measured today with my own count over fenced code blocks, which differs slightly in method from the existing script:

- 235 C# blocks. 235 distinct types declared; **87 are declared in two or more documents**, four in three or more.
- The existing script reports **20 types whose member lists differ between documents** today, among them `FieldDescriptor`, `ConstraintDescriptor`, `ConstraintInfluenceMap`, `ProofRequirement`, `Precept`, `Version`, `StateGraph`, `TokenMeta`.
- The documents with the most shared declarations: `proof-engine.md` and `evaluator.md` (29 each), `result-types.md` (28), `catalog-system.md` (27), `precept-builder.md` (15).
- Counts: `diagnostic-system.md` declares 147 codes and states other totals elsewhere (script output today; plan:1071).
- Orderings: the stage order appears in at least `docs/compiler/README.md:17`, `agent-onboarding.md:39`, `diagnostic-system.md:54` and the architecture overview; the builder numbers its own passes two ways (plan:1066).
- Prose: the induction paragraph appears in the specification, the proof engine document and the architecture overview in near-identical words.

The first three kinds can be found by script. Prose restatement cannot, and the plan treats it differently (Part 2).

### 1.4 Decided and possibly not yet in canon

Your rulings from the 2026-07-26 walk are listed at plan:1568–1600 and 291–340, and the period-ordering design was locked on 2026-08-11 (plan:319). I did not verify that each is stated in canon. Week 1 produces that table. A ruling missing from canon is writing work, not a question for you.

---

## Part 2 — What "done" means

### 2.1 Complete

Five lists, each finite, each checkable by count.

1. **The want map.** Every claim in your want document, with the canonical section that owns its statement. Two agents build the list of claims independently; the lists are compared. A row with no owning section is a gap. Done at zero gaps.
2. **Canon's promises to itself.** Every place a document says something is defined elsewhere, resolved to a section that defines it. Done when a script finds no reference that lands on a placeholder, a stub or nothing, and a search for "fill per slice", "fill when scheduled" and stub status returns nothing.
3. **The proof model's contents.** A fixed list of questions it must answer, set in week 1 before writing starts: what is owed and where (rules at creation and at every write of a field they mention; faults at each evaluation; state constraints on every way into the state); what a proof may rely on at each point along the execution order (`precept-language-spec.md:2003–2012`); the permitted proof steps, each named; the three results and their messages; the certificate's contents; what the checker confirms and where it runs; the argument that the induction holds for this language, naming state position, computed fields, `omit` resets, entry and exit actions and first-match rows (plan:1716); the structural rules.
4. **Coverage against the language's own lists.** For every expression form, action, writer, constraint kind, type and operation the language documents declare, the proof model gives either the step that handles it or the refusal and its message. The table is generated from the lists, so a member cannot be skipped unnoticed. This measures against canon, never against the samples.
5. **Open questions.** Every row in the specification's open questions, the two README indexes (eleven rows in compiler, seven in runtime) and the plan's § 4 carries your answer, a recorded default, or your word that it waits. Done at zero rows with none of the three.

### 2.2 Consistent

1. **The ownership table.** One row per record, enum, count, ordering and catalog-keyed table: the owning document and section. Every other site refers to the owner and does not restate it. No detail is dropped; a member list that existed in three places exists in one, in full.
2. **Scripts over the whole set.** The four existing checks cover `docs/language`, `docs/compiler`, `docs/runtime`, `docs/tooling`, the architecture overview and the root `README.md` (`declared-types.py:15–17`). They do not cover `glossary.md`, `agent-onboarding.md` or `docs/contributing/`; only one includes `philosophy.md`; the diagnostic-code check starts from one file. They read fenced code blocks and nearby numbers only. And they print candidates to be read, not a pass or fail: today they report 20 differing types, 10 undeclared identifiers and 109 codes a stage document does not name, the last mostly noise by the script's own note. Needed: widen the file list; add the ordering check the plan records as unbuilt (plan:1105); add a check for tables keyed by a language list; add a check that each README's status column matches the document's own; and give every check a file of explained exceptions, so that "zero unexplained" is a result a script can print.
3. **The hand walk.** One example is carried through every document in order: tokens, parsed constructs, symbols, typed model, graph facts, obligations, certificate, builder output, then a `Fire` and an `Update` in the evaluator. A different agent does each stage, given only that stage's document and the previous agent's output. A stage that cannot produce its output from its document is incomplete. A stage whose output the next cannot consume is inconsistent. This tests the internals documents by use, which reading prose against prose never did.
4. **Paired derivation.** Two agents who have not seen each other's work, given the design, write out obligations and the accept-or-reject result for each example in the set. They must agree with each other and with your table.
5. **Attack rounds.** Agents are asked for a definition the design accepts in which some sequence of operations breaks a rule or faults, traced by hand. Only a concrete definition and trace counts as a finding. Three rounds at most.

Two limits, stated plainly. Checks 3–5 are agents reasoning on paper; a shared blind spot is possible, and the certificate checker, which is code, is the eventual backstop. And prose that paraphrases an owned fact can still drift. No check here hunts for that by reading, because that is the search that did not end last time; the hand walk and paired derivation catch it only where it changes an answer.

### 2.3 What you sign

Not documents. Your example worked under the finished model, the refused list, and the defaults list.

---

## Part 3 — The path

### 3.1 Order: the proof model beside deduplication

- **Not after.** Reconciliation ahead of the unwritten part is the arrangement that produced phase 2. The proof model is also the only part that might not be achievable (your want document says so at line 133), and that should be learned in week four, not week ten.
- **Not strictly before.** Deduplication needs none of your input and little judgement, and agents can do it while the proof-model author works. It also makes the later step cheaper: when the proof model changes `ProofRequirement` or `Compilation`, there is one place to change.
- **Beside, with one rule to prevent double work.** In week 1 the ownership table marks which facts the proof model is expected to change (the proof records, the certificate carrier, the constraint descriptors, the diagnostic totals). Those get an owner now and are left untouched until the proof model has passed its checks. Everything else is deduplicated immediately: front-end types, result types, stage and pass orderings, status columns, the code ranges.

### 3.2 The steps

| Step | Work | Elapsed |
|---|---|---|
| 1 | Ownership table; want map; rulings-in-canon table; example set; checks widened; one-page plan. Your first batch of answers. | Week 1 |
| 2 | Proof model written by one author. Language-level statements (what is owed, what may be relied on) go to the specification's § 5; how it is closed, certificates and the checker go to `soundness-and-coverage.md`; engine mechanism stays in `proof-engine.md`. That split is my recommendation and is a filing choice, not a language decision. | Weeks 2–4 |
| 3 | Deduplicate everything outside the proof model's reach, one cluster of facts per agent. | Weeks 2–4, parallel |
| 4 | Paired derivation and attack rounds on the proof model; revise; three rounds at most. | Weeks 3–5 |
| 5 | Carry the proof model into the documents it changes, each fact into its owner. Per-type property tables (exact or approximate arithmetic, ordering, which qualifiers must agree) are filled by one agent per type document against a fixed column list. | Weeks 5–9 |
| 6 | Hand walk; final script run; coverage table; refused list. Your two reads. | Weeks 9–11 |

Why not the earlier report's four to seven weeks: that figure excluded the internals documents. Step 5 rewrites much of a 2,653-line document and touches at least six others. Phase 2 was sized at two to three days and ran a month, so treat my figure the same way and rely on the stop rules in 3.8.

### 3.3 The thirteen-phase plan and phase 2

- **The plan** is replaced by one page: the six steps, the five completeness counts, the script results, defaults taken, questions for you. The old file is kept and marked superseded.
- **Phases 0 and 1** are done and stand.
- **Phase 2** stops as a phase. Its 145 applied corrections stay. Of its sixteen open findings (plan:1059–1080): the twelve between canonical documents become rows in the ownership table, where most resolve by naming an owner, as the plan found for eleven of the fifteen scripted ones (plan:1096). The four against your want document are handled in 3.6.
- **Phase 3.** Its machine-readable lists are kept, because completeness item 4 needs them. Numbering every normative statement (546 to 2,205 by the plan's own estimate, plan:1460–1466) is preparation for acceptance criteria and moves after the design, which is your stated order.
- **Phases 4–7** become steps 2 and 4. The lessons recorded there are kept: organise by expression form along the execution order, do not multiply properties together (plan:1653–1684).
- **Phase 8** becomes your read of the refused list. **Phases 9–12** follow the design.
- **Records.** `docs/working-reset/` holds 78 files and about 27,500 lines of markdown; `docs/Working/` holds 443 files. They are frozen in place, not deleted, not maintained and not re-verified. A short list is named as input to the proof-model author: the walk rulings, the write-ups in `phase-5/`, `phase-4/what-a-refusal-claims.md`, the locked period-ordering design. Agents are told nothing else in those folders binds them.

### 3.4 Splitting work among agents

- The main session orchestrates and relays. It does not author, analyse or verify; that is your standing correction (memory, `feedback_main_loop_not_authority.md`).
- **One proof-model author** holds the philosophy, the want document, specification sections 0, 3A, 4 and 5, your walk rulings and the short `phase-5` write-ups. That is a small fraction of canon. Type documents are read by section when a case needs them.
- **Deduplication agents** each get one cluster: the owning section and excerpts of every restating site. Small inputs, mechanical output.
- **One owner per statement.** In any round, exactly one agent edits an owning section. An agent that needs a change elsewhere writes a one-line request to a queue. Parallel agents work on disjoint files.
- **Checkers are fresh agents** briefed to refute, and they produce worked cases, not prose reviews. No review of a review.

### 3.5 How questions reach you

One batch now. After that, one page a week with at most five questions; fewer is normal. Each question carries a `.precept` example, what each answer makes the compiler say, and what is taken if you do not answer. Before a question reaches you, a fresh agent checks whether text you approved already answers it and whether it is a design question at all; in your own measurements four of four, then four of five, failed those tests (memory, `feedback_agents_overproduce_owner_decisions.md`).

A default is allowed only where it makes the compiler refuse more and can be widened later without undoing anything. That rests on your 2026-07-26 ruling: "we are going with a minimal proof set for the MVP, but we can and will expand the compiler's proof power over time" (plan:279). A question that changes what an author may write, or that touches a locked decision, is never defaulted. It waits for you, and work continues around it.

### 3.6 The open questions, one by one

| Question (plan § 4) | Needs you? | Text that bears on it |
|---|---|---|
| Where the set of provable formulas stops | Yours, at the refused-list read, not in the abstract | Your "minimal proof set" ruling; "the compiler works with the expression as written" (plan:1595). Default: the smallest set that proves your example and rejects its table. |
| Must a proof name what establishes and keeps each fact | Default | The write-up says both options are sound and that your catalog ruling narrows it to almost nothing (`phase-5/must-a-proof-name…:90–101`). Want:199 has the certificate name the pre-state rule used. |
| Where a divisor's non-zeroness comes from | Default: declared on the divisor itself | The write-up calls this "the safe resting place" that can be widened later (`phase-5/where-a-divisors…:119`). Shown to you in the refused list. |
| What closes a rule when an editable field is edited | **Yours** (question 2) | Want:143 says the rules mentioning the field are evaluated at entry. Want:171 and :199 have been read against it. You declined to settle it in passing on 2026-08-26. |
| May a proof use that an earlier row did not match | Default: no | Your example does not need it. Cost: an author restates the negated condition. |
| May a computed field's formula establish a constraint on it | Probably answered | Want:22 (a modifier is a rule) and want:20 (a rule's obligation attaches to every write of fields it mentions) read as "proven, not assumed". One fresh check against canon. |
| May "the entity is in state S" be a premise | Default: no | The plan keeps the conservative restriction (plan § 4). |
| Does `from any` mean one row per state | Default: yes (question 9) | Comma lists expand per state (`precept-language-spec.md:968`); the wildcard is unstated (`:2203`). It concerns what the language means, so you see it. |
| State actions: how many, and when entry actions run | **Yours** (question 5) | `precept-language-spec.md:2016` records both parts as unsettled. Visible runtime behaviour. |
| The arithmetic model for `integer` | Yours, as a scope question (question 8) | You placed overflow after the first working compiler (plan:1599). Whether its design also waits is not stated in what I read. |
| May a rule mention the current time | **Yours** (question 3) | `philosophy.md:28`, approved by you, admits clock-dependent rules; `temporal-type-system.md:568` locks the same; your want document (earlier) left it open at :219. The plan's row does not cite the philosophy line. |

The four findings against your want document:

- **Dead end.** Yours (question 4). Want:152 and `philosophy.md` ("every non-terminal state has a way forward") against `graph-analyzer.md:439–455`.
- **`on <Event> ensure` at entry, and `ensure` absent from the want document.** Probably answered: the philosophy you approved names "the ensures that must hold in specific states or on specific events", and want:22 calls an argument modifier "a precondition on its event". One fresh check, then writing.
- **What carries the certificate.** Answered by want:199 ("alongside the compiled definition"). Writing work.

Three need you now, two are scope or confirmation, two are probably answered, five take a default.

### 3.7 What you do not have to read

Each check reports a number or a short table. The weekly page gives the counts, about ten plain statements of what changed with a file and line you can spot-check, the defaults taken, and what the checks did not cover.

### 3.8 Risks, and the rules that stop a loop

Phase 2 looped because its finish line was "a fresh reader finds nothing new", every pass found that the previous pass's reference point was unverified, and its own three-attempts rule never fired: the attempts column still reads 1 (plan § 2). These rules are designed against that.

1. **No open-ended reading passes.** Every check enumerates a finite list named in advance. A proposal for a new check must say what list it walks. "Read it and report anything" is not run.
2. **The stop rule is counted by script, not by the agent doing the work.** Rounds are tagged commits. If the third round on the proof model finds a new kind of flaw, work stops and comes to you as a question about narrowing the language, which want:189 names as the negotiable part.
3. **Open items must fall.** The weekly page shows the completeness counts and unexplained script results. If the total rises two weeks running, work stops and the page says why.
4. **Records stay small.** Phase 2 changed about 1,900 lines of canon (`git diff --shortstat b93d1db1 HEAD`: 927 added, 955 removed, 22 files) and wrote about 14,000 lines of records in `phase-2/` alone. Here: one append-only log. If lines written outside canon exceed lines changed inside it in any week, that is reported as a fault.
5. **New work found on the way is parked.** The period-ordering design grew inside phase 2. Anything not needed for the five completeness lists goes on a parked list.
6. **Settled things stay settled.** The want document carries your approval in full (your statement of 2026-08-26, memory). Who typed which line is not reopened.

Other risks:

- **The proof model may not close.** It has failed before. Differences this time: a fixed contents list, examples with known answers, a cap on rounds.
- **The refused list may be long.** Refuse-by-default with a small proof set means some constructs cannot appear in rules that need proving. You see it once, in one place.
- **Rewriting `proof-engine.md` could lose detail.** Rule: no line leaves canon without a destination, and the weekly page reports lines removed and where each group went. Text describing today's build moves to the document's existing implementation-notes section; it is not deleted.
- **Paper checks can miss what a machine would catch.** Stated in 2.2.
- **Your answer latency.** The estimate assumes the starting batch within a few days. Questions 2 and 5 hold up the proof model's treatment of edits and state actions; the rest of it proceeds.
- **The estimate itself.** If step 4 uses all three rounds or step 5 finds the runtime documents need more than restatement, twelve weeks becomes fourteen or more. The first real evidence arrives at the end of week 4.

---

## Part 4 — The first week

| Day | Work | By |
|---|---|---|
| 1 | You answer the starting batch (about forty minutes). The one-page plan replaces the tracker; the old plan is marked superseded. | You; one agent |
| 1–2 | Checks widened to the full set, ordering and status-column checks added, exception files started. Ownership table drafted from their output: every multiply-stated record, count and ordering with a proposed owner, and a mark on those the proof model will change. | Two agents, then a fresh one to spot-check twenty rows |
| 1–2 | Want map, built twice independently and compared. Rulings-in-canon table from the walk file and plan § 4. | Three agents |
| 2–3 | Example set: your example (corrected if you agree), its fourteen mutations, and further examples chosen so that every action, writer and constraint kind in the language's lists appears at least once. Expected results come from your want document only. | One agent; a second checks list coverage |
| 2–3 | Proof-model contents list fixed. The two "probably answered" questions checked by fresh agents. | One author; two checkers |
| 3–5 | The author works your example by hand under canon as it stands: for each handler, what is owed, what the documents let a proof rely on, and where they give no answer. | The proof-model author |
| 3–5 | First deduplication batch on facts the proof model will not touch: front-end record types, the stage and builder-pass orderings, the status columns, the Roslyn rule range. | Three or four agents on disjoint files |

**What you see on Friday:**

1. One page: canon's size; multiply-stated facts found, deduplicated, remaining; want claims in total, stated in canon, missing (the missing ones listed); rulings not yet in canon; unexplained script results before and after; defaults taken; at most three new questions.
2. Three pages: your bank example as a table. Each handler, each obligation, the premise that closes it, and for each of the fourteen mutations whether the design as written today explains the rejection. The blank cells are the exact outline of what steps 2 and 5 must write.

Nothing in the first week asks you to read a canonical document.

---

## Sources and limits

**Read in full:** the want document; `docs/README.md`, `docs/language/README.md`, `docs/compiler/README.md`; the plan's §§ 2, 4, 5, exit condition 4, and the heads of phases 3–8; the four check scripts' headers and today's output; `project_design_first_restart_2026_11_13.md` and nine feedback memory files; the second earlier report.

**Read in part:** `philosophy.md` (each paragraph's opening, lines 22–28 in full); `soundness-and-coverage.md` (headings and placeholder lines); `proof-engine.md` (headings, status, §14); the specification's §§ 0.6, 0.7, 3A.4, 4, 5 and open questions; `docs/runtime/README.md`; `project_reset_phase_1_2_2026_07_28.md` (first 120 lines); the first earlier report (summary and Part 1); two of the `phase-5` write-ups and the opening of the editable-write one.

**Not read:** `catalog-system.md` beyond line 103; the type documents; `evaluator.md`, `precept-builder.md`, `runtime-api.md` and the tooling documents beyond status lines and counts; the contradiction register; `cold-read-findings.md` and `cold-read-register-diff.md` (taken from the plan's summary); the two `canon-against-canon` write-ups; the walk file; the period-ordering design; the 198 KB overflow recommendation; the earlier matrix and establishment-and-preservation drafts beyond one header; git history beyond monthly commit counts.

**Not verified:** that each of your walk rulings is stated in canon; that the period-ordering design has been carried into the type documents; the exact specification line that refuses an unguarded read of an optional field in a guard (the memory file gives `:1577`, which at HEAD is a function table row; `:1561` states the principle for interpolation); the number of claims in the want document; whether a single agent session can hold the proof-model author's reading list; and whether the "probably answered" questions are answered. Those last are my readings of text you approved, to be tested by fresh agents, not conclusions.
