# The proof model, read as a technical requirement

Written 2026-11-22. Nothing in the repository was edited. Every factual claim cites `file:line`. Paths are relative to `/home/sfalik/source/repos/Precept/docs/` unless they start with `want:`, which means `docs/Working/what-i-want-2026-07-16.md`.

Short names used for files:

| Short name | File |
|---|---|
| want | `Working/what-i-want-2026-07-16.md` |
| spec | `language/precept-language-spec.md` |
| engine | `compiler/proof-engine.md` |
| soundness | `compiler/soundness-and-coverage.md` |
| graph | `compiler/graph-analyzer.md` |
| arch | `compiler-and-runtime-design.md` |
| api | `runtime/runtime-api.md` |
| evaluator | `runtime/evaluator.md` |
| builder | `runtime/precept-builder.md` |
| primitive, temporal, business, collection | the four type documents in `language/` |

No compiler output, test result or sample file was used as evidence for anything below.

## How to read the status words

For each piece of the design, canon is in one of four conditions:

- **Full** — one canonical document states it completely enough to build and to test against.
- **Partial** — the idea is stated, but something a builder or a test writer needs is missing.
- **Contradicted** — two canonical statements disagree, or a canonical statement disagrees with the want document.
- **Silent** — no canonical document states it.

## The single most important finding

The spec states the proof as an induction in one paragraph (spec:268), and the architecture document and the proof engine document repeat it (arch:149, engine:2612). The want document asks for exactly this (want:16-20).

But the proof engine document, which is where the mechanism has to live, describes a different machine. Every obligation it creates comes from a requirement attached to an operator, a function, an accessor or an action, or from a write into a field that carries a declared bound (engine:376-387, engine:2321-2323). There is no obligation that says "this handler must leave this authored rule holding", and none that says "this initial event must make this authored rule true". The list of obligation kinds has neither (engine:489). The document says so itself: there is "no general carrying of 'every rule held before this operation' into a handler's discharge" (engine:2614).

So for an authored relational rule such as `rule Balance >= -OverdraftLimit`, canon states the promise and is silent on the mechanism: what obligations are generated, at which sites, from which premises, and how they are discharged. Everything in part (a) below that is marked Silent or Partial traces back to this.

---

## Part 1 — What the want document requires, as a numbered list

| # | Requirement | Source |
|---|---|---|
| R1 | Every business rule is proved or the definition is rejected | want:14 |
| R2 | The proof is an induction over reachable configurations: a base case and an inductive step | want:16-19 |
| R3 | Base case: the default configuration satisfies every rule, and every initial event establishes every rule from the constraints on its arguments | want:18 |
| R4 | Inductive step: every handler preserves every rule, from four kinds of premise — field modifiers, argument constraints, the handler's guard, all rules holding beforehand | want:19 |
| R5 | A rule's obligations attach to every write site of every field the rule mentions | want:20, want:103 |
| R6 | Modifiers are rules; there is one constraint mechanism | want:22 |
| R7 | A rejection names the missing premise; the compiler computes the guard or argument constraint that would close the proof, shows it, and never inserts it | want:24, want:179 |
| R8 | Faults (division by zero, overflow, out of range) get the same premise-and-certificate treatment as business rules | want:104 |
| R9 | A non-linear rule is proved only when a guard or argument constraint states the post-state condition directly; otherwise rejected; never checked after the mutation and never handed to a solver the compiler does not carry | want:105, want:135 |
| R10 | The default configuration is checked statically against modifiers and rules | want:106, want:124 |
| R11 | The fourteen one-line mutations of the worked example must each be rejected with the stated message | want:112-127 |
| R12 | Ingress is exactly two places: event arguments and editable-field writes. Restore is not ingress | want:141 |
| R13 | At ingress for an argument, its modifier rules are evaluated. For an editable-field write, the field's modifier rules and every rule that mentions the field are evaluated | want:143 |
| R14 | Every state reachable, no dead ends unless marked terminal, dead rows rejected, and a state reachable only through a dead row is unreachable | want:151-155, want:125-127 |
| R15 | The runtime omits fault checks and does not re-check proven bounds; it runs only the declared premises | want:165 |
| R16 | No deferral; the guarantee is uniform for the author | want:171-173 |
| R17 | The compiler emits a certificate: per obligation the theorem, the premises used, the derivation | want:199 |
| R18 | One small checker, used as a compiler stage and as a gate when the runtime loads a definition; an unproven, tampered or stale definition never governs | want:201-204 |
| R19 | The premise list in the certificate is the list of checks the runtime must run | want:210 |
| R20 | Inspect can cite where a proof came from | want:211 |
| R21 | The proof stops at the edge of one precept | want:193 |
| R22 | Open: whether a rule may refer to the current time | want:219 |

---

## Part 2 — (a) and (b): the pieces of the proof model, and what canon says about each

### 2.1 The system the induction runs over

**What the design must contain.** A precise definition of a configuration, of each operation that can change one, the order of the steps inside an operation, and a closed list of everything that can write a field. The induction is only as sound as that list: a writer the list omits is a change the proof never examined.

**Canon.**
- Configuration is state plus field values, or field values alone for a stateless precept — Full (spec:134, arch:100).
- Operations and the eight-step order inside one — Full for the order (spec:2003-2012).
- The list of eight writers — Partial. It is written down (spec:1984-1995), and the spec says in its own words that it is "asserted rather than kept" and that five of the eight have no catalog entry (spec:1999, spec:2205, spec:2209).
- Three things the order leaves open, each of which changes what a handler is: whether several state actions on one state all fire and in what order; whether entry actions fire at construction and on a self-transition (spec:2016, spec:2207); and whether `from any` expands to one handler per state (spec:2203).

