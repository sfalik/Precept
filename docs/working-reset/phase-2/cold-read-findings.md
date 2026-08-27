# The cold read — what it found, verified

**Written 2026-08-26 against HEAD `621bde2b`.** Nothing in this document edits canon. It lists and it
assigns; it settles nothing.

**Two line-number drifts, from commits made after the read and before this file was committed.** `f32c2253`
deleted two lines from `docs/compiler/proof-engine.md` at `:322`, so every `proof-engine.md` citation below
that point sits two lines lower at HEAD — the text quoted here as `:491`, for instance, is at `:489` now.
And the one uncommitted line this document notes above `soundness-and-coverage.md:158` has since been
committed, so that quotation is at `:159` at HEAD, as the note predicted. Nothing either commit changed
alters a finding; only where to look for the text.

**Twenty-nine findings were on the list. One was withdrawn before verification began — the owner's own
answer of 2026-08-26 settled it — so twenty-eight were checked.** Of those twenty-eight: **twenty-two
reproduce as stated or with a line number corrected by a few lines; five reproduce only in a changed form,
and the change is stated at each one; and one is confirmed resolved and closed.** Nothing was found to be
simply wrong, but five findings say something different from what they were written to say, and that
difference matters in at least two of the five.

## What the read was, and why it was done cold

A reader was sent through the owner's own statement of what he wants
([`docs/Working/what-i-want-2026-07-16.md`](../../Working/what-i-want-2026-07-16.md)) and then through the
runtime and compiler documents, with nothing else — no contradiction register, no search procedure, no
plan, no list of what earlier passes had already found.

The reason was narrow and worth stating plainly, because it is the whole justification for spending the
time. **A search procedure cannot find the holes in its own scope.** Phase 2's procedure had already been
widened once: fifteen search patterns gained synonyms, the widened run found nothing new, and a later pass
then found an entire folder (`docs/tooling/`) that no pattern had ever covered. Widening the patterns could
not have found that, because the patterns were not the thing that was wrong — the file list was. A reader
with no patterns at all has no such blind spot, and pays for it in a different way: no repeatability, and no
guarantee of coverage.

So this read is not a substitute for the register and does not claim completeness. It is a second, differently
shaped pass, and its value is in the places the first shape could not reach.

## What the read did not cover

- **It read the runtime and compiler documents, not the language documents.** `precept-language-spec.md`,
  `primitive-types.md`, `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md` and
  the rest of `docs/language/` were not part of the read. Where verification had to reach into them — it did,
  twice, and both times it changed a finding — that is said at the finding.
- **It verified almost nothing against `src/`.** Every claim below is a claim about what the documents say.
  A finding that canon has no obligation for something is a finding about the documents, not a measurement
  of the compiler. Where canon itself states what the code does, that statement is quoted as canon's claim
  and not as a verified fact about the code. The one exception is Part 3 item 11, where the documents say
  three record types are still declared and verification checked that they are.
- **It did not read the contradiction register, the plan, or anything under `docs/working-reset/`.** Several
  findings turn out to sit next to rows already in the register — that is noted where it happens, and it is
  not a criticism of either pass. Verification, as opposed to the read, did consult the register and the
  plan, because assigning an owner is impossible without them.

---

# Part 1 — Disagreements between canon and the owner's document

**Seven were claimed. One was withdrawn before verification (finding 4). One was overtaken by a commit made
the same day (finding 2) and is confirmed closed. Of the remaining five, four survive as disagreements and
one — finding 3 — turns out not to be a disagreement at all and is written up with the correction.**

Heaviest first.

## 1. The proof engine has no obligation for "this handler preserves this rule"

**This is the most important finding in the read.** It reproduces, and canon states the gap in its own
voice in one place while asserting the opposite in another.

The owner's model is an induction. `what-i-want-2026-07-16.md:19`:

> **Inductive step.** Every handler *preserves* every rule, provable from four premise classes: (a) field
> modifiers, (b) arg constraints, (c) the handler's guard, and (d) all rules holding in the pre-state.

And `:20`:

> **Symmetric obligations.** A rule's obligations attach to every write site of *every* field the rule
> mentions — not just the field the author was thinking about.

**What the proof engine's obligation vocabulary actually contains.** `proof-engine.md:491`:

> The `ProofRequirement` discriminated union has a subtype for every `ProofRequirementKind` (numeric,
> presence, dimension, modifier, qualifier-compatibility, qualifier-chain, interval/length/count
> containment, key-presence, index-bounds, dimensional-product, and assignment-qualifier — the open-field
> assignment case discharged via guard narrowing)

Thirteen kinds. Neither preservation nor establishment is among them. `proof-engine.md:2648` confirms the
count from the other side: "`ProofRequirementKind` enum (thirteen members)".

**Where obligations come from.** `proof-engine.md:379–385`, the first pass:

> • Walk explicit SemanticIndex members (PE-G4):
>   - TransitionRows[].Actions[] (+ nested InputExpression)
>   - EventHandlers[].Actions[]
>   - StateHooks[].Actions[]
>   - Rules[].Condition
>   - Ensures[].Condition
>   - Fields[].ComputedExpression
> • For each TypedBinaryOp/TypedFunctionCall/TypedMemberAccess/TypedAction with non-empty
>   ProofRequirements:
>   - Create ProofObligation with ObligationContext (PE-G6)

`Rules[].Condition` is on that list, but it is walked for expressions *inside* the rule that carry their own
stamped requirements — a division in a rule condition raises a divisor obligation. Every obligation on the
list comes from a requirement the catalog stamped on an operator, function, accessor or action.
`proof-engine.md:487` states the mechanism directly:

> The type checker stamps these requirements onto `TypedExpression` and `TypedAction` nodes when it resolves
> them. The proof engine instantiates `ProofObligation` records from the stamped requirements — it reads
> them, doesn't compute them.

Nothing in that walk turns "rule R exists" plus "handler H writes a field R mentions" into an obligation.

**A declared rule is consumed the other way round — as a fact, not as a goal.** `proof-engine.md:1383`:

> The field-to-field relational fact (`FieldToFieldConstraint`) is sourced from declared **unconditional
> rules** as well as from guards. A `rule X >= Y because "…"` (and its `>`, `<=`, `<` siblings) over two
> field references contributes the same relational fact a same-row `when X >= Y` guard does

