# Hero Sample Deliberation

## The Discussion

**Steinbrenner:** Alright, let's talk about the hero sample. The first thing a developer sees on our README. The TimeMachine is out. I want to know why it's out and what replaces it.

**Peterman:** The TimeMachine has a fundamental problem, George. It's a *novelty*. A developer reads it, chuckles at the DeLorean reference, and moves on. They never think, "I should use this for my software." There's no *transfer* — no moment where the reader sees their own problem reflected back at them.

**Steinbrenner:** Specifically. What's broken? I don't want feelings, I want a diagnosis.

**Peterman:** Three missing constructs: no `invariant`, no `when` guard, no `reject`. Those are the headline features — the things that make Precept different from a state machine library. The hero sample doesn't show any of them. It's like advertising a restaurant by showing the parking lot.

**Steinbrenner:** And the `because` messages?

**Peterman:** "You need 1.21 gigawatts" — it's a pop culture joke pretending to be a domain rule. Our `because` strings are supposed to carry the voice of a domain expert, not a movie buff. When a developer sees `because "Monthly price cannot be negative"`, they think: *yes, that's my invariant, expressed the way my product owner would say it.* When they see `because "You need 1.21 gigawatts"`, they think: *cute, but what does this have to do with my expense approval workflow?*

**Steinbrenner:** What about the line formatting? I specifically asked for the multi-line transition body as a visual signature.

**Peterman:** The TimeMachine crams everything onto single lines. The multi-line form — where you see `when`, then `set`, then `set`, then `transition`, each on its own indented line — that's the visual that communicates *pipeline*. Top to bottom. Guard, then mutate, then move. It's the hero's money shot, and TimeMachine doesn't use it.

**Steinbrenner:** So what does the hero need to do? Give me the list.

**Peterman:** Nine things. It must show: `invariant` on a real business rule, a `when` guard expression, a `reject` with a `because` message, `set` using dotted argument access like `Event.Arg`, a `transition` to a new state, a `no transition` in-place update, at least three meaningful states, a typed event argument, and an event-level `on assert` pre-condition.

**Steinbrenner:** And the line budget?

**Peterman:** Twelve to sixteen non-blank lines. Tight enough to read in a single glance, generous enough to breathe. Two or three blank separators for structure. The whole thing should render in under twenty lines total.

**Steinbrenner:** Here's what I care about: the hero must prove the product thesis. "Invalid states are structurally impossible." If the sample doesn't make that claim tangible — if a developer can't point to a specific line and say "that's the line that prevents the bad thing" — then we've failed.

**Peterman:** Precisely. The `reject` line is where that thesis becomes concrete. `reject "Cancelled subscriptions cannot be reactivated"` — that's not error handling. That's a *structural fence*. The system doesn't check whether reactivation is allowed; it *cannot express* reactivation from that state. That's the differentiation.

**Steinbrenner:** And the domain? I don't want another toy. No traffic lights, no vending machines, no time machines.

**Peterman:** The domain must satisfy three tests. First, *recognition*: any developer, regardless of stack or industry, should understand the domain in under three seconds. Second, *plausibility*: it should feel like something they'd actually build. Third, *narrative tension*: the states should tell a story with stakes. Something that moves forward, hits a gate, and either progresses or doesn't.

**Steinbrenner:** What domains are we considering?

**Peterman:** Thirty candidates across billing, infrastructure, workflow, consumer, and professional domains. Subscription billing. Deploy pipelines. Feature flags. Food delivery. Coffee orders. Freelance contracts. Everything from SaaS trials to database migrations. We'll score each on six criteria and rank them.

**Steinbrenner:** Six criteria. Define them.

**Peterman:** One: *DSL Coverage* — does the snippet exercise all nine required constructs? Two: *Line Economy* — is it in the twelve-to-sixteen sweet spot without cramming or padding? Three: *Domain Legibility* — how fast does any developer recognize the domain? Four: *Voice and Wit* — do the `because` messages carry personality and domain authority? Five: *Emotional Hook* — does it create curiosity, delight, or recognition on first read? Six: *Precept Differentiation* — does it prove structural impossibility, not just state routing?

**Steinbrenner:** Each scored one to five?

**Peterman:** Correct. Maximum thirty. Ties broken first by Precept Differentiation, then by Emotional Hook. The hero must win on differentiation — that's non-negotiable.

**Steinbrenner:** Let me be clear about the voice. The `because` messages are the only warm token in the palette. I approved occasional wit — the BTTF bar is `"The flux capacitor cannot run on vibes."` That's the ceiling. No emoji, no snark, no programmer noise. Domain expert voice or nothing.

**Peterman:** Agreed. A good `because` reads like something a VP of Product would say in a requirements meeting. Or occasionally — *occasionally* — like something a domain expert would murmur with a wry smile. The gold standard is that the reader nods and thinks, "Yes, that's exactly the rule." The occasional joke earns its place only when it sharpens the point rather than undercutting it.

**Steinbrenner:** One more thing. The hero must work as *teaching material*. A developer should be able to read it top to bottom and understand the DSL's major concepts without consulting documentation. Every line should teach something.

**Peterman:** That's the multi-line transition format doing its work. When a developer sees:

```
from Trial on Activate when PlanName == null
  -> set PlanName = Activate.Plan
  -> set MonthlyPrice = Activate.Price
  -> transition Active
```

They learn five concepts in four lines: state scoping, event binding, guard expressions, field mutation with event arguments, and state transitions. It reads like a pipeline, because it *is* a pipeline.

**Steinbrenner:** Good. Let's build the rubric and evaluate thirty candidates. I want the best one — not the safest one, not the most obvious one. The one that makes a developer stop scrolling and think, "I need this."

