# Internal DSL Verbosity Analysis

**Authored by:** Uncle Leo (Code Reviewer)  
**Date:** 2026-04-05  
**Purpose:** Identify constructs in the Precept DSL that inflate statement count, assess which existing samples come closest to the hero rubric's 6–8 gate, and surface verbosity smells for design consideration.

---

## Methodology

Statement counting per the rubric:

> `precept`, `field`, `invariant`, `state`, `event` declarations + `from X on Y` rule headers + `set`/`transition`/`reject` actions

Counted individually. `no transition`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `in/to/from assert`, `on Event assert`, and whitespace do **not** count.

All 21 `.precept` files in `samples/` were read and counted by hand from source.

---

## 1. Statement Count Distribution

**Gate: 6–8 meaningful statements. Samples passing: 0 of 21.**

| Sample | Decl (P+F+I+S+E) | Headers (H) | Actions (SET+TR+REJ) | **Total** |
|--------|-------------------|-------------|----------------------|-----------|
| `crosswalk-signal` | 11 | 6 | 12 | **29** |
| `restaurant-waitlist` | 15 | 6 | 12 | **33** |
| `subscription-cancellation-retention` | 18 | 5 | 13 | **36** |
| `building-access-badge-request` | 21 | 8 | 12 | **41** |
| `refund-request` | 21 | 6 | 15 | **42** |
| `clinic-appointment-scheduling` | 22 | 6 | 16 | **44** |
| `vehicle-service-appointment` | 22 | 9 | 13 | **44** |
| `loan-application` | 24 | 6 | 14 | **44** |
| `apartment-rental-application` | 23 | 7 | 15 | **45** |
| `travel-reimbursement` | 23 | 6 | 16 | **45** |
| `parcel-locker-pickup` | 21 | 9 | 17 | **47** |
| `trafficlight` | 17 | 11 | 20 | **48** |
| `event-registration` | 23 | 7 | 20 | **50** |
| `it-helpdesk-ticket` | 23 | 13 | 16 | **52** |
| `utility-outage-report` | 25 | 14 | 15 | **54** |
| `maintenance-work-order` | 26 | 10 | 19 | **55** |
| `hiring-pipeline` | 24 | 13 | 20 | **57** |
| `library-hold-request` | 21 | 17 | 29 | **67** |
| `insurance-claim` | 25 | 10 | 14 | **49** |
| `library-book-checkout` | 27 | 14 | 45 | **86** |
| `warranty-repair-request` | 24 | 10 | 15 | **49** |

**Sorted ascending:** 29, 33, 36, 41, 42, 44, 44, 44, 45, 45, 47, 48, 49, 49, 50, 52, 54, 55, 57, 67, 86

**Median:** ~47. **Range:** 29–86. **Gate ceiling:** 8.

The smallest sample in the corpus (`crosswalk-signal`, 29) is **3.6× the gate maximum**. The gate is not achievable by trimming an existing reference sample. It requires a purpose-built hero snippet.

---

## 2. Verbosity Patterns

| Construct | Why it inflates count | Frequency | Could be compressed? |
|-----------|----------------------|-----------|----------------------|
| **Event argument ingestion (`set field = Event.Arg`)** | Every data-carrying event that transitions must manually map each argument to a field. A 4-arg intake event adds 4 SET statements per transition row. | All 21 samples. Every sample that transitions on a data event. | Yes — a hypothetical `absorb Event` or field-name-matching shorthand could collapse N sets to 1 statement. |
| **Guard-pair header duplication** | A conditional transition requires two rule headers: `from X on Y when condition -> …` and `from X on Y -> reject "…"`. Every guarded transition doubles its own header count. | All 21 samples. Typically 1–5 such pairs per sample. | Partially — a `when condition else reject "…"` inline form would halve the header cost. |
| **Non-negative numeric invariants** | `invariant Amount >= 0 because "Amount cannot be negative"` appears in boilerplate form for every numeric field. Samples average 2–4 of these. | 19 of 21 samples. | Yes — a field-level constraint attribute (`field Amount as number min 0`) would replace the invariant statement entirely. |
| **Standalone state declarations** | Each `state X` is one statement. A 5-state machine = 5 statement slots consumed by naming alone. | All 21 samples; 3–7 states each. | Partially — multi-state shorthand (`state Draft, Submitted, Approved`) exists in the language but is not used in any sample because states typically carry different annotations (`initial`, asserts). |
| **Full type annotation on every field** | Every `field X as string nullable` or `field X as number default 0` is one statement with mandatory type and default annotation. Fields sharing the same type/default cannot share a declaration unless they share both. | All 21 samples. | Partially — multi-name shorthand (`field A, B as number default 0`) exists but only works when fields share type and default. Most samples have heterogeneous defaults. |

