# Data-First vs. State-First Positioning — PM Research
## Evaluating Shane's "States Are Vehicles" Proposal

**Date:** 2026-06-12
**Researcher:** Steinbrenner (PM)
**Scope:** Adoption/evaluation lens — developer discovery, positioning risk, and flagship claim recommendation
**Requested by:** Shane

---

## The Proposal

Shane's framing: *"States are just vehicles to drive data through a workflow."* Evaluate whether Precept should reframe its philosophy with **data as the primary concern** rather than state.

---

## 1. What Are Developers Actually Searching For?

The developer who will benefit most from Precept is rarely searching for "state machine library." They're searching for the symptom, not the cure.

**High-volume, developer-recognizable searches:**
- "validate domain data c#"
- "enforce business rules dotnet"
- "domain model validation"
- "prevent invalid entity state"
- "centralize validation rules"

**Low-volume, specialist searches:**
- "state machine dotnet"
- "finite state machine library c#"
- "workflow state management"

**Category-search volume proxy from NuGet download leaders:**
- FluentValidation: ~250M downloads — the dominant "data validation" tool
- Stateless: ~25M downloads — the dominant "state machine" tool
- Guard/GuardClauses (Ardalis): ~20M downloads combined

The ratio is roughly 10:1. **Ten times more developers are searching for data validation tools than state machine tools.** This is Shane's strongest argument.

However, there's a critical complication: the developer searching for "validate domain data" is not necessarily the developer who needs Precept. They might just need FluentValidation. The developer who needs Precept is the one who has **already tried FluentValidation** and found it insufficient — because their validation depends on state, and FluentValidation doesn't know about state.

**The wedge search query — what the frustrated developer types:**
- "validation depends on state c#"
- "different validation rules per status"
- "business rules change based on entity lifecycle"
- "state machine with validation constraints"
- "enforce invariants across state transitions"

This is the exact space Precept owns. These are lower volume but **higher purchase intent** — developers who've already ruled out the simpler tools.

### Conclusion on Searchability

Data-first language casts a wider net but catches too many developers who don't need Precept's power. State-first language is precise but self-limiting. The **real target query space** is the intersection: *data validation that depends on workflow state*.

---

## 2. Who Is the Target Developer?

Two plausible personas — only one is the actual wedge.

### Persona A: The Scattered Validation Developer
> "I have validation logic spread across controllers, services, and ORM event handlers. I need to centralize it."

**Their pain:** Duplicate rules, rules that contradict each other, rules buried in layers they can't easily find.  
**What they want:** One place to declare rules.  
**What they'll reach for first:** FluentValidation, Data Annotations, MediatR pipeline behaviors.  
**Why Precept might be overkill:** They don't have complex state transitions. Their entities don't move through lifecycles — they're just records with constraints.  
**Data-first framing relevance:** High. This persona resonates immediately.  
**Conversion probability:** Low. They'll be satisfied with FluentValidation + a clean architecture pattern.

### Persona B: The Lifecycle Integrity Developer
> "My entity goes through states — Draft, Submitted, Approved, Rejected. Different fields are required at different states. Some transitions are conditional. My validation logic is spread across state-checking `if` blocks, and I've duplicated business rules in multiple handlers."

**Their pain:** Can't trust that invalid state combos are impossible. State-checking code is scattered and error-prone. Transitions happen in multiple code paths and don't all enforce the same rules.  
**What they want:** A single source of truth for "what can happen, when, and under what data conditions."  
**What they'll reach for first:** Stateless + FluentValidation (used together, awkwardly).  
**Why Precept is exactly right:** It binds the state machine and the validation into one contract, enforced at the runtime boundary.  
**Data-first framing relevance:** Moderate. They identify with "lifecycle" language, but their *actual* frustration is data integrity failures caused by bad state transitions.  
**Conversion probability:** Very high. This is the developer Precept was built for.

### Persona C: The Domain Modeler
> "I want my aggregate roots to enforce their own invariants. My domain model should be the single source of truth, not a bunch of scattered validators that can be bypassed."

**Their pain:** Anemic domain models. Business rules bypassed by direct database writes. No structural enforcement of DDD invariants.  
**Data-first framing relevance:** High — they think in terms of data integrity and invariants.  
**State-first framing relevance:** Also high — they think about entity lifecycle.  
**Conversion probability:** High. This persona is already philosophically aligned.

### The Wedge Use Case

The wedge is **Persona B + Persona C in the same developer** — a team building a domain model with lifecycle states (Order, Invoice, Subscription, Application) that has been burned by:
1. A `CancelledOrder` that still had `Total > 0` because cancellation code forgot to zero it.
2. A validator that checked `RequiredField != null` but didn't check *which state* made it required.
3. A service method that transitioned state without running the right validation.

**This developer's thought process:** "I need something that makes it *structurally impossible* to get to a bad state." That mental model is state-first in the solution but data-integrity-first in the motivation.

---

## 3. Competitive Landscape and Differentiation Risk

### Pure Data Validation Tools