**Peterman:** Then let's begin.

---

## The Rubric

| Criterion | Code | Max | Description |
|---|---|---|---|
| DSL Coverage | DSL | 5 | Exercises all 9 hero requirements: invariant, when, reject, dotted set, transition, no transition, 3+ states, typed event, event assert |
| Line Economy | Eco | 5 | Fits 12–16 non-blank lines; no cramming, no padding; breathable structure with 2–3 blank separators |
| Domain Legibility | Leg | 5 | Any developer — regardless of stack, industry, or seniority — understands the domain in under 3 seconds |
| Voice / Wit | Wit | 5 | `because` messages carry domain-expert authority with optional warmth; no programmer noise, no snark |
| Emotional Hook | Hook | 5 | Creates curiosity, delight, or immediate recognition on first read; the reader wants to keep reading |
| Precept Differentiation | Diff | 5 | Proves structural impossibility — not just state routing; at least one line that makes the product thesis tangible |

**Maximum total: 30**
**Tie-breaking order: Precept Differentiation → Emotional Hook**

---

## 30 Candidates (Unranked)

---

### Candidate 1: Subscription Billing

**Theme:** The universal SaaS lifecycle — trial, activation, cancellation — with the structural guarantee that cancelled subscriptions stay cancelled.

```
precept Subscription

field PlanName as string nullable
field MonthlyPrice as number default 0

invariant MonthlyPrice >= 0 because "Monthly price cannot be negative"

state Trial initial, Active, Cancelled

event Activate with Plan as string, Price as number
on Activate assert Price > 0 because "Plan price must be positive"

event Cancel

from Trial on Activate when PlanName == null
  -> set PlanName = Activate.Plan
  -> set MonthlyPrice = Activate.Price
  -> transition Active
from Active on Activate -> set MonthlyPrice = Activate.Price -> no transition
from Active on Cancel -> transition Cancelled
from Cancelled on Activate -> reject "Cancelled subscriptions cannot be reactivated"
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 4   | 5    | 5    | **29** |

---

### Candidate 2: Support Ticket

**Theme:** Escalation priority ratchets down until it hits the floor — then the system says no.

```
precept SupportTicket

field Summary as string nullable
field Priority as number default 3
field EscalationCount as number default 0

invariant Priority >= 1 because "Priority must be at least 1"

state Open initial, Triaged, Resolved

event Create with Title as string, Level as number default 3
on Create assert Title != "" because "A ticket summary is required"

event Escalate
event Resolve

from Open on Create -> set Summary = Create.Title -> set Priority = Create.Level -> transition Triaged
from Triaged on Escalate when Priority > 1 -> set Priority = Priority - 1 -> set EscalationCount = EscalationCount + 1 -> no transition
from Triaged on Escalate -> reject "Priority 1 tickets cannot be escalated further"
from Triaged on Resolve -> transition Resolved
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 5   | 3   | 3    | 4    | **24** |

---

### Candidate 3: Pull Request Review

**Theme:** Approval accumulation with a threshold gate — you can't merge until enough reviewers agree, and you can't reopen what's been merged.

```
precept PullRequest

field Title as string nullable
field ApprovalCount as number default 0

invariant ApprovalCount >= 0 because "Approval count cannot be negative"

state Draft initial, Review, Approved, Merged

event Submit with Name as string
on Submit assert Name != "" because "A pull request title is required"

event Approve
event Merge

from Draft on Submit -> set Title = Submit.Name -> transition Review
from Review on Approve when ApprovalCount + 1 >= 2 -> set ApprovalCount = ApprovalCount + 1 -> transition Approved
from Review on Approve -> set ApprovalCount = ApprovalCount + 1 -> no transition
from Approved on Merge -> transition Merged
from Merged on Submit -> reject "Merged pull requests cannot be reopened"
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 5   | 3   | 4    | 4    | **25** |

---

### Candidate 4: Deploy Pipeline

**Theme:** Infrastructure promotion gates — health checks must pass before code reaches production, or the pipeline refuses to move.

```
precept Deployment

field ServiceName as string nullable
field VersionTag as string nullable
field Attempts as number default 0
field HealthChecksPassed as boolean default false

invariant Attempts >= 0 because "Attempt count cannot be negative"

state Queued initial, Running, Live, RolledBack

event Deploy with Service as string, Version as string
on Deploy assert Service != "" because "A service name is required"

event PassHealthCheck, Promote, Rollback

from Queued on Deploy -> set ServiceName = Deploy.Service -> set VersionTag = Deploy.Version -> transition Running
from Running on PassHealthCheck -> set HealthChecksPassed = true -> set Attempts = Attempts + 1 -> no transition
from Running on Promote when HealthChecksPassed -> transition Live
from Running on Promote -> reject "Health checks must pass before promotion"
from Live on Rollback -> transition RolledBack
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 4    | 5    | **27** |

---

### Candidate 5: API Key Lifecycle

**Theme:** Key rotation with a cooldown — freshly issued keys cannot be rotated, and revoked keys are gone forever.

```
precept ApiKey

field KeyName as string nullable
field DaysSinceCreation as number default 0
field RotationCount as number default 0

invariant DaysSinceCreation >= 0 because "Days since creation cannot be negative"
invariant RotationCount >= 0 because "Rotation count cannot be negative"

state Pending initial, Active, Revoked

event Issue with Name as string
on Issue assert Name != "" because "An API key must have a name"

event Rotate
event Revoke

from Pending on Issue -> set KeyName = Issue.Name -> transition Active
from Active on Rotate when DaysSinceCreation > 0 -> set DaysSinceCreation = 0 -> set RotationCount = RotationCount + 1 -> no transition
from Active on Rotate -> reject "A newly issued key cannot be rotated immediately"
from Active on Revoke -> transition Revoked
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 4   | 3   | 3    | 4    | **23** |

---

### Candidate 6: Feature Flag

**Theme:** Controlled rollout with a ratchet — percentages can only go up during staging, never down.

```
precept FeatureFlag

