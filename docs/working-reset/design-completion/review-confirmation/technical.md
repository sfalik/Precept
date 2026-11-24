# Confirmation of the technical findings against the repository

**Checked against:** `5c4269bc`. The plan (`design-completion-plan.md`) has not changed since the reviews were written. Its only commit is `b177ce69`, so the line numbers the reviewers cite still hold.

**Scope.** This file covers every technical finding about the proof model, the compiler, the runtime and canon from three places. The first is the second reviewer's "Where the proposed proof can be wrong" section (`outside-review-report-2.md:9-65`). The second is that reviewer's rulings on the first reviewer's section 3, together with its reachability standard (`outside-review-report-2-followup.md` § 1 "Technical findings" and § 3). The third is its audit of the three technical input files (`outside-review-report-2-followup-2.md` § 2). Each claim was checked against the input file it attacks and against canon. A finding counts as confirmed only if it is true of the current plan and repository and it matters for finishing a complete, consistent design. The last section lists every place where the plan itself relies on an input claim that turned out to be wrong.

In this file, "Plan" is `docs/working-reset/design-completion/design-completion-plan.md`, "Want" is `docs/Working/what-i-want-2026-07-16.md` and "Spec" is `docs/language/precept-language-spec.md`. Other documents are named by file.

I read the cited lines myself in every case. I did not run the compiler or read any tests or sample files. None of them could settle a design question anyway.

---

## Part 1. Confirmed

### 1. Preview can evaluate a division that dispatch proved it never reaches (critical)

**What is true.** The plan makes the negated guards of earlier rows into premises for a later row: "the complete premise set at each site, the negated guards of earlier rows included" (Plan:436-437). Dispatch does stop at the first match: "the first matching guard wins and the remaining rows are not evaluated" (Spec:2006). Inspection must agree with execution, and the fault guarantee "holds during inspection exactly as during execution" (Spec:2167).

The evaluator design does not keep to that. It loops over every row. For any row whose guard is not `Impossible` it runs `foreach (var action in row.Actions) ExecuteAction(...)` and recomputes computed fields (evaluator.md:837-843). Only afterwards does it set `prospect = Prospect.Impossible; // An earlier row already wins` (evaluator.md:864-866).

Take a definition whose first row refuses when the divisor is zero and whose unguarded fallback divides by it. With all inputs supplied and the divisor zero, firing selects the refusal and never divides. Preview reaches the fallback, finds its guard `Certain`, and runs the division. The paragraph on undetermined values (evaluator.md:889-891) covers only inputs that were not supplied, so it does not help here.

The plan names inspection only in step 12, which comes after the proof-model sign-off (Plan:575), and in step 15. The step-15 hand walk lists seven operations: "create, fire, update, restore, inspect a fire, inspect an update, and load a definition" (Plan:630). `InspectCreate` is missing from that list. It is public (runtime-api.md:89, :186: "Same pipeline, same outcome types"), and `precept_inspect` requires it (mcp.md:71-72).

**What has to change.** Put the inspection operations, `InspectCreate` included, among the evaluation positions that step 4 enumerates, before the proof model is signed off. Require the design to say which premises hold before each guard, action and message is evaluated in preview. Walk the zero-divisor case with complete inputs and a priority case with a missing input.

### 2. The plan's dead-end test can certify a state an entity cannot leave (critical)

**What is true.** The plan requires that "the compiler must likewise exhibit an input that satisfies some exit row's guard" (Plan:177). It repeats this in step 9 as "the requirement that the compiler exhibit an input that leaves the state" (Plan:525-526), and step 6 supplies "a step that exhibits values satisfying a guard, checkable by evaluating it" for step 9 (Plan:471-473). The wording comes from `technical-precedent.md:228`.

The owner's standard is per entity. "A state you can enter but never leave is rejected" (Want:152), and the approved philosophy says "no dead end an entity enters and cannot leave" (philosophy.md:44).

