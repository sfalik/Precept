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

- Earlier archive/merge passes established the durable Scribe workflow and captured the major parser-gap, language-research, MCP, and language-tool closeout batches; the canonical technical outcomes live in `.squad/decisions.md`.
- Recent pre-batch work already recorded the message-position archive sweep, the language-tool closeout, and the Newman MCP/literal merge passes, so this history now keeps only the newest batch-level state.

## Recent Updates

### 2026-05-10T03:13:51Z — Toolchain bug batch recorded
- Pre-check measured `.squad/decisions.md` at 204405 bytes with 3 inbox file(s); the hard-gate 7-day archive pass ran first and found 1 eligible decision entry block(s).
- Merged Frank's bug-cluster analysis and Soup-Nazi's test-strategy audit into one canonical decision entry, cleared the inbox, wrote orchestration logs for `frank-10`, `soup-nazi`, and `kramer-4`, and propagated the resulting team updates into the affected agent histories.
- Health report is recorded in the session log, and Frank's oversized history was summarized back under the 15 KB gate during this pass.

### 2026-05-10T02:50:04Z — Event-italic and LS inbox batch recorded
- Pre-check measured `.squad/decisions.md` at 199037 bytes with 9 inbox files, so the hard-gate archive ran first and found 0 active entries older than 2026-04-10T02:50:04Z or 2026-05-03T02:50:04Z.
- Merged 9 inbox files into 4 canonical decision entries, wrote orchestration logs for `frank-7` and `frank-8`, recorded the batch session log, and propagated the catalog/LS prerequisite updates into George and Kramer history.
- Health report: `decisions.md` 199037B -> 204405B; `decisions-archive.md` 809100B -> 809100B; inbox processed = 9; history files summarized = 2 (`kramer/history.md`, `scribe/history.md`).

### 2026-05-09T15:26:09Z — MCP/literal batch recorded
- Pre-check measured `decisions.md` at 181475 bytes with 0 inbox files, so the 7-day archive gate ran before merge and moved 0 entries.
- Verified a concurrent Scribe pass had already cleared the inbox and merged the color/literal/currency decisions, then recorded the remaining Newman MCP decision, wrote orchestration logs for `newman-2`, `newman-3`, and `scribe-6`, and logged the health report (`decisions.md` 181475B -> 182387B; inbox processed = 0; history files summarized = 0).

### 2026-05-09T14:04:05Z — LanguageTool closeout recorded
- Pre-check measured `decisions.md` at 175232 bytes with 2 inbox files, so the hard-gate 7-day archive pass ran before merge.
- Merged the Newman completion note plus Frank's timing verdict into one canonical `precept_language` decision entry, cleared the inbox, and wrote the Newman/Soup-Nazi orchestration records plus the session log.
- Health report: `decisions.md` 175232B -> 175923B; `decisions-archive.md` stayed 809100B; history files summarized = 0.

