# Sample Ceiling Philosophy Addendum

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-05-18
**Status:** Research addendum — extends sample-realism research with philosophy and domain-fit analysis
**Depends on:** `frank-language-and-philosophy.md`, `peterman-realistic-domain-benchmarks.md`, `steinbrenner-sample-portfolio-plan.md`

---

## Purpose

The existing sample-realism research establishes language guardrails, realism criteria, domain benchmarks, and a 42-sample portfolio plan. What it does not make explicit is:

1. How Precept's core philosophy should shape the **size and shape** of the sample corpus — not just its contents.
2. What kinds of domains Precept is **especially well-suited for**, and why.
3. What domains or example styles would **dilute** the corpus even if they increase count.
4. How domain breadth should influence the **realistic upper-end sample range**.

This addendum makes those four things explicit. It is philosophy and synthesis, not implementation planning.

---

## 1. How Precept Philosophy Shapes the Corpus

### 1.1 The philosophy, restated

Precept's design philosophy (from `PreceptLanguageDesign.md` and `design/brand/philosophy.md`) rests on five load-bearing commitments:

| # | Commitment | Implication for samples |
|---|-----------|------------------------|
| 1 | **Prevention, not detection.** Invalid states are structurally impossible. | Every sample must demonstrate at least one case where the contract prevents a state that the domain genuinely forbids. If a sample has no invariant or assert that a domain expert would recognize as a real business constraint, it is not demonstrating the product's reason for existing. |
| 2 | **One file, complete rules.** Every guard, constraint, invariant, and transition lives in the definition. | Samples must be self-contained. A sample that requires external context or assumes "the real rules are elsewhere" contradicts the product's foundational claim. The sample IS the complete contract. |
| 3 | **Full inspectability.** At any point you can preview every possible action. | The corpus should include samples where inspection is genuinely informative — multiple events available from one state, guards that may or may not pass depending on data, competing transition rows. Samples where every path is obvious without inspection waste Precept's most distinctive capability. |
| 4 | **Deterministic semantics.** Same definition + same data = same outcome. | Samples should not model inherently non-deterministic processes (concurrent actors racing, external system polling, real-time sensor fusion). The definition is the truth. If a domain requires probabilistic or time-race behavior to be credible, it is a poor Precept domain. |
| 5 | **AI is a first-class consumer.** Keyword-anchored, flat, parseable, tool-queryable. | The corpus is training data for AI authoring. Every sample must be fully comprehensible to an AI agent using `precept_language`, `precept_compile`, and `precept_inspect` — no tribal knowledge, no hidden conventions, no implicit sequencing that can only be understood by reading a human tutorial. |

### 1.2 What this means for corpus size

Philosophy does not dictate a specific number. It dictates a **minimum quality floor** that constrains how many samples the corpus can sustain without filler.

