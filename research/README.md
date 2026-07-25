# Research

Technical and cross-domain research that informs Precept's language design, architecture, tooling, and repository-level policy.

Research lives in this repository. Proposal decisions and canonical proposal bodies live in GitHub issues.

## Start here

- **[`research/INDEX.md`](INDEX.md)** — cross-corpus topic-to-file map. First stop for "has the team already researched X?". Spans every sub-folder.
- `research/language/README.md` — entry point for language research, issue map, and reading order
- `research/architecture/compiler/README.md` — entry point for compiler-architecture research (if present)

## Structure

| Folder | Purpose |
|--------|---------|
| `language/` | Unified language research: comparative expressiveness studies, implementation-grounded audits, and formal language references that inform GitHub proposal issues. Owned by George and Steinbrenner. See [`language/README.md`](language/README.md). |
| `architecture/` | External research on architectural patterns for the compiler and runtime: pipeline architecture, proof systems, type systems, state-graph analysis, language-server integration, runtime APIs. See [`architecture/README.md`](architecture/README.md). |
| `philosophy/` | Product-philosophy evidence: entity-first positioning, category analysis, and durable conclusions promoted from domain-specific research. Grounds claims in `docs/philosophy.md`. Owned by Frank. |
| `product/` | Product-level landscape and positioning research: the entity-governance landscape, product-management framing, README positioning studies. |
| `security/` | Security investigations. |
| `brand/` | Brand research: positioning studies, README and hero research, visual identity precedent, voice and tone. Owned by the brand domain rather than by the language and architecture work. See [`brand/README.md`](brand/README.md). |
| `design-system/` | Design-system and UX research: semantic visual studies, cross-surface UX research, interaction-pattern analysis, accessibility findings. Owned by the product-facing visual system rather than by the language and architecture work. See [`design-system/README.md`](design-system/README.md). |
| `references/` | Mirrored excerpts and source captures for external material a decision rests on, one subfolder per topic. Defends citations against URL rot. |
| `archive/` | Research retired from active circulation, kept as historical context. |

## Storage Rule

Use `research/` for:

- technical research
- architecture and tooling research
- implementation-grounded feasibility studies
- cross-domain synthesis that affects more than one domain
- temporary incubation work that does not yet have a clear long-term owner

Do not use the language and architecture folders as a catch-all for brand or UX research. Those two areas live in `research/` as well, but in their own folders, owned by their own domains.

- Brand research belongs in `research/brand/`.
- Design-system and UX research belongs in `research/design-system/`.
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

The status field is the **inbound signal for the promote-or-cite rule** in `.claude/skills/research/SKILL.md § Step 7`. Research with `Active` status that lacks both the `horizon groundwork` qualifier and inbound citations is shadow policy and will be flagged by `precept-reviewer` § 14 (Stage-1 Research-Doc Review Path).

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

- `research/brand/` — Brand research owned by J. Peterman. (Moved here from `design/brand/research/`; that path no longer exists.)
- `research/design-system/` — Design-system and UX research owned by Elaine. (Moved here from `design/system/research/`; that path no longer exists.)
- `design/brand/` — The brand artifacts themselves: brand spec, decisions, explorations, and artifact reviews. Research grounding them lives in `research/brand/`.
- `design/system/` — The product-facing visual-system artifacts. Research grounding them lives in `research/design-system/`.
- `docs/language/precept-language-spec.md` — The DSL spec that this research informs.
