# Technical consistency of the canonical docs with the want document, and with each other

Read at HEAD `2f126a3b`, branch `spike/Precept-V2-Radical-reset`, 2026-11-22. Nothing in the repository was edited.

The last commit that changed any canonical document is `1969e3e5` (2026-08-27). Every commit after it touched only `docs/working-reset/`. So the canonical text today is the text the August work left, and anything that work recorded as open is still open unless this report says otherwise.

"want:N" means `docs/Working/what-i-want-2026-07-16.md` line N. All other citations are `file:line` under `docs/`.

Each item is classified one of three ways:

- **Mechanical** — the want document or an existing ruling already says which side is right; the fix is an edit.
- **Design argument** — the right answer is not written anywhere; somebody has to work it out and defend it.
- **Shane** — only he can say, because his own lines pull two ways or he has said it is undecided.

---

## 1. Where the existing lists stand

**The 2026-07-19 decision packet, § (d)** (`docs/Working/want-analysis-2026-07-19/DECISION-PACKET.md:193–224`). It listed 25 verified rows, one extra row from the clarity analysis, and 20 unverified leads. All 46 were carried into the phase-2 register and all are closed at HEAD: executed by a named commit, or in one case (`R13`, removing three `FaultCode` members) ruled against and deliberately not done. Its two "not yet read" notes are also closed: `compiler-and-runtime-design.md` was corrected in `25894ae3`, and the type documents under `docs/language/` got rows of their own (`N31`–`N33`, `N40`, `N43`, `N74`, `N75`, `N101`). **Nothing from § (d) is open.** The example the task named is fixed: `precept-language-spec.md:1949`, `:1953` and `:278` now say there is no evaluation after mutation, and `philosophy.md:42` says "Nothing re-evaluates the rules after the change."

**The phase-2 register** (`docs/working-reset/phase-2/contradiction-register.md`). 165 identifiers. Open at HEAD:

- Nine rows that are really six questions, all deliberately left for a later phase: `N8`/`N70`/`N79` (what inspection reports), `N10`/`N80` (the constraint index against the premise list), `N66` (certificate format), `N81` (the induction has never been argued), `N96` (what an editable write evaluates), `N100` (strategy numbering).
- Fifteen rows `N105`–`N119` from the four mechanical checks in `phase-2/checks/`. None is executed.
- One bookkeeping slip: the `N82` cell (period ordering) still says "Uncommitted, in the working tree; no hash exists yet." The edit is committed — `temporal-type-system.md:1481` carries it and the tree is clean.

**The cold read** (`phase-2/cold-read-register-diff.md`). Sixteen findings were left when exit condition 4 was rewritten. Two were fixed on 2026-08-27 (the inspection table at `evaluator.md:1941–1944` no longer has a Constraints column; `compiler-and-runtime-design.md:863–865` now scopes its "cannot be one" sentence). One became row `N112`. The rest still reproduce; I re-checked each by reading the cited lines.

**What was never done by any of the lists** is in § 3 below: twenty findings that are in no register row and not in the cold read.

---

## 2. Still open, carried from the existing lists

### 2.1 Where canon and the want document disagree, or canon is silent

**C1. What an editable-field write evaluates.** *(register `N96`)*
want:143 says an edit evaluates "the field's modifier-rules *and* every rule that mentions the field", and that this "is exactly the premise that closes the editable write sites in the compiler's proof". want:106 and want:129 repeat it. want:171 and want:199 say the runtime never re-evaluates proven rules. Shane ruled the "and nothing wider" wording out of canon on 2026-08-26 and declined to settle the question.
**The removal did not make canon neutral.** About twenty sentences still state the narrow answer positively — "held to the constraints declared on the slot it enters" at `precept-language-spec.md:274`, `:1925`, `runtime-api.md:402`, `result-types.md:148`, `:152`, `evaluator.md:115`, `:1597`, `:1779`, `proof-engine.md:133`, `:2595`, `compiler-and-runtime-design.md:107`, `:137`, `precept-builder.md:144` — and two go further and assert the other answer as fact: `runtime-api.md:402` ("an editable field is an ordinary write site, and the compiler discharged its obligations at compile time like any other") and `evaluator.md:759`. Only `evaluator.md:1361` and `:1387` say the question is open.
The want document's own worked example depends on the answer: `RepaymentCapPercent` is `editable` (want:36) and appears in a relational rule (want:40).
**Shane**, on the prepared paper `docs/working-reset/phase-5/what-discharges-an-editable-write.md`. Then mechanical across the sentences above.

