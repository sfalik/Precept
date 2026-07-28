# What discharges a rule's obligation when a field it mentions is edited?

**Prepared for Shane, 2026-07-27. This is section 6 of
[`phase-5/editable-write-cascade.md`](../phase-5/editable-write-cascade.md), where five independent
readings all agreed the obligation exists and disagreed entirely about what discharges it. It blocks the
disputed scope note now sitting in the spec at §0.7 and §3A.4, and phase 5's premise set.**

**Reading time: about six minutes — it carries four options rather than two.** Compile results are
reproduced at HEAD today and marked as such; they show what the current compiler does, never what it
should do. Every line number is at committed HEAD, not the working tree, which has uncommitted phase-2
edits to the spec. **The worked examples are mine, not yours**, for the reason in § 3.

---

## 1. The question in one sentence

A field is declared `editable`. A rule relates it to another field. Someone writes a value straight into
that field — a value that is perfectly legal for the field, and that makes the rule false. What is the
compiler entitled to point at and say *this is what holds the rule*?

## 2. What is already settled, so you are not re-answering it

- **The edit carries the obligation.** Walk item 35: asked plainly whether a rule must be preserved
  wherever a field it mentions is written, "including construction rows, state actions and editable-field
  writes", you answered yes.
- **It reaches through computed fields.** Walk item 8's activation route is yours ("one, clearly"); the
  computed route is the easier case of the same principle, and `precept-language-spec.md:1968` states the
  expansion independently — "with transitive expansion to the concrete stored fields they depend on".
- **Timing is not the difficulty, and no post-mutation pass returns.** An `Update` runs the patch and the
  recomputation and nothing else — no dispatch, no actions, no state change — and computed fields cannot
  form cycles. So the configuration it would commit can be worked out before anything is written, however
  long the derivation chain. Nothing below proposes a pass over the finished copy.
- **The cascade computation gets built either way** — from each editable field, the computed fields it
  reaches and the rules those touch. It does not separate the options.

## 3. Your own document does not settle this, in either direction

The example that has been used to argue this — `RepaymentCapPercent` inside a relational rule — is not
yours. `phase-1/want-document-provenance.md` traces `want:36` and `want:40` to `73f4db7d`, the
Copilot-co-authored amendment of 2026-07-19, and lists them among eleven lines it declares unsafe to
quote. Your original example declares exactly one editable field, `DailyWithdrawalLimit`, and **no rule
statement mentions it** — you say so yourself at `want:129`, "no *other* proof in the file consumes them".
So your example never exercises this case in either direction.

**What is left is two sentences of yours, fifty lines apart, pulling against each other.** `want:143` —
"For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the field …
so evaluating those rules at the ingress point is exactly the premise that closes the editable write
sites in the compiler's proof." Against `want:171`, "there is **no deferral**", and `want:199`, the
runtime "does **not** re-evaluate them against the post-mutation configuration."

Canon cannot break the tie, because canon is downstream of it. Every canonical sentence taking the narrow
side — `precept-language-spec.md:274`, `:1976`, `runtime-api.md:402` — was written on 2026-07-27 by the
phase-2 correction pass, and the sentence it replaced was the post-mutation sweep you removed. §3A.4 now
carries a note saying the scope is disputed; §0.7 at `:274` carries the identical narrowing **without**
the note, which wants fixing whichever way you rule.

## 4. What today's compiler does

Reproduced at HEAD, 2026-07-27. **This compiles with zero diagnostics:**

```precept
precept CardAccount

field CreditLimit as decimal nonnegative max 25000.0 default 1000.0 editable
field OutstandingBalance as decimal nonnegative default 0.0

rule OutstandingBalance <= CreditLimit because "The outstanding balance {OutstandingBalance} cannot exceed the credit limit {CreditLimit}"

event Spend(Amount as decimal positive)

on Spend when OutstandingBalance + Spend.Amount <= CreditLimit
    -> set OutstandingBalance = OutstandingBalance + Spend.Amount
```

`Spend` is proven — its guard is the post-state condition. But spend to an outstanding balance of
3000.00, then edit `CreditLimit` to 100.00. That value is legal for the field, the rule is now false, and
nothing refused it. The compiler produced **no obligation at all** for the edit: `soundness-and-coverage.md`
§ 3.2 enumerates the obligation-creation sites and has no row for the update patch. So this is not a
choice between two guarantees; today there is none on this route.

## 5. What the guarded access mode can actually see

