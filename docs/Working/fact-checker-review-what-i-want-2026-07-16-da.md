# Devil's-Advocate review — what-i-want-2026-07-16

**Status**: Advisory review — 2026-07-19  
**Reviewer**: Fact Checker (DA mode)  
**Scope**: Stress-test the wisdom of adopting the posture in `docs/Working/what-i-want-2026-07-16.md`, not its architecture or implementation details.

## Read set

Reviewed in full:
- `docs/Working/what-i-want-2026-07-16.md`
- `docs/Working/what-i-want-2026-07-16-review.md`
- `docs/Working/prove-govern-collation-index-2026-07-15.md`
- `docs/philosophy.md`

Cross-checks used where helpful:
- `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md`
- `research/architecture/compiler/fault-floor-definition-2026-06-11.md`
- Precept MCP references for current proof/fault/modifier/syntax surfaces

---

## 1. Steelman of the opposition

The strongest counter-case is **not** "this is technically impossible." It is: **even if feasible, this may be the wrong bet to center the product on right now.**

### 1.1 The posture may recreate the exact spiral the retrospective diagnosed

- **What:** `what-i-want` tries to end the prove-vs-govern argument by setting the ideal at "prove or reject, no deferral, ideal first, balance later."
- **Why this is risky:** the collation index says the prior six-week loop persisted because the boundary kept being defined on an **effort axis**, and effort has no natural stopping point. If the new posture still depends on "prove one more class" whenever a hard case appears, the vocabulary changes but the loop survives.
- **Counter-hypothesis:** the team does not escape the identity fight; it merely re-runs it under new labels such as "missing premise," "certificate obligation," or "surface shrink."
- **Suggested mitigation:** before adopting the posture as the product center, require a stopping rule: which classes of obligations are in scope for the first proving posture, which are explicitly out, and what evidence would justify widening the boundary.

### 1.2 It may optimize for philosophical sharpness over founder-stage leverage

- **What:** the document chooses the strongest possible guarantee as the reference point.
- **Why this is risky:** for a solo-founder-plus-agents product, the cost is not only proof machinery; it is also language redesign, diagnostics, certificate infrastructure, runtime/checker contracts, and documentation realignment. That is a large systems bet before the repo has closed the engineering wall the retrospective says it should diagnose first.
- **Counter-hypothesis:** a narrower, shippable guarantee could teach the team more, faster, about real user value than a long proof-program aimed at the perfect posture.
- **Suggested mitigation:** pair any adoption decision with a time-boxed proof-of-value plan: what user-visible capability lands in 30 days, and what evidence would tell you the stronger posture is earning its cost.

### 1.3 "Ideal first, balance later" may underweight the cost of author ergonomics

- **What:** the document says the guarantee is non-negotiable and the surface is negotiable.
- **Why this is risky:** the prior adversarial review's F8/F9 point is load-bearing: if authors cannot express the needed premises, or can express them only by awkward guard gymnastics, the result is not "better precepts" but either **unwritable precepts** or **spec suppression**. The collation corpus already names band-suppression / constraint-deletion as a likely failure mode.
- **Counter-hypothesis:** the product loses declared truth because authors delete rules they cannot make provable.
- **Suggested mitigation:** treat retained authoring power as a first-class acceptance criterion, not a follow-on nice-to-have.

### 1.4 The certificate story may move, not remove, trust risk

- **What:** the document's answer to a lighter runtime is "compiler-emitted certificate + small checker."
- **Why this is risky:** this can be excellent architecture, but as a product bet it creates a second load-bearing system: now both the prover and the certificate format/checker boundary must remain correct, small, explainable, versioned, and in sync with the runtime and docs. If the checker grows or the certificate surface becomes hard to understand, the simplification story weakens.
- **Suggested mitigation:** treat "small trusted kernel" as something to measure, not assume. Define size/complexity limits before using it as a strategic justification.

---

## 2. Load-bearing assumptions

These are the assumptions that, if false, materially weaken the plan.

### 2.1 Assumption A — every needed proof precondition on an external value is expressible in the current language

**Assessment:** **not safe to assume yet.**