**The floor:** Every sample in `samples/` must demonstrate at least one philosophy commitment credibly. If it doesn't, it belongs in `test/` (if it's a regression case), in a tutorial (if it teaches syntax), or nowhere (if it's filler).

**The ceiling:** A sample that meets the floor but covers a domain already well-represented in the corpus adds less value than one that opens a new domain lane. The corpus should grow until it covers the natural domain breadth of the product (§2 below), then stop. Adding samples past that point is count inflation.

**The current 42 target** is defensible only if every addition either fills a genuine domain gap or deepens an existing sample that is currently too shallow. The number 42 is not sacred. 35 excellent samples beat 42 where seven are thin.

### 1.3 Shape over size

The corpus should have a recognizable shape:

| Layer | Purpose | Target proportion |
|-------|---------|-------------------|
| **Teaching** | Entry ramps: tiny, obvious, explain the DSL mechanics | ~5% (2–3 samples) |
| **Standard** | Working domain contracts: real business rules, real exception paths, real inspection value | ~50–55% |
| **Complex** | Full-pressure domain models: dense policy, multiple exception loops, compound eligibility, collection-heavy workflows | ~25–30% |
| **Data-only** | Reference/configuration entities: field integrity without state machines (when #22 ships) | ~10–15% |

The standard layer is the product's center of gravity. It is where a developer decides "I could build my workflow in this." The teaching layer exists only as an entry ramp. The complex layer exists only to prove the language handles real load. The data-only layer exists only to prove Precept isn't workflow-only.

If the corpus becomes majority-teaching, it looks like a toy. If it becomes majority-complex, it looks unapproachable. If it becomes majority-data-only, it contradicts the product's primary identity as a state-governed integrity engine.

---

## 2. Domains Precept Is Especially Well-Suited For

### 2.1 The sweet spot

Precept's architecture — one entity, explicit states, explicit events, readable guards, durable constraints, full inspectability — has a natural home. The product is strongest where:

1. **A single business entity moves through a governed lifecycle.** Not a swarm of entities interacting. Not a pipeline of tasks. One thing, from creation to closure, with rules at every transition.

2. **Decisions have institutional consequences.** Approval means money moves. Denial means rights are withheld. Escalation means regulatory exposure changes. The stakes make the contract worth writing.

3. **The rules are policy-shaped, not algorithm-shaped.** Eligibility thresholds, categorical gates, document requirements, approval chains, regulatory constraints. Not matrix multiplication, not pathfinding, not scheduling optimization.

4. **Inspectability has business value.** Someone — an auditor, a compliance officer, a case reviewer, an AI agent — needs to ask "what can happen next, and why?" The answer needs to be deterministic and complete.

5. **The lifecycle has non-trivial branching.** At least one point where the path forward depends on data, not just sequence. Exception routing, conditional approval, evidence-loop re-entry, partial outcomes.

### 2.2 Domain families ranked by Precept fit

| Tier | Domain family | Why it fits | Sample role |
|------|--------------|-------------|-------------|
| **Tier 1 — Native** | Claims, disputes, cases, appeals | One case file, many decision points, evidence loops, denial/appeal, audit trail. This is the canonical Precept shape. | **Flagship.** These should be the densest, most polished samples. |
| **Tier 1 — Native** | Financial approvals and exceptions | Approval chains, threshold routing, 3-way match, discrepancy queues, reconciliation gates. The policy density is genuine and the inspection value is high. | **Flagship.** These justify computed fields, decimal, and named rules. |
| **Tier 1 — Native** | Compliance and regulatory filing | Eligibility rules, document requirements, window deadlines, escalation. Pure policy enforcement. | **Core.** These justify choice, date, and conditional invariants. |
| **Tier 2 — Strong** | Identity and access governance | Access grants, attestation, SoD checks, recertification, revocation. Governed lifecycle with clear inspection value. | **Supporting.** These justify field constraints and declaration guards. |
| **Tier 2 — Strong** | Healthcare authorization and intake | Clinical review, urgency routing, denial reasons, appeal paths. Regulated and evidence-bearing. | **Supporting.** These justify date and rich policy guards. |
| **Tier 2 — Strong** | HR lifecycle management | Onboarding, leave, performance review, disciplinary action. Governed workflows with approval chains. | **Supporting.** Good for edit declarations, collection usage, constraint variety. |
| **Tier 3 — Credible** | Scheduling, logistics, fulfillment | Queue management, inspection outcomes, return authorization. Fits when the entity is a single case/order, not an orchestration graph. | **Breadth.** These show Precept's range but shouldn't dominate. |
| **Tier 3 — Credible** | Reference data and configuration | Customer profiles, rate cards, provider directories. No lifecycle, just field integrity. | **Supporting.** These justify data-only precepts (#22) and prevent the "workflow-only" perception. |

### 2.3 The Tier 1 test

A domain belongs in Tier 1 if a reader can look at the sample and feel three things simultaneously:

1. The entity has a **real lifecycle** with genuine branching.
2. The rules have **institutional consequences** — not just data validation, but policy enforcement.
3. The contract makes the whole thing **inspectable** — you can ask "what can happen now?" and the answer is both complete and meaningful.

Peterman's formulation is exactly right. The corpus should be weighted toward domains that pass all three.

---

## 3. Domains and Styles That Dilute the Corpus

### 3.1 The dilution principle

A sample dilutes the corpus when it occupies a slot without demonstrating Precept's value proposition. The count goes up, the credibility goes down. This happens in predictable ways.

### 3.2 Domains that don't fit

| Domain pattern | Why it doesn't fit | Risk if included |
|---------------|-------------------|------------------|
| **Service orchestration / pipeline choreography** | Multiple services interacting, not one entity governed. Temporal, Step Functions, and Durable Functions own this. | Makes Precept look like a worse version of Temporal. |
| **Data-pipeline ETL** | Transform chains, not lifecycle governance. No state to protect, no rules to enforce beyond schema. | Makes the corpus feel like demos for a different product. |
| **Real-time systems / sensor fusion** | Time-race behavior, continuous signals, non-deterministic input. Precept's deterministic model is the wrong fit. | Undermines the "same inputs = same outcome" promise. |
| **CRUD admin panels** | No meaningful lifecycle. Create, read, update, delete — but no governed transitions, no policy gates. | Makes Precept look like overkill for basic data entry. |
| **UI component state** | Client-side ephemeral state. React/Vue state machines exist for this. No institutional consequence. | Trivializes the product. TrafficLight and CrosswalkSignal are already the floor. |
| **Algorithmic / computational problems** | Sorting, pathfinding, optimization. Precept is policy, not algorithm. | Confuses the product identity. |

### 3.3 Styles that dilute even in good domains

| Style | Problem | Example |
|-------|---------|---------|
| **"Draft → Approved → Closed" ladders** | No branching, no exception, no evidence. A linear approval chain with no guards is not a domain contract — it's a placeholder. | A purchase-order sample that has three states, one approval event, no rejection path, and no amount threshold. |
| **Syntax showcase samples** | Exist to exercise a language feature, not to model a domain. Guards are artificial, field names are generic, states are labeled for feature coverage. | A sample named "collection-demo" that uses set, queue, and stack in one file with no domain context. |
| **Duplicated domain coverage** | A second loan-application sample that covers the same domain shape with slightly different field names. | `personal-loan-application` alongside `loan-application` when the state graph is identical. |
| **Aspirational-majority files** | Samples where more than 40% of lines are FUTURE comments. These are wish lists, not working contracts. | A healthcare-prior-auth sample that can't express clinical criteria, urgency routing, or denial reasons without future syntax — leaving the active code as a bare intake form. |
| **Forced-workflow data entities** | Reference data (product catalog, rate card, address book) shoehorned into state machines because data-only precepts (#22) don't exist yet. | A "product-catalog" with states `Active`/`Inactive` that exist solely to justify a state machine. |

### 3.4 The dilution test

Before adding any sample, ask:

1. **Does this domain require governed lifecycle transitions?** If no, it's either data-only (wait for #22) or wrong for Precept.
2. **Does this sample demonstrate a real business rule that Precept prevents?** If no, the sample is decorative.
3. **Does this sample offer inspection value?** If every path is obvious without calling `inspect`, the sample wastes Precept's most distinctive capability.
4. **Is this domain already well-represented?** If yes, the new sample must add a materially different policy shape or exception pattern — not just a different industry name on the same state graph.
5. **Would removing this sample leave a visible gap in the corpus?** If no, the sample is filler.

---

## 4. How Domain Breadth Influences the Realistic Upper-End Range

### 4.1 The domain-ceiling model

The sample ceiling should be driven by **distinct, defensible domain shapes** — not by a target number.

Here is the model: count the number of genuinely distinct lifecycle shapes that Precept can credibly serve, then allocate 1–3 samples per shape depending on policy density.

| Domain shape | Distinct lifecycle patterns | Samples needed per pattern | Subtotal |
|-------------|---------------------------|---------------------------|----------|
| Claims, disputes, appeals | 3–4 (insurance, chargeback, benefits, warranty) | 2 each (standard + complex) | 6–8 |
| Financial approvals and exceptions | 3–4 (invoice, PO, expense, loan servicing) | 1–2 each | 4–6 |
| Compliance and regulatory | 2–3 (KYC/AML, permit/filing, audit/certification) | 1–2 each | 3–5 |
| Identity and access governance | 1–2 (access grant, lifecycle review) | 1–2 each | 2–3 |
| Healthcare authorization | 1–2 (prior auth, treatment auth) | 1–2 each | 2–3 |
| HR lifecycle | 2–3 (onboarding, leave, performance) | 1 each | 2–3 |
| Scheduling and logistics | 2–3 (appointment, dispatch, return/inspection) | 1 each | 2–3 |
| Service intake and case management | 2–3 (helpdesk, incident, maintenance) | 1 each | 2–3 |
| Reference data (data-only) | 3–4 (master record, rate card, directory entry, config) | 1 each | 3–4 |
| Teaching | 2 (traffic light, crosswalk — already exist) | 1 each | 2 |
| **Total** | | | **28–40** |

### 4.2 What this tells us about the ceiling

The 42 target from Steinbrenner's portfolio plan sits at the top of this range. That's fine — but only if every sample beyond ~30 is genuinely earning its slot by covering a distinct policy shape or domain exception pattern that the existing samples do not.

**The realistic range is 30–42.** Below 30, there are domain gaps that weaken the product's breadth claim. Above 42, samples start duplicating lifecycle shapes with cosmetic domain differences. The range is driven by how many genuinely distinct governed-lifecycle patterns exist in business operations — not by how many industry verticals can be named.

### 4.3 The breadth-vs-depth tradeoff

When approaching the ceiling, depth beats breadth.

A single `insurance-claim` sample with deep exception handling, evidence loops, partial approval, and fraud investigation is worth more than three thin samples across insurance, healthcare, and finance that all follow the same "intake → review → approve/deny → close" shape.

The corpus should pursue breadth until each major domain shape is represented, then pivot to depth within the best-fit shapes. Steinbrenner's phased plan (rewrite anchors first, add missing lanes, then stabilize) already sequences this correctly.

### 4.4 The count-inflation warning

There is a predictable failure mode: the corpus reaches 35 samples, someone says "we said 42, we need 7 more," and the team reaches for thin samples in marginal domains. This is how dilution happens.

**Guard against it:** If the corpus is at 35 and the next candidate doesn't pass the dilution test (§3.4), the target is 35. The number is a guide, not a quota.

### 4.5 Philosophy-driven ceiling summary

| Factor | Effect on ceiling |
|--------|-------------------|
| Prevention-not-detection requires real constraints | **Lowers** ceiling — samples without genuine constraint prevention should not exist |
| One-file completeness requires self-containment | **Neutral** — doesn't affect count, affects quality |
| Inspectability requires branching and guard density | **Lowers** ceiling — linear approval ladders are excluded |
| Deterministic semantics excludes time-race/probabilistic domains | **Lowers** ceiling — some otherwise-plausible domains are excluded |
| AI-first requires machine-parseable, tool-queryable samples | **Neutral** — quality constraint, not count constraint |
| Domain breadth across governed-lifecycle patterns | **Raises** ceiling — many real domains have distinct lifecycle shapes |
| Depth within best-fit domains | **Stabilizes** ceiling — diminishing returns past domain coverage |

Net effect: the philosophy supports a corpus in the **30–42 range**, with the exact count determined by how many samples pass the quality floor. The upper bound is the number of genuinely distinct domain shapes, not the number of industry names.

---

## 5. Synthesis

### 5.1 What this addendum adds to the existing research

The existing research answers "what should we build?" (Steinbrenner), "what domains should we look at?" (Peterman), and "what language decisions affect sample quality?" (Frank prior). This addendum answers the question Shane raised: **why this many, why these domains, and what should we refuse?**

The answer traces back to philosophy:

- **Size** is bounded by the number of genuinely distinct governed-lifecycle shapes that demonstrate prevention, inspectability, and determinism. That number is 30–42.
- **Shape** is dictated by the product's center of gravity: standard-complexity domain contracts in the claims/finance/compliance/governance families, with teaching and data-only layers as supporting evidence.
- **Domain selection** is driven by Tier 1–3 fit, not by industry-name diversity. A third insurance-variant sample that passes the dilution test earns its slot. A first IoT-sensor sample that doesn't demonstrate governed policy does not.
- **Refusal** is the most important portfolio discipline. Every sample that doesn't demonstrate at least one of prevention, inspectability, or deterministic policy enforcement is worse than no sample — it teaches the wrong mental model of what Precept is for.

### 5.2 The one-sentence version

**The sample corpus should be as large as the number of genuinely distinct governed-lifecycle patterns that Precept can credibly serve, and not one sample larger.**

---

*This document is a research addendum for the sample-realism initiative. It does not change the 42-sample target or the phased plan — it provides the philosophical grounding for why that target is defensible and how to recognize when a candidate sample should be refused.*
