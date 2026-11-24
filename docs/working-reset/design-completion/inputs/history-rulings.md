# Shane's rulings — every one found, where it is recorded, and whether canon carries it

Compiled 2026-11-22 against HEAD `2f126a3b`, branch `spike/Precept-V2-Radical-reset`. Nothing in the
repository was edited.

## How to read this

**What counts as his ruling.** A row is in the main tables only if one of these holds: (a) his words are
quoted in a record, (b) he marked it *mine* at the 2026-07-26 walk, or (c) a record says he said yes to a
plainly stated proposal and gives the date. Commit messages, "Locked", "owner decision", "Settled" and
"ratified" written by agents are not enough on their own — every commit here is authored under his
identity by an agent (`docs/working-reset/phase-1/decision-register-FINDINGS.md:23`). Items that carry
only such a label are in the second table.

**Sources read.** `docs/Working/what-i-want-2026-07-16.md` (the want document, abbreviated *want*);
`docs/working-reset/phase-1/the-walk.md` (*walk*); `phase-1/decision-register-FINDINGS.md`;
`phase-2/trace-walk-rulings.md` (*trace-walk*); `phase-2/five-items-walk-2026-08-27.md` (*five-items*);
`phase-2/want-amendment-marking.md`; `compiler-readiness-plan.md` § 3 and § 4 (*plan*); every file in the
memory folder (*mem:* followed by the file name stem). Phase-2 file paths are relative to
`docs/working-reset/`.

**Status column.**
- **in canon** — a canonical document (philosophy, `docs/language/`, `docs/compiler/`, `docs/runtime/`,
  `docs/tooling/`, `docs/compiler-and-runtime-design.md`) states it; the line is cited.
- **partly** — canon states part of it; the missing part is named.
- **not in canon** — no canonical sentence found. For rulings about *how the work runs*, canon is not where
  they belong; the column then says where they are written instead (CLAUDE.md, memory, or the plan).
- **superseded** — a later ruling of his replaced it; the replacing row is named.
- **unclear** — the record does not let me tell whether it is his, or canon is ambiguous.

"Not found" means a search of the canonical folders for the obvious wordings returned nothing; it is not
proof of absence, and the search terms are in the row.

---

## 1. The 2026-11-13 restart

| # | Ruling, in his words where they exist | Recorded at | Status | Canon |
|---|---|---|---|---|
| 1 | Finish the design first, then acceptance criteria, tests, code, code reviews. *"my goal is to complete the design, so that we can begin coding."* Rejected a code-first path the same day. | mem:project_design_first_restart_2026_11_13:11–15 | not in canon (memory only). The plan at HEAD still reads as the 2026-08-27 phase-2 board and has not been updated for the restart (`compiler-readiness-plan.md:26`) | — |
| 2 | The design keeps all its detail, compiler and runtime internals included. *"you're suggesting to finish the design by removing detail and stripping the docs down? that doesn't work for me."* | mem:project_design_first_restart_2026_11_13:17 | not in canon (memory only) | — |
| 3 | Where one fact is stated in several canonical documents, one document owns the full statement and the others point to it; nothing is dropped. | mem:project_design_first_restart_2026_11_13:19 | partly — the convention exists and is applied in places (`docs/language/primitive-types.md:740`, `docs/language/collection-types.md:1549`); eleven of fifteen canon-against-canon rows await exactly this repair (mem:project_reset_phase_1_2_2026_07_28:91–94) | `primitive-types.md:740` |
| 4 | The design states the full intended proof power. *"it describes the full proof power, is that not clear from all the forensics and what I want"*. His 2026-07-26 "minimal proof set for the MVP" is about the first build, not the design. | mem:project_design_first_restart_2026_11_13:21 | in canon as the target — the induction and no-deferral statement is at `precept-language-spec.md:268`. `docs/compiler/proof-engine.md:2614–2616` records that the obligation vocabulary has no establishment or preservation member, stated as a gap against the target | `precept-language-spec.md:266–268` |

## 2. The want document (his, 2026-07-16; the 2026-07-19 amendment approved by him 2026-08-26)

He confirmed on 2026-08-26 that he reviewed and approved everything in it: *"lets assume that I didn't
write it directly, but did review it"* (`phase-2/want-amendment-marking.md:21–31`). So every line carries
his authority, though `want:135` is not his verbatim words.