- **Why I think this is load-bearing:** `what-i-want` relies on authors being able to add the missing premise when the compiler asks. If the needed premise cannot be written, "prove or reject" becomes "reject without repair path."
- **Evidence:**
  - The prior adversarial review's **F8** flags this directly.
  - `what-i-want` itself leaves `ExpiryDate > today` open, which is already one class of precondition with no obvious ingress carrier.
  - Current surfaced vocabulary is thin at ingress: MCP shows event-arg and field modifiers are mostly unary structural constraints (`min`, `max`, `positive`, `nonzero`, etc.). Relational facts exist as `rule`, `when`, and `ensure`, but there is no dedicated first-class "editable write must satisfy relation R to current entity state" construct; the document is depending on top-level rules plus ingress evaluation to carry that load.
- **Failure mode if false:** common domains need cross-field, temporal, or externally-derived premises that cannot be stated legibly at the input boundary. The result is either over-rejection or a quiet return to runtime sweep logic under another name.
- **Suggested mitigation:** build an obligation-to-surface matrix from real corpus examples before ratifying the posture. For each missing-premise diagnostic family, show the exact legal author surface that would repair it.

### 2.2 Assumption B — language simplicity is sufficient for decidable and tractable proof at real-precept scale

**Assessment:** **not safe to assume yet.**

- **Why I think this is load-bearing:** the document leans on "no loops, no functions, not Turing-complete" as evidence that strong proof should be practical.
- **Evidence:**
  - The prior adversarial review's **F9** already calls this out.
  - The current proof catalog is richer than a simple interval checker: qualifier compatibility, interval containment, count/length containment, index bounds, dimensional products, function-domain checks, etc.
  - The syntax surface still permits arithmetic, branching (`if/then/else`), relational rules, and collection operations. Simplicity helps, but it does not by itself settle decidability or tractability for the actual theories Precept wants.
- **Failure mode if false:** the team spends agent-hours widening theories, narrowing false positives, and benchmarking pathological cases instead of shipping core product value.
- **Suggested mitigation:** define the target proof fragment explicitly and benchmark it against a representative corpus now. The plan should not inherit "tractable enough" as a philosophical premise.

### 2.3 Assumption C — the boundary in `what-i-want` is hard-edged and legible, not another effort axis in disguise

**Assessment:** **currently unproven.**

- **Why I think this is load-bearing:** the collation index says the whole recurrence came from fuzzy boundaries. If this boundary is fuzzy per obligation, the plan does not solve the root problem.
- **Evidence:**
  - The collation index's strongest warning is that **partial static coverage is not partial guarantee; false security comes from a fuzzy boundary**.
  - `what-i-want` is sharper than prior drafts, but it still leaves open time-based rules, cross-precept scope, and a philosophy-level Fire semantic conflict.
  - The hardest classification question remains the same one the corpus has been tripping on: when a rule over runtime-supplied values is preserved by runtime premise checks, is that "governance proven sufficient" or "proof work deferred"? If different readers can still answer differently, the edge is not yet hard.
- **Failure mode if false:** the next hard case becomes another debate over whether it is a legitimate premise, illegitimate deferral, or an example that the language should shrink.
- **Suggested mitigation:** require a per-obligation classifier that can be applied mechanically and exposed in diagnostics/certificates. If a reviewer cannot classify an example without explanation prose, the boundary is still fuzzy.

### 2.4 Assumption D — shrinking the surface will preserve enough of the intended business domain to keep Precept valuable

**Assessment:** **important and currently underspecified.**

- **Why I think this is load-bearing:** the document explicitly allows expressibility tradeoffs but says the target business problem set is not yet pinned down.
- **Failure mode if false:** the posture stays pure by defining away too many real business cases, and the product learns that only after burning time on proof infrastructure.
- **Suggested mitigation:** name the "must-stay-writable" archetypes before further narrowing the language.

---

## 3. Pre-mortem — a plausible 30-day failure scenario

### Day 1-7: the team commits to the posture
The repo treats "prove or reject, no deferral" as the north star. Work starts on mapping every rule-preservation story to explicit premises and certificate entries.

### Day 8-14: the first unwritable cases appear
A cluster of realistic precepts hits the same wall:
- rules relating two independently editable fields,
- time-sensitive rules (`today`, expiry windows, age calculations),
- domains where the cleanest statement is a rule over entity state, not a modifier on a single ingress value.

