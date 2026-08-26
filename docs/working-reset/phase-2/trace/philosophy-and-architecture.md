# Trace — `docs/philosophy.md` and `docs/compiler-and-runtime-design.md`

**Range**: `b93d1db1..HEAD`
**Files**: `docs/philosophy.md` (35 hunks, 117 changed lines), `docs/compiler-and-runtime-design.md` (53 hunks, 141 changed lines)
**Hunks examined**: 88, counted at **zero context** — `git diff -U0 b93d1db1..HEAD -- <file>`. Three lines
of context would collapse the same range to 21 regions, and for `docs/philosophy.md` to a single one,
because that file was rewritten end to end and almost every changed line touches its neighbour. Zero
context is the only setting that separates the edits here into pieces small enough to trace one at a time.

Grouped below into 24 changes for philosophy and 22 for the architecture document. The architecture
document has 23 entry headings but 22 changes: `:175` is described twice, once inside the banned-vocabulary
entry at `:6` and once as an entry of its own, and it is one change written up in two places.

## Summary

Both documents were changed for the same reason: they described a runtime that re-evaluates every rule against
the finished configuration and discards the result if a rule fails, and the want document removes that
mechanism and replaces it with compile-time proof plus checks on values as they enter. The architecture
document additionally carried, in seventeen places, a model in which the evaluator plants fault traps behind
the compiler's proofs, which the want document also removes. `docs/philosophy.md` was then rewritten
end-to-end, well beyond the eight corrections that were prepared for approval.

**Counting by category, and why the numbers add up to more than 46.** Eighteen of the 46 changes carry more
than one category — a deletion warranted by the want document alongside a replacement wording that repairs a
contradiction, most often — and each of those entries says so where it sits. So a change is counted once in
every category its warrant names, and the category counts total 68 assignments over 46 changes rather than 46:

- **32 rest on the owner's own text in the want document (category 1)** — 15 in philosophy, 17 in the
  architecture document.
- **9 rest on a ruling of his quoted verbatim somewhere (category 2)** — 5 and 4.
- **12 are internal repairs**, canon contradicting itself, a broken link, a count nobody could reconcile
  (category 3) — 7 and 5.
- **12 carry a part that traces to nothing he wrote or ruled (category 4)** — 11 and 1.
- **3 are mechanical consequences** of another change in this same list and inherit its warrant, so they are
  counted in no category of their own — 1 and 2.

Twelve category-4 parts, **four items on the short list**. Nine of the twelve are individual editorial
decisions taken inside the single rewrite of `docs/philosophy.md` — dropping the opener, deleting the
four-operation list, deleting the etymology section, and so on — and they are one item, because they are one
rewrite and he would read them as one question. The other three are one item each. The four are at the end.
Two of them matter: the philosophy rewrite's editorial decisions, and a scope sentence in the architecture
document that the editor recorded as resting on a reading he no longer believes.

---

## `docs/philosophy.md`

A standing note that applies to every entry below: the project rule is that this file is not edited without
the owner's explicit approval. Every entry's *substance* is traced here in the ordinary way. Whether the
approval itself is on the record is a separate question, and it is the first item on the short list.

### `:3–6` — a status block added, declaring the document locked

**What changed**: four new lines at the top saying the document is locked as of 2026-07-28 and approved by the
owner, that it is not edited without his explicit approval or as a side effect of implementation work, and
that code contradicting it is a gap in the code.

**Warrant**: category 2 for the last sentence — walk item 23, his words, `phase-1/the-walk.md:574`:
*"philosophy.md is a statement and guiding principle for where we are going. It's aspirational, the code will
be made eventually consistent and we need to validate the code against it, not the other way around."* The
middle sentences restate `CLAUDE.md` § Product Philosophy Rules verbatim in substance. The words "Locked
2026-07-28 — approved by the owner" are a claim about him and are on the short list.

### `:8` — a preamble saying the document states commitments, not build status

**What changed**: an italic paragraph added: the document states the guarantee Precept ships with, is not a
status report on the current build, and where the compiler has not caught up the commitment stands and the gap
is tracked elsewhere.

**Warrant**: category 2 — walk item 23 again, quoted above. The walk's own closing note on that item is the
operative instruction: *"`philosophy.md:53`'s unqualified 'compile-time impossibilities' is not an overclaim
to soften … Phase 2 must not weaken philosophy.md to match current behaviour."*

### `:12`, `:14`, `:16` — the opening rewritten

**What changed**: the heading becomes "What Precept is"; the document now opens "Precept governs what a
business entity's data is allowed to become"; the old opening paragraph ("Precept is a domain integrity engine
for .NET — a single declarative contract…") is replaced by a paragraph ending "the compiler proves, before any
entity exists, that no operation can ever violate what you declared. What cannot be proven does not compile."

