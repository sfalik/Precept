# The editable-field write: the whole design space

**Status:** Enumeration — 2026-07-27. Not a recommendation, not a ranking, not an argument for any
position. Every design that satisfies the stated constraints, plus the directions that are empty and the
reason each is empty.

**Evidence used:** `docs/Working/what-i-want-2026-07-16.md`; `docs/working-reset/phase-1/the-walk.md`;
the canonical documents at HEAD (`docs/language/precept-language-spec.md`,
`docs/compiler-and-runtime-design.md`, `docs/runtime/runtime-api.md`, `docs/philosophy.md`); the phase-2
reconciliation files. **Not used:** catalogs, `samples/`, compiler behaviour. Nothing was compiled.

---

## 1. The problem in one paragraph

A field declared `editable` is written directly — the caller hands a value to `Version.Update` and it
lands in the slot. There is no event, so there is no argument to constrain; there is no row, so there is
no guard. Of the four premise classes the induction runs on (want:19 — field modifiers, argument
constraints, the handler's guard, the rules holding in the pre-state), only two are available at an
edit: the field's own modifiers, and the pre-state rules. For a rule that relates the edited field to
another field, those two are not enough. The worked example in the want document is exactly this shape:

```
field RepaymentCapPercent as decimal default 0.25 positive max 1.0 editable          (want:36)
rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set   (want:40)
```

Editing the cap from 0.25 to 0.05 is legal for the field — still positive, still at most 1.0 — and makes
the rule false, with neither other field touched. And a second problem sits behind it: if a rule mentions
a computed field derived from the edited one, anything checking that rule needs the recalculated value,
and recalculation happens at execution phase 7 (`precept-language-spec.md:2011`) while ingress runs at
phase 1 (`:2005`).

## 2. The four constraints, restated

| | Where | What it says |
|---|---|---|
| **C1** | want:199 | Once the compiler has proven every rule, the runtime does **not** re-evaluate them against the post-mutation configuration. |
| **C2** | want:143 | For an editable-field write, the applicable rules are evaluated at the point the value enters — "exactly the premise that closes the editable write sites in the compiler's proof". |
| **C3** | want:171 | A rule whose enforcement the compiler cannot prove complete is rejected, never handed to the runtime. |
| **C4** | want:24 | Acceptance means: no reachable operation can violate any rule. |

And three rulings from the walk that bear directly:

- **Item 16** — a declared premise decides what the compiler proves, not where the value came from. Put
  as a worked case with two options side by side (compiler refuses until the author writes a condition
  / runtime evaluates the rule on every operation and discards the change), he chose the first.
- **Item 8** — writing a field a rule merely *reads*, including through the rule's activation `when` and
  through a computed field, takes on that rule's obligation.
- **Item 9** — faults are prevented unconditionally at the point of evaluation; every other rule need
  only hold on the configuration the handler commits.

**A note on C2 that every design below depends on.** C2 is a sentence about *mechanism* written before
item 16 was ruled. The phase-2 reconciliation (`sweep-correction-list.md` § S1) treats items 16, 8 and 7
as having replaced it, and canon at HEAD now says the opposite of it in eight places
(`precept-language-spec.md:274`, `:1976`; `runtime-api.md:402`; and others) — ingress upholds the
field's own declared contract and nothing wider. So when a design below is marked "strains C2", that
means: it overrides a sentence he wrote, on the strength of a later ruling. It does not mean the design
contradicts something still standing unchallenged. Designs marked "satisfies C2" satisfy it *literally* —
there is a check at the door, on the incoming value, and that check is what closes the write site's
proof. That is what C2's own words ask for; C2 does not require that the check be a re-evaluation of the
rule.

---

## 3. The designs

Grouped by what they change. Nothing here is ranked.

### Group A — change *when* something is evaluated

#### A1. Evaluate the reachable rules at the write, as want:143 states it

**What it does.** At execution phase 1, before anything derives, the runtime evaluates the edited field's
modifier-rules *and* every rule that mentions the field, against the pre-state with the patch applied.
Any failure refuses the whole patch, collect-all. The compiler treats that evaluation as a declared
premise and closes the edit write site with it.

**Constraints.** Satisfies C2 verbatim and C3 (the site *is* proven, given a premise the definition
declares). Strains C1: rules are being evaluated against a configuration that includes the mutation, and
the only distinction from the removed sweep is that the rule list is narrower and the proof consumes the
result. Fails C4 for one subset unless paired with something from Group C: a rule reaching the edited
field through a computed field is evaluated at phase 1 against a derived value the operation then
discards, so the check passes and the committed configuration can still violate the rule.

**What stops compiling.** Nothing, unless C4 is repaired — in which case, every definition where a rule
reaches an editable field through a computed field.

**What has to be built.** A compile-time reachability computation producing, per editable field, the list
of rules it reaches by all three routes of walk item 8. A runtime evaluator for rules at ingress (the
runtime keeps a rule-evaluation path the "lighter runtime" section wanted gone). Collect-all violation
reporting on `UpdateOutcome.ConstraintsFailed`. A certificate step kind recording the ingress evaluation
as the discharging premise, so the load-time checker knows it must run.

**What the author writes that they do not write today.** Nothing. The cost lands on the caller instead:
an edit that used to be accepted is now refused at runtime with a rule violation.

#### A2. Evaluate after recomputation, before commit — a phase 7.5 check on the reachable rules only

**What it does.** Same rule list as A1, evaluated after computed fields recalculate, on the finished
working copy, discarding the patch if any fails.

**Constraints.** Satisfies C2's intent and C4 including the computed-field case. Strains C1 harder than
any other design here: this is literally an evaluation of rules against the post-mutation configuration.
The only defence available is "the list is compiler-computed and bounded, and a proof consumes the
result" — which is A1's defence, and moving the check later does not strengthen it.

**What stops compiling.** Nothing.

**What has to be built.** A1's reachability computation, plus a new execution phase between 7 and 8 for
`Update` only, plus the discard path. Canon's execution-order section gains a phase, and the sentence
"there is no constraint-evaluation phase between recomputation and commit"
(`precept-language-spec.md:2012`) is deleted.

**What the author writes.** Nothing.

**Included because the direction has to be closed explicitly.** This is the partial sweep the reset plan
names (`compiler-readiness-plan.md` § Known risks). It is the cheapest design in the whole list to build
and the one that most plainly contradicts want:199.

#### A3. Recompute the patch's dependency closure *before* the ingress check

**What it does.** Split recomputation: on `Update`, recompute the computed fields the patched fields
reach, then run the ingress check (A1's list) against fully-derived values, then commit.

**Constraints.** Repairs A1's C4 hole without adding a post-commit pass. Strains a different statement
than A1 does — not want:199 but the ingress ordering: "checked … as they enter, **before the working copy
derives any dependent value**" (`precept-language-spec.md:2005`, want:141). Ingress stops being a door
check and becomes a check over a partially-derived working copy.

**What stops compiling.** Nothing.

**What has to be built.** A dependency-closure recomputation ordered by the patch (the graph is already
acyclic — computed fields cannot form cycles, `precept-language-spec.md:1362`). A canon change to the
execution phase order. Everything A1 needs.

**What the author writes.** Nothing.

#### A4. Read-time evaluation

**What it does.** Nothing is checked on write; a read of a configuration violating a rule fails.

**Why the direction is empty.** Fails C4 outright — a violating configuration exists, was committed, and
is durable, so a reachable operation did violate a rule. Fails the prevention commitment in
`philosophy.md` at the same time, and does not even reduce runtime work, it relocates it. Recorded so
nobody re-derives it.

#### A5. Load-time evaluation

**What it does.** The certificate gate the want document already asks for (want:201–204) runs at load.

**Why it is not a design of its own.** It checks the *definition*, once, not the data. It can verify that
every editable write site cites a discharging premise and that the reachable-rule list is complete
against the definition — real value, and every design below inherits it — but it cannot make an
unprovable edit safe. It is a checker for whichever design is chosen, never the choice.

### Group B — the compiler carries more, the runtime less

#### B1. Refuse any editable field a rule mentions that is not provable from declared bounds

**What it does.** For each editable field, take every rule it reaches. If the rule cannot be proven from
the edited field's own declared modifiers plus the pre-state rules alone, reject the definition. Runtime
ingress goes back to upholding the field's own contract only, which is what canon at HEAD already says.

**Constraints.** Satisfies C1, C3, C4 cleanly. Strains C2 in the sense set out in § 2 — the ingress
premise he named is deleted.

**What stops compiling.** The want document's own worked example, as written. To make it compile the
author declares bounds that close the rule arithmetically: a floor on the cap, a ceiling on the
repayment, a floor on the overdraft limit, chosen so `MonthlyRepayment_max <= OverdraftLimit_min *
RepaymentCapPercent_min`. That is a real business claim the author may not want to make — "this account
type always has a limit of at least £1,000" — and it is strictly stronger than the rule they wrote.

**What has to be built.** The reachability computation (shared with A1); a provability check per rule
against the bound set; and the teachable message. Nothing at runtime.

**What the author writes.** Bounds on several fields, or removal of `editable` and an event instead.

#### B2. B1, plus the compiler computes and prints the missing bound

**What it does.** Where B1 refuses, compute the weakest declared bound that would close the rule and name
it in the diagnostic — "add `min 0.05` to `RepaymentCapPercent`, or `max 50.00` to `MonthlyRepayment`".
The compiler never inserts it (want:179).

**Constraints.** Same as B1.

**What stops compiling.** Same as B1; the difference is entirely in what the author has to work out for
themselves.

**What has to be built.** B1, plus a weakest-bound computation. Tractable for the shapes substitution
already handles. For a non-linear rule there is generally no *single* bound that closes it — the cap rule
needs bounds on all three fields — so the diagnostic has to choose which combination to suggest, and that
choice is a design question in itself.

**What the author writes.** What B1 asks, with less thinking.

#### B3. Derive a precondition on the incoming value and check *that* at the door

**What it does.** At compile time, solve each reachable rule for the edited field and emit a per-field
admissible-value condition over the pre-state — for the cap rule, something of the form "the new cap must
be at least `MonthlyRepayment / OverdraftLimit`". The runtime evaluates that one derived predicate on the
incoming value against the pre-state, then applies. The runtime never evaluates a rule and never touches
the post-mutation configuration.

**Constraints.** Satisfies C1 (nothing post-mutation is evaluated — the predicate is over the pre-state
and the incoming value), C2 (the check at the door is the premise that closes the site), C3 (where the
predicate cannot be derived, reject), C4.

**What it strains, and it is not one of the four.** The derivation is algebraic rearrangement. want:135
says the proof "closes against what the author wrote, not against algebra the compiler won't attempt",
and walk item 13 makes "the compiler works with the expression as written" his ruling, with each rewrite
added one at a time under a written argument that it does not change the answer. Dividing both sides by
`OverdraftLimit` requires its sign and its non-zeroness, so the derived predicate carries its own fault
obligation. The derivable subset is small: linear rules with a literal coefficient on the edited field.
Outside it, refuse.

**What stops compiling.** Every rule outside the derivable subset — which for the want example means the
cap rule, since the coefficient is a field, not a literal.

**What has to be built.** A solving stage with a written validity argument per rearrangement kind; a
certificate step kind for a derived ingress condition; fault obligations on the derived predicate itself.

**What the author writes.** Nothing — but the refusal message becomes "cannot derive an admissible-value
condition", which is harder to teach than "add a guard".

#### B4. Prove-with-a-named-check: the compiler proves the rule *given* a boolean it also proves checkable

**What it does.** Separates the two things A1 conflates. The compiler proves, per edit write site, three
things: (i) there is a boolean expression evaluable at phase 1 from stored pre-state values plus the
incoming value; (ii) evaluating it is fault-free; (iii) it implies the rule holds in the committed
configuration. The runtime evaluates that boolean. Where the boolean *is* the rule and the rule mentions
no computed field, this collapses to A1. Where the rule reaches the field through a computed field,
condition (i) fails and the definition is refused.

**Constraints.** Satisfies C2, C3, C4. Strains C1 exactly as much as A1 and no more.

**Why it is separate from A1.** It turns A1's staleness hole from a rule somebody has to remember into a
proof obligation the compiler discharges or refuses on. A1 without B4's condition (i) is unsound; A1 with
it is B4.

**What stops compiling.** Definitions where a rule reaches an editable field through a computed field —
unless combined with C3 or C4 below, which make those expressible again.

**What has to be built.** Three certificate step kinds; an evaluability analysis over what phase 1 can
see; everything A1 needs.

**What the author writes.** Nothing.

#### B5. Finite-domain exhaustion

**What it does.** Where the edited field's admissible value set is finite and small — a boolean, an enum,
a bounded integer — the compiler enumerates every admissible value and proves each reachable rule for
each one. If every admissible value keeps every reachable rule true, the write site is proven with no
author cost and no runtime rule evaluation at all: the field's own bounds, checked at ingress, are
exactly what makes the proof close.

**Constraints.** Satisfies all four, literally, for its subset. C2 is satisfied in its own words — the
check at the door is the premise closing the write site. Outside its subset the definition is refused,
which is C3.

**What stops compiling.** Nothing new; it only widens what B1 accepts. Note the interaction with walk
item 8's activation route: an editable boolean or enum that switches a rule on is precisely this case, so
this closes a shape that would otherwise be a common refusal.

**What has to be built.** A bounded enumeration with a declared cap and the "couldn't prove — here is
what would" fallback that `precept-language-spec.md:225` licenses. The reset plan already has a bounded
counterexample search in scope, so the machinery is adjacent rather than new.

**What the author writes.** Nothing.

**The honest limit.** It does nothing for a `decimal`. It is a widening strategy, not an answer on its
own.

### Group C — make the cascade not exist

#### C1'. A computed field may not depend on an editable field

**What it does.** A computed field whose dependency closure contains an editable field is a compile
error.

**Constraints.** Kills the staleness half of the problem outright, so any Group A or B design becomes
sound for the whole space without further work. Says nothing about the direct-relational half.

**What stops compiling.** Any file deriving anything from an edited value — a total from an edited
quantity, a percentage from an edited rate. That is a very common shape and the cost is large.

**What has to be built.** A dependency-closure check. Trivial.

**What the author writes.** They stop editing the input and route it through an event, or they stop
deriving from it.

#### C2'. A rule may not mention a computed field that depends on an editable field

**What it does.** Narrower than C1'. The derivation stays legal; only *constraining* it is refused.

**What stops compiling.** Files that put a bound on a derived value whose inputs are edited — for
instance a computed total with a `max`, where a component is editable.

**What has to be built.** The same closure check, applied at rule sites instead of at declarations.

**What the author writes.** They restate the constraint over the stored inputs instead of over the
derived value, which is often possible and always more verbose.

#### C3'. The same refusal, pointed at the `editable` instead of at the rule

**What it does.** Identical accepted set to C2'; the compiler refuses the `editable` modifier and names
the computed field and the rule that forced it.

**Why it is listed separately.** Same programs refused, different message, different place the author
fixes it. Which of C2'/C3' is right is a diagnostic-design question, not a proof question, but the two
teach different models — C2' teaches "do not constrain derived values", C3' teaches "do not make inputs
to constrained derivations editable".

#### C4'. Inline computed fields into the rules that mention them

**What it does.** The compiler rewrites every rule mentioning a computed field into the stored fields it
derives from — the spec already requires exactly this expansion for constraint attribution
(`precept-language-spec.md:1968`, "transitive expansion to the concrete stored fields they depend on").
After the rewrite no rule mentions a computed field, so a phase-1 ingress check evaluates only stored
fields, whose phase-1 values are already their post-patch values. The staleness problem dissolves rather
than being handled.

**Constraints.** Satisfies C1, C2, C4; C3 unaffected. Makes A1/B4 sound over the whole space with no
refusals.

**What it strains, and again it is not one of the four.** Inlining is a rewrite, and walk item 13 makes
every rewrite owe a written validity argument. This particular argument is much easier than algebraic
rearrangement — it is substitution of a definition, and the acyclicity the language already guarantees
makes it terminate — but it is owed. Two real costs follow: the inlined expression can be large, and any
fault-prone operation inside a computed body (a division) now appears inside an expression evaluated at
ingress, so its obligations move too.

**What stops compiling.** Nothing directly. Some definitions may become unprovable for a different
reason once the expanded expression is what the proof engine sees.

**What has to be built.** An inlining pass; diagnostic mapping back to the computed field's name so
messages do not quote text the author never wrote; a ruling on whether the certificate records the
written form or the inlined form.

**What the author writes.** Nothing.

### Group D — change what an author may declare

#### D1. `editable` only on fields no rule mentions

**What it does.** A purely syntactic restriction: `editable` is permitted only where the field appears in
no rule beyond its own modifiers.

**Constraints.** Satisfies all four. C2 becomes exactly what canon already says ingress does.

**What stops compiling.** The want example, and any file where an edited field participates in a
relation — most of the interesting uses.

**What has to be built.** Almost nothing.

**What the author writes.** An event and a guard, instead of an edit.

**Why it is in the list.** It is B1 with the analysis replaced by a syntactic test, and the difference
between the two accepted sets is a precise measure of what the analysis is worth. If that difference is
small, B1 is not worth building.

#### D2. `editable` and `rule` mutually exclusive per precept

The same idea at file grain. Strictly more expensive than D1 for exactly the same guarantee. Recorded so
the corner is closed.

#### D3. Use the guarded access mode that already exists as a premise

**What it does.** `in <State> when <Guard> modify <Field> editable` is already in the language
(`precept-language-spec.md:1071`, `:1079` rule 4a). The guard is already evaluated at ingress, because it
decides whether the write is permitted at all. Read it as a premise: the compiler may use it at that
write site exactly as it uses a row guard.

**Constraints.** Satisfies all four literally. Nothing post-mutation is evaluated (C1); the check at the
door — access mode plus field modifiers — is the premise closing the site (C2); an edit whose guard does
not close the rule is refused (C3); C4 follows.

**The limit, stated plainly.** The guard is over the pre-state and cannot mention the incoming value. So
it closes rules where the edited field's own declared bounds suffice *once the guard has pinned the other
fields* — `in Active when OverdraftLimit >= 1000 modify RepaymentCapPercent editable`, with
`RepaymentCapPercent min 0.05` and `MonthlyRepayment max 50`, proves the cap rule. It cannot express a
condition on the value being written.

**What stops compiling.** Nothing. It widens what B1 accepts.

**What has to be built.** The proof engine must consume access-mode guards as premises; the certificate
must record the access-mode guard among the checks the proof depends on; canon must say the access-mode
guard is a premise spelling. Under want:22 — one constraint mechanism, several spellings — that is a
spelling, not a fifth class.

**What the author writes.** A guard on the `modify` declaration, plus bounds. Both already exist.

**Why it is under-noticed.** It needs no new language surface at all, and the phase-2 reconciliation
never considers it.

#### D4. A declared direction on an edit — the value may only move one way

**What it does.** A modifier saying an editable field's value may only increase (or only decrease). Then
the pre-state rules premise closes every rule in which the field appears monotonically: if the cap may
only rise, `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` holding before implies it holds
after.

**Constraints.** Satisfies all four literally for its subset — the check at the door is the field's
modifiers plus the direction test against the current value, and that is what closes the site.

**New language surface, and it needs its own design pass.** No syntax is proposed here. It is a modifier
rather than a construct, so the cost is well below D5's, but it still goes through catalog, grammar,
binder, type checker, proof engine, tooling and documents.

**What stops compiling.** Nothing; it widens B1.

**What has to be built.** A monotonicity analysis of each reachable rule with respect to the edited
field, which the interval machinery largely covers; the modifier itself.

**What the author writes.** The direction modifier, and they accept a genuine restriction — a cap that
can only ever go up is a different business object from one that moves freely.

#### D5. A condition on the edit that can mention the incoming value

**What it does.** The premise class missing at an edit site is (c), the guard, because there is nothing to
guard. Give the author a way to declare a condition the incoming value must satisfy, expressed over the
pre-state and that value. The author then writes the post-state condition directly, exactly as
`PlanRepayment`'s guard does at want:68, and the proof closes by substitution against what they wrote.

**Constraints.** Satisfies all four. C2 in its own words: the condition is evaluated on the value as it
enters, and that evaluation is the declared premise closing the write site. C1 holds — nothing
post-mutation is evaluated. C3 holds — no condition, no proof, refusal.

**This is new language surface and it needs its own design pass. No syntax is proposed here.** Questions
it opens, all of which belong to that pass and none of which is settled by listing the design: how the
incoming value is named; whether the condition attaches to the field declaration, to the `modify`
declaration, or to a state; whether it is one condition or several rows with `reject` messages, which is
what want:177's explanation requirement would push toward; how it composes with D3's access-mode guard;
whether it is one condition per field or per (field, state).

**What stops compiling.** Nothing at first. Then, if the condition is *required* wherever B1 would have
refused, everything B1 refuses stops compiling until the author writes one.

**What has to be built.** A construct — grammar, catalog entry, parser, name binder, type checker, a
premise class in the proof engine, certificate steps, diagnostics, language server, MCP vocabulary, and
the documents. This is the largest build in the list.

**What the author writes.** A condition at every edit site a relational rule reaches.

#### D6. Declared patch groups — prove the shape of the patch, not the single write

**What it does.** Walk item 9 says a handler is judged on the configuration it commits, and an `Update`
patch is a handler with N writes and no guard. So a patch lowering the cap *and* lowering the repayment
together can be legal where either alone is not. The design: the author declares which fields may be
patched as a unit, each declared group is one write site proven as a unit, and a caller may only submit a
declared group.

**Constraints.** Satisfies C1, C3, C4. C2 is satisfied by the door check being over the whole patch.

**The honest limit, and it matters.** Grouping does not close any proof by itself. It only makes closure
*possible*, because both sides of the relation are now under the caller's control at the same instant —
without a condition on the incoming pair, still nothing is proven. D6 is a companion to D5 or B3, never
an answer alone.

**What stops compiling.** Nothing; callers lose the freedom to patch arbitrary field subsets.

**What has to be built.** A grouping construct (new language surface, own design pass), a runtime check
that the patch matches a declared group, and a change to the `Update` API contract.

**What the author writes.** A group declaration per legal edit combination.

#### D7. The author lists, at the field, which rules the edit must respect

**Why the direction is nearly empty.** The compiler can compute that list, and an author-written list can
be wrong, so the compiler has to check it anyway. It buys one thing and only one: the check the proof
depends on becomes visible in the file rather than only in the certificate. That is a documentation
argument, not a proof argument, and if it is wanted it is cheaper as an editor or `inspect` feature than
as language.

### Group E — remove the ingress point

#### E1. Delete `editable` from the language

**What it does.** Every write goes through an event. Editing a field becomes declaring an event with one
argument and a row.

**Constraints.** Satisfies all four trivially. C2 becomes vacuous: ingress is one point, event arguments,
and the existing induction covers it unchanged.

**What stops compiling.** Every file using `editable`, at both the field-declaration and per-state
`modify` positions.

**What has to be built.** Removal work, mostly: `Update` / `InspectUpdate` lose their reason to exist or
become a dispatch to a synthesized event; `UpdateOutcome.FieldNotEditable` goes; the access-mode
composition rules shrink to `readonly` and `omit`; the write-site list loses a member. Documents across
the language, runtime and tooling areas.

**What the author writes.** An event declaration, a row and — where a rule needs it — a guard, per field
they used to edit.

#### E2. Desugar the edit into a compiler-synthesized event

**What it does.** Keep the keyword. Per editable field, per state where it is editable, the compiler
generates an event carrying one argument that inherits the field's modifiers, one row, and a `set`. The
synthesized handler is proven or refused exactly like a hand-written one.

**Constraints.** Satisfies C1, C3, C4. C2 is satisfied *in mechanism*: ingress validates the synthesized
argument against the field's modifiers, which is premise class (b), and anything relational must come
from a guard.

**Difference from E1.** No author-visible change until a relational rule needs a condition — at which
point the author needs a way to attach a guard to a synthesized row, which is D5 arrived at from the
other direction, and the same design pass.

**What stops compiling.** Nothing initially; then whatever B1 would refuse, refused with a message about
a synthesized handler, which is a diagnostic-provenance problem.

**What has to be built.** A desugaring stage before the proof engine; a rule that generated names cannot
collide with authored ones; diagnostic mapping back to the field declaration; a decision on whether the
synthesized events appear in `AvailableEvents` and in `inspect`, which is a visible API change.

**What the author writes.** Nothing, until a rule forces a condition.

#### E3. Edits produce a candidate version that an event must accept

**What it does.** An edit writes to a draft that is not a governed configuration. An explicit accepting
event — an ordinary handler with a guard — promotes it. The induction applies to the accepting event
unchanged.

**Constraints.** Satisfies all four.

**What it strains outside the four.** `Version` is defined as an immutable committed snapshot (want:163)
and "an invalid configuration never exists, even transiently" (`precept-language-spec.md:1974`). A draft
that rules do not hold over is a new category of entity state, and the design only works if the draft is
defined as sitting outside the entity. It also changes the runtime API shape: `Update` returns a draft
and a new operation accepts it.

**What stops compiling.** Nothing; every editable field needs an accepting event declared.

**What has to be built.** A draft tier in the runtime, new outcome types, new inspection variants, and
canon changes to the atomicity and version sections.

**What the author writes.** An acceptance event with a guard. For a single field that is E1 with extra
steps; for a caller staging several edits it is genuinely different, and it is the only design here that
lets a caller build up a configuration incrementally.

### Group F — closed directions, recorded so they are not re-derived

- **Clamp the incoming value to the admissible range instead of refusing.** The runtime would silently
  change the author's data and, worse, supply a premise the author never wrote — the thing want:179
  forbids in the compiler, done at runtime instead. It also destroys the caller's ability to know what
  was stored.
- **Warn instead of refuse.** Named as dilution rather than balance at want:189, in those words.
- **Let the runtime pick a rule to relax.** Not a design; a way of not having one.
- **Defer to a solver at runtime.** want:135 rules out a solver the compiler does not carry; a solver the
  *runtime* carries is the same thing moved.
- **Re-govern on the next operation** (evaluate the rules when the next event fires, not at the edit).
  The invalid configuration was committed and observable in between, so C4 fails, and it is want:199
  again with a delay.

---

## 4. Which designs combine

Several are strategies rather than positions, and the interesting answers are combinations:

| Building block | What it needs alongside it |
|---|---|
| A1 (evaluate at the write) | B4's condition (i), or one of C1'–C4', or it is unsound for computed-field reach |
| B5 (finite-domain exhaustion) | a fallback for continuous types — B1, D3, D4 or D5 |
| D3 (access-mode guard as premise) | a fallback where the guard cannot pin the other fields |
| D4 (declared direction) | a fallback for non-monotone appearances |
| D6 (patch groups) | D5 or B3 — grouping alone proves nothing |
| C4' (inline computed fields) | makes A1, B3, B4 sound over the whole space |
| A5 (load-time certificate check) | inherits under all of them; never a choice on its own |

---

## 5. Does any design satisfy all four constraints without straining one?

Yes — three, each over part of the space, and one composite over all of it.

**B5, D3 and D4 satisfy all four literally.** In each, the check performed at the door is the field's own
declared contract (plus, for D3, the access-mode guard that is already evaluated there), nothing
post-mutation is evaluated, and that door check is what makes the write site's proof close — which is
what C2 asks for in its own words. Each covers a different part of the space: B5 covers finite-domain
fields, D3 covers rules the access-mode guard can pin, D4 covers monotone appearances. None covers a
decimal field in a non-monotone relational rule with no useful state guard, which is the want example.

**The composite that covers everything without straining any of the four is B1 with B5, D3 and D4 as
widening strategies, and refusal everywhere else.** Its entire price is paid in refused definitions and
in what authors write instead — bounds, an access-mode guard, a direction modifier, or an event. That is
the same trade want:171 and want:189 already take, stated as a price rather than as a virtue: the want
document's own worked example does not compile as written, and making it compile means declaring a floor
on the repayment cap and a ceiling on the repayment, which say more about the business than the author
said.

**Everything else strains something.** A1, A2, A3 and B4 strain C1 — they evaluate rules against a
configuration that includes the mutation, and the narrowness of the rule list is the whole defence. B1,
B2, D1, D2, E1 and E2 strain C2 — they delete the ingress premise he named, on the strength of walk item
16, and canon at HEAD has already been written that way in eight places. B3 and C4' strain nothing in the
four but both rest on rewrites that walk item 13 makes each owe a written validity argument. D5, D6 and
E3 satisfy all four and cost new language surface that does not exist, each needing its own design pass.
C1'–C3' satisfy all four and pay in refused programs of a shape that is common rather than exotic.