Here is a case where the two come apart. An application enters Review only with approval false. Nothing in Review changes approval, and the only exit requires approval true. The assignment approval = true satisfies the exit guard and any rule that says nothing about approval, so the plan's test passes, yet no applicant in Review can ever leave. The graph document as it stands checks something different again: "no structural path to any terminal" (graph-analyzer.md:439).

**What has to change.** Specify three things separately: that some run enters each state; that a particular row can actually fire; and that every reachable configuration in a non-terminal state has a feasible route out. An assignment that satisfies a guard is not a sequence of operations. The second reviewer's standard D (`outside-review-report-2-followup.md:106-123`) meets Want:151-152 and philosophy.md:44 as written. It needs no new language and does not settle the two-state-cycle question, which Plan:992 rightly keeps open as sheet 11.

### 3. Two canonical documents disagree about the `required` state check, and the plan's inventory does not have it (major)

**What is true.** The spec defines `required` as "Structural: all initial→terminal paths visit this state (dominator)" (Spec:430). The graph document instead checks "A required state must dominate at least one terminal state", emitting a diagnostic only when "terminalsReached is empty" (graph-analyzer.md:489-496).

These give different answers. Suppose Start can reach Paid only through Review, and can also reach Cancelled directly. Review dominates Paid, so the graph document's check passes, but the path Start→Cancelled skips Review, which the spec forbids. Philosophy.md:44 says "Required states lie on every path to completion — no entity skips a mandatory step."

Searching the plan and all three technical inputs for "dominat", "mandatory" and "required state" finds no conflict row. `technical-proof-model.md:253` rates "Reachability, dead ends, dominators" as Full, and Plan:141 carries "Reachability Full" forward.

**What has to change.** Add this to the conflict inventory for step 9, with the spec's wording and philosophy.md:44 as the target. It is a repair, not a question for the owner.

### 4. Restoring data under a changed definition is designed only after the proof model is signed off (major)

**What is true.** The proof-model sign-off follows step 11 (Plan:555-559). Step 12 then carries "The design of schema evolution on the restore path" (Plan:571-572), and the rulings table says "its design is owed in step 12" (Plan:909).

The want document trusts restored data "because it was governed when it was written" and puts schema evolution on the restore path (Want:141). The runtime says "what the runtime verifies for itself is the *definition*" (runtime-api.md:277). It also says that a host restoring data written under an older definition "has stepped outside the guarantee, and nothing inside the engine will notice" (runtime-api.md:906). The certificate gate verifies a definition, not a stored instance (precept-builder.md:639).

So version one can allow a balance of 5 while version two requires at least 10. Both certificates can be valid and the field keep its name and type, and still nothing establishes the new rule for the restored balance. The induction's assumption that every rule held before the next handler then has no basis.

The certificate check cannot catch this alone. Recomputing what a definition owes and checking every derivation detects a mismatched proof. It cannot tell which revision the data was governed by when a different definition, internally valid, arrives with its own valid certificate (`outside-review-report-2-followup-2.md:86`, checked against `technical-precedent.md:127`).

**What has to change.** Before step 11 closes, distinguish restoring under the same definition, which stays trusted and unchecked per Want:141, from starting under a different definition. The design must say what identifies the governing definition, what establishes the new rules, and when no governed version is returned. The second reviewer rates this critical. I rate it major: the guarantee as currently written does not claim to cover this case, and the defect is the order of work. Left as is, though, it reopens the signed-off induction.

### 5. The writer list misses the third field that `dequeue … into … by` writes (major)

**What is true.** The spec's table of writers lists "The `into` binding target … writing a **second** field" (Spec:1990), and its note says "`dequeue Q into D` writes both the collection and `D`" (Spec:1997). The collection design adds an optional capture: "`-> dequeue ClaimQueue into CurrentClaim by CurrentSeverity` … `CurrentSeverity` receives the dequeued element's ordering value" (collection-types.md:1205-1210). It is approved syntax (collection-types.md:1288).

That makes three destinations. Neither the plan nor any technical input mentions the ordering capture. `technical-proof-model.md:132` names only "the second field written by `dequeue … into`". Plan:129 counts "five of eight uncatalogued (spec:1999)", and the bank example that seeds the frozen list (Plan:817-818) has no collection action at all.