**C2. The induction is stated in canon but has no design.** *(register `N81`; cold-read finding 1)*
want:16–20 defines the promise: a base case, an inductive step from four premise classes, and obligations at every write site of every field a rule mentions. Canon states this in prose (`precept-language-spec.md:268`, `proof-engine.md:2612`, `compiler-and-runtime-design.md:127`, `:149`). The technical model underneath describes a different engine:

- The engine's stated job is "discharge statically preventable runtime hazards" (`proof-engine.md:91`).
- Obligations exist only where a catalog entry for an operator, function, accessor or action declares one, or where a write targets a field with a declared bound (`proof-engine.md:379–387`, `:473–485`; `compiler-and-runtime-design.md:163`). "The proof engine does NOT identify what needs to be proved" (`proof-engine.md:2379–2388`). An authored rule such as `rule Balance >= -OverdraftLimit` produces no obligation at the handlers that write `Balance` or `OverdraftLimit`.
- The obligation vocabulary has thirteen kinds and neither establishment nor preservation is one (`proof-engine.md:489`).
- The completeness contract is defined over catalog requirements only (`proof-engine.md:2319–2323`).
- The coverage map has a fault-code axis and a creation-site axis and no row for an authored rule (`soundness-and-coverage.md:212–263`); its bucket test sends anything with no matching cell to "new language surface, not a bug" (`:297–299`).
- The base case is a constant fold of literal defaults against `in`-anchored ensures and unguarded rules (`proof-engine.md:1931–1960`). It does not prove that an initial event establishes a rule from its argument constraints, which is what want:18 asks for and what want:117–118 require to be rejected when it fails.
- No diagnostic exists for "this handler does not preserve this rule" or "this initial event does not establish this rule". The three-way verdict is defined for "containment obligations" only (`diagnostic-system.md:181`). Ten of the fourteen rows in the want document's rejection table (want:114–123) therefore have no named diagnostic.
- `compiler-and-runtime-design.md` contradicts itself on this: `:127` says every handler that could break a rule carries an obligation; `:163` defines the complete set of obligation sites and that case is not in it.

Canon's own note at `proof-engine.md:2614` calls two of these "gaps against the target" in the build. On 2026-08-27 Shane ruled that "the compiler has not built it" is not a documents question, and that stands. But under the 2026-11-13 goal the problem is one level up: **the design itself does not say** how a preservation obligation is generated, at which write sites (the eight writers at `precept-language-spec.md:1988–1995`, including the `omit` reset, entry and exit actions, and computed-field recomputation), how it is discharged, how state ensures and guarded rules fit, or what the author is told.
**Design argument.** This is the largest hole in canon measured against the want document.

**C3. What inspection reports.** *(register `N79`, `N70`, `N8`)*
want:161 and want:211: inspect reasons about an event before it fires and "can cite provenance, not just verdicts". Canon records the gap in its own voice (`result-types.md:250`, `evaluator.md:912`) and leaves three result arrays described in the old model: `result-types.md:213` (`EventEnsures`), `:227` ("rules evaluated against the hypothetical post-patch state"), `:248`. The public API text still promises constraint results: `runtime-api.md:412–416` (the "Evaluated" tier: "what passed, failed, or couldn't be resolved"), `:445–449`, `:697–699` ("which constraints fail"), `:725`, `:729`.
**Design argument** — the result type, and how a certificate entry is surfaced through it.