And `:1387`: "`rule X >= Y` tightens `X`'s lower bound to `Y`'s already-established lower bound." The rule
narrows an interval so that some *other* obligation — a divisor, an overflow, a containment — discharges.
`:2455` gives the design rationale for exactly that arrangement. A rule earns its way into the engine by
helping prove something else; it never becomes a thing to be proved.

**Canon admits the gap in one place and asserts the opposite in another.** The admission is
`proof-engine.md:2616`, and it is not a half-admission — it is explicit:

> **Two things the engine does not do today, stated so the model above is not read as a description of the
> build.** The pre-state premise is available only where a declared rule contributes an unconditional fact
> (§ *Relational reasoning*, spec §0.6 item 2); there is no general carrying of "every rule held before this
> operation" into a handler's discharge. And the induction quantifies over every write site of every field a
> rule mentions, while the write-site enumeration the compiler works from is asserted by spec §3A.4 rather
> than kept closed by the build […] Both are gaps against the target, not revisions of it.

That covers premise class (d) and the symmetric write sites. It does not say the obligation itself is
missing.

The assertion in the other direction is what makes this serious, because several runtime decisions rest on
it. `proof-engine.md:2172`:

> **It is not a re-check index.** The runtime does not re-evaluate the definition's rules against a changed
> configuration — every handler was proven at compile time to preserve every rule, so there is no pass after
> the mutation for an index of affected constraints to drive

`compiler-and-runtime-design.md:151` says the same in the same words: "There is no post-mutation pass left to
make a pass/fail decision, because every handler was proven at compile time to preserve every rule." The
removal of the post-mutation constraint sweep — executed in phase 2, register rows `S2` and `S9` — rests on
that sentence. If nothing raises a preservation obligation, then "every handler was proven to preserve every
rule" describes an obligation kind that does not exist, and the sentence justifying the removal of the
runtime's last rule check is asserting something the compiler has no vocabulary to state.

**Not a contradiction, said honestly.** `:2616` and `:2172` are about different things — one says a premise
class is unavailable, the other says the proof was performed. But read together they are hard to reconcile,
and the reconciliation is not written anywhere.

**Owner:** **Phase 5.** Its stated scope reaches this directly. The plan's phase 5 entry, § *What survives of
the matrix's six ways of slicing the problem*, first bullet:

> **Obligation family** — not an axis. Establishment and preservation are the base case and the step of one
> induction, differing only in whether the inductive hypothesis is in the premise set: one flag on the
> obligation.

And phase 5's *What it produces* opens with "The obligation families." A closely related but distinct
question is already on § 4 Waiting on Shane — *"Must a proof name what makes each fact it uses true, and what
keeps it true?"* — which asks whether the certificate must **name** establishment and preservation. That
question presupposes they exist as things a proof does; this finding is that no obligation kind for either
exists today. Both belong to phase 5.

## 2. The ingress "nothing wider" narrowing — resolved, and no trace remains

Overtaken by commit `621bde2b` on the day this was written, before verification began. **Verified: no trace
remains in canon.** `grep -rn "nothing wider"` and `grep -rn "is disputed and is not settled canon"` over
`docs/language/`, `docs/runtime/`, `docs/compiler/`, `docs/compiler-and-runtime-design.md` and
`docs/philosophy.md` both return zero. Not written up as open.

## 3. A state reachable only through a provably-dead row is never reported unreachable

**Changed by verification, and the change matters: canon does not disagree with the owner here. It agrees,
and records the capability as designed but not built.**

The owner's text is real. `what-i-want-2026-07-16.md:155`:

> **Dead rows are structural defects too.** A row whose guard can never be true given the declared
> constraints is unreachable — and whatever state it leads to may be unreachable in consequence.

And `:127`, in the must-not-compile table: "Dead row: the guard contradicts the rule, so it can never be true
— and `Closed` becomes unreachable in consequence."

The mechanics the finding describes are all confirmed. `graph-analyzer.md:96` — "Guard expressions are opaque
to the graph analyzer. The proof engine handles guard satisfiability." Reachability is computed at
`:418–435` by breadth-first search over the adjacency map, with no reference to guards. The proof engine runs
afterwards and produces the fact, `proof-engine.md:451`:

> The scan also produces `UnreachableRowFact` instances into `ProofLedger.ProducedFacts` for every transition
> row with a proven-unsatisfiable guard. This is the first `ProofForwardingFact` variant produced by the
> proof engine itself […] The DU is extended rather than forked because consumption is uniform.

**What the finding got wrong.** It concluded that nothing consumes the fact and treated that as a
disagreement with the owner. Canon states the same thing itself, as a numbered specification item with a
recorded status. `precept-language-spec.md:214`, obligation 12: "**Sharpening of reachability and routing
diagnostics** from proven-dead guards." Its status row, `:245`: "Specification-only — depends on #9 + #10."
And `:247` names it as the last one:

> Obligation 12 (sharpened reachability/routing from proven-dead guards) is the one remaining
> specification-only item; an author who depends on it today supplies the equivalent hand-written constraint
> (an explicit `rule` or `ensure`) until it ships.

So canon's target matches the owner's text and canon discloses that the target is not met. That is the
arrangement the project asks for, not a disagreement.

**What survives, and it is a document defect rather than a disagreement.** `graph-analyzer.md` § 4 is the
document that owns the forwarding-fact hierarchy — `:261` calls the subtype set below it "the current
contract consumed by the proof engine" — and its table at `:265–271` lists five subtypes and does not list
`UnreachableRowFact`. `proof-engine.md:451` says that variant is in the same hierarchy. A reader building
against `graph-analyzer.md` § 4 does not learn the variant exists. This is carried in Part 3 below.

**Owner:** the specification item is a numbered claim in a language document. Phase 3 corrects the language
documents and numbers every normative statement; phase 10 turns numbered claims into failing tests; phase 11
builds. That chain reaches it. The `graph-analyzer.md` § 4 omission is a documentation defect with the same
owner as the rest of Part 3 — see there.

## 4. Non-linear rules — skipped

Not verified. The owner reviewed and approved the want-document material behind it (recorded in commit
`37b531d1` and in [`trace-walk-rulings.md`](trace-walk-rulings.md)), so it is not a gap against his text.

## 5. Three different definitions of a dead end

**Reproduces.** Three statements, two of them in the same document.

The owner, `what-i-want-2026-07-16.md:152`:

> **No dead ends.** Every non-terminal state has an exit — `Frozen` exits via `Unfreeze`. A state you can
> enter but never leave is rejected, unless the author deliberately marked it `terminal`

