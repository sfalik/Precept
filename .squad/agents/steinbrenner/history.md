## Core Context

- Owns PM briefs, hero-evaluation rubrics, README ship planning, and cross-agent sequencing.
- Hero decisions are judged on recognizability, feature density, line budget, and adoption clarity; once a temporary domain is chosen, downstream work should execute without reopening the selection casually.
- README delivery is a gated sequence: proposal/spec first, then rewrite, review, and final sign-off.

## Recent Updates

### 2026-04-08 - Language research plan fully executed
- The three-batch domain-first plan is complete on `squad/language-research-corpus`, closed by `3cc5343` after Batch 1 `54a77da` and Batch 2 `48860ae`.
- PM guardrails held through closeout: no proposal-body edits, horizon domains remained represented, and the final indexes point active proposals back to their grounding research.

### 2026-04-08 - Language research batching finalized
- Finished `docs/research/language/domain-research-batches.md` as the domain-first execution plan for the corpus.
- Regrouped Batch 1 so constraint composition stays with the rest of the validator/rule/declaration lane instead of being split into a later pass.
- Preserved the session rules: no proposal-body edits during corpus work, horizon domains stay visible, and each completed batch closes with its own commit (`54a77da` for Batch 1, `48860ae` for Batch 2). Batch 3 and the final README/index sweep remain open.

### 2026-04-05 - Proposal #8 finalized around named rules
- Synced the roadmap framing to rule <Name> when <BoolExpr>, locked the field-only/boolean-only boundaries, and recorded the issue rename to "Proposal: Named rule declarations."
- PM proposal guidance now requires philosophy fit, non-goals, and the configuration-like readability check on future language work.

### 2026-04-05 - Expressiveness proposal label locked for the next wave
- Created the `dsl-expressiveness` repository label and applied it to the expression-focused proposal issues #8, #9, and #10.
- Added `docs/research/dsl-expressiveness/expression-tracking-notes.md` so the team has one repo-local definition of what belongs under the tag and how it differs from `dsl-compactness`.

### 2026-04-05 - Compactness proposal label standardized on GitHub issues
- Created the `dsl-compactness` repository label and applied it to language improvement proposal issues #8, #9, #10, #11, #12, and #13.
- Verified the label now sits alongside `squad:frank` on all six proposals, giving the roadmap a durable compactness-focused slice across the language queue.

### 2026-04-05 - Proposal bodies expanded for issues #8-#10
- Expanded GitHub issues #8, #9, and #10 into a shared proposal format covering motivation, Precept-today pain, hypothetical syntax, reference-language snippets, benefits, and open questions.
- Reinforced the PM guardrail that hypothetical DSL examples in roadmap issues must be labeled as unimplemented behavior.

### 2026-04-05 - Freeze-and-curate cutover became the safe team path
- Proposed freezing the exact feature SHA, cutting a fresh integration branch from 'main', and re-landing approved content as curated commits.
- Uncle Leo's review ratified that sequence as the only approved trunk-return pattern, so PM sequencing now assumes curation, validation gates, and post-cutover cleanup.

## Learnings