**What has to change.** Derive the write destinations from every permitted form of each action, optional captures included, not one row per action name. For each destination, give a rule that reads only that field, so the inventory can be tested.

### 6. Building the runtime checks only from the premises proofs cite would drop required ingress checks (major)

**What is true.** Step 10 produces "the run-time checks derived from the premise list" (Plan:545). Step 7 says "the search can change without touching soundness" (Plan:490-491). Both rest on the want document: "the premise list **is** the runtime's validation, … derived from the proof, never maintained in parallel" (Want:210). The input says the list "follows from the certificate format — collect every premise cited by any certificate" (`technical-precedent.md:136`).

The owner's text also requires more than that. "For an arg, that's its modifier-rules" are evaluated at ingress (Want:143). What runs at run time is "ingress validation of args and editable fields, guards" (Want:165). His example contains a declared bound that "no *other* proof in the file consumes" (Want:129). The evaluator already evaluates every declared constraint on every supplied argument (evaluator.md:1366-1369).

So take an argument declared `positive` that no proof uses. Minus one must still be refused at ingress, but a union of the premises that proofs cited would not contain the check. And if two valid proofs of the same definition cite different bounds, deriving checks from whichever one the search found would make business behaviour depend on the search, which Plan:490-491 says must not happen.

**What has to change.** Step 10 must account for two duties: every declared ingress constraint is enforced, and every premise a proof uses is justified. They can share one record. The checker must detect a missing ingress check that no later proof relied on. Want:143/:165 and Want:210 have to be reconciled inside the design, not by choosing one of them.

### 7. Two arithmetic claims in the inputs would allow unsound proofs, and the plan inherits one of them (major)

**What is true.** `technical-proof-model.md:331` says a product "with a field that no handler in question writes, is linear for that handler's purposes". `technical-proof-model.md:332` says a product divisor is non-zero when both factors are, "true for integer and decimal; false for `number`".

Both are wrong. A field the handler does not write still varies from one configuration to the next, so a product of two fields is not a linear expression in both of them. On underflow, `phase-5/where-a-divisors-non-zeroness-comes-from.md:17-21` says plainly that two decimal values of `0.0000000000000001` multiply to a stored zero, and the earlier plan warns "the underflow is not confined to `number`" (`compiler-readiness-plan.md:281`).

The plan confines the problem to `number` in two places. One is contents item 7, "including a product of two non-zero `number` values that rounds to zero" (Plan:135). The other is a hard problem titled "A product of two non-zero `number` values can be exactly zero" (Plan:167).

Canon's own sign rules have the same flaw: "Multiplication: positive × positive = positive" and "Division: positive ÷ positive = positive" (proof-engine.md:1473-1474). That is my reasoning, not a reviewer's. With the 28-fractional-place limit that `primitive-types.md:239` implies, a quotient of two positive values can round to zero. That is harmless for a `nonnegative` conclusion and unsound for a non-zero divisor.

**What has to change.** Widen the zero-product hard problem and item 7 to every finite arithmetic model step 2 considers, `decimal` and the decimal-backed business types included. Give each a smallest-positive-value example. Have the rewrite rules say whether a coefficient is known, unchanged or merely bounded. Import neither input sentence as a proof rule, and list proof-engine.md:1473-1474 among the passages step 6 replaces.

### 8. The plan promises a repair suggestion that cannot always exist (major)

**What is true.** The input says "a guard that closes the proof always exists and can be printed" (`technical-proof-model.md:412`). The plan imports the problem with that source (Plan:175) and asks step 8 for "the rule for telling 'add a guard' from 'bound the argument' from 'add a rule'" (Plan:506-507). It also asks for "How the diagnostic tells this apart from a missing guard" for rules that are true but not inductive (Plan:176).

Consider an action that unconditionally assigns −1 to a field declared `nonnegative`. Substituting the assignment gives false. The only guard that closes the proof is always false, and that makes a dead row, which canon rejects (diagnostic-system.md:189, `UnsatisfiableGuard`).