field FlagName as string nullable
field RolloutPercent as number default 0

invariant RolloutPercent >= 0 because "Rollout percentage cannot be negative"
invariant RolloutPercent <= 100 because "Rollout cannot exceed 100 percent"

state Off initial, Staged, Active, Retired

event Create with Name as string, Percent as number default 0
on Create assert Name != "" because "A feature flag must have a name"

event Ramp with Percent as number
event Activate, Retire

from Off on Create -> set FlagName = Create.Name -> set RolloutPercent = Create.Percent -> transition Staged
from Staged on Ramp when Ramp.Percent > RolloutPercent -> set RolloutPercent = Ramp.Percent -> no transition
from Staged on Ramp -> reject "Rollout percentage can only increase during staging"
from Staged on Activate -> set RolloutPercent = 100 -> transition Active
from Active on Retire -> transition Retired
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 4    | 5    | **27** |

---

### Candidate 7: Password Reset

**Theme:** Rate-limited identity verification — you must initiate before you can verify, and the attempt counter enforces the ceiling.

```
precept PasswordReset

field Email as string nullable
field AttemptCount as number default 0
field CodeVerified as boolean default false

invariant AttemptCount >= 0 because "Attempt count cannot be negative"
invariant AttemptCount <= 5 because "Maximum reset attempts must not be exceeded"

state Pending initial, Verified, Completed, Expired

event Begin with Address as string
on Begin assert Address != "" because "An email address is required"

event VerifyCode, Complete, Expire

from Pending on Begin -> set Email = Begin.Address -> set AttemptCount = AttemptCount + 1 -> no transition
from Pending on VerifyCode when AttemptCount > 0 -> set CodeVerified = true -> transition Verified
from Pending on VerifyCode -> reject "A reset must be initiated before verification"
from Verified on Complete -> transition Completed
from Pending on Expire -> transition Expired
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 4   | 5   | 3   | 3    | 4    | **23** |

---

### Candidate 8: Purchase Order

**Theme:** Partial and full approval — amounts below the total stay in review, amounts that meet or exceed it advance the order.

```
precept PurchaseOrder

field VendorName as string nullable
field TotalAmount as number default 0
field ApprovedAmount as number default 0

invariant TotalAmount >= 0 because "Order total cannot be negative"

state Draft initial, Submitted, Approved, Fulfilled

event Submit with Vendor as string, Amount as number
on Submit assert Amount > 0 because "Order amount must be positive"

event Approve with Amount as number
event Fulfill

from Draft on Submit -> set VendorName = Submit.Vendor -> set TotalAmount = Submit.Amount -> transition Submitted
from Submitted on Approve when Approve.Amount >= TotalAmount -> set ApprovedAmount = TotalAmount -> transition Approved
from Submitted on Approve when Approve.Amount > 0 -> set ApprovedAmount = Approve.Amount -> no transition
from Submitted on Approve -> reject "Approval amount must be positive"
from Approved on Fulfill -> transition Fulfilled
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 3    | 4    | **25** |

---

### Candidate 9: Expense Report

**Theme:** Approval-before-reimbursement gate — the system structurally prevents paying out an expense that hasn't been reviewed.

```
precept ExpenseReport

field EmployeeName as string nullable
field TotalAmount as number default 0
field ApprovedAmount as number default 0

invariant TotalAmount >= 0 because "Expense total cannot be negative"

state Draft initial, Submitted, Reimbursed, Rejected

event Submit with Employee as string, Amount as number
on Submit assert Amount > 0 because "Expense amount must be positive"

event Approve with Amount as number
event Reimburse, Reject

from Draft on Submit -> set EmployeeName = Submit.Employee -> set TotalAmount = Submit.Amount -> transition Submitted
from Submitted on Approve when Approve.Amount <= TotalAmount -> set ApprovedAmount = Approve.Amount -> no transition
from Submitted on Approve -> reject "Approved amount cannot exceed the expense total"
from Submitted on Reimburse when ApprovedAmount > 0 -> transition Reimbursed
from Submitted on Reimburse -> reject "Expenses must be approved before reimbursement"
from Submitted on Reject -> transition Rejected
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 5   | 3   | 3    | 4    | **24** |

---

### Candidate 10: Job Queue Task

**Theme:** Retry logic with a structural ceiling — tasks bounce between queued and running until the retry limit shuts the door.

```
precept JobTask

field TaskName as string nullable
field RetryCount as number default 0
field MaxRetries as number default 3

invariant RetryCount >= 0 because "Retry count cannot be negative"

state Queued initial, Running, Completed, Failed

event Enqueue with Name as string
on Enqueue assert Name != "" because "A task name is required"

event Start, Complete, Fail

from Queued on Enqueue -> set TaskName = Enqueue.Name -> no transition
from Queued on Start when TaskName != null -> transition Running
from Queued on Start -> reject "A task must be enqueued before it can start"
from Running on Complete -> transition Completed
from Running on Fail when RetryCount < MaxRetries -> set RetryCount = RetryCount + 1 -> transition Queued
from Running on Fail -> transition Failed
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 4   | 3   | 3    | 5    | **25** |

---

### Candidate 11: Hotel Reservation

