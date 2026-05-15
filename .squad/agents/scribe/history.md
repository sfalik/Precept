## Core Context

- Owns the squad's durable record: `.squad/decisions.md`, `.squad/decisions-archive.md`, `.squad/orchestration-log/`, `.squad/log/`, agent histories, and PR-body stewardship.
- The standing Scribe loop is fixed: measure health first, run archive gates before merges, consolidate overlapping inbox notes into canonical decisions, propagate affected history updates, and stage only the exact allowed `.squad/` paths written in-session.
- When a history file crosses the 15 KB gate, summarize it immediately and keep only the live batch state in `## Recent Updates`.

## Learnings

- Measure decision-ledger size, inbox count, and archive eligibility before every merge pass; log pre/post health explicitly.
- Deduplicate overlapping inbox notes into one canonical decision entry and name every merged source.
- Record no-op outcomes explicitly when an inbox is empty, an archive pass is ineligible, or no PR/body update is required.
- Only mark work complete in durable records when the agent output or validation confirms it.
- Oversized histories should be compressed back into durable guidance plus the newest batch state instead of carrying full chronology forever.

## Historical Summary

- Earlier Scribe passes already recorded the quantity-normalization review loop, the Slice 21/23/24 closeout batches, the review-warning gating directive, the affine conversion and cross-unit comparison decisions, and the associated health reports; those details now live canonically in `.squad/decisions.md` and the matching session logs.
- The durable Scribe baseline is unchanged: archive before merge when the size gate triggers, merge overlapping inbox files into canonical entries, clear the inbox, propagate cross-agent context, summarize oversized histories immediately, and stage only the exact `.squad/` paths written during the pass.

## Recent Updates

### 2026-05-15T20:40:13Z — Price qualifier enforcement architecture batch recorded

- Pre-check measured `.squad/decisions.md` at 53606 bytes with 9 inbox file(s); the hard-gate 7-day archive pass ran before merge and moved 0 active entries.
- Merged 9 inbox notes into 4 canonical decision entries, wrote `.squad/orchestration-log/2026-05-15T20-40-13Z-frank.md`, `.squad/orchestration-log/2026-05-15T20-40-13Z-george.md`, `.squad/orchestration-log/2026-05-15T20-40-13Z-soup-nazi.md`, and recorded `.squad/log/2026-05-15T20-40-13Z-price-qualifier-enforcement-arch.md`.
- Propagated the shipped architecture outcome into Frank / George / Soup Nazi histories, summarized Scribe history back under the 15 KB gate, and cleared the inbox.
- Health report: decisions.md 53606 B -> 57156 B; inbox processed = 9 (9 -> 0); history files summarized = `.squad/agents/scribe/history.md`.

### 2026-05-15T16:25:03Z — Review-warning gating directive merged

- Captured Shane's directive that Frank review warnings are proceed/no-proceed gates just like blockers and merged it into `.squad/decisions.md`.
- Durable process update: approval-closeout now requires both tracker sync and closure of all Frank findings before moving on.

### 2026-05-15T16:15:38Z — Slice 21/23/24 inbox batch merged and tracker synced

- Merged the Slice 21/23/24 closeout notes into canonical decisions, cleared the inbox, and recorded the resulting health report and follow-up obligations.
- Preserved the remaining Slice 19 regression/test debt as non-blocking forward context in the durable record.

### 2026-05-15T02:32:44Z — Affine conversion design batch recorded

- Logged the affine conversion design ruling, merged the inbox notes, and kept the archive pass explicit even though it moved no entries.
- Reinforced the Scribe rule that current-batch health reports belong in the paired session log, not in long-running history prose.

### 2026-05-15T02:26:33Z — Cross-unit comparison solution batch recorded

- Recorded the cross-unit comparison closeout as one canonical decision and kept the history focused on the durable outcome plus process lessons.
- Continued the pattern of trimming long chronology back into a compact durable baseline.
