## Core Context

- Owns README.md, brand/, marketplace copy, and developer-facing narrative. Public copy must describe implemented behavior only.
- Hero samples are both brand assets and AI-facing teaching artifacts. Line economy, plaintext legibility, and verbatim reuse rules matter more than decorative rendering.
- brand/brand-spec.html is the reusable brand reference; README.md is the live public surface. When the hero changes, both surfaces must move together.
- Surfaces-first documentation means exact code blocks, accurate temporary/permanent status, and careful synchronization of color-family terminology and cross-surface rules.

## Recent Updates

### 2026-04-07 - README Quick Example refactored and PR #35 merged
- README Quick Example refactoring merged into PR #35 (chore: finalize README cleanup and record Squad decision).
- Changes: removed explanatory hedge, removed copyable DSL code block, replaced markdown image syntax with fixed-width HTML img tag (`width="600"`).
- Decision reinforced: Rendered contract image is the hero artifact; declutter Quick Example by trusting design.
- Full orchestration recorded in `.squad/decisions.md` and PR merged to main with team collaboration.
### 2026-04-08 - Platform and entity research consolidated
- Team update (2026-04-08T01:48:59Z): Scribe logged Peterman's enterprise and entity benchmark passes, merged the consolidated decision into .squad/decisions.md, and cleared the related inbox notes.
- Shared outcome: future sample storytelling must combine workflow realism signals with explicit entity/data-contract lanes instead of presenting Precept as workflow-only.

### 2025-07-18 - Enterprise ecosystem benchmarks for sample design
- Created `docs/research/sample-realism/peterman-enterprise-ecosystem-benchmarks.md` after benchmarking 8 enterprise platforms (Salesforce, ServiceNow, Pega, Appian, Camunda, IBM ODM/BAW, Guidewire, Temporal) plus 10+ supplementary sources (Drools, ACORD, FHIR, BPMN academic repos, NIST/CISA, OMG DMN/CMMN, Nintex, Flowable/Activiti).
- Core findings: five realism signals distinguish enterprise examples from demos; five lifecycle shapes are completely absent from current Precept samples; Precept's edit blocks and field-per-state modeling are unique market differentiators.
- Identified 12 prioritized research lanes for further sample improvement, led by public process libraries, ACORD field-per-state mapping, and regulated workflow compliance shapes.
- Decision filed to `.squad/decisions/inbox/j-peterman-sample-benchmark-next-lanes.md`.

