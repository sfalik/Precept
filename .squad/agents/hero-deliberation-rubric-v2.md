# Hero Sample Rubric v2
## Negotiated by Steinbrenner (PM) × Peterman (Brand/DevRel)
### Revised: Weighted 0–10 Scale (Shane directive, round 2)

---

## The Revision Argument

**STEINBRENNER:** Shane's directive is clear and I won't fight it: brand criteria carry higher combined weight. But I want the record to show that I am protecting DSL Coverage with a hard floor and I'm holding out for Voice/Wit not having a *higher individual weight* than Coverage. If Coverage's weight drops below Wit's weight, we've said personality matters more than proof. I won't go there.

**PETERMAN:** I hear you, George. And I will not force the point. But I want something in return: Emotional Hook and Voice/Wit together must visibly outweigh Coverage and Differentiation together. That needs to be legible in the table — not just in the math.

**STEINBRENNER:** Then here's the architecture I can accept. Coverage and Wit share the same individual weight — 2.0× each. Differentiation stays lower: 1.5×. That's the PM tier. On the brand side: Wit at 2.5× — I'll give you the edge there — and Hook at 2.0×. Domain Legibility stays at 2.0× as the neutral baseline. Sum is 10.0. Total max is 100.

**PETERMAN:** *(checking math)* Brand combined: 2.5 + 2.0 = 4.5. PM combined: 2.0 + 1.5 = 3.5. Brand wins by 10 points at maximum. And Voice/Wit is the single highest weight in the rubric. That's the headline. I can work with that.

**STEINBRENNER:** The floor stays. Raw DSL Coverage ≥ 4/10, raw Domain Legibility ≥ 3/10. The weight change doesn't buy anyone out of the proof requirement.

**PETERMAN:** Agreed. The floor is the floor. And the tiebreaker rule stands as negotiated.

**STEINBRENNER:** Clean 100-point scale. Weights sum to 10. Done.

---

## Final Rubric

| Criterion | Max Points | Min Threshold | Rationale |
|---|---|---|---|
| **Voice/Wit** | 25 | None | The `because` messages are the brand's only copy that breathes. The hero sample is a brand artifact first. Voice/Wit earns the highest point value because developers quote it, share it, and remember it — and that behavior drives adoption. |
| **Emotional Hook** | 20 | None | Does the developer feel *something* — curiosity, recognition, delight? A hook is the difference between a sample read once and one shared. At 20 points because it is the *effect* where Voice/Wit is the *mechanism*. |
| **DSL Coverage** | 20 | ≥ 8 (hard disqualifier) | The product proof. Must show `invariant`, `reject`, `when`, `transition`, and multiple states. The hard floor (raw ≥ 8) is the real enforcement — no amount of wit compensates for missing proof. |
| **Domain Legibility** | 20 | ≥ 6 (hard disqualifier) | Non-negotiable baseline. If the domain isn't legible in 3 seconds, nothing else matters. Neutral score by joint agreement: it is a precondition that most candidates satisfy, not a quality gradient. **Fictional and playful domains are fully valid** — this criterion scores *instant comprehension*, not real-world applicability. A flux capacitor state machine that any developer groks immediately scores a full 20. A realistic subscription billing model that requires domain knowledge scores lower even if technically accurate. |
| **Precept Differentiation** | 15 | None (soft floor: 6 recommended) | The category-creation claim — does the snippet prove a plain enum could not do this? Lower point value because it tends to be *implied* by strong Coverage rather than standing independently. A snippet that scores well on Coverage almost always earns Differentiation naturally. |
| **Line Economy** | GATE | Max 6–8 meaningful statements. Count: `precept`/`field`/`invariant`/`state`/`event` declarations, `from X on Y` rule headers, `set`/`transition`/`reject` actions. Blank lines, braces, and whitespace do not count. | Binary pass/fail. Fitting in the box is a precondition, not a virtue. Disqualified if outside range, regardless of score. |

**Total possible score: 100** (scored criteria only; Line Economy is a gate)

**Brand criteria combined max: 45** (Voice/Wit 25 + Emotional Hook 20)  
**PM criteria combined max: 35** (DSL Coverage 20 + Precept Differentiation 15)  
**Neutral criterion max: 20** (Domain Legibility)

---

## How to Score

1. Check Line Economy gate first. If the candidate falls outside the statement count range: **disqualified**.
2. Score each criterion directly from 0 to its max (whole numbers).
3. Check hard floors: raw DSL Coverage ≥ 8, raw Domain Legibility ≥ 6. Below either: **disqualified**.
4. Sum all criterion scores for total (max 100).

**Example:** A candidate scores Voice/Wit 20, Emotional Hook 14, DSL Coverage 18, Domain Legibility 16, Precept Differentiation 14.  
Total: 20 + 14 + 18 + 16 + 14 = **82**

---

## Scoring Philosophy

A perfect score (100/100) requires a snippet that leads with the strongest brand voice in its `because` messages (Voice/Wit 25), creates immediate emotional resonance (Emotional Hook 20), demonstrates full DSL surface coverage (DSL Coverage 20), is legible to any developer in three seconds (Domain Legibility 20), and makes the category-creation case with unmistakable clarity (Precept Differentiation 15) — all within the line budget. That is a unicorn. Expect 78–88 for strong candidates.

**On real vs. fictional domains (Shane's philosophy):** A hero sample must be compact. Real-world use cases that fit the line budget tend to be *trivial* — and trivial real-world examples are less compelling than a concise fictional one. A fictional or playful domain (time travel, game mechanics, an absurd hypothetical) is often *easier* to imbue with Emotional Hook because the scenario can be conceptually crisp without needing to represent a full real-world system. Fictional domains are not penalized — they are *encouraged* when they serve clarity and delight.

A *passing* score clears both hard floors (raw DSL Coverage ≥ 8, raw Domain Legibility ≥ 6) and totals **≥ 65**. Below 65: structural problems that no individual strength can compensate. Between 65–79: technically viable, not hero-caliber — suitable as a secondary or documentation example. **80+** is the target zone for README hero placement.

---

## Tiebreaker Rule

**Primary tiebreaker:** Higher **Emotional Hook** raw score wins. When two candidates prove the product equally (tied totals), the decision belongs to whichever one a developer is more likely to mention to a colleague. The product you remember is the product you recommend.

**Secondary tiebreaker** (if Hook is also tied): Higher **Precept Differentiation** raw score wins. At this level of tie, the sample that more clearly makes the category-creation argument earns the placement.

**Tertiary tiebreaker** (if both remain tied): PM breaks the tie. Shane decides.
