# Line Economy — Statement Count Research
## By J. Peterman

## Method

Each of the following is counted as one meaningful DSL statement, regardless of physical line placement:

- `precept X` declaration
- `field X as T ...` declaration (one per field)
- `invariant ... because "..."` declaration (one per invariant)
- `state X, Y, Z ...` declaration line (the whole line counts as one, even with multiple states)
- `event X with ...` declaration line (the whole line counts as one, even with multiple events)
- `from X on Y [when Z]` rule header (one per handler, with or without guard)
- `set X = expr` action
- `transition X` or `no transition` action
- `reject "..."` action

**Excluded from count:**
- `on X assert ...` — treated as zero; it is part of the containing event declaration
- Blank lines, `{` / `}`, and indentation

Multi-statement collapsed lines (`from A on B when C -> set X = Y -> transition Z`) are unpacked: each arrow-separated element counts separately as a rule header + actions.

---

## Sample Analysis

| Candidate | Rank | Score | Statement Count | Notes |
|-----------|------|-------|----------------|-------|
| Subscription Billing | 1 | 29 | 18 | Most compact of the top tier; the crammed `from Active on Activate when` line hides 3 statements in 1 non-blank line |
| SaaS Trial | 2 | 29 | 23 | Six `from…on` handlers carry the trial-countdown narrative; each one earns its place |
| Coffee Order | 3 | 29 | 22 | The 5-action `from Ordered on Order` rule (set × 3 + no transition) is dense but justified |
| Deploy Pipeline | 4 | 27 | 23 | Four fields and a boolean flag add bulk; PassHealthCheck rule contributes two sets |
| Food Delivery | 5 | 27 | 20 | Leanest of the 27-point tier; clean driver-assignment gate, nothing wasted |
| Feature Flag | 6 | 27 | 23 | Two invariants + three event lines + Activate's bonus `set RolloutPercent = 100` add up |
| Freelance Contract | 7 | 26 | 24 | Two reject paths on Complete are the right call but push the count |
| Email Campaign | 8 | 26 | 23 | Solid batch-completion story; double reject (Launch + Finalize) is structurally necessary |
| Gym Membership | 9 | 26 | 25 | Six handlers; the `from Active on Join` update rule is the only line that feels elective |
| Job Queue Task | 10 | 25 | 22 | MaxRetries field and the Fail-with-retry rule contribute well-earned complexity |

**Range across top 10:** 18 – 25  
**Median:** 22  
**Top-ranked candidate:** 18 (Subscription Billing)  
**Lowest-count candidates at Eco: 5 (original 12–16 line gate):** Subscription Billing (18) and Food Delivery (20)

---

## Recommendation

**Proposed gate: 12–20 statements**

**Rationale:**

The old line count was a convenient fiction. A developer looking at Subscription Billing saw fifteen non-blank lines and felt satisfied. They did not see that one of those lines silently contained three statements. Precept does not care where you put your carriage returns, and neither should a rubric that claims to measure economy.

When you unpack every collapsed arrow, the top-ranked candidate lands at eighteen statements. The fifth-ranked candidate — Food Delivery, the leanest of the 27-point tier — lands at exactly twenty. Those two candidates are the pole stars. They represent what a hero sample looks like when the writer exercised genuine discipline. The gate should live where those candidates live: ceiling at twenty.

The floor at twelve ensures a sample can express all nine required constructs without padding. Below twelve, something important has been left out.

Setting the ceiling at twenty is also a meaningful tightening of the *effective* current gate. Candidates with Eco: 5 under the old rubric ranged from 18 to 25 statements once crammed lines were unpacked. The old gate was letting in samples with 25 statements by rewarding compression over clarity. Twenty cuts that ceiling by five. That is not a trim — it is a standard.

One more thing. Shane said twelve to sixteen lines felt too long. He was right — but not because the numbers were wrong. He was right because the unit was wrong. Sixteen non-blank lines, when lines can contain five statements, is a gate that measures nothing. Twelve to twenty statements, properly counted, is a gate with teeth.

---

## Edge Cases

| Candidate | Count | Verdict | Notes |
|-----------|-------|---------|-------|
| Coffee Order (#3, 29 pts) | 22 | 2 over | The `from Ordered on Order` rule packs 3 sets + no transition = 4 statements in one handler. Trimming one field (ShotCount or SizeOunces) would bring it in; editorial call |
| SaaS Trial (#2, 29 pts) | 23 | 3 over | Six event handlers are the cost of the trial-countdown narrative. Collapsing the Churn path (drop `from Expired on Churn -> transition Churned`) saves 2; still needs 1 more cut |
| Deploy Pipeline (#4, 27 pts) | 23 | 3 over | Four fields is one too many for hero duty; dropping `Attempts` and its tracking rule gets to 19 |
| Feature Flag (#6, 27 pts) | 23 | 3 over | Three event declaration lines and the `set RolloutPercent = 100` bonus action are the excess; removing the inline Activate set gets to 21 |
| Email Campaign (#8, 26 pts) | 23 | 3 over | The double-reject (Launch + Finalize) is narratively strong but structurally generous; removing one brings it to 21 |
| Freelance Contract (#7, 26 pts) | 24 | 4 over | The double-reject-on-Complete is the right call for the domain, but it pushes this past hero range |
| Gym Membership (#9, 26 pts) | 25 | 5 over | The `from Active on Join -> set MonthlyFee -> no transition` update rule is the one elective line; cut it and you're at 22, still 2 over |

**Acceptable disqualifications:** Yes. A gate that admits everything is not a gate. Coffee Order and SaaS Trial are close enough that editorial trimming — removing one field, consolidating one event path — would clear them. Deploy Pipeline and Feature Flag need similar but minor surgery. Gym Membership and Freelance Contract are genuinely too dense for a hero sample and belong in the extended sample library.

The gate is not a punishment. It is a target. "Write your hero sample in twenty meaningful statements" is a creative constraint that produces better work.