- 2026-04-05: The durable repo-wide issue model should separate **workflow**, **taxonomy**, and **exceptions**. Use one shared project lifecycle (`Inbox`/`Ready`/`In Progress`/`In Review`/`Done`) for every issue type; keep labels for durable type/domain/owner/slice metadata; keep only `blocked` and `deferred` as optional exception labels because they answer questions open/closed state cannot. Relevant paths: `.squad/templates/issue-lifecycle.md`, `.squad/routing.md`, `.squad/team.md`, `docs/@ToDo.md`, `docs/research/language/README.md`, `.copilot/skills/architectural-proposals/SKILL.md`, `.squad/decisions/inbox/steinbrenner-standard-issue-workflow.md`, `.squad/skills/unified-issue-workflow/SKILL.md`.
- 2026-04-05: The durable philosophy screen for language proposals is: preserve domain integrity, deterministic inspectability, keyword-anchored flat statements, first-match routing, compile-time soundness, and AI legibility. Named guards pass only when they stay field-only, predicate-only, and explicitly avoid row-body abstraction or trivial one-clause aliases. Relevant paths: `README.md`, `docs/PreceptLanguageDesign.md`, `docs/research/dsl-expressiveness/README.md`, `docs/research/dsl-expressiveness/expression-feature-proposals.md`, `docs/research/dsl-expressiveness/expression-language-audit.md`, `.squad/decisions/inbox/steinbrenner-philosophy-pass.md`, `.squad/skills/dsl-philosophy-filter/SKILL.md`.
- 2026-04-05: Domain survey of 10 business domains (100 fields) and 5 workflow platforms confirmed choice and date as Universal-tier type gaps. Choice appeared in 41/100 fields across all 10 domains; date appeared in 30/100 fields across all 10 domains. Integer and currency have no modeling gap because `number` is a tolerable workaround. The three entity-definition platforms (ServiceNow, Salesforce, Dataverse) all treat choice and date as first-class types; the two workflow orchestrators (Camunda 8, Temporal) delegate typing to the host language. This validates proposal #25 scope exactly — no expansion needed. Relevant paths: `docs/research/language/expressiveness/type-system-domain-survey.md`, `.squad/decisions/inbox/steinbrenner-type-system-domain-research.md`.
- 2026-04-05: The clean PM split is `dsl-expressiveness` for capability-gap proposals and `dsl-compactness` for ceremony-reduction proposals. The research-backed first wave is #8 named guards, #9 ternary-in-`set`, and #10 string `.length`; shortcut features like `absorb` or inline fallback should not inherit the expressiveness tag by default. Relevant paths: `.squad/agents/steinbrenner/history.md`, `docs/research/dsl-expressiveness/README.md`, `docs/research/dsl-expressiveness/expression-tracking-notes.md`, `.squad/decisions/inbox/steinbrenner-dsl-expressiveness-tag.md`.
- 2026-04-05: A shared proposal tag is worth locking early. `dsl-compactness` cleanly groups the six language-improvement issues (#8-#13) without overloading release or owner labels, so PM filtering can track one roadmap theme across multiple rollout waves. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/decisions.md`, `.squad/orchestration-log/2026-04-05T15-17-53Z-steinbrenner.md`.
- 2026-04-05: Language proposal issues land better when they use one durable structure — problem, proposed feature, Precept today, proposed syntax, external reference code, benefits, open questions — and explicitly label hypothetical DSL as unimplemented. Pairing a current Precept snippet with concrete xstate/LINQ/Zod/FluentValidation examples makes review faster for PM and architecture. Relevant paths: `.squad/agents/steinbrenner/history.md`, `docs/research/dsl-expressiveness/README.md`, `docs/research/dsl-expressiveness/xstate.md`, `docs/research/dsl-expressiveness/linq.md`, `docs/research/dsl-expressiveness/zod-valibot.md`, `docs/research/dsl-expressiveness/fluent-validation.md`.
- 2026-04-05: When the working tree has narrowed to one documentation artifact that explains branch lineage and consolidation risks, package it as a single freeze-point commit and treat that SHA—not the moving branch name—as the planning reference.
- 2026-04-05: GitHub Projects v2 work for `sfalik/Precept` is blocked unless the active `gh` auth gains `project` and `read:project` scopes; repo-level `repo` scope is not enough for listing or creating project boards. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/decisions.md`, `.squad/identity/wisdom.md`, `.squad/identity/now.md`, `.squad/skills/`.
- 2026-04-05: Preview-panel board setup must start with a scope gate. In this repo, `gh project list --owner sfalik` fails without `read:project`, `gh project create` would still need `project`, and the old repo-project REST fallback is unavailable (`repos/sfalik/Precept/projects` returns 404). Relevant paths: `.squad/decisions/inbox/steinbrenner-preview-board.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.
- 2026-04-05: Retrying preview-panel project-board creation confirmed the same hard blocker: the active `sfalik` GitHub CLI token still has only `gist`, `read:org`, `repo`, and `workflow`, so `gh project list --owner sfalik` fails for missing `read:project` and `gh project create --owner sfalik --title "Preview Panel Redesign"` fails for missing `project` and `read:project`; classic repo projects remain unavailable because `repos/sfalik/Precept/projects` returns `404 Not Found`. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.
- 2026-04-05: After auth refresh, GitHub Projects v2 creation succeeded in the `sfalik` owner context with `gh` showing `project` scope. `gh project list --owner sfalik`, `gh project create --owner sfalik --title "Precept Preview Panel Redesign"`, and `gh project edit 1 --owner sfalik --description ...` all worked, and the board now lives at `https://github.com/users/sfalik/projects/1`. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.
- 2026-04-05: The strongest remembered language-roadmap bundle was the three research-ranked expressiveness proposals (named guards, ternary-in-`set`, string `.length`) plus the three hero-condensation reducers from corpus review (`absorb`-style event ingestion, inline `else reject`, and field-level basic constraints). The field-level constraint item remains caveated because research flags a direct conflict with Precept's keyword-anchored statement design. Relevant paths: `docs/research/dsl-expressiveness/README.md`, `docs/research/dsl-expressiveness/internal-verbosity-analysis.md`, `.squad/decisions/inbox/steinbrenner-language-proposals.md`.
- 2026-04-05: For GitHub Project v2 intake, `gh issue create --project "<project title>"` is the cleanest path when the board already exists — it creates the issue and adds it to the board in one step, avoiding a second `project item-add` pass. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

### 2026-04-05 - Language proposal sequencing locked after architecture review
- Recorded the six-issue language roadmap as a staged rollout: first wave #10 string .length and #8 named guards; second wave #9 ternary-in-set and #11 event absorb shorthand; last wave #12 inline else reject and #13 field-level constraints.
- Key learning: roadmap order should follow DSL-fit and containment risk. Proposals that pressure keyword-anchored flat statements or first-match routing belong late and need explicit architectural scrutiny.

### 2026-04-05 - Type system expansion PM scoping delivered
- Ranked type candidates by user value: enum/value-set (high), integer subtype (medium-high), date/duration (medium), constrained ranges (medium-low, overlaps #13), record/struct (non-goal).
- Key learning: type system proposals should be scored on expressiveness-gap-closed (can authors say something new?) rather than convenience (is a workaround shorter?). Enum passes that bar clearly; integer and date do not pass as cleanly because the `number` workaround is tolerable. Record/struct directly conflicts with flat-field philosophy and is a non-goal.
- Key learning: type system work has the widest blast radius of any language proposal category — it touches parser, type checker, evaluator, grammar, language server, MCP DTOs, and runtime API simultaneously. Sequencing it after Wave 1 expression foundations (#10, #8) are stable is critical to avoid destabilizing the stack mid-wave.
- Sequencing position: Wave 2 or 2.5, after #10 and #8 land, potentially parallel with #9 and #11. Enum should be the first type addition; integer and date should be evaluated after enum ships. Relevant paths: `.squad/decisions/inbox/steinbrenner-type-system-scoping.md`, `docs/research/language/expressiveness/expression-language-audit.md`.
- 2026-04-05: Beyond-v1 domain type roadmap: Re-analyzed 10 business domains (70+ residual fields) and 5 workflow platforms post-choice/post-date. Key findings: (1) The constraint system (#13) absorbs most remaining type pressure — email, phone, URL, percentage, and partially currency are all better served by field-level constraints than new types. (2) Only `integer` and `duration` are strong Phase 2 type candidates, because both need new expression semantics constraints can't provide. (3) Most surprising finding: attachment/document references appear in 10/10 domains — an unaddressed gap that neither types nor current constraints handle. (4) The never-add list includes reference/FK, record/struct, calculated fields, anyType, encrypted text, auto-number, journal/append-only, time-only, and geolocation — all conflict with Precept's single-entity isolation, flat-field philosophy, or deterministic inspectability. (5) Multipicklist may already be solved by `set<choice>` if choice can be a collection inner type. Relevant paths: `docs/research/language/expressiveness/type-system-domain-survey.md`.
