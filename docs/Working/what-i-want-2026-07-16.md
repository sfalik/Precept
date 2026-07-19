# What I Want

**Status**: Draft — 2026-07-16
**Author**: Shane

## Scope

What Precept should *be* is already defined at a high level in `docs/philosophy.md` — this document does not redo that. What it clears up is **how Precept achieves it**: specifically, how far the compiler should go, and where the line sits between **compile-time proof** and **runtime governance** — the proof engine especially, but the whole compiler too.

The goal here is a clear description of what I would want **in an ideal world**. The task after that will be to understand whether that ideal is feasible for one person with a team of agents to build. If not, we will find a balance — but the balancing comes later, and it is done against a stated ideal, not instead of one. And whatever balance we strike, **we cannot dilute the value proposition in the process of practicality**.

## The compiler's promise

In an ideal world, the compiler **proves or rejects all business rules**. That is the strong value proposition.

What "prove" means here is **proof by induction over reachable configurations** — not proof that each rule is a static mathematical truth (a rule relating two runtime-supplied values has no static truth to prove). The compiler proves:

- **Base case.** The default configuration satisfies every rule, and every initial event *establishes* every rule, provable from the constraints declared on its args.
- **Inductive step.** Every handler *preserves* every rule, provable from four premise classes: (a) field modifiers, (b) arg constraints, (c) the handler's guard, and (d) all rules holding in the pre-state.
- **Symmetric obligations.** A rule's obligations attach to every write site of *every* field the rule mentions — not just the field the author was thinking about.

**Modifiers are sugar for rules.** `nonnegative` on `OverdraftLimit` is a compact spelling of `rule OverdraftLimit >= 0`; a modifier on an arg is a compact spelling of a precondition on its event. There is one constraint mechanism underneath — the premise classes above are spellings, not separate systems.

**Rejection** means the proof for some handler cannot close, and the diagnostic names the missing premise: bound the arg, or add a guard. **Acceptance** means: given everything the author declared, no reachable operation can violate any rule — the rules are proven theorems.

The premises are also what the runtime enforces — governance, not deferred proof, because the compiler has proven those checks *sufficient*.

### Worked example

```precept
precept BankAccountInvariant

field OverdraftLimit as decimal nonnegative
field Balance as decimal
field MonthlyRepayment as decimal optional nonnegative
field RepaymentCapPercent as decimal default 0.25 positive max 1.0 editable
field DailyWithdrawalLimit as decimal default 500.0 positive max 10000.0

rule Balance >= -OverdraftLimit because "Balance cannot go below the account's overdraft limit"
rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set because "A planned repayment may not exceed the capped fraction of the overdraft facility"

state Active initial
state Frozen
state Closed terminal

in Active modify DailyWithdrawalLimit editable

event OpenAccount(OpeningBalance as decimal min 0.0, OverdraftLimit as decimal nonnegative) initial
on OpenAccount
  -> set Balance = OpenAccount.OpeningBalance
  -> set OverdraftLimit = OpenAccount.OverdraftLimit

event Deposit(Amount as decimal nonnegative)
from Active on Deposit
  -> set Balance = Balance + Deposit.Amount
  -> no transition

event Withdraw(Amount as decimal)
from Active on Withdraw when Balance - Withdraw.Amount >= -OverdraftLimit and Withdraw.Amount <= DailyWithdrawalLimit
  -> set Balance = Balance - Withdraw.Amount
  -> no transition
from Active on Withdraw when Withdraw.Amount > DailyWithdrawalLimit
  -> reject "Withdrawal of {Withdraw.Amount} exceeds the daily limit of {DailyWithdrawalLimit}"
from Active on Withdraw
  -> reject "Withdrawing {Withdraw.Amount} would exceed the overdraft limit (balance {Balance}, limit {OverdraftLimit})"

event PlanRepayment(Months as integer positive)
from Active on PlanRepayment when Balance < 0.0 and -Balance / PlanRepayment.Months <= OverdraftLimit * RepaymentCapPercent
  -> set MonthlyRepayment = -Balance / PlanRepayment.Months
  -> no transition
from Active on PlanRepayment when Balance < 0.0
  -> reject "A monthly repayment of {-Balance / PlanRepayment.Months} would exceed the capped fraction ({RepaymentCapPercent}) of the overdraft facility (limit {OverdraftLimit})"

event ReduceLimit(NewLimit as decimal nonnegative)
from Active on ReduceLimit when Balance >= -ReduceLimit.NewLimit and MonthlyRepayment <= ReduceLimit.NewLimit * RepaymentCapPercent
  -> set OverdraftLimit = ReduceLimit.NewLimit
  -> no transition
from Active on ReduceLimit when Balance < -ReduceLimit.NewLimit
  -> reject "Cannot reduce the overdraft limit to {ReduceLimit.NewLimit} while the balance is {Balance}"
from Active on ReduceLimit
  -> reject "Cannot reduce the overdraft limit to {ReduceLimit.NewLimit}: a planned repayment of {MonthlyRepayment} would exceed the capped fraction ({RepaymentCapPercent}) of the reduced facility"

event Freeze
from Active on Freeze
  -> transition Frozen

event Unfreeze
from Frozen on Unfreeze
  -> transition Active

event CloseAccount
from Active on CloseAccount when Balance == 0.0
  -> transition Closed
from Active on CloseAccount
  -> reject "Cannot close the account while the balance is {Balance} - settle to exactly zero first"
```