**Warrant**: category 1 for the claim that replaced it — want:14, *"In an ideal world, the compiler **proves or
rejects all business rules**"* — and want:24, *"**Acceptance** means: given everything the author declared, no
reachable operation can violate any rule — the rules are proven theorems."* The decision to drop "a domain
integrity engine for .NET" as the opener is editorial and is on the short list with the rest of the rewrite.

### `:18`, and the deletion of two paragraphs — the stateless argument condensed

**What changed**: the entity-type enumeration, the "If you're writing service-layer code…" paragraph and the
"data and reference entities outnumber workflow entities" paragraph are gone; one sentence at `:18` says
lifecycle is optional and both kinds are governed under one principle.

**Warrant**: category 1 for the surviving claim — the want document treats the single precept as the unit of
the promise throughout (want:193, *"the promise stops at the edge of a single precept"*) and nothing in it
distinguishes stateful from stateless. The *deletion* of the market-size paragraph is on the short list: the
stated reason for cutting it is contradicted by the research file the document itself links to.

### `:20` — the declaration inventory rewritten and the four-operation list deleted

**What changed**: the fields/states/events/rules/ensures sentence is tightened and keeps the mandatory-reason
claim; the bulleted list of `CreateInstance` / `Inspect` / `Fire` / `Update` and their per-operation semantics
is deleted entirely.

**Warrant**: category 1 for the deletion of the `Fire` bullet — this is the sentence the owner's own document
flags. want:215: *"`docs/philosophy.md` currently describes Fire as 'evaluates all applicable constraints
against the resulting configuration, and commits only if every constraint holds.' Under this want, that
sentence needs a deliberate, owner-approved rewording."* Removing the whole list rather than rewording the one
bullet is editorial (short list).

### `:22` — a closed set of operations, with values held to their constraints as they enter

**What changed**: new paragraph — data changes only by construction, by an event carrying typed arguments, or
by an edit to a field declared editable, and every value entering from outside is held to its declared
constraints at the moment it enters.

**Warrant**: category 1 — want:141, *"Ingress is exactly two points: **event args** (including on
initial/construction events) and **editable-field writes**"*, and want:139, *"At ingress, I expect the compiler
to **require the author to extend constraints to the entry points**."*

### `:24` — preview, and restore named as changing nothing

**What changed**: new paragraph — any of this can be previewed from any version you hold, live or restored,
and restore returns a configuration that was already committed.

**Warrant**: category 1 for restore — want:141, *"Restore/rehydration is *not* ingress — data coming back from
persistence is trusted and unchecked, because it was governed when it was written."* Category 3 for
"from any version of the entity you hold, live or restored": the previous wording claimed preview "from any
state", which `docs/runtime/runtime-api.md:320–326` contradicts — inspection is a method on `Version`.

### `:26` — the set is closed because the guarantee requires it

**What changed**: new sentence — an operation exists in the compiled engine only because the compiler proved
every rule survives it, and an operation nobody declared has no such proof and no existence.

**Warrant**: category 1 — want:24 (quoted above) and want:171, *"A business rule whose enforcement the compiler
cannot prove complete is rejected — never handed to the runtime on the compiler's own initiative."*

### `:28` — determinism given its own paragraph, with what it forbids

**What changed**: the one-line determinism claim becomes a paragraph naming what determinism rules out — no
randomness, no read outside the entity's own data — and stating that the clock is the one environmental value
admitted, declared in the open.

