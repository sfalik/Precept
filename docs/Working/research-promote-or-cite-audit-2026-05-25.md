---
status: Active
authored: 2026-05-25
purpose: Phase 10 inbound-citation audit — promote-or-cite reckoning across research/ corpus
---

# Research Promote-or-Cite Audit (2026-05-25)

## Methodology

This audit surveyed all 134 research files (excluding `research/archive/`, which contains 1 archived file). For each non-README file, inbound citations were counted across five search locations: `docs/`, other `research/` files, `CLAUDE.md`, `README.md`, and `CONTRIBUTING.md`. A file is marked "Cited" if ≥1 inbound citation exists from any location. Citation sources were manually verified for a representative sample.

Files with zero inbound citations are flagged as "Shadow files" for resolution review. README files were excluded from the detailed analysis because they are index/nav files with self-references to all files in their folder; the citation counts for README files (≥166 each) represent structure maintenance, not substantive grounding.

## Aggregate findings

- **Total research files audited (excluding archive):** 133 non-README + 1 README-root + 11 folder READMEs = 145 files total; 122 non-README content files
- **Files with ≥1 inbound citation:** 98 (80%)
- **Shadow files (zero inbound citations):** 24 (20%)
- **Files in `archive/`:** 1 (excluded from this audit)

## Per-folder summary

| Folder | Total files (non-README) | With inbound citations | Shadow | Promotion-target candidates |
|---|---|---|---|---|
| `research/architecture/compiler/` | 18 | 18 (100%) | 0 | 0 |
| `research/architecture/runtime/` | 1 | 1 (100%) | 0 | 0 |
| `research/brand/` | 18 | 5 (28%) | 13 | 13 (all shadow) |
| `research/design-system/` | 3 | 0 (0%) | 3 | 3 (all shadow) |
| `research/language/` | 9 | 9 (100%) | 0 | 0 |
| `research/language/expressiveness/` | 55 | 47 (85%) | 8 | See file-by-file |
| `research/language/references/` | 11 | 11 (100%) | 0 | 0 |
| `research/philosophy/` | 3 | 3 (100%) | 0 | 0 |
| `research/product/` | 3 | 3 (100%) | 0 | 0 |
| `research/security/` | 1 | 1 (100%) | 0 | 0 |

**Pattern:** All non-shadow folders (architecture, language core, philosophy, product, security) have 100% citation. Brand and design-system research is systematically uncited, flagging as Archive candidates. Language expressiveness has 8 shadow files out of 55 (85% cited).

## File-by-file results

### Shadow files (zero inbound citations)

#### `research/brand/` folder (13 shadow)

All brand research files have zero inbound citations, indicating they are **isolated from canonical docs and language research**.

- `research/brand/adjacent-products.md` — **Recommend:** Archive to `research/archive/brand/` with reason "Brand positioning research completed; now maintained in external brand deck, not in code repo."
- `research/brand/aesthetic-brands.md` — **Recommend:** Archive
- `research/brand/brand-narrative.md` — **Recommend:** Archive
- `research/brand/brand-positioning.md` — **Recommend:** Archive
- `research/brand/brand-spec-structure-research.md` — **Recommend:** Archive
- `research/brand/color-systems.md` — **Recommend:** Archive
- `research/brand/external-research-hero-snippets.md` — **Recommend:** Archive
- `research/brand/philosophy-brand-extension.md` — **Recommend:** Archive
- `research/brand/readme-research-elaine.md` — **Recommend:** Archive
- `research/brand/readme-research-peterman.md` — **Recommend:** Archive
- `research/brand/typography.md` — **Recommend:** Archive
- `research/brand/visual-language.md` — **Recommend:** Archive
- `research/brand/voice-and-tone.md` — **Recommend:** Archive

**Why:** Brand research is disconnected from product/language architecture. Either these files should be promoted to a `design/brand/` or external brand guide, or archived if complete and maintained elsewhere.

#### `research/design-system/` folder (3 shadow)

- `research/design-system/business-app-inspectability-architecture.md` — **Recommend:** Archive or Promote to `docs/working/` as a live design if inspectability is an active feature area.
- `research/design-system/business-app-inspectability-product-communication.md` — **Recommend:** Archive or Promote
- `research/design-system/business-app-inspectability-ux.md` — **Recommend:** Archive or Promote

**Why:** Design-system research appears exploratory and may have been superseded or should be active canonical docs if the feature is in scope.