**C4. The premise list against the constraint index.** *(register `N80`, `N10`, `N107`)*
want:210: "the premise list **is** the runtime's validation surface … derived from the proof, never maintained in parallel." `runtime-api.md:258` states that. `precept-builder.md:505–516` still builds a five-anchor `ConstraintPlanIndex` from the declarations, which is the parallel structure, and `precept-builder.md:812`, `:866` and `catalog-system.md:1444` record that nothing reconciles the two. `proof-engine.md:2162` and `precept-builder.md:563` also declare the related `ConstraintInfluenceMap` with different members and different key types.
**Design argument.**

**C5. The certificate has a contract and no format.** *(register `N66`)*
want:199 and want:206 define the certificate as a per-obligation record of theorem, premises and derivation that a small checker walks. In `soundness-and-coverage.md` the verdict (`:122–124`), the counterexample (`:131–133`), the certificate format (`:144–146`), the checker (`:153–155`), the hole coverage (`:319`) and the soundness rails (`:334`) are each one paragraph tagged "Intent — fill per slice". The document's own status is "Design (skeleton) … filled per implementation slice" (`:8–9`), which is the code-first ordering Shane rejected. The step vocabulary the spec makes a condition of a strategy being admissible (`precept-language-spec.md:225`, `proof-engine.md:2349`) does not exist.
**Design argument.**

**C6. How the certificate leaves the compiler, and where the checker runs inside it.**
want:199: emitted "alongside the compiled definition". want:203: one call site is "a compiler stage". Canon: `Compilation` has no certificate member in either of its two printed shapes (`compiler-and-runtime-design.md:687`, `precept-builder.md:181–190`); the artifact inventory has none (`compiler-and-runtime-design.md:691–703`); the pipeline is six stages with no checker (`compiler/README.md:17`). Both gaps are recorded as open questions in canon (`precept-builder.md:643`, `:865`; `compiler/README.md:61`). Nothing says what binds a certificate to the definition it certifies, which is what would make a "tampered" or "stale" one (want:204) detectable, or whether a compiled definition is ever stored and loaded later rather than always rebuilt from source.
**Design argument.**

**C7. What happens when the load-time gate refuses.**
want:204 requires the refusal; want:177 requires it to explain itself. `precept-builder.md:641` and `:864`: throw or structured result, whether the failing step is named, and whether verification is cached are all undecided.
**Design argument** (small).

**C8. Event ensures, and the `ensure` construct generally.** *(cold-read finding 7 and its companion; register `N117`)*
The want document never uses the word `ensure`. want:143 says of an argument that ingress evaluates "its modifier-rules; anything relational an event needs lives in its guards", and want:19 lists four premise classes with no ensure among them. Canon:

- evaluates `on <Event> ensure` at ingress (`evaluator.md:1377–1380`, `:2139`; `runtime-api.md:883`; `precept-language-spec.md:2088`);
- says in another place that event ensures are "proven rather than re-evaluated" (`result-types.md:119`) — a direct contradiction of the lines above, **not previously recorded**;
- lists "an ensure" as something a proof may rest on (`precept-language-spec.md:206`), a fifth premise class;
- counts constraint kinds as four in the spec (`precept-language-spec.md:1961`) and five in the API (`runtime-api.md:421`).

**Shane** for whether an event ensure is ingress validation, a premise class, both, or should not exist; then **design argument** for how state ensures (`in`, `to`, `from`) enter the induction.

**C9. May a rule mention the current time.** *(plan § 4, fifteenth item)*
want:219 leaves it "deliberately left open". On 2026-08-27 Shane said "it is undecided, add it to the list". Canon answers yes: `temporal-type-system.md:560–577` declares `now()` and shows `rule now() - IncidentAt <= '72 hours'`; `:568` locks the consequence; `precept-language-spec.md:1605` carries the signature. **Not previously recorded:** `philosophy.md:28` also answers it — "a rule whose correctness depends on when it is evaluated says so in the definition" — in the document he approved on 2026-07-28, a month before he called the question undecided.
**Shane.** If the answer is no, the philosophy sentence needs his approval to change.

**C10. What a dead end is.** *(cold-read finding 5)*
want:152: "Every non-terminal state has an exit … A state you can enter but never leave is rejected, unless the author deliberately marked it `terminal`." Canon has four definitions:

