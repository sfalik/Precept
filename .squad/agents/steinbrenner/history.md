## Core Context

- Owns PM briefs, hero-evaluation rubrics, README ship planning, and cross-agent sequencing.
- Hero decisions are judged on recognizability, feature density, line budget, and adoption clarity; once a temporary domain is chosen, downstream work should execute without reopening the selection casually.
- README delivery is a gated sequence: proposal/spec first, then rewrite, review, and final sign-off.

## Recent Updates

### 2026-04-05 - Freeze-and-curate cutover became the safe team path
- Proposed freezing the exact feature SHA, cutting a fresh integration branch from 'main', and re-landing approved content as curated commits.
- Uncle Leo's review ratified that sequence as the only approved trunk-return pattern, so PM sequencing now assumes curation, validation gates, and post-cutover cleanup.

## Learnings

- 2026-04-05: When the working tree has narrowed to one documentation artifact that explains branch lineage and consolidation risks, package it as a single freeze-point commit and treat that SHA—not the moving branch name—as the planning reference.
- 2026-04-05: GitHub Projects v2 work for `sfalik/Precept` is blocked unless the active `gh` auth gains `project` and `read:project` scopes; repo-level `repo` scope is not enough for listing or creating project boards. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/decisions.md`, `.squad/identity/wisdom.md`, `.squad/identity/now.md`, `.squad/skills/`.
- 2026-04-05: Preview-panel board setup must start with a scope gate. In this repo, `gh project list --owner sfalik` fails without `read:project`, `gh project create` would still need `project`, and the old repo-project REST fallback is unavailable (`repos/sfalik/Precept/projects` returns 404). Relevant paths: `.squad/decisions/inbox/steinbrenner-preview-board.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.
- 2026-04-05: Retrying preview-panel project-board creation confirmed the same hard blocker: the active `sfalik` GitHub CLI token still has only `gist`, `read:org`, `repo`, and `workflow`, so `gh project list --owner sfalik` fails for missing `read:project` and `gh project create --owner sfalik --title "Preview Panel Redesign"` fails for missing `project` and `read:project`; classic repo projects remain unavailable because `repos/sfalik/Precept/projects` returns `404 Not Found`. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.
- 2026-04-05: After auth refresh, GitHub Projects v2 creation succeeded in the `sfalik` owner context with `gh` showing `project` scope. `gh project list --owner sfalik`, `gh project create --owner sfalik --title "Precept Preview Panel Redesign"`, and `gh project edit 1 --owner sfalik --description ...` all worked, and the board now lives at `https://github.com/users/sfalik/projects/1`. Relevant paths: `.squad/agents/steinbrenner/history.md`, `.squad/skills/github-project-v2-auth/SKILL.md`.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).
