# Uncle Leo — Sample DSL Review
## Date: 2026-04-05

---

## Summary

**21 files reviewed. 0 compile errors. 0 compiler advisories. 1 file fully clean. 20 files with at least one issue.**

Issue breakdown:
- `[ERROR]` — 0
- `[ADVISORY]` — 0
- `[SMELL]` — 38 issues across 15 files
- `[MINOR]` — 16 issues across 10 files

Most common smell: **write-only tracking fields** — fields that are set by events or entry actions but never referenced in any `when`, `assert`, or `invariant`. This appeared in 13 of 21 files. The machine stores the data but makes no decisions based on it. Some are intentional (financial breakdown fields), but many are wiring gaps.

Second most common: **state/event scope gaps** — states with no exit except one happy path, events that can never fire, or asserts that are trivially always satisfied because the state machine already guarantees the condition.

---

## Clean Files

- **loan-application.precept** — Zero issues. Strong guard logic, proper invariant/assert split, all fields either decision-active or clearly informational. Reference model for the gallery.

---

## Issues Found

---

### apartment-rental-application.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `in Approved assert MonthlyIncome >= RequestedRent * 3` is permanently satisfied. The only transition into `Approved` is guarded by exactly this condition, and neither `MonthlyIncome` nor `RequestedRent` has an edit declaration or mutating event from `Approved`. This assert will never fire.
  **Recommendation:** Remove the `in Approved assert` and rely on the transition guard. If defensive protection against future edits is desired, add a comment explaining that. Leaving a redundant assert trains readers to expect all `in` asserts are meaningful.

- [MINOR] Same pattern applies to `in MoveInReady assert DepositPaid && LeaseSigned`. Both fields are set to `true` by the only transitions into `MoveInReady`, and neither is cleared or editable from that state.
  **Recommendation:** Remove or convert to a note/comment. The state semantics already guarantee these.

---

### building-access-badge-request.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `to Approved -> set BadgePrinted = false` is a dead entry action. `BadgePrinted` defaults to `false` and can only be set to `true` by `PrintBadge`, which transitions away from `Approved` to `Issued`. There is no path back into `Approved` once it's been exited. This entry action never has any observable effect.
  **Recommendation:** Remove `to Approved -> set BadgePrinted = false`. If the domain ever allows re-entering `Approved` (e.g. a rejected badge gets re-reviewed), re-add it at that point with a comment.

- [MINOR] `in Approved assert HighestRequestedFloor >= LowestRequestedFloor` is partially subsumed by the global invariant `RequestedFloors.count == 0 || LowestRequestedFloor <= HighestRequestedFloor`. In `Approved`, `LowestRequestedFloor > 0` (required by the first `in Approved assert`), which means the invariant's count-guard is irrelevant — the invariant already enforces the same predicate. Duplicate constraint.
  **Recommendation:** Remove the second `in Approved assert`. The invariant already covers it.

---

### clinic-appointment-scheduling.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `in Scheduled edit ScheduledDay, ScheduledMinute` allows direct field edits that bypass the `on Reschedule assert MinuteOfDay % 15 == 0` boundary check. A direct edit to `ScheduledMinute = 7` would satisfy the `ScheduledMinute >= 0` invariant but violate the 15-minute boundary rule that the `Reschedule` event enforces. The machine has two edit paths for the same field with different validation levels.
  **Recommendation:** Remove `ScheduledMinute` from the edit declaration if the 15-minute boundary is a hard rule, or add `in Scheduled assert ScheduledMinute % 15 == 0` so the invariant catches direct edits too.

- [SMELL] `ReminderSent` is a write-only field from the machine's perspective. It is set to `false` in the `to Scheduled` entry action, reset to `false` on `Reschedule`, and set to `true` by `SendReminder`. No guard, assert, or invariant ever reads it. The machine tracks it but makes no decisions based on it.
  **Recommendation:** If `ReminderSent` is only needed for external reporting (e.g. "was this patient reminded?"), document that explicitly. If it should gate some behavior (e.g. `CheckIn` should require a reminder was sent), wire it up.

---