**Theme:** Night extensions with positivity enforcement — guests can add nights but never subtract them, and completed stays are final.

```
precept HotelReservation

field GuestName as string nullable
field NightCount as number default 0
field RoomRate as number default 0

invariant NightCount >= 0 because "Night count cannot be negative"
invariant RoomRate >= 0 because "Room rate cannot be negative"

state Pending initial, Confirmed, CheckedIn, CheckedOut

event Book with Guest as string, Nights as number, Rate as number
on Book assert Nights > 0 because "At least one night must be reserved"

event Extend with Nights as number
event CheckIn, CheckOut

from Pending on Book -> set GuestName = Book.Guest -> set NightCount = Book.Nights -> set RoomRate = Book.Rate -> transition Confirmed
from Confirmed on Extend when Extend.Nights > 0 -> set NightCount = NightCount + Extend.Nights -> no transition
from Confirmed on Extend -> reject "Additional nights must be a positive number"
from Confirmed on CheckIn -> transition CheckedIn
from CheckedIn on CheckOut -> transition CheckedOut
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 4   | 5   | 3   | 4    | 3    | **23** |

---

### Candidate 12: Food Delivery

**Theme:** Dispatch requires a driver — the kitchen can finish prep, but the order physically cannot leave until someone is assigned to carry it.

```
precept FoodDelivery

field OrderTotal as number default 0
field DriverName as string nullable

invariant OrderTotal >= 0 because "Order total cannot be negative"

state Placed initial, Preparing, EnRoute, Delivered

event Place with Total as number
on Place assert Total > 0 because "Order total must be positive"

event AssignDriver with Driver as string
event MarkReady, Deliver

from Placed on Place -> set OrderTotal = Place.Total -> transition Preparing
from Preparing on AssignDriver -> set DriverName = AssignDriver.Driver -> no transition
from Preparing on MarkReady when DriverName != null -> transition EnRoute
from Preparing on MarkReady -> reject "A driver must be assigned before dispatch"
from EnRoute on Deliver -> transition Delivered
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 4    | 5    | **27** |

---

### Candidate 13: Return / Refund

**Theme:** Item-receipt gate — the refund cannot be processed until the physical item is back in hand.

```
precept ReturnRequest

field OrderId as string nullable
field RefundAmount as number default 0
field ItemReceived as boolean default false

invariant RefundAmount >= 0 because "Refund amount cannot be negative"

state Pending initial, UnderReview, Completed, Denied

event Submit with Order as string, Amount as number
on Submit assert Amount > 0 because "Refund amount must be positive"

event Approve, ReceiveItem, Process, Deny

from Pending on Submit -> set OrderId = Submit.Order -> set RefundAmount = Submit.Amount -> no transition
from Pending on Approve -> transition UnderReview
from UnderReview on ReceiveItem -> set ItemReceived = true -> no transition
from UnderReview on Process when ItemReceived -> transition Completed
from UnderReview on Process -> reject "The item must be returned before processing a refund"
from Pending on Deny -> transition Denied
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 5   | 3   | 3    | 4    | **24** |

---

### Candidate 14: Email Campaign

**Theme:** Batch-completion gate — the campaign cannot finalize until every batch has been recorded as sent.

```
precept EmailCampaign

field Subject as string nullable
field RecipientCount as number default 0
field SentCount as number default 0

invariant RecipientCount >= 0 because "Recipient count cannot be negative"

state Draft initial, Scheduled, Sending, Sent

event Schedule with Title as string, Recipients as number
on Schedule assert Recipients > 0 because "At least one recipient is required"

event Launch, RecordBatch, Finalize

from Draft on Schedule -> set Subject = Schedule.Title -> set RecipientCount = Schedule.Recipients -> transition Scheduled
from Scheduled on Launch when RecipientCount > 0 -> transition Sending
from Scheduled on Launch -> reject "A campaign needs recipients before launch"
from Sending on RecordBatch -> set SentCount = SentCount + 1 -> no transition
from Sending on Finalize when SentCount >= RecipientCount -> transition Sent
from Sending on Finalize -> reject "Not all batches have been sent"
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 3    | 5    | **26** |

---

### Candidate 15: SSL Certificate

**Theme:** Domain-validation gate and revocation finality — certificates cannot be issued without verification, and revoked certificates are terminal.

```
precept SslCertificate

field DomainName as string nullable
field ValidityDays as number default 0
field DaysElapsed as number default 0

invariant ValidityDays >= 0 because "Validity period cannot be negative"
invariant DaysElapsed >= 0 because "Elapsed days cannot be negative"

state Requested initial, Validated, Issued, Revoked

event Request with Domain as string, Days as number default 365
on Request assert Domain != "" because "A domain name is required"

event Validate, AdvanceDay, Revoke

from Requested on Request -> set DomainName = Request.Domain -> set ValidityDays = Request.Days -> transition Validated
from Validated on Validate when DomainName != null -> transition Issued
from Validated on Validate -> reject "Domain ownership must be verified before issuance"
from Issued on AdvanceDay -> set DaysElapsed = DaysElapsed + 1 -> no transition
from Issued on Revoke -> transition Revoked
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 4   | 4   | 3   | 3    | 4    | **22** |

---

### Candidate 16: Course Enrollment

**Theme:** Credit accumulation with a graduation gate — you cannot complete a course until the credits are earned.

```
precept CourseEnrollment

field StudentName as string nullable
field CourseName as string nullable
field CreditsEarned as number default 0
field RequiredCredits as number default 30

invariant CreditsEarned >= 0 because "Earned credits cannot be negative"

state Registered initial, Active, Completed, Dropped