There is a second problem. A failed preservation step gives a configuration that the declared rules allow, which need not be reachable (`technical-precedent.md:67`). From that alone the compiler cannot know whether the author left out a supporting rule or wrote an unsafe handler.

**What has to change.** Separate a replayable run, a counterexample to preservation from the stated rules, and a search that hit its cap. Promise a suggested premise only when one is expressible, consistent with the rules and would close the proof. Otherwise name the assignment or rule that must change. This keeps Want:24 ("the diagnostic names the missing premise") wherever a premise can exist. It does not invent one where none can.

### 9. The plan's non-linear restriction, read as written, refuses a proof the owner accepted (major)

**What is true.** Plan:164 says "sign and monotonicity facts may be used only inside a permitted rewrite that shows an author's stated premise is that condition. They may not prove a non-linear rule from field bounds alone."

In the owner's example, the `nonnegative` bound on `MonthlyRepayment` after `set MonthlyRepayment = -Balance / Months` is closed "by the guard (`Balance < 0.0` makes `-Balance` positive)" together with "`Months positive`" (Want:104). That is a sign proof through a quotient. The guard does not restate the post-state condition, so on a literal reading the plan's rule refuses it. Want:22 ("Modifiers are sugar for rules") means the same obligation written as a rule must get the same proof.

The owner drew the line at "A relational rule whose post-state condition is non-linear" (Want:135), such as the repayment cap. That is narrower than any product or quotient. The plan's escape route, sending a case that "field bounds would prove" to the owner as sheet 14 (Plan:164, :994), would re-ask a question Want:104 already answers. The bank proof uses a guard and an argument constraint, not field bounds.

The first reviewer's section 3.2 is right about this conflict. Its supporting citation, "Strategy 10", is wrong: the sign inference is in Strategy 7 (proof-engine.md:1442-1476), and Strategy 10 is "Count Containment Proof" (proof-engine.md:1685).

**What has to change.** Scope the restriction to Want:135's relational non-linear rules. Record Want:104's sign proof, and the refusal when the cap conjunct is removed (Want:121), as accepted results that step 6 must reproduce. Keep representability and overflow as separate obligations.

### 10. The conflict inventory comes from an input that only searched the type documents, and real conflicts are missing (major)

**What is true.** The plan copies the consistency input's conflicts as its inventory: "58 open" (Plan:187-191). That input says the type documents were "searched, not read" (`technical-canon-consistency.md:243`). Searching the plan and the inputs for these items finds none of the following:

- `business-domain-types.md:1917` says the value types have "No arithmetic operators, no validation logic". :2016-2018 says to "surface a `ConstraintViolation`" and to "Compute via the typed value object's operator overload".
- :1823 calls `QualifierMismatch` "the proven-violation-only policy". :1826 says "Without the guard, `QualifierMismatch` fires (unproven …)".
- :1878 enforces the implicit `positive` on exchange rates at "`set` time (runtime)". This is against Want:165, "no re-checking of proven bounds on writes". (The first reviewer cited :1892 and the second :1879; the sentence is at :1878.)
- :1719 says "No dedicated `units { }` block syntax", and :2037 relies on an "entity-scoped units block".

Each of these is a conflict between two statements in canon, or between canon and the want document, that the design must resolve.

**What has to change.** Before treating the inventory as complete, read the type documents for conflicts rather than relying on the search the input describes, and give each distinct decision its own row.

### 11. The plan names only logarithmic units as outside the exact model; two more cases are inexact (major)

**What is true.** The plan's hard problem cites only "logarithmic units such as `dB` and `[pH]`" (Plan:168), following proof-engine.md:109 and Spec:1456. Two other cases are also inexact.

The first is the plane-angle family. `catalog-system.md:1209` says `ScaleIsRational` "is `false` for units whose scale-to-base is irrational — the plane-angle family (`rad`/`deg`/`gon`/`'`/`''`…)".