**Why it matters here.** "Handler" in R4 has to mean the whole composite an operation performs: exit actions, the state change, the reset of omitted fields, the row's own actions, entry actions, and recomputation of computed fields. Until the three open items are settled, the unit the inductive step is stated over is not defined.

### 2.2 What counts as a rule

**What the design must contain.** One table that turns every constraint spelling into the statement the induction must keep true, and says in which configurations it must hold:

- an unguarded `rule` — holds in every configuration;
- a guarded `rule P when G` — the statement is "if G then P", in every configuration;
- each constraint modifier on each type, including what it means on an optional field;
- `in S ensure` — holds in every configuration whose state is S;
- `to S ensure` and `from S ensure` — statements about a transition, not about a configuration;
- `on E ensure` — a statement about arguments;
- a value modifier on a collection's element type — a statement about every element;
- a quantified rule (`each`, `any`, `no`);
- a declared bound on a computed field — a statement about the defining expression.

**Canon.**
- Modifiers are rule shorthand, proved identically — Full (spec:1143-1146, soundness:99-108).
- Guarded rules hold where the guard is true — Full as meaning (spec:1874).
- The four ensure anchors — Partial. Their meaning is stated (spec:1892-1895), and the base case names entry and residency ensures (spec:2090). The inductive paragraph speaks only of "every rule" (spec:268). `to` and `from` ensures are described as "checked on any transition" (spec:1893-1894), which reads as evaluation, in a document that says nothing is evaluated after a mutation (spec:2012). How a `to S ensure` turns into an obligation on each row entering S is Silent.
- What a modifier on an optional field means — Partial. The presence guard is spelled out only for the business types (business:1590).
- Quantified rules — Contradicted; see 2.9.
- `maxplaces` — Contradicted; see 2.7.

### 2.3 The base case

**What the design must contain.**
1. The default configuration: how it is built (defaults in declaration order, the value of a field with no default, computed fields), and the obligation that every statement from 2.2 holds of it.
2. Every initial event: an obligation, per rule, that the configuration the event commits satisfies the rule, using only the constraints on the event's arguments, the row's guard, and the defaults that survive.
3. The rule for a default that the initial event overwrites: when it need not satisfy the rules.
4. Construction with no initial event: the default configuration is itself the first committed configuration, so it must satisfy everything, including `in` and `to` ensures of the initial state.
5. Guarded rules: "if G then P" must be established, not skipped.

**Canon.**
- The statement — Full (spec:268, spec:2090, want:18).
- A default the initial event provably overwrites is exempt, per field — Full as a rule (spec:213, spec:2183-2185). What "provably overwrites" means when construction has several guarded rows is Silent.
- The mechanism — Contradicted, inside the engine document:
  - "Initial event arguments are NOT considered … that's a runtime guarantee, not a compile-time one" (engine:2052-2056). This denies R3 outright.
  - Guarded ensures are skipped and treated as satisfiable (engine:2058-2060), and an unknown result passes (engine:2064), while the same document says an unknown result must reject (engine:1933) and the spec agrees (spec:213).
  - Only `in` ensures of the initial state are collected, with a stated reason for leaving `to` ensures out (engine:1960), while the spec puts `to <InitialState> ensure` in the base case (spec:2090).
- A field with no default is given 0, the empty string or false for the fold (engine:1972-1975). Whether that is the language's meaning or a convenience of the fold is not stated anywhere I found.
- Stateless construction — Contradicted. The spec says that without an initial event "global rules are checked. The construction succeeds if these pass" (spec:2151), which describes a check at run time; the same document says construction has no constraint-failure outcome because nothing is evaluated (spec:1949).

### 2.4 The inductive step

**What the design must contain.** For every handler and every statement in 2.2 that must hold in the configuration the handler commits, an obligation: assuming the premises (2.5), the statement holds afterwards. With these parts:

- **Which pairs need work.** If a handler writes none of the fields a rule mentions, directly or through a computed field, the rule carries over with no argument needed. This is what keeps the number of obligations manageable, and it depends on the closed writer list of 2.1 and on following computed fields back to the stored fields they read.
- **Every write site of every mentioned field** (R5). Canon states this for rules (spec:270, arch:127).
- **A change of state is a write.** A row that moves to state S must establish every `in S ensure` even if it changes no field, and must satisfy `to S` and `from S'` ensures.
- **The effect of a handler as a substitution.** After `set Balance = Balance - Withdraw.Amount`, the rule about the new balance is a statement about the old balance and the argument. The design must define this for every action kind, including the second field written by `dequeue … into` (spec:1997), `clear`, the reset of omitted fields, and collection actions.
- **Rows that write nothing** (a `reject` row, an unmatched event) need no obligation.
- **Editable-field writes** are handlers too; see 2.11.

**Canon.** The model — Full as a sentence (spec:268, engine:2612). The mechanism — Silent. No obligation kind (engine:489), no generation rule (engine:376-387), and the engine document records the absence (engine:2614). The CLAUDE.md catalog notice records the same thing about the catalog.

What canon does have is narrower: a declared rule is used as a fact that helps discharge *other* obligations, and only when the rule is unconditional and relates two fields (spec:256, engine:2451-2455).

### 2.5 What a proof may rely on

**What the design must contain.** The complete premise set at a site, and a statement that nothing else may be used.