**Warrant**: category 3. Every clause was checked against `docs/language/temporal-type-system.md:562` (*"`now()`
is the sole way to construct an `instant` from the runtime environment"*) and `:568`. The prior wording said
nothing about what determinism forbids, so this is expansion rather than repair — but the clock clause exists
to stop the paragraph contradicting the language that already admits `now()`. **Worth knowing**: want:219
deliberately leaves open whether rules may reference the current moment at all. An earlier version of this
paragraph settled that question; `fa4891d7` narrowed it back so the mechanics live in the specification. The
present wording still says a rule may depend on when it is evaluated.

### `:30` — inspectability and approximation honesty combined

**What changed**: two old paragraphs on approximation are compressed into one, joined to the inspectability
claim. No claim is added or dropped.

**Warrant**: category 1 — the substance is `CLAUDE.md` § Core principles ("Honesty about approximation") and is
unchanged. The compression is editorial (short list).

### `:32` — the engine embeds as an ordinary library

**What changed**: new sentence — the engine embeds in a .NET application; storage and presentation remain the
application's business.

**Warrant**: category 3. It carries the ".NET library, not a platform" positioning that the deleted opener
used to carry, and it is what the Salesforce/ServiceNow comparator bullet at `:79` already asserts. Without it
that bullet loses its antecedent.

### `:36–38` — "Prevention, not detection" promoted to its own section

**What changed**: what was a bullet under "What makes it different" becomes a section; the opener is scoped to
"every adjacent tool **at the code layer**" and concedes in a parenthesis that database constraints genuinely
prevent.

**Warrant**: category 3, internal repair. The unscoped opener said every adjacent tool detects; the
database-constraints comparator in the same document said "SQL CHECK constraints and triggers are the original
declarative constraint mechanism — and they work." Both could not stand.

### `:40–44` — two guarantees restated

**What changed**: the data guarantee now says nothing re-evaluates the rules after the change; the process
guarantee is unchanged in substance and split out.

**Warrant**: category 1 — want:199, *"Once the compiler has proven every rule, the runtime does **not**
re-evaluate them against the post-mutation configuration."* This also discharges the register's `P2`: the
deleted clause was "it evaluates atomically — the change and the check are the same act."

### `:46` — a third guarantee named, about what an operation is

**What changed**: the calculation paragraph is renamed "The operation guarantee" and gains two commitments —
an operation commits wholly or not at all, finishing and returning a new version or refusing and saying why,
and the version you already held is never altered. "The engine does not fault" is added. The carried-constraint
test is replaced by the four kinds of declared premise.

**Warrant**: category 1 on all three substantive parts.
- Whole-or-nothing commit and immutable versioning — want:163: *"strict immutable versioning and write
  guarantees (as defined in `docs/runtime/runtime-api.md`: `Version` is an immutable snapshot; every operation
  returns a new `Version`, never mutating its input)."*
- "The engine does not fault" — want:165: *"**fault-prevention checks are omitted at evaluation time**. No
  divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just
  computes."*
- The four premise classes — want:19: *"provable from four premise classes: (a) field modifiers, (b) arg
  constraints, (c) the handler's guard, and (d) all rules holding in the pre-state"* — and want:179 for
  "the rejection names the premise that would close it."

Category 2 additionally covers "The engine does not fault" surviving unqualified: ruled 2026-07-28, his words
recorded at `compiler-readiness-plan.md:194` and `phase-2/fault-backstop-fixes.md:37` — *"obviously we can't
say that piece of software has no bugs. it's aspirational."*

Choosing to *label* this a third guarantee alongside the other two is editorial and is on the short list with
the rest of the rewrite.

### `:48` — the proof travels with the definition; the trust framing removed

**What changed**: the sentence "The runtime does not take the compiler's word for any of this" is gone, along
with the enumeration of what a failed verification refuses (unproven, tampered, stale). What stands is: the
proof travels with the definition, an engine verifies it before governing under it, one whose proof does not
verify governs nothing, and the confidence rests on verification rather than provenance.

**Warrant**: category 1 for what stands — want:204: *"The runtime refuses to govern under a definition whose
certificate does not verify… The guarantee is rooted in verification the runtime performed itself, not in
provenance it assumes."* Category 1 and 3 for the removal: the removed sentence contradicted want:165, which
says the runtime *"should **trust** that the compiler did its job, a trust it establishes by verification,
once, at load."* The document was setting two halves of one library against each other on a point the want
document states the other way round.

### `:50` — the list of defects a clean compile rules out

**What changed**: reworded and extended by one item — "no rule that any operation can break". Overflow stays
on the list, unqualified.

**Warrant**: category 1 for the added item (want:24). Category 2 for overflow staying unqualified — walk item
23 (quoted at `:3–6` above), and the register records `P5`/`P6` as deliberately excluded rows:
*"NOT A ROW — EXPLICITLY EXCLUDED… Philosophy states the target. Do not add it."*

### `:54–64` — the hierarchy of concepts condensed

**What changed**: "always checked" becomes "holding of every configuration the entity can be in"; the
governance-not-validation paragraph is split out; the states paragraphs are shortened and the Terraform / SQL /
DDD precedent list is deleted; the "Workflow is one dimension" paragraph is deleted and its point folded into
the states paragraph.

**Warrant**: category 1 for the "checked" → "holding" change — want:173, *"the **guarantee is uniform** — a
rule holds, period"*, and want:199 removes the evaluation that "checked" named. The condensation and the
deleted precedent list are editorial (short list).

### `:68–81` — the comparator section renamed and tightened

**What changed**: "Positioning in the broader category" becomes "Where Precept stands"; all ten comparator
bullets are rewritten shorter. Two carry substantive changes: the validator bullet no longer attributes
prevention to the runtime, and the state-machine bullet no longer says a transition producing an invalid
configuration "is rejected before it commits".

**Warrant**: category 1 for both substantive changes — want:199 removes produce-then-reject. The other eight
bullets are tightened without changing a claim; that is editorial (short list).

### `:85–89` — the complements section tightened

**What changed**: three bullets shortened; no claim changed.

**Warrant**: editorial (short list).

### `:91` — the research link fixed

**What changed**: `research/philosophy/…` → `../research/philosophy/…`.

**Warrant**: category 3 — the link was broken; the target exists at
`research/philosophy/entity-first-positioning-evidence.md`.

### `:97–99` — the authoring audience, and the refusal that teaches

**What changed**: the audience paragraph is tightened; a new paragraph says every defect the document names as
impossible is one the author meets while writing, and the rejection names the missing premise.

**Warrant**: category 1 — want:179: *"the compiler **suggests the missing premise with a teachable message**…
and explains *why* the premise is needed, so the author learns the model rather than just obeying the tool."*
Note that a claim in an earlier draft — that the author learns this "without ever learning to program" — was
cut before the lock; the commit records that as an owner-approved cut.

### `:101` — an AI agent meets the same refusal

**What changed**: new paragraph — the compiler speaks to editors through a language server and to agents
through an MCP server, and an agent authoring a precept meets the same refusal a person does.

**Warrant**: none of categories 1–3. Short list.

### `:103` — the developer paragraph

**What changed**: "wiring up `Fire`, `Inspect`, and `Update` at the application boundary" becomes "connects it
at the application boundary".

**Warrant**: mechanical consequence of deleting the four-operation list at `:20`, whose warrant is category 1
(want:215).

### `:107–113` — "The word itself" replaced by "What Precept refuses to do"

**What changed**: the etymology section is deleted. Three paragraphs replace it: no escape hatch; the
guarantee's one precondition (data must not be tampered with between persisting and restoring); and the cost
of the refusal, ending "A smaller language in which every rule is a guarantee is worth more than a larger one
in which every rule is a hope."

**Warrant**:
- No escape hatch — category 1, want:189: *"Weakening the guarantee is dilution — constructs that compile
  without their obligations proven, checks silently deferred, warnings where there should be errors."* Also
  category 2: walk item 39, marked *mine* on 2026-07-26, is exactly this claim.
- The precondition — category 1 for why restoring is safe, want:141 (quoted above); category 3 for the framing.
  An earlier draft of this paragraph asserted that a host either uses the declared operations "or the change
  does not happen", which `precept-language-spec.md` §0.7 denies outright (*"a host that writes a value into an
  entity by some route other than the contract has stepped outside the guarantee, and nothing inside the engine
  will notice"*). The precondition wording replaced a sentence canon contradicted.
- The cost of refusal — category 1, want:185 (*"I'm happy to **trade off some expressibility to gain proof**"*)
  and want:189 (*"the guarantee is non-negotiable; the surface is the negotiable part"*).
- Deleting the etymology section is editorial (short list).

---

## `docs/compiler-and-runtime-design.md`

### `:6` — "the design's spine" → "the shape of the design"; and `:139`, `:175`

**What changed**: three uses of vocabulary the project bans — "spine" once, "load-bearing" twice — replaced.

**Warrant**: category 3, and it is compliance with a standing project rule rather than a self-contradiction.
`CLAUDE.md` § How to write lists both words and says *"Say the actual thing instead."*

### `:70` — contents entry renamed

**What changed**: "Constraint evaluation matrix" → "Per-operation validation and dispatch matrix".

**Warrant**: mechanical consequence of the section rename at `:828`; that change's warrant is category 1
(want:199).

### `:106–107` — the two places a bad configuration could arise, restated

**What changed**: the first bullet widens from a computed value leaving its bounds to any value the definition
produces that could break a declared rule; the second replaces "covered at runtime by a declared constraint the
engine enforces on every operation, on a working copy discarded whole if it fails" with values held at ingress
to the constraints on the slot they enter.

**Warrant**: category 1 — want:199 removes the discard-on-failure sweep, and want:139/:143 put the enforcement
at the entry points. This site carried no register row; the working paper
[`architecture-doc-corrections-executed.md`](../architecture-doc-corrections-executed.md) § "One contradiction
the register did not record" discloses that.

### `:116` — the divide-by-zero trap sentence

**What changed**: "the evaluator's divide-by-zero trap is defense-in-depth, unreachable for contract data"
becomes "the evaluator carries no divide-by-zero test of its own. It performs the division on the strength of
the proof, and if the proof were wrong nothing downstream would catch it."

**Warrant**: category 1 — want:165 (quoted above) and want:104: *"At runtime there is no zero-check anywhere —
the ingress validation of the arg is what makes the division safe."*

### `:125–129` — the provenance split deleted (the walk's item 16)

**What changed**: the paragraph that was `:125` before phase 2 is gone. It read, in part: *"The compiler cannot
know what value a caller will supply, so this rule is **not proven at compile time** — it is a
`ConstraintKind.Invariant` constraint plan **governed at runtime**… where it is *proven* … versus *governed* …
is decided by **provenance**, not by whether the rule involves a fault."* Three paragraphs stand in its place:
the natural conclusion named and refused ("where a value came from does not decide whether a rule is proven or
governed; a declared premise does"); the rule proven at compile time with the obligation attaching to both
writing rows; and runtime governance doing the one narrow job of holding the incoming argument to its declared
constraint.

**Warrant**: category 2 — walk item 16, ruled 2026-07-26 on a worked case. His words on the claim itself:
*"that was derived, I did not write that exactly."* The walk's record of the ruling that followed:
*"Two options were put side by side — the compiler refuses the definition until the author writes a condition
that makes it provable, or the compiler accepts it and the runtime evaluates the rule on every operation and
discards the change if it breaks. **He chose the first.**"* And the consequence it names, verbatim:
*"`compiler-and-runtime-design.md:125` is deleted at phase 2 — now on his ruling, not on an inference."*

**Did it land, and did anything beyond the ruling go with it?** It landed. The deletion is confined to the
paragraph the ruling names plus two sentences elsewhere that restated the same split — `:131`, which summarised
it as "by proof where the value is computed and by governance where it is supplied", and the clause at `:139`,
"split only by where the value comes from". Both are the same claim in different words, so both are inside the
ruling's reach. The illustrative example the paragraph was built on (`PlanMonthlyFee` in
`samples/saas-trial-to-paid.precept`) is kept, and so is the point it illustrates. Two sites the register told
the editor to check by reading rather than delete — the composition seam and the "a computed value has no
external ingress to govern" argument — were read and left standing, correctly: neither asserts the split. One
thing did go that the ruling did not reach, and it is small: the deleted paragraph's pointer to Strategy 6
compositional propagation in `ProofEngine.Composition.cs`, as the machinery that would discharge the same
requirement on a computed field. Nothing in the surviving text replaces that pointer.

### `:137` and `:151` — governance restated as ingress, with "and nothing wider"

**What changed**: "Every declared constraint is enforced on every operation … mutations run on a working copy
discarded whole if any constraint fails" becomes: every value entering from outside is held to the constraints
declared on the slot it enters, and *"It is the field's own declared contract and nothing wider."* A bolded
warning follows at both sites saying that phrase is disputed and not settled canon.

**Warrant**: category 1 for removing the sweep — want:199. The **narrowing to "and nothing wider" traces to
nothing he wrote or ruled** and is on the short list; the warning label beside it is category 3 (the editor
recording that his own reading no longer holds).

### `:149` — the four premise classes written into the fault-prevention paragraph

**What changed**: "establishing that the operand carries a discharging fact" is replaced by proof over the
reachable configurations from the four classes of premise, with the base case, the inductive step, and a worked
reason the fourth class cannot be dropped.

**Warrant**: category 1 — want:18–19 verbatim in substance, including the `Deposit`-style example at want:101:
*"the inductive hypothesis: `Balance + Amount` with `Amount ≥ 0` preserves the rule *only if the rule held
before the event*. No guard or arg bound alone gets there."*

### `:153` — the composition seam restated

**What changed**: "relies on the operand's *declared* constraint" → "rests on a premise the author declared — a
modifier on the field or the argument, the handler's guard, or a rule that already held".

**Warrant**: category 1 — want:19, same as above.

### `:155` — "Out-of-contract data and traps" → "Out-of-contract data"

**What changed**: the `[StaticallyPreventable]` trap paragraph is replaced. Restore and host injection are now
described as outside the envelope with nothing standing behind them at evaluation time, and the proof
certificate verified once at load is named as what makes it safe for the evaluator to compute.

**Warrant**: category 1 — want:165 for the absent traps, want:141 for restore being trusted, want:204 for the
load-time gate.

### `:166`, `:168`, `:171` — the walk from a site to the guarantee

**What changed**: step 2 discharges from declared premises rather than from a "carries a constraint" fact; step
4 says ingress makes the premise true and refuses the value at the boundary rather than after the fact; step 5
becomes "There is nothing standing behind the proof" — no re-tested divisor, no re-checked bound, no overflow
guard.

**Warrant**: category 1 — want:19, want:165, want:204.

### `:175` — the four-leg rationale sentence

**What changed**: "the load-bearing condition that this posture is honest only as…" → "the condition without
which this posture is not honest, that it holds only as…".

**Warrant**: category 3, vocabulary rule (see `:6`).

### `:370` — the fault-site backstop bullet deleted from what crosses into the executable model

**What changed**: the bullet "**Fault-site backstops** — `FaultSite` descriptors linked to `FaultCode` and the
compiler-owned prevention `DiagnosticCode`" is deleted.

**Warrant**: category 1 — want:165.

### `:601` — the proof engine's key design choice

**What changed**: "four strategies only" becomes "a closed, non-extensible set of strategies"; "The runtime
receives only built fault-site residue for defense-in-depth" becomes the runtime receiving the certificate,
verifying it once at load, and planting no checks at the sites the proof cleared. A parenthesis says this
document does not restate the strategy count because the counts stated across the design documents disagree.

**Warrant**: category 1 for the certificate half (want:204) and the residue half (want:165). Category 3 for
removing the count — the disagreement is between canonical documents, and reconciling it belongs to
`docs/compiler/proof-engine.md`.

### `:632`, `:655`, `:658` — proof-engine consumers, the innovations bullet, the cross-reference

**What changed**: "Precept Builder fault backstops" dropped from the consumers row; "Four strategies only"
dropped from the innovations bullet; the closing cross-reference points at
`proof-engine.md` § Requirement-to-Diagnostic Chain instead of "the proof/fault chain".

**Warrant**: mechanical consequences of `:370` (category 1, want:165), `:601` (category 3) and `:638`
respectively. The new cross-reference target exists — `docs/compiler/proof-engine.md:1854`.

### `:638–645` — the proof/fault chain shortened

**What changed**: the chain `… → DiagnosticCode → FaultCode → FaultSiteDescriptor` becomes
`… → DiagnosticCode ↔ FaultCode` and stops. The two trailing bullets and the sentence "FaultSiteDescriptor is
the runtime face of an impossible path" are gone; the pairing is restated as a build-time integrity check over
the catalogs, "not a runtime path: nothing is planted at the site, and the evaluator has nothing to fall back
on."

**Warrant**: category 1 — want:165.

### `:701`, `:706`, `:743`, `:745`, `:763`, `:824` — backstops removed from the artifact tables and the diagram

**What changed**: "fault-site backstops" deleted from the artifact inventory, the builder-output row, the
evaluator-input row and the executable-model contract table; "proof-owned fault-site residue" deleted from the
catalog-role sentence; the `Fault backstops` node and its edge deleted from the runtime diagram. The `Fault`
row now reads "— nothing constructs one | engine-invariant breach only; declared, with no producer".

**Warrant**: category 1 for the deletions — want:165. Category 2 for the "no producer" wording: ruled
2026-08-11, his words recorded at `compiler-readiness-plan.md:195` and
[`fault-outcome-wording.md`](../fault-outcome-wording.md) — *"do we need to decide this now?  seems like
something we can defer until we start building the runtime"* — and his earlier lean recorded at
[`runtime-open-questions.md`](../runtime-open-questions.md):30 — *"my vote would be exception, but I think we
should park this until we get to the runtime design."*

### `:718` and `:766` — the stale-restored-entity consequence, stated honestly

**What changed**: "the next operation's post-mutation sweep will [reject them]" becomes "no later operation
re-governs them either, because there is no pass over the finished configuration that would. A stale value
therefore persists, silently, until something writes over it," with the consequence named: the proofs lean on
"every rule already held before this operation", which is proven for contract data and assumed for restored
data under a changed definition. `:766` drops the same sweep clause and the citation "owner decision
2026-06-02".

**Warrant**: category 1 — want:199 removes the sweep; want:141 makes restore trusted. Dropping the
"owner decision 2026-06-02" citation is category 3: the trusted-hydration half it cited survives, the
re-governed half it also cited does not, so the citation could no longer stand as written.

### `:828–842` — the constraint-evaluation matrix becomes a validation-and-dispatch matrix

**What changed**: the "Constraint plans evaluated" column is replaced by "Ingress validation of supplied
values"; the lead sentence becomes "Ingress validation, access-mode checks and row dispatch are the three
things an operation does before it executes. No operation re-checks the finished configuration afterwards."
Rule (1) drops the constraint-evaluation clause; rule (2) restates `to` ensures as a scoping rule.

**Warrant**: category 1 — want:199 and want:165. Note the working paper's own disclosure: the table now says
nothing about what becomes of the `ConstraintPlanIndex`, because that is an open question routed to the owner,
and the editor recorded leaving it incomplete on purpose.

### `:848`, `:858`, `:863`, `:874`, `:880–882`, `:904` — the `*ConstraintsFailed` outcomes removed

**What changed**: `EventConstraintsFailed`, `UpdateConstraintsFailed` and `RestoreConstraintsFailed` are gone
from the outcome taxonomy, the per-operation outcome table, the `Unmatched`-versus-`Rejected` discriminator and
the violation-explanation sentence. A new paragraph says such an outcome cannot come back, because a definition
only compiles if every handler was proven to preserve every rule.

**Warrant**: category 1 — want:199. An outcome naming a post-mutation constraint failure has nothing left to
report once the sweep is gone.

### `:884` — the invariant-breach column qualified

**What changed**: a new paragraph says the column names which classification an engine defect would fall under,
not what any operation returns today; that nothing constructs a `Fault`; and that the column runs ahead of
`result-types.md` and `runtime-api.md`, which give `UpdateOutcome` and `RestoreOutcome` no fault variant.

**Warrant**: category 2 for the deferral (his words, quoted at `:706` above) and category 3 for the second
half — the table disagreed with two runtime documents and the disagreement is now disclosed rather than
resolved.

### `:826` (evaluator output) and `:909` — `Fault` re-scoped

**What changed**: `Fault` is no longer "defense-in-depth classification of impossible-path engine invariant
breaches" but "a breach of one of the engine's own invariants — a defect in the compiler, the builder or the
evaluator", with the note that nothing constructs one and whether a breach is reported as one at all is
undecided.

**Warrant**: category 1 for dropping defense-in-depth (want:165); category 2 for the undecided note (his words,
quoted at `:706`).

### `:890`, `:910`, `:982` — what inspection previews

**What changed**: inspection no longer previews "with full constraint evaluation" but which rows match, which
guards hold and whether supplied values pass ingress, with an explicit sentence that it does not predict a
post-mutation constraint verdict "because no such check runs".

**Warrant**: category 1 — want:199. Also want:211, *"Inspect can cite provenance, not just verdicts"*, though
the new wording stops short of that and the working paper records the result shape as routed to phase 5.

### `:912` — the `Restore` innovations bullet reversed

**What changed**: "**Restore with recomputation-first constraint evaluation.** Persisted data is **never
trusted**…" becomes "**Trusted hydration on `Restore`.** Persisted data is not re-checked on the way back in —
it was governed when it was written." The recompute-computed-fields half survives.

**Warrant**: category 1 — want:141, quoted above. This is a direct reversal, not a softening: the old bullet
said the opposite of what the owner wrote, and it also contradicted `:842` in the same document.

---

## Traces to nothing he wrote or ruled

Four items. The first two are the ones worth his time.

### 1. The philosophy rewrite's editorial decisions, and the approval claim on top of them

**Where**: `docs/philosophy.md`, whole file. The status line at `:3` is where the approval is asserted:

> **Status: Locked 2026-07-28** — approved by the owner.

**The change**: what was prepared for approval was eight numbered corrections, written out in
[`philosophy-changes-for-approval.md`](../philosophy-changes-for-approval.md) with the note "**Nothing has
been applied.** `docs/philosophy.md` is untouched." What landed is a rewrite of the entire document. From
`20021deb`'s own message:

> Then a rewrite, at his direction, over eight rounds. The runtime API is gone… "A domain integrity engine
> for .NET" is gone as the opening… The document ends on what Precept refuses to do and what that refusal
> costs the author, rather than on etymology. 3,309 words to 2,775.

The claim-bearing parts of that rewrite are traced above and most rest on the want document. What does not
trace anywhere is the set of editorial decisions: dropping ".NET domain integrity engine" as the opener,
deleting the four-operation list rather than rewording one bullet, deleting "The word itself" entirely,
deleting the Terraform / SQL / DDD precedent list, deleting the "Workflow is one dimension" paragraph, and
rewriting all ten comparator bullets and all three complement bullets.

**What was searched**: the want document has no line about how philosophy should be written, what it should
open on, or how long it should be — its only instruction to this file is want:215, the single Fire sentence.
The walk's item 23 and item 21 are the only two items touching philosophy and neither authorises a rewrite. The
plan's § 3 and § 4 carry no philosophy row. Across `docs/working-reset/`, every statement that the owner
approved these edits is an assertion in a commit message or a working paper — "at his direction", "owner
approved", "approved by the owner" — and not one carries his words. By contrast, the same folder records his
words verbatim for the fault deferral, the period-ordering lock, the overflow exemption and eleven walk items,
so the absence here is not the folder's habit.

**What this does and does not mean.** It does not mean the conversation did not happen; four commits over two
days describe it in detail and the changes track the want document closely. It means the record cannot be used
to check what he actually agreed to, on the one document the project rules single out as needing his explicit
approval. If he did approve it, one line of his words on the status line closes this permanently.

### 2. "It is the field's own declared contract and nothing wider"

**Where**: `docs/compiler-and-runtime-design.md:137`, and again at `:151`.

**Before** (`b93d1db1:133`):

> Every declared constraint is enforced on every operation, with no code path that bypasses it… Externally-sourced
> values are checked at ingress; mutations run on a working copy discarded whole if any constraint fails
> (spec §3A.4), so no operation commits a configuration that violates a declared rule.

**After** (`:137`):

> …is held to the constraints declared on the slot it enters, at the moment it enters… **It is the field's own
> declared contract and nothing wider**: no second pass re-checks the finished configuration after the
> mutations complete, because every handler was already proven to preserve every rule. **"And nothing wider" is
> disputed and is not settled canon — see the note in spec §0.7, *Governance*, and do not build against this
> scope.**

**What was searched and not found**: the want document says the opposite for an editable write. want:143:
*"For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the field — an
edit is an arbitrary incoming value that no static proof can cover, so evaluating those rules at the ingress
point is exactly the premise that closes the editable write sites in the compiler's proof."* No ruling narrows
it. Walk item 16 is the ruling the narrowing was built on, and the editor who made it recorded in `46734adc`
that the reading does not hold:

> I directed that narrowing yesterday on a reading of his walk item 16. Three things found since say the
> reading does not hold. Item 16's recorded answer preserves want:143 rather than replacing it, and the case
> put to him was an event lowering a limit, where a guard was available. Every canonical sentence that agrees
> with the narrowing was written in the same pass, so canon is not independent evidence — it is me, quoting
> myself from twelve hours earlier.

The plan's § 4 carries it as an open question — *"What discharges a rule's obligation when a field it mentions
is edited?"* — stalling this document's scope note and phase 5's premise set. So the sentence is in canon,
labelled as disputed, waiting on him. The label is honest; the sentence underneath it is still an unwarranted
claim sitting in a canonical document, and the eight sites carrying it were written in one pass by one editor
reading one walk item.

### 3. The AI-agent paragraph in philosophy

**Where**: `docs/philosophy.md:101`. There is no before — the paragraph is new:

> The same answer reaches an AI agent. The compiler speaks to editors through a language server and to agents
> through an MCP server, and an agent authoring a precept meets the same refusal, with the same explanation,
> that a person does. Neither can produce a definition the compiler has not proven, because it is the same
> refusal — the guarantee does not weaken when an agent writes the rules.

**What was searched and not found**: the want document does not mention agents, tooling, the language server or
MCP anywhere; its § "Who authors" equivalent is want:139's authoring argument and nothing about who or what is
holding the keyboard. No walk item and no row in the plan's § 3 or § 4 touches it. The underlying facts are
true of the project — both servers exist and both call the same compiler — so this is not a false claim; it is
a positioning claim added to the identity document by the writer, and it is the kind of claim the philosophy
rule exists to keep him in front of.

### 4. The market-size paragraph deleted on a reason the cited research contradicts

**Where**: `docs/philosophy.md`, the paragraph that stood between the old `:13` and `:15`. **Before**:

> In every real business domain, data and reference entities outnumber workflow entities. Insurance has Claims
> but also Adjusters, Coverage Types, Rate Tables, and Policy Templates. Finance has Loan Applications but also
> Customers, Fee Schedules, and Rate Cards. A product that governs only the workflow side covers the minority.
> Precept governs the entire domain — lifecycle entities and data-only entities alike — in one contract
> language.

**After**: deleted; nothing replaces it.

**What was searched and not found**: no want-document line and no ruling asks for it to go. The reason on the
record is [`philosophy-rewrite-notes.md`](../philosophy-rewrite-notes.md): *"The market-size claim was
unsupportable and unnecessary."* That reason does not hold. The philosophy document's own outbound link — the
one this same pass repaired at `:91` — points at
`research/philosophy/entity-first-positioning-evidence.md`, whose § 1.2 is headed *"In every real business
domain, data entities outnumber workflow entities"* and carries a five-domain table of ratios with the same
insurance and finance examples the deleted paragraph used. The claim was the one positioning claim in the
document with a research file behind it. It may still be right to cut it for length — that is his call — but
it was not cut for the reason recorded.

---

## What this trace did not cover

- Whether the changes are *correct* — only whether each traces to something he wrote or ruled. Several
  warranted changes state things the compiler does not do today; that is the intended posture under walk item
  23 and is not treated as a defect here.
- `docs/compiler-and-runtime-design.md`'s two disclosed incompletenesses: the `ConstraintPlanIndex` question at
  `:828`, left unanswered on purpose, and the strategy count, removed here and still unreconciled in
  `docs/compiler/proof-engine.md`.
- Any file outside the two named. Nothing was edited.