| # | Ruling, in his words | Recorded at | Status | Canon |
|---|---|---|---|---|
| 5 | *"In an ideal world, the compiler proves or rejects all business rules."* | want:14 | in canon | `precept-language-spec.md:266–268`; `docs/philosophy.md:42`, `:109` |
| 6 | Proof by induction over reachable configurations: base case (default configuration and every initial event establish every rule), inductive step (every handler preserves every rule) from four premise classes. | want:16–19 | in canon as the target; mechanism gap recorded | `precept-language-spec.md:268`; gap at `proof-engine.md:2614–2616` |
| 7 | *"A rule's obligations attach to every write site of every field the rule mentions."* | want:20 | not found in canon (searched "every write site", "every field … mentions", "symmetric") | — |
| 8 | *"Modifiers are sugar for rules."* One constraint mechanism underneath. | want:22 | in canon | `precept-language-spec.md:1143` |
| 9 | A rejection names the missing premise; acceptance means no reachable operation can violate any rule. | want:24 | in canon | `philosophy.md:99`; `precept-language-spec.md:268` |
| 10 | The premises are what the runtime enforces — governance, not deferred proof. | want:26 | in canon | `precept-language-spec.md:274–276` |
| 11 | The "What must not compile" table: each deletion of a premise must be a compile-time rejection with a teachable message. | want:110–131 | partly — individual consequences are in canon (default violating a bound `precept-language-spec.md:213`; dead row `docs/compiler/diagnostic-system.md:189`; unreachable and dead-end at Error `diagnostic-system.md:193`); the table itself is not carried as a canonical list | `diagnostic-system.md:188–193` |
| 12 | Non-linear rules: proven only from a guard or argument constraint that states the post-state condition directly, otherwise rejected; never a post-mutation sweep, never a solver. | want:105, :123, :135 (approved 2026-08-26, `phase-2/trace-walk-rulings.md:35–38`) | in canon | `precept-language-spec.md:270` |
| 13 | Simple language, no loops, no functions; *"the surface is the negotiable part."* | want:137, :189 | in canon | `philosophy.md:113` |
| 14 | *"require the author to extend constraints to the entry points, so that ingress validation stays simple."* | want:139 | partly — governance at ingress is stated (`precept-language-spec.md:274`); the requirement that the author must extend constraints to entry points is not stated as such | `precept-language-spec.md:274` |
| 15 | Ingress is exactly two points — event arguments and editable-field writes. Restore is not ingress; restored data is trusted. | want:141 | in canon | `precept-language-spec.md:278`, `:2005`; `docs/runtime/evaluator.md:1670` |
| 16 | Post-MVP, the restore path handles schema evolution. (The parenthesis was pinned to restore by an editor pass, `decision-register-FINDINGS.md:78`.) | want:141 | not in canon as one position — canon disagrees with itself: `docs/compiler-and-runtime-design.md:718` puts definition migration out of scope for v1 and on the host; `docs/runtime/runtime-api.md:277`, `:906` and `evaluator.md:1757` give it to the restore path. Now a register obligation (`phase-2/five-items-walk-2026-08-27.md:95–107`) | — |
| 17 | At an editable-field write, the field's modifier-rules and every rule that mentions the field are evaluated; that check is the premise that closes editable write sites. | want:143 | not in canon, deliberately: on 2026-08-26 he ruled the narrowing "and nothing wider" out of canon and canon now says nothing about what a direct edit evaluates (`phase-2/trace-walk-rulings.md:42–48`). The question itself is open on § 4 (`compiler-readiness-plan.md:282`) | — |
| 18 | Every state is reachable; no dead ends unless marked `terminal`. | want:151–152 | in canon | `philosophy.md:44`; `docs/compiler/graph-analyzer.md:452` |
| 19 | Absence of a row is the enforcement; absence expresses inapplicability, `reject` is for resolvable refusals. | want:153–154 | in canon | `precept-language-spec.md:1926`, `:1935` |
| 20 | Dead rows — a guard that can never be true — are rejected. | want:155 | in canon | `diagnostic-system.md:189` |
| 21 | Runtime provides inspect, respects the business process, keeps immutable versioning. | want:159–163 | in canon | `philosophy.md:24`, `:46` |
| 22 | *"fault-prevention checks are omitted at evaluation time."* Only the declared premises run. | want:165 | in canon | `precept-language-spec.md:278` |
| 23 | *"In an ideal world there is no deferral."* | want:171 | in canon | `precept-language-spec.md:268` |
| 24 | The guarantee is uniform — *"no tiers of trustworthiness to reason about."* | want:173 | partly — the no-escape-hatch statement covers it (`philosophy.md:109`); the "no tiers" claim is not stated in those words | `philosophy.md:109` |
| 25 | The compiler computes the missing premise and shows it; *"It never inserts the premise itself."* | want:179 | partly — naming the premise is in canon (`philosophy.md:99`); "never inserts it" not found (searched "insert", "premise the author never") | `philosophy.md:99` |
| 26 | Trade some expressibility for proof; *"the guarantee is non-negotiable; the surface is the negotiable part."* | want:185–189 | in canon | `philosophy.md:109`, `:113` |
| 27 | The proof boundary is the single precept for MVP; message passing and saga precepts later. | want:191–195 | partly — canon states no cross-precept resolution (`docs/compiler/name-binder.md:92`, `:374`), not the proof boundary as such | `name-binder.md:374` |
| 28 | A certificate is emitted; one small checker runs as a compiler stage and as a load-time gate in the runtime. | want:199–206 | in canon | `docs/compiler/soundness-and-coverage.md:159–161`; `philosophy.md:48`; `runtime-api.md:254` |
| 29 | *"the premise list **is** the runtime's validation surface"*, derived from the proof. | want:210 | not in canon — recorded as an open question (`docs/runtime/README.md:50`; `docs/runtime/precept-builder.md:812`, `:866`) | — |
| 30 | Inspect cites provenance, not just verdicts. | want:211 | not in canon — recorded as open (`docs/runtime/README.md:38`) | — |
| 31 | The philosophy's Fire sentence needs an owner-approved rewording. | want:215 | in canon — the sentence is gone from the 2026-07-28 rewrite (no hit for "evaluates all applicable" in `philosophy.md`) | `philosophy.md:42`, `:46` |
| 32 | Rules that reference the current moment: *"Deliberately left open."* Reaffirmed 2026-08-27: *"it is undecided, add it to the list"*. | want:219; `phase-2/five-items-walk-2026-08-27.md:121` | not in canon — canon contradicts him: `docs/language/temporal-type-system.md:568` locks a decision accepting such rules and `:577` gives `rule now() - IncidentAt <= '72 hours'`. Plan says canon's locked decision stands until he rules (`compiler-readiness-plan.md:289`) | — |
| 33 | The promise across precepts is left open. | want:220 | in canon as out of scope | `name-binder.md:374` |