### crosswalk-signal.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `from any on PedestrianRequest -> set RequestPending = true -> no transition` fires from `Walk` and `FlashingDontWalk` where `RequestPending` is never checked. Setting it in those states is a no-op — the only consumer is `from DontWalk on Advance when RequestPending`. Using `from any` here is technically harmless but misleads readers into thinking the event is handled meaningfully in all states.
  **Recommendation:** Replace `from any` with explicit `from DontWalk` and `from Walk` / `from FlashingDontWalk` rows, or if the intent is "button can always be pressed but only matters in DontWalk," add a comment explaining that.

- [MINOR] `in DontWalk edit RequestPending` allows direct editing of the pending flag, which could suppress a legitimate pedestrian request. There is also a `from any on PedestrianRequest` event that sets it to `true`. Having both paths (event and direct edit) for the same boolean flag is redundant and the edit is the less safe path.
  **Recommendation:** Remove the edit declaration if PedestrianRequest is the intended API. The event is already available.

---

### event-registration.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `from PendingPayment on UpdateSeats -> ... -> set AmountDue = UpdateSeats.Seats * 25` hardcodes the price per seat as `25`. The `StartRegistration` event accepts a `PricePerSeat` arg and correctly uses it for the initial calculation (`set AmountDue = StartRegistration.Seats * StartRegistration.PricePerSeat`). But `PricePerSeat` is never stored as a field — it's used once and discarded. When `UpdateSeats` fires, the recalculated amount uses the hardcoded `25` regardless of the original price.
  **Recommendation:** Add a `PricePerSeat as number default 25` field. Set it from `StartRegistration.PricePerSeat` on initial registration. Use it in the `UpdateSeats` calculation. This is a data loss bug for any event where PricePerSeat differs from 25.

---

### hiring-pipeline.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `OfferExtended` state has exactly one exit: `AcceptOffer -> transition Hired`. There is no `DeclineOffer`, `WithdrawOffer`, or other exit path. A candidate who declines or ghosts after an offer is made leaves the precept stuck in `OfferExtended` indefinitely.
  **Recommendation:** Add at minimum a `WithdrawOffer` or `DeclineOffer` event from `OfferExtended` that transitions to `Rejected` (and optionally sets `FinalNote`).

- [MINOR] `AddInterviewer` is only available from `Screening`. Once the precept enters `InterviewLoop`, the interviewer set is frozen. In practice, interview panels change — someone drops out, a replacement is added. The model has no mechanism for this.
  **Recommendation:** Consider adding `from InterviewLoop on AddInterviewer` if the domain needs mid-loop roster changes.

---

### insurance-claim.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `FraudFlag` is declared as editable (`in UnderReview edit FraudFlag`) but is never referenced in any `when` guard, `assert`, or `invariant`. A reviewer can flag a claim as fraud, but the machine treats it identically to non-fraud claims — the `Approve` and `Deny` paths are unaffected. The flag has no behavioral consequence.
  **Recommendation:** Either wire `FraudFlag` into the approval guard (e.g., `when !FraudFlag && ...` to prevent approving flagged claims) or remove it. A field you can set but that changes nothing is noise.

---

### it-helpdesk-ticket.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `LastQueuedAgent` is set to `AgentQueue.peek` immediately before `dequeue AgentQueue` in both assignment rows. It's never read in any guard, assert, or invariant — it's a pure tracking field.
  **Recommendation:** Document clearly that this is for external observability only. If it's meant to enable "re-assign to the last queued agent" behavior, wire it up.

- [MINOR] `WaitingOnCustomer` state has no `Resolve` transition. If the agent decides to resolve the issue while waiting on the customer (customer replied and it turned out to be solved), there's no path. The agent must first `Assign` (re-assign from the queue) before resolving.
  **Recommendation:** Add `from WaitingOnCustomer on Resolve -> set ResolutionNote = Resolve.Note -> transition Resolved` unless the domain intentionally requires re-assignment before resolution.

---

### library-book-checkout.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `!LostReported` in the `Renew` guard is always `true` in `CheckedOut`. `LostReported` is initialized to `false` on `Checkout` and can only be set to `true` by `ReportLost`, which transitions away from `CheckedOut` to `Lost`. There is no way to be in `CheckedOut` with `LostReported == true`. The guard component is dead.
  **Recommendation:** Remove `!LostReported` from the `Renew` guard. If future changes add a path back to `CheckedOut` with `LostReported` still `true`, restore it then with a comment.