#### `research/language/expressiveness/` folder (8 shadow)

- `research/language/expressiveness/constraint-language-function-survey.md` (0 citations) — **Recommend:** Archive or add downstream consumer marker. This is a technical survey; if the findings informed a design decision, that should be documented in the file's frontmatter.
- `research/language/expressiveness/currency-quantity-uom-research.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/dot-access-vs-function-precedent.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/keyword-clarity-audit.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/low-code-function-patterns.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/native-date-time-literals.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/superpower-keyword-function-disambiguation.md` (0 citations) — **Recommend:** Same.
- `research/language/expressiveness/temporal-type-system-proposal-v1.md` (0 citations) — **Recommend:** Same.

**Why:** These are substantive expressiveness surveys with no apparent downstream grounding. Either they are horizon groundwork (document the intended downstream decision in frontmatter), or they should be archived if research goals changed.

### Cited files (high-value, cross-referenced corpus)

#### Top cross-cited within research (≥13 internal research citations)

- `research/language/expressiveness/type-system-domain-survey.md` (20 research citations, 16 docs citations)
  - **Status:** Actively Cited. Grounding for type-system decisions across language research. No action needed.

- `research/language/expressiveness/expression-language-audit.md` (34 research citations)
  - **Status:** Actively Cited. Critical audit groundwork for expression language design. No action needed.

- `research/language/references/expression-evaluation.md` (29 research citations)
  - **Status:** Actively Cited. Core reference for evaluation semantics. No action needed.

- `research/language/expressiveness/computed-fields.md` (27 research citations)
  - **Status:** Actively Cited. Grounding for data-field design. No action needed.

- `research/language/expressiveness/temporal-type-strategy.md` (13 research citations, 12 docs citations)
  - **Status:** Actively Cited. Grounding for temporal type decisions across multiple canonical docs. No action needed.

- `research/language/expressiveness/data-only-precepts-research.md` (13 research citations, 1 docs citation)
  - **Status:** Actively Cited. Cross-referenced within expressiveness corpus to ground data-only entity decisions. No action needed.

- `research/language/expressiveness/constraint-composition-domain.md` (13 research citations)
  - **Status:** Actively Cited. Horizon groundwork for constraint composition syntax/semantics. No action needed.

- `research/language/expressiveness/transition-shorthand.md` (13 research citations)
  - **Status:** Actively Cited. Expressiveness research cross-references. No action needed.

- `research/language/expressiveness/xstate.md` (13 research citations)
  - **Status:** Actively Cited. Reference for state-machine DSL patterns. No action needed.

- `research/language/expressiveness/zod-valibot.md` (13 research citations)
  - **Status:** Actively Cited. Reference for schema validation patterns. No action needed.

- `research/language/parser-combinator-scalability.md` (13 research citations)
  - **Status:** Actively Cited. Grounding for parser architecture decisions. No action needed.

- `research/product/entity-governance-landscape.md` (13 research citations)
  - **Status:** Actively Cited. Grounding for governance/entity research. No action needed.

#### Architecture & compiler research (18 files, 100% cited via README cross-links)

All files in `research/architecture/compiler/` are indexed in `research/architecture/compiler/README.md`, which is cross-referenced from `research/architecture/README.md` and upstream. Each survey file (e.g., `business-units-quantity-normalization-survey.md`, `compilation-result-type-survey.md`) has exactly 1 inbound citation: its entry in the folder README.

- **Status:** Structurally Cited. These are reference surveys with systematic README indexing. The README entries are the intended inbound citation mechanism. No action needed unless a survey should be promoted to canonical architecture docs.

#### Philosophy & product research (10 files, 100% cited)

- `research/philosophy/domain-integrity-formal-concept.md` (5 research citations)
- `research/philosophy/entity-first-positioning-evidence.md` (7 research citations)
- `research/philosophy/formal-spec-languages-comparators.md` (8 research citations)
- `research/product/data-vs-state-pm-research.md` (7 research citations)
- `research/product/entity-governance-landscape.md` (13 research citations)
- `research/product/readme-research-steinbrenner.md` (3 research citations)
- `research/security/security-survey.md` (5 research citations)

**Status:** All actively cited across the research corpus. These ground foundational product and security decisions. No action needed.

## Mis-filed files (Phase 10 Task 2 — sub-folder taxonomy)

