# George Audit: Current Sample Corpus

Date: 2026-04-08  
Scope: current `samples\` corpus, current runtime/spec behavior, and open language proposal issues relevant to sample realism.

## What I reviewed

- Runtime/spec context:
  - `docs\PreceptLanguageDesign.md`
  - `docs\RulesDesign.md`
  - `docs\ConstraintViolationDesign.md`
  - `precept_language` runtime reference
- Current samples: all 21 `.precept` files in `samples\`
- Open language-enhancement issues most relevant to sample realism:
  - `#8` named rule declarations
  - `#9` conditional expressions
  - `#10` string `.length`
  - `#11` event argument `absorb`
  - `#13` field-level constraints
  - `#14` declaration `when` guards
  - `#15` string `.contains()`
  - `#16` built-in functions
  - `#17` computed fields
  - `#25` choice type
  - `#26` date type
  - `#27` decimal type
  - `#31` keyword logical operators

## Executive read

The corpus is good at showing that Precept can model a single-record workflow with state, event asserts, guards, entry/exit actions, and collection mutation. It is weak at showing believable business systems with typed categories, dates, money, policy tables, partial outcomes, reopen/appeal loops, and multi-actor coordination.

The samples currently optimize for syntax coverage more than domain realism. That makes them useful as parser/runtime fixtures, but it also pushes authors toward toy surrogates: day counters instead of dates, numeric severities instead of named categories, `"READY"` codes instead of generated values, and repeated `set Field = Event.Arg` transcription instead of policy-focused rows.

## Inventory of current sample themes

| Sample | Theme | What it demonstrates today |
|---|---|---|
| `apartment-rental-application.precept` | rental screening | guard-heavy approval policy, nullable fields, post-approval obligations |
| `building-access-badge-request.precept` | access request | `set of number`, `add/remove`, `.min/.max`, editability, approval vs rejection |
| `clinic-appointment-scheduling.precept` | appointment booking | modulo arithmetic, reschedule with `no transition`, entry action, nullable note handling |
| `crosswalk-signal.precept` | control system toy | `from any`, countdown mutation, self-looping operational state machine |
| `event-registration.precept` | simple registration | tutorial-style intake, `to` action, payment gate, check-in |
| `hiring-pipeline.precept` | recruiting | `set of string`, repeated guarded rows, transition to decision after last feedback |
| `insurance-claim.precept` | claim handling | document set, approve/deny split, event-arg validation, combined guard logic |
| `it-helpdesk-ticket.precept` | ticket routing | `queue`, `.peek`, `dequeue`, `from any`-like repeated registration rows, reopen loop |
| `library-book-checkout.precept` | circulation | day-counter aging, overdue transition, renewals, loss handling |
| `library-hold-request.precept` | hold fulfillment | `queue` + `stack`, entry/exit actions, handoff state, expiry logic via counters |
| `loan-application.precept` | underwriting | large eligibility guard, approval/decline split, document verification |
| `maintenance-work-order.precept` | facilities work | `from` assert, hours accumulation, urgent-parts guard |
| `parcel-locker-pickup.precept` | locker pickup | `stack`, `.peek`, timed reminders, return after timeout |
| `refund-request.precept` | refund review | approve amount guard, awaiting-return state, cancellation path |
| `restaurant-waitlist.precept` | queueing toy/business hybrid | `queue`, `.peek`, service close/reopen, compact state flow |
| `subscription-cancellation-retention.precept` | save attempt workflow | compact intake, boolean flags, one-off retention offer |
| `trafficlight.precept` | control system toy | `from any`, emergency override, basic counters, branchy state machine |
| `travel-reimbursement.precept` | finance approval | multiplication/division, reimbursement total calculation, approval cap |
| `utility-outage-report.precept` | outage operations | `set` + `queue`, verification before dispatch, dispatch round counter |
| `vehicle-service-appointment.precept` | service estimate | recommendation set, approval-count guard, `from` assert, pickup notification |
| `warranty-repair-request.precept` | repair loop | `stack`, undo via `pop into`, repair completion guard |

## What the corpus exercises well