1. `compiler-and-runtime-design.md:147` — every non-terminal state must have an outgoing path (matches the want document).
2. `graph-analyzer.md:451` — a reachable non-terminal state from which no terminal state can be reached. Stronger: two states that only lead to each other pass definition 1 and fail this one, and a lifecycle with no terminal state at all makes every state a dead end.
3. `precept-language-spec.md:186` — a non-terminal state "where all outgoing rows reject or produce no-transition". **Not previously recorded.** It also says "Reported as a structural diagnostic", where want:126 and `graph-analyzer.md:457` say rejected.
4. `precept-language-spec.md:185` defines a terminal state as one "with no outgoing transition rows", where want:152 makes `terminal` a deliberate marking.

The want document and `philosophy.md:44` ("no dead end an entity enters and cannot leave") support definition 1 as the floor. Whether the stronger test is also wanted is not said anywhere.
**Design argument.**

**C11. Who handles data written under an older definition.** *(cold-read finding 6)*
want:141: "Post-MVP, the restore path itself expands to handle schema evolution." `runtime-api.md:277`, `:906` and `evaluator.md:1765` agree. `compiler-and-runtime-design.md:718` says the opposite: "definition migration is out of scope for v1 … Host applications that need to handle definition evolution must manage the migration externally."
**Mechanical** — the want document names the owner.

### 2.2 Where two canonical documents disagree

**C12. Strategy numbering.** *(register `N100`)* The `ProofStrategy` enum at `proof-engine.md:301–314` and the `#### Strategy N` headings at `:1426–1685` number the same strategies differently from 6 on. **Mechanical** once one is picked.

**C13. The fifteen rows from the mechanical checks, `N105`–`N119`.** None executed. By my reading of each row's own resolution:

- **Mechanical, ten:** `N105` (`catalog-system.md:1117` against its own diagram at `:596`), `N108`, `N111`, `N112`, `N113`, `N114`, `N115`, `N116`, `N118` (`precept-grammar.md:63`, `:808` say fourteen catalogs; `catalog-system.md:129` says fifteen), `N119`.
- **Mechanical after one small choice, two:** `N109` (every descriptor carries `SourceSpan` in `precept-builder.md` and `int SourceLine` in `descriptor-types.md`), `N110`.
- **Design argument, three:** `N106` (two different `ConstraintDescriptor` shapes, `precept-builder.md:518` against `runtime-api.md:423`), `N107` (folds into C4), `N117` (folds into C8).

Shane's "one full statement, deduplicate" ruling is the repair for the twelve: name the owning document, the others point to it.

**C14. Fourteen cold-read findings with no register row**, each re-read at HEAD:

| # | What disagrees | Lines | Fix |
|---|---|---|---|
| 1 | `UpdateOutcome` variant names share nothing | `compiler-and-runtime-design.md:883`, `:861` against `result-types.md:136–142` | Mechanical |
| 2 | Restore recomputes computed fields, or leaves them stale | `compiler-and-runtime-design.md:842`, `:914`, `runtime-api.md:277`, `precept-language-spec.md:1992` against `evaluator.md:1983–1985` | Mechanical — four sites and the spec say recompute |
| 3 | The builder numbers its passes two ways | `precept-builder.md:110–117` against `:306`, `:420`, `:505` | Mechanical |
| 4 | `Compilation` has two memberships | `compiler-and-runtime-design.md:687` against `precept-builder.md:181–190`, `:193` | Mechanical for today's members; the certificate member is C6 |
| 5 | 147 codes declared, 164 claimed, set called closed | `diagnostic-system.md:419`, `:421`, `:57` | Mechanical |
| 6 | Four codes discussed and not declared | `diagnostic-system.md:201–205`, `:419`, `:752` | Mechanical |
| 7 | `DiagnosticCategory` used throughout and rejected | `diagnostic-system.md:454` against `:772` | Mechanical |
| 8 | The analyzer rule range given twice | `diagnostic-system.md:81` against `:646` | Mechanical |
| 9 | Diagnostic count differs between documents | `runtime-api.md:672` (132) against `diagnostic-system.md:419` (164) | Mechanical |
| 10 | A graph diagnostic the graph analyzer document never mentions | `diagnostic-system.md:187`, `:193` (`StructuralSinkState`); zero occurrences in `graph-analyzer.md` | Design argument (small) — what the check is |
| 11 | The fact table omits a subtype | `graph-analyzer.md:261–271` against `proof-engine.md:449` | Mechanical |
| 12 | `EventOutcome` has nine, eight or seven variants, and `Created` is used but never declared | `runtime-api.md:106`, `result-types.md:47`, `:56`, `:95–104`; `Created` at `runtime-api.md:145–182`, `precept-language-spec.md:1944` | Mechanical |
| 13 | `HypotheticalResult` documented on a record that does not declare it | `result-types.md:229` against `:220–223` | Mechanical |
| 14 | A survey row reading "Faults throw" beside two statements that they never throw | `result-types.md:369` against `:51`, `:72` | Mechanical — not re-verified beyond reading the row; it may describe the surveyed system |