Every handler is provable, and each one needs a different premise class:

- **`OpenAccount`** — arg constraints alone: `OpeningBalance min 0.0` and `OverdraftLimit nonnegative` give `Balance ≥ 0 ≥ −OverdraftLimit`. The base case.
- **`Deposit`** — the inductive hypothesis: `Balance + Amount` with `Amount ≥ 0` preserves the rule *only if the rule held before the event*. No guard or arg bound alone gets there.
- **`Withdraw`** — the guard is the proof: `Balance − Amount >= −OverdraftLimit` is literally the post-state rule. The fallback row rejects with an explanation and writes nothing.
- **`ReduceLimit`** — the symmetric write site. The rule is relational, so lowering `OverdraftLimit` threatens it even though `Balance` is untouched — a case authors naturally miss, because they think of the rule as "about Balance." Without the guard, the ideal compiler rejects this handler ("cannot prove `Balance >= -NewLimit`; add a guard"); with it, the proof closes by substitution.
- **`PlanRepayment`** — the fault family. Business rules are not the only obligations: faults (division by zero, overflow, out-of-range) get the identical premise-and-certificate treatment. Here the divide-by-zero obligation is closed by the arg constraint (`Months positive` — the divisor is provably non-zero), and the `nonnegative` bound on `MonthlyRepayment` is closed by the guard (`Balance < 0.0` makes `-Balance` positive). At runtime there is no zero-check anywhere — the ingress validation of the arg is what makes the division safe, and the certificate records that dependency.
- **`RepaymentCapPercent`** — the non-linear obligation. The added rule `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` multiplies two fields, so no single write closes it by substitution the way `Withdraw`'s guard closes the linear rule. `PlanRepayment` sets `MonthlyRepayment` to a *quotient* and the obligation compares it against a *product* — four free variables and nothing to cancel — so premise class (c) has to carry it: the guard restates the post-state condition verbatim (`-Balance / PlanRepayment.Months <= OverdraftLimit * RepaymentCapPercent`) and the proof closes against what the author wrote, not against algebra the compiler won't attempt. And because the rule mentions `OverdraftLimit`, `ReduceLimit` inherits the same obligation from the other side — lowering the limit shrinks the product even though `MonthlyRepayment` is untouched, so it too must restate the condition (`MonthlyRepayment <= ReduceLimit.NewLimit * RepaymentCapPercent`) or be rejected. This is the symmetric write site again, but non-linear: substitution alone never closes it, and the ideal compiler rejects any unguarded non-linear rule rather than reach for a solver it does not carry.
- **`DailyWithdrawalLimit`** — the remaining premise sources. Its `default 500.0` is a genuine business value (accounts start with it; no constructor arg sets it), so the base case has a default to evaluate: `500.0` must satisfy `positive` and `max 10000.0`. And it is the sample's editable-field ingress: `in Active modify … editable` opens that ingress only while `Active` — writes are validated against the field's modifier-rules at the ingress point, and the editing window closes structurally the moment the account freezes or closes.

### What must not compile

The example is accepted *because of* its declared premises — so the rejection side of the promise is enumerable by construction: delete a premise, and the proof that depended on it must fail to close. Each row below is a one-line mutation of the example above, and each **must be a compile-time rejection** with a teachable message naming the missing premise:

| Mutation | What the compiler must say |
|---|---|
| Delete `Withdraw`'s guard | Cannot prove `Balance >= -OverdraftLimit` after `set Balance = Balance - Withdraw.Amount` — `Amount` is unbounded; add a guard or bound the arg |
| Delete `ReduceLimit`'s guard | Cannot prove `Balance >= -NewLimit` — the rule mentions `OverdraftLimit`, so this write site carries the obligation too |
| Remove `nonnegative` from `Deposit`'s `Amount` | Inductive step fails: `Balance + Amount` with a possibly-negative `Amount` can breach the rule |
| Remove `min 0.0` from `OpeningBalance` | Base case fails: the rule is not established at construction — an opening balance below `-OverdraftLimit` is admissible |
| Remove `nonnegative` from `OpenAccount`'s `OverdraftLimit` arg | The write to `OverdraftLimit` cannot satisfy the field's `nonnegative`, and a negative limit breaks the rule's establishment |
| Remove `positive` from `PlanRepayment`'s `Months` | Divisor can be zero — the division is unsafe |
| Remove `PlanRepayment`'s guard (`Balance < 0.0`) | Cannot prove `MonthlyRepayment` `nonnegative`: `-Balance` can be negative |
| Remove `PlanRepayment`'s cap conjunct (`-Balance / Months <= OverdraftLimit * RepaymentCapPercent`) | Cannot prove `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` after `set MonthlyRepayment = -Balance / Months` — the quotient is unbounded above; restate the post-state condition in the guard |
| Remove `ReduceLimit`'s cap conjunct (`MonthlyRepayment <= NewLimit * RepaymentCapPercent`) | Cannot prove `MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent` — the rule mentions `OverdraftLimit`, so lowering it carries the obligation even though `MonthlyRepayment` is untouched; restate it in the guard |
| Guard `PlanRepayment` on `Balance < 0.0` alone, dropping the restating conjunct | Cannot prove the cap rule: the product is non-linear, so substitution can't close it and no post-mutation sweep or solver stands behind it — the guard must state the post-state condition directly, or the rule must be linearized |
| Change the default to `20000.0` | Default value violates `max 10000.0` — the base case has a counterexample |
| Add `state Suspended` with no inbound row | Unreachable state |
| Delete the `Unfreeze` rows | `Frozen` is a dead end — enterable, not exitable, not marked `terminal` |
| Change `CloseAccount`'s guard to `Balance < -OverdraftLimit` | Dead row: the guard contradicts the rule, so it can never be true — and `Closed` becomes unreachable in consequence |

One deliberate omission: there is no deletion row for `DailyWithdrawalLimit`'s modifiers. They *are* proven unviolable — the default is checked statically, the editable-field ingress evaluates them on every write (that ingress check is the declared premise, and the certificate marks it load-bearing), and no other write path exists. But no *other* proof in the file consumes them: `Withdraw`'s guard reads the field's live value, not its bounds. So deleting them breaks no proof — it just weakens the specification to an unconstrained cap, which is the author saying less, not the compiler proving less. The asymmetry in miniature: deleting a consumed premise makes a file unprovable; deleting a rule nobody consumes merely makes it a weaker spec.

This table is the promise in operational form: an accepted file demonstrates nothing by itself — the compiler's power is visible only in what it refuses. Every premise class in the example has its deletion row here; a compiler that accepts any of these mutations is not the compiler this document asks for.

This is also where the feasibility question will concentrate: the practicality of this proof is exactly what the follow-on study has to establish.

On non-linear rules I want the line drawn in the open. A relational rule whose post-state condition is non-linear — a product or quotient of fields, like the repayment cap above — is proven only when a guard or arg constraint makes that condition directly checkable, so the proof closes against what the author wrote. Where no such premise exists, the compiler rejects the rule with a teachable message and names the guard that would close it. It is never governed by a post-mutation sweep, and never deferred to a solver the compiler doesn't carry. This is the same discipline as everywhere else in this document — prove from a declared premise or reject — I'm only saying it out loud for the non-linear case, because substitution closes the linear rules so quietly that it could leave the impression the harder ones come along for free. They do not: the author guards them, linearizes them, or the compiler refuses them.

