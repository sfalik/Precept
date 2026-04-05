## Core Context

- Owns PM briefs, hero-evaluation rubrics, README ship planning, and cross-agent sequencing.
- Hero decisions are judged on recognizability, feature density, line budget, and adoption clarity; once a temporary domain is chosen, downstream work should execute without reopening the selection casually.
- README delivery is a gated sequence: proposal/spec first, then rewrite, review, and final sign-off.

## Recent Updates

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

