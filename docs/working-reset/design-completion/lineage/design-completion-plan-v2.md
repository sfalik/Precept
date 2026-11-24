# Finishing the Precept design across all canonical documents — revision 2

Written 2026-11-13 against `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository was edited. This revision applies the independent review (`plan-review.md`, same folder). The change list at the end says where each of its findings landed. Two further passes the same day removed the calendar, added Part 5 on how the work runs, and then, on your ruling, replaced scheduled starts with steps you start yourself. The last two entries in the change list describe those passes; the last one governs.

Your four rules are taken as fixed: design first, no code and no compiler behaviour used to settle design; all detail stays, internals included; duplication is removed by naming one owning statement and pointing the others at it; much less of your time.

---

## One-page summary

**What finishing means.** Every canonical document states the design you want, in full detail, and three things can be checked by someone who is not you:

1. **Nothing is promised and unwritten.** The proof model is written out and every document that depends on it agrees with it. No placeholder sections, no stub documents, no list that canon hands to a source file. Every open question in canon has your answer, a recorded default with its reasoning, or your quoted word that it waits.
2. **Each fact has one owning statement.** For records, lists, counts and orderings, scripts confirm no second full statement exists. For prose rules, a fixed list of concepts each has an owning section, and a search of each concept's terms confirms every other mention is a pointer.
3. **The documents work when used.** Independent agents carry worked examples by hand through every document and every operation (create, fire, update, restore, and both inspections) and agree with each other and with your want document. Other agents try to write a definition the design accepts that breaks a rule, and fail.

**The path.** Seven steps. None has a date. Each has what it needs before it can start, what it produces, a condition that finishes it, and counts that measure it, and every count is read by a script from files in the repository, never stated by an agent. Steps that do not depend on each other run side by side. How large the work is depends on how many arguments the proof model contains, which is not known until step 1 fixes the list; the design states the full intended proof power, as you said on 2026-11-13, so the number is larger than the old plan assumed.

0. Build the counting scripts and the workflow scripts, and test each against made-up inputs. Nothing else starts until they pass.
1. Extract the language's lists from the language documents; widen the checks; mark every fact's owner; map your want document to canon; check every candidate question against your past rulings; work your bank example by hand; fix the list of what the proof model must contain.
2. One author writes the proof model, section by section, into the canonical documents. Starts when the contents list is fixed and you have agreed to replace the old plan.
3. Beside step 2: deduplicate what the proof model does not reach; settle disagreements between documents against your text; draft answers to the internals questions; finish the stub and design-only documents.
4. Each argument in the proof model is attacked separately for twelve rounds, starting as soon as that argument is written. An early checkpoint with written pass criteria is reached when the first five arguments have each been through three rounds.
5. Carry the proof model into every document it changes. Starts at your sign-off of the proof model.
6. The full hand walk, final script run, your last reads.

The thirteen-phase plan is replaced by one page at a fixed path. Phase 2 stops as a phase; its open findings become rows in this work.

**How it runs (Part 5).** Each step is a workflow script that starts agents in a fixed order and runs until the step's finish condition is met, a stop condition fires, or the run reaches its spending limit. Progress is saved to files in the repository after every piece of work, so the next run starts where the last one stopped. You start each step yourself, with one short instruction to a session; nothing starts on a schedule. Once started, a step runs on its own until it finishes or a stop condition fires, and the session that started it begins the next run by itself whenever a run ends only because it reached its spending limit. When a model's usage limit pauses the work, or a run fails, you get a notice and restart with the same instruction, which continues from the saved progress. A weaker model is never put in an attacker's or adjudicator's place to keep going.

**When you hear from the work.** Only when one of six things happens: a prepared batch of questions is ready; a second author has failed on an argument; a count moved the wrong way between runs; a run failed or made no progress; a usage limit paused work; or a step finished or can go no further without you. The notice for a finished step says what it produced and which steps you can start next, and that notice is how you know to start them. There is no weekly page. One short status file is rewritten after every run and you can look at it or not.

**The first things you see.** You start step 0. Nothing reaches you while it runs unless it fails; when it finishes, a notice says so and that step 1 can be started. The first packet arrives when step 1 has checked every candidate question with a fresh agent against the walk file, your 2026-08-26 rulings, the old plan's ruled list and today's memory, and prepared the survivors with worked examples. It holds one page of numbers, a three-page table of your bank example, a first draft of what the full design still refuses and why, grouped by language area, and the first batch of questions.

**What you read and decide over the whole effort**, as counts and not as hours (Part 6):

- Two batches of prepared decisions, four in each, plus two opening conversations on language questions. Up to four more questions may come out of the fresh checks.
- Three longer reads: the first packet; your example under the finished proof model with the refused list; the final summary.
- Three sign-offs: replacing the plan; the proof model after its attack rounds; the finish. The early checkpoint's result is written to the status file and included in the next thing that reaches you; it is not signed.
- Defaults, which cover only internals nobody outside can observe, are kept in one list. You overturn any you disagree with, whenever you look. Those a caller can observe are shown with the next batch or sign-off.
- Notices under the six conditions above, as they arise. Their number cannot be known in advance.

**Already answered by you, 2026-11-13:** the design states the full intended proof power (want:14, want:171, want:183). Your "minimal proof set for the MVP" ruling is about what the first build ships.

**Decisions in the first batch**, sent when step 1 has prepared them, if each survives its fresh check:

1. May the thirteen-phase plan be replaced by one page, keeping the rulings listed in Part 4 and replacing the three named there?
2. What holds a rule when an editable field it mentions is edited? Four prepared options; by my reading your own example is accepted under only one of them.
3. Do entry actions run at creation and on a transition back into the same state, and may a state declare several?
4. Which of three written definitions of a dead end is yours?

**Decisions in the second batch**, sent when its packets are prepared and the first batch has been answered, so you never have two batches open at once:

5. May a rule mention the clock? One option overrides locked text; the other leaves a gap in the philosophy.
6. Is `integer` fixed-width, owing overflow proofs, or arbitrary precision?
7. Does a certificate name what establishes and keeps each fact true?
8. Does each construct carry metadata so the builder assembles generically? Canon marks this "pending owner ruling".
9. Opening conversations on the open language questions inside the proposed extensions; two are known.

---

## Part 1 — Inventory

### 1.1 The canonical set

36 documents, 30,154 lines, 2.2 MB.

| Area | Documents | Lines | State |
|---|---|---|---|
| `philosophy.md` | 1 | 115 | Finished. Approved by you (`philosophy.md:3`; confirmed 2026-08-26, `phase-2/trace-walk-rulings.md:14–15`). |
| `docs/language/` | 9 | 11,263 | Detailed for what an author can write. The specification's § 5 holds real content on the construction exemption and on what a proved qualifier means (`precept-language-spec.md:2177–2197`) but nothing on how rules are established and preserved. § 4 is a pointer to the graph analyzer document, which is the arrangement deduplication wants. Two type documents end in empty "TBD" open-question sections (`temporal-type-system.md:1935`, `business-domain-types.md:2085`). `collection-types.md:1294–1300` carries two open questions on proposed extensions. `graph-analyzer-roadmap.md` lists ten deferred modifiers. |
| `compiler-and-runtime-design.md` | 1 | 1,030 | An overview that restates facts owned elsewhere, including the proof model in its § 1. |
| `docs/compiler/` | 12 | 9,913 | `soundness-and-coverage.md` calls itself a skeleton (`:7`, `:14`). `proof-engine.md` describes the base engine as built and four further strategies as "designed, not yet implemented" (`:8`); neither covers establishing and preserving rules. `tooling-surface.md:217` and `:439` carry two questions marked unresolved. |
| `docs/runtime/` | 7 | 5,189 | Detailed designs. `descriptor-types.md` is a stub. `runtime/README.md:34–54` indexes **21** open questions (revision 1 said seven). |
| `docs/tooling/` | 4 | 2,280 | `extension.md` is a stub (`:7`). Preview and inspect are "a design surface" not yet designed (`language-server.md:1575`). |
| `glossary.md`, `agent-onboarding.md` | 2 | 364 | Restate facts, the proof model among them. |

`docs/README.md` routes to nothing under `docs/future/`; `docs/future/reasoning-ingress-vision.md` is recorded as outside the canonical set.

A search for open-question headings and pending markers across canon returns about 70 lines (my grep; the pattern is in Part 2.1 item 5). That is the size of the open-question inventory, not the eighteen rows revision 1 counted.

### 1.2 What is unwritten

The proof model:

- `soundness-and-coverage.md` is named the home for how the guarantee is made sound (`:15`). Placeholder sections: `:66`, `:124`, `:133`, `:146`, `:155` ("fill when scheduled"), `:319`, `:334`. Its § 3.2 lists where obligations are created and marks guards, member-call arguments, defaults, lookup keys, field-reference bounds and message holes as not yet enrolled (`:244–263`). Its § 6 ties completion to code fixes, which a design-first document should not.
- `proof-engine.md:2612–2614` states the induction as the model and then says the engine does not carry "every rule held before this operation" into a handler.
- `catalog-system.md:103` records that the certificate-step catalog does not exist and that no obligation kind covers establishment or preservation.
- `precept-language-spec.md:268` states the promise in one paragraph.

Counted as matching lines (`grep -ci`) in nine documents — the specification, `proof-engine.md`, `soundness-and-coverage.md`, the architecture overview, `evaluator.md`, `runtime-api.md`, `precept-builder.md`, `catalog-system.md`, `philosophy.md` — "induct" matches 11 lines, "base case" 11, and "certificate" 64, of which 24 are in the skeleton.

Also unwritten, outside the proof model: the overflow model, which canon says is in scope with only its build deferred (`precept-language-spec.md:104`, `:110`, `:272`; `primitive-types.md:697–701`), covering `integer`, `decimal`, the temporal types and the approximate number type (stated once, under question 6); the catalog entries for five of the eight writers (`precept-language-spec.md:1999`); preview and inspect in the tooling documents; the extension document; where the certificate checker sits in the stage order (`docs/compiler/README.md:61`); and whatever of the locked period-ordering design (plan:319) has not reached the type documents.

### 1.3 Where facts are stated more than once

- **Records and enums.** 235 types declared in fenced code blocks; 87 declared in two or more documents (my count); 20 with differing member lists (the existing script, run today).
- **Counts and orderings.** 147 diagnostic codes declared against other totals stated elsewhere (plan:1071); the stage order in at least four places; builder passes numbered two ways (plan:1066).
- **Prose rules.** The induction paragraph in three documents; the definition of a dead end, stated in five documents with three different meanings (Part 4, question 4); the operation order; the writer list.
- **Lists canon hands to source files.** `soundness-and-coverage.md:200–203` and `:214–217` name two `.cs` files as "the source of truth" for the fault codes and obligation kinds; `tooling-surface.md:1251–1261` does the same for six catalogs; `primitive-types.md:707` for diagnostics. My grep finds 12 such lines. Pointing duplicates at these would make code the design, so they need the opposite treatment (Part 2.2).

Not every duplicate is a copy. Four of the fifteen the scripts found disagree on substance (plan:1096–1100). `Restore` recomputes computed fields in one document and leaves them stale in another (plan:1065). Choosing an owning document for those chooses an answer, so they are handled as decisions, not merges (Part 3.3).

### 1.4 What phase 2 left

145 register rows: 133 applied with a commit, 12 dispositioned with a reason (plan § 2). Rows N105–N119 are resolved in the register but not carried out; N105, N106, N107 and N117 are substantive and concern records the proof model will change. Sixteen findings are open under exit condition 4 (plan:1040–1080).

Phase 2 changed canon by 927 lines added and 955 removed across 22 files (`git diff --shortstat b93d1db1 HEAD -- docs/philosophy.md docs/compiler-and-runtime-design.md docs/glossary.md docs/language docs/compiler docs/runtime docs/tooling`). Its record files total 17,749 lines (`find docs/working-reset/phase-2 -name '*.md' | xargs wc -l`).

---

## Part 2 — What "done" means

### 2.1 Complete

1. **The want map.** Every claim in your want document, with the canonical section that owns its statement. Two agents build the claim list independently; an adjudicator (Part 3.5) settles differences. Done at zero unowned claims.
2. **Canon's promises to itself.** Every "defined in", "see §" and named catalog resolves to a section that states the thing. Done when a script finds none landing on a placeholder, stub or nothing.
3. **The proof model's contents.** The list is built from the want map, not written from memory, and fixed before writing starts. It must include at least:
   - what is owed and where: rules at creation and at every write of a field they mention, through the condition, the `when` condition and computed fields (walk items 8 and 35); state constraints on every way into the state, including ways that write nothing (walk item 7); fault obligations at the point of evaluation while other rules are judged on what the handler commits (walk item 9); one proof per path to an expression (walk item 3);
   - every position an expression can sit in, not only every form (`soundness-and-coverage.md:244–263`);
   - what a proof may rely on: the four kinds of premise plus substitution along the handler (walk item 5); the second write made by an `into` target (`precept-language-spec.md:1997`); and, each as a proof step owed its own argument under full proof power: that earlier rows did not match, that the entity is in a given state, a computed field's formula, and magnitude reasoning for divisors;
   - the permitted proof steps, each named, with the limits on what counts as an admissible strategy (`precept-language-spec.md:225`; `soundness-and-coverage.md:273–279`) and the safety limits now in the placeholder at `soundness-and-coverage.md:334`;
   - the three results, visible to authors (ruled 2026-07-26, plan:279), and for a refusal the computed guard or constraint that would close the proof (want:179);
   - the certificate's contents (want:199); the set of runtime checks derived from its premise list (want:210); what inspection reports as provenance (want:211);
   - the checker: what it confirms, and where both call sites sit. In the finished product the checker works out what a definition owes by asking the catalogs (walk item 1, "the idea to use the catalog was mine"), so the design specifies those catalog entries. That is a different thing from where this work takes its lists: the lists in item 4 are extracted from `docs/language/`, never from today's catalogs, because today's catalogs are incomplete;
   - the bounded counterexample search (plan § 3 ruled list; walk item 1);
   - each operation: the construction exemption (spec § 5); what restore assumes and whether it recomputes; proofs when inspection lacks an input (`precept-language-spec.md:2165–2167`);
   - the argument that the induction holds for this language, naming state position, computed fields, `omit` resets, entry and exit actions and first-match rows (plan:1716);
   - the structural rules;
   - the overflow model.
4. **Coverage against the language's lists.** For every expression form in every position, every action, writer, constraint kind, type and operation, the proof model names the step that handles it or the refusal and its message. The table is generated from the lists. Because the design states the full intended proof power, a refusal row must cite the principle that excludes the construct (`precept-language-spec.md:225`, want:135, or a ruling of yours). "Not built yet" and "not in the first compiler" are not reasons.
5. **Open questions.** The inventory is built from every heading matching "open question" or "pending" and every marker matching `TBD`, "unresolved", "undecided", "not yet ruled", "pending owner" across canon, plus the two README indexes and the old plan's § 4. An open-questions section that names an item nothing defines (as `type-checker.md` § 13 does with W2 and G1–G3) counts as a failure until the item is defined or removed. Done when every row has your answer, a default recorded with its four-part reasoning, or your quoted word that it waits.
6. **Deferrals.** Every "deferred", "post-MVP", "later" and "v1" in canon is listed and sorted into one of three: designed now; ruled out of the first version by you, with your words quoted; or deferred in build order only, in which case its design is still owed here. "The MVP" is a build milestone by your confirmation (`trace-walk-rulings.md:71–77`), so "post-MVP" on its own puts an item in the third group.
7. **Unfinished documents.** One row each, with a finish condition:

   | Document or part | Finish condition |
   |---|---|
   | `soundness-and-coverage.md` | Every placeholder replaced; § 6 restated as design |
   | `docs/tooling/extension.md` | Full document at the detail of its neighbours |
   | Preview and inspect (`language-server.md:1575`) | Designed, including what it shows as proof provenance |
   | `descriptor-types.md` | Becomes the owning statement for the descriptor records |
   | The five uncatalogued writers | Catalog entries specified in `catalog-system.md` |
   | Overflow model | Designed for `integer`, `decimal`, the temporal types and the approximate number type (stated once, under question 6); the `integer` part after question 6 |
   | Period ordering | The locked design stated in the type documents |
   | `graph-analyzer-roadmap.md` modifiers | Each sorted under item 6 |
   | Empty TBD sections in two type documents | Filled or removed with a reason |
   | Two unresolved questions in `tooling-surface.md` | Answered; the README already says both are stale text |

8. **A status that means finished.** Every canonical document carries the two-row block the stage documents already use: document maturity and implementation state. A finished design reads "Full" on the first row whatever the second says. README status columns are generated from the documents. This reuses an existing value; it needs the taxonomy in `docs/README.md` and `CLAUDE.md` to say so, which is a one-line notice to you, not a question.

### 2.2 Consistent

1. **Ownership recorded in canon.** Each restating site becomes a pointer of one fixed form naming the owning document and section. A script regenerates the ownership table from those pointers and from the declarations, so the table is output, not a side record.
2. **Lists canon hands to code are restated in canon.** Each of the lists in 1.3 is written out in full in its owning document and the sentence is reversed: the document is what the source file is checked against.
3. **Scripts over the whole set.** Three of the four existing checks read `docs/language`, `docs/compiler`, `docs/runtime`, `docs/tooling`, the overview and the root `README.md`; they omit `glossary.md`, `agent-onboarding.md` and `docs/contributing/`. `declared-types.py` and `undeclared-identifiers.py` read fenced code. `stated-counts.py` already scans every prose line, and `code-stage-attribution.py` reads the diagnostic document plus a fixed stage map and searches each stage document's full text. All four print candidates for a reader. Needed: the wider file list; the ordering check (plan:1105); a check for tables keyed by a language list; the status-column check; pointer-form and resolution checks; and an exceptions file per check. **An exception is approved by a different agent from the one that proposed it.**
4. **Prose rules.** A fixed list of concepts is taken from the specification's 50 section headings and the glossary's entries. Each gets an owning section. Each concept's terms are searched across canon; every hit outside the owner is classified as a pointer, a restatement to convert, or an unrelated use of the word. The list is finite, so the check ends.
5. **Language documents against themselves.** For each member of each extracted list, every canonical statement of its rules is compared. This replaces the reread phase 3 was going to give them.
6. **The hand walk.** Examples are carried through every document in stage order and then through all six operations: create, fire, update, restore, inspect-fire and inspect-update. A different agent does each stage with only that stage's document and the previous output. The example set is your bank example and its mutations plus at least one example per type family, chosen so every action, writer and constraint kind appears.
7. **Paired derivation.** Two agents independently write out obligations and results for each example. Expected results for your example and its fourteen mutations come from your want document. Expected results for every other example are derived from canon by agents and marked as such; where two derivations disagree and canon does not settle it, the case goes through the question gate.
8. **Attack rounds, per argument.** Part 3.6.

Stated limits: items 6–8 are agents reasoning on paper and can share a blind spot; the certificate checker, which is code, is the eventual backstop.

### 2.3 What you sign

Three things, and only these: the replacement of the plan (question 1); the proof model after its attack rounds, which you read as your example worked under it together with the refused list; and the finish, which you read as the final counts. Defaults are not signed. They are kept in one list in the repository and you overturn any you disagree with, whenever you look; nothing waits for you to do so. The early checkpoint (3.8) is recorded in the status file, not signed.

---

## Part 3 — The path

### 3.1 Order: the proof model beside deduplication

- **Reasoning.** Reconciliation ahead of the unwritten part is how phase 2 ran. The proof model is the one part that may not be achievable (want:133), so it must be tested early. Deduplication of plain copies needs no judgement and can run meanwhile.
- **Alternatives rejected.** Deduplicate first: repeats phase 2's order. Proof model strictly first: leaves work that depends on nothing in the proof model waiting for no reason, and leaves every changed record in several places when step 5 arrives.
- **Precedent.** The project's own record (plan:1653–1684 on the earlier attempts; the phase-2 loop diagnosis in memory).
- **Tradeoff accepted.** Some deduplicated facts will later be changed by the proof model. Each such change edits the one owning statement and is logged.

**What the proof model reaches is marked from evidence, not guessed.** Revision 1 marked it before the model's contents were known. Here it is marked twice, and each marking is tied to the thing it is made from. A provisional marking, made from the hand-working of your example, is what freezes deduplication: no merge starts before it exists, and nothing so marked is merged. A second marking, made from the fixed contents list at the end of step 1, confirms it, and can add to the frozen set or release from it. Frozen until the proof model passes: the proof records, the certificate carrier, constraint and field descriptors, **all result types** (`result-types.md:119` justifies a missing outcome by the induction, and `:148`/`:152` encode one answer to the editable-edit question), anything touching update or restore, the stage order (the checker's place is open), the diagnostic totals, and register rows N105, N106, N107 and N117.

### 3.2 The steps

Each step is stated the same way: what must exist before it can start, what it produces, the condition that finishes it, and the counts that measure it. The counts are printed by the scripts built in step 0, reading files in the repository. No step has a date or a length. A step that needs nothing from another runs beside it.

**Step 0 — build the scripts.**
- *Needs:* nothing. It edits no canonical document, so it runs before question 1 is answered.
- *Produces:* the counting scripts; one workflow script per kind of work; the layout of the progress files; the run settings; the one-line instruction that starts each step (5.7).
- *Finished when:* every script has been run against made-up inputs and behaved as designed, and one real piece of each kind of work has been run and its cost recorded (5.3). Parsing a script is not enough.
- *Counts:* scripts that pass their tests, of the total; kinds of work with a measured cost, of the total.

**Step 1 — lists, maps, questions, the bank example, the contents list.**
- *Needs:* step 0.
- *Produces:* the language's lists, extracted from `docs/language/` (never from the catalogs), including expression positions, each checked against the prose by a second agent and recorded with a hash. The writer list is design work, since five writers have no catalog entry. The widened checks. The ownership draft. The want map. The rulings table. The deferral and open-question inventories. Every candidate question checked, and the survivors prepared. The bank example worked by hand. A pilot of the hand walk on the lexer, parser, name-binder and type-checker documents, with the first merges of plain copies in those four documents, made only after the provisional marking exists. The provisional marking of what the proof model reaches. Last, the fixed list of the proof model's contents (2.1 item 3) with a hash, the confirmed marking, and the number of arguments.
- *Finished when:* every list is extracted, checked and hashed; the want map has no difference left between its two builders; every heading and marker the inventory patterns match has a row; every candidate question is either dropped with its settling citation or prepared; two independent derivations of your example and its mutations agree; the contents list is fixed and hashed.
- *Counts:* lists extracted, checked, hashed; want claims listed, owned, unowned; inventory rows by state; candidate questions checked, dropped, prepared; derivations in agreement; arguments on the contents list.
- *The first packet does not wait for all of that.* It goes to you when the question checks, the prepared survivors, the hand-worked example and the draft refused list exist (Part 7).

**Step 2 — write the proof model.**
- *Needs:* the fixed contents list from step 1, and your answer to question 1, because this is the first step that writes the new design into canon at scale. Each section also needs the sections it rests on.
- *Produces:* the proof model, written section by section straight into the target canonical sections by one author working in sessions. Each session ends with a short hand-over note: what was written, what it rests on, what is open. The five arguments the early checkpoint names (3.8) are written first, so that the test of whether the model can be written at all comes as early as the order of work allows. Sections that depend on question 2 or 3 are written both ways; the induction over operations is written without clock rules until question 5 is answered.
- *Finished when:* every section on the contents list is written in its canonical place with its hand-over note, and no section remains in two versions.
- *Counts:* sections written, of the total; sections held in two versions for an unanswered question.

**Step 3 — everything the proof model does not reach.** Runs beside steps 2 and 4.
- *Needs:* your answer to question 1. Then each kind of work has its own need. Merging plain copies needs the provisional marking, the ownership draft and the widened checks. Deciding a disagreement needs the want map. An internals answer needs the open-question inventory. The `integer` part of the overflow model needs question 6. What preview and inspect show as proof provenance needs the proof model's section on provenance from step 2, so that one row finishes later than the rest of this step.
- *Produces:* plain copies merged; disagreements decided (3.3); prose concepts given owners; lists that canon hands to source files restated in canon; internals answers (3.4); the unfinished documents finished.
- *Finished when:* outside the frozen set, the scripts find no second full statement of any fact and no pointer that fails to resolve; no disagreement is undecided; every concept's hits are classified; every internals row has an answer or a recorded default; every row of the unfinished-documents table that does not wait on the proof model meets its finish condition.
- *Counts:* facts found, merged, frozen; disagreements between canonical documents remaining; concepts classified; internals rows open; unfinished-document rows met; lines changed in canon against lines written outside it.

**Step 4 — attack each argument (3.6).** Runs beside steps 2 and 3.
- *Needs, per argument:* that argument's section written in step 2; the kinds of flaw written down, once, before any round; and no open question the argument's text depends on. An argument written in two versions is parked, not attacked twice, until the answer arrives; it is counted as waiting on you.
- *Produces:* the record of every round; the repaired arguments; the early checkpoint's result; and, when the last argument has passed, your example worked under the finished model together with the refused list.
- *Finished when:* every argument on the contents list has passed under 3.6, or has been resolved after reaching you; a hash of the proof-model sections is recorded; and you have signed the proof model.
- *Counts:* arguments held; arguments still in rounds, with the round each has reached; arguments with a second author; arguments that failed a second author and reached you; arguments parked on an unanswered question; refutations open.

**Step 5 — carry the model through.**
- *Needs:* your sign-off of the proof model.
- *Produces:* the model carried into every document it changes: `proof-engine.md`, `catalog-system.md`, builder, evaluator, runtime API, result types, diagnostics, tooling; and into every document that restates it: specification §§ 0.6–0.7, overview § 1, glossary, onboarding, `docs/README.md`. The frozen facts deduplicated. Register rows N105, N106, N107 and N117 carried out. `CLAUDE.md`'s catalog notice shown to you as a proposed edit.
- *Finished when:* every document on that list is updated; the scripts find no second full statement and no unresolved pointer anywhere in canon, the frozen set included.
- *Counts:* documents carried, of the total; frozen facts remaining; changes made to the hashed proof-model sections (3.7).

**Step 6 — the walk and the finish.**
- *Needs:* steps 3 and 5 finished; every inventory row closed; every question answered.
- *Produces:* the full hand walk with its repair budget (3.7); the final script run; the coverage table; the final refused list; the final summary.
- *Finished when:* every condition in 2.1 and 2.2 is met as the scripts read it; the walk's second pass found fewer new defects than its first and none is open; and you have signed the finish.
- *Counts:* examples walked by stage and operation; defects found per pass; conditions in Part 2 met, of the total.

Phase 3 of the old plan never produced its lists (plan § 2), so there are none to keep and extraction is new work. Two workflow scripts from that phase are in the repository (`docs/working-reset/phase-3/enumeration.workflow.js`, `block-inventory.workflow.js`); step 0 reads them and reuses what fits. I read the opening of one for this pass and neither in full. Numbering every statement stays with acceptance criteria.

### 3.3 Deduplication is two kinds of work

- **Copies that agree** are merged mechanically: the owner keeps the full statement, the others become pointers. No detail is dropped.
- **Copies that disagree** are decisions. A fresh agent, given the want document, the philosophy and the owning specification section, decides against that text with a citation. If the text does not decide it, the item goes through the question gate. A disagreement is never settled by picking which document owns the fact.

Deduplication agents get the want document and the relevant specification section, not excerpts alone.

### 3.4 Internals questions

Most of the 21 runtime rows are choices no author or caller can observe: how a version stores its slots, opcode behaviour, stack against registers. They are not proof questions and not language questions, so they get their own step and rule. An agent drafts each answer with reasoning, alternatives rejected, precedent and tradeoff; a second agent attacks it; the result is recorded as a default you can overturn. A row a caller can observe (for example, whether a refused certificate throws or returns a result) is flagged as such in the defaults list and shown to you with the next batch or sign-off. Rows inside the proof model (the certificate gate; how the certificate reaches `Precept.From`) are answered by step 2. One row is not defaulted: `catalog-system.md:1509` says "Pending owner ruling", so it is reserved to you and is question 8.

One inherited exception is recorded in the new plan so that freezing the old one does not lose it: whether anything constructs a `Fault`. Your words, 2026-08-11: "do we need to decide this now? seems like something we can defer until we start building the runtime" (plan:273). `runtime/README.md:36` words it as "until the runtime design is done", which is not what you said; that difference is a disagreement to settle under 3.3.

### 3.5 Who settles a dispute between agents

One fresh adjudicator per dispute, with a written order of precedence: your quoted ruling; then the want document and philosophy; then the language documents; then compiler and runtime documents; then a restating site. Where none decides, the item goes to the question gate. Each ruling is logged with path and line. Requests for a ruling are files in a queue. The workflow script that runs a round starts one adjudicator per queued request and does not end the round with the queue non-empty; no session decides a dispute by itself, and starting an adjudicator is not analysis. A request still open after the round that should have settled it is counted as stalled, which is one of the conditions that reaches you (5.6).

### 3.6 Attack rounds are budgeted per argument

The measured record: two arguments that went through twelve rounds held; five that went through none were all refuted in one pass (memory, `feedback_soundness_arguments_need_adversarial_rounds.md`). Revision 1 allowed three rounds for the whole model, a quarter of that.

- Each argument on the contents list is attacked separately, for **twelve** rounds. The old plan expected "a couple of dozen" arguments (plan:547); with full proof power there will be more; the count is known when the contents list is fixed.
- **There is no early pass.** The record supports twelve rounds and gives no evidence that fewer suffice, so every argument gets all twelve, with fresh attackers each round. A refutation is fixed and the rounds continue. An argument passes when round twelve ends with nothing open and the last two rounds found nothing. It is never marked settled in the session that wrote it.
- The kinds of flaw are written down before round one. A fresh agent that is neither author nor attacker classifies each refutation. Counts go in a data file a script reads.
- An argument that ends round twelve with something open, or with a finding in either of the last two rounds, or whose open refutations rise for three rounds running, goes to a **second author** on a different model with the counterexamples, for up to six more rounds.
- Only if that fails does it reach you, with the counterexamples and three choices on equal terms: more rounds, a different argument, or narrowing the language. Narrowing is language design: it carries your override where a locked decision is touched and goes through `/design`, never decided inside this work.

### 3.7 Reopening the proof model

When step 4 passes, a hash of the proof-model sections is recorded. Any later change to them is logged with its reason and re-runs the attack rounds on the changed arguments only. The status file counts such changes. The hand walk gets two passes; if the second pass finds more new defects than the first, that line of work stops and you are told (5.6).

### 3.8 Stop rules, counted by script

Two lines of work, reported separately so fast deduplication cannot hide a stalled proof model.

- **Proof model.** The per-argument rules above. **The early checkpoint, criteria written now, all of them about agent progress.** It is reached by an event, not a date: the five arguments named here have each completed three of their twelve rounds. Step 2 writes those five first and step 4 attacks them first, so nothing else is ahead of the checkpoint in the order of work. The criteria: the lists are extracted and hashed; the arguments for establishing a rule at creation, preserving it across a `set` in an event row, and using a guard, an argument constraint and a pre-state rule as premises are each written and have completed at least three rounds with no refutation left open; two independent derivations of your example and its mutations agree. The result is written to the status file, with any failing criterion named. A failing argument goes to the second author as 3.6 says; nothing is put to you from the checkpoint itself. You are asked only if a second author fails, and then with the three choices in 3.6. If the checkpoint is not being approached at all, that shows as runs that make no progress, which reaches you under 5.6.
- **Waiting on you, reported separately.** The status file lists each unanswered question and the work parked behind it. That is never a pass criterion. Work that depends on an answer is written both ways or parked.
- **Everything else.** Open items (unowned facts, undecided disagreements, unanswered inventory rows, unexplained script results) must fall from one run of a step to the next. A count that rises is reported to you when it happens. A rise across two consecutive runs stops that line with a named next action.
- **No open-ended reading passes.** Every check walks a list named in advance.
- **New work found on the way is parked** unless one of the completeness lists needs it.
- **Settled things stay settled.** The want document carries your approval in full.

### 3.9 Decisions, defaults and their records

Every decision and every default is recorded with reasoning, alternatives rejected, precedent and tradeoff accepted, as `CLAUDE.md` requires. Anything that would add or change what an author may write is surfaced to you first and goes through `/design`.

**No refuse-more defaults on proof questions.** Revision 2 first rested such defaults on your 2026-07-26 "minimal proof set for the MVP" ruling. That ruling is about the first build. The design states the full intended proof power (Part 4, "Not asked", first row), so "the compiler refuses this for now" is not a design position and no default of that kind is taken.

What replaces it is a rule about soundness, which holds whatever the proof power: **no proof step enters the design until its argument is written and has passed its attack rounds** (3.6). The grounds are the specification's first proof principle, "Soundness over completeness. The proof layer must never claim an expression is safe when it is not" (`precept-language-spec.md:221`), and the measured record in 3.6. A step whose argument has not yet passed is listed as pending, not as refused. A step whose argument fails after the second author reaches you under 3.6.

Defaults remain for one kind of thing only: internals choices nobody outside can observe (3.4), each recorded with its four parts. A question about what the language means, or one canon or the walk reserves to you, is never defaulted.

**Records.** One plan page at a fixed path, `docs/working-reset/design-completion-plan.md`; the old plan is marked superseded in the same commit, and the memory index's pointer to the live plan is updated in the same step and checked by script (it lives outside the repository, so it cannot be in the commit). Besides the page: one append-only log; the progress files, one folder per step, of which the attack-round record is one (5.2); the status file and the notices file (5.6); the defaults list; the scripts and their settings (5.3); one exceptions file per check; one short packet per question put to you. Nothing else. If, over the runs of a step, lines written outside canon exceed lines changed inside it, that is reported to you as a count that moved the wrong way.

### 3.10 Agents and models

What I checked: the model table in the `claude-api` skill, cached 2026-11-03. It lists Claude Fable 5.2 and Claude Opus 5.5 with a 1M-token context and 128K output, and Claude Sonnet 5.5 at 1M. I did not query the live Models API, and I did not check what context a Claude Code session actually gets, which may be less.

**Can one session hold the proof-model author's reading list?** The list: philosophy, want document, the full specification, `proof-engine.md`, `soundness-and-coverage.md`, `catalog-system.md`, the overview, the walk file, your 2026-08-26 rulings, and four named write-ups (`phase-5/what-discharges-an-editable-write.md`, `phase-5/must-a-proof-name-what-holds-a-fact-up.md`, `phase-5/where-a-divisors-non-zeroness-comes-from.md`, `phase-4/what-a-refusal-claims.md`). That is 844 KB (`wc -c`), about 210,000 tokens at four characters per token and up to about 285,000 if the tokenizer runs 35% heavier. Under a third of the listed window, by estimate; not measured with the token-counting endpoint. Type documents are read as the coverage table calls for them, row by row. The author still works in sessions of one contents-list section each, with hand-over notes, because the whole model is more than one session can write, and because a section is the piece of work a run saves (5.2).

Before the write-ups are handed over, passages in `what-discharges-an-editable-write.md:37–42` and `editable-write-cascade.md:116–122` that call want:36 and want:40 "not yours" are marked superseded: you approved the whole want document on 2026-08-26.

| Work | Agents | Model |
|---|---|---|
| Proof-model author | 1, in sessions | Fable 5.2 — the hardest unsolved part; briefed with goal and constraints, not a checklist |
| Attackers, flaw classifier, adjudicator, question checkers | 2 attackers per argument per round; 1 classifier; 1 adjudicator per dispute | Opus 5.5, pinned by name — a different model from the author, so agreement means more |
| Second author for a failing argument | 1 | Opus 5.5 |
| Deciding disagreements; internals answers | 1 per item, plus 1 attacker | Opus 5.5 |
| Merging plain copies; list extraction; inventories | 4–6 on disjoint files | Sonnet 5.5; scripts where possible |
| Hand walk | 1 per stage per example | Opus 5.5 |
| Counting scripts and workflow scripts (step 0) | 1 writer; a different agent writes the tests against made-up inputs | Opus 5.5 — every stop rule in this plan depends on these being right |

The memory file on model roles (2026-07-25) says an agent with no model named inherits the session's model, and to pin review to Opus. Fable draws on weekly limits faster than the others (memory, quoting the Help Center). Your standing instruction is to use the model the task needs, not to change models near a cap without asking you, and that waiting for the reset is often the right call. This table follows that, and 5.5 says what an unattended run does at a limit: it pauses and tells you; it does not change models.

---

## Part 4 — Questions for you, re-derived

Each candidate below was checked by me against the walk file, `trace-walk-rulings.md`, the old plan's § 3 and § 4, and today's memory. That is the author's check. One further filter was added after your answer on proof power: if the options differ only in how much the compiler can prove, the item is design work, not a question. In step 1 a fresh agent repeats it without seeing these conclusions, and only what survives reaches you. Worked examples here are illustrations; the `precept-author` agent drafts each one against the specification before you see it. No option below proposes new syntax.

### Not asked, and why

| Revision 1 asked | Why it is no longer a question |
|---|---|
| How much proof power the design states | **Answered by you, 2026-11-13: the full intended proof power.** Your words: "it describes the full proof power, is that not clear from all the forensics and what I want". Grounds: want:14, "the compiler **proves or rejects all business rules**"; want:171, "there is **no deferral**"; want:183, "for now we are thinking ideal world". Your 2026-07-26 ruling, "a minimal proof set for the MVP" (plan:279), is about what the first build ships, not what the design states. The limits on proof power are the ones the design itself draws: the four tests a proof strategy must meet (`precept-language-spec.md:225`) and the line on non-linear rules (want:135). |
| May a proof use that earlier rows did not match | Proof power, so design work under that answer, not a question. Rows are tried in order and the first match wins (`precept-language-spec.md:2006`), so the inference is sound if its argument survives its rounds. It goes on the contents list. |
| Where a divisor's non-zeroness comes from | The same. "Declared on the divisor itself" and "reasoned through the arithmetic with magnitudes tracked" differ only in how much is proven (`where-a-divisors…`, options one and two; the survey you asked for is done). The design specifies the magnitude reasoning, with an argument for each numeric type. What the first build ships is a build decision. |
| May "the entity is in state S" be a premise; may a computed field's formula establish a constraint on it | The same. Both are proof steps the design owes an argument for. Until an argument passes its rounds, neither is used: for the computed field that is the position you marked yours at walk item 32, "on the conditional form". |
| May the example be corrected? | A consequence of the rule you marked yours at walk item 18: an optional read is refused unless guarded (`precept-language-spec.md:1561`). There are **two** unguarded reads. The guard at want:75 has nothing before it that shows the field is set, so it is corrected and shown in the first packet. The message at want:81 is in the third row; with the first row's guard corrected, the first two rows not matching shows the field is set. So want:81 stands as written and depends on the first-match argument above. If that argument fails its rounds and its second author, you are told and the message is rewritten then. Your mutation table (want:112–127) is rechecked against the corrected guard. |
| Is the overflow design part of this? | Canon answers: in scope, build deferred (`precept-language-spec.md:104`, `:110`, `:272`), and "MVP" is a build milestone. What it covers is stated once, under question 6. The choice left to you is question 6. |
| `from any` per state | Not defaulted. The specification records it as open (`:2203`). A fresh agent first checks whether the two readings differ in anything an author or caller can observe. If not, it is a proof-design choice with an argument covering `from any` and `in any`. If so, it comes to you as a worked case. |
| Must a proof name what keeps a fact true | Not defaulted: you reserved it (walk item 6: "I'm not prepared to rule right now"). It is question 7. |
| `ensure` absent from the want document | Answered by text you approved: `philosophy.md:20` names "the ensures that must hold in specific states or on specific events". The other half of that finding — an event `ensure` evaluated at entry, against want:143's "anything relational an event needs lives in its guards" — is a separate item and stays open for the fresh check, which is given the relational case and `evaluator.md:1377`. |
| What carries the certificate | Answered by want:199 ("alongside the compiled definition"). Writing work. |

### Batch 1 — sent when step 1 has prepared it

**Question 1. Replace the thirteen-phase plan.**
Existing text: walk item 1 lists the reset rulings; your mark was "mostly mine".

| Ruling from walk item 1 and plan § 3 | Here |
|---|---|
| Checker in scope, including its runtime half | Kept |
| Coverage rests on the catalogs | Kept |
| Bounded counterexample search in scope | Kept, on the contents list |
| The procedure walks the language's expression forms; the grid is generated | Kept |
| Vocabulary rulings | Kept |
| A branch, not a copied folder; promoting it gates nothing | Kept |
| A closed list stays closed, with a recorded hash | Kept (3.2, 3.7) |
| Thirteen phases | **Replaced** by the seven steps in 3.2, none with a date |
| Three attempts then stop | **Replaced** by per-argument budgets (3.6) |
| The phase-4 early stop | **Replaced** by the early checkpoint (3.8) |
| Confirm, narrow or revert at phase 8 | Kept as your read of the refused list |
| The build phase carries no size | Untouched; after the design |

Options: replace as above; or resume the thirteen-phase plan at phase 2's exit condition 4. Reasoning for replacing: the old order puts the unwritten model behind a reconciliation phase and a numbering phase. Rejected: resuming, for that reason. Precedent: the loop diagnosis you made yourself on 2026-08-27. Tradeoff: three of your rulings are replaced, which is why this is asked.

**Question 2. What holds a rule when an editable field it mentions is edited.**
Existing text, all yours: want:143, "For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the field … evaluating those rules at the ingress point is exactly the premise that closes the editable write sites"; want:171, "there is **no deferral**"; want:199, the runtime "does **not** re-evaluate them against the post-mutation configuration"; `philosophy.md:22`, "every value that enters from outside is held to its declared constraints at the moment it enters"; `philosophy.md:42`, a rule "does not hold because something checks it at the right moments". On 2026-08-26 you declined to have this settled in passing and had the canonical sentences that decided it removed (`trace-walk-rulings.md:42–46`).

**The finding to read first.** Your approved example declares `RepaymentCapPercent … editable` (want:36) and a rule that multiplies by it (want:40), and says the example is accepted (want:110). Nothing in the example bounds an edit to that field. So the example is accepted only if rules are evaluated at the edit, which is the want:143 reading. The fresh check is asked to confirm or refute this before it reaches you.

Worked case for all options (from the prepared write-up):

```precept
field CreditLimit as decimal nonnegative max 25000.0 default 1000.0 editable
field OutstandingBalance as decimal nonnegative default 0.0
rule OutstandingBalance <= CreditLimit because "…"
```

| Option | What happens to this file | What it refuses |
|---|---|---|
| 1. Prove from the field's declared bounds and any guard on the edit; the engine checks only the field's own constraints | Refused, with a counterexample (balance 3000, limit edited to 100) and three ways to close it | Any editable field in a relational rule the declarations do not entail. Your example as written. |
| 2. The engine evaluates the rules the edit could break, before applying it, and the compiler cites that | Compiles; a bad edit is refused at runtime with the rule's own message | Nothing. Costs: on this route the rule is held by a check, not a proof; `philosophy.md:42` would need rewording, which is yours to authorise |
| 3. A field a relational rule mentions may not be `editable` | Refused at the declaration | Everything option 1 refuses plus what option 1 would have proven. Your example as written. |
| 4. A declared condition on the incoming value | — | New language construct. Goes to `/design` as its own pass; option 1 or 3 stands meanwhile |

No default. Meanwhile the author drafts the edit section for options 1 and 2 side by side; option 3 needs less than 1.

**Question 3. State actions.**
Existing text: `precept-language-spec.md:2016`, "Still unsettled … when a state carries more than one state action, whether all of them fire and in what order; and whether entry actions fire at construction and on a self-transition." Also `:1024` (a state action may carry a `when` guard), `:1082` (the `omit` reset applies "on any transition into an `omit` state (including self-transitions)"), `:2092` (construction is "entry into the initial state").

Worked case:

```precept
state Draft initial
state Review
to Draft -> set Opened = true
to Review when Priority == "High" -> set Escalations = Escalations + 1
to Review -> set ReviewCount = ReviewCount + 1
from Review on Reassign
  -> transition Review