event Enroll with Student as string, Course as string, Credits as number default 30
on Enroll assert Student != "" because "A student name is required"

event EarnCredits with Credits as number
event Complete, Drop

from Registered on Enroll -> set StudentName = Enroll.Student -> set CourseName = Enroll.Course -> set RequiredCredits = Enroll.Credits -> transition Active
from Active on EarnCredits -> set CreditsEarned = CreditsEarned + EarnCredits.Credits -> no transition
from Active on Complete when CreditsEarned >= RequiredCredits -> transition Completed
from Active on Complete -> reject "Required credits have not been earned"
from Active on Drop -> transition Dropped
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 5   | 3   | 3    | 4    | **24** |

---

### Candidate 17: Inventory Restock

**Theme:** Quantity-exact receiving — partial shipments accumulate, but the moment received exceeds ordered, the system refuses the delivery.

```
precept InventoryRestock

field ProductName as string nullable
field QuantityOrdered as number default 0
field QuantityReceived as number default 0

invariant QuantityOrdered >= 0 because "Order quantity cannot be negative"

state Requested initial, Ordered, Received

event Request with Product as string, Quantity as number
on Request assert Quantity > 0 because "Restock quantity must be positive"

event Receive with Quantity as number

from Requested on Request -> set ProductName = Request.Product -> set QuantityOrdered = Request.Quantity -> transition Ordered
from Ordered on Receive when QuantityReceived + Receive.Quantity == QuantityOrdered -> set QuantityReceived = QuantityOrdered -> transition Received
from Ordered on Receive when QuantityReceived + Receive.Quantity < QuantityOrdered -> set QuantityReceived = QuantityReceived + Receive.Quantity -> no transition
from Ordered on Receive -> reject "Received quantity cannot exceed the ordered amount"
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 4   | 3   | 2    | 5    | **24** |

---

### Candidate 18: Medical Appointment

**Theme:** Doctor-assignment gate — you cannot confirm an appointment until a physician is assigned to it.

```
precept MedicalAppointment

field PatientName as string nullable
field DoctorName as string nullable
field WaitMinutes as number default 0

invariant WaitMinutes >= 0 because "Wait time cannot be negative"

state Scheduled initial, Confirmed, CheckedIn, Completed

event Book with Patient as string, Doctor as string
on Book assert Patient != "" because "A patient name is required"

event Confirm, CheckIn, Complete

from Scheduled on Book -> set PatientName = Book.Patient -> set DoctorName = Book.Doctor -> no transition
from Scheduled on Confirm when DoctorName != null -> transition Confirmed
from Scheduled on Confirm -> reject "A doctor must be assigned before confirmation"
from Confirmed on CheckIn -> transition CheckedIn
from CheckedIn on Complete -> transition Completed
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 5   | 3   | 3    | 4    | **24** |

---

### Candidate 19: Two-Factor Auth

**Theme:** Attempt-limited verification — you must try at least once before confirming, and the system locks you out after too many failures.

```
precept TwoFactorSetup

field UserEmail as string nullable
field AttemptCount as number default 0
field MaxAttempts as number default 3

invariant AttemptCount >= 0 because "Attempt count cannot be negative"

state Pending initial, Challenging, Active, Locked

event Enroll with Email as string
on Enroll assert Email != "" because "An email address is required"

event Attempt, Confirm, Disable

from Pending on Enroll -> set UserEmail = Enroll.Email -> transition Challenging
from Challenging on Attempt when AttemptCount < MaxAttempts -> set AttemptCount = AttemptCount + 1 -> no transition
from Challenging on Attempt -> reject "Maximum verification attempts exceeded"
from Challenging on Confirm when AttemptCount > 0 -> transition Active
from Challenging on Confirm -> reject "At least one verification attempt is required"
from Active on Disable -> transition Locked
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 4   | 3   | 3    | 5    | **25** |

---

### Candidate 20: Gym Membership

**Theme:** Freeze-limit enforcement — members can pause and resume, but only twice, ever.

```
precept GymMembership

field MemberName as string nullable
field MonthlyFee as number default 0
field FreezeCount as number default 0

invariant MonthlyFee >= 0 because "Monthly fee cannot be negative"
invariant FreezeCount <= 2 because "Memberships allow a maximum of two freezes"

state Pending initial, Active, Frozen, Cancelled

event Join with Name as string, Fee as number
on Join assert Fee > 0 because "Monthly fee must be positive"

event Freeze, Unfreeze, Cancel

from Pending on Join -> set MemberName = Join.Name -> set MonthlyFee = Join.Fee -> transition Active
from Active on Join -> set MonthlyFee = Join.Fee -> no transition
from Active on Freeze when FreezeCount < 2 -> set FreezeCount = FreezeCount + 1 -> transition Frozen
from Active on Freeze -> reject "Maximum freeze limit has been reached"
from Frozen on Unfreeze -> transition Active
from Active on Cancel -> transition Cancelled
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 5   | 4   | 4    | 4    | **26** |

---

### Candidate 21: Building Access Badge

**Theme:** Security-level ratchet — badge access levels can be upgraded but never downgraded, enforced structurally.