1. **Core fire pipeline behavior is visible.**  
   Event asserts, first-match row selection, `no transition`, entry/exit actions, and validation rollback all have at least some representation.

2. **Collection actions are covered.**  
   `add/remove`, `enqueue/dequeue`, `push/pop`, `.count`, `.contains`, `.min`, `.max`, and `.peek` all appear in real files.

3. **State-scoped semantics are covered.**  
   `in`, `to`, and `from` assertions/actions all exist in the set.

4. **Common workflow shapes are represented.**  
   Intake → review → approve/deny, queue promotion, reminder loops, and reopen/return flows are present.

## Realism problems and recurring toy patterns

### 1. The corpus is still mostly “single form + single review gate”

Many samples stop at one decisive check:

- submit
- optional review
- approve/deny
- terminal fulfillment

That is valid syntax coverage, but it undersells actual business entropy: reconsideration, appeals, exception handling, compliance hold, partial fulfillment, SLA breach, reassignment, and audit escalation are mostly absent.

### 2. Missing primitive types force obvious surrogates

The samples repeatedly flatten domain data into `number`/`string`/`boolean`:

- `CurrentDay`, `DayNumber`, `MinuteOfDay` instead of dates/times
- `Severity` as `number` instead of a constrained category
- monetary values as `number` instead of exact decimal/money
- domain vocabularies as free-form `string` or `set of string`

This is technically honest, but visually unrealistic. It makes policy-heavy domains look thinner than they really are.

### 3. Intake rows are dominated by event-arg transcription

Issue `#11` is right: the most common visual pattern in the corpus is long runs of:

```precept
-> set Field = Event.Arg
```

That shows the engine, but it does not show the business. In many files, the first major transition reads more like manual DTO mapping than domain policy.

### 4. The samples often choose placeholder values over believable business facts

Examples:

- `ConfirmationCode = "CONFIRMED"`
- `PickupCode = "READY"`
- numeric day counters instead of appointment dates / due dates
- severity `1..5` instead of named severity levels

These are understandable under current language limits, but they make the corpus feel tutorial-first.

### 5. Current string validation is visibly underpowered

Most string checks are just:

- `!= ""`
- `== null || != ""`

That leaves names, notes, emails, license plates, document ids, and reasons almost unmodeled. The samples visibly want:

- max length
- substring/domain checks
- maybe richer formatting later

### 6. Business vocabularies are too often “open string bags”

Several sets/fields are semantically closed domains but modeled as unconstrained strings:

- `MissingDocuments`
- `RecommendedServices`
- `AffectedBlocks` (arguably domain-coded, not arbitrary prose)
- notes/reasons/status-like fields across multiple samples

Without `choice`, these samples cannot show the type checker protecting domain vocabularies.

### 7. The corpus contains two explicit toy/system samples

- `trafficlight.precept`
- `crosswalk-signal.precept`

They are useful as finite-state-machine demos, but they dilute the “domain integrity engine for business workflows” story if they remain a large fraction of the visible catalog.

### 8. Some samples simplify away the very business logic that would make them memorable

- `event-registration` says it is “intentionally tutorial-like”
- `clinic-appointment-scheduling` says slots are “kept intentionally simple”
- several samples deliberately avoid richer policy logic rather than documenting it as aspirational comments

That is the key missed opportunity. The file can stay valid **and** still tell us what the real policy would be next.

## Missing business domains, workflow shapes, and constraint patterns

### Missing business domains

The current set is broad but still light on domains where integrity rules are dense and realistic:

- procurement / purchase approval / spend controls
- invoice matching / AP exceptions
- prior authorization / utilization review
- KYC / AML / sanctions review
- vendor onboarding / security review
- order fulfillment with partial shipment / backorder / substitution
- subscription billing delinquency / dunning / write-off
- incident response / postmortem / severity escalation
- permit / case / licensing review
- benefits enrollment / dependent verification

### Missing workflow shapes

