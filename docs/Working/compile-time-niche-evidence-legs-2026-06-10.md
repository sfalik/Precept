---
status: Evidence appendix — scenario / cost / external / UX-unlock / demand legs (read with the decision packet)
authored: 2026-06-10
topic: The five evidence legs behind the niche decision
companion: compile-time-niche-decision-packet-2026-06-10.md
---

# Compile-Time Niche Evidence — The Five Legs

## Leg 1 — Realistic bug scenarios

### [compile-time-clearly-wins] Cross-field relational contradiction (mutual bound cycle) (Wholesale/retail pricing configuration)

**Story:** A pricing analyst maintains a price sheet. Policy: retail must clear wholesale by $1, wholesale must clear the supplier floor by $1. A copy-paste error leaves WholesalePrice's min referencing RetailPrice instead of SupplierFloor — a mutual cycle (each price must exceed the other plus 1). Both fields are optional, so the sheet works perfectly while only one price is filled. Weeks later, the first analyst to enter BOTH prices hits a permanent wall: every value pair violates one bound, and the two refusal messages each demand a bound the other rule forbids. The pricing feature for any fully-specified product is broken forever, and the dual contradicting refusals actively mislead the person trying to fix the data instead of the definition.

**The buggy precept:**

```
precept WholesalePriceSheet

field SupplierFloor as integer default 0 nonnegative editable
field WholesalePrice as integer optional min RetailPrice + 1 editable   # BUG: meant min SupplierFloor + 1
field RetailPrice as integer optional min WholesalePrice + 1 editable
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. The only proof obligation emitted is the SupplierFloor default check ('Proved'). The unsatisfiable cycle generates no diagnostic and no obligation.
- **Under governance+inspector:** Governance never sees a definition problem — it refuses each write correctly, per operation, once both fields are set. Until that day, everything commits. The end user who first fills both prices gets two mutually contradicting refusals and no way to satisfy either; support reads two correct-looking rules. The live inspector shows the refusal the moment an author TRIES setting both values in one scenario — but an author who tests fields one at a time (the natural flow for optional fields) sees all green. The defect requires the specific two-fields-both-set scenario, and nothing prompts the author to construct it.
- **Compile-time catch:** Bounded relational closure over declared cross-field constraints (the already-locked relational-rules design extends spec §0.6 item 2's single-pass narrowing): WholesalePrice ≥ RetailPrice+1 ∧ RetailPrice ≥ WholesalePrice+1 closes to an empty set in one transitive step. Solver-free, deterministic, attributable. Diagnostic for a domain expert: 'WholesalePrice must be at least RetailPrice + 1, and RetailPrice must be at least WholesalePrice + 1 — no pair of prices can satisfy both. Once both fields are set, every edit will be refused. One of these bounds is likely inverted.' This is a universal claim (no value pair exists, ever) that per-scenario inspection can only sample.

### [compile-time-clearly-wins] Clock-decaying unconditional rule (entity bricked by time alone) (Workplace-safety incident compliance)

**Story:** A compliance officer encodes the 72-hour processing SLA exactly as the temporal-type-system doc's own example suggests: rule now() - IncidentAt <= '72 hours'. It reads like policy. For 72 hours per incident it IS policy. At hour 73 on any unresolved incident, EVERY operation refuses — including Resolve, the very escalation the because-text demands — because the constraint sweep re-checks all rules on every operation. The entity is permanently bricked by the calendar: no operation caused it, no operation can cure it (IncidentAt is not editable), and after a holiday weekend the whole open-incident queue strands at once. Worse, between operations the persisted data silently violates its own declared rule with no record.

**The buggy precept:**

```
precept IncidentReport

field IncidentAt as instant optional
field Summary as string optional
field ResolutionNote as string optional

rule now() - IncidentAt <= '72 hours' when IncidentAt is set because "Incident reports must be processed within 72 hours of the incident — escalate stale reports to the compliance desk"

state Open initial
state Resolved terminal

in Open modify Summary editable

event Report(OccurredAt as instant, Details as string notempty) initial
event Resolve(Note as string notempty)

on Report
    -> set IncidentAt = Report.OccurredAt
    -> set Summary = Report.Details

from Open on Resolve
    -> set ResolutionNote = Resolve.Note
    -> transition Resolved
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. The presence obligation for optional IncidentAt is 'Proved' via the guard; the monotone time decay generates nothing.
- **Under governance+inspector:** Option 1 fails structurally here, twice. Governance: nothing happens until hour 73, then every operation on the affected entity returns ConstraintsFailed with the 72-hour because-text — including the Resolve that would close it out. The refusal is correct and useless: no caller action can ever succeed. Inspector: it evaluates at the real wall clock, so during authoring (incident just created in the scenario) every simulation passes; there is no time-travel control in the designed paradigm, so the author literally cannot ask 'what happens to this report next week.' The defect is invisible to both halves of Option 1 until production entities age into it.
- **Compile-time catch:** Syntactic monotonicity classification over the closed temporal operator set — no solver: now() - IncidentAt <= D can only flip from satisfied to violated as the clock advances (the anchor field IncidentAt has no re-arming write site after construction). Flag decaying predicates in rules whose violated state blocks every operation. Diagnostic: 'This rule can only go from satisfied to violated as time passes, and once violated no operation on the report can ever succeed — including Resolve. Scope it to specific states, or model the deadline as an escalation transition instead of a standing rule.' The proper fix (a declared deadline/expiry construct) is a language-surface unlock — flagged, routes to /design.

### [compile-time-clearly-wins] Guard-locked strand (exit guard depends on a fact no reachable operation can establish) (Property insurance claims)

**Story:** An insurer models claims: open, adjust, settle. Settlement policy: 'only with an adjuster of record' — so the Settle row is guarded 'when AdjusterId is set'. The author meant to add an AssignAdjuster event and never did; AdjusterId has no write site and no editable window anywhere. Every claim that enters UnderAdjustment is permanently trapped: Settle returns Unmatched forever — not a refusal, not an error, just 'no row matched.' Claims pile up for weeks; claimants call; support sees nothing wrong because nothing ever FAILS. This is precisely the stranded-workflow failure the philosophy promises is structurally impossible, passing today's structural dead-end analysis because the edge exists on paper.

**The buggy precept:**

```
precept InsuranceClaim

field ClaimantName as string notempty maxlength 200
field ClaimAmount as money in 'USD' default '0.00 USD' nonnegative
field AdjusterId as string optional maxlength 100
field PayoutAmount as money in 'USD' default '0.00 USD' nonnegative

state Submitted initial
state UnderAdjustment
state Settled terminal

in UnderAdjustment modify PayoutAmount editable

event Open(Claimant as string notempty maxlength 200, Amount as money in 'USD' positive) initial
event BeginAdjustment
event Settle

on Open
    -> set ClaimantName = Open.Claimant
    -> set ClaimAmount = Open.Amount

from Submitted on BeginAdjustment -> transition UnderAdjustment

from UnderAdjustment on Settle when AdjusterId is set
    -> transition Settled
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:1 — only PRE0158 warning 'Field AdjusterId has no write site — it can only hold its declared default or remain unset.' The compiler half-knows the fact but draws no consequence: nothing says the Settle row can never fire, that Settled is unreachable in practice, or that UnderAdjustment is a trap. An author can plausibly read PRE0158 as 'it's optional, that's fine.'
- **Under governance+inspector:** Governance NEVER fires: Unmatched is a legitimate outcome ('data condition the caller can address'), indistinguishable from 'not yet.' No refusal, no because-text, no signal. The inspector shows Settle as blocked with the guard reason 'AdjusterId is set' — which reads as 'assign an adjuster first,' exactly what a sane author concludes; nothing distinguishes 'not yet' from 'never possible.' The author must independently realize there IS no assignment affordance — the inspector cannot make the universal claim 'no reachable operation sequence ever establishes this fact.'
- **Compile-time catch:** Compose facts the compiler already has: per-state writable/attainable-fact sets (access modes + row/hook assignments along reachable paths, the PRE0158 write-site analysis made path-aware) folded against exit-guard requirements. Pure graph + set reasoning, no intervals needed for the 'is set' case. Diagnostic: 'The only exit from UnderAdjustment requires AdjusterId to be set, but no event, edit window, or hook can set AdjusterId before or in UnderAdjustment. Every claim that reaches this state is permanently stuck — add an assignment event or an editable window.' Sharpened PRE0158 with the consequence attached.

### [compile-time-clearly-wins] Dead reject row via first-match shadowing (authored prohibition silently inert) (Export-control / sanctions compliance)

**Story:** A logistics firm's shipment precept has had a general Ship row since launch. After a sanctions update, the compliance officer adds the prohibition — 'shipments to embargoed destinations must reject' — and naturally appends it at the bottom of the Ship rows. Routing is first-match: the unconditional general row above always wins, so the authored prohibition NEVER fires. Embargoed shipments dispatch successfully, as clean governed successes, for months. Reading the file suggests the control exists — an auditor sees the reject row, the author believes it is enforced. The failure is a wrong SUCCESS, so there is no refusal, no fault, nothing to investigate until the regulator calls.

**The buggy precept:**

```
precept ExportShipment

field Destination as string notempty maxlength 100
field Carrier as string optional

state Quoted initial
state Shipped terminal
state Cancelled terminal

event Create(Dest as string notempty maxlength 100) initial
event Ship(CarrierName as string notempty)
event Cancel

on Create -> set Destination = Create.Dest

# General shipping row — original flow.
from Quoted on Ship
    -> set Carrier = Ship.CarrierName
    -> transition Shipped

# Embargo prohibition — appended after the sanctions update. DEAD: row above always matches first.
from Quoted on Ship when Destination == "RestrictedZone"
    -> reject "Shipments to embargoed destinations are prohibited under the export-control policy"

from Quoted on Cancel -> transition Cancelled
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. An unconditional row preceding both a guarded row and a reject row for the same (state, event) draws no diagnostic of any kind.
- **Under governance+inspector:** Governance never objects — the embargoed shipment is a valid, governed, committed success under the rules as actually ordered. The inspector helps only if the author (a) constructs a RestrictedZone scenario after adding the row AND (b) notices that the matched row was the general one rather than the reject — the outcome 'Shipped' looks plausible at a glance. The corpus idiom (specific row first, fallback after) means authors trust ordering; the one who appends a prohibition at the bottom is exactly the one who won't re-test ordering. Discovery in production requires someone noticing a shipment that should have been refused — an absence of refusal, the hardest thing to observe.
- **Compile-time catch:** Tier 1 is purely syntactic, zero proof machinery: any row after an unconditional same-(state,event) row is unreachable. Tier 2 (guard subsumption over the interval/choice fragment) generalizes it. Diagnostic: 'The Ship row at line 16 has no condition and always matches first — the embargo prohibition at line 22 can never fire. Move the prohibition above the general row.' For the dead-reject case specifically this is a governance-integrity catch: the product's whole premise is that authored prohibitions are enforced.

### [compile-time-clearly-wins] Expiring-exit clock trap (every completing exit time-upper-bounded, no expiry path) (Parcel-locker pickup / reservations)

**Story:** A locker operator models a parcel hold: deposit with a deadline, claim with a code inside the window. The author guards Claim with 'now() <= PickupDeadline' — correct policy — but provides no expiry transition (no ReturnToSender, no Expire). The day after any deadline passes, that locker slot is in limbo forever: Claim returns Unmatched (worked yesterday, silently dead today, no rule violated, no refusal text), Nudge still 'works' but goes nowhere, and the physical slot is occupied indefinitely. Operations staff end up writing host-side sweep jobs — the scattered out-of-band logic Precept exists to eliminate. The dooming event is the passage of time, which fires no operation and triggers no governance.

**The buggy precept:**

```
precept LockerPickup

field ParcelId as string notempty maxlength 50
field PickupDeadline as instant
field PickupCode as string optional

state Held initial
state PickedUp terminal

event Deposit(Parcel as string notempty maxlength 50, Deadline as instant) initial
event Claim(Code as string notempty)
event Nudge

on Deposit
    -> set ParcelId = Deposit.Parcel
    -> set PickupDeadline = Deposit.Deadline

# Pickup is only valid inside the hold window. BUG: no expiry path exists.
from Held on Claim when now() <= PickupDeadline
    -> set PickupCode = Claim.Code
    -> transition PickedUp

from Held on Nudge -> no transition
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. The graph analyzer sees a healthy Held→PickedUp edge (guards are deliberately overapproximated as traversable); no temporal reasoning of any kind runs.
- **Under governance+inspector:** Governance never refuses anything — after the deadline, Claim is simply Unmatched with a mechanical guard trace and no because (guards carry no rationale by design). The end user sees an operation that worked yesterday return a typed non-answer today. The inspector is structurally blind: it evaluates at the current clock, so every authoring-time simulation (deadline in the future) shows Claim enabled, and there is no control to ask 'show me this locker in 31 days.' This and the incident scenario are the two cleanest counterexamples to the per-keystroke inspection paradigm — the defect lives at a future instant no point-in-time simulation visits.
- **Compile-time catch:** Monotonic-falsifiability classification, no solver: 'now() <= PickupDeadline' is a closing window (once false, false forever; the anchor has no later write site). Flag any state whose every completing exit carries a closing-window guard with no stable or opening-window fallback. Diagnostic: 'After PickupDeadline passes, Held has no exit that can ever succeed — the parcel will be permanently stuck. Add an expiry path (e.g., a return-to-sender transition for the post-deadline case).' The matching expressiveness fix — a first-class timeout/expiry row — is a flagged language-surface unlock (routes to /design); the warning needs no surface change.

### [compile-time-clearly-wins] Qualifier-source field mutation (dynamic currency base changed under dependent money values) (Multi-currency fee schedule administration)

**Story:** A SaaS billing admin precept carries CurrencyCode as the qualifier base for BaseFee and MinimumCharge ('money in {CurrencyCode}') — the exact shape shipped today in samples/fee-schedule.precept, where safety is delegated to a prose comment about a host-level approval flow. The Frankfurt office is migrated from USD to EUR: an admin fires SwitchCurrency. The write commits cleanly — no rule touches it. BaseFee's stored magnitude (say 49.00, deposited as USD) is now labeled EUR: every invoice computed from it is silently wrong money, or the next arithmetic against it hits the backstop fault the runtime reserves for 'cannot happen' states. The flagship defense of the product — currency safety — has an unguarded hole on its own source-of-truth field, and the failure surfaces (if ever) as wrong financial figures long after the causal write.

**The buggy precept:**

```
precept FeeScheduleAdmin

field CurrencyCode as currency default 'USD'
field BaseFee as money in '{CurrencyCode}' default '0 {CurrencyCode}' nonnegative editable
field MinimumCharge as money in '{CurrencyCode}' default '0 {CurrencyCode}' nonnegative editable

rule MinimumCharge <= BaseFee because "Minimum charge {MinimumCharge} exceeds the base fee {BaseFee}"

event SwitchCurrency(NewCode as currency)