```

- (a) Two entry actions on `Review`: both run in written order where their guards hold, or the second declaration is refused.
- (b) At creation: `Opened` is set, or it is not.
- (c) On `Reassign`: `ReviewCount` rises, or it does not.

The two canon lines above lean toward "yes" on (b) and (c); they do not say it. No default on any of the three: each is about what the language means, not how much the compiler proves. Meanwhile the proof model's treatment of paths is written for both answers; your bank example has no state actions.

**Question 4. What a dead end is.**
Existing text, quoted and not interpreted:
- want:152: "Every non-terminal state has an exit … A state you can enter but never leave is rejected, unless the author deliberately marked it `terminal`."
- `philosophy.md:38`: "a business process cannot contain an unreachable step, a position with no way out, or a route around a mandatory stage."
- `philosophy.md:44`: "Every non-terminal state has a way forward — no dead end an entity enters and cannot leave. Required states lie on every path to completion — no entity skips a mandatory step."
- `compiler-and-runtime-design.md:147`: "every non-terminal state must have an outgoing path".
- `precept-language-spec.md:186`: "Non-terminal states where all outgoing rows reject or produce no-transition".
- `graph-analyzer.md:451`, `:455`: a reachable non-terminal state that "cannot reach any terminal".
- `graph-analyzer.md:457`, the same document's stated reason: "A state an entity can enter but never leave is a structural defect: the lifecycle the author declared cannot complete from there."

| Case | Exit-only reading | "All rows reject" reading | "Cannot reach a terminal" reading |
|---|---|---|---|
| `Active` and `Paused` lead only to each other; a `Closed terminal` exists but neither reaches it | Accepted | Accepted | Both refused |
| A subscription with `Active` and `Paused` and no terminal state at all | Accepted | Accepted | Every state refused |
| `Frozen` whose only rows reject | Refused | Refused | Refused |

No default: each reading changes what compiles in a different direction.

### Batch 2 — sent when its packets are prepared and batch 1 has been answered

**Question 5. Rules that mention the clock.**
Existing text: want:219, "Do rules that reference the current moment exist at all, or does time belong only in guards … Deliberately left open." `philosophy.md:28` (approved later): "The one environmental value the language admits is the clock … a rule whose correctness depends on when it is evaluated says so in the definition". `temporal-type-system.md:568`: such a rule "will produce different results when fired at different times. This is accepted behavior". Against both: `philosophy.md:18`, "the entity's data satisfies its declared rules at every moment", and `:42`, "there is no moment at which it could fail".
Worked case (`temporal-type-system.md:577`): `rule now() - IncidentAt <= '72 hours'`.

- **Keep such rules.** Nothing is refused. The rule can become false with no operation running, so what the compiler proves about it has to be defined, and `philosophy.md:18` and `:42` no longer hold of it as written. That is a gap in the philosophy, raised here for your direction and not resolved.
- **Time in guards only.** That rule is refused; the author writes the condition as a `when` on the rows that depend on it. **This overrides locked text** at `philosophy.md:28` and `temporal-type-system.md:568`, which only you can authorise.

No default. The induction over operations is written without clock rules in the meantime.

**Question 6. The `integer` arithmetic model.**
Existing text: walk waiting list — "fixed-width 64-bit under prove-or-reject versus arbitrary precision for `integer`, and the temporal case that follows it"; `primitive-types.md:697`, "a post-MVP decision, not yet ruled".

**What the overflow model covers is not part of the question.** It covers `integer`, `decimal`, the temporal types and the approximate number type. `primitive-types.md:697–701` names the first, third and fourth; `primitive-types.md:236` says of `decimal + decimal` that "Overflow is an error — the target, with the proof behind it in scope and not yet implemented, as for `integer`"; `philosophy.md:46` names arithmetic overflow without restricting it by type. `precept-language-spec.md:272` lists "integer, temporal, and number-lane infinity" and omits `decimal`; that is a disagreement between two canonical documents, decided under 3.3 against the philosophy and `primitive-types.md:236`. One consequence shown in the first packet: your bank example's `Balance + Deposit.Amount` owes an overflow proof, so it needs declared bounds or the design must say what else closes it.

Worked case for the choice that is yours:

```precept
field Visits as integer nonnegative
  -> set Visits = Visits + 1
