# Research

Technical and cross-domain research that informs Precept's language design, architecture, tooling, and repository-level policy.

Research lives in this repository. Proposal decisions and canonical proposal bodies live in GitHub issues.

## Start here

- `research/language/README.md` — entry point for language research, issue map, and reading order

## Structure

| Folder | Owner | Purpose |
|--------|-------|---------|
| `language/` | George + Steinbrenner | Unified language research: comparative expressiveness studies, implementation-grounded audits, and formal language references that inform GitHub proposal issues. |
| `philosophy/` | Frank | Product-philosophy evidence: entity-first positioning, category analysis, and durable conclusions promoted from domain-specific research. Grounds claims in `docs/philosophy.md`. |
| `sample-realism/` | Frank + Steinbrenner | *(Incoming — pending merge from `chore/misc`.)* Sample-specific realism research: corpus planning, domain benchmarks, realism criteria, and enterprise platform surveys. Philosophy-relevant conclusions are cited from `philosophy/`, not duplicated. |

## Storage Rule

Use `research/` for:

- technical research
- architecture and tooling research
- implementation-grounded feasibility studies
- cross-domain synthesis that affects more than one domain
- temporary incubation work that does not yet have a clear long-term owner

Do not use `research/` as a catch-all for brand or UX research.

- Brand research belongs in `design/brand/research/`.
- Design-system and UX research belongs in `design/system/research/`.
- Raw precedent and source captures belong in each domain's `references/` folder.
- Critiques of specific artifacts belong in each domain's `reviews/` folder.

## Working model

1. Capture technical and cross-domain research in `research/`.
2. Capture proposal framing, scope, and status in GitHub issues.
3. Promote accepted conclusions into decisions or specs; do not let research become shadow policy.
4. Keep repo docs evidence-oriented; link out when a reader needs the proposal body.

## Status convention (Phase 9 addition)

Every research file declares a `status:` field in YAML frontmatter. Values:

| Status | Meaning |
|---|---|
| `Active` | Research is current and informing in-flight decisions. |
| `Active — horizon groundwork` | Research is current but is intentional pre-work for a downstream decision not yet active. Use sparingly; document the intended downstream consumer. |
| `Cited` | Research is referenced from a `docs/` or another `research/` file. |
| `Promoted to: <canonical-link>` | The conclusion has been adopted into a canonical doc; the research file remains as evidence. |
| `Stale` | Research is overtaken by later work; preserved as historical context. |
| `Superseded by: <link>` | A specific later document replaces this one. |
| `Archived` | Moved to `research/archive/` with `archived: YYYY-MM-DD — <reason>` in frontmatter. |

The status field is the **inbound signal for the promote-or-cite rule** in `.claude/skills/lifecycle-1-research/SKILL.md § Step 7`. Research with `Active` status that lacks both the `horizon groundwork` qualifier and inbound citations is shadow policy and will be flagged by `precept-reviewer` § 14 (Stage-1 Research-Doc Review Path).

**Other frontmatter conventions** (per the skill's Document Structure):

```yaml
---
status: <value from above>
authored: YYYY-MM-DD
author: <name or role>
topic: <one-line topic for INDEX discoverability>
external-engagement: strong | partial | purely-internal
---
```

`external-engagement` declares the comparator-survey depth: `strong` (multiple external comparators with verbatim excerpts), `partial` (some external engagement but not exhaustive), `purely-internal` (the question is genuinely about Precept-internal state — must be honestly declared, not used as a default).

## Related

- `design/brand/research/` — Brand research owned by J. Peterman.
- `design/brand/references/` — Brand precedent, captures, and source material.
- `design/system/research/` — Design-system and UX research owned by Elaine.
- `docs/language/precept-language-spec.md` — The DSL spec that this research informs.
