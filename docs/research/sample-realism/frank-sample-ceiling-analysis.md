# Sample Corpus Ceiling Analysis

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-05-17
**Status:** Research artifact — architectural recommendation for long-term corpus sizing

---

## Purpose

Shane asked: "Can we do some research to propose a more realistic upper end? I just guessed when I asked to double."

Steinbrenner's portfolio plan targets **42 samples** (doubling from 21). That number was a reasonable planning anchor, but it was never argued from first principles. This document provides the architectural reasoning for a long-term upper-end range — not a single number, but a defended band with structural justification for where diminishing returns set in, and a tiered model for managing quality across that band.

---

## 1. What Makes a Sample Corpus Too Small

A corpus is undersized when any of these symptoms appear:

| Symptom | How to detect | Current corpus? |
|---------|--------------|----------------|
| **Construct gaps.** A shipped language construct has zero or one sample exercising it. | Audit feature × sample matrix. | Yes — `from State assert` appears in only 3 of 21 files (14%). |
| **Domain monoculture.** Most samples share the same workflow shape (intake → review → approve/deny), so new domains look forced into that mold. | Cluster by state-graph topology. | Yes — George's audit calls this the loudest structural weakness. |
| **No difficulty gradient.** Everything is either trivially simple or unrealistically complex, with nothing in between. | Classify by state/event/guard counts. | Partially — we have 3 teaching, 10 standard, 8 complex. The teaching tier is thin. |
| **Roadmap silence.** Pending language proposals cannot point to a sample that would materially improve if the feature shipped. | Map proposals to FUTURE comments. | Yes — most proposals lack concrete sample evidence. |
| **Single-shape saturation.** A reader picks up 3-4 samples and concludes they've seen everything the language can do. | Structural similarity analysis. | Borderline — the collection-heavy samples (queue, stack, set) add shape variety, but the approval-gate pattern dominates. |

The current corpus of 21 hits **at least three** of these. We are undersized. That much is clear.

---

## 2. What Makes a Sample Corpus Right-Sized

A corpus is right-sized when every addition above its current count faces a meaningful test: *does this sample teach something that no existing sample teaches, along at least one of these axes?*

### The three axes of marginal value

1. **Domain axis.** Does this sample introduce a business domain family not yet represented? First sample in a new domain family: high value. Second sample in the same family: moderate value (shows within-family variation). Third: low value unless the workflow shape is genuinely distinct.

2. **Shape axis.** Does this sample introduce a workflow topology not yet represented? Key shapes: linear intake, branching approval, evidence-collection loop, appeal/reconsideration, partial fulfillment, SLA/deadline escalation, suspend/resume, data-only, queue promotion, quorum/multi-party. Each distinct shape warrants at least 2 samples (one teaching, one realistic).

3. **Construct axis.** Does this sample exercise a language construct or construct *combination* that no existing sample demonstrates? This axis saturates fastest — Precept's intentionally small surface means most construct combinations appear within 15-20 well-designed samples.

A right-sized corpus has enough samples that all three axes are adequately covered, with 2-3 redundant demonstrators per construct for teaching resilience, but not so many that any axis has 5+ samples adding no new information.

---

## 3. What Makes a Sample Corpus Too Large

A corpus becomes oversized when:

| Symptom | Why it hurts |
|---------|-------------|
| **Maintenance drag.** Every language change requires touching N samples. If N > 50, the update pass itself becomes a multi-day project that discourages language improvement. | The FUTURE(#N) grep-and-update protocol scales linearly. Past ~55 files, a single-feature rollout (like `choice` replacing string categories across the corpus) becomes a project, not a task. |
| **Discoverability collapse.** New users, AI agents, and doc authors cannot identify the "right" sample for their domain without a catalog, and the catalog itself needs maintenance. | At 21 samples, scanning the file list works. At 40, it's tight. Past 60, you need a curated index — which is another artifact to maintain. |
| **Structural redundancy.** Multiple samples in the same domain family with the same workflow shape exist. They differ only in field names. | Redundant samples dilute the corpus's teaching density. They make it harder to identify which sample is the authority for a given pattern. |
| **Quality variance.** With more samples, some inevitably lag behind on realism, aspirational comments, or construct coverage. The weakest samples become the visible ones (Murphy's Law of documentation). | A corpus of 40 high-quality samples is strictly better than a corpus of 60 with 20 mediocre ones. |
| **AI training noise.** For AI authoring (Principle 12), more samples means a larger context window burden. If 30% of the corpus is structurally redundant, the AI trains on noise. | Training efficiency drops when the signal-to-noise ratio of the corpus degrades. |

### The maintenance cost model

Each sample currently averages ~58 lines. A language feature that touches all samples (like `and`/`or`/`not` replacing `&&`/`||`/`!`) requires:

- **21 samples:** ~1,200 lines to review. One person, one session.
- **42 samples:** ~2,400 lines. One person, one long session or two sessions.
- **55 samples:** ~3,200 lines. Pushes toward needing a second reviewer or an automated migration pass.
- **70+ samples:** ~4,000+ lines. Requires tooling or dedicated multi-person effort.

The inflection point where manual update passes become unreliable is around **55 files**. Past that, you need automated migration tooling — which is a real cost that should be justified by real value.

---

## 4. How Breadth, Shape, and Pedagogy Interact

These three forces pull in different directions. The corpus ceiling is the point where they stop being able to all improve simultaneously.

### Breadth (domain families)

Peterman's domain benchmark identifies **9 strong-fit domain families** for Precept, and my earlier research adds e-commerce, manufacturing, legal, and HR as plausible extensions. Call it **12-14 addressable domain families** for a governed-lifecycle DSL.

At 2-3 samples per family (one core, one or two extensions), breadth saturates at **24-42 samples**. Past that, new domain families start feeling forced — Precept is not a general-purpose workflow engine, and modeling, say, a continuous manufacturing process or a real-time bidding system would distort the product narrative rather than strengthening it.

### Shape (workflow topology)

I count **10 distinct workflow topologies** that Precept can meaningfully express:

| # | Shape | Example | Minimum samples needed |
|---|-------|---------|----------------------|
| 1 | Linear intake → terminal | Event registration | 1-2 (teaching tier) |
| 2 | Branching approval/denial | Loan application | 2-3 |
| 3 | Evidence-collection loop | Insurance claim | 2-3 |
| 4 | Appeal/reconsideration | Benefits appeal | 1-2 |
| 5 | Partial fulfillment | Order fulfillment | 1-2 |
| 6 | SLA/deadline escalation | Incident response | 1-2 |
| 7 | Suspend/resume/hold | Prior authorization | 1-2 |
| 8 | Queue promotion | Helpdesk ticket | 1-2 |
| 9 | Data-only (no states) | Vendor master record | 2-3 |
| 10 | Cyclic operational | Traffic light | 1 (teaching only) |

Total shape coverage requires **14-22 samples**, with overlap (many samples combine shapes).

### Pedagogy (difficulty gradient)

A healthy gradient for a corpus of size N:

| Tier | Share | At N=42 | At N=50 | At N=55 |
|------|-------|---------|---------|---------|
| Teaching (entry ramps) | 10-15% | 4-6 | 5-7 | 5-8 |
| Standard (working developers) | 50-55% | 21-23 | 25-28 | 28-30 |
| Complex (architects / domain experts) | 30-35% | 13-15 | 15-18 | 17-19 |

The teaching tier has a hard ceiling: past ~8 teaching samples, you're repeating the same introductory patterns. The complex tier has a soft ceiling around 18-20: past that, readers cannot tell the complex samples apart without deep reading.

### Where they converge

All three forces suggest the same band:

- **Breadth** saturates at 24-42.
- **Shape** saturates at 14-22 (but samples combine shapes, so this is a lower bound).
- **Pedagogy** stays healthy up to ~55 total, starts degrading past ~60.
- **Maintenance** stays manageable through ~55, gets expensive past that.

The architectural ceiling is **50-55 samples**. The operating sweet spot is **40-50**.

---

## 5. Signs of Diminishing Returns in Sample Growth

These are the early-warning signals that the corpus is growing past the point of value:

### Signal 1: New samples fail the "what does this teach?" test

If a proposed sample cannot articulate — in one sentence — what it demonstrates that no existing sample demonstrates, it's redundant. When this starts happening regularly, the corpus is near or past its ceiling.

### Signal 2: The update-pass cost exceeds the feature-development cost

If implementing a language feature takes 2 days but updating all affected samples takes 3 days, the tail is wagging the dog. The sample corpus is supposed to *serve* the language, not *govern* it.

### Signal 3: Readers skip samples

Usage analytics (if available) or anecdotal evidence that users/agents only ever read 5-8 "known good" samples and ignore the rest. This means the corpus has a discovered core and an undiscovered periphery — which means the periphery is dead weight.

### Signal 4: Quality variance rises

When sample reviews start finding files that are 2+ language versions behind on aspirational comments, or that use constructs incorrectly, or that have stale header comments. Maintenance debt accumulates faster in large corpora.

### Signal 5: Structural duplication

When new samples look structurally identical to existing ones after you strip the domain-specific field names. If `purchase-order-approval` and `expense-approval` and `travel-reimbursement` all have the same state graph (Submit → Review → Approve/Deny → Close), the third one is redundant as a sample — even though it's a valid business domain.

---

## 6. Recommended Upper-End Range

### The number

| Band | Range | Characterization |
|------|-------|-----------------|
| **Minimum viable** | 35-40 | Adequate domain breadth, good shape coverage, thin teaching tier. Maintenance is easy. |
| **Optimal operating band** | 40-50 | Full domain breadth, excellent shape coverage, healthy difficulty gradient, manageable maintenance. This is where value per sample is highest. |
| **Architectural ceiling** | 50-55 | Maximum defensible corpus size. Every addition past 50 must justify itself against all three axes. Past 55, maintenance costs reliably exceed marginal value. |
| **Overextended** | 55+ | Maintenance drag, discoverability collapse, and quality variance become chronic. Not recommended without dedicated tooling and a formal curation role. |

### The recommendation

**Target the 40-50 band as the long-term operating range. Treat 50 as a soft ceiling and 55 as a hard ceiling.**

Steinbrenner's 42-sample target is a sound near-term anchor — it falls at the low end of the optimal band. The realistic long-term ceiling is about 50, with architectural justification for up to 55 if the language surface grows significantly (e.g., post-Wave-3 with `choice`, `date`, `decimal`, and `integer` all shipped, creating genuine new sample opportunities).

The important discipline is not "stop at N" — it's "every sample past 42 must pass the marginal-value test on at least two of the three axes (domain, shape, construct)."

---

## 7. The Tiered Model

Not all samples carry equal weight. A tiered model distributes maintenance effort and reader attention appropriately.

### Tier 1: Core Canon (15-20 samples)

**What it is:** The primary teaching and reference corpus. Every reader, every AI agent, every documentation reference should know these files. These are the flagship samples.

**Properties:**
- Updated with every language change in the same PR.
- Every shipped construct appears in at least 2 core canon samples.
- Aspirational comments kept current with the latest proposal states.
- Header comments include complexity classification and domain label.
- These samples appear in README references, documentation examples, and AI training prompts.

**Maintenance commitment:** Highest. These are product-grade artifacts. A core canon sample with a known defect is a shipped bug.

**Selection criteria for core canon:**
- Demonstrates a distinct, high-value domain (finance, healthcare, compliance, identity, operations).
- Exercises multiple construct categories (guards + collections + state actions + asserts).
- Shows a non-trivial workflow topology (branching, looping, or exception paths).
- Passes the full realism test from my earlier research (§5.1 of frank-language-and-philosophy.md).

**Current best core canon candidates:**
- `insurance-claim` — evidence loop, compound guards, approval/denial split
- `loan-application` — complex eligibility rules, underwriting logic
- `travel-reimbursement` — financial calculations, multi-step approval
- `vehicle-service-appointment` — recommendation set, partial approval
- `utility-outage-report` — queue + set, dispatch operations
- `maintenance-work-order` — in-progress work, parts approval
- `hiring-pipeline` — multi-reviewer, feedback collection
- `building-access-badge-request` — numeric set operations, approval gates
- Plus 4-6 net-new samples from the expansion pass (prior-auth, chargeback, KYC, permit)
- Plus 2-3 data-only samples when #22 ships

### Tier 2: Extended Canon (20-25 samples)

**What it is:** Broader domain coverage, advanced patterns, and samples that primarily serve roadmap pressure or niche construct demonstration.

**Properties:**
- Updated when language changes directly affect them (FUTURE grep pass).
- Not necessarily updated for every cosmetic language improvement.
- May serve a single roadmap purpose (e.g., "this sample exists to pressure #27 decimal").
- Lower documentation reference weight — mentioned in catalogs, not in introductory docs.

**Maintenance commitment:** Moderate. These compile and stay current with breaking changes. Aspirational comments may lag one language wave.

**What belongs here:**
- Domain-specific samples that overlap structurally with core canon samples but serve a different business domain (e.g., `leave-request` overlaps with `refund-request` structurally, but covers the HR domain).
- Samples that primarily exist to pressure a specific future feature.
- Deep-complexity samples that demonstrate advanced patterns most readers won't need.

### Tier 3: Experimental / Research (5-10 samples, rotating)

**What it is:** Samples that serve a specific research or proposal purpose, that push language limits intentionally, or that explore domains where Precept's fit is uncertain.

**Properties:**
- Compile today, but may be heavily annotated with FUTURE comments.
- Explicitly labeled as experimental in their header comment.
- Not referenced in product documentation.
- May be promoted to extended canon when the features they depend on ship.
- May be retired when the research question they address is resolved.

**Maintenance commitment:** Low. These are not product-grade. They are research instruments.

**What belongs here:**
- Samples created during proposal evaluation to test whether a feature improves the language.
- Domains at the edge of Precept's fit (e.g., a continuous-process sample, an IoT monitoring sample).
- Samples deliberately written to stress-test a specific language constraint.

### Tier distribution at steady state

| Tier | Range | At 42 | At 50 |
|------|-------|-------|-------|
| Core Canon | 15-20 | 16-18 | 18-20 |
| Extended Canon | 20-25 | 20-22 | 22-25 |
| Experimental | 5-10 | 4-6 | 5-8 |

The experimental tier is explicitly allowed to fluctuate. It's a working bench, not a permanent collection.

---

## 8. Comparable Systems — What Others Do

Worth noting for calibration, not prescription:

| System | Type | Approximate sample/example count | Notes |
|--------|------|--------------------------------|-------|
| Temporal (workflow engine) | General-purpose orchestration | ~25-30 featured examples | Biased toward code SDK samples, not DSL definitions |
| AWS Step Functions | Cloud workflow service | ~15-20 workflow patterns | Templates, not standalone DSL files |
| Cedar (Amazon policy DSL) | Authorization policy language | ~15 policy examples in spec | Closest structural analog — small DSL, focused domain |
| Drools (rule engine) | Business rules | ~30-40 across docs | More dispersed, many are code-embedded |
| OPA/Rego (policy DSL) | Authorization/policy | ~20-30 examples | Similar to Cedar — small surface, saturates fast |
| Azure Logic Apps | Low-code workflow | 50+ templates | Templates != samples; much is generated scaffold |

**Key observation:** Focused DSLs (Cedar, Rego) saturate at 15-30 examples. General-purpose workflow platforms carry more but rely on templates and code, not standalone definitions. Precept sits between these — a focused DSL, but one that models richer entities than Cedar. The 40-50 range is defensible precisely because Precept's entity contracts are individually more complex than a typical Cedar policy, so each sample carries more weight.

---

## 9. Growth Triggers — When to Add Past 42

Rather than a calendar-driven expansion schedule, the corpus should grow in response to specific triggers:

| Trigger | Action | Ceiling impact |
|---------|--------|---------------|
| **New language feature ships** (e.g., `choice` type) | Update all affected FUTURE samples. Add 1-2 new samples if the feature unlocks a genuinely new pattern. | May push toward upper band (45-50). |
| **New domain family proves Precept-fit** | Add one core + one extended sample in the new domain. Retire an experimental sample if one was the exploration vehicle. | Net neutral or +1-2. |
| **Language surface grows significantly** (post-Wave-3) | Audit whether new construct combinations need dedicated demonstration. | Justifies moving from 42-45 toward 48-52. |
| **Community/user request for a specific domain** | Add to extended canon if the domain passes the three-axis test. | +1-2. |
| **Proposal evaluation needs a test sample** | Add to experimental tier. Promote or retire after proposal ships or is closed. | Experimental tier absorbs this. |

**What should NOT trigger growth:**
- "We haven't added a sample in a while."
- "This domain sounds interesting."
- "We need more samples for the README."
- Competitive pressure from systems with different architectures.

---

## 10. Practical Implications

### For the current initiative

Steinbrenner's 42-target is confirmed as a sound near-term goal. It's at the low end of the optimal band, which is the right place to start — grow from quality, not toward a number.

### For the long term

- Maintain a formal sample registry (even if it's just a markdown table) that tracks each sample's tier, domain family, workflow shape, primary construct coverage, and last-updated date.
- Institute a marginal-value gate: every proposed sample addition past 42 must state what it teaches that nothing else teaches, on at least two of three axes.
- Re-evaluate the ceiling after Wave 3 ships. If `choice`, `date`, `decimal`, and `integer` all land, the language surface will be materially larger, and the corpus may justifiably grow toward 48-52.

### For maintenance

- Core canon samples are updated in the same PR as any language change.
- Extended canon samples are updated in a follow-up pass within the same release cycle.
- Experimental samples are updated best-effort.
- If the update-pass for a single feature exceeds 2 person-days, that's a signal to invest in migration tooling, not to accept sample staleness.

---

## 11. The Short Answer

The realistic long-term upper end for Precept's sample corpus is **50 samples**, with an architectural hard ceiling at **55**. The 42 near-term target is right. Past 50, maintenance costs reliably exceed marginal value for a DSL of this surface area and scope.

The number matters less than the discipline: every sample must teach something new. A corpus of 45 high-quality, well-tiered samples is worth more than 65 that vary in quality and overlap in structure.

---

*This document is research input for the sample-realism initiative. It provides architectural reasoning for corpus sizing. Specific sample selection and authoring decisions are owned by the implementing agents, subject to these ceilings and the tiered model.*