| Premise | Notes | Canon |
|---|---|---|
| Field modifiers | These are rules (2.2), so this is a special case of "rules held beforehand" | Full (spec:268) |
| Constraints on event arguments, and `on E ensure` | The want says relational conditions on an event live in its guards (want:143); canon also has event ensures evaluated at ingress (spec:1895, evaluator:1379). The design must say which premise class an event ensure belongs to | Partial |
| The handler's guard | Full as a class (spec:268) | Full |
| The negation of every earlier row's guard for the same state and event | Rows are first-match (spec:1912), so row three runs only when rows one and two did not match. Canon uses this only when the earlier row is a `reject` row with a single comparison, and gives it up otherwise (engine:451-453) | Partial |
| Every rule holding beforehand, including "if G then P" for guarded rules and `in S` ensures for the source state | Canon: only unconditional field-to-field rules contribute, and a guarded rule contributes nothing (engine:2455, engine:2614) | Partial |
| Facts made true by earlier actions in the same chain | Canon states sequential flow and invalidation on reassignment (spec:233, spec:251-254) | Full for the cases listed |
| Facts intrinsic to a type (a choice value is one of its declared values; a count is not negative) | Scattered | Partial |
| The value an editable write was checked against at ingress | See 2.11 | Silent |

Two things a proof may **not** rely on, both stated in canon: the actual run-time value (arch:153), and anything outside the file (spec:227).

One existing strategy sits outside the premise list. Strategy 7 collects every assignment to a field across all rows and concludes a sign for the field from them (engine:1444-1446). That is an inferred fact the author never declared. Under R4 a fact about a field is a declared rule, proved once and then assumed. Whether inferred facts of this kind are allowed is something the design has to state; canon does not notice the difference.

The assumption about restored data — a rule "held beforehand" is proved for data that came through a declared operation and assumed for restored data — is Full (spec:278-280).

### 2.6 Fault obligations

**What the design must contain.** The same obligation shape as a rule, attached to every position where an expression is evaluated: action right-hand sides, guards of every kind, rule and ensure conditions, computed-field expressions, default expressions, and the text holes in messages. Rule conditions and guards matter doubly because the runtime evaluates them at ingress and at dispatch (R13, R15), so they must be proved unable to fault with only the premises true at that moment.

**Canon.**
- Division, square root, empty-collection access, presence, qualifier compatibility, bounds on writes — Full as commitments (soundness:220-236).
- Sites — Partial. Guard positions, member-call arguments, field and argument default expressions, and message holes are committed but recorded as not yet enrolled (soundness:256-263).
- `and` and `or` stop early (primitive:305-306), so the left operand is a premise for the right. Canon does not state this as a premise rule.
- Overflow — see Part 3, problem 4.

### 2.7 What each numeric type requires

**Integer.** Exact arithmetic on whole numbers; division truncates (primitive:206). Needs: intervals, linear relations between fields, exact handling of strict comparisons (engine:455 does this), and an answer on overflow. Canon: Partial; overflow undecided (primitive:697).

**Decimal.** Called exact (primitive:82), but it is a finite format: division gives "full 28-digit precision" (primitive:239), so `1 / 3` is rounded, and addition and multiplication can exceed the format (primitive:236). A proof done in ordinary real-number algebra is therefore not automatically true of what the evaluator computes. Needs: a stated rounding behaviour for every operator, an overflow answer, and a rule for which algebraic steps a proof may take. Canon: Silent on the proof side — the engine lists "precision propagation awareness" as a future consideration (engine:2563). See Part 3, problem 3.

**`maxplaces`.** It is listed among the modifiers that are rule shorthand (spec:1143), so keeping a value within N decimal places is rule preservation and must be proved. The business-types document says the opposite: "Runtime enforcement" in the modifier table (business:1577) and "a constraint violation at `set` time" for an arithmetic result (business:1584). Contradicted. Proving it needs a way to bound the number of decimal places of an expression — sum, product, quotient, `round`.

**Number.** Approximate by design (primitive:83). Not-a-number is prevented by obligations plus a check at ingress (primitive:662). Infinity is undecided (primitive:663, primitive:699). Two non-zero operands multiplying to exactly zero, which breaks the proof that a product is a safe divisor, is recorded as open and unrouted (primitive:667). The engine "cannot prove exact equality on `number`" (primitive:83) — the design must say what it *can* prove: which inequalities survive rounding. Canon: Partial.

### 2.8 Temporal, quantity and money

**Temporal.** Durations and instants are counts of fixed units, so they behave like integers. Calendar periods do not: adding a month clamps at month end (temporal:399, temporal:444), periods have no ordering (temporal:1485), zoned values have no ordering (temporal:956), and conversion through a time zone depends on zone data and on a fixed gap-and-overlap policy (temporal:1096-1101). Point types accept no `min`/`max`, so there is no declared premise that bounds a date or an instant (temporal:628). Range overflow is undecided (temporal:626). Canon for proof: Partial for the type rules and divisor safety (temporal:707-711), Silent on how a rule comparing dates is preserved through period arithmetic.

**The current time.** The want leaves open whether a rule may mention it (want:219). The temporal document shows exactly such a rule as a usage pattern, `rule now() - IncidentAt <= '72 hours'` (temporal:577), and accepts that such a rule gives different results at different times (temporal:568). A rule that can become false with no operation in flight cannot be an invariant of the induction. Contradicted, and it needs the owner: the want says open, canon shows it as settled.

**Quantity, money, price, exchange rate.** Two layers. The magnitude is a decimal and inherits everything in 2.7. The qualifier (currency, unit, dimension) needs its own obligations: compatibility, conversion chains, products of dimensions — Full as commitments (soundness:232, spec:2189-2193). Comparison across units normalises to base units before comparing (spec:207), exact only for units whose factors are representable (engine:109). When the currency or unit is not fixed by the declaration and "comes from event data" (business:443), the compatibility proof has to come from a guard; canon states the pattern (business:811) but not as a premise rule.

