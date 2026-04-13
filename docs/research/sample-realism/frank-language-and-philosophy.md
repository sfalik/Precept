# Sample Realism: Language & Philosophy Research

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-05-17
**Status:** Research artifact — input for sample-realism initiative

---

## Purpose

This document establishes the language-design guardrails, realism criteria, and philosophy for expanding and deepening Precept's sample corpus. It answers four questions:

1. What should a realistic Precept sample optimize for?
2. Where do current samples fall short?
3. Which planned language enhancements most affect sample quality?
4. How should aspirational samples handle not-yet-supported logic?

---

## 1. What a Realistic Precept Sample Should Optimize For

### 1.1 Primary optimization targets

A Precept sample is not an academic exercise. It is the first thing a domain expert, AI agent, or .NET developer reads to decide whether Precept can carry their workflow. Every sample must optimize for these properties, in order:

| Priority | Property | Why |
|----------|----------|-----|
| 1 | **Domain credibility** | If a domain expert reads the sample and says "that's not how this works," the sample is worse than useless. It actively teaches a wrong mental model. |
| 2 | **Language surface coverage** | Samples are the de facto teaching corpus. The collection of samples should collectively exercise every implemented construct. No major feature should be invisible. |
| 3 | **Business rule density** | A realistic workflow enforces real rules — not just `Amount > 0` and `Name != ""`. Guards should encode genuine domain logic: eligibility rules, approval thresholds, capacity constraints, regulatory gates. |
| 4 | **AI authoring demonstrability** | Precept is AI-first (Principle 12). Samples are training data for AI authoring. A sample that an AI can read, understand, and confidently modify is better than one that requires human tribal knowledge. |
| 5 | **Progressive complexity** | The corpus needs an explicit difficulty gradient. Some samples should be entry ramps. Others should show the language under real load. |

### 1.2 What a sample is NOT

- **Not a tutorial.** Tutorials explain step by step. Samples demonstrate by example. Comments should clarify domain intent, not teach syntax.
- **Not a stress test.** A sample that exists solely to exercise an edge case belongs in `test/`, not `samples/`.
- **Not aspirational fiction.** A sample that describes a workflow that doesn't exist in any real organization is misleading. Every sample should be traceable to a real domain pattern — even if simplified.

### 1.3 The realism test

A sample passes the realism test when:

1. A domain expert can read it and recognize the workflow without explanation.
2. The guard conditions encode real business rules, not placeholder arithmetic.
3. The state graph has at least one non-trivial path (branching, loopback, or rejection).
4. Fields carry domain-meaningful names, not generic labels.
5. At least one invariant or assert encodes a real business constraint (not just non-negativity).
6. The sample is self-contained — no external context required to understand the contract.

---

## 2. Where Current Samples Fall Short

### 2.1 The toy problem

Three current samples read as toy demos rather than domain contracts:

| Sample | Issue |
|--------|-------|
| `crosswalk-signal` | Only 2 events, 3 fields, 3 states. No real-world crosswalk complexity (ADA, timing coordination, vehicle signal interaction). Useful as a teaching entry point but should not count toward the "realistic" corpus. |
| `restaurant-waitlist` | Only 5 events. Real waitlists track party size, estimated wait, text notifications, VIP priority, no-show timeouts. The current sample is a queue exercise, not a restaurant operations model. |
| `trafficlight` | Better than the crosswalk (emergency override is genuine), but fundamentally toy: no timing, no sensor integration, no pedestrian phase. The emergency mode saves it from pure toy status. |

**Verdict:** These three should be explicitly labeled as "teaching samples" (simple entry ramps), not realistic domain models. The corpus needs to be honest about what's a tutorial and what's a real contract.

### 2.2 The boilerplate problem

Every sample suffers from the same repetitive patterns that obscure domain logic:

