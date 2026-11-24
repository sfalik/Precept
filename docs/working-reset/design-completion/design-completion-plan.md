# Design completion plan

**Status: proposed.** It becomes the live plan when Shane says yes and step 0 makes its first commit, which
places this file at `docs/working-reset/design-completion-plan.md` and marks
`docs/working-reset/compiler-readiness-plan.md` superseded at its head in the same commit.

Written 2026-11-22 against branch `spike/Precept-V2-Radical-reset` at `2f126a3b`. Nothing in the repository
was edited to write it.

**This page holds no live numbers.** Every count that changes as the work proceeds is printed by a script
into a generated status file. Numbers quoted below are measurements taken from the input files on
2026-11-22, each with its source.

**This page uses no lookup codes.** Conflicts and rulings are described in words. The codes the input
files use for them, and the row numbers of the rulings file, are kept in the working files that scripts
read (`technical/conflicts.jsonl`, `rulings.jsonl`) and in the routes file listed below.

## Short names for sources

| Short name | File |
|---|---|
| want | `docs/Working/what-i-want-2026-07-16.md` |
| spec | `docs/language/precept-language-spec.md` |
| old | `docs/working-reset/compiler-readiness-plan.md` |
| req | `inputs/requirements.md` |
| failures | `inputs/history-failures.md` |
| rulings | `inputs/history-rulings.md` |
| routes | `inputs/rulings-routes.md` |
| proof-model | `inputs/technical-proof-model.md` |
| precedent | `inputs/technical-precedent.md` |
| consistency | `inputs/technical-canon-consistency.md` |
| v2 | `inputs/design-completion-plan-v2.md` |
| review | `inputs/plan-vs-old-review.md` |

`inputs/` means `docs/working-reset/design-completion/inputs/`. Step 0 commits those files there, because
`cite-check` (§ 4.1) can only open committed text. Until it does, they sit in the session scratchpad: the
files written for this plan in its `v3/` folder, and v2 and review one folder above it.

---

## 1. The goal and the five rules

The goal, in Shane's words: "A completed and consistent design first, then acceptance criteria, tests,
code reviews" (rulings:40). The plan ends when the design is finished. It writes no product code.

His five rules, which every part of this plan is checked against:

1. **Design first.** Compiler behaviour, test results and the sample files never settle a design question
   (rulings:40, :147).
2. **All detail stays**, compiler and runtime internals included (rulings:41).
3. **One full statement, deduplicate.** One document owns each fact, the others point to it, nothing is
   dropped (rulings:42).
4. **The design states the full intended proof power** (rulings:43; want:14, want:171, want:183). What the
   first build ships is a separate matter. Something hard to prove is an open design problem with an owner
   step here. It is never an accepted boundary.
5. **Much less of his time.** Long unattended runs on the strongest models. He starts each step with one
   line. Nothing runs on a schedule. § 6.5 counts every time this plan reaches him.

### What finished means

Seven conditions. Each is a count printed by a script from a named file, and each must be zero.

| # | Condition | List file | Script |
|---|---|---|---|
| 1 | Every item on the contents list (§ 2.1) is written in its owning canonical document, and every argument it rests on has passed its attack rounds | `technical/contents.jsonl`, `technical/arguments.jsonl` | `status --finish` |
| 2 | Every hard problem (§ 2.2) is closed by a design, or by a refusal that meets § 4.3 and has itself passed its attack rounds | `technical/hard-problems.jsonl`, `refusals.jsonl` | `refusal-check` |
| 3 | Every canon conflict (§ 2.3) is closed by a committed edit | `technical/conflicts.jsonl` | `status --finish` |
| 4 | Every ruling of his, including every answer he gives during this work, is stated in canon with the line recorded, or is carried by a named mechanism, or is retired on his own words (§ 5) | `rulings.jsonl` | `rulings-check` |
| 5 | Each fact has one owning statement and every other mention is a pointer that resolves. This has a part a script can see and a part it cannot, counted separately (below) | `ownership.jsonl`, `dedup/pairs.jsonl` | `pointer-check`, the four widened checks, `status --finish` |
| 6 | Nothing open is left in the design: no open finding, no parked item, and no open question in canon | `findings.jsonl`, `parked.jsonl`, `open-questions.jsonl` | `status --finish` |
| 7 | Everything passed on has an owner and is of a kind that may be passed on: every row handed to the build and every deferral | `handoff.jsonl`, `deferrals.jsonl` | `status --finish` |

**Condition 5 in two parts.** A script can check that a pointer has the fixed form and that it resolves,
and the four checks from phase 2 (`docs/working-reset/phase-2/checks/`) compare declared types, stated
counts, identifiers and stage attribution. None of them can find a fact restated in different words. That
part is an agent pass over a closed list: `dedup/pairs.jsonl` holds one row for every pair of a canonical
document and a fact on the ownership list, generated by script. One agent reads one whole document and
fills that document's rows with one of three values: owns the full statement, points to it, or does not
mention it. A second agent re-reads every row marked "does not mention" for a sample of documents set in
`settings.json`. The script counts rows not filled, and rows where a document other than the owner states
the fact in full. Both must be zero. A zero from the scripts alone does not meet the condition.

**Conditions 6 and 7.** A finding or an open question in canon must be closed at the finish; having an
owner is not enough. `status` fails, on every run and not only at the finish, any open row in any of the
five files whose owner step has already finished. A row of `handoff.jsonl` must be one of four kinds: an
acceptance criterion, code to be written against a finished design, the choice of what the first build
includes, or a process rule for the build stage. A row of any other kind fails, so unfinished design
cannot be handed to the build. A row of `deferrals.jsonl` must be in one of the four groups of § 4.4. The
only exception to conditions 6 and 7 is a row that cites his quoted words in `answers.md` or
`rulings.jsonl` saying that the item waits. One is known: who constructs a `Fault` (§ 5).

All file paths in this plan that do not start with `docs/` are under
`docs/working-reset/design-completion/`.

---

## 2. The technical track

Shane asked that the work include "a technical track, that looks at what I want from a technical
perspective". This section is that track. It is a standing line of work: steps 1 to 13 in § 3 are its
steps, and it owns five files that the rest of the plan reads.

| File | Holds |
|---|---|
| `technical/contents.jsonl` | Everything the finished design must contain, each item with its owning canonical document and its step |
| `technical/hard-problems.jsonl` | The fifteen hard problems, each with its one-page engineering write-up, its design, and its arguments |
| `technical/conflicts.jsonl` | Every place canon disagrees with the want document or with itself, each classified, given an owner step, and carrying the code its source file uses for it |
| `technical/dependencies.jsonl` | Which arguments and canonical sections each argument rests on. Authors do not write this file (§ 4.2) |
| `questions/<id>/technical.md` | For every question put to Shane: what each answer does to the proof model, with a worked case derived from canon |

The single most important finding behind this track: canon states the promise as an induction in one
paragraph (spec:268), and the document where the mechanism has to live describes a different machine. No
obligation says "this handler must leave this authored rule holding", and none says "this initial event
must make this rule true" (proof-model:33-37; `docs/compiler/proof-engine.md:2614`). Most of what follows
traces back to that.

### 2.1 What the finished design contains

Eighteen items. "State today" is the reading in the proof-model file, where Full means one document
states it completely enough to build and test against, Partial means something a builder needs is missing,
Contradicted means two statements disagree, and Silent means nothing states it (proof-model:24-29). The
owning document is a proposal. Two agents draft the contents list independently in step 1. A third agent
compares the two drafts item by item, and every item that appears in only one draft gets a recorded
disposition: added to the list, merged into a named item, or dropped with a citation. Nothing relies on
the list until `status` prints zero unmatched items without a disposition (old:1510-1517).

| # | Content | Proposed owner | State today | Step |
|---|---|---|---|---|
| 1 | **The system the induction runs over.** A configuration; each operation; the eight-step order inside one; the closed list of everything that can write a field; what a handler is, including several state actions on one state, entry actions at construction and on a self-transition, and `from any` | spec (execution order); `catalog-system.md` for the writer entries | Order Full; writer list Partial, five of eight uncatalogued (spec:1999); three points open (proof-model:77-80) | 1 |
| 2 | **What counts as a rule.** One table turning every constraint spelling into the statement that must hold and where: unguarded and guarded rules, every modifier on every type including on optional fields, the four `ensure` anchors, event ensures, element-type modifiers, quantified rules, bounds on computed fields, `maxplaces` | spec | Modifiers and guarded rules Full; ensure anchors Partial; quantified rules and `maxplaces` Contradicted (proof-model:99-104) | 3 |
| 3 | **The base case.** The default configuration; every initial event per rule; the default an initial event overwrites; construction with no initial event; guarded rules established, not skipped; the value of a field with no default | `soundness-and-coverage.md`, mechanism in `proof-engine.md` | Statement Full; mechanism Contradicted inside the engine document (proof-model:116-123) | 4 |
| 4 | **The inductive step.** Which rule and handler pairs need work; every write site of every field a rule mentions; a change of state as a write; a handler's effect as a substitution for every action kind; rows that write nothing; editable writes | `soundness-and-coverage.md`, `proof-engine.md` | Sentence Full; mechanism Silent (proof-model:136) | 4 |
| 5 | **What a proof may rely on.** The complete set of premises at a site and the statement that nothing else may be used; a decision on facts the compiler infers that the author never declared | `soundness-and-coverage.md` | Partial (proof-model:144-157) | 4 |
| 6 | **Fault obligations at every position where an expression is evaluated**, guards and rule conditions at ingress and dispatch included; the early stop of `and` and `or` as a premise rule | `soundness-and-coverage.md` | Commitments Full; sites Partial (proof-model:166-169) | 4 |
| 7 | **What each numeric type requires**: `integer`, `decimal` with its rounding, `maxplaces`, `number`, including a product of two non-zero `number` values that rounds to zero | `primitive-types.md` for meaning; the proof documents for facts | Partial, Silent and Contradicted by type (proof-model:173-179) | 2, 5 |
| 8 | **Temporal, quantity and money**, the qualifier layer, qualifiers that come from event data, and units whose conversion the exact model cannot hold | `temporal-type-system.md`, `business-domain-types.md` | Partial; Silent on rules preserved through period arithmetic (proof-model:183-187) | 2, 5 |
| 9 | **Strings, choices, optional values, collections, computed fields**; string equality, case-insensitive equality and prefix and suffix tests as proof facts; presence as a fact that actions create and destroy; quantified rules proved for all sizes | the type documents; `collection-types.md` | Mixed; string facts other than length Silent (proof-model:191); quantified rules Contradicted, their proof Silent (proof-model:191-203) | 3, 5 |
| 10 | **Guards, linear and non-linear relational rules, transitions, stateless definitions** | `soundness-and-coverage.md`; spec for stateless | Linear Contradicted against the want document; non-linear Partial; stateless Contradicted (proof-model:214-220) | 4, 6 |
| 11 | **Ingress and editable fields**: argument ingress, what an edit evaluates, in what order, in which states | spec; `evaluator.md` | Arguments Full; edits Silent to Contradicted (proof-model:225-230) | 4 |
| 12 | **Discharge, verdicts and explanations**: the inference steps; whether each class always reaches an answer; three verdicts; the counterexample; the missing premise | `soundness-and-coverage.md`; `diagnostic-system.md` | Verdicts Full; strategy set, counterexample and missing premise Partial (proof-model:242-246) | 6, 7, 8 |
| 13 | **Graph structure**, with dead rows feeding reachability, and rows that can never fire because earlier rows always match first | `graph-analyzer.md` | Reachability Full; feeding dead rows back is at odds with the stage order; rows hidden behind earlier rows Silent (proof-model:253-257) | 9 |
| 14 | **Certificates and the checker**: the record; the step vocabulary; the checker working out for itself what a definition owes; what ties a certificate to a definition; how it reaches the runtime; the compiler stage; the load check and its failure; the runtime checks derived from premises; what the checker trusts; whether graph verdicts are in the certificate | `soundness-and-coverage.md`; `precept-builder.md`; `runtime-api.md` | Commitment Full; format Partial; vocabulary Silent; route to the runtime Contradicted (proof-model:272-279) | 10 |
| 15 | **What the runtime does and does not do**, and what tooling shows: inspection with where a proof came from, what a refusal carries, the clock at run time, the three verdicts in tooling | `docs/runtime/`, `docs/tooling/` | Mostly Full; inspection, refusal contents and tooling open (proof-model:285-304; consistency:200-219) | 12 |
| 16 | **The induction argument itself**, naming state residency, computed fields recomputed every operation, `omit` resets, entry and exit chains and first-match rows; the statement that "every reachable configuration" is delivered by proving the rule set inductive; and, as one argument with the rule induction, that removing dead rows and dropping obligations on rows that leave unreachable states is sound | `soundness-and-coverage.md`; the guarantee statement in spec | No argument exists (failures:80-81; old:1716-1718) | 11 |
| 17 | **The overflow model** for `integer`, `decimal`, the temporal types and `number` | `primitive-types.md`, `temporal-type-system.md` | Target stated, model undecided (proof-model:351; consistency:170) | 2 |
| 18 | **Catalog entries and diagnostics** for obligation kinds, premise kinds, certificate steps and writers | `catalog-system.md`, `diagnostic-system.md` | Absent (proof-model:442; `CLAUDE.md` catalog notice) | 12 |

