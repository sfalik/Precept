# Steinbrenner Sample Portfolio Plan

Date: 2026-04-08  
Owner: Steinbrenner (PM)  
Scope: planning only

## Executive call

- The repo currently has **21** `.precept` samples.
- The target for the next portfolio pass should be **42 total samples** in-repo: **40 roadmap-grade business samples** plus **2 minimal teaching/control samples** kept for syntax smoke coverage.
- Current breadth is decent, but the corpus is still too weighted toward simple intake/review flows. It is weak on **exact-money**, **calendar-date**, **master-data**, **regulated exception**, and **high-policy-density** scenarios — the exact places future language decisions will be made.

## What the current corpus says

### Current count

| Slice | Count | Notes |
|---|---:|---|
| Total samples | 21 | Current repo state |
| Roadmap-grade business samples | 19 | Most are realistic enough to evolve |
| Minimal teaching/control samples | 2 | `trafficlight`, `crosswalk-signal` |
| Data-only samples | 0 | Clear gap; blocked on #22 if we want true stateless coverage |

### Current portfolio strengths

- Strong starter coverage for intake + review workflows
- Good use of collections (`set`, `queue`) and repeated-event flows
- Enough existing anchors to support realistic rewrites instead of inventing everything from scratch

### Current portfolio gaps