`in <State> when <Guard> modify <Field> editable` already exists (`precept-language-spec.md:396` for the
keyword, `:1038`–`:1046` for the production, composition rule 4a at `:1075`). Nobody had checked what its
guard can see, and it decides how much of the problem option 1 solves with no new language.

**It is evaluated before the write.** `precept-language-spec.md:2016`: *"For `Update`, the patch's access
modes (§2.2) are enforced before any value is applied."* `runtime-api.md:400` gives the same ordering for
the shipped API — access mode check, then type validation, then ingress validation, then patch
application. The guard therefore reads the configuration as it stood before the patch, unconditionally.
Usable as a premise.

**It cannot mention the incoming value, and canon says so only indirectly.** The expression-scope table
at `:1350`–`:1360` has **no row for the access-mode guard at all** — a real gap that should get a row. Every
comparable non-event context in that table is "All field names". The only way in the language to name a
value supplied from outside is `EventName.ArgName`, and `:1384` restricts that to "transition rows, event
ensures, event handlers"; an access mode is none of those. There is no name for the value being patched
in, anywhere in the language. So the guard can pin the *other* operands of a rule. It can never say
anything about the value arriving.

**How solid that is.** `docs/runtime/evaluator.md:1326`–`:1341` describes access-mode enforcement as a
flat per-state dictionary lookup that never evaluates a guard at all, still in the retired `writable`
vocabulary. The guarded form is specified and type-checked but nothing has ever evaluated it. If option 1
leans on it, that is work, not a free inheritance.

---

## 6. The options

### Option 1 — the field's own declared contract, and nothing wider

The compiler proves each reachable rule from what the author declared — the edited field's modifiers, the
access-mode guard where one is written, and the rules that held before. Where it cannot, the definition
is refused. The engine at the write checks only the field's declared contract.

**What stops compiling.** Any definition where a field is `editable` and a rule relates it to another
field, unless the declarations entail the rule. `CardAccount` above is refused. So is this, which also
compiles clean today:

```precept
field BaseFee as decimal nonnegative max 100.0 default 10.0 editable
field HandlingFee as decimal nonnegative max 100.0 default 5.0 editable
field TotalFee as decimal nonnegative <- BaseFee + HandlingFee

rule TotalFee <= 50.0 because "The total shipping fee {TotalFee} cannot exceed 50.00"
```

**What the author writes instead** — three routes, all existing language. Bound the fields so the
declarations entail the rule (`max 25.0` on each fee closes the second example, and says more about the
business than the author said). Or guard the edit, which needs states:

```precept
field CreditLimit as decimal min 1000.0 max 25000.0 default 1000.0
field OutstandingBalance as decimal nonnegative default 0.0

rule OutstandingBalance <= CreditLimit because "…"

in Active when OutstandingBalance <= 1000.0 modify CreditLimit editable
```

That is provable: the guard pins `OutstandingBalance` at or below 1000.00 before the write, and
`CreditLimit`'s floor is 1000.00, so every value the field admits keeps the rule true. Or drop `editable`
and route the change through an event with a `when` guard — always available, costs one event.

**The refusal an author sees.** Under the phase-4 ruling this is the *proven unsafe* kind, not the
*unproven* kind: the compiler has an actual counterexample.

> `CreditLimit` is editable and `rule OutstandingBalance <= CreditLimit` mentions it. Editing
> `CreditLimit` can break that rule.
> Counterexample: `OutstandingBalance` = 3000.00, legal under the previous limit; an edit setting
> `CreditLimit` = 100.00 is legal for the field and makes the rule false.
> To close it: bound `OutstandingBalance` above and `CreditLimit` below so the bounds entail the rule; or
> guard the edit — `in <State> when <condition on OutstandingBalance> modify CreditLimit editable`; or
> remove `editable` and write `CreditLimit` through an event with a `when` guard.

**The cost, measured.** This shape is common in files people actually wrote: ten sample files name a
field-level `editable` field directly in a rule, `fee-schedule.precept` among them — two editable money
fields and `rule MinimumCharge <= BaseFee`. That is cost evidence, not a coverage or correctness claim,
and it undercounts, missing the computed route and per-state `modify … editable`.

**One thing it does not buy that has been claimed for it.** Finite-domain exhaustion — check a boolean or
small enum one value at a time — is listed in the design space as a widening strategy. It proves only
rules already true for every configuration the declarations admit, because it never uses the fact that
the rule held before the edit; and on walk item 8's activation route it gives nothing at all, since the
case it must prove is the one where the rule has just been switched on. Do not price option 1 as though
exhaustion recovers much.

### Option 2 — the engine evaluates the reachable rules at the write, and the compiler cites that

