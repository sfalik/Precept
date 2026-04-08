## Core Context

- Owns PM briefs, roadmap sequencing, hero-evaluation rubrics, GitHub issue/label workflow, and cross-agent delivery planning.
- Proposal issues should separate current behavior from hypothetical syntax, include philosophy fit and non-goals, and use durable labels/taxonomy instead of ad-hoc status markers.
- Sample portfolio planning is not neutral demo curation; it is roadmap evidence about where the language still feels thin or overly ceremonial.
- Toy/control samples can stay in-repo for teaching, but flagship sample choices should optimize recognizability, policy density, and roadmap pressure.
- Project/board hygiene remains label-light: GitHub Project status handles lifecycle, labels handle durable ownership/slice metadata, and only true exceptions merit special labels.

## Recent Updates

### 2026-04-08 - Sample realism portfolio consolidated
- Scribe merged Steinbrenner's portfolio plan with Frank, George, and J. Peterman's sample-realism findings into .squad/decisions.md, alongside Shane's directive to use Opus when sample/design judgment is especially high.
- The active sample plan targets 42 total repo samples, rewrites the eight roadmap-anchor samples first, adds missing domain lanes next, and holds a deliberate data-only lane for proposal #22.
- trafficlight and crosswalk-signal remain useful teaching controls but should not drive language-priority decisions.

### 2026-04-05 - Proposal #8 finalized around named rules
- Synced the proposal framing to rule <Name> when <BoolExpr>, locked the field-only/boolean-only boundaries, and reinforced the readability bar for future language work.

### 2026-04-05 - Expressiveness and compactness labeling standardized
- Locked dsl-expressiveness for capability-gap proposals and dsl-compactness for ceremony-reduction work, giving the roadmap two durable slices that survive beyond one sprint.

### 2026-04-05 - Freeze-and-curate cutover became the safe team path
- Shifted trunk-return planning from branch ritual to editorial curation: freeze the exact reference SHA, cut clean integration work from main, and re-land only approved artifacts.

## Learnings

- 2026-04-08: sample planning should treat the corpus as roadmap instrumentation, not as a neutral demo shelf. Rewrite known anchor samples before inventing more examples.
- 2026-04-05: the durable repo-wide issue model is one shared lifecycle plus durable taxonomy/ownership labels, with blocked/deferred reserved for true exceptions.
- 2026-04-05: the philosophy screen for language proposals remains domain integrity, deterministic inspectability, keyword-anchored flat statements, first-match routing, compile-time soundness, and AI legibility.
- 2026-04-05: domain research still points to choice and date as the most universal type gaps; integer and currency remain lower-priority follow-ons.
- 2026-04-05: dsl-expressiveness versus dsl-compactness is a useful long-lived split for planning, labeling, and review bandwidth.
- 2026-04-05: when worktree scope narrows to one documentation artifact, use a freeze-point commit and treat the SHA—not the moving branch name—as the planning reference.
- 2026-04-05: GitHub Projects v2 requires project/read:project; once auth is correct, gh issue create --project ... is the cleanest intake path.