| Tool | What They Do | Precept Differentiation |
|------|-------------|------------------------|
| FluentValidation | Rule chains on POCOs, validator classes, IValidate<T> | Stateless — same rules apply regardless of entity lifecycle. No transition enforcement. Rules can be bypassed by skipping the validator. |
| DataAnnotations | Attribute-based validation on model properties | No runtime enforcement, no state awareness, no transition guards |
| Ardalis.GuardClauses | Inline precondition checks | Assertion style, not declarative. No state machine. No invariant enforcement across operations. |
| MediatR pipeline behaviors | Validation in request pipeline | Validates the *command*, not the *entity*. Entity can still end up in invalid state via direct writes. |

**The differentiator Precept has that none of these have:** Validation rules are **state-aware and transition-enforced**. Rules are not applied by calling a method — they are enforced by the runtime *before any state change is committed*.

### Pure State Machine Tools

| Tool | What They Do | Precept Differentiation |
|------|-------------|------------------------|
| Stateless | Fluent state machine builder in C# | No data constraints. No guard predicates on entity data. No structural enforcement of invariants. State transitions are behavioral, not contract-driven. |
| Automatonymous | State machine for MassTransit | Saga/process manager-oriented. Heavyweight infrastructure dependency. No data validation integration. |
| MassTransit Sagas | Distributed workflow orchestration | Infrastructure-level, not domain-model-level. Distributed systems overhead for local domain integrity. |
| xstate (JS) | Actor model, statecharts | JavaScript-only. No .NET story. No data constraint enforcement in the machine definition. |
| Workflow Core | Long-running workflow engine | Infrastructure-heavy, process orchestration level, not domain model level. |

**The differentiator Precept has that none of these have:** Data constraints and state transitions are **co-defined in one contract** — they cannot diverge. A Stateless state machine has no knowledge of whether `Price > 0` should gate the `Activate` transition.

### The Critical Gap in Both Categories

Every tool falls into one of two buckets:
- Validates data, ignores state
- Manages state, ignores data

**Precept's unique position is the space between them.** No incumbent owns "state-aware data integrity" as a category.

### Risk of Data-First Framing: The Validation Library Trap

This is real and significant.

If Precept's opening line leads with data governance or constraint enforcement, a developer scanning NuGet will pattern-match to:
> "Oh, another FluentValidation alternative."

And they will skip it. FluentValidation has 250M downloads and 10+ years of community. You cannot beat it on mindshare in its own category.

The same risk exists in reverse for state-first framing. "State machine for .NET" pattern-matches to Stateless, which is lightweight and well-loved. The developer thinks: "I already have Stateless, why do I need this?"

**The only safe position is the third one: owning the gap.** Precept is not a validation library. Precept is not a state machine library. It is what you need when a state machine and a validation library have to work together and you're tired of making them agree.

---

## 4. Framing Comparison: Adoption Potential

### Option A: Data-First
> "Your domain data is governed by rules that cannot be bypassed."

**Pros:** Broader recognition. Maps to developer pain language. Wider search funnel.  
**Cons:** Sounds like FluentValidation with marketing. No structural differentiation signal. Invites the "why not FluentValidation?" objection immediately.  
**Verdict:** High top-of-funnel awareness, low conversion. Draws the wrong persona.

### Option B: State-First
> "Invalid states are structurally impossible."

**Pros:** Precise, powerful, differentiating. No competitor says this. Resonates immediately with the right developer.  
**Cons:** Narrow. "Invalid states" is state-machine vocabulary — developers not thinking in those terms will skip it. Misses Persona A entirely.  
**Verdict:** Low top-of-funnel, high conversion. Draws the right persona but reaches fewer of them.

### Option C: Unified (Current)
> "A single declarative contract that binds state, data, and business rules into an engine where invalid states are structurally impossible."

**Pros:** Complete. Accurately describes the product. Contains all three categories.  
**Cons:** Long. Too dense for a first-pass scan. Requires parsing. "Domain integrity engine" is a category term that needs education.  
**Verdict:** Accurate but not punchy. Category creation requires repetition and education to land.

### Option D: Problem-Led Unified (Recommended — see Section 5)
> "State machines and validation always disagree. Precept makes them one contract."

**Pros:** Leads with a pain the target developer has *felt*. Immediately excludes developers who don't have both. Names the gap rather than naming the category. Short enough to scan.  
**Cons:** Requires the developer to have experienced the specific frustration. Won't resonate with Persona A who's never tried combining Stateless + FluentValidation.  
**Verdict:** Best conversion rate among qualified prospects. Possibly better as a secondary tagline than the primary.

### Option E: Workflow-Data Frame (Shane's Proposal in Best Form)
> "States are checkpoints. Data is the contract. Rules are enforced at every step."

**Pros:** Captures the "states as vehicles" insight in a way that's marketable. Data is front and center. Still conveys state-awareness.  
**Cons:** Abstract. What is a "checkpoint"? The word "contract" needs clarification. Still educates rather than hooks.  
**Verdict:** Interesting as supporting copy or a conceptual explanation, not the opening claim.

---

## 5. Recommendation: Flagship Positioning Claim