The list stays open until step 6 writes the inference steps, and closes then with a recorded hash
(req:583-585). Step 6 may not close it until step 9 has recorded the proof steps the graph analysis needs
(§ 3, step 9). That closing point stands on its reasoning, which is that the steps a proof may take
cannot be listed completely before the procedure is written (old:1510-1517). The old plan's label calling it
Shane's ruling has no quotation behind it (rulings:225).

### 2.2 The hard problems

Fifteen. Each is a row in `technical/hard-problems.jsonl` with its own owner step. The first piece of
work on each is a one-page engineering write-up: the problem, the candidate techniques, and what each
costs. Then a design, then its arguments through the attack rounds in § 4.2. A hard problem closes only
with a design, unless a refusal meets § 4.3.

| Problem | What the design must decide | Sources | Step |
|---|---|---|---|
| Combining several declared facts in one proof | Whether linear obligations are decided completely, with the multipliers of a linear combination as the certificate; strict inequalities, which need their own form of that record; the rounding steps integers need; case splits. Deciding linear integer obligations completely has no known polynomial cost bound (precedent:92), and § 4.3 allows a cost refusal only with a written bound, so step 7 must settle what happens to an integer obligation the procedure cannot finish within its cap, and say which of the two it is: an unresolved verdict at a stated cap, or a refusal on cost. The want document's own `Deposit` and `OpenAccount` need combined facts, and canon's wording refuses them (spec:256) | proof-model:312-324; precedent:79-93 | 6, 7 |
| Non-linear rules | What "states the condition directly" means exactly; the set of permitted rewrites, each true of the arithmetic the evaluator performs; products that are linear in disguise. His approved text binds here: a non-linear rule "is proven only when" a guard or an argument constraint states the post-state condition directly (want:135; spec:270). So sign and monotonicity facts may be used only inside a permitted rewrite that shows an author's stated premise is that condition. They may not prove a non-linear rule from field bounds alone. If the write-up finds a rule that field bounds would prove and this wording rejects, that case goes to Shane as a question (§ 6.2), because rule 4 and his approved text then pull different ways and no agent may choose | proof-model:326-332; precedent:151-168 | 6 |
| The evaluator's arithmetic is not the arithmetic of proofs | One written model per type, shared by the evaluator, the prover and the checker; how exactness is shown or an error bound carried; what is provable about `number`. No verifier was found that reasons about base-10 rounding (precedent:179), so this starts with a dedicated pass through `/research` | proof-model:334-347; precedent:170-189 | 2 |
| Overflow | The model per type. The want document's `Deposit` adds two `decimal` values with no upper bound declared on either (want:34, want:52-55) and is called provable (want:101). That is evidence about the `decimal` model only: if every operation must be proved to stay inside a fixed range, `Deposit` is rejected (proof-model:353). The want document has no comparable `integer` example | proof-model:349-362 | 2 |
| A product of two non-zero `number` values can be exactly zero | Canon records this as "an open question, not yet routed" (`docs/language/primitive-types.md:667`). It breaks the proof that a product is a safe divisor. The design must say what a proof may conclude about a `number` product and what is rejected | proof-model:179, :362; precedent:149 | 2, 6 |
| Units whose conversion the exact model cannot hold | Canon puts logarithmic units such as `dB` and `[pH]` "outside that proof guarantee" and states no outcome (`docs/compiler/proof-engine.md:109`; spec:1456). Under want:171 an unproven case is rejected. The design must say what is proved about a quantity in such a unit, or that a rule over one is rejected and with what message | consistency:163 | 2, 5 |
| Rules over the elements of a collection | A preservation table with one row for each collection kind and each action that kind has, against every predicate form: `each`, `any`, `no`, membership, key presence, `.min` and `.max`, and ordering in the priority-ordered kinds (precedent:215; proof-model:369). Existential rules when an element is removed; element rules that mention other fields | proof-model:364-376; precedent:203-218 | 3, 5 |
| Temporal arithmetic | The list of calendar properties a proof may use; time-zone conversion usable only as a function; range | proof-model:378-386; precedent:191-201 | 2, 5 |
| Scale | Which pairs are skipped; how `from any` counts; a written cost bound for each proof step; where any cap sits. A cap is allowed only under § 4.3: written as a number or a formula over a definition's declared sizes, with an attacked argument, from canon alone, of which definitions stay under it. Sample files are not evidence for it | proof-model:388-396; old:1723-1731 | 7 |
| Rows are tried in order | The negations of earlier rows' guards as premises, by splitting into cases | proof-model:398-400 | 4, 6 |
| The check at an editable write | Three ways to close it were put to him before and he has not chosen (old:282): prove the rule from the field's own declared bounds and the guard on editable access, refusing where that cannot be done; a check the engine runs at the write over the configuration the edit would produce (the reading of want:143); or refuse the `editable` declaration. The write-up covers all three: what each proves, what each refuses, and how each stands beside want:171 and want:199. For the second, the wording must not contradict "nothing is evaluated after a mutation" | proof-model:402-404 | 4 |
| What the checker is trusted for | The checker recomputes what a definition owes from the definition itself; what it reads from the catalog; its own arithmetic exact | proof-model:406-408; precedent:127, :133-137 | 10 |
| Producing the missing premise | The smallest suggestion that does not contradict the rules; a guard against an argument constraint; "add a rule" when the rules are not inductive; counterexample values a field could really hold, for arguments too | proof-model:410-412; precedent:95-112 | 8 |
| Rules that are true but not inductive | How the diagnostic tells this apart from a missing guard; presenting the counterexample as "a configuration your rules allow" | precedent:61-69, :254 | 8, 11 |
| Reachability to the standard he wrote | "Every state is reachable ... A state no path reaches is rejected" (want:151). Canon treats every row as one that can fire (spec:191), so a state entered only through rows that never fire is accepted today. Exact reachability over unbounded integers cannot be decided in general (precedent:226). The design standard is the same as everywhere else: the compiler shows each state reachable or rejects the definition, naming what would show it. One candidate is to exhibit a run from creation that enters the state, which a checker can replay. The opposite reading, accepting any state that cannot be shown unreachable, is not adopted by a design step. If the write-up shows the standard cannot be met without rejecting definitions an author would reasonably write, that goes to Shane with a worked case (§ 6.2). For "no dead ends" the compiler must likewise exhibit an input that satisfies some exit row's guard; an undecided case is never treated as "can be left" (precedent:228) | precedent:224-228; proof-model:256 | 9 |

Two limits are facts of mathematics and not of effort: exact reachability over unbounded integers, and
general non-linear integer arithmetic. In both, the response is the want document's own: prove from
declared premises or reject, and let the author state more (precedent:264). For reachability that means
rejecting a state that cannot be shown reachable. It does not mean accepting one that cannot be shown
unreachable.

### 2.3 The canon conflicts

The consistency file counts 58 open: 35 mechanical, 19 needing a design argument, 4 needing Shane first
(consistency:225-232). The proof-model file lists sixteen contradictions, most of them the same items seen
from the proof side (proof-model:452-472). Step 0 copies every one into `technical/conflicts.jsonl` as its own
row, with the code its source file gives it, so an agent can look it up. The nineteen design arguments
hang off four pieces of design (consistency:236) and are scheduled as those four.

**Settled by a piece of design.**

| Conflict | Step |
|---|---|
| The induction is stated and has no mechanism: no obligation kind, no generation rule, no diagnostic. The base case ignores initial-event arguments, passes an unknown result, skips guarded ensures and leaves out `to` ensures of the initial state. Only unconditional field-to-field rules count as premises | 4, argued in 11 |
| Stateless handlers are said to have no guard, and the base case is skipped for them | 1, 4 |
| `maxplaces` is rule shorthand in one document and run-time enforcement in another | 3, 5 |
| A quantified rule is called "a runtime governance rule" | 3, 5 |
| Event ensures: evaluated at ingress in one place, "proven rather than re-evaluated" in another, a fifth premise class in a third; how the three state forms of `ensure` enter the induction | 3, 4 |
| What an editable write evaluates. About twenty sentences still state the narrow answer | his answer, then 4; the sentences in 12 |
| Combining two declared facts is excluded | 6 |
| The limits of proof are written as permanent: "non-extensible", "sufficient", "out of scope", an exclusion of general dataflow that cannot stand beside the induction paragraph, and logarithmic units placed outside the guarantee with no outcome stated (`proof-engine.md:109`, `:139`, `:2500`, `:2537-2541`, `:2597-2606`, `:2618`; `compiler-and-runtime-design.md:165`; spec:256, spec:1456; `soundness-and-coverage.md:271-296`) | 6; the units in 2 and 5 |
| Overflow has an exemption and no design; nothing says what the runtime does if an expression overflows | 2 |
| The want document's own guard reads an optional field with no presence test (want:75) | 2; shown to Shane, not asked |
| A rule that mentions the current time is shown as normal usage; where the clock comes from at run time | his answer, then 3; the clock in 12 |
| The dead-row check never reads relational rules, and reachability ignores guards | 9 |
| A dead end has four definitions; a graph diagnostic the graph document never mentions | 9 |
| Strategy numbering differs between the enum and the headings | 7 |
| Computing the missing premise, and nothing forbids tooling from inserting it | 8; tooling in 12 |
| The certificate has a contract and no format; `Compilation` has no place for it; no checker stage; nothing ties a certificate to its definition; the load refusal is undecided; "nothing from the proof ledger crosses into runtime" | 10 |
| The premise list against the constraint index built in parallel; two shapes of `ConstraintDescriptor` | 10 |
| What inspection reports, and what a run-time refusal carries | 12 |
| No row matched at construction is merged with a refusal | 12 |
| Tooling says nothing about verdicts, certificates or where a proof came from | 12 |

**Mechanical.** The want document or a ruling already says which side is right, and the fix is an edit.
These run in step 14 unless the fact is on the frozen list (§ 4.6), in which case step 12 carries them.
A script reads the frozen list and decides which; no agent does. They are: stateless construction still
"checks global rules"; an exit ensure "will be checked"; three leftovers in the architecture document;
the glossary; the "justification mechanism", which moves to rejected alternatives in step 6 because it
sits in the proof engine document; two sentences saying the spec describes the build; project vocabulary
inside canon; the planned MCP tool names; the order of an edit's inspection; who handles data written
under an older definition; twelve of the fifteen rows from the four mechanical checks, two of them after
one small choice; thirteen of the fourteen findings from the cold reads; and two stale entries in the
phase 2 register, one for period ordering and one a classification (consistency:25; review:74).

Phase 2's executed register rows are settled input. They rank below Shane's quoted words and the want
document. Their recorded search patterns are re-run after every run that writes canon, and an agent reads
each pattern that fails, because two patterns broke under correct later edits (old:978-994).

### 2.4 The order

From the proof-model file's Part 4 (proof-model:416-446). A step may not start a piece whose prerequisite
has not passed its attack rounds, with the one exception written into step 4. `next-run` applies this
table; it is not left to a session to read.