---

## 3. Candidates That Are Close

No existing sample passes or approaches the 6–8 gate, but three are structurally the leanest:

### `crosswalk-signal` (29 statements — closest)

```
precept CrosswalkSignal

field RequestPending as boolean default false
field CycleCount as number default 0
field CountdownSeconds as number default 0

invariant CycleCount >= 0 because "Cycle count cannot be negative"
invariant CountdownSeconds >= 0 because "Countdown seconds cannot be negative"

state DontWalk initial
state Walk
state FlashingDontWalk

event PedestrianRequest
event Advance

from any on PedestrianRequest -> set RequestPending = true -> no transition
from DontWalk on Advance when RequestPending -> set RequestPending = false -> set CountdownSeconds = 20 -> set CycleCount = CycleCount + 1 -> transition Walk
from DontWalk on Advance -> no transition
from Walk on Advance -> set CountdownSeconds = 7 -> set CycleCount = CycleCount + 1 -> transition FlashingDontWalk
from FlashingDontWalk on Advance when CountdownSeconds <= 1 -> set CountdownSeconds = 0 -> set CycleCount = CycleCount + 1 -> transition DontWalk
from FlashingDontWalk on Advance -> set CountdownSeconds = CountdownSeconds - 1 -> no transition
```

**Where the 29 statements come from:**

| Category | Count | Notes |
|----------|-------|-------|
| Declarations (P+F+I+S+E) | 11 | 3 fields, 2 invariants, 3 states, 2 events |
| Rule headers | 6 | 3 guarded/unguarded pairs |
| SET actions | 9 | CycleCount+1 appears 3×; no arg-ingestion pattern |
| TR actions | 3 | Walk, FlashingDontWalk, DontWalk |

**What would need to compress to approach 8 statements:**

1. Non-negative invariants as field constraints → saves 2 (I: 2 → 0)
2. Guarded-pair implicit fallthrough (`from DontWalk on Advance -> no transition` deleted if guard-only branches have implicit pass-through) → saves 1
3. `CycleCount = CycleCount + 1` shortened to `increment CycleCount` → saves 0 count but reduces line noise
4. Even with all of the above: 29 − 2 − 1 = **26**. Still 3× the gate.

`crosswalk-signal` would need roughly a 75% statement reduction to pass the gate. That is not editing; it is rewriting for a different purpose.

---

### `subscription-cancellation-retention` (36 statements)

This is the emotionally richest compact sample. The intake event is its main inflator:

```
from Active on RequestCancellation -> set SubscriberName = RequestCancellation.Name -> set PlanName = RequestCancellation.Plan -> set MonthlyPrice = RequestCancellation.Price -> set CancellationReason = RequestCancellation.Reason -> set SaveOfferAccepted = false -> transition RetentionReview
```

Five SET statements in a single rule. An `absorb` shorthand would reduce this from 6 actions to 1. Combined with field-level `min 0` constraints (replacing 3 invariants) and one fewer field, this sample *might* reach ~25. Still not 8.

---

### `restaurant-waitlist` (33 statements)

Most compact sample with a collection field. The `PartyQueue as queue of string` and the `dequeue into CurrentParty` pattern show queue semantics in only 33 statements. But even stripping it to a queue-less 2-state version would still be ~20+.

---

## 4. Natural Hero Domains

Ranked by emotional hook + DSL coverage potential, not by gate proximity:

### Tier 1: High emotional hook, compact, authentic