`compiler-and-runtime-design.md:147` matches him — "detects dead-ends (every non-terminal state must have an
outgoing path)".

`graph-analyzer.md:451`, the algorithm, does not:

> 4. DeadEndStates = { s ∈ ReachableStates : s ∉ TerminalStates AND s ∉ reverseVisited }

where `reverseVisited` is built by walking backwards from the terminal states (`:445–450`). `:455` states the
same definition in prose: "A dead-end state is reachable from initial (it's not an unreachable state) but
cannot reach any terminal".

**They differ on a real case.** Two non-terminal states that transition into each other and nowhere else both
have an exit, so `compiler-and-runtime-design.md:147` and the owner accept them; neither can reach a terminal,
so `graph-analyzer.md:451` rejects both.

**The same document holds both readings.** `graph-analyzer.md:457`, immediately below the algorithm, gives the
owner's definition as the rationale for the algorithm's severity: "A state an entity can enter but never leave
is a structural defect: the lifecycle the author declared cannot complete from there." The first clause is
"enter but never leave"; the second is "cannot complete". They are not the same test, and the sentence reads as
though they were.

**Not what the register already covers.** Rows `C7`, `N45`, `N46` and `N49` are about the *severity* of
`DeadEndState` — Warning against Error. None of them touches which states the diagnostic applies to.

**Owner:** **Phase 2**, which is open. This is exactly its stated goal — "Find every place where the canonical
documents on this branch and the want document say different things." Whether the resolution is a wording fix
or a change to what the compiler rejects is a question about what a `.precept` file may be, which closes at
phase 3 when the language closes. Phase 2 has to decide which of those two it is; that is not settled here.

## 6. Who owns schema evolution on restore

**Reproduces, and the finding's own assessment is right: this is one document missing a reconciliation, not a
live disagreement.**

The owner, `what-i-want-2026-07-16.md:141`:

> (Post-MVP, the restore path itself expands to handle schema evolution — data written under an older
> definition.)

The three runtime documents agree with him:

- `runtime-api.md:277` — "Handling data written under an older definition is the post-MVP job of the restore
  path itself, through migration."
- `runtime-api.md:906` (cited as `:908`; the correct line is `:906`) — "**No migration logic.** If the
  definition changed since data was stored, nothing in the current restore path transforms it; handling that
  is the post-MVP job of the restore path itself."
- `evaluator.md:1757` (cited as `:1761`; the correct line is `:1757`) — the same sentence again, and it cites
  the want document by path and line: "Restored data is trusted because it was governed when it was written
  (`what-i-want-2026-07-16.md:141`)."

`compiler-and-runtime-design.md:718` does not:

> **This is a known gap — definition migration is out of scope for v1.** The contract digest hash provides
> change detection; a structural diff API provides change enumeration; but automated migration is deferred.
> Host applications that need to handle definition evolution must manage the migration externally.

"Out of scope for v1" with the work assigned to the host is a different disposition from "the post-MVP job of
the restore path itself". Note that register row `N16` already corrected a *different* sentence in the same
paragraph — the post-mutation sweep clause — and cites want `141` and `199` while doing it. The migration
ownership sentence was not part of that correction.

One thing worth flagging separately: `evaluator.md:1757` cites `what-i-want-2026-07-16.md:141` from inside a
canonical document. `docs/Working/` is transient by the project's own rule and canon citing it is a defect
independent of this finding. Not carried in Part 3 because Part 3 is document-against-document contradictions
and this is a different kind of problem.

**Owner:** **Phase 2**, open. Canon-versus-want, and three of the four documents already carry the corrected
wording.

## 7. `on <Event> ensure` runs at ingress, where the owner routes that job to guards

**Reproduces on the mechanics. Whether it is a disagreement at all is genuinely unclear, and the finding was
right to flag itself as low-to-medium confidence.**

The owner, `what-i-want-2026-07-16.md:143`:

> What ingress validation means: the applicable **rules** are evaluated — however they were spelled, as
> modifiers or as rule statements. For an arg, that's its modifier-rules; anything relational an event needs
> lives in its guards.

Two things at ingress for an event: the arg's own modifier-rules, and the event's guards.

Canon has a third. `evaluator.md:1364–1373`, the ingress function:

> ```
>     // The constraints declared on each supplied argument — the modifier form.
>     foreach (var arg in @event.ArgDescriptors)
>         foreach (var constraint in arg.Constraints)
>             EvaluateConstraint(constraint, slots, args, violations);
>
>     // on <event> ensures — event preconditions (ConstraintKind.EventPrecondition),
>     // declared against the event's arguments and evaluated before the operation runs.
>     if (index.OnEvent.TryGetValue(@event, out var eventConstraints))
>         foreach (var constraint in eventConstraints)
>             EvaluateConstraint(constraint, slots, args, violations);
> ```

`evaluator.md:2130`, the ingress matrix: "`Fire` | no | yes | the constraints declared on each supplied
argument, plus `on <event>`". `runtime-api.md:883` (cited as `:885`; the correct line is `:883`): "Arg ensures
(`on <Event> ensure`) | Evaluated at ingress if initial event declared".

**Why it is a different thing from a guard, and not just another spelling.** A guard that is false selects a
different row or none — `what-i-want-2026-07-16.md:154` calls that inapplicability, "Unmatched — the operation
is not offered". An `on <Event> ensure` that fails produces a `ConstraintViolation` collected in the ingress
violation list, which is the same list an arg-modifier failure lands in. Same evaluation point, different
outcome. So the language has a relational check on an event that is not a guard, and the owner's sentence
routes relational event conditions to guards.

**Why this is unclear.** The owner's document never mentions `ensure` at all — `grep -i ensure` over
`what-i-want-2026-07-16.md` returns nothing. His sentence is a description of what ingress validation means,
not an enumeration of every construct the language offers, and reading it as "therefore `ensure` should not
exist" is an inference the document does not license. It is equally readable as: an `on <Event> ensure` *is*
an arg constraint in relational form, and he was describing the simple case. Which of those it is has not
been decided by anyone.

**Owner:** **Phase 3**, which closes what you can write in a `.precept` file. `ensure` is a construct, and
whether it survives, and in what form, is a language question. See also the first entry in Part 2 below,
which is the same subject from the silence side.

---

# Part 2 — Places canon is silent on something the owner requires

Six were claimed and all six were checked. **Three survive as stated; one survives with a correction to how
widely canon states the requirement; two changed substantially under verification and are written up with
the change stated.**

## The `ensure` construct is absent from the want document

