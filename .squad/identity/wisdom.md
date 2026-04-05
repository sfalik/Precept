---
last_updated: 2026-04-04T04:06:33.491Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** When a label retirement list exists in a sync workflow, verify it covers *every* superseded label family from the previous workflow model — not just the ones that were immediately obvious at migration time. **Context:** Any workflow closeout where labels are being deprecated. A partial retirement list creates silent drift between the repo's actual label state and the intended model.

**Pattern:** Always mirror the active workflow and its template copy in the same edit pass. **Context:** Any change to `.github/workflows/sync-squad-labels.yml` must be reflected in `.squad/templates/workflows/sync-squad-labels.yml` or new repos provisioned from the template start in an inconsistent state.