Before anything is applied, the engine evaluates the rules the edit could falsify, with the derivation
chain substituted so computed-mediated rules are covered, and refuses the whole patch if any is false.
The compiler treats that evaluation as the premise closing the edit write site; the certificate records
it.

**What stops compiling.** Nothing. Both files above compile unchanged and the author writes nothing. That
is the appeal, and it is real. The caller sees the rule's own message at runtime instead:

> Update refused. The outstanding balance 3000.00 cannot exceed the credit limit 100.00.

**What you give up.** The compiler's remaining obligation becomes "the check passed, and the pre-state
rules held, and the field's modifiers hold, therefore the rule holds at commit" — which is the identity.
It closes for every rule the check evaluates, linear and non-linear alike, because multiplying two
numbers you already hold is not hard. So no definition is ever refused on account of an edit, and on this
route the compiler has proven nothing about any relational rule: **what holds the rule is the check.**
`want:171`'s no-deferral does not survive here; `want:173`'s uniform guarantee survives only in the
author-facing sense, with two mechanisms underneath; and `philosophy.md:37` — "Nothing re-evaluates the
rules after the change, and nothing needs to: the invalid configuration is not produced and then
rejected. It is never produced." — needs rewording, which is a philosophy edit and separately yours to
authorise.

One real difference from the sweep you removed survives: the compiler still owes a genuine proof that the
rule list is *complete*, that no rule the edit could falsify was left out. The sweep never needed that,
because it evaluated everything.

**What has to be built.** The compiled definition carries, per editable field, the reachable rules
rewritten into predicates over stored fields; the engine evaluates them at phase 1; the refusal outcome
widens, because `precept-language-spec.md:1925` defines the ingress failure as a value that "violates a
constraint declared on the slot it enters" and refusing a patch because a rule about some *other* field
came out false is not that; and the certificate records both the check set and the completeness proof.

### Option 3 — a field a relational rule mentions may not be `editable`

The refusal lands on the declaration rather than on the rule. No analysis — a syntactic test.

**What stops compiling.** Everything option 1 refuses, plus everything option 1 would have proven: the
guarded example above is refused under this option, and `fee-schedule.precept` cannot declare either
field `editable`. Almost nothing to build.

> `CreditLimit` cannot be declared `editable`: `rule OutstandingBalance <= CreditLimit` relates it to
> another field, and a direct edit has no guard that could carry the proof. Write `CreditLimit` through
> an event, where a `when` guard can.

The difference between its accepted set and option 1's is exactly what option 1's analysis is worth.

### Option 4 — a declared condition on the incoming value. **New language surface — this stops here.**

The premise class missing at an edit is the guard, because there is nothing to guard. A way to declare a
condition the incoming value must satisfy, over the pre-state and that value, closes every case: the
author writes the post-state condition directly and the proof closes by substitution against what they
wrote, exactly as a transition guard does. It satisfies `want:143`, `want:171` and `want:199` at once.

**No syntax is proposed here and none should be.** It is a new construct — grammar, catalog entry,
parser, name binder, type checker, a premise class in the proof engine, certificate steps, diagnostics,
language server, MCP vocabulary and the documents — and it opens questions a design pass owns: how the
incoming value is named, whether the condition attaches to the field, to the `modify` declaration or to a
state, and how it composes with the access-mode guard. **If you want this, it routes to `/design` as its
own pass, and one of options 1–3 stands in the meantime.**

A cheaper relative: a modifier saying an edited value may only move one way — a limit that can only rise.
That closes every rule in which the field appears monotonically, from the pre-state rules alone. A
modifier rather than a construct, so much smaller, but still new surface and still its own design pass.

---

## 7. What is not being asked

- **Not whether the edit carries the obligation.** Walk item 35 settled that: it does.
- **Not whether a post-mutation sweep returns.** No option proposes one.
- **Not whether a refusal explains itself.** It does — `want:179`, and the phase-4 ruling that the author
  is told which kind of refusal they have.
- **Not whether the cascade computation gets built.** It does, under every option.
- **Not `set` inside an event handler.** That route has a guard and the induction covers it already.

## 8. What happens after you answer

Phase 2 removes the disputed note from §3A.4 and makes §0.7 agree with the ruling — right now those two
say the same thing and only one admits it is disputed. Phase 5 adds the edit write site to the
obligation-creation list, which has no row for it today, and takes the discharge rule into its premise
set. If you pick option 4, phase 5 builds against option 1 or 3 and the new surface goes to `/design`
separately.

---

## Ruled — *(awaiting your answer)*