**Confirmed.** `grep -i ensure docs/Working/what-i-want-2026-07-16.md` returns nothing. The document
describes a language with fields, rules, modifiers, states, events, guards, transition rows and reject rows,
and never mentions the construct that the specification lists as one of five constraint anchors
(`runtime-api.md:421`: `ConstraintKind { Invariant, StateResident, StateEntry, StateExit, EventPrecondition }`).

This is a silence in the *owner's* document rather than in canon, which is the opposite direction from the
rest of this section, and it is the reason finding 7 above cannot be resolved from the text.

**Owner:** **Phase 3.**

## The certificate checker's compiler-stage call site

**Confirmed as a silence, with one correction: it is stated in two places in `soundness-and-coverage.md`, not
one.**

The owner, `what-i-want-2026-07-16.md:201–203`:

> One small checker component, two call sites:
> - **A compiler stage** — audits the proof engine's work at authoring time, catching proof-engine bugs when
>   they are cheapest to fix.

Canon carries the requirement. `soundness-and-coverage.md:158` (HEAD; `:159` in the working tree, which has
one uncommitted line added above it): "**A compiler stage**, auditing the proof engine's work at authoring
time, where an engine defect is cheapest to fix." The same claim appears earlier in the same document at
`:55–58`, and `proof-engine.md:2536` states it too: "at both of its call sites — a compiler stage that audits
the engine at authoring time, and a load-time gate in the runtime". So the finding's "only place" claim is
wrong; the requirement is written three times.

**What is actually silent is where the stage goes.** `docs/compiler/README.md:17`: "The compiler is a linear
six-stage pipeline." The table at `:23–30` lists six stages — lexer, parser, name binder, type checker, graph
analyzer, proof engine — and no checker. No stage document exists for it, and the sixteen-section stage
template has not been filled for it. `proof-engine.md:2159` separately calls the builder "pipeline stage 7,
after ProofEngine", which is a seventh stage that is neither the checker nor in the README's six.

**Owner: none, and after verification it is the only finding in this read with no owner anywhere.** It is
set out in Part 4, with the two things that could give it one.

## What artefact carries the certificate out of the compiler

**Confirmed.** The owner, `what-i-want-2026-07-16.md:199`: "Alongside the compiled definition, the compiler
emits a **certificate**".

Nothing carries one:

- `compiler-and-runtime-design.md:687` — `Compilation` is `TokenStream Tokens`, `ConstructManifest
  ConstructManifest`, `SemanticIndex Semantics`, `StateGraph Graph`, `ProofLedger Proof`,
  `ImmutableArray<Diagnostic> Diagnostics`, `bool HasErrors`. No certificate.
- `proof-engine.md` § Output declares `ProofLedger(ImmutableArray<ProofObligation> Obligations,
  ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
  ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults, ImmutableArray<Diagnostic>
  Diagnostics)`. No certificate field — and `:2368` and `:2587` both say the ledger does not cross into the
  runtime at all.
- `precept-builder.md:865` names it as open, in those words:

  > **How the certificate reaches `Precept.From` at all.** The `Compilation` record does not carry one, and
  > the proof record is described as emitted alongside the definition rather than inside the compiler's
  > analysis output. Whether it becomes a sixth member of `Compilation`, a separate argument, or a file the
  > method is pointed at is decided with the rest of the gate's shape.

**Owner: the runtime documents' open-questions table**, where the question is parked and where it stays
readable after any phase closes. `runtime/README.md` § Open questions carries it as: "How the certificate
reaches `Precept.From` at all — a sixth member of `Compilation`, a separate argument, or a file the method
is pointed at", pointing at `precept-builder.md` § 9 The Certificate Gate. Set out in Part 4.

## The premise list as what the runtime is supposed to validate

**Changed by verification. Canon is not silent — it states the requirement in the owner's own terms and then
describes a mechanism built independently of it, and says so.**

The owner, `what-i-want-2026-07-16.md:210`: "the premise list **is** the runtime's validation surface — which
guards and constraints must run and can never be skipped — derived from the proof, never maintained in
parallel."

`runtime-api.md:258` says the same thing:

> The certificate also tells the runtime which checks it may not skip. The premise list *is* the set of
> runtime validations the proofs depend on — which argument constraints and which guards must run — derived
> from the proof rather than maintained as a parallel list alongside it.

What actually routes an incoming value is the `ConstraintPlanIndex` (`precept-builder.md:505–516`), five
buckets of `ConstraintDescriptor` sorted by activation anchor, assembled in pass 5 of the builder from the
declared constraints. It has no connection to any proof. `precept-builder.md:812` and `:866`, and
`catalog-system.md:1444`, all record the same open question in the same words: the index "could shrink to what
ingress needs, or be replaced by a premise list derived from the definition's certificate. Nothing canonical
answers that."

So this is not silence. It is a stated requirement, an existing parallel mechanism, and a written
acknowledgement that nothing has reconciled them — which is the honest arrangement, minus an owner.

**Owner: the runtime documents' open-questions table.** `runtime/README.md` § Open questions carries it as:
"Whether the `ConstraintPlanIndex` keeps its five state-and-event anchors, now that both surviving uses are
anchored to a slot or to a catalog listing. It could shrink to what ingress needs, or be replaced by a
premise list derived from the definition's certificate", pointing at `precept-builder.md` § 12. No phase of
this plan reaches it — phase 9 fills the catalogs, but its stated output is "Catalog entries for the
discharge rules, step kinds, obligation families and write sites" and the runtime's constraint index is not
on that list — and it does not need one. Set out in Part 4.

## What inspection reports instead of a constraint verdict

**Confirmed, with two citations corrected.** The owner, `what-i-want-2026-07-16.md:211`: "Inspect can cite
provenance, not just verdicts: 'no check needed here — proven from these premises'".

`evaluator.md:912` (cited as `:914`):

> **What inspection reports in place of a constraint verdict.** Execution has no constraint-evaluation phase,
> so inspection has none to preview (spec §3A.6). What the want document asks inspection to report instead is
> proof provenance […] but neither that document nor the specification says which type carries it.
> `TransitionInspection.Constraints`, `UpdateInspection.Constraints` and `EventInspection.EventEnsures` are
> left exactly as they are, with `BuildConstraintResults` unspecified, until that is decided.

`result-types.md:250` (cited as `:252`) says the same and adds: "The three arrays are left exactly as they
are, and their descriptions above still describe the old model."

Both documents name the question, name the three types it would change, and say nobody has decided.