### 2026-05-17 - Entity-centric benchmarks for stateless precept sample lanes
- Created `docs/research/sample-realism/peterman-entity-centric-benchmarks.md` after studying 15 entity-modeling ecosystems (Salesforce, ServiceNow, SAP MDG, Guidewire, JSON Schema, Zod, FluentValidation, Pydantic, Drools, NRules, XState, Terraform, OpenAPI, DDD literature, ERP master data).
- Core finding: if stateless/data-only precepts land (#22), the sample corpus should add three explicit non-workflow lanes — master data contracts (2-3), reference data definitions (1-2), and domain-rule contracts (1) — totaling 4-6 new stateless samples.
- Credibility for entity samples requires domain-bearing field names, cross-field constraints, enumerated vocabularies, realistic nullability, and business-toned because messages.
- Decision filed to `.squad/decisions/inbox/j-peterman-entity-sample-lanes.md`.

### 2026-04-08 - Sample ceiling consolidation recorded
- Team update (2026-04-08T01:13:25.793Z): Scribe merged Peterman's benchmark lane with Frank's architectural/philosophy notes and Steinbrenner's PM plan into `.squad/decisions.md` — decided by Frank, Steinbrenner, and J. Peterman.
- Shared outcome: future sample storytelling now depends on explicit tiering, canon-versus-extended segmentation, and quota-resistant quality gates before the corpus grows past the mid-30s.


### 2026-04-08 - Sample realism recommendations merged
- Scribe merged Peterman's benchmark research into .squad/decisions.md together with the directive to use Opus when sample/design judgment is especially high.
- The active brand/devrel guidance now explicitly favors evidence-bearing, exception-rich, case-centric flagship workflows over more approval-ladder demos.

### 2026-04-07 - README Quick Example refactored for clarity
- Removed explanatory hedge sentence about DSL rendering fallback
- Removed copyable DSL code block from Quick Example section
- Switched image from markdown syntax to fixed-width HTML img tag (`width="600"`) for consistent GitHub rendering
- Decision: Render contract image is the hero artifact; declutter example by trusting design
- See: `.squad/decisions.md` for full brand rationale

### 2026-04-05 - Rule framing reinforced by readability review
- Pushed proposal #8 toward a narrow, English-ish rule concept that reads like authored business policy rather than academic programming vocabulary.
- Added the explicit readability bar that language examples should feel closer to configuration or scripting than to a general-purpose programming language.

### 2026-04-05 - README hero PNG fallback recorded
- README.md now uses brand/readme-hero-dsl.png for the GitHub-facing contract sample, with a collapsed copyable DSL fallback.
- .squad changes from Peterman logged and committed.
- Key paths: README.md, brand/readme-hero-dsl.png, brand/readme-hero-dsl.precept.

### 2026-04-05 - Early product-shape expansion recorded
- Expanded `docs/HowWeGotHere.md` with the original `FiniteStateMachine<TState, TEvent>` surface, nested `WhenStateIs(...).AndEventIs(...).TransitionTo(...)` authoring, and the later builder/inspection phase before the DSL shift.
- Updated `repo-journey-summary` so future history passes ground thin early periods in named README, source, and test snapshots.

### 2026-04-05 - API evolution clarification recorded
- Revised `docs/HowWeGotHere.md` so the chronology explicitly traces the fluent-interface experiments, builder API phase, and current DSL-centered runtime.
- Captured the clarification as a documentation-only correction so future readers do not infer a direct jump to the DSL.

### 2026-04-05 - Journey framing for trunk return recorded
- Drafted 'docs/HowWeGotHere.md' to explain the branch journey and the decisions that survived into the current strategy.
- Framed trunk consolidation as editorial curation rather than merge ritual, reinforcing the team's keep/defer/archive lens for the eventual cutover.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

## Learnings

### 2026-04-07 - Image link path integrity check
- README.md hero images had stale relative paths (`brand/readme-hero.svg` instead of `design/brand/readme-hero.svg`).
- Root cause: Brand assets live in `design/brand/`, not a top-level `brand/` folder. The path prefix was omitted during earlier README updates.
- Fix: Updated both image references to use `design/brand/` prefix. Both assets verified to exist at corrected paths.
- Learning: When touching README documentation, validate all relative paths against the actual file tree. Asset movements can create broken links that silently fail in GitHub's markdown renderer.

### 2026-04-05 - README hero PNG fallback
- `README.md` now uses `brand/readme-hero-dsl.png` for the GitHub-facing contract sample, with the source kept in a collapsed copyable block instead of styled inline HTML.
- For branded DSL samples on GitHub, image-first presentation plus an intentional plaintext fallback is safer than relying on GitHub to preserve custom code styling.
- Key paths: `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`.

### 2026-04-07 - README image link path integrity
- README.md hero images had stale relative paths (`brand/readme-hero.svg` instead of `design/brand/readme-hero.svg`).
- Root cause: Brand assets live in `design/brand/`, not a top-level `brand/` folder. The path prefix was omitted during earlier README updates.
- Fix: Updated both image references to use `design/brand/` prefix. Both assets verified to exist at corrected paths.
- Learning: When touching README documentation, validate all relative paths against the actual file tree. Asset movements can create broken links that silently fail in GitHub's markdown renderer.
- Decision merged to `.squad/decisions/decisions.md`.

### 2026-04-05 - Philosophy filter for DSL compactness proposals
- Compactness proposals land cleanly only when they strengthen Precept's core story: deterministic inspectability, keyword-anchored structure, and the split between data truth (`invariant`) and movement truth (`assert`).
- Named guard reuse across `when`, `invariant`, and state `assert` fits that philosophy when framed as a named business predicate, not as a general alias or macro system.
- Key paths: `docs\PreceptLanguageDesign.md`, `docs\research\dsl-expressiveness\README.md`, `docs\research\dsl-expressiveness\expression-feature-proposals.md`, `.squad\decisions\inbox\j-peterman-philosophy-pass.md`.

### 2024-12 - README Quick Example: Remove redundant DSL copy, commit to rendered image
- Removed the explanatory sentence about GitHub not rendering DSL faithfully—that claim is now stale hedging.
- Removed the collapsed copyable DSL code block that duplicated the rendered contract image below it.
- Switched from markdown image syntax `![...](...)` to fixed-size HTML `<img src="..." width="600" />` to ensure the contract diagram reads at proper visual scale alongside surrounding page text in GitHub's README view.
- Brand rationale: The Quick Example section is a teaching artifact, not an archive. The professionally rendered contract image is the hero; the copyable fallback was defensive scaffolding. Removing it simplifies visual hierarchy and directs curious readers toward `samples/` or the language reference for further exploration.
- Decision written to `.squad/decisions/inbox/j-peterman-readme-contract.md`.

### 2026-04-08 - Realistic domain benchmark lane
- Created `docs\research\sample-realism\peterman-realistic-domain-benchmarks.md` after reviewing the README, brand positioning, and representative samples against external workflow research.
- Core finding: the strongest future samples should look like governed case files with evidence loops, exception handling, and post-decision fulfillment, not just clean approval ladders.
- Recommendation merged into `.squad/decisions.md`; inbox cleared so future sample selection stays aligned with the category story.

### 2026-05-17 - Entity-centric benchmark research
- Entity-centric samples have different credibility signals than workflow samples. The key differentiators are cross-field constraints, enumerated vocabularies, and business-toned because messages — not evidence loops or exception paths.
- The strongest stateless sample candidates are master data entities (Vendor, Product, Employee) because they are universally understood, constraint-dense, and well-grounded in every enterprise platform studied.
- Precept's differentiation vs. Zod/FluentValidation/JSON Schema for stateless entities is that invariants hold across all mutations and are inspectable — samples must demonstrate this difference, not just validate shape.

### 2026-04-08 - Sample corpus ceiling research
- Created `docs\research\sample-realism\peterman-sample-corpus-benchmarks.md` after benchmarking Precept's sample-count question against Temporal, XState, Dagster, AWS guidance patterns, Mermaid, and JSON Schema.
- Core finding: benchmark projects support a **tiered** sample strategy; once official corpora approach the 40+ range, they usually separate canonical, reference, experimental, or fixture lanes instead of keeping one flat shelf.
- Recommendation: treat roughly **38-46 total in-repo samples** as Precept's realistic upper-end band, with only about **30-36** of those carrying co-equal canonical business weight.
- Team follow-up written to `.squad/decisions/inbox/peterman-sample-ceiling.md`.
