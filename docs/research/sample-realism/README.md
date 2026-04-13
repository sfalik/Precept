# Sample Realism Research

This directory contains sixteen research artifacts produced across the sample-realism initiative. They range from external benchmarks and architectural philosophy to audits and concrete portfolio plans. This README classifies what's here, marks which conclusions belong to sample design versus broader product philosophy, and tells future contributors where to put new work.

---

## Document Map

### Corpus Planning — What to Build

Actionable plans for corpus size, shape, phasing, and curation economics.

| Document | Author | Summary |
|----------|--------|---------|
| `steinbrenner-sample-portfolio-plan.md` | Steinbrenner (PM) | 42-sample target with domain-lane distribution, difficulty tiers, and phased rollout sequence. The operational blueprint. |
| `steinbrenner-sample-ceiling-plan.md` | Steinbrenner (PM) | Ceiling discipline: 30–36 operating range, 42 hard cap, core canon of 12–14 flagships plus an extended set. Curation economics over count inflation. |
| `steinbrenner-state-graph-taxonomy-planning.md` | Steinbrenner (PM) | Two-axis taxonomy for workflow, hybrid, and stateless/entity sample planning; converts realism research into lane priorities, rewrite order, and sequencing. |
| `steinbrenner-eam-maintenance-sample-planning.md` | Steinbrenner (PM) | Places EAM / maintenance across W3, W4, H1, and future E1/E2/E3 lanes; recommends work-order-first sequencing plus asset/failure-code/PM companions. |

### Realism Criteria & Sample-Design Guidance — How to Evaluate and Write Samples

Standards, tests, and patterns for what makes a sample credible.

| Document | Author | Summary |
|----------|--------|---------|
| `frank-language-and-philosophy.md` | Frank (Architect) | Realism test (six criteria), language-ceiling analysis per open proposal, aspirational-comment conventions, and progressive-complexity guidance. The quality constitution for samples. |
| `george-current-sample-audit.md` | George (Engineer) | File-by-file audit of all 21 current samples. Inventories strengths, recurring toy patterns, missing domains/shapes/constraints, and concrete per-sample improvement notes. The baseline. |

### Philosophy-Relevant Inputs — Conclusions That Inform Broader Product Framing

These documents were written for the sample initiative but contain conclusions that transcend sample design. They should be referenced from product philosophy, positioning, and language-design discussions.

| Document | Author | Summary | Philosophy-relevant conclusions |
|----------|--------|---------|-------------------------------|
| `frank-sample-ceiling-philosophy-addendum.md` | Frank (Architect) | Philosophy-driven corpus shape: domain-fit tiers (1–3), the dilution test (five questions), domain-ceiling model (30–42 range driven by distinct lifecycle shapes, not industry names), and corpus-layer proportions. | **Domain-fit tiers** and the **dilution test** define which domains Precept should pursue broadly — not just in samples but in positioning and go-to-market. The five-commitment table (§1.1) is a reusable philosophy reference. |
| `frank-entity-modeling-addendum.md` | Frank (Architect) | Correction to the workflow-heavy framing: Precept is a domain integrity engine, not just a workflow engine. Data/reference entities outnumber workflow entities in every real domain. Issue #22 (data-only precepts) is a category expansion, not a niche feature. | **Central to product identity.** The "workflow-only" framing is explicitly rejected. Every Precept philosophy statement should reflect the entity + workflow duality. |
| `frank-enterprise-platform-and-research-gaps.md` | Frank (Architect) | Enterprise platform survey (8 platforms + 10 supplementary sources) plus research gap analysis. Identifies lifecycle shapes, policy patterns, and domain nouns across Salesforce, ServiceNow, Pega, Appian, Camunda, IBM, Guidewire, and Temporal. | The **platform-fit analysis** (which enterprise patterns Precept models naturally vs. poorly) informs competitive positioning. The **five realism signals** and **five absent lifecycle shapes** are useful beyond sample design. |

### External Benchmark Artifacts — Evidence from Outside Precept

Primary research grounding sample decisions in external evidence. These are reference material — consult them when making domain, sizing, or positioning decisions.