## 3. The phase-1 walk, 2026-07-26 — items he marked *mine* or ruled

| # | Ruling, in his words | Recorded at | Status | Canon |
|---|---|---|---|---|
| 34 | The reset plan's twelve process rows: *"mostly mine"*. | walk:96–100 | not in canon (process; plan § 3 at `compiler-readiness-plan.md:229–242`, whose own note at `:244–245` says the rows carry no quotations) | — |
| 35 | Coverage rests on the catalogs: *"the idea to use the catalog was mine"*. Overrode the printed correction. | walk:101–112 | partly — CLAUDE.md:89–109 states the catalog goal and suspends it; no canonical description of a checker that asks the catalog what each thing owes | — |
| 36 | Evidence rules — samples never measure anything: *"agents continually present me arguments based on an assumption the sample corpus is comprehensive, I have to keep reminding them it's not"*. And 2026-07-25: *"the samples are not comprehensive, the code is not complete, and the catalog is not correct. it should be the canonical docs that are comprehensive."* | walk:114–128; mem:feedback_corpus_is_witnesses_not_denominator:62 | not in canon (process; written in `CLAUDE.md:99`, `:117–125`) | — |
| 37 | One written line reached by several paths owes one proof per path: *"yes"*. | walk:135–151 | not in canon (searched "route", "occasion", "paths") | — |
| 38 | A transition into a state that writes nothing still owes that state's residency constraints: *"it has to be one, otherwise the constraint could be violated at runtime"*. | walk:212–235 | partly — residency is defined (`precept-language-spec.md:1892`); the obligation on a no-write entry route is not stated | `precept-language-spec.md:1892` |
| 39 | Writing a field that only a rule's `when` reads takes on that rule's obligation: *"one, clearly"*. Earlier, 2026-07-20, on all three ways a rule mentions a field (condition, `when`, computed fields transitively): *"yes, definitely -- adopt all 3."* | walk:237–259; mem:project_boundary_define_what_i_want_2026_07_16:174 | not in canon (searched "activation", "when guard reads", "transitively") | — |
| 40 | Non-fault rules are judged on the configuration the handler commits; faults are prevented at the point of evaluation. *"this should not compile. rules only have to hold at the end of the handler, but faults must be prevented unequivocally."* Restated: *"Faults must always be prevented by the compiler. Other rules that don't prevent faults only need to be true at the end of the handler."* | walk:261–308 | not in canon. Sequential proof flow, which the fault half applies, already is (`precept-language-spec.md:233`); the end-state rule and the split are not (searched "end of the handler", "intermediate", "point of evaluation") | `precept-language-spec.md:233` (related) |
| 41 | The compiler works with the expression as written; widening is post-MVP. *"one, with the justification that we can always improve it later beyond MVP to accept more spellings or normalization"*. | walk:352–375 | not in canon (searched "as written", "rearrang") | — |
| 42 | Overflow — integer, temporal, number-lane infinity — is deferred past the MVP, unguarded at both ends: *"yes"*. | walk:394–421 | in canon | `precept-language-spec.md:272`; `primitive-types.md:697–699`; `soundness-and-coverage.md:28–33` |
| 43 | A declared premise decides prove-versus-govern, not where the value came from. Chose "refuse until the author writes the condition" on the worked case; of the canon sentence: *"that was derived, I did not write that exactly."* | walk:423–452 | in canon — the provenance sentence formerly at `compiler-and-runtime-design.md:125` is absent at HEAD (no hit for "raw external", "own ingress slot"); spec states the four premise classes | `precept-language-spec.md:268`, `:276` |
| 44 | The obligation-discharge matrix was to become the canonical proof-engine definition: *"yes"*. | walk:454–463 | superseded — by his reset plan: the discharge procedure is a walk over expression forms (`compiler-readiness-plan.md:239`, `:251`) | — |
| 45 | Message holes: an interpolation hole is a read; optional reads enrol a presence obligation, refused uniformly. Marked *"mine"*; earlier, 2026-07-24: *"refuse uniformly, close the friction question."* | walk:467–480; mem:project_presence_tolerant_rendering_2026_07_24:92 | in canon | `precept-language-spec.md:1561`; `soundness-and-coverage.md:263` |
| 46 | Same-basis periods order: *"mine, I ruled that we should make same-basis periods orderable, which was against canon."* | walk:482–504 | in canon (Decision 14 rewritten) | `precept-language-spec.md:1433`; `temporal-type-system.md:1481–1483` |
| 47 | Currency codes are uppercase only (ISO 4217 against UCUM collision): *"yes"*. | walk:527–529 | in canon | `docs/language/business-domain-types.md:1804–1806` |
| 48 | The 2026-07 reword of philosophy.md:49 was approved: *"yes"*. | walk:531–546 | superseded — by the 2026-07-28 whole-document rewrite (row 61); its substance survives at `philosophy.md:38` | `philosophy.md:38` |
| 49 | Three severities rise to Error — required-state dominance, unsatisfiable rule, contradictory rule: *"I ruled that these should be elevated to error"*. | walk:548–563 | in canon | `diagnostic-system.md:188`, `:193` |
| 50 | Philosophy is aspirational; code is validated against it, not the reverse: *"philosophy.md is a statement and guiding principle for where we are going. It's aspirational, the code will be made eventually consistent and we need to validate the code against it, not the other way around."* | walk:574–582 | in canon | `philosophy.md:7` |
| 51 | A rule consumed as a premise must be established and preserved (sample items 30 and 31): *"I didn't write that directly, but it reflects what I asked for"*; *"mine on substance"*. | walk:699–720 | in canon as the target; mechanism gap recorded | `precept-language-spec.md:268`; `proof-engine.md:2614–2616` |
| 52 | A constraint modifier on a computed field may not be used as a premise while nothing establishes it — conditional form: *"those two lines you highlighted, those are mine."* | walk:722–731 | not in canon (BUG-048 in the transient `docs/Working/bugs.md`; the permanent form is open, `compiler-readiness-plan.md:284`) | — |
| 53 | A fault-prone expression inside a reject message is a compile-time obligation: *"yes"* on the corrected basis. Recorded separately as his 2026-07-23 ruling for message holes' value faults. | walk:733–745 | in canon | `soundness-and-coverage.md:262` |
| 54 | `nonnegative` contributing nothing to a field's interval is a defect: *"the quoted line is mine"*. | walk:747–755 | in canon as the principle (one mechanism, `precept-language-spec.md:1143`); the defect is recorded in CLAUDE.md § Use the MCP Tools First | `precept-language-spec.md:1143` |
| 55 | A rule is preserved wherever a field it mentions is written — construction rows, state actions, editable writes: *"yes"*. | walk:757–770 | not in canon (same search as row 7) | — |
| 56 | Create, Fire and Update owe preservation; Restore is exempt because the data was governed when written: *"yes"*. | walk:772–792 | partly — the Restore half is in canon (`precept-language-spec.md:278`); the per-operation duty for Create, Fire and Update is not stated as such | `precept-language-spec.md:278` |
| 57 | Both non-proven outcomes stop compilation, and the printed suggestion is shown: *"yes"*. | walk:794–806 | in canon | `diagnostic-system.md:183` |
| 58 | No author-visible "trust me" escape hatch: *"yes"*. | walk:808–815 | in canon | `philosophy.md:109` |
| 59 | What a refusal claims — a refusal reports the search, and messages are honest about which kind: *"option one — we are going with a minimal proof set for the MVP, but we can and will expand the compiler's proof power over time."* | `phase-4/what-a-refusal-claims.md:142`; `compiler-readiness-plan.md:279` | in canon | `diagnostic-system.md:183` |