### 2.9 Strings, choices, optional values, collections, computed fields

**Strings.** Length is an integer interval carried through concatenation, conditionals, and the length-preserving functions — Full for length (spec:249, engine:1669-1681). Equality, case-insensitive equality, prefix and suffix tests in rules are Silent as proof facts.

**Choices.** A closed set of values; membership is decided by the type checker (spec:1750-1763). Rules that compare a choice field to a value, used as the guard of another rule (`when LoanType == "conventional"`, spec:1871), need reasoning by cases over the declared values. Silent.

**Optional values.** Reading an optional needs proof that it is set (soundness:226), and the rule is uniform at every position (spec:1561, soundness:263). Needs: presence as a fact that guards, `clear`, `set` and the reset of omitted fields create and destroy.

One conflict with the want's own example: the `ReduceLimit` guard reads `MonthlyRepayment`, which is optional, with no presence test (want:75), and the want calls every handler provable (want:98). Under canon's uniform rule that guard is refused. Either the example needs a presence test or comparisons on an unset value need a defined meaning. Contradicted (want against canon).

**Collections.** Canon has: count as an interval advanced by each action (spec:258, engine:1215), non-emptiness for accessors, index and key presence, element bounds at write and read sites (collection:625-628), and a uniqueness precondition for keyed logs (collection:431). Recorded as incomplete: membership facts across a removal (engine:1215), key-presence reads for lookups (soundness:230).

**Quantified rules.** `rule each item in Items (item > 0)` is approved syntax (collection:818). The collection document calls such a rule "a runtime governance rule over the current elements" (collection:796). Under R1 and R16 it is a rule like any other and must be proved preserved by every action that changes the collection and by every write to any other field the predicate mentions. Contradicted. The proof side is Silent. The spec's remark that a quantifier "unfolds to a finite conjunction" (spec:160) is true of evaluation; it gives no proof method, because the number of elements is not known at compile time.

**Computed fields.** A computed field is a function of stored fields, recomputed in every operation (spec:1992, spec:2011). A bound on it is an obligation on its expression (engine:562). A rule that mentions it is really about the stored fields underneath (spec:1968), so every write to those is a write site for the rule. Canon: Partial — the pieces are stated, the consequence for write sites is not.

### 2.10 Guards, relational rules, transitions, stateless definitions

**Guards.** Three roles: a premise for the row's proofs; an expression that must itself be unable to fault; and a subject of the dead-row check. First two covered above. Dead rows: 2.13.

**Linear relational rules.** The want's worked example needs three distinct arguments:
- `Withdraw`: the guard, after substituting the assignment, *is* the rule (want:102).
- `OpenAccount`: two separate bounds, `OpeningBalance >= 0` and `OverdraftLimit >= 0`, combine to give `Balance >= -OverdraftLimit` (want:100).
- `Deposit`: the rule holding beforehand plus the argument bound `Amount >= 0` combine to give the rule afterwards (want:101).

The second and third each combine two separately declared facts into a conclusion neither states. Canon says the engine "never combines two separately-declared facts to derive a bound neither states alone" (spec:256), calls this a deliberate limit, lists combination as out of scope (engine:2539), and names the single-step limit a soundness boundary (soundness:332-337). As literally written, canon refuses the want's own `Deposit` and `OpenAccount`. Contradicted. A strategy named "§1a (multi-term single-fact + guard-sum matching)" is listed as designed (engine:2535) and may be meant to cover this, but its definition is not in any canonical document; the name refers to a section number that canon does not contain.

**Non-linear relational rules.** Full as a rule of the design (spec:270, matching want:135). Partial as a mechanism: what "states the post-state condition directly" means — identical text, or equal after some set of rewrites — is not defined. See Part 3, problem 2.

**Transitions.** Covered in 2.4. The state change, state-scoped ensures and the reset of omitted fields are Silent as obligations.

**Stateless definitions.** The same induction with one implicit state. Canon — Contradicted inside itself: the engine says guard-based strategies do not apply because a stateless handler "has no `Guard` field" (engine:2121-2122), while the spec allows guards on `on <Event>` rows and says stateless handlers share that row form (spec:2106, spec:2110). And the engine skips the base case entirely for a stateless precept (engine:2125), although its unguarded rules must hold of the default configuration like any others.

### 2.11 Ingress and editable fields

**What the design must contain.**
- For arguments: modifier rules and event ensures are evaluated before anything else, all failures reported together. Canon: Full (spec:274, spec:1911, evaluator:1359-1385).
- For an editable-field write: the field's modifier rules *and every rule that mentions the field* are evaluated against the value arriving (want:143). This is the premise that closes the editable write site for each of those rules. Canon: Silent to Contradicted. The spec says an incoming value is held to "the constraints declared on the slot it enters" (spec:274), which is narrower. The evaluator document says outright that what an edit evaluates beyond the field's own constraints "is a separate question, and this document does not settle it" (evaluator:1361, evaluator:1388).
- What "evaluate the rules that mention the field" means in detail: a patch may write several fields; a relational rule needs the values of the other fields; a rule may read a computed field that depends on the edited one. So the evaluation is over the configuration as it would be after the patch, restricted to the rules that mention a patched field. That has to be reconciled with the sentences that say there is no pass over the finished working copy (spec:1976, spec:2012).
- Those rule conditions must be unable to fault when evaluated on an arbitrary incoming value, so the order matters: modifier rules first, relational rules after.
- Which states allow the edit is part of the proof input: the write site exists only in states where the field is editable (want:106).
- The requirement that authors extend constraints to the entry points (want:139) is Partial in canon (spec:249 says the author closes a gap by declaring the bound on the source).