---

## 3. New: found by this read, in no register row and not in the cold read

### 3.1 Text that still describes checking rules at runtime

The August passes cleared this family from the places their patterns reached. These survive.

**T1. Stateless construction still checks global rules.** `precept-language-spec.md:2151`: "it fires through the standard pipeline (arg ensures, mutations, field constraints, global rules) … If no initial event is declared, defaults and computed fields are set and global rules are checked. The construction succeeds if these pass." Against want:199 and against the spec itself at `:2090` and `:1949`. **Mechanical.**

**T2. An exit ensure "will be checked on transition out".** `runtime-api.md:486`. And `:490` describes the third tier as "what was actually evaluated for this specific operation (which includes to-ensures once a target is resolved)". Against want:199 and `runtime-api.md:880–881`, which say entry and residency ensures are proven and not evaluated. **Mechanical** for `:486`; `:490` goes with C3.

**T3. Three leftovers in the architecture document.** `compiler-and-runtime-design.md:873` lists "Business prohibition or rule failure" as a runtime "structured domain outcome"; `:902` describes constraint kinds as "active on entering a state", "active on exiting a state"; `:900` says the evaluated tier is "what was actually checked during a specific operation". **Mechanical.**

**T4. The glossary.** `glossary.md:100` — the evaluator "applies an event, validates against rules, computes the new Configuration". `:103` — an event "triggers transitions and rule evaluation". `:97` and `:195` define an ensure as event-scoped only, dropping the three state forms. `:103` also shows event syntax (`event Activate with Plan as string`) that is not the language's (want:48 shows the real form). `:240` names the type `Version<TConfig>`; `runtime-api.md:289` has `Version`. The glossary has no entry for certificate, premise, counterexample, obligation or ingress. **Mechanical.**

### 3.2 The compiler's technical model against the want document

**T5. "Nothing from the proof ledger crosses into runtime."** `proof-engine.md:106`, `:2364–2377`, `:2583–2589`, with the reason "Runtime instances don't carry proof baggage" and `:2180` ("`Obligations` — Not consumed by builder — Diagnostic-only"). want:199 has the compiler hand the runtime a per-obligation record of the proof, and want:210 makes the premise list drawn from it the runtime's validation surface. The same document also contradicts the sentence on its own terms: `:2151` and `:2157–2166` have the builder turn the ledger's influence entries into a runtime index, as does `precept-builder.md:117`, `:558`. **Design argument** for what crosses (with C6); the rewording after that is mechanical.

**T6. The limits of proof are written as permanent, and one of them excludes a premise class.** Shane's 2026-11-13 rule is that the design states the full intended proof power and that something not yet provable is an open problem, never an accepted boundary; his 2026-07-26 ruling was "a minimal proof set for the MVP, but we can and will expand the compiler's proof power over time." want:105 and want:135 do rule out "a solver the compiler does not carry". Canon goes further than that:

