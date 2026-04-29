---
last_updated: 2026-04-28T23:09:18.057-04:00
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** When a label retirement list exists in a sync workflow, verify it covers *every* superseded label family from the previous workflow model — not just the ones that were immediately obvious at migration time. **Context:** Any workflow closeout where labels are being deprecated. A partial retirement list creates silent drift between the repo's actual label state and the intended model.

**Pattern:** Always mirror the active workflow and its template copy in the same edit pass. **Context:** Any change to `.github/workflows/sync-squad-labels.yml` must be reflected in `.squad/templates/workflows/sync-squad-labels.yml` or new repos provisioned from the template start in an inconsistent state.

**Pattern:** Spike branches use the `spike/{kebab-description}` naming convention. During a spike, `spike_mode: true` is set in `.squad/identity/now.md`, all ceremony auto-triggers that require PRs are suppressed, and commits go directly to the spike branch. The spike ends with a Spike Closeout ceremony that answers: what did we learn, what do we keep, and what becomes a proper implementation PR. **Context:** Any exploratory or validation work that should not create PRs or trigger implementation gates. Spike mode is first-class — activate it deliberately via "let's start a spike on X", not by accident.