## 4. Rulings from 2026-07-28 to 2026-08-11

| # | Ruling, in his words | Recorded at | Status | Canon |
|---|---|---|---|---|
| 60 | "The engine does not fault" is aspirational: *"obviously we can't say that piece of software has no bugs. it's aspirational."* Also, that day: *"a runtime that does not fault is what I want."* | `compiler-readiness-plan.md:272`; mem:feedback_main_loop_not_authority:121 | in canon | `philosophy.md:7`, `:46` |
| 61 | The 2026-07-28 philosophy rewrite (3,309 words to 2,775, four amendments, third guarantee named) is approved; confirmed again 2026-08-26 with its editorial decisions. | `phase-2/trace-walk-rulings.md:12–25`; mem:project_reset_phase_1_2_2026_07_28:298 | in canon | `philosophy.md:3` |
| 62 | Period ordering: ship both the scalar and the collection half — *"build both."* (2026-07-29) | `compiler-readiness-plan.md:274`; `phase-2/period-ordering-design.md:879` | partly — the scalar half is in canon (`temporal-type-system.md:1481–1483`); the collection half is not: `collection-types.md:533` still says `.min`/`.max` legality on pinned periods "does not follow" and is "type error today; open" | `temporal-type-system.md:1481` |
| 63 | The comparator-survey research gate is waived for period ordering: *"waive it, we're built on noda."* | `phase-2/period-ordering-design.md:99` | not in canon (process ruling on one design; does not close `research/INDEX.md:48`) | — |
| 64 | Lock the period-ordering design and file the defects: *"yes lock the design and log the bugs."* (2026-08-11) | `phase-2/period-ordering-design.md:2`; `compiler-readiness-plan.md:274` | partly — the design document is locked; its scalar rule is in canon (row 62); BUG-068 and BUG-069 are in the transient bug list | `temporal-type-system.md:1481` |
| 65 | Who constructs a `Fault` is deferred until the runtime is being built: *"do we need to decide this now? seems like something we can defer until we start building the runtime"*, then *"yes"*. | `compiler-readiness-plan.md:273`, `:437–439` | in canon as a recorded open question | `docs/runtime/fault-system.md:8`; `precept-language-spec.md:1947`; `docs/runtime/README.md` § Open questions (`:28`) |

## 5. The trace walk, 2026-08-26