```
precept AccessBadge

field HolderName as string nullable
field AccessLevel as number default 1
field BadgeActive as boolean default false

invariant AccessLevel >= 1 because "Access level must be at least 1"
invariant AccessLevel <= 5 because "Access level cannot exceed 5"

state Requested initial, Approved, Issued, Deactivated

event Issue with Holder as string, Level as number default 1
on Issue assert Holder != "" because "A badge holder name is required"

event Activate, Deactivate
event Upgrade with Level as number

from Requested on Issue -> set HolderName = Issue.Holder -> set AccessLevel = Issue.Level -> transition Approved
from Approved on Activate -> set BadgeActive = true -> transition Issued
from Issued on Upgrade when Upgrade.Level > AccessLevel -> set AccessLevel = Upgrade.Level -> no transition
from Issued on Upgrade -> reject "Access level can only be increased"
from Issued on Deactivate -> set BadgeActive = false -> transition Deactivated
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 4   | 3   | 3    | 4    | **23** |

---

### Candidate 22: Parking Permit

**Theme:** Renewal-positive enforcement — permits can be extended but only with a positive duration, and revocation is final.

```
precept ParkingPermit

field OwnerName as string nullable
field MonthsRemaining as number default 0
field TotalCost as number default 0

invariant MonthsRemaining >= 0 because "Remaining months cannot be negative"
invariant TotalCost >= 0 because "Permit cost cannot be negative"

state Applied initial, Active, Revoked

event Issue with Owner as string, Months as number, Cost as number
on Issue assert Owner != "" because "A permit holder is required"
on Issue assert Months > 0 because "Duration must be at least one month"

event Renew with Months as number
event Revoke

from Applied on Issue -> set OwnerName = Issue.Owner -> set MonthsRemaining = Issue.Months -> set TotalCost = Issue.Cost -> transition Active
from Active on Renew when Renew.Months > 0 -> set MonthsRemaining = MonthsRemaining + Renew.Months -> no transition
from Active on Renew -> reject "Renewal must be for at least one month"
from Active on Revoke -> transition Revoked
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 4   | 5   | 3   | 2    | 3    | **21** |

---

### Candidate 23: Flight Booking

**Theme:** Seat-reservation gate and boarding finality — you can update a reservation, but once boarded, the booking is sealed.

```
precept FlightBooking

field PassengerName as string nullable
field FlightNumber as string nullable
field SeatCount as number default 0

invariant SeatCount >= 0 because "Seat count cannot be negative"

state Searching initial, Reserved, Ticketed, Boarded

event Reserve with Passenger as string, Flight as string, Seats as number default 1
on Reserve assert Passenger != "" because "A passenger name is required"

event Purchase, Board

from Searching on Reserve -> set PassengerName = Reserve.Passenger -> set FlightNumber = Reserve.Flight -> set SeatCount = Reserve.Seats -> transition Reserved
from Reserved on Reserve -> set SeatCount = Reserve.Seats -> no transition
from Reserved on Purchase when SeatCount > 0 -> transition Ticketed
from Reserved on Purchase -> reject "Seats must be reserved before ticketing"
from Ticketed on Board -> transition Boarded
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 4   | 5   | 5   | 3   | 4    | 4    | **25** |

---

### Candidate 24: Freelance Contract

**Theme:** Payment accumulation with a balance gate — the contract cannot be completed until every dollar is accounted for.

```
precept FreelanceContract

field ClientName as string nullable
field ContractValue as number default 0
field AmountPaid as number default 0

invariant ContractValue >= 0 because "Contract value cannot be negative"

state Drafted initial, Active, Completed, Disputed

event Sign with Client as string, Value as number
on Sign assert Value > 0 because "Contract value must be positive"

event RecordPayment with Amount as number
event Complete, Dispute

from Drafted on Sign -> set ClientName = Sign.Client -> set ContractValue = Sign.Value -> transition Active
from Active on RecordPayment when AmountPaid + RecordPayment.Amount <= ContractValue -> set AmountPaid = AmountPaid + RecordPayment.Amount -> no transition
from Active on RecordPayment -> reject "Payment would exceed the contract value"
from Active on Complete when AmountPaid == ContractValue -> transition Completed
from Active on Complete -> reject "Outstanding balance must be settled before completion"
from Active on Dispute -> transition Disputed
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 5   | 3   | 4    | 5    | **26** |

---

### Candidate 25: License Renewal

**Theme:** Fee-payment gate — the license cannot be approved until the fee is paid, enforced as a structural precondition.

```
precept LicenseRenewal

field LicenseHolder as string nullable
field FeeAmount as number default 0
field FeePaid as boolean default false

invariant FeeAmount >= 0 because "License fee cannot be negative"

state Pending initial, UnderReview, Renewed, Denied

event Apply with Holder as string, Fee as number
on Apply assert Holder != "" because "A license holder is required"
on Apply assert Fee > 0 because "License fee must be positive"

event PayFee, Approve, Deny

from Pending on Apply -> set LicenseHolder = Apply.Holder -> set FeeAmount = Apply.Fee -> transition UnderReview
from UnderReview on PayFee -> set FeePaid = true -> no transition
from UnderReview on Approve when FeePaid -> transition Renewed
from UnderReview on Approve -> reject "License fee must be paid before approval"
from UnderReview on Deny -> transition Denied
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 2    | 4    | **24** |

---

### Candidate 26: Coffee Order

**Theme:** The barista workflow — order, brew, pick up — with a warm voice that proves business rules don't have to sound like error codes.

```
precept CoffeeOrder

field DrinkName as string nullable
field ShotCount as number default 1
field SizeOunces as number default 12

invariant ShotCount >= 1 because "Every drink gets at least one shot"
invariant SizeOunces >= 8 because "The smallest cup is eight ounces"

state Ordered initial, Brewing, Ready, PickedUp

event Order with Drink as string, Shots as number default 1, Size as number default 12
on Order assert Drink != "" because "Even the barista needs to know what you want"

event Brew, Finish, PickUp