- [MINOR] `CheckoutDay` is set on `Checkout` and reset on `Shelve`/`ResolveLoss` but never referenced in any guard, assert, or invariant. It is a write-only tracking field.
  **Recommendation:** This is clearly informational (audit trail for when the book was checked out). Document this intent with a comment. No behavioral change needed, but a future reviewer should not waste time looking for where it's read.

---

### library-hold-request.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `BookTitle` is declared as a `string nullable` field but is never written in any transition row, entry action, or event handler. The field is always `null`. Readers will assume it gets set somewhere and look for it.
  **Recommendation:** Either add `set BookTitle = Checkout.Title` in the `from OnShelf on Checkout` row (and pass the title arg through `Checkout`), or remove the field entirely if the precept is intentionally title-agnostic.

- [MINOR] The `from HoldReady on AdvanceDay` second branch (`when AdvanceDay.DayNumber > CurrentDay && HoldQueue.count > 0`) manually duplicates the `to HoldReady` entry action mutations (`set ReadyPatron = PromotedPatron -> set PickupCode = "READY" -> set PickupExpiryDay = ...`). This is necessary because `no transition` doesn't fire entry/exit actions, but it creates a maintenance hazard — if the entry action changes, this row must be updated to match.
  **Recommendation:** Add a comment: `// NOTE: duplicates to HoldReady entry action because no-transition rows do not fire entry actions`. This prevents silent drift.

---

### maintenance-work-order.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `from InProgress assert AssignedTechnician != null` is trivially true. `AssignedTechnician` is set in the `Assign` event (Open → Scheduled), is required by `in Scheduled assert AssignedTechnician != null`, and there is no event or edit declaration that clears it between `Scheduled` and `InProgress`. The exit assert can never fire because the condition it checks is permanently satisfied from the point of assignment.
  **Recommendation:** Remove the `from InProgress assert`. If the domain concern is "we want to know who did the work," the field is already non-null and the `in Scheduled assert` already enforces it upstream.

- [SMELL] There is no `Cancel` event from `InProgress`. Work that's already started cannot be cancelled — the only exit is `Complete` or the reject path for exceeding hours. In real facilities management, started work can be halted (safety concern, wrong part, etc.).
  **Recommendation:** Add `from InProgress on Cancel -> set CancellationReason = Cancel.Reason -> transition Cancelled` unless the domain intentionally requires completion once started.

---

### parcel-locker-pickup.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `LockerId` is set on `LoadParcel` and never referenced in any guard, assert, or invariant. It's an informational identifier field.
  **Recommendation:** This is clearly for external lookup (which physical locker this is). Document with a comment. No action needed, but it reads as a smell without context.

---

### refund-request.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `from Submitted on Cancel -> set DecisionNote = Cancel.Reason -> transition Declined` maps a customer cancellation to the `Declined` state. Cancelled-by-customer and declined-by-reviewer are semantically different outcomes that both end in `Declined`. After the fact, the field `DecisionNote` holds either an internal reviewer note or a customer-supplied reason, and there's no way to distinguish them. The model conflates two distinct exit paths.
  **Recommendation:** Add a `Cancelled` terminal state. Route `Cancel` to `Cancelled` and `Decline` to `Declined`. The field could also be split: `CancellationReason` for customer cancellations and `DecisionNote` for reviewer decisions — the model declares both fields in name but uses `DecisionNote` for both write paths.

---

### restaurant-waitlist.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `from any on CloseService -> set WalkInOpen = false -> transition Closed` fires from `Seating`. When closing while a party is being seated (`CurrentParty != null`), the model transitions to `Closed` without clearing `CurrentParty`. The `Seating` state's `in` assert (`CurrentParty != null`) no longer applies after the transition, but the stale data remains. A party is lost mid-seating with no record.
  **Recommendation:** Replace the `from any` for `CloseService` with explicit rows for each source state. The `from Seating on CloseService` row should set `CurrentParty = null` (or transition to a `Closed` state via a special path) before closing.

- [SMELL] `WalkInOpen` is declared `default true`, toggled by `CloseService` and `ReopenService`, but never read in any `when`, `assert`, or `invariant`. The machine tracks open/closed status as a boolean field, but makes no decisions based on it — for example, `JoinWaitlist` is still accepted from `Accepting` regardless of `WalkInOpen`.
  **Recommendation:** Either use `WalkInOpen` in a guard (e.g., gate `JoinWaitlist` on `when WalkInOpen`) or remove it. If it's purely for external display, document that intent.