| # | Ruling, in his words | Recorded at | Status | Canon |
|---|---|---|---|---|
| 66 | The 2026-07-19 want amendment carries his approval: *"i don't remember, I didn't write it by hand, but worked with the agent to revise the sample"* → *"lets assume that I didn't write it directly, but did review it."* Nothing comes out of canon. | `phase-2/want-amendment-marking.md:21–31` | in canon — `precept-language-spec.md:270` stands; no canonical document cites want `:72` or `:135` at HEAD (searched) | `precept-language-spec.md:270` |
| 67 | The "and nothing wider" narrowing of what an ingress check evaluates comes out of canon at every site; canon stays silent until phase 5. | `phase-2/trace-walk-rulings.md:42–48` | in canon (executed `621bde2b`) | — (removal) |
| 68 | The runtime open-question indexes stay: *"agree"*. One entry — `FieldDescriptor.AccessModes` in `descriptor-types.md` — comes out. | `phase-2/trace-walk-rulings.md:50–59` | partly — indexes stay (`docs/runtime/README.md:28`); `docs/runtime/descriptor-types.md:109` still discusses `AccessModes`, and I could not tell whether that is the removed index entry or the later register finding N112 | `runtime/README.md:28` |
| 69 | The `proof-engine.md` paragraph calling the reference-identity rule on `ProofObligation.Site` pointless comes out: *"lets go clean"*. | `phase-2/trace-walk-rulings.md:61–65` | in canon (removed; no hit for "buys nothing" in `proof-engine.md`) | — (removal) |
| 70 | "What the chain rules out" in `diagnostic-system.md` stays; no action. | `phase-2/trace-walk-rulings.md:67–69` | in canon (unchanged) | `diagnostic-system.md` |
| 71 | "The MVP" is an internal build milestone that nobody outside receives: *"yes"*. | `phase-2/trace-walk-rulings.md:71–78` | in canon | `soundness-and-coverage.md:28–33` |

## 6. The five-items walk, 2026-08-27, and 2026-08-28

| # | Ruling, in his words | Recorded at | Status | Canon |
|---|---|---|---|---|
| 72 | A build-state observation is not phase 2's business: *"obviously it can't yet, we haven't built it yet. this is getting ridiculous going around in circles. What is the point of phase 2, I thought we were just trying to get the design aligned to the want document?"* | `phase-2/five-items-walk-2026-08-27.md:12–22` | not in canon (process; `compiler-readiness-plan.md:84–88`) | — |
| 73 | Two canonical documents disagreeing is in scope: *"when did I rule that two documents disagreeing with each other is not a problem? as I understand it, the goal is ensure consistency between the want doc and the canonical docs"*. Then: *"then wide now."* | `phase-2/five-items-walk-2026-08-27.md:23–33` | not in canon (process; `compiler-readiness-plan.md:68–74`) | — |
| 74 | Fix the outcome sentence that declared `ConstraintsFailed` impossible: *"we should fix it"*. | `phase-2/five-items-walk-2026-08-27.md:49` | in canon | `compiler-and-runtime-design.md:863` |
| 75 | Fix the inspection table still showing the removed pass: *"do it"*; leave `evaluator.md:125` alone. | `phase-2/five-items-walk-2026-08-27.md:65–77` | in canon (executed `1969e3e5`) | `evaluator.md` |
| 76 | Schema-migration ownership: first *"record it as a canon-against-canon note"*. | `phase-2/five-items-walk-2026-08-27.md:103` | superseded — by row 73 the same day; it became a register obligation, and canon still disagrees (row 16) | — |
| 77 | Close phase 2 with defects recorded, rather than re-scope or revert. | mem:project_reset_phase_1_2_2026_07_28:164 | not in canon (process; no quotation recorded) | — |
| 78 | Build a compiler-side open-questions index mirroring the runtime one (given as an instruction). | mem:project_reset_phase_1_2_2026_07_28:217–221 | in canon | `docs/compiler/README.md:47` |
| 79 | *"forget about the code for now, this is about getting the design right."* (2026-08-28, on citing code in the register) | commit `2f126a3b` message | not in canon (process; memory index and commit only) | — |

## 7. Earlier rulings on the design, before the reset, in his words