The second is duration cancellation. `business-domain-types.md:1848` says `duration.ToInt64Nanoseconds()` "always yields an exact conversion to any time unit", but :1853 and :2010 specify the conversion as division, `(decimal)duration.ToInt64Nanoseconds() / <nanoseconds-per-unit>m`. One second in minutes is 1/60, which has no finite decimal form. The cast is exact and the division is not.

**What has to change.** The step 2 and step 5 model must distinguish identity conversions, exact rational factors, approximate factors and rounded results, and must state an outcome for each.

### 12. Several runtime and catalog contradictions in canon are missing from the inventory (minor)

**What is true.** Each of these was checked at its line:

- **Storage representation.** `precept-builder.md:771` says "entity state is a contiguous `object?[]`" and :819 shows `new Stack<PreceptValue>()`. `evaluator.md:223` uses `PreceptValue[]`, and :543-548 says "no heap allocation per evaluation" with `stackalloc`.
- **CLR type table.** `runtime-api.md:770-791` lists `text`, `list of T keyed by K` and `map K to T`, which are not current collection names. `stateref` is a real internal type (`literal-system.md:234`), so it should not be dropped just because it appears in the table.
- **Defensive faults.** `precept-grammar.md:855` calls the fault catalog "defensive coverage for paths the compiler proved safe", against Want:165's "fault-prevention checks are omitted at evaluation time".
- **Catalog authority.** `soundness-and-coverage.md:200-201` says the lists "are closed at those source files". The class of repair is already assigned to step 14 (Plan:607-609), but this passage is not named.

**What has to change.** Add the first three as conflict rows. Name the fourth passage under the existing step-14 duty.

### 13. The fault deferral is stated in four canonical places, and the plan corrects one (minor)

**What is true.** The owner's words are "seems like something we can defer until we start building the runtime" (`compiler-readiness-plan.md:273`), and the plan quotes them correctly (Plan:894). Canon says "until the runtime design is done" in four places: `runtime/README.md:36`, `fault-system.md:320`, `evaluator.md:1629` and `evaluator.md:2015`. Step 14 names only `docs/runtime/README.md:36` (Plan:615-616).

The first reviewer said the plan misstated his condition. That part is wrong, as the second reviewer found. The second reviewer cited two of the stale copies; `evaluator.md:1629` is a third.

**What has to change.** Correct all four, and keep the dependent fault-context choices conditional (`fault-system.md:310`).

### 14. The plan inherits a conflict between the dataflow exclusion and the induction that does not exist (minor)

**What is true.** The plan lists, among permanent limits to replace, "an exclusion of general dataflow that cannot stand beside the induction paragraph" (Plan:204). This follows `technical-canon-consistency.md:162`.

Proof-engine.md:2606 excludes "tracking values across multiple transitions". The paragraph after it says the inductive step "is one handler applied once" (:2612), which needs no tracking across transitions. The real gap is the next paragraph: "there is no general carrying of 'every rule held before this operation' into a handler's discharge" (:2614).

**What has to change.** Drop the claimed conflict. Keep the gap at :2614. Do not use the exclusion to forbid the reasoning across several operations that the reachability standard in finding 2 needs.

### 15. Other input statements that are wrong or too broad to copy into canon (minor)

Each was checked against the input and canon. None needs an owner decision. Each would mislead whoever carries it into canon.