- "This is a deliberate scope limit, not a capability gap" (`proof-engine.md:139`); "Bounded, Non-Extensible Strategy Set" (`:2500`); `compiler-and-runtime-design.md:165` repeats "non-extensible".
- Combining two declared facts is "out of scope … deliberately deferred, not scheduled", justified by "Such relationships are rare in business state machines" (`proof-engine.md:2358`, `:2537–2541`); `precept-language-spec.md:256` ("deliberately retained"); `soundness-and-coverage.md:271–286`, which also tells a reader not to reopen a deferred cell (`:293–296`).
- "General dataflow (tracking values across multiple transitions …) is out of scope" (`proof-engine.md:2597–2606`). Premise class (d), want:19, *is* a fact carried across operations: every rule held in the pre-state because every earlier operation preserved it. The exclusion and the induction paragraph forty lines below it (`:2612`) cannot both stand.
- Logarithmic units are "outside that proof guarantee" with no stated outcome (`proof-engine.md:109`). Under want:171 an unproven case rejects; the document does not say so.
- "The bounded strategy set is sufficient for the DSL's constrained expression language" (`proof-engine.md:2618`), while `:2545` says its coverage has not been validated.

Which side: Shane's quoted rulings support stating the full intended power; the want document supports "no opaque solver, reject what cannot be proved". Neither supports "non-extensible" or "sufficient". **Design argument** — this is the open plan item "where the discharge fragment stops".

**T7. A "justification mechanism" is listed as the precedented response if the strategies prove too restrictive.** `proof-engine.md:2565`. Against `philosophy.md:109` ("no annotation that says trust me"), `soundness-and-coverage.md:117–120`, and want:189. **Mechanical** — remove it or move it to rejected alternatives.

**T8. Overflow has an exemption and no design.** want:165: "no overflow guards". `precept-language-spec.md:272` and `soundness-and-coverage.md:233`, `:312–316` say the overflow model is "in scope and not yet implemented" and sequenced after the first compiler. That ordering is Shane's ruling and is not in question. But no canonical document contains the overflow obligation's design, and the arithmetic model for `integer` it depends on is an open item for him (plan § 4). `precept-language-spec.md:272` also says a clean definition "can still overflow when an expression is evaluated" and no document says what the runtime does then. Under "the first build may not shrink the design", this is a missing section. **Shane** for the integer model, then **design argument**.

**T9. Computing the premise the author is missing.** want:179: the compiler "computes the guard or arg constraint that would close the proof and shows it", explains why, and "never inserts the premise itself". Canon says an unresolved verdict "carries the weakest precondition, printed verbatim" (`compiler-and-runtime-design.md:165`; `diagnostic-system.md:181`) — for containment obligations only, with no account of how it is computed, and nothing that forbids tooling from inserting it (`language-server.md:1090–1117` already defines code actions that edit source for other diagnostics). **Design argument**, downstream of C2.

**T10. Two framing sentences in the spec say it describes the build.** `precept-language-spec.md:196` — "§5 is implemented. These requirements define the contract the implementation satisfies." `:2224` — "this spec tracks what's implemented". Against Shane's rule that the spec states the target, and against the spec's own `:272`. **Mechanical.**

**T11. Project vocabulary inside canon.** `soundness-and-coverage.md:94` ("The load-bearing condition"), `:233` and `:312` ("GATE-O"), `:310` ("Slice-0"), `:74` and `:124` ("the ratified proof-engine decision ledger" as authority), `:324` (a link into `docs/Working/`), and "§1b" / "§6" used for a strategy list that is not this document's own section numbering; `proof-engine.md:2535` cites "§1a, §2, §3, §6" the same way. `CLAUDE.md` forbids each of these. A reader cannot tell what "§6" is, which matters because it is named as the author's only recourse (`soundness-and-coverage.md:119`). **Mechanical.**

### 3.3 Runtime and tooling

**T12. What a runtime refusal carries.** want:177: runtime refusals "must explain themselves clearly"; want:211: a refusal "can cite the rule's `because` as the reason a guard exists". Canon:

- `compiler-and-runtime-design.md:906` says `Rejected` and `InvalidArgs` carry `ConstraintViolation` objects and that `Update` returns `InvalidInput`. `result-types.md:99–100` gives both variants a single string, and `:139` names the update failure `ConstraintsFailed`. Two documents, two shapes.
- `result-types.md:123` records the shape of `InvalidArgs` as open.
- Nothing anywhere connects a guard or a `reject` row to the rule it protects, so the citation want:211 describes has no source. It would have to come from the certificate.

**Mechanical** for the cross-document mismatch; **design argument** for the rest (with C3 and C5).

**T13. No row matched at construction.** want:154 separates inapplicability (not offered, nothing to explain) from refusal (explained). At construction canon merges them: `precept-language-spec.md:1945` — "`Rejected` — … or all guards failed with no unconditional fallback", and `:1949` makes `Unmatched` impossible there. `Rejected` carries an authored reason (`result-types.md:99`) and in this case no author wrote one. **Design argument** (small), or **Shane** if he sees it as a product choice.

**T14. Where the clock comes from at runtime.** `philosophy.md:28` admits the clock as the one environmental value; the language documents define `now()`. It appears nowhere under `docs/runtime/` or `docs/compiler/`. Open even if C9 is answered "guards only": whether one operation reads the clock once, whether inspection and the fire that follows it can see different times (`precept-language-spec.md:2162` promises they match), and whether a value written by `set FiledAt = now()` is a third way a value enters from outside, beside the two want:141 says are all there are. **Design argument.**

**T15. The planned MCP runtime tools use outcome names the runtime does not have.** `mcp.md:56–58` (`NoMatchingRow`), `:76` (`NoTransition`, `Unhandled`), `:64` ("a validation error"), against `result-types.md:95–104` and `precept-language-spec.md:1942–1949`. `precept_inspect` (`mcp.md:68–72`) covers events only; there is no tool for previewing an edit. **Mechanical.**

**T16. Tooling says nothing about verdicts, certificates or provenance.** The word "certificate" does not occur in `docs/tooling/` or `docs/compiler/tooling-surface.md`. The proof-obligation shape `precept_compile` returns (`mcp.md:338–352`) has a two-valued disposition and no counterexample, missing premise, or premises used, while `diagnostic-system.md:183` requires the three verdicts to reach the author. `language-server.md:701` defers runtime inspection facts and `extension.md:109` is a placeholder. `philosophy.md:101` says an agent "meets the same refusal, with the same explanation, that a person does". **Design argument**, downstream of C3, C5 and T9.

**T17. Inspection of an edit does not follow the order of the edit.** `evaluator.md:935–947` applies the patch and recomputes computed fields, then runs the ingress check. `runtime-api.md:400` and `precept-language-spec.md:2005` put the ingress check before anything derives from the value, and `runtime-api.md:662` calls sharing one path "a structural guarantee". The call at `evaluator.md:946` also does not match the signature declared at `:1364–1368`. **Mechanical.**

---

## 4. The runtime and tooling requirements the want document implies