# BUG: re-labels every held money value with no conversion and no guard.
on SwitchCurrency -> set CurrencyCode = SwitchCurrency.NewCode
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0 — and the QualifierCompatibility obligation reports 'Proved'. Writes INTO the dependent fields are proof-gated (PRE0141 demands narrowing guards), but the write to the qualifier SOURCE is wholly unguarded: the asymmetry compiles clean with a green proof ledger.
- **Under governance+inspector:** Governance commits the switch as a clean success — no rule is violated because the rule system has no concept of the re-labeling. The inspector shows the same numbers before and after (the magnitudes don't change — that IS the bug); an author would have to know to scrutinize the qualifier semantics of an unchanged-looking value. Downstream, the business discovers it as wrong invoices or an unattributable engine fault. The corpus evidence is decisive: the shipped sample 'mitigates' this with a comment deferring to out-of-band host process — the scattered-rules pattern the product exists to kill.
- **Compile-time catch:** Static structural obligation, no solver: the compiler already knows which fields are qualifier bases of which dependents. Any write targeting such a field discharges a new obligation — prove every dependent field is unset or at its zero default at that write site (the same presence/narrowing machinery behind PRE0116/PRE0141), else reject. Diagnostic: 'CurrencyCode is the currency of BaseFee and MinimumCharge. Changing it while they hold amounts re-labels those amounts in the new currency without converting them. Guard this write to fire only while the fees are zero, or clear them first.' A genuine convert-in-place flow needs an explicit conversion construct — flagged language-surface unlock.

### [marginal] Quantifier mischoice — 'any' written where 'each' was meant (protection inverted to existential) (Employee expense reporting)

**Story:** A finance-ops author writes the per-line cap rule: 'no expense line may exceed $10,000.' They reach for 'any amt in LineAmounts (amt <= ApprovalCap)' — which passes whenever AT LEAST ONE line is under the cap. Every test they run during authoring passes for the right-looking reasons: empty report (vacuous), one compliant line, several compliant lines. In production, an employee submits a report with a $50 parking line and a $48,000 line: the rule is satisfied (the parking line complies), the report commits cleanly, and the because-text — which says the opposite of what is enforced — sits in the audit log documenting a control that does not exist. The failure is a silent wrong ACCEPT of exactly the configuration the rule was written to forbid.

**The buggy precept:**

```
precept ExpenseReport

field LineAmounts as list of integer
field ApprovalCap as integer default 10000

# BUG: 'any' passes when ONE line complies; the author means 'each'.
rule any amt in LineAmounts (amt <= ApprovalCap) because "No expense line may exceed the per-line approval cap of {ApprovalCap}"

state Draft initial
state Submitted terminal

event AddLine(Amount as integer positive)
event Submit

from Draft on AddLine -> append LineAmounts AddLine.Amount -> no transition
from Draft on Submit -> transition Submitted
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:1 — only an incidental PRE0158 on ApprovalCap (no write site). The quantifier direction draws nothing; corpus has zero real quantifier usage, so this whole family is unexercised (corpus bias — a floor, not a ceiling).
- **Under governance+inspector:** Governance never refuses the bad case — the rule is satisfied as written. The inspector is blind on the collections most entities carry early: with zero or one element, 'any', 'each', and the intended rule all behave identically, so every natural authoring scenario confirms the wrong code. Only a deliberately constructed mixed two-element scenario (one compliant, one violating) distinguishes them, and nothing prompts the author to construct it. When it bites, the symptom is an absence of refusal — unobservable by any monitoring built on outcomes.
- **Compile-time catch:** Honestly: no compile-time analysis can decide this — the intent is not in the artifact. What compile-time CAN do solver-free is mitigation: (a) semantic paraphrase at authoring ('this passes if AT LEAST ONE line is within the cap'); (b) an auto-generated discriminating witness in hover/inspector (a {compliant, violating} population showing the rule still accepts); (c) a polarity lint when because-text negation words ('No ... may exceed') contradict an existential quantifier — this exact example would trip it. None of these proves the defect; all surface it. Compile-time's edge over Option 1 is real but heuristic.

### [marginal] Exit-hook vs entry-ensure contradiction (transition refused for all inputs by the definition's own hook) (Construction-site tool crib / asset loans)

**Story:** A site manager models tool loans. Two correct declarations, written months apart: an exit hook on CheckedOut clears BorrowerId (so the shelf state's omit invariant holds when a tool comes back), and an ensure on Lost requiring BorrowerId to be set (liability attaches to the last holder). Adding the ReportLost row connects them fatally: leaving CheckedOut fires the clear, then Lost's ensure evaluates against the cleared field — EVERY ReportLost is refused, for any tool, any borrower, forever, with a because-text blaming the data ('a lost tool must keep the borrower of record') when the defect is the definition fighting itself. If reporting losses is a back-office path exercised rarely, it ships broken. Notably, samples/library-book-checkout.precept ships this exact topology today (exit-clear on CheckedOut/Overdue + 'in Lost ensure BorrowerId is set' + ReportLost rows from those states).

**The buggy precept:**

```
precept ToolCribLoan

field AssetTag as string optional maxlength 50
field BorrowerId as string optional maxlength 100
field SettlementNote as string optional

state Available initial
state CheckedOut
state Lost

in Available modify AssetTag editable
in Available omit BorrowerId

in CheckedOut ensure BorrowerId is set because "A checked-out tool must record the borrower"
in Lost ensure BorrowerId is set because "A lost tool must keep the borrower of record — replacement cost attaches to the last holder"

# Exit hook — keeps Available's omit invariant on return. BUG: also fires on CheckedOut -> Lost.
from CheckedOut, Lost -> clear BorrowerId

event Checkout(WorkerId as string notempty maxlength 100)
event ReturnTool
event ReportLost
event ResolveLoss(Note as string notempty)

from Available on Checkout
    -> set BorrowerId = Checkout.WorkerId
    -> transition CheckedOut

from CheckedOut on ReturnTool -> transition Available

from CheckedOut on ReportLost -> transition Lost

from Lost on ResolveLoss
    -> set SettlementNote = ResolveLoss.Note
    -> transition Available
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. The exit-clear/entry-ensure composition is never folded; the same shape exists undiagnosed in the shipped library-book-checkout sample.
- **Under governance+inspector:** Governance catches it at FIRST FIRE, loudly and every time — the refusal carries the ensure's because-text. The inspector shows the refusal the first time the author previews ReportLost, and the designed authoring loop makes that preview cheap. So Option 1 plausibly catches it during authoring IF the author exercises the edge. The counter-evidence is uncomfortable: a hand-polished corpus sample contains this exact contradiction, suggesting edges genuinely go unexercised even by careful authors. The residual harms under Option 1: the refusal blames data rather than the definition, and a rarely-trodden path ships broken and strands real entities at the worst moment.
- **Compile-time catch:** Reuse the implemented sequential proof flow (presence facts) over the EFFECTIVE action chain of each row — exit hooks + row actions + entry hooks in evaluator order — folded against the target state's ensures: 'clear BorrowerId' yields a provable presence violation of Lost's ensure for ALL inputs. Diagnostic: 'The exit hook on CheckedOut clears BorrowerId before this transition reaches Lost, but Lost requires BorrowerId to be set — every ReportLost will be refused. Exclude this edge from the clear, or move the clear to the return path.' Near-free engineering against shipped machinery; conservative (provably-always-fails only).

### [marginal] Doomed trajectory (accepted input forecloses every future completion) (Consumer credit-line origination)

**Story:** A lender's intake accepts any score 300–850 at Submit — all constraints pass, the application is created, the applicant gets a confirmation. But the lifecycle's exits from UnderReview are Approve (score ≥ 680) and Escalate to senior review (score ≥ 600); during a policy edit, Decline was moved to senior review only. A 550-score applicant is dead on arrival: every event from UnderReview is Unmatched or undefined, the score is never writable again, and the contractually correct moment to refuse — Submit — passed with a success. Months later there's a cohort of applications that can be neither approved, escalated, nor even declined; each individual operation behaved correctly, and the customer is owed an adverse-action notice the system structurally cannot produce.

**The buggy precept:**

```
precept CreditLineApplication

field ApplicantName as string notempty maxlength 200
field CreditScore as integer min 300 max 850
field ApprovedLimit as money in 'USD' default '0.00 USD' nonnegative

state UnderReview initial
state SeniorReview
state Approved terminal
state Declined terminal

event Submit(Applicant as string notempty maxlength 200, Score as integer min 300 max 850) initial
event Approve(Limit as money in 'USD' positive)
event Escalate
event Decline(Note as string notempty)

on Submit
    -> set ApplicantName = Submit.Applicant
    -> set CreditScore = Submit.Score

from UnderReview on Approve when CreditScore >= 680
    -> set ApprovedLimit = Approve.Limit
    -> transition Approved

from UnderReview on Escalate when CreditScore >= 600
    -> transition SeniorReview

# BUG: Decline is reachable only from SeniorReview — sub-600 applications have no exit at all.
from SeniorReview on Approve when CreditScore >= 600
    -> set ApprovedLimit = Approve.Limit
    -> transition Approved

from SeniorReview on Decline
    -> transition Declined
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0. The score interval-containment obligation is 'Proved' ([300..850] fits); guard-blind reachability sees healthy edges everywhere; partial event coverage is deliberately silent (CC#21).
- **Under governance+inspector:** Governance accepts the dooming Submit (correctly, per the declared rules) and never refuses anything afterward — Approve/Escalate are Unmatched, Decline is UndefinedEvent. The inspector partially covers this: an author who runs a 550-score scenario end-to-end and checks the available-events panel in UnderReview would see everything blocked or absent, and the per-state affordance view makes that fairly visible. But the verdict shown is 'blocked for this data' — never 'this Submit created an entity that can NEVER complete,' and the author has to pick the sub-600 scenario deliberately. The commit-time version of the claim ('accepting this value dooms the entity') is something only universal reasoning can state.
- **Compile-time catch:** Backward progressable-predicate fixpoint over the interval fragment: from terminals, compute per-state the disjunction of (exit guard ∧ target progressable); UnderReview's predicate closes to CreditScore >= 600, while Submit's accepted envelope is [300..850] with no later write site for the deciding field. Diagnostic: 'Submit accepts scores 300–850, but no path out of UnderReview exists for scores below 600 and CreditScore is never writable again — applications in that range can never be approved, escalated, or declined.' Honest cost note: this is the most expensive analysis in the candidate set (graph-level fixpoint), conservative outside the interval fragment, and the runtime-refusal variant must gate on provably-doomed only to avoid refusing savable entities.

### [marginal] Definition evolution — tightened rule strands the persisted fleet (trusted restore + readonly composition) (Loan servicing over a persisted book)

**Story:** v1 services loans at any origination score. A year in, policy moves sub-580 loans to a special-assets book, and the author 'hardens' the servicing precept with one line: rule CreditScore >= 580. v2 compiles spotlessly — internally it IS consistent. On redeploy, every persisted sub-580 loan restores fine (trusted hydration) and then bricks on its next touch: the borrower's perfectly good PAYMENT is refused with 'loans under a 580 score are handled on the special-assets book' — a refusal about a field set at origination, on an operation that never touched it, with no in-contract cure (CreditScore has no write site post-construction). The blast radius is the whole legacy segment of the book, discovered borrower by borrower as payments bounce.

**The buggy precept:**

```
# v2 (the edit that bricks the fleet — v1 is identical minus the rule; both compiled)
precept ServicedLoan

field CreditScore as integer min 300 max 850
field OutstandingBalance as money in 'USD' default '0.00 USD' nonnegative
field PaymentsMade as integer default 0 nonnegative

rule CreditScore >= 580 because "Servicing policy: loans under a 580 score are handled on the special-assets book, not standard servicing"

state Active initial
state PaidOff terminal

event Originate(Score as integer min 300 max 850, Principal as money in 'USD' positive) initial
event RecordPayment(Amount as money in 'USD' positive)

on Originate
    -> set CreditScore = Originate.Score
    -> set OutstandingBalance = Originate.Principal

from Active on RecordPayment when RecordPayment.Amount < OutstandingBalance
    -> set OutstandingBalance = OutstandingBalance - RecordPayment.Amount
    -> set PaymentsMade = PaymentsMade + 1
    -> no transition

from Active on RecordPayment
    -> set OutstandingBalance = '0.00 USD'
    -> set PaymentsMade = PaymentsMade + 1
    -> transition PaidOff
```

- **Today's compiler:** precept_compile on BOTH versions: success:true, diagnosticCount:0 each. Each version is internally sound; the defect exists only between them, and no surface in the toolchain takes two definitions as input. (Note: v2's initial event can still accept Score 300–579 and the new rule will refuse those Creates at runtime — also undiagnosed — but the fleet-brick face is the severe one.)
- **Under governance+inspector:** Governance 'catches' it only by stranding: production-later, per entity, with a refusal that blames the borrower's credit score for refusing their payment — maximally misdirected. The inspector is structurally blind twice over: it binds to one definition and one live entity, and has no concept of the persisted population written under the prior contract. Under Option 1 the first warning anyone gets is a customer complaint. Spec §0.7 explicitly scopes this out ('restored state is trusted'); migration is caller responsibility with no tooling to discharge it.
- **Compile-time catch:** Impossible from one file by principle — the honest classification is a third lane: a deploy-time definition DIFF. Mechanically solver-free: compare v1's proven envelope per field/rule against v2's (interval containment, set inclusion, rule-implication via the existing relational representations); any tightening flags 'values in [300, 579] were legal under v1 and may exist — these entities will refuse every operation, and CreditScore has no write site to cure them.' All inputs exist in two ordinary compilations; the missing substrate is the two-version input surface (and ideally a definition fingerprint in the persistence envelope). High-value, mostly mechanical — but it is deploy-gate tooling, not single-definition compile checking, so it supports Option 2 only if the niche is drawn to include contract-evolution safety.

### [marginal] Omit-state field read by a global rule (phantom enforcement against the reset default) (Corporate procurement / purchase requests)

**Story:** A procurement author writes the policy 'a request cannot exceed its assigned budget line' as a global rule. ApprovedBudget is meaningless before triage, so it is omitted in Draft — the corpus-sanctioned idiom. But the global rule is swept on EVERY operation including in Draft, where the omitted field reads as its reset default of $0.00: the first requester who types any positive amount is refused with 'Request $250.00 exceeds the assigned budget line of $0.00' — citing a field their form does not even show (it's omitted). Drafting is hard-blocked for everyone, and the refusal text gaslights the requester about a budget line that doesn't exist yet. The mirrored orientation of the same mistake is worse: the rule silently governs nothing in the omit state while partner fields drift.

**The buggy precept:**

```
precept PurchaseRequest

field RequestedAmount as money in 'USD' default '0.00 USD' nonnegative
field ApprovedBudget as money in 'USD' default '0.00 USD' nonnegative
field Description as string optional

# BUG: meant to govern post-triage states only; as a global rule it is swept in Draft,
# where ApprovedBudget is omitted and reads as its reset default of 0.00.
rule RequestedAmount <= ApprovedBudget because "Request {RequestedAmount} exceeds the assigned budget line of {ApprovedBudget}"

state Draft initial
state Triaged
state Ordered terminal

in Draft omit ApprovedBudget
in Draft modify Description, RequestedAmount editable

event AssignBudget(Budget as money in 'USD' positive)
event PlaceOrder

from Draft on AssignBudget
    -> set ApprovedBudget = AssignBudget.Budget
    -> transition Triaged

from Triaged on PlaceOrder -> transition Ordered
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0 — the QualifierCompatibility obligation even reports 'Proved'. D130 (omitted-field-read) protects only state-anchored expression contexts; global rules are not among them, so the omit×rule composition draws nothing.
- **Under governance+inspector:** This face surfaces FAST under Option 1: the very first Draft edit with a positive amount refuses, and in the live-inspector authoring loop the author hits it on their first test keystroke. The refusal is confusing (cites an invisible field) but immediate and universal across users, so it cannot ship unnoticed past day one of real use. The compile-time case rests on (a) better attribution — the diagnostic names the SCOPING fix, where the runtime refusal points at data — and (b) the silent mirrored orientation, which governance and inspector never surface at all. For this phantom face specifically, Option 1 is honestly close to adequate.
- **Compile-time catch:** Catchable today with shipped machinery: intersect each global rule's field-reference set with the per-state omit maps the compiler already materializes for D130/D131/D132 — one more anchoring context for an existing join, no intervals. Diagnostic: 'This rule reads ApprovedBudget, which is omitted in Draft and evaluates as its reset default of $0.00 there — in Draft the rule will refuse every positive RequestedAmount. Scope the rule to the states where ApprovedBudget has meaning (e.g., an in-state ensure or a when-guard), or remove the omit.' Cheap, exact, and it teaches the right idiom.

### [governance-sufficient] Never-committable mutation row (relational rule violated by every value the row can produce) (Contractor invoicing)

**Story:** A contractor's invoice precept maintains TotalDue manually with a bookkeeping rule tying it to labor + materials. The ReviseMaterials row correctly updates both the line and the total; ReviseLabor — added later — sets only LaborCharge. The post-mutation sweep then fails for essentially every revised value (any NewCharge that changes the sum breaks the equality), so the labor-revision feature simply does not work: every attempt is refused with the total-consistency because-text. The office manager revising a labor line gets a refusal that names the rule and both sides of the equation; nothing wrong ever persists.

**The buggy precept:**

```
precept ContractorInvoice

field LaborCharge as money in 'USD' default '0.00 USD' nonnegative
field MaterialsCharge as money in 'USD' default '0.00 USD' nonnegative
field TotalDue as money in 'USD' default '0.00 USD' nonnegative

rule TotalDue == LaborCharge + MaterialsCharge because "The invoice total must equal labor plus materials — line items and total cannot disagree"

state Draft initial
state Issued terminal

event ReviseLabor(NewCharge as money in 'USD' nonnegative)
event ReviseMaterials(NewCharge as money in 'USD' nonnegative)
event Issue

from Draft on ReviseMaterials
    -> set MaterialsCharge = ReviseMaterials.NewCharge
    -> set TotalDue = LaborCharge + ReviseMaterials.NewCharge
    -> no transition

# BUG: forgot the parallel TotalDue update — the sweep refuses every labor change.
from Draft on ReviseLabor
    -> set LaborCharge = ReviseLabor.NewCharge
    -> no transition

from Draft on Issue -> transition Issued
```

- **Today's compiler:** precept_compile: success:true, diagnosticCount:0 (with all write sites present, even the incidental PRE0158s vanish). No per-row commitability check exists; the row that can never succeed compiles green.
- **Under governance+inspector:** Governance catches it at the FIRST fire, every time, with the most self-diagnosing refusal in this whole set: the because-text states the exact relationship ('total must equal labor plus materials'), which points a competent author straight at the missing 'set TotalDue'. In the designed authoring loop, the first inspector preview of ReviseLabor shows the violation with both values visible; the collect-all sweep names the rule and its fields. Nothing invalid ever persists, the failure is loud, immediate, deterministic, and the fix is legible from the refusal itself. What compile-time would add — the universal statement 'this row can never commit for ANY value' and earlier timing — is real but small here: the per-scenario experience already communicates the defect adequately.
- **Compile-time catch:** Feasible solver-free (the relational machinery from the locked relational-rules design): propagate the row's net effect through the equality rule — LaborCharge becomes a free interval while TotalDue and MaterialsCharge are unchanged, so the equality is violated for every NewCharge ≠ current LaborCharge. Diagnostic: 'This row changes LaborCharge but not TotalDue, while the rule requires TotalDue == LaborCharge + MaterialsCharge — the revision will be refused for every new value. Add a parallel set of TotalDue (or make TotalDue a computed field).' Worth having as a polish item; not needed to protect anyone here.

**Leg summary:** Twelve scenario deep-dives, all probe-verified against the live 2026-06-10 build via precept_compile; every buggy definition compiles success:true (several with the relevant proof obligation reporting 'Proved'), with at most incidental PRE0158/PRE0119 warnings that do not name the defect. Verdict distribution: 6 compile-time-clearly-wins, 5 marginal, 1 governance-sufficient. The dividing line that emerged is sharp and consistent: compile-time clearly wins exactly where (a) governance's 'catch' is silence or a wrong SUCCESS rather than a refusal — shadowed embargo prohibitions ship goods, qualifier-source mutation re-labels money, guard-locked strands return Unmatched forever — or (b) the defect lives somewhere no point-in-time scenario visits: a future clock instant (incident-rule brick, locker expiry trap), the conjunction of two optional fields nobody sets together (relational cycle), or the universal claim 'never' hiding behind the inspector's honest 'not yet'. All six winning catches are solver-free and mostly extensions of shipped machinery (interval/relational folds, write-site analysis, monotonicity classification of now()-comparisons, a structural qualifier-dependency obligation), and the diagnostics write naturally in domain language. The marginal verdicts are honest splits: first-fire universal refusals (exit-hook contradiction, omit×rule phantom face) surface immediately in the designed authoring loop — though the exit-hook contradiction shipping undetected in samples/library-book-checkout.precept is uncomfortable counterevidence about unexercised edges; quantifier mischoice is undecidable with only heuristic compile-side mitigation; doomed trajectories need the most expensive analysis with conservatism risk; and the definition-evolution fleet-brick — arguably the most severe story of the twelve, with both versions compiling clean — is structurally a two-definition deploy-gate lane, orthogonal to single-file compile checking, supporting Option 2 only if the niche is drawn to include contract evolution. The one governance-sufficient verdict (never-committable invoice row) calibrates the set: when the mandatory because-text itself states the violated relationship, the first-fire refusal is effectively a compile diagnostic delivered slightly late, and Option 1 covers it. Net evidence shape: the strongest compile-time case is not 'more checking' but the specific family of silent-wrong-success and clock/path/pair-universal defects where Option 1's two pillars — refusals and per-scenario inspection — are both structurally mute.

---

## Leg 2 — Expressiveness / ceremony cost simulation (corpus numbers = indicative floors)

### [moderate] Overflow obligations via interval arithmetic (max bounds demanded on operands of *, +, accumulation)

- **Catches:** Arithmetic overflow on unbounded operands (Probe14 hole: 2e9*2e9 compiles clean today against Principle 10's explicit claim) — a fault-floor breach to close under EITHER option, but the obligation propagates a bounds-discipline demand onto every multiplied/added numeric field.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 360 numeric-lane fields, only 59 (16%) carry max; 61 multiplication lines, ~35 distinct multiplied field operands; 247 set/computed lines with * or +. Verified live: loan-application absorbs the full obligation with 8 added max clauses (4 fields + 4 args) and compiles clean, every IntervalContainment obligation Proved.
- **Ceremony:** ~1 max clause per multiplied operand: roughly 30–50 new declarations corpus-wide if obligation-driven (only where arithmetic demands), ~300 if blanket-mandated on all numeric fields. The bound doubles as documentation, consistent with the divisor discipline the product already demands. Real cost is BOUND-INVENTION: 'max 10000000.00 USD' on AnnualIncome is a representability fact, not a business fact — authors must pick a number, and an invented bound can later refuse legitimate data (composing with the definition-diff stranding class when widened/tightened).
- **Expressiveness loss:** None structural — no pattern becomes unwritable. The loss is honesty-adjacent: invented ceilings masquerade as policy.
- **Language change could dissolve it:** Yes, mostly — type-level default representable bands on business-domain types (money/quantity carry a sane default max unless overridden), so explicit max is needed only when the domain band is tighter; kind: catalog/type-metadata change, not grammar.

### [cheap] Write-site preservation / durable-truth proof (event-door ensures must remain true: subject fields frozen afterward or truth re-asserted as rule/in-state ensure)

- **Catches:** The validation-not-governance trap inside the language: 'on Submit ensure X' checked once at the door while X stays editable afterward — checked-once truth silently un-made by a later legal Update.
- **Corpus footprint (floor):** INDICATIVE FLOOR, and a striking one: 83 field-subject event ensures across 77 samples; 0 of 83 have a baseline-editable subject field. The corpus idiom (per-state 'in Draft modify ... editable' lists, fields freeze after the door — insurance-claim, contractor-invoice verified by read) is ALREADY durable. The check passes the entire corpus unchanged.
- **Ceremony:** Zero on the corpus as measured. When flagged in real authoring (field-level editable baseline + door ensure): one state-scoped ensure or covering rule per non-durable truth, or narrowing editable to the pre-door states — the narrowing IS the correct fix, not redundancy.
- **Expressiveness loss:** Deliberate snapshot-at-door semantics (validate once, then let it drift) needs an explicit acknowledgment — rare and arguably should be loud.
- **Language change could dissolve it:** Yes — the spec §0.5 deferred modifiers ('sealed after <State>', 'writeonce') turn durability into a one-token declaration instead of a duplicated ensure; additive modifier, already roadmapped.

### [moderate] Local-contract mandates on events (declared bounds required on all args; unconditional fallback/reject row required per routed (state,event))

- **Catches:** Unconstrained-arg garbage ingress, unwinnable argument contracts (arg band vs guard disjoint — Kleene 'Possible' lying to forms), guard-coverage holes yielding reasonless Unmatched.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 563 numeric args, 456 (81%) lack max; 1006 string args, 366 (36%) lack maxlength; 661 (state,event) row groups, only 17 (2.6%) are all-guarded with no unconditional fallback, while 185 already follow the guarded+fallback idiom the mandate would enforce.
- **Ceremony:** Split verdict. Fallback mandate: ~17 added reject rows corpus-wide — trivial, and each forces an authored refusal reason where today there is a reasonless Unmatched. Arg-bound mandate: the single largest ceremony item in the whole simulation — ~456 numeric max bounds + up to 366 maxlengths ≈ 800 tokens, ~10 per file, most of them invented numbers on Note/Reason-style strings where no business bound exists.
- **Expressiveness loss:** None for fallbacks. Blanket arg bounds punish free-text and open-magnitude args; obligation-driven bounds (demanded only where the arg flows into a bounded sink or arithmetic — the already-shipped composition-forcing path) capture most of the catch value at a fraction of the cost.
- **Language change could dissolve it:** Partially — precept-level or type-level default bands (e.g., a declared default maxlength for string args, overridable) amortize the blanket cost to near zero; kind: defaults-declaration surface, not per-site grammar.

### [cheap] Affine/3-field equality reasoning (relational closure incl. never-committable-row check: rows must preserve declared equality/linear rules)

- **Catches:** Paired-field drift bricks (rule Total == Base + Tax + row that sets only Base → row provably never commits, refusals blame the user's numbers) and the cross-field relational contradictions (A min B+1 / B min A+1, Probe33) that compile clean today.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 30 equality (==) rules, 22 arithmetic-bearing rules. Exemplar verified by read (saas-license-management, 'Available + Allocated == Total'): every mutation row already updates partner fields in paired ±1 deltas — the analysis PROVES the existing idiom and demands nothing. The contradiction check rejects only definitions that are already broken.
- **Ceremony:** ~Zero on the corpus: no declarations demanded, no redundancy. Forced edits occur only at genuine defects (the forgotten partner 'set' — which is the bug, not ceremony). Guarded equalities outside the affine fragment conservatively stay runtime-governed (no rejection, no cost).
- **Expressiveness loss:** None — locked relational design (octagon-style single-pass, bounded closure) flags proven cases only.
- **Language change could dissolve it:** Not needed — though making invariant-paired updates declarative (a derived/computed form: 'field Available <- Total - Allocated' already expressible today) deletes both the rule and the paired-set ceremony entirely; kind: authoring guidance, existing surface.

### [cheap] Semantic dead-end analysis (per-state value envelopes; guard satisfiability vs values entities actually carry, incl. guard-locked strands and dead quantified guards)

- **Catches:** ProbeN-class permanent traps (only inbound row sets X=5, only exit needs X>50 — clean compile today), guard-locked strands ('Approve when ApprovedBy is set' with ApprovedBy unattainable in-state), the stranded-process bug philosophy.md claims is impossible.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 296 states with outgoing rows; only 3 states corpus-wide have every exit guarded (the at-risk topology); 17/661 all-guarded event groups. The corpus's dominant guarded-route + unconditional-fallback/reject shape (185 groups) and unconditional Decline/Cancel escapes pass the analysis trivially — loan-application passes via its unguarded Decline row with zero edits.
- **Ceremony:** As warning: zero forced. As prove-or-reject: ~3 states would each need one escape row or widened guard (indicative floor). Per-state envelope machinery is compiler engineering cost, not author ceremony.
- **Expressiveness loss:** Low — non-interval guards (string predicates, quantifiers) conservatively widen the envelope: fewer catches, never false rejections. Deliberate permanent-hold states need the existing dead-end-warning posture (warning, not error).
- **Language change could dissolve it:** No ceremony to dissolve; an optional 'perpetual'/hold intent modifier (additive) silences the warning where holds are deliberate.

### [moderate] Trajectory/completability analysis (backward progressable-predicate from terminals; doomed-input refusal at commit time)

- **Catches:** Inputs accepted today that foreclose every future completion (Score=500 accepted into a state whose only exits need >=680 with Score never writable again) — the defining gap between per-operation governance and whole-trajectory soundness; the contractually correct refusal moment otherwise passes silently.
- **Corpus footprint (floor):** INDICATIVE FLOOR: corpus decision states virtually all carry unconditional escape rows (258 reject rows, ubiquitous Decline/Cancel paths, 5 from-any escape rows), so compile-time completability passes the corpus nearly everywhere — loan-application is completable as written because Decline exists. The refuse-at-commit upgrade is opt-in ceremony: the verified AFTER adds exactly 1 Submit door ensure to refuse born-doomed applications.
- **Ceremony:** Compile-time warning tier: ~zero forced on corpus. Commit-time doom-refusal tier: ~1–2 door ensures per decision-heavy precept where the author wants 'refuse the dooming input now' instead of 'declinable later'. The analysis itself is the most expensive ENGINEERING item in the inventory (backward fixpoint), but author-facing ceremony is small.
- **Expressiveness loss:** Low-to-moderate: value concentrates in interval/relational-fragment guards; heavy string/quantifier guards degrade coverage gracefully. Refusal direction needs care — runtime refusal must gate on provably-doomed only, else conservative analysis refuses savable entities (a real soundness-of-UX risk, flagged in the inventory).
- **Language change could dissolve it:** Optional 'progressable'/completable intent modifier per state scopes where the obligation binds (additive); not required for the warning tier.

### [severe-today-but-dissolvable] Places-dataflow proof (maxplaces vs division/multiplication) with rounding discharge

- **Catches:** Probe22 hole: 'maxplaces 2 <- Total / Parts' compiles clean while 10/3 violates the bound at runtime — data-dependent stranding or silent rounding breaching Principle 8 (honesty about approximation). Core to the product's financial domain (46 maxplaces uses, exchangerate maxplaces 8).
- **Corpus footprint (floor):** INDICATIVE FLOOR: 46 maxplaces declarations; 32 division sites; ~20 division sites feed money/decimal/percent sinks (ProrationCredit, YieldPercent x4, OverallHealthScore x3, AverageCost, MilestonePercentage x3, UtilizationPercentage, DiscountApplied x2, OEE, OverallScore, ClaimAmount/2 x2). PROBE-VERIFIED this session: round(Total / Parts, 2) into a maxplaces 2 field already compiles — round(value, places) EXISTS in the surface (primitive-types.md:391).
- **Ceremony:** With round-recognition in the places-dataflow rule: one round(...) wrap per division-into-places sink, ~20 edits corpus-wide — each making a currently-implicit rounding decision explicit (a correctness gain, not pure ceremony). Without round-recognition: the proof bans division into any maxplaces/money sink — ~20 existing sites become unwritable.
- **Expressiveness loss:** Severe if shipped without the rounding discharge rule (bans a core financial idiom); collapses to one explicit token per site with it.
- **Language change could dissolve it:** Yes and cheaply — no grammar needed: teach the places-dataflow to treat round(x, n) as places-n (proof-rule change over an existing function); optionally an assignment-position rounding modifier as sugar.

### [cheap] Clock-decay classification (monotonicity taint on now()-bearing rules/guards/access-mode guards; expiry metadata on inspection results)

- **Catches:** Entities bricked or silently stranded by time alone (Probe23/ProbeQ: now() in unconditional rules refuses even the remedial Close; deadline-only exits become permanent traps with no operation to govern) — the cleanest inspector-blind class (per-keystroke Inspect evaluates at real now(), cannot time-travel).
- **Corpus footprint (floor):** INDICATIVE FLOOR — and the floor/ceiling gap matters most here: 437 now() uses across 49/77 files, but 428 are the safe 'set X = now()' timestamp idiom; 0 in rules, 1 in guards, 2 in ensures → ~3 flag sites in today's corpus. The corpus modeled time as explicit day-counter fields precisely because the language offers no expiry surface — real deadline-driven domains (holds, offers, lockers, SLAs) will reach for now() immediately.
- **Ceremony:** ~Zero today (3 sites). Per flagged site: a scoping guard, a stable fallback exit row, or a declared escape — the fix the author needed anyway. Inspection expiry metadata is additive result-shape, no authoring cost.
- **Expressiveness loss:** None for the warning form; banning bare decaying rules outright would be moderate (the temporal doc's own §575 example) — 'decaying rule requires a declared escape path' is the right-sized posture.
- **Language change could dissolve it:** Yes — a first-class timeout/expiry-row construct (declared expiry disposition per state) replaces both the warning ceremony AND host-side sweep-job code; net expressiveness GAIN; kind: new transition-row form, routes to /design.

### [cheap] Definition-diff checking (cross-version envelope containment, routing-partition diff, hook/default/ordering change reports; envelope fingerprinting substrate)

- **Catches:** The entire evolution vantage's critical cluster: tightened-band fleet stranding, removed/renamed states failing Restore, added-required-field breakage, restore-time recompute faults, silent routing drift, default-change skew — population-scale, invisible to compiler, governance, AND inspector (all single-version surfaces).
- **Corpus footprint (floor):** Zero authoring footprint on all 77 samples — the capability consumes TWO compilations at deploy time plus an envelope fingerprint; no .precept line changes. It is a third lane orthogonal to the Option-1/Option-2 axis (single-definition compile strength is unchanged either way). N/A as a corpus-ceremony measurement; labeled for completeness.
- **Ceremony:** Process ceremony only: a deploy gate that flags tightenings/removals/renames as migration obligations (friction by design, landing exactly at the highest-blast-radius moment); rename disambiguation needs one mapping annotation per rename; substrate cost is one hash field in the persistence envelope.
- **Expressiveness loss:** None on the language.
- **Language change could dissolve it:** Inverse framing: a small version-linkage/rename-mapping surface is what UNLOCKS it (deploy-manifest annotation, not authoring grammar); nothing to dissolve.

### [cheap] Because/reject expression checking + structured refusal attribution (bind/typecheck interpolations at because/reject/default positions; conjunct-level guard attribution in fallback rejects)

- **Catches:** Probes 8/9/11: {Missing} and {A + true} in because-text compile clean today while the same expression in a set-string errors PRE0017 — the product's refusal voice breaks exactly at refusal time; plus the loan-application fallback-divergence (reject text lists 3 of 5 guard conjuncts, misleading every refused user).
- **Corpus footprint (floor):** INDICATIVE FLOOR: 139 interpolated because-messages + 258 reject rows are the exposed surface; checking demands ZERO new declarations (it rejects only genuinely broken references — a wiring pass over machinery that exists). The divergence fix without language change = hand-maintained conjunct prose: the verified AFTER expanded loan-application's fallback reject from 3 to 5 narrated conditions (+2 prose clauses that will drift again on the next guard edit).
- **Ceremony:** Checking: none. Honest fallback prose: ~1 line of re-narration per guarded row group (185 such groups) that re-states guard logic as text — a forced duplication that is itself the defect's cause.
- **Expressiveness loss:** None from checking.
- **Language change could dissolve it:** Yes — a structured reject form referencing the failed guard (evaluator already evaluates every conjunct deterministically; derive per-conjunct failure attribution instead of re-narrating) removes the duplication entirely; negative net ceremony; kind: reject-clause form, routes to /design.

### [cheap] Floor-completion cluster (choice-default membership, mincount-at-birth fold, hook-guard PRE0082 coverage, hook-vs-ensure fold, omit×global-rule join, quantifier binder-shadowing/unused-binder/vacuity extensions, unused-arg walk, syntactic shadowed-row tier)

- **Catches:** The probe-verified holes that are coverage gaps in EXISTING machinery, not new reasoning: Probe16 (choice default outside vocabulary), Probe34 (mincount>0 empty at birth marked Proved), ProbeR/ProbeM/ProbeE, the omit×rule phantom/suspension faces, nested-binder capture (PRE0103 already exists for the sibling case).
- **Corpus footprint (floor):** Compiled-corpus check: the entire 77-file corpus compiles clean today (~44ms) and contains zero quantifier usage, near-zero at-risk topologies for these checks — rejections forced on the corpus: ~0 (INDICATIVE FLOOR; these checks exist precisely for the inputs demos never contain).
- **Ceremony:** None — every check in the cluster rejects only definitions that are already defective (born-invalid, self-contradictory, dead prohibition, captured binder). This cluster strengthens Option 1's own floor regardless of the decision.
- **Expressiveness loss:** None.
- **Language change could dissolve it:** No ceremony exists to dissolve.

### [moderate] Qualifier-source mutation obligation (writes to a field serving as a '{X}' qualifier base must prove dependents unset/at-zero)

- **Catches:** Probe-verified hole with live corpus evidence: 'set CurrencyCode = ...' while Amount holds money in '{CurrencyCode}' compiles clean and silently reinterprets stored magnitudes — the silently-wrong-money class on the qualifier system's own source of truth.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 1/77 files ships the hazard (fee-schedule.precept: editable CurrencyCode, two money-in-'{CurrencyCode}' dependents, mitigated only by a prose comment deferring to a host-level approval flow — the exact out-of-band pattern Precept exists to eliminate). Interpolated-qualifier money fields appear in several samples (invoice-line-item et al.) but with non-editable sources.
- **Ceremony:** Per qualifier-source write site: one guard proving dependents are unset/at-default (machinery behind PRE0116/PRE0141 already proves these facts). fee-schedule would need 1 guard or a redesign of its currency-switch flow.
- **Expressiveness loss:** Moderate at the edge: genuine convert-in-place flows (change code AND convert held values) become inexpressible-by-rejection rather than silently wrong — today they are silently WRONG, so the rejection buys correctness; full expressiveness needs the conversion construct.
- **Language change could dissolve it:** Yes — an explicit conversion-action surface (declared rate + atomic convert) makes repricing expressible and dissolves the guard ceremony for that flow; kind: new action form, routes to /design.

### [cheap] Terminal-frozen semantics (terminal implies modify-all-readonly + no wildcard/no-transition mutation rows unless opted out)

- **Catches:** ProbeP/ProbeH: post-completion mutation surfaces invisible at the state declaration site — explicit no-transition rows from terminals, from-any wildcards expanding into terminals, editable-baseline fields writable in Done — the audit-grade 'completed records are frozen' expectation the current 'terminal' does not deliver.
- **Corpus footprint (floor):** INDICATIVE FLOOR: 122 terminal states across 77 files; 139 baseline-editable field declarations; 17/77 files use from-any wildcards (every one silently expands into its terminals today). loan-application needed zero edits (no baseline editable; access already state-scoped) — the corpus's per-state-modify idiom largely passes free.
- **Ceremony:** One explicit opt-out line per legitimate post-completion mutation (append an audit note) — honest ceremony that documents the exception; wildcard authors add 'except <terminals>' or accept exclusion. Estimated low-tens of edits corpus-wide.
- **Expressiveness loss:** None — every current behavior remains expressible, opted into visibly.
- **Language change could dissolve it:** This IS the language change (modifier semantics: terminal defaults to frozen + an opt-out clause; wildcard exclusion form); the ceremony is the feature.


### Before/after — a complex sample under all-in checking

REPRESENTATIVE REWRITE — samples/loan-application.precept (the flagship sample; 109 lines; money arithmetic, multi-factor guard, sentinel defaults, fallback reject) under ALL-IN checking. The AFTER below was compiled live this session: success:true, 0 diagnostics, every proof obligation (IntervalContainment x5, QualifierCompatibility x3, Presence, LengthContainment) reports Proved.

DELTA LEDGER (what all-in checking actually demanded):
+8 max clauses — overflow/band obligations on the four money fields (RequestedAmount, ApprovedAmount, AnnualIncome, ExistingDebt) and the four money args that feed them; the bounds are invented representability ceilings (10M/30M USD), not business facts: this is the one honest ceremony cost.
−5 sentinel defaults REMOVED ('0.00 USD' x4, CreditScore 300) — Submit is the initial event and assigns all intake fields, so the compiler's existing initial-assignment proof makes the sentinels pure hazard (the inventory's legal-but-wrong-sentinel class); ApprovedAmount becomes honest-absence: optional + positive.
+1 'when ApprovedAmount is set' guard on the cap rule — the presence proof's price for honest absence (one clause).
+1 Submit door ensure (Debt <= 3x Income) — trajectory analysis as written PASSES via the unconditional Decline escape; this opt-in ensure upgrades 'declinable later' to 'refused at commit', killing the born-doomed application class at its door.
~1 reject-prose expansion — the fallback reject now narrates all FIVE guard conjuncts instead of three (the fallback-divergence defect, present in the shipped sample); without a structured reject form this prose will drift again — the duplication is exactly what the sketched reject-references-guard language change dissolves.
0 changes for: durable-truth (no baseline-editable subjects), semantic dead-end / envelopes (unguarded Decline exit), terminal-frozen (access already state-scoped), clock-decay (all now() uses are the safe timestamp idiom), affine reasoning (no equality rules), places-dataflow (no division into places sinks).

AFTER (verified compiling clean):

precept LoanApplication

field ApplicantName as string notempty maxlength 200
field RequestedAmount as money in 'USD' positive max '10000000.00 USD'
field ApprovedAmount as money in 'USD' optional positive max '10000000.00 USD'
field CreditScore as integer min 300 max 850
field AnnualIncome as money in 'USD' nonnegative max '10000000.00 USD'
field ExistingDebt as money in 'USD' nonnegative max '30000000.00 USD'
field DecisionNote as string optional
field DocumentsVerified as boolean default false

field SubmittedAt as instant
field ApprovedAt as instant optional
field FundedAt as instant optional
field DeclinedAt as instant optional

rule ApprovedAmount <= RequestedAmount when ApprovedAmount is set because "Approved amount {ApprovedAmount} exceeds the submitted request of {RequestedAmount} — the bank cannot approve more than the applicant asked for"
rule ExistingDebt <= AnnualIncome * 3.0 when DocumentsVerified because "Existing debt {ExistingDebt} exceeds the 3x income ceiling — maximum allowed at {AnnualIncome} income is {AnnualIncome * 3.0}"

state UnderReview initial
state Approved
state Funded terminal
state Declined terminal

in Approved ensure DocumentsVerified because "Approved loan must have verified documents — approval cannot precede the verification step"
in Approved ensure ApprovedAt is set because "An approved loan must carry the approval timestamp"
in Funded ensure ApprovedAmount is set because "Funded loan must carry an approved amount"
in Funded ensure FundedAt is set because "A funded loan must record when the disbursement occurred"
in Declined ensure DeclinedAt is set because "A declined loan must record when the decision was made"

in UnderReview when DocumentsVerified modify DecisionNote editable

event Submit(
    Applicant as string notempty maxlength 200,
    Amount as money in 'USD' positive max '10000000.00 USD',
    Score as integer min 300 max 850,
    Income as money in 'USD' nonnegative max '10000000.00 USD',
    Debt as money in 'USD' nonnegative max '30000000.00 USD') initial

event VerifyDocuments
event Approve(Amount as money in 'USD' positive max '10000000.00 USD', Note as string optional)
event Decline(Note as string notempty)
event FundLoan

on Submit ensure Submit.Debt <= Submit.Income * 3.0 because "Debt {Submit.Debt} already exceeds the 3x income ceiling for {Submit.Income} income — this application can never be approved; correct the figures or do not submit"

on Submit
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> set CreditScore = Submit.Score
    -> set AnnualIncome = Submit.Income
    -> set ExistingDebt = Submit.Debt
    -> set SubmittedAt = now()

to Approved -> set ApprovedAt = now()
to Funded -> set FundedAt = now()
to Declined -> set DeclinedAt = now()

from UnderReview on VerifyDocuments
    -> set DocumentsVerified = true
    -> no transition

from UnderReview on Approve when DocumentsVerified and CreditScore >= 680 and AnnualIncome >= ExistingDebt * 2.0 and RequestedAmount < AnnualIncome / 2.0 and Approve.Amount <= RequestedAmount
    -> set ApprovedAmount = Approve.Amount
    -> set DecisionNote = if Approve.Note is set then Approve.Note else if CreditScore >= 750 then "Prime tier — auto-approved" else "Standard tier — approved"
    -> transition Approved
from UnderReview on Approve
    -> reject "Approval requires: verified documents (verified: {DocumentsVerified}), credit score 680+ (yours: {CreditScore}), income at least double existing debt ({AnnualIncome} vs {ExistingDebt}), requested amount under half of income ({RequestedAmount} vs {AnnualIncome}), and an approved amount within the request ({RequestedAmount})"

from UnderReview on Decline
    -> set DecisionNote = Decline.Note
    -> transition Declined

from Approved on FundLoan
    -> transition Funded

NET: 109 → 81 lines; the AFTER is SHORTER and strictly more honest than the BEFORE. Total ceremony across every all-in capability simultaneously: 8 invented bounds, 1 presence guard, 1 opt-in door ensure, 1 prose expansion — against 5 hazardous sentinels deleted and a born-doomed input class refused at its door. No pattern in the file became unwritable.

**Leg summary:** CEREMONY SIMULATION HEADLINE (all corpus numbers are INDICATIVE FLOORS — 77 demo files expressing only what today's language allows): the all-in checking posture's expressiveness cost is concentrated in exactly ONE place — bounds-invention. Every other capability is either zero-ceremony catch-only analysis that the corpus's existing idioms already pass, or has its ceremony dissolved by a small, mostly already-roadmapped language change.

THE THREE-TIER COST STRUCTURE: (1) FREE TIER — most of the inventory's universal-reasoning capabilities cost the corpus nothing because the safe idiom is already dominant: durable door-checks (0/83 field-subject event ensures have an editable-baseline subject — per-state modify freeze is the universal idiom), affine equality preservation (saas-license-management's paired ±1 updates are exactly what the analysis proves), semantic dead-ends (3/296 states have all-guarded exits; the guarded+unconditional-fallback shape covers 185/661 groups), clock decay (0 now() in rules, 1 in guards across 437 uses — 428 are the safe timestamp idiom), terminal-frozen and the entire floor-completion cluster (rejects only already-broken definitions). The demos being authored by people who knew the safe shapes is precisely why these floors understate REAL-author value: the checks exist for the authors who DON'T know the idioms. (2) PAID TIER — overflow obligations and arg-contract bounds are the one genuine ceremony bill: 81% of numeric args and 84% of numeric fields lack max; obligation-driven enforcement (bounds demanded only where arithmetic/sinks require — the already-shipped carries-constraint pattern) costs ~30-50 declarations corpus-wide and the verified loan-application rewrite absorbed it in 8 clauses, while a blanket mandate would cost ~800 invented tokens and is not warranted. The qualitative cost is honesty, not volume: invented representability ceilings (max '10000000.00 USD') masquerade as policy and compose badly with definition-diff tightening semantics. (3) DISSOLVABLE TIER — the only capability that would be SEVERE as naively shipped (places-dataflow banning division into maxplaces/money sinks, ~20 corpus sites) dissolves to one token per site because round(value, places) ALREADY EXISTS in the surface (probe-verified compiling into a maxplaces sink) — the fix is a proof-rule recognizing it, not grammar. The structured-reject form, expiry-row construct, sealed-after/writeonce modifiers, conversion action, and default-band metadata each turn their capability's residual ceremony negative (they delete existing duplication or host-side code).

DECISION-RELEVANT NET: the simulation found NO capability whose honest cost is unavoidable severe expressiveness loss. The BEFORE/AFTER (loan-application, compiled clean with all obligations Proved) ends SHORTER than the original. If Option 2's bar includes 'without severe limitations on expressiveness', expressiveness is not the discriminator — the real discriminators remain the catch-value and engineering-cost questions from the defect inventory, not author ceremony. Conversely, this is NOT evidence that Option 2 is free: the bounds-discipline bill lands on every real author on day one (the corpus floor understates it for authors with genuinely unbounded domains), and the two most expensive analyses (trajectory fixpoint, definition-diff substrate) carry their cost in engineering and process rather than syntax. Definition-diff remains a third lane orthogonal to both options. One process risk worth naming: several capabilities are cheap ONLY in their warning/obligation-driven form — blanket-mandate or prove-or-reject framings of the same capabilities (mandatory arg bounds, mandatory fallbacks, conservative doom-refusal at runtime) flip them from cheap to punitive, so the posture choice per capability matters as much as the capability choice.

---

## Leg 3 — External evidence


### Pain (are these defects real-world problems?)

- **Stranded/stuck workflow instances are a real, documented failure mode in Camunda: resolving an incident can leave the process instance permanently unable to continue — the engine's own state machine rejects the resume command.** [primary]
  - https://github.com/camunda/camunda/issues/22140 (Camunda bug tracker, issue #22140)
  - > "There was an incident on my parallel gateway due to a faulty execution listener. When I tried resolving the incident, the incident disappeared but the process instance did not continue." ... rejected with: "Expected to be able to activate parallel gateway 'and', but not all sequence flows have been taken."
- **In Temporal, a definition/data mismatch (non-deterministic error) leaves workflows stuck and unable to make progress while burning resources — the runtime detects the inconsistency but cannot recover it.** [secondary]
  - https://medium.com/@qlong/myths-in-temporal-non-deterministic-errors-4e0ca7a6aa1b (Long Quanzheng, ex-Temporal engineer / iWF creator)
  - > "The workflow will be stuck, and cannot make any progress, not responding to query requests. The workers will keep retrying in a loop, consuming lots of resources on both sides (temporal workflow workers and Temporal Cloud)." ... "NDEs are usually pretty bad in production."
- **Migration-broken entities are real and remediation tooling is weak: a real Temporal user deployed a definition change without versioning, in-flight workflows hung, and Temporal staff confirmed the remedy is terminate-and-restart with no good way to even find all affected instances.** [primary]
  - https://community.temporal.io/t/how-to-automatically-restart-workflow-which-have-non-deterministic-error/7530 (Temporal community forum; user Youssef_Saeed + Temporal staff Chad_Retz)
  - > User: "I deployed some code changes in the workflow without using patching, then the workflow may throw a non-deterministic error (because history doesn't match)." Staff: "the workflow is intentionally 'hung' with task failure which you can see in the UI" ... "we admittedly don't have a good 'select all running workflows with this specific task failure occurring'."
- **Structural errors (deadlocks, improper completion) ship in real industrial process models and are discovered late: the SAP reference model — a vendor-curated best-practice collection — contained at least 34 erroneous models out of ~600, and the authors stress this is a lower bound.** [primary]
  - https://hverbeek.win.tue.nl/downloads/preprints/Mendling08.pdf (Mendling, Verbeek, van Dongen, van der Aalst, Neumann — Data & Knowledge Engineering 64 (2008) 312–329; verbatim from PDF)
  - > "This model collection contains about 600 process models expressed as Event-driven Process Chains (EPCs). We translated these EPCs into YAWL models, and analyzed them using the verification tool WofYAWL. We discovered that at least 34 of these EPCs contain errors." ... "We have to stress that this analysis yields a lower bound for errors" ... "It is a fundamental insight of software engineering that errors should be detected as early as possible in order to minimize development cost."
- **Conflicting business rules that reach production fail unpredictably or silently corrupt data; the rules-engine industry itself frames build-time conflict detection as the fix (vendor content — market evidence that the pain is recognized, not neutral measurement).** [tertiary]
  - https://www.nected.ai/us/blog-us/what-happens-when-rules-conflict-rule-engine (Nected vendor blog)
  - > "The system will either crash, fire both rules unpredictably, or blindly fall back on a default resolution strategy to guess a winner." ... "If you haven't explicitly defined priorities or tie-breakers in your architecture, overlapping rules will just quietly corrupt your payload state in the background." ... "If the engine detects overlapping conditions in a mutually exclusive group during your deployment pipeline, failing the build outright prevents production data corruption."


### Value (where compile-time guarantees paid)

- **AWS found per-scenario techniques (testing, review, fault injection) structurally insufficient for subtle design errors — the state space is too large — and that universal model checking found bugs those techniques provably missed (e.g., a DynamoDB bug needing a 35-step trace). Directly supports the universal-claims-vs-per-scenario distinction in the bar's clause (d).** [primary]
  - https://lamport.azurewebsites.net/tla/formal-methods-amazon.pdf (Newcombe, Rath, Zhang, Munteanu, Brooker, Deardeuff — 'Use of Formal Methods at Amazon Web Services', 2014; verbatim from PDF)
  - > "We have found that testing the code is inadequate as a method to find subtle errors in design, as the number of reachable states of the code is astronomical." ... DynamoDB replication & group-membership system, 939 lines TLA+: "Found 3 bugs, some requiring traces of 35 steps." ... "In every case TLA+ has added significant value, either finding subtle bugs that we are sure we would not have found by other means, or giving us enough understanding and confidence to make aggressive performance optimizations without sacrificing correctness."
- **AWS's core argument is prevention-not-detection for data-integrity bugs: reactive/runtime mechanisms cannot recover from bugs that permanently damage data — exactly the class Precept's governance discards-and-refuses cannot undo if the rules themselves are wrong. Also: the universal 'what needs to go right' framing beat scenario-by-scenario 'what might go wrong'.** [primary]
  - https://lamport.azurewebsites.net/tla/formal-methods-amazon.pdf (same AWS paper; verbatim from PDF)
  - > "reactive mechanisms cannot recover from the class of bugs that cause permanent damage to customer data; instead, we must prevent such bugs." ... "We have found this rigorous 'what needs to go right?' approach to be significantly less error prone than the ad hoc 'what might go wrong?' approach." ... "Engineers from entry level to Principal have been able to learn TLA+ from scratch and get useful results in 2 to 3 weeks."
- **SPARK's compile-time elimination of defect classes produced measured industrial wins: Lockheed C130J saved 80% of the MC/DC test budget at near-normal coding rates, with fault density under one tenth of industry norm; proof was found more cost-effective at finding faults than testing; and proofs are 'valid for all input data' — the universal claim per-scenario testing cannot make.** [primary]
  - https://www.sigada.org/ada_letters/dec2000/chapman-paper.pdf (Roderick Chapman, 'Industrial Experience with SPARK', ACM SIGAda Ada Letters Dec 2000; verbatim from PDF)
  - > "Lockheed have reported an 80% saving in the expected budget allocated to MC/DC testing, yet coding proceeded at near normal Ada rates." ... "the code reaching MC/DC testing exhibited an unusually low fault density, reported to be less than one tenth of the expected industry norm for safety critical software" ... "The most important finding was that proof (of both Z and code) was significantly more cost-effective at finding faults than traditional testing activities." ... "These proofs are a static analysis ... and are valid for all input data, offering a qualitative improvement over testing."
- **A single defect class that dynamic investigation could not find (30 person-days of rig testing, failed) was trivially caught by static analysis (1 person-hour) — concrete head-to-head of universal static checking vs scenario-driven debugging.** [primary]
  - https://www.sigada.org/ada_letters/dec2000/chapman-paper.pdf (same Chapman paper; verbatim from PDF)
  - > "In one such case, some 30 person days (and some nights) of rig-based testing were spent failing to find a simple data-flow error in a function. The Examiner detected this problem trivially, needing approximately 1 person-hour of effort to locate and analyse the offending package."
- **Google's Rust-in-Android data shows language-level prevention of a defect class at scale also improved velocity, not just safety: 1000x lower vulnerability density, 4x lower rollback rate, 25% less review time — prevention guarantees compound into trust and speed.** [primary]
  - https://blog.google/security/rust-in-android-move-fast-fix-things/ (Google Security Blog, Nov 2025)
  - > "memory safety vulnerabilities falling below 20% of total vulnerabilities for the first time" ... "a 1000x reduction in memory safety vulnerability density compared to Android's C and C++ code" ... "Rust changes having a 4x lower rollback rate" ... "spending 25% less time in code review"
- **Quantified floor for lightweight static checking: even plain gradual type checking would have caught 15% of real shipped (post-test, post-review) bugs in JavaScript projects — and the authors note this understates the benefit since it excludes bugs caught during private development.** [primary]
  - https://earlbarr.com/publications/typestudy.pdf (Gao, Bird, Barr — 'To Type or Not to Type: Quantifying Detectable Bugs in JavaScript', ICSE 2017; verbatim from PDF)
  - > "Evaluating static type systems against public bugs, which have survived testing and review, is conservative: it understates their effectiveness at detecting bugs during private development..." ... "our central finding is that both static type systems find an important percentage of public bugs: both Flow 0.30 and TypeScript 2.0 successfully detect 15%!"
- **Even in the friction-heavy Dafny study, practitioners reported a UX upside unique to universal checking: automated change-impact analysis enabling aggressive change without fear — a capability per-scenario re-inspection cannot give (you'd have to re-try every scenario yourself).** [primary]
  - https://cseweb.ucsd.edu/~mcoblenz/assets/pdf/OOPSLA_2025_Dafny.pdf (Mugnier, Zhou, Jhala, Coblenz — 'On the Impact of Formal Verification on Software Development', OOPSLA 2025; verbatim from PDF)
  - > "We find that engineering-first developers particularly value the fact that verifiers enhance coding agility by precisely automating change impact analysis, hence enabling aggressive code optimizations without fear of breaking functionality."


### Friction (where provability hurt)

- **Auto-active verification (Dafny-class, SMT-backed) remains sparsely adopted because of effort and an automation 'uncanny valley': when the prover fails, users must reverse-engineer what the verifier knows. Caveat for the decision: this is evidence against SOLVER-BACKED functional verification, not against bounded solver-free checking — Precept's Option 2 is explicitly the latter.** [primary]
  - https://cseweb.ucsd.edu/~mcoblenz/assets/pdf/OOPSLA_2025_Dafny.pdf (OOPSLA 2025 interview study, 14 experienced Dafny users; verbatim from PDF)
  - > "despite the automation and other programmer-friendly features, they remain sparsely used in real-world software development, due to the significant effort required to apply them in practice." ... "dark clouds swiftly appear when the automation inevitably fails, and the user is left to decipher what knowledge the verifier has (and lacks) to provide hints that can guide the verifier to a valid proof. Consequently, the automation provided by these verifiers places them in an uncanny valley in terms of usability, defeating the purpose of the tools..."
- **SMT-backed proof is brittle: verified proofs fail after irrelevant changes (version upgrade, renaming a variable), forcing a 'proof hardening' maintenance phase. This is the canonical argument FOR deterministic solver-free checking — brittleness is a property of heuristic solvers, which Option 2 forgoes by construction.** [primary]
  - https://dafny.org/blog/2023/12/01/avoiding-verification-brittleness/ (Dafny team blog, Dec 2023)
  - > "If you rely too much on automation, you may find that the outcome of verification becomes chaotic." ... "A particular proof goal may fail to verify after seemingly unrelated changes such as upgrading to a new version of Dafny, adding an unrelated definition to your development, or even changing the name of a variable."
- **Compile-time-enforced guarantees impose a real learning-curve cost paid up front: the most serious barrier to Rust adoption was the paradigm shift the guarantees demand — though participants judged the negatives outweighed by the positives. Relevant ceiling check: Rust's ownership discipline is far more invasive than rule/range checking on a small DSL.** [primary]
  - https://www.cs.umd.edu/~mwh/papers/rust-adoption.pdf (Fulton, Chan, Votipka, Hicks, Mazurek — SOUPS 2021, n=16 interviews + n=178 survey; verbatim from PDF)
  - > "participants also noted key drawbacks that can inhibit adoption, most seriously a steep learning curve to adjust to the paradigms that enforce security guarantees." ... "we find that much of the cost of adoption occurs up front, while benefits tend to accrue later and with more uncertainty" ... "For our participants, these negatives, while important, were generally outweighed by the positive aspects of the language."
- **Static guarantees bolted on after authoring fail: the one unsuccessful SPARK project wrote code first and tried to make it verifiable later, and the project missed its requirements. For Precept this cuts FOR deciding now (pre-release, zero users) rather than retrofitting checking onto an established language.** [primary]
  - https://www.sigada.org/ada_letters/dec2000/chapman-paper.pdf (Chapman, SIGAda 2000, § 'A Less Successful Project'; verbatim from PDF)
  - > "Retrospective 'SPARKification' of code is ill-advised, and often leads to significant difficulty." ... "actual code changes had to be implemented late in the project—these were unfortunately seen as 'distortions' of the original design. Progress slowed significantly at this stage, and the integrated system did not meet its requirements."
- **Even AWS, the strongest industrial advocate, is explicit that universal checking has hard scope limits — emergent/quantitative behavior stays outside the provable fragment. Honest boundary: an Option 2 claim must be scoped to structural/logical properties, mirroring this limit.** [primary]
  - https://lamport.azurewebsites.net/tla/formal-methods-amazon.pdf (AWS formal methods paper, § 'What Formal Specification Is Not Good For'; verbatim from PDF)
  - > "problems in the second category can cripple a system even though no logic bug is involved." ... "We don't yet know of a feasible way to model a real system that would enable tools to predict such emergent behavior. We use other techniques to mitigate those risks."
- **Full functional verification costs are heavy in absolute terms: formally verifying the Ethereum 2.0 Beacon Chain in Dafny took ~16 person-months (per the paper's own accounting). Scale anchor for what Option 2 must NOT become — this figure is for full functional correctness proofs, the far end of the spectrum.** [secondary]
  - https://arxiv.org/pdf/2110.12909 ('Formal Verification of the Ethereum 2.0 Beacon Chain'; figure via search-result synthesis, not page-verified verbatim)
  - > Reported in the paper: net verification effort ~16 person-months — ~6 person-months translating the reference implementation into Dafny, ~10 person-months synthesizing functional specifications including proofs.

**Leg summary:** External evidence run for the Option 1 (fault floor + live inspector) vs Option 2 (compile-time checking as defining niche) decision. Method: web search fan-out, then primary-source verification — all key quotes were extracted verbatim from fetched PDFs/pages I read directly (one sub-model hallucination in the Mendling figures was caught and corrected against the PDF: the real claim is 'at least 34 of ~600 EPCs contain errors, a lower bound', not 76%).

PAIN — confirmed real. Stranded instances: a Camunda engine bug leaves instances permanently stuck after incident resolution (primary bug report). Migration-broken entities: a Temporal user's unversioned definition change hung in-flight workflows; Temporal staff's remedy is terminate-and-restart and they 'admittedly don't have a good' way to even enumerate affected instances (primary forum thread). Stuck workflows burn resources and don't respond to queries (expert practitioner). Late-discovered structural flaws: at least 34 errors shipped in SAP's own ~600-model best-practice reference collection, a stated lower bound (peer-reviewed). Rule-conflict corruption pain is asserted mainly by vendor content (tertiary) — weakest leg.

VALUE — strong and directly on the bar's clause (d). AWS: testing/review are 'inadequate' because the state space is astronomical; model checking found bugs 'we are sure we would not have found by other means' (35-step trace); 'reactive mechanisms cannot recover from the class of bugs that cause permanent damage to customer data; instead, we must prevent such bugs'; the universal 'what needs to go right' framing beat scenario-by-scenario 'what might go wrong' — the cleanest external articulation of why per-scenario inspection (Option 1's paradigm) structurally differs from universal claims. SPARK/C130J: 80% MC/DC test-budget saving, <1/10 industry fault density, proofs 'valid for all input data'; 30 person-days of testing failed to find what static analysis found in 1 hour. Rust/Android: 1000x vulnerability-density reduction plus velocity gains (4x lower rollback, 25% less review). Even plain type checking catches 15% of shipped bugs (ICSE 2017, stated as conservative).

FRICTION — real but concentrated at machinery heavier than Option 2's ambit. The documented failures are SMT-backed auto-active verification (Dafny: sparse adoption, 'uncanny valley', proofs breaking on a variable rename, ~16 person-months for full functional verification) and invasive type disciplines (Rust: steep learning curve as the top adoption barrier, costs up front / benefits later — though participants judged net positive). Two friction findings actually argue FOR Precept's specific posture: brittleness is a property of heuristic solvers (Option 2 is solver-free and deterministic by construction), and the failed SPARK project shows retrofitting checking late is the expensive path — Precept is pre-release with a language that is not closed. AWS also documents the honest scope limit: emergent/quantitative behavior stays outside any provable fragment, so an Option 2 claim must be scoped to structural/logical properties.

Net read (evidence, not the decision): the universal-vs-per-scenario value distinction has strong primary industrial support; the severe friction evidence attaches to solver-backed functional verification and whole-program ownership typing, with no located evidence that bounded, deterministic, domain-specific checking of the kind Option 2 contemplates produced adoption-killing friction — but note the absence of direct comparators for 'solver-free DSL-scale checking' means that gap is unmeasured rather than cleared.

---

## Leg 4 — UX unlock (beyond the inspection paradigm)


### The current inspector paradigm and its structural limits

The designed UX (design/system/semantic-visual-system-manifest.md; design/prototypes/inspector-preview-v1.html) is a live round-trip sampling paradigm: a webview holds a real entity instance behind a host evaluator and, on every keystroke, debounce-fires Inspect requests — 30ms debounce per event-arg edit (inspectEventOnServer, line ~996), 140ms per draft field edit (scheduleDraftInspect, line ~3115), one 'inspect' request PER EVENT per refresh (refreshCurrentEventStatusesFromInspect). It promises a lot and honestly: per the manifest, surfaces are "a faithful projection over runtime truth"; events read as attempts with inspected outcomes (available/blocked/incomplete/rejected with visible because-text), direct edits show hypothetical-patch verdicts inline, and §3A.6 guarantees the inspection answer matches what execution would do. STRUCTURAL limits, all visible in the artifacts themselves: (1) PER-SCENARIO — it evaluates exactly the (event, args) or patch asked, against the current instance's configuration; it can never state anything about all inputs, all states, or all time. (2) ROUND-TRIP TO A LIVE EVALUATOR — needs the host online; the prototype carries the scar tissue of this architecture: stale-response sequence tokens (latestDraftInspectSequence, eventStatusRefreshToken), focus/caret-preservation hacks across re-render, and a probe-args workaround ("Re-inspect with probe args so event rules on required string args are evaluated (the snapshot aggregate Inspect uses no args and skips event rule checks)" — lines 1551–53, 2834–36): concrete values must be synthesized just to get answers. (3) NEEDS AN INSTANCE — InspectUpdate returns event prospects only "for every event defined in the current state" (runtime-api.md §InspectUpdate); to learn how the entity behaves three states away you must drive a real instance there. (4) ANSWERS ONLY THE QUESTION ASKED — pass/fail for the values typed; finding what WOULD pass is manual binary search. (5) The manifest itself names the gaps: past-tense timeline needs receipts the runtime can't substantiate, and the data form needs "field-level requiredness as a surface semantic rather than an inferred styling choice" and "a fuller stable contract to distinguish editable, locked, invalid, conditionally editable…" — i.e., the current paradigm's own open questions are requests for a statically derived contract.

### [transformative] Statically derived form contracts (offline / client-side / instance-free)

- **What:** Compile each state into an exportable per-field contract — type, effective bounds (declared modifiers folded with unguarded rules and in-state ensures via the existing interval machinery), requiredness, editability, choice vocabularies, because-text — as JSON Schema / TypeScript / host artifacts. End-user forms validate every keystroke locally and offline with zero evaluator round-trip; guarded/relational residue is explicitly marked 'needs evaluation' so the live Inspect becomes the exception path, not the per-keystroke path. This is exactly what the manifest's data-form open questions are asking for ('requiredness as a surface semantic rather than an inferred styling choice').
- **Who benefits:** End users (instant, flicker-free, offline feedback); hosts (no evaluator round-trip per keystroke, client-side and edge deployment, less server load); the design system itself (a public field-status truth model it says it lacks).
- **Why the inspector cannot:** Structurally requires a live instance plus host round-trip per keystroke (30/140ms debounce, sequence-token race handling in inspector-preview-v1.html); cannot run where the evaluator isn't — client-only, offline, static docs, third-party surfaces.
- **Compile-time power needed:** Per-state effective-constraint projection: fold field modifiers + unguarded rules + state-resident ensures into per-field intervals/flags, plus an export surface. The interval/qualifier machinery and precomputed descriptor tiers already exist (runtime-api.md Constraint Exposure Model tiers 1–2 are 'Zero — precomputed'; precept_compile already returns structured proofObligations with disposition+strategy, verified live). Honesty rule: anything not statically decidable is exported as 'evaluator-required', never silently dropped.
- **Language change needed:** none
- **External precedent:** react-jsonschema-form: "A simple React component capable of building HTML forms out of a JSON schema." — https://rjsf-team.github.io/react-jsonschema-form/docs/ . An entire mainstream ecosystem (rjsf, AJV, JSON Forms) exists to derive validating UIs from a static schema; Precept's compiled contract is strictly richer (proof-backed bounds, mandatory rationale text, lifecycle-scoped editability).

### [significant] Counterfactual refusal guidance computed from proofs

- **What:** Refusals and blocked affordances carry the boundary that flips the verdict — 'raise Score to at least 680 to proceed', 'Amount must be at most half of ClaimAmount (62,500)' — computed from the effective interval/relational representation (the intersection of all applicable rules, ensures, and guards), not just the authored because-text.
- **Who benefits:** End users (actionable next step instead of a verdict); support/ops staff resolving stuck cases; agents consuming structured proof data.
- **Why the inspector cannot:** The inspector evaluates only the scenario asked — pass/fail for the values typed; discovering the threshold means the user binary-searches by retyping. Reading a single rule's source text isn't sufficient either when several constraints compose: the effective bound is the intersection across rules + state ensures + relational rules, which only the proof representation knows.
- **Compile-time power needed:** The interval + relational (octagon-style) representation already built for the fault floor, plus a query surface: 'minimal satisfying boundary per referenced field for this violated constraint set.' Spec §0.6 #13 and proof-philosophy #6 already mandate structured proof attribution ('proof results flow as structured data, not parsed prose') — this is a consumer of that commitment.
- **Language change needed:** none
- **External precedent:** Wachter, Mittelstadt & Russell, 'Counterfactual Explanations without Opening the Black Box' (Harvard JOLT 31; arXiv:1711.00399): "You were denied a loan because your annual income was £30,000. If your income had been £45,000, you would have been offered a loan." Counterfactuals are the established gold standard for explaining automated decisions — and Precept can compute them exactly, from declared rules, where ML systems must approximate.

### [significant] Ahead-of-time operation-availability map (state × event, no instance)

- **What:** For every (state, event) pair, classify statically: impossible (reject-only, dead guard), always available, or conditional with the governing condition extracted — derived from the definition alone. UI surfaces, docs, and host code render availability and its 'why' before any entity exists, and end-user surfaces precompute affordance maps instead of fanning out one Inspect call per event per render.
- **Who benefits:** End users (instant affordances with no round-trip flicker); domain-expert authors (a per-state behavior table while authoring); hosts and AI agents (plan multi-step flows against the map without instantiating entities).
- **Why the inspector cannot:** Needs an instance and answers only for its current state — runtime-api.md: InspectUpdate returns prospects 'for every event defined in the current state'. To see how Approve behaves from Appealed, you must drive a real entity there with satisfying data. The prototype's refreshCurrentEventStatusesFromInspect already issues one round-trip per event on every refresh — the AOT map removes that entire class of traffic.
- **Compile-time power needed:** Graph-analyzer outcome classification per (state,event) — live today (verified: PRE0080 unreachable, PRE0108 'no path to any terminal state', reject-only classification per §0.5 item 6) — plus dead/tautological guard detection (§0.6 items 9–10, currently specification-only, Phase 5) and guard-condition extraction for the 'conditional' cells.
- **Language change needed:** none
- **External precedent:** GraphQL introspection (Hasura GraphQL tutorial): "the Introspection feature allows you to query the schema and discover the available queries, mutations, subscriptions, types and fields in a specific GraphQL API" and "GraphiQL and GraphQL Playground … leverages the Introspection feature to provide self-documentation to developers and try out APIs quickly." — https://hasura.io/learn/graphql/intro-graphql/introspection/ . A static capability map is what made GraphQL's tooling ecosystem possible. Stately.ai shows the same for statecharts (build/visualize machines from the definition, no running app — https://stately.ai/).

### [significant] Whole-landscape authoring verdicts as you type (instance-free)

- **What:** The author sees, for all states at once and live in the editor: contradictory rule pairs, vacuous (always-true) rules, dead guards, unsatisfiable guard combinations, and routing sharpened by proven-dead guards — without creating an instance or steering it into each state with hand-crafted satisfying data.
- **Who benefits:** Domain-expert authors (the primary audience per §0.8 — they get verdicts in domain vocabulary while typing, not after runtime experimentation); reviewers auditing a definition.
- **Why the inspector cannot:** The inspector samples one concrete configuration at a time. It can show that a guard didn't match for THESE values; it structurally cannot show 'no data can ever satisfy this guard' or 'these two rules admit no common value' — those are universal claims over all inputs.
- **Compile-time power needed:** Spec §0.6 items 7, 8, 9, 10, 12 — contradiction detection, vacuity detection, dead-guard, tautological-guard, sharpened routing. All currently specification-only (Phase 5, per the spec's own implementation-status table); all solver-free interval/relational closure over the machinery the fault floor already built.
- **Language change needed:** none
- **External precedent:** Dafny IDE (Leino & Wüstholz, arXiv:1404.6602): the IDE integrates "verification feedback as the user types and can present more helpful information" — the verifier runs continuously in the background at design time. Proves that as-you-type proof feedback is a viable, valued authoring paradigm, not a batch step.

### [significant] Guard-aware liveness certificate — end-user trust surface ('this process cannot get stuck')

- **What:** Extend the shipped structural reachability/dead-end proofs into a publishable certificate: every admitted entity can reach completion; every declared state is genuinely inhabitable. Surfaced to end users as a verifiable trust claim on forms/flows ('this form cannot reach an invalid or stuck configuration'), and to compliance as an audit artifact. The deeper tier adds data-awareness: a state whose every outgoing guard is unsatisfiable under that state's resident ensures is a data-dependent dead end the structural pass (deliberately, per §0.5's overapproximation rule) cannot see.
- **Who benefits:** End users and customers (trust in the surface itself); compliance/audit (a certificate, not a test report); product positioning (the philosophy's 'business processes cannot get stuck' claim becomes a user-visible artifact rather than an internal property).
- **Why the inspector cannot:** This is a universal claim over all instances, all data, all time. The inspector can only ever show that one instance, with one set of values, has a next step right now.
- **Compile-time power needed:** Structural half is shipped (verified live: PRE0108 'State Draft has no path to any terminal state — entities that enter it can never complete their lifecycle'; PRE0080 unreachability; dominator analysis for required states per §0.5). The data-aware half needs guard-interval narrowing under in-state ensures — the same interval machinery, applied per (state,event) — kept honest by conservative classification.
- **Language change needed:** None for the structural certificate. The data-aware tier may want the roadmap's declarative liveness modifier family (graph-analyzer-roadmap: advancing/settling/completing) — kind of change: author-declared progress assertions the analyzer verifies, already on the deferred roadmap.
- **External precedent:** Elm's 'make impossible states impossible' (Feldman; elm-patterns): "Elm has a great and expressible type system. This type system allows us to avoid having impossible states in our application." — https://sporto.github.io/elm-patterns/basic/impossible-states.html . Compile-time impossibility of bad states is a celebrated, community-defining capability — Precept extends it from data shapes to whole business-process liveness.

### [significant] Generated host-API contracts carrying the proofs

- **What:** Generate typed C#/TypeScript/OpenAPI artifacts from the compiled definition: per-event arg signatures with carried constraints (Amount: positive money in USD), per-state editability contracts, and outcome unions with structurally impossible variants removed — runtime-api.md today hand-writes comments like '// Transitioned — structurally impossible (construction is terminal in initial state)'; generation makes the host compiler enforce what those comments assert. Host-side integration mistakes become host build errors instead of runtime InvalidArgs/UndefinedEvent outcomes.
- **Who benefits:** Integrating developers (the §0.8 second audience) — exhaustive, narrowed pattern matches and typed arg builders; downstream API consumers who receive constraint-carrying schemas.
- **Why the inspector cannot:** The inspector is a runtime, per-instance surface. A contract is a compile artifact consumed by another compiler — the inspector cannot make a host's build fail or shape its types.
- **Compile-time power needed:** Mostly projection of designed/precomputed metadata: FieldDescriptor.ClrType / ArgDescriptor (precomputed at Precept Builder time), ConstraintDescriptor catalog (tier 1, zero-cost), construction outcome-space narrowing facts (already compiler-guaranteed: Unmatched/UndefinedEvent impossible from Create per runtime-api.md). Needs emitters, not new proof power; richer narrowing (per-state outcome elimination) reuses graph + guard analysis.
- **Language change needed:** none
- **External precedent:** OpenAPI Generator: "OpenAPI Generator allows generation of API client libraries (SDK generation), server stubs, documentation and configuration automatically given an OpenAPI Spec (v2, v3)" — https://github.com/OpenAPITools/openapi-generator . Contract-derived codegen is table stakes in API ecosystems; Precept's generated contracts additionally carry proof-backed constraints and impossibility facts no OpenAPI spec can express.


### Paradigm verdict

Yes — with a precise shape. Strong compile-time checking enables a genuinely better default paradigm: PROJECT THE CONTRACT, DON'T PROBE THE INSTANCE. The debounced inspector is a sampling paradigm — concrete and honest (§3A.6's inspect-equals-execute guarantee is real and valuable), but structurally per-scenario, instance-bound, online-only, and quadratic in UI fan-out (the prototype fires one inspect per event per refresh and carries stale-response tokens, focus-preservation hacks, and probe-arg synthesis as the cost of that architecture). Compile-time projection inverts the default: the statically decidable majority of surface truth — field types, effective bounds, requiredness, editability, choices, availability classification, dead-end-freedom — ships as a precomputed contract that works offline, instance-free, and universally, while the live evaluator round-trip is reserved for two things only it can do: simulating concrete outcomes on a real entity's actual values, and evaluating the guarded/relational residue the static contract honestly marks evaluator-required. So this is static-first with a live-inspect escape hatch, not a replacement of Inspect. Three honesty notes for the decision: (1) the strongest single unlock (static form contracts) and the host-contract unlock lean mostly on descriptors plus interval machinery the fault floor already built — they are cheap projections, which cuts BOTH ways: they don't require Option 2's full ambition, but they also show Option 1's own designed UX already straining toward static contracts (the manifest's data-form open questions literally request one). (2) The unlocks that genuinely need option-2-grade power are the Phase-5 proof obligations (contradiction/vacuity/dead-guard — enabling instance-free authoring verdicts and the availability map's 'impossible' cells), counterfactual boundary extraction, and guard-aware liveness — all solver-free extensions of existing interval/relational/graph machinery, none requiring expressiveness loss, and effectively none requiring language changes (one optional modifier family is already on the graph-analyzer roadmap). (3) Nothing here argues the inspector away: the per-scenario simulator remains the right tool for 'what exactly happens to THIS entity' — the paradigm question is what runs on every keystroke for every end user, and there the static contract is structurally superior.

**Leg summary:** Evidence for the UX-unlock leg of the Option 1 vs Option 2 decision. Grounded in: semantic-visual-system-manifest.md + notes (read in full), inspector-preview-v1.html (round-trip mechanics: 30ms/140ms debounces, per-event inspect fan-out, sequence-token races, probe-arg workaround), philosophy.md, spec §0.1/0.4–0.8/3A, runtime-api.md (InspectFire/InspectUpdate, three-tier constraint exposure, descriptors), and live precept_compile verification (PRE0083 divisor obligation with named cause, PRE0080/PRE0108 universal graph proofs, structured proofObligations with disposition+strategy). Six unlocks derived, each meeting the bar's universality-or-structural test: (1) statically derived offline form contracts [transformative — and the manifest's own open questions request exactly this]; (2) counterfactual refusal guidance from interval proofs; (3) AOT state×event availability maps; (4) instance-free whole-landscape authoring verdicts (Phase-5 obligations); (5) guard-aware liveness certificates as an end-user trust surface; (6) generated host contracts carrying proofs. All solver-free; five of six need no language change (the sixth optionally wants the already-roadmapped liveness modifier family). External precedent with verbatim excerpts: rjsf (schema-derived forms), Wachter et al. (counterfactual explanations), GraphQL introspection tooling, Dafny as-you-type verification, Elm impossible-states, OpenAPI Generator. Verdict: compile-time checking enables a static-first paradigm (project the contract, don't probe the instance) that is structurally superior for the per-keystroke/end-user path, with the live inspector retained as the concrete-simulation escape hatch. Caveat preserved for the owner: the cheapest unlocks are projections of machinery the Option-1 fault floor already builds, so the UX leg alone supports a middle reading as well as the all-in reading; the proof-dependent unlocks (2,3-impossible-cells,4,5) are where Option 2 specifically earns differentiation.

---

## Leg 5 — Real-world demand (independent of today's surface)

### Decision-band completeness and non-overlap — for any input combination, exactly one rule/row matches (no gaps where nothing fires, no conflicting double-matches) (Lending (credit decisioning), insurance pricing/eligibility, claims routing — any guarded decision logic)

- **Demand evidence:** Calvanese et al., 'Semantics and Analysis of DMN Decision Tables' (arXiv:1603.07466, BPM 2016): 'The increasing use of DMN decision tables to capture critical business knowledge raises the need to support analysis tasks on these tables such as correctness and completeness checking' — providing 'scalable algorithms to tackle two such tasks, i.e., detection of overlapping rules and of missing rules', validated 'on decision tables derived from real credit lending datasets.' Commercial tooling ships this: Sparx Enterprise Architect DMN validation detects gaps ('given a combination of input values, no rule is matched') and overlaps ('multiple rules are matched, which is a violation if the Decision table specifies Unique as its Hit Policy').
- **Expressible today:** partially · **Growth implied:** No new authoring surface needed — guarded rules and guarded transition rows already express the bands. The growth is compiler-side: exhaustiveness/overlap analysis over a guard family per (state, event) using interval geometry (the hyper-rectangle method in the paper is solver-free interval math, directly compatible with the proof engine's existing numeric-interval machinery). Today's spec has only dead-guard detection (spec-only, F-LANG-SPEC-02) and reject-only-pair classification — no gap/overlap proof.
- **Compile-time value:** HIGH — this is the strongest demand-side case for Option 2. The defect is a universal claim ('some input falls through / double-matches'), which the per-scenario inspector structurally cannot find: the author only discovers a gap if they happen to try a value inside it. Academic and commercial DMN ecosystems both invested in exactly this check against real credit-lending tables, which is direct evidence the defect class is real and worth static detection. Solver-free feasibility is demonstrated by the geometric approach.

### Tiered approval thresholds (delegation-of-authority bands) — who may approve what amount, with escalation above each band (Procurement, accounts payable, treasury, corporate finance — near-universal)

- **Demand evidence:** Ken from Finance (DoA matrix guide, kenfromfinance.com): a matrix 'specifies who can approve which spend, up to what dollar limit, with what supporting documentation', with size-scaled tiers ('$5M companies need 3–4 tiers; $50M needs 5–6'); and 'Nearly 90 percent of companies have one, but most fail at enforcement because the matrix sits in a spreadsheet nobody checks. The fix is embedding rules into live workflow systems.' Tallyfy approval-limits-matrix template corroborates banded thresholds (≤$50k, $50k–$250k, $250k–$1m, >$1m).
- **Expressible today:** yes · **Growth implied:** Expressible now via guarded transition rows on an amount field. Growth is the same compiler-side band analysis as the decision-table pattern: prove the bands tile the amount domain with no gap (e.g., rows cover <=10000 and >=25000 but nothing covers the middle) and no ambiguous overlap. An actor/role concept would deepen the 'who' axis but is not required for the threshold logic itself (sketch only).
- **Compile-time value:** HIGH — a band gap means a legitimate amount has no approval path (operationally: a stuck request), and the '90% have one, most fail at enforcement' evidence says the demand is for structural enforcement, exactly Precept's pitch. Gap/overlap over one numeric dimension is the easiest possible interval proof. Universal claim (all amounts route) vs the inspector's one-amount-at-a-time check.

### Four-eyes / segregation of duties — maker ≠ checker; the initiator of a sensitive operation cannot be its approver (Banking/treasury payments, procurement, accounting close, regulated release processes)

- **Demand evidence:** Association of Corporate Treasurers (treasurers.org, 'Segregation of duties' masterclass): 'no employee should be in a position both to commit and conceal fraud or errors in the usual course of their duties (sometimes referred to as the duality or four eyes principle)'; 'Four-Eyes approval protocols are implemented within banking and procurement software for all transactions above a defined financial threshold.' XTRM maker-checker guide: 'One person, the maker, prepares or initiates a request, and a second person, the checker, reviews and approves or rejects it', with a 'tamper-proof audit trail of every approval step.'
- **Expressible today:** yes · **Growth implied:** Expressible now: SubmittedBy/ApprovedBy fields plus `rule ApprovedBy != SubmittedBy when ApprovedBy is set` (string inequality is live in samples, e.g. currency-exchange-rates.precept), with the threshold trigger as a guard. The audit-trail half maps to the existing `log of string` append-only type. Growth (sketch only): a first-class actor/identity concept would let the compiler reason about roles rather than string equality.
- **Compile-time value:** MODERATE — the rule itself is runtime governance (the identities are runtime data; nothing universal to prove about their values). The compile-time contribution is path-shaped: prove via the existing dominator/required-state analysis that no path to the committed state bypasses the approval step, and that the maker≠checker rule actually applies on every such path. That is a universal all-paths claim the inspector cannot make, but the marginal value over governance + graph analyzer (largely shipped) is incremental, not category-defining.

### Deadline / SLA escalation — a decision or task must complete within a bound, with automatic escalation on breach (Healthcare prior authorization (regulatory), ITSM/helpdesk, claims handling, BPM generally)

- **Demand evidence:** CMS Interoperability and Prior Authorization Final Rule CMS-0057-F (cms.gov fact sheet): impacted payers must 'send prior authorization decisions within 72 hours for expedited (i.e., urgent) requests and seven calendar days for standard (i.e., non-urgent) requests' — a federally mandated per-instance deadline. Kissflow SLA-in-BPM guide: an SLA 'defines how long each step of a process should take, what escalation happens when that threshold is breached, and who is accountable'; 'when the threshold is breached, an escalation triggers' — the workflow itself is the enforcement mechanism.
- **Expressible today:** partially · **Growth implied:** Deadline FIELDS and rules are expressible (duration/instant types, now(), e.g. insurance-claim-adjudication's ReviewSLA/TargetCompletion fields), but enforcement only fires when some operation arrives — Precept has no autonomous time-based trigger, so the host must poll-and-fire an Escalate event. Growth (sketch): a declared deadline/timeout surface on states (compiled to a host-driven clock contract), making 'in UnderReview for more than ReviewSLA' a first-class guard.
- **Compile-time value:** MODERATE-HIGH if the surface exists — the valuable universal claims are structural: every state carrying a deadline has a defined escalation transition (no 'overdue with nowhere to go'), deadline arithmetic is fault-free, and escalation targets are reachable/non-dead-end. The deadline FIRING itself is inherently runtime. Without the timer surface this pattern is a host-integration story under either option; the regulatory evidence (CMS 72h/7d) shows the demand is real and compliance-grade, not cosmetic.

### Aggregate cap with erosion — a running total (payments, claims, spend) consumes a declared limit pool and must never exceed it; per-occurrence and aggregate limits compose (Insurance (general aggregate vs per-occurrence limits), budgets/spend control, benefits maximums)

- **Demand evidence:** MoneyGeek per-occurrence-vs-aggregate guide (and Hartford glossary): 'The per-occurrence limit caps what the insurer pays for a single covered incident, while the general aggregate caps what the insurer pays for all covered incidents combined during the policy period'; 'if your policy has a $2 million general aggregate limit and you have $1.5 million in paid claims midyear, there is only $500,000 left for additional covered claims.' Carriers 'track erosion by monitoring the remaining aggregate after each paid claim.'
- **Expressible today:** partially · **Growth implied:** A scalar running-total field plus `rule PaidToDate <= AggregateLimit` and increment actions is expressible today, and the per-occurrence cap is a guard on the event argument. What is NOT expressible is the total as a derivation over a collection of payment records — the builtin catalog (§3.7) has min/max/abs/clamp/etc. but NO sum/aggregate over collections. Growth (sketch): aggregate accessors over collection fields (sum/total), the exact capability Drools ships as accumulate ('Drools ships with several accumulate functions... like sum, average, min, max, count' — docs.drools.org) and that rule authors demonstrably use for credit-limit-style rules.
- **Compile-time value:** HIGH — the universal claim is 'no sequence of operations can push the total over the cap,' which the proof engine's interval reasoning over increment chains can discharge or reject (it already does count-interval reasoning for collection cardinality, PRE0136). The inspector can only show one firing at a time; cap erosion bugs are cumulative across operations, precisely the all-sequences claim. Collection-sum support would also make the total derived-by-construction rather than a hand-maintained counter that can drift.

### Balance / conservation identities — derived totals must equal the sum of their parts (debits = credits; line items sum to subtotal; allocations sum to allocated total) (Accounting/ledgers, invoicing/e-invoicing, claims financial breakdowns, inventory reconciliation)

- **Demand evidence:** Wall Street Prep (double-entry): 'The total debits and total credits must balance at all times under double-entry accounting'; Xero: 'if they don't balance, you know that you've made a mistake somewhere in the ledgers.' SAP KB 3614763 documents the production failure mode verbatim: 'Unable to Post Supplier Invoice due to Error — Item 1: Sum of items does not match total value in amount.' Invoice-validation guides list 'the total amount doesn't match the sum of the line items' as a standard catch.
- **Expressible today:** partially · **Growth implied:** Scalar identities are fully expressible and already idiomatic — invoice-line-item.precept derives `Subtotal <- UnitPrice * Quantity`, `LineTotal <- TaxableAmount + TaxAmount` as computed fields, making violation structurally impossible rather than checked. The gap is identities over collections (N line items summing to a header total) — blocked by the same missing collection aggregation as the cap pattern. Growth: sum over collection fields feeding computed fields.
- **Compile-time value:** HIGH, and partly already delivered — computed fields are the strongest form of compile-time value: the identity holds by construction, no input can break it, which is categorically beyond what per-scenario inspection or even runtime checking offers (a checked identity can be violated and refused; a derived identity cannot be violated at all). SAP shipping a dedicated error code for the sum-mismatch failure is evidence the defect class is common enough to name. Extending derivation across collections completes the pattern.

### Partial fulfillment — cumulative delivered/allocated/registered quantity must never exceed the ordered quantity across any number of partial operations (Logistics, warehousing, ERP order management, manufacturing)

- **Demand evidence:** SAP Help (Partial Delivery and Complete Delivery): 'you can enter as many partial deliveries as you wish for an order item until the maximum number permitted is reached'; SAP Community: 'Delivery quantity should not exceed order or confirm quantity.' Microsoft ships dedicated error surfaces for the violation: Dynamics NAV 'The total item tracking quantity [total quantity] exceeds the line quantity'; Dynamics 365 'You can't confirm a shipment because the quantity exceeds the overdelivery percentage.'
- **Expressible today:** yes · **Growth implied:** Expressible now with a cumulative counter: `set ShippedQty = ShippedQty + Ship.Qty` guarded by `when Ship.Qty <= OrderedQty - ShippedQty`, plus `rule ShippedQty <= OrderedQty`. Growth is quality-of-life, not capability: collection-sum aggregation (per-shipment records deriving the cumulative total) and the overdelivery-tolerance variant (cap = ordered × (1 + tolerance%)) are both within existing types.
- **Compile-time value:** MODERATE-HIGH — the universal claim 'no event sequence overshoots the ordered quantity' is exactly interval reasoning over increment chains, the proof engine's home turf; the compiler can prove the guard is sufficient or demand it. Two major ERP vendors shipping named error codes for this defect class proves it occurs in production at scale. Note the cap is runtime-data-relative, so the compile-time proof is about guard sufficiency (structural), with governance enforcing the carried constraint — the existing §0.7 composition pattern.

### Retroactive adjustments / effective-dated records — changes applied with a valid-from date in the past (backdated endorsements, retro pay), with history preserved and a bounded retro window (Insurance (endorsements, claims-made retro dates), payroll/HR, benefits eligibility)

- **Demand evidence:** Inscipher (E&S endorsements): 'An insurance endorsement is any change to the original policy made after the policy effective date, and if necessary, endorsements may be backdated.' SAP payroll (guru99 PA03 tutorial): the control record defines 'the date up to which the system carries out retroactive accounting, commonly referred to as the retro wall'; Oracle HCM: 'When processing retroactive changes, the system never overwrites historical payroll data; instead, it creates one or more retroactive entries.' US patent 11935046 defines the retroactive transaction: 'a valid from time that is prior to a created at time' (bitemporal).
- **Expressible today:** no · **Growth implied:** LARGE — Precept governs the current configuration only (§0.2: 'current field data alone'); there is no valid-time dimension, no history model, no as-of evaluation. Growth sketch: effective-dated field values or an as-of inspection axis — a genuinely heavy lift that touches the configuration model itself. Date-ORDERING invariants (RetroDate <= EffectiveDate <= ExpiryDate) are expressible today; the reconstruction-of-past-state semantics are not.
- **Compile-time value:** This is primarily an EXPRESSIVENESS gap, not a compile-time-checking gap — it does not differentiate Option 1 from Option 2 (neither option hosts it today). Recorded for honesty: real demand exists in two large verticals Precept explicitly targets (insurance, payroll-adjacent HR). If the surface ever existed, retro-wall bounds and date-ordering would be cheap interval proofs; until then the pattern argues for roadmap awareness, not for either option.

### Unit-of-measure integrity — quantities carry units; mismatched or misconverted units are errors, not silent scaling (Healthcare dosing, manufacturing, logistics, engineering, energy)

- **Demand evidence:** Nurse.com mcg-to-mg guide: 'Confusing a 1 mg dose with a 1 mcg dose would result in a 1,000-fold overdose'; documented incidents include 'an infant weighing 3 kg received a 36-mcg bolus dose (12 mcg/kg) of fentanyl instead of a 12-mcg dose (4 mcg/kg)' and a propofol order 'administered at 80 mcg/kg/minute' instead of per-hour (PMC3046620 and unit-conversion safety literature: 'Errors in unit conversion have been documented as a contributing factor in serious medication mistakes, some of which have resulted in patient harm').
- **Expressible today:** yes · **Growth implied:** None for the core — `quantity ... in '<unit>'` with UCUM-normalized compile-time interval comparison is shipped (spec §0.6 item 5: cross-unit bounds are normalized 'via UCUM scale factors before interval containment comparison'). Remaining edge: counting-unit qualifiers ('each' vs 'box') are explicitly excluded and need matching qualifiers or a conversion field — a small future refinement.
- **Compile-time value:** HIGH and ALREADY VALIDATED — this is external confirmation that an existing compile-time investment targets a defect class with documented severe harm. The claim is universal (every unit conversion in the definition is dimensionally correct for all values), which scenario-based inspection cannot establish: a unit error scales ALL values uniformly, so a plausible-looking inspector output does not reveal it unless the author independently knows the right magnitude. Strong evidence that compile-time checking, where Precept already does it, is the right mechanism for this class.

### Process soundness — no stuck/dead-end states, every state reachable, no approval routing that reaches a position with no way forward (ERP/BPM generally; procurement, invoicing, ticketing — anything with a lifecycle)

- **Demand evidence:** Can!do via Bizcommunity ('Why ERP workflows get stuck'): 'Stuck workflows are one of the most common frustrations organisations experience in the months following an ERP go-live. Purchase orders sit pending. Invoices await authorisation that never arrives'; 'A request that should take hours can sit for days... because the routing has reached a dead end.' Atlassian and BMC publish dedicated KB articles for stuck status transitions, evidence the failure mode is common enough to document.
- **Expressible today:** yes · **Growth implied:** Structural reachability, dead-end detection, required-state dominator analysis, and irreversibility are shipped (§0.5). The growth is precision, not surface: guard-AWARE soundness — a structurally-present transition whose guard is provably unsatisfiable given field constraints is a semantic dead end the current overapproximation misses (§0.5 treats all edges as traversable). That is the §0.6 dead-guard obligation (spec-only) feeding back into graph diagnostics (item 12, also spec-only).
- **Compile-time value:** HIGH — this is the canonical all-paths universal claim: the inspector simulates one state at a time and cannot enumerate path space; the external evidence shows stuck workflows are a top operational failure of exactly the systems Precept replaces. Half the value is already shipped (structural analysis); the demand evidence argues for completing the guard-aware half, which is precisely the spec-only proof-engine items 9/10/12 — i.e., demand supports finishing planned work rather than inventing new surface.

### Rule-base consistency — no two rules that contradict (unsatisfiable together), no rules made vacuous/redundant by other constraints (Any rule-authored system; acute in large rule bases (insurance rating, eligibility) maintained over years)

- **Demand evidence:** Rule-base V&V literature (arXiv cs/0609119, semantic-web rule V&V): anomalies include 'contradicting rules (e.g., rules that conclude both s and ¬s under the same conditions)' and 'typical design errors/anomalies are duplication, inconsistency, or subsumedness'; 'verification techniques involve checking reachability graphs to identify incompleteness, inconsistency, circularity, and redundancy of rule bases.' Practitioner literature (Nected rules-engine design guide): 'two rules firing on the same conditions can produce contradictory actions, which is one of the more common production bugs in rule engine implementations.'
- **Expressible today:** partially · **Growth implied:** No authoring surface needed — rules exist. Contradictory-rule detection and vacuous-rule detection are §0.6 obligations 7 and 8, currently specification-only (F-LANG-SPEC-03/04). The growth is shipping the planned proof-engine analyses; Precept's no-chaining, finite, declarative rule model makes this far cheaper than the general production-rule case (no circularity is even possible — a structural advantage worth claiming).
- **Compile-time value:** HIGH — contradictions are the universal claim 'no value can satisfy both rules,' invisible to scenario inspection (each rule individually refuses plausibly; the author never sees that their intersection is empty until a real entity hits it). The literature classifies this as a common production bug class in rule systems. Differentiating note: a contradiction in Precept is not just a wrong answer — it silently converts a state into a semantic dead end, compounding with the process-soundness pattern.

### Milestone / state-conditional permission — an operation or edit is allowed only while the entity is in (or before) a given lifecycle position, and becomes impossible after (Generic workflow (the Workflow Patterns initiative's state-based class); claims (no edits after adjudication), orders (no changes after picking))

- **Demand evidence:** Workflow Patterns initiative, Milestone pattern WCP18 (van der Aalst et al., via Wikipedia's pattern catalog): 'Allow a certain activity at any time before the milestone is reached, after which the activity can no longer be executed' — one of 20 canonical control-flow patterns, in the State-based category that the initiative notes is poorly supported by classical activity-centric workflow engines (state-based patterns are where entity-state systems differentiate).
- **Expressible today:** yes · **Growth implied:** Core strength today: state-scoped editability (`in X omit/editable`), state-gated transition rows, and state ensures express milestones directly — Precept's entity-state model natively hosts the pattern class that activity-centric BPM engines handle awkwardly. Growth: none required; the irreversible/sealed-after modifier family on the graph-analyzer roadmap deepens it.
- **Compile-time value:** MODERATE — the pattern itself is structural and already enforced; compile-time adds consistency proofs across the permission surface (a field required by an ensure in a state where it is also omitted; an event only routable in states it can never co-occur with — contradictions between the milestone declarations themselves). Value is real but incremental; the demand evidence mostly confirms Precept's model fits a canonical pattern class, supporting the product thesis under either option.

### Cross-instance / cross-entity aggregate limits — a cap over MANY entities (total exposure to one borrower across all loans; concentration limits) (Banking (regulatory lending limits), insurance (per-insured accumulation), procurement (vendor concentration))

- **Demand evidence:** 12 CFR § 32.3 (OCC lending limits, law.cornell.edu): 'a national bank's or savings association's total outstanding loans and extensions of credit to one borrower may not exceed 15 percent of the bank's capital and surplus' — a hard regulatory cap whose subject is an aggregate across many loan entities, not any single one.
- **Expressible today:** no · **Growth implied:** OUT OF CURRENT IDENTITY — §0.1 principle 2 ('one file, complete rules... no cross-file references') and the one-entity configuration model exclude cross-instance visibility by design. Growth would be a fleet-level or portfolio-level construct, a different product layer (sketch only: host-computed aggregate injected as a governed event argument is the honest near-term pattern — the cap check is then expressible today against that supplied figure).
- **Compile-time value:** LOW for the option decision — neither compile-time checking nor the inspector can cover what the language cannot see; this pattern does not differentiate Option 1 from Option 2. Recorded because the demand is regulatory-grade and adjacent to Precept's lending/insurance target domains: the boundary should be documented honestly (what Precept governs is the single entity; portfolio caps live in the host), or it will be discovered as a surprise by exactly the domain experts Precept targets.

**Leg summary:** Demand-side evidence from 20+ external sources (Workflow Patterns initiative, DMN verification literature, Drools docs, CMS regulation, OCC regulation, SAP/Microsoft ERP error surfaces, treasury/SoD practice guides, patient-safety literature) yields 13 requirement patterns. Headline findings for the Option 1 vs Option 2 decision: (1) The strongest Option-2 cases are the UNIVERSAL-CLAIM patterns the per-scenario inspector structurally cannot deliver: decision-band completeness/overlap (academically and commercially validated against real credit-lending tables, solver-free via interval geometry), aggregate-cap erosion safety over operation sequences, rule contradiction detection (a named production-bug class in rule-engine practice), and guard-aware process soundness (stuck workflows documented as a top ERP failure mode). Notably, three of these four map to ALREADY-PLANNED spec-only proof obligations (§0.6 items 7-10, 12) rather than new invention — the demand evidence supports finishing planned work. (2) Two existing compile-time investments are externally validated as targeting severe real defect classes: unit-of-measure integrity (documented 1000x dosing overdoses) and conservation identities via computed fields (SAP ships a dedicated error code for sum-mismatch) — by-construction derivation is stronger than both inspection and runtime checking. (3) The most consequential expressiveness gap demand reveals is COLLECTION AGGREGATION (sum over collections) — it blocks the full form of three high-demand patterns (aggregate caps, balance identities, partial fulfillment from records) and is the Drools-accumulate capability rule authors demonstrably use; it matters under EITHER option. (4) Two demand patterns do not differentiate the options because Precept cannot host them today regardless: bitemporal/retroactive adjustments (big in insurance/payroll) and cross-instance aggregates (regulatory lending limits) — these are identity-boundary findings to document honestly, not compile-time arguments. (5) Deadline/SLA escalation has compliance-grade demand (CMS 72h/7d) but is primarily a runtime/host-timer surface; compile-time adds structural completeness proofs (every deadline has an escalation path) only after such a surface exists. Net: demand evidence neither forces Option 2 nor right-sizes to Option 1 by itself, but it concentrates the genuine Option-2 differentiation in a small, solver-free, largely already-specified set of universal-claim analyses, and shows the corpus's scalar-only aggregation is a real floor effect masking collection-aggregate demand.