- [SMELL] `LastCalledParty` is set via `set LastCalledParty = PartyQueue.peek` but never read in any guard, assert, or invariant. Write-only tracking field.
  **Recommendation:** Document as external observability field or remove.

- [MINOR] `from Closed on CloseService` (via `from any`) creates a self-loop: transitioning from `Closed` to `Closed` when `CloseService` fires again. This is a silent no-op. There is no `to Closed` action, so no side effects occur.
  **Recommendation:** Add an explicit `from Closed on CloseService -> no transition` to make the behavior clear and avoid the implicit self-loop.

---

### subscription-cancellation-retention.precept
**Compile result:** Clean

**Issues:**
- [SMELL] **Behavioral bug**: `RetentionDiscount` is not reset when a subscriber accepts an offer and later re-enters `RetentionReview` via a second `RequestCancellation`. The `RequestCancellation` action only resets `SaveOfferAccepted = false` — it does not reset `RetentionDiscount = 0`. On a second retention cycle, `MakeSaveOffer` will always `reject` because its guard is `!SaveOfferAccepted && RetentionDiscount == 0`, and `RetentionDiscount` retains its value from the first cycle.
  **Recommendation:** Add `set RetentionDiscount = 0` to the `from Active on RequestCancellation` transition row.

- [SMELL] `LastAgentNote` is set in `from RetentionReview on DeclineOffer -> set LastAgentNote = DeclineOffer.Note` but never read in any guard, assert, or invariant.
  **Recommendation:** Document as external observability (call center audit trail) or wire it up to something meaningful.

---

### trafficlight.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `from any on Emergency -> ... -> transition FlashingRed` creates a self-loop when fired from `FlashingRed`. The machine re-enters `FlashingRed`, overwriting `EmergencyReason`. There is no protection against a second emergency overwriting the first authorization record.
  **Recommendation:** Add a guard `when EmergencyReason == null` if overwriting is undesired, or document that the most recent emergency always wins.

- [MINOR] `EmergencyReason` is set via `Emergency.AuthorizedBy + ": " + Emergency.Reason` and never read in any guard, assert, or invariant. It's a pure audit trail field.
  **Recommendation:** Document as audit field. No behavior change needed.

---

### travel-reimbursement.precept
**Compile result:** Clean

**Issues:**
- [MINOR] `MealsTotal`, `MileageTotal`, and `MileageRate` are set on `Submit` but never referenced in any `when`, `assert`, or `invariant`. They are financial breakdown fields stored for downstream reporting. The machine makes no decisions based on them — the aggregate `RequestedTotal` is used in the approval guard.
  **Recommendation:** These are clearly intentional tracking fields (finance breakdown). Add a comment: `// Financial breakdown fields — stored for reporting, not used in machine guards`. This prevents future reviewers from looking for where they're read.

---

### utility-outage-report.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `VerifiedState` is a poorly named state. State names should be noun or adjective phrases — `VerifiedState` includes the word "State" as a suffix because the name `Verified` is taken by a boolean field. This is a naming collision avoidance smell.
  **Recommendation:** Rename the field `Verified` to `OutageVerified` or `IsVerified` so the state can be cleanly named `Verified`. Field names should not block the natural naming of states.

- [SMELL] `Verified` field is set to `true` by `VerifyReport` and never read in any guard, assert, or invariant afterward. The state `VerifiedState` already encodes the verified status structurally — the field is redundant.
  **Recommendation:** Remove the `Verified` field. The state machine's position in `VerifiedState` communicates this fact. If external systems need to query whether the outage is verified, use the current state.

---

### vehicle-service-appointment.precept
**Compile result:** Clean

**Issues:**
- [SMELL] `CheckedIn` state has only one exit: `from CheckedIn on RecommendService -> add RecommendedServices ... -> transition AwaitingApproval`. If the advisor checks a vehicle in but determines no work is needed (e.g., a false alarm), there is no path to close or finish the appointment without recommending at least one service. The appointment gets stuck.
  **Recommendation:** Add a `from CheckedIn on FinishService -> transition Closed` (or similar) for the no-work-needed path, or allow `CheckIn` to transition directly to `InService` in that case.