from Ordered on Order -> set DrinkName = Order.Drink -> set ShotCount = Order.Shots -> set SizeOunces = Order.Size -> no transition
from Ordered on Brew when DrinkName != null -> transition Brewing
from Ordered on Brew -> reject "An order must be placed before brewing can begin"
from Brewing on Finish -> transition Ready
from Ready on PickUp -> transition PickedUp
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 5   | 5    | 4    | **29** |

---

### Candidate 27: Shopping Cart Checkout

**Theme:** Empty-cart gate — the system structurally prevents checking out a cart with zero items.

```
precept ShoppingCart

field ItemCount as number default 0
field CartTotal as number default 0
field PaymentMethod as string nullable

invariant ItemCount >= 0 because "Cart cannot have negative items"
invariant CartTotal >= 0 because "Cart total cannot be negative"

state Browsing initial, Cart, Checkout, Purchased

event AddItem with Price as number
on AddItem assert Price > 0 because "Item price must be positive"

event BeginCheckout
event Pay with Method as string

from Browsing on AddItem -> set ItemCount = ItemCount + 1 -> set CartTotal = CartTotal + AddItem.Price -> transition Cart
from Cart on AddItem -> set ItemCount = ItemCount + 1 -> set CartTotal = CartTotal + AddItem.Price -> no transition
from Cart on BeginCheckout when ItemCount > 0 -> transition Checkout
from Cart on BeginCheckout -> reject "Cart must contain at least one item"
from Checkout on Pay -> set PaymentMethod = Pay.Method -> transition Purchased
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 5   | 3   | 4    | 4    | **25** |

---

### Candidate 28: SaaS Trial

**Theme:** Trial countdown with a conversion gate — once the days run out, activation is structurally impossible.

```
precept SaasTrial

field CompanyName as string nullable
field TrialDaysLeft as number default 14
field MonthlyPrice as number default 0

invariant TrialDaysLeft >= 0 because "Trial days remaining cannot be negative"

state Trial initial, Active, Expired, Churned

event Activate with Company as string, Price as number
on Activate assert Price > 0 because "Subscription price must be positive"

event AdvanceDay, Expire, Churn

from Trial on Activate when TrialDaysLeft > 0 -> set CompanyName = Activate.Company -> set MonthlyPrice = Activate.Price -> transition Active
from Trial on Activate -> reject "An expired trial cannot be activated"
from Trial on AdvanceDay -> set TrialDaysLeft = TrialDaysLeft - 1 -> no transition
from Trial on Expire when TrialDaysLeft == 0 -> transition Expired
from Trial on Expire -> reject "Trial days have not been exhausted"
from Expired on Churn -> transition Churned
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 4   | 5    | 5    | **29** |

---

### Candidate 29: Database Migration

**Theme:** Step-by-step verification gate — every migration step must complete before the migration can be verified, with a rollback escape hatch.

```
precept DatabaseMigration

field MigrationName as string nullable
field StepCount as number default 0
field CompletedSteps as number default 0

invariant StepCount >= 0 because "Step count cannot be negative"
invariant CompletedSteps <= StepCount because "Completed steps cannot exceed total steps"

state Pending initial, Running, Verified, RolledBack

event Begin with Name as string, Steps as number
on Begin assert Steps > 0 because "At least one migration step is required"

event CompleteStep, Verify, Rollback

from Pending on Begin -> set MigrationName = Begin.Name -> set StepCount = Begin.Steps -> transition Running
from Running on CompleteStep when CompletedSteps < StepCount -> set CompletedSteps = CompletedSteps + 1 -> no transition
from Running on CompleteStep -> reject "All migration steps have already been completed"
from Running on Verify when CompletedSteps == StepCount -> transition Verified
from Running on Verify -> reject "All steps must complete before verification"
from Running on Rollback -> transition RolledBack
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 4   | 4   | 3   | 3    | 5    | **24** |

---

### Candidate 30: Code Review Approval

**Theme:** Comment-before-approval gate — rubber-stamp approvals are structurally impossible; at least one comment must exist.

```
precept CodeReview

field AuthorName as string nullable
field CommentCount as number default 0
field ApprovalCount as number default 0

invariant CommentCount >= 0 because "Comment count cannot be negative"
invariant ApprovalCount >= 0 because "Approval count cannot be negative"

state Open initial, InReview, Approved, Rejected

event Submit with Author as string
on Submit assert Author != "" because "A review author is required"

event Comment, Approve, Reject

