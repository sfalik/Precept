## Core Context

- Owns the squad's durable record: `.squad/decisions.md`, `.squad/decisions-archive.md`, `.squad/orchestration-log/`, `.squad/log/`, agent histories, and PR-body stewardship.
- The standing Scribe loop is fixed: measure health first, run archive gates before merges, consolidate overlapping inbox notes into canonical decisions, propagate affected history updates, and stage only the exact allowed `.squad/` files written in-session.
- When a history file grows past the 15 KB gate, summarize it immediately and keep only live batch state in `## Recent Updates`.

## Learnings

- Measure decision-ledger size, inbox count, and archive eligibility before every merge pass; log pre/post health explicitly.
- Deduplicate overlapping inbox notes into one canonical decision entry and name every merged source.
- When the inbox is empty, an archive move is ineligible, or no PR exists for the branch, record the no-op/skip explicitly.
- Only check PR boxes for work that is clearly complete; uncertainty stays unchecked.
- Oversized histories must be summarized back into durable context instead of carrying every batch forward verbatim.

## Historical Summary

- Earlier archive/merge passes established the durable Scribe workflow and captured the parser-gap, research, MCP, and LanguageTool closeout batches; the canonical technical outcomes live in `.squad/decisions.md`.
- The 2026-05-09 and early 2026-05-10 passes already recorded the message-position archive sweep, Newman MCP/literal merges, event-italic and LS inbox batches, the toolchain bug batch, value-modifier closeout, and the Track 1 focus switch.
- This history now keeps only the newest batch-level state; detailed health reports stay in the matching session logs.

## Recent Updates

### 2026-05-10T12:15:36Z — Post-batch archive, inbox merge, and Soup Nazi closeout recorded
- Pre-check measured `.squad/decisions.md` at 169588 bytes with 1 inbox file; the hard-gate 7-day archive pass found 0 eligible active entries older than `2026-05-03T12:15:36Z`.
- Merged `kramer-status-bar.md` into `.squad/decisions.md`, deleted the processed inbox file, appended the resulting activation-path update to `.squad/agents/kramer/history.md`, and recorded Soup Nazi's completed grammar-regression batch in `.squad/orchestration-log/2026-05-10T12-15-36Z-soup-nazi-11.md`.
- Health report: `decisions.md` 169588B -> 170247B; `decisions-archive.md` 869467B -> 869467B; inbox processed = 1; history files summarized = 0.

### 2026-05-10T12:15:36Z — Kramer grammar-color batch recorded
- Pre-check measured `.squad/decisions.md` at 444287 bytes with 13 inbox file(s); the 7-day archive hard gate ran before the inbox merge because the ledger was still over the 50 KB threshold.
- Recorded the `kramer-26` orchestration result, merged the remaining inbox backlog into `decisions.md`, summarized `kramer/history.md`, and logged health: `decisions.md` 444287B -> 445246B; inbox processed = 13; history files summarized = 1 (`.squad/agents/kramer/history.md`).

### 2026-05-10T04:36:29Z — Slice 13 completion batch recorded
- Pre-check measured `.squad/decisions.md` at 160862 bytes with 2 inbox file(s); the 7-day archive hard gate ran first and found 0 eligible active entries older than `2026-05-03T04:36:29Z`.
- This pass merged the Slice 13 inbox note into one canonical decision entry, deleted the processed inbox file, wrote orchestration records for `kramer-9`, `soup-nazi-3`, and the coordinator closeout batch, and propagated the resulting updates into the relevant agent histories.
- Health report: `decisions.md` 160862B -> 161293B; `decisions-archive.md` 869467B -> 869467B; inbox processed = 1 (2 -> 1); history files summarized = 1 (`.squad/agents/soup-nazi/history.md`).

### 2026-05-10T04:33:18Z — Track 1 focus-switch batch recorded
- Pre-check measured `.squad/decisions.md` at 152750 bytes with 14 inbox file(s); the 7-day archive hard gate ran first and found 0 eligible active entries older than `2026-05-03T04:33:18Z`.
- This pass merged 13 inbox notes into 6 canonical decisions, wrote the `kramer-8` and coordinator orchestration records, and kept Track 1 as the exclusive execution lane.