The language was designed with this goal: simple, not Turing-complete, no loops, no functions. It is not a general-purpose programming language, and I want to leverage that simplicity to provide strong proof at compile time. Whether the current surface actually delivers tractable proof everywhere is a feasibility question — where it doesn't, the surface is the negotiable part.

At ingress, I expect the compiler to **require the author to extend constraints to the entry points**, so that ingress validation stays simple. I believe this forces authors to be more explicit — and that leads to better precepts.

Ingress is exactly two points: **event args** (including on initial/construction events) and **editable-field writes**. Restore/rehydration is *not* ingress — data coming back from persistence is trusted and unchecked, because it was governed when it was written. (Post-MVP, the restore path itself expands to handle schema evolution — data written under an older definition.)

What ingress validation means: the applicable **rules** are evaluated — however they were spelled, as modifiers or as rule statements. For an arg, that's its modifier-rules; anything relational an event needs lives in its guards. For an editable-field write, it's the field's modifier-rules *and* every rule that mentions the field — an edit is an arbitrary incoming value that no static proof can cover, so evaluating those rules at the ingress point is exactly the premise that closes the editable write sites in the compiler's proof.

This work largely lands in the proof engine, but not only there: the graph analyzer also plays a key role, and I'm open to additional compiler stages and techniques if needed.

## Structural integrity

The proof engine's half of the promise governs data; the **graph analyzer** governs shape. Per the philosophy, structural defects are compile-time impossibilities:

- **Every state is reachable.** In the example: `Frozen` via `Freeze`, `Closed` via `CloseAccount`. A state no path reaches is rejected.
- **No dead ends.** Every non-terminal state has an exit — `Frozen` exits via `Unfreeze`. A state you can enter but never leave is rejected, unless the author deliberately marked it `terminal` (`Closed`): an end on purpose is a declaration, not a defect.
- **Absence of a row is the enforcement.** Depositing into a frozen account is impossible because no transition row exists for `Deposit` in `Frozen` — not because a runtime check refuses it. There is nothing to bypass. This is prevention-not-detection in its purest form.
- **Inapplicability vs. refusal.** The same principle applies at guard level: `PlanRepayment` offers no row on a healthy balance, so there the event is simply inapplicable (Unmatched — the operation is not offered), while `Withdraw` over the limit gets an explained `reject`. Absence expresses *inapplicability*; `reject` is reserved for *resolvable refusals*. Explanations are for things the user can act on, not for things that simply don't apply.
- **Dead rows are structural defects too.** A row whose guard can never be true given the declared constraints is unreachable — and whatever state it leads to may be unreachable in consequence. The ideal compiler rejects it with an explanation (e.g., a guard comparing a 0-to-1 ratio against `75` instead of `0.75` can never fire — and the total-loss path it guards silently never happens).

## The runtime's role

Runtime governance is more than just validating ingress against declared constraints. It provides:

- the **inspect** mechanism — reasoning about an event before it fires
- respect for the business process the precept defines
- strict immutable versioning and write guarantees (as defined in `docs/runtime/runtime-api.md`: `Version` is an immutable snapshot; every operation returns a new `Version`, never mutating its input)

But the runtime **can be lighter, because the compiler's work is complete** — it should trust that the compiler did its job, a trust it establishes by verification, once, at load (see Certificates). "Lighter" means concretely: **fault-prevention checks are omitted at evaluation time**. No divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just computes. What runs at runtime are the declared premises (ingress validation of args and editable fields, guards) that the compiler's proofs depend on, and nothing beyond them. The worked example's `PlanRepayment` division is exactly this: no zero-check anywhere, because `Months positive` was validated at ingress.

And the real advantage is not runtime economy: **the compiler finds problems at the time when it's cheapest to fix them**.

## No deferral

In an ideal world there is **no deferral**. A business rule whose enforcement the compiler cannot prove complete is rejected — never handed to the runtime on the compiler's own initiative. The runtime keeps the governance duties described above (not an exhaustive list); what it never does is pick up proof work the compiler couldn't finish.

I want Precept's guarantees to be **easy for authors to understand and trust** — this is a big part of Precept's value proposition. Authors should not have to understand the difference between things checked at compile time vs things governed at runtime. To be precise: the **guarantee is uniform** — a rule holds, period; there is no author-facing distinction between "compile-time-checked rule" and "runtime-governed rule," no tiers of trustworthiness to reason about. The mechanisms differ and are visible (diagnostics teach them, refusals reveal them) — what the author is spared is not seeing the machinery, but ever having to ask "how strongly is this rule held?"