**`subscription-cancellation-retention`**  
Every developer has watched a retention flow fail in production. The arc — customer wants to leave, agent makes one save attempt, accept or let go — is crisp and instantly legible. The `because` messages have natural wit potential ("Only one outstanding save offer can exist at a time" is already doing real work). 3 states. Shows `when`, `reject`, `invariant`, direct field mutation, and state-scoped edits. If the hero sample is purpose-built around this domain with fewer fields, it could score extremely high on Voice/Wit and Emotional Hook.

**`restaurant-waitlist`**  
Universal domain (everyone has waited for a table), shows queue semantics cleanly. The `from Accepting on SeatNextParty when PartyQueue.count > 0 -> set LastCalledParty = PartyQueue.peek -> dequeue PartyQueue into CurrentParty -> transition Seating` line is the DSL at its most differentiated — a plain enum cannot do that. Strong Precept Differentiation candidate.

### Tier 2: Elegant mechanics, moderate emotional hook

**`crosswalk-signal`**  
Technically beautiful: `from any on PedestrianRequest` demonstrates the `any` wildcard, `from DontWalk on Advance when RequestPending` shows data-dependent cycling. The domain is legible in 2 seconds. Weaker emotional hook — you don't *feel* anything about a crosswalk. Better as a secondary documentation example.

**`trafficlight`**  
Emergency override adds genuine drama (`from any on Emergency -> ... -> transition FlashingRed`). A developer reading `because "AuthorizedBy is required"` on an emergency event gets the severity immediately. More complex than crosswalk, but the `from any` + `transition FlashingRed` beat is memorable.

### Tier 3: Strong domain, too complex for a hero snippet

**`hiring-pipeline`** — Relatable to developers, but 7 states and complex interviewer-tracking logic. The real DSL interest (set-based pending interviewers) is buried under workflow scaffolding.

**`loan-application`** — The approval guard is genuinely impressive:
```
from UnderReview on Approve when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
```
This is a strong Precept Differentiation moment, but extracting it into a hero sample while satisfying the gate is structurally impossible from the current file.

---

## 5. Top 3 Verbosity Smells

### #1 — Event Argument Ingestion (Frequency: 21/21, Impact: HIGH)

**Construct:** `set Field = EventName.ArgName` repeated N times per intake transition.  
**Why it inflates:** Every event that carries data and transitions to a new state must explicitly map each argument to a field. There is no shorthand. A 5-argument intake event costs 5 SET statements plus 1 transition = 6 actions for one logical operation ("capture this event's data").

**Examples across samples:**

`event-registration.precept` line 54:
```
from Draft on StartRegistration -> set RegistrantName = StartRegistration.Name -> set ContactEmail = StartRegistration.Email -> set SeatsReserved = StartRegistration.Seats -> set AmountDue = StartRegistration.Seats * StartRegistration.PricePerSeat -> transition PendingPayment
```
4 SET statements. The domain only needs: "record this registration." The language requires: "manually copy each argument."

`travel-reimbursement.precept` line 47:
```
from Draft on Submit when Submit.Lodging / Submit.Days <= 350 -> set EmployeeName = Submit.Employee -> set TripDays = Submit.Days -> set LodgingTotal = Submit.Lodging -> set MealsTotal = Submit.Meals -> set MileageTotal = Submit.Miles * Submit.Rate -> set MileageRate = Submit.Rate -> set RequestedTotal = Submit.Lodging + Submit.Meals + (Submit.Miles * Submit.Rate) -> transition Submitted
```
7 SET statements. This is wall-to-wall argument transcription.

`subscription-cancellation-retention.precept` line 39:
```
from Active on RequestCancellation -> set SubscriberName = RequestCancellation.Name -> set PlanName = RequestCancellation.Plan -> set MonthlyPrice = RequestCancellation.Price -> set CancellationReason = RequestCancellation.Reason -> set SaveOfferAccepted = false -> transition RetentionReview
```
5 SET statements. The domain concept: "start a retention review." The implementation: a five-field transcription.

**What a compact form might look like:** An `absorb Event` action that auto-maps event arguments to fields with matching names, with explicit overrides for computed or non-matching fields. Or a field declared as `field SubscriberName from RequestCancellation.Name` to express the binding at declaration time.

---

### #2 — Guard-Pair Header Duplication (Frequency: 21/21, Impact: MEDIUM-HIGH)