### 2.12 Discharge, verdicts and explanations

**What the design must contain.**
- A fixed set of inference steps, each small enough to check mechanically, and a statement of which class of obligation each one settles.
- For each class, whether the procedure always reaches an answer. For linear arithmetic it can; see Part 3, problem 1.
- Three verdicts: proved, proved violating with a concrete case, unresolved with the missing premise.
- How the concrete case is found and re-checked.
- How the missing premise is computed and written as something the author can type: a guard, or a constraint on an argument (R7).

**Canon.**
- Three verdicts, both non-proved ones reject, and the author can tell them apart — Full (spec:223, spec:229, `compiler/diagnostic-system.md`:181-183).
- The four admissibility conditions for a strategy — Full (spec:225, engine:2349).
- The strategy set — Partial. Ten or eleven strategies are described, aimed at fault and bound obligations; the document says the count is unsettled and the numbering disagrees with itself (engine:148-152). Four more are named but not defined in canon (engine:2535).
- The counterexample — Partial; a skeleton section of intent only (soundness:131-138).
- The missing premise — Partial; "the weakest precondition, printed verbatim" is named (spec:223, engine:2533) with no method for computing it or for choosing between "bound the argument" and "add a guard".

### 2.13 Structure of the state graph

**What the design must contain.** Reachability, dead ends, required states — and dead rows feeding back into reachability (R14): a row whose guard can never be true is removed, and a state reachable only through it is unreachable.

**Canon.**
- Reachability, dead ends, dominators — Full (spec:184-189, graph:76-88).
- Dead rows rejected — Full as a commitment (`compiler/diagnostic-system.md`:189; spec:211).
- Feeding dead rows back into reachability — Partial and at odds with the stage order. The graph analyzer runs before the proof engine (engine:96) and treats every row as traversable whatever its guard (spec:191). Sharpening reachability from dead guards is listed as "specification-only" (spec:245).
- The want's example of a dead row is a guard that contradicts a *relational rule*, `Balance < -OverdraftLimit` (want:127). Canon's dead-guard scan deliberately reads only each field's own declared interval and never the relational facts, and calls that isolation a soundness invariant (engine:445-447). So the scan as designed cannot see the want's example. Contradicted (want against canon).
- A row that can never fire because earlier rows always match first is not discussed. Silent.

### 2.14 Certificates and the checker

**What the design must contain.**
1. The record: per obligation, the statement proved, the premises each cited by source position, and the steps.
2. The closed vocabulary of steps.
3. The checker's own duties. Replaying steps is not enough: a missing obligation has no steps to replay (soundness:61-64). For "no unproven definition can govern" (want:204) the checker has to work out for itself which obligations the definition requires and confirm each has a derivation.
4. What ties a certificate to one definition, so a changed or outdated definition is detected.
5. How the certificate travels from the compiler to the runtime, and in what form.
6. The compiler stage that runs the checker.
7. The list of run-time checks derived from the premises (R19).
8. What the checker itself trusts. If the checker reads operator meanings and obligation declarations from the same catalog the engine reads, an error in the catalog passes both.

**Canon.**
- The commitment to all of it — Full (soundness:153-169, api:250-258, arch:171, `philosophy.md`:48).
- The format — Partial; a paragraph of intent (soundness:144-151).
- The step vocabulary — Silent. The catalog it is to be drawn from does not exist (`language/catalog-system.md`:103; CLAUDE.md catalog notice).
- How the certificate reaches the runtime — Contradicted and open. The engine document says nothing from the proof ledger crosses into the runtime (engine:106, engine:2364-2377) while also saying a proved verdict carries its certificate (engine:2533). The architecture document says the certificate does cross (arch:601). The builder document records that `Compilation` has no place for one and that the route is undecided (builder:643, builder:865). The list of what crosses into the executable model has no certificate (arch:362-373).
- The checker as a compiler stage — Silent as to where; the pipeline diagram ends at the proof engine and the builder (engine:96).
- Tying certificate to definition — Silent. A semantic hash of the definition is proposed for other purposes (arch:712-714) and not connected to the certificate.
- What happens when the gate refuses — open (builder:864).
- Item 3 — Silent beyond the remark that replay cannot see a missing obligation.

The philosophy sentence the want flags (want:215) has since been reworded; the current text says the proof travels with the definition and the engine verifies it before governing (`philosophy.md`:48). I did not edit that file.

### 2.15 What the runtime does and does not do

| The runtime does | Canon |
|---|---|
| Check the certificate once when a definition is loaded | Full as commitment (api:254) |
| Validate arguments at ingress, all failures together | Full (evaluator:1359) |
| Validate editable writes at ingress | Partial — see 2.11 |
| Enforce which fields may be edited in the current state | Full (spec:1929) |
| Evaluate guards in order, first match wins; honour `reject` rows | Full (spec:1912, spec:2006) |
| Run mutations on a working copy, recompute computed fields, commit | Full (spec:1974, spec:2005-2012) |
| Inspect an event without firing it | Full (spec:2157-2167) |
| Return a new immutable version every time | Full (want:163; arch:379) |
| Restore with structural checks only | Full (spec:1953, api:277) |

| The runtime does not | Canon |
|---|---|
| Evaluate rules against the finished configuration | Full (spec:1911, spec:2012), with the stray contradictions at spec:2151, business:1584, collection:796 |
| Test divisors, bounds or overflow during evaluation | Full (spec:110, arch:171) |
| Re-validate restored data | Full (spec:278) |
| Take over a proof the compiler could not finish | Full (spec:268) |