**Owner: the runtime documents' open-questions table.** `runtime/README.md` § Open questions carries it as:
"Which type carries what inspection reports where execution has no constraint verdict to preview — proof
provenance rather than a verdict. `BuildConstraintResults` stays unspecified until it is decided", pointing
at `evaluator.md` § 7.2. Set out in Part 4.

## Time-referencing rules

**Changed substantially by verification, and this is the second place where reaching into the language
documents changed a finding.**

The owner left it open at `what-i-want-2026-07-16.md:219`:

> **Time-referencing rules.** A rule like `ExpiryDate > today` can become false with no event in flight —
> nothing writes `today`, there is no ingress point for it, and the inductive model has no step where pure
> passage of time fits. Do rules that reference the current moment exist at all […] Deliberately left open.

**The language documents have already answered the first half, and the answer is yes.**
`temporal-type-system.md:560–562` declares `now()` — "the sole way to construct an `instant` from the runtime
environment". `:577–578` gives a rule as a worked usage pattern:

> ```precept
> # Time-bounded obligation rule
> rule now() - IncidentAt <= '72 hours'
>   because "HIPAA: incident must be filed within 72 hours"
> ```

And `:568` states the consequence in the document's own voice:

> **Determinism:** `now()` is the canonical example of environment-relative determinism (Locked Decision #9).
> A precept rule containing `now()` will produce different results when fired at different times. This is
> accepted behavior — Precept guarantees internal consistency (same inputs, same outputs within a single fire)
> but cannot freeze the clock.

That is precisely the rule shape the owner says the inductive model has no step for, documented as supported
and given a locked decision number.

**The silence is in the compiler and runtime documents, and it is total.** `grep -n "now()"` over
`docs/compiler/*.md`, `docs/runtime/*.md` and `docs/compiler-and-runtime-design.md` returns **zero hits**. The
only mention outside `temporal-type-system.md` is `precept-language-spec.md:1605`, a row in the function
signature table: "`now()` | `() → instant` | `instant` | —". No document says what obligation a rule
containing `now()` raises, what premise could discharge it, whether such a rule participates in the
inductive step at all, or what happens to it at the ingress points.

So the finding is right that no compiler or runtime document treats it, and wrong that no canonical document
treats it — a language document treats it, supports it, and locks a decision about it, apparently without the
proof model having been asked.

**Owner: unclear, and I am saying unclear rather than picking one.** Phase 3 closes the language and would
have to decide whether such rules exist. Phase 5 settles the obligation families and would have to decide
what obligation one raises. Neither phase entry mentions time, `now()`, or environment-relative values. Phase
5's § *Where the fragment stops* lists what will appear in real constraints — "`contains`, quantifiers, member
access, lookup by key, string functions, temporal arithmetic and qualifier algebra" — and temporal arithmetic
is there, but a value that changes with no operation in flight is a different problem from arithmetic over
temporal values. Carried in Part 4 as an item whose owner needs deciding rather than one that plainly has
none.

---

# Part 3 — Documents contradicting each other

Sixteen were claimed. **All sixteen reproduce.** One of them turned out to be two separate defects, so they
are numbered 1 to 17 below; an eighteenth was found while verifying Part 1 finding 3 and is included.

Eight needed a line number moved by a few lines. One is correct in substance but has the two documents the
wrong way round, and is written up corrected. These are ordinary defects rather than design questions, so
they are listed rather than analysed. Owner at the end of the section.

**1. `EventOutcome` is described three different ways, and one variant is used but never declared.**
`result-types.md:47` and `:56` both say eight variants. The code block at `:95–104` declares seven —
`Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `Unmatched`, `UndefinedEvent`, `Faulted` — and the
variant table at `:109–115` has the same seven rows. `runtime-api.md:106` says nine. `evaluator.md:327–338`
declares the same seven as `result-types.md`. `runtime-api.md` has no declaration block at all, and uses a
`Created` variant at `:145` (`EventOutcome.Created c => c.Result,`), `:160` ("It always returns
`EventOutcome.Created` on success") and `:168` (a table row). The string `Created` appears in no other runtime
document. Meanwhile `evaluator.md:606` returns `new EventOutcome.Applied(...)` for the same construction
success, and `compiler-and-runtime-design.md:382` comments `// Applied or Transitioned`.

**2. `UpdateOutcome`'s variant names do not overlap between two accounts.**
`compiler-and-runtime-design.md:881` gives `FieldWriteCommitted`, `AccessDenied`, `InvalidInput`, `Fault`
(with `InvalidInput` and `AccessDenied` repeated at `:860–861` and `:904`). `result-types.md:136–142` and
`evaluator.md:355–360` both declare `Updated`, `ConstraintsFailed`, `FieldNotEditable`, `InvalidFields`, and
`runtime-api.md:393–396` uses those. Not one name is shared.

**3. `ConstraintsFailed` is declared in one document and declared impossible in another.**
`result-types.md:139` declares `public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation>
Violations) : UpdateOutcome;`, describes it as produced at `:148`, and `evaluator.md:743` returns it.
`compiler-and-runtime-design.md:863`: "There is no `*ConstraintsFailed` outcome in any of these families, and
there cannot be one: a definition only compiles if every handler was proven to preserve every rule…"

**4. Restore either recomputes computed fields or leaves them stale, depending on which paragraph you read —
including two paragraphs of the same document.** Recompute: `compiler-and-runtime-design.md:842` ("it does
recompute computed fields"), `:912`, `runtime-api.md:277`, and `evaluator.md:2138`'s own matrix row
("computed fields recomputed"). Stale: `evaluator.md:1973–1977` (cited as `:1977–1981`) — "those values may
be stale … `FromJson` is a hydration primitive, not a recomputation pipeline. Freshness enforcement is a
caller concern." `evaluator.md:1750` ties `FromJson` and `Restore` together, so this is not two different
operations.

**5. `evaluator.md`'s inspection summary table still shows the constraint pass the rest of the document says
was removed.** The table at `:1933–1936` (cited as `:1937–1940`) has a `Constraints` column reading
`Evaluate` for both Commit and Inspect. The same document says there is no such pass at `:694`, `:717`,
`:912`, `:1680`, `:2034` and `:2126`.

**6. `result-types.md` describes a post-mutation evaluation that the same document says does not exist.**
`:227` — "`Constraints` — rules evaluated against the hypothetical post-patch state." `:248` — "`Constraints`
— constraint evaluation results for this specific row." `:250` — "were shaped around a pipeline that
evaluated rules against the finished working copy. Execution no longer does that … The three arrays are left
exactly as they are, and their descriptions above still describe the old model." (Cited as `:229` versus
`:252`; the substance is at `:227`/`:248` versus `:250`, and `:250` names the mismatch itself rather than
merely contradicting it.) **A second defect at the line originally cited:** `:229` documents
`HypotheticalResult` as a member of `UpdateInspection`, and the record printed at `:220–223` declares only
`Fields`, `Constraints` and `Events`.

**7. `result-types.md` says faults throw in one place and never throw in two others.** `:369` (cited as
`:371`), the Temporal Workflow row of the comparator table, under a column headed "How it informs R2":
"Faults throw (not return). Business outcomes are typed results." Against `:51` — "never thrown" — and `:72`
— "rather than a thrown exception".

**8. `precept-builder.md` numbers its own passes two ways.** The list at `:110–117` puts the constraint plan
pass fourth and the execution plan pass fifth. The diagram at `:267–279` transposes them, and so does the
rest of the document: the dependency table at `:300–301`, the sentence at `:306` ("pass numbers now reflect
execution order"), and the section headings at `:420` and `:505`. The list at `:110–117` is the only place
with the other order.

**9. `FieldDescriptor` has two different shapes, and the property both documents say the evaluator needs is
in neither.** `descriptor-types.md:78–85` declares seven members; `precept-builder.md:322–331` declares
eight. `DefaultExpression` is only in the first; `IsOptional` and `ClrType` only in the second; `Modifiers`
is `IReadOnlyList<ModifierKind>` in one and `ImmutableArray<ModifierKind>` in the other; source position is
`int SourceLine` against `SourceSpan Source`. Neither carries `AccessModes`, which `precept-builder.md:336`
says is needed ("`FieldDescriptor` needs an `AccessModes` property for per-state access-mode lookup") and
which `evaluator.md:1324` and `:1333` both read (`field.AccessModes.TryGetValue(currentState, out var
mode)`).

**Worth flagging separately, because it is happening in the working tree right now.** At HEAD,
`descriptor-types.md:177` is the one place that records the absence: "The property is not on the
`FieldDescriptor` shape printed in § Descriptor Shapes, and both the builder and the evaluator need it
there." The uncommitted working-tree edit deletes exactly that line and nothing else, while
`precept-builder.md:336` and `evaluator.md:1324`/`:1347`/`:2030` all still say the property is needed. If
that edit is committed as it stands, the only written record that the shape is missing a property goes away
and the requirement stays. Not a finding of the read — an observation made while verifying it, and it is
somebody's call, not mine.

**10. `Compilation` has two memberships.** `compiler-and-runtime-design.md:687` lists seven things including
`ConstructManifest ConstructManifest`. `precept-builder.md:181–190` declares five members and no
`ConstructManifest`, and renames three of them — `SemanticIndex` / `StateGraph` / `ProofLedger` against
`Semantics` / `Graph` / `Proof`. The design document also feeds `ConstructManifest` into the snapshot in its
own diagram at `:671` and `:680`. And `precept-builder.md:193` says "The builder reads all **three** analysis
artifacts" about a record it has just printed with four.

**11. Three record types the documents say are in the code and should not be.** `FaultSiteDescriptor` —
`descriptor-types.md` § Source Files (`:206` at HEAD, `:205` in the working tree): "It is still declared in
code and its removal has not been made"; also `precept-builder.md:959` and `evaluator.md:2022`/`:2158`, both
saying "on its way out". `FaultSiteLink` and `FaultSiteAnnotation` — `precept-builder.md:967`: "no builder
pass reads any of the three, and their removal has not been made." (Verification for this one item did reach
`src/`, and the documents' factual claim holds: all three are declared, and `ProofLedger` still carries the
`FaultSiteLinks` field.)

**12. `diagnostic-system.md` lists 147 codes, claims 164, and calls the list closed.** The enum block at
`:215–417` contains 147 members with no duplicate values and a highest value of 164. Seventeen numbers in the
range have no member: 112–118, 120–126, 130–132. The document states 164 at `:419` ("**164 active diagnostic
codes**"), `:448` ("`// 164 arms — one per DiagnosticCode member.`") and `:457`. It calls the set closed at
`:421` ("The enum **is** the complete set of diagnostic rules. It is a closed set — you cannot produce a
diagnostic that is not a member"), and again at `:57`, `:67` and `:96`.

**13. Four codes are discussed by name and are not in the list.** PRE0113 (`:203`), PRE0114 (`:201`, `:203`,
`:205`), PRE0115 (`:419`) — and PRE0116, which the claim did not include: `:752`, "**`UnprovedPresenceRequirement`
(PRE0116) recovery hint at a display position (pending)**". Those four are the complete set of `PRE0nnn`
tokens named in prose with no enum member.

**14. `DiagnosticCategory` is both owned and rejected by the same document.** Owned: `:71`, under OWNS —
"The `DiagnosticCategory` enum"; `:437`, a field of `DiagnosticMeta`; `:473` and the seven-member enum
declaration at `:476–485`; `:800` in Source Files; and five switch arms at `:452–456` passing values.
Rejected: `:772`, under Deliberate Exclusions — "**Category field orthogonal to stage:** Rejected:
`DiagnosticStage` is the only classification axis needed at DSL scale."

**15. The Roslyn rule range is given twice, differently.** `:81` — "Roslyn rules
PRECEPT0001–PRECEPT0031". `:646` — "24 Roslyn rules (PRECEPT0001–PRECEPT0024)."

**16. The diagnostic count differs between documents.** `runtime-api.md:672` (cited as `:674`) — "132
diagnostic codes catch invalid configurations statically." `diagnostic-system.md:419` — "164 active
diagnostic codes".

**17. `StructuralSinkState` — reproduces, with the two documents the other way round from the claim.** The
claim said `graph-analyzer.md` assigns it and `diagnostic-system.md` omits it. It is the reverse.
`diagnostic-system.md` declares it at `:321` under the `// ── Graph ────` header (`StructuralSinkState = 119`)
and names it twice more, at `:187` and `:193`. `graph-analyzer.md` never mentions it — and the code number
119 appears nowhere in `docs/compiler/` or `docs/runtime/` at all. So the diagnostic document assigns a code
to the graph analyzer that the graph analyzer's own document does not know exists.

**18. `graph-analyzer.md` § 4's forwarding-fact table omits a subtype that `proof-engine.md` says is in the
same hierarchy.** Found while verifying finding 3 in Part 1 rather than on the original list. `:261` calls
the list below it "the current contract consumed by the proof engine"; the table at `:265–271` gives five
subtypes. `proof-engine.md:451` says `UnreachableRowFact` is a sixth, produced by the proof engine into the
same DU.

**Two things adjacent to this section that already have somewhere to go, recorded so nobody re-files them.**
Register row `N57` reconciled four different counts of the proof strategy set and is executed. Register row
`N100` — the `ProofStrategy` enum numbering every strategy from 6 upward differently from the section
headings in the same document — is recorded as **not executed and waiting on the owner**.

**Owner for Part 3: phase 2, by the practice it has already established.** The plan's phase 2 entry states
its goal as canon against the want document, which does not on its face reach a defect between two canonical
documents. In practice the register created a category for exactly this — `contradiction-register.md:94`
defines the marker: "**`INTRA-CANON`** — canon contradicting canon rather than contradicting the want
document" — and carries eleven rows so marked (`:644`). So the phase has taken this kind of work eleven times
and has a convention for recording it. That is an owner in practice. Whether it should be written into the
phase entry's stated goal, so that the next reader of the plan does not have to infer it from the register,
is a call for whoever owns the plan.

---

# Part 4 — What is left open, and who has it

**This section was rewritten. The first version reported six findings as having no owner, and for three of
the six that was wrong.** Its reasoning was that no phase of this plan builds the runtime, so no phase's
stated scope reaches these questions. That much is true and still stands. But it is an answer to the
question *which phase owns this*, and that is not the only question worth asking. It never asked whether
the finding is already written down somewhere that stays readable after a phase closes. Three of the six
are, in the same place and by the same route as a question the owner has already ruled on.

The plan's rule — § *No phase closes with an open deferral that has no owner* — says a phase closes only
when every item it leaves open carries a later phase whose stated scope actually reaches it, a ruling by
the owner that the work is not being done, or a named exception. The rule states plainly what it is
protecting against: "an open item with no owner is invisible the moment the phase closes, because nothing
downstream reads a closed phase's open rows." That is the concern to test each item against.

## Three of the six are parked in the runtime documents' open-questions table

`docs/runtime/README.md` § *Open questions* is a table of everything the runtime documents leave undecided,
each row naming what somebody has to decide and pointing at the section where the case for it is argued. It
is a canonical document in the folder whoever builds the runtime will be reading, not a row in a phase, so
it does not go quiet when phase 2 closes. An item in it is the opposite of invisible.

**The precedent is the `Fault`-producer question, and it is exact.** Whether anything ever constructs a
`Fault` sits in that same table — "Whether a defect in Precept itself — a bug in the compiler, the builder
or the evaluator — reaches the caller as a structured outcome or as a thrown exception. **Parked by the
owner until the runtime design is done; his stated lean is exception, and a lean is not a ruling.**" It
reached that state exactly as the three below did: no phase's scope reaches it, so it was put to the owner
as a question about ownership, and on 2026-08-11 he ruled it deferred until the runtime is being built. The
plan carries it as a named exception; canon carries it in that table. The three below are in the table
already, and the read did not notice.

### 1. What artefact carries the certificate out of the compiler

The table's row — its two cells, the question and where it is argued:

> How the certificate reaches `Precept.From` at all — a sixth member of `Compilation`, a separate argument,
> or a file the method is pointed at. — [precept-builder.md](../../runtime/precept-builder.md) § 9 The
> Certificate Gate

That is the same question this read found, stated in the same three candidate answers as
`precept-builder.md:865`. The first version of this section argued that `Compilation` being a compiler
artefact made the item *worse* — falling between compiler phases that do not mention it and a runtime no
phase builds. That argument does not survive the table: the row names `Compilation` as one of its three
answers, and the document it points at is about the compile-to-runtime transformation, which is where a
question spanning both belongs. **Not unowned.**

### 2. Whether the premise list replaces the constraint plan index

The table's row — its two cells, the question and where it is argued:

> Whether the `ConstraintPlanIndex` keeps its five state-and-event anchors, now that both surviving uses are
> anchored to a slot or to a catalog listing. It could shrink to what ingress needs, or be replaced by a
> premise list derived from the definition's certificate. —
> [precept-builder.md](../../runtime/precept-builder.md) § 12 Constraint Indexes, and What They Index Now

"Or be replaced by a premise list derived from the definition's certificate" is this finding. **Not
unowned.**

### 3. Which type carries what inspection reports in place of a constraint verdict

The table's row — its two cells, the question and where it is argued:

> Which type carries what inspection reports where execution has no constraint verdict to preview — proof
> provenance rather than a verdict. `BuildConstraintResults` stays unspecified until it is decided. —
> [evaluator.md](../../runtime/evaluator.md) § 7.2 Inspection Operations

The same question, naming the same unspecified method. **Not unowned.**

**What this does and does not mean.** These three are recorded, findable, and in front of whoever does the
runtime design. That is what the plan's rule is for. It does not mean any of them is answered, and it does
not mean a phase will answer them — none will, because no phase builds the runtime. It means phase 2 is not
leaving them somewhere they will disappear.

## The three record types are a work item, and the plan already names phase 11

`FaultSiteDescriptor`, `FaultSiteLink`, `FaultSiteAnnotation`, and `ProofLedger.FaultSiteLinks`. Four
documents say the reasoning is settled and the removal has not been made.
[`runtime-open-questions.md`](runtime-open-questions.md) classifies them deliberately: "Decided and not done.
All three documents state the reasoning … and then say the removal has not been made. **That is a work item,
not a question.**" It was therefore left out of the open-question indexes, and that was right — there is
nothing here to decide.

**The plan already carries it.** `compiler-readiness-plan.md` § 5 has a row headed "Take `FaultSiteDescriptor`,
`FaultSiteLink`, `FaultSiteAnnotation` and `ProofLedger.FaultSiteLinks` out of the code", which names every
declaration site, records that the language server reads the field and that its hover behaviour survives the
removal, names the two test files that come with it, and names phase 11 in its blocked-on cell as where the
removal itself happens. So the item is written down in a list whose rows leave only when the work is done or
when a phase's stated scope takes it up, and it is not invisible either.

**One thing about that row does not hold, and it is worth saying rather than repeating the row.** Phase 11's
stated goal is to make the frozen test suite green by building the compiler, and what it produces is code
"driven by a red test written in phase 10". No red test demands the removal of a dead record type — which
was the first version of this section's argument and is still correct. Worse, phase 11's second finish-line
condition requires that the diff of the test tree against the freeze commit show "no test deleted, renamed,
skipped or weakened", and the § 5 row says the removal takes `DescriptorsTests.cs` and
`FaultMapDerivationTests.cs` tests with it. Those two sentences are in tension: the removal deletes tests
that the phase's own finish line forbids deleting.

That is a defect in the plan, not a new open question, and it has one obvious repair — sequence the removal
before phase 10 freezes the suite, so the tests are gone before the freeze rather than deleted after it.
**I am not making that call here.** What this read reports is: the work item is recorded and owned on the
plan's own record, and the phase named as its owner has a finish line that its execution would breach. Both
belong in front of whoever owns the plan.

## Where the certificate checker's compiler stage goes — genuinely without a home

The requirement is written three times (`soundness-and-coverage.md:158` and `:55–58`, `proof-engine.md:2536`).
`docs/compiler/README.md:17` says "The compiler is a linear six-stage pipeline" and lists six stages, none of
them a checker. No stage document exists.

**Why no phase reaches it.** Phase 5 produces "**The specification of the certificate checker**: what it
walks, what it asks the catalog for each thing it finds, and what it does when the certificate does not cover
something" — three questions about the checker's behaviour, none about where it is invoked. Phase 11 produces
"the certificate checker and **the runtime's load-time call to it**", naming one call site and not the other.
Phase 12 condition 3 tests "The certificate checker runs **at load time**". A generous reading of phase 11's
"the certificate checker" might be said to include both halves — but phase 11 builds only what phase 10 has a
red test for, phase 10's input is the numbered claims from phases 3, 4, 5 and 7, and phase 5's checker
specification as scoped does not produce a claim about a pipeline stage. So on the plan's own machinery, no
test is written and nothing gets built.

**And unlike the three above, nothing outside the plan holds it either.** The three parked questions are safe
because the runtime documents keep an open-questions table at `runtime/README.md`. **The compiler documents
have no such table.** Individual stage documents each carry an `Open Questions / Implementation Notes`
section — that is section 13 of the sixteen-section template every stage document follows — but
`docs/compiler/README.md` has no folder-level index gathering them, and this question belongs to no stage
document because the stage it is about does not exist. There is nowhere in `docs/compiler/` it currently
lives.

**Two things would give it a home, and I am recording them rather than choosing.**

- **A compiler-side equivalent of the runtime documents' open-questions table.** `docs/compiler/README.md`
  would gain an index of what the compiler documents leave undecided, on the same terms as the runtime one —
  each row naming the decision and pointing at where the case is argued. This question would be its first row.
  Note the cost honestly: the runtime table has an argued discussion in a document to point at for every row,
  and this question has none, so the table would either start with a row pointing nowhere or the discussion
  would have to be written first.
- **Phase 5**, whose stated output already includes the specification of the certificate checker. Its three
  named questions are about the checker's behaviour rather than its call site, so this would be widening
  phase 5's stated scope by one question, not reading it as already covered.

**This item is genuinely without an owner, and it is the only finding in this read of which that is true.**
Neither option above is taken here. Creating the table is a change to canon and widening a phase is a change
to the plan, and both are calls for whoever owns those documents.

## Time-referencing rules — the owner is unclear rather than plainly absent

Set out in Part 2. The owner deliberately left the question open at `what-i-want-2026-07-16.md:219`;
`temporal-type-system.md:560–578` already supports the construct, gives a rule as a worked example, and
carries a locked decision number; and the compiler and runtime documents do not mention `now()` once.

Phase 3 closes what a `.precept` file may contain, so "do such rules exist" is reachable from its stated
scope. Phase 5 settles the obligation families, so "what obligation does one raise" is reachable from its
stated scope. Neither phase entry mentions time, `now()`, or a value that changes with no operation in
flight, and phase 5's list of what will appear in real constraints names temporal *arithmetic*, which is a
different problem. **I am recording this as unclear rather than picking a phase**, because picking one would
be exactly the labelling the plan's rule was written to stop. What it needs is somebody deciding whether it
is one question for phase 3 or two questions split across phases 3 and 5.

---

# Summary of owners

| Finding | Owner |
|---|---|
| 1. No preservation obligation | Phase 5 — § *What survives of the matrix*, obligation-family bullet; § *What it produces*, "The obligation families" |
| 2. Ingress narrowing | Closed — commit `621bde2b`; no trace remains |
| 3. Dead-row reachability | Numbered spec item 12 → phase 3 numbering, phase 10 test, phase 11 build. The `graph-analyzer.md` § 4 omission goes with Part 3 |
| 5. Three definitions of a dead end | Phase 2 (open); the underlying language question closes at phase 3 |
| 6. Schema evolution on restore | Phase 2 (open) |
| 7. `on <Event> ensure` at ingress | Phase 3 |
| `ensure` absent from the want document | Phase 3 |
| Certificate checker's compiler stage | **None** — and no table in `docs/compiler/` holds it either. Two candidate homes recorded in Part 4; neither chosen |
| What carries the certificate out | Parked in `runtime/README.md` § Open questions; argued in `precept-builder.md` § 9. No phase, and it does not need one |
| Premise list against the constraint index | Parked in `runtime/README.md` § Open questions; argued in `precept-builder.md` § 12 |
| What inspection reports instead | Parked in `runtime/README.md` § Open questions; argued in `evaluator.md` § 7.2 |
| Three record types to be removed | Phase 11, per the plan's § 5 row — with a defect in that row noted in Part 4: the removal deletes tests that phase 11's finish line forbids deleting |
| Time-referencing rules | **Unclear** — phase 3, phase 5, or split |
| Part 3, all eighteen items | Phase 2, by the `INTRA-CANON` practice the register already established |

---

# Cross-references

- Owner's document: [`docs/Working/what-i-want-2026-07-16.md`](../../Working/what-i-want-2026-07-16.md)
- The plan and its ownership rule: [`compiler-readiness-plan.md`](../compiler-readiness-plan.md) § *No phase
  closes with an open deferral that has no owner*
- The register this read deliberately did not consult: [`contradiction-register.md`](contradiction-register.md)
- The runtime open-question index, and its statement that indexing is not answering:
  [`runtime-open-questions.md`](runtime-open-questions.md)
- The trace, which is a different exercise — justifying canon's changes rather than finding disagreements:
  [`trace/README.md`](trace/README.md)