```

- **Fixed width.** The addition owes an overflow proof: refused until `Visits` has a declared `max` and the row a guard below it.
- **Arbitrary precision.** Accepted; an `integer` cannot overflow. Costs a different representation in the runtime and at the host boundary. `decimal`, the temporal types and the approximate number type owe their proofs either way.

No default. The packet is a three-page condensation of the overflow recommendation, made after the corrections its own review requires (`review-of-the-recommendation-2026-07-27.md` §§ 2–3).

**Question 7. Whether a certificate names what establishes and keeps each fact.**
Existing text: your answer at walk item 6, "this has not been put to me plainly. I'm not prepared to rule right now, would need more context and/or research." The six-minute write-up is ready. Both options are sound and accept the same definitions, so this is not a question of proof power; they differ in certificate size and in what the load-time check can confirm alone. The write-up's own suggestion, stated as yours to take or leave: name nothing by default and let the proof model identify the steps that must carry a name.

**Question 8. Builder metadata on constructs.**
Existing text: `catalog-system.md:1509`, on giving each construct metadata so the builder's assembly loop is fully generic: "Status: Candidate — value is marginal for ~12 constructs. Pending owner ruling." Canon reserves it to you, so it is asked, in one line: add the metadata, or record that the builder assembles construct by construct. Nothing an author can write changes either way. Prepared with the two record shapes side by side.

**Question 9. Open language questions inside the proposed extensions.**
The proposed extensions written into the canonical type documents are part of the design and are finished, not sorted in or out. Where one still has an open language question, `CLAUDE.md` requires a conversation with you before any design work. Two are known (`collection-types.md:1296–1300`): how a quantifier binds over a one-type collection against a two-type one, and whether `ascending` and `descending` are reserved broadly or only for priority queues. The deferral inventory may add others, the ten roadmap modifiers among them. Each arrives as a conversation opener with the canon text quoted and no shape proposed; the design then runs through `/design`.

### May come later

From the fresh checks: `from any`; the event `ensure` at entry; anything else an adjudicator cannot settle from your text. Budget: four. Each is first tested against one more filter added in this pass: if the options differ only in how much the compiler can prove, it is design work under your answer on proof power, not a question.

---

## Part 5 — How the work runs

The earlier revision said what the work is and never said what makes it happen. This part does. You start each step. From there the aim is that it runs without you, stops by a rule and not by an agent's judgement, and reaches you only when one of the conditions in 5.6 fires.

### 5.1 Each step is a workflow that runs to a condition

Every kind of work in 3.2 is carried out by a workflow script: a Claude Code Workflow tool script, which is plain JavaScript that starts agents in a fixed order, some one after another and some side by side. The order of the work is written in the script. No agent chooses what happens next.

A run of a script ends for one of three reasons, and for no other:

1. the step's finish condition in 3.2 is met;
2. a stop condition fires (3.6, 3.8, 5.5);
3. the run reaches its spending limit (5.4), in which case the next run continues from the saved progress.

Nothing ends, and nothing is called late or early, because time has passed.

The session you start for a step (5.7) does five things in a fixed order, and nothing else: it reads the progress files; it runs the counting scripts; it picks the next run of that step from the needs written in 3.2; it starts that workflow; and when the workflow returns it runs the counting scripts again, rewrites the status file, checks the conditions in 5.6 and commits. If the run ended only because it reached its spending limit and no condition in 5.6 fired, the session goes round again and starts the next run without you. If a condition fired, it writes the notice, and stops unless work remains that the condition does not touch (5.6). It analyses nothing and settles nothing. This replaces the "orchestrating session" of the earlier revision.

### 5.2 Progress is saved to files, and counts are read from them

Each kind of work has a smallest piece that can be finished and recorded alone:

| Kind of work | The piece that is recorded |
|---|---|
| Extraction | one list, extracted; then one list, checked and hashed |
| Question checking | one candidate question, checked; then one survivor, prepared |
| Writing the proof model | one section of the contents list, with its hand-over note |
| Attack | one round of one argument: both attackers' findings, the classifier's result, and any repair |
| Merging copies | one fact: the owning statement kept and every other site turned into a pointer |
| Deciding disagreements; internals answers | one item, with its four-part reasoning and the attacker's result |
| Unfinished documents | one row of the table in 2.1 item 7 |
| Carrying the model through | one document |
| Hand walk | one example through one stage or one operation |

The rules for these files:

- **Every agent writes its result to a file in the repository before it returns.** A workflow script cannot read or write files itself, so no result may exist only inside a running script. A piece counts as done when its file exists in the fixed form and is committed, and not before.
- **The next run starts from the files.** The session reads them and passes the script the list of pieces not yet recorded. This is the same whether the next run is begun by the session that is already going or by a new session after you restart the step. The Workflow tool can also resume a saved run and reuse the results it already has; that is used when the saved run is available, but the plan does not depend on it, because a new session may not have the earlier run's saved record.
- **An agent that returns nothing has not done its piece.** A workflow agent that fails returns an empty result. The scripts treat that as "not done, run it again", never as "found nothing". For an attacker the difference matters most: a failed attacker must never be counted as a round that found no flaw.
- **Counts are printed by scripts that read these files.** The counting scripts are ordinary programs run from the command line by the session, like the four checks already in `docs/working-reset/phase-2/checks/`. No count in the status file, in a notice or in a packet is typed by an agent. Where a script's result has to be copied into a document, the starting session pastes the script's literal output.
- **Anything a run needs from outside the repository is copied in first.** The question checks read today's memory files, which live outside the repository, where no commit records which version a run read. Step 0 copies the ones the plan cites into the progress folder and records the date of the copy.

The progress files live under `docs/working-reset/design-completion/`, one folder per step, beside the plan page.

### 5.3 Step 0: the scripts are the first piece of work

Every stop rule in Part 3 depends on a count, and the scripts that produce those counts do not exist. Neither do the workflow scripts. Building them is work with its own finish condition, and nothing else starts until it is met.

**The counting scripts** print, from the progress files and from canon:

- arguments on the contents list; written; the round each has reached; held; with a second author; failed a second author; parked on an unanswered question; refutations open per argument per round;
- facts found, owned, merged, frozen; second full statements remaining; pointers that do not resolve; promises in canon that land on a placeholder or a stub;
- disagreements between canonical documents: found, decided, remaining;
- want claims listed, owned, unowned;
- open-question and deferral inventory rows by state;
- unfinished-document rows met;
- questions prepared, sent, answered, waiting, and the pieces of work parked behind each;
- hand-walk pieces done and defects per pass;
- lines changed in canon and lines written outside it;
- for the last run of each step: pieces completed, agents that returned nothing, and why the run ended.

**The workflow scripts**, one per row of the table in 5.2, plus the widened and new checks listed in 2.2 item 3.

**Finish condition for step 0.** All of these, each shown by a recorded command and its output:

1. Each counting script is run against a small made-up progress folder whose correct counts are known, and prints them. It is also run against a folder with a missing file, a half-written file and a malformed file, and in each case stops with an error. A counting script that prints zero for a file it could not read would make every stop rule pass falsely.
2. Each workflow script parses, checked with the `new Function` wrapper in `CLAUDE.md`. `node --check` is not used: every workflow script opens with an `export` statement, and on such a file that command reports success whatever the errors.
3. Each workflow script is then run from start to finish under Node with stand-in versions of the functions the Workflow tool supplies, returning prepared results. The prepared results include an agent that returns nothing, an attacker that reports a refutation, a dispute, and a spending limit reached in the middle of a piece. The test passes when the order of agent starts, the pieces recorded, and the reason the run ended are the ones designed. Parsing does not find an error that only appears when the code runs, which is why this test is required.
4. One real piece of each kind of work is run, its result is recorded in the fixed form, the counting scripts read it correctly, and its cost in agent runs and tokens is written to the settings file. These are the first measurements the plan has.
5. One real run is stopped part-way and the next run is shown to start at the first piece not recorded and to repeat nothing that was.
6. A session started with a step's one-line instruction (5.7) is shown to do everything a step needs: run the workflow with agents on the two pinned models, reach the Precept MCP server, run the Python checks and the counting scripts, and commit to the branch. It is also shown to begin a second run by itself after a first run ends at its spending limit, and to stop with a notice after a run that fails.

Two workflow scripts from the old plan's phase 3 are already in the repository and record the same constraint this part is built around: the script has no command line and no file access, so agents do the reading and writing. Step 0 reuses what fits.

### 5.4 What a run may spend, and how the work is divided into runs

The Workflow tool sets two hard limits of its own: at most 1,000 agents in one run, and at most 16 running at once. The attack step alone is larger than one run can hold. Each round of each argument takes two attackers and a classifier, so twelve rounds take at least 36 agent runs per argument before any repair or dispute. At thirty arguments that is 1,080. The work therefore has to be divided, and it is divided by the pieces in 5.2.

A run is given a fixed number of pieces and a token ceiling. Both are settings in one file in the repository, not judgements made during the run. When either is reached, the run finishes the piece in hand if it can, records it, and ends; the next start continues.

| Kind of work | Pieces in one run, to begin with | Agent runs that implies |
|---|---|---|
| Attack | up to 8 arguments, each advanced by up to 3 rounds: 24 pieces | 72 for attackers and classifier, plus one author repair for each round with a refutation and one adjudicator for each dispute; at most 120 |
| Writing the proof model | 1 section | 1 author session, on Fable |
| Merging copies | up to 40 facts on files no other agent in the run touches | about 40, plus the checks |
| Deciding disagreements; internals answers | up to 10 items | 20: one to draft or decide, one to attack |
| Extraction; question checking | every list, or every candidate question, once | one per list or question, and the same again for the check |
| Hand walk | one example through every stage and all six operations | one per stage and operation |

Those piece counts are starting values chosen so that a run that goes wrong wastes little. They are not measurements. Step 0 measures the cost of one piece of each kind, and the token ceiling for a run is then set at one and a half times the measured cost of its pieces. After the first three real runs of a kind, the piece count and ceiling are set again from what those runs used, and the change is written to the log. The piece count is enforced by the script itself. The token ceiling is the Workflow tool's own budget, which refuses to start further agents once it is reached; whether the session can set that budget from the settings file without you typing it is not verified, and if it cannot, the piece count alone bounds a run.

Three attack rounds per argument per run is also what makes the early checkpoint (3.8) fall at the end of a run: the first attack run takes the five named arguments through three rounds and stops.

Runs that touch different documents may be in progress at once. The proof-model line (steps 2 and 4) and the other line (step 3) are started separately for that reason, and the frozen set in 3.1 is what keeps them off each other's documents. You start the two lines in two sessions (5.7). Within a line, one run at a time; a lock file in the progress folder makes a second session started on the same line stop at once and say why.

You said not to ration models for fear of cost and not to leave budget unused. Nothing here holds work back to save usage. The limits above bound what one failure can waste and keep each run inside the tool's own caps; the session begins a new run as soon as the last one ends at its spending limit.

### 5.5 When a usage limit is reached

Fable and Opus draw on weekly limits, and Fable can use at most half of the weekly amount (memory, quoting the Help Center on 2026-07-22; step 0 rechecks the current wording). A step the size of the attack step will meet a limit.

- **The work that needs that model pauses.** The run records the pieces it finished, marks the rest as not done, and writes "paused: usage limit" with the model and, if the tool reports it, when the limit resets.
- **It resumes when you restart the step after the limit resets.** The session writes the notice and stops; it does not sit waiting. You give the same one-line instruction again and the step continues from the saved progress.
- **No weaker model is put in the place of a stronger one to keep going.** Attackers, the classifier, adjudicators, question checkers, the second author and the hand walk stay on Opus 5.5. The proof-model author stays on Fable 5.2. This is not open to an agent's judgement and the scripts name each model explicitly; nothing inherits a session's model.
- **A limit on one model stops only the work that needs it.** If the Fable share is used up, writing pauses and attack rounds continue on arguments already written. If the limit covering all models is reached, everything pauses.
- **You are told once per pause** (5.6), with what is paused and, if the tool reports it, when the limit resets. If you restart before it has reset, the session finds the limit still in force, says so and stops. Waiting is the rule the work follows unless you say otherwise on that notice; your own standing instruction is that waiting for the reset is often right and that a change of model is yours to approve.

How a usage limit shows itself to a running script, and whether it can be told apart from any other failed agent, I have not checked. Step 0's test 3 uses a prepared stand-in for it; the first real pause will show whether the stand-in was right, and until then a run in which several agents in a row return nothing stops and is reported as a failed run.

### 5.6 When you hear from the work

Six conditions, each checked by the session after every run from the counting scripts' output. When one fires the session writes the notice. Whether it then stops depends on whether any work is left that the condition does not touch; the last column says which.

| Condition | What you receive | What you do to continue |
|---|---|---|
| A prepared batch of questions is ready | The batch, one packet per question, with the list of questions dropped and why | Answer. The step keeps running on whatever does not depend on the answers |
| A second author has failed on an argument | The argument, the counterexamples, and the three choices in 3.6 | Choose. Other arguments keep running |
| A count moved the wrong way between runs: disagreements or open refutations rising, open items rising, changes to the hashed proof-model sections, the walk's second pass finding more than its first, more written outside canon than changed inside it | The count before and after, the run that moved it, and whether that line has stopped under 3.8 | Nothing if the line is still running. If it stopped, the notice names the next action |
| A run failed or made no progress: it recorded no piece, or two runs of a step in a row left its counts unchanged, or a request for a ruling stayed open past its round, or step 0's finish condition cannot be met | What failed, the literal error or the unchanged counts, and what is stopped | Restart the step with the same instruction; it continues from the saved progress |
| A usage limit paused work | Which model, what is paused, when the limit resets if the tool reports it | Restart the step with the same instruction after the reset |
| A step finished, or can go no further without you | For a finished step: that it is done, what it produced, the read for your sign-off if it needs one (2.3), and **which steps you can now start**, taken from the table in Part 6. For a step that has run out of work it may do: which answer or sign-off it is waiting for | Start the next step or steps. Or answer, then restart the step |

The last row is how you know to start the next step. Nothing else starts one.

Each notice is one entry in a single notices file, and says at its top whether it needs something from you or is for your information. The session that wrote it also ends by showing it, so you see it where you started the step. Whether a notice can also be pushed to you somewhere else is settled in step 0.

Everything else goes to the status file and nowhere else: the early checkpoint's result, a run ending at its spending limit and the next beginning, a new default recorded. The status file is a few lines per step, rewritten after every run from the counting scripts' output: the counts in 3.2, the last run and why it ended, and what is waiting on you. You are never sent it. If something in it should have reached you and did not, that is a fault in the conditions above and they are corrected.

One limit, stated plainly. The conditions are checked by the session between runs. If the session itself stops without writing anything (the machine sleeps, the terminal is closed, the session hangs inside a run that never returns), no check runs and no notice is written. The only evidence is a status file whose last run is old and a step with no finish notice. Restarting the step with the same instruction is safe in that case, because it continues from the saved progress and the lock file is cleared by a session that finds no run alive behind it.

### 5.7 You start each step

Nothing starts on a schedule. You start a step by giving a session one line, and the same line restarts it.

| To start | You type, in a session opened in the repository |
|---|---|
| Step 0 | `Carry out step 0 of the design-completion plan` — the scripts do not exist yet, so this one is an ordinary instruction that points at section 5.3 |
| Step 1 | `Run design-completion step 1` |
| Steps 2 and 4, the proof-model line | `Run design-completion steps 2 and 4` |
| Step 3 | `Run design-completion step 3` |
| Step 5 | `Run design-completion step 5` |
| Step 6 | `Run design-completion step 6` |

Step 0 builds what makes the other five lines work: a small saved entry point per step that reads the progress files and does the five things in 5.1. Whether that is best built as a saved workflow, a skill or a command is a step 0 choice; what is fixed here is that it is one line, that it is the same line every time, and that it takes no arguments, because where to continue from is read from the files and not told to it.

Steps 2 and 4 are started together because step 4 has nothing to do until step 2 has written an argument, and then has work each time another is written. One session runs both: it writes a section, and the arguments in it enter their attack rounds. Step 3 touches other documents and is started in a second session, which can run at the same moment.

Once started, a step runs until it finishes or a stop condition fires. It may take many runs. What carries it from one run to the next depends on why the run ended:

| Why the run ended | What happens next |
|---|---|
| It reached its spending limit (5.4), and nothing in 5.6 fired | The same session starts the next run. You are not involved |
| A usage limit paused it | Notice; the session stops. You restart with the same line after the reset |
| The run failed or made no progress | Notice; the session stops. You restart with the same line |
| Everything left waits on an answer or a sign-off | Notice; the session stops. You answer, then restart with the same line |
| The step's finish condition is met | Notice naming the steps you can start next |

---

## Part 6 — Order, size, and what reaches you

**What can run beside what.** This follows from the needs in 3.2 and is not a separate decision.

| Step | Cannot start before | Runs beside | Waits on you for |
|---|---|---|---|
| 0 | — | nothing | nothing, unless it fails |
| 1 | step 0 | nothing; its own parts run side by side | nothing |
| 2 | the fixed contents list; your answer to question 1 | steps 3 and 4 | questions 2 and 3, written both ways until answered; question 5 for clock rules |
| 3 | your answer to question 1; the provisional marking | steps 2 and 4 | question 6 for the `integer` part of the overflow model; question 8 |
| 4 | per argument, that argument's section | steps 2 and 3 | an argument that depends on an open question is parked; otherwise nothing unless a second author fails |
| 5 | your sign-off of the proof model | the end of step 3, if any remains | that sign-off |
| 6 | steps 3 and 5 finished; every question answered | nothing | the final reads and the finish sign-off |

**Size.** There is no estimate of how long this takes, and the earlier revision's fifteen weeks is withdrawn. It had no measurement behind it. The old plan's sizes were of the same kind: phase 2 was sized at days and ran four times over (plan:1281). A date also asks for the wrong behaviour, either moving on because the date has come or reporting the work as behind, when the only thing that should move a step forward is its finish condition.

What the plan has in place of a date:

- **The amount of work, counted.** At the end of step 1 the number of arguments is known, and with it the least the attack step can cost: 36 agent runs per argument. The numbers of facts, disagreements, inventory rows and unfinished-document rows are known at the same point. These are written to the status file.
- **The cost of a piece, measured.** Step 0 measures one piece of each kind; the first three runs of each kind refine it.
- **What is left, at any moment.** Pieces remaining, by kind, from the counting scripts. Pieces remaining times measured cost per piece is a statement about usage, and it can be set against the weekly limits to say how many resets the remaining work needs. That is the nearest thing to a forecast the plan will give, it is recomputed after every run, and it is not a commitment.

**What reaches you, as counts.**

| What | How many |
|---|---|
| Sign-offs: replacing the plan (question 1); the proof model after its attack rounds; the finish | 3 |
| Batches of prepared decisions | 2, four decisions in each |
| Opening conversations on language questions inside the proposed extensions | 2 known |
| Later questions from the fresh checks | up to 4 |
| Longer reads: the first packet (numbers, example table, draft refused list, batch 1); your example under the finished model with the refused list; the final summary | 3, each of which goes with a batch or a sign-off |
| Notices under 5.6 | not knowable in advance; each is short, and most need no decision |
| Pages sent whether or not anything needs you | none |
| Starts: one line per step, and the same line again after a pause or a failed run | 6 to begin the steps; restarts not knowable in advance |

Decisions: eight prepared, two opening conversations, up to four more. Defaults are not signed. Each question is prepared before it is asked. The earlier revision put this at 510 minutes of your time; 225 of those were the weekly page, which is gone, and the rest were guesses at reading speed that I have not kept.

**Main risks.** The proof model may not close; the early checkpoint and the per-argument rules are there to show that as soon as the order of work allows. The full proof power you asked for means more arguments than the "couple of dozen" the old plan expected, each owed twelve rounds; the count is known when step 1 fixes the contents list. Paper checks can miss what a machine would catch. Rewriting `proof-engine.md` could lose detail: no line leaves canon without a destination, text about today's build moves to the document's existing implementation-notes section (the project's own convention), and the status file reports lines removed and where they went. Several of the review's fixes add work whose combined effect on each other nobody has tested; the measured cost per piece is the first check on that. And the running arrangement in Part 5 is itself untested: the stop rules are only as good as the counting scripts, which is why step 0 tests them against inputs with known answers, and a session that stops without writing a notice goes unnoticed until you look (5.6).

---

## Part 7 — The first two steps

**Step 0** is described in 5.3. You start it with the line in 5.7. It produces nothing you need to read. You hear from it if it cannot meet its finish condition, and when it finishes, with a notice that step 1 can be started.

**Step 1**, up to the first packet. The parts are listed with what each needs; parts that need nothing from each other run side by side in the same run.

| Part | Needs | By |
|---|---|---|
| One-page plan drafted at the fixed path, not yet committed as live (that is question 1). Checks widened; exceptions files started. | step 0 | 2 agents |
| Language lists extracted from `docs/language/`, with expression positions. | step 0 | 3 agents |
| A second agent checks each list against the prose; hashes recorded. | each list extracted | 1 agent per list |
| Want map, twice, adjudicated. Rulings table from the walk file, `trace-walk-rulings.md`, plan § 3, § 4 and the "work the walk created" list, and today's memory. Open-question and deferral inventories. | step 0 | 4 agents |
| **Every candidate question checked by a fresh agent** given those sources and not this plan's conclusions: already answered? a design question at all? Dropped ones listed in one line each with the settling citation. | the rulings table | 1 agent per question |
| Survivors prepared: text quoted, each option with an example drafted by the `precept-author` agent, what each refuses, the message an author would see. Stale passages in the two editable-write write-ups marked superseded. | each question's check | 2 agents |
| Your example hand-worked under canon as it stands. The guard at want:75 is corrected to test presence, a consequence of the presence rule. The message at want:81 is left as written and worked as a case for the first-match argument. | step 0 | Proof-model author, and a second derivation by another agent |
| Draft refused list by language area. Ownership drafted from script output. What the proof model reaches is marked provisionally; **this marking is what freezes deduplication**, and nothing so marked is merged. | the hand-worked example; the lists extracted so far; the widened checks | 2 agents |
| Hand-walk pilot on the lexer, parser, name-binder and type-checker documents with your example. First merges of plain copies in those same documents. | the hand-worked example; for the merges, the provisional marking | 4–5 agents |

**The first packet** is sent when the question checks, the prepared survivors, the hand-worked example and the draft refused list all exist. That is the first of the conditions in 5.6 firing. It holds:

1. One page, every number on it printed by a script: canon's size; facts found, merged, disagreeing; want claims stated and missing; rulings not yet in canon; inventory sizes; unexplained script results; the measured cost of one piece of each kind of work.
2. Three pages: your bank example as a table, with the corrected guard at want:75 shown; the message at want:81 is shown as written, marked as resting on the first-match argument.
3. Two to three pages: the draft refused list: what the full design still refuses, each item with the principle that excludes it.
4. Batch 1 (four decisions), with the list of questions that were dropped and why.

**Step 1 then continues without waiting for your answers**: the remaining list checks and hashes, and the fixed contents list with the confirmed marking and the argument count. When it finishes, the argument count goes to the status file. When step 1 finishes and question 1 is answered, the notice tells you that the proof-model line and step 3 can be started.

---

## Sources and limits

**Read in full for this revision:** the review; `phase-1/the-walk.md`; `phase-2/trace-walk-rulings.md`; `phase-5/what-discharges-an-editable-write.md`, `must-a-proof-name-what-holds-a-fact-up.md`, `where-a-divisors-non-zeroness-comes-from.md`; `runtime/README.md:34–56`; the specification lines the review cites; `philosophy.md:18–46`; the `claude-api` skill's model table; the two model memory files; for the last pass, the Workflow tool's authoring reference and the want document.

**Read in part:** `editable-write-cascade.md` (§§ 1, 3.1 tail, 6); `editable-write-design-space.md`, the two `maxplaces` notes and the overflow recommendation (headings; the recommendation's summary); its review (verdict, § 3.6, section headings); the canon lines cited for the open-question and code-owned-list findings.

**Not read:** the contradiction register; `cold-read-findings.md`; the period-ordering design; `phase-4/what-a-refusal-claims.md`; the type documents beyond cited lines; `evaluator.md`, `precept-builder.md`, `runtime-api.md` and `result-types.md` beyond cited lines.

**Not verified:** that the bank example is accepted only under the want:143 reading (my reading of want:36, :40, :110, for the fresh check); whether each walk ruling is in canon; the review's line citations I did not open (`result-types.md:119`, `:148`, `:152`; register rows); the token estimate, which is bytes divided by four; the live context limit of a Claude Code session; the number of arguments in the proof model; the cost of any piece of work. Nobody has attempted any part of the proof model, so whether it can be written at all is unknown until the early checkpoint.

**Not verified about how the work runs (Part 5):** whether one session can keep starting run after run of a long step without losing track as its own context fills (step 0's test 6 shows two runs, not fifty); what form the one-line entry point for a step is best built in; whether a new session can use the Workflow tool's resume from an earlier session's saved run; how a usage limit appears to a running script; whether a notice can be pushed to you anywhere besides the notices file and the session; whether the Workflow tool's token budget can be set from the settings file. The Workflow tool's limits quoted in 5.4 (1,000 agents per run, 16 at once, no file access from the script, an empty result from a failed agent) are from its authoring reference as loaded in this session, not from a test. The usage-limit facts are from a memory file dated 2026-07-22. The starting piece counts in 5.4 are my choices, not measurements. Step 0 exists to replace each of these with something observed.

---

## Change list

Numbered findings from the review's summary.

1. Questions before checks — **applied.** Batch moved to end of week 1 after a fresh-agent check against the walk file, `trace-walk-rulings.md`, plan § 3/§ 4 and memory (summary; Part 4 opening; Part 6 days 2–5). I read those files for this revision.
2. Attack budget — **applied.** Twelve rounds per argument, fresh classifier, second author, three choices on equal terms, narrowing via `/design` (3.6).
3. Lists do not exist — **applied.** Extraction is a sized step-1 task from `docs/language/`, with positions and hashes; writer list as design work (3.2 step 1 and note; Part 6).
4. Deduplication not mechanical where copies disagree — **applied.** Two kinds (3.3); result types and update/restore/constraint facts frozen (3.1).
5. Prose left out — **applied.** Concept list with owners and term search (2.2 item 4); summary item 2 reworded.
6. Proof boundary defaulted and shown late — **applied.** Now question 2; draft refused list on the first Friday and after step 4.
7. Default rule contradicts itself — **applied.** One test (3.9); naming, divisor and `from any` no longer defaulted (Part 4 "Not asked").
8. Editable-write framing — **applied.** All four options with what each refuses, the quotes, the lead finding, stale passages marked superseded (question 3; 3.10).
9. Old question 8 — **applied.** Dropped; replaced by the integer model with overflow and divisors (question 8).
10. Clock question and locked text — **applied.** Locked lines quoted, override labelled, philosophy gap raised (question 7).
11. Earlier-row default refuses his example — **applied.** Corrected `ReduceLimit` worked first; now question 4 with that case.
12. Reach marked before the model exists — **applied.** Marked from the hand-working at end of week 2; frozen set listed; later changes logged (3.1).
13. No one settles disputes — **applied.** Adjudicator, precedence, logging, queue (3.5).
14. No reopening rule — **applied.** Hash, re-attack on change, pilot walk in weeks 1–2, two-pass repair budget (3.7; Part 6).
15. Stop and record rules without force — **applied, one part adjusted.** Two lines with separate stops (3.8); exceptions approved by another agent (2.2 item 3); ownership generated from canon (2.2 item 1); fixed path (3.9). The memory pointer cannot be in the same commit because it is outside the repository; it is updated in the same step and checked by script.
16. Estimate without basis — **applied.** Effort and waiting separated, week-1 rates, week-4 checkpoint with criteria, questions 3 and 5 worked both ways (Part 5; 3.8).
17. "One author" and reading list — **applied.** Sessions with hand-over notes writing into canon; `proof-engine.md` and `catalog-system.md` added; type documents driven by the coverage table; write-ups named (3.2 step 2; 3.10).
18. Four-part reasoning and `/design` — **applied.** 3.9, the defaults table, 3.1, question 1.
19. Open-question inventory — **applied.** Built from headings and markers; 21 runtime rows; internals step with its own rule (1.1; 2.1 item 5; 3.4).
20. Deferrals passing as "waits" — **applied.** Three-way classification (2.1 item 6); `Fault` exception carried into the new plan with the wording difference noted (3.4); overflow covers all three types (question 8).
21. Unfinished documents — **applied.** Table in 2.1 item 7.
22. Source files named as owners — **applied.** 1.3; 2.2 item 2.
23. Create, restore, inspection — **applied.** Six operations in the walk (2.2 item 6); contents list additions (2.1 item 3).
24. His reading and decisions understated — **applied.** One total in Part 5 and the summary; counts reconciled.
25. Second unguarded read — **applied.** Both sites, reported not asked (Part 4 "Not asked"; Part 6).

Minor items.

- Git command and record size — **applied** (1.4, with the path filter and 17,749 lines).
- "145 applied corrections"; N105–N119 — **applied** (1.4; 3.1).
- Dead-end question misquotes and omits definitions — **applied** (question 6: five sources quoted, three cases, no reading of the philosophy asserted).
- Term counts — **applied** (1.2 names the nine documents and the method).
- Two scripts already read prose — **applied** (2.2 item 3).
- Spec § 5 and `proof-engine.md` misdescribed — **applied** (1.1).
- Placeholder list — **applied** (1.2 adds `:66`, quotes `:155`).
- Old question 6 re-asks a set policy — **applied** (dropped; 3.9).
- Computed-field item and walk item 32 — **applied** (defaults table; checker briefed with the walk record).
- `ensure` covers half its finding — **applied** (split in "Not asked").
- State-action question omits canon lines — **applied** (question 5).
- Question 1 and walk item 1; counterexample search; tally — **applied** (question 1 table; 2.1 item 3; Part 5).
- Contents list omissions (want:179, :210, :211; safety limits; admissibility; `into`; § 6) — **applied** (2.1 item 3; item 7).
- Proof-set scope taken without asking — **applied** (question 2).
- `from any` default — **applied** ("Not asked").
- Language documents get no internal check — **applied** (2.2 item 5).
- Documents restating the model missing from step 5; `docs/future/` — **applied** (3.2 step 5; 1.1).
- No status means finished — **applied** (2.1 item 8).
- Hand walk on one example — **applied** (2.2 item 6).
- Expected results beyond the bank example — **applied** (2.2 item 7).

"Not covered" items: one session's capacity and model per step are addressed in 3.10, with what was and was not checked. The schedule is re-estimated in Part 5. Whether the proof model can be written at all, and full line-number accuracy, remain uncovered and are listed under "Not verified".

No finding was rejected. The review's refuted items (moving build text within `proof-engine.md`; re-asking the `Fault` deferral) were left as the review left them.


### Last pass, 2026-11-13

**Your answer on proof power, recorded.** Asked whether the design states the first compiler's small proof set or the full intended proof power, you said: "it describes the full proof power, is that not clear from all the forensics and what I want". The question is removed and the answer is recorded in Part 4 with its grounds: want:14, want:171, want:183. Your 2026-07-26 "minimal proof set for the MVP" ruling governs what the first build ships, not what the design states. Everything that leaned on the other reading was rechecked: the refuse-more defaults are gone (3.9); the earlier-row, divisor, state-position and computed-field items are now proof-design work, not questions or defaults (Part 4 "Not asked"); a refusal in the coverage table must cite the principle that excludes the construct (2.1 item 4); the proposed-extensions item no longer asks whether they are in the design (question 9).

1. Attack budget — every argument now gets all twelve rounds; no early pass (3.6).
2. Checkpoint against 3.6 — a failing argument always goes to the second author first; nothing reaches you from the checkpoint itself (3.8).
3. Your response time inside an agent gate — removed from the pass criteria; unanswered questions are reported on a separate line (3.8).
4. want:81 corrected before its question — the earlier-row question is dropped as proof-design work under your answer; want:75 is corrected, want:81 stands as written and is changed only if the first-match argument fails its rounds (Part 4 "Not asked"; Part 6; Friday item 2).
5. Overflow types — one statement: integer, `decimal`, temporal and the approximate number type, with the canon disagreement named (1.2; 2.1 item 7; question 6).
6. `catalog-system.md:1509` — no longer a default; it is question 8 (3.4; Part 4).
7. "Coverage comes from the catalogs" — the two senses are separated (2.1 item 3).
8. Schedule — one set of week ranges in the summary, 3.2 and Part 5; list extraction runs days 1–5 with its check and hashes in week 2; the provisional marking on days 3–5 is what freezes deduplication, and the end-of-week-2 marking confirms it (3.1; 3.2; Part 5; Part 6).
9. Your load — the table is summed (510 minutes, about eight and a half hours); three sign-offs, the same in 2.3, Part 5 and the summary; defaults are overturned weekly, not signed.
10. Dead-end question — `graph-analyzer.md:457` and `philosophy.md:38` quoted (question 4).

Earlier entries in this change list describe revision 2 before this pass; where they name question numbers or the default test, this section governs.


### Running without a calendar, 2026-11-13

**This entry governs over every earlier entry in this change list, and over the "Last pass" section above, wherever they mention weeks, days, Fridays, the weekly page, the week-4 checkpoint, the fifteen-week figure or the 510-minute total.** Those entries are left as written because they record what was done at the time. Their references to "Part 5" and "Part 6" mean what are now Parts 6 and 7.

**Why.** You asked whether the plan was built for long stretches of agent work with your input only when needed, and why it was based on arbitrary days and weeks. It was only partly built that way. It limited what reached you, but it was organised around dates that were agent guesses, it never said what starts a step or what a run may spend, its stop rules relied on scripts nobody had planned to write, and it sent you a page every week whether or not anything needed you.

**What changed.**

1. **The calendar is removed.** Every step in 3.2 now states what it needs before it can start, what it produces, the condition that finishes it, and the counts that measure it. Week numbers, the day-by-day table, the fifteen-week figure and the minutes total are gone (summary; 3.1; 3.2; 3.8; Part 4 headings; Part 6; Part 7). Your total is given as counts of sign-offs, batches, conversations and reads.
2. **Step 0 is added.** The counting scripts and the workflow scripts are built and tested first, each run against prepared inputs and not just parsed (3.2; 5.3). The steps are now numbered 0 to 6.
3. **Part 5 is new: how the work runs.** Workflow scripts that run to a finish condition, a stop condition or a spending limit; progress saved to files after every piece of work; what one run may spend and how each kind of work is divided into runs; what happens at a usage limit; runs started on a schedule.
4. **You hear from the work on six conditions only** (5.6). The weekly page is removed. The status file replaces it and is never sent. Defaults are kept in one list; those a caller can observe are shown with the next batch or sign-off (2.3; 3.4).
5. **The week-4 checkpoint becomes the early checkpoint**, reached when five named arguments have each completed three rounds; those five are written and attacked first (3.2 steps 2 and 4; 3.8; 5.4). Its criteria are unchanged.
6. **"Falls each week" becomes "falls from one run to the next"** in the stop rules, and the record rule compares lines outside and inside canon over a step's runs (3.8; 3.9).
7. **The orchestrating session is replaced** by a starting session with five fixed actions and by the round script, which starts adjudicators itself (3.5; 5.1).
8. **One row added to the model table**, for the scripts in step 0. Every other model assignment is unchanged (3.10).
9. Old Part 5 (schedule and total) is now Part 6; old Part 6 (the first week) is now Part 7 (the first two steps).

**What did not change.** The scope of the design, the level of detail, the inclusion of the internals documents, the rule of one full statement with pointers elsewhere, the full intended proof power, the order of precedence in 3.5, twelve attack rounds per argument with no early pass, the flaw classifier, the second author, the fresh check of every question, the three sign-offs, and the questions themselves. No question was added.

**Ordering points that the dates had been hiding.** Taking the weeks out left five places where the order had been set by a week number and by nothing else. Each is now stated as a need in 3.2; each is a choice made in this pass and is listed here so you can overrule it.

1. *What may run before you answer question 1.* Steps 0 and 1 do, as before. Steps 2 and 3 now wait for the answer, because they are where the new plan starts rewriting canon at scale. The earlier revision implied this by putting the batch before week 3 and never said it. Step 1's pilot merges in four documents still happen before your answer, as they did.
2. *When batch 2 is sent.* It was "week 3", which only spaced it from batch 1. It is now sent when its packets are prepared and batch 1 has been answered.
3. *Arguments that depend on an open question.* The earlier revision had them written both ways and said nothing about attacking them. They are now parked until the answer arrives and not attacked in two versions. This means an unanswered question 2 or 3 holds those arguments, and the proof-model sign-off behind them, for as long as it is unanswered. The status file shows the count.
4. *Preview and inspect.* What they show as proof provenance depends on a proof-model section, so that row of step 3 cannot finish until step 2 has written it. The week ranges had step 3 and step 4 ending together and did not show this.
5. *The early checkpoint.* With no week to fall in, it needed an event. It is tied to five named arguments reaching round three, and those are moved to the front of steps 2 and 4.


### You start each step, 2026-11-13

**This entry governs over "Running without a calendar" above wherever they conflict**, which is wherever that entry says runs start on a schedule or that the work runs without you starting it.

**Your ruling.** No scheduled runs. You start each step yourself.

**What changed.**

1. **Scheduled starting is removed** from the summary, 3.2 step 0, 5.1, 5.2, 5.4, 5.5, the old 5.7, Part 6 and "Sources and limits". The old 5.7 is replaced by "You start each step": one line per step, the same line to restart, no arguments.
2. **How a step continues across runs** (5.1; 5.7). The session that started the step begins the next run by itself when a run ended only at its per-run spending limit. After a usage-limit pause or a failed run it writes a notice and stops, and you restart with the same line, which continues from the saved progress. This is the arrangement you were offered as the preferred one, taken as given.
3. **A finished step always produces a notice** saying it is done, what it produced, and which steps you can start next from the table in Part 6 (5.6). Before, only a step needing a sign-off reached you. A column was added to the 5.6 table saying what you do to continue in each case.
4. **Step 0's test 6** now shows that a session started with a step's line can run the workflow on the pinned models, reach the Precept MCP server, run the Python checks and commit to the branch, begin a second run by itself, and stop with a notice after a failed run (5.3).
5. **The risk that a schedule stops firing is removed.** One risk of the same kind remains and is stated in 5.6 and in the main risks: a session that stops without writing anything produces no notice.

**Two things added that the instruction did not spell out**, both needed to make "you start each step" complete:

- *Steps 2 and 4 are started with one line and run in one session.* Step 4 has nothing to do until step 2 writes an argument and then has work each time another is written, so starting it separately would mean a session that mostly waits or that you restart after every section. Step 3 is a second session. The steps, their needs and their finish conditions in 3.2 are unchanged.
- *A step that has run out of work it may do stops with a notice naming the answer or sign-off it waits for*, and you restart it after answering. It is folded into the sixth condition, so there are still six.

Nothing else changed: scope, rulings, models, questions, and the five ordering choices listed in the previous entry, which still await your ruling.