Still open in canon: which type carries proof provenance in an inspection result (evaluator:2016), and whether anything ever produces a `Fault` (evaluator:1627-1629).

---

## Part 3 — (c): the hard problems, each as a design problem with candidate techniques

None of these is a reason to prove less. Each is something the design has to answer.

### Problem 1 — Which obligations can always be decided, and the single-step limit

Most obligations in a definition like the worked example are linear: sums and differences of fields and arguments, compared with each other or with constants. Whether a set of linear premises implies a linear conclusion is decidable, for real numbers and for integers. So for this class, "unresolved" need never mean "the compiler did not find it"; it can always mean "it is not true, and here is a case".

Canon's single-step limit (spec:256) gives that up, and as shown in 2.10 it also refuses two handlers of the want's own example. The rationale canon gives — that combining facts adds convenience and not expressive power (engine:2539) — is an argument about what to build first. The owner's rule is that the design states full proof power.

Candidate techniques:
- **Linear combination with a checkable record.** If premises imply a conclusion, there is a set of non-negative multipliers such that the weighted sum of the premises is the conclusion (Farkas' lemma). The multipliers are the certificate; checking them is a few additions and multiplications. This fits the certificate conditions directly: readable ("add the rule that held before to `Amount >= 0`"), small, deterministic.
- **Finding the multipliers**: variable elimination (Fourier–Motzkin) or the simplex method. With the handful of variables one handler touches, either is fast.
- **Integers**: the same, plus rounding of bounds to whole numbers, or the Omega test.
- **Mixed conditions** (a guarded rule, an optional field, a choice value, a conditional expression, the negation of an earlier row's guard): split into cases and prove each. The number of cases is the cost; see problem 7.

The design decision to make: is the linear class decided completely, and if so what replaces the single-step wording in spec:256, engine:2539 and soundness:332-337.

### Problem 2 — Non-linear rules

The want sets the line itself: proved only when a guard or argument constraint makes the condition directly checkable (want:135). Three things remain to design.

- **What "directly" means.** Identical text after substituting the assignments is the minimum. Whether `A * B` matches `B * A`, or `(A / B) <= C` matches `A <= C * B`, has to be decided. Each permitted rewrite must be true of the machine arithmetic, not only of real numbers (problem 3); `A / B * C` and `A * C / B` differ once division rounds. Candidate: a fixed normal form for expressions with a short list of rewrites each proved exact, recorded as steps in the certificate.
- **Products that are linear in disguise.** A product with a constant, or with a field that no handler in question writes, is linear for that handler's purposes. The design should say so, or `Deposit`-style reasoning stops working the moment a rate is involved.
- **Non-linear fault obligations.** A divisor that is a product is non-zero when both factors are (true for integer and decimal; false for `number`, primitive:667). Signs of products and quotients, intervals of products. Candidates: sign rules and interval multiplication as certificate steps. For polynomial inequalities beyond that, there are checkable certificate forms (a sum of squares with multipliers); whether they belong in the design is a question of the want's own line at want:135, which puts them outside.

### Problem 3 — The arithmetic the evaluator performs is not the arithmetic of proofs

Decimal division rounds to 28 digits (primitive:239); `number` rounds on every operation (primitive:265). A proof by algebra over real numbers can conclude something the evaluator's result violates in the last digit. Example shape: a guard proves `-Balance / Months <= Limit * Cap`; the action stores `-Balance / Months`. That one is safe, because the guard and the action evaluate the same expression the same way. But a proof that rearranges — dividing both sides, cancelling a factor — is safe only if the rearrangement is exact.

This gives a useful division:
- **Proofs by identical expression** are sound under any deterministic arithmetic. This is exactly the want's rule for non-linear conditions.
- **Proofs by interval** are sound if bounds are rounded outward and every operator is monotone in its rounding.
- **Proofs by linear combination** assume addition and subtraction are exact. They are for integers barring overflow, and for decimals as long as no digits are lost.

Candidate techniques:
- Prove exactness as its own obligation: track the largest magnitude and number of decimal places an expression can have, and show it fits. This is the same analysis `maxplaces` needs (2.7) and the same intervals overflow needs (problem 4).
- Carry an explicit error bound through division and through `number` operations, and prove the conclusion with the bound included.
- Change the representation so the exact operations really are exact (unbounded integers; decimals of unbounded precision; division left as the one rounding operation with a declared mode).
- Whichever is chosen: one statement of every operator's exact behaviour, used by the evaluator, the proof engine and the checker alike. Canon has no such statement for decimal rounding mode in division.

### Problem 4 — Overflow

Canon's target is that overflow is proved absent (spec:110, spec:112) and its build is sequenced later (spec:272, soundness:312-317). The *model* is undecided: prove every operation stays in range, or remove the failure by changing representation (primitive:697). For `number` only proof is available (primitive:699). For temporal types there is no premise to prove from (temporal:628).

The want's own example bears on this decision. `Deposit` computes `Balance + Deposit.Amount` with no upper bound declared on either (want:34, want:53-55), and the want calls it provable (want:101). Under "prove every operation stays in a fixed range", that handler cannot be proved and the example would have to change. So either the representation removes overflow for decimal and integer, or the worked example and its acceptance are wrong as written. The design has to pick, and the want's example is evidence about which the owner expects.

Candidate techniques:
- Range obligations on every arithmetic node, discharged by intervals. Cost: every accumulating field needs a declared maximum and a guard.
- Unbounded integers and unbounded-precision decimals: no obligation; cost is in the evaluator and in storage.
- A declared range per type that ingress enforces, giving every argument a bound for free; sums still need proof.
- `number`: intervals with outward rounding, or reject arithmetic whose range cannot be bounded.
- Temporal: bounds vocabulary on point types is new language and needs the owner; the alternatives are guards only, or a representable-range fact enforced at ingress.

Related and unrouted: the product of two non-zero `number` values can be zero (primitive:667), which breaks a divisor proof.

### Problem 5 — Rules over the elements of a collection

The size of a collection is unknown at compile time, so a quantified rule cannot be proved by listing elements. It has to be proved once for all sizes.

What the design needs:
- For each of the action kinds on each of the nine collection kinds, what it does to an `each`, an `any` and a `no` statement. `each P` survives a removal for free and survives an insertion if the inserted value is proved to satisfy P. `any P` survives an insertion for free and does not survive a removal unless the removed element is shown not to be the only one satisfying P — which needs either a count of satisfying elements or a guard restating the condition after the removal.
- Predicates that mention other fields (`each x in C (x <= Limit)`): every write to `Limit` is a write site (R5), and lowering it needs a guard that restates the quantified condition with the new value.
- Reading an element gives the element the property the `each` rule states; element-type modifiers are the same idea and canon already has them (collection:625-628).
- Facts about membership and keys across mutations, which canon records as incomplete (engine:1215).
- The count of a set after an insertion grows by zero or one, depending on membership.
- Obligations inside the predicate itself, proved with the element's known properties as premises.

Candidate techniques: a fixed table of preservation steps per (action, quantifier), each a certificate step; for the general case, the decidable fragment of array reasoning known as the array property fragment (Bradley, Manna, Sipma), which covers `each`-style statements with insertions and reads.

### Problem 6 — Temporal arithmetic

- Instants and durations: counts of ticks. Linear arithmetic applies as for integers.
- Dates with day-based periods: counts of days. Same.
- Months and years: addition clamps at month end, so adding then subtracting a month need not return the start (temporal:444). Candidate: a small set of proved facts as certificate steps — adding a non-negative period never moves a date earlier; `d + n months` lies between `d + 28n days` and `d + 31n days`.
- Time of day wraps round; arithmetic is modular.
- Time-zone conversion depends on data outside the file. A proof may use only that the conversion is a function (same inputs, same output), nothing about its values. The design should say that.
- The current time: if allowed in guards only, it is a fresh unknown value per operation, usable as a premise for that operation and no further. If allowed in rules, the induction has no step that covers time passing (want:219). Owner decision; canon and want disagree (2.8).
- Range: problem 4.

### Problem 7 — Scale

The count of obligations is roughly rules times write sites times rows, multiplied by case splits. Canon requires recompiling a file on each keystroke (spec:225, condition 2) and expects files small enough that the whole pipeline re-runs (arch:708-710, arch:723). The load-time check must be fast (want:206).

Candidate techniques:
- Skip every (rule, handler) pair where the handler writes no field the rule depends on. This removes most pairs and needs the closed writer list.
- Decide how `from any` counts (spec:2203): one proof against what holds in every state, or one per state.
- Bound case splitting by proving each case only from the premises it needs; where a cap is hit, the verdict is unresolved and honest (spec:225, condition 3). Under full proof power a cap is a stated limit of the procedure, and the design should say where it sits and why a real definition will not reach it.
- Certificate size grows with obligations; checking is linear in its size. Whether verification is cached per definition is open in canon (builder:864).

### Problem 8 — Rows are tried in order

Row k fires only if rows 1 to k-1 did not match. Their negated guards are premises for row k. A negated conjunction is a disjunction, which means cases. Canon uses only the single-comparison case and drops the rest (engine:453). The worked example's fallback rows write nothing, so it does not exercise this, but any definition whose later row writes a field under "otherwise" does. Technique: the case splitting of problem 1.

### Problem 9 — The editable-write check

Stated in 2.11. The design problem is to define one evaluation — the rules that mention a patched field, over the configuration the patch would produce, with computed fields recomputed — and to prove that this evaluation cannot fault, and to describe it without contradicting "nothing is evaluated after a mutation". The likely wording: ingress for an edit evaluates on the candidate values before commit, and it is the one place where a rule is evaluated at all, because an edit is an arbitrary incoming value (want:143).

### Problem 10 — What the checker must be trusted for

Stated in 2.14 items 3, 4 and 8. The design problem: make the trusted part small and say exactly what it is. Candidate shape: the checker recomputes the obligation set from the definition by a rule simple enough to audit (every rule against every handler that writes a field it depends on; every fault-prone node), checks each derivation step by arithmetic or by matching, and checks a hash tying the certificate to the definition. Precedent is the one the want names (want:213).

### Problem 11 — Producing the missing premise

For a linear obligation that fails, the rule with the assignments substituted in is itself a condition over the old field values and the arguments, and a guard may mention both; so a guard that closes the proof always exists and can be printed. The design work is in making it the one an author would write: drop the parts already implied by other premises, prefer a constraint on an argument when one argument alone is responsible (want:114 offers both), and for a failed base case suggest an argument constraint. For a non-linear obligation the suggestion is the post-state condition restated (want:121-123). For a proved violation, the concrete case comes from the same linear procedure and must be re-evaluated before it is shown (soundness:133-135).

---

## Part 4 — (d): what must be designed before what

Each step lists what it needs from earlier steps.

1. **The meaning of an operation.** Settle the three open items in the execution order (spec:2016, spec:2203, spec:2207) and close the writer list (spec:1999). Needs nothing earlier. Everything after depends on it, because it defines "handler" and "write site".

2. **The meaning of every operator on every type**, including rounding, the overflow model, and what a comparison on an unset optional means. This is where problems 3 and 4 are decided. Needs nothing earlier. It must come before any proof rule about numbers, because a proof rule is true or false of these meanings.

3. **The table of rules** (2.2): every spelling of a constraint, the statement it stands for, and where it must hold. Includes the decision on time in rules (owner), quantified rules, `maxplaces`, modifiers on optional fields. Needs 2.

4. **The obligation model**: establishment and preservation obligations, fault obligations, the sites, which (rule, handler) pairs need no proof, and the premise set at each site. Needs 1 and 3.

5. **The ingress contract** for arguments, event ensures and editable writes. Designed together with 4: the proof at an editable write site assumes exactly what ingress checks, and ingress checks exactly what the proofs assume.

6. **The facts each type contributes and how each action changes them**: intervals, linear relations, length, count, presence, choice value, qualifier, element properties, temporal facts. Needs 2 and 4.

7. **The inference steps, which are also the certificate vocabulary.** These are one design, not two: each step a proof may take is a line the checker can confirm. Decide here what replaces the single-step limit (problem 1) and what "directly" means for non-linear conditions (problem 2). Needs 6.

8. **The procedures that find proofs** using those steps, with their limits. Needs 7. Kept after 7 on purpose: authority is in the steps, and the search can change without touching soundness (spec:225).

9. **Verdicts**: the concrete violating case, and the missing premise. Needs 8.

10. **Graph structure with dead rows**: guard satisfiability using the rule premises, removal of dead rows, reachability recomputed, and the stage order that makes this possible. Needs 4 and 8. It also feeds back: obligations on rows from unreachable states are dropped (engine:2092), so the design must show this loop terminates in one pass or define the order.

11. **The certificate as an object and the checker**: contents, tie to the definition, route to the runtime, the compiler stage, the load gate and its failure, the list of run-time checks, proof provenance for inspect. Needs 7 for the vocabulary and 4 for the obligation set the checker recomputes.

12. **Catalog and diagnostics**: the catalog entries for obligation kinds, premise kinds, certificate steps and writers; the diagnostic codes and messages. Needs all of the above. "Catalog before code" then applies at build time.

13. **Acceptance criteria.** The fourteen mutations (want:112-127) are the first set and can be written down now, since they come straight from the want. Criteria for each type and each hard problem need steps 3 to 11.

Steps 1, 2 and 3 can proceed in parallel. Steps 4 and 5 are one piece of work. Step 7 is the centre: most of what is Silent in Part 2 is decided there.

---

## Part 5 — Contradictions found, in one list

Canon against canon:

1. The base case uses initial-event arguments (spec:268, spec:2090) — the engine says it does not (engine:2052-2056).
2. An unknown result in the base case rejects (spec:213, engine:1933) — the same algorithm passes it (engine:2064) and skips guarded ensures (engine:2058-2060).
3. `to <InitialState> ensure` is in the base case (spec:2090) — the engine excludes it (engine:1960).
4. Nothing is evaluated at stateless construction (spec:1949) — "global rules are checked" (spec:2151).
5. `maxplaces` is rule shorthand, so proved (spec:1143) — "Runtime enforcement" (business:1577, business:1584).
6. No rule is governed at run time (spec:268) — a quantified rule is "a runtime governance rule" (collection:796).
7. Nothing from the proof ledger crosses to the runtime (engine:106, engine:2366) — the certificate crosses (arch:601), and the proved verdict carries it (engine:2533).
8. Stateless handlers have no guard (engine:2121) — guards on `on <Event>` rows are allowed (spec:2110).
9. Every rule held beforehand is a premise (spec:268) — a guarded rule contributes nothing, and only unconditional field-to-field rules contribute (engine:2455, engine:2614).

Canon against the want:

10. The engine never combines two declared facts (spec:256, engine:2539) — the want's `OpenAccount` and `Deposit` need exactly that (want:100-101).
11. An editable write is held to the constraints on its own slot (spec:274), with the rest undecided (evaluator:1361) — the want says every rule that mentions the field (want:143).
12. The dead-guard scan never reads relational facts (engine:445-447) — the want's dead row contradicts a relational rule (want:127).
13. Reachability ignores guards (spec:191) — a state behind a dead row is unreachable (want:127).
14. A rule using `now()` is shown as normal usage (temporal:577) — the want leaves it open (want:219).
15. Reading an optional needs a presence test everywhere (spec:1561) — the want's `ReduceLimit` guard reads one without (want:75).
16. Overflow proved for every operation is the stated target (spec:110) — the want's `Deposit` is unbounded and called provable (want:101). Not a contradiction until the overflow model is chosen, but one of the two candidate models makes it one.

---

## What this document did not cover

- I read the spec's preamble and semantics sections, the soundness document, the architecture document's sections 1 and 8, and the engine document's architecture, base-case, contracts, decisions and exclusions sections in full. I read the engine's individual strategy sections (engine:668-1905) only in part, and the four type documents by targeted search, not end to end. A statement that canon is Silent on something inside those ranges is weaker than the others.
- I did not read `compiler/type-checker.md`, `compiler/diagnostic-system.md` beyond its severity section, `runtime/fault-system.md`, `runtime/result-types.md`, `language/catalog-system.md` beyond line 103, or the tooling documents.
- I did not search git history. "Canon is silent" means silent in the files as they stand today.
- The four strategies named at engine:2535 are defined somewhere outside canon; I did not go looking, because a definition outside canon does not change canon's status.
- Nothing here proposes syntax. Where a gap would need new language (bounds on temporal point types, for example) it is named as a gap only.
