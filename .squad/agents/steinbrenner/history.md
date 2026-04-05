## Core Context

- Owns PM briefs, hero-evaluation rubrics, README ship planning, and cross-agent sequencing.
- Hero decisions are judged on recognizability, feature density, line budget, and adoption clarity; once a temporary domain is chosen, downstream work should execute without reopening the selection casually.
- README delivery is a gated sequence: proposal/spec first, then rewrite, review, and final sign-off.

## Recent Updates

### 2026-04-05 - Freeze-and-curate cutover became the safe team path
- Proposed freezing the exact feature SHA, cutting a fresh integration branch from 'main', and re-landing approved content as curated commits.
- Uncle Leo's review ratified that sequence as the only approved trunk-return pattern, so PM sequencing now assumes curation, validation gates, and post-cutover cleanup.