| # | Ruling, in his words where they exist | Recorded at | Status | Canon |
|---|---|---|---|---|
| 80 | Restore is trusted hydration — no re-validation (2026-06-02, recorded as D4). | mem:project_proof_engine_guarantee_contract:113 | in canon | `precept-language-spec.md:278`; `evaluator.md:1670` |
| 81 | The guarantee contract's four parts, named "fault prevention" — he rejected "fault-freedom" and "totality" (2026-06-02). | mem:project_proof_engine_guarantee_contract:101–111 | partly — the label and parts 1–3 are in canon (`precept-language-spec.md:262–276`); part 4, runtime fault traps firing for out-of-contract data, is superseded by want:165 and canon now says there are none (`:278`) | `precept-language-spec.md:266` |
| 82 | Philosophy line 35 left unchanged — *"do 1 not 2"* (2026-06-02). | mem:project_proof_engine_guarantee_contract:111 | superseded — by the 2026-07-28 rewrite (row 61) | — |
| 83 | Relational rules and bounds design, D1: a bound is admitted and governed at runtime (locked 2026-06-02, owner-directed rule-centric reframe). | mem:project_relational_rules_bounds_design:126–128 | superseded — by want:14/:171 and walk item 16 (row 43): the bound is proven or the definition rejected | — |
| 84 | The verify-don't-trust checker deleted, with his approval (2026-06-08). | mem:project_soundness_infrastructure_build:151 | superseded — the checker is back in scope including its load-time half (want:199–206; walk item 1, row 34) | `soundness-and-coverage.md:159–161` |
| 85 | Overflow accepted as one known temporary gap; number-model redesign parked (2026-06-15). | mem:project_compiler_readiness_2026_06_11:11 | superseded — by the 2026-07-14 ruling and walk item 15 (row 42) | — |
| 86 | He accepted in principle a philosophy change: operation-level approximation honesty ("sin to N digits") may satisfy Principle 8 in place of the type-level lane split (2026-06-15). | mem:project_compiler_readiness_2026_06_11:11 | unclear — never applied; not mentioned in the 2026-07-28 rewrite record I read; the lane split still stands (`primitive-types.md:284`) | — |
| 87 | `integer` is fixed-width 64-bit today; the overflow model (proof-or-reject against arbitrary precision) is deferred. Later: *"I am considering arbitrary precision but no decision yet"* (2026-07-20). | mem:project_gap_probe_rework_2026_07_14:35; mem:project_boundary_define_what_i_want_2026_07_16:163 | in canon (fixed width now, model open) | `primitive-types.md:81`, `:697` |
| 88 | `pow` takes a non-negative exponent in every lane (2026-07-14). | mem:project_gap_probe_rework_2026_07_14:36 | in canon | `primitive-types.md:613` |
| 89 | The bounds-qualifier example uses `in 'each'`, not `in 'points'` (2026-07-14). | mem:project_gap_probe_rework_2026_07_14:38 | unclear — no hit for `quantity in 'each' max 100` in the spec; the line has moved or been rewritten | — |
| 90 | WF-0 rulings (2026-07-13): `sqrt` bars `decimal` only and `integer` widens; fault delivery is `EventOutcome.Faulted`; `choice` field-against-field per the subsequence rule; a collections implementation-state row. | mem:project_compiler_readiness_v3_2026_07_12:22 | partly — `sqrt` in canon (`precept-language-spec.md:1594`); `Faulted` in canon (`precept-language-spec.md:1947`; `fault-system.md:11`); subsequence in canon (`precept-language-spec.md:1406`); the collections row not checked | `precept-language-spec.md:1594`, `:1947`, `:1406` |
| 91 | Fold literal division, type-aware, through the one shared evaluator; modulo stays unfolded (2026-07-20). Also "extras no problem" on coverage, and negated ordering comparisons flip for exact families. | mem:project_boundary_define_what_i_want_2026_07_16:183, :186 | not in canon (in the working normal-form draft; walk item 13, row 41, keeps folding through one evaluator) | — |
| 92 | "Constraint" is the umbrella term — *"I used the term 'rules' more loosely"* — and all five constraint forms are proof obligations (2026-07-20). | mem:project_boundary_define_what_i_want_2026_07_16:175 | unclear — canon uses "constraint" broadly but no sentence makes it the umbrella term (searched "umbrella"); five forms as obligations rests on walk item 7 (row 38) | — |
| 93 | Structural-soundness codes move to Error; `terminal` becomes a required declaration of intent (2026-07-12). | mem:project_compiler_readiness_v3_2026_07_12:17 | in canon (confirmed by walk item 22, row 49) | `diagnostic-system.md:188–193`; `graph-analyzer.md:452` |
| 94 | Universal certificates at slice 0 — all step kinds now, "Option A" (2026-07-12). | mem:project_compiler_readiness_v3_2026_07_12:18 | superseded — by the reset plan: the step vocabulary closes at phase 5 (`compiler-readiness-plan.md:248`) | — |
| 95 | Incremental compilation ruled out on the 44 ms benchmark (2026-05-29). | mem:project_compiler_fast_no_incremental:3 | in canon | `docs/tooling/language-server.md:1498`; `docs/compiler/README.md:71` |
| 96 | `because` is mandatory on ensures: *"remove the optional one, that is AI slop."* (2026-05-26) | mem:feedback_ai_slop_pattern:48 | in canon | `precept-language-spec.md:108` |
| 97 | *"14 was already decided — no comparison, why did we re-open it?"* (2026-07-24, on period ordering) | mem:feedback_dont_resurface_ruled:103 | superseded — by row 46 (2026-07-26, where he said the contrary ordering ruling was his) and row 62. The memory file still teaches the 07-24 version and does not record the reversal | — |
| 98 | The language spec states what should exist, not only what is built: *"the canonical spec is full of features that don't exist. Its purpose is to represent what should exist, and our mission is to make that true."* (2026-06-01) | mem:feedback_remove_dont_annotate_unbuilt_docs:247 | partly — the same position is in canon for the philosophy (`philosophy.md:7`); not stated for the spec | `philosophy.md:7` |
| 99 | Precept ships only when compiler and runtime are both complete; design states every guarantee in end-state terms (2026-07-11). And: *"we are in design — current capabilities of the proof engine and the non-existent runtime shouldn't have any bearing on this analysis."* (2026-07-12) | mem:feedback_no_interim_pre_runtime_staging:145–146 | not in canon (memory only); consistent with the MVP definition at `soundness-and-coverage.md:28–33` | — |
| 100 | Implementation parked: *"we are not working on implementation right now, the goal is to complete the design work first."* (2026-07-20). Also 2026-07-13: no production code until the readiness plan is complete. | mem:project_boundary_define_what_i_want_2026_07_16:183; mem:project_compiler_readiness_v3_2026_07_12:22 | superseded — restated more strongly by row 1 | — |

## 8. Rulings on how the work runs (all outside canon by nature)