### My Recommendation: Asymmetric Two-Part Opening

Keep "domain integrity engine" as the **category anchor** (for category creation reasons already decided in `brand-decisions.md`), but restructure the problem statement to **lead with data pain, resolve with state mechanics**:

**Tier 1 — The hook (awareness, 5 seconds):**
> "Precept is a domain integrity engine for .NET."

This is the category anchor. It stays.

**Tier 2 — The problem resolution (evaluation, 15 seconds):**
> "Most validation libraries don't know about state. Most state machines don't know about data. Precept binds both into one executable contract where invalid combinations are structurally impossible."

This is where Shane's insight lives. The framing is: *data and state have a broken relationship in the current ecosystem, and Precept fixes it.* States are implicitly positioned as the mechanism that makes data-validation powerful — not the primary concern, but the differentiating mechanism.

**Tier 3 — The proof claim (evaluation, 30 seconds):**
> "Same definition. Same data. Same outcome. Every time."

Determinism as a trust signal. This is the follow-on from the opening.

### Why This Works for the Evaluation Funnel

1. **Persona A (Scattered Validation):** Reads "validation libraries don't know about state" → clicks through, learns, possibly converts.
2. **Persona B (Lifecycle Integrity):** Reads "state machines don't know about data" → immediately recognizes their pain → high intent.
3. **Persona C (Domain Modeler):** Reads "one executable contract" + "structurally impossible" → already sold.

Shane's "states as vehicles" insight is correct at the *explanation layer* — it's how you teach what Precept does once someone is already reading. It is not the opening claim, because it requires explaining what the vehicle is doing and where the data is going.

---

## 6. What Would Change in Current Brand Decisions

If the "data-first / states-as-vehicles" reframing were adopted in the current brand decisions (`brand-decisions.md`):

### Would NOT change:
- **Category anchor:** "domain integrity engine" stays. The category creation play is correct and should not be abandoned.
- **Narrative archetype:** Category Creator stays. The insight is that data validation and state machines belong in one contract — that is still a new category.
- **Voice:** Authoritative with warmth. Nothing changes here.
- **Visual language:** The DSL is the hero image. Data *and* states are equally visible in the DSL syntax — no visual change needed.
- **Brand color:** Semantic color system stays. Notably, the color system already treats States (violet) and Data (slate) as co-equal visual families — the design already encodes the "both matter equally" principle.

### Would change:
- **Combined single-sentence positioning:** Would need a revision to lead with the data-state integration pain point rather than the "binds state, data, and rules" enumeration. The current phrasing lists components; the new phrasing should describe the *failure mode* it prevents.

  **Current:**
  > "Precept is a domain integrity engine for .NET — a single declarative contract that binds state, data, and business rules into an engine where invalid states are structurally impossible."

  **Revised:**
  > "Precept is a domain integrity engine for .NET — a single declarative contract where state and data rules coexist, making invalid combinations structurally impossible."

- **README problem statement:** Would gain an explicit "why both state and data" paragraph in the educational section. The "what is domain integrity?" section (currently missing — flagged in my prior research) would use Shane's insight as its organizing idea: validation that ignores state is incomplete, and state machines that ignore data are toothless.

- **Philosophy.md:** The philosophy document would gain a statement articulating that states are the *enforcement mechanism* for data integrity, not the primary concept. This is the correct philosophical grounding for the "states as vehicles" idea — it explains *why* the runtime needs states without making states feel like the end goal.

### Would NOT change but should be reinforced:
- **"Prevention, not detection"** — this is the data-integrity-first claim in disguise. Invalid data combinations are prevented, not detected after the fact. This bullet should be surfaced higher in README positioning, as it speaks directly to the data-pain audience.
- **"One file, all rules"** — this is the consolidation promise that Persona A responds to. It should remain prominent.

---

## 7. Summary

| Question | Answer |
|----------|--------|
| Better adoption framing: data-first or state-first? | Neither alone. Problem-led unified: "validation without state is incomplete; state without validation is unsafe." |
| Target developer primary frustration | Data ends up in invalid combinations because state transitions don't enforce it — or because validation doesn't account for state. |
| Risk of data-first framing | Real and significant. Sounds like FluentValidation unless clearly differentiated. Must keep state-awareness visible as the mechanism. |
| Risk of state-first framing | Narrower audience; misses developers who don't think in state machine terms. |
| Flagship positioning claim | Keep "domain integrity engine" as category anchor. Add problem-resolution copy that names the broken relationship between validation and state machines. |
| Shane's "states as vehicles" insight | Correct as explanation copy, not as opening claim. Use it in the "what is domain integrity?" educational section, not in the one-sentence hook. |
| Changes to brand-decisions.md | Minimal. Single-sentence positioning revision + README problem statement + philosophy.md elaboration. |

The data-first framing has higher awareness potential but lower conversion quality. The insight that *states make data validation meaningful* is the right framing — but the *opening claim* should name the pain (broken relationship between state and data rules), not the mechanism (states as vehicles). Let the education section carry the philosophical weight.

---

*Steinbrenner · PM · Precept*