| The want document needs | Lines | What canon has | State |
|---|---|---|---|
| Inspect reasons about an event before it fires | want:161 | `precept-language-spec.md:2156–2166`, `runtime-api.md:685–733`, `evaluator.md:772–960` | Stated. Result types still describe the old model (C3) |
| Inspect cites provenance | want:211 | Recorded as open at `result-types.md:250`, `evaluator.md:912` | Open (C3) |
| Immutable versions, every operation returns a new one | want:163 | `runtime-api.md:289–346`, `philosophy.md:46` | Agrees |
| No fault checks at evaluation time | want:165 | `compiler-and-runtime-design.md:169`, `:828`; `evaluator.md` § 9 | Agrees. Overflow is the exception (T8) |
| Only the declared premises run: ingress and guards | want:165, :210 | `runtime-api.md:370`, `:258` | Stated. Structure contradicts it (C4); editable writes undecided (C1); event ensures unplaced (C8) |
| Restore is trusted and is not ingress | want:141 | `precept-language-spec.md:278`, `:1953`; `runtime-api.md:277` | Agrees. Recompute-or-stale (C14 #2) and migration owner (C11) conflict between documents |
| Certificate emitted with the compiled definition | want:199 | `runtime-api.md:254` states it; no artifact carries it | Open (C5, C6, T5) |
| One checker, run as a compiler stage and at load | want:201–204 | `soundness-and-coverage.md:153–171` commits to both; no stage, no interface | Open (C6) |
| Runtime refuses a definition whose certificate does not verify | want:204 | `runtime-api.md:254`, `precept-builder.md:637–643`, `:681` | Stated. Failure shape open (C7) |
| Whether an engine defect is reported or thrown | — | `fault-system.md` Q3, recorded as parked | Ruled deferred by Shane on 2026-08-11 until the runtime is built. Not reopened here |
| Runtime refusals explain themselves | want:177, :211 | Two shapes; no link from a guard to its rule | Open (T12) |
| Compile-time rejection names and computes the missing premise | want:24, :110, :179 | "Weakest precondition" for containment only | Open (C2, T9) |
| Three kinds of answer visible to the author | ruling 2026-07-26 | `diagnostic-system.md:183` states it; the tooling shapes do not carry it | Open (T16) |
| Inapplicable is not the same as refused | want:154 | `result-types.md:101–102`; merged at construction | Partly (T13) |
| Structural defects are rejections | want:125–127, :149–155 | `diagnostic-system.md:187–189` | Agrees on severity. Dead-end definition conflicts (C10) |
| The same explanation reaches an agent | `philosophy.md:101` | Planned MCP tools use other names and omit proof detail | Open (T15, T16) |

---

## 5. Counts

| | Items | Mechanical | Design argument | Shane first |
|---|---|---|---|---|
| § 2.1 carried, canon against the want document or silent (C1–C11) | 11 | 1 | 7 | 3 |
| § 2.2 strategy numbering (C12) | 1 | 1 | — | — |
| § 2.2 mechanical-check rows (C13) | 15 | 12 | 3 | — |
| § 2.2 cold-read findings with no row (C14) | 14 | 13 | 1 | — |
| § 3 new in this read (T1–T17) | 17 | 8 | 8 | 1 |
| **Total open** | **58** | **35** | **19** | **4** |

T12 is counted once under design argument although part of it is mechanical. Three sub-findings recorded inside carried items are also new and are not counted separately: the event-ensure contradiction at `result-types.md:119` (C8), the philosophy sentence that answers the time question (C9), and the spec's third and fourth dead-end definitions (C10). With those, this read added twenty findings.

The nineteen design arguments are not nineteen independent pieces of work. Most hang off four: the induction's machinery (C2), the certificate (C5, C6, T5), what inspection and refusals report (C3, T12), and where proof stops (T6). C4, C7, T9, T16 and three of the check rows follow from those.

---

## 6. What this did not cover

- The 133 register rows recorded as executed were not re-verified one by one. I re-ran the widest pattern for evaluation after mutation over all of canon plus `glossary.md`, `agent-onboarding.md` and `README.md`, and read the spec's guarantee and execution sections, `philosophy.md`, `runtime-api.md`, and large parts of `proof-engine.md`, `soundness-and-coverage.md`, `compiler-and-runtime-design.md`, `evaluator.md`, `result-types.md` and `mcp.md` in full. T1–T4 are what that found; there may be more of the same kind.
- `business-domain-types.md`, `collection-types.md`, `temporal-type-system.md` (apart from `now()`), `primitive-types.md`, `precept-grammar.md`, `catalog-system.md`, `type-checker.md`, `parser.md`, `lexer.md`, `name-binder.md`, `literal-system.md`, `grammar-generator.md`, `fault-system.md`, `descriptor-types.md`, `tooling-surface.md` and most of `language-server.md` were searched, not read.
- No claim here rests on compiler behaviour, tests, sample files or the catalogs.
- The four mechanical checks in `phase-2/checks/` were not re-run. Canon has not changed since they ran, so their output stands.
- "Not previously recorded" means not found by searching the register and the cold-read diff for the cited lines and phrases. It does not mean no working document anywhere mentions it.