| Pattern | Prevalence | Lines wasted per sample | Root cause |
|---------|-----------|------------------------|------------|
| `set Field = Event.Arg` intake chains | 21/21 samples | 3–7 lines per intake event | No `absorb` shorthand (#11) |
| `invariant X >= 0` / `invariant X <= N` | 18/21 samples | 2–5 lines per sample | No field-level constraints (#13) |
| `on Event assert Arg != ""` | 20/21 samples | 2–4 lines per event | No `notempty` constraint on args (#13) |
| Duplicated guard expressions | 6/21 samples | 2–4 duplicated lines | No named rules (#8) |
| Duplicated transition rows for value-conditional assignments | 4/21 samples | Full row duplication | No `if...then...else` expressions (#9) |

**Cumulative impact:** In a typical 50-line sample, 15–25 lines are boilerplate that could be compressed by planned language features. This means the current samples are 30–50% ceremony and 50–70% domain logic. The ratio should be inverted.

### 2.3 The semantic type problem

The most damaging shortcoming across the sample corpus is the absence of semantically precise types. Current samples use `string` for categorical values and `number` for all quantities:

| What the domain needs | What the sample uses | Samples affected |
|----------------------|---------------------|-----------------|
| Severity levels (Low/Medium/High/Critical) | `number` with invariant chains | it-helpdesk-ticket |
| Document types (ProofOfLoss, PoliceReport, etc.) | `set of string` with no validation | insurance-claim, warranty-repair-request |
| Service types (OilChange, BrakeInspection, etc.) | `set of string` with no validation | vehicle-service-appointment |
| Geographic zones | `set of string` with no validation | utility-outage-report |
| Currency amounts | `number` (IEEE 754 double) | loan-application, insurance-claim, travel-reimbursement, refund-request, subscription-cancellation |
| Day counts, renewal counts, feedback counts | `number` (allows 2.7 renewals) | library-book-checkout, hiring-pipeline, it-helpdesk-ticket |
| Calendar dates | `number` (DaysSinceCheckedOut) | library-book-checkout, clinic-appointment-scheduling |
| Percentages | `number` with manual 0–100 invariant | subscription-cancellation-retention |

**Impact:** These type gaps make samples look like prototype code rather than production contracts. A domain expert reads `field Severity as number default 3` and immediately wonders "number? It's a severity level." This is the single biggest credibility problem in the corpus.

### 2.4 The shallow-guard problem

Most samples have guards that check only one or two conditions. Real business workflows have compound eligibility rules that combine multiple field checks, categorical conditions, and threshold logic. The current corpus has exactly two samples with genuinely complex guards:

- `loan-application`: 5-condition underwriting guard (documents + credit + income + debt + amount)
- `insurance-claim`: 3-condition approval guard (police report + documents + amount ceiling)

The rest have guards like `when PendingInterviewers.count > 0` or `when SaveOfferAccepted == false`. These are structurally correct but domain-shallow.

### 2.5 The missing-domain problem

The current 21 samples cluster in a few domain families:

| Domain cluster | Count | Examples |
|---------------|-------|---------|
| Service/intake workflows | 7 | Loan, insurance, warranty, refund, rental, reimbursement, maintenance |
| Queue/scheduling | 4 | Clinic, restaurant, parking, event registration |
| Document/approval flows | 3 | Badge request, hiring, utility outage |
| Infrastructure/IoT | 3 | Traffic light, crosswalk, parcel locker |
| Subscription/retention | 2 | Subscription cancellation, library checkout |
| Library management | 2 | Book checkout, hold request |

**Missing entirely:**
- **Financial services:** Payment processing, invoice lifecycle, expense approval chains, billing disputes
- **Healthcare beyond scheduling:** Patient intake, treatment authorization, medication order, discharge planning
- **Compliance/regulatory:** Audit workflows, regulatory filing, license renewal, policy exception requests
- **E-commerce:** Order fulfillment, return merchandise authorization, cart-to-checkout, inventory reservation
- **Software/DevOps:** Incident response, change management, deployment pipeline, feature flag lifecycle
- **Legal:** Contract lifecycle, case management, document review, settlement negotiation
- **Manufacturing:** Quality control, defect tracking, batch release, equipment calibration
- **HR beyond hiring:** Employee onboarding, leave request, performance review, disciplinary action
- **Government:** Permit application, inspection scheduling, public records request, grant application

### 2.6 The absence of data-only contracts

Issue #22 (data-only precepts) proposes stateless precepts for entities that don't need workflow protection. The current sample corpus has zero examples of this pattern. Yet most real domains have reference data entities (Customers, Products, Adjusters, Policies) that need field integrity without state machines. This is a conspicuous gap.

---

## 3. Which Planned Enhancements Most Affect Sample Quality

I've reviewed all 15 open proposal issues. Here is my assessment of which ones most improve sample realism, ranked by impact on the sample corpus:

### Tier 1: Transformative impact on sample quality

| Issue | Feature | Wave | Impact on samples |
|-------|---------|------|-------------------|
| **#25** | `choice` type | Wave 3 | **Highest-impact single feature.** Eliminates `string` for all categorical fields, adds compile-time value safety, enables ordinal comparison for severity/priority/tier fields. Affects 15+ samples. Every sample that uses `string` for a finite value set becomes more credible. |
| **#9** | `if...then...else` | Wave 1 | **Eliminates row duplication** for value-conditional assignments. Enables inline conditional logic in `set` expressions. Affects 4–6 samples directly, improves readability of guards in many more. |
| **#13** | Field-level constraints | Wave 2 | **Eliminates 54+ invariant lines** across the corpus. `nonnegative`, `positive`, `notempty`, `maxlength` replace manual invariant chains. Makes every sample shorter and more focused on domain logic. |
| **#31** | `and`/`or`/`not` keywords | Wave 1 | **Every compound guard in every sample** becomes more readable. `when not IsPremium and Score >= 680` vs `when !IsPremium && Score >= 680`. Affects all 21 samples. |

### Tier 2: Significant impact on realistic samples

| Issue | Feature | Wave | Impact on samples |
|-------|---------|------|-------------------|
| **#29** | `integer` type | Wave 3 | Fixes the "2.7 renewals" problem. Count fields, score fields, day fields all get semantic precision. Affects 12+ samples. |
| **#27** | `decimal` type | Wave 3 | Fixes the financial precision problem. Every sample with currency amounts (`number` today) should be `decimal`. Affects 8+ samples. |
| **#14** | Conditional invariants | Wave 2 | Enables tier-based, category-based, and flag-based constraints without state explosion. New samples can express "premium tier limit is 100, standard is 10" in one invariant. |
| **#17** | Computed fields | Wave 4 | Eliminates manual `RequestedTotal` synchronization. `travel-reimbursement` and `loan-application` become dramatically cleaner. Enables derived fields in new samples. |
| **#8** | Named rules | Wave 4 | Eliminates guard duplication in `loan-application` (5-condition rule repeated 3 times). Enables named business policies in complex samples. |

### Tier 3: Targeted impact

| Issue | Feature | Wave | Impact on samples |
|-------|---------|------|-------------------|
| **#22** | Data-only precepts | Wave 1 | Enables a new category of samples: reference data entities without state machines. |
| **#11** | `absorb` shorthand | Wave 4 | Compresses intake transitions (3–7 `set` lines → 1 `absorb` line). Affects all 21 samples. High noise reduction, moderate realism impact. |
| **#26** | `date` type | Wave 3+ | Enables calendar-aware samples: appointment scheduling, SLA tracking, renewal dates. Currently faked with `number`. |
| **#10** | `.length` accessor | Wave 1 | Enables string length validation in constraints. Table-stakes but narrow. |
| **#15** | `.contains()` | Wave 5 | Enables substring checks (email domain, keyword detection). Narrow but genuine. |
| **#16** | Built-in functions | Wave 5 | Enables `min()`, `max()`, `abs()`, `round()` in expressions. Affects computed-value and financial samples. |

### Wave-to-sample-impact summary

| Wave | Features | Sample impact |
|------|----------|--------------|
| **Wave 1** | `if...then...else`, `and`/`or`/`not`, `.length`, data-only precepts | Every sample reads better (keywords), 4–6 gain conditional expressions, new sample category unlocked |
| **Wave 2** | Field constraints, conditional invariants | 54+ invariant lines eliminated, complex-domain samples become expressible |
| **Wave 3** | `choice`, `integer`, `decimal`, `date` | The type system becomes credible. 15+ samples gain semantic precision. Financial samples become trustworthy. |
| **Wave 4** | Named rules, `absorb`, computed fields | Boilerplate collapse. Complex samples lose 30–40% of their lines. Named business policies emerge. |
| **Wave 5** | `.contains()`, built-in functions | String and numeric expressiveness. Enables niche but genuine domain patterns. |

---

## 4. Philosophy Guardrails for Aspirational Samples

Shane's directive is clear: "Don't let current language limitations constrain the sample ideas: if business logic can't be expressed today, add comments showing the intended logic instead." This is the right call. But it requires discipline.

### 4.1 The comment protocol

When a sample uses proposed-but-not-implemented syntax, it MUST follow this protocol:

**Rule 1: Working code first, aspirational comments second.**
Every sample must compile and pass `precept_compile` with zero errors using today's language. Aspirational logic appears in comments only. A sample that doesn't compile is not a sample — it's a spec.

**Rule 2: Aspirational comments use a standard prefix.**
```precept
# FUTURE(#9): set TransactionFee = if PaymentMethod == "Card" then 2.50 else 0.50
# FUTURE(#25): field Severity as choice("Low", "Medium", "High", "Critical") ordered
# FUTURE(#13): field Amount as decimal nonnegative
```

The `FUTURE(#N)` prefix links directly to the GitHub issue. An AI agent can grep for `FUTURE(#25)` to find every sample that would benefit from the choice type. A human reviewer can see at a glance which features would improve this specific sample.

**Rule 3: Show the current workaround alongside the aspirational form.**
```precept
# Today's workaround:
field Severity as number default 3
invariant Severity >= 1 because "Severity minimum"
invariant Severity <= 5 because "Severity maximum"

# FUTURE(#25): Replace with:
# field Severity as choice("Info", "Low", "Medium", "High", "Critical") ordered default "Medium"
```

This keeps the sample honest about what works today while showing the improvement path. Omitting the workaround makes the comment look like broken code.

**Rule 4: Never use aspirational syntax in active (non-commented) lines.**
No mixing. If a line compiles, it uses today's syntax. If it uses proposed syntax, it's a comment. Period.

**Rule 5: Each aspirational comment block must cite the specific issue number.**
This is the traceability requirement. When issue #25 ships, the team greps `FUTURE(#25)` and updates every sample in one pass. Without issue numbers, aspirational comments become stale fiction.

### 4.2 The modeling-over-syntax principle

The most important guardrail is not about syntax — it's about modeling choices. Current language limitations should NEVER constrain the *domain model* of a sample. They should only constrain the *expression* of that model.

**Example — don't simplify the domain because the type system is narrow:**

❌ Wrong: "We don't have `choice`, so let's not model severity levels — just use a boolean `IsUrgent`."

✅ Right: Model severity as `number default 3` with `invariant Severity >= 1` and `invariant Severity <= 5`, then add `FUTURE(#25)` comments showing the `choice` type version.

The domain model is the sample's reason for existing. The syntax is how we express it today. When the syntax improves, the model stays the same — only the expression gets cleaner.

### 4.3 The domain-first authoring sequence

When writing a new sample, follow this sequence:

1. **Pick a real domain workflow.** Not a hypothetical. Name a real business process that exists in real organizations.
2. **List the domain entities, states, events, and rules.** Do this in plain English first. What are the fields? What are the states? What are the business rules? What events drive transitions?
3. **Identify which rules can be expressed today.** Map each business rule to current Precept syntax.
4. **Identify which rules need future features.** Map each rule that can't be expressed to a specific proposal issue.
5. **Write the sample using today's syntax.** Make it compile. Make it correct.
6. **Add FUTURE comments for the unsupported rules.** Follow the comment protocol above.
7. **Verify domain credibility.** Would a domain expert recognize this workflow? Are the field names, state names, and event names idiomatic for the domain?

### 4.4 Things aspirational comments should NOT do

- **Don't invent syntax.** If a business rule needs a feature that has no open proposal, don't invent one. Describe the rule in plain English and note "no current proposal covers this."
- **Don't assume proposal shapes will ship as-is.** Proposals evolve. Use the current proposal syntax but acknowledge it may change.
- **Don't use comments as a substitute for modeling.** If a rule can be expressed (even awkwardly) with today's syntax, express it. Don't skip it just because a future feature would make it cleaner.
- **Don't aspirational-comment every line.** A sample where 40% of the lines are FUTURE comments is not a working sample — it's a wish list. Limit aspirational comments to genuinely blocked logic, not cosmetic improvements.

---

## 5. Realism Criteria for Sample Authoring

These criteria apply to every new or revised sample. They are designed to be checkable by any team member (human or AI).

### 5.1 Structural criteria (MUST pass)

| # | Criterion | Check |
|---|-----------|-------|
| S1 | **Compiles clean.** `precept_compile` returns zero errors, zero warnings on the active (non-commented) code. | Automated |
| S2 | **Domain-real name.** The precept name and all field/state/event names use domain-idiomatic terminology, not generic labels. | Manual review |
| S3 | **Non-trivial state graph.** At least 4 states (3 + initial), at least 2 events, at least one branching path (guard or rejection). Teaching samples (explicitly labeled) may have fewer. | Count check |
| S4 | **At least one real business rule.** At least one guard, invariant, or assert encodes a genuine domain constraint — not just `>= 0` or `!= ""`. | Manual review |
| S5 | **Self-contained.** No external context required to understand the contract. Header comment explains the domain in 1–3 sentences. | Manual review |
| S6 | **Aspirational comments follow protocol.** Every FUTURE comment cites an issue number. No aspirational syntax in active lines. | Grep check |

### 5.2 Quality criteria (SHOULD pass — aim for all, accept exceptions with justification)

| # | Criterion | Check |
|---|-----------|-------|
| Q1 | **Compound guard.** At least one transition row has a guard with 2+ conditions joined by `&&`/`and`. | Pattern check |
| Q2 | **Collection usage.** Uses at least one `set`, `queue`, or `stack` with meaningful domain semantics. | Feature check |
| Q3 | **Rejection path.** At least one `reject` outcome with a domain-meaningful message. | Pattern check |
| Q4 | **Edit declaration.** At least one `in <State> edit` declaration showing state-scoped editability. | Pattern check |
| Q5 | **State assertion.** At least one `in`/`to`/`from` assert encoding a state-specific constraint. | Pattern check |
| Q6 | **Non-trivial invariant.** At least one invariant that constrains relationships between fields (not just single-field bounds). | Manual review |
| Q7 | **Event argument validation.** Events with arguments have assert constraints on those arguments. | Pattern check |
| Q8 | **Multiple event paths from one state.** At least one state has 2+ events with different outcomes. | Structural check |

### 5.3 Corpus-level criteria (apply to the full set, not individual samples)

| # | Criterion | Target |
|---|-----------|--------|
| C1 | **Domain diversity.** At least 10 distinct domain families represented (finance, healthcare, legal, manufacturing, HR, etc.). | Count |
| C2 | **Complexity gradient.** At least 5 simple (teaching), 15 medium, 10 complex samples in a 40-sample corpus. | Count |
| C3 | **Feature coverage.** Every implemented construct appears in at least 3 samples. | Audit |
| C4 | **Aspirational coverage.** Each Wave 1–3 proposal issue is demonstrated in at least 2 samples via FUTURE comments. | Grep audit |
| C5 | **Data-only representation.** At least 3 data-only precepts (when #22 ships) or commented equivalents. | Count |
| C6 | **Collection type coverage.** `set`, `queue`, and `stack` each appear in at least 4 samples with meaningful operations. | Count |

### 5.4 The complexity classification

Samples should be explicitly classified:

| Level | States | Events | Guards | Business rules | Target audience |
|-------|--------|--------|--------|----------------|----------------|
| **Teaching** | 2–3 | 2–3 | 0–1 simple | Minimal | First-time readers |
| **Standard** | 4–6 | 4–7 | 2–4, at least one compound | 2–3 real constraints | Working developers |
| **Complex** | 5–8+ | 5–10+ | 3–6, compound and multi-path | 4+ real constraints with interactions | Architects and domain experts |

Each sample file should declare its classification in its header comment:
```precept
precept LoanApplication
# Complexity: Complex
# Domain: Financial services — consumer lending
# A loan application workflow with underwriting rules...
```

---

## 6. Recommendations for Doubling the Corpus

### 6.1 New sample candidates by domain

The following domains are currently unrepresented or underrepresented and have strong Precept fit (state-driven, rule-heavy, inspectable):

| Domain | Sample name | Complexity | Key language features exercised |
|--------|------------|-----------|-------------------------------|
| **E-commerce** | `order-fulfillment` | Complex | Collection of line items, payment state, shipping state, partial fulfillment |
| **E-commerce** | `return-merchandise-authorization` | Standard | Return reasons (choice), refund calculation, restocking fee |
| **Healthcare** | `patient-intake` | Standard | Insurance verification, consent tracking, triage priority (choice) |
| **Healthcare** | `treatment-authorization` | Complex | Clinical criteria guards, multi-level approval, denial/appeal cycle |
| **Compliance** | `regulatory-filing` | Standard | Filing window dates, amendment tracking, submission validation |
| **Compliance** | `policy-exception-request` | Complex | Risk scoring, approval chain, expiration tracking |
| **Software/DevOps** | `incident-response` | Complex | Severity escalation (choice), on-call rotation, postmortem requirement |
| **Software/DevOps** | `change-management` | Standard | Change type classification, approval gates, rollback capability |
| **Legal** | `contract-lifecycle` | Complex | Version tracking, multi-party approval, amendment chain, termination |
| **Manufacturing** | `quality-inspection` | Standard | Pass/fail/conditional criteria, defect categorization (choice), batch hold |
| **Manufacturing** | `equipment-calibration` | Standard | Calibration schedule, tolerance checking, certificate tracking |
| **HR** | `employee-onboarding` | Complex | Document collection (set), task completion tracking, multi-department approval |
| **HR** | `leave-request` | Standard | Accrual balance checking, manager approval, overlap detection |
| **HR** | `performance-review` | Standard | Multi-reviewer feedback, rating aggregation, calibration |
| **Finance** | `invoice-lifecycle` | Standard | Payment terms, partial payment tracking, dispute resolution |
| **Finance** | `expense-approval` | Complex | Policy limits by category, multi-level approval, receipt requirements |
| **Government** | `permit-application` | Complex | Zoning rules, inspection scheduling, fee calculation, renewal |
| **Government** | `public-records-request` | Standard | Redaction tracking, response deadline, exemption handling |
| **Data-only** | `customer-profile` | Teaching | No states — field integrity only. Demonstrates #22 pattern. |
| **Data-only** | `product-catalog-entry` | Teaching | No states — category validation, price constraints. |

### 6.2 Existing samples that need deepening

| Sample | Current issue | Recommended improvement |
|--------|--------------|------------------------|
| `restaurant-waitlist` | Toy-level | Add party size, estimated wait calculation, no-show handling, VIP priority |
| `crosswalk-signal` | Toy-level | Label as teaching sample explicitly. Keep simple but add ADA comment. |
| `subscription-cancellation-retention` | Too few states | Add PauseSubscription event, win-back state, offer tier logic |
| `event-registration` | Hardcoded price | Add ticket types (choice), early-bird pricing, waitlist overflow |
| `refund-request` | Missing fraud/policy logic | Add return window check, restocking fee logic, fraud flag |
| `clinic-appointment-scheduling` | Missing provider logic | Add provider assignment, insurance verification |

---

## 7. The Language Enhancement → Sample Quality Feedback Loop

This research establishes a bidirectional relationship:

**Samples → Language:** Writing realistic samples reveals expression gaps. If a business rule is common across domains but inexpressible in Precept, that's signal for a language proposal.

**Language → Samples:** When a language proposal ships, samples should be updated in the same pass. The `FUTURE(#N)` grep pattern makes this mechanical.

**The commitment:** Every language enhancement PR that ships must include sample updates. If the feature affects existing samples (found via `FUTURE(#N)` grep), update them. If the feature enables new patterns, add at least one new sample or deepen an existing one.

This is not optional. A language feature that ships without sample updates is a feature that doesn't exist for new users.

---

## 8. Appendix: Issue-to-Sample Impact Matrix

This matrix maps each proposal issue to the samples it would improve, enabling the team to prioritize sample work alongside language work.

| Issue | Feature | Samples directly improved |
|-------|---------|--------------------------|
| #8 | Named rules | loan-application (3 guard duplications), insurance-claim (2), hiring-pipeline (2) |
| #9 | `if...then...else` | travel-reimbursement (mileage calc), event-registration (pricing), loan-application (approval amount), any new financial sample |
| #10 | `.length` | All samples with string args (20/21) — enables `Name.length >= 1` instead of `Name != ""` |
| #11 | `absorb` | All 21 samples — every intake transition compresses |
| #13 | Field constraints | 18/21 samples — `nonnegative`/`positive`/`notempty` replace manual invariants |
| #14 | Conditional invariants | New complex samples (tier-based limits, category-based rules); insurance-claim, loan-application |
| #15 | `.contains()` | New samples with email/keyword validation; utility-outage (zone matching) |
| #16 | Built-in functions | travel-reimbursement (`round`), loan-application (`min`/`max`), financial samples |
| #17 | Computed fields | travel-reimbursement (RequestedTotal), loan-application (debt ratios), any sample with derived totals |
| #22 | Data-only precepts | New reference-data samples (customer-profile, product-catalog-entry, adjuster-record) |
| #25 | `choice` type | it-helpdesk-ticket (severity), insurance-claim (doc types), vehicle-service (service types), utility-outage (zones), all new samples with categorical fields |
| #26 | `date` type | clinic-appointment (appointment date), library-book-checkout (due date), any scheduling sample |
| #27 | `decimal` type | loan-application, insurance-claim, travel-reimbursement, subscription-cancellation, refund-request, all financial samples |
| #29 | `integer` type | hiring-pipeline (feedback count), it-helpdesk-ticket (reopen count), library-book-checkout (renewal count), crosswalk/traffic (cycle counts) |
| #31 | `and`/`or`/`not` | All 21 samples — every compound guard becomes more readable |

---

## 9. Appendix: Current Corpus Statistics

Based on Opus analysis of all 21 samples:

| Metric | Value |
|--------|-------|
| Total samples | 21 |
| Teaching-level (toy) | 3 (crosswalk, restaurant-waitlist, trafficlight) |
| Standard complexity | 10 |
| Complex | 8 |
| Use collections | 14 (67%) |
| Use compound guards | 18 (86%) |
| Use `from any` | 5 (24%) |
| Use `edit` declarations | 14 (67%) |
| Use `to/from` state actions | 8 (38%) |
| Use `from State assert` (exit gates) | 3 (14%) |
| Distinct domain families | ~6 |
| Average fields per sample | ~7.5 |
| Average states per sample | ~5 |
| Average events per sample | ~5.5 |
| Average transition rows per sample | ~10 |

---

*This document is research input for the sample-realism initiative. It establishes criteria and philosophy. Individual sample authoring decisions are owned by the implementing agents, subject to these guardrails.*
