Sandbox guardrails for unattended icon exploration.

Scope
- Work only inside this folder.
- Treat this folder as the entire world for the session.
- Do not read, write, rename, or delete files outside this folder.

Allowed work
- Create and edit SVG candidates in this folder.
- Create and edit {stem}.json files — one per SVG candidate (loop-owned metadata).
- Read {stem}.review.json files as user-provided review input. Never write to them.
- Read local reference files in this folder.
- Prune weak candidates only if they are inside this folder and clearly inferior duplicates.
  When removing a candidate: delete the SVG, delete {stem}.json.
  Never delete {stem}.review.json — those are user-owned.

Forbidden actions
- Do not use git.
- Do not create or modify runnable helper files such as .cmd, .bat, .ps1, .exe, .com, .vbs, .js, or .ts files as a workaround for broader execution.
- Do not access the network unless the user explicitly changes this rule.
- Do not read parent directories, sibling directories, the repo root, home directory, temp directories, or external config files.
- Do not install packages, update tools, or modify system settings.
- Do not invoke external MCP servers or GitHub tools.
- Do not open URLs, fetch web pages, or call remote APIs.
- Do not touch secrets, environment configuration, credentials, or tokens.

Operational rules
- Prefer local file reasoning over shell exploration.
- Do not use shell in this run.
- Do not create replacement wrapper scripts or temporary command files to regain shell access.
- Each candidate's data lives in {stem}.json — create or rewrite this file for every candidate change.
- User review data lives in {stem}.review.json — read these for direction, never overwrite them.
- Never append raw HTML fragments to index.html.
- Never write planning text, scoring commentary, or transcript output into any *.json file.
- Newly created SVG candidates must have a matching {stem}.json created immediately, in exploring status, even before scoring is finished.
- If a task would require leaving this folder or using the network, stop and skip that action.
- If unsure whether something is inside scope, treat it as out of scope.

Quality rules
- Stay focused on icon exploration, curation, and comparison.
- Keep the candidate set diverse and avoid churn outside the shortlisted set.
- Favor safe in-folder iteration over broad experimentation.