- **Time is faked as numbers** (`clinic-appointment-scheduling`, `library-book-checkout`) instead of giving the roadmap pressure for #26.
- **Severity/status/category values are faked as numbers or free strings** (`it-helpdesk-ticket`, `insurance-claim`, `utility-outage-report`) instead of forcing the choice-type discussion in #25.
- **Money remains overly simple**; the corpus does not push hard enough on decimal arithmetic, rounding, proration, or reconciliation (#27, #16, #17).
- **Many samples are still “clean path” workflows**. We need more exception loops, partial approvals, policy overrides, and deadline pressure.
- **No data-only portfolio lane exists**, so #22 currently lacks a strong sample-backed rollout path.

## Target distribution for a doubled portfolio

### By domain

| Domain lane | Target total | Why it belongs |
|---|---:|---|
| Finance and money movement | 7 | Best pressure for decimal, computed fields, conditional expressions |
| Service, claims, and case management | 8 | Best pressure for named rules, declaration guards, absorb, field constraints |
| Scheduling, logistics, and fulfillment | 7 | Best pressure for date/time, integer counts, queue/set workflows |
| People, identity, and workplace ops | 6 | Good for editability, choice values, input realism, data capture |
| Public sector, community, and infrastructure | 6 | Strong exception routing and public-policy style validation |
| Data-only / reference / configuration | 6 | Needed to justify #22 and to keep Precept from being workflow-only |
| Minimal teaching/control samples | 2 | Keep for tiny examples and smoke-value, not roadmap priority |
| **Total** | **42** |  |

### By difficulty

| Difficulty | Target total | Characteristics |
|---|---:|---|
| Starter | 10 | 3-5 states, 3-5 events, one obvious happy path |
| Standard | 20 | 5-7 states, exception rows, edits, collections or policy rules |
| Advanced | 12 | High rule density, repeated exceptions, money/date pressure, richer policy surface |

The portfolio should stay majority-standard. That is where language decisions become visible without turning every file into a monster.

## Proposed sample portfolio

### Rewrite-first existing anchors

These should move before most net-new additions because they already show up in issue discussions and design debates.

| Priority | Sample | Action | Why |
|---|---|---|---|
| 1 | `clinic-appointment-scheduling` | Rewrite | Strongest current argument for #26 date/time realism |
| 2 | `library-book-checkout` | Rewrite | Best overdue / due-date / renewal anchor for #26 and #29 |
| 3 | `travel-reimbursement` | Rewrite | Best anchor for #27 decimal, #16 functions, #17 computed fields |
| 4 | `it-helpdesk-ticket` | Rewrite | Best anchor for #25 choice values and better priority/severity modeling |
| 5 | `insurance-claim` | Rewrite | Best anchor for choice sets, document types, exception loops |
| 6 | `loan-application` | Rewrite | Best anchor for #8 named rules and #9 conditional expressions |
| 7 | `utility-outage-report` | Rewrite | Strong queue/set/public-ops sample; good for choice/date pressure |
| 8 | `subscription-cancellation-retention` | Rewrite | Best compact anchor for #9 and #10 |

### Keep but do not lead the roadmap

- `trafficlight`
- `crosswalk-signal`

Keep them as tiny teaching/smoke samples. Do **not** use them as evidence when prioritizing language work.

### Net-new additions to reach 42

| Domain lane | Sample idea | Difficulty | Primary roadmap pressure |
|---|---|---|---|
| Finance | invoice-discrepancy-resolution | Advanced | #27, #16, #17 |
| Finance | purchase-order-approval | Standard | #25, #9, #14 |
| Finance | loan-servicing-hardship-plan | Advanced | #8, #26, #27 |
| Finance | chargeback-dispute | Advanced | #9, #27, #31 |
| Service/case | prior-authorization-review | Advanced | #26, #14, #11 |
| Service/case | benefits-appeal-case | Advanced | #8, #14, #26 |
| Service/case | fraud-investigation-case | Advanced | #8, #25 |
| Service/case | vendor-risk-review | Standard | #25, #10, #15, #31 |
| Scheduling/logistics | shipment-exception-resolution | Standard | #25, #26 |
| Scheduling/logistics | warehouse-return-inspection | Standard | #29, #25 |
| Scheduling/logistics | field-service-dispatch | Standard | #26, #11 |
| Scheduling/logistics | quality-sample-inspection | Advanced | #29, #16 |
| People/workplace | access-certification-review | Standard | #14, #25 |
| People/workplace | leave-request | Starter | #26, #13 |
| People/workplace | contractor-onboarding | Standard | #11, #10, #15 |
| People/workplace | performance-improvement-plan | Standard | #8, #26 |
| Public/community | permit-review | Advanced | #8, #14, #26 |
| Public/community | code-enforcement-case | Advanced | #8, #26, #31 |
| Data-only | vendor-master-record | Starter | #22, #13, #10, #15 |
| Data-only | tariff-rate-card | Starter | #22, #27, #25 |
| Data-only | provider-directory-entry | Starter | #22, #13 |

That is **21 additions**, which gets the repo from **21** to **42** while keeping the expansion tied to roadmap pressure instead of novelty.

## Which ideas justify which pending enhancements best

| Issue | Best sample anchors | Why these matter |
|---|---|---|
| #8 Named rules | `loan-application` rewrite, `loan-servicing-hardship-plan`, `permit-review`, `benefits-appeal-case` | Repeated multi-clause eligibility logic becomes reviewable instead of duplicated |
| #9 Conditional expressions | `subscription-cancellation-retention` rewrite, `purchase-order-approval`, `chargeback-dispute` | These domains need value selection without splitting rows purely for assignment |
| #10 String `.length` | `vendor-master-record`, `contractor-onboarding`, `building-access-badge-request` rewrite | Real forms need max-length and non-empty handling everywhere |
| #11 `absorb` shorthand | `warranty-repair-request` rewrite, `prior-authorization-review`, `contractor-onboarding`, `field-service-dispatch` | Intake-heavy rows currently waste lines on transcription |
| #13 Field constraints | `vendor-master-record`, `leave-request`, `event-registration` rewrite, `it-helpdesk-ticket` rewrite | This is still the biggest boilerplate reducer across the whole corpus |
| #14 Declaration guards | `permit-review`, `access-certification-review`, `benefits-appeal-case` | Best proof that applicability rules should not explode state counts |
| #15 String `.contains()` | `vendor-master-record`, `contractor-onboarding`, `vendor-risk-review` | Email/domain and keyword presence checks are common and realistic |
| #16 Built-in functions | `travel-reimbursement` rewrite, `invoice-discrepancy-resolution`, `quality-sample-inspection` | These give real need for `abs`, `round`, and numeric helpers |
| #17 Computed fields | `travel-reimbursement` rewrite, `invoice-discrepancy-resolution`, `loan-servicing-hardship-plan` | Today these totals are manual and drift-prone |
| #22 Data-only precepts | `vendor-master-record`, `tariff-rate-card`, `provider-directory-entry` | Gives a real portfolio lane that is not forced into fake workflow states |
| #25 Choice type | `it-helpdesk-ticket` rewrite, `insurance-claim` rewrite, `vendor-risk-review`, `permit-review` | Strongest fix for fake numeric/string categories and typo-prone values |
| #26 Date type | `clinic-appointment-scheduling` rewrite, `library-book-checkout` rewrite, `leave-request`, `permit-review` | Current numeric day counters are the cleanest “this is wrong” evidence |
| #27 Decimal type | `travel-reimbursement` rewrite, `refund-request` rewrite, `invoice-discrepancy-resolution`, `tariff-rate-card` | The current corpus still under-demonstrates exact-money pressure |
| #29 Integer type | `warehouse-return-inspection`, `quality-sample-inspection`, `library-book-checkout` rewrite | These are true counts, sample sizes, and renewal counts — not fractional numbers |
| #31 `and` / `or` / `not` keywords | `loan-servicing-hardship-plan`, `permit-review`, `vendor-risk-review` | Readability gains show best in dense business-policy expressions |

## Prioritization: rewrite versus add

### Rule

**Rewrite first where the file is already a roadmap anchor. Add new files where the current corpus has no domain lane at all.**

### Practical order

1. **Rewrite the eight anchor samples listed above.**
   - Fastest quality gain
   - Reuses domains already familiar to the team
   - Gives immediate before/after evidence for issue discussions
2. **Add the 12 stateful net-new samples next.**
   - Fills domain gaps without waiting on #22
   - Gives finance, regulated ops, and policy-heavy cases enough weight
3. **Add the 3 data-only samples once #22 is either implemented or firmly scheduled.**
   - Do not fake these as state machines just to hit the count
4. **Only after the portfolio stabilizes, build the sample integration-test lane called out in `docs/@ToDo.md`.**
   - Test harness work should follow corpus stabilization, not precede it

## Phased plan after research wraps

### Phase 1 — Portfolio rules and rewrite briefs

- Lock the target count at **42**
- Lock the role of `trafficlight` and `crosswalk-signal` as teaching samples only
- Write one-page briefs for the eight rewrite-first anchors
- Define a realism rubric: domain recognizability, policy density, data realism, exception coverage, roadmap pressure

### Phase 2 — Rewrite anchor samples

- Rewrite the eight anchor files
- Make each rewrite explicitly pull on at least one pending language issue
- Use these rewrites as the core evidence set in future proposal conversations

### Phase 3 — Add missing stateful lanes

- Add the **12** stateful net-new samples
- Priority order: finance/case/public-policy first, then logistics/workplace
- Goal: remove the current over-reliance on “simple intake plus one approval step” examples

### Phase 4 — Add data-only lane

- Add the **3** data-only/reference samples once #22 is available
- If #22 slips, keep these as approved briefs and do not replace them with fake workflow samples

### Phase 5 — Stabilize and operationalize

- Freeze the expanded portfolio
- Add sample integration tests
- Tag each sample by domain, difficulty, and roadmap pressure so future issue research can cite the right evidence fast

## PM call

The corpus should become a **roadmap instrument**, not just a demo shelf. If a proposed feature cannot point to at least one realistic sample that becomes materially clearer or safer, it should lose priority.