- **partial approval** (approve some, deny some)
- **appeal / reconsideration loops**
- **escalation by age / SLA breach**
- **multi-party approvals / quorum / sequential signoff**
- **parallel prerequisites that converge**
- **suspend / hold / resume**
- **fulfillment in batches**
- **reassignment across teams with ownership history**

### Missing constraint patterns

- category-driven policy (“if type is X, limit is Y”)
- date-window rules (“must be within 30 days”, “cannot schedule inside blackout window”)
- exact-currency rounding / tax / proration
- derived totals and consistency checks that should be computed, not manually synchronized
- typed enumerations with ordinal comparison
- format-ish string checks (email domain, code prefix, identifier length)
- conditional editability beyond pure state membership

## Where sample authors are visibly hitting current language ceilings

### `#11` Event argument `absorb`

This is the loudest sample-level pain. Nearly every intake sample starts with a row dominated by copied argument assignment. It shows up in:

- `loan-application`
- `insurance-claim`
- `subscription-cancellation-retention`
- `refund-request`
- `warranty-repair-request`
- `apartment-rental-application`
- `travel-reimbursement`
- and many others

### `#13` Field-level constraints

The corpus spends a lot of vertical space on obvious local checks:

- `>= 0`
- `> 0`
- non-empty string/event arg assertions

Issue `#13` is correct that this boilerplate is everywhere. The samples often read like they are compensating for a missing field constraint zone.

### `#17` Computed fields

Several files manually store values that are clearly derived:

- `travel-reimbursement` — `RequestedTotal`
- `building-access-badge-request` — `LowestRequestedFloor`, `HighestRequestedFloor`
- `vehicle-service-appointment` — `ApprovedWorkCount`
- `event-registration` — `AmountDue`, `TicketsIssued`

These are maintainable in small samples, but they train authors toward manual synchronization.

### `#26` Date type

The absence of a date/time type visibly bends multiple files:

- `clinic-appointment-scheduling`
- `library-book-checkout`
- `library-hold-request`
- `parcel-locker-pickup`

These domains are inherently calendar-driven. The samples currently fake that with counters.

### `#25` Choice type

The absence of `choice` shows up in two ways:

1. numeric ordinals masquerading as categories  
   - `it-helpdesk-ticket` severity
2. domain-coded string sets with no type safety  
   - `insurance-claim` documents
   - `vehicle-service-appointment` services
   - multiple reason/note/category-ish fields

### `#27` Decimal type

Money-bearing samples currently use `number`:

- `travel-reimbursement`
- `loan-application`
- `insurance-claim`
- `refund-request`
- `subscription-cancellation-retention`

That is fine for demo math, but not for realistic financial language.

### `#9` Conditional expressions

The current corpus mostly avoids value-conditional business logic rather than expressing it. Where it would help:

- conditional fee / payout / cap calculations
- choosing next field values without duplicating whole rows
- policy-dependent defaulting

The sample set currently hides this gap by keeping calculations shallow.

### `#8` Named rule declarations

Current samples avoid repeated named policy concepts by keeping most policies single-site. That makes the language look less repetitive than it really becomes in serious definitions.

Best pressure points:

- `loan-application` eligibility
- `apartment-rental-application` approval policy
- `insurance-claim` approval readiness
- `maintenance-work-order` urgent-parts readiness

### `#10` String `.length` and `#15` string `.contains()`

These are missing everywhere the samples want believable text rules:

- max note lengths
- email sanity checks
- code/id prefix checks
- required substring checks in descriptions / domains / identifiers

### `#14` Declaration guards

Conditional invariants and conditional editability would improve realism in places where the sample currently collapses nuance:

- deductible/police-report conditionality in `insurance-claim`
- plan-specific retention rules in `subscription-cancellation-retention`
- role/lock-dependent editability in `hiring-pipeline`, `vehicle-service-appointment`, `maintenance-work-order`

### `#16` Built-in functions

Higher-value samples want:

- rounding
- `min`/`max` on scalar math, not just collections
- absolute difference / tolerance checks

Finance and scheduling samples are the clearest victims.

## Sample-specific realism notes

### Strongest current business samples

These are the best foundations for “make this more real” work:

- `insurance-claim`
- `loan-application`
- `travel-reimbursement`
- `vehicle-service-appointment`
- `utility-outage-report`
- `maintenance-work-order`

They already have recognizable policy surfaces; they mainly need richer types, derived values, and aspirational comments.

### Weakest current realism

- `trafficlight`
- `crosswalk-signal`
- `event-registration`
- `restaurant-waitlist`

These are not bad samples; they are just disproportionately tutorial/system-oriented relative to the product narrative.

## Where aspirational comments would add the most future-planning value

The right move is not to contort the sample down to today’s language. Keep the valid current logic, then add comments showing the next layer of intended business rules.

### `clinic-appointment-scheduling.precept`

Add comments for:

- real appointment date/time fields once `date`/time support lands (`#26`)
- provider/resource conflict rules
- cancellation cutoff windows (“cannot cancel inside 24 hours unless staff override”)
- reminder schedule rules instead of a single `ReminderSent` boolean

### `travel-reimbursement.precept`

Add comments for:

- per-diem category tables and exact rounding (`#27`, `#16`)
- receipt thresholds by category
- computed totals instead of manual `RequestedTotal` (`#17`)
- conditional policy by trip type / employee tier (`#25`, `#14`, `#9`)

### `insurance-claim.precept`

Add comments for:

- required document **types** rather than arbitrary strings (`#25`)
- police-report-specific requirement instead of generic `MissingDocuments.count == 0`
- partial approvals and deductible/coverage math (`#17`, `#27`, `#9`)
- fraud escalation / SIU review branch

### `loan-application.precept`

Add comments for:

- named underwriting rules (`#8`)
- exact debt-to-income rounding / caps (`#27`, `#16`)
- conditional document requirements by product type (`#14`, `#25`)
- counteroffer / reconsideration path

### `apartment-rental-application.precept`

Add comments for:

- occupant-age / bedroom-count policy using typed categories and conditional rules
- co-signer path
- income source / employment verification
- move-in deadline / lease-expiry dates (`#26`)

### `vehicle-service-appointment.precept`

Add comments for:

- estimate line items, labor + parts totals, tax (`#17`, `#27`)
- advisor override / customer partial approval
- approval expiry window (`#26`)
- typed service categories (`#25`)

### `utility-outage-report.precept`

Add comments for:

- crew skill matching / region eligibility
- severity categorization (`#25`)
- SLA/escalation windows (`#26`)
- staged restoration / customer-communication obligations

### `hiring-pipeline.precept`

Add comments for:

- required interviewer mix by role
- scorecard thresholds as named reusable rules (`#8`)
- offer band / compensation rounding (`#27`)
- recruiter vs hiring-manager edit restrictions (`#14`)

### `library-book-checkout.precept`, `library-hold-request.precept`, `parcel-locker-pickup.precept`

Add comments for:

- real dates rather than counters (`#26`)
- generated pickup codes / expiry timestamps
- patron-category policy differences (`#25`)
- fee/proration/waiver rules (`#27`, `#16`)

## Concrete corpus-planning conclusions

1. **Keep the current samples, but stop pretending simplifications are the real policy.**  
   When a domain is flattened for current language reasons, say so in comments.

2. **Prioritize richer business workflows over more small tutorial flows.**  
   Doubling the corpus should mostly add denser, policy-shaped domains rather than more toy controllers.

3. **Use future-facing comments to turn samples into planning artifacts.**  
   A valid current sample plus 4-6 honest aspirational comments is more valuable than a “clean” but artificially simple sample.

4. **Treat type-system proposals as sample realism multipliers.**  
   `choice`, `date`, and `decimal` will change sample quality more than almost any syntax sugar.

5. **Use the best existing files as anchors for future language work.**  
   `insurance-claim`, `loan-application`, `travel-reimbursement`, and `vehicle-service-appointment` should become the flagship pressure-test samples for upcoming proposals.

## Verdict

The current corpus proves that the runtime can model workflows. It does **not yet** prove that Precept can carry believable business policy without distortion. The next sample pass should optimize for domain truth first, then annotate the gaps where current language support is not there yet.