| Document | Author | Summary |
|----------|--------|---------|
| `peterman-realistic-domain-benchmarks.md` | J. Peterman | Domain-by-domain benchmark across insurance, healthcare, finance, IAM, compliance, case management, and incident response. Identifies the eight-stage governed-case-file shape as Precept's natural home. |
| `peterman-sample-corpus-benchmarks.md` | J. Peterman | Corpus-size benchmarks from Temporal (75), XState (49), Dagster (36), AWS patterns, Mermaid, and JSON Schema. Finding: mid-30s is normal; 50+ needs infrastructure; tiering is universal past ~40. |
| `peterman-enterprise-ecosystem-benchmarks.md` | J. Peterman | Deep enterprise platform analysis (Salesforce, ServiceNow, Pega, Appian, Camunda, IBM, Guidewire, Temporal + 10 supplementary sources). Extracts five realism signals and twelve prioritized research lanes. |
| `peterman-entity-centric-benchmarks.md` | J. Peterman | Entity-modeling ecosystem benchmarks (15 systems: Salesforce, SAP MDG, JSON Schema, Zod, FluentValidation, Pydantic, Drools, NRules, XState, Terraform, OpenAPI, DDD, ERP). Grounds the data-only sample lane in external evidence. |
| `peterman-additional-sample-research.md` | J. Peterman | Closes the remaining external-facing lanes: public process corpora, regulatory/compliance obligations, and serious entity/reference-data standards. Recommends paired workflow + entity/reference-data sample bundles. |
| `peterman-eam-maintenance-benchmarks.md` | J. Peterman | Deep EAM / maintenance / APM domain research. Benchmarks 5 EAM platforms (SAP PM, IBM Maximo, Bentley AssetWise, Infor EAM, cloud CMMS) and 5 standards (ISO 14224, ISO 55000, MIMOSA, API 510/570/580/581, OSHA LOTO). Identifies 6 sample candidates across workflow, hybrid, and entity archetypes. Scores EAM as Tier 1 domain (24/25). |

### Structural Analysis — Graph Shapes and Domain-Depth Evidence

Research that formalizes the structural vocabulary for lifecycle graph shapes and provides domain-specific depth evidence.

| Document | Author | Summary |
|----------|--------|---------|
| `frank-state-graph-taxonomy-and-insurance-realism.md` | Frank (Architect) | Nine-shape lifecycle graph taxonomy (L, B, D, SL, ML, AG, AL, EL, CP), current corpus mapping showing four missing enterprise shapes, ACORD/Guidewire insurance field-per-state editability evidence, and insurance-depth mandate for three-sample domain suite. |

---

## What Belongs Where

| Conclusion type | Belongs in | Example |
|----------------|-----------|---------|
| "This domain is a good fit for Precept samples" | `docs/research/sample-realism/` | Domain benchmarks, portfolio plans |
| "This domain is a good fit for Precept as a product" | `docs/research/sample-realism/` (origin) → referenced from `design/brand/philosophy.md` or `docs/PreceptLanguageDesign.md` | Domain-fit tiers, entity-vs-workflow duality |
| "Samples should pass this quality test" | `docs/research/sample-realism/` | Realism test, dilution test |
| "Precept's identity includes data contracts, not just workflows" | `design/brand/philosophy.md` (canonical home) — cite `frank-entity-modeling-addendum.md` as research source | Entity modeling addendum |
| "This enterprise platform models lifecycles this way" | `docs/research/sample-realism/` | Platform surveys |
| "Precept competes with X for Y use cases" | `docs/research/` (competitive research) — cite sample-realism benchmarks as evidence | Competitive positioning derived from benchmarks |

### Four documents with dual citizenship

These originated here but carry conclusions that should propagate:

1. **`frank-sample-ceiling-philosophy-addendum.md`** → The domain-fit tiers and dilution test should be referenced from product philosophy docs when those are next revised.
2. **`frank-entity-modeling-addendum.md`** → The entity-vs-workflow identity correction should be reflected in brand philosophy and language design docs.
3. **`frank-enterprise-platform-and-research-gaps.md`** → The platform-fit analysis should inform competitive positioning research.
4. **`frank-state-graph-taxonomy-and-insurance-realism.md`** → The nine-shape lifecycle taxonomy is useful beyond sample planning — for evaluating customer use cases and competitive positioning. The ACORD field-editability findings strengthen the product's governed-integrity positioning.

These files stay here as their research origin. Philosophy and positioning docs should **cite** them, not duplicate them.

---

## Guidance for Future Research Authors

### Where to put new work

| If your research is about… | Put it in… |
|---------------------------|-----------|
| Sample quality, domain selection, corpus sizing, or sample-design patterns | `docs/research/sample-realism/` |
| A specific language proposal's design rationale | `docs/research/language/` |
| DSL expressiveness, syntax alternatives, or operator design | `docs/research/dsl-expressiveness/` |
| Competitive positioning or market analysis | `docs/research/` (new subdirectory if warranted) |
| Brand philosophy or product identity | `design/brand/` |

### Naming convention

```
{author-handle}-{topic-slug}.md
```

Examples: `frank-language-and-philosophy.md`, `peterman-realistic-domain-benchmarks.md`, `steinbrenner-sample-ceiling-plan.md`.

### What to include

1. **Author, date, and status** at the top.
2. **Purpose** — what question the document answers.
3. **Dependencies** — which prior documents it builds on.
4. **Conclusions** — explicit, separated from evidence.
5. **Philosophy flag** — if any conclusion informs product identity beyond sample design, say so explicitly. Future readers should not have to guess which findings are sample-specific and which are product-level.

### What NOT to put here

- Implementation plans for specific samples (those belong in issues or `.squad/` planning artifacts).
- Language design decisions (those belong in `docs/PreceptLanguageDesign.md` once implemented, or in `docs/research/language/` as proposals).
- Brand copy or README drafts (those belong in `design/brand/` or the README itself).

---

*Last updated: 2026-07-19. Seventeen research artifacts classified.*
