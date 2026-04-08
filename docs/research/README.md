# Research

Technical and cross-domain research that informs Precept's language design, architecture, tooling, and repository-level policy.

Research lives in this repository. Proposal decisions and canonical proposal bodies live in GitHub issues.

## Start here

- `docs/research/language/README.md` — entry point for language research, issue map, and reading order

## Structure

| Folder | Owner | Purpose |
|--------|-------|---------|
| `language/` | George + Steinbrenner | Unified language research: comparative expressiveness studies, implementation-grounded audits, and formal language references that inform GitHub proposal issues. |
| `philosophy/` | Frank | Product-philosophy evidence: entity-first positioning, category analysis, and durable conclusions promoted from domain-specific research. Grounds claims in `docs/philosophy.md`. |
| `sample-realism/` | Frank + Steinbrenner | *(Incoming — pending merge from `chore/misc`.)* Sample-specific realism research: corpus planning, domain benchmarks, realism criteria, and enterprise platform surveys. Philosophy-relevant conclusions are cited from `philosophy/`, not duplicated. |

## Storage Rule

Use `docs/research/` for:

- technical research
- architecture and tooling research
- implementation-grounded feasibility studies
- cross-domain synthesis that affects more than one domain
- temporary incubation work that does not yet have a clear long-term owner

Do not use `docs/research/` as a catch-all for brand or UX research.

- Brand research belongs in `design/brand/research/`.
- Design-system and UX research belongs in `design/system/research/`.
- Raw precedent and source captures belong in each domain's `references/` folder.
- Critiques of specific artifacts belong in each domain's `reviews/` folder.

## Working model

1. Capture technical and cross-domain research in `docs/research/`.
2. Capture proposal framing, scope, and status in GitHub issues.
3. Promote accepted conclusions into decisions or specs; do not let research become shadow policy.
4. Keep repo docs evidence-oriented; link out when a reader needs the proposal body.

## Related

- `design/brand/research/` — Brand research owned by J. Peterman.
- `design/brand/references/` — Brand precedent, captures, and source material.
- `design/system/research/` — Design-system and UX research owned by Elaine.
- `docs/PreceptLanguageDesign.md` — The DSL spec that this research informs.