| Step | Designs | Needs passed | Waits on Shane for |
|---|---|---|---|
| 1 | What an operation is | nothing | state actions, for the sub-items written both ways; `from any` only if its check leaves it open |
| 2 | What every operator means on every type, rounding and overflow included | nothing | the overflow model for `integer` and for `decimal`; the conversation on bounds for temporal point types; the decimal rounding model only if the research shows a choice an author can see |
| 3 | The table of rules | 2 | the clock, for clock rules only; event ensures only if the check leaves it open; the conversation on quantifier binding |
| 4 | The obligation model and the ingress contract, as one piece | 1, 3 | the editable-write answer; event ensures only if the check leaves it open |
| 5 | The facts each type contributes and how each action changes them | 2, 4 | the two conversations, for the pieces they touch |
| 6 | The inference steps, which are also the certificate's vocabulary | 5; step 9's list of the proof steps it needs | the non-linear case only if the write-up finds one |
| 7 | The procedures that find proofs, with limits and cost bounds | 6 | nothing |
| 8 | Verdicts, the counterexample, the missing premise | 7 | nothing |
| 9 | Graph structure with dead rows | 4, 7 | the narrow dead-end case and the reachability standard, each only if its check or write-up leaves it open |
| 10 | The certificate and the checker | 4, 6; 9 for the piece on graph verdicts | whether a certificate names what establishes each fact, only if the checks leave it open |
| 11 | The induction argument | 1, 4, 5, 6; 9 for the piece on dead rows and unreachable states | nothing |
| 12 | Catalog entries, diagnostics, runtime and tooling | 1 to 11, and his sign-off of the proof model | that sign-off |
| 13 | Acceptance criteria handed to the next stage | 3 to 11 | nothing |

Steps 1 and 2 are written side by side. Step 3 follows step 2. After step 6 passes, step 7 and the
pieces of steps 10 and 11 that do not need step 9 run side by side; step 9 follows step 7, and the
remaining pieces of steps 10 and 11 follow step 9. Step 6 is the centre: most of what canon is silent on
is decided there (proof-model:446).

Every wait in the last column follows one rule: until he answers, the sub-item is written once for each
option, behind pending markers, and its arguments are attacked in each form. So an unanswered question
holds back only the step's finish, never the work that can be done without it.

---

## 3. The steps

Sixteen steps, numbered 0 to 15. Each states what it needs, what it produces, the condition that finishes
it, and the counts that measure it. Every finish condition is "this script prints zero for this file".
No step has a date, a length or a size estimate.

Steps 1 to 11 share one way of working, stated once here:

1. **Lists first.** The step extracts the language lists it will walk from `docs/language/`, never from
   the catalogs. A second agent checks each list against the prose and its hash is recorded in `lists/`.
2. **One page per hard problem**, before any design text (§ 2.2).
3. **One author writes the design text straight into the owning canonical document**, one section per
   sitting, behind a fixed pending marker. Each sitting ends with a short hand-over note. The step fills
   the placeholders in the sections of `soundness-and-coverage.md` it owns; no other step writes there.
4. **Every argument goes through the attack rounds** in § 4.2. When it passes, its marker comes off.
5. **The step closes the conflicts § 2.3 gives it** with edits, and moves each ruling § 5 routes to it
   to a row that records the canonical line and a quoted fragment.
6. **It adds its acceptance criteria** to `handoff.jsonl`.

The shared finish condition for steps 1 to 11, called "the step's rows are closed" below:
`status --step N` prints zero for each of: sections of the step's part of `technical/contents.jsonl` not
written; arguments in `technical/arguments.jsonl` not passed; rows of `technical/hard-problems.jsonl` owned by
the step with no passed design; rows of `technical/conflicts.jsonl` owned by the step still open; rows of
`rulings.jsonl` routed to the step with no canonical line; open rows in `findings.jsonl` against the step's
sections. The shared counts are those six, plus arguments by round reached, arguments with a second
author, rows found and not yet worked, rows reopened by a dependency and rows reopened otherwise, and
lines changed in canon against lines of record written outside it.

### Step 0 — Commit the inputs, build the first scripts, test what is untested

- **Needs:** Shane's yes to this plan, recorded as the first row of `answers.md`. That yes also adopts
  the starting values in § 4.5 and the bounds on this step below.
- **Who does it.** The main session starts agents and commits what they return; it writes no script and
  no test. One agent writes the scripts. A different agent writes the tests. A third runs the tests and
  records the results. Every commit carries the agent's run identifier.
- **Bounded before anything else is built.** The first piece is `step0-guard`, a script small enough to
  read in one sitting, written and tested first. It is run before every later piece of this step and
  refuses to continue when any of these is true: the step has used ten runs; the scripts total more than
  4,000 lines, tests not counted; two runs in a row passed no new test; `scripts/` holds a file that is
  not on the list below. Then it writes one notice to Shane with the numbers. Only his words clear it.
  The earlier tool built to manage a design document grew to about 5,700 lines (failures:556).
- **Produces:**
  - One commit that adds this plan at its fixed path, marks the old plan superseded, and commits the
    inputs under `inputs/` with v2 marked as a superseded draft. The memory index's pointer to the live
    plan is updated in the same step. That index is the one path outside the repository this plan
    allows, and the path check in § 4.7 names it as its single exception.
  - The fixed-form files, seeded from the inputs: `rulings.jsonl` (121 rows, rulings:244-254, each with
    its route from the routes file); `precedence.md` with the eighteen agent labels listed as "not his
    ruling" (rulings:203-227); `technical/conflicts.jsonl`; `technical/hard-problems.jsonl`; `handoff.jsonl`
    holding the fourteen mutations of the bank example as the first acceptance criteria (want:112-127);
    `settings.json` with its hash recorded beside the first row of `answers.md`; the list of flaw kinds
    (§ 4.2) with its hash.
  - A closed list of scripts. Built in this step, fifteen: `step0-guard`, `step0-tests`, `locked-write`,
    `next-run`, `status`, `stop-rules`, `cite-check`, `lint`, `rulings-check`, `question-check`,
    `attack-check`, `removed-lines`, `plan-lint`, `trailer-check`, `prompt-check`. Built later, each as the
    first piece of the step that first uses it and each passing test 1 below before it is used:
    `refusal-check` (step 6), and `pointer-check` and the four phase 2 checks widened to all of canon
    (step 14). `plan-lint` fails if `scripts/` holds anything else. What each does is in § 4 and § 7.
  - Four workflow scripts: write a section, run an attack round, check a question, settle a dispute. And
    the start command of § 7.4.
  - `docs/Working/decision-index.md` marked as not authority.
  - The permission rules an unattended run needs, as an exact list, put to Shane for his yes (§ 6.5). No
    agent changes a permission setting or `.claude/settings.local.json` without that yes.
- **Finished when** `step0-tests` prints zero rows of `step0/tests.jsonl` without a recorded outcome,
  where an outcome is either a pass or the fallback § 7.6 states for that test, adopted and recorded. The
  list is closed at eleven tests:
  1. Each script, run on a small made-up folder whose right answers are known, prints them; run on a
     missing, a half-written and a malformed file, it stops with an error. A script that prints zero for a
     file it could not read would make every stop rule pass falsely (v2:516).
  2. Each stop rule in § 4.5 fires on a prepared input, stays fired when the same start is tried again,
     and clears only by the route § 4.5 states for it. A change to `settings.json` with no matching row
     in `answers.md` makes `next-run` refuse.
  3. Each workflow script parses under the `new Function` check in `CLAUDE.md`, then runs start to finish
     under Node with stand-ins for the Workflow tool's functions. The prepared results include an agent
     that returns nothing, an attacker that reports a refutation, a dispute, and a spending limit reached
     in the middle of a piece.
  4. An agent started by a design workflow cannot reach `precept_compile` or `precept_proofs`.
  5. An agent pinned to Fable 5.2 loads the author's reading list, reports its model identifier, and
     writes a short section; the output file records the model and the run identifier.
  6. One real piece of each of the four first kinds of work is run on material that already exists (the
     write-ups in `docs/working-reset/phase-5/` and the bank example), and its cost is recorded as
     measured on a stand-in.
  7. A real run is stopped part-way, and the next run starts at the first piece not recorded.
  8. The one-line start (§ 7.4) carries a step from one run to the next without Shane, and stops with a
     notice after a failed run.
  9. Two lines of work commit at the same moment from two git worktrees without damage, with the shared
     files written only through `locked-write`.
  10. How a short usage window, a weekly limit and an exhausted Fable share each show up to a running
      script is recorded; the extra-usage setting is checked; a notice is pushed, and whether it was
      received is recorded.
  11. An unattended run completes without stopping at a permission prompt under the rules he approved.
- **Counts:** tests with an outcome, of eleven, and how many are fallbacks; scripts built, of fifteen;
  script lines; runs used; kinds of work with a measured cost.

### Step 1 — What an operation is

- **Carries:** contents item 1. The conflict over stateless handlers (§ 2.3). The writer list closed in
  canon, with the route through the execution order that the old plan identified as what its six
  slicings missed (old:1679-1684).
- **Needs:** step 0.
- **Waits on Shane:** state actions, three sheets (§ 6.2). Each sub-item is written both ways until he
  answers, and the creation argument is attacked in a form with no state actions so the answer cannot
  hold the checkpoint (review:80). `from any`, only if the check below leaves it a question; then it too
  is written both ways.
- **Produces:** the definition of a configuration, of each operation, of a handler as the whole composite
  an operation performs, and the closed writer list. A fresh check on `from any`: if the two readings
  differ in nothing an author or caller can observe, it is a design choice argued here; otherwise it is a
  question (§ 6). The two independent drafts of the contents list and their item-by-item comparison
  (§ 2.1). The first frozen list (§ 4.6).
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; sub-items held in two versions; contents items without a disposition.

### Step 2 — What every operator means on every type

- **Carries:** contents items 7 and 8 (meaning) and 17. Hard problems: evaluator arithmetic, overflow,
  the `number` product that is zero, the meaning half of units outside the exact model, the meaning half
  of temporal arithmetic. The conflicts and rulings § 2.3 and § 5 route here.
- **Needs:** step 0. Its first piece is the research pass on base-10 rounding, run through `/research`.
  His waiver of a comparator survey covered period ordering only (§ 5) and is not used here.
- **Waits on Shane:** the overflow model for `integer` and for `decimal`, one sheet each. The
  conversation on bounds for temporal point types (§ 6.2): until it has happened, the temporal overflow
  piece is written for both alternatives canon already has, guards only and a representable-range fact
  checked at ingress (proof-model:360), and proposes no new bound vocabulary. The decimal rounding model,
  only if the research shows a choice an author can see.
- **Produces:** one written arithmetic model per type: what each operation computes, when it is exact,
  what it is when it is not, and what overflow is. One definition of each operation, stated once and
  shared by the evaluator, the prover and the checker (precedent:133). The overflow model for all four
  type families. What a comparison on an unset optional means. Literal division folded through the one
  shared evaluator. What a `number` product may be concluded to be. What a quantity in a logarithmic unit
  means for conversion and comparison.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; operators with a written meaning, of the extracted operator list.

### Step 3 — The table of rules

- **Carries:** contents item 2 and the meaning half of item 9. The conflicts over `maxplaces`, quantified
  rules and the forms of `ensure`. The meaning of quantified rules. His ruling that "constraint" is the
  wider word and that all five constraint forms join the proof, each by where it is anchored (§ 5).
- **Needs:** step 2 passed.
- **Waits on Shane:** the clock, for rules that mention it; everything else is written without them.
  Event ensures, only if the two checkers leave the question open; then the event-ensure rows are
  written for each option. The conversation on how a quantifier binds over one-type and two-type
  collections (`docs/language/collection-types.md:1296`): until it has happened, the quantified-rule rows
  are written for both binding shapes.
- **Produces:** one table: every constraint spelling, the statement it stands for, and the configurations
  in which it must hold. `to` and `from` ensures stated as statements about a transition. A rule in a
  state stated as established on every entry and preserved while the entity stays there.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; constraint spellings with a row, of the extracted list.

### Step 4 — The obligation model and the ingress contract

- **Carries:** contents items 3, 4, 5, 6 and 11. Hard problems: the editable-write check, and the premise
  half of rows tried in order. The conflict that the induction has no mechanism, and the rulings on
  obligations at every write site that § 5 routes here. This is the largest hole in canon measured
  against the want document (consistency:55-56).