from Open on Submit -> set AuthorName = Submit.Author -> transition InReview
from InReview on Comment -> set CommentCount = CommentCount + 1 -> no transition
from InReview on Approve when CommentCount > 0 -> set ApprovalCount = ApprovalCount + 1 -> transition Approved
from InReview on Approve -> reject "At least one comment is required before approval"
from InReview on Reject -> transition Rejected
```

| DSL | Eco | Leg | Wit | Hook | Diff | Total |
|-----|-----|-----|-----|------|------|-------|
| 5   | 5   | 5   | 3   | 3    | 4    | **25** |

---

## Final Ranking

| Rank | # | Domain | Total | DSL | Eco | Leg | Wit | Hook | Diff | Reasoning |
|------|---|--------|-------|-----|-----|-----|-----|------|------|-----------|
| 1 | 1 | Subscription Billing | **29** | 5 | 5 | 5 | 4 | 5 | 5 | Universal SaaS lifecycle with the quintessential structural impossibility line; the multi-line hero format reads like a product spec. |
| 2 | 28 | SaaS Trial | **29** | 5 | 5 | 5 | 4 | 5 | 5 | Trial countdown creates narrative tension; "An expired trial cannot be activated" is the conversion cliff every SaaS builder knows. |
| 3 | 26 | Coffee Order | **29** | 5 | 5 | 5 | 5 | 5 | 4 | Highest delight score of any candidate; "Even the barista needs to know what you want" is unforgettable — loses on Diff tiebreak. |
| 4 | 4 | Deploy Pipeline | **27** | 5 | 5 | 5 | 3 | 4 | 5 | Infrastructure teams will see their CI/CD pipeline immediately; "Health checks must pass before promotion" is a production-grade gate. |
| 5 | 12 | Food Delivery | **27** | 5 | 5 | 5 | 3 | 4 | 5 | Consumer-universal domain; "A driver must be assigned before dispatch" is the clearest physical-world structural impossibility in the set. |
| 6 | 6 | Feature Flag | **27** | 5 | 5 | 5 | 3 | 4 | 5 | The rollout ratchet is an elegant structural rule; dev audience resonance is very high. |
| 7 | 24 | Freelance Contract | **26** | 5 | 4 | 5 | 3 | 4 | 5 | Payment accumulation with a balance gate; "Outstanding balance must be settled before completion" is a real-world structural fence. |
| 8 | 14 | Email Campaign | **26** | 5 | 5 | 5 | 3 | 3 | 5 | Clean batch-completion story; strong Diff but lower emotional pull than the top tier. |
| 9 | 20 | Gym Membership | **26** | 5 | 4 | 5 | 4 | 4 | 4 | The freeze-limit invariant is a business rule everyone has encountered; strong voice in "Memberships allow a maximum of two freezes." |
| 10 | 10 | Job Queue Task | **25** | 5 | 5 | 4 | 3 | 3 | 5 | Retry ceiling with structural enforcement; strong DSL coverage and clean economy, but less universal recognition. |
| 11 | 19 | Two-Factor Auth | **25** | 5 | 5 | 4 | 3 | 3 | 5 | Attempt-limiting with double reject pattern; excellent Diff but the security domain is narrower. |
| 12 | 3 | Pull Request Review | **25** | 4 | 5 | 5 | 3 | 4 | 4 | Developer self-recognition is high; approval-threshold logic is intuitive but missing some DSL constructs. |
| 13 | 27 | Shopping Cart | **25** | 5 | 4 | 5 | 3 | 4 | 4 | Universal e-commerce; the empty-cart gate is immediately understood but not deeply surprising. |
| 14 | 23 | Flight Booking | **25** | 4 | 5 | 5 | 3 | 4 | 4 | Highly relatable travel domain; seat-reservation gate is clean but standard. |
| 15 | 8 | Purchase Order | **25** | 5 | 5 | 5 | 3 | 3 | 4 | Business-process classic; partial-approval pattern is strong but lacks emotional hook. |
| 16 | 30 | Code Review | **25** | 5 | 5 | 5 | 3 | 3 | 4 | Comment-before-approval is a real workflow rule; clean execution but quiet personality. |
| 17 | 2 | Support Ticket | **24** | 4 | 5 | 5 | 3 | 3 | 4 | Escalation floor is a nice structural rule but the domain lacks novelty. |
| 18 | 9 | Expense Report | **24** | 5 | 4 | 5 | 3 | 3 | 4 | Approval-before-reimbursement is solid; too similar to Purchase Order to stand on its own. |
| 19 | 13 | Return / Refund | **24** | 4 | 5 | 5 | 3 | 3 | 4 | Item-receipt gate is a clean physical-world rule but the flow is complex for a hero. |
| 20 | 16 | Course Enrollment | **24** | 5 | 4 | 5 | 3 | 3 | 4 | Credit-accumulation gate is intuitive; education domain is broad but not emotionally charged. |
| 21 | 17 | Inventory Restock | **24** | 5 | 5 | 4 | 3 | 2 | 5 | Strong structural story with quantity-exact receiving; too warehouse-specific for universal appeal. |
| 22 | 18 | Medical Appointment | **24** | 4 | 5 | 5 | 3 | 3 | 4 | Doctor-assignment gate is universally understood; clean but clinical, appropriately enough. |
| 23 | 29 | Database Migration | **24** | 5 | 4 | 4 | 3 | 3 | 5 | Step-completion gate is a strong structural rule; dev audience only, lower legibility for general audience. |
| 24 | 25 | License Renewal | **24** | 5 | 5 | 5 | 3 | 2 | 4 | Fee-payment gate is clear; well-executed but lacks memorable personality. |
| 25 | 5 | API Key Lifecycle | **23** | 4 | 5 | 4 | 3 | 3 | 4 | Rotation cooldown is a nice security pattern; too infrastructure-niche for hero duty. |
| 26 | 7 | Password Reset | **23** | 4 | 4 | 5 | 3 | 3 | 4 | Everyone knows the domain; the attempt ceiling is structural but the flow feels procedural. |
| 27 | 11 | Hotel Reservation | **23** | 4 | 4 | 5 | 3 | 4 | 3 | Highly relatable but weaker structural impossibility story; extension pattern is nice but not the core thesis. |
| 28 | 21 | Building Access Badge | **23** | 5 | 4 | 4 | 3 | 3 | 4 | Security-level ratchet is a good rule; too corporate-specific for universal hero appeal. |
| 29 | 15 | SSL Certificate | **22** | 4 | 4 | 4 | 3 | 3 | 4 | Domain-validation gate is real infrastructure; too narrow and too many concepts for non-infrastructure audience. |
| 30 | 22 | Parking Permit | **21** | 4 | 4 | 5 | 3 | 2 | 3 | Lowest structural impossibility payoff; the domain is clear but the stakes are too low for a hero sample. |