- **`technical-proof-model.md:122`.** It says whether an undefaulted field means zero "is not stated anywhere I found". The spec does state it: construction without the required assignments is refused, and an early read "would observe an undefined value" (Spec:2085, :2094-2097). The fold's zero is not the language's meaning.
- **`technical-proof-model.md:183`.** It says "periods have no ordering". Two periods pinned to the same single unit are ordered (temporal-type-system.md:1329, :1483).
- **`technical-proof-model.md:339`.** It says "Proofs by identical expression are sound under any deterministic arithmetic." That holds only if the operands are the same at both evaluations. Exit actions and earlier actions in the chain run between the guard and a later write (Spec:2007-2010).
- **`technical-proof-model.md:369` and `technical-precedent.md:211-217`.** They say "`each P` survives a removal for free". That fails when the predicate reads other values the action changes, such as the ordering capture in finding 5, or the collection's own count.
- **`technical-proof-model.md:404`.** It calls an edit "the one place where a rule is evaluated at all". Want:143 also has argument modifier-rules evaluated at ingress.
- **`technical-proof-model.md:314` and `technical-precedent.md:17`.** They say "unresolved" "can always mean 'it is not true, and here is a case'" and "There is no third outcome". Those statements hold only over the rationals with consistent premises. They ignore integer-only consequences, which the precedent itself gives at :92 (`2x <= 1`). They ignore premises that contradict each other. They ignore the cap, at which Spec:225 requires an honest refusal.
- **`technical-precedent.md:197`.** It says temporal overflow "has no premise to prove from". `temporal-type-system.md:628` and `primitive-types.md:701` both offer "guard-only closure".
- **`technical-precedent.md:239`.** It says exact linear reasoning "is affordable on every keystroke". The cited 44 ms figure measures today's compiler, not the proposed prover and checker.
- **`technical-canon-consistency.md:38`.** It says "want:106 and want:129 repeat" the wider edit check. Neither does; only Want:143 states it.
- **`technical-canon-consistency.md:52`.** It says ten bank rows "have no named diagnostic". `DivisionByZero` and the bounds codes exist (diagnostic-system.md:181). Also, two rows describe one edit: removing the restating cap conjunct (Want:121 and :123).
- **`technical-canon-consistency.md:170`.** It says "no document says what the runtime does then". `temporal-type-system.md:626` says an out-of-range instant operation "faults at runtime". That is not an acceptable answer, but canon is not silent.
- **`technical-canon-consistency.md:188`.** It says that at an unmatched construction "no author wrote one" (a reason). `runtime-api.md:169` gives the synthesized reason "No construction row matched arguments", and `mcp.md:55-57` plans `NoMatchingRow`. The real question is whether this caller gets "not offered" or "refused" (Want:154).
- **`technical-canon-consistency.md:109`.** It calls strategy-type numbering "Mechanical once one is picked". `Prospect` is implicitly numbered from zero in `result-types.md:171` and explicitly from one in `evaluator.md:402`. If a caller can see the numbers, the choice is not mechanical.
- **`technical-canon-consistency.md:218` and :232.** The summary missed the `required` conflict in finding 3. The total of 58 counts evidence rows, some of which fold into others (:115), so it is not a count of independent questions.

---

## Part 2. Not confirmed as defects in the plan