- **Needs:** steps 1 and 3 passed.
- **The one exception to pass-before-start: the checkpoint.** Five arguments are drafted in the progress
  folder, not in canon, as soon as step 1 has its first section written: establishing a rule at
  creation; preserving it across a `set` in an event row; and using a guard, an argument constraint, and a
  rule that held beforehand as premises (v2:265). They are stated over exact numbers, with no state
  actions, no collections and no clock, and each states the plain rule it uses in full, because step 3's
  table does not exist yet. They are attacked for three rounds. The reason is that whether the model can
  be written at all is the question the want document itself leaves open (want:183), and it should be
  tested before the arithmetic work in step 2 is finished. Nothing from the checkpoint enters canon;
  step 4 re-attacks all of it in full when it runs in order. The rule that reads the result is in § 4.5.
- **Waits on Shane:** the editable-write answer. Until then the edit section is written once for each of
  the three options, behind pending markers. Event ensures, only if the check leaves the question open,
  on the same terms.
- **Produces:** establishment and preservation obligations; fault obligations at every evaluation
  position; the sites; which rule and handler pairs need no proof; the complete premise set at each site,
  the negated guards of earlier rows included; the ingress contract, designed with the obligations so
  that an edit's proof assumes exactly what ingress checks.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; checkpoint arguments with a refutation open at round three.

### Step 5 — Facts per type and how each action changes them

- **Carries:** the proof-fact half of contents items 7, 8 and 9. Hard problems: collection elements,
  calendar properties, the proof half of units outside the exact model. The per-type table of algebraic
  properties (old:1676-1677). The period-ordering type condition on relational facts (old:1610-1616), for
  which no comparator survey is run, on his waiver (§ 5).
- **Needs:** steps 2 and 4 passed.
- **Waits on Shane:** the two conversations (§ 6.2), for the temporal-bounds facts and the quantifier
  binding only. Those pieces are written both ways until then.
- **Produces:** for each type, the facts it contributes and what each action does to each: intervals,
  linear relations, length, count, presence, choice value, qualifier, element properties, temporal
  facts, and for strings equality, case-insensitive equality and prefix and suffix tests. The
  preservation table for collections. Its rows are read from canon, one for each collection kind and each
  action canon gives that kind; its columns are the predicate forms canon gives that kind (§ 2.2). A cell
  exists only where canon gives the kind both the action and the form, so the table is not two lists
  multiplied together, which safeguard 18 in § 8 forbids.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; cells of the preservation table filled, of the cells the script
  generates from the extracted per-kind lists.

### Step 6 — The inference steps

- **Carries:** contents item 12 (the steps) and the proof half of item 10. Hard problems: combining
  facts, non-linear rules, the proof half of the zero `number` product, and the case-splitting half of
  rows tried in order. The conflicts over combined facts and permanent limits (§ 2.3). The rulings on
  full proof power, non-linear rules and the expression as written (§ 5).
- **Needs:** step 5 passed, and step 9's list of the proof steps the graph analysis needs.
- **Waits on Shane:** nothing, unless the non-linear write-up finds the case named in § 2.2.
- **Produces:** the closed vocabulary of proof steps, each small enough to check mechanically, which is
  also the certificate's vocabulary. It includes a step for strict inequalities, a step that shows a set
  of rules and a guard has no solution, and a step that exhibits values satisfying a guard, checkable by
  evaluating it; the last two are what step 9 needs. What replaces the single-step wording. The intended
  set of rewrites for expressions, each with its argument against the evaluator's arithmetic, with the
  first build's set marked (review:38). Canon's permanent-limit text replaced. The closed contents list
  with its hash. The coverage table, generated from the extracted lists with one row per member of each
  list and never as the product of two lists. The first refused list. The earlier validity arguments are
  given to this step as claims of unknown standing to attack, never as settled (old:1664-1667,
  :1819-1820).
- **Finished when:** the step's rows are closed, and `refusal-check` prints zero rows of
  `refusals.jsonl` that fail § 4.3.
- **Counts:** the shared counts; proof steps named; coverage rows with a step or a permitted refusal.

### Step 7 — The procedures that find proofs

- **Carries:** the procedure part of contents item 12. Hard problem: scale, and the cost half of
  combining facts. The conflict over strategy numbering.
- **Needs:** step 6 passed.
- **Produces:** the procedures, their limits, and a written cost bound for each proof step, attacked
  with its argument. For linear integer obligations, the stated outcome when a complete procedure has no
  acceptable bound (§ 2.2). Any cap, with the argument § 4.3 requires for it. Authority stays in the
  steps; the search can change without touching soundness (spec:225).
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; proof steps with a cost bound, of the total; caps with a passed
  argument, of caps stated.

### Step 8 — Verdicts, the counterexample, the missing premise

- **Carries:** the rest of contents item 12. Hard problems: producing the missing premise; rules true but
  not inductive. The conflict over computing the missing premise. The rulings on the table of what must
  not compile, on never inserting the premise, and on the three verdicts (§ 5). The design of the bounded
  counterexample search; the search itself, being code, goes in `handoff.jsonl` (req:748-750).
- **Needs:** step 7 passed.
- **Produces:** the three verdicts for rule obligations as well as fault obligations; how the concrete
  case is found, re-checked and shown; how the missing premise is computed and chosen; the rule for
  telling "add a guard" from "bound the argument" from "add a rule"; the statement that the compiler never
  inserts the premise.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts; rows of the want document's rejection table with a named diagnostic, of
  fourteen.

### Step 9 — Graph structure with dead rows

- **Carries:** contents item 13. Hard problem: reachability to the standard he wrote. The conflicts over
  the dead-row check, the definitions of a dead end and the unexplained graph diagnostic.
- **Needs:** steps 4 and 7 passed. One piece runs earlier: as soon as step 4 has passed, this step
  writes its one-page write-ups and `step9/proof-steps-needed.jsonl`, the list of proof steps the graph
  analysis needs. Step 6 may not close the contents list until that file exists, and `status` checks it.
- **Waits on Shane:** nothing, unless the dead-end check leaves the narrow case in § 6.2, or the
  reachability write-up shows the standard in § 2.2 cannot be met.
- **Produces:** guard satisfiability using the rule premises; removal of dead rows; reachability
  recomputed; the stage order that allows it; a demonstration that the loop with obligations on
  unreachable states ends in one pass, or the order that makes it end (proof-model:438); how each state
  is shown reachable, or the definition rejected; for dead ends, the requirement that the compiler
  exhibit an input that leaves the state, with the rule that an undecided case is never counted as "can
  be left"; rows that can never fire because earlier rows always match first, using the negated earlier
  guards from step 4; one definition of a dead end, settled against want:152 and `docs/philosophy.md:44`.
  That the loop ends is a separate claim from its being sound. The soundness argument belongs to step 11.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts.

### Step 10 — The certificate and the checker

- **Carries:** contents item 14. Hard problem: what the checker is trusted for. The conflicts over the
  certificate's format and route, and over the premise list against the constraint index. The rulings on
  the certificate and one checker, and on the premise list being what the runtime validates (§ 5).
- **Needs:** steps 4 and 6 passed. The piece on graph verdicts needs step 9 passed. It decides whether
  reachability, dead-row and dead-end verdicts are recorded in the certificate and replayed by the
  checker, or says why not, given that no unproven definition may govern (want:204).
- **Waits on Shane:** whether a certificate names what establishes and keeps each fact, only if the two
  checkers leave it open (§ 6.2). If it reaches him it goes with the write-up this step produces, which
  is the context he asked for (rulings:231-233).
- **Produces:** the record; the checker's duty to recompute what a definition owes; what ties a
  certificate to its definition; its route to the runtime; the compiler stage; the load check and what
  its refusal returns; the run-time checks derived from the premise list.
- **Finished when:** the step's rows are closed.
- **Counts:** the shared counts.

### Step 11 — The induction argument

- **Carries:** contents item 16. The rulings on the induction, on no tiers, and on the proof stopping at
  one precept (§ 5).
- **Needs:** steps 1, 4, 5 and 6 passed. The piece on dead rows and unreachable states needs step 9
  passed.
- **Produces:** the argument that the induction holds for this language, attacked like any other. It
  covers, as one argument, the fact that each rule is proved only over rows still counted as live while a
  row is called dead by assuming the rules hold: the two depend on each other, and the argument is by
  induction on the length of a run (`docs/compiler/proof-engine.md:2092`; want:127). The statement that a
  true but non-inductive rule is rejected with advice to add a rule. The guarantee section of the spec
  stating that there are no tiers and that the proof stops at one precept.
- **Finished when:** the step's rows are closed.
- **Then the proof-model sign-off** (§ 6.3), once steps 1 to 11 have all finished, step 9 included. A
  hash of the proof-model sections is recorded. A later change to them is logged with its reason and
  sends back into rounds the changed arguments and every argument `technical/dependencies.jsonl` says
  rests on them (§ 4.5).
- **Counts:** the shared counts; changes to the hashed sections.

### Step 12 — Catalog entries, diagnostics, runtime and tooling

- **Carries:** contents items 15 and 18. The conflicts § 2.3 gives this step, the editable-write
  sentences, and every mechanical conflict that was on the frozen list. The design of schema evolution on
  the restore path, and the other rulings § 5 routes here.
- **Needs:** steps 1 to 11 finished and Shane's sign-off of the proof model.
- **Produces:** the catalog entries the checker asks, specified in `catalog-system.md`; every diagnostic;
  the proof model carried into every document it changes and every document that restates it; inspection
  results with where a proof came from; what a refusal carries; tooling followed through hover,
  `precept_proofs` and inspect on paper. A proposed edit to the `CLAUDE.md` catalog notice saying that the
  design now specifies the catalog contents and that the suspension stays until the build enforces it.
  Internals answers that depend on the proof model. The rows of `dedup/pairs.jsonl` for the frozen facts.
- **Finished when:** `status --step 12` prints zero documents not carried in `step12/documents.jsonl`
  and zero unfilled or failing rows of `dedup/pairs.jsonl`, and `pointer-check` prints zero pointers of
  the wrong form and zero unresolved pointers across canon, the frozen list included.
- **Counts:** documents carried; frozen facts remaining; diagnostics specified.

### Step 13 — Acceptance criteria handed to the next stage

- **Needs:** steps 3 to 11 finished.
- **Produces:** `handoff.jsonl` completed: criteria for each type and each hard problem; the first-build
  cut named as the next stage's first decision; the bounded counterexample search; the rules for the build
  stage that Shane has already given, which are that executing must not rewrite design and that the
  reviewer runs at every slice boundary (rulings:192-193), and that the floating NodaTime reference stays
  unpinned (§ 5).
- **Finished when:** `status --step 13` prints zero rows of `handoff.jsonl` without an owner or of a
  kind condition 7 does not allow.
- **Counts:** handoff rows by kind; rows without an owner.

### Step 14 — Everything the proof model does not reach

Runs beside steps 1 to 12 in its own worktree.

- **Needs:** step 0, and two rules in § 4.5. `next-run` will not start this step until step 1 has its
  first section committed in canon, and pauses it whenever the lines it has changed in canon run ahead of
  the lines changed in the proof-model sections by more than the set multiple.
- **Does not write** in `soundness-and-coverage.md` or `proof-engine.md`. Both are on the frozen list
  whole (§ 4.6), and their placeholders belong to the steps that own those sections.
- **Produces:** the mechanical conflicts in § 2.3. Plain copies merged into one owning statement with
  the others turned into pointers of one fixed form. Its rows of `dedup/pairs.jsonl` (§ 1). Lists that
  canon hands to a source file restated in canon, with the sentence reversed so the document is what the
  source file is checked against (v2:154). Every "deferred", "post-MVP", "later" and "v1" in canon
  sorted into the four groups of § 4.4. Every open question in canon given an answer, a recorded
  default, or his quoted word that it waits. Internals questions nobody outside can observe answered as
  defaults under the rule in § 4.2. The unfinished documents finished: `docs/tooling/extension.md`,
  preview and inspect, `descriptor-types.md`, the empty open-question sections in two type documents, the
  two stale questions in `tooling-surface.md`, the roadmap modifiers sorted (v2:134-147). The two rulings
  whose words are in doubt, checked (§ 5). The line at `docs/runtime/README.md:36` corrected to his words
  on the `Fault` question (v2:241).
- **Finished when:** `pointer-check` and the four widened checks print zero failures outside the frozen
  list; `status --step 14` prints zero unfilled or failing rows of `dedup/pairs.jsonl` outside the frozen
  list; `removed-lines` prints zero removed lines without a destination; `status --step 14` prints zero
  open for the step's rows of `technical/conflicts.jsonl`, `deferrals.jsonl`, `open-questions.jsonl` and
  `step14/documents.jsonl`.