The compiler can diagnose missing premises, but authors do not always have a clean surface to write them. Agents start proposing guard refactors, duplicated rules, or surface reductions.

### Day 15-21: the identity debate returns in a new costume
One camp says "this shows the surface must shrink more." Another says "this is governance, not proof failure." Another says "the certificate can record a justified runtime check." The old prove-vs-govern argument is back, except now the terms are "premise," "checker," and "load-bearing ingress validation."

### Day 22-30: product motion stalls
- Some rules get deleted or weakened so examples compile.
- Diagnostics get better, but authoring gets less natural.
- Docs drift harder: philosophy still says Fire evaluates all applicable constraints against the resulting configuration, while the new posture wants proof-plus-premise-checks instead.
- The team has spent most of the month refining the boundary, not demonstrating a new user-visible capability.

**Result:** the plan fails not because proving is impossible, but because it turns the current engineering wall into another month of product-identity adjudication.

**Suggested mitigation:** if proceeding, set an explicit 30-day review question narrower than "did we settle the philosophy?" Example: can the team classify and repair 20 representative failing obligations without inventing new surface area or deleting the original rule intent?

---

## 4. Alternative approach

### Option A — ratify a fault-floor + graph-integrity MVP, keep full business-rule prove-or-reject as a research track

This is the most credible alternative in the existing corpus.

- **What:** make the hard guarantee for now: compile-time proof/rejection of true evaluation faults and structural graph defects; runtime governance remains first-class for business-rule enforcement over runtime data, with explicit legibility about what is proven vs governed.
- **Why it is credible:** the fault-floor research already argues this boundary has a hard edge: unrecoverable computation faults vs recoverable governance refusals. The band analysis argues the legibility of the boundary matters more than maximizing static coverage. This gives a stopping rule.
- **What it buys:** a narrower trusted claim, less surface pressure, faster product motion, and a chance to learn whether users actually need the stronger theorem before betting the repo on it.
- **Its risk:** it may feel like conceding too much philosophically, and it requires careful messaging so "partial static" is not mistaken for "partial guarantee."
- **Mitigation if chosen:** make the legibility surface explicit in tooling and docs; do not allow clean-compile marketing to imply more than the engine actually guarantees.

### Option B — same posture, narrower MVP gate

If the owner wants to preserve the direction of `what-i-want`, a softer alternative is: do **not** adopt it as the repo-wide answer yet; adopt it as a **hypothesis** with a kill-test. That keeps the direction visible without making every hard case an identity battle.

---

## 5. Risk acceptance

If the team proceeds with `what-i-want` as stated, these are the risks to accept consciously rather than inherit silently.

1. **Authoring-surface shrink risk**  
   You may preserve theorem strength by making common domain statements awkward or impossible.  
   **Mitigation:** maintain a protected corpus of must-stay-natural examples.

2. **Re-litigation risk**  
   Without a mechanical classifier and stopping rule, the same argument can recur under new vocabulary.  
   **Mitigation:** require per-obligation classification criteria before more design expansion.

3. **Documentation/philosophy drift risk**  
   `what-i-want` already flags a philosophy mismatch on Fire semantics. If the posture proceeds, this becomes an owner-level philosophy decision, not a quiet implementation detail.  
   **Mitigation:** treat the philosophy gap as a gated decision, not cleanup.

4. **Checker/kernel growth risk**  
   "Small trusted checker" can erode into another complex subsystem.  
   **Mitigation:** define non-negotiable limits for the checker and certificate surface.

5. **Opportunity-cost risk**  
   A month spent proving the boundary is a month not spent validating demand with shippable behavior.  
   **Mitigation:** pair the posture with a short horizon product-learning milestone.

---

## Bottom line

`what-i-want` is a strong statement of desired destination, but the devil's-advocate case is that **treating it as the product's settled posture now may repeat the repo's exact failure mode: turning an engineering wall into another identity loop.** The strongest counter-argument is therefore not "don't aim high" but **"do not let the ideal outrun the evidence that the boundary is truly hard-edged, writable, and affordable."**

If the team proceeds, it should do so with explicit kill criteria, a protected corpus of must-stay-natural examples, and a mechanical per-obligation classifier. Otherwise the plan risks becoming one more effort-axis spiral in sharper language.