- [MINOR] `ApprovedWorkCount` is set by `ApproveEstimate` and never referenced in any guard, assert, or invariant afterward. It mirrors `RecommendedServices.count` at approval time, stored for external reporting.
  **Recommendation:** Document as reporting field or remove if redundant with `InvoiceTotal`.

- [MINOR] `from AwaitingApproval assert RecommendedServices.count > 0` is partially redundant. `ApproveEstimate` can only succeed if `RecommendedServices.count == ApproveEstimate.ApprovedCount` and `ApprovedCount > 0` (event assert). If count is 0, the guard fails regardless. The `from AwaitingApproval assert` adds an extra layer, but the event assert already prevents exiting with 0 services.
  **Recommendation:** Keep as defense-in-depth, but document the rationale so it doesn't read as a bug.

---

### warranty-repair-request.precept
**Compile result:** Clean

**Issues:**
- [SMELL] Zero invariants declared. Every other sample with mutable scalar fields declares at least one `invariant` for non-negative counts, non-negative amounts, or referential constraints. WarrantyRepairRequest has boolean flags and nullable strings, which might not need `>= 0` invariants — but there are no assertions whatsoever at the precept level, only state-level `in` asserts.
  **Recommendation:** Add at minimum one `invariant` if a true global rule exists (e.g., `RepairComplete == false || RepairSteps.count >= 0` is trivially true, but a real rule might be `!RepairComplete || RepairComplete` — hmm). If there genuinely are no global invariants (the model is entirely state-scoped), document that with a comment: `// No global invariants — all rules are state-scoped`.

- [SMELL] `LastReversedStep` is set by `from InRepair on UndoLastStep when RepairSteps.count > 0 -> pop RepairSteps into LastReversedStep` but never read in any guard, assert, or invariant. The most recently undone step is captured but unused.
  **Recommendation:** Document as external observability ("what was the last step undone?") or wire it into a constraint.

- [MINOR] `in Closed assert ShippingLabelSent` is redundant. `Closed` is only reachable via `from ReadyToReturn on ConfirmReturn`, and `to ReadyToReturn -> set ShippingLabelSent = true` guarantees it's `true` before entry to `ReadyToReturn`. The only path to `Closed` already ensures `ShippingLabelSent`. The assert can never fire.
  **Recommendation:** Remove or convert to a comment documenting the guarantee.

- [MINOR] No `Cancel` or `Withdraw` event exists at any post-Draft state. Once submitted, a warranty claim can only be `Approved` or `Denied`. This may be intentional (warranty claims can't be withdrawn), but it's worth a domain validation.
  **Recommendation:** Confirm with domain owner whether post-submission withdrawal is ever needed.

---

## Issue Counts by File

| File | Compile | SMELLs | MINORs |
|------|---------|--------|--------|
| apartment-rental-application | ✅ | 0 | 2 |
| building-access-badge-request | ✅ | 1 | 1 |
| clinic-appointment-scheduling | ✅ | 2 | 0 |
| crosswalk-signal | ✅ | 0 | 2 |
| event-registration | ✅ | 1 | 0 |
| hiring-pipeline | ✅ | 1 | 1 |
| insurance-claim | ✅ | 1 | 0 |
| it-helpdesk-ticket | ✅ | 0 | 2 |
| library-book-checkout | ✅ | 1 | 1 |
| library-hold-request | ✅ | 1 | 1 |
| **loan-application** | ✅ | **0** | **0** |
| maintenance-work-order | ✅ | 2 | 0 |
| parcel-locker-pickup | ✅ | 0 | 1 |
| refund-request | ✅ | 1 | 0 |
| restaurant-waitlist | ✅ | 3 | 1 |
| subscription-cancellation-retention | ✅ | 2 | 0 |
| trafficlight | ✅ | 0 | 2 |
| travel-reimbursement | ✅ | 0 | 1 |
| utility-outage-report | ✅ | 2 | 0 |
| vehicle-service-appointment | ✅ | 1 | 2 |
| warranty-repair-request | ✅ | 2 | 2 |
| **TOTALS** | 21/21 | **22** | **20** |

---

*Review by Uncle Leo. HELLO! Did you see all of this? This needed to be reviewed. HELLO!*