| # | Ruling, in his words where they exist | Recorded at | Where it is written now |
|---|---|---|---|
| 101 | Plain engineering prose, no coined vocabulary: *"I hate the word denominator… we need to lose the agent speak… that instruction needs to flow throughout and all subagents/workflows need to follow that guidance. Review is exhausting with all the agent speak."* (2026-07-25); expansion, not dumbing down (2026-07-01); *"agent-speak sucks"* (2026-07-26). | mem:feedback_no_coined_jargon_in_artifacts:128, :137, :140 | `CLAUDE.md:16–28` |
| 102 | Plain register in chat; labels such as register row numbers are not names: *"those n numbers mean nothing to me."* (2026-07-28); *"what is a licensed premisis? why do you continue to talk like that?"* (2026-07-20) | mem:feedback_plain_register_in_chat:204–210 | memory only |
| 103 | Put a question to him as a plain consequence with a worked case, never in the artifact's phrasing (asked partway through the walk which items to re-ask plainly). | walk:48–74 | `compiler-readiness-plan.md:342–352` |
| 104 | No `AskUserQuestion` modals; one question at a time in plain text (2026-05-24). | mem:feedback_ask_user_question:58–59 | memory only |
| 105 | Numbered slices: *"these labels drive me nuts. I want slice 1, slice 2, etc."* (2026-06-01) | mem:feedback_numbered_slices_not_labels:198 | memory only |
| 106 | Agent reviews do not stand in for his: *"don't substitute Frank's approval for mine — Frank's an agent and I still need to be able to digest this."* (June 2026) | mem:feedback_agent_verdicts_arm_owner:43 | memory only |
| 107 | Agent-written docs and commits are not his decisions (2026-07-06). | mem:feedback_agent_authored_labels_not_owner_authority:2 | `decision-register-FINDINGS.md:23` |
| 108 | Do not bring him what he has already ruled: *"don't bring it to me if it's something I've already ruled on, that is draining."* (2026-07-21); *"we very clearly decided to defer this, is that not documented already?"* (June) | mem:feedback_dont_resurface_ruled:96; mem:feedback_decided_but_unapplied:73 | `compiler-readiness-plan.md:226–227` |
| 109 | A ruling covers what he said it of; widening one is the same failure as inventing one (from rows 72–73). | `phase-2/five-items-walk-2026-08-27.md:137–140` | memory only |
| 110 | Keep the main session thin and delegate: *"I prefer to keep this session thin and use subagents."* (2026-07-12); *"you should have delegated to skill"*; *"use independent subagents to check, you do too much on the main session with polluted context."* (2026-07-24); *"you're not supposed to do that inline with the main session."* (2026-07-28) | mem:feedback_delegate_execution:77–80; mem:feedback_main_loop_not_authority:120–121 | memory only |
| 111 | Model use: spend for the best result, use Fable for its power, economise only near a cap and only with his say: *"I don't want to leave tokens on the table"* (2026-07-21); *"judicious use of fable"* (2026-07-25). | mem:reference_model_usage_limits:209–215; mem:reference_workflow_agent_models:219 | memory only |
| 112 | Consult him before designing new language surface: *"I should have been consulted and a conversation should happen before designing any new features, especially ones that challenge what is locked in the canonical docs."* (2026-05-28) | mem:feedback_pre_design_consultation:214 | `CLAUDE.md:234–278` |
| 113 | Working-folder documents, including `bugs.md`, are transient: *"i think any doc in the working folder, including bugs.md, is transient. the only docs that matter are the canonical docs."* (2026-05-26) | mem:feedback_no_transient_doc_refs_in_code:179 | `CLAUDE.md` § Transient vs Canonical references |
| 114 | Execute must not rewrite a locked design; surface the gap instead (2026-06-01). | mem:feedback_execute_no_design_rewrite:110 | memory and the `/execute` skill |
| 115 | Precept-reviewer at every slice boundary: *"the review after each slice — I meant precept-reviewer."* | mem:feedback_slice_review_checkpoints:284 | memory only |
| 116 | Solo developer, no release, no migration or back-compat concerns (2026-05-26). | mem:project_solo_dev_pre_release:138–148 | memory only |
| 117 | Stop re-flagging that the runtime is a stub (2026-07-06). | mem:feedback_stop_reflagging_runtime_stub:319 | memory only |
| 118 | Use the whole time budget: *"the time box was overnight, not one hour."* | mem:feedback_use_full_time_budget:327 | memory only |
| 119 | Search history before claiming canon is silent — his reactions *"I feel like we've already covered this"* and *"this is fundamental to the whole compiler/runtime split, I don't know why it's even a question"* (2026-07-21). Also *"is there a runtime parking lot?"* | mem:feedback_search_history_not_just_tree:265–267; mem:project_reset_phase_1_2_2026_07_28:261 | memory only |
| 120 | The floating `NodaTime 3.*` reference is not pinned — he declined (2026-07-21). | mem:reference_mcp_nodatime_reboot_flake:188 | memory only |
| 121 | Two working files stay live and are not archived: `bugs.md` and the want document (carve-outs added 2026-07-28). | `compiler-readiness-plan.md:246` | plan only |

---

## 9. Agent-authored labels that are not his rulings

These are recorded somewhere as "owner ruling", "settled", "locked" or "ratified", but he marked them
*not mine* or *don't remember*, or no quotation exists. They are not authority.