- **Counts:** facts found, merged, frozen; conflicts closed; deferrals by group; open-question rows by
  state; unfinished-document rows met; removed lines with and without a destination; lines changed
  against lines changed in the proof-model sections.

### Step 15 — The hand walk and the finish

- **Needs:** steps 12, 13 and 14 finished; every question answered.
- **Produces:** worked examples carried by hand through every document in stage order and through seven
  operations: create, fire, update, restore, inspect a fire, inspect an update, and load a definition
  with a valid, a stale and a tampered certificate; plus the typed and JSON boundary (review:126). A
  different agent does each stage with only that stage's document and the previous output. The examples
  are the bank example, its fourteen mutations, and at least one per type family. Two agents derive each
  example independently. Every defect becomes a row in `findings.jsonl` and is worked like any other,
  under the same stop rules, the one on found rows included. The agent pass of condition 5 is run again
  on the final text. A fresh agent sorts the whole change to canon into four groups: rests on the want
  document; rests on a quoted ruling; an internal repair; traces to nothing he wrote or ruled (old:370).
  Every canonical document's maturity row set to "Full" by script from the check results.
- **Finished when:** `status --finish` prints zero for all seven conditions in § 1, zero unrecorded
  pieces in `step15/walk.jsonl`, and Shane has signed the finish.
- **Counts:** walk pieces recorded, of the closed list; findings opened and closed; the seven conditions.

---

## 4. Rules every step follows

### 4.1 Evidence and authority

- **No compiler, no tests, no samples, no catalogs as evidence.** Design agents are given no compile or
  proof tools. `lint` reports any citation into `src/`, `test/`, `samples/` or a catalog source file, and
  the words "compiles", "Proved" and "the compiler accepts", in every design output; the piece is redone.
  The syntax of every example is taken from the grammar sections of `docs/language/`, with the line
  cited, and a second agent confirms each example against those lines. The `precept-author` agent is not
  used, because its workflow compiles what it writes and so lets today's parser decide. Two agents
  derive what each option accepts and refuses from canon (review:39).
- **Every factual claim carries `file:line` and a short quoted fragment.** `cite-check` opens each cited
  line with `git show HEAD:path`, so a session's own uncommitted edit cannot confirm itself. A claim that
  canon is silent must record its search terms and the output of a history search over the canonical
  folders.
- **Who settles a dispute between agents.** One fresh adjudicator per dispute, started by script, given
  `precedence.md`: Shane's quoted words; the want document, which carries his approval in full
  (rulings:47-49), and the philosophy; the language documents; the compiler and runtime documents;
  executed register rows; a restating site. Where none decides, the item goes to the question check.
- **Labels are not authority.** `lint` reports "locked", "ratified", "owner decision", "settled" and
  "ruled" in any canon change or anything sent to Shane unless the line cites a row of `answers.md` or
  `rulings.jsonl`.
- **Every use of a ruling is checked for width.** Each canon edit, plan rule, refusal or adjudication
  that rests on his words cites the row, and an agent that did not make the use writes a file under
  `uses/` saying whether the use covers only what the quoted words cover. A use that cites nothing would
  escape this, so after every run that writes canon a fresh agent reads the diff for statements that
  rest on what he wants or said and cite no row, and writes a `uses/` file for each one found.
- **Decisions carry four parts**: reasoning, alternatives rejected, precedent, tradeoff accepted.
  Anything that would add or change what an author may write goes to Shane as a conversation and then
  through `/design`. No agent proposes syntax.
- **Agents are told two standing facts** so they do not argue from them: there is one developer and no
  release, so migration cost is never a reason (rulings:194); and the runtime being unbuilt is not
  mentioned as a caveat (rulings:195).

### 4.2 Attack rounds

Two arguments that went through twelve rounds held; five that went through none were all refuted in one
pass (failures:362-370).

- Every argument gets twelve rounds with fresh attackers each round. There is no early pass. A refusal
  is an argument (§ 4.3) and gets the same rounds.
- Each attacker's file must contain at least one concrete attempted counterexample: a definition, a
  sequence of operations, and the claimed wrong outcome. `attack-check` rejects a file without one, and the
  round is run again, not counted (old:1801-1803).
- **The kinds of flaw are one list for the whole work**, written in step 0 by an agent that authors and
  attacks nothing, with its hash recorded. It includes "this refusal narrows what the design can prove
  beyond what its cited reason covers". The list changes only through a row in `answers.md`;
  `attack-check` fails a round run against a list whose hash does not match. A finding is a flaw of one of
  those kinds. A note on wording does not reset anything. A fresh agent that is neither author nor
  attacker classifies each finding, and another confirms each repair.
- **Dependencies are not written by authors.** After each section or argument is written, a script
  reads its citations and records every canonical section and argument it cites in
  `technical/dependencies.jsonl`. A fresh agent then reads the text for anything it relies on without
  citing, and adds those rows.
- One fact-checker per round reads the argument's citations against canon.
- Attackers are seeded with the known counterexamples (old:1679-1684) and with the two limits that apply
  to every argument: rules that are true but not inductive, and rounding (precedent:61-69, :170-189).
- An argument passes when round twelve ends with nothing open and the last two rounds produced no
  finding. It is never marked passed in the sitting that wrote it.
- An argument that fails that test goes to a second author on a different model for up to six more
  rounds, with some attackers moved to the first author's model for those rounds. If that fails, it
  reaches Shane with the counterexamples and three choices on equal terms: more rounds or another
  author; a different argument; or a change to the language, which goes through `/design`. These are
  gathered into one sheet per step, sent when that step has no other piece it can run. Weakening the
  guarantee is not a choice on the sheet (want:189).
- Text behind a pending marker may not be cited by any other canonical section or argument;
  `attack-check` fails one that does.
- **An internals default is a decision, not an argument.** It answers a question nobody outside the
  compiler or runtime can observe. It gets the four parts of § 4.1 and one attack. Its canonical lines
  are listed in `internals.jsonl`, and `attack-check` fails any argument or proof-model section that
  cites one of those lines. If an argument needs what a default says, the default becomes an argument
  and gets twelve rounds.
- An agent that returns nothing has not done its piece. A failed attacker is never a round that found no
  flaw.

### 4.3 The only reasons a design may refuse something

A row of `refusals.jsonl` must cite one of these. `refusal-check` fails a row that cites anything else,
a row whose argument has not passed its attack rounds, and a row with no `uses/` file where one is
required below.

1. want:135: no solver the compiler does not carry; a non-linear rule is closed only by a premise that
   states the condition.
2. The first test in spec:225: the proof must leave a certificate that an independent checker can
   replay. And determinism, which has its own source: same definition, same data, same outcome (spec:96).
3. A mathematical impossibility, citing a published result. Two are known (§ 2.2), and in both the design
   response is "prove from declared premises or reject". The refusal's argument must show that the
   result applies to this case and not merely near it.
4. A ruling of Shane's, quoted. Every such refusal is a use of a ruling and needs its `uses/` file. A
   ruling about the first build or about build order is never a reason: `refusal-check` holds the list
   of those rows, seeded from the routes file, and rejects any refusal that cites one. The list today is
   his words on working with the expression as written and widening later, on overflow being built after
   the first compiler, on the minimal proof set for the first build, and on schema evolution at restore.

Three tests in spec:225 may be cited only under conditions:

- **Cost** (its second test) only with a written cost bound for that proof step, attacked with its
  argument.