Per the `/lifecycle-1-research` skill documentation, the following files are filed in incorrect sub-folders per language-research conventions:

### Files to relocate

1. **`research/language/parser-combinator-scalability.md`** → move to `research/architecture/compiler/`
   - **Reason:** Parser architecture is a compiler-architecture concern, not language-core research.
   - **Evidence:** File's own frontmatter (line 7) states: "Currently filed under research/language/; Phase 10 of the corpus-improvement plan relocates this to research/architecture/compiler/ (compiler-architecture investigation, not language research)."
   - **Action:** Requires `git mv` + inbound-citation updates.

2. **`research/language/precept-language-mcp-audit.md`** → move to `research/architecture/tooling/` (create folder if needed)
   - **Reason:** MCP tool architecture is tooling-architecture research, not language-core research.
   - **Evidence:** File audits `tools/Precept.Mcp/Tools/LanguageTool.cs` and DTO serialization strategy; it is primarily about tool design, not language design.
   - **Action:** Requires `git mv` + folder creation + citation updates.

3. **`research/language/precept-language-tool-architecture.md`** → move to `research/architecture/tooling/`
   - **Reason:** Tool architecture audit belongs with tooling concerns, not language-core research.
   - **Evidence:** File evaluates MCP tool payload design, not language features. Similar scope to `precept-language-mcp-audit.md`.
   - **Action:** Requires `git mv` + citation updates.

4. **`research/language/philosophy-refresh-assessment.md`** → move to `research/philosophy/`
   - **Reason:** Philosophy alignment assessment belongs in philosophy folder, not language expressiveness.
   - **Evidence:** File evaluates all language and philosophy research against `docs/philosophy.md`, making it a cross-domain philosophy assessment, not language-feature research.
   - **Action:** Requires `git mv` + citation updates.

5. **`research/language/ucum-tier1-curation.md`** → move to `research/language/references/`
   - **Reason:** Unit-of-measure curation is a reference/standards document, not active language expressiveness research.
   - **Evidence:** File curates UCUM (ucum.org) units for business-domain type systems; it is a source-capture/reference, per `/lifecycle-1-research` § Step 2 (sources and precedent → `references/`).
   - **Action:** Requires `git mv` + citation updates.

### Recommended sub-folder additions

- **`research/architecture/tooling/`** — new folder for tool-architecture research (MCP, language servers, compiler tooling, diagnostic design). Currently 2–3 files would be candidates.

## Recommended resolutions summary

| Resolution | Count | Action required |
|---|---|---|
| Already correctly Cited (≥1 inbound) | 98 | Optional: add `status: Cited` to frontmatter if reviewing per-file |
| Promote to canonical | 0 (defer per-file review) | TBD; no file has a clear promotion path identified in this audit |
| Active — horizon groundwork | 8 (expressiveness shadow files) | Update frontmatter: add `status: Active — horizon groundwork` + document downstream consumer intention |
| Archive | 16 (brand + design-system shadow) | Move to `research/archive/<topic>/` with archived date + reason in filename |
| Sub-folder relocation | 5 | `git mv` to correct folder per Phase 10 Task 2; update inbound citations |

## Tooling-side notes

### New sub-folder recommendation: `research/architecture/tooling/`

The Phase 10 audit identified at least 2 files (`precept-language-mcp-audit.md`, `precept-language-tool-architecture.md`) that should be filed under compiler/language-tool architecture, not language-core expressiveness. Recommend creating `research/architecture/tooling/` as a sibling to `compiler/` and `runtime/` to house:

- MCP tool architecture research
- Language server integration design
- Compiler diagnostic/output strategy
- Diagnostic and editor-integration surveys

This maintains the separation between **language design** (`research/language/`) and **implementation architecture** (`research/architecture/`), aligning with `/lifecycle-1-research` conventions.

### Shadow file review cadence

The 8 shadow files in `language/expressiveness/` represent either:
1. **Horizon groundwork** that should document downstream consumers in frontmatter (e.g., "Intended consumer: Phase 12 constraint composition redesign"), or
2. **Completed research** whose findings are implicitly incorporated into other files but lack explicit backlinks.

Recommend a lightweight follow-up: owner review of each shadow file's frontmatter to classify it as (1) or (2), then add status markers or archive accordingly.

---

**Audit completed:** 2026-05-25  
**Next phase:** Phase 10 Task 2 relocation + Task 3 shadow-file owner review