- **The first reviewer's 3.1 claim that the plan overrides locked canon on reachability.** Want:151 says "Every state is reachable" and "A state no path reaches is rejected". Spec:191's over-approximation contradicts that, so canon is what has to change. The plan offers a replayable run as "One candidate" (Plan:177) and does not prescribe a search. The second reviewer's ruling holds. The risk of refusing reasonable definitions is real and is handled by Plan:177's route to the owner and by the standard in finding 2.
- **The first reviewer's 3.6 claim about a mandatory one-pass shortcut.** Plan:523 says "ends in one pass, or the order that makes it end", and :528 separates termination from soundness.
- **The first reviewer's 3.8 claim that nobody owns the boundary between the proof ledger and the runtime.** The contradiction is real: proof-engine.md:106 says "nothing from it crosses into runtime", while precept-builder.md:117 builds an index from `ProofLedger.ConstraintInfluenceEntries`. But Plan:212 lists "nothing from the proof ledger crosses into runtime" under step 10, and Plan:545 requires the certificate's route to the runtime. The worked transfer the second reviewer suggests is a good test, but it does not reveal a defect.
- **The first reviewer's 3.9 claim that editable-field ingress is treated as settled.** Plan:432-434 makes it a step-4 wait with three written options, and sheet 1 calls it "an open question with three options, not a confirmation" (Plan:976). That agrees with `trace-walk-rulings.md:42-46`.
- **The first reviewer's 3.7 claim that agents will invent public API.** Some choices are visible to callers (`result-types.md:123`, `precept-builder.md:641`, `literal-system.md:554`, `collection-types.md:533`). But the plan limits default answers to "a question nobody outside the compiler or runtime can observe" (Plan:713), and steps 10 and 12 own those contracts. Whether each one needs the owner is an owner-workload question for the other review group, not a technical defect.
- **The second reviewer's objection to `technical-proof-model.md:353`.** That input claim is wrong, but Plan:166 already corrects it: "That is evidence about the `decimal` model only".
- **The second reviewer's objection to `technical-canon-consistency.md:67`, that the checker only walks the record.** The plan already requires the checker to recompute what a definition owes (Plan:175 and step 10, Plan:543).
- **The second reviewer's objection to `technical-canon-consistency.md:51` and :86 about state ensures at construction.** Plan:197 already lists that the base case "leaves out `to` ensures of the initial state", and Plan:902 routes no-write arrivals to step 4.
- **The second reviewer's objection to `technical-proof-model.md:129-133`.** The input's next bullet (:133) already requires a row entering a state to establish that state's ensures even when it writes no field. Fault obligations in a refusal's message are covered by the plan's contents item 6, "every position where an expression is evaluated" (Plan:134).
- **The second reviewer's objection to `technical-proof-model.md:457`, that it conflates "no rule sweep" with "no evaluation".** That line is one entry in a list of canon contradictions. It sets Spec:1949 against Spec:2151; it does not assert either one.
- **The second reviewer's objection to the precedent input's Farkas wording (:87, :252) as a mandated encoding.** The plan frames it as a question: "Whether linear obligations are decided completely, with the multipliers … as the certificate" (Plan:164).
- **The second reviewer's objection to the precedent input's "Exact rationals remove the problem for the prover" (:40, :180).** It is correct that this applies only to the prover. The plan already has a separate hard problem, "The evaluator's arithmetic is not the arithmetic of proofs" (Plan:165).

---

## Part 3. Where the plan relies on an input claim that is wrong

1. **Plan:135 and Plan:167** confine the zero product to `number`, following `technical-proof-model.md:332`. Decimal underflows too (finding 7).
2. **Plan:164** lists "products that are linear in disguise", taken from `technical-proof-model.md:331`. That presumes an unwritten field acts as a constant across configurations (finding 7).
3. **Plan:177, Plan:471-473 and Plan:525-526** adopt the dead-end test "exhibit an input that satisfies some exit row's guard" from `technical-precedent.md:228` (finding 2).
4. **Plan:141** rates reachability Full, following `technical-proof-model.md:253`, so the `required` dominance conflict is never inventoried (finding 3).
5. **Plan:129 and the writer list in step 1** follow Spec:1990-1999 and `technical-proof-model.md:132`. Those count the `dequeue` destination as a "second" field and miss the ordering capture (finding 5).
6. **Plan:545**, which derives the run-time checks from the premise list, rests on `technical-precedent.md:136`'s "collect every premise cited by any certificate" (finding 6).
7. **Plan:175 and Plan:506-507** take `technical-proof-model.md:412`'s "a guard that closes the proof always exists". **Plan:176** takes `technical-precedent.md:254`'s promise that the diagnostic can tell a non-inductive rule from a missing guard (finding 8).
8. **Plan:204** takes `technical-canon-consistency.md:162`'s claim that the dataflow exclusion and the induction "cannot both stand" (finding 14).
9. **Plan:168** takes `technical-canon-consistency.md:163` and canon's statement that only logarithmic units fall outside the exact model (finding 11).
10. **Plan:187-191** treats the consistency input's 58 rows as the conflict inventory, although that input only searched the type documents (`technical-canon-consistency.md:243`). Real conflicts are missing as a result (findings 10 and 12).
11. **Plan:143** carries "Mostly Full" for the runtime from `technical-proof-model.md:285-304`. That table rates "Inspect an event without firing it" and "Restore with structural checks only" as Full, and this is why inspection and restore under a new definition are scheduled after the proof sign-off (findings 1 and 4).
12. **Plan:994 (sheet 14) and Plan:164** treat a non-linear case that field bounds would prove as a possible owner question. They do not record that Want:104 already accepts a sign proof through a quotient from a guard and an argument constraint (finding 9).