**Construct:** `from X on Y when condition -> … + from X on Y -> reject "…"` — every conditional path requires a matching fallthrough header.  
**Why it inflates:** The first-match model requires an explicit unguarded row to capture the "else" case. There is no inline conditional syntax. Every guarded branch doubles its own header cost.

**Examples:**

`apartment-rental-application.precept`:
```
from Submitted on Approve when MonthlyIncome >= RequestedRent * 3 && CreditScore >= 650 && HouseholdSize < 8 -> set ReviewerNote = Approve.Note -> transition Approved
from Submitted on Approve -> reject "Approval requires strong income coverage, acceptable credit, and a manageable household size"
```
Two headers for one conditional decision.

`warranty-repair-request.precept`:
```
from InRepair on FinishRepair when RepairSteps.count > 0 -> set RepairComplete = true -> transition ReadyToReturn
from InRepair on FinishRepair -> reject "At least one repair step must be logged before the repair can finish"
```

In `crosswalk-signal`, 3 of the 6 headers are unguarded fallthrough rows. In `trafficlight`, 4 of 11 headers are fallthrough-only. The pattern accounts for roughly 20–35% of all H counts across the corpus.

**What a compact form might look like:** An inline `else reject "…"` suffix on a guarded rule: `from X on Y when condition -> … else reject "…"`. One header, one statement. Would halve the H cost for all guarded transitions.

---

### #3 — Non-Negative Numeric Invariant Boilerplate (Frequency: 19/21, Impact: MEDIUM)

**Construct:** `invariant FieldName >= 0 because "FieldName cannot be negative"` repeated for every numeric field.  
**Why it inflates:** The `invariant` form is the only way to express a field-level constraint. There is no inline minimum/maximum annotation. A file with 4 numeric fields generates 4 invariant statements before any logic is expressed.

**Examples:**

`loan-application.precept`:
```
invariant RequestedAmount >= 0 because "Requested amount cannot be negative"
invariant ApprovedAmount >= 0 because "Approved amount cannot be negative"
invariant ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"
invariant AnnualIncome >= 0 because "Annual income cannot be negative"
invariant ExistingDebt >= 0 because "Existing debt cannot be negative"
```
5 invariants. 3 are pure non-negative guards.

`travel-reimbursement.precept`:
```
invariant TripDays > 0 because "Trip days must be positive"
invariant RequestedTotal >= 0 because "Requested total cannot be negative"
invariant ApprovedTotal >= 0 because "Approved total cannot be negative"
invariant ApprovedTotal <= RequestedTotal because "Approved total cannot exceed the request"
```
4 invariants — all structural, not semantic.

**What a compact form might look like:** A field-level range annotation: `field Amount as number default 0 min 0` or `field TripDays as number default 1 min 1`. The invariant statement would only be needed for cross-field relational constraints (`ApprovedAmount <= RequestedAmount`) where inline annotation is insufficient. This would eliminate roughly 60–70% of all `invariant` statements across the corpus.

---

## Summary

The 6–8 gate is unreachable by trimming any existing sample. These are reference implementations documenting DSL coverage — they are not hero snippet candidates. The gate is a constraint for a purpose-built hero snippet that has not yet been written.

The three verbosity smells (event argument ingestion, guard-pair duplication, non-negative boilerplate) together account for the majority of the count gap. For a purpose-built hero snippet in a compact fictional domain:

- Eliminate argument ingestion (use hypothetical `absorb`) → saves 4–7 statements
- Eliminate non-negative invariants (field-level constraints) → saves 2–4 statements
- Collapse guard pairs to `else reject` → saves 1–3 statements

Even with all three improvements, achieving 8 requires a domain with ≤2 fields, ≤2 states, ≤1 event, and ≤1 guarded transition — which is borderline too simple to demonstrate DSL Coverage at the required floor (invariant, reject, when, transition, multiple states). The gate and the DSL Coverage floor are in tension. That tension is the core finding.

**Most emotionally compelling hero domains in the existing corpus:** `subscription-cancellation-retention` and `restaurant-waitlist`. Both are compact, both have authentic domain hooks, and both show the DSL's differentiated capabilities without requiring a 5-state scaffold.