## Clear explanations

We have always strived for a strong developer experience, and **clear explanations are critical** to it — a rejection is not just a verdict. This holds on both sides of the boundary: compile-time rejections and runtime refusals alike must explain themselves clearly.

When a proof cannot close, the compiler **suggests the missing premise with a teachable message**: it computes the guard or arg constraint that would close the proof and shows it — "cannot prove `Balance >= -NewLimit` after this write; adding `when Balance >= -ReduceLimit.NewLimit` would make it provable" — and explains *why* the premise is needed, so the author learns the model rather than just obeying the tool. It never inserts the premise itself. The author writes it, deliberately: a premise the author never wrote is deferral relabeled as authorship.

## Expressibility trades for proof

We have already shrunk the language once. Whether prove-or-reject is actually feasible is exactly the question this document sets up — but for now we are thinking ideal world.

In that ideal world, I'm happy to **trade off some expressibility to gain proof**, as long as we are still providing a valuable tool that fits the kinds of business problems we intend to solve.

That set of business problems is not pinned down, and I expect it will continually expand as we add more features and functionality.

The criterion that separates balance from dilution: **the guarantee is non-negotiable; the surface is the negotiable part.** Shrinking the language surface is balance — fewer constructs, but everything that compiles is fully proven. Weakening the guarantee is dilution — constructs that compile without their obligations proven, checks silently deferred, warnings where there should be errors. The guarantee must be exactly that: a guarantee. Evaluating how much expressibility the proven surface can retain is the follow-on work.

## The proof boundary is the single precept (for MVP)

For now, the promise stops at the edge of a single precept, and cross-entity data is out of scope.

After the MVP, I envision adding the capability for one precept to invoke an event on another (message passing), and **saga precepts** that govern across multiple precepts. That is not MVP scope.

## Certificates — lighter runtime, with confidence

Once the compiler has proven every rule, the runtime does **not** re-evaluate them against the post-mutation configuration — but it does not blindly trust the compiler either. Alongside the compiled definition, the compiler emits a **certificate**: a checkable record of the proof — per obligation, the theorem, the premises used (which guard, which arg constraint, which pre-state rule), and the derivation.

One small checker component, two call sites:

- **A compiler stage** — audits the proof engine's work at authoring time, catching proof-engine bugs when they are cheapest to fix.
- **A load-time gate in the runtime** — the same checker, run once when a definition is loaded. The runtime refuses to govern under a definition whose certificate does not verify: no unproven, tampered, or stale definition can ever govern. The guarantee is rooted in verification the runtime performed itself, not in provenance it assumes.

This is the best of both worlds: **a lighter runtime, with confidence**. The hard work — figuring out *how* to prove each obligation — happens once, at compile time. The certificate records the finished proof step by step, and the checker simply walks those steps and confirms each one. Every step is re-verified; the figuring-out is never repeated. That's why checking stays fast and the checker stays small.

Two further things fall out:

- The certificate tells the runtime exactly which checks are load-bearing: the premise list **is** the runtime's validation surface — which guards and constraints must run and can never be skipped — derived from the proof, never maintained in parallel.
- Inspect can cite provenance, not just verdicts: "no check needed here — proven from these premises," and refusals can cite the rule's `because` as the reason a guard exists.

Precedent: proof-carrying code (Necula & Lee), and the small-trusted-kernel discipline of proof assistants — the thing you must trust stays tiny and auditable even when the prover is large.

**Philosophy flag (not resolved here):** `docs/philosophy.md` currently describes Fire as "evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds." Under this want, that sentence needs a deliberate, owner-approved rewording — the constraint evaluation moves to proof-plus-premise-checks, verified by certificate.

## Open questions

- **Time-referencing rules.** A rule like `ExpiryDate > today` can become false with no event in flight — nothing writes `today`, there is no ingress point for it, and the inductive model has no step where pure passage of time fits. Do rules that reference the current moment exist at all, or does time belong only in guards (evaluated at event time, like any other premise)? Deliberately left open.
- **The promise across precepts.** Whether and how prove-or-reject and no-deferral extend across precepts (message passing, saga precepts) is undefined; the MVP proof boundary is the single precept. Deliberately left open until that capability is designed.