- **Size and caps** (its third test, "right-sized", with "a hard cap" that falls back to "couldn't
  prove") only with the cap written as a number or as a formula over a definition's declared sizes, and
  an argument, resting on canon alone, of which definitions stay under it. Sample files and what
  definitions have been written so far are not evidence. A cap without that argument is listed as
  pending, and what it would refuse counts as not yet designed.
- **"Earns its place"** (its fourth test) only with his word.

"Not built yet", "the first version" and "the MVP" are never reasons; he confirmed that "the MVP" is an
internal build milestone (rulings:134). No default refuses more because a proof is hard. A proof step
whose argument has not yet passed is listed as pending, not as refused.

### 4.4 Deferrals and removed text

- **Four groups for every deferral in canon**: designed here; excluded on a reason from § 4.3; a
  capability he deliberately left open, with his words; build order only, with the design owed here.
  Schema evolution on the restore path and proof across precepts are named rows.
- **No line leaves canon without a destination.** `removed-lines` lists every line removed in a run and
  requires each to appear in a destination record: the owning statement it was merged into, the section
  it moved to, or a rejected-alternatives section. Merges are checked sentence by sentence by a second
  agent, because paraphrases defeat a search for terms (failures:516-518).
- **Copies that agree are merged. Copies that disagree are decisions**, settled against his text by a
  fresh agent with a citation, never by choosing which document owns the fact (v2:230-235).
- **A default counts** only once it is written into its owning section and its row is gone from the
  open-question index, both checked by script.

### 4.5 Stop rules

Each is computed by `stop-rules` from the progress files and read by `next-run`, which refuses to start
the affected work while one has fired. None depends on an agent noticing.

**How a fired rule clears.** A rule that halts work stays fired until `clearances.jsonl` holds a row
naming that firing. Only a script writes that file, and only in one of two ways. The first is his words:
the row cites a row of `answers.md`. The second is a repair: the row names a repair file written by an
agent that did none of the halted work, and `stop-rules` computes that the repair condition in the table
is true. After a clearance, every running count in the rule starts again from the clearance row, so a
count that has reached its threshold does not hold the work for ever. A rule that fires again on the
same step within three runs of a repair clearance can then be cleared only by his words.

**The thresholds are protected.** They are in `settings.json`, and the starting values are given here
and adopted with the plan. `plan-lint` compares the file's hash with the hash recorded beside the latest
row of `answers.md` that names a settings change, and `next-run` refuses to start while they differ. A
change is applied by script from a proposal file after his words are recorded; no agent edits the file.
Run ceilings worked out from measured cost (§ 7.5) are not thresholds; they are in `measured.json`,
written only by `status` from a fixed formula. The progress files the rules read are written only
through `locked-write`, and `trailer-check` fails any commit that changes one by another route.

| Rule | Fires when | What happens next | Cleared by |
|---|---|---|---|
| Proof model first | Step 1 has no section committed in canon | Step 14 does not start | The script, when the section is committed |
| Tidying runs ahead | Lines step 14 has changed in canon exceed twice the lines changed in the proof-model sections | Step 14 pauses; steps 1 to 11 continue | The script, when the proof-model lines catch up |
| Open finding | A piece's inputs include a section with an open row in `findings.jsonl` | That piece does not start; the finding is worked first | The script, when the row is closed and its repair confirmed by an agent that did not make it |
| Dependents reopen | A section or argument changes after passing | Every argument that `technical/dependencies.jsonl` says rests on it goes back into rounds. These are counted as reopened by a dependency | Not a halt |
| Different problem each time | Three repairs in a row of one argument are each refuted on a different kind of flaw, by the frozen list of kinds | The argument goes to the second author early | Not a halt |
| Cascading repairs | Two repairs in one section each cause a refutation in an argument resting on it or under it | The section halts; one notice to Shane with both cases | His words |
| Whole model | Two of the five checkpoint arguments still have a refutation open at round three (this half can fire once); or, since the last clearance, at least three arguments and more than a third of those that have finished their first author's rounds went to a second author | Steps 1 to 11 halt. One sheet to Shane with the counts and the counterexamples, offering the three choices of § 4.2 and nothing else: more rounds or another author; a different way of writing the model; or a change to what an author may write, through `/design` (want:185-189). Weakening the guarantee is not offered. The sheet passes the five-rule check of § 6.1 before it is sent | His choice |
| Reopened work rising | The count of items worked and then reopened for a reason other than a dependency rises across two runs in a row | That step halts with a notice | His words; or a repair file naming the common cause, with the dependencies derived again and one trial run in which the count does not rise |
| Found faster than closed | For one step, rows found and not yet worked have risen across three runs in a row; or the rows found in all exceed twice the number recorded when step 1 finished | That step halts. A fresh agent groups the open rows by cause. One sheet to Shane with the found and closed counts per run and the grouping | His words |
| Records outgrow the design | From a step's third run on, lines of record written for it exceed ten times the lines it has changed in canon | That step halts. A fresh agent lists which kinds of record account for the lines and which no script or later piece reads. One notice to Shane | His words, usually a settings change that stops a kind of record |
| Wide use of a ruling | A use has no file under `uses/`, or one marked wider than the words | The line of work that made the use halts | The script, when a third agent's `uses/` file marks the corrected use as within the words, or the use is reverted |
| No progress | A run records no piece, or two runs in a row leave the step's counts unchanged, or several agents in a row return nothing | The session stops with a notice and the literal error | His start line, which the start command records, permits one trial run. If the trial records a piece the rule clears. If not it stays fired and the notice says so |
| Record too large | A piece's record exceeds the size cap in `settings.json` | The piece is rejected and redone | Not a halt |

The rounds of one argument are bounded without a rule: twelve, then six with a second author, then
Shane. What is not bounded by that is new rows being found faster than rows are closed, which is how
phase 2's finish line kept moving (failures:146-151). The "found faster than closed" rule is the answer
to that, and it applies to the hand walk in step 15 as well.

### 4.6 The frozen list

Facts the proof model will change are not merged or edited by step 14 until the proof model passes: the
whole of `soundness-and-coverage.md` and `proof-engine.md`, the proof records, whatever carries the
certificate, constraint and field descriptors, all result types, anything touching update or restore,
the stage order, the diagnostic totals, and the three rows from the mechanical checks that concern the
premise list, the two shapes of the constraint descriptor and event ensures (v2:179). The list is a file.
It is first written from the hand-working of the bank example in step 1 and confirmed when the contents
list closes at step 6.

When a fact joins the list after step 14 has started, a script lists every step 14 commit that touched
it. A fresh agent checks each of those edits against the proof-model text as it then stands. An edit
that does not hold is reverted. Either way the fact passes to step 12, and step 14 does not touch it
again.

### 4.7 Records

- One plan page, this one. `plan-lint` finds exactly one file under `docs/` whose status line says it is
  the live plan, and fails any finish condition here that names no list file and no script. An edit to
  this page after adoption needs a row in `answers.md` carrying his words, and a check file written by an
  agent that did not propose the edit, testing it against all five rules in § 1 and against
  `rulings.jsonl` at once.
- Every record is a fixed-form data file, one per piece. Each kind of work may produce only its own
  record. There is one adversarial review per artifact and no review of a review. The total is bounded
  by the "records outgrow the design" rule in § 4.5, not only record by record.
- `status.md` and `notices.md` are generated or appended by script. No count is typed by an agent.
- Every commit that changes canon or a design file carries a trailer naming the workflow run and the
  output file it came from. `trailer-check` confirms that the output file exists, that it records that
  run and its model, and that the lines the commit adds to canon are in it. A commit that fails is
  reverted by script, the revert is recorded, and `next-run` does not start another run until that is
  done. The main session writes no canon and no design text.
- `prompt-check` fails any workflow prompt that does not contain the "How to write" section of
  `CLAUDE.md`. `lint` applies that section's banned list to every canon change and everything sent to
  Shane, and also rejects, in anything sent to him, register row identifiers, walk item numbers, the
  conflict codes of the input files, and bare ruling row numbers.
- Every notice lists the rows added to `answers.md` since the previous notice, so he sees what has been
  recorded as his.
- No path in the plan or a progress file may point outside the repository, with the one exception of
  the memory index named in step 0. `bugs.md` and the want document stay live and are not archived
  (rulings:199). Canon cites nothing under `docs/Working/` or `docs/working-reset/`.
- **Closed lists that have to reopen.** Lists are re-extracted after any step that changes
  `docs/language/`. If a list's hash then differs, a script compares the members. Each added or changed
  member gets a new open row in every table built from that list. Each removed member's rows are closed
  with the removing edit cited. Rows for unchanged members stand and are not re-run. The new hash is
  recorded with the commit that caused it.

---

## 5. His rulings: the route of each into canon

The rulings file found 121 rulings (rulings:244-254). The routes file gives the route of each by its row
number; step 0 seeds `rulings.jsonl` from the two together. This section says the same in words.

Each ruling is one row of `rulings.jsonl` with his words, where they are recorded, and exactly one of
four outcomes:

1. The canonical file and line that states it, with a quoted fragment.
2. The rule of this plan, the `CLAUDE.md` line or the row of `handoff.jsonl` that carries it.
3. Superseded by a later ruling of his.
4. No longer applies, with the reason.

Outcomes 3 and 4 retire something he said, so an agent's judgement is not enough for them. Each needs
the later words quoted from `rulings.jsonl` or `answers.md`, and a `uses/` file by another agent
confirming that those words do cover it. A retirement that has neither is not made. It stays open and is
listed for him at the next sign-off, one line each, to confirm or refuse. A search that finds nothing
never retires a ruling.

**His answers during this work become rulings.** `rulings-check` appends a row to `rulings.jsonl` for
every new row of `answers.md`, including his direction after a notice and his choice on a sheet, with no
outcome yet. `next-run` schedules the routing of each as a piece. Condition 4 of § 1 therefore covers
what he says from now on, and cannot be met while any row has none of the four outcomes.

`rulings-check` runs after every run that changes canon. A fragment that no longer matches is read by an
agent, who records whether the ruling was undone or the sentence reworded.

| Rulings | Route |
|---|---|
| Forty that the rulings file records as already stated in canon | The row records the line; the script keeps it there; authors and adjudicators are given it as settled |
| Full proof power | Target in canon. Step 6 removes the permanent-limit text; § 4.3 is the closed list of refusal reasons |
| The induction; a rule used as a premise must be established and preserved | Steps 4 and 11 |
| Non-linear rules are proven only from a premise that states the condition | In canon (spec:270). Step 6 defines "states the condition directly" and is bound by it (§ 2.2) |
| A certificate and one checker | Step 10 |
| Overflow is built after the first compiler | Governs build order only. Step 2 designs the model. Never a refusal reason |
| Three verdicts; "minimal proof set for the MVP" | About the first build. Steps 8 and 12 state the three verdicts for rule obligations too. Never a refusal reason |
| Who constructs a `Fault` waits: "seems like something we can defer until we start building the runtime" | Stays deferred on his words. He said building, and this work designs, so reading his words as covering the design would widen them. It is the one exception under conditions 6 and 7. One line in the first batch tells him it is the only design item this plan leaves to the build, and that he can say otherwise |
| `integer` is fixed width for now, model open | His question (§ 6.2); step 2 |
| One full statement | The pointer form, the ownership table and the document-by-document pass in step 14; step 12 for the frozen list |
| The table of what must not compile | Acceptance criteria in step 0; a diagnostic for each row in steps 8 and 12 |
| Authors extend constraints to entry points | Step 4 |
| No tiers; the proof stops at one precept | Step 11, in the spec's guarantee section. The philosophy is not edited |
| The compiler never inserts the premise | Step 8; `language-server.md` in step 12 |
| Coverage rests on the catalogs | Step 12 specifies the entries the checker asks. Separately, this work takes its lists from `docs/language/` |
| A route into a state that writes nothing owes its constraints; Create, Fire and Update owe preservation | Step 4 |
| Period ordering, both halves, and its design | Mechanical edit in step 14; each of its decisions stated in its owning document in steps 5 and 12 |
| The research waiver, "waive it, we're built on noda", for period ordering only | Carried by the lines in steps 2 and 5: no comparator survey for period ordering, and the waiver is used nowhere else |
| One runtime index entry; the earlier rulings on `sqrt`, fault delivery and `choice` | A checker in step 14 confirms each against canon and records the line, or opens a finding |
| The guarantee contract | Record only; its fourth part is superseded, with the later words quoted |
| The spec states the target | Mechanical edit in step 14 |
| Obligations at every write site; one proof per path; a field read in a `when`; faults at the point of evaluation; a computed field's modifier | Step 4 |
| Restore handles schema evolution later | Build order only. Mechanical edit in step 14; its design is owed in step 12. Never a refusal reason |
| What an editable write evaluates | His answer (§ 6.2); step 4; the sentences in step 12 |
| The premise list is what the runtime validates; inspect cites where a proof came from | Steps 10 and 12 |
| A rule that mentions the current time | His question (§ 6.2); step 3 |
| The expression as written; widening later | About the first build. Step 6 states the intended rewrites and marks the first build's set. Never a refusal reason |
| Fold literal division through the one evaluator | Steps 2 and 6 |
| "Constraint" is the wider word, "I used the term 'rules' more loosely", and all five constraint forms join the proof, each by where it is anchored | His words are recorded. Step 3 writes the table on that footing; step 4 gives each form its obligations |
| Design first; all detail; evidence | § 1, § 4.1, § 4.4 |
| The reset plan's rows "mostly mine" | The kept-rulings table in § 9 |
| Close phase 2 with its defects recorded | Carried by § 2.3 |
| The floating NodaTime reference is not pinned | A build-stage rule in `handoff.jsonl`, step 13 |
| Canon against canon is in scope | The checks read all of canon |
| Plain prose; plain questions with a worked case; one at a time, no pop-up prompts; numbered steps; agent reviews are not his; labels are not his; do not re-ask; do not widen; thin main session; transient folders; search history | § 4.1, § 4.7, § 6, § 7 |
| Use the strongest model; use the whole budget | § 7.2, § 7.5 |
| Consult before new language | § 4.1, § 6.2 |
| Executing must not rewrite design; the reviewer at each slice | `handoff.jsonl`, step 13 |
| One developer; stop mentioning the stub runtime | § 4.1 |
| Two working files stay live | § 4.7 |
| Ten that later rulings replaced | Superseded; each row names what replaced it and quotes it (req:495-497) |
| The bounds-qualifier example uses `in 'each'`, recorded only in memory | A checker in step 14 looks for the example. Found with `in 'each'`: the line is recorded. Found with another unit: a mechanical edit. Not found: the row stays open and goes on the sign-off list for his one-line answer |
| Approximation stated per operation might satisfy the philosophy in place of the split between exact and approximate types, recorded only in memory | A checker in step 14 looks for his words. Found: it touches the philosophy, so it becomes a question. Not found: the row stays open and goes on the sign-off list. It is not recorded as dropped |

One correction lies outside the repository: a memory file still teaches an earlier version of a ruling
that two later rulings reversed (rulings:265-267). It is corrected with his say, asked as one line in the
first batch.

---

## 6. What reaches Shane

### 6.1 How a question gets to him

A question sheet is sent only when all of this is true, and `question-check` refuses otherwise:

- Two checker files exist, written by two agents who were given `rulings.jsonl`, the walk file,
  `phase-2/trace-walk-rulings.md` and `phase-2/five-items-walk-2026-08-27.md`, and not the proposer's
  conclusion. Both record that no text settles it. `docs/Working/decision-index.md` is off limits to them.
- A third file checks the question against all five rules in § 1 at once. A question about what Precept
  is or how much it promises fails here: want:14, want:171 and his 2026-11-13 answer settle it.
- If the options differ only in how much the compiler can prove, it is design work, not a question.
- The sheet is a plain consequence with a worked case, the quoted text, every option on equal terms,
  what each option refuses, and the message an author would see. One question to a sheet, in plain
  text.

**When a batch goes out.** `question-check` records which steps wait on each question. A sheet is ready
when its checks have passed and anything it must show him exists. A batch is sent as soon as some step
can run no further piece without an answer in it, and it holds every sheet that is ready at that
moment. A sheet that is not ready moves to the next batch; no ready sheet is ever held for one that is
not. `question-check` also fails any assignment in which a question would wait for a step that itself
waits on that question.

Every batch carries the list of questions that were dropped, one line each with the citation that settled
it. A random sample of ten dropped items is re-checked in full by a fresh agent that did not drop them
(old:816-823). He is sent only the result: how many of the ten were wrongly dropped. If more than one
was, the filter is run again before anything is sent. His reply is written into `answers.md` word for
word in the same sitting and committed.

### 6.2 The questions, checked against his recorded rulings

I checked each candidate against the rulings file, the walk file and the two later walk files. This is
the author's check; the two fresh checkers in § 6.1 repeat it without seeing these conclusions.

**Seven sheets that stand after the check.** All seven go in the first batch, because steps 1 to 4
cannot finish without them and none needs anything written first.

| # | Question | What the check found |
|---|---|---|
| 1 | **What closes a rule's obligation when a field it mentions is edited directly?** Three options on equal terms, as he was shown them before (old:282): prove the rule from the field's own declared bounds and the guard on editable access, and refuse where that cannot be done; have the engine evaluate, at the write, the field's own constraints and every rule that mentions it, which is what want:143 says; or refuse the `editable` declaration. The sheet shows, for each, what it refuses and the message an author would see | His approved text says the second (want:143), and his own example depends on an editable field (want:36). His text also says no rule is handed to the runtime and the runtime does not re-evaluate rules after a mutation (want:171, want:199). The old plan recorded that these "are all yours and pull opposite ways" (old:282). On 2026-08-26 he had the narrow wording removed from canon and declined to settle the question in passing (`docs/working-reset/phase-2/trace-walk-rulings.md:42-46`). So it is an open question with three options, not a confirmation |
| 2 | **Is `integer` fixed-width, owing overflow proofs, or arbitrary precision?** Shown with an `integer` example taken from canon | "I am considering arbitrary precision but no decision yet" (rulings:160) |
| 3 | **Must every `decimal` operation be proved to stay inside a fixed range, or does `decimal` have no overflow?** Shown with his own `Deposit`: under the first it is rejected until an upper bound is declared; under the second it is provable as he wrote it (want:52-55, want:101; proof-model:353) | Canon has a target and no model (proof-model:351). Nothing he has said chooses, and an author can see the difference |
| 4 | **May a state declare several state actions?** | Listed in the walk as waiting on him, never ruled (`docs/working-reset/phase-1/the-walk.md:837-839`); spec:2016 records it as unsettled |
| 5 | **Do a state's entry actions run when an entity is created in that state?** | The same record |
| 6 | **Do they run on a transition from a state back into itself?** | The same record |
| 7 | **May a rule mention the current time, or does time belong only in guards?** | "it is undecided, add it to the list" (`docs/working-reset/phase-2/five-items-walk-2026-08-27.md:121`). Canon and the approved philosophy say yes (`docs/philosophy.md:28`), so a "no" needs his approval for a philosophy edit |

**Eight that reach him only if the fresh checks leave them open.**

| # | Candidate | Why it may not need him |
|---|---|---|
| 8 | Must a certificate name what establishes and keeps each fact | He said "I'm not prepared to rule right now, would need more context and/or research" (`docs/working-reset/phase-1/the-walk.md:201-202`). The old plan recorded that both answers are sound and differ in certificate size and in whether the check stays local, and that the coverage job the naming would do is the one he gave the catalogs (old:280). So the two checkers are given that ruling and the internals rule of § 4.2 as inputs. If it remains, it is asked after step 10's write-up supplies the context |
| 9 | Is an event `ensure` ingress validation, a premise, both or neither | The approved philosophy names ensures "on specific events" (`docs/philosophy.md:20`), and want:143 says what ingress evaluates for an argument. The two checkers decide whether those settle it |
| 10 | The decimal rounding model | Only if step 2's research shows a choice an author can see (precedent:187) |
| 11 | Whether two non-terminal states that loop with no way to completion are a dead end | want:152 and `docs/philosophy.md:44` settle the general question; only this narrow case may remain (req:742-745) |
| 12 | Whether `from any` means one row per state | Only if the two readings differ in something an author or caller can observe |
| 13 | Approximation stated per operation in place of the split between exact and approximate types | Only if a checker finds his words; it touches the philosophy (rulings:159) |
| 14 | A non-linear rule that field bounds would prove and want:135 rejects | Only if step 6's write-up finds such a rule (§ 2.2) |
| 15 | The reachability standard | Only if step 9's write-up shows want:151 cannot be met as written (§ 2.2) |

**Two conversations**, as `CLAUDE.md` requires before any design of new language. They are not question
sheets and do not count against the cap below.

| Conversation | Steps that wait | What they write meanwhile |
|---|---|---|
| Bounds on temporal point types (proof-model:360) | 2 and 5 | Both alternatives canon already has: guards only, and a representable-range fact checked at ingress |
| The two open language questions in `docs/language/collection-types.md:1296-1300`: how a quantifier binds, and whether the ordering keywords are reserved more widely | 3 and 5 for the binding; no step waits on the keywords | The quantified-rule rows for both binding shapes |

Each arrives with the canon text quoted and no shape proposed. Both are raised with the first batch.

**The cap: fifteen sheets in three batches**, seven of them certain. The cap is confirmed from the count
of candidates when step 1 finishes. If a sixteenth candidate survives its checks, the filter is run
again with fresh checkers; if it still stands, one notice asks him whether to raise the cap, and nothing
else is added to his reading.

**Not asked, because a ruling or his text already answers it:**

- How much proof power the design states: the full intended power (rulings:43).
- Whether a proof may use that earlier rows did not match, that the entity is in a given state, a
  computed field's formula, magnitude reasoning for divisors, and where proof stops: these differ only in
  proof power, so they are design work in steps 4 to 6.
- The general definition of a dead end, and builder metadata on constructs: settled by design argument
  and by a recorded default. The "pending owner ruling" label on the second came from an agent-written
  commit (review:174).
- A machine counterexample search during the design: rule 1 puts it after the design. If he wants it
  sooner, that is his to say.
- The `Fault` deferral: his words stand and nothing is asked. One line tells him it is the one design
  item left to the build (§ 5).
- The presence test missing from his example's guard at want:75: a consequence of the presence rule he
  marked his. It is shown in the first batch. The want document itself is not edited.

### 6.3 Sign-offs

Three, and agent reviews never stand in for them (rulings:184). Each read says what the agents did not
cover.

1. **This plan**, before step 0. He reads a summary of at most two pages, written from this page by an
   agent that did not write it, in plain prose with no codes or row numbers, and passed through the
   `lint` vocabulary and code checks by hand until the script exists. It states the goal, the steps, what
   is kept and replaced from the old plan (§ 9), and everything in § 6.5. The summary is a reading aid
   and holds no rule; where it and this page differ, this page stands and the difference is a defect.
2. **The proof model**, once steps 1 to 11 have finished. He reads his bank example worked under the
   finished model, the refused list with the reason for each refusal, any ruling an agent proposes to
   retire (§ 5), and the changes to canon that trace to nothing he wrote or ruled, capped and with
   citations (review:34).
3. **The finish**, after step 15. He reads the seven counts, the same four-group sort of the whole
   change, any ruling still listed for retirement, and the proposed edits to `CLAUDE.md` and, if any
   question's answer required one, to the philosophy. Philosophy edits happen only on his approval.

### 6.4 Notices

He hears from the work only when one of these happens. Each notice is one entry in `notices.md`, says at
its top whether it needs something from him, and is pushed to him where step 0 showed that works.

| Condition | What he gets | What continues it |
|---|---|---|
| A batch of questions is ready | The sheets, the dropped list, the sample's error rate | His answers. Work that does not depend on them keeps running |
| A step has arguments a second author failed on, and nothing else it can run | One sheet for the step: each argument, its counterexamples, the three choices | His choice for each. Other steps keep running |
| The whole-model, cascading-repair, found-faster-than-closed or records rule fired | One sheet with the counts and the cases | His words (§ 4.5) |
| A run failed or made no progress | The literal error or the unchanged counts | The same start line, which permits one trial run |
| A weekly usage limit paused work | Which model, what is paused, when it resets if known | The same start line after the reset |
| A step finished, or can go no further without him | What it produced, the read for a sign-off if one is due, which steps the start line will run next | The same start line, or his answer |

With the notice that step 1 has finished he also gets, for information: the number of arguments, the
least number of agent runs that implies, and the expected number of restarts for each line of work.
Nothing is sent on a schedule.

### 6.5 How often this plan reaches him

Rule 5 is checked against this count. `status` prints the running total of each kind in every notice.

**What is certain.**

| What | How many |
|---|---|
| Reading the two-page summary and saying yes to the plan | 1 |
| Saying yes or no to the exact permission rules for unattended runs (step 0). No agent can approve these | 1 |
| One line: may the memory file that teaches a reversed ruling be corrected | 1 |
| Question sheets in the first batch | 7 |
| Conversations on new language | 2 |
| The proof-model sign-off | 1 |
| The finish sign-off, with the `CLAUDE.md` edit | 1 |
| Start lines, one for each step | 16 |

That is fourteen decisions or reads and sixteen start lines.

**What may be added, and what bounds each.**

| What | Bound |
|---|---|
| Conditional question sheets | Eight candidates; the cap of fifteen sheets |
| Approval of a philosophy edit | At most three known causes: a "no" on the clock question, and the two memory-only rulings if his words are found |
| Rulings listed for retirement at a sign-off | One line each, read inside a sign-off he is already doing |
| Second-author sheets | One per step at most, so no more than eleven, however many arguments fail |
| Whole-model sheet | Once for the checkpoint; after that only when more than a third of finished arguments have failed with their first author |
| Cascading-repair, found-faster-than-closed and records notices | One per firing, per section or step; each fires on a stated number, not on an agent's judgement |
| A notice asking to raise the question cap | Once per extra candidate that survives two filters |
| Edits to this plan after adoption | Each needs his words; none is planned |
| Restarts after a failed run or a weekly limit | Not bounded by the plan; each is the same start line |
| Restarts under the fallback arrangement of § 7.4 | One for every five runs. With A arguments and S sections that is about (A/2 + S)/5, since an attack run moves eight arguments three rounds and a writing run is one section (§ 7.5). The number is printed in the step 1 notice. If step 0 has to adopt the fallback, its notice says so before any design work starts |

---

## 7. How the work runs

### 7.1 A workflow per kind of work

Each kind of work is a Claude Code workflow script: plain JavaScript that starts agents in a fixed order.
The order of work is in the script. No agent chooses what happens next. A run ends for one of three
reasons: the step's finish condition is met, a stop rule fired, or the run reached its spending limit.

### 7.2 Models, pinned by name per role

| Work | Model |
|---|---|
| Proof-model author, one section per sitting with a hand-over note | Fable 5.2 |
| Attackers, flaw classifier, repair confirmer, fact-checker, adjudicator, question checkers, hand walk | Opus 5.5 |
| Second author for a failing argument | Opus 5.5, with some attackers on Fable 5.2 for those rounds |
| Deciding conflicts; internals answers | Opus 5.5, one to decide and one to attack |
| Writing the list of flaw kinds; adding uncited dependencies; reading canon diffs for uncited uses of his words; width files; the document-by-document pass of condition 5 | Opus 5.5, never the author of the text read |
| Merging plain copies; extracting lists; inventories | Sonnet 5.5, and scripts where possible |
| The scripts in step 0 | Opus 5.5 to write; a different agent writes the tests; a third runs them |

Every output file records the model and the run identifier, and a script rejects output from a model
other than the one pinned for that role. That rejection works only if step 0's test 5 shows the model
can be read. If it cannot, the file records the model as requested and not observed, the rejection
cannot be made, and § 10 lists that as a known limit. No weaker model is ever put in a stronger one's
place to keep going, and no model is changed without his say (rulings:189).

### 7.3 Progress is saved to files

- Every agent writes its result to a fixed-form file in the repository before it returns. A piece is done
  when its file exists in that form and is committed.
- The smallest pieces: one list extracted, then checked; one question checked; one section written; one
  round of one argument; one fact merged; one conflict decided; one document carried; one document read
  for condition 5; one example through one stage or operation.
- The next run starts from the files. `next-run` prints the next run; the session runs what it prints and
  picks nothing itself.
- Each line of work has its own git worktree. Files shared between lines are written only by
  `locked-write`, which takes a lock that carries a heartbeat so a dead run can be told from a live one.
  Half-written files found at start are set aside, not read.
- Anything a run needs from outside the repository, such as memory files, is copied in first with the
  date of the copy.

### 7.4 He starts each step with one line

`Run the design-completion plan` — the same line every time, typed to a session opened in the
repository. It takes no arguments. Where to continue is read from the files. A new step begins only after
he types the line following the notice that the previous one finished, so nothing starts that he did not
start. The start command records each start, which is what clears the "no progress" rule for one trial
run. Step 0 is the exception in wording only: the scripts do not exist yet, so its line is
`Carry out step 0 of the design-completion plan`.

The line runs a small skill or command, not a saved workflow (review:82). Step 0 builds it in one of two
arrangements and keeps the one that passes test 8:

1. **Preferred.** The skill starts a shell loop in the background. The loop starts a fresh unattended
   session for each run, so no session's context fills up and no session carries anything from one run to
   the next. When the loop exits, the session that started it shows the notice.
2. **Fallback.** The session itself starts run after run, capped at five in a row, then stops with a
   notice (review:49).

### 7.5 Spending limits and usage limits

- A run is given a fixed number of pieces and a ceiling on output tokens. The piece counts are in
  `settings.json`; the ceilings are in `measured.json`. The ceiling is passed to the script in its
  arguments and compared against tokens spent (review:81). When either is reached the run finishes the
  piece in hand, records it and ends. Starting values: an attack run advances up to eight arguments by up
  to three rounds; a writing run is one section (v2:531-538). After step 0 the ceiling is one and a half
  times the measured cost of the run's pieces, worked out again by `status` from the first three real
  runs of each kind.
- The Workflow tool's own limits, as its authoring reference states them and not yet tested: at most
  1,000 agents in a run and 16 at once (v2:527). One round of one argument is two attackers, a classifier
  and a fact-checker, so twelve rounds are at least 48 agent runs per argument before any repair.
- **A short usage window**: the run waits for it to reset and continues by itself (review:50).
- **A weekly limit**: the work that needs that model pauses, records what it finished, and writes one
  notice. He restarts with the same line after the reset. If only the Fable share is used up, writing and
  repairs pause, and so do attack rounds on any argument waiting for a repair (review:51).
- Extra paid usage is never drawn on silently; step 0 checks the setting and the scripts stop on their
  own.
- Nothing here holds work back to save usage. The limits bound what one failure can waste.

### 7.6 Untested, and tested in step 0

Each of these is an assumption about Claude Code as it exists today. Step 0's numbered test settles each
before any design work depends on it. Every test has a fallback that lets step 0 finish, so a failed
test changes how the work runs and never leaves the plan with no way forward.

| Assumption | Step 0 test | If it fails |
|---|---|---|
| Each script gives known answers and stops on bad input | 1 | The script is repaired; this is bounded by `step0-guard`, whose notice reaches Shane |
| Each stop rule fires, stays fired and clears by its route | 2 | The same |
| Workflow scripts run start to finish against stand-ins | 3 | The same |
| A workflow can fix each agent's tools so design agents cannot compile | 4 | The Precept MCP server is switched off for design sessions in `.claude/settings.local.json`, which `CLAUDE.md` says decides which servers are enabled. That is a settings change and is made only on his yes, asked with the permission rules |
| A Fable-pinned workflow agent can hold the author's reading list, about 844 KB (v2:288) | 5 | Sections are cut smaller and the type documents are read row by row |
| The model an agent ran on can be read and recorded | 5 | The pinned name is recorded as requested and not observed; the rejection in § 7.2 cannot be made; listed in § 10 |
| The four first kinds of work can be run and costed | 6 | The kind that failed starts with the piece count alone as its bound and is costed from its first three real runs |
| A fresh unattended session can run a workflow with pinned models and commit | 8 | The fallback arrangement in § 7.4 |
| A stopped run resumes from the files without repeating work | 7 | Every run is cut to one piece, so a stopped run can lose or repeat at most that piece |
| Two worktrees and a locking script keep two lines of work apart | 9 | One line of work at a time |
| A usage limit can be told apart from any other failed agent | 10 | Several empty results in a row stop the run as a failure |
| A notice can be pushed to him | 10 | Notices appear only in the file and in the session; test 10 records that outcome and passes |
| An unattended run meets no permission prompt under rules he approved | 11 | If he declines the rules or they do not work, runs happen in a session he has open, under the fallback arrangement in § 7.4, and the notice from step 0 says what that costs him |
| The token ceiling can be enforced from the script's arguments | 3, 6 | The piece count alone bounds a run |

---

## 8. Safeguards against the recurring failures

The failures file lists twenty-two, and finds that every earlier fix was a rule in a document enforced by
the same kind of agent it was meant to restrain; the fixes that held were checked by a machine or an
outside party (failures:603-609). So each safeguard below is a file, a script that reads it, and a point
where the script stops work.

| # | Failure | Safeguard |
|---|---|---|
| 1 | The unfinished part of the design was never the part being worked on | Steps 1 to 11 are the proof model and start first. `next-run` holds step 14 until step 1 has text in canon, and pauses it whenever its changed lines run ahead of the proof model's by the set multiple. Step 14 may not write in the two proof-model documents |
| 2 | Plans replaced the work, each a new copy | One page; `plan-lint` finds exactly one live plan; an edit needs his words in `answers.md` and a five-rule check file. The summary he signs holds no rule |
| 3 | Finish lines that were searches with no end | Every finish condition names a list file and a script. "A fresh look finds nothing" is not a finish condition anywhere. A fresh read only adds rows, and the "found faster than closed" rule halts a step whose rows keep growing and takes the trend to him |
| 4 | Decided but never carried into canon | `rulings.jsonl` and `rulings-check`; every row of `answers.md` becomes a ruling row; condition 4 of § 1 |
| 5 | Agents manufactured owner decisions | Two blind checkers and a five-rule check per question; the dropped list and an error rate from a fresh agent's re-check; a cap |
| 6 | A ruling widened past what he said | `answers.md` word for word; a width check under `uses/` for every use; a fresh reader of each canon diff for uses that cite nothing; a stop rule; no ruling retired without his later words |
| 7 | Agent labels treated as his authority | `precedence.md` with the eighteen labels marked; the label check in `lint`; the decision index marked |
| 8 | The design narrowed to make something finishable | The closed list of refusal reasons, with first-build rulings barred and caps allowed only with an attacked argument; every refusal attacked as an argument; four deferral groups; no removed line without a destination; hard problems close only with a design; the whole-model sheet does not offer a weaker guarantee |
| 9 | Build state, samples or catalogs used as evidence | No compile tools for design agents; no agent that compiles writes examples; the citation check in `lint`; lists from `docs/language/` only |
| 10 | Claims about canon without reading it | `cite-check` against committed text; silence claims need search terms and a history search |
| 11 | Arguments called finished before anyone attacked them | Twelve rounds; a concrete counterexample in every attacker file; repairs confirmed by a third agent; text behind a pending marker |
| 12 | Stop rules that never fired | Every stop rule is computed by `stop-rules` and enforced by `next-run`; each has a stated way to clear and no other; the thresholds change only with his words; the kinds of flaw are fixed before any round; step 0 shows each rule firing, staying fired and clearing |
| 13 | Findings recorded, then work carried on | `findings.jsonl` blocks any piece whose inputs it names; dependents reopen from dependencies no author wrote; findings and open questions must be closed at the finish; an open row whose owner step has finished fails `status` |
| 14 | Tracking pages and duplicated facts went stale | The status file is generated whole; one pointer form resolved by script; a document-by-document pass for restatements a script cannot see; the memory pointer updated in step 0 |
| 15 | Work held where the next session cannot see it | Step 0 commits the inputs; a script fails any path outside the repository but the one named; answers are committed in the sitting |
| 16 | Agent vocabulary and compressed questions | `prompt-check`; the vocabulary and code checks in `lint`; questions as a plain consequence with a worked case; no lookup codes on this page |
| 17 | The main session acted as the authority | `next-run` chooses; the session writes no canon, no script and no test; `trailer-check` reverts a commit that did not come from a recorded run; a script decides when a batch of questions goes out; a fresh session per run where step 0 shows it works |
| 18 | Enumeration covered only the cases tripped over, then overcorrected | One closed, hashed list per family from `docs/language/`; one coverage row per member; never the product of two lists; lists re-extracted after any step that changes `docs/language/`, with a stated rule for which rows reopen |
| 19 | An engineering wall read as a question about what Precept is | One page of engineering first for each hard problem; identity questions fail the five-rule check, and the whole-model sheet passes through it |
| 20 | Records grew faster than the work | Fixed-form records with a size cap; one review per artifact; a stop rule on the ratio of record lines to lines changed in canon, per step; a bound on step 0's scripts |
| 21 | Results reported that nobody observed | An empty result is "not done"; counts by script only; every output records model and run |
| 22 | Each correction swung to its opposite | Any change to the plan or any new rule is checked against all five rules and the rulings file at once, by an agent that did not propose it, before it is adopted |

---

## 9. What is kept from the old plan's rulings

Shane marked the old plan's process rows "mostly mine" (rulings:87). Each is kept, replaced or moot here.

| Old ruling (old:229-251, :486-503) | Here |
|---|---|
| A branch, not a copied folder; promoting it gates nothing | Kept |
| The certificate checker is in scope, its load-time half included | Kept; step 10 |
| Coverage rests on the catalogs | Kept; step 12 specifies the entries. This work's own lists come from `docs/language/` |
| A bounded counterexample search is in scope | Kept: designed in step 8, built after the design |
| The procedure walks the language's expression forms; the coverage grid is generated | Kept; step 6 |
| Vocabulary: counterexample, case | Kept |
| A closed list stays closed, with a recorded hash | Kept; lists, the contents list, the proof-model sections. § 4.7 says how one reopens when canon changes under it |
| Three attempts that each turn up a different problem stop the work | Kept, applied by script per argument (§ 4.5) |
| Thirteen phases; only phase 1 sliced | **Replaced** by the sixteen steps |
| Confirm, narrow or revert at phase 8 | **Replaced** by the proof-model sign-off. Narrowing the language happens only through `/design` with his word |
| The build phase carries no size | Moot here; nothing in this plan carries a size |
| `bugs.md` and the want document stay live | Kept |
| Six rows dated "Settled 2026-07-26" with no quotation (rulings:225) | Not treated as his rulings. The step vocabulary closing when the procedure is written is kept on its reasoning. "No numeric threshold" is replaced by the whole-model rule in § 4.5, which he can change. The rest concern the build |

Not taken from the old plan: build checks inside design phases; "the first honest fragment … smaller"
(old:1645); overflow as "an authorized exemption … not a hole" (old:333).

---

## 10. Known limits

- **The proof model may not close.** The want document itself says feasibility is the open question
  (want:183). The checkpoint in step 4 and the whole-model rule exist to show that as early as the order
  of work allows, and to bring it to Shane once, with counts and with choices that keep the guarantee.
- **Paper checks can share a blind spot.** Attack rounds and the hand walk are agents reasoning on paper.
  The certificate checker, which is code, is the eventual backstop (v2:162).
- **The size is not known.** It depends on the number of arguments, which is known when step 1 finishes
  and fixed when step 6 closes the contents list. Full proof power means more arguments than the "couple
  of dozen" the old plan expected (v2:251). § 6.5 says what this does to the number of times he is
  reached.
- **Some checks still rest on agents.** A restated fact in different words, a use of his words that
  cites nothing, and a dependency that is relied on without a citation are each found by a fresh agent
  reading, not by a script. Each produces a list file a script counts, and none is done by the author of
  the text, but a reader can miss one.
- **Nothing mechanical proves a row of `answers.md` is his.** The session he is talking to writes it.
  Every notice shows him the rows added since the last one, which is the only check.
- **The model an agent ran on may not be observable.** If step 0's test 5 falls back, pinned models are
  requested and not confirmed.
- **The sources behind this plan were not all read end to end.** The proof-model file read the proof
  engine's strategy sections and the four type documents only in part and searched no history
  (proof-model:478-481). The consistency file did not re-verify the executed register rows one by one
  (consistency:242-246). The precedent file's outside sources are search summaries, and two of its central
  readings are its own inferences to be tested (precedent:305). Step 0 turns these into rows of
  `findings.jsonl` with owner steps, so they are worked and not forgotten.
- **How the work runs is untested** until step 0 passes (§ 7.6).

---

## Where this came from

This page replaces the second-revision draft (v2) and, on adoption, the thirteen-phase plan (old). It
keeps v2's scope, its script-printed counts, its saved progress, its twelve attack rounds and its
one-line starts. It takes back from the old plan the whole-model stop, the "different problem each
attempt" test, concrete counterexamples in every attack, two drafters, owners for every deferral, the
trace of what justified each change to canon, the sampled error rate for filtered questions, and the
per-type algebraic table. It applies the findings of the adversarial comparison (review) as the
requirements file disposes of them (req:689-772), and it is ordered by the technical dependency order in
the proof-model file (proof-model:416-446). The twenty-two failures are from the failures file, the
rulings from the rulings file, and the conflicts from the consistency file.