| Item | Label and where | What he said, or why it is not his |
|---|---|---|
| Exactly four premise classes; editable-ingress evaluation is a mechanism, not a premise | "owner ruling", `b801451b` (walk:153) | *"agents wrote that, not me"* (walk:165) |
| A value written earlier in an operation is a fifth premise source | `33707e77` (walk:174) | *"this is derived"* (walk:183) |
| Fault-family membership counts — eleven, ten, nine in four days | stamped ruled 07-21/23/24 (walk:310) | *"derived"* (walk:318) |
| The discharge contract is exact and must name a finite procedure | `5d554d59` (walk:321) | *"I did not rule on this previously"* (walk:336) |
| Respelling judged once per family; guard conjuncts discharge like argument bounds | `5d554d59` / `26ed4d68` (walk:340) | *"derived"* (walk:350) |
| Not-NaN, infinity deferral and underflow on the `number` lane | `ce72725d` (walk:377) | *"derived"*; the underflow half *"never put to me"* (walk:387–392) |
| Ratification of the 2026-07-06 boundary ruling and the 07-12 architecture gate | 07-12 note "all four architecture decisions ruled by the owner" (walk:586) | *"I don't remember if I ratified the document, but the summary you showed me matches what I asked for."* (walk:600) — and the summary was then overridden by his row 43 |
| `notempty` is string-only | `c67f334e`, back-dated to 06-03 (walk:524) | *don't remember* (walk:525). Canon carries it; it stands as derived, not ruled |
| Function-argument constraints are build work, not a fork | `4774ed43` (walk:522) | withdrawn — the commit claimed no ruling |
| Matrix tooling, cell mechanics, corpus counts | `60ef5a11` and kin (walk:619) | *"derived"* (walk:626) |
| Records of refuted arguments and reversals | `b59cd79b` and kin (walk:629) | *"agent work"* (walk:638) |
| Bug filings BUG-033 to BUG-066 | walk:641 | *"agents created the bugs, as we were walking through"* (walk:648) — they stand on their reproductions |
| 2026-07-12 slice designs; 21 step kinds and 11 premise kinds | walk:654 | *"derived"* (walk:662) |
| Decisions inside the two unlocked establishment/preservation designs | `97b12d7c` and others (walk:666) | *"that's pure agent speak, so guessing agent work"* (walk:681) |
| The withdrawal of same-basis ordering "on the owner reaffirming D14" | `e33be4b6` (walk:497) | did not happen; he marked the contrary ruling his (row 46) |
| The six reset-plan rows dated "Settled 2026-07-26": Working folder kept until phase 8; `nonnegative` fixed in phase 11; step vocabulary closes at phase 5; no numeric threshold at phase 4 or phase 6; the matrix's six slicings do not carry as structure | `compiler-readiness-plan.md:246–251` | unclear — no quotation; the plan's own note says rows without his words are removed by the walk unless he said them (`:244–245`). The `bugs.md`/want-document carve-out is row 121 |
| Frank's recommendations on OF1–OF5, and the "Frank ruled pow" attribution | mem:project_gap_probe_rework_2026_07_14:36, :65 | an agent's; the pow ruling is his (row 88) |
| Lean that an engine-invariant breach is an exception, not a `Fault` | `phase-2/runtime-open-questions.md:30` | his words — *"my vote would be exception, but I think we should park this…"* — but a lean, not a ruling; the plan says nothing may be built on it (`compiler-readiness-plan.md:273`) |

## 10. Questions he has declined to rule on, or said were never put to him

Not rulings, listed so they are not mistaken for them: must a proof name what establishes and preserves a
fact (*"this has not been put to me plainly. I'm not prepared to rule right now, would need more context
and/or research."*, walk:201–202; `compiler-readiness-plan.md:280`); where a divisor's non-zeroness must
come from (*"never put to me"*, walk:388; plan:281); what discharges an editable write (plan:282); the
complement of an earlier row's guard, a computed field's formula as a premise, state residency as a
premise, whether `from any` means one row per state, state-action multiplicity, and the `integer`
arithmetic model (walk:819–844; plan:283–288); rules that mention the current time (row 32); where the
discharge fragment stops (plan:278).

---

## Counts

Main tables, rows 1–121:

| Status | Rows | Count |
|---|---|---|
| in canon | 4, 5, 6, 8, 9, 10, 12, 13, 15, 18, 19, 20, 21, 22, 23, 26, 28, 31, 33, 42, 43, 45, 46, 47, 49, 50, 51, 53, 54, 57, 58, 59, 60, 61, 65, 66, 67, 69, 70, 71, 74, 75, 78, 80, 87, 88, 93, 95, 96 | 49 |
| partly | 3, 11, 14, 24, 25, 27, 35, 38, 56, 62, 64, 68, 81, 90, 98 | 15 |
| not in canon — design rulings | 7, 16, 17, 29, 30, 32, 37, 39, 40, 41, 52, 55, 91 | 13 |
| not in canon — process rulings (rows 1, 2, 34, 36, 63, 72, 73, 77, 79, 99, 101–121) | | 31 |
| superseded | 44, 48, 76, 82, 83, 84, 85, 94, 97, 100 | 10 |
| unclear | 86, 89, 92 | 3 |
| **total** | | **121** |

Agent-authored labels that are not his rulings: 18 items (§ 9). Questions recorded as never ruled: 11
(§ 10).

**Of the 13 design rulings not in canon, five are deliberate or already tracked**: row 17 (he ordered
canon silent until phase 5), row 32 (canon contradicts him, waiting on his ruling), row 16 (canon
contradicts itself, already a register obligation), rows 29–30 (parked in the runtime index). **The other
eight — rows 7, 37, 39, 40, 41, 52, 55 and 91 — are rulings of his that no canonical document states**, most of them from the 2026-07-26 walk, where the plan routes them to phase 5 rather than
to a canonical edit (`compiler-readiness-plan.md:291–340`, `:1569–1590`).

**Three things in the record worth a look**: the memory file `feedback_dont_resurface_ruled.md:103` still
teaches that Decision 14 should not have been reopened, which his 2026-07-26 and 2026-07-29 rulings
reversed (row 97); the collection half of "build both" is still recorded as open in
`collection-types.md:533` (row 62); and the plan has not been updated since 2026-08-27, so nothing in it
records the 2026-11-13 restart rulings (rows 1–4).